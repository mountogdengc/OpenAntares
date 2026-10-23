# Exploration and Colonization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (Native, recommended here) or superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. The owner approved this plan and Native execution on 2026-10-22; execute inline and perform a final independent review.

**Goal:** Deliver the approved scout -> discover -> build/embark settler -> travel -> found -> manage two colonies -> save/reload loop, while keeping legacy campaigns playable.

**Architecture:** Extend the existing plain .NET state/command/turn pipeline with vessel state, per-empire knowledge and typed production targets. Expose immutable permitted views to the existing C# Godot presentation; preserve the economy calculator as the single source for forecasts and resolved quantities. This is one coupled vertical slice, with dependency-ordered testable tasks rather than independently shippable subsystems.

**Tech Stack:** Existing C#, net8.0, Godot.NET.Sdk 4.7.2 / GL Compatibility, JSON, xUnit; no new dependencies.

**Spec:** [Approved exploration and colonization design](../specs/2026-10-22-exploration-and-colonization-design.md), approved in [ADR-006](../../DECISIONS/ADR-006-approved-expansion-milestone.md). ADR-005's human-playtest deferral remains in force.

**Status:** Owner-approved for Native execution, 2026-10-22. Setup baseline passed: normal restore, build with zero warnings/errors, 195 simulation tests. Tasks 1–9 are complete; current fresh results are 281 simulation tests, 275 expansion and 466 legacy runtime assertions. One independent review and regression-first correction pass is complete; [review evidence](../../../reports/EXPANSION_REVIEW_2026-10-23.md).

## Global Constraints

- Preserve C#/Godot presentation and headless simulation, JSON content, stable IDs, integer/PCG32 rules, shared command validation, snapshot economy, lossless reserves, and save failure atomicity.
- Starting colony remains population 6, workforce 2/2/2 on `verdant_world`; existing support/growth/building/research values are unchanged.
- Scout cost 800, speed 300, cargo 0; settler cost 2400, speed 200, cargo 2; embark requires snapshot population >= 4 and >= 2 support workers. New expansion games get one scout, no settler.
- Transfer two existing support workers only on eligible settler completion. One production completion per colony/turn; no silent loss when stalled, cancelled or switched.
- Direct travel is committed until arrival. No regional travel modifier, random exploration reward, fuel, combat, AI, diplomacy, species mechanics or campaign-upgrade tool.
- New campaigns use schema 3/rules 2. Read (schema,rules) = (1,1), (2,1), (3,1), (3,2) explicitly; reject every other combination. Old games keep original embedded content/layout and old-loop behavior.
- Full First Playable acceptance stays deferred/unverified; do not recruit a participant or rerun tests to bypass the owner's deferral.
- Preserve primary-checkout changes and evidence. No resetting, stashing, automatic merging, pushing or worktree cleanup. Commit steps below apply only to reviewed task changes in the later authorized implementation checkout.

## Review Focus

1. A real pre-change schema-2 save, plus the schema-1 fixture, must continue after resaving without injected scouts/content or regenerated layout (Task 2).
2. The readiness command temporarily makes all humans ready; input validation must permit that unpublished resolving state while saves/published states still reject it (Tasks 1, 4).
3. A birth cannot satisfy snapshot embark eligibility; at-capacity growth and stalled embarkation must conserve population/cargo/progress/reserves (Task 5).
4. Changing hidden planets, colonies or units must not alter an empire's visible counts, tooltips, action availability or unknown-target rejection (Tasks 3, 6, 7).
5. Two colonies in one system, saved transit and saved founding intent must keep exact IDs/selection/continuation; overflow or conflicting founding must not consume the wrong settler (Tasks 6-8).

## Execution setup and baseline preservation

