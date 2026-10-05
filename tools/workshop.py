#!/usr/bin/env python3
"""Prepare a Workshop update, or upload it with a cached SteamCMD session."""
import argparse
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[1]
STAGING = ROOT / 'dist/workshop'
MANIFEST = STAGING / 'item.vdf'


def item_id(value):
    if not re.fullmatch(r'[1-9][0-9]*', value or '') or int(value) >= 2**64:
        raise ValueError('STEAM_WORKSHOP_ID에 기존 KoRim Utility 게시물의 숫자 ID를 설정하세요. 0(신규 생성)은 허용하지 않습니다.')
    return value


def quote(value):
    if any(ord(char) < 32 and char not in '\n\r\t' for char in value):
        raise ValueError('VDF 값에 지원하지 않는 제어 문자가 있습니다.')
    # Workshop KeyValues preserves literal newlines; backslash-n is published as text.
    return '"' + value.replace('\\', '\\\\').replace('"', '\\"').replace('\r', '') + '"'


def prepare(archive, destination, published_id, change_note, description):
    published_id = item_id(published_id)
    if destination.exists():
        raise ValueError(f'새 스테이징 폴더가 필요합니다: {destination}')
    with ZipFile(archive) as zipped:
        files = {}
        for info in zipped.infolist():
            path = PurePosixPath(info.filename)
            if path.is_absolute() or '..' in path.parts or '\\' in info.filename or path.parts[0] != 'KoRimUtility':
                raise ValueError('KoRimUtility ZIP 내부 경로가 올바르지 않습니다.')
            if info.is_dir():
                continue
            if len(path.parts) < 2 or info.filename in files:
                raise ValueError('중복 또는 잘못된 ZIP 파일 경로입니다.')
            files[info.filename] = zipped.read(info)
        about = ET.fromstring(files['KoRimUtility/About/About.xml'])
        if about.findtext('packageId') != 'snowykte0426.korimutility':
            raise ValueError('KoRim Utility 통합 패키지가 아닙니다.')
        preview = files['KoRimUtility/About/Preview.png']
        if not preview.startswith(b'\x89PNG\r\n\x1a\n') or len(preview) >= 1024 * 1024:
            raise ValueError('미리보기는 1 MiB 미만의 PNG여야 합니다.')
    content = destination.resolve() / 'KoRimUtility'
    fields = {
        'appid': '294100',
        'publishedfileid': published_id,
        'contentfolder': str(content),
        'previewfile': str(content / 'About/Preview.png'),
        'title': about.findtext('name'),
        'description': description,
        'changenote': change_note,
        # Omitting visibility preserves the existing Workshop item's visibility.
    }
    manifest = '"workshopitem"\n{\n' + ''.join(f'  {quote(k)} {quote(v)}\n' for k, v in fields.items()) + '}\n'
    for name, data in files.items():
        path = destination / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
    (content / 'About/PublishedFileId.txt').write_text(published_id + '\n', encoding='utf-8')
    (destination / 'item.vdf').write_text(manifest, encoding='utf-8')
    print(f'업로드 준비 완료: {content} (기존 항목 {published_id})')


def plain_steam_output(output):
    # Linux SteamCMD emits ANSI colors even when its output is redirected.
    return re.sub(r'\x1b\[[0-?]*[ -/]*[@-~]', '', output)


def upload_succeeded(output, published_id):
    # Older clients include the ID. Current clients print a standalone Success.
    # after Preparing update; quick uploads may skip intermediate progress labels.
    # The caller verifies the manifest target both before and after SteamCMD.
    output = plain_steam_output(output)
    pattern = r'Success\.\s+Published\s+(?:item\s+|File ID:\s*)' + re.escape(published_id) + r'\b'
    if re.search(r'Success[.!]?\s+Published\b', output, re.IGNORECASE):
        return re.search(pattern, output, re.IGNORECASE) is not None
    progress = re.search(r'Preparing update\.\.\..*?\bSuccess[.!]?[ \t]*(?:\r?\n|$)',
                         output, re.IGNORECASE | re.DOTALL)
    return progress is not None and 'error!' not in progress.group(0).lower()


