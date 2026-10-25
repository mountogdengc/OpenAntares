# Colony-development validation — 2026-10-24

All twelve approved Native implementation tasks are implemented. The five-scenario gate passes at functional/harness source `47eb9cbe2e3e48d1361087b7bbac63114e1afbb4`. This is the pre-review gate checkpoint. Final independent review and its blocking correction are now resolved; see the disposition below. Local main integration is next. [Plan](../docs/superpowers/plans/2026-10-24-colony-development.md), [approved specification](../docs/superpowers/specs/2026-10-24-colony-development-design.md), [execution ledger](colony-execution/progress.md).

## Implemented behavior

Rules5 new games have paid foundations and three specialist buildings/technologies, four editable governor templates, immutable assignment snapshots, independent opt-in workforce/construction automation, finite mixed construction queues, explicit project replacement, optional new-colony inheritance, scalable own-colony management and ten-turn action history. Manual orders retain authority. Default colonies start manual; Balanced is only a visible suggestion. No governor chooses ships, research, movement or founding independently. Original procedural planet portraits vary structurally and retain private last-observed appearance.

Writer6 supports exactly16 schema/rules pairs: (1,1), (2,1), (3,1), (3,2), (4,1), (4,2), (4,3), (5,1), (5,2), (5,3), (5,4), (6,1), (6,2), (6,3), (6,4), (6,5). Genuine old campaigns keep embedded content and behavior; no implicit upgrade. No new dependency or third-party art. Source is MIT; original portrait catalogue data is CC0, as recorded in LICENSING.md.

## Fresh gate evidence

[Machine summary](colony-final-20261024-run1/summary.json) derives counts from actual result rows, separating provenance/geometry from assertions. Each result's `passed` is true, `error` is null, every row passes, all retained QA paths exist and engine diagnostics are empty.

| Check | Assertions | Additional rows | Retained QA paths | PNGs |
| --- | ---: | --- | ---: | ---: |
| Simulation | 982 tests | 0 failures/skips | — | — |
| Colony | 582 | 4 generated provenance | 26 | 55 |
| Rival | 1146 | 12 generated provenance | 64 | 22 |
| Species | 1194 | — | 74 | 29 |
| Expansion | 275 | — | 20 | 10 |
| Legacy | 466 | 27 geometry | 11 | 16 |

Windows Native execution, .NET8, Godot4.7.2 .NET console, graphical display. Commands: `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers`; sequential `./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <retained console executable> -Scenario Colony|Rival|Species|Expansion|Legacy -OutputDirectory reports/colony-final-20261024-run1/<scenario>`; ordinary `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild`; `Verify-NormalAssembly.ps1` against the ordinary assembly. Every process exits0. Normal build has0 warnings/errors. Each runner first demonstrates metadata rejection of the real opt-in assembly, then restores ordinary code in finally; all five harness types are absent. Raw command/build/engine/metadata logs are adjacent to results. No fresh dependency-security scan is implied by --no-restore.

The four unchanged seed42 Small campaigns start through the actual current form with a Peldra rival. All complete three foundations and the required specialist: Harl/Industrial Production Complex turn24, Peldra/Research Discovery Center turn10, Sulan/Growth Habitat Network turn15, Wenth/Balanced Production Complex turn17. Harl founds an ordinary second colony, retains a paused manual favorite and delegates its child. Wenth demonstrates capacity6→8, blocked immature seed and full8→parent5+seed3. Queued/paused/founding states use actual save/load dialogs. Every ordinary End Turn equals an independent canonical reference, then three future UI turns after reload equal uninterrupted continuation. Injection/reroll count0;160-turn guards are QA bounds, not balance claims. Twelve Rival campaigns meet the unchanged80-turn contract, max14 turns. Two-human pending Ready save/load is explicitly a labelled fixture, because a one-human game resolves immediately.

## Acceptance mapping

Paths below are relative to `tests/OpenAntares.Simulation.Tests/`; runtime groups are in `tests/OpenAntares.Game.Validation/ColonyGraphicalValidation.cs`. All automated functional criteria are implemented with passing evidence; final review remains a separate gate.

