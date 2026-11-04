import hashlib
import json
from pathlib import Path
import subprocess
import shutil

root = Path.cwd()
base = root / 'reports/conflict-ui-integration-20261104'
main = Path('F:/Projects-PCGames/OpenAntares')
workspace = root.parent.parent
def git(*args, cwd=root):
    return subprocess.check_output(['git', '-C', str(cwd), *args]).decode('utf-8').strip()
def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
gates = json.loads((base/'final-gates09.json').read_text(encoding='utf-8-sig'))
assert gates.get('passed') is True, 'Final replay must pass before preparing merge'
jobs=[gates['layoutJob'],gates['build']['queueJob'],gates['default']['queueJob'],gates['cleanupJob'],gates['verifierSelfTestsJob'],gates['summaryJob'],gates['evidenceReplayJob']]
for job in jobs:
    origin=Path('F:/Tools/CodexTestRunner/state/results/openantares')/job
    dest=base/'final-queue-evidence'/job
    dest.mkdir(parents=True,exist_ok=False)
    for filename in ['request.json','result.json','command.log']:
        p=origin/filename
        if p.is_file(): shutil.copyfile(p,dest/filename)
verification=json.loads((base/'final-queue-evidence'/gates['evidenceReplayJob']/'command.log').read_text(encoding='utf-8-sig'))
assert verification == {'passed':True,'errors':[]}, verification
summary = json.loads((base/'final-summary.json').read_text())
baseline = json.loads((base/'integration-baseline.json').read_text(encoding='utf-8-sig'))
checks = {'branch': git('branch', '--show-current') == 'codex/conflict-ui-integration',
          'mainUnchanged': git('rev-parse','HEAD',cwd=main) == baseline['main'],
          'mainClean': not git('status','--porcelain',cwd=main),
          'conflictRulesAndContentUnchanged': not git('diff','b806821','HEAD','--','src','content'),
          'newerThemeResearchAndRendererUnchanged': not git('diff','234b45e','HEAD','--','game/theme/openantares_theme.tres','game/Components/ResearchBar.cs','project.godot')}
for row in baseline['preservedConflictLocalDocuments']:
    checks['retained:'+row['path']] = digest(workspace/row['path']) == row['sha256']
current_refs = dict(line.rsplit(' ',1)[::-1] for line in git('show-ref').splitlines())
for line in baseline['refs'].splitlines():
    commit, ref = line.split(' ',1)
    if ref != 'refs/heads/codex/conflict-ui-integration':
        checks['ref:'+ref] = current_refs.get(ref) == commit
checks['stashesRetained'] = git('stash','list') == baseline['stashes'].strip()
old_trees = [line for line in baseline['worktrees'].splitlines() if line.startswith('worktree ')]
current_trees = git('worktree','list','--porcelain').splitlines()
checks['worktreesRetained'] = all(line in current_trees for line in old_trees)
for row in summary['productSource']['files']:
    checks['source:'+row['path']] = digest(root/row['path']) == row['sha256']
invariants = {'sourceCommit':git('rev-parse','HEAD'),'passed':all(checks.values()),'checks':checks}
(base/'premerge-invariants.json').write_text(json.dumps(invariants,indent=2)+'\n')
assert invariants['passed'], [k for k,v in checks.items() if not v]

prefix = ('**Combined acceptance verified, 2026-11-04; authorized main merge pending:** '
          'The isolated newer map-first UI plus conflict source d49e63c passes the full Windows2402/2402 suite,107 layout checks,all12 graphical scenarios,four earned Main campaigns and the authoritative28-save future replay. '
          'The newer UI/renderer and conflict controls/reports/focus/legacy cleanup are combined. '
          'See [combined integration report](reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md). '
          'Merge only this tested source; retain every branch/worktree,owner prose and raw evidence. '
          'No push/release/cleanup; human/platform/balance/security/release scopes remain separate. '
          'Earlier statuses below retain their historical scopes;17 parent rows/totals are unchanged.\r\n\r\n')
for name in ['AGENTS.md','PROJECT_STATUS.md','CLAUDE.md']:
    p=root/name
    old=p.read_bytes()
    assert not old.startswith(prefix.encode()), 'Do not repeat preparation'
    (base/('preprefix-'+name+'.sha256')).write_text(hashlib.sha256(old).hexdigest()+'\n')
    p.write_bytes(prefix.encode()+old)

p=root/'docs/TEST_RUNNER.md'
data=p.read_bytes()
data=data.replace(b'unchanged per-scenario frame limits and actual graphical processes',b'actual graphical processes and unchanged assertions/turn limits (Rival now uses the72000 emergency-frame cap shared by longer campaigns; its incomplete7200-frame attempt is retained)')
p.write_bytes(data)

table=[]
for case in summary['nativeMatrix']:
    result=json.loads((root/case['evidence']['path']).read_text(encoding='utf-8-sig'))
    table.append(f"| {case['scenario']} | {len(result['checks'])} | Passed |")
