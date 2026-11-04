from pathlib import Path
import hashlib,json,subprocess,sys,xml.etree.ElementTree as ET
root=Path('F:/Projects-PCGames/OpenAntares')
out=root/'reports/ui-layout-integration-20261103/windows-suite-01'
if out.exists():raise SystemExit('Use a fresh evidence directory')
out.mkdir(parents=True)
control=json.loads((root/'reports/ui-layout-integration-20261103/controls-01/source.json').read_text(encoding='utf-8-sig'))
for entry in control['files']:
    assert hashlib.sha256((root/entry['path']).read_bytes()).hexdigest().lower()==entry['sha256'].lower(),entry['path']
prior=json.loads(Path('F:/Tools/CodexTestRunner/state/results/openantares/0b12df0e956b4e6dbd698b9e5093b80a/snapshot.json').read_text())
manifest={p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in prior['files']}
assembly=root/'tests/OpenAntares.Simulation.Tests/bin/Debug/net8.0/OpenAntares.Simulation.Tests.dll'
assembly_hash=hashlib.sha256(assembly.read_bytes()).hexdigest()
head=subprocess.check_output(['git','-C',str(root),'rev-parse','HEAD']).decode().strip()
(out/'source.json').write_text(json.dumps({'sourceRoot':str(root),'head':head,'files':manifest,'ordinaryMainBuildEvidence':'../controls-01/normal-rebuild.log','controlsJob':'65edd46d5c1849c9ad43362f13a74a27','testAssemblySha256':assembly_hash},indent=2)+'\n')
command=['C:/Program Files/dotnet/dotnet.exe','test','tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj','--no-restore','--no-build','--nologo','--disable-build-servers','--logger','trx;LogFileName=main.trx','--results-directory',str(out)]
(out/'command.json').write_text(json.dumps(command,indent=2)+'\n')
with (out/'dotnet.log').open('wb') as log:done=subprocess.run(command,cwd=root,stdout=log,stderr=subprocess.STDOUT)
changed=[p for p,h in manifest.items() if hashlib.sha256((root/p).read_bytes()).hexdigest()!=h]
assembly_unchanged=hashlib.sha256(assembly.read_bytes()).hexdigest()==assembly_hash
counters=None
if (out/'main.trx').exists():
    c=ET.parse(out/'main.trx').find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters');counters=dict(c.attrib) if c is not None else None
passed=done.returncode==0 and not changed and assembly_unchanged and counters is not None and counters.get('passed')=='2140' and counters.get('failed')=='0' and counters.get('notExecuted')=='0'
(out/'result.json').write_text(json.dumps({'passed':passed,'commandExitCode':done.returncode,'sourceFiles':len(manifest),'changedSources':changed,'testAssemblyUnchanged':assembly_unchanged,'counters':counters},indent=2)+'\n')
print('\n'.join((out/'dotnet.log').read_text(encoding='utf-8-sig',errors='replace').splitlines()[-14:]))
print(json.dumps({'passed':passed,'sourceFiles':len(manifest),'counters':counters},indent=2))
sys.exit(0 if passed else 1)