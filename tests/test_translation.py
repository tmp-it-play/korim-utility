import contextlib
import io
import json
from pathlib import Path
import tempfile
import unittest

from tools.mod import catalog, read_language, render_catalog, check, content_roots, combined_language


class TranslationWorkflowTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'English'
        (self.source / 'Keyed').mkdir(parents=True)
        (self.source / 'Keyed/UI.xml').write_text(
            '<LanguageData><Hello>Hello {PAWN_name}: {0} &lt;b&gt;ok&lt;/b&gt;</Hello>'
            '<Later>Not translated</Later></LanguageData>', encoding='utf-8')
        self.catalog = self.root / 'catalog.json'
        self.output = self.root / 'Korean'
        with contextlib.redirect_stdout(io.StringIO()):
            catalog(self.source, self.catalog)

    def translate(self, text):
        data = json.loads(self.catalog.read_text(encoding='utf-8'))
        data['entries'][0]['korean'] = text
        self.catalog.write_text(json.dumps(data, ensure_ascii=False), encoding='utf-8')

    def test_roundtrip_preserves_tokens_and_skips_untranslated(self):
        self.translate('{PAWN_name}: {0} <b>완료</b> & 확인')
        with contextlib.redirect_stdout(io.StringIO()):
            render_catalog(self.catalog, self.output)
        result = read_language(self.output)
        self.assertEqual(len(result), 1)
        self.assertEqual(result[('Keyed', 'Hello')]['text'], '{PAWN_name}: {0} <b>완료</b> & 확인')

    def test_missing_placeholder_rejected_before_writing(self):
        self.translate('{PAWN_name}: <b>완료</b>')
        with self.assertRaisesRegex(ValueError, '불일치'):
            render_catalog(self.catalog, self.output)
        self.assertFalse(self.output.exists())

    def test_duplicate_keys_across_files_rejected(self):
        (self.source / 'Keyed/Duplicate.xml').write_text('<LanguageData><Hello>Duplicate</Hello></LanguageData>')
        with self.assertRaisesRegex(ValueError, '중복'):
            read_language(self.source)

    def test_same_filename_in_load_folders_rejected_even_with_different_keys(self):
        roots = [self.root / 'Common', self.root / 'Continued']
        for root, key in zip(roots, ('CommonLabel', 'ContinuedLabel')):
            folder = root / 'Languages/Korean/Keyed'
            folder.mkdir(parents=True)
            (folder / 'UI.xml').write_text(f'<LanguageData><{key}>번역</{key}></LanguageData>', encoding='utf-8')
        with self.assertRaisesRegex(ValueError, '파일 경로 중복'):
            combined_language(roots, 'Korean')
        folder = roots[1] / 'Languages/Korean/Keyed'
        (folder / 'UI.xml').rename(folder / 'Continued.xml')
        self.assertEqual(len(combined_language(roots, 'Korean')), 2)

    def test_source_updates_rejected(self):
        self.translate('{PAWN_name}: {0} <b>완료</b>')
        path = self.source / 'Keyed/UI.xml'
        path.write_text(path.read_text().replace('Hello {PAWN_name}', 'Changed {PAWN_name}'))
        with self.assertRaisesRegex(ValueError, '원문 변경'):
            render_catalog(self.catalog, self.output)

    def test_output_cannot_escape_staging_folder(self):
        data = json.loads(self.catalog.read_text())
        data['entries'][0]['file'] = '../escaped.xml'
        self.catalog.write_text(json.dumps(data))
        with self.assertRaisesRegex(ValueError, '경로'):
            render_catalog(self.catalog, self.output)
        self.assertFalse((self.root / 'escaped.xml').exists())

    def test_existing_work_is_never_overwritten(self):
        with self.assertRaisesRegex(ValueError, '덮어쓰지'):
            catalog(self.source, self.catalog)
        self.output.mkdir()
        with self.assertRaisesRegex(ValueError, '새 출력 폴더'):
            render_catalog(self.catalog, self.output)

    def test_def_type_is_part_of_key_identity(self):
        for kind in ('ThingDef', 'RecipeDef'):
            folder = self.source / 'DefInjected' / kind
            folder.mkdir(parents=True)
            (folder / 'Labels.xml').write_text('<LanguageData><Example.label>example</Example.label></LanguageData>')
        result = read_language(self.source)
        self.assertIn(('DefInjected/ThingDef', 'Example.label'), result)
        self.assertIn(('DefInjected/RecipeDef', 'Example.label'), result)

    def test_invalid_xml_character_is_rejected_before_writing(self):
        self.translate('{PAWN_name}: {0} <b>완료</b>\x00')
        import xml.etree.ElementTree as ET
        with self.assertRaises(ET.ParseError):
            render_catalog(self.catalog, self.output)
        self.assertFalse(self.output.exists())

    def test_conditional_translations_are_validated(self):
        (self.root / 'About').mkdir()
        (self.root / 'About/About.xml').write_text(
            '<ModMetaData><name>Test</name><author>Test</author><packageId>test.mod</packageId>'
            '<supportedVersions><li>1.6</li></supportedVersions></ModMetaData>')
        folder = self.root / 'Translations/Optional/Languages/Korean/Keyed'
        folder.mkdir(parents=True)
        (self.root / 'LoadFolders.xml').write_text(
            '<loadFolders><v1.6><li>/</li><li IfModActive="test.optional">'
            'Translations/Optional</li></v1.6></loadFolders>')
        (folder / 'Bad.xml').write_text('<LanguageData><Empty>TODO</Empty></LanguageData>')
        with self.assertRaisesRegex(ValueError, 'TODO'):
            check(self.root)

    def test_load_folder_cannot_escape_mod(self):
        (self.root / 'About').mkdir()
        (self.root / 'About/About.xml').write_text(
            '<ModMetaData><supportedVersions><li>1.6</li></supportedVersions></ModMetaData>')
        (self.root / 'LoadFolders.xml').write_text(
            '<loadFolders><v1.6><li>..</li></v1.6></loadFolders>')
        with self.assertRaisesRegex(ValueError, '잘못된 로드 폴더'):
            content_roots(self.root)


if __name__ == '__main__':
    unittest.main()
