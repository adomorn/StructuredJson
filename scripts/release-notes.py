#!/usr/bin/env python3
import pathlib
import re
import xml.etree.ElementTree as ET
root = pathlib.Path(__file__).resolve().parents[1]
version = ET.parse(root / 'StructuredJson/StructuredJson.csproj').findtext('.//Version')
text = (root / 'CHANGELOG.md').read_text(encoding='utf-8')
match = re.search(r'^## \[' + re.escape(version) + r'\][^\n]*\n(.*?)(?=^## |\Z)', text, re.M | re.S)
if not match or not match.group(1).strip():
    raise SystemExit('Release notes missing')
print(match.group(1).strip())
