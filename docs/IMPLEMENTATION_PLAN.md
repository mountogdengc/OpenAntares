# OpenAntares continuation plan

Adoption reconciliation and subsequent owner deferral, 2026-10-22. This plan orders work and gates; it does not authorize broad implementation. The [approved 2026-10-19 plan](superpowers/plans/2026-10-19-first-playable-validation-and-exploration-design.md) retains its detailed tasks, amended by [ADR-005](DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md) to permit expansion design while the unavailable uncoached session is deferred. Read [PROJECT_STATUS.md](../PROJECT_STATUS.md) first.

## Baseline

`main` at `6ab206d3ca4811cc9163ee6ea3d5c00ff4bb7b6b`, plus preserved local changes. Build: 0 warnings/errors. Tests: 195 passed, 0 failed/skipped. The continuation adds 463 passing graphical runtime assertions, with a retained opt-in harness and legacy/corrupt UI load evidence. This verifies a working local baseline, not a clean committed release. See [validation evidence](../reports/FIRST_PLAYABLE_VALIDATION_2026-10-22.md).

The old spiral execution checklist is not a completion log; its header says implementation merged in PR #22. The newer validation checklist remains partly unchecked despite later owner passes and fixes. Reconcile those statuses against evidence rather than implementing either plan again.

## Milestones and dependency order

| ID | Objective / requirements | Dependencies and tasks | Verification | Status |
| --- | --- | --- | --- | --- |
| M0 | Adopt/reconcile the existing project; all REQ IDs | Inventory code/docs/Git, recover decisions, map criteria, run baseline, write handoff | Documentation links/IDs/counts, original-file preservation, build/test/startup evidence | Verified for adoption scope; no feature changes |
| M1 | Close First Playable; REQ-001 through REQ-012, REQ-014 | Preserve technical evidence and owner passes; uncoached session deferred by owner; full acceptance remains unverified | Retain runtime passes and explicit limits; gather remaining human evidence when available | Deferred for full acceptance; not a design blocker under ADR-005 |
| M1-I | Establish an integrated technical baseline | M1 evidence; inspect all current diffs and untracked evidence, separate UI/test evidence from unrelated species/research edits, review intended commits/branch | Build/tests and relevant graphical regression on combined intended result; record final SHA and remaining edits | Verified for local technical integration, 2026-10-23; full human acceptance remains M1 |
| M2-D | Reviewed exploration/travel/second-colony design; REQ-015 | Design entry authorized by ADR-005; knowledge/visibility, movement/founding/timing, contrasting examples, legacy policy, multi-colony UI | EXP-AC criteria and builder check complete; owner approved full specification in ADR-006 | Verified for design scope only |
| M2-P | Review the separate expansion implementation plan | Approved M2-D; nine dependency-ordered tasks, exact interfaces/fixtures, regression and runtime evidence | Owner approved plan and Native execution | Verified |
| M2-I | Implement the reviewed expansion milestone | M2-P handoff; isolated checkout/preserved baseline, state/content/commands, persistence compatibility, simulation, UI | New exact-result/failure tests, legacy continuation, graphical multi-colony scenarios | Verified for executed local automated criteria; independent review/corrections complete |
| M3-D | Review species mechanics; REQ-016 | ADR-007 drafting authority; full design approved in ADR-008 with classic direction | AC-016 / SPC-AC-010 owner design review | Verified for design scope |
| M3-P | Plan approved species implementation | Approved M3-D; separate ten-task plan written/self-reviewed | Owner approved plan (“yes”); Native preference preserved | Verified |
| M3-I | Implement reviewed species slice | Approved M3-P; preserve old campaigns and core boundaries | SPC-AC-001..009 and retained regressions; independent review | Verified for local automated acceptance; independent review/correction pass complete |
| M4-D | Define bounded rival expansion; first REQ-017 child | Full design accepted in ADR-010 after builder check | RIV-AC-012 owner approval satisfied | Verified for design scope |
| M4-P | Plan rival implementation | Approved M4-D; separate ten-task plan written/self-reviewed | Owner continue; Native retained | Verified |
| M4-I | Implement bounded rival slice | Approved M4-P; preserve old campaigns and scoped planning | RIV-AC-001..011, retained regressions and final independent review | Verified for bounded local automated acceptance; both follow-up details reviewed and verified |
| M5-D | Define colony development and delegation | Full owner approval in ADR-012 after builder check | COL-AC-012 full design gate | Verified for design scope |
| M5-P | Plan colony-development implementation | Approved M5-D; retained Native | Twelve-task plan self-review and owner plan confirmation | Verified; owner approved with “yes” |
| M5-I | Implement approved colony-development slice | Approved M5-P; isolated execution/preserved genuine old campaigns | COL criteria and retained gates, independent whole-branch review | Verified for local automated scope; authorized corrections verified/integrated |
| M6-D | Define bounded colony crises, beginning with plague | Owner direction/ADR-013; preserve M5 | Full design/builder check/owner review | Verified for design; ADR-014 full approval2026-10-25 |
| M6-P | Plan colony-crisis implementation | Approved M6-D/ADR-014 | Separate14-task plan/self-review/owner review | Verified; owner approved Native inline execution |
| M6-I | Implement approved crisis slice | Approved separate M6-P | CRI criteria, retained gates, independent review | Verified for local automated scope; fourteen task gates, independent review, one correction pass |
| M7-D | Scout survival and space-ameba design | ADR-015 drafting/amendment, full approval ADR-016 | SCT-AC-012 full gameplay approval | Verified for design only |
| M7-P | Plan scout/ameba implementation | Approved M7-D; retained Native inline | Separate15-task plan/self-review/owner review | Verified; owner-approved Native inline execution |
| M7-I | Implement approved scout/ameba slice | Approved M7-P;49 frozen rules6 baselines/196 futures | Functional criteria, retained gates, single review/corrections plus M1 follow-up | Verified for local automated scope;2026-10-26 M1 closure |
| M8-D | Fleet Command and named-admiral gameplay specification | ADR-017 owner direction; FLT-001..009 draft | FLT-AC-012 full gameplay review and fresh builder dispositions | In Progress |
| M8-P | Plan approved fleet/admiral design | Full M8-D approval required | Separate implementation plan review; Native retained | Not Started |
| M8-I | Implement bounded Fleet Command/admiral slice | Approved separate M8-P | Functional FLT criteria and retained regressions/one final fresh review | Not Started |
| M-Later | Remaining strategic systems; REQ-017 | Focused approved subsystem specifications; no inferred AI/combat/diplomacy scope | AC-017 then subsystem criteria | Deferred |
| M-Delivery | Target-platform and release validation; REQ-013 | Define platform/package/performance/support policy, review Release mapping, add export/dependency attribution/CI as approved | AC-013 plus clean install/export/startup/save checks, dependency audit | Not Started as a delivery milestone; local code exists |