report='''# Newer UI and empire conflict integration — 2026-11-04

Status: Combined automated acceptance verified in the isolated checkout. Authorized main merge pending.

The owner requested combining the newer UI with conflict in an isolated checkout, running fresh combined acceptance, then merging into main after passing. Push, release and cleanup remain outside scope.

## Combined source and behavior

Branch `codex/conflict-ui-integration` combines main `234b45e5eab968d768095d7c858c95bfa12e92f0` (including newer map-first UI `f2dc9bf`) with conflict `b8068216af74627324e250d17f5420fe2270f1a2`. Final recorded source is `d49e63c79e7e7389b134e9f015646a38d84562b6`; compiled game/UI inputs are identical to `b9cb837356c0aecfff32c58775ddaae7dcbf99c9`.

The map-first shell, right console, bottom navigation, management workspaces, theme and Research controls remain. Conflict orders/attention, battle/blockade reports, responses, readiness blocking, feedback and legacy-label ownership are integrated. ANGLE,60FPS and VSync remain unchanged. Simulation/content match the accepted conflict branch; default rules12/writer13/79 supported pairs remain active. Setup now scrolls within the physical viewport while keeping Start/Load reachable. The resize callback preserves existing focus without duplicating automatic scrolling for newly focused controls.

Harnesses navigate actual workspaces and Game-menu actions, reacquire controls after rebuilt panels and retain state/save/future/visibility assertions. The scout fixture no longer duplicates automatic focus scrolling in the same frame; its original reachability assertion remains. The responsive colony gallery adds24 bounds checks. No gameplay assertion or campaign turn/seed limit was relaxed.

## Passing combined acceptance

Full native Windows solution build passed. Full unfiltered simulation suite:2402 passed,0failed/skipped. Layout:107 passed. Legacy cleanup:rules8/9/11/12 passed. All native cases have zero engine/console diagnostics and matching committed source/DLL evidence; ordinary rebuild/metadata restoration passed, with every validation namespace type absent.

| Native scenario | Assertions | Result |
| --- | ---: | --- |
'''+ '\n'.join(table)+'''

Selected native records are `gate06-{Legacy,Scout,Colony}`, the eight passing `final07-*` cases and fresh `rival08-Rival`. Four actual Main conflict campaigns passed for Harl42,Peldra45,Sulan43,Wenth42, each retaining seven stages,three equal future boundaries per stage and observed expiry. The original authoritative verifier reloaded all28 Main saves and replayed their futures successfully. Its seven negative self-tests and nine additional source-equivalence negative controls passed.

Final gates and exact queue IDs: [final-gates09.json](conflict-ui-integration-20261104/final-gates09.json). Machine summary: [final-summary.json](conflict-ui-integration-20261104/final-summary.json). Invariants: [premerge-invariants.json](conflict-ui-integration-20261104/premerge-invariants.json).

## Retained failures and source equivalence

All initial/targeted failures remain under `reports/conflict-ui-integration-20261104`; they are not counted as passing. They exposed old navigation assumptions,800x600 setup clipping and a newly-focused picker being scrolled twice during resize. Diagnostic traces and subsequent passing regressions are retained.

`final07-Rival` exited at its7200-frame emergency cap without a result and remains incomplete. The two committed launchers changed only Rival's emergency cap to72000; fresh `rival08-Rival` then completed1194 assertions. Every other authoritative byte, including all game/UI/C# harness inputs, is identical. The explicit proof checks all408 authoritative inputs and the exact two-file Git delta. Recorded source identities remain honest; no evidence was relabeled. [Source equivalence](conflict-ui-integration-20261104/source-equivalence.json), [negative controls](conflict-ui-integration-20261104/source-equivalence-negative-controls.json).

## Preservation and scope

All prior branches/worktrees/stashes,owner prose,saves/raw evidence and local conflict-document edits are retained. No second implementation reviewer was dispatched: the prior conflict implementation review and separate UI review remain. Parent17 requirement rows/totals are unchanged. All current Godot launches use explicit log files. The earlier owner-approved exception for exactly three historical diagnostic logs remains unchanged;8219 other original QA files and all saves/screenshots were preserved in that audit. No complete8222-file survival claim is made.

This establishes combined local automated acceptance through real rendered controls/signals and saved-game replay. It does not establish physical input, human readability,accessibility,platform/export,balance,security or release acceptance; their existing checklist and scopes remain separate. No push,release or cleanup.
'''
(root/'reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md').write_text(report,encoding='utf-8')
with (base/'progress.md').open('a',encoding='utf-8') as f:
    f.write('\n- FinalGates09 passed: Windows2402,layout107,cleanup8/9/11/12,original verifier7 self-tests and authoritative28 Main-save replays. Source d49e63c; nine equivalence controls also pass. Premerge preservation/source invariants verified; final evidence/docs preparation complete. Main merge is pending.\n')
print(json.dumps({'passed':True,'sourceFiles':len(summary['productSource']['files']),'main':git('rev-parse','HEAD',cwd=main)}))
