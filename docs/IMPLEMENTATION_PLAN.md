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
| M1-I | Establish an integrated accepted baseline | M1 evidence; inspect all current diffs and untracked evidence, separate UI/test evidence from unrelated species/research edits, review intended commits/branch | Build/tests and relevant graphical regression on combined intended result; record final SHA and remaining edits | Not Started; no merge/reset/commit during adoption |
| M2-D | Reviewed exploration/travel/second-colony design; REQ-015 | Design entry authorized by ADR-005; knowledge/visibility, movement/founding/timing, contrasting examples, legacy policy, multi-colony UI | EXP-AC criteria and builder check complete; owner approved full specification in ADR-006 | Verified for design scope only |
| M2-P | Review the separate expansion implementation plan | Approved M2-D; nine dependency-ordered tasks, exact interfaces/fixtures, regression and runtime evidence | Owner approved plan and Native execution | Verified |
| M2-I | Implement the reviewed expansion milestone | M2-P handoff; isolated checkout/preserved baseline, state/content/commands, persistence compatibility, simulation, UI | New exact-result/failure tests, legacy continuation, graphical multi-colony scenarios | Implemented, Unverified; final independent review pending |
| M3 | Species mechanics and remaining strategic systems; REQ-016/017 | Focused approved specs; preserve accepted creative roster and charter scope | AC-016/017 planning gates, then system-specific tests | Deferred |
| M-Delivery | Target-platform and release validation; REQ-013 | Define platform/package/performance/support policy, review Release mapping, add export/dependency attribution/CI as approved | AC-013 plus clean install/export/startup/save checks, dependency audit | Not Started as a delivery milestone; local code exists |

M1-I can be prepared alongside validation, but do not call it an accepted baseline until the intended changes and evidence are reviewed. The clean detached spiral worktree and remote feature branch have no commits outside `main`; no feature merge is required from them.

## Next concrete action

Finish the final independent branch review of the implemented expansion and its [executed evidence](../reports/EXPANSION_VALIDATION_2026-10-22.md), then hand over the playable isolated feature checkout. The owner already approved both design and Native execution. Do not ask again or re-block on the deferred uncoached session. No merge/push/cleanup is implied; reconcile the documented index/integration state deliberately.

## Risks and autonomy

- Automation may rerun existing builds/tests, inspect code, reconcile documentation, and propose scoped defect work. A confirmed future fix should be reproduced, reviewed, and tested within its authorized scope.
- Player comprehension remains unverified and is explicitly deferred. ADR-006 approves this expansion milestone; future material changes beyond that scope still require owner review.
- Preserve the dirty main checkout, its one local commit, species/faction prose, UI fixes, viewport setting, and untracked evidence. Existing worktree cleanup and remote branch deletion are separate owner choices.
- Avoid a rewrite, new engine/framework, dependency upgrade, species balancing, or a combat/AI/network implementation. The approved expansion observation model is the bounded visibility change; do not extend it into strategic sensors or espionage.
- Performance, accessibility, localization, hostile-input limits, licensing inventory and distribution remain explicit later gaps. Do not turn unspecified targets into asserted requirements met.

Use Not Started / In Progress / Implemented, Unverified / Verified / Blocked / Deferred. Update requirement statuses only to match their exact verified contracts, with evidence and dates; distinguish milestone state from code existence.
