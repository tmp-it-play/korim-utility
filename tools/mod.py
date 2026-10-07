#!/usr/bin/env python3
"""Local XML translation workflow; Python 3.10+, no third-party dependencies."""
import argparse
from collections import Counter
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
from zipfile import ZIP_DEFLATED, ZipFile

ROOT = Path(__file__).resolve().parents[1]
RUNTIME_DIRS = ('About', 'Languages', 'Defs', 'Patches', 'Textures', 'Sounds')
KEY = re.compile(r'^[A-Za-z_][A-Za-z0-9_.-]*$')
TOKENS = re.compile(r'(?<!\{)\{[^{}]+\}(?!\})|\[[A-Za-z_][A-Za-z0-9_.]*\]|</?[A-Za-z][^<>]*>')
COMPATIBILITY_ASSEMBLIES = {
    'Translations/CharacterEditor/Assemblies/KoRimUtility.CharacterEditor.dll': 'Character Editor 번역',
    'Integrations/RimJobWorld/Common/Assemblies/KoRimUtility.MedicalIcons.dll': '의료 아이콘 호환',
    'Integrations/MainButtons/Assemblies/KoRimUtility.MainButtons.dll': '하단 메뉴 UI',
    'Translations/FoodAlertContinued/Assemblies/KoRimUtility.FoodAlert.dll': 'Food Alert 번역',
    'Translations/RimJobWorld/Assemblies/KoRimUtility.RimJobWorld.dll': 'RJW 알림 번역',
    'Integrations/SlaveSuppression/Assemblies/KoRimUtility.SlaveSuppression.dll': '노예 억압',
}


def tokens(text):
    """Conservative comparison: placeholders, simple grammar refs and rich text."""
    return Counter(TOKENS.findall(text))


def language_scope(path):
    if len(path.parts) >= 2 and path.parts[0] == 'Keyed':
        return 'Keyed'
    if len(path.parts) >= 3 and path.parts[0] == 'DefInjected':
        return '/'.join(path.parts[:2])
    raise ValueError(f'지원하지 않는 번역 경로: {path}')


def read_language(folder):
    """Index by (Keyed/DefInjected type, key), independent of XML filenames."""
    entries = {}
    for path in sorted(folder.rglob('*.xml')):
        rel = path.relative_to(folder)
        if rel.parts[0] not in ('Keyed', 'DefInjected'):
            continue
        scope = language_scope(rel)
        root = ET.parse(path).getroot()
        if root.tag != 'LanguageData':
            raise ValueError(f'{path}: 루트는 LanguageData여야 합니다.')
        for node in root:
            identity = (scope, node.tag)
            if identity in entries:
                raise ValueError(f'{path}: 중복 키 {scope}/{node.tag}')
            if len(node) or not KEY.fullmatch(node.tag):
                raise ValueError(f'{path}: 잘못된 키/중첩 태그 {node.tag}; 서식 태그는 XML 이스케이프가 필요합니다.')
            value = node.text or ''
            if not value.strip() or value.strip() == 'TODO':
                raise ValueError(f'{path}: 빈 번역 또는 TODO: {node.tag}')
            entries[identity] = {'file': rel.as_posix(), 'key': node.tag, 'text': value}
    return entries


def compare(source, translated):
    for identity in source.keys() & translated.keys():
        if tokens(source[identity]['text']) != tokens(translated[identity]['text']):
            raise ValueError(f'치환 변수/서식 불일치: {identity}')


def content_roots(root):
    """All declared content roots for validation/packaging, including optional ones."""
    manifest = root / 'LoadFolders.xml'
    if not manifest.exists():
        return [root]
    tree = ET.parse(manifest).getroot()
    if tree.tag != 'loadFolders':
        raise ValueError('LoadFolders.xml 루트는 loadFolders여야 합니다.')
    versions = ET.parse(root / 'About/About.xml').findall('supportedVersions/li')
    resolved_root = root.resolve()
    roots = []
    for version in versions:
        section = tree.find('v' + version.text.strip())
        if section is None:
            raise ValueError(f'로드 폴더 정의 누락: {version.text}')
        for node in section.findall('li'):
            relative = (node.text or '').strip()
            path = root if relative in ('', '/') else root / relative
            resolved = path.resolve()
            if not resolved.is_relative_to(resolved_root) or not path.is_dir():
                raise ValueError(f'잘못된 로드 폴더: {relative}')
            if path not in roots:
                roots.append(path)
    return roots