M1-I is now technically integrated under explicit owner authorization, with fresh combined regression evidence. This does not close M1 human acceptance. The clean detached spiral worktree and remote feature branch have no commits outside `main`; no feature merge is required from them.

## Next concrete action

Review/amend the complete proposed M8 Fleet Command/admiral gameplay rules. Full design approval precedes a separate implementation plan. M7 integration is complete; do not repeat it. Retain Native after those gates; no automatic implementation.

## Risks and autonomy

- Automation may rerun existing builds/tests, inspect code, reconcile documentation, and propose scoped defect work. A confirmed future fix should be reproduced, reviewed, and tested within its authorized scope.
- Player comprehension remains unverified and is explicitly deferred. ADR-006 approves this expansion milestone; future material changes beyond that scope still require owner review.
- Preserve the two uncommitted species/faction prose changes, incorporated UI fixes/window setting, recovery stash/snapshot and retained evidence. Existing worktree cleanup and remote branch deletion are separate owner choices.
- Avoid a rewrite, new engine/framework, dependency upgrade, unapproved species redesign or combat/AI/network implementation during this bounded milestone. Approved species fixture numbers are reproducible contracts, not verified balance. The approved expansion observation model is the bounded visibility change; do not extend it into strategic sensors or espionage.
- Performance, accessibility, localization, hostile-input limits, licensing inventory and distribution remain explicit later gaps. Do not turn unspecified targets into asserted requirements met.

Use Not Started / In Progress / Implemented, Unverified / Verified / Blocked / Deferred. Update requirement statuses only to match their exact verified contracts, with evidence and dates; distinguish milestone state from code existence.

Historical execution entry: owner approved the separate twelve-task plan with “yes”; M5-P Verified, M5-I In Progress. Follow the execution ledger; no repeated plan/method approval.

Historical M5 review: reports/COLONY_FINAL_REVIEW_2026-10-24.md records the original findings and initial deferrals; current correction closure follows below.

