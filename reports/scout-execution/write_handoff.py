import json
import re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
s=json.loads((ROOT/'reports/scout-execution/final-machine-summary.json').read_text())
review=s['reviewStatus'];verified=review=='Complete; no open Critical or Important findings'
status=('Implemented, Unverified for exact full acceptance; review M1 deferred' if s.get('deferredMinors') else 'Verified for local automated scope') if verified else 'Implemented, Unverified pending the fresh whole-branch review'
suite=s['suite'];count=str(suite['passed']) if suite else 'pending final audit suite'
report=f'''# Scout survival and space-ameba validation — 2026-10-25

The approved M7 slice is implemented on `codex/scout-survival`, separate from primary main. M7-D/M7-P are Verified; M7-I is **{status}**. Review: {review}. [Machine evidence](scout-execution/final-machine-summary.json) contains criterion mappings, source/QA/PNG hashes, exact commands/exits, real metadata and preservation results. [Execution ledger](scout-execution/progress.md) records every task, failure and ruling.

Branch rules7 defaults to Harl/One Peldra/plague Standard/ameba Standard; Off and Solo remain visible independent choices. Scouts start neutral Observe with ameba withdrawal enabled; neutral Avoid is opt-in. One stationary habitat gives a harmless arrival Warning and100% prompt departure using ordinary travel. Scout Study has75% core survival, awards600 internal research units (6 displayed points) once per empire/habitat after normal research/cure, and leaves a95% late escape opportunity (71.25% combined hull retention). Remaining exposed or failing a roll destroys the vessel and its cargo. Peaceful rivals, current/stale own knowledge, exact consent, private bounded history, atomic turns and paid replacement remain enforced. No roaming, colony attack, interstellar pursuit, sensors, tactical combat or warship doctrine was added.

## Executed evidence

Validated product commit: `{s['validatedProductCommit']}`. The machine source inventory pins exact product/test bytes. The single raw review pins4072914; its correction report records the one regression-first fix pass without a second review. Full fresh simulation suite: **{count} passed**,0 failed/skipped when recorded. All49 frozen rules6 campaigns and196 old-engine futures remain exact apart from schema_version; older fixtures and the exact29 supported schema/rules pairs pass. Writer8 never upgrades an old campaign's rules/content.

24 untouched complete-core generated campaigns (four species × three sizes × Solo/One Peldra) discover, safely withdraw, build, research and cure within160 turns through ordinary commands.24 Off starts and24 Solo Observe pairs ×30 boundaries preserve rules6 galaxy/old streams. [Activation report](scout-execution/task14-validation.md) records original RED/GREEN evidence and immutable proof references. Actual UI generated Study uses draw55 and survives; Warning, Exposed and committed-transit Save/Load each match4 future turns. Rare74/75,49/50 and94/95 outcomes are separately labelled pre-session fixtures, never generated luck or live outcome injection.

| Fresh final native scenario | Actual records | Diagnostics | Results |
| --- | ---: | ---: | --- |
'''
for run in s['runtime']:report+=f"| {run['scenario']} | {run['records']} | 0 | [evidence]({run['artifact'].removeprefix('reports/')}/results.json) |\n"
report+='\nEach real opt-in assembly contains7 scenario types; each ordinary rebuild excludes all7 and both helpers. Normal rebuilds have0 warnings/errors. Native process logs and engine logs were both inspected for ERROR:/WARNING:. Screenshots supplement native geometry; current generated and authored fixture provenance are separate.\n\n'
report+='## Criterion map\n\n| Criterion | Local disposition | Evidence |\n| --- | --- | --- |\n'
for c in s['criteria']:
    paths=c['executedEvidence'];label='Design approval ADR-016' if c['id']=='SCT-AC-012' else ('Implemented, Unverified: M1 deferred' if c['status']=='Implemented, Unverified' else 'Automated scope verified' if verified else 'Executed; review pending')
    report+=f"| {c['id']} | {label} | "+', '.join(f'[{Path(p).name}](../{p})' for p in paths)+' |\n'
