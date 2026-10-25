"""Build an acceptance inventory from completed runs, never projected counts."""
import datetime
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path
from audit_artifacts import ROOT, digest, runtime

OUT = ROOT / 'reports/scout-execution'
def git(*args, cwd=ROOT):
    return subprocess.check_output(['git', *args], cwd=cwd, text=True).strip()

commands=json.loads((OUT/'task15-commands.json').read_text(encoding='utf-8-sig'))
assert len(commands)==7 and all(c['exitCode']==0 for c in commands)
runs=[]
for command in commands:
    row=runtime(command['artifact'],command['artifact']+'/process.log')
    row.update(command)
    runs.append(row)

primary=Path('F:/Projects-PCGames/OpenAntares')
baseline=json.loads((ROOT/'reports/scout-design/document-checks.json').read_text())
expected=next(value for value in baseline.values() if isinstance(value,dict) and 'docs/FEATURE_REQUESTS.md' in value)
owners=[dict(path=path,expectedSha256=sha,actualSha256=digest(primary/path)) for path,sha in expected.items()]
assert len(owners)==4 and all(row['expectedSha256']==row['actualSha256'] for row in owners)
main=git('rev-parse','main',cwd=primary)
assert main=='42e1282b9f38fd8d51d4c99ed0f7481437efcee1'
worktrees=[block for block in git('worktree','list','--porcelain',cwd=primary).split('\n\n') if block]
assert len(worktrees)==7
index=git('diff','--cached','--name-only',cwd=primary)
assert not index
preservation=dict(main=main,indexEmpty=not index,primaryOwnerHashesUnchanged=True,ownerFiles=owners,worktrees=worktrees,primaryStatus=git('status','--porcelain',cwd=primary))

classes={1:['Command','Generation'],2:['Query','Ai'],3:['Query','Ready'],4:['Resolution','Compatibility'],5:['Query','Resolution'],6:['Ai','Integration'],7:['Ready','Resolution'],8:['Report','Command'],9:['Save','State'],10:['Compatibility','Save','Generation','Integration'],11:['Integration','Compatibility'],13:['Content','Generation','Query'],14:['Ready','Resolution','Ai'],15:['Resolution','Save','Command']}
trx=OUT/'task15-simulation/task15.trx'
tests=[]
if trx.is_file():
    document=ET.parse(trx);ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    tests=[dict(name=node.get('testName'),outcome=node.get('outcome')) for node in document.findall('.//t:UnitTestResult',ns)]
    assert tests and all(test['outcome']=='Passed' for test in tests)
criteria=[]
for n in range(1,16):
    evidence=['docs/DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md'] if n==12 else ['tests/OpenAntares.Simulation.Tests/Scouting'+kind+'Tests.cs' for kind in classes[n]]
    if n!=12:evidence+=['reports/scout-execution/task14-validation.md','reports/scout-execution/task15-scout-20261025/results.json','reports/scout-execution/task15-commands.json']
    if n!=12 and tests:evidence+=['reports/scout-execution/task15-simulation/task15.trx']
    assert all((ROOT/path).is_file() for path in evidence)
    names=[test['name'] for test in tests if n!=12 and any('Scouting'+kind+'Tests.' in test['name'] for kind in classes[n])]
    if n!=12 and tests:assert names
    criteria.append(dict(id=f'SCT-AC-{n:03}',status='Verified for design only' if n==12 else 'Verified for local automated scope',executedEvidence=evidence,executedTestNames=names,limits=[] if n==12 else ['Physical input, comfortable reading, comprehension, balance, other platforms, security audit, export and release remain open.']))

source=[]
for folder in ['src','game','content','tests']:
    for path in sorted((ROOT/folder).rglob('*')):
        if path.is_file() and not any(part in {'bin','obj'} for part in path.parts):
            source.append(dict(path=path.relative_to(ROOT).as_posix(),sha256=digest(path)))
suitepath=OUT/'task15-suite.log'
suite=None
if suitepath.exists():
    match=re.search(r'Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+), Total:\s+(\d+)',suitepath.read_text(encoding='utf-8-sig'))
    if match:
        failed,passed,skipped,total=map(int,match.groups());assert failed==0 and skipped==0
        suite=dict(failed=failed,passed=passed,skipped=skipped,total=total,artifact='reports/scout-execution/task15-suite.log')
previous=json.loads((OUT/'final-machine-summary.json').read_text()) if (OUT/'final-machine-summary.json').exists() else {}
summary=dict(capturedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),branch=git('branch','--show-current'),validatedProductCommit='1a74167b04470e4535e10d0d87bdb4e662a31e02',snapshotHead=git('rev-parse','HEAD'),rulesDefault=7,writer=8,criteria=criteria,runtime=runs,suite=suite,preservation=preservation,sourceInventory=source,reviewStatus=previous.get('reviewStatus','Pending fresh whole-branch review'),limits=['Local Windows Godot signals/handlers and geometry only; no physical-input, human comprehension, balance, cross-platform, dependency/security audit, clean export or release acceptance.','No primary integration, push, branch/worktree/evidence cleanup.','Parent REQ-017 remains In Progress;17-row totals unchanged.'])
(OUT/'final-machine-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print('Actual runtime records:',[(r['scenario'],r['records']) for r in runs])
print('QA/PNG:',sum(len(r['qaSaves']) for r in runs),sum(r['pngCount'] for r in runs))
print('Primary owner hashes/main/index/seven worktrees verified; suite:',suite)