| Criterion | Executed evidence |
| --- | --- |
| COL-AC-001 Foundations | ColonyContentTests, current-core/economy cases in ColonyStateTests and GovernorPlanningTests; CurrentStartChecks and all four GeneratedDevelopmentCampaigns complete paid foundations/specialists. |
| COL-AC-002 Templates | ColonyCommandTests, TemplatePresetTests, ColonySaveTests; InitialControls covers defaults/copy/edit/revisions/64 limit/presets/independent switches and snapshot isolation. |
| COL-AC-003 Queues | ColonyCommandTests, GovernorPlanningTests, GovernorTransactionTests.OneCompletionConsumesOnlyBoundQueueEntry and ManualBuildingCompletionRemovesSatisfiedQueueWithoutStartingAnother; InitialControls/BlockedCargo/generated Wenth. |
| COL-AC-004 Workforce | GovernorPlanningTests literal all-species outputs, one/extreme population, zero support, flat output, cargo/capacity and scoped overflow cases; actual four generated policies. |
| COL-AC-005 Transactions | GovernorTransactionTests.NonfinalReadyPublishesOnlyItsOwnOrders, LastHumanFailureRestoresExactPreReadyState, WithdrawAndResubmitDoesNotDuplicateActions, ManualInterventionWinsAfterPreparation, BadAndOverBudgetPreparationPublishNothing, LateValidationFailureRetainsAcceptedPlanningAndExactRandomState, IdOverflowAndOwnOutputOverflowPublishNothing; retained rival transaction cases and actual two-human Ready/withdraw/manual/save/reload group. |
| COL-AC-006 Manage scale | ManagementChecks explicitly labelled1/6/11/30-colony fixtures, atomic bulk/favorite/filter/exception/clearable suggestion, manual starts and no takeover. |
| COL-AC-007 Reports | CompletedReportsArePrivateAndRetainLastTen, command/knowledge cases, PreferenceChecks and ManagementChecks twelve ordinary turns/ten reports/reload; ManualOverrideDigest actual pending journal. |
| COL-AC-008 Appearance | PlanetAppearanceTests five literal hash vectors and48 generation comparisons; PortraitChecks96 type/environment/signature combinations at800/1920, stale/save/cache stress/neutral/modded fallback;128 cache cap, held texture survival. |
| COL-AC-009 Privacy | ColonyKnowledgeTests, GovernorPlanningTests hidden overflow/own scoped forecast; portrait stale/unknown and journal privacy tests; shared projections only for generated human decisions. |
| COL-AC-010 Compatibility | ColonyCompatibilityTests, ColonySaveTests, TemplatePresetTests;16 pairs and strict invalid fields/counters; genuine frozen rules4 continuation captured before changes in Task1, unchanged rules3/2/1/passive-AI expected traces; actual invalid80-error current-content Load recovery and all retained scenes. |
| COL-AC-011 Integrated regression | Four GeneratedDevelopmentCampaigns plus labelled pending Ready/scale/gallery fixtures; complete five-scenario evidence table above, ordinary build and real metadata negative control. |
| COL-AC-012 Design | Verified by owner full approval in ADR-012, independent of runtime results. Separate twelve-task plan approved by subsequent “yes”. |

Task-specific RED/GREEN logs and failed graphical runs remain in [colony-execution](colony-execution/), including literal transaction/governor tests, malformed preference overflow, missing-control checks and current activation. Passing acceptance coverage additions are not described as witnessed RED. Early ledger shorthand586 Colony/1158 Rival rows includes4/12 provenance; the exact assertion counts are582/1146 as above.

## Visual inspection and limits

Task10 inspected all24 gallery PNGs (96 signatures at both800/1920) in task10-ui-run1, plus owned/management views. Final-run representative generated species800/1920,30-colony management and actually scrolled manual override images were inspected; current portraits and distinct surface structures render, controls fit/scroll in process. Earlier template/queue/report captures remain retained. These are automated interaction/layout evidence, not physical OS input, comfortable small-screen reading or owner aesthetic approval.

Parent REQ-017 remains In Progress;17-parent totals stay12 Verified,3 Implemented, Unverified,1 In Progress,1 Deferred. Uncoached comprehension remains Deferred under ADR-005. Balance/enjoyment, readability/accessibility, physical input, Linux/macOS/ARM determinism, clean-machine/export/security and release acceptance remain open. Plagues/revolts/culture/destruction, emergency acknowledgements, fleet/scout doctrine and peaceful mode remain future scope. Preserve all owner changes, seven worktrees, saves, recovery and failed/successful evidence; no push/release/cleanup.

## Final review/correction disposition

[Final review](COLONY_FINAL_REVIEW_2026-10-24.md) records one Important preview finding corrected regression-first at9db9933. Suite984/984; full affected Colony593 assertions+4 provenance,28 QA/57 PNG,0 diagnostics; normal build0 warnings/errors/five exclusions. [Correction summary](colony-correction-20261024-run1/summary.json). Both individual/bulk replacement confirmations now match next Ready with immutable source/queue/reserve preservation. No re-review. Three Minors remain deferred: historical condition wording, affected-colony deletion details, routine Attention distinction. COL-AC-002/007 and M5-I remain Implemented, Unverified for these exact details; other functional COL criteria are Verified for bounded local automated scope, COL-AC-012 remains Verified design. Fresh combined main gates follow authorized integration; parent counts/human/release limits unchanged.
