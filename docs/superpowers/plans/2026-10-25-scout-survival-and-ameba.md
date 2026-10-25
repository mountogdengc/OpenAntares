# Scout Survival and Space Ameba Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved scout standing orders and space-ameba warning, withdrawal, study and loss loop while preserving every old campaign.

**Architecture:** Extend the existing independent simulation with explicitly versioned scouting state, own-view planning, shared commands and a hazard phase before movement. Keep disease/economy ordering intact, deposit study research after ordinary research/cure processing, and publish only complete valid transactions. Presentation consumes immutable projected facts; activate new defaults after simulation, codecs and controls pass.

**Tech Stack:** Existing C#/.NET8 simulation/xUnit, Godot4.7.2 .NET presentation, JSON and PCG32. No dependencies, image assets, engine changes or general combat framework.

**Spec:** [Full approved scout/ameba specification](../specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md), owner approval2026-10-25 in [ADR-016](../../DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md). Approval accepted the full amended rules at738c68e; read both documents.

**Gate:** M7-D/SCT-AC-012 Verified for design only. M7-P In Progress for review of this separate plan; M7-I and all functional SCT criteria Not Started. Retain **Native inline**, one fresh final whole-branch reviewer, regression-first corrections and no repeat review unless requested. **No per-task agents.** No implementation, application run or genuine future capture in this planning turn. No main integration/push/release/cleanup authorization.

## Global Constraints

- “Existing rivals remain peaceful expansion competitors.” Neutral contact never becomes an attack.
- “Arrival causes no roll, damage or loss, irrespective of vessel speed or other empires' prior visits.”
- “Warning-departure success100% with no roll and one successful study reward per empire/habitat are algorithm invariants”. Do not expose configurable overrides for these guarantees.
- “Only a currently stationary eligible scout can receive **Study ameba**.” Eligibility uses non-founding/zero-cargo capabilities, not species/role-ID branches.
- “No old campaign auto-upgrades.” Old rules1..6 retain embedded content, formulas, AI, IDs, PRNG and genuine futures; no fixture regeneration to bless changed behavior.
- “Save/load never rolls, acknowledges, studies, prepares movement or advances warnings.”
- “Cargo vessels require an ordinary manual departure; no silent population movement authority is invented.” AI retains its separate approved ordinary movement authority.
- “No-op” and rejection preserve state/reference/Ready; changes clear only issuer Ready. Any late error restores the exact enclosing pre-request state, including another human's accepted planning.
- C# simulation never references Godot. Use integers/Int128, mathematical floor for signed odds, ordinal ordering, invariant culture and existing PCG32. No GetHashCode/System.Random/time-derived rules.
- Core ameba: maximum1 permanent stationary non-home/uncolonized habitat; require at least2 eligible stars to place1; pursuit200; late escape clamp(50+floor(100*(speed-200)/200),5,95); study clamp(50+floor(50*(speed-200)/200),5,90); reward600 research units, once per empire/habitat. Scout cost800/speed300 unchanged; founders speed200 unchanged. Ordinary arrival fully surveys before response; no diversion/pursuit in transit.
- Observe is the neutral-contact default; Withdraw from ameba is explicit saved default survival authority. Avoid is opt-in, not a waiver of ameba survival. Human outward exploration remains manual. AI never studies/remains deliberately.
- New target rules7/writer8: retain22 existing pairs and add(8,1..7), exactly29. Keep CurrentVersion6 until Task14; keep writer7 until Task11 after genuine capture. Off requires explicit null hazard streams, no habitat/encounters/claims, but retains ordinary scout posture/report state.
- Preserve all seven worktrees/branches, source QA, backups/stashes, primary main42e1282 and four byte-exact owner files. Use unique QA/output names; never overwrite a player's save or notification preference file.
- Shared theme and containers; reread theme immediately before any required minimal variation. Original setting/roster and FR-001 backlog remain intact. No roaming/splitting/colony attack, combat/removal, sensors, warships, diplomacy, multiplayer or release scope.

## Review Focus

1. A destination becomes quarantined during Ready preparation: a cargo ship must not consume its safe opportunity or roll against the ameba when it cannot actually depart. Task7 response validation and Task8 late rollback tests own this.
2. A receipt was obtained for the automatic retreat and preparation then materializes that same route: do not invalidate identical consequences, silently acknowledge a different route, or replan another ready human. Tasks5/7 own exact effective-action tokens and tests.
3. A study survivor's research would fund a technology/cure: neither gets an extra completion or this-turn old-reserve diversion. Task8 owns exact reserve/phase tests.
4. The only remaining retreat star contains a peaceful rival, or the last colony has been lost: survival can override neutral caution, but must never override a known ameba habitat or acquire unseen intelligence. Tasks5/7/10 own the fixtures.
5. An Exposed speed200 test accidentally uses a founder, or a saved destroyed vessel is looked up as live: Exposed comes only from successful scout Study; model slow scouts through labelled embedded content, and use immutable retired facts. Tasks3/8/9/11 own these checks.

## Baseline, evidence and commands

Reuse the current clean registered checkout `.worktrees/colony-development-design`, branch `codex/scout-survival-design`. After plan approval, preserve this branch and create execution branch `codex/scout-survival` from the approved plan commit in the same checkout; inspect the existing worktree first. Primary F:/Projects-PCGames/OpenAntares stays untouched. Baseline main42e1282 has the same product as M6 source d8bef99. Historical evidence is1,265 simulation tests and six graphical gates; it is not a fresh planning run.

**Genuine rules6 future capture has not run.** [Source catalog](../../../reports/scout-design/rules6-source-catalog.json) locates/hashes32 retained actual generated UI saves from the fresh M6 main Crisis/Colony runs, including four species, Standard/Off, onset, contained cases, cargo/founding and developed campaigns. Cataloging bytes is not running their futures. Task1 must freeze old futures before **any** product/content/default/writer edit; all existing old/passive-AI fixtures remain byte-exact.

Commands run from the execution checkout, with logs under `.superpowers/sdd/2026-10-25-scout-survival-and-ameba/` and reports under `reports/scout-execution/`:

- **Build:** `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers`.
- **Focused(Type):** `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers --filter FullyQualifiedName~Type`.
- **Suite:** Build, then `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo`.
- **Runtime(Scenario,Folder):** `./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario Scenario -OutputDirectory Folder`. Verify that executable first; use an actual .NET graphical display and new folder per attempt. Execute scenarios sequentially because they share the build assembly.
- **Normal:** `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild`, then `./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll'`.

Normal dependency audit remains enabled. Restore only if needed and record result; --no-restore is not a new vulnerability audit. Every task records a semantic RED, minimal implementation, focused GREEN, Suite and explicit-path commit in its ledger. Missing API/compile errors are not semantic RED; introduce only the named minimal compile contracts required to reach that task's behavioral failure. Do not claim test counts in advance. No per-task review dispatch is implied by these gates.

## Existing file map

Rows name exact current files. New files are listed in their owning task; S=`src/OpenAntares.Simulation`, T=`tests/OpenAntares.Simulation.Tests`, G=`game`, V=`tests/OpenAntares.Game.Validation`, all relative to repo root.

