from pathlib import Path
import tempfile
import unittest

from tools.mod import package_files


class PackageTests(unittest.TestCase):
    def test_only_own_assembly_is_included(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            assembly = root / 'Translations/FoodAlertContinued/Assemblies/KoRimUtility.FoodAlert.dll'
            assembly.parent.mkdir(parents=True)
            assembly.touch()
            (assembly.parent / 'FoodAlert.dll').touch()

            actual = package_files(root)

            self.assertEqual([assembly], actual)

    def test_missing_assembly_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'Translations/FoodAlertContinued').mkdir(parents=True)

            with self.assertRaisesRegex(ValueError, 'Food Alert 번역 DLL'):
                package_files(root)