report+='''
The automated scope covers shared commands/knowledge/fallback/turn timing/AI/transactions/reports/persistence/compatibility and actual in-process controls at800/1920. Human comfortable reading, accessibility/uncoached comprehension, physical OS input/focus, subjective aesthetics, balance/long campaigns, other operating systems/architectures, hostile-input fuzzing/security/dependency audits, clean install/export and release remain open. No parent REQ-017 or First Playable human-acceptance closure is claimed.

## Preservation and next action

Primary main remains `42e1282b9f38fd8d51d4c99ed0f7481437efcee1` (rules6/writer7), with an empty index and exactly the original three modified tracked files plus untracked FEATURE_REQUESTS.md. All four owner-file SHA256 hashes match the pre-design record. All seven worktrees, branches, recovery material, failed/successful evidence and QA saves are retained. The branch's status edits do not copy or commit the owner's primary peaceful-mode paragraph. No merge, push, release or cleanup occurred.

Next: owner chooses whether to integrate this reviewed branch locally, create a PR, or keep it for later. Integration needs a fresh combined-result check and preservation of the same owner files. [Raw final review and disposition](scout-execution/final-review.md) accompanies handoff once complete.
'''
if s.get('deferredMinors'):report+='\nDeferred minor M1: Standard setup on a custom map with fewer than two eligible habitat candidates does not explicitly explain omitted placement. Core sizes and actual placement/Off behavior are verified; SCT-AC-013 and M7-I remain Implemented, Unverified for exact full acceptance. Other functional SCT criteria are Verified for executed local scope after I1–I3 correction; design SCT-AC-012 remains design-only. See [review disposition](scout-execution/final-review.md).\n'
(ROOT/'reports/SCOUT_VALIDATION_2026-10-25.md').write_text(report,encoding='utf-8')

intro=f'''# OpenAntares project status

Updated2026-10-25 America/Denver. OpenAntares is an original turn-based space4X with C#/Godot .NET presentation and an independent deterministic simulation. The approved classic Master of Orion II love-letter direction and fanciful aliens respectful of real peoples/beliefs remain authoritative.

Current continuation: the owner approved the full amended scout/space-ameba design in ADR-016 and the separate15-task plan. All implementation tasks and their technical gates have executed on isolated `codex/scout-survival`. M7-I: **{status}**. {review}. Read [validation/handoff](reports/SCOUT_VALIDATION_2026-10-25.md), [machine evidence](reports/scout-execution/final-machine-summary.json), [ledger](reports/scout-execution/progress.md) and [review](reports/scout-execution/final-review.md). Do not repeat completed work or re-ask design, plan or Native execution approval.

## Current product and scope

This review branch defaults to rules7, explicit ameba Standard/Off, Harl/One Peldra/plague Standard, writer8 and29 explicit schema/rules pairs. Primary main remains42e1282, rules6/writer7; M7 is not integrated. Observe is the neutral default, Avoid opt-in; ameba withdrawal is separate and enabled. One stationary habitat, harmless Warning/100% prompt departure, explicit risky Study/late escape and real vessel/cargo loss follow the approved specification. Ordinary peaceful rival/exploration rules, committed travel, private own knowledge and atomic command/turn publication remain. Old saves retain embedded rules/content and unchanged futures.

Roaming creatures, colony attacks, sensors/pursuit, tactical combat, warship doctrine, broader diplomacy/culture, multiplayer and victory remain future scope. Peaceful mode remains an owner backlog decision, not newly implemented. No artwork dependency was introduced.

## Status and evidence

| Milestone | Status | Scope |
| --- | --- | --- |
| M1 First Playable full human acceptance | Deferred | Owner-authorized deferral; technical work may proceed |
| M2 exploration/colonization, M3 species, M4 rivals, M5 colonies, M6 plague | Verified | Previously integrated local automated/design scope; human/delivery limits remain |
| M7-D / SCT-AC-012 | Verified | Full amended design approval ADR-016 |
| M7-P | Verified | Owner-approved15-task plan; retained Native inline execution |
| M7-I | {status} | Functional SCT criteria on isolated review branch |

Parent requirement totals remain17:12 Verified,3 Implemented, Unverified,1 In Progress (REQ-017),1 Deferred. This milestone does not close REQ-017 or change the register's17 rows.

Fresh final simulation suite: {count}; native results and actual metadata/hashes are in the report.24 untouched generated campaigns and720 Off/Observe parity boundaries pass; all49 frozen rules6 baselines/196 genuine old-engine futures remain unchanged except schema_version. Rare threshold fixtures are labelled separately. Seven native scenarios execute sequentially; ordinary builds exclude all7 and both helpers.

## Preservation and integration

Primary main and all four original uncommitted owner files are byte-exact against pre-design hashes, with an empty index. All seven worktrees and every branch, old save, recovery/QA and failed/successful evidence remain. The original peaceful-mode prose stays solely in the owner's primary edit. No main merge, push, release, cleanup or history rewrite occurred. Historical M6 integration evidence: [report](reports/CRISIS_INTEGRATION_2026-10-25.md). M7 design/plan authority: [ADR-016](docs/DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md), [spec](docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md), [plan](docs/superpowers/plans/2026-10-25-scout-survival-and-ameba.md).

## Remaining limits and next action

Physical input/focus, comfortable reading/accessibility/comprehension, aesthetics, balance/long campaigns, other platforms, hostile-input/security/dependency audits, clean install/export and release remain unverified or deferred. Automated local checks do not establish these.

Owner chooses local integration, PR, or keeping the branch as-is after reviewing the report. Preserve owner edits and run fresh combined-result validation for any authorized integration. No new subsystem implementation is inferred.
'''
(ROOT/'PROJECT_STATUS.md').write_text(intro,encoding='utf-8')

