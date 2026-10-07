import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
from zipfile import ZipFile

from tools.workshop import item_id, quote, read_package, upload, upload_diagnostic, upload_succeeded


class WorkshopTests(unittest.TestCase):
    def test_invalid_item_ids_are_rejected(self):
        conditions = ('', '0', '-1', '123\n', 'abc', str(2**64))
        for value in conditions:
            with self.subTest(value=value), self.assertRaises(ValueError):
                item_id(value)

    def test_upload_success_matches_the_target(self):
        conditions = (
            ('Success. Published item 123.', True),
            ('Success. Published File ID: 123', True),
            ('Preparing update...\nSuccess.', True),
            ('\x1b[32mPreparing update...\nSuccess.\x1b[0m', True),
            ('Success. Published item 1234.', False),
            ('Success. Logged in.', False),
            ('Preparing update...\nERROR! Upload failed.\nSuccess.', False),
        )
        for output, expected in conditions:
            with self.subTest(output=output):
                actual = upload_succeeded(output, '123')

                self.assertEqual(expected, actual)

    def test_vdf_keeps_newlines_and_escapes_quotes(self):
        value = 'first\n"second"'

        actual = quote(value)

        self.assertEqual('"first\n\\"second\\""', actual)

    def test_diagnostics_exclude_private_output(self):
        output = "private-session-token\nLogging in user 'private-name' ...ERROR (Invalid Password)"

        actual = upload_diagnostic(output, 5)

        self.assertNotIn('private-', actual)
        self.assertIn('invalid_password=1', actual)

    def test_invalid_archive_paths_are_rejected(self):
        for name in ('./', 'KoRimUtility/../../escape', '/outside'):
            with self.subTest(path=name):
                archive = io.BytesIO()
                with ZipFile(archive, 'w') as zipped:
                    zipped.writestr(name, b'')

                with self.assertRaisesRegex(ValueError, '경로'):
                    read_package(archive)

    def test_timeout_removes_session_file(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            steamcmd = root / 'steamcmd.sh'
            steamcmd.touch()
            manifest = root / 'item.vdf'
            manifest.write_text('"appid" "294100"\n"publishedfileid" "123"')
            environment = {'STEAM_USERNAME': 'test_user', 'STEAM_CONFIG_VDF': 'test-session'}
            with patch.dict('os.environ', environment), patch('tools.workshop.subprocess.run',
                    side_effect=subprocess.TimeoutExpired('steamcmd', 600)):
                with self.assertRaises(subprocess.TimeoutExpired):
                    upload(steamcmd, manifest, '123')

            self.assertFalse((root / 'config/config.vdf').exists())
