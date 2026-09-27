import importlib.util
import pathlib
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('release', pathlib.Path(__file__).with_name('validate-release.py'))
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)


class ReleaseValidationTests(unittest.TestCase):
    def test_version_and_tag_must_match_documented_release(self):
        with tempfile.TemporaryDirectory() as temp:
            root = pathlib.Path(temp)
            (root / 'StructuredJson').mkdir()
            (root / 'StructuredJson/StructuredJson.csproj').write_text('<Project><PropertyGroup><Version>2.0.0</Version></PropertyGroup></Project>')
            (root / 'CHANGELOG.md').write_text('## [2.0.0] - 2026-09-27\n\n### Fixed\n- Preserve numbers.\n')
            self.assertEqual('2.0.0', release.validate(root, 'v2.0.0'))
            with self.assertRaises(ValueError):
                release.validate(root, 'v1.0.0')
            (root / 'CHANGELOG.md').write_text('## [1.0.0] - 2025-01-01\n- Initial.\n')
            with self.assertRaises(ValueError):
                release.validate(root, 'v2.0.0')


class PublicationIdentityTests(unittest.TestCase):
    def test_repository_signatures_are_ignored_but_changed_payload_is_rejected(self):
        import io
        import zipfile
        module_spec = importlib.util.spec_from_file_location('publication', pathlib.Path(__file__).with_name('verify-publication.py'))
        publication = importlib.util.module_from_spec(module_spec)
        module_spec.loader.exec_module(publication)
        def package(payload, signed=False):
            stream = io.BytesIO()
            with zipfile.ZipFile(stream, 'w') as archive:
                archive.writestr('lib/net8.0/StructuredJson.dll', payload)
                if signed:
                    archive.writestr('.signature.p7s', b'repository signature')
            return stream.getvalue()
        publication.verify_same(package(b'a'), package(b'a', True))
        with self.assertRaises(ValueError):
            publication.verify_same(package(b'a'), package(b'b', True))


if __name__ == '__main__':
    unittest.main()