def combined_language(roots, language):
    entries = {}
    files = set()
    for root in roots:
        folder = root / 'Languages' / language
        relative_files = {path.relative_to(folder).as_posix().lower() for path in folder.rglob('*.xml')}
        # RimWorld merges a mod's load folders by relative file path before reading keys.
        # A later XML with the same path hides the entire earlier file.
        duplicate_files = files & relative_files
        if duplicate_files:
            raise ValueError(f'로드 폴더 간 언어 파일 경로 중복: {sorted(duplicate_files)}')
        files.update(relative_files)
        data = read_language(folder)
        duplicate = entries.keys() & data.keys()
        if duplicate:
            raise ValueError(f'로드 폴더 간 중복 키: {sorted(duplicate)}')
        entries.update(data)
    return entries


def check(root=ROOT):
    about = ET.parse(root / 'About/About.xml').getroot()
    if about.tag != 'ModMetaData':
        raise ValueError('About.xml 루트는 ModMetaData여야 합니다.')
    for tag in ('name', 'author', 'packageId', 'supportedVersions/li'):
        if not (about.findtext(tag) or '').strip():
            raise ValueError(f'About.xml 필수 항목 누락: {tag}')
    roots = content_roots(root)
    for content in roots:
        for dirname in RUNTIME_DIRS:
            for path in (content / dirname).rglob('*.xml'):
                ET.parse(path)
    english = combined_language(roots, 'English')
    korean = combined_language(roots, 'Korean')
    compare(english, korean)
    for content in roots:
        reference = content / 'TranslationReference/KoreanSource'
        if reference.is_dir():
            original = read_language(reference)
            translated = read_language(content / 'Languages/Korean')
            compare(original, translated)
            if original.keys() != translated.keys():
                raise ValueError(f'외부 번역과 기록한 원문 키가 일치하지 않습니다: {content}')
    missing = english.keys() - korean.keys()
    if missing:
        raise ValueError(f'모드 자체 한국어 번역 누락: {sorted(missing)}')
    print(f'검증 통과: 자체 영문 {len(english)}개, 한국어 {len(korean)}개. 게임 내 동작 검증은 별도입니다.')


def catalog(source, output):
    if not source.is_dir():
        raise ValueError(f'언어 폴더를 찾을 수 없습니다: {source}')
    data = read_language(source)
    if not data:
        raise ValueError('Keyed/DefInjected 원문이 없습니다. Languages/English 폴더를 지정하세요.')
    if output.exists():
        raise ValueError(f'기존 작업 파일을 덮어쓰지 않습니다: {output}')
    rows = [dict(file=x['file'], key=x['key'], english=x['text'], korean='') for x in data.values()]
    output.parent.mkdir(parents=True, exist_ok=True)
    payload = {'source': str(source.resolve()), 'entries': rows}
    output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'원문 {len(rows)}개 추출: {output} (korean 값을 작성하세요)')


def render_catalog(path, output):
    """Create a new staging language directory; never overwrite existing work."""
    if output.exists():
        raise ValueError(f'새 출력 폴더를 지정하세요: {output}')
    payload = json.loads(path.read_text(encoding='utf-8'))
    source = Path(payload['source'])
    if not source.is_dir():
        raise ValueError(f'원문 대조를 위해 원래 언어 폴더가 필요합니다: {source}')
    originals = read_language(source)
    files = {}
    seen = set()
    count = 0
    for row in payload['entries']:
        rel = Path(row['file'])
        if rel.is_absolute() or '..' in rel.parts or '\\' in row['file'] or rel.suffix != '.xml':
            raise ValueError(f'잘못된 파일 경로: {rel}')
        identity = (language_scope(rel), row['key'])
        if identity in seen or not KEY.fullmatch(row['key']):
            raise ValueError(f'중복/잘못된 키: {identity}')
        seen.add(identity)
        original = originals.get(identity)
        if original is None or original['text'] != row['english']:
            raise ValueError(f'원문 변경/키 불일치: {identity}. 최신 원문을 확인하세요.')
        text = row['korean']
        if not isinstance(text, str):
            raise ValueError(f'번역은 문자열이어야 합니다: {identity}')
        if not text.strip():
            continue
        if text.strip() == 'TODO' or tokens(row['english']) != tokens(text):
            raise ValueError(f'TODO 또는 치환 변수/서식 불일치: {identity}')
        language = files.setdefault(rel, ET.Element('LanguageData'))
        ET.SubElement(language, row['key']).text = text
        count += 1
    if not count:
        raise ValueError('완료된 번역이 없습니다. korean 값을 작성하세요.')
    # Serialize and reparse all files before writing anything (reject invalid XML characters).
    serialized = {}
    for rel, language in files.items():
        ET.indent(language, space='  ')
        blob = ET.tostring(language, encoding='utf-8', xml_declaration=True) + b'\n'
        ET.fromstring(blob)
        serialized[rel] = blob
    for rel, blob in serialized.items():
        target = output / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(blob)
    print(f'완료 번역 {count}개 생성: {output}; 미번역 {len(seen) - count}개 제외')


