# Civilian Logistics and Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task by task, inline. The owner's retained method is Native, without per-task agents and with one final fresh whole-implementation review. Steps use checkboxes for implementation progress, not approval or acceptance.

**Goal:** Deliver optional civilian provision stocks, paid reusable transports, policy-driven deliveries, natural migration and explicit bulk population transfers in one playable, saveable strategic loop.

**Architecture:** Add a version-gated Logistics subsystem to the existing deterministic simulation. One pure support calculation serves forecasts and resolution; a reservation ledger and one transfer phase govern actual goods/population changes. Godot reads owned published views and issues the same commands used by automation and AI.

**Tech Stack:** Existing C#/.NET8, Godot4.7.2.NET/GL Compatibility, JSON content, xUnit and focused opt-in native validation. No engine, renderer or package upgrade; no GDScript or Docker.

**Spec:** [Supply and migration gameplay design](../specs/2026-11-16-supply-and-migration-design.md), civilian provisions/transport/migration increment only. [ADR-031](../../DECISIONS/ADR-031-approved-civilian-logistics-direction.md) records owner approval of civilian behavior and authority to prepare this separate plan.

**Status,2026-11-16:** Plan ready for owner review, self-reviewed. Civilian behavior approved; concrete plan choices below are proposals until this plan is approved. LOG-P In Progress; LOG-I Not Started. No code, baseline capture, build, native check, rules change or new execution checkout has run for this plan. No commit/merge/push/release/cleanup authority is inferred.

## Global Constraints

- Implement only provisions, civilian transports, policies, routes and natural/manual migration. Development goods, fleet supplies/endurance, interception, new civilian attack permissions and tactical maps are later deliveries; expose no nonfunctional goods/supply projects.
- Preserve the original setting/identity, species mechanics, Wenth body identity, existing settlers/daughter seeds, civilian protections, crisis/inspection semantics and unchanged old campaign behavior.
- Commands, AI and automation use the same authority and permitted knowledge. Integer/fixed-point arithmetic, checked overflow, stable ID ordering, explicit phases and atomic publication are required. No logistics random rolls.
- Provision capacity is2,000 internal support points per colony;100 points per cargo slot; eight slots per transport; two slots per whole migrant. Natural migration uses80% origin/60% destination thresholds, one person per origin per boundary and an initial origin floor4. Explicit bulk floors may be as low as1, never0.
- Local support is consumed first, then existing provision stock for a deficit; refill the chosen reserve from newly produced surplus before existing surplus-growth calculation. Stored provisions do not grant surplus growth. Default stock targets/export/migration are disabled/zero; enabling reserves proposes two boundaries of current demand capped by storage.
- **Unit clarification for plan review:** all content prices are integer hundredths, as in the current code. Normalize the draft's ambiguous “400 production points” transport price to the proposed literal400 internal units, displayed as4 production points. The starter scout costs800 internal units. This is an assistant balance default for review, not an owner-confirmed price; do not multiply400 by100 silently. Later goods prices are outside this increment.
- Propose `Rules.LogisticsVersion=13` and `SaveGame.SchemaVersion=14` only when executing the approved plan. Keep `Rules.CurrentVersion=12` and ordinary setup unchanged; an explicit new-game Supplies and migration option, initially Off, selects13. Old saves never auto-convert. The plan allocates no active version today.
- Preserve all79 existing explicit schema/rules pairs; add exactly `(14,1)` through `(14,13)`, total92. Writer14 must still serialize old rules without logistics fields or transport content. Reject `(schema<=13,rules13)` and unknown future pairs.
- Execute from an isolated checkout containing the approved plan and preserved current product, using the worktree skill at execution time. Baseline primary is feature/planet-globes-modern/e90b6b2 with the session's pending Markdown edits, beyond local main/d994d34. Never substitute HEAD-only older main for that product or discard the pending documents.
- Preserve theme edits, orbital view, globe shaders, renderer/window settings, saves, fixtures, raw evidence, branches/worktrees and historical source scopes. No planet-performance fix is part of logistics.
- One disk-heavy job at a time through `F:/Tools/CodexTestRunner/runner.py exec --project openantares`; actual active source root recorded. Smallest affected checks only, no broad baseline/full suite or campaign matrix without explicit owner request. No desktop control for routine tests.
- No automatic commits despite generic task-workflow advice. Checkpoint diffs/evidence after tasks; commit/integration remain separate owner actions.

## Review Focus

1. Loading after births, illness or another commitment must not spend nonexistent population or invent destination space: Tasks5/6/8 test concurrent reservations and loading-time revalidation.
2. A new civilian role must have a valid zero-attack combat profile and species/build availability, without entering old campaigns or becoming a legal target: Tasks2/4/10 test transport production, movement and protection.
3. The same passenger count must drive old quarantine code and the new manifest: Tasks1/8 keep `VesselState.CarriedPopulation` authoritative and test exposure exactly once.
4. A return leg or generic fleet command must not double-assign a ship or let cargo disappear: Tasks5/7/10 test repeated routes, cancellation, transit and merge/move guards.
5. New version13 must retain every earlier subsystem capability, while writer14 preserves the explicit old compatibility table: Tasks2/11 test capability gates, round trips and genuine frozen old futures.

---

## Source map and shared contracts

Paths below marked New are proposed; all other named paths were inspected. Do not create a second general economy or movement engine.

