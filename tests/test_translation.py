from pathlib import Path
import tempfile
import unittest

from tools.mod import compare, read_language


class TranslationTests(unittest.TestCase):
    def test_reordered_placeholders_are_accepted(self):
        key = ('Keyed', 'Greeting')
        source = {key: {'text': '{0}: <b>{PAWN_name}</b>'}}
        translated = {key: {'text': '<b>{PAWN_name}</b>: {0}'}}

        actual = compare(source, translated)

        self.assertIsNone(actual)

    def test_missing_placeholder_is_rejected(self):
        key = ('Keyed', 'Greeting')
        source = {key: {'text': '{PAWN_name}: {0}'}}
        translated = {key: {'text': '{PAWN_name}'}}

        with self.assertRaisesRegex(ValueError, '불일치'):
            compare(source, translated)

    def test_duplicate_keys_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            (folder / 'Keyed').mkdir()
            for name in ('First.xml', 'Second.xml'):
                (folder / 'Keyed' / name).write_text('<LanguageData><Hello>hello</Hello></LanguageData>')

            with self.assertRaisesRegex(ValueError, '중복'):
                read_language(folder)

    def test_def_types_have_separate_keys(self):
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            for kind in ('ThingDef', 'RecipeDef'):
                target = folder / 'DefInjected' / kind
                target.mkdir(parents=True)
                (target / 'Label.xml').write_text('<LanguageData><Item.label>item</Item.label></LanguageData>')

            actual = read_language(folder)

            self.assertEqual({('DefInjected/ThingDef', 'Item.label'),
                              ('DefInjected/RecipeDef', 'Item.label')}, actual.keys())