| Action | Path | Responsibility |
| --- | --- | --- |
| Modify | `src/OpenAntares.Simulation/Rules.cs` | Explicit7 predicates; later current default |
| Modify | `src/OpenAntares.Simulation/Content/ContentSet.cs` | Effective hazard definition |
| Modify | `src/OpenAntares.Simulation/Content/ContentLoader.cs` | Optional requireScouting, dependencies and located errors |
| Modify | `src/OpenAntares.Simulation/Content/ContentWriter.cs` | Explicit versioned embedded section |
| Modify | `src/OpenAntares.Simulation/State/GameState.cs` | Game/empire scouting slots and deep clones; EmpireState is in this file |
| Modify | `src/OpenAntares.Simulation/State/VesselState.cs` | Scout capability state plus separate cargo-capable encounter |
| Modify | `src/OpenAntares.Simulation/State/GameStateValidator.cs` | Scout invariant delegation |
| Modify | `src/OpenAntares.Simulation/Galaxy/GalaxyGenerator.cs` | Explicit setting, initialization after old generation |
| Modify | `src/OpenAntares.Simulation/Knowledge/EmpireView.cs` | Immutable own scouting view |
| Modify | `src/OpenAntares.Simulation/Knowledge/EmpireViewProjector.cs` | Private current/history/report projection |
| Modify | `src/OpenAntares.Simulation/Commands/CommandProcessor.cs` | Scout command dispatch and Ready transaction |
| Modify | `src/OpenAntares.Simulation/Commands/ExpansionCommands.cs` | Shared move/founding validation and order replacement |
| Modify | `src/OpenAntares.Simulation/Colonization/FoundingRules.cs` | Known/current guardian founding rejection |
| Modify | `src/OpenAntares.Simulation/Colonization/ColonizationResolver.cs` | Guard consumed vessels and habitat, preserving old paths |
| Modify | `src/OpenAntares.Simulation/Movement/MovementResolver.cs` | Omit consumed vessels, ordinary survivor travel |
| Modify | `src/OpenAntares.Simulation/Crises/CrisisTransport.cs` | Confirm no destroyed-cargo exposure/null dereference |
| Modify | `src/OpenAntares.Simulation/Turns/TurnResolver.cs` | Explicit phase integration, fault seam and complete report |
| Modify | `src/OpenAntares.Simulation/Saves/SaveGame.cs` | Writer8 and exact29-pair codec dispatch |
| Modify | `src/OpenAntares.Simulation/Saves/ExpansionSaveData.cs` | Per-vessel scout/encounter persistence |
| Modify | `src/OpenAntares.Simulation/Rivals/RivalTurnPlanner.cs` | Scoped command whitelist and versioned budget |
| Modify | `src/OpenAntares.Simulation/Rivals/RivalVesselPlanner.cs` | Survival first, filtered targets, cargo evacuation |
| Modify | `src/OpenAntares.Simulation/Rivals/RivalColonyPlanner.cs` | Own-known guarded worlds unavailable |
| Modify | `game/ContentFiles.cs` | Require current feature content at activation |
| Modify | `game/Screens/NewGameScreen.cs` | Ameba picker/preview and explicit setting |
| Modify | `game/Screens/GameScreen.cs` | Projected command wiring/grouped emergency/navigation |
| Modify | `game/Components/VesselPanel.cs` | Posture, route status and own encounter actions |
| Modify | `game/Components/GalaxyMap.cs` | Known-only habitat marker |
| Modify | `game/Components/StarPanel.cs` | Dated hazard/survey information |
| Modify | `game/Components/TurnReportDialog.cs` | Own immutable order/reward/loss facts |
| Modify | `tests/OpenAntares.Game.Validation/Run-Validation.ps1` | Scout scenario routing |
| Modify | `tests/OpenAntares.Game.Validation/Validation.targets` | Seventh opt-in harness |
| Modify | `tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1` | Seven-type negative/positive metadata controls |
| Modify | `tests/OpenAntares.Game.Validation/README.md` | Scope, provenance and run commands |
| Modify | `tests/OpenAntares.Game.Validation/CrisisGraphicalValidation.cs` | Task12 labelled scout UI supplement before seventh scene exists |

## Interface and state contracts

These are plan-level names and decomposition choices; they do not change approved gameplay. Create focused files under S/Scouting and use existing namespaces/partial command/codec patterns. All public collections in views/tokens/results are ImmutableArray; persisted mutable containers deep-clone. Keep private raw-state queries inside simulation. A policy context exposes only its own EmpireView, public ContentSet and pure travel helper, never GameState or a foreign view.