| Files | Responsibility |
| --- | --- |
| New `src/OpenAntares.Simulation/Logistics/LogisticsState.cs`, `LogisticsStateValidator.cs` | Owned stocks, route/mission/reservation identity and clone/invariant checks |
| New `Logistics/LogisticsContent.cs`, `LogisticsContentReader.cs`, `LogisticsContentWriter.cs`; new `content/core/logistics.json` | One versioned definition, transport role and validated numerical policy limits |
| Existing `Content/ContentSet.cs`, `ContentLoader.cs`, `ContentWriter.cs`, `Rules.cs`, `Species/SpeciesRules.cs` | Core loading, role availability and capability gates; vessel/expansion readers are inside ContentLoader.cs; preserve legacy sections |
| New `Logistics/LogisticsEconomy.cs`; existing `Economy/EconomyCalculator.cs`, `EconomyResults.cs` | Pure stock/support/growth calculation with named breakdowns |
| New `Logistics/LogisticsCommands.cs`, `LogisticsCommandProcessor.cs`, `LogisticsCommandGuards.cs`, `LogisticsPreviews.cs` | Atomic policy/manual mission authority and material preview tokens |
| New `Logistics/LogisticsReservations.cs`, `LogisticsMigration.cs`, `LogisticsPlanner.cs` | Feasible allocations, post-departure support tests and deterministic policy dispatch |
| New `Logistics/LogisticsTransfer.cs`, `LogisticsResolutionContext.cs`, `LogisticsTurnCoordinator.cs`, `LogisticsReports.cs`, `LogisticsViews.cs`, `LogisticsQueries.cs` | One transfer phase, phase receipts, owner views and reports |
| Existing `State/GameState.cs`, `VesselState.cs`, `GameStateValidator.cs`, `Commands/CommandProcessor.cs`, `Turns/TurnResolver.cs` | Attach/clone/validate and integrate the subsystem transaction |
| Existing `Galaxy/GalaxyGenerator.cs`, `Economy/VesselProduction.cs`, `Combat/CombatInitializer.cs`, `ShipDesigns/VesselLoadouts.cs`, `Movement/MovementResolver.cs`, `Crises/CrisisTransport.cs` | Initial state, paid transport commissioning, protected hull profile, movement/health adapters |
| New `Saves/LogisticsSaveData.cs`; existing `Saves/SaveGame.cs` | Explicit ordered content/state serialization and exact schema/rules compatibility |
| Existing `Knowledge/EmpireView.cs`, `EmpireViewProjector.cs`; `Rivals/RivalTurnPlanner.cs` | Published own facts and shared fair preparation; no foreign inventory exposure |
| New `game/Components/LogisticsPanel.cs`, `LogisticsMissionDialog.cs`, `game/Screens/GameScreen.Logistics.cs`; existing `ColonyPanel.cs`, `NewGameScreen.cs`, `GameScreen.cs`, `GameScreen.SelectionConsole.cs`, `GameScreen.FleetPanel.cs`, `game/Presentation/FleetRoster.cs`, theme | Setup, colony stocks/policies, transport/migrant orders, readable exceptions and cargo-aware stacking |
| New `tests/OpenAntares.Simulation.Tests/Logistics*Tests.cs`, `LogisticsFixtures.cs`; new focused capture/test/native workers described below | Focused simulation, compatibility and affected native controls |

`Logistics/` and `Saves/` paths above are relative to `src/OpenAntares.Simulation/`. Tests use namespace `OpenAntares.Simulation.Tests`; product types use `OpenAntares.Simulation.Logistics`. New API declarations below define the boundary names; implementers choose private helpers and serialization DTO detail without changing the specified behavior.

Define value IDs `MissionId(int Value)` and `RouteId(int Value)` using the existing comparable stable-ID pattern and the shared `GameState.AllocateId()`. `GameState.Logistics` is nullable `LogisticsState`; it is null for rules1..12. `LogisticsState` holds sorted colony stock/policy entries, owned routes and live missions. Each material edit increments an owned revision. A route owns a hull; each trip has one mission. No transient per-boundary receipts are saved.

`CargoDraft(long ProvisionPoints,int Passengers)` is a command/preview quantity. `MissionDraft(VesselId Vessel,ColonyId Origin,ColonyId Destination,CargoDraft Cargo,bool Repeat,bool AcknowledgeOriginShortage)` describes one finite load and optional fixed return loop. `LogisticsPolicy(long MinimumStock,long TargetStock,bool ExportEnabled,bool NaturalDeparture,bool AcceptMigrants,int MinimumPopulation,int Priority)` is validated against capacity and species. `RouteDraft(VesselId Vessel,ColonyId Origin,ColonyId Destination,bool CarryProvisions,bool CarryMigrants,int Priority)` opts a fixed owned route into automatic service. Risk permission defaults to refusing known effective blockade; manual known-blockade consent is a separate explicit action flag/token consequence.

`LogisticsPreview(bool Allowed,string? RejectionCode,string Token,ImmutableArray<string> Consequences)` is immutable. `LogisticsEmpireView` publishes immutable own Stocks, Routes, Missions and Reservations plus material Revision; its rows include the exact stock, manifest, provenance, available-capacity and policy facts needed by the pure preview/planner, never foreign state. `ReservationLedger` exposes immutable ExportableProvisions and AvailableMigrants keyed by origin ColonyId, FreePopulationCapacity and FreeStorage keyed by destination ColonyId, and AssignedVessels. `LogisticsManifest` contains ProvisionPoints and optional PassengerOrigin/SpeciesId provenance; its passenger count is read from the owning vessel, never separately stored. Private DTO/helper structure remains an implementer choice.