- [x] Read current status/Git diff/worktree inventory. Reuse no worktree merely because it exists: the detached spiral checkout is an ancestor, not this dirty baseline. Use the worktree skill/native tooling at execution time to establish an isolated feature checkout from current local HEAD.
- [x] Bring the intended verified baseline into that checkout using a reviewed file manifest: the existing `ColonyPanel.cs`/`GameScreen.cs` fixes, `project.godot`, retained graphical harness and approved workflow/spec/plan documents. Preserve their original bytes/provenance; leave unrelated species/faction edits and every primary-checkout file untouched. Include linked evidence needed by the copied handoff. Record any baseline-only commit separately from new implementation work; never stage an entire dirty directory blindly.
- [x] Before changing serialization, copy the `*-small-pre.json` QA save selected from `reports/first-playable-2026-10-22-run4/results.json` into `tests/OpenAntares.Simulation.Tests/Fixtures/legacy-v2-save.json`. The source was verified during planning as schema 2/rules 1, turn 2, SHA-256 `8E8AE7D8D83D352BA3B8B62B675E633FE23884E569ED500713F3B62E8893DDDA`. Verify that hash and copy without overwriting a different fixture. If unavailable, capture with the untouched baseline writer before proceeding; do not fabricate an old save using the new writer.
- [x] Establish fresh baseline build/test results and, if source/configuration differs from the verified snapshot, relevant graphical regressions. Commands: `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers`; `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo`. Restore normally only if needed. Record actual counts, not the historical 195/463 as fresh results.

## Contract choices for the implementer

These are bounded implementation details of the approved design, not new gameplay approvals. Types listed here are produced in the numbered tasks; do not refer to a future type before its owning task.

| Contract | Exact convention |
| --- | --- |
| Rules | `Rules.LegacyVersion = 1`, `Rules.ExpansionVersion = 2`, `Rules.IsSupported(int version)`. Keep `CurrentVersion = 1` and product new-game default legacy until Task 7; then set current/default to 2. Keep hand-built `GameState.RulesVersion` default explicitly legacy 1. Expansion tests request 2 explicitly |
| Content | Preserve existing `ContentSet` positional constructor; add init properties `Vessels: ImmutableSortedDictionary<string,VesselDefinition>` (empty by default) and `Expansion: ExpansionDefinition?`. `VesselDefinition(string Id, string Name, long Cost, int Speed, int EmbarkPopulation, int MinimumSourcePopulation, bool CanFoundColony)`; `ExpansionDefinition(string StartingVesselId)`. `PlanetTypeDefinition.Colonizable` defaults false for omitted legacy data |
| Production | `ProductionTarget(string Kind, string ContentId)` and `ProductionKinds.Building/Vessel` string constants. Retain `ColonyState.ProjectId`; add `ProjectKind`, default/reset `building`. `null` project means no target. Retain `SetProjectCommand` as a building-only compatibility alias; add a general command in Task 5 |
| Vessels | `VesselId` follows existing typed-ID pattern. `VesselState`: ID, owner, role ID, `StarId? StationaryStarId`, `TransitState? Transit`, `StarId? PendingMove`, `PlanetId? PendingFounding`, `int CarriedPopulation`; clone all state. `TransitState`: origin, destination, total turns, remaining turns. Stationary XOR transit; pending orders mutually exclusive |
| Observations | `EmpireState.Surveys: List<SystemObservation>`; each has StarId, LastObservedTurn and sorted PlanetObservations containing PlanetId, Orbit, PlanetTypeId, nullable ColonyId/OwnerId. Preserve empty-known versus unknown via surveyed membership; current observation is derived from owned colonies/stationary vessels |
| Input-state validation | Extend `GameStateValidator.Validate(ContentSet, GameState, bool allowAllHumansReady = false)`; the flag relaxes only the published all-ready invariant during `TurnResolver.Resolve`, never content/ID/workforce/knowledge checks |
| Planning order replacement | Non-null move/founding replaces the other unstarted order only after the new order validates. Clearing a null move/founding clears only that kind; it does not cancel the other kind. In-transit commands reject even when the requested target equals the active destination |

## Task 1: Content, versioned state, IDs and fixtures