Historical M5 correction closure (2026-10-24): all three authorized presentation fixes and the fresh review duplicate-vessel correction are verified and locally integrated. Functional source4a2e430 passes988 simulation tests; Colony665 assertions+4 provenance; Rival1146+12 provenance; Species1194; Expansion275; Legacy466+27 geometry. Fresh main Suite/Colony/normal rebuild/five harness exclusions pass separately. All twelve COL child criteria and M5-I are Verified for local automated/design scope. Parent REQ-017 and17-row totals remain12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred. See PROJECT_STATUS.md and reports/COLONY_POLISH_INTEGRATION_2026-10-24.md. Human/platform/delivery/release limits remain open. Next is bounded milestone selection/specification; no automatic implementation or cleanup.

Current M6 closure: full design and separate plan were approved; implementation, independent review, correction and owner-authorized local integration are complete within the verified automated scope. Earlier drafting-only checkpoints remain historical. Select and specify the next bounded milestone before further feature implementation.

Current M7 entry2026-10-25: the owner selected bounded scout survival/encounter-avoidance drafting under [ADR-015](DECISIONS/ADR-015-scout-survival-design-entry.md). [Complete proposed gameplay rules](superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md) precede owner review. M7-D In Progress; M7-P/M7-I Not Started. Next action is review/amendment and full design approval, then a separate implementation plan. Retain Native preference after those gates. No new implementation task sequence, product/default/writer change or primary integration is authorized here; M6/parent statuses remain unchanged.

M7 scope amendment: owner selected space-ameba danger. Review the amended SCT-007 habitat/warning/survival/study/loss/streams contract and new SCT-AC-013..015 before a separate plan. The creature choice supersedes peaceful-only scope; no numeric gameplay/plan/implementation approval is inferred. Retain peaceful rivals, Native after gates, main/owner-edit preservation and current product versions.

Current M7 approval2026-10-25: [ADR-016](DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md) accepts the full amended gameplay design, M7-D/SCT-AC-012 Verified for design only. Review the [separate15-task implementation plan](superpowers/plans/2026-10-25-scout-survival-and-ameba.md) and [author self-review](../reports/SCOUT_IMPLEMENTATION_PLAN_REVIEW_2026-10-25.md). M7-P In Progress awaiting confirmation; M7-I Not Started. Native inline/one final fresh reviewer/no per-task agents retained. Task1 genuine rules6 future capture precedes any product/default/writer change; only source inventory/reference arithmetic/document checks ran during planning. No primary integration/push/release/cleanup authorization.

<!-- M7 current acceptance -->
## Current M7 implementation and acceptance — 2026-10-25

Owner approved the full amended ADR-016 design and separate15-task plan; earlier drafting/plan-pending statements above are historical. M7-D/M7-P Verified; M7-I **Implemented, Unverified for exact full acceptance; review M1 deferred** on isolated codex/scout-survival. SCT-AC-012 is design-only Verified; functional SCT-AC-001..011/013..015 have the executed criterion mapping in [validation/handoff](../reports/SCOUT_VALIDATION_2026-10-25.md), with review status: Complete; no open Critical or Important findings. Branch current rules7/writer8/29 pairs; primary main42e1282 remains rules6/writer7. Actual suite 1398; all7 fresh native scenarios, zero diagnostics and ordinary7/helper2 exclusions are recorded, with genuine older futures and generated/labelled provenance. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. No integration/push/cleanup; owner chooses next disposition.

<!-- Scout M1 closure2026-10-26 -->
## Scout M1 follow-up closure — 2026-10-26

The remaining omitted-habitat setup explanation is corrected on codex/scout-polish, validated source907207f, with actual native RED→GREEN and five custom-map boundaries. [Follow-up evidence](../reports/SCOUT_POLISH_2026-10-26.md): Scout237, full Suite1398/1398,0 diagnostics, ordinary build0 warnings/errors and actual7-scenario/2-helper exclusions. SCT-AC-013/M7-I are now Verified for their local automated scope; earlier M1 deferral statements are historical. No known review finding remains open. Other six native scenarios retain their prior-source evidence; no second independent review or fresh other-six run is claimed. Primary main42e1282/four owner edits/seven worktrees/old evidence remain preserved; no integration/push/release/cleanup. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. Next is owner disposition of the completed follow-up branch.


## Scout local main integration — 2026-10-27