For a loaded migrant, `VesselState.CarriedPopulation` remains the only authoritative passenger count; new manifest provenance records origin colony and species, not a second mutable passenger total. `VesselState.Crisis` remains the health/carrier record used by existing transport code. Manifest projection combines those facts. Goods quantities live solely in logistics manifests; colony stock is debited only at loading.

## Verification commands used by tasks

Task1 creates `tools/Logistics/Run-LogisticsTests.py`, a focused worker (not a new scheduler). From the actual execution checkout set these task-specific PowerShell variables once:

```powershell
$logisticsSource = (Get-Location).Path
$logisticsQueue = 'F:/Tools/CodexTestRunner/runner.py'
```

Every simulation task supplies its exact test class filter through this queue command; replace `CLASS_FILTER` and `UNIQUE_RELATIVE_OUTPUT` with the task's named class and a fresh evidence path:

```powershell
python $logisticsQueue exec --project openantares --timeout 900 -- python tools/Logistics/Run-LogisticsTests.py --source-root $logisticsSource --filter 'FullyQualifiedName~OpenAntares.Simulation.Tests.CLASS_FILTER' --output 'UNIQUE_RELATIVE_OUTPUT'
```

For tasks naming multiple classes, join complete qualified clauses with `|`, for example `FullyQualifiedName~OpenAntares.Simulation.Tests.LogisticsContentTests|FullyQualifiedName~OpenAntares.Simulation.Tests.LogisticsVersionTests`. Do not put an unqualified second class after the first predicate.

The worker runs `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --nologo --disable-build-servers --filter <filter> --logger trx --results-directory <output>` in SourceRoot, builds the changed test dependency graph, and parses TRX. Require nonzero executed tests, zero failed/skipped, unchanged source hashes and exact requested filter. No `--no-build`, unfiltered fallback or silent empty pass. Expected red is a named assertion failure or the specifically missing new API; infrastructure failure is not a red test. A compilation-only red from adding a new API is permitted once; arithmetic/regression cases require an actual failing assertion before the fix.

The worker retains command, source root, HEAD plus dirty-file hashes, content/fixture hashes, log, TRX and result.json under a fresh directory. Only execute selected cases. Direct worker invocation occurs inside the shared queue, never starts another queued job and never touches global latest pointers. A final focused invocation combines the new Logistics test classes once; do not rerun every unchanged prior task after a UI-only correction.

## Task 1: Capture the actual old behavior and establish focused state validation

**Files:** New `Logistics/LogisticsState.cs`, `Logistics/LogisticsStateValidator.cs`, `tests/OpenAntares.Simulation.Tests/LogisticsStateTests.cs`, `LogisticsFixtures.cs` in that test directory, `tools/Logistics/Run-LogisticsTests.py`, `tools/LogisticsBaselineCapture/Program.cs`, `LogisticsBaselineCapture.csproj` in that tools directory; modify `State/GameState.cs`, `GameStateValidator.cs`, `Rules.cs`.

**Interfaces:** `LogisticsState.Clone() -> LogisticsState`; `LogisticsStateValidator.Validate(ContentSet content,GameState state) -> IReadOnlyList<string>`; add only `Rules.LogisticsVersion=13` and `Rules.HasLogistics(int version) -> bool` as the gating bootstrap here. Task2 adds13 to general supported/capability gates and can then create full validated13 worlds. Task1 exercises deep clones and the logistics validator directly on explicitly tagged synthetic state, not a generated playable13 world. `LogisticsFixtures.Create(string speciesId="harl",ulong seed=42) -> LoadedGame` is introduced/used after Task2; initial fixture helpers construct only the standalone state needed for these direct checks. Synthetic two-colony states must validate once full support exists and never be labelled played campaigns. Baseline capture is a separate executable referencing the unchanged simulation, not test output as a source of truth.

- [ ] Capture before any product edit: hash current src/content and all pending Markdown, branch/worktree/ref inventory and existing fixtures. In a queued baseline-capture job against the actual old product, generate an unchanged rules12 Harl/Solo and Wenth/Solo seed42 start through normal generator/commands, record six ordinary ready boundaries and canonical state+PRNG at each. Sentries/amebas off, crises off to avoid unrelated response consent. Also freeze the existing genuine v1/rules3 and a rules11 `harl-trade-earmark` fixture's bytes and its accepted finite continuation trace. Record real paths/provenance and fail if missing; do not fabricate an old-rules12 save after introducing13. These narrow futures are needed because shared serialization/economy/turn dispatch will change; no broad campaign matrix.
- [ ] Write `CloneDoesNotShareStocksRoutesOrManifests` and invalid duplicate owner/hull/ID/stock/capacity tests. Assert `Assert.NotSame(original.Logistics, clone.Logistics);` and changing clone stock leaves original unchanged. Old-rules states with injected logistics must be rejected.
- [ ] Run `LogisticsStateTests` red using the focused queue command.
- [ ] Add nullable root state, deep clone, minimal HasLogistics gating and version-gated validator; keep all existing state fields/clone semantics. Rules13 is not yet generally supported or offered in setup, so no full generated13 world is needed for this task's pure state checks. Finalize absent-state/full-world capability tests with Task2. Define orphan reservation and mismatched passenger identity errors with path/entity details.
- [ ] Run the same class green; checkpoint diff/evidence. No commit.

## Task 2: Add opt-in version/content contracts without changing old capability gates