- **Task2 content:** S/Content/AmebaDefinition.cs defines `ProbabilityRule(int BasePercent,int RelativeSpeedCoefficient,int MinimumPercent,int MaximumPercent)` and `AmebaDefinition(string Id,string Name,int PursuitSpeed,ProbabilityRule LateEscape,ProbabilityRule Study,long ResearchReward)`. ContentSet adds `AmebaDefinition? SpaceAmeba`. Singular JSON section `space_ameba` contains id/name/pursuit_speed/late_escape/study/research_reward; probability objects use base_percent/relative_speed_coefficient/minimum_percent/maximum_percent. Bases/coefficients0..100, ordered probability bounds0..100, pursuit1..int.MaxValue and positive reward fitting long; aggregate reserve overflow fails atomically. Unknown fields (including warning_success/reward_count), duplicate section/ID and invalid values reject. Append optional `bool requireScouting=false` to `ContentLoader.Load`; enabling it requires existing species/rivals/development/crises content. Old writer field sets omit it.
- **Task3 state:** S/Scouting/ScoutState.cs defines enum `ScoutPosture { Observe,AvoidEncounters }`, `AmebaStage { Warning,Exposed }`, `AmebaRiskAction { Study,Remain }`. `GameState.Scouting:ScoutGameState?` stores Enabled, nullable PlacementRandom, SelectionCount0/1 and nullable `AmebaHabitat(int Id,string DefinitionId,StarId Star)` (local Id1). `EmpireState.Scouting:EmpireScoutState?` stores nullable RiskRandom, RiskRollCount, NextEncounterNumber1+, sorted KnownHabitats, SuccessfulStudies, Receipts, current/ten-turn Digests and retired own-vessel facts. `VesselState.Scout:ScoutVesselState?` stores Posture, explicit WithdrawFromAmeba=true, optional exact pending approach acceptance and wait signature. `VesselState.AmebaEncounter:AmebaEncounterState?` independently stores Number, HabitatId, ArrivalTurn, Stage, Revision, optional risk intent. Cargo can have an encounter, never Scout. Revision/encounter counters are checked long; monotone per-owner encounter numbers distinguish re-entry. Standard requires streams even without a habitat; Off requires null streams/no ameba facts while retaining neutral scout metadata. Also create S/Scouting/ScoutRecords.cs with **all immutable saved token/event/digest/receipt/retired-fact records** described in the Task5/9 contracts, and S/Commands/ScoutCommands.cs with the concrete record declarations described below. Defining these plain contracts now avoids forward type dependencies; no command handler, projection or report behavior is implemented early.
- **Task5 views:** S/Scouting/ScoutViews.cs defines `ScoutView(EmpireId Owner,int Turn,ImmutableArray<ScoutVesselView> Vessels,ImmutableArray<KnownAmebaView> Habitats,ImmutableArray<AmebaAttentionToken> Attention,ImmutableArray<ScoutTurnDigest> History)`. ScoutVesselView includes own Id/role/cargo/posture/station/transit/pending/effective departure/risk, projected odds/reward/deadline/blocker; known habitat includes dated static observation only. The Task3 `AmebaAttentionToken` is a value record of VesselId, EncounterNumber, Revision, Stage, effective action, destination, role speed, cargo, percent and reward. A receipt matches the exact current token set; no string hash/random identifier. Define `ScoutPreparationPreview(ImmutableArray<Command> Commands,ImmutableArray<AmebaAttentionToken> Attention,ImmutableArray<ScoutOrderEvent> Outcomes,ImmutableArray<string> Blockers)` using Task3 command records; it computes decisions, never executes them. `ScoutQueries.Project(ContentSet,GameState,EmpireId)->ScoutView`, `ScoutQueries.PreviewPreparation(ContentSet,GameState,EmpireId)->ScoutPreparationPreview`, `AmebaOdds.LateEscape(AmebaDefinition,int speed)->int`, `.Study(...)->int`, `.Succeeds(int percent,int draw)->bool`. Extract existing projection into internal `EmpireViewProjector.ProjectBase(ContentSet,GameState,EmpireId)->EmpireView` without scout enrichment; public Project enriches it, and ScoutQueries uses only ProjectBase to avoid recursion. Both public queries use a shared pure evaluator of the own base view/permitted scouting facts, without calling each other or validating recursively. Effective action includes the automatic fallback before materialization; doing exactly that preserves the token. Commands renew revision only when effective consequences change, so acknowledgement cannot invalidate itself merely by preparing its previewed route.
- **Task6 commands:** S/Commands/ScoutCommands.cs defines `ScoutCommand : Command`; concrete records preserve `(EmpireId Issuer,int ExpectedTurn,...)`: `SetScoutPostureCommand(...,VesselId Vessel,ScoutPosture Posture)`, `SetAmebaRiskOrderCommand(...,VesselId Vessel,long EncounterNumber,long ExpectedRevision,AmebaRiskAction? Action)`, `SetAmebaApproachOrderCommand(...,VesselId Vessel,StarId Destination,int HabitatId)`, `AcknowledgeAmebaCommand(...,ImmutableArray<AmebaAttentionToken> Tokens)`, `SetScoutRetreatOrderCommand(...,VesselId Vessel,StarId Destination)`. The last is ordinary automatic intent whose shared move validation permits neutral fallback only when it equals the owner's legal survival choice; it is never an arbitrary bypass. S/Commands/ScoutCommandProcessor.cs extends existing partial processor; all mutation goes through CommandProcessor.Execute. A non-null ordinary move replaces a risk order; clearing only move leaves a separate Study/Remain unchanged. Posture can change in transit; approach/study/remain/retreat require the appropriate stationary owner/current facts. Foreign/nonexistent vessel validation remains generic before detailed reasons.
- **Task7 planning:** `IScoutPlanningContext` exposes ContentSet Content, EmpireView View, ScoutPreparationPreview Preparation and `int? TravelTurns(StarId,StarId,string roleId)`. Define `ScoutPlan(ImmutableArray<Command> Commands,ImmutableArray<ScoutOrderEvent> Outcomes)` and `ScoutPreparationResult(GameState State,bool Succeeded,ScoutPlan Plan)`. `ScoutPlanner.Plan(IScoutPlanningContext,bool ai)->ScoutPlan` uses Task5's common decisions rather than maintaining a conflicting fallback algorithm. `ScoutTurnCoordinator.Prepare(ContentSet,GameState,EmpireId)->ScoutPreparationResult` returns original on failure/no change and exposes its outcome plan for Task9's later report recording. `ScoutReadyQueries.ValidateResponse(ContentSet,GameState,EmpireId)->ImmutableArray<string>` validates exact acknowledgement and **actual legal departure** after private crisis/governor/scout preparation; no mere PendingMove-is-not-null test. Expose pure equivalent preview for UI. Human one movement/cancellation per scout; cargo manual. Direct Resolve applies idempotent same preparation and cannot bypass human consent.
- **Task8 resolution:** `AmebaResolutionContext.Capture(ContentSet,GameState)->AmebaResolutionContext` stores ToTurn, stable encounter inputs, ConsumedVessels, own immutable Events and deferred ResearchAwards; it is transient and never serialized. `AmebaResolver.ResolveExisting(ContentSet,GameState snapshot,GameState candidate,AmebaResolutionContext)->void`; `.ObserveArrivals(ContentSet,GameState candidate,AmebaResolutionContext)->void`; `.DepositResearch(ContentSet,GameState candidate,AmebaResolutionContext)->void`. Append optional `AmebaResolutionContext? scouting=null` after existing crisis context in MovementResolver.Apply/ColonizationResolver.Apply, preserving older calls. Only a consumed-ID membership permits skipping a missing candidate; unexplained disappearance remains failure. Existing snapshot economic/cargo facts are not rewritten to evade their contracts. `ScoutResolutionPhase { Preparation,Economy,Health,Encounter,Transport,Observation,Reward,Reports,Validation }` and an internal `TurnResolver.ResolveScouting(ContentSet,GameState,Action<ScoutResolutionPhase>)->TurnResolution` seam fail individual phases while using the same core implementation/public rules.
- **Task9 reports:** S/Scouting/ScoutReports.cs implements reporting using Task3's immutable `ScoutOrderEvent` (owner/turn/vessel/role/cargo/star/kind/reason/route/odds/reward/encounter identity) and `ScoutTurnDigest`. `ScoutReports.RecordPreparation(GameState,EmpireId,ScoutPlan)->void`, `.Finalize(GameState snapshot,GameState candidate,AmebaResolutionContext)->void`. Latest planning choices coalesce per vessel/current turn; retain ten completed turn digests, not an unbounded click log. Each vessel/turn has up to8 fixed event slots (construction, decision, encounter, study, departure/escape, arrival, loss, wait); overwrite same-slot planning facts, preserve distinct actual events. Bound each digest by8*(NextEntityId-1) using Int128; retained immutable retired IDs must be below NextEntityId, own, nonconflicting and referenced by history. Keep live unresolved warnings/receipts independently of history, bounded by current owned vessels; no lifetime receipt accumulation. A wait signature suppresses repeated identical neutral wait records until condition changes. No truncation of legitimate events or plague receipts.
- **Task11 codec:** S/Saves/ScoutSaveData.cs follows CrisisSaveData's WriteGame/ReadGame, WriteEmpire/ReadEmpire, WriteVessel/ReadVessel with existing Utf8JsonWriter/EntryReader and rules argument; not a second save format. Rules1..6 omit every scout/hazard field, including null/empty. Rules7 requires explicit fields/streams/defaults/empty lists, exact enum spellings and strict unknown-field rejection. JSON section names for state are scouting at game/empire, scout and ameba_encounter at vessel. No data repair or schema-only migration.

### Valid fixture and frozen reference contracts

Task3 creates `T/ScoutingFixtures.cs`: `Game(bool enabled=true,string species="harl")->LoadedGame`, `AtAmeba(AmebaStage stage=Warning,int scoutSpeed=300)->LoadedGame`, `TwoHumansAndAi()->LoadedGame`, `WithSecondScout()->LoadedGame`, `WithNextDraw(LoadedGame,int draw)->LoadedGame`, `Bytes(GameState)->string`. Bytes uses stable test JSON state serialization before writer8 exists; codec tests use SaveGame after Task11. Every helper returns fully valid sorted state and calls the validator before starting a GameSession. No changing a running generated campaign to manufacture evidence.

