from pathlib import Path
import hashlib,json,re,subprocess,xml.etree.ElementTree as ET
root=Path('F:/Projects-PCGames/OpenAntares')
backup=Path('C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.ui-layout-integration-20261103')
out=root/'reports/ui-layout-integration-20261103'
controls=out/'controls-01'
queue=Path('F:/Tools/CodexTestRunner/state/results/openantares')
native_id='65edd46d5c1849c9ad43362f13a74a27'
windows_id='d47cb66d60f745f397058b003db3b9a5'
failed_portable_id='2cd8b2716b7441b480976e514c4b037b'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def require(c,t):
    if not c:raise RuntimeError(t)
def sha(data):return hashlib.sha256(data).hexdigest()
require(read(queue/native_id/'result.json')['exit_code']==0,'Native queue did not finish successfully')
r=read(controls/'results.json');require(r['passed'] and len(r['checks'])==107 and all(c['passed'] for c in r['checks']),'107 current/legacy checks did not pass')
source=read(controls/'source.json');require(Path(source['sourceRoot']).resolve()==root.resolve(),'Control check used wrong root')
for entry in source['files']:require(sha((root/entry['path']).read_bytes()).lower()==entry['sha256'].lower(),'Native source changed: '+entry['path'])
for name in ['baseline-build.log','validation-build.log','normal-rebuild.log']:
    text=(controls/name).read_text(encoding='utf-8-sig');require('Build succeeded.' in text and '0 Warning(s)' in text and '0 Error(s)' in text,'Build log: '+name)
require(not re.search(r'^ERROR:|^SCRIPT ERROR:|^WARNING:.*leaked',(controls/'console.log').read_text(encoding='utf-8-sig'),re.M),'Godot diagnostics remain')
meta=read(controls/'normal-metadata.json');require(meta['presentScenarioTypes']==[] and meta['presentHelpers']==[],'Harness present in normal game')
require(read(controls/'opt-in-metadata.json')['presentScenarioTypes']==['LayoutTrialValidation'],'Negative metadata control failed')
require(read(queue/windows_id/'result.json')['exit_code']==0,'Queued Windows suite did not pass')
windows=out/'windows-suite-01'
result=read(windows/'result.json');require(result['passed'] and result['commandExitCode']==0 and not result['changedSources'] and result['testAssemblyUnchanged'],'Windows test source/evidence mismatch')
snapshot=read(windows/'source.json');require(Path(snapshot['sourceRoot']).resolve()==root.resolve(),'Windows suite used wrong root')
for path,digest in snapshot['files'].items():require(sha((root/path).read_bytes())==digest,'Windows source changed: '+path)
require(sha((root/'tests/OpenAntares.Simulation.Tests/bin/Debug/net8.0/OpenAntares.Simulation.Tests.dll').read_bytes())==snapshot['testAssemblySha256'],'Test assembly changed')
counter=ET.parse(windows/'main.trx').find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters')
require(counter is not None and counter.get('passed')=='2140' and counter.get('failed')=='0' and counter.get('notExecuted')=='0','Full Windows TRX counts mismatch')
before=read(backup/'before.json');merge=read(backup/'merge-progress.json')
for entry in before['files']:
    data=(root/entry['path']).read_bytes();old=(backup/'owner-before'/entry['path']).read_bytes()
    if entry['path']=='project.godot':require(data.replace(b'window/stretch/mode="disabled"',b'window/stretch/mode="canvas_items"')==old,'Owner renderer/frame/window config changed')
    elif entry['path'] in ['PROJECT_STATUS.md','reports/VALIDATION_REPORT.md']:require(old.split(b'\n',1)[1] in data,'Owner report/status bytes changed')
    else:require(sha(data)==entry['sha256'],'Owner file changed: '+entry['path'])
raw_files=0
for file in (root/'reports/ui-layout-trial').rglob('*'):
    if file.is_file():
        rel=file.relative_to(root).as_posix();require(file.read_bytes()==subprocess.check_output(['git','-C',str(root),'show',merge['feature']+':'+rel]),'Archived raw evidence changed: '+rel);raw_files+=1
summary={'mainSourceRoot':str(root),'layoutCommit':merge['feature'],'testedMainHead':source['head'],'controlJob':native_id,'controlAssertionsPassed':107,'nativeSourceFilesMatch':len(source['files']),'buildWarnings':0,'buildErrors':0,'engineDiagnostics':0,'ordinaryScenarioTypes':meta['presentScenarioTypes'],'ordinaryHelpers':meta['presentHelpers'],'windowsJob':windows_id,'simulationPassed':2140,'simulationFailed':0,'simulationSkipped':0,'simulationSourceFilesMatch':len(snapshot['files']),'testAssemblySha256':snapshot['testAssemblySha256'],'failedDockerPreparationJob':failed_portable_id,'dockerAttemptRanTests':False,'preservedOwnerFiles':len(before['files']),'originalRawEvidenceFilesPreserved':raw_files,'recoveryBranch':merge['recovery'],'alreadyRestoredPreservationStash':merge['ownerStash'],'retainedQaSavePaths':r['retainedQaSavePaths'],'limits':['Docker portable image preparation failed because Docker Desktop Linux backend was stopped; CLI startup timed out. Official queue recovery followed confirmed failed connection/process exit.','The complete Windows suite ran instead, including all six cases excluded by the portable selector.','Headless controls and owner layout preference do not close the full rendered/physical-input/accessibility/platform/release matrix.','Historical wider graphical toolbar/workspace helpers remain Deferred; their old results are not merged-layout evidence.'],'remotePush':False,'worktreesDeleted':False}
if '--check-only' not in __import__('sys').argv:(out/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
print(json.dumps(summary,indent=2))