**Files:** New Logistics content trio and `content/core/logistics.json`; modify `Rules.cs`, `Content/ContentSet.cs`, `ContentLoader.cs`, `ContentWriter.cs`, `Combat/CombatContentReader.cs`, `CombatContentWriter.cs`, `Species/SpeciesRules.cs`; new `LogisticsContentTests.cs`, `LogisticsVersionTests.cs`.

**Interfaces:** `LogisticsDefinition` supplies the exact global constants; `ContentSet.Logistics -> LogisticsDefinition?`; `Rules.HasLogistics(int version) -> bool`; `LogisticsInitializer.Initialize(ContentSet content,GameState state) -> void` in new `Logistics/LogisticsInitializer.cs`. Extend `ContentLoader.Load` with trailing `bool requireLogistics=false`, preserving all existing call sites. New vessel role ID is `civilian_transport`, marked `LogisticsOnly`; existing `VesselDefinition` gains an init-only bool default false. Add loader/writer support for this property only in logistics campaigns.

- [ ] Write theories over rules1..13: old HasExpansion/Species/Rivals/Development/Crises/Scouting/Fleets/Combat/ShipDesigns/Diplomacy/Conflict capability truth values unchanged for1..12; all enabled for13; only13 has logistics. Assert `Assert.False(Rules.HasLogistics(12)); Assert.True(Rules.HasConflict(13));` while CurrentVersion remains12.
- [ ] Add invalid JSON cases for negative/overflow quantities, minimum greater than target, wrong/missing transport role and invalid slot sizes; errors name file/entry/field. Assert the role is available to all four species only under13, with no changes to historical allowed-role lists.
- [ ] Run `LogisticsContentTests|LogisticsVersionTests` red, then implement content loading/conditional writing, explicit13 support in every capability gate and legacy metadata exclusions. Before existing combat-content validation, add the logistics civilian role profile by copying scout MaxHull/Armor and using Attack0, and derive its speed from scout speed. Keep the source combat profiles untouched. Core loading may contain new definitions, but serializing old campaigns must omit both logistics and its role/profile; changing dictionary order must not alter canonical old content.
- [ ] Initialize empty stock and disabled policies for every generated existing colony; new founding gets the same defaults. Add this initializer to generation for explicit13 only, not to old-save load.
- [ ] Run the same filters green; checkpoint. No new setup default or active writer version yet.

## Task 3: Make support forecasts and resolution use one stock calculation

**Files:** New `LogisticsEconomy.cs`; modify `EconomyCalculator.cs`, `EconomyResults.cs`; new `LogisticsEconomyTests.cs`.

**Interfaces:** `SupportStockResult(long LocalUsed,long StockUsed,long Stored,long ClosingStock,long GrowthSurplus,long RemainingDeficit)`; `LogisticsEconomy.CalculateSupport(long produced,long required,long openingStock,long target,long capacity) -> SupportStockResult`. `ColonyEconomy` gains optional `SupportStockResult? LogisticsSupport` without changing existing positional constructor. The pure calculator determines stock deltas; the turn coordinator applies them exactly once later.

- [ ] Write exact examples: produced500/required600/opening300/target0/cap2000 yields StockUsed100, ClosingStock200, RemainingDeficit0, GrowthSurplus0. Produced800/required600/opening0/target150 yields Stored150 and GrowthSurplus50. Target0 leaves full200 surplus; at capacity retain opening stock and report unused surplus. Peldra demand uses200 per person; other species use their content values.
- [ ] Include `Assert.Equal(new SupportStockResult(500,100,0,200,0,0), LogisticsEconomy.CalculateSupport(500,600,300,0,2000));` plus overflow/negative rejection, zero demand and partial shortage tests.
- [ ] Run `LogisticsEconomyTests` red; implement support satisfaction before existing shortage/output/growth logic, then apply crisis/blockade/diplomacy modifiers in their existing order. Add named reserve-used/reserve-filled breakdowns. Do not recompute outputs from a second UI formula or allow stored stock to yield growth surplus.
- [ ] Assert ordinary forecast equals applied economic record, and rules12 calls follow the old calculation exactly. Run green; checkpoint.

## Task 4: Commission a paid protected transport through ordinary production

**Files:** Modify `Economy/VesselProduction.cs`, `Combat/CombatInitializer.cs`, `ShipDesigns/VesselLoadouts.cs`, logistics initialization/content wiring and only affected production/species queries; new `LogisticsProductionTests.cs`.

**Interfaces:** Reuse existing production target/vessel completion APIs. `LogisticsManifest.Empty` attaches to a completed `civilian_transport` with no passengers/stock, no design and no mission. Add a logistics-only zero-attack combat role profile by deriving scout civilian hull/armor values from existing content at load time, with transport speed from scout speed300; embed the resolved profile in the new-rules effective content. Do not change existing `combat.json` profiles for old roles.

- [ ] With this boundary's net production fixed at0, test bank399 cannot finish and bank400 can finish at the next resolution; verify exact400 deduction, zero free hulls and no concurrent second colony completion. Test new hull cannot load/move on its completion boundary. Its combat stats have attack0 and retain the existing protected-civilian target rules.
- [ ] Run `LogisticsProductionTests` red; extend normal build availability, commission/init and forecast paths. Permit all species to build transports without founding or Wenth-lobe embark authority. No research/design-equipment unlock, fleet supply store or free starting hull.
- [ ] Ensure `VesselLoadouts.Resolve` and ordinary native inspection accept the valid civilian profile instead of throwing “Combat role profile required”; assert `Assert.Equal(0, loadout.Profile!.Attack);` against the inspected `CombatProfile.Attack` declaration.
- [ ] Run green; checkpoint. Preserve manual active queues and construction request semantics.

