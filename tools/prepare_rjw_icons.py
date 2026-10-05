#!/usr/bin/env python3
"""Import built-in image_gen outputs and resize/pad them for the game.

Requires Pillow only for asset preparation. No drawing, recoloring, background
removal or image generation occurs here. Tinting is done by RimWorld at runtime.
"""
import argparse
import base64
import hashlib
import html
import json
from pathlib import Path
import shutil

from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Artwork/RimJobWorld'


def import_assets(jobs):
    records = []
    for job in jobs:
        style, asset = job['key'].split('/')
        if style not in ('Vanilla', 'BionicIcons') or not asset.replace('_', '').isalnum():
            raise ValueError(f'Invalid asset key: {job["key"]}')
        original = Path(job['generated_path'])
        raw_copy = ROOT / 'work/rjw-icon-generation/originals' / style / (asset + '.png')
        raw_copy.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(original, raw_copy)
        target = ROOT / f'Integrations/RimJobWorld/{style}/Textures/KoRimUtility/RJW/{style}/{asset}.png'
        target.parent.mkdir(parents=True, exist_ok=True)
        with Image.open(original) as source:
            if source.mode != 'RGBA' or source.getchannel('A').getextrema() != (0, 255):
                raise ValueError(f'Generator did not return true transparency: {original}')
            source_size = list(source.size)
            output = ImageOps.pad(source, (128, 128), method=Image.Resampling.LANCZOS, color=(0, 0, 0, 0))
            if any(output.getpixel(p)[3] for p in ((0, 0), (127, 0), (0, 127), (127, 127))):
                raise ValueError(f'Artwork reaches a canvas corner: {original}')
            output.save(target, optimize=True)
        records.append({
            'key': job['key'], 'tool': 'built-in image_gen', 'prompt': job['prompt'],
            'references': job['references'], 'source_size': source_size,
            'source_sha256': hashlib.sha256(original.read_bytes()).hexdigest(),
            'file': target.relative_to(ROOT).as_posix(),
            'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
            'conversion': 'aspect-preserving Lanczos downscale and transparent square padding to 128x128 RGBA',
        })
    (ART / 'generation-prompts.json').write_text(json.dumps({
        'created_on': '2026-10-05', 'assets': records,
    }, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    return records


def gallery():
    manifest = json.loads((ART / 'icon-manifest.json').read_text(encoding='utf-8'))
    tiers = {'Natural': '일반·자연', 'Prosthetic': '의수·의족급', 'Bionic': '생체공학', 'Archotech': '초월공학', 'Material': '재료'}
    parts = {'Male': '남성 부위', 'Female': '여성 부위', 'Breast': '가슴', 'Anus': '괄약근',
             'FeaturelessChest': '평평한 흉부', 'ResinGlob': '수지', 'SlimeGlob': '슬라임'}
    by_name = {(v['style'], v['variant']): v for v in manifest['variants']}
    filters, cards = [], []
    ordered = sorted(manifest['variants'], key=lambda v: (list(tiers).index(v['tier']), list(parts).index(v['part']), v['style']))
    for index, entry in enumerate(ordered):
        if entry['style'] != 'Vanilla':
            continue
        visuals = []
        for style in ('Vanilla', 'BionicIcons'):
            v = by_name[style, entry['variant']]
            uid = f'tint{index}{style}'
            rgb = [x / 255 for x in v['color_rgb']]
            filters.append(f'<filter id="{uid}" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values="{rgb[0]} 0 0 0 0 0 {rgb[1]} 0 0 0 0 0 {rgb[2]} 0 0 0 0 0 1 0"/></filter>')
            uri = 'data:image/png;base64,' + base64.b64encode((ROOT / v['file']).read_bytes()).decode()
            visuals.append(f'<div class="sample"><img alt="{html.escape(style + " " + entry["variant"])}" src="{uri}" style="filter:url(#{uid})"><span>{"기본 게임" if style == "Vanilla" else "Bionic icons"}</span></div>')
        label = parts[entry['part']]
        cards.append(f'<article data-tier="{entry["tier"]}"><div class="card-heading"><strong>{label}</strong><small>{tiers[entry["tier"]]}</small></div><div class="pair">{"".join(visuals)}</div><details><summary>연결된 아이템 {len(entry["def_names"])}개</summary><p>{html.escape(", ".join(entry["def_names"]))}</p></details></article>')
    document = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>RJW 의료 부품 · 생성 아이콘 비교</title>
<style>*{box-sizing:border-box}body{margin:0;background:#13191d;color:#e6ecea;font:15px/1.6 system-ui,"Malgun Gothic",sans-serif}main{max-width:1100px;margin:auto;padding:28px 22px}h1{font-size:26px;line-height:1.35;margin:8px 0 12px}p{color:#b5c4c5}.eyebrow{font-size:12px;letter-spacing:2px;color:#a1caaa}.badges{display:flex;gap:8px;flex-wrap:wrap;margin:18px 0}.badges span{background:#263237;border:1px solid #405352;border-radius:5px;padding:5px 10px}nav{display:flex;gap:8px;flex-wrap:wrap;margin:22px 0}button{background:#263237;color:#dce7e2;border:1px solid #52645c;padding:8px 12px;border-radius:5px;cursor:pointer}button[aria-pressed=true]{background:#adc9ae;color:#132119}#grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:16px}article{border:1px solid #405052;border-radius:8px;overflow:hidden;background:#20292e}.card-heading{padding:13px 16px;display:flex;justify-content:space-between;border-bottom:1px solid #39484a}.card-heading small{color:#b2c7b6}.pair{display:flex;justify-content:center;gap:10px;background:#283238;padding:8px}.sample{width:46%;text-align:center}.sample img{display:block;width:128px;height:128px;max-width:100%;object-fit:contain;margin:auto}.sample span{color:#bdcac8;font-size:12px}details{padding:9px 14px;font-size:12px;color:#acbfbb}details p{overflow-wrap:anywhere}footer{border-top:1px solid #45524f;margin-top:24px;padding-top:16px;font-size:12px;color:#afbfbb}a{color:#bbdbbc}article[hidden]{display:none}.filters{position:absolute;width:0;height:0;overflow:hidden}@media(max-width:620px){main{padding:22px 14px}#grid{grid-template-columns:1fr}h1{font-size:23px}}</style>
<svg class="filters" aria-hidden="true"><defs>FILTERS</defs></svg><main><div class="eyebrow">KORIM UTILITY · MEDICAL ITEMS</div><h1>같은 부품, 환경에 맞는 두 가지 아이콘</h1><p>기본 게임은 얇은 의료 상자, Bionic icons는 정사각형 케이스.<br>모드 활성 상태에 따라 해당 이미지와 색상이 자동 적용됩니다.</p><div class="badges"><span>46개 아이템</span><span>19종 × 2가지 스타일</span><span>투명 PNG 18개 + 게임 색상</span></div><nav aria-label="등급 필터">BUTTONS</nav><div id="grid">CARDS</div><footer>이미지 생성: 내장 image_gen. 게임용 PNG: 128 × 128 RGBA.<br>미리보기에는 XML의 색상 곱셈을 적용했습니다. 실제 게임 렌더링을 캡처한 화면은 아닙니다. 기본 게임용 drawSize 0.80, Bionic icons용 1.0이며 여기서는 같은 캔버스로 비교합니다.<br>고유 Def 이름·수치·레시피·저장 데이터는 유지하며, 아이템 graphicData만 교체합니다. <a href="index.html">이전 원본 조사 보기</a></footer></main><script>document.querySelectorAll('nav button').forEach(button=>button.addEventListener('click',()=>{document.querySelectorAll('nav button').forEach(x=>x.setAttribute('aria-pressed',String(x===button)));document.querySelectorAll('article').forEach(x=>x.hidden=button.dataset.tier!=='all'&&x.dataset.tier!==button.dataset.tier)}));</script></html>'''
    buttons = '<button data-tier="all" aria-pressed="true">전체</button>' + ''.join(f'<button data-tier="{k}" aria-pressed="false">{v}</button>' for k, v in tiers.items())
    document = document.replace('FILTERS', ''.join(filters)).replace('BUTTONS', buttons).replace('CARDS', ''.join(cards))
    output = ROOT / 'work/rjw-icon-audit/generated.html'
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(document, encoding='utf-8')
    print(f'Preview: {output}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--jobs', type=Path, required=True)
    args = parser.parse_args()
    records = import_assets(json.loads(args.jobs.read_text(encoding='utf-8')))
    gallery()
    print(f'Imported {len(records)} generated images; original alpha preserved.')
