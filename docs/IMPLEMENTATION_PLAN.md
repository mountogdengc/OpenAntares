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
| M7-P | Plan scout/ameba implementation | Approved M7-D; retained Native inline | Separate15-task plan/self-review/owner review | In Progress; plan review pending |
| M7-I | Implement approved scout/ameba slice | Future approved M7-P, genuine rules6 capture first | Functional SCT criteria, retained gates, one final reviewer | Not Started |
| M-Later | Remaining strategic systems; REQ-017 | Focused approved subsystem specifications; no inferred AI/combat/diplomacy scope | AC-017 then subsystem criteria | Deferred |
| M-Delivery | Target-platform and release validation; REQ-013 | Define platform/package/performance/support policy, review Release mapping, add export/dependency attribution/CI as approved | AC-013 plus clean install/export/startup/save checks, dependency audit | Not Started as a delivery milestone; local code exists |

M1-I is now technically integrated under explicit owner authorization, with fresh combined regression evidence. This does not close M1 human acceptance. The clean detached spiral worktree and remote feature branch have no commits outside `main`; no feature merge is required from them.

## Next concrete action

Review/confirm the separate [15-task scout/ameba implementation plan](superpowers/plans/2026-10-25-scout-survival-and-ameba.md), after full design approval in [ADR-016](DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md). M7-D/SCT-AC-012 Verified for design, M7-P In Progress, M7-I Not Started. Native inline/one final reviewer remains retained after the plan gate. Task1 genuine rules6 future capture precedes every product edit. Preserve all M6 implementation/integration evidence, main and four owner edits; human/delivery limits remain open.

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
