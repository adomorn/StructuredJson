#!/usr/bin/env python3
"""Require matching package contents before recovering an already published version."""
import io
import os
import pathlib
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile


def package_contents(data):
    with zipfile.ZipFile(io.BytesIO(data)) as package:
        # NuGet may append a repository signature after publication.
        return {name: package.read(name) for name in package.namelist() if name != '.signature.p7s'}


def verify_same(local, remote):
    if package_contents(local) != package_contents(remote):
        raise ValueError('This version already exists with different contents. Never reuse a published version.')


def should_publish(local, version):
    url = 'https://api.nuget.org/v3-flatcontainer/structuredjson/' + version + '/structuredjson.' + version + '.nupkg'
    try:
        with urllib.request.urlopen(url, timeout=60) as response:
            verify_same(local, response.read())
        return False
    except urllib.error.HTTPError as error:
        if error.code != 404:
            raise
        return True


def main(root):
    version = ET.parse(root / 'StructuredJson/StructuredJson.csproj').findtext('.//Version')
    package = root / 'artifacts' / ('StructuredJson.' + version + '.nupkg')
    publish = should_publish(package.read_bytes(), version)
    print('Version is not published' if publish else 'Existing package matches')
    with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
        output.write('should_publish=' + str(publish).lower() + '\n')


if __name__ == '__main__':
    main(pathlib.Path(__file__).resolve().parents[1])
