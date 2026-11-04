import hashlib,json,shutil,subprocess
from pathlib import Path
root=Path('F:/Projects-PCGames/OpenAntares')
workspace=Path('C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres')
base=root/'reports/conflict-ui-integration-20261104'
out=base/'postmerge'
def git(*args):return subprocess.check_output(['git','-C',str(root),*args]).decode().strip()
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
gates=load(out/'gates.json');merge=load(out/'merge.json');summary=load(base/'final-summary.json')
assert gates['passed'] and merge['passed']
assert git('branch','--show-current')=='main' and git('rev-parse','HEAD')==gates['mainMerge']
for row in summary['productSource']['files']:
    assert hashlib.sha256((root/row['path']).read_bytes()).hexdigest()==row['sha256'],row['path']
suite=load(root/gates['default']['folder']/'result.json')
assert suite['passed'] and suite['counters']['passed']=='2402' and suite['counters']['total']=='2402'
assert suite['counters']['failed']=='0' and suite['counters']['notExecuted']=='0'
assert suite['sourceRoot'].replace('\\','/')==str(root).replace('\\','/')
metadata=load(out/'normal-metadata.json')
assert not metadata['presentScenarioTypes'] and not metadata['presentHelpers'] and not metadata['validationNamespaceTypes']
merge['postmergeBuildAndSuite']='passed'
(out/'merge.json').write_text(json.dumps(merge,indent=2)+'\n')
for job in [gates['build']['queueJob'],gates['default']['queueJob'],gates['normalQueueJob']]:
    dest=out/'queue-evidence'/job;dest.mkdir(parents=True,exist_ok=False)
    for filename in ['request.json','result.json','command.log']:
        p=Path('F:/Tools/CodexTestRunner/state/results/openantares')/job/filename
        if p.is_file():shutil.copyfile(p,dest/filename)
for name in ['.Run-CombinedPostmerge.ps1','.Complete-CombinedIntegration.py']:
    shutil.copyfile(workspace/name,out/name.lstrip('.'))
result={'passed':True,'mainMerge':gates['mainMerge'],'testedSource':summary['productSource']['commit'],
        'exactSourceFiles':len(summary['productSource']['files']),'simulationPassed':2402,'failed':0,'skipped':0,
        'normalValidationNamespaceTypes':[],'build':gates['build'],'suite':gates['default'],
        'mainRenderedCoverage':'Exact game/UI/content/source identity with the isolated combined acceptance; no physical-input claim',
        'retainedBranchesWorktrees':True,'pushReleaseCleanup':False}
(out/'verification.json').write_text(json.dumps(result,indent=2)+'\n')
prefix=('**Combined UI/conflict locally integrated, 2026-11-04 — Verified for combined automated scope:** '
        'Owner-authorized main integration preserves the newer map-first UI and conflict controls/reports/focus/legacy cleanup. '
        'Tested source d49e63c;12 rendered scenarios,four earned Main campaigns,28 authoritative save/future replays,107 layout checks and legacy rules8/9/11/12 cleanup pass. '
        'Fresh actual-main solution build and Windows2402/2402 pass,0failed/skipped;all408 authoritative inputs match and ordinary validation namespace is empty. '
        'See [integration report](reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md). '
        'All branches/worktrees,owner prose,raw evidence and the existing three-log exception are retained. '
        'No push/release/cleanup;physical input,human readability,accessibility,platform,balance,security and release remain separate. '
        'Earlier pending statements below are historical;17 parent rows/totals remain unchanged.\n\n')
for name in ['AGENTS.md','PROJECT_STATUS.md','CLAUDE.md']:
    p=root/name;old=p.read_bytes();p.write_bytes(prefix.encode()+old)
p=root/'reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md'
report=p.read_text(encoding='utf-8')
report=report.replace('Status: Combined automated acceptance verified in the isolated checkout. Authorized main merge pending.',
                      'Status: Merged locally into main. Combined automated acceptance and fresh actual-main checks passed.')
