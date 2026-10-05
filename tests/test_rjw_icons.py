"""Check the complete item mapping, optional artwork and visual-only boundary."""
from collections import Counter
import json
from pathlib import Path
import re
import struct
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


class RjwIconTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.audit = json.loads((ROOT / 'Artwork/RimJobWorld/icon-audit.json').read_text(encoding='utf-8'))
        cls.manifest = json.loads((ROOT / 'Artwork/RimJobWorld/icon-manifest.json').read_text(encoding='utf-8'))

    def test_every_audited_item_has_one_graphic_in_each_style(self):
        expected = Counter(x['def_name'] for x in self.audit['items'])
        self.assertEqual(len(expected), 46)
        for style in ('Vanilla', 'BionicIcons'):
            patch = ET.parse(ROOT / f'Integrations/RimJobWorld/{style}/Patches/MedicalIcons.xml')
            adds = patch.findall('.//li[@Class="PatchOperationAdd"]')
            actual = Counter(name for add in adds
                             for name in re.findall(r'defName="([A-Za-z0-9_]+)"', add.findtext('xpath')))
            self.assertEqual(actual, expected)
            self.assertEqual(len(adds), 19)
            for add in adds:
                self.assertEqual([n.tag for n in add.find('value')], ['graphicData'])
                graphic = add.find('value/graphicData')
                self.assertEqual(graphic.get('Inherit').lower(), 'false')
                self.assertEqual(graphic.findtext('graphicClass'), 'Graphic_Single')
                self.assertEqual(graphic.findtext('shaderType'), 'Cutout')
                self.assertTrue(graphic.findtext('texPath').startswith(f'KoRimUtility/RJW/{style}/'))
            for removal in patch.findall('.//match[@Class="PatchOperationRemove"]'):
                self.assertTrue(removal.findtext('xpath').endswith('/graphicData'))
            self.assertFalse(patch.findall('.//defName'))
            self.assertFalse(patch.findall('.//statBases'))

    def test_all_runtime_artwork_exists_as_square_rgba_png(self):
        paths = {v['file'] for v in self.manifest['variants']}
        self.assertEqual(len(paths), 18)
        for name in paths:
            with self.subTest(file=name):
                data = (ROOT / name).read_bytes()
                self.assertEqual(data[:8], b'\x89PNG\r\n\x1a\n')
                self.assertEqual(struct.unpack('>IIBB', data[16:26]), (128, 128, 8, 6))

    def test_colors_materials_and_flat_chest_remain_distinct(self):
        variants = self.manifest['variants']
        for tier, color in [('Natural', [190, 190, 190]), ('Prosthetic', [154, 124, 104]),
                            ('Bionic', [189, 169, 118]), ('Archotech', [155, 165, 148])]:
            for entry in (v for v in variants if v['style'] == 'Vanilla' and v['tier'] == tier):
                self.assertEqual(entry['color_rgb'], color)
        for style in ('Vanilla', 'BionicIcons'):
            items = {d: v for v in variants if v['style'] == style for d in v['def_names']}
            self.assertNotEqual(items['ResinGlob']['tex_path'], items['SlimeGlob']['tex_path'])
            flat = items['FeaturelessChest']
            self.assertEqual(flat['part'], 'FeaturelessChest')
            self.assertEqual(flat['def_names'], ['FeaturelessChest'])
        for entry in (v for v in variants if v['style'] == 'BionicIcons' and v['tier'] == 'Archotech'):
            self.assertEqual(entry['color_rgb'], [255, 255, 255])
            self.assertIn('/Archotech_', entry['tex_path'])