## Task 5: Own reservations and command authority atomically

**Files:** New logistics command/action/preview/reservation/guard files; modify `Commands/CommandProcessor.cs` and affected generic move/fleet/refit guard dispatch; new `LogisticsCommandTests.cs`, `LogisticsReservationTests.cs`.

**Interfaces:** `LogisticsCommand(EmpireId Issuer,int ExpectedTurn,LogisticsAction Action,string PreviewToken) : Command`; actions `ConfigurePolicy(ColonyId,LogisticsPolicy)`, `AssignMission(MissionDraft)`, `AssignRoute(RouteDraft)`, `CancelMission(MissionId)`, `ReleaseRoute(RouteId)`, `RerouteMission(MissionId,ColonyId)`. `LogisticsQueries.Preview(ContentSet content,EmpireView view,LogisticsAction action) -> LogisticsPreview` produces Allowed, RejectionCode, Token and immutable consequences. `LogisticsCommandProcessor.Execute(ContentSet content,GameState state,LogisticsCommand command) -> CommandOutcome`. `LogisticsReservations.Available(ContentSet content,GameState state,EmpireId owner) -> ReservationLedger` is pure.

- [ ] Write tests for foreign issuer/endpoint/vessel, stale turn/token, duplicate hull/space, reserved founding people, missing published route, invalid floor and insufficient full manual load. Clone+canonicalize before each rejection; `Assert.Same(before, rejected.State); Assert.False(rejected.Changed);` and unchanged RNG/readiness must hold.
- [ ] Test two requests competing for one destination slot: exactly one accepted reservation, no population/stock yet moves. Local support may consume reserved export provisions; reservation cannot starve the origin or double-load goods. Departure/growth/health material facts invalidate preview tokens without disclosing foreign intents.
- [ ] Run both named classes red; implement clone/validate/publish command dispatch using the same readiness and complete-state validation as existing newer commands. Only independent stationary civilian transports can receive missions/routes in this increment. Guard generic move/merge/split/refit/founding actions while assigned; require explicit cancellation/release/reroute rather than silently dropping manifests.
- [ ] Run green; checkpoint. Manual blocker warnings are explicit, including known blockade risk and acknowledged post-departure origin shortage.

## Task 6: Plan whole-person migration without changing species identity

**Files:** New `LogisticsMigration.cs`; extend previews/reservations; new `LogisticsMigrationTests.cs`.

**Interfaces:** `MigrationDecision(bool Allowed,string? Reason,int Passengers,Workforce OriginAfter,long OriginSupportDeficit,int DestinationReservedPopulation)`; `LogisticsMigration.Evaluate(ContentSet content,EmpireView view,ColonyId origin,ColonyId destination,int passengers,int minimumPopulation,bool natural,bool acknowledgeShortage) -> MigrationDecision`. It uses only owned published forecasts and reservations; authoritative command validation independently rechecks the same conditions.

- [ ] Test80/60% boundaries with integer comparisons, origin floor4 natural/floor1 manual, one natural departure per origin, destination reservations and no last-person departure. In a ten-capacity origin8/destination5 natural transfer may be eligible; destination6 blocks it. Reserve missing transport capacity and no move occurs.
- [ ] Test support after removing support workers first, production second, research third; an origin that was supported before removal but short after is refused automatically. Manual transfer requires an explicit acknowledged deficit. Destination support uses current population plus every reserved passenger including this one; no future birth/output promise counts as cargo.
- [ ] Test all four species and native/sealed Sulan habitats. Wenth ordinary passenger requests are rejected with a body-specific reason; daughter seeds/founding stay unchanged. Fractional growth stays at origin and does not travel with people.
- [ ] Run `LogisticsMigrationTests` red; implement evaluations and outward-auto-migration pause on accepted bulk-transfer destination. Re-enabling is an explicit policy command; existing bulk reservations take precedence over natural requests. Run green; checkpoint.

## Task 7: Schedule policy deliveries and repeat routes deterministically

**Files:** New `LogisticsPlanner.cs`, `LogisticsTurnCoordinator.cs`; extend route state; new `LogisticsPlannerTests.cs`.

**Interfaces:** `LogisticsPlanner.Plan(ContentSet content,EmpireView view) -> ImmutableArray<LogisticsAction>`; `LogisticsTurnCoordinator.Prepare(ContentSet content,GameState state,EmpireId owner) -> CommandOutcome` applies that finite plan through ordinary commands. Both AI and human-ready preparation call the same coordinator; no controller-specific resource grant.

- [ ] Test dispatch order: emergency provisions, ordinary reserve, finite bulk transfers, natural migration. Within class use priority, oldest request turn, destination/origin/vessel/request ID. Assigned manual/passenger-only hulls cannot be commandeered. Supply enabling does not enable migration or automatic production.
- [ ] Test native route semantics: the inspected game uses direct star-to-star `TravelCalculator`, not a starlane graph. Limit this increment to a direct legal known leg between the two owned colonies; do not invent multi-hop pathfinding. Choose shortest feasible leg with stable endpoint tie-breaking. Unknown hostiles cannot change the plan.
- [ ] Test opted-in fixed route returns empty to the same origin after unloading; no same-boundary reload/departure. A blocked return parks at a legal port and reports Attention. Manual missions end idle unless their accepted order specified return; automatic migration only selects among opted-in passenger routes.
- [ ] Run `LogisticsPlannerTests` red; implement finite planning and next-boundary return orders. Assert same actions under reversed dictionary insertion and hidden enemy-stock mutations: `Assert.Equal(firstPlan, reorderedPlan);`. Run green; checkpoint.