**Files:** Modify `Content/ContentSet.cs`, `ContentLoader.cs`, `ContentWriter.cs`, `Rules.cs`, `Ids.cs`, `State/GameState.cs`, `State/GameStateValidator.cs` under `src/OpenAntares.Simulation/`; create `State/VesselState.cs`, `State/KnowledgeState.cs`, `State/ProductionTarget.cs`; create `content/core/expansion.json`; update `content/core/planet_types.json`. Test: new `ExpansionContentTests.cs`, `ExpansionStateTests.cs`, `ExpansionFixtures.cs` and existing content/state/generation tests under `tests/OpenAntares.Simulation.Tests/`.

**Interfaces:** Produce the contracts above; `GameState.FindVessel(VesselId)`; `NewGameSettings(string GalaxySizeId, ulong Seed, int RulesVersion = Rules.CurrentVersion)` (generation stays legacy by default until Task 7). `ExpansionFixtures.Content()` returns base fixture plus exact expansion definitions; `ExpansionFixtures.AtHome()` returns a valid rules-2 state: empire 1, home planet 2/colony 3/star 4 at (0,0), scout 5, remote star 6 at (300,0), forge planet 7, NextEntityId 8, home surveyed at turn 0. Keep existing `Fixtures.StartingState()`/`PrototypeContent()` as legacy fixtures.

- [x] **RED:** Write named tests with these assertions; compile/run and confirm they fail for missing expansion members or behavior, not a broken test setup:

```csharp
// ExpansionContentTests.CoreDefinitionsMatchApprovedFixtures
Assert.Equal((800L, 300, 0), (scout.Cost, scout.Speed, scout.EmbarkPopulation));
Assert.Equal((2400L, 200, 2, 4), (settler.Cost, settler.Speed,
    settler.EmbarkPopulation, settler.MinimumSourcePopulation));
Assert.False(scout.CanFoundColony); Assert.True(settler.CanFoundColony);
```

Also test missing/partial expansion sections, unknown starting role, zero/negative speed/cost, invalid embark/minimum-population relationships, nonpositive-support colonizable worlds, duplicate role IDs, unsupported rules, ID collisions, invalid transit/order/cargo, deep clone isolation and published all-ready rejection. Names/locations remain in loader errors. Expansion-only aggregate output bounds use the maximum legal planet/colony count and Int128; do not newly reject old rules-1 content on expansion bounds.
- [x] **GREEN:** Implement schema sections `vessels` and singleton `expansion` together, optional when both absent. JSON role fields use snake_case of the definition fields; `expansion.starting_vessel` is `scout`. Set the four current planet types `colonizable: true`. Preserve existing content values and legacy empty-extension round trips. In legacy-focused galaxy tests, pass `Rules.LegacyVersion` explicitly rather than deleting their assertions.
- [x] **Verify:** `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --filter "FullyQualifiedName~ExpansionContentTests|FullyQualifiedName~ExpansionStateTests|FullyQualifiedName~ContentLoaderTests|FullyQualifiedName~GameStateValidatorTests" --nologo`; expected all selected tests pass. Run full simulation suite before committing.
- [x] **Checkpoint:** Review and commit only this task's changes plus its genuine legacy-v2 fixture provenance; no generated build output or unrelated edits.

## Task 2: Explicit schema/rules compatibility and extension persistence

**Files:** Modify `Saves/SaveGame.cs` and `Content/ContentWriter.cs`; create `Saves/ExpansionSaveData.cs` for focused extension read/write helpers if needed. Tests: new `ExpansionSaveTests.cs`, updated `SaveGameTests.cs`, both `Fixtures/legacy-v1-save.json` and captured `legacy-v2-save.json`.

**Interfaces:** Existing `SaveGame.Serialize/Deserialize/ReadFile/WriteFile` and `SaveLoadResult` stay public entry points. `SaveGame.SchemaVersion = 3`. Rules version determines legacy/expansion mode without a second mutable mode flag. New schema-3 fields represent ProjectKind, vessels/orders/transits and surveys; require expansion fields for rules 2, empty/absent extension state for rules 1. Legacy readers supply building target kind and empty unit/survey state, never core expansion content.