def upload_diagnostic(output, returncode):
    """Report fixed status labels only, never Steam's raw text or credentials."""
    had_ansi = '\x1b[' in output
    output = plain_steam_output(output)
    lowered = output.lower()
    signals = {
        'ansi_present': had_ansi,
        'cached_credentials_missing': 'cached credentials not found' in lowered,
        'using_cached_credentials': 'using cached credentials' in lowered,
        'login_completed': 'waiting for user info...ok' in lowered,
        'upload_started': 'uploading content' in lowered or 'preparing update' in lowered,
        'success_message_seen': bool(re.search(r'\bSuccess[.!]?(?=\s|$)', output, re.IGNORECASE)),
        'success_word_present': 'success' in lowered,
        'error_word_present': 'error' in lowered,
        'invalid_password': 'invalid password' in lowered,
        'guard_required': any(text in lowered for text in (
            'account logon denied', 'two-factor code', 'enter the current code',
            'steam guard code', 'authenticator code')),
        'connection_failed': any(text in lowered for text in (
            'no connection', 'failed to connect', 'connection timeout')),
        'access_denied': 'access denied' in lowered or 'insufficient privilege' in lowered,
    }
    # A fixed vocabulary reveals the Workshop failure stage without copying
    # account names, paths, IDs, tokens, or arbitrary Steam output into CI logs.
    vocabulary = {'preparing', 'update', 'content', 'uploading', 'preview', 'image',
                  'committing', 'success', 'successfully', 'published', 'file', 'item',
                  'workshop', 'ok', 'error', 'failed', 'complete', 'completed', 'invalid',
                  'parameter', 'password', 'access', 'denied', 'timeout', 'limit',
                  'exceeded', 'quota', 'service', 'unavailable', 'failure', 'result'}
    start = lowered.rfind('preparing update')
    stage = lowered[start:] if start >= 0 else ''
    tokens = [word for word in re.findall(r'[a-z]+', stage) if word in vocabulary][:60]
    return (f'exit={returncode}; ' + '; '.join(f'{key}={int(value)}' for key, value in signals.items())
            + '; workshop_stage_tokens=' + ','.join(tokens))


def upload(steamcmd, manifest, published_id):
    published_id = item_id(published_id)
    username = os.environ.get('STEAM_USERNAME', '')
    session = os.environ.get('STEAM_CONFIG_VDF', '')
    if not re.fullmatch(r'[A-Za-z0-9_]+', username) or not session.strip():
        raise ValueError('STEAM_USERNAME과 STEAM_CONFIG_VDF Secrets를 설정하세요.')
    config_text = manifest.read_text(encoding='utf-8')
    if re.findall(r'"publishedfileid"\s+"([0-9]+)"', config_text) != [published_id]:
        raise ValueError('업로드 대상 ID가 준비된 manifest와 다릅니다.')
    if re.findall(r'"appid"\s+"([0-9]+)"', config_text) != ['294100']:
        raise ValueError('RimWorld용 manifest가 아닙니다.')
    steamcmd = steamcmd.resolve(strict=True)
    config = steamcmd.parent / 'config/config.vdf'
    if config.exists():
        raise ValueError('기존 인증 파일을 덮어쓰지 않습니다. CD 전용의 새 SteamCMD 폴더를 사용하세요.')
    config.parent.mkdir(parents=True, exist_ok=True)
    with config.open('x', encoding='utf-8') as stream:
        config.chmod(0o600)
        stream.write(session)
    environment = {k: v for k, v in os.environ.items() if k != 'STEAM_CONFIG_VDF'}
    command = [str(steamcmd), '+@ShutdownOnFailedCommand', '1', '+@NoPromptForPassword', '1',
               '+login', username, '+workshop_build_item', str(manifest.resolve()), '+quit']
    try:
        # Do not stream or upload raw Steam logs, which can contain session details.
        result = subprocess.run(command, cwd=steamcmd.parent, env=environment,
                                stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, errors='replace', timeout=600)
        final_target = re.findall(r'"publishedfileid"\s+"([0-9]+)"', manifest.read_text(encoding='utf-8'))
        if final_target != [published_id]:
            raise ValueError('Steam 업로드 성공을 확인하지 못했습니다: 업로드 후 manifest의 대상 ID가 달라졌습니다.')
        if result.returncode != 0 or not upload_succeeded(result.stdout, published_id):
            raise ValueError('Steam 업로드 성공을 확인하지 못했습니다. '
                             + upload_diagnostic(result.stdout, result.returncode)
                             + '. Steam Guard 세션, 항목 소유권 및 네트워크를 확인하세요. 자동 재시도하지 않습니다.')
    finally:
        config.unlink(missing_ok=True)
    print(f'창작마당 업데이트 완료: https://steamcommunity.com/sharedfiles/filedetails/?id={published_id}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    commands.add_parser('prepare')
    deploy = commands.add_parser('upload')
    deploy.add_argument('--steamcmd', type=Path, required=True)
    args = parser.parse_args()
    try:
        published_id = os.environ.get('STEAM_WORKSHOP_ID', '')
        if args.command == 'prepare':
            note = f"KoRim Utility {os.environ.get('BUILD_REF', 'manual')} ({os.environ.get('BUILD_SHA', 'local')[:12]})"
            prepare(ROOT / 'dist/KoRimUtility.zip', STAGING, published_id, note,
                    (ROOT / 'distribution/Workshop-Core.bbcode').read_text(encoding='utf-8'))
        else:
            upload(args.steamcmd, MANIFEST, published_id)
    except (ValueError, OSError, KeyError, ET.ParseError, subprocess.TimeoutExpired) as exc:
        if isinstance(exc, subprocess.TimeoutExpired):
            print('오류: SteamCMD 시간 초과. Steam Guard 세션을 갱신하세요.', file=sys.stderr)
        else:
            print(f'오류: {exc}', file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
