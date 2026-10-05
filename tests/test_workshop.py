import contextlib
import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
from zipfile import ZipFile

from tools.workshop import item_id, prepare, quote, upload, upload_diagnostic, upload_succeeded


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
        self.assertIn('한국어\n설명', text)
        self.assertNotIn('한국어\\n설명', text)
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
        self.assertTrue(upload_succeeded(
            'Preparing update...\nPreparing content...\nUploading content...\n'
            'Uploading preview image...\nCommitting update...\nSuccess.', '123456789'))
        self.assertTrue(upload_succeeded('Preparing update...\nSuccess.\n', '123456789'))
        self.assertTrue(upload_succeeded(
            '\x1b[0mPreparing update...\n\x1b[32mSuccess.\x1b[0m\n', '123456789'))
        for output in ('Success. Logged in.', 'Success.', 'Success. Published item 1234567890.',
                       'Preparing update...\nSuccess. Published item 1234567890.',
                       'ERROR! Upload failed.', 'Preparing update...\nUnloading Steam API...OK',
                       'Preparing update...\nERROR! Upload failed.\nCommitting update...\nSuccess.'):
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

    def test_diagnostic_reports_login_failure_without_raw_output(self):
        diagnostic = upload_diagnostic(
            "session-secret\nCached credentials not found.\n"
            "Logging in user 'private-login' ...ERROR (Invalid Password)", 5)
        self.assertIn('exit=5', diagnostic)
        self.assertIn('cached_credentials_missing=1', diagnostic)
        self.assertIn('invalid_password=1', diagnostic)
        self.assertIn('login_completed=0', diagnostic)
        self.assertNotIn('session-secret', diagnostic)
        self.assertNotIn('private-login', diagnostic)

    def test_diagnostic_distinguishes_authenticated_upload_failure(self):
        diagnostic = upload_diagnostic(
            'Using cached credentials.\nWaiting for user info...OK\n'
            'Uploading content...ERROR (Access Denied)\nprivate-path', 9)
        self.assertIn('using_cached_credentials=1', diagnostic)
        self.assertIn('login_completed=1', diagnostic)
        self.assertIn('upload_started=1', diagnostic)
        self.assertIn('access_denied=1', diagnostic)
        self.assertNotIn('private-path', diagnostic)

    def test_upload_target_mismatch_does_not_start_steam(self):
        manifest = self.prepare()
        with patch.dict('os.environ', {'STEAM_USERNAME': 'test_user', 'STEAM_CONFIG_VDF': 'session-secret'}):
            with patch('tools.workshop.subprocess.run') as run:
                with self.assertRaisesRegex(ValueError, '대상 ID'):
                    upload(self.steamcmd, manifest, '987654321')
                run.assert_not_called()

    def test_success_for_changed_manifest_target_is_rejected(self):
        manifest = self.prepare()
        def change_target(*args, **kwargs):
            manifest.write_text(manifest.read_text(encoding='utf-8').replace('123456789', '987654321'),
                                encoding='utf-8')
            return subprocess.CompletedProcess([], 0, 'Preparing update...\nCommitting update...\nSuccess.')
        with patch.dict('os.environ', {'STEAM_USERNAME': 'test_user', 'STEAM_CONFIG_VDF': 'session-secret'}):
            with patch('tools.workshop.subprocess.run', side_effect=change_target):
                with self.assertRaisesRegex(ValueError, '성공을 확인하지'):
                    upload(self.steamcmd, manifest, '123456789')
        self.assertFalse((self.steamcmd.parent / 'config/config.vdf').exists())

    def test_timeout_also_cleans_session(self):
        manifest = self.prepare()
        with patch.dict('os.environ', {'STEAM_USERNAME': 'test_user', 'STEAM_CONFIG_VDF': 'session-secret'}):
            with patch('tools.workshop.subprocess.run', side_effect=subprocess.TimeoutExpired('steamcmd', 600)):
                with self.assertRaises(subprocess.TimeoutExpired):
                    upload(self.steamcmd, manifest, '123456789')
        self.assertFalse((self.steamcmd.parent / 'config/config.vdf').exists())