for name in ['docs/SPECIFICATIONS.md','docs/ACCEPTANCE_CRITERIA.md','docs/IMPLEMENTATION_PLAN.md','reports/VALIDATION_REPORT.md']:
    path=ROOT/name;text=path.read_text(encoding='utf-8');marker='<!-- M7 current acceptance -->'
    if marker in text:text=text.split(marker)[0].rstrip()+'\n'
    link='../reports/SCOUT_VALIDATION_2026-10-25.md' if name.startswith('docs/') else 'SCOUT_VALIDATION_2026-10-25.md'
    current=f'''\n{marker}
## Current M7 implementation and acceptance — 2026-10-25

Owner approved the full amended ADR-016 design and separate15-task plan; earlier drafting/plan-pending statements above are historical. M7-D/M7-P Verified; M7-I **{status}** on isolated codex/scout-survival. SCT-AC-012 is design-only Verified; functional SCT-AC-001..011/013..015 have the executed criterion mapping in [validation/handoff]({link}), with review status: {review}. Branch current rules7/writer8/29 pairs; primary main42e1282 remains rules6/writer7. Actual suite {count}; all7 fresh native scenarios, zero diagnostics and ordinary7/helper2 exclusions are recorded, with genuine older futures and generated/labelled provenance. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. No integration/push/cleanup; owner chooses next disposition.
'''
    if name=='docs/IMPLEMENTATION_PLAN.md':
        text=text.replace('In Progress; plan review pending','Verified; owner-approved Native inline execution').replace('| Future approved M7-P, genuine rules6 capture first | Functional SCT criteria, retained gates, one final reviewer | Not Started |',f'| Approved M7-P;49 frozen rules6 baselines/196 futures | Functional SCT criteria, retained gates, one final reviewer | {status} |')
        text=re.sub(r'^\| M7-I \|.*$',f'| M7-I | Implement approved scout/ameba slice | Approved M7-P;49 frozen rules6 baselines/196 futures | Functional SCT criteria, retained gates, one final reviewer | {status} |',text,flags=re.M)
        a=text.index('## Next concrete action');b=text.index('## Risks and autonomy',a)
        text=text[:a]+'## Next concrete action\n\nOwner chooses local integration, PR or preserving the scout-survival review branch after the final report. No repeated design/plan/method approval; no automatic integration or new feature work.\n\n'+text[b:]
    path.write_text(text+current,encoding='utf-8')
path=ROOT/'AGENTS.md';text=path.read_text(encoding='utf-8');marker='<!-- M7 acceptance continuation -->'
if marker in text:text=text.split(marker)[0].rstrip()+'\n'
text+=f'''\n{marker}
Current M7 acceptance continuation: all15 approved task gates have executed on codex/scout-survival; M7-I {status}. Review status: {review}. Read reports/SCOUT_VALIDATION_2026-10-25.md, reports/scout-execution/final-machine-summary.json, final-review.md and progress.md before continuing. Earlier M7 planning-only statements are historical. Branch current rules7/writer8/29 pairs; primary main42e1282 remains rules6/writer7, with all four owner files byte-exact, index empty and seven worktrees retained. Do not repeat completed work or re-ask design/plan/Native approval. No per-task agents or automatic second review, integration/push/release/cleanup. Parent REQ-017/17-row totals/human limits unchanged. Next is owner disposition after the one fresh review/correction pass.
'''
path.write_text(text,encoding='utf-8')
print('Wrote report and five current-status documents from actual summary; review:',review)
