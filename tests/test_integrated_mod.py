"""Manifest-level regressions; these do not execute RimWorld itself."""
import contextlib
import io
from itertools import combinations
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
from zipfile import ZipFile

from tools.mod import ROOT, CHARACTER_EDITOR_ASSEMBLY, MEDICAL_ICONS_ASSEMBLY, MAIN_BUTTONS_ASSEMBLY, check, combined_language, pack


class IntegratedModTests(unittest.TestCase):
    def test_one_hundred_twenty_eight_optional_mod_combinations(self):
        entries = ET.parse(ROOT / 'LoadFolders.xml').findall('v1.6/li')
        cooler = 'GodlyAnnihilator.TheLowCooler'
        rjw = 'rim.job.world'
        editor = 'void.charactereditor'
        replace = 'Memegoddess.ReplaceStuff'
        original_replace = 'Uuugggg.ReplaceStuff'
        bionic_icons = 'automatic.bionicicons'
        harmony = 'brrainz.harmony'
        counts = {cooler: 2, rjw: 26, editor: 8, replace: 2, original_replace: 0,
                  bionic_icons: 0, harmony: 0}
        active_sets = [set(group) for size in range(len(counts) + 1) for group in combinations(counts, size)]
        for active in active_sets:
            with self.subTest(active=active):
                def enabled(node):
                    any_of = set(filter(None, node.get('IfModActive', '').split(',')))
                    all_of = set(filter(None, node.get('IfModActiveAll', '').split(',')))
                    none_of = set(filter(None, node.get('IfModNotActive', '').split(',')))
                    return (not any_of or bool(any_of & active)) and all_of <= active and not (none_of & active)
                roots = [ROOT if n.text == '/' else ROOT / n.text for n in entries if enabled(n)]
                translations = combined_language(roots, 'Korean')
                has_replace = bool({replace, original_replace} & active)
                self.assertEqual(len(translations), 3 + sum(counts[mod] for mod in active)
                                 + (6 if has_replace else 0))
                self.assertEqual(('DefInjected/ThingDef', 'LingCooler.label') in translations,
                                 cooler in active)
                self.assertEqual(('Keyed', 'RJW_Message_BecameHero') in translations, rjw in active)
                self.assertEqual(('DefInjected/ThingDef', 'ResinGlob.label') in translations, rjw in active)
                self.assertEqual(('DefInjected/rjw.SexFluidDef', 'Resin.label') in translations, rjw in active)
                self.assertEqual(('Keyed', 'KoRimUtility.CE.Zombrella.Label') in translations, editor in active)
                self.assertEqual(('Keyed', 'KoRimUtility.CE.MainButton.Label') in translations, editor in active)
                self.assertEqual(('DefInjected/JobDef', 'EnterZGrave.reportString') in translations, editor in active)
                self.assertEqual(ROOT / 'Translations/CharacterEditor' in roots, editor in active)
                self.assertEqual(('DefInjected/ThingDef', 'Vent_Over2W.label') in translations, replace in active)
                for def_name in ('Cooler_Over', 'Cooler_Over2W', 'Vent_Over'):
                    for field in ('label', 'description'):
                        self.assertEqual(('DefInjected/ThingDef', f'{def_name}.{field}') in translations,
                                         has_replace)
                self.assertEqual(roots.count(ROOT / 'Translations/ReplaceStuff'), int(has_replace))
                self.assertEqual(ROOT / 'Translations/ReplaceStuffContinued' in roots, replace in active)
                if original_replace in active and replace not in active:
                    self.assertFalse(any(key.startswith('Vent_Over2W.') for _, key in translations))
                self.assertEqual(ROOT / 'Integrations/RimJobWorld/Vanilla' in roots,
                                 rjw in active and bionic_icons not in active)
                self.assertEqual(ROOT / 'Integrations/RimJobWorld/BionicIcons' in roots,
                                 rjw in active and bionic_icons in active)
                self.assertEqual(ROOT / 'Integrations/RimJobWorld/Common' in roots, rjw in active)
                self.assertEqual(ROOT / 'Integrations/MainButtons' in roots, harmony in active)

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
            for name in ('About', 'Languages', 'Translations', 'Integrations'):
                shutil.copytree(ROOT / name, root / name, ignore=shutil.ignore_patterns('Assemblies'))
            shutil.copyfile(ROOT / 'LoadFolders.xml', root / 'LoadFolders.xml')
            path = root / 'Translations/TheCooler/Languages/Korean/DefInjected/ThingDef/Buildings_Temperature.xml'
            tree = ET.parse(path)
            tree.getroot().remove(tree.getroot()[0])
            tree.write(path, encoding='utf-8')
            with contextlib.redirect_stdout(io.StringIO()):
                with self.assertRaisesRegex(ValueError, '원문 키'):
                    check(root)

    def test_pack_requires_compatibility_and_excludes_upstream_assemblies(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ('About', 'Languages', 'Integrations'):
                shutil.copytree(ROOT / name, root / name, ignore=shutil.ignore_patterns('Assemblies'))
            # The test must also work before a developer has built the real DLL.
            shutil.copytree(ROOT / 'Translations', root / 'Translations',
                            ignore=shutil.ignore_patterns('Assemblies'))
            shutil.copyfile(ROOT / 'LoadFolders.xml', root / 'LoadFolders.xml')
            with patch('tools.mod.ROOT', root), contextlib.redirect_stdout(io.StringIO()):
                with self.assertRaisesRegex(ValueError, 'Character Editor 번역 DLL'):
                    pack(root=root)
                assembly = root / CHARACTER_EDITOR_ASSEMBLY
                assembly.parent.mkdir(parents=True)
                assembly.write_bytes(b'test compatibility assembly')
                for name in ('CharacterEditor.dll', 'Assembly-CSharp.dll', '0Harmony.dll'):
                    (assembly.parent / name).write_bytes(b'not for redistribution')
                with self.assertRaisesRegex(ValueError, '의료 아이콘 호환 DLL'):
                    pack(root=root)
                medical = root / MEDICAL_ICONS_ASSEMBLY
                medical.parent.mkdir(parents=True)
                medical.write_bytes(b'test medical icon assembly')
                (medical.parent / 'BionicIcons.dll').write_bytes(b'not for redistribution')
                with self.assertRaisesRegex(ValueError, '하단 메뉴 UI DLL'):
                    pack(root=root)
                main_buttons = root / MAIN_BUTTONS_ASSEMBLY
                main_buttons.parent.mkdir(parents=True)
                main_buttons.write_bytes(b'test main button assembly')
                (main_buttons.parent / '0Harmony.dll').write_bytes(b'not for redistribution')
                pack(root=root)
            with ZipFile(root / 'dist/KoRimUtility.zip') as archive:
                dlls = [name for name in archive.namelist() if name.endswith('.dll')]
                self.assertEqual(sorted(dlls), sorted(['KoRimUtility/' + CHARACTER_EDITOR_ASSEMBLY,
                                                     'KoRimUtility/' + MEDICAL_ICONS_ASSEMBLY,
                                                     'KoRimUtility/' + MAIN_BUTTONS_ASSEMBLY]))
                self.assertFalse(any('TranslationReference' in name for name in archive.namelist()))