- [x] **RED:** Parameterize `ReadsOnlySupportedSchemaRulesPairs`: (1,1), (2,1), (3,1), (3,2) pass; (1,2), (2,2), (3,0), (3,3), unknown schemas fail. `ActualLegacyFixturesContinueAfterResave` loads both old files, preserves coordinates/regions/content/RNG/workforce/reserves/targets, writes schema 3 with rules 1, reloads, and compares deterministic continuation for multiple turns. Adapt the old hard-coded schema-2 writer assertion to approved schema 3 while retaining its original legacy assertions.
- [x] **GREEN:** Serialize all extension data in defined order; validate through existing content/state boundaries. No lookup of current disk core definitions while loading. Keep temporary-write replacement and failed-load session atomicity unchanged. Reader errors identify invalid IDs, metadata and unsupported combinations.
- [x] **Verify:** Run `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --filter "FullyQualifiedName~Save" --nologo`; expect all save tests pass, including transit/pending orders/survey round trips, duplicate/foreign references, malformed data, failed write and failed session load.
- [x] **Checkpoint:** Commit the versioned save path and tests as one reviewed unit; no automatic upgrade-copy UI.

## Task 3: Knowledge projection and explicit expansion new games

**Files:** Create `Knowledge/ObservationUpdater.cs`, `Knowledge/EmpireView.cs`, `Knowledge/EmpireViewProjector.cs`; modify `Galaxy/GalaxyGenerator.cs`, `GameSession.cs`, state validation. Tests: new `KnowledgeTests.cs`, `ExpansionGenerationTests.cs`; existing generator tests keep equivalent old-layout coverage.

**Interfaces:** `ObservationUpdater.Refresh(GameState candidate, int observedTurn)` updates only current observations from final own colonies/stationary vessels; `EmpireViewProjector.Project(ContentSet content, GameState state, EmpireId viewer) : EmpireView`; `GameSession.View(EmpireId viewer) : EmpireView`. Produce immutable copied DTOs named `GalaxyMapView` (charted IDs/names/coordinates/regions, saved visual framing/seed, own home markers), `StarView` (chart record, nullable known planet array, observation turn/current flag), `KnownPlanetView` (known ID/orbit/type/rates/colonizable and observed colony/owner IDs only), `OwnedColonyView` (exact IDs, population/workforce/targets/reserves/buildings, own ColonyEconomy), `OwnedVesselView` (own station/transit/orders/cargo and role), `OwnResearchView` (own known/selected research, reserve and EmpireEconomy), and `ForeignVesselView` (currently observable stationary ID/owner/role/star only). `EmpireView` exposes Turn, Viewer, RulesVersion, Galaxy and immutable arrays of those records plus Research. Missing viewer rejects consistently; views contain no mutable authoritative objects or unfiltered empire totals. Report DTOs are added only after report events exist.

- [x] **RED:** `ExpansionGenerationStartsOneScoutAndHomeSurvey` explicitly requests rules 2 at every supported size; asserts original star/planet generation and RNG state equal a same-seed rules-1 generation, only one extra allocated scout at home, only home planets surveyed, no settler. `HiddenWorldChangesDoNotChangeEmpireView` changes unseen planet count/types, colony/owner and foreign vessel data while keeping chart fixed; canonical permitted views remain equal. Add current-to-last-seen colony observations, foreign co-location/no cargo/no orders/no remembered sightings, and legacy all-visible projection tests.
- [x] **GREEN:** Initialize explicit rules-2 new games from complete expansion content without random draws for scout/knowledge. `NewGameSettings(..., RulesVersion: 2)` without expansion content returns a clear new-game error; never silently downgrades. Keep product default rules 1 until Task 7's presentation is ready. Observations are timestamped at the published turn; owned-colony/current vessel presence gives current facts, retained observations give dated facts.
- [x] **Verify:** Run the `KnowledgeTests|ExpansionGenerationTests|GalaxyGeneratorTests` filtered suite; all pass, including no hidden counts/IDs in projections and clone isolation. Run full suite.
- [x] **Checkpoint:** Commit projection/new-game foundations, still without changing default player visibility until the UI task.

