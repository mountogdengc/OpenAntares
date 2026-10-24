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

Task1: missing-fixture RED observed (1 failure); exact source hash verified; unchanged writer5/rules4 captured four ordinary Ready states14→18. Suite649/649 GREEN; no src/game/content/art diff. Primary four exact hashes verified at execution entry.
Task 1: complete (commits 31cff50..5a9ae10, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   649, Skipped:     0, Total:   649, Duration: 730 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task2: Ruling: Copy the already declared immutable appearance descriptor in ObservationUpdater now, advancing this tiny hook from Task7 — Task2 requires valid rules5 observed facts and current-observation equality includes the new record field — cost if wrong: adjust one observation assignment before generation activation. Hash generation/projections remain Task7.

Task2: API RED then semantic parser/validator RED observed. Complete34 development tests; Suite683/683 GREEN; Build0 warnings/errors. Current4 and live core JSON untouched; frozen rules4 trace passes.
Task 2: complete (commits 5a9ae10..aaa1e94, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   683, Skipped:     0, Total:   683, Duration: 721 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task3:14 semantic RED cases observed before handlers;21 command tests GREEN. Suite704/704 and normal Build0 warnings/errors. Ordinary manual changes pause only affected human channel; no-op/reject retain original reference/readiness; scheduling remains inactive.
Task 3: complete (commits aaa1e94..9fb9b44, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   704, Skipped:     0, Total:   704, Duration: 767 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task4:13 semantic RED cases observed;22 governor tests GREEN with all literal outputs, population extremes, zero support, private overflow, Sulan habitats and Wenth cargo/capacity. Suite726/726; Build0 warnings/errors. No IDs/RNG consumed.