def package_files(root, include_ui=False):
    files = []
    for name, label in COMPATIBILITY_ASSEMBLIES.items():
        compatibility = root / name
        if not compatibility.parent.parent.is_dir():
            continue
        if not compatibility.is_file():
            raise ValueError(f'{label} DLL이 없습니다. 먼저 make compatibility를 실행하세요.')
        files.append(compatibility)
    for content in content_roots(root):
        for dirname in RUNTIME_DIRS:
            files.extend(p for p in (content / dirname).rglob('*') if p.is_file() and not p.name.startswith('.'))
    for name in ('LoadFolders.xml', 'LICENSE', 'NOTICE.txt'):
        if (root / name).is_file():
            files.append(root / name)
    files.extend(p for p in (root / 'ThirdPartyNotices').rglob('*.txt') if p.is_file())
    if include_ui:
        dll = root / 'Assemblies/KoRimUtility.dll'
        if not dll.is_file():
            raise ValueError('UI DLL이 없습니다. 먼저 선택적 C# 프로젝트를 빌드하세요.')
        files.append(dll)
    return sorted(set(files))


def pack(include_ui=False, root=ROOT):
    check(root)
    files = package_files(root, include_ui)
    folder_name = 'KoRimUtility' if root == ROOT else root.name
    output = ROOT / 'dist' / (folder_name + '.zip')
    output.parent.mkdir(exist_ok=True)
    with ZipFile(output, 'w', ZIP_DEFLATED) as archive:
        for path in files:
            archive.write(path, folder_name + '/' + path.relative_to(root).as_posix())
    print(f'패키지 생성: {output} ({len(files)}개 파일)')


def install(mods_dir, root=ROOT):
    if not mods_dir:
        raise ValueError('MODS_DIR 또는 --mods-dir로 실제 게임의 Mods 폴더를 지정하세요.')
    parent = Path(mods_dir).expanduser().resolve()
    if not parent.is_dir() or parent.name != 'Mods':
        raise ValueError(f'기존 Mods 폴더가 필요합니다: {parent}')
    target = parent / ('KoRimUtility' if root == ROOT else root.name)
    if target.is_symlink() and target.resolve() == root:
        print(f'이미 연결되어 있습니다: {target}')
        return
    if target.exists() or target.is_symlink():
        raise ValueError(f'기존 설치를 덮어쓰지 않습니다: {target}')
    if parent == root or root in parent.parents:
        raise ValueError('프로젝트 내부에는 설치할 수 없습니다.')
    check(root)
    target.symlink_to(root, target_is_directory=True)
    print(f'개발용 링크 생성: {target} -> {root}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    validate = commands.add_parser('check')
    validate.add_argument('--root', type=Path, default=ROOT)
    build = commands.add_parser('pack')
    build.add_argument('--root', type=Path, default=ROOT)
    build.add_argument('--include-ui', action='store_true')
    deploy = commands.add_parser('install')
    deploy.add_argument('--root', type=Path, default=ROOT)
    deploy.add_argument('--mods-dir', required=True)
    scan = commands.add_parser('catalog', help='Read an explicitly selected English language folder')
    scan.add_argument('--source', type=Path, required=True)
    scan.add_argument('--output', type=Path, required=True)
    render = commands.add_parser('render', help='Validate translations and write a NEW staging folder')
    render.add_argument('--catalog', type=Path, required=True)
    render.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.command == 'check':
            check(args.root.resolve())
        elif args.command == 'pack':
            pack(args.include_ui, args.root.resolve())
        elif args.command == 'install':
            install(args.mods_dir, args.root.resolve())
        elif args.command == 'catalog':
            catalog(args.source, args.output)
        elif args.command == 'render':
            render_catalog(args.catalog, args.output)
    except (ValueError, OSError, ET.ParseError, KeyError, TypeError) as exc:
        print(f'오류: {exc}', file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