Task3 initializes fixture metadata directly from the literal reference constants/existing Pcg32 primitives; it does not call the as-yet-unimplemented Task4 initializer. Task4 can then reuse its initializer for generated test starts while retaining explicit labelled fixture provenance. Task3 defines immutable event fields including Owner, Turn, Vessel, RoleId, Cargo, Star, Kind, Reason, Destination, Percent, ResearchAward and encounter number/revision; optional fields are nullable and source strings remain invariant, bounded facts. Task9 implements storage/aggregation without redefining those record types.

Labelled Harl fixture: owner1, colony3/planet2/star4 at(0,0), scout5; surveyed-capable empty star6/planet7 at(0,300), guarded star8/planet9 at(301,0); NextEntityId10; population6/workforce6/0/0; turn10 for encounters, ordinary species/appearance/development/crisis metadata complete. Separate hazard ID1 is not an entity allocation. Standard seed42 placement selects second sorted candidate, star8. AtAmeba puts scout5 there, refreshes ordinary surveys plus own hazard knowledge, encounter1/revision1/arrival10 and next encounter2, no receipt/risk order. Exposed fixture models a previous successful Study and saved successful claim; speed200 is a **modified embedded scout definition**, never an invalid Exposed cargo vessel. Multi-human/AI variants allocate fresh noncolliding IDs, homes outside the hazard, correct species/ports/contacts, explicit streams and sorted arrays.

Independent reference vectors are retained in [reference-vectors.json](../../../reports/scout-design/reference-vectors.json). They were computed without executing game code and checked against the existing PCG42/54 reference sequence; executing tests must independently match the actual C# implementation.

| Seed42 stream | Initial state | Increment | First bounded results |
| --- | --- | --- | --- |
| Placement |15004228035630008857|10413031053861234691|NextInt(2):1,0,1,1|
| Owner1 risk |13725908560590527421|9990820718965293057|NextInt(100):55,85,11,23|
| Owner10 risk |6162952895543560625|9990820718965293079|NextInt(100):67,67,32,1|

Owner1 threshold states before NextInt(100), all increment9990820718965293057: draw74/state5962262318774883999/prior86 bounded rolls;75/16262608648794321724/47;94/1666470698191063269/88;95/7717953694584393234/73;49/16344847632548268183/30;50/13875876282034758786/537. WithNextDraw uses these labelled pre-session reachable states/counters; thresholds are not injected during live execution. Warning departures/arrival/Remain consume no roll; study/exposed departure each consume one owner bounded roll, respecting internal rejection sampling.

## Dependency-ordered tasks

### Task 1: Freeze genuine rules6 saves and unchanged-engine futures

**Files:** Create `T/ScoutingCompatibilityTests.cs`, `T/Fixtures/rules6-content.json`, `T/Fixtures/rules6-scout-baselines/{manifest.json,saves/,futures/,commands/}`; retain a capture helper/evidence under the execution ledger. Modify no product file.

**Interfaces:** Consume existing SaveGame/GameSession/GalaxyGenerator, actual source catalog and all old compatibility fixtures. Produce exact schema7/rules6 source/future/command/content hashes and canonicalization removing schema_version only. Read32 catalogued genuine UI sources; additionally capture **16 generated** unchanged-rules6 small seed42 reference campaigns (4 species×Solo/One Peldra×plague Off/Standard), explicitly labelled simulation-generated, not UI evidence. Capture4 subsequent resolved states each via ordinary current CrisisQueries.PendingAttention/AcknowledgeCrisisCommand and human Ready in ID order; log every command. Add a separately labelled valid held-cargo baseline using the unchanged engine if none of the retained genuine sources is held; never relabel a synthetic case as genuine.

- [ ] **RED:** Add `[Fact] GenuineRules6SourcesAndFuturesAreFrozen` with `Assert.Equal(32, manifest.UiSources.Count); Assert.Equal(16, manifest.GeneratedSources.Count); Assert.All(manifest.Cases,c=>Assert.Equal(4,c.Futures.Count));`. Run Focused(ScoutingCompatibilityTests); expect missing-capture failure. Preserve all existing fixture hashes.
- [ ] **Implement:** Verify product diff versus main42e1282 is empty and all catalogued source/evidence SHA256 values match. Copy source saves without overwriting originals; run retained Native capture helper referencing unchanged rules6/writer7 simulation. Manifest records source type/product/hash/settings, embedded-content hash, commands/future hashes and capture helper hash. Fail rather than regenerating an expected old future with feature code. If a source moved, locate/hash equivalent genuine evidence and record the substitution before progressing.
- [ ] **GREEN:** Focused(ScoutingCompatibilityTests), Suite, validate every source/future as rules6/writer7 and no scout fields; current crisis receipts/holds/scout AI routes continue under exactly recorded commands. Compare reloaded and uninterrupted canonical future states.
- [ ] **Commit:** `test: freeze rules6 scout compatibility baselines`; exact fixture/test/capture provenance paths and Task1 ledger entry. This is the prerequisite to every following product edit.

### Task 2: Versioned hazard content and preserved feature predicates

**Files:** Create `S/Content/AmebaDefinition.cs`, `ScoutContentReader.cs`, `ScoutContentWriter.cs`, `T/ScoutingContentTests.cs`, `T/Fixtures/scouting-content.json`; modify Rules.cs/ContentSet.cs/ContentLoader.cs/ContentWriter.cs. Do not add core hazard JSON yet.

**Interfaces:** Produce Task2 content contract, `Rules.ScoutVersion=7`, `HasScouting(int)->bool`; explicitly extend supported/expansion/species/rivals/development/crisis predicates to7 with all1..6 behavior unchanged. CurrentVersion remains6.

- [ ] **RED:** Pin `Assert.True(Rules.HasCrises(7)); Assert.True(Rules.HasScouting(7)); Assert.False(Rules.HasScouting(6)); Assert.Equal(6,Rules.CurrentVersion);` and exact pursuit200/reward600/odds bounds. Focused(ScoutingContentTests) must fail semantically after minimal contracts compile.
- [ ] **Implement:** New located reader/writer through existing EntryReader/builder; reject duplicate/missing/unknown/invariant-override fields, malformed numeric types, negative values and unsupported metadata in old embedded output. Read full fixture content from old crisis JSON plus exact new singular section, not partial defaults.
- [ ] **GREEN:** Focused(ScoutingContentTests), Suite; old rules1..6 content bytes unchanged after canonical writer normalization. Validate modded speed/odds/reward and0/100 endpoints without changing fixed warning/single-claim semantics.
- [ ] **Commit:** `feat: define versioned scout hazard content`; explicit content/rules/tests only and ledger.

### Task 3: Explicit state, deep clones and valid fixtures

**Files:** Create `S/Scouting/ScoutState.cs`, `ScoutRecords.cs`, `ScoutStateValidator.cs`, `S/Commands/ScoutCommands.cs` (record contracts only), `T/ScoutingFixtures.cs`, `ScoutingStateTests.cs`; modify State/GameState.cs, State/VesselState.cs, State/GameStateValidator.cs.

**Interfaces:** Produce Task3 state/fixture/plain-record contracts. `ScoutStateValidator.Validate(ContentSet,GameState,List<string> errors)->void` composes with current validation and checks old metadata absence, Standard/Off shape, capability, noncolony habitat, odd expected stream selectors, bounded counters/revisions, exact owner references and sorted unique knowledge/claims/encounters/receipts. Initially check ready-human consent structurally; Task5 adds effective-action validation after shapes are sound, with no recursive full validation. Do not regenerate worlds to validate a save.