## Task 4: Atomic move orders and deterministic transit

**Files:** Create `Movement/TravelCalculator.cs`, `Movement/MovementResolver.cs`, `Turns/ExpansionEvent.cs`; modify `Commands/Command.cs`, `CommandProcessor.cs`, `CommandOutcome.cs`, `Turns/TurnResolver.cs`, `GameStateValidator.cs`. Tests: new `MovementTests.cs`, `ExpansionCommandTests.cs`.

**Interfaces:** `TravelCalculator.Duration(int x1, int y1, int x2, int y2, int speed) : int`; `RoutePreview` exposes public origin/destination, squared distance, integer ceiling distance, speed and turns. `SetMoveOrderCommand(EmpireId Issuer, int ExpectedTurn, VesselId Vessel, StarId? Destination)`. `MovementResolver.Apply(ContentSet, GameState snapshot, GameState candidate, List<ExpansionEvent> events) : void` advances only snapshot vessel IDs. `TurnReport` keeps its existing constructor and adds immutable `ExpansionEvents`, empty by default. Events carry stable kind, owner empire, relevant nullable vessel/star/planet/colony IDs and reason, not localized text; each event is private to its owner. Foreign ownership observations remain in the permitted system view rather than globally broadcast events.

- [x] **RED:** Exact duration tests:

```csharp
[Theory]
[InlineData(300, 0, 300, 1)]
[InlineData(301, 0, 300, 2)]
[InlineData(300, 300, 200, 3)]
public void TravelDuration(int x, int y, int speed, int expected) =>
    Assert.Equal(expected, TravelCalculator.Duration(0, 0, x, y, speed));
```

Add `ReadyCommandAllowsOnlyUnpublishedAllReadyInput`, `OneTurnRouteArrivesAtTPlusOne`, `TwoTurnRouteArrivesAtTPlusTwo`, `SaveDuringTransitMatchesUninterruptedArrival`, no RNG consumption, pending cancel/replace/no-op readiness, AlreadyThere/invalid speed, owner/stale-turn/legacy rejection and in-transit redirect rejection without mutation. Future arithmetic/ID overflow must retain the state and pre-trigger readiness.
- [x] **GREEN:** Calculate duration with checked Int128 integer comparisons/search. Validate owner/turn/mode before target-specific facts. Apply only validated planning changes to clones. `TurnResolver` validates its unpublished all-ready input using the narrowly scoped flag, then existing economy, snapshot-vessel movement, final observation refresh, readiness reset/normal validation/one turn publication. Same-target/no-order cancellation preserves readiness; all in-transit move/founding orders reject `vessel_in_transit`.
- [x] **Verify:** Filter `MovementTests|ExpansionCommandTests|CommandProcessorTests`; expect all pass, including existing multi-human readiness/rollback tests. Run full suite.
- [x] **Checkpoint:** Commit movement and its report/save continuation tests together.

## Task 5: Vessel production, embark forecasts and conserved population

**Files:** Modify command processor, `Economy/EconomyCalculator.cs`, `EconomyResults.cs`, `Turns/TurnResolver.cs`; create `Economy/VesselProduction.cs`. Tests: new `VesselProductionTests.cs`, existing `EconomyTests.cs`/command tests.

**Interfaces:** `SetProductionTargetCommand(EmpireId Issuer, int ExpectedTurn, ColonyId Colony, ProductionTarget? Target)`; old `SetProjectCommand` routes to the same validation as kind `building`. `ReserveOutcome` gains default building `TargetKind` plus nullable `BlockedReasonCode`; `ColonyEconomy` gains nullable `VesselCompletionPlan` and `EmbarkOutcome`. The completion plan has role/cargo/source/star, not an allocated ID. EmbarkOutcome provides actual after-growth-and-transfer population/workforce and next-snapshot support produced/required as simulation quantities; include a warning when reassignment is needed. `EconomyCalculator.Calculate` remains pure and allocates no IDs.