The owner's “merge that” authorized local integration of the completed scout/space-ameba work and final M1 follow-up. Main now contains validated source `a52dfc61c9a16ec10e0091b52783029afac73e38`: rules7/writer8/29 pairs. [Fresh merged-main evidence](../reports/SCOUT_INTEGRATION_2026-10-27.md) passes1398/1398 simulation tests and all seven native scenarios/4491 records (Scout 237; Legacy 493; Expansion 275; Species 1194; Rival 1158; Colony 670; Crisis 464), with0 diagnostics, normal builds0 warnings/errors and actual7-scenario/2-helper exclusions. SCT functional criteria/M7-I remain Verified for local automated scope; prior pending-integration statements are historical. Checkout preservation is verified425/425; original owner edits remain uncommitted and all branches/seven worktrees/recovery/QA remain retained. No new review, push, release or cleanup. REQ-017/17-row totals and human/platform/security/export/release limits remain unchanged. Next is bounded milestone selection/specification before implementation.


## M8 Fleet Command and named admirals — design review pending

The owner selected centralized fleet/shipbuilding control, named officers/bonuses/service records/simple XP and unlocks, with culture-based manually adjustable loss defaults and possible research. [ADR-017](DECISIONS/ADR-017-fleet-command-and-admirals-design-entry.md) records drafting direction; [FLT-001..009 / FLT-AC-001..012](superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md) propose the complete bounded rules. M8-D/FLT-AC-012 In Progress; M8-P/M8-I and functional criteria Not Started. [Fresh builder report/dispositions and document checks](../reports/FLEET_COMMAND_DESIGN_REVIEW_2026-10-27.md) are documentation evidence only. Exact fleet/request authority, officer XP/bonuses, cultural defaults, Wenth embodiment, recovery/research and future rules8/writer9 await full gameplay approval before a separate plan. Current product/main remains rules7/writer8/29 pairs; parent REQ-017/17-row counts, M7/M6 acceptance and human/platform/release limits are unchanged. Original owner files and all seven worktrees/branches/QA/recovery remain preserved. No implementation, fresh C# gameplay validation or primary integration/push/release/cleanup is claimed.


M8 scoped decision2026-10-27: the owner confirms doctrine editable from the start and research improving recovery equipment (ADR-017/FLT-006). No research gate for doctrine changes remains open. Exact equipment/bonus/XP/culture values and the rest of the full M8 design remain pending gameplay review; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Documentation only; current main/product and owner edits remain unchanged.


M8 scoped owner choice2026-10-27: Navigator, Surveyor and Organizer; promotions at10/30 XP. The old Recovery specialist, ameba-only Surveyor proposal and20/60 thresholds are superseded in the active specification; historical review/check artifacts remain intact. Exact bonus magnitudes/event XP/proposed XP cap, Organizer accounting, recovery values/cultural defaults and full M8 acceptance remain pending review. Doctrine remains editable from the start; research improves recovery equipment. M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. No code/main/owner-file change.


M8 scoped owner decision2026-10-27:50% officer recovery,70% with recovery research and a two-turn successful-recovery delay are confirmed. Current review topic is cultural defaults; do not treat provisional Harl Last watch as approved. Other unreviewed rules/values and full M8 acceptance remain pending; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Main and four owner files remain unchanged.


M8 cultural-default decision2026-10-27: all four cultures begin recovery-focused; Wenth present Re-establish command link. Last watch is manual-only; the original Harl default is superseded. Do not reopen settled doctrine/research, specialties/10-30 XP,50-70% recovery/two-turn delay or cultural defaults. Use the specification's remaining-review checklist for unconfirmed details. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees/evidence remain preserved.


M8 bonus/XP decision2026-10-27: Navigator5/10%, Surveyor1/2 research per first survey, Organizer5/10% scoped request-ship cost reduction and2/1/8 XP awards are owner-confirmed; do not re-ask. Current review is fleet/construction operations, with other unconfirmed details/full design approval still pending. Preserve docs/design-notes/2026-10-27-combat-retreat-morale-and-damaged-ships.md as future owner direction (degrading capabilities, retreat/morale, limping home/detachment/hit-and-run, automation with possible manual control), not M8 damage/combat scope. Plan/implementation remain Not Started; main/four owner files/worktrees/evidence unchanged.


M8 fleet/construction review2026-10-27: owner retains the reviewed proposal and Normal/Urgent request priority. Normal preserves explicit queues; Urgent previews moving before unstarted work, never silently replacing the active project. Preserve finite authority, priority-aware estimates, tokens/reserve/manual control and current combat-note future-only scope. Next review: commissioning/Wenth details/recovery technology and other unconfirmed details, then full M8 approval before separate planning. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees unchanged.
