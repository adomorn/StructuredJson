#!/usr/bin/env python3
"""Install the locally built package into an isolated consumer; never accidentally use NuGet.org's copy."""
import argparse
import pathlib
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument('--artifacts', default='artifacts')
args = parser.parse_args()
dotnet = shutil.which('dotnet')
if dotnet is None:
    raise SystemExit('Install the SDK pinned in global.json and put dotnet on PATH.')
root = pathlib.Path(__file__).resolve().parents[1]
version = ET.parse(root / 'StructuredJson/StructuredJson.csproj').findtext('.//Version')
artifacts = (root / args.artifacts).resolve()
package = artifacts / ('StructuredJson.' + version + '.nupkg')
frameworks = ['net8.0', 'net9.0', 'net10.0', 'net11.0']
with zipfile.ZipFile(package) as archive:
    for framework in frameworks:
        assert 'lib/' + framework + '/StructuredJson.dll' in archive.namelist()
    manifest = ET.fromstring(archive.read('StructuredJson.nuspec'))
    ns = {'n': manifest.tag.split('}')[0].lstrip('{')}
    assert manifest.findtext('n:metadata/n:version', namespaces=ns) == version
    assert not manifest.findall('.//n:dependency', ns), 'The library should have no external package dependencies'
with tempfile.TemporaryDirectory(prefix='structuredjson-package-') as temp:
    consumer = pathlib.Path(temp)
    config = ET.Element('configuration')
    sources = ET.SubElement(config, 'packageSources'); ET.SubElement(sources, 'clear')
    ET.SubElement(sources, 'add', key='local', value=str(artifacts))
    ET.SubElement(sources, 'add', key='framework-packs', value='https://api.nuget.org/v3/index.json')
    mapping = ET.SubElement(config, 'packageSourceMapping')
    local = ET.SubElement(mapping, 'packageSource', key='local'); ET.SubElement(local, 'package', pattern='StructuredJson')
    runtime = ET.SubElement(mapping, 'packageSource', key='framework-packs'); ET.SubElement(runtime, 'package', pattern='Microsoft.*')
    ET.ElementTree(config).write(consumer / 'NuGet.Config', encoding='unicode')
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    group = ET.SubElement(project, 'PropertyGroup')
    for name, value in [('OutputType', 'Exe'), ('TargetFrameworks', ';'.join(frameworks)), ('ImplicitUsings', 'enable')]:
        ET.SubElement(group, name).text = value
    ET.SubElement(ET.SubElement(project, 'ItemGroup'), 'PackageReference', Include='StructuredJson', Version=version)
    project_path = consumer / 'Consumer.csproj'
    ET.ElementTree(project).write(project_path, encoding='unicode')
    (consumer / 'Program.cs').write_text((root / 'examples/StructuredJson.Example/Program.cs').read_text())
    subprocess.run([dotnet, 'restore', str(project_path), '--packages', str(consumer / 'packages'), '--configfile', str(consumer / 'NuGet.Config')], cwd=root, check=True)
    for framework in frameworks:
        subprocess.run([dotnet, 'run', '--project', str(project_path), '-f', framework, '-c', 'Release', '--no-restore', '--disable-build-servers'], cwd=root, check=True)
        print('PACKAGE SMOKE PASSED:', framework, flush=True)
