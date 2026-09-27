#!/usr/bin/env python3
"""Validate the authoritative package version, changelog section and optional release tag."""
import argparse
import pathlib
import re
import xml.etree.ElementTree as ET


def validate(root, tag=None):
    version = ET.parse(root / 'StructuredJson/StructuredJson.csproj').findtext('.//Version')
    if not version or not re.fullmatch(r'\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?', version):
        raise ValueError('Package Version must be an explicit semantic version')
    changelog = (root / 'CHANGELOG.md').read_text(encoding='utf-8')
    match = re.search(r'^## \[' + re.escape(version) + r'\] - \d{4}-\d{2}-\d{2}\s*\n(.*?)(?=^## |\Z)', changelog, re.M | re.S)
    if not match or not match.group(1).strip():
        raise ValueError('A nonempty dated changelog section for ' + version + ' is required')
    if tag is not None and tag != 'v' + version:
        raise ValueError('Release tag must equal v' + version)
    return version


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag')
    args = parser.parse_args()
    print(validate(pathlib.Path(__file__).resolve().parents[1], args.tag))
