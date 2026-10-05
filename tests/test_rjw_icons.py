"""Check the complete item mapping, optional artwork and visual-only boundary."""
from collections import Counter
from pathlib import Path
import re
import struct
import unittest
import xml.etree.ElementTree as ET
import zlib

ROOT = Path(__file__).resolve().parents[1]

# RJW 6.2.1 item coverage, independent of the production XML selectors.
EXPECTED_VARIANTS = {
    'Archotech_Anus': ['ArchotechAnus'],
    'Archotech_Breast': ['ArchotechBreasts'],
    'Archotech_Female': ['ArchotechVagina'],
    'Archotech_Male': ['ArchotechPenis'],
    'Bionic_Anus': ['BionicAnus'],
    'Bionic_Breast': ['BionicBreasts'],
    'Bionic_Female': ['BionicVagina'],
    'Bionic_Male': ['BionicPenis'],
    'Natural_Anus': ['Anus', 'CloacalAnus', 'DemonAnus', 'InsectAnus'],
    'Natural_Breast': ['Breasts', 'UdderBreasts'],
    'Natural_Chest': ['FeaturelessChest'],
    'Natural_Female': ['CatVagina', 'CloacalVagina', 'DemonVagina', 'DogVagina',
                       'DragonVagina', 'HorseVagina', 'NarrowVagina', 'OvipositorF',
                       'RodentVagina', 'Vagina'],
    'Natural_Male': ['CatPenis', 'CloacalPenis', 'CrocodilianPenis', 'DemonPenis',
                     'DemonTentaclePenis', 'DogPenis', 'DragonPenis', 'HemiPenis',
                     'HorsePenis', 'NeedlePenis', 'OvipositorM', 'Penis',
                     'RaccoonPenis', 'RodentPenis'],
    'Prosthetic_Anus': ['HydraulicAnus'],
    'Prosthetic_Breast': ['HydraulicBreasts'],
    'Prosthetic_Female': ['HydraulicVagina'],
    'Prosthetic_Male': ['HydraulicPenis', 'PegDick'],
    'ResinGlob': ['ResinGlob'],
    'SlimeGlob': ['SlimeGlob'],
}