- [ ] **RED:** Assert `GameStateValidator.Validate(content,ScoutingFixtures.AtAmeba().State)` is empty; cloning nested streams/knowledge/encounter/risk/report collections does not alias. `Assert.Null(g.State.Vessels.Single(v=>v.CarriedPopulation>0).Scout);` in labelled cargo fixture; Exposed founder rejects, Exposed slow scout accepts. Focused(ScoutingStateTests).
- [ ] **Implement:** All required initializers and clone paths; fixture contracts above with immutable source/appearance facts. Do not silently add metadata while validating/load. Counters/revision overflow returns failure through callers.
- [ ] **GREEN:** Focused(ScoutingStateTests), Suite; corrupt owner/habitat/ID/duplicate/missing/null/legacy and mixed human/AI states reject. Existing supported states and historical sorted-ID contract unchanged.
- [ ] **Commit:** `feat: add explicit scouting state and invariants`; state/tests/fixture paths and ledger.

### Task 4: Isolated ameba generation and deterministic streams

**Files:** Create `S/Scouting/ScoutInitializer.cs`, `T/ScoutingGenerationTests.cs`; modify Galaxy/GalaxyGenerator.cs and vessel initialization during construction in Turns/TurnResolver.cs.

**Interfaces:** `ScoutInitializer.PlacementRandom(ulong seed)->Pcg32`, `.RiskRandom(ulong seed,EmpireId)->Pcg32`, `.Initialize(ContentSet,GameState,bool enabled)->void`, `.InitializeVessel(ContentSet,int rules,VesselState)->void`. Append `bool? AmebasEnabled=null` to NewGameSettings for source-compatible older callers: rules1..6 accept null only and acquire no state; rules7 requires explicit true/false. The actual new-game UI will pass its visible Standard/Off choice in Task14. Update explicit rules7 fixture/generation callers to pass it; do not infer setup from missing metadata. Reuse the current completed rules6 generation first, then initialize7; never reseed its existing streams.

- [ ] **RED:** Match the three literal vector rows above in actual C#. Assert seed42 two eligible candidates selects second StarId with SelectionCount1; fewer than2 candidates selects none/Count0; Off null/no draws. After stripping new metadata `Assert.Equal(oldGalaxyFacts,newGalaxyFacts); Assert.Equal(old.NextEntityId,new.NextEntityId);`. Focused(ScoutingGenerationTests).
- [ ] **Implement:** Ordered candidate filtering excludes both human/rival home colony stars, independent of species. All created eligible scouts get Observe/Withdraw=true; cargo gets encounter capability only. Generation/read/preview does not consume risk rolls; construction still waits until next planning boundary.
- [ ] **GREEN:** Focused(ScoutingGenerationTests), Suite;4 species×3 sizes×Solo/One parity, alternate1/2/3/4-star maps, count1 selection eligibility, explicit Standard metadata even when no habitat, Off and old content failure atomicity. No public default switch yet.
- [ ] **Commit:** `feat: initialize stationary ameba with isolated streams`; exact generation/state tests and ledger.

### Task 5: Private knowledge, odds and effective-action previews

**Files:** Create `S/Scouting/ScoutViews.cs`, `ScoutQueries.cs`, `AmebaOdds.cs`, `T/ScoutingQueryTests.cs`; modify Knowledge/EmpireView.cs, EmpireViewProjector.cs and Scouting/ScoutStateValidator.cs for exact effective-action receipt checks after structural validation.

**Interfaces:** Produce Task5 projection/odds/attention contracts. Add ScoutView? Scouting to EmpireView and own report view; observe own presence only. The pure preparation preview includes fallbacks, legal port/hold status, effective action tokens,100% deadline, exact odds breakdown/combined numerator over10000 and research amount.

- [ ] **RED:** `Assert.Equal(95,AmebaOdds.LateEscape(d,300)); Assert.Equal(75,AmebaOdds.Study(d,300)); Assert.Equal(5,AmebaOdds.LateEscape(d,100)); Assert.Equal(25,AmebaOdds.Study(d,100)); Assert.Equal(49,AmebaOdds.Study(d,199));` plus0/100 bounds. The199 case pins mathematical floor for nondivisible negative delta. Assert identical projected bytes/tokens for states differing only in unseen foreign plans, habitat sightings, study claims or cargo. Focused(ScoutingQueryTests).
- [ ] **Implement:** Own static known habitat persists with date; foreign ship sightings remain current-only. Unknown-star routes never read hidden habitat truth. Prefer own-colony then surveyed retreat, duration/StarId; survival may choose a neutral-occupied fallback only if all ordinarily permitted ones fail, never a known hazard. Query missing/non-owned IDs generically. No callback exposes raw state to planners/UI.
- [ ] **GREEN:** Focused(ScoutingQueryTests), Suite; no fallback/last colony gone/stale colony/current foreign ship/shared star/ties/unknown destinations. Repeated preview and forecast/save reads preserve streams/IDs/readiness/reports. Exact same automatic route before/after preparation yields identical token; changing its effective destination makes an old receipt insufficient.
- [ ] **Commit:** `feat: project private scout decisions and ameba odds`; queries/views/tests and ledger.

### Task 6: Shared posture, approach, risk, acknowledgement and founding commands

**Files:** Create `S/Commands/ScoutCommandProcessor.cs`, `T/ScoutingCommandTests.cs`; consume the ScoutCommands.cs records created in Task3; modify Commands/CommandProcessor.cs, ExpansionCommands.cs, Colonization/FoundingRules.cs.

**Interfaces:** Produce Task6 command records and ordinary Execute dispatch. Exact new rejection codes: scout_unavailable, scout_not_eligible, scout_destination_excluded, ameba_approach_confirmation_required, ameba_encounter_stale, ameba_study_reserved, ameba_study_already_rewarded, ameba_habitat_guarded, ameba_acknowledgement_required, ameba_response_required. Reuse existing not_owner/stale_turn/transit/unknown_target reasons before hazard-specific disclosures.

- [ ] **RED:** Reject excluded destination and duplicate study with `Assert.Same(before,result.State); Assert.False(result.Changed);`; Observe permits neutral co-location but not a silent known-ameba approach; cargo cannot Study/approach known habitat. Exact token acknowledgement with a foreign/missing/extra/stale row rejects atomically. Focused(ScoutingCommandTests).
- [ ] **Implement:** Role-based posture, pure consequence preview and atomic cancellation when Avoid activates; transit posture only changes future planning. Study/Remain replace pending departure, bind resulting current revision and expire after one boundary; null clears only risk intent. Non-null move replaces risk, while null move clears only move. Exact approach command sets route/acceptance together and clears acceptance on route change/clear/launch. Ordinary movement and retreat share one validation function; survival exception is validated from own projected fallback. Guarded founding rejects in preview and command, unknown-target privacy first.
- [ ] **GREEN:** Focused(ScoutingCommandTests), Suite; no-op reference/Ready, stale/foreign/transit/unknown attacks, reservation release on cancellation/loss, explicit Remain cargo consequence, post-success study rejection and per-empire independent claims. Selecting orders never rolls, spends reward or mutates foreign state.
- [ ] **Commit:** `feat: validate scout standing and ameba risk orders`; commands/founding/tests and ledger.

### Task 7: Bounded survival preparation and atomic human Ready

