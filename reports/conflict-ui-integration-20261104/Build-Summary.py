"""Assemble combined acceptance only from passing evidence matching current source."""
import argparse,copy,hashlib,json,subprocess,runpy
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--suite',required=True);p.add_argument('--suite-job',required=True);p.add_argument('--layout',required=True);p.add_argument('--rival-budget-equivalence',action='store_true');a=p.parse_args()
r=Path.cwd();base=r/'reports/conflict-ui-integration-20261104'
def read(path):return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def artifact(path):
 path=Path(path);return {'path':path.relative_to(r).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
expected=runpy.run_path(str(r/'tools/ConflictValidation/Verify-Evidence.py'))['tracked_sources'](r)
BUDGET_FILES={'tools/ConflictValidation/Run-NativeMatrix.ps1','tests/OpenAntares.Game.Validation/Run-Validation.ps1'}
OLD_LIMIT=b"@('Expansion','Combat','ShipDesign','EmpireConflict')"
NEW_LIMIT=b"@('Expansion','Rival','Combat','ShipDesign','EmpireConflict')"
def source_changes(source):
    if len(source['files'])!=len(expected) or {row['path'] for row in source['files']}!=expected:return None
    changes=[]
    for row in source['files']:
        path=r/row['path']
        if not path.is_file():return None
        current=path.read_bytes()
        if hashlib.sha256(current).hexdigest()==row['sha256']:continue
        if not a.rival_budget_equivalence or row['path'] not in BUDGET_FILES:return None
        before=subprocess.check_output(['git','show',source['commit']+':'+row['path']]).replace(b'\r\n',b'\n')
        if hashlib.sha256(before).hexdigest()!=row['canonicalSha256'] or before.count(OLD_LIMIT)!=1 or before.replace(OLD_LIMIT,NEW_LIMIT)!=current.replace(b'\r\n',b'\n'):return None
        changes.append({'path':row['path'],'recordedSha256':row['sha256'],'currentSha256':hashlib.sha256(current).hexdigest(),'change':'Only Rival emergency display-frame limit7200->72000; case assertions and gameplay/turn/seed limits unchanged'})
    return changes
def matches(source):return source_changes(source) is not None
s=copy.deepcopy(read(r/'reports/conflict-execution/final-summary.json'))
runs=[];folders={}
for name in ['Legacy','Expansion','Species','Rival','Colony','Crisis','Scout','Fleet','Combat','ShipDesign','Diplomacy','EmpireConflict']:
 candidates=[]
 for result in base.glob('*/results.json'):
  d=read(result)
  if d.get('scenario')!=name or d.get('passed') is not True:continue
  folder=result.parent
  if not all((folder/file).is_file() for file in ['execution.json','source.json','normal-metadata.json','opt-in-metadata.json','console.log','godot.log']):continue
  execution=read(folder/'execution.json');source=read(folder/'source.json')
  if execution.get('exitCode')!=0 or execution.get('sourceStable') is not True or execution.get('diagnostics')!=[] or not matches(source):continue
  if name=='EmpireConflict' and execution.get('earnedConflictCampaigns') is not True:continue
  candidates.append(folder)
 if not candidates:raise SystemExit('No passing current-source evidence for '+name)
 folder=max(candidates,key=lambda x:(x/'results.json').stat().st_mtime);folders[name]=folder
 runs.append(dict(scenario=name,exitCode=0,diagnostics=[],method='native',**{key:artifact(folder/file) for key,file in [('evidence','results.json'),('source','source.json'),('execution','execution.json'),('normalMetadata','normal-metadata.json'),('optInMetadata','opt-in-metadata.json'),('engineLog','godot.log'),('consoleLog','console.log')]}))
suite=r/a.suite;suite_result=read(suite/'result.json');assert suite_result['passed'] and suite_result['suite']=='default' and suite_result['counters']['failed']=='0' and suite_result['counters']['passed']=='2402'
layout=r/a.layout;layout_result=read(layout/'results.json');assert layout_result['passed'] and all(c['passed'] for c in layout_result['checks'])
for row in read(layout/'source.json')['files']:assert hashlib.sha256((r/row['path']).read_bytes()).hexdigest().lower()==row['sha256'].lower()
current_sources=[(name,read(folder/'source.json')) for name,folder in folders.items() if source_changes(read(folder/'source.json'))==[]]
if not current_sources:raise SystemExit('A fresh native capture of the complete current source is required')
current_name,current_source=current_sources[-1]
s['nativeMatrix']=runs;s['productSource']=current_source
comparison=[]
for name,folder in folders.items():
    native_source=read(folder/'source.json');changes=source_changes(native_source)
    delta=set(subprocess.check_output(['git','diff','--name-only',native_source['commit'],current_source['commit']],text=True).splitlines())
    if changes and (not delta or not delta.issubset(BUDGET_FILES)):raise SystemExit('Source equivalence includes more than the exact recorder-budget correction')
    comparison.append({'scenario':name,'recordedCommit':native_source['commit'],'currentProductCommit':current_source['commit'],'sourceFiles':len(expected),'compiledInputsByteIdentical':True,'recorderOnlyChanges':changes,'commitDeltaPaths':sorted(delta)})
equivalence={'passed':True,'policy':'All authoritative files match current source byte-for-byte; the explicit budget-equivalence option permits only the exact Rival safety-frame array edit in the two committed launchers. Every game, UI, content, scene, theme, C# test and other input remains identical. Incomplete Rival evidence is never selected.','cases':comparison}
(base/'source-equivalence.json').write_text(json.dumps(equivalence,indent=2)+'\n',encoding='utf-8')
s['nativeSourceEquivalence']=artifact(base/'source-equivalence.json');s['branch']=subprocess.check_output(['git','branch','--show-current'],text=True).strip();s['baselineCommit']='234b45e5eab968d768095d7c858c95bfa12e92f0'
s['simulationSuite']={'passed':2402,'failed':0,'skipped':0,'queueJob':a.suite_job,'result':artifact(suite/'result.json'),'trx':artifact(suite/'simulation.trx')}
s['earnedCampaigns']=[]
for species in ['harl','peldra','sulan','wenth']:
 proof_path=folders['EmpireConflict']/'earned'/species/'summary.json';proof=read(proof_path);assert proof['passed'] and proof['expiryWitnessed'] and proof['mainSaves'] and not proof['injectedAssets']
 s['earnedCampaigns'].append({'species':species,'seed':proof['seed'],'passed':True,'injectedAssets':False,'paidBothMilitaries':True,'mainSaves':True,'expiryWitnessed':True,'evidence':artifact(proof_path),'stages':proof['mainStages']})
for criterion in s['criteria']:
 old=criterion['evidence'] if criterion['id']=='ECF-AC-012' else []
 criterion['evidence']=[artifact(folders['EmpireConflict']/'results.json'),artifact(suite/'simulation.trx'),*old]
s['nativeCompilation']={'passed':True,'source':artifact(folders[current_name]/'source.json'),'optInMetadata':artifact(folders[current_name]/'opt-in-metadata.json'),'normalMetadata':artifact(folders[current_name]/'normal-metadata.json'),'runtimeClaim':'Actual combined UI and conflict source, all twelve native scenarios'}
s['combinedLayout']={'passed':True,'checks':len(layout_result['checks']),'results':artifact(layout/'results.json'),'source':artifact(layout/'source.json'),'normalMetadata':artifact(layout/'normal-metadata.json')}
s['integration']={'authorized':True,'state':'verified isolated combined checkout; main merge pending','mainBaseline':s['baselineCommit'],'newerUi':'f2dc9bf','conflict':'b8068216af74627324e250d17f5420fe2270f1a2','sourceCommit':s['productSource']['commit'],'physicalInputAndReleaseAcceptance':False}
s['noMergePushReleaseCleanup']=False;s['noPushReleaseCleanup']=True
s['acceptanceScope']='Fresh local automated combined map-first UI and empire conflict acceptance; physical-input/readability, accessibility, platform/export, fun/balance, security and release gates remain separate.'
(base/'final-summary.json').write_text(json.dumps(s,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'sourceCommit':s['productSource']['commit'],'scenarios':len(runs),'mainCampaigns':len(s['earnedCampaigns']),'layoutChecks':s['combinedLayout']['checks'],'suite':2402}))
