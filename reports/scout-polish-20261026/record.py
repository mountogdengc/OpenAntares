"""Record actual M1 follow-up results and owner preservation; no gameplay changes."""
import hashlib
import json
import subprocess
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'reports/scout-execution'))
from audit_artifacts import runtime
def git(*args,cwd=ROOT):return subprocess.check_output(['git',*args],cwd=cwd,text=True).strip()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
native=runtime('reports/scout-polish-20261026/green','reports/scout-polish-20261026/green-process.log')
assert native['records']==237
suite=(OUT/'suite.log').read_text(encoding='utf-8-sig')
assert 'Failed:     0, Passed:  1398' in suite
red=json.loads((OUT/'red/results.json').read_text())
assert not red['passed'] and 'Custom 2-star/rivals1 setup explains omitted habitat' in red['error']
original=json.loads((ROOT/'reports/scout-execution/final-machine-summary.json').read_text())
primary=Path('F:/Projects-PCGames/OpenAntares')
owner=[]
for row in original['preservation']['ownerFiles']:
    actual=sha(primary/row['path']);assert actual==row['actualSha256']
    owner.append(dict(path=row['path'],sha256=actual))
assert git('rev-parse','main',cwd=primary)=='42e1282b9f38fd8d51d4c99ed0f7481437efcee1'
assert not git('diff','--cached','--name-only',cwd=primary)
worktrees=git('worktree','list','--porcelain',cwd=primary).split('\n\n');assert len(worktrees)==7
assert git('rev-parse','codex/scout-survival')=='47e4b386392b78e3f0493f70b8705174c6a465f7'
product=git('rev-parse','HEAD')
data=dict(date='2026-10-26',branch=git('branch','--show-current'),base='47e4b386392b78e3f0493f70b8705174c6a465f7',validatedSource=product,
          scope='M1 follow-up: existing generated setup preview explains omitted habitat; no simulation/default/writer changes',
          suite=dict(passed=1398,failed=0,artifact='reports/scout-polish-20261026/suite.log'),runtime=native,
          cases=[dict(stars=n,rivals=r,omitted=o) for n,r,o in [(2,1,True),(2,0,True),(3,1,True),(3,0,False),(4,1,False)]],
          source=[dict(path=p,sha256=sha(ROOT/p)) for p in ['game/Screens/NewGameScreen.cs','tests/OpenAntares.Game.Validation/ScoutGraphicalValidation.cs']],
          preservation=dict(main=git('rev-parse','main',cwd=primary),indexEmpty=True,ownerFiles=owner,worktrees=worktrees,primaryStatus=git('status','--porcelain',cwd=primary)),
          closedReviewFinding='M1',openReviewFindings=[],limits=['In-process controls and screenshot inspection only; no physical OS input, human comprehension, balance, other-platform, security audit, export or release acceptance.','Other six native gates remain retained evidence from the prior source; this follow-up reruns the affected Scout scenario and normal assembly checks.','No second independent review, main integration, push or cleanup.'])
(OUT/'summary.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
report=f'''# Scout setup explanation follow-up — 2026-10-26

The remaining M1 review detail is corrected on `codex/scout-polish`, source `{product}`, from preserved scout-survival47e4b38. The owner requested continuing in this chat. This bounded follow-up changes one production line: the existing successful Standard setup preview now explains when eligible non-home habitat is insufficient. It reveals no candidate identity/location; Off retains its own explanation. Simulation, placement, RNG, gameplay decisions, rules7 and writer8 are unchanged.

The actual native test failed before implementation on a valid2-star/One setup. After the fix, **237/237 Scout records** pass, including35 new checks across the five independently enumerated0/1/2-candidate boundaries. Actual Start remains usable, generated habitat presence matches the explanation, Off clears the notice, and repeated previews preserve deterministic Start/streams. The inspected1920x1080 [setup screenshot](scout-polish-20261026/green/custom-no-habitat-setup.png) shows the explanation and usable Start. [RED](scout-polish-20261026/red/results.json), [GREEN](scout-polish-20261026/green/results.json), [machine summary](scout-polish-20261026/summary.json).

Full simulation baseline and final suite both pass1398/1398,0 failed/skipped. The native runner's real opt-in assembly contains all7 scenarios; the normal rebuild excludes all7 and both helpers, with0 warnings/errors. Both engine and process logs have0 diagnostics. The other six native scenarios retain their explicitly historical prior-source evidence; no fresh rerun is claimed for them. No second independent review was commissioned for this narrow follow-up; the original raw review/correction evidence remains unchanged.

M1 is closed. SCT-AC-013 and M7-I are now Verified for their local automated scope; the prior report's deferral remains a historical snapshot. All other approved criteria retain their existing acceptance. Parent REQ-017/17-row totals, M1 First Playable human deferral, human comprehension/balance/physical-input/platform/security/export/release limits remain unchanged.

Primary main remains42e1282 (rules6/writer7), with an empty index and the original four uncommitted owner files matching retained SHA256 values. All seven worktrees, scout-survival47e4b38, design branch and all existing QA/recovery evidence remain. No main merge, push, release or cleanup. Next is owner disposition of this completed follow-up branch for local integration, PR or continued preservation.
'''
(ROOT/'reports/SCOUT_POLISH_2026-10-26.md').write_text(report,encoding='utf-8')
print('Recorded:',product,'Scout',native['records'],'suite1398; primary owner/main/index/seven worktrees verified')