report+=f'''\n## Main integration and fresh verification\n\nFast-forwarded clean main from `{merge['mainBefore']}` to `{merge['mainAfter']}` after isolated acceptance passed. Recovery branch: `{merge['recoveryBranch']}`. Both newer UI and conflict commits remain ancestors. All408 authoritative source inputs match the tested source byte for byte;{merge['rawCommittedArtifacts']} committed raw acceptance/native artifacts matched their Git objects. No newer UI was replaced by the older conflict layout.\n\nFresh checks executed from `F:/Projects-PCGames/OpenAntares`: solution build passed; full Windows2402/2402,0failed/skipped passed; ordinary assembly metadata confirms all13 scenario types,both helpers and the entire validation namespace are absent. Source snapshots before/after each native job match. [Postmerge verification](conflict-ui-integration-20261104/postmerge/verification.json), [merge identity](conflict-ui-integration-20261104/postmerge/merge.json).\n\nThe isolated acceptance summary retains its original premerge source/branch identity. This postmerge record establishes the exact source relationship and fresh main checks; no recorded evidence was relabeled. All prior branches/worktrees and the isolated integration checkout remain. No push,release or cleanup.\n'''
report+='''
## Separate owner-operated physical follow-up

Prerequisites: .NET8,the installed Godot4.7.2 Mono build,the ordinary assembly verified above,and fresh QA save names. Keep the retained saves/evidence unchanged. These checks assess physical input and human understanding; the automated merge gate above is complete.

Build from main with `& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot 'F:/Projects-PCGames/OpenAntares' -Suite build`. Launch the ordinary game from PowerShell:

```powershell
$qaLog = Join-Path $env:TEMP ('OpenAntares-combined-ui-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
& 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --path 'F:/Projects-PCGames/OpenAntares' --log-file $qaLog
```

1. At800x600,use mouse and keyboard on New Game. Scroll every setting and activate Start. Expected:Start/Load remain reachable,default rules12 Standard opens on the map,and no control is clipped.
2. Visit each bottom-navigation workspace,open a picker,scroll and Tab/Shift+Tab,then resize between800x600 and1920x1080. Expected:focused controls remain visible,popups stay reachable,return restores the map,and navigation/resize does not advance the campaign or submit an order.
3. Copy `F:/Projects-PCGames/OpenAntares/reports/conflict-ui-integration-20261104/final07-EmpireConflict/earned/harl/main-active-battle.json` to a fresh timestamped `qa-combined-human-...json` name under `C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves`; retain the original. Load that copy through Main. Open conflict orders,battle/blockade reports and permitted system/fleet links. Expected:readable controls and clear own authority/response requirements,no private opposing orders/equipment assumptions,and no automatic command from opening or resizing.
4. Physically review and apply one legal order,acknowledge only its intended report,and advance one boundary. Save under another fresh QA name and reload. Expected:one submission,clear outcome,retained orders/damage/dates and no duplicate report/action. The dated truce and other six saved stages are available beside the active-battle input for the corresponding checks in the retained historical checklist; use these actual Main saves as inputs.
5. Load a fresh QA copy of a genuine rules11 archive. Expected:the older rules and peaceful rival behavior remain;opening current conflict controls creates no war or orphan label. Return to New Game and verify the current default remains rules12.

Report back:main commit,engine/display size,input method,QA filename/seed,action,expected/actual result,pass/fail and screenshot/log path. Those results unlock any human-usability/accessibility corrections and their focused regressions;platform/export,balance,security and release remain separately scoped.
'''
p.write_text(report,encoding='utf-8')
with (base/'progress.md').open('a',encoding='utf-8') as f:
    f.write(f"\n- Main fast-forward completed at {merge['mainAfter']}; fresh actual-main build and Windows2402 pass. All408 source bytes match; ordinary namespace empty. See postmerge/verification.json. No push/release/cleanup.\n")
# Keep the chat's separate instruction file current while preserving all its prior bytes.
p=workspace/'AGENTS.md'
if p.exists():
    p.write_bytes(('**Completed combined integration, 2026-11-04:** Newer map-first UI and conflict are merged into local main after combined acceptance; actual-main build/2402 tests and408-file identity pass. See F:/Projects-PCGames/OpenAntares/reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md. All old branches/worktrees retained; no push/release/cleanup. Earlier future-gate wording below is historical.\n\n').encode()+p.read_bytes())
paths=['AGENTS.md','PROJECT_STATUS.md','CLAUDE.md','reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md','reports/conflict-ui-integration-20261104/progress.md','reports/conflict-ui-integration-20261104/postmerge',gates['build']['folder'],gates['default']['folder']]
for row in suite['restoredEvidencePointers']:
    paths.append(Path(row['newArtifactRetained']).relative_to(root).as_posix())
newsave=git('ls-files','--others','--exclude-standard','--','reports/conflict-execution/save-tests').splitlines()
paths.extend(newsave)
assert not git('diff','--cached','--name-only')
git('add','--',*paths)
git('diff','--cached','--check')
git('commit','-m','Record completed main integration and fresh native acceptance checks')
for line in git('ls-files','--stage','--','reports/conflict-ui-integration-20261104/postmerge',gates['build']['folder'],gates['default']['folder']).splitlines():
    fields,name=line.split('\t',1);data=(root/name).read_bytes()
    assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==fields.split()[1],name
assert not git('status','--porcelain'),git('status','--porcelain')
print(json.dumps({'passed':True,'main':git('rev-parse','HEAD'),'testedSource':summary['productSource']['commit'],'simulationPassed':2402,'sourceFiles':408,'report':str(root/'reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md')}))