- [x] **RED:** `SettlerCompletionTransfersExistingSupportWorkers`: rules-2 fixture, population 6 / 2/2/2, reserve 2000, settler selected; earned production 400 pays 2400 exactly, creates one cargo-2 settler, ends with population 4/workforce 0/2/2 and retained growth fraction. Next support forecast is 0 produced / 400 required. Assert `sum(colony population)+sum(cargo)` changes only by births. Repeating vessel production is legal and no building ID/effect is added.
- [x] **RED:** `BirthCannotUnlockEmbarkThisTurn`: snapshot population 3, support 2, growth progress 90, enough reserve; birth occurs, but no vessel/cost/worker removal until a later eligible snapshot. Test population 4 with support 1 stalls/banks all output, changing support later completes once, switching/clearing conserves reserve, at-capacity growth remains zero before transfer, and a building/vessel sharing a content ID stays distinct by kind.
- [x] **GREEN:** Select target by kind, preserve original building/research accounting. Compute embark eligibility from snapshot, apply economic/growth results then approved transfers to candidate, allocate new unit IDs in colony-ID order and append completion events. On successful vessel completion clear the active project/reset kind to building, preserving leftover reserve as for existing completions; the player may select that vessel role again. Ignore newly created units in this turn's movement/founding. Stalled production retains its selected vessel project and reports `awaiting_embarkation` without failing the turn or spending cost. Rules-1 vessel targets reject `expansion_unavailable`.
- [x] **Verify:** Filter `VesselProductionTests|EconomyTests|ExpansionCommandTests`; all selected assertions pass, forecast equals applied result, one completion/owner remains, and overflow rolls back cost/cargo/ID changes. Run full suite.
- [x] **Checkpoint:** Commit shared forecast/resolution changes and their exact accounting tests.

## Task 6: Founding orders, conflicts and final observation phase

**Files:** Create `Colonization/ColonizationResolver.cs`; modify command definitions/processor/rejection codes, turn resolver/report, knowledge projection/updater, state validator. Tests: new `ColonizationTests.cs`, extended `ExpansionCommandTests.cs`, save tests.

**Interfaces:** `SetFoundingOrderCommand(EmpireId Issuer, int ExpectedTurn, VesselId Vessel, PlanetId? Planet)`; `ColonizationResolver.Apply(ContentSet, GameState snapshot, GameState candidate, List<ExpansionEvent> events) : void`. Founding checks known planet membership before hidden existence/occupancy, then role/station/eligibility and same-empire reservations (excluding the issuing vessel). A valid non-null order replaces its own pending move atomically; null clears founding only.

- [x] **RED:** `FoundingConsumesOneCargoAndStartsNextSnapshot`: pending eligible settler founds at T+1, is removed, and creates one owned colony with population 2/workforce 2/0/0, zero growth/reserves/buildings/project; no founding-turn output, research or growth. For a forge world the following forecast is support 400/200 and growth 30. A move replacing unstarted founding cancels its reservation without spending anything.
- [x] **RED:** Test same-empire reservation rejection, different-empire conflict ordered by empire then vessel ID (winner consumed, loser/cargo retained, loser order cleared), two worlds in one star, scout/foreign/transit/unsupported/occupied rejection, unknown-valid and nonexistent planet IDs both `target_not_known`, arrival this turn cannot found, and `NextEntityId` overflow rolls back the entire turn/ready command. Save/reload pending founding continues identically.
- [x] **GREEN:** Apply existing economy and production, snapshot movement, snapshot-eligible founding, final observations and one final publication in that order. Handle an ordinary losing founding conflict as a reportable outcome, not a global validation failure. Observation refresh sees final ownership; emit only permitted events to each empire's presentation view.
- [x] **Verify:** Filter `ColonizationTests|ExpansionCommandTests|ExpansionSaveTests|CommandProcessorTests`; all pass. Run full simulation suite and determinism sequences spanning production, transit and settlement.
- [x] **Checkpoint:** Commit founding and conflict/atomicity coverage as one unit.

