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


if __name__ == '__main__':
    root = pathlib.Path(__file__).resolve().parents[1]
    version = ET.parse(root / 'StructuredJson/StructuredJson.csproj').findtext('.//Version')
    package = root / 'artifacts' / ('StructuredJson.' + version + '.nupkg')
    url = 'https://api.nuget.org/v3-flatcontainer/structuredjson/' + version + '/structuredjson.' + version + '.nupkg'
    should_publish = True
    try:
        with urllib.request.urlopen(url, timeout=60) as response:
            verify_same(package.read_bytes(), response.read())
        should_publish = False
    except urllib.error.HTTPError as error:
        if error.code != 404:
            raise
    print('Existing package matches' if not should_publish else 'Version is not published')
    with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
        output.write('should_publish=' + str(should_publish).lower() + '\n')