**Files:** Create `S/Scouting/IScoutPlanningContext.cs`, `ScoutPlanningQueries.cs`, `ScoutPlanner.cs`, `ScoutTurnCoordinator.cs`, `ScoutReadyQueries.cs`, `T/ScoutingReadyTests.cs`; modify Commands/CommandProcessor.cs, Turns/TurnResolver.cs.

**Interfaces:** Produce Task7 planning/response contracts. Validate saved exact acknowledgement before any human preparation; run existing crisis then governor then scout preparation privately; validate resulting actual responses before Ready. Preserve issuer readiness semantics and enclosing transaction root; already-ready humans are not replanned from other private choices. Direct Resolve runs the same rules idempotently and rejects missing human consent.

- [ ] **RED:** AtAmeba with no receipt: `Assert.Same(input,denied.State); Assert.Equal(ScoutingFixtures.Bytes(input),ScoutingFixtures.Bytes(denied.State));`. Acknowledge effective auto route, Ready: pending retreat materializes without invalidating consent. Another accepted human's bytes stay exact on final-human failure. Focused(ScoutingReadyTests).
- [ ] **Implement:** At most one movement/cancellation operation/scout; permitted manual departures win; danger takes precedence over neutral Avoid; absent fallback keeps blocker, offers legal unknown manual destination. Study/Remain suppress only this encounter's automatic retreat for this boundary. Cargo requires valid manual departure; if crisis preparation closes destination/cancels move, reject Ready and restore pre-request state rather than spending warning/rolling or inventing automatic cargo movement.
- [ ] **GREEN:** Focused(ScoutingReadyTests), Suite; repeated/no-op/non-final Ready, withdraw readiness, stale accepted routes, pure previews, unacknowledged reload/direct Resolve, port/hold and mixed plague/scout receipts. All unexpected command/budget failures restore original reference, reports/streams/IDs/counters and another human's accepted planning.
- [ ] **Commit:** `feat: prepare scout survival inside ready transaction`; coordinator/Ready/tests and ledger.

### Task 8: Resolve warning, study, escape, vessel loss and delayed reward

**Files:** Create `S/Scouting/AmebaResolutionContext.cs`, `AmebaResolver.cs`, `T/ScoutingResolutionTests.cs`; modify Turns/TurnResolver.cs, Movement/MovementResolver.cs, Colonization/ColonizationResolver.cs, Crises/CrisisTransport.cs where required.

**Interfaces:** Produce Task8 phase/context/fault seam. Keep economic snapshot and crisis progression first. Resolve only pre-existing stationary encounters by EmpireId/VesselId before departure tagging; consumed IDs skip downstream movement/founding/transport. Observe new arrivals after ordinary movement/final observations, allocate encounter numbers and safe warnings then. Deposit study awards after normal cure/research/onset/observation, before complete reports/final validation; checked addition can abort entire turn.

- [ ] **RED:** Warning departure has `Assert.Equal(beforeRiskState,afterRiskState); Assert.Equal(0L,afterRiskRolls-beforeRiskRolls);` and ordinary2-turn travel. Threshold74 Study survives/+600/Exposed versus75 consumed/no reward; exposed94 escapes versus95 consumed. Slow embedded scout uses49/50 threshold. Focused(ScoutingResolutionTests).
- [ ] **Implement:** Each Study/Exposed departure calls owner NextInt(100) exactly once; success iff draw<percent even under modded0/100, with no extra draw for arrival/Remain/prompt escape. Remove exact vessel/cargo/live flags/orders, keep immutable events/observations and prior reward. Guarantee no start-and-arrive-and-depart twice, no new completed-vessel action, no interstellar pursuit. Any supposedly departing survivor blocked by downstream transport is an unexpected atomic failure, not stationary safety or reroll.
- [ ] **GREEN:** Focused(ScoutingResolutionTests), Suite. Start reserve1199/target cultivation_methods1200/ordinary research0: successful Study publishes reserve1799 and no completion, next ordinary interval completes once. Active cure progress1199/cost1200/new research0 stays1199 through Study, reward enters normal reserve after cure. Retain earned600 if late escape later fails. Test consumed flagged cargo produces no exposure/refund, founder bypass rejects, two arrivals get independent full warning, same-owner claim uniqueness and other-owner stream independence. Inject failure at every ScoutResolutionPhase, reward overflow and downstream invalid candidate: exact enclosing bytes/reference/RNG/readiness restored.
- [ ] **Commit:** `feat: resolve ameba survival study and vessel loss`; phase/movement/cargo tests and ledger.

### Task 9: Bounded own history, retired facts and grouped attention

**Files:** Create `S/Scouting/ScoutReports.cs`, `T/ScoutingReportTests.cs`; modify Knowledge/EmpireViewProjector.cs and Turns/TurnResolver.cs; extend ScoutStateValidator.

**Interfaces:** Produce Task9 event/digest/retired-fact contracts; add ImmutableArray<ScoutOrderEvent> ScoutEvents to TurnReport and projected own report. Record own actual reward/loss and historical observed foreign facts, never current foreign intelligence. Require live unresolved warning receipts independently of ten-turn trimming.

- [ ] **RED:** Destroy a vessel, project/load report without live lookup: `Assert.Equal(600,ownHistory.Single(e=>e.Kind=="study").ResearchAward); Assert.DoesNotContain(foreignHistory,e=>e.Owner==owner);`. Repeated identical neutral wait records once;30 warnings group once while each exact response remains selectable. Focused(ScoutingReportTests).
- [ ] **Implement:** Coalesce the eight slots/vessel/turn,10 completed digests/current planning digest, checked entity-based bounds, stable keys and cleanup of unreferenced retired own facts/receipts after safe retirement. Persist effective planned/ordered/travelling/arrived distinctions; never infer foreign destruction from vanished sighting. Notify own loss accurately while preserving normal encounter identity/orbit wording.
- [ ] **GREEN:** Focused(ScoutingReportTests), Suite; repeated toggles/Ready do not grow unbounded histories; wait persists beyond10 turns without duplicates/disappearance; encounter revision/re-entry cannot reuse old acknowledgement; no foreign cargo/study/research/route leaks, and no plague receipt is satisfied/trimmed by scout activity.
- [ ] **Commit:** `feat: retain bounded private scout reports`; report/view/invariant tests and ledger.

### Task 10: Own-view AI survival and filtered expansion

**Files:** Create `T/ScoutingAiTests.cs`; modify Rivals/RivalTurnPlanner.cs, RivalVesselPlanner.cs, RivalColonyPlanner.cs and own-only planning queries/context as needed; reuse ScoutPlanner.

**Interfaces:** Allow the specific scout posture/retreat commands through the same scoped Execute path. Rules7 budget2C+2V+1 (one posture plus one move/founding/cancellation per vessel), separate unchanged crisis C+V+4; rules4/5/6 keep2C+V+1. Cargo evacuation uses its existing single movement allowance. No new AI raw-state accessor or risk/acknowledgement command authority.

