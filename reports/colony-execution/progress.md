# SDD ledger — plan: docs/superpowers/plans/2026-10-24-colony-development.md

Owner approved design, separate plan and retained Native execution. Baseline e77b598; product main10cb0b4. Fresh preservation audit: exact four owner hashes, empty primary index, seven retained worktrees.

## Pre-flight shared interfaces

| Producer → consumer | Contract | Result |
| --- | --- | --- |
| 1 → 6,11,12 | Genuine rules4/content/four future states | Capture before product changes; freeze thereafter. |
| 2 → 3,4,5,6,7 | Version5 models/clones/validation, Current4 | Consistent nullable old state, required new state. |
| 3 → 4,5,6,8,9 | Snapshot assignment, tokens, manual pauses | Consistent command API and independent channels. |
| 4 → 5,7,8,9 | Scoped projections/policy/forecast | Task4 publishes projections before Ready consumes them. |
| 5 → 6,9,11,12 | Atomic Ready/pending and completed journals | Same planning-turn semantics and rollback. |
| 6 → 8,11,12 | Writer6/16 pairs/presets | Current remains4 until11. |
| 7 → 10,11,12 | Descriptor/hash/observed projections | No RNG/IDs; stale privacy preserved. |
| 8 → 9,10,11,12 | Colony harness/template/queue UI | Shared command callbacks and opt-in harness. |
| 9 → 11,12 | Dashboard/reports/local preferences | Routine only; no crisis framework. |
| 10 → 11,12 | Portrait cache/gallery |128 bounded; no simulation dependency. |
| 11 → 12 | Rules5/current content/all retained gates | Frozen legacy3/4 content remains explicit. |

## Tasks
- [ ] Task 1
- [ ] Task 2
- [ ] Task 3
- [ ] Task 4
- [ ] Task 5
- [ ] Task 6
- [ ] Task 7
- [ ] Task 8
- [ ] Task 9
- [ ] Task 10
- [ ] Task 11
- [ ] Task 12
`nBaseline: ordinary restore exit0; Build exit0/0 warnings/errors; Suite648 passed/0 failed/skipped. Logs in reports/colony-execution. Product source unchanged.
`nTask1: missing-fixture RED observed (1 failure); exact source hash verified; unchanged writer5/rules4 captured four ordinary Ready states14→18. Suite649/649 GREEN; no src/game/content/art diff. Primary four exact hashes verified at execution entry.