def rgb_pixels(data):
    """Decode 8-bit RGB PNG scanlines without adding a test dependency."""
    width, height, depth, color = struct.unpack('>IIBB', data[16:26])
    assert (depth, color) == (8, 2)
    compressed, offset = bytearray(), 8
    while offset < len(data):
        length = struct.unpack('>I', data[offset:offset + 4])[0]
        if data[offset + 4:offset + 8] == b'IDAT':
            compressed.extend(data[offset + 8:offset + 8 + length])
        offset += length + 12
    scan = zlib.decompress(compressed)
    previous = [0] * (width * 3)
    for y in range(height):
        start = y * (width * 3 + 1)
        kind = scan[start]
        row = list(scan[start + 1:start + 1 + width * 3])
        for i in range(len(row)):
            left, above, upper_left = (row[i - 3] if i >= 3 else 0), previous[i], (previous[i - 3] if i >= 3 else 0)
            estimate = left + above - upper_left
            distances = [abs(estimate - value) for value in (left, above, upper_left)]
            paeth = (left, above, upper_left)[distances.index(min(distances))]
            predictor = (0, left, above, (left + above) // 2, paeth)[kind]
            row[i] = (row[i] + predictor) & 255
        for x in range(width):
            yield x, y, tuple(row[x * 3:x * 3 + 3])
        previous = row


class RjwIconTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.patches = {
            style: ET.parse(ROOT / f'Integrations/RimJobWorld/{style}/Patches/MedicalIcons.xml')
            for style in ('Vanilla', 'BionicIcons')
        }
        cls.graphics = {
            style: {name: add.find('value/graphicData')
                    for add in patch.findall('.//li[@Class="PatchOperationAdd"]')
                    for name in re.findall(r'defName="([A-Za-z0-9_]+)"', add.findtext('xpath'))}
            for style, patch in cls.patches.items()
        }

    def test_every_audited_item_has_one_graphic_in_each_style(self):
        expected = Counter(name for names in EXPECTED_VARIANTS.values() for name in names)
        self.assertEqual(len(expected), 46)
        for style in ('Vanilla', 'BionicIcons'):
            patch = self.patches[style]
            adds = patch.findall('.//li[@Class="PatchOperationAdd"]')
            actual = Counter(name for add in adds
                             for name in re.findall(r'defName="([A-Za-z0-9_]+)"', add.findtext('xpath')))
            self.assertEqual(actual, expected)
            self.assertEqual(len(adds), 19)
            for add in adds:
                self.assertEqual([n.tag for n in add.find('value')], ['graphicData'])
                graphic = add.find('value/graphicData')
                self.assertEqual(graphic.get('Inherit').lower(), 'false')
                self.assertEqual(graphic.findtext('graphicClass'), 'KoRimUtility.MedicalIcons.Graphic_MedicalIcon')
                self.assertEqual(graphic.findtext('shaderType'), 'CutoutComplex')
                self.assertTrue(graphic.findtext('maskPath').startswith(f'KoRimUtility/RJW/{style}/Masks/'))
                base = graphic.findtext('texPath')
                if style == 'Vanilla':
                    self.assertEqual(base, 'Things/Item/Health/HealthItem')
                else:
                    self.assertIn(base, ['BionicIcons/Boxes/' + n for n in ('Default', 'Prosthetic', 'Bionic', 'Archotech')])
                    self.assertEqual(graphic.findtext('color'), '(255,255,255)')
            for removal in patch.findall('.//match[@Class="PatchOperationRemove"]'):
                self.assertTrue(removal.findtext('xpath').endswith('/graphicData'))
            self.assertFalse(patch.findall('.//defName'))
            self.assertFalse(patch.findall('.//statBases'))

    def test_masks_preserve_the_case_outside_the_pictogram(self):
        paths = {f'Integrations/RimJobWorld/{style}/Textures/{g.findtext("maskPath")}.png'
                 for style, items in self.graphics.items() for g in items.values()}
        self.assertEqual(len(paths), 14)
        actual = {p.relative_to(ROOT).as_posix() for p in (ROOT / 'Integrations/RimJobWorld').rglob('*.png')}
        self.assertEqual(actual, paths)  # No repainted case PNGs remain in the package.
        for name in paths:
            with self.subTest(file=name):
                data = (ROOT / name).read_bytes()
                self.assertEqual(data[:8], b'\x89PNG\r\n\x1a\n')
                self.assertEqual(struct.unpack('>IIBB', data[16:26]), (64, 64, 8, 2))
                glyph_pixels = 0
                for x, y, (red, green, blue) in rgb_pixels(data):
                    self.assertEqual(red + green, 255)
                    self.assertEqual(blue, 0)
                    if x < 10 or x >= 54 or y < 8 or y >= 38:
                        self.assertEqual((red, green, blue), (255, 0, 0))
                    glyph_pixels += green > 0
                self.assertGreater(glyph_pixels, 30)
                self.assertLess(glyph_pixels, 800)

    def test_colors_materials_and_flat_chest_remain_distinct(self):
        colors = {'Natural': '(190,190,190)', 'Prosthetic': '(154,124,104)',
                  'Bionic': '(189,169,118)', 'Archotech': '(155,165,148)'}
        boxes = {'Natural': 'Default', 'Prosthetic': 'Prosthetic',
                 'Bionic': 'Bionic', 'Archotech': 'Archotech'}
        for variant, names in EXPECTED_VARIANTS.items():
            if '_' not in variant:
                continue
            tier, part = variant.split('_', 1)
            part = 'FeaturelessChest' if part == 'Chest' else part
            for name in names:
                vanilla = self.graphics['Vanilla'][name]
                bionic = self.graphics['BionicIcons'][name]
                self.assertEqual(vanilla.findtext('color'), colors[tier])
                self.assertEqual(bionic.findtext('texPath'), 'BionicIcons/Boxes/' + boxes[tier])
                for style, graphic in [('Vanilla', vanilla), ('BionicIcons', bionic)]:
                    self.assertEqual(graphic.findtext('maskPath'), f'KoRimUtility/RJW/{style}/Masks/{part}')
        for style, items in self.graphics.items():
            self.assertNotEqual(items['ResinGlob'].findtext('maskPath'), items['SlimeGlob'].findtext('maskPath'))
            self.assertNotEqual(items['ResinGlob'].findtext('colorTwo'), items['SlimeGlob'].findtext('colorTwo'))
            flat_mask = items['FeaturelessChest'].findtext('maskPath')
            self.assertEqual([name for name, g in items.items() if g.findtext('maskPath') == flat_mask], ['FeaturelessChest'])