- [ ] **RED:** All species scouts at warning depart100% with no new risk draw; capture the accepted command list through the existing internal rival-planning test seam and assert `Assert.DoesNotContain(commands,c=>c is SetAmebaRiskOrderCommand); Assert.InRange(commands.Count,0,2*C+2*V+1);`. Unknown habitat mutations yield same target/command bytes. Focused(ScoutingAiTests).
- [ ] **Implement:** Opt into Avoid, survival first; when not escaping, preserve approved unknown/resurvey age/travel/StarId ranking after known exclusions and own destination deconfliction. Filter known guarded founder targets; if none legal, wait. Known neutral fallback can yield to hazard survival, never vice versa; zero-colony/no-surveyed-fallback AI can choose charted unknown noncurrent star by duration/StarId. No study/remain, free scout, home lookup, bonus or new epidemic authority.
- [ ] **GREEN:** Focused(ScoutingAiTests), Suite; Harl/Peldra/Sulan/Wenth, multiple scouts/cargo, alternate allowed roles and no-candidate waits, own-known stale facts, resurvey age3 and paid replacement after labelled loss. All Task1 old AI/passive futures remain exact; rejected submitted command/bound violation rolls back everyone.
- [ ] **Commit:** `feat: apply own-view scout survival to rival planning`; AI/query/tests and ledger.

### Task 11: Strict writer8 persistence and29-pair compatibility

**Files:** Create `S/Saves/ScoutSaveData.cs`, `T/ScoutingSaveTests.cs`; modify Saves/SaveGame.cs, ExpansionSaveData.cs, content writer dispatch and state validators. Extend ScoutingCompatibilityTests.

**Interfaces:** Produce Task11 codec, activate writer8 only after Task1 captures and prior simulation work. Retain CurrentVersion6. Preserve public SaveGame APIs and flushed temporary replacement. Explicit supported pairs are(1,1),(2,1),(3,1..2),(4,1..3),(5,1..4),(6,1..5),(7,1..6),(8,1..7); all others reject before publication.

- [ ] **RED:** `Assert.Equal(8,SaveGame.SchemaVersion); Assert.Equal(29,acceptedPairs.Count); Assert.Equal(before,Canonical(reloaded));` across Warning/Exposed/pending risk/known approach/transit/no-fallback/retired-history/claim and Off states. Old rules payload with injected scout:null/empty metadata rejects. Focused(ScoutingSaveTests).
- [ ] **Implement:** Exact enums/required nullable fields, saved PCG state/odd selectors/counts, encounters/revisions/receipts/action proofs, claims/history/retired identities and explicit survival default. Old content/state writer branches omit every new field; the version-aware save reader must also reject an injected space_ameba content section for rules1..6 even though the general current-content loader recognizes it. Never rebuild/hydrate missing data. Validate read/written state before replacing sessions/files.
- [ ] **GREEN:** Focused(ScoutingSaveTests), Focused(ScoutingCompatibilityTests), Suite. Full rejection matrix including(7,7), duplicate/unsorted/foreign references, invalid stage-role/stream/token/claim/bounds, unknown invariant overrides, missing Off nulls; failed load/write preserve session/prior file. All32 genuine and16 reference Task1 futures compare exactly except schema_version; all pre-existing old/passive fixture bytes stay unchanged.
- [ ] **Commit:** `feat: persist scout and ameba state with writer8`; codec/compatibility tests and ledger.

### Task 12: Player controls, safe consequence confirmation and displays

**Files:** Create `G/Components/ScoutOverviewDialog.cs`, `G/Presentation/ScoutNotificationPreferences.cs`, `V/ScoutUiControlChecks.cs`; modify Screens/GameScreen.cs, Screens/NewGameScreen.cs, Components/VesselPanel.cs, GalaxyMap.cs, StarPanel.cs, TurnReportDialog.cs, V/CrisisGraphicalValidation.cs and V/Validation.targets. Reread `G/theme/openantares_theme.tres` before any necessary minimal variation. The seventh scene itself belongs to Task13.

**Interfaces:** `ScoutOverviewDialog.ShowScouting(ContentSet,ScoutView)->void` plus `event Action<Command> CommandIssued`; all UI action semantics come from projected commands/tokens. Preferences path `user://scout-notifications.json`, enum Interrupt/Attention/Digest; tests override to a unique QA path. Append nullable ameba argument to NewGameScreen.Configure and display picker only when CurrentVersion supports scouting; before Task14 actual default remains6. `ScoutUiControlChecks.Run(Node root,Action<bool,string> check,string outputDirectory,string runId)->Task` supplies a labelled supplemental test through the **existing Crisis scenario**, so this task has an independent real UI gate. It uses complete core content plus exact approved hazard definition and explicit rules7/setting in pre-session generated or labelled fixtures; it never edits a live campaign. Hazard-content Load failure retains usable Load Game.

- [ ] **RED:** Add the opt-in helper/call to existing Crisis scene and actual-control checks equivalent to `Assert.False(screen.View.Scouting!.Attention.IsEmpty);` before acknowledgement, `Assert.Equal(75,studyPreview.Percent);` and exact7125/10000 combined result. Missing controls/actions record explicit failed checks without masking existing scenario results. Runtime(Crisis,unique-task12-red-folder) must reach these semantic assertions, not fail on missing scene/results or a compile error.
- [ ] **Implement:** Separate neutral posture from always-present scout survival; confirmed route acceptance, study/remain cargo-loss warning and exact-set grouped acknowledge/withdraw; no waiver or per-vessel modal storm. Exposed prompts fresh consent; loading unresolved warnings reopens by default. Interrupt/Attention/Digest never erase blocker/badge/history; routine Off cannot hide emergencies. Navigation preserves relevant scout/star after death via own report facts; in-transit controls cannot redirect. Known marker derives from permitted view, no creature art needed.
- [ ] **GREEN:** Build, Suite and Runtime(Crisis,unique-task12-green-folder) verify actual SetMove/Study/Remain/Acknowledge/Ready at800x600/1920x1080, valid/stale confirmations, focus/scroll bounds only to harness scope and canonical purity of resize/preference/report reads. Keep labelled scout assertions distinct from generated Crisis results; all existing Crisis cases must pass too. No physical OS input/comprehension/comfortable-reading claim.
- [ ] **Commit:** `feat: expose scout survival and ameba decisions`; presentation/helper/existing-scene opt-in paths after this task's actual gate passes, ledger states exact evidence.

### Task 13: Seventh graphical scenario and real save/load futures

**Files:** Create `V/ScoutGraphicalValidation.cs`, `V/ScoutGraphicalValidation.tscn`; modify Run-Validation.ps1, Validation.targets, Verify-NormalAssembly.ps1, V/README.md. Reuse existing FrozenContent and test-only scene patterns; update existing six scenario assumptions only where writer/current version genuinely changed, never rewrite frozen legacy expected behavior.

**Interfaces:** Scenario name Scout; seventh forbidden normal-assembly type ScoutGraphicalValidation; reuse Task12's ScoutUiControlChecks helper. Results keep actual assertions, generated provenance, supplemental fixtures, geometry, diagnostics, screenshot/save paths and hashes separate. Add no harness class/helper to ordinary game compilation.

