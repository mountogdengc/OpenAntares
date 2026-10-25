"""Close the one review from actual post-correction gates, retaining the raw verdict."""
import json
import subprocess
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'reports/scout-execution'
commands=json.loads((OUT/'final-fixes-commands.json').read_text(encoding='utf-8-sig'))
assert len(commands)==7 and all(c['exitCode']==0 for c in commands)
subprocess.run([sys.executable,str(OUT/'finalize_acceptance.py'),'final-fixes'],cwd=ROOT,check=True)
summary=json.loads((OUT/'final-machine-summary.json').read_text())
assert summary['suite']['failed']==0 and summary['suite']['passed']==1398
assert all(row['diagnostics']==[] for row in summary['runtime'])
summary['reviewStatus']='Complete; no open Critical or Important findings'
summary['deferredMinors']=[dict(id='M1',description='Custom Standard maps with fewer than two eligible habitat candidates lack an explicit omitted-placement explanation.',criterion='SCT-AC-013')]
summary['criteria'][12]['status']='Implemented, Unverified'
summary['criteria'][12]['limits'].append('Review M1: insufficient-candidate custom maps omit the promised no-habitat setup explanation; core Standard/Off placement is verified.')
(OUT/'final-machine-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
report=f'''# Single whole-branch review and correction disposition — 2026-10-25

One fresh-context gpt-6-astra/xhigh review inspected main42e1282 through4072914. [Raw completed report](final-review-raw.md) is retained verbatim. The reviewer found no Critical, three Important and one Minor issue; all five required focus cases were checked. No considered gameplay behavior was declined as outside scope. Existing human/platform/security/export/release limits remain open.

The executor verified and accepted I1–I3 as Important by their player effects. One regression-first correction pass is committed at `{summary['validatedProductCommit']}`; no second reviewer or re-review was commissioned. Product source hashes, exact commands and actual inventories are in [final machine evidence](final-machine-summary.json); [pre-review evidence](pre-review-machine-summary.json) retains the original1391-test/4436-record snapshot.

| Finding | Observed RED | Correction and GREEN |
| --- | --- | --- |
| I1 health command versus exact ameba consent | Acknowledged quarantine and last-colony purge reject invalid_crisis; delegated preparation fails. Unacknowledged quarantine control passes. | Reconcile changed scout consequences once inside the shared crisis path before validation, including automatic commands. Manual quarantine/purge retire stale receipts and advance revision once. Delegated containment can prepare, while the enclosing Ready rolls back unchanged when its response loses consent; no roll or Warning opportunity is spent. |
| I2 cargo authority/budget | Both strict save/load-accepted Ameba Off cargo cases emit a cancellation, with zero or one unrelated scout. | Shared preview assesses cargo legality without issuing human cargo cancellation commands. Both cases Ready successfully and preserve the waiting order. Qualification remains: this is a valid saved/custom-state reproduction, not an established generated command sequence. |
| I3 informed route consent | Projection lacks the route, and actual Scout Warning UI fails its destination/duration/date check. | Own-view route projection includes chart name/ID, ordinary duration, observation date and survival override. UI displays those before acknowledgement, exposes response_required when no route exists, and reports automatic override reasons. Actual800/1920 route/override/no-route checks pass, with screenshots inspected. |

Seven simulation regression cases pass; full corrected Suite1398/1398,0 failed/skipped. [Valid RED](final-fixes-red-valid.log), [focused GREEN](final-fixes-focused.log), [full suite](final-fixes-suite.log), [TRX](final-fixes-simulation/final-fixes.trx), [UI RED](final-fixes-ui-red-20261025/results.json). Initial test-fixture mistakes were corrected and the RED rerun before product edits; these initial failures are retained, not counted as proof of product defects.

| Post-correction native gate | Actual records | Diagnostics |
| --- | ---: | ---: |
'''
for row in summary['runtime']:report+=f"| {row['scenario']} | {row['records']} | 0 |\n"
report+='''
All seven gates ran sequentially on the corrected source. Each real opt-in assembly contains7 scenarios; ordinary rebuilds exclude all7 and both helpers with0 warnings/errors. Both process and engine diagnostics were inspected. Primary main42e1282, four original owner hashes, empty primary index and all seven worktrees are reverified in the final inventory.

## Deferred minors

M1: custom Standard maps with fewer than two eligible habitat candidates do not explicitly explain omitted habitat placement in setup. Placement itself and core Standard/Off behavior pass; the missing explanation is deferred per the one-pass workflow. SCT-AC-013 and M7-I remain Implemented, Unverified for exact full acceptance. Remaining functional SCT criteria are Verified for their executed local scope; SCT-AC-012 is design-only Verified. Parent REQ-017 and17-row counts remain unchanged.

No open Critical or Important issue remains after verified corrections. This is executor verification of the single review's fixes, not a second independent verdict. Owner disposition is next: local integration, PR, or preserving the branch. No merge, push, release, branch/worktree/evidence cleanup occurred.
'''
(OUT/'final-review.md').write_text(report,encoding='utf-8')
subprocess.run([sys.executable,str(OUT/'write_handoff.py')],cwd=ROOT,check=True)
ledger=ROOT/'.superpowers/sdd/2026-10-25-scout-survival-and-ameba/progress.md'
with ledger.open('a',encoding='utf-8') as stream:
    stream.write('\nFinal: fixed I1 — QuarantineCancelsCargoDepartureAndReconcilesItsExactConsentOnce/PurgingLastColonyReconcilesAnAcknowledgedAutomaticFallback/DelegatedQuarantineReconcilesButReadyRollsBackTheWholeChangedResponse RED→GREEN; suite1398/1398, affected native gates green.\n')
    stream.write('Final: fixed I2 — AcceptedOffCargoSaveKeepsItsWaitingOrderRegardlessOfUnrelatedScoutCount false/true RED→GREEN; suite1398/1398. Valid-save/custom-state qualification preserved.\n')
    stream.write('Final: fixed I3 — RetreatRouteDisclosesOnlyOwnChartDurationFreshnessAndNeutralOverride plus actual Warning label RED→GREEN at800/1920; suite1398/1398; route/override/cargo-response controls and inspected image evidence retained.\n')
    stream.write('Final correction gates: '+str([(r['scenario'],r['records']) for r in summary['runtime']])+'; all7 exit0/zero diagnostics/opt-in7/ordinary0/helper0, normal0 warnings/errors. One fresh review only; no re-review. M1 deferred, SCT-AC-013/M7-I exact acceptance Implemented, Unverified; parent totals/human limits unchanged. Primary/main/owner/index/seven worktrees reverified.\n')
(OUT/'progress.md').write_text(ledger.read_text(encoding='utf-8'),encoding='utf-8')
print('Single review closed by regression verification; M1 explicitly deferred.')