## Task 8: Transfer once, conserve manifests and integrate passenger health

**Files:** New `LogisticsTransfer.cs`, `LogisticsResolutionContext.cs`; modify `Crises/CrisisTransport.cs` only through logistics-gated entry points and passenger projection; new `LogisticsTransferTests.cs`, `LogisticsHealthTests.cs`.

**Interfaces:** `LogisticsResolutionContext.Capture(ContentSet content,GameState snapshot) -> LogisticsResolutionContext`; `LogisticsTransfer.Apply(ContentSet content,GameState snapshot,GameState candidate,LogisticsResolutionContext context,CrisisResolutionContext? crisis) -> void`. `CrisisTransport.TagPopulationLoading(ContentSet,GameState snapshot,GameState candidate,CrisisResolutionContext,VesselId,ColonyId)` reuses existing episode tagging without the production-only TagCompletion assumption. Add destination unload exposure through the existing exposure ledger exactly once, retaining present system-arrival effects.

- [ ] Test101 provision points occupy2 slots,800 points occupy8,801 exceed8; four passengers occupy8. Loading debits exact source stock/population and increments only manifest goods/CarriedPopulation. Unload reverses that ownership transfer; partial goods unload retains remainder; no population is present in both locations.
- [ ] Run test cases for load after growth/health changes: manual infeasible accepted mission becomes Waiting, releases obsolete reservations, keeps the hull and requires new preview; automatic request releases and retries next boundary. A quarantined/full/invalid destination holds passengers onboard with origin/species identity intact. Cancel before load releases only reservations; after load never remotely refunds cargo.
- [ ] Write crisis-on/off cases: load at an infectious open origin, arrive at quarantined port, complete inspection, unload after safe release and apply destination exposure once. Keep `CarriedPopulation` synchronized by design, not by copying a second count; zero-passenger goods create no new disease vector. Existing `PrepareDepartures`/`HandleArrivals` must still recognize passengers while manifest counts change.
- [ ] Run both classes red; implement one-transfer receipt per original vessel per boundary; exclude newly completed hulls and newly produced stock. In-transit/stranded manifests survive saveable state; no civilian attrition, destruction or abandoned colony. Run green; checkpoint.

## Task 9: Integrate one atomic turn and truthful owner reports

**Files:** Modify `Turns/TurnResolver.cs`, `Rivals/RivalTurnPlanner.cs`, `Knowledge/EmpireView.cs`, `EmpireViewProjector.cs`; new logistics report/view/query files and `LogisticsTurnTests.cs`, `LogisticsVisibilityTests.cs`.

**Interfaces:** `LogisticsTurnCoordinator.ApplyEconomy(GameState candidate,TurnEconomy economy,LogisticsResolutionContext context) -> void`; `LogisticsQueries.Project(ContentSet content,GameState state,EmpireId viewer) -> LogisticsEmpireView`; `EmpireView.Logistics -> LogisticsEmpireView?`; `TurnReport.LogisticsEvents -> ImmutableArray<LogisticsEvent>`. Reports contain explicit quantities/causes and stable owned IDs, not live foreign state.

- [ ] Test a six-boundary fixture covering loading, ordinary travel, arrival/unload and return; departure people contribute to this boundary, arrivals contribute only next boundary. Compare forecast with the actual record and assert goods/population conservation including normal births/health losses separately.
- [ ] Run `LogisticsTurnTests|LogisticsVisibilityTests` red; freeze source facts with the existing contexts, calculate/apply provision deltas once, retain existing economy/health/combat/movement/founding order. Place logistics transfers after `MovementResolver.CompleteConflictFounding` and all arrival handlers, before `CrisisTransport.ApplyExposures` and crisis finalization; this is the inspected exact integration point. Feed new unload exposure into that same ledger before its one application. New load orders cannot move until the next snapshot.
- [ ] Add a logistics phase test seam analogous to existing conflict/crisis seams. Inject failure after stock, load and unload phases; assert original canonical state/PRNG/readiness unchanged. Never publish candidate early or retain calculator-only facts in a save.
- [ ] Project only own stocks/passengers/routes; `LogisticsPlanner` consumes this public view. Confirm hidden enemy inventory/orders/health cannot affect own controls, counts, sorting, token or auto plan. Reports aggregate repeated blocked causes and separate known incoming from current stock. Run green; checkpoint.

## Task 10: Preserve civilian protection and ordinary movement controls

**Files:** Logistics command guards, only affected fleet/production/move query adapters, existing `Combat/CombatCommandGuards.cs`, `Conflict/ConflictCommandGuards.cs` if needed; new `LogisticsConflictTests.cs`, `LogisticsMovementTests.cs`.

**Interfaces:** `LogisticsCommandGuards.Conflicts(EmpireView view,Command command) -> bool`, used before generic orders. Reuse `TravelCalculator.Duration` and existing movement receipts rather than writing a logistics movement loop. `LogisticsQueries.Preview` includes truthful known-blockade warning/consent consequences.

