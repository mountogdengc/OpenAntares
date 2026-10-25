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
Task 4: complete (commits 9fb9b44..ff70874, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   726, Skipped:     0, Total:   726, Duration: 845 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task5: Ruling: Attach new-colony development after ordinary founding in ColonizationResolver rather than the plan’s listed FoundingRules file — the resolver owns creation, and preview must stay read-only — cost if wrong: relocate one creation hook; same-turn economics remains unchanged.
Task5:7 semantic transaction RED cases;11 new transaction tests and16 retained rival transaction tests GREEN. Suite737/737; Build0 warnings/errors. Late-fault test corrected to overflowing AI production reserve: research earned0 did not overflow. No product workaround.
Task 5: complete (commits ff70874..5200d5c, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   737, Skipped:     0, Total:   737, Duration: 777 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task6:17 missing-format/schema/preset RED failures;79 focused compatibility/persistence cases GREEN before additional all-old-pair injections. Suite875/875; Build0 warnings/errors. Writer6 supports exactly16 pairs; Current remains4. Old expected trace files unchanged; only schema_version normalization used. Existing Expansion/Species writer assertions were advanced to6 alongside SaveGame/Rival assertions; their campaign-rule and continuation assertions remain unchanged.
Task 6: complete (commits 5200d5c..ef0400e, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   875, Skipped:     0, Total:   875, Duration: 1 s - OpenAntares.Simulation.Tests.dll (net8.0))

Task7:57 semantic RED failures observed;57 focused cases GREEN including five hash vectors,48 species-pair/size base-fact comparisons and private previews/stale facts. Suite932/932; Build0 warnings/errors. Current remains4; no live core JSON changes. Current observations still require exact descriptors via ordinary SequenceEqual; stale facts retain validated last-seen descriptors.
Task 7: complete (commits ef0400e..6f28043, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   932, Skipped:     0, Total:   932, Duration: 1 s - OpenAntares.Simulation.Tests.dll (net8.0))

Task8: Actual Load/control RED observed at missing GovernorTemplatePicker. Final graphical run task8-ui-run4:58 passed assertions,4 retained unique QA saves,7 PNGs,0 engine diagnostics; normal rebuild0 warnings/errors. Inspected representative800/1920 template/controls screenshots. Metadata negative control finds all five real opt-in types; ordinary metadata excludes all five. Intermediate coordinate check was corrected from physical-window size to viewport coordinates; non-focusable diagnostic-label focus warning was removed from the harness. All failed evidence retained. Primary/main and exact four hashes rechecked, index empty, seven worktrees retained.
Task 8: complete (commits 6f28043..0dbbfbc, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   932, Skipped:     0, Total:   932, Duration: 983 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task9: Actual missing-management-control RED observed, plus malformed preference version FormatException RED. Replaced unchecked GetInt32 with bounded TryGetInt32; recovery GREEN. Final management run task9-ui-run3:168 passed assertions,10 retained unique QA saves,0 engine diagnostics; normal rebuild0 warnings/errors and five harness exclusions. All1/6/11/30-colony groups, favorite/bulk/filter/suggestion, local Off/derived shortage,12 ordinary turns/10 report history, save/load and pending manual-override detail passed. Representative management800/1920 and actually scrolled history/override PNGs inspected. Suite932/932. Minor singular/plural labels corrected after this capture; next normal build verifies those text-only edits.
Task 9: complete (commits 0dbbfbc..8d1fef4, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   932, Skipped:     0, Total:   932, Duration: 986 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task10: Actual missing-portrait-output RED observed. task10-ui-run1:264 passed assertions,0 engine diagnostics; normal rebuild0 warnings/errors and five harness exclusions. Captured and visually inspected all24 core gallery PNGs (96 labelled combinations ×both sizes), plus owned/management captures; original water/land or dry structural patterns vary beyond tint. Stable save/load and stale privacy, no source state/RNG/ID mutation,180-request cache stress,128 cap, still-bound texture survival after eviction/Clear and generic modded fallback passed. Suite932/932. Human aesthetic/readability acceptance remains unverified.
Task 10: complete (commits 8d1fef4..6064bc6, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   932, Skipped:     0, Total:   932, Duration: 1 s - OpenAntares.Simulation.Tests.dll (net8.0))

Task11: Ruling: Pin the existing explicit rules4 RivalFixtures helper to Task1 genuine frozen content — old economy/policy tests must not silently acquire current additions; current5 core behavior has48 generation cases and all12 actual rival campaigns — cost if wrong: an AI defect unique to new content needs current5 coverage rather than these old fixtures.
Task11:49 current-core/default RED cases and actual current Start RED observed before activation; whitespace-name load validation RED corrected. Suite982/982 GREEN; current5/writer6 exact approved content now live on branch. task11-colony-run1:275 assertions; task11-rival-run1:1158 assertions including12 genuine current5 generated provenance rows within unchanged80-turn bound; both0 diagnostics, normal builds0 warnings/errors and five exclusions. Actual malformed-default80-error recovery loads genuine4. All old expected trace files remain untouched.
Task 11: complete (commits 6064bc6..8afdc3a, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   982, Skipped:     0, Total:   982, Duration: 1 s - OpenAntares.Simulation.Tests.dll (net8.0))

Task12 coverage entry: missing observable end-to-end tests are four unchanged current-form species campaigns completing foundations/specialist, a generated ordinary second colony/manual favorite, generated Wenth6→8 maturity/cargo, actual queued/paused/founding Save/Load, and an actual nonfinal Ready Save/Load in a labelled two-human fixture. These are acceptance coverage additions over implemented behavior; no manufactured RED is claimed if they pass immediately. Retained five-scenario regressions remain independent gates.

Task12 preliminary coverage: task12-colony-run1 passes586 actual assertions on first coverage run, including4 unchanged generated species campaigns and three canonical future turns each; no RED claimed for passing coverage additions. Harl has ordinary founding plus paused favorite/delegated child; Wenth actual6→8 and full8→parent5+seed3 verified; separately labelled two-human actual nonfinal Ready Save/Load passed. All source state decisions use own views/public definitions/shared previews; injection/reroll count0. Normal rebuild0 warnings/errors. Final five-scenario gates and independent review are still pending.

Task12: final five-scenario gate at47eb9cb: Suite982/982; Colony582 assertions+4 generated provenance/26 QA/55 PNG; Rival1146+12 provenance/64 QA/22 PNG; Species1194/74 QA/29 PNG; Expansion275/20 QA/10 PNG; Legacy466+27 geometry/11 QA/16 PNG. Every process exit0, no engine diagnostics, normal rebuild0 warnings/errors, five real opt-in metadata detections followed by ordinary exclusions. QA paths exist; summary derives exact row counts (earlier shorthand586/1158 included provenance). Final representative800/1920 generated/scale/override PNGs inspected; all24 gallery pages inspected in Task10. Four owner hashes/empty primary index/main10cb0b4/seven worktrees reverified. Mapped COL001..011 as Implemented, Unverified pending independent review; parentREQ017/human limits unchanged.
Task 12: complete (commits 8afdc3a..2daf34b, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers → Passed!  - Failed:     0, Passed:   982, Skipped:     0, Total:   982, Duration: 1 s - OpenAntares.Simulation.Tests.dll (net8.0))

Final review: one fresh gpt-6-astra/xhigh read-only reviewer ofe77b598..9487b93, no reviewer subagents. Raw returned findings retained in reports/colony-execution/final-review.md. One Important preview truthfulness finding and three Minors; no Critical. Re-grading: Important stands because the displayed economic tradeoff differs from preparation. Three Minors stand: current derived badge/history still exist, deletion rejects safely, and routine action/completion Attention remains visible as Summary; these are presentation/discovery gaps, not transaction/data loss.
Final: minor (deferred): Planning digest calls preparation-time conditions unresolved even after manual changes (GovernorReportPanel); label historical count explicitly.
Final: minor (deferred): Assigned-template deletion omits affected owned colony IDs/names and separate inheritance blocker; rejection remains atomic.
Final: minor (deferred): Action/completion Attention preference behaves like Summary; give routine records a distinct nonblocking attention treatment.
Final: Ruling: Physical mouse/keyboard/controller/OS focus set aside by reviewer — retain unverified because harness invokes in-process signals — cost if wrong: an input defect needs a physical session/fix.
Final: Ruling: Small-screen readability/accessibility/screen-reader/uncoached comprehension set aside — keep human acceptance open and ADR-005 deferral — cost if wrong: UI rework after human evaluation.
Final: Ruling: Subjective portrait aesthetics/owner visual approval set aside — structural variation is verified, owner taste remains open — cost if wrong: replace/tune original visual styles.
Final: Ruling: General balance/enjoyment/strategy/long pacing set aside — bounded campaigns prove outcomes, not fun or broad balance — cost if wrong: balance/content iteration.
Final: Ruling: Linux/macOS/ARM runtime/cross-machine pixel equality set aside — only Windows executed; descriptor vectors do not prove every renderer — cost if wrong: platform-specific fixes.
Final: Ruling: Hostile-input fuzzing/dependency audit/clean install/export/release set aside — local gates do not establish delivery/security acceptance — cost if wrong: further audit, packaging or hardening work.
Final: Ruling: Future crises/culture/fleet/scout/warfare/diplomacy/peaceful/multiplayer/victory set aside — preserve bounded approved scope and backlog — cost if wrong: a later scoped milestone is needed.
Final: Ruling: Combined-main behavior/owner preservation set aside by reviewer — require fresh integration audit and full combined gates before claiming them — cost if wrong: recover from exact backup/scoped stash and reconcile locally.
Final: Ruling: Retain this plan scratch directory, branches/worktrees, all failed/successful QA and recovery despite skill cleanup — explicit owner retention takes precedence — cost if wrong: additional local disk use.
Final correction: witnessed two semantic workforce RED cases and actual combined individual/bulk UI RED (3/1/2 Daughter Seed versus actual2/1/3 Discovery Center). Added optional preview replacement input without behavior first to expose semantic failures; then immutable candidate clearing preserves queue/reserve and both UIs distinguish immediate Apply from next Ready. Initial test diagnostic-property compile failure retained separately, not counted as semantic RED. Focused2/2 and Suite984/984 GREEN; complete affected Colony gate pending. No re-review.

Final: fixed misleading replacement forecasts — ReplacementPreviewMatchesAssignmentAndNextReadyWithoutMutatingSource2 cases and actual individual/bulk controls witnessed RED→GREEN; Suite984/984. Full affected Colony gate593 assertions+4 provenance/28 QA/57 PNG,0 diagnostics; normal rebuild0 warnings/errors and all five exclusions. Corrected individual/bulk PNGs inspected. Preserves queue/reserve/source bytes and matches next nonfinal Ready; no re-review.