## Task 7: Permitted Godot views and the playable two-colony interface

**Files:** Modify `game/Main.cs`, `Screens/NewGameScreen.cs`, `Screens/GameScreen.cs`, `Components/GalaxyMap.cs`, `StarPanel.cs`, `ColonyPanel.cs`, `ResearchBar.cs`, `TurnReportDialog.cs`; create `Components/VesselPanel.cs`, `ColonyList.cs`. Reread `game/theme/openantares_theme.tres` immediately before minimal needed type variations; no hard-coded styling or new product scenes. Modify `Rules.CurrentVersion`/new-game default to 2 only when this UI is ready.

**Interfaces:** `GameSession.View(EmpireId)` supplies the immutable owner view. `GalaxyMap.ShowGalaxy(GalaxyMapView view)` takes only public chart/framing/home markers; `StarPanel.ShowStar(ContentSet, StarView)` uses only permitted planets; `ColonyPanel.ShowColony(ContentSet, OwnedColonyView)`/`ResearchBar.ShowEmpire(ContentSet, OwnResearchView)` read own projections/forecasts. Add `EmpireViewProjector.ProjectReport(ContentSet, GameState, TurnReport, EmpireId) : EmpireTurnReportView`, containing FromTurn/ToTurn, only owned colony/research outcomes, permitted names and only events whose owner is the viewer; `TurnReportDialog.ShowReport(ContentSet, EmpireTurnReportView)` never receives raw world state/unfiltered TurnEconomy. `VesselPanel` and `ColonyList` emit selected IDs and Command objects; `GameScreen` owns selected StarId/PlanetId/ColonyId/VesselId presentation state and submits through `GameSession.Execute`. Raw authoritative state never supplies hidden inspector/list/tooltip/report facts.

- [x] **RED:** In the opt-in Godot harness, create focused failing checks for Not surveyed/no planet count, all-star chart with home-only details, disabled controls with permitted reasons, correct second colony when both share a star, and legacy mode banner/no expansion actions. Change hidden fixture truth and assert the visible tree/text/action set is unchanged. Preserve FP-001/002's deferred focus/open behavior when adapting project selectors to typed targets.
- [x] **GREEN:** Build destination previews from known chart plus owned role speed; show integer duration and committed-travel warning. Show production population/support transfer, stalled embark reason and next-turn support warning from simulation output. Add explicit known-planet Found action, exact colony selection, owned unit/pending/remaining turns, current/last-seen labels and relevant report navigation. Legacy saves continue all-visible with the approved banner; new games default rules 2 with one scout. Retain save/load error atomicity and theme/container layout; avoid leaking foreign cargo/orders or hidden totals.
- [x] **Verify:** `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers` and full simulation tests pass. Run opt-in focused view/control scenarios graphically; review 1920x1080 and the existing smaller window sizes. Fix concrete truncation/unreachable controls without changing the approved viewport or redesigning the theme. Any remaining subjective readability concern is recorded, not asserted solved by a numeric test.
- [x] **Checkpoint:** Commit reviewed UI/view integration separately from unrelated source/theme edits.

## Task 8: Retained end-to-end runtime acceptance and legacy regression

**Files:** Add `tests/OpenAntares.Game.Validation/ExpansionGraphicalValidation.cs` and `.tscn`; update `Validation.targets`, `Run-Validation.ps1`, existing `GraphicalValidation.cs` and harness README. Add a `-Scenario Legacy|Expansion` runner parameter, default Legacy; include both validation classes only in opt-in compilation and choose the corresponding test scene. Keep normal rebuild in `finally` and unique QA names/output folders.

**Interfaces:** Existing runner remains usable with its previous arguments. Expansion scene uses the real New Game/controls, owner views and commands; no injected economy outcomes or private authoritative mutations. A labelled synthetic layout may be used separately for known route/time fixtures; generated seeded campaign evidence is recorded separately. Old harness loads/uses rules-1 fixtures for its original all-visible loop; do not force expansion data to become all-visible merely to keep old assertions green.

