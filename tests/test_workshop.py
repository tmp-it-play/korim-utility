import contextlib
import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
from zipfile import ZipFile

from tools.workshop import item_id, prepare, quote, upload, upload_succeeded


class WorkshopDeploymentTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.archive = self.root / 'mod.zip'
        self.destination = self.root / 'staging'
        with ZipFile(self.archive, 'w') as archive:
            archive.writestr('KoRimUtility/About/About.xml',
                             '<ModMetaData><name>KoRim Utility</name>'
                             '<packageId>snowykte0426.korimutility</packageId></ModMetaData>')
            archive.writestr('KoRimUtility/About/Preview.png', b'\x89PNG\r\n\x1a\nfixture')
        self.steamcmd = self.root / 'Steam/steamcmd.sh'
        self.steamcmd.parent.mkdir()
        self.steamcmd.write_text('not executed: mocked in tests')

    def prepare(self):
        with contextlib.redirect_stdout(io.StringIO()):
            prepare(self.archive, self.destination, '123456789', 'test "note"', '한국어\n설명')
        return self.destination / 'item.vdf'

    def test_manifest_points_to_mod_root_and_preserves_visibility(self):
        text = self.prepare().read_text()
        self.assertIn('"appid" "294100"', text)
        self.assertIn('"publishedfileid" "123456789"', text)
        self.assertIn(quote(str((self.destination / 'KoRimUtility').resolve())), text)
        self.assertNotIn('"visibility"', text)
        self.assertIn('test \\"note\\"', text)
        self.assertIn('한국어\\n설명', text)
        self.assertEqual((self.destination / 'KoRimUtility/About/PublishedFileId.txt').read_text().strip(), '123456789')

    def test_creation_and_invalid_ids_rejected(self):
        for value in ('', '0', '-1', '123\n', 'abc', str(2**64)):
            with self.subTest(value=value), self.assertRaises(ValueError):
                item_id(value)

    def test_unsafe_zip_path_rejected_before_writing(self):
        with ZipFile(self.archive, 'a') as archive:
            archive.writestr('KoRimUtility/../../escape', 'bad')
        with self.assertRaisesRegex(ValueError, '경로'):
            self.prepare()
        self.assertFalse(self.destination.exists())

    def test_only_matching_upload_success_is_accepted(self):
        self.assertTrue(upload_succeeded('Success. Published item 123456789.', '123456789'))
        self.assertTrue(upload_succeeded('Success. Published File ID: 123456789', '123456789'))
        for output in ('Success. Logged in.', 'Success. Published item 1234567890.', 'ERROR! Upload failed.'):
            self.assertFalse(upload_succeeded(output, '123456789'))

    def test_failed_upload_does_not_log_session_and_cleans_config(self):
        manifest = self.prepare()
        captured = io.StringIO()
        with patch.dict('os.environ', {'STEAM_USERNAME': 'test_user', 'STEAM_CONFIG_VDF': 'session-secret'}):
            with patch('tools.workshop.subprocess.run', return_value=subprocess.CompletedProcess([], 0, 'session-secret\nERROR!')) as run:
                with contextlib.redirect_stdout(captured), self.assertRaisesRegex(ValueError, '성공을 확인하지'):
                    upload(self.steamcmd, manifest, '123456789')
                self.assertNotIn('STEAM_CONFIG_VDF', run.call_args.kwargs['env'])
        self.assertNotIn('session-secret', captured.getvalue())
        self.assertFalse((self.steamcmd.parent / 'config/config.vdf').exists())

    def test_upload_target_mismatch_does_not_start_steam(self):
        manifest = self.prepare()
        with patch.dict('os.environ', {'STEAM_USERNAME': 'test_user', 'STEAM_CONFIG_VDF': 'session-secret'}):
            with patch('tools.workshop.subprocess.run') as run:
                with self.assertRaisesRegex(ValueError, '대상 ID'):
                    upload(self.steamcmd, manifest, '987654321')
                run.assert_not_called()

    def test_timeout_also_cleans_session(self):
        manifest = self.prepare()
        with patch.dict('os.environ', {'STEAM_USERNAME': 'test_user', 'STEAM_CONFIG_VDF': 'session-secret'}):
            with patch('tools.workshop.subprocess.run', side_effect=subprocess.TimeoutExpired('steamcmd', 600)):
                with self.assertRaises(subprocess.TimeoutExpired):
                    upload(self.steamcmd, manifest, '123456789')
        self.assertFalse((self.steamcmd.parent / 'config/config.vdf').exists())