- [ ] **RED:** Actual scene/control tests include `Assert.Equal(7,OptInHarnessTypes.Count); Assert.Empty(NormalHarnessTypes);` and current exact controls/outcomes. Runtime(Scout,unique-task13-red-folder) records missing/wrong behavior meaningfully; a missing results.json alone is an execution error, not semantic RED.
- [ ] **Implement:** Before default activation, use explicit rules7 **generated** complete core+exact hazard content saves through actual Load to exercise controls; label that provenance as generated pre-session, not New Game UI. Actual untouched runtime campaigns discover ameba using own view only, prepare retreat, and perform real UI Save/Load plus4-future canonical comparison. Supplement labelled pre-session threshold/cargo-loss/slow scout/no-fallback/two humans/30 warnings/expired history fixtures, never mutate live RNG/population/orders.
- [ ] **GREEN:** Runtime(Scout,unique-task13-green-folder), then all6 retained scenarios sequentially, Suite and Normal. Inspect new arrival/warning/study/cargo-loss/800 screenshots; record engine diagnostics and all QA paths. Confirm real opt-in metadata negative control sees all7 and ordinary rebuild excludes all7. Task12's existing-scenario evidence remains retained separately.
- [ ] **Commit:** `test: add scout ameba graphical acceptance`; opt-in harness/runner/docs and actual gate records. No per-task reviewer.

### Task 14: Activate rules7 defaults and prove generated setup/parity

**Files:** Create `content/core/space_ameba.json`, `T/ScoutingIntegrationTests.cs`; modify Rules.cs CurrentVersion, G/ContentFiles.cs, Screens/NewGameScreen.cs, generated Scout scenario setup checks and relevant current-version test assumptions; retain every frozen content fixture.

**Interfaces:** New Game explicitly passes rules7/Ameba Standard, One Peldra/Harl and current plague Standard defaults, with independent visible Off/Solo controls. Core content requires exactly the approved singular hazard definition; missing current content never blocks old save loading. Existing explicitly old programmatic callers retain their optional null parameter; current rules7 callers must explicitly supply true/false as in Task4.

- [ ] **RED:** `Assert.Equal(7,Rules.CurrentVersion); Assert.True(start.State!.Scouting!.Enabled); Assert.True(start.State.Vessels.Where(IsScout).All(v=>v.Scout!.Posture==ScoutPosture.Observe&&v.Scout.WithdrawFromAmeba));` and actual New Game picker assertions. Focused(ScoutingIntegrationTests), Runtime(Scout,unique-task14-red-folder).
- [ ] **Implement:** Add exact JSON; flip only after preceding gates. Generation validates current content and exposes no hazard location/count before observation. Core-sized Standard has1 habitat, Off has0/null streams. Pure setup previews do not advance live streams or imply a warning/consent.
- [ ] **GREEN:** Focused(ScoutingIntegrationTests), Suite, Runtime(Scout), affected retained scenarios and Normal. Actual untouched seed42 New Game campaigns4 human species×3 sizes×Solo/One (24) discover/withdraw through permitted own-view choices within160 core turns; build/research/cure/acknowledge through ordinary commands without injected income/outcomes. Compare24 Ameba Off starts with rules6 galaxy/old streams, plus Solo Observe future parity for plague Off/Standard. Record actual Study result; labelled threshold fixtures separately prove both outcomes. Save/load4-future equivalence at warning/Study-survivor/transit boundaries, with no phantom reward or extra study. Preserve genuine Task1 and all older futures under writer8.
- [ ] **Commit:** `feat: activate approved scout survival and ameba setup`; defaults/core/generated evidence paths and ledger. Branch is not ready for integration until Task15/review.

### Task 15: Whole-branch acceptance, one fresh review and handoff

**Files:** Create `reports/SCOUT_VALIDATION_2026-10-25.md`, `reports/scout-execution/progress.md`, `final-machine-summary.json`, `final-review.md` and any correction evidence; update PROJECT_STATUS.md, docs/SPECIFICATIONS.md, docs/ACCEPTANCE_CRITERIA.md, docs/IMPLEMENTATION_PLAN.md and reports/VALIDATION_REPORT.md with actual scope/results only.

**Interfaces:** Produce criterion evidence map and final source SHA/commands/exit codes/counts/provenance/known limits. Retain Native one fresh independent whole-branch reviewer after all tasks/gates, using requesting-code-review skill at that time; no approval inferred for integration.

- [ ] **RED:** Acceptance audit reads actual artefacts: `Assert.All(criteria.Where(c=>c.Id!="SCT-AC-012"),c=>Assert.NotEmpty(c.ExecutedEvidence)); Assert.Equal(7,metadata.OptInCount); Assert.Equal(0,metadata.OrdinaryCount);` fail missing paths/mappings rather than assigning future test totals. Focused(ScoutingIntegrationTests) and documentation checks own the audit.
- [ ] **Implement:** Resolve evidence gaps, run full fresh Suite, all7 graphical scenarios sequentially with unique folders, Normal, source/QA/PNG/hash/diagnostic inventory and exact owner/main preservation. One fresh whole-branch review checks the approved design, code, meaningful test evidence and disclosure limits. Resolve findings regression-first in one correction pass, rerun affected/full required gates; retain raw report/failures and do not commission repeat reviews on your own.
- [ ] **GREEN:** Every criterion has actual automated evidence or disclosed outstanding limit; no open Important/Critical finding. Mark only exact locally executed functional acceptance Verified; design SCT-AC-012 already satisfied in ADR-016. Human comprehension/balance/physical input/cross-platform/security/export/release remain open. `Assert.True(primaryOwnerHashesUnchanged); Assert.Equal(7,preservedWorktrees.Count);` plus all genuine future checks, Suite/Runtime/Normal. Do not close parent REQ-017 or First Playable human acceptance.
- [ ] **Commit:** `docs: record scout ameba validation and review`; task-done/actual final SHA, retained branches/evidence and a concrete next action. Return results for owner choice about local integration; do not merge/push/release/clean up automatically.

## Coverage and self-review map

| Requirement | Owning tasks |
| --- | --- |
| SCT-001 |2,3,6,7,12,14|
| SCT-002 |4,5,6,10,12|
| SCT-003 |5,6,7,8,10|
| SCT-004 |5,8,9,12,13|
| SCT-005 |7,8,10|
| SCT-006 |1,2,3,9,11,12,14,15|
| SCT-007 |2,3,4,5,6,7,8,9,10,11,12,13,14|

| Criterion | Owning tasks/evidence |
| --- | --- |
| SCT-AC-001 |3,6,7,12,14|
| SCT-AC-002 |4,5,6,9,10,12|
| SCT-AC-003 |5,6,7,10|
| SCT-AC-004 |4,7,8,13|
| SCT-AC-005 |5,8,12,13|
| SCT-AC-006 |4,10,14|
| SCT-AC-007 |6,7,8,9,11|
| SCT-AC-008 |9,12,13,14|
| SCT-AC-009 |3,9,11,13|
| SCT-AC-010 |1,2,4,11,14,15|
| SCT-AC-011 |13,14,15|
| SCT-AC-012 |ADR-016 full design approval; never relabelled runtime evidence|
| SCT-AC-013 |2,3,4,5,14|
| SCT-AC-014 |3,5,6,7,8,9,11,13|
| SCT-AC-015 |5,6,8,9,11,13,14|

Author self-review (no reviewer dispatch): checked every spec requirement/criterion against this map, command/record names and phase dependencies, semantic RED/GREEN/commit gates, all five Review Focus cases and plan proportion. Fixed a UI/harness dependency cycle by giving Task12 real labelled controls through the existing Crisis scene; Task13 independently adds the full seventh scenario. Also fixed ambiguous old-API setup handling through a source-compatible nullable setting, defined nonrecursive base projection and froze valid slow-scout/retired-fact fixtures. No specification gameplay choice is reopened. Source catalog/independent vectors/doc checks are planning evidence only; no game application was built/tested/run, no future capture performed and no implementation has started. Full plan review/confirmation is next; retained Native follows afterward.