- [x] **RED:** Define runtime assertions for each EXP-AC-006 action: one home survey, scout route, discovery after actual End Turns, settler construction and source-population warning, station/travel/founding, two independent colony workforce/projects, empire research aggregation, post-founding next-snapshot effect and save/reload continuation. Cover pending transit/founding saves, same-system colony selection, actual default dimensions and live resize. Require both old save fixtures to render/continue with the banner and no expansion controls.
- [x] **GREEN:** Extend the retained harness to dispatch real UI signals/handlers and compare published state/allowed view with predicted quantities. Keep native popup geometry comparisons and supplementary explicitly embedded menu captures distinct. Run both scenarios using a real graphical Godot .NET console executable; save JSON assertions, exact method/limits, logs and PNGs in unique output directories. Preserve failed attempts and all unique QA saves; never overwrite player saves.
- [x] **Verify:** From the execution checkout, run `./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot '<installed Godot .NET console executable>' -Scenario Legacy -OutputDirectory reports/<unique-legacy-run>` and again with `-Scenario Expansion`/a new output directory. Expect runner exit 0, all recorded assertions true, no unexplained engine ERROR/WARNING lines, normal assembly restored. Inspect PNGs as well as results; do not report OS clicking or uncoached comprehension from signal tests.
- [x] **Checkpoint:** Commit repeatable acceptance harness changes and only the intended evidence/fixture documentation.

## Task 9: Combined verification, traceability and handoff

**Files:** Update `PROJECT_STATUS.md`, `docs/SPECIFICATIONS.md`, `docs/ACCEPTANCE_CRITERIA.md`, `docs/ARCHITECTURE.md`, `docs/IMPLEMENTATION_PLAN.md`, relevant existing subsystem specs, README and `reports/VALIDATION_REPORT.md`; add a dated expansion validation report. Record final intended branch/worktree/commit/diff inventory.

- [x] Map EXP-AC-001..008 to named executed tests and runtime artifacts; run the full solution build/test and both relevant graphical scenarios after the last implementation changes. Record actual counts/exit codes, missing physical-input/platform checks, clean-restore/dependency-audit limits and retained QA paths. No guessed test totals.
- [x] Review the combined result against the approved spec and this plan's Review Focus. Confirm no Godot reference in simulation; no float/random/time outcome logic; no legacy content injection or hidden-state UI access; no change to old economy fixtures except approved schema assertions/new-mode setup.
- [x] Only mark REQ-015 functionality Verified when its executed acceptance scope justifies it; record any remaining integration/manual limits explicitly. M2-D design approval does not itself verify gameplay. REQ-014 remains Deferred under ADR-005.
- [x] Review/stage intended task hunks only. Validate any intended integrated result before merging or release; no automatic push/merge/branch/worktree deletion. Keep the original dirty primary checkout and unrelated creative edits intact.
- [x] Leave an actionable status with executed evidence, integration state and next task. If all authorized work is complete, hand over the playable local result and limits rather than expanding into AI/combat/species or package delivery.

## Self-review record

Coverage: EXP-001 -> Tasks 1/3/7/8; EXP-002 -> 1/4/7/8; EXP-003 -> 1/5/7/8; EXP-004 -> 1/6/7/8; EXP-005 -> 1/4/5/6; EXP-006 -> 3/7/8; EXP-007 -> 1/2/8; EXP-AC-008 -> every task's regression plus Task 9. All five Review Focus conditions have named owning tests.

Interfaces were checked in dependency order. Observation/model types precede projection, explicit expansion generation precedes movement tests, move/founding commands use the same owned IDs and order fields, vessel completion plans allocate IDs only in resolution, and the product new-game default changes only with the permitted-view UI. Existing public compatibility entry points remain. Fixtures and intermediate unpublished readiness validation are explicit; no migration/upgrade policy is left to guess.

The preceding planning self-review predates execution. The owner approved Native execution; Tasks 1–9 are now implemented and checked with retained evidence. Final independent whole-branch review is the remaining workflow gate; no merge/push/cleanup is authorized.