- [ ] Test attack cannot select a civilian transport even with passengers/goods; no combat-side opt-in or blockade contest from a new role. Effective blockade keeps its current50% production rule and never also halves cargo/support. Auto delivery refuses known effective blockade; explicit manual consent permits current protected delivery without lifting it.
- [ ] Test generic grouped move/merge/split/assignment while a mission owns a hull rejects atomically; an unassigned empty transport retains normal legal travel. Cancelling a loaded mission leaves goods on the hull; reroute previews/revalidates endpoint ownership, space and quarantine. No same-boundary transfer plus departure; distinct vessels cannot share one manifest.
- [ ] Run both classes red, add only demonstrated adapters needed to preserve the existing civilian/movement model, then green. Do not rewrite combat formulas, retest the full conflict matrix or implement evasion/interception.

## Task 11: Serialize new state and prove old futures stay old

**Files:** New `Saves/LogisticsSaveData.cs`, modify `SaveGame.cs` plus conditional content writer; new `LogisticsSaveTests.cs`, `LogisticsCompatibilityTests.cs` and immutable baseline fixture files captured by Task1.

**Interfaces:** The inspected `SaveGame` is static and not partial. Keep its public entry points there and use an internal `LogisticsSaveData` helper, following the other subsystem DTOs. Required helper contracts are `LogisticsSaveData.Write(Utf8JsonWriter writer,GameState state) -> void` and `LogisticsSaveData.Read(JsonElement element,ContentSet content,GameState state,IList<ContentError> errors) -> void`. Write ordered IDs, exact cargo/provenance/policies/reservations/revisions, no transient context. Reject invalid state before replacing the session.

- [ ] Add all92 allowed-pair cases and nearby invalid pairs; assert `Assert.Equal(92, acceptedPairs.Count);`. All79 original pairs retain their meanings, plus14/1..13. Writer14 serializing rules12 must omit logistics, transport role and new metadata;14/13 requires complete valid logistics data. Unknown cargo/duplicate hull/reservation overflow/mismatched species and passenger provenance fail with useful entity/field diagnostics.
- [ ] Run the named save/compatibility classes red; add conditional readers/writers and version diagnostics, preserving original pair clauses rather than replacing them with `schema >= rules`. Tests for historical raw input must load actual frozen bytes, never merely rewrite a schema number on a new save.
- [ ] Replay Task1's frozen finite futures for rules1/3/11 and captured rules12 profiles, compare original canonical state/content/PRNG at every frozen checkpoint. New logistics-disabled setup and old resaves retain rules12. Test13 save/reload during Planned, Loaded, Transit, Waiting, partial goods unload and empty return yields identical future commands/states.
- [ ] Run green. This narrow compatibility expansion is justified by changed global save/version/capability paths; no full historical campaign matrix or unrelated native suite. Preserve failed inputs/results and original fixture hashes.

## Task 12: Expose the playable civilian workflow in existing screens

**Files:** New LogisticsPanel, LogisticsMissionDialog, GameScreen.Logistics.cs; modify NewGameScreen, GameScreen/SelectionConsole/FleetPanel, ColonyPanel, FleetRoster and theme. Establish the opt-in LogisticsValidation scene/targets/control checks and queued native worker here as the red/green infrastructure owned by this UI deliverable; Task13 adds the full affected journey/evidence verification. No globe shader, orbital rendering or authoritative-state UI reads.

**Interfaces:** `LogisticsPanel.Show(ContentSet content,EmpireView view,ColonyId colony) -> void`; event `Action<LogisticsAction> ActionRequested`; `LogisticsMissionDialog.ShowMission(ContentSet content,EmpireView view,VesselId vessel) -> void` with the same event. GameScreen's handler gets the pure preview, displays actual consequences and issues LogisticsCommand through the existing session; it never mutates state directly. Extend `NewGameScreen.Configure` with trailing `bool? logisticsEnabled=null`, default false; Start chooses13 only for explicit On. No schema/rules jargon in player labels.

- [ ] Create the minimal opt-in scene/targets and finite queued worker using the exact parameters/lifecycle specified in Task13; write failing Setup/Stocks/Mission controls before wiring this UI. This establishes their infrastructure within Task12, without executing a later task out of order. The flow is setup On → colony support reserve/export policy → paid transport production → owned destination → finite goods/passenger preview → confirmation → turn report → save/load/reopen with exact current manifest. Setup Off and old campaigns show neither logistics settings nor dead projects.
- [ ] Show `Local support`, `Needed`, `Reserve`, `Incoming`, `Arrival`, `Reserved capacity` and a specific blocked reason. Growth progress is not people. Wenth migrant actions explain the body/daughter-seed constraint; provisioning remains usable. Confirm origin-shortage/known-blockade consent and outward-migration pause explicitly. Keyboard cancellation/focus returns and ordinary map/course controls remain usable.
- [ ] Extend FleetRoster's equivalence signature with immutable own cargo/mission/provenance/commitment facts so different deliveries never collapse into equivalent rows. Preserve Shift selection/exact member IDs and all existing fleet controls; do not replace the roster.
- [ ] Put all styling/metrics in the shared theme. Retain one colony-globe viewport; opening the logistics section must not rebuild it. Run only affected native cases, then checkpoint. Visual code needs the build required to run those cases, not a full solution suite.

## Task 13: Verify the affected native flow and restore the ordinary assembly

**Files:** Extend Task12's new `tests/OpenAntares.Game.Validation/LogisticsValidation.cs`, `LogisticsValidation.tscn`, `LogisticsValidation.targets`, `LogisticsControlChecks.cs` and `tools/Logistics/Run-Logistics.ps1`; new `Verify-LogisticsEvidence.py` in that tools directory; new owner checklist `docs/playtests/civilian-logistics-2026-11-16.md`.

