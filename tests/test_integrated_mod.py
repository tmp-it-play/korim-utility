"""Manifest-level regressions; these do not execute RimWorld itself."""
import contextlib
import io
from pathlib import Path
import shutil
import tempfile
import unittest
import xml.etree.ElementTree as ET

from tools.mod import ROOT, check, combined_language


class IntegratedModTests(unittest.TestCase):
    def test_four_optional_mod_combinations(self):
        entries = ET.parse(ROOT / 'LoadFolders.xml').findall('v1.6/li')
        cooler = 'GodlyAnnihilator.TheLowCooler'
        rjw = 'rim.job.world'
        for active, count in [(set(), 3), ({cooler}, 5), ({rjw}, 13), ({cooler, rjw}, 15)]:
            with self.subTest(active=active):
                roots = [ROOT if n.text == '/' else ROOT / n.text for n in entries
                         if not n.get('IfModActive') or n.get('IfModActive') in active]
                translations = combined_language(roots, 'Korean')
                self.assertEqual(len(translations), count)
                self.assertEqual(('DefInjected/ThingDef', 'LingCooler.label') in translations,
                                 cooler in active)
                self.assertEqual(('Keyed', 'RJW_Message_BecameHero') in translations, rjw in active)

    def test_one_mod_with_no_required_translation_targets(self):
        metadata = ET.parse(ROOT / 'About/About.xml')
        self.assertEqual(metadata.findtext('packageId'), 'snowykte0426.korimutility')
        self.assertIsNone(metadata.find('modDependencies'))
        self.assertIsNone(metadata.find('modDependenciesByVersion'))
        self.assertFalse((ROOT / 'Addons').exists())
        self.assertEqual(list((ROOT / 'Translations').rglob('About.xml')), [])

    def test_each_optional_source_snapshot_is_checked(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ('About', 'Languages', 'Translations'):
                shutil.copytree(ROOT / name, root / name)
            shutil.copyfile(ROOT / 'LoadFolders.xml', root / 'LoadFolders.xml')
            path = root / 'Translations/TheCooler/Languages/Korean/DefInjected/ThingDef/Buildings_Temperature.xml'
            tree = ET.parse(path)
            tree.getroot().remove(tree.getroot()[0])
            tree.write(path, encoding='utf-8')
            with contextlib.redirect_stdout(io.StringIO()):
                with self.assertRaisesRegex(ValueError, '원문 키'):
                    check(root)
