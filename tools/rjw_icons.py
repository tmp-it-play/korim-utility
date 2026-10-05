#!/usr/bin/env python3
"""Build RJW visual-only XML mappings from the audited item inventory.

The artwork itself is created with the built-in image generator. This script
only assembles XML/metadata; it does not draw or modify artwork.
"""
import json
from collections import defaultdict
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / 'Artwork/RimJobWorld/icon-audit.json'
MANIFEST = ROOT / 'Artwork/RimJobWorld/icon-manifest.json'
COLORS = {
    'Natural': (190, 190, 190),
    'Prosthetic': (154, 124, 104),
    'Bionic': (189, 169, 118),
    'Archotech': (155, 165, 148),
}
STYLES = ('Vanilla', 'BionicIcons')


def build():
    audit = json.loads(AUDIT.read_text(encoding='utf-8'))
    groups = defaultdict(list)
    for item in audit['items']:
        groups[item['proposed_tex_path'].rsplit('/', 1)[1]].append(item['def_name'])
    variants = []
    for style in STYLES:
        patch = ET.Element('Patch')
        sequence = ET.SubElement(patch, 'Operation', Class='PatchOperationSequence')
        operations = ET.SubElement(sequence, 'operations')
        for name, def_names in sorted(groups.items()):
            material = name in ('ResinGlob', 'SlimeGlob')
            tier, part = ('Material', name) if material else name.split('_', 1)
            part = 'FeaturelessChest' if part == 'Chest' else part
            asset = part
            color = (255, 255, 255) if material else COLORS[tier]
            if style == 'BionicIcons' and tier == 'Archotech':
                asset = 'Archotech_' + part
                color = (255, 255, 255)  # Green band and casing are already painted.
            tex_path = f'KoRimUtility/RJW/{style}/{asset}'
            size = '0.80' if style == 'Vanilla' else '1.0'
            selector = 'Defs/ThingDef[' + ' or '.join(f'defName="{d}"' for d in sorted(def_names)) + ']'
            # Patches run before XML inheritance. Some concrete defs already
            # have graphicData, while other members inherit it. Remove only
            # existing concrete graphics, then give every selected def one
            # complete local graphic, without changing their other inheritance.
            conditional = ET.SubElement(operations, 'li', Class='PatchOperationConditional')
            ET.SubElement(conditional, 'xpath').text = selector + '/graphicData'
            remove = ET.SubElement(conditional, 'match', Class='PatchOperationRemove')
            ET.SubElement(remove, 'xpath').text = selector + '/graphicData'
            add = ET.SubElement(operations, 'li', Class='PatchOperationAdd')
            ET.SubElement(add, 'xpath').text = selector
            value = ET.SubElement(add, 'value')
            graphic = ET.SubElement(value, 'graphicData', Inherit='false')
            ET.SubElement(graphic, 'texPath').text = tex_path
            ET.SubElement(graphic, 'graphicClass').text = 'Graphic_Single'
            ET.SubElement(graphic, 'shaderType').text = 'Cutout'
            ET.SubElement(graphic, 'color').text = '(' + ','.join(map(str, color)) + ')'
            ET.SubElement(graphic, 'drawSize').text = f'({size},{size})'
            variants.append({
                'style': style, 'variant': name, 'tier': tier, 'part': part,
                'def_names': sorted(def_names), 'tex_path': tex_path,
                'color_rgb': list(color), 'draw_size': float(size),
                'file': f'Integrations/RimJobWorld/{style}/Textures/{tex_path}.png',
            })
        path = ROOT / f'Integrations/RimJobWorld/{style}/Patches/MedicalIcons.xml'
        path.parent.mkdir(parents=True, exist_ok=True)
        ET.indent(patch, space='  ')
        path.write_bytes(ET.tostring(patch, encoding='utf-8', xml_declaration=True) + b'\n')
    manifest = {
        'game_version': '1.6', 'rjw_version_audited': audit['rjw_version'],
        'generator': 'built-in image_gen; one call per asset, followed by PNG size conversion only',
        'activation': {
            'Vanilla': 'rim.job.world active AND automatic.bionicicons inactive',
            'BionicIcons': 'rim.job.world AND automatic.bionicicons active',
        },
        'item_count_per_style': len(audit['items']),
        'visual_variants_per_style': len(groups),
        'unique_png_count': len({v['file'] for v in variants}),
        'variants': variants,
    }
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'{len(groups)} variants x {len(STYLES)} styles; {manifest["unique_png_count"]} PNG assets; {len(audit["items"])} items per style')


if __name__ == '__main__':
    build()