**Interfaces:** Worker parameters `-SourceRoot`, `-OutputDirectory`, `-Case` with allowed values `Setup`, `Stocks`, `Mission`, `Migration`, `Health`, `FleetRows`, `Feature`; opt-in scene uses production session/controls. Expected viewport sizes800x600 and1920x1080. Verify-LogisticsEvidence consumes the worker's result.json and exact source/DLL manifests; it cannot turn missing frames/control records into a pass.

- [ ] Implement each focused red case before its corresponding UI wiring: genuine globe-area/current-card click, setup Off/On, stock forecast, finite mission preview/cancel, natural/manual transfer and Wenth explanation, quarantine hold, return leg and cargo-distinct roster. Use labelled synthetic published two-colony fixtures for hard-to-reach UI state plus at least one paid new-game transport/order/save path; never call fixture injection an earned campaign.
- [ ] Worker design follows the existing PlanetGlobes worker lifecycle: fresh evidence directory inside actual SourceRoot, selected opt-in targets build, finite native scene, logs/frames/source+DLL hashes, finally ordinary game rebuild and empty validation-namespace inspection. Retain all failed outputs. Only allow one queue owner; worker does not recursively queue itself.
- [ ] Execute a case through `python $logisticsQueue exec --project openantares --timeout 900 -- powershell -NoProfile -ExecutionPolicy Bypass -File tools/Logistics/Run-Logistics.ps1 -SourceRoot $logisticsSource -OutputDirectory 'reports/civilian-logistics-20261116/native-UNIQUE' -Case Mission`. Inspect result/logs and verification; require zero engine/shader/runtime errors, both viewport records and restored ordinary assembly. Do not relabel pre-existing globe performance as newly verified.
- [ ] Once independent work is exhausted, owner checklist prerequisites are the ordinary restored build from the tested checkout, Godot4.7.2.NET and restart of any old run. Number actions: enable Supplies and migration; set reserve/export; build a transport; order provisions then passengers between owned compatible colonies; inspect arrival and return; save/reload; try Wenth. Expected outputs include correct counts, no vanished cargo and clear reasons for restrictions. Report-back fields: source/build ID, resolution, species/seed, turn, action, expected/observed result and optional screenshot/save name. Explain that failed observations identify bounded fixes and passing human comprehension is a separate acceptance scope.

## Task 14: Close the selected scope with one review and evidence ledger

**Files:** New `reports/CIVILIAN_LOGISTICS_IMPLEMENTATION_2026-11-16.md`, report machine summary; update status, guide, specifications/acceptance indexes and an execution progress ledger chosen at implementation start. Keep all parent row IDs/statuses/totals unchanged unless separately qualified evidence supports a specifically approved change.

**Interfaces:** Final evidence maps LOG-001..006/008..010 to the civilian subset and named tests/cases/source. LOG-007/fleet endurance and development-goods clauses are explicitly Deferred to later delivery, not failed or falsely passing. LOG-AC-011 has civilian behavior approval scope only; broader future defaults remain proposed.

- [ ] Run the new Logistics simulation filter once for the final changed source, plus only directly affected existing cases demonstrated necessary by integration fixes. Run the single native Feature case if the final UI integration has not already been covered on identical source; otherwise use its matching evidence without claiming a rerun. Require selected cases pass with no skipped/empty/missing evidence and ordinary build/namespace restoration. No full suite by default.
- [ ] Dispatch exactly one fresh whole-implementation reviewer after implementation, as retained by owner instructions. Review product behavior, reservations/conservation, privacy, health, legacy compatibility and native evidence; do not run per-task agents or a second reviewer. Correct findings with focused failing tests and matching reruns; retain failed evidence and explicit unresolved findings.
- [ ] Reconcile implementation report, version/pair counts, actual executed commands and source scopes. Preserve all old fixtures, pending owner documents/theme, renderer/shaders, branch/worktree/ref inventory and parent statuses. Separate automated verification, owner input/comprehension and later balance/platform/release limits.
- [ ] Present concrete reviewable implementation and remaining human observations. No commit, merge, push, release, cleanup or automatic switch of normal setup to logistics. Owner branch disposition is separate.

## Coverage and self-review

| Design requirement in selected delivery | Tasks |
| --- | --- |
| LOG-001 provisions, stock and support/growth timing |2/3/9 |
| LOG-002 paid transport, capacity and manifest identity |1/4/8 |
| LOG-003 policies, priority, reservations and shared authority |5/6/7 |
| LOG-004 loading/travel/unload/cancel/return |5/7/8/9/10 |
| LOG-005 natural/manual migration and Wenth/habitat constraints |6/8/12 |
| LOG-006 health, protection and no double blockade penalty |8/9/10 |
| LOG-008 deterministic phases/conservation/rollback |1/3/8/9 |
| LOG-009 state, old compatibility, commands and fair AI |2/5/7/9/11 |
| LOG-010 player information and control |9/12/13 |
| LOG-001 development goods / LOG-007 fleet supplies / tactical interception |Outside selected increment; later plans/design |

Author self-review: each owning task has a finite red/green check and defined interfaces; cross-task API names and unit/version choices were reconciled; all five Review Focus conditions have named tests. The [self-review report](../../../reports/CIVILIAN_LOGISTICS_PLAN_REVIEW_2026-11-16.md) records resolved planning ambiguities and approval limits. This is planning/document verification only; none of the implementation tests above has run.

Next: owner review of this concrete plan, including the explicit unit/cost normalization and opt-in setup/version choices. Native inline execution is already selected; do not re-ask execution method. Start Task1 only after plan approval.
