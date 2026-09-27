import importlib.util
import io
import os
import pathlib
import tempfile
import unittest
import urllib.error
import zipfile
from unittest.mock import patch


def load_script(name):
    spec = importlib.util.spec_from_file_location(name, pathlib.Path(__file__).with_name(name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


release = load_script('validate-release')
notes = load_script('release-notes')
publication = load_script('verify-publication')
CHANGELOG = '## [2.0.0] - 2026-09-27\n\n### Fixed\n- Preserve numbers.\n'


def fixture(root, version='2.0.0', changelog=CHANGELOG):
    (root / 'StructuredJson').mkdir(exist_ok=True)
    (root / 'StructuredJson/StructuredJson.csproj').write_text(
        '<Project><PropertyGroup><Version>' + version + '</Version></PropertyGroup></Project>', encoding='utf-8')
    (root / 'CHANGELOG.md').write_text(changelog, encoding='utf-8')


def package(payload, signed=False):
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, 'w') as archive:
        archive.writestr('lib/net8.0/StructuredJson.dll', payload)
        if signed:
            archive.writestr('.signature.p7s', b'repository signature')
    return stream.getvalue()


class ReleaseValidationTests(unittest.TestCase):
    def test_version_and_tag_must_match_documented_release(self):
        with tempfile.TemporaryDirectory() as temp:
            root = pathlib.Path(temp)
            fixture(root)
            self.assertEqual('2.0.0', release.validate(root, 'v2.0.0'))
            self.assertEqual('2.0.0', release.validate(root))
            with self.assertRaises(ValueError):
                release.validate(root, 'v1.0.0')

    def test_missing_empty_and_undated_sections_are_rejected(self):
        for changelog in ['## [1.0.0] - 2025-01-01\n- Initial.\n',
                          '## [2.0.0] - 2026-09-27\n\n', '## [2.0.0]\n- Missing date.\n']:
            with self.subTest(changelog=changelog), tempfile.TemporaryDirectory() as temp:
                root = pathlib.Path(temp)
                fixture(root, changelog=changelog)
                with self.assertRaises(ValueError):
                    release.validate(root)

    def test_missing_or_invalid_version_is_rejected(self):
        for version in ['', '2.0', 'latest']:
            with self.subTest(version=version), tempfile.TemporaryDirectory() as temp:
                root = pathlib.Path(temp)
                fixture(root, version=version)
                with self.assertRaises(ValueError):
                    release.validate(root)

    def test_release_notes_contain_only_current_version(self):
        with tempfile.TemporaryDirectory() as temp:
            root = pathlib.Path(temp)
            fixture(root, changelog=CHANGELOG + '\n## [1.0.0] - 2025-01-01\n- Initial.\n')
            self.assertEqual('### Fixed\n- Preserve numbers.', notes.extract(root))

    def test_missing_or_empty_release_notes_are_rejected(self):
        for changelog in ['', '## [2.0.0] - 2026-09-27\n\n']:
            with self.subTest(changelog=changelog), tempfile.TemporaryDirectory() as temp:
                root = pathlib.Path(temp)
                fixture(root, changelog=changelog)
                with self.assertRaises(ValueError):
                    notes.extract(root)


class PublicationIdentityTests(unittest.TestCase):
    def test_repository_signatures_are_ignored_but_changed_payload_is_rejected(self):
        local = package(b'a')
        signed = package(b'a', True)
        changed = package(b'b', True)
        publication.verify_same(local, signed)
        with self.assertRaises(ValueError):
            publication.verify_same(local, changed)

    def test_only_missing_version_allows_publication(self):
        missing = urllib.error.HTTPError('https://api.nuget.org', 404, 'Not Found', {}, None)
        with patch.object(publication.urllib.request, 'urlopen', side_effect=missing):
            self.assertTrue(publication.should_publish(package(b'a'), '2.0.0'))

    def test_server_failure_does_not_allow_publication(self):
        failure = urllib.error.HTTPError('https://api.nuget.org', 503, 'Unavailable', {}, None)
        local = package(b'a')
        with patch.object(publication.urllib.request, 'urlopen', side_effect=failure):
            with self.assertRaises(urllib.error.HTTPError):
                publication.should_publish(local, '2.0.0')

    def test_existing_identical_version_is_recovered(self):
        local = package(b'a')
        with patch.object(publication.urllib.request, 'urlopen', return_value=io.BytesIO(package(b'a', True))):
            self.assertFalse(publication.should_publish(local, '2.0.0'))

    def test_existing_different_version_is_rejected(self):
        local = package(b'a')
        with patch.object(publication.urllib.request, 'urlopen', return_value=io.BytesIO(package(b'b'))):
            with self.assertRaises(ValueError):
                publication.should_publish(local, '2.0.0')

    def test_publication_decision_is_written_to_github_output(self):
        for publish in [True, False]:
            with self.subTest(publish=publish), tempfile.TemporaryDirectory() as temp:
                root = pathlib.Path(temp)
                fixture(root)
                (root / 'artifacts').mkdir()
                local = package(b'a')
                (root / 'artifacts/StructuredJson.2.0.0.nupkg').write_bytes(local)
                output = root / 'output'
                with patch.dict(os.environ, {'GITHUB_OUTPUT': str(output)}), \
                        patch.object(publication, 'should_publish', return_value=publish) as decision:
                    publication.main(root)
                decision.assert_called_once_with(local, '2.0.0')
                self.assertEqual('should_publish=' + str(publish).lower() + '\n', output.read_text())


if __name__ == '__main__':
    unittest.main()
