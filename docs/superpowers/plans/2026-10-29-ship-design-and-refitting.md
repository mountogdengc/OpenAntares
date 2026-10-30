# Ship Design and Refitting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. The owner has selected Native execution, no per-task agents and one fresh whole-branch implementation reviewer at the end. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the approved modular warship catalogue, research unlocks, exact-design construction/fleet requests and paid same-frame refits while preserving genuine older campaigns.

**Architecture:** Add a rules10-only ShipDesigns subsystem with immutable saved recipes, owner-local IDs, pure loadout calculations and explicit refit jobs. Reuse the existing typed production target container, colony reserve, finite queues, shared command transaction and snapshot-based combat/travel. Refit payment occurs after existing loss/movement/repair phases and before observation/publication, with a single equipment resolver used throughout.

**Tech Stack:** Existing C#/.NET8, xUnit, Godot4.7.2 .NET, JSON content and deterministic integer/PCG32 infrastructure. No new dependency, engine, asset or RNG stream.

**Spec:** [Approved ship-design/refitting description](../specs/2026-10-29-ship-design-and-refitting-design.md), [ADR-022 approval](../../DECISIONS/ADR-022-approved-ship-design-and-refitting-design.md). Read both; [ADR-021](../../DECISIONS/ADR-021-ship-design-and-refitting-design-entry.md) and the [builder check](../../../reports/SHIP_DESIGN_GAMEPLAY_REVIEW_2026-10-29.md) retain drafting history.

## Global Constraints

- “rules10; writer11”; preserve46 accepted pairs and add only `(11,1)`..`(11,10)` for56; no `(10,10)`, implicit migration or M10 fields/content/research in rules1..9 saves.
- “Maximum32 active designs and256 retained designs per empire”; names1..40 trimmed characters, ordinal case-insensitive uniqueness;16 catalogue rows/page; IDs/revisions checked and never reused.
- “Exactly one engine”; one/two banks, optional one armor package; summed attack still fires one M9 shot. Core Escort100 hull/6 space/cost1200/factor100; Frigate180/10/2400/75. Preserve every component/technology value in SHP-002/003.
- “Standard Escort” is conventional drive400 + pulse600 + plated200 + frame1200 =2400, hull100/attack24/armor4/speed300. It is permanent/active and creates no free ships or research.
- “Editing an existing design creates a copy”; no floating latest recipe, automatic upgrade, hidden substitution, infinite order, salvage, combat XP, new rival behavior or civilian/scout customization.
- “Refit price = max(1, ceil(target undiscounted design build cost /4))”; same frame/full hull/cohesion100/zero cargo/legal own port only, one job/ship and one/colony, idle production slot, no Organizer discount. No extra colony completion, arrival tick, implicit repair or cancel.
- Refit pauses construction automation, retains workforce/queue/template, keeps ship/fleet/flagship/officer identity and locks affected commands. Cancellation preserves banked reserve/old equipment; completion/Cancel leaves construction paused and Hold.
- Preserve automatic M9 volleys, targeting/bands/cohesion, frozen transit/withdrawal, finite repair; M8 Navigator/Organizer/Surveyor and original XP/recovery/cultural/Wenth rules; peaceful AI and deferred peaceful mode.
- Installed engines change travel and M9 sentry escape speed. Existing M7 ameba study/late-escape odds continue using their established role-speed input; do not accidentally route them through damage/Navigator/equipment travel modifiers.
- Simulation has no Godot dependency. Use embedded JSON, string content IDs, ordinal ordering, checked integer/Int128 intermediates, invariant parsing and atomic command/Ready/turn/save publication.
- Preserve all owner prose, peaceful paragraph, raw evidence/QA/genuine futures, eight existing worktrees/branches and recovery. Implementation approval will not authorize main merge, push, release or cleanup.

## Review Focus

1. An archived design is still queued or targeted by an accepted refit: finish its pinned recipe, refuse new selection and never delete referenced data. `ArchiveAfterCommitStillFinishesPinnedRecipe` in Tasks6/9/12.
2. A completed request's vessel is later refitted: keep its historical original design/XP, while active slots still demand exact current design. `CompletedRequestSurvivesLaterRefitWithoutNewXp` in Tasks7/9/12.
3. A new engine or second bank reaches a forgotten role-based call site: preview/combat/travel/repair must use installed stats, while M7 odds and frozen trips keep their existing contracts. `LoadoutCallSitesAgreeWithoutChangingAmebaOdds` in Tasks4/10/16.
4. A refit becomes invalid after economic forecasting: preserve banked output, make no provisional charge/completion, pause/cancel honestly and do not let the colony finish another target. `RefitLateInvalidationDoesNotSpendOrDoubleComplete` in Tasks9/11/12.
5. A second human changes a hidden catalogue/job or ownership context while another is Ready: tokens/views reveal nothing foreign; stale commands and late failure roll back every counter/reserve/consent. `HiddenRivalRecipeDoesNotChangeOwnPreview` and `FinalReadyRefitFailureIsAtomic` in Tasks8/11/13.

## Execution baseline and gates

All17 tasks are Not Started. Full design approval is ADR-022; this separate plan still awaits owner review. No baseline capture/product change/engine validation or execution worktree has occurred in this planning turn. Historical main verification is1,733 tests and Combat3,271 checks/93 separate provenance/12 campaigns on unchanged product source4176996; do not present those as fresh M10 results.

At execution use using-git-worktrees, inventory attached/registered worktrees and establish/reuse a suitable isolated `codex/ship-design` checkout from the accepted planning commit. Preserve primary main and all eight existing worktrees, dirty work and ignored evidence. Native worktree tools are preferred when attached to the actual repository; use a checked Git fallback if that is unavailable. No reset/stash replay, cleanup or copied dirty checkout. Record exact primary and execution-checkout hashes separately: CRLF checkout differences are not evidence of owner mutation.

Keep `.superpowers/sdd/2026-10-29-ship-design-and-refitting/progress.md` as the canonical execution ledger and a tracked final copy in `reports/ship-design-execution/progress-final.md`. Use fresh `reports/ship-design-execution/task-XX/attempt-NN/` directories, never overwrite old attempts. Record commands, exits, elapsed times, source SHA, meaningful RED/GREEN and commit per task. Preserve all historical `*latest*.json` bytes before each test run; restore them afterward and relocate newly emitted timestamped Scout reports under that task with a SHA256 relocation map. Task1 supplies the reusable gate wrapper.

Commands below run from the isolated checkout. `Focused(Type)` means replace Type with the named class, using the same gate wrapper; `Suite`, `Build`, `Normal` and `Runtime(Scenario,unique-folder)` are defined here, not undefined tools:

```powershell
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers --filter FullyQualifiedName~Type
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll'
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario ShipDesign -OutputDirectory reports/ship-design-execution/task-16/attempt-01/runtime
```

Check the executable path/SDK before execution; restore only when needed, retain normal dependency audit and distinguish `--no-restore` from a fresh security audit. Graphical scenarios share the assembly and run sequentially. Every native run must prove its actual opt-in assembly fails the normal-type checker, then rebuild normal code in `finally` and require10 scenario types/2 helpers absent once ShipDesign is added. Keep all unique QA saves, failed runs, screenshots and command transcripts. UI signals do not prove OS input or comprehension.

Every simulation task follows behavioral RED, minimal implementation, focused GREEN, Suite and exact-path commit. Missing API/compiler errors do not count as behavioral RED: introduce only compiling contracts first, then demonstrate the wrong behavior. UI tasks add the actual control scenario/Build. Expected outcomes are contracts, not promised test counts. No per-task reviewer dispatches; Task17 owns the one final independent implementation review.

## File responsibility map

S=`src/OpenAntares.Simulation`; T=`tests/OpenAntares.Simulation.Tests`; G=`game`; V=`tests/OpenAntares.Game.Validation`. Paths below are repository-relative; braces enumerate files, not new folders to infer.

| Files | Responsibility / tasks |
| --- | --- |
| `tools/ShipDesignBaselineCapture/{ShipDesignBaselineCapture.csproj,Program.cs}`, `tools/ShipDesignValidation/Run-Gate.ps1`, `T/ShipDesignCompatibilityTests.cs`, `T/Fixtures/rules9-ship-design-baselines/`, `.gitattributes` | Untouched-engine futures, command decoding, preservation/gate wrapper;1/12/17 |
| `S/ShipDesigns/{ShipDesignContent,ShipDesignContentReader,ShipDesignContentWriter,ShipDesignMath}.cs`; `S/Content/{ContentSet,ContentLoader,ContentWriter}.cs`; `S/Rules.cs`; `content/core/ship_design.json` | Definitions, pure build calculation, gated technologies and strict embedding;2/4/16 |
| `S/ShipDesigns/{ShipDesignState,ShipDesignInitializer,ShipDesignStateValidator}.cs`; `S/State/{VesselState,GameState,GameStateValidator}.cs`; `S/Galaxy/GalaxyGenerator.cs` | Owner-local catalogue/job state, references, deep clones/init; EmpireState is declared within GameState.cs;3/16 |
| `S/ShipDesigns/{ShipDesignMath,VesselLoadouts,ShipDesignTargets}.cs` | One stat/cost/target identity implementation;4/6/10 |
| `S/ShipDesigns/{ShipDesignCommands,ShipDesignCommandProcessor,ShipDesignCatalogue}.cs`; `S/Commands/CommandProcessor.cs` | Save/archive/restore/delete and transaction dispatch;5 |
| `S/ShipDesigns/ShipDesignProduction.cs`; `S/State/ProductionTarget.cs`; `S/Economy/{VesselProduction,EconomyCalculator,EconomyResults}.cs`; `S/Commands/{CommandProcessor,DevelopmentCommandProcessor}.cs`; `S/Development/{GovernorPlanner,GovernorPlanningQueries,DevelopmentCompletion,DevelopmentStateValidator}.cs` | Pinned production/finite queue targets, forecasts, one completion;6/9 |
| `S/Fleets/{FleetState,FleetViews,AssemblyPlanner,AssemblyCommandProcessor,FleetPlanningQueries,FleetResolver,FleetStateValidator,OfficerBonuses,RequestQueueAuthority}.cs`; `S/Saves/FleetSaveData.cs` | Exact-design quotas, matching, price and immutable fulfillment history;7/12 |
| `S/ShipDesigns/{RefitCommands,RefitCommandProcessor,RefitQueries,RefitGuards,RefitEconomy,RefitResolutionContext,RefitResolver}.cs`; `S/Turns/TurnResolver.cs` | Refit commit/lock/forecast/provisional payment/late phase;8/9/11 |
| `S/Combat/{CombatInitializer,CombatStateValidator,CombatQueries,CombatResolutionContext,CombatRepair,CombatObservations}.cs`; `S/Fleets/{FleetTravel,OfficerBonuses,AssemblyPlanner}.cs`; `S/Movement/MovementResolver.cs`; `S/Scouting/{ScoutPlanningQueries,ScoutQueries,AmebaResolver}.cs` | Installed stats, freeze boundaries and unchanged M7 risk inputs;4/10/11 |
| `S/ShipDesigns/{ShipDesignViews,ShipDesignQueries,ShipDesignReports}.cs`; `S/Knowledge/{EmpireView,EmpireViewProjector}.cs` | Immutable own catalogue/jobs/reference counts/reports;5/8/13 |
| `S/Saves/{ShipDesignSaveData,SaveGame,ExpansionSaveData,DevelopmentSaveData,FleetSaveData}.cs` | Strict writer11/56-pair codec, omissions and historical facts;12 |
| `G/Components/{ShipDesignDialog,ShipDesignEditor,RefitDialog}.cs`; existing `FleetCommandDialog,VesselPanel,ConstructionQueueEditor,ColonyPanel,TurnReportDialog,ResearchBar`; `G/Screens/{GameScreen,NewGameScreen}.cs`, `G/ContentFiles.cs`, `G/theme/openantares_theme.tres` | Reachable catalogue/designer/build/request/refit and research;14/15/16 |
| `V/{ShipDesignGraphicalValidation.cs,ShipDesignGraphicalValidation.tscn,Run-Validation.ps1,Validation.targets,Verify-NormalAssembly.ps1,README.md}` | Opt-in tenth scenario, current campaigns and normal exclusions;14..17 |

No broad rewrite of existing subsystems. New feature code lives in `S/ShipDesigns`; minimal adapters extend established files. All new files/methods below are plan decisions; modifying an existing path requires reading it fresh, especially the shared theme.

## Shared interfaces and ownership

New simulation types use `OpenAntares.Simulation.ShipDesigns`. All public view arrays are ImmutableArray and dictionaries use ordinal keys; mutable state lists never escape. IDs are comparable record structs `DesignId(int Value)` and `RefitId(int Value)`, local to an empire, never the global entity allocator. Use checked positive Int32 counters and positive Int64 revisions/events. Names use Trim and StringComparer.OrdinalIgnoreCase.

- Task2: `ShipRecipe(string FrameId,string EngineId,string? ArmorId,ImmutableArray<string> WeaponBankIds)`; immutable `ShipFrameDefinition(Id,Name,Hull,Space,Cost,TravelPercent,ImmutableArray<string> Prerequisites)`; `ShipComponentDefinition(Id,Name,Kind,Space,Cost,Value,ImmutableArray<string> Prerequisites)` with enum Engine/Weapon/Armor; `ShipDesignDefinition` owns configured WarshipRoleId, StartingRecipe and ordinal frame/component dictionaries. Cost is Int64; stats/space/value are Int32. Declare `DesignStats(long BuildCost,int MaxHull,int Attack,int Armor,int BaseSpeed,int SpaceUsed,int SpaceCapacity)` and implement `ShipDesignMath.Calculate(ShipDesignDefinition,ShipRecipe):DesignStats` here because the content loader needs it to validate the starting recipe. Add `ContentSet.ShipDesigns` nullable. Add `TechnologyDefinition.ShipDesignOnly {get;init;}` defaultfalse and a loader `requireShipDesigns=false` option.
- Task3: immutable `ShipDesign(DesignId Id,string Name,ShipRecipe Recipe,bool Archived)`; `ShipDesignEmpireState` has NextDesignId/NextRefitId, CatalogueRevision, NextEventId, List<ShipDesign> Designs, List<RefitJob> Refits, PendingEvents/RecentEvents (last20). `RefitJob` has Id,Vessel,Port,SourceDesign,TargetDesign,AcceptedTurn,Revision,Price,Status Active/Paused,Reason and deep Clone. Add nullable `EmpireState.ShipDesigns` and `VesselState.Design`; all other feature fields are under that owner state. Completed/cancelled jobs leave copied event facts, not live job rows. `ShipDesignInitializer.Initialize(ContentSet,GameState)` is generation-only; codecs never repair missing references by calling it.
- Task4: consume Task2's Calculate without duplicating it; add `ShipDesignMath.RefitPrice(long):long`; `VesselLoadouts.Resolve(ContentSet,GameState,VesselState):VesselLoadout` and `Resolve(ContentSet,EmpireView,VesselId):VesselLoadout`. `VesselLoadout` carries RoleId, optional DesignId/FrameId, BuildCost, BaseSpeed and optional CombatProfile. Legacy paths return the existing role-based facts unchanged. `VesselLoadouts.CombatProfile(ContentSet,GameState,VesselState):CombatProfile` requires combat rules and a nonnull resolved profile.
- Task5: `ShipDesignCommand(EmpireId Issuer,int ExpectedTurn,long CatalogueRevision,ShipDesignAction Action):Command`. Actions `Save(string Name,ShipRecipe Recipe)`, `Archive(DesignId)`, `Restore(DesignId)`, `Delete(DesignId)`. `ShipDesignCommandProcessor.Execute(ContentSet,GameState,ShipDesignCommand):CommandOutcome`. `ShipDesignCatalogue.References(GameState,EmpireId,DesignId):DesignReferences` returns counts for vessels, active/queued targets, retained request slots and refits. `ShipDesignQueries.PreviewDraft(ContentSet,EmpireView,string,ShipRecipe):DesignPreview` reports allowed/reason, stats, space and missing prerequisites, without modifying state.
- Task6: `ProductionKinds.ShipDesign="ship_design"`, `Refit="refit"`. `ShipDesignTargets.Build(DesignId):ProductionTarget` and `Refit(RefitId):ProductionTarget` serialize a positive ID as invariant decimal ContentId, without a content-ID namespace. `Normalize(ContentSet,GameState,EmpireId,ProductionTarget):ProductionTarget` resolves a role-only configured warship target to pinned Standard Escort at command acceptance. `VesselCompletionPlan` gains optional DesignId; created warships bind it before combat initialization. `ShipDesignProduction.Calculate(ContentSet,GameState,ColonyState,ColonyEconomy):ColonyEconomy` handles the new-build kind without duplicating ordinary reserve mathematics.
- Task7: `AssemblyQuota(string RoleId,DesignId? Design)` and `AssemblyDraft.Quotas {get;init;}:ImmutableArray<AssemblyQuota>`. Default/absent Quotas means normalize existing Roles to civilian roles or pinned Standard Escort; if both are populated they must normalize to the same ordered slots or reject. `AssemblySlot`, `AssemblySlotPlan`, `AssemblySlotView` and own `FleetShipView` gain optional DesignId. Canonical accepted slots always pin designs for warships. Add `OfficerBonuses.RequestShipCost(ContentSet,GameState,ColonyId,ProductionTarget):long` and internal `RequestShipCost(IFleetPlanningContext,AssemblyDraft,ColonyId,AssemblyQuota,RequestId? replacing):long`; FleetPlanningQueries delegates to the latter. Retain existing role overloads for old rules/civilian adapters.
- Task8: `RefitCommand(EmpireId Issuer,int ExpectedTurn,RefitAction Action,string Token):Command`; actions `Start(VesselId Vessel,ColonyId Port,DesignId Target)`, `Cancel(RefitId Job,long Revision)`, `Resume(RefitId Job,long Revision)`. `RefitQueries.Preview(ContentSet,EmpireView,RefitAction):RefitPreview`; `RefitCommandProcessor.Execute(ContentSet,GameState,RefitCommand):CommandOutcome`; `RefitGuards.Conflicts(EmpireView,Command):bool`. Preview contains Allowed/RejectionCode/Token, job/ship/port/source/target identity, old/target stats, price, estimate and the specific locks/manual-pause consequences. Tokens hash canonical own material facts, turn and relevant job revisions; never hidden foreign state or GetHashCode.
- Task9: `RefitPlan(RefitId Job,EmpireId Owner,VesselId Vessel,ColonyId Port,DesignId Source,DesignId Target,long Price,bool Covered)`; nullable `ColonyEconomy.RefitPlan {get;init;}`; `RefitEconomy.Calculate(ContentSet,GameState,ColonyState,ColonyEconomy):ColonyEconomy`. `RefitResolutionContext.Capture(ContentSet,GameState):RefitResolutionContext` captures all committed jobs, Active and Paused, before economy; only eligible Active jobs can have Covered=true. `RefitApplyResult(TurnEconomy Economy,ImmutableArray<ShipDesignEvent> Events)` is returned by `RefitResolver.Apply(ContentSet,GameState snapshot,GameState candidate,RefitResolutionContext,TurnEconomy):RefitApplyResult`. Add internal `ShipDesignResolutionPhase {Capture,Economy,PostLoss,Refit,Observation,Validation}` hooks for failure injection through `TurnResolver.ResolveShipDesigns(ContentSet,GameState,Action<ShipDesignResolutionPhase>):TurnResolution`. Production first banks output for a refit, reserving the colony completion slot; only Apply posts a payment and changes equipment. TurnReport uses the adjusted actual economy, not a forecast charge that never occurred.
- Task13: `ShipDesignEmpireView` owns CatalogueRevision, immutable DesignViews, RefitViews and copied events. `DesignView` has recipe/stats/archive/unlocks/reference counts; `RefitView` has all owner-visible preview/job/blocked facts. `ShipDesignQueries.Catalogue(ContentSet,EmpireView,int Page,bool Archived,string? Filter):DesignPage` validates/clamps nonnegative pages and returns16 rows with total/pages/current page. `ShipDesignReports.Finalize(ContentSet,GameState,GameState,ImmutableArray<ShipDesignEvent>)` bounds owner events20; historical summaries contain copied names/frame/stats and never require a retained catalogue record.

Task3 adds `T/ShipDesignFixtures.cs`: `Session(ImmutableArray<string> known=default,string species="harl",bool sentries=false):GameSession`, creating an explicitly labelled pre-session rules10 Small/seed42 fixture with valid owner/port and known-tech metadata. Task5 later adds `SaveDesign(GameSession,string,ShipRecipe):DesignId`, which always issues that task's real command. Economical/loss/limit fixtures may customize valid state before the session is published and must be labelled supplements. The generated runtime campaigns in Task16 cannot call these helpers or inject earned outcomes.

DTO field decisions: `DesignReferences(int Vessels,int ActiveProjects,int QueueEntries,int RequestSlots,int Refits)`; `DesignPreview(bool Allowed,string? RejectionCode,DesignStats? Stats,ImmutableArray<string> MissingTechnologyIds)`; `DesignPage(ImmutableArray<DesignView> Rows,int Page,int TotalPages,int TotalRows)`. `EquipmentFact(DesignId Id,string Name,ShipRecipe Recipe,DesignStats Stats)` and `ShipDesignEvent(EmpireId Owner,long Id,int Turn,string Kind,string Reason,VesselId? Vessel,ColonyId? Port,EquipmentFact? Before,EquipmentFact? After,long Paid)` are declared with Task3 state so history has no undefined dependency on Task13. RefitPreview fields use nullable job/ship/port/source/target IDs and nullable old/target DesignStats/price/turn estimate for rejected actions, plus Allowed, RejectionCode, Token and ImmutableArray<string> Consequences. Add fields to these immutable records only for a demonstrated approved view need; do not change their meanings between tasks.

## Task 1: Freeze genuine rules9 behavior and preservation

**Files:** Create capture project/gate wrapper, `T/ShipDesignCompatibilityTests.cs`, `T/Fixtures/rules9-ship-design-baselines/`; modify `.gitattributes` only for raw frozen fixtures and new execution evidence.

**Interfaces:** Consume unchanged writer10/rules9 `SaveGame`, GameSession and actual current command types. Produce immutable manifest with source/product SHA, raw file hashes, classifications, serialized typed commands and canonical future checkpoints3/10; `Run-Gate.ps1 -Name -OutputRoot -Command -Arguments` preserves historical latest reports and retains unique attempt logs/JSON/relocations.

- [ ] **Step1 entry/RED:** Record main/index/refs/worktrees/stashes/owner/QA hashes and untouched Suite through the gate wrapper before adding the manifest test. `FrozenRules9SourcesAndFuturesExistBeforeProductChanges` must fail because no genuine archive exists, not because of a missing API.
- [ ] **Step2 capture:** Copy all72 current generated Combat saves (12 species/size campaigns × start/pending/active/return/repair/cleared) from `reports/combat-integration-20261029/combat-01/generated/`; assert rules9/writer10 and actual hashes. Run `dotnet run --project tools/ShipDesignBaselineCapture/ShipDesignBaselineCapture.csproj -- --sources reports/combat-integration-20261029/combat-01/generated --output tests/OpenAntares.Simulation.Tests/Fixtures/rules9-ship-design-baselines`; first use may restore this new tool with the ordinary audit enabled. For each, run the untouched current engine through ten boundaries, recording required acknowledgements/withdrawal/ordinary orders via real commands and checkpoints3/10. Record all72 generated sources/144 generated checkpoints. Add labelled old-engine supplemental cases for Off, live project/queue/request/paused request/officer, plague/cargo/ameba, first-human Ready and renamed embedded armed role; retain a named coverage manifest and actual supplemental counts. Never upgrade an old source or call a future new implementation to produce its oracle.
- [ ] **Step3 assert GREEN:** Replay the frozen typed transcripts (including CombatActionCommand nested actions/revisions/tokens, Fleet/Assembly/Officer commands) with the untouched engine; compare canonical content/state after removing only schema_version. Representative assertion: `Assert.Equal(9, loaded.State.RulesVersion); Assert.Equal(expectedFuture, CanonicalWithoutSchema(actual));` Every raw file SHA must match its manifest. Capture failures stay as separate attempts.
- [ ] **Step4 gate:** Focused(ShipDesignCompatibilityTests), Suite and byte-hash closing audit. Ensure no `src/`, `game/`, `content/` or old fixture/report byte changed before the freeze is accepted; stop product work if it cannot be established. Exclude no defined source silently.
- [ ] **Step5 commit:** Stage only capture/gate/test/new frozen files and archive attributes; commit `test: freeze genuine rules9 ship-design baselines`.

## Task 2: Add inactive ship content, rules gates and research definitions

**Files:** Create ShipDesignContent/Reader/Writer and ShipDesignMath.Calculate; modify ContentSet/Loader/Writer, Rules, TechnologyDefinition and `content/core/ship_design.json`; create `T/ShipDesignContentTests.cs`.

**Interfaces:** Produce Task2's definitions and `Rules.ShipDesignVersion=10`, `HasShipDesigns(version)`, loader option/technology flag. Extend every explicit older feature predicate/IsSupported to include10 while retaining `CurrentVersion=9` until Task16.

- [ ] **Step1 RED:** `CoreShipContentMatchesApprovedTables` loads the new section and asserts `(100,6,1200,100)` Escort, `(180,10,2400,75)` Frigate; component `(space,cost,value)` `(0,400,300)`, `(0,800,400)`, `(3,600,24)`, `(4,1000,36)`, `(2,200,4)`, `(3,500,8)`; technology costs2400/2400/3600/3600 and exact spec prerequisites. Also test missing recipe/binding, undefined prerequisite, optional advanced entries omitted together, duplicate IDs, invalid stats, overflow and renamed configured warship role. All four tech definitions are ShipDesignOnly.
- [ ] **Step2 verify RED:** Focused(ShipDesignContentTests) fails on missing/incorrect parsed definitions with compiling contracts.
- [ ] **Step3 implement:** Strict EntryReader-style diagnostics and unknown-field rejection. Implement the one pure Calculate method with checked Int128 sums, component kinds/count/space and frame-speed factor so the loader can compare the starting recipe against the declared role profile/cost/speed without a later-task dependency. Starting role must be armed, zero-embark and non-founding; starting recipe legal/unlocked. Gate research selection as well as UI availability. Old content writing omits new definitions/tech flag/technologies entirely; current disk content may contain them without exposing them to an old campaign. Invalid disk content does not remove Load recovery.
- [ ] **Step4 GREEN:** Focused + Suite. `Assert.False(Rules.TechnologyAvailable(core,9,"vector_drives")); Assert.True(Rules.TechnologyAvailable(core,10,"vector_drives")); Assert.Equal(9,Rules.CurrentVersion);` Verify every feature predicate supports10 and old1..9 remains exact.
- [ ] **Step5 commit:** Exact content/rules/test paths; `feat: define gated ship-design content and research`.

## Task 3: Add bounded owner state, cloning and generation-only initialization

**Files:** Create ShipDesignState/Initializer/StateValidator and test fixture; modify EmpireState within `S/State/GameState.cs`, VesselState/GameStateValidator/GalaxyGenerator; create `T/ShipDesignStateTests.cs`.

**Interfaces:** Produce Task3 state/IDs and `ShipDesignStateValidator.Validate(ContentSet,GameState):IReadOnlyList<string>`. Keep explicit rules10 creation available for tests without changing current default.

- [ ] **Step1 RED:** `Rules10InitializesOnlyPermanentStandardEscort` covers4 species ×3 sizes, Standard/Off and alternative declared role. Exactly one active DesignId1, NextDesignId2, empty jobs, no extra vessel/reserve/known technology; own recipes private. `CloneDoesNotShareJobsOrCatalogueLists` mutates cloned lists/job reason and asserts the original canonical state is unchanged. Reject absent10 catalogue, extra old-rule state, duplicate owner-local IDs, invalid revision/counters and duplicate port/ship jobs.
- [ ] **Step2 verify RED:** Focused(ShipDesignStateTests) demonstrates absent initialization or shared clone mutation.
- [ ] **Step3 implement:** Initialise before any rules10 warship combat fields; generation-only assignment is not a save repair. Preserve old global-ID/RNG ordering: catalogue IDs allocate locally and create no actor. Catalogue recipes are immutable; jobs/state clone deeply. Enforce bounds32/256/name40 and mandatory design binding only for configured rules10 warships; civilians remain null. More detailed cross-reference/profile/job validation is added with its owning task, without accepting an invalid published state between tasks.
- [ ] **Step4 GREEN:** Focused + Suite. `Assert.Single(owner.ShipDesigns.Designs); Assert.Null(oldOwner.ShipDesigns); Assert.Equal(beforeGlobalId,afterGlobalId);` Repeat seeds and compare complete state/RNG.
- [ ] **Step5 commit:** `feat: initialize private ship-design campaign state`.

## Task 4: Expose exact equipped profiles and checked refit pricing

**Files:** Create VesselLoadouts; extend ShipDesignMath with RefitPrice, modify CombatStateValidator and add required immutable own loadout facts in ShipDesignViews/EmpireViewProjector; create `T/ShipDesignMathTests.cs`.

**Interfaces:** Produce Task4 calculations/overloads. Legacy role-based resolution is unchanged; rules10 custom recipes resolve through owning catalogue and embedded content, never a disk/global latest design.

- [ ] **Step1 RED:** `ApprovedLoadoutsHaveExactStats` resolves valid labelled pre-session equipped ships through the new state/view resolver and asserts Standard `(2400,100,24,4,300,5,6)`, Fast `(2800,100,24,4,400,5,6)`, Gun `(2800,100,48,0,300,6,6)`, Armored Frigate `(4900,180,60,8,225,10,10)`. Reuse Task2 capacity/multiplicity tests: Escort lance+reinforced7/6 and Frigate two-lance+reinforced11/10 reject, repeated pulse allowed. `RefitPriceUsesCheckedCeiling` asserts700/1225, the formula minimum1 for0, negative-input rejection and `RefitPrice(long.MaxValue)==2305843009213693952L` without false intermediate overflow. Truly unrepresentable aggregate design costs/stats reject separately. `LegacyLoadoutIsByteStable` pins old source role stats. The behavioral RED must include the new resolver/refit-price behavior, not demand an already-passing Calculate test fail again.
- [ ] **Step2 verify RED:** Focused(ShipDesignMathTests), using only compiling method/record contracts.
- [ ] **Step3 implement:** Int128 intermediates and checked final ranges; enforce component kind/multiplicity/space, sums then one profile, speed factor then M9 damage then Navigator. Update state validation to use equipped max hull, including Frigate180; do not normalize current damage on read. Add exactly the own view facts needed by the view overload; no mutable GameState lookup or foreign catalogue in a query.
- [ ] **Step4 GREEN:** Focused + Suite. `Assert.Equal(225,damagedStandardSpeed); Assert.Equal(150,crippledStandardSpeed); Assert.Equal(46,healthyGunShotAgainstArmor2);` Include reordered dictionaries/banks, alternative IDs and checked bounds. Task10 owns applying this resolver at every existing behavior call site.
- [ ] **Step5 commit:** `feat: calculate immutable ship loadout capabilities`.

## Task 5: Implement catalogue commands, immutable recipes and reference safety

**Files:** Create ShipDesignCommands/CommandProcessor/Catalogue and initial Queries; extend Task4's Views, CommandProcessor/EmpireViewProjector and ShipDesignFixtures.SaveDesign; create `T/ShipDesignCatalogueTests.cs`.

**Interfaces:** Consume Task3/4; produce Task5 command/preview/reference contracts and `ShipDesignFixtures.SaveDesign`.

- [ ] **Step1 RED:** `SaveCopyArchiveRestoreDeleteRespectIdentity` uses real commands: saving allocates one ID, Copy creates a distinct recipe/identity, no edit mutates the original. Test case-insensitive trimmed names, invalid40/41 lengths,32nd/33rd active and256th/257th retained, permanent starter refusal, referenced deletion refusal and unreferenced archived deletion without ID reuse. `IdenticalValidSaveDoesNotAllocateOrClearReady` is a no-op only with a current catalogue revision and identical retained name/recipe; stale replay rejects unchanged.
- [ ] **Step2 verify RED:** Focused(ShipDesignCatalogueTests).
- [ ] **Step3 implement:** Clone-and-validate dispatcher path, checked revisions/counters and shared references index. Unavailable technology/invalid recipe rejects before allocating. Archive affects selection only. An unrelated foreign catalogue is absent from tokens; no new RNG/global IDs. Limit/publication rejections preserve Ready, even at Int32/Int64 bounds.
- [ ] **Step4 GREEN:** Focused + Suite, asserting canonical equality/readiness/counters/RNG on every reject/no-op and old recipe equality after saving a copy.
- [ ] **Step5 commit:** `feat: add bounded immutable ship-design catalogue`.

## Task 6: Pin design identity through colony production and finite queues

**Files:** Create ShipDesignTargets/Production; modify ProductionTarget, VesselProduction/EconomyCalculator/EconomyResults, CommandProcessor/DevelopmentCommandProcessor, GovernorPlanner/PlanningQueries/Completion/StateValidator and TurnResolver completion binding; create `T/ShipDesignProductionTests.cs`.

**Interfaces:** Produce Task6 target normalization and completion DesignId. Change combat vessel initialization to the canonical `CombatInitializer.InitializeVessel(ContentSet,GameState,VesselState)` overload; retain any legacy int-version overload only for explicit <=9 callers and reject its use for custom10 warships.

- [ ] **Step1 RED:** `BuildQueuePaysAndPinsExactDesign` queues two Gun Escorts and one civilian; costs2800 each, repeat count finite, identity survives reordering/normalization and one completion per colony. `ArchiveAfterCommitStillFinishesPinnedRecipe` archives a queued target then verifies completion while new selection rejects. `RoleOnlyAliasPinsStandardAtAcceptance` resolves only the configured warship alias, including alternative IDs; persisted ambiguity is invalid.
- [ ] **Step2 verify RED:** Focused(ShipDesignProductionTests).
- [ ] **Step3 implement:** Extend typed target validation/forecast/queue selection and completion binding once. Manual takeover pauses construction only and preserves workforce/template/queue/reserve. Buildings-only governor templates and portable presets reject ship/refit recipes; no automatic choice of warship. Set DesignId before max-hull/cohesion initialization; new ships cannot depart/fire/complete another project in their birth resolution.
- [ ] **Step4 GREEN:** Focused + Suite. `Assert.Equal(2800,receipt.TargetCost); Assert.Equal(gunId,newShip.Design); Assert.Equal(100,newShip.Combat.Hull); Assert.Equal(100,newShip.Combat.Cohesion);` Verify active manual project retention,32 queue cap, exact consumed token, enough reserve for many ships still one completion, and failed selection allocates/spends nothing.
- [ ] **Step5 commit:** `feat: build pinned ship designs through finite queues`.

## Task 7: Extend finite fleet requests and Organizer pricing to exact designs

**Files:** Modify FleetState/Views, AssemblyPlanner/CommandProcessor, FleetPlanningQueries, FleetResolver/StateValidator, OfficerBonuses and RequestQueueAuthority; create `T/ShipDesignRequestTests.cs`.

**Interfaces:** Produce Task7 quotas/design fields and target-aware price overloads; existing role-only APIs remain explicit Standard/civilian adapters.

- [ ] **Step1 RED:** `RequestsMatchExactDesignWithoutSubstitution` distinguishes Standard and Gun idle hulls sharing a role; accept only matching selected designs. Reject contradictory Roles/Quotas, stale cross-page candidate, wrong owner and archived new quota. `OrganizerDiscountAppliesOnceToWholeRecipe` asserts Standard2280/2160, Gun2660/2520. `CompletedRequestSurvivesLaterRefitWithoutNewXp` initially pins its historical slot contract; Task9 completes the refit half.
- [ ] **Step2 verify RED:** Focused(ShipDesignRequestTests).
- [ ] **Step3 implement:** Carry pinned IDs through proposals, queue tokens, active-project/reserve simulation, bindings, fulfillment, pause/resume/amend/cancel/rally and safety loss. Normal/Urgent cannot replace active work or refits. Editing an unstarted quota cannot mutate built/committed hulls. Completed request slots are historical original design facts; active bindings require current exact equality. Preserve all original XP/coordinator/forfeiture rules and32-row candidate paging/selection disclosures.
- [ ] **Step4 GREEN:** Focused + Suite; assert no extra ship, partial quota edit, repeated XP or repriced completion. Include all4 species/cargo choices, alternative role IDs, manual queue takeover, refund-free cancellation, missing slot/no automatic replacement and current officer/Wenth recovery regressions.
- [ ] **Step5 commit:** `feat: bind fleet requests to exact ship designs`.

## Task 8: Add refit previews, acceptance, cancellation and command locks

**Files:** Create RefitCommands/CommandProcessor/Queries/Guards; modify shared CommandProcessor, relevant Fleet/Officer/Assembly/Combat dispatch guards and ShipDesignStateValidator; create `T/RefitCommandTests.cs`.

**Interfaces:** Produce Task8 action/preview/guard contracts; jobs are actual own planning state, with price frozen on Start and relevant revisions/tokens only.

- [ ] **Step1 RED:** `StartRefitRequiresSameFrameFullConditionAndIdlePort` tests legal Standard->Fast700 and rejects cross-frame, hull99/100, cohesion99, cargo, wrong owner/port, active project, same equipment under another ID, archived/locked target, transit/pending move/founding/ameba/combat/return/repair/active request. `RefittingFlagshipKeepsOfficerButLocksAffectedCommands` covers fleet Hold/flagship/officer/membership, multi-selection atomicity and no teleport. `HiddenRivalRecipeDoesNotChangeOwnPreview` checks token/view equality under foreign changes.
- [ ] **Step2 verify RED:** Focused(RefitCommandTests), including a valid alternate-role recipe so a core-ID guard cannot falsely pass.
- [ ] **Step3 implement:** Start allocates one job, sets the manual refit target, pauses construction only, preserves untouched queue/template/reserve and pays/installs nothing. Current-revision identical Start is a no-op; different Start rejects until Cancel. Cancel preserves old equipment/banked reserve, removes only that project/job and leaves Hold/construction paused. Resume uses a fresh preview. Guard ordinary SetProject/production/queue/resume/move/fleet/assembly/officer/combat/scout paths before any implicit takeover; unaffected legal members may split out. No implicit cancellation through another command.
- [ ] **Step4 GREEN:** Focused + Suite. Assert source/target/port/turn/cost changes stale the preview; unrelated hidden facts do not. Rejections/no-ops preserve IDs/readiness/RNG and whole selection; successful changes clear only issuer readiness. Direct shared processor calls must match UI behavior.
- [ ] **Step5 commit:** `feat: commit refit jobs with explicit ship and port locks`.

## Task 9: Resolve refits with provisional payment and actual report accounting

**Files:** Create RefitEconomy/ResolutionContext/Resolver; modify EconomyCalculator/EconomyResults, TurnResolver, DevelopmentCompletion and ShipDesignReports; create `T/RefitResolutionTests.cs`.

**Interfaces:** Produce Task9 snapshot/plan/apply-result contracts; all job processing is stable OwnerId/RefitId order and each port owns one completion slot.

- [ ] **Step1 RED:** `RefitBanksThenPaysOnceAfterFullInterval` arranges the labelled port fixture at turn10, reserve300/output200, Standard->Fast700. Assert acceptance changes no money/equipment; turn11 reserve500/old speed300; turn12 reserve0/new speed400, same ship/hull/cohesion/flagship/officer. Cancel at11 instead leaves500/Standard. Assert1225 ceiling, zero-output covered reserve still requires a full snapshot interval, and no arrival/completion tick. `ArchiveAfterCommitStillFinishesPinnedRecipe`, `CompletedRequestSurvivesLaterRefitWithoutNewXp` and `RefitLateInvalidationDoesNotSpendOrDoubleComplete` exercise their complete end-to-end outcomes.
- [ ] **Step2 verify RED:** Focused(RefitResolutionTests) fails on premature spend/install, lost reserve or missing completion.
- [ ] **Step3 implement:** Capture before economy; RefitEconomy banks output and supplies a provisional plan while reserving the slot. Paused jobs still occupy that slot and are captured for missing-ship cleanup but cannot pay/install until explicit Resume. Run Apply after CombatRepair.Apply/ObserveArrivals and before DevelopmentCompletion/turn increment/ObservationUpdater. Recheck surviving ship, old equipment, current eligibility/job/port and actual reserve against frozen price. Only successful Apply deducts once, changes DesignId and clears the job/manual target. Return adjusted TurnEconomy so reserve breakdown/completion reports reconcile exactly. A missing ship cancels with one Attention fact/no payment; other supported eligibility loss pauses/no payment. An archived committed target remains legal. Do not complete another queued/building target when a refit pauses or cancels that boundary.
- [ ] **Step4 GREEN:** Focused + Suite. `Assert.Equal(beforeBankedReserve,pausedPort.ProductionReserve); Assert.Null(pausedReport.CompletedId); Assert.Equal(originalRequestXp,afterRefitXp);` Test two colonies at one star, stable job ordering, active/paused save-ready shape, manual construction remains paused, no repair/XP/recovery effect and no double completion/replay payment. Retain ordinary finite repair behavior.
- [ ] **Step5 commit:** `feat: finish paid refits after a full stationary interval`.

## Task 10: Route equipped profiles through combat, travel, repair and own forecasts

**Files:** Modify CombatInitializer/StateValidator/Queries/ResolutionContext/Repair/Observations, FleetTravel/OfficerBonuses/AssemblyPlanner, MovementResolver, GovernorPlanningQueries, ScoutPlanningQueries/ScoutQueries/AmebaResolver and own view projection; create `T/ShipLoadoutIntegrationTests.cs`.

**Interfaces:** Consume Task4 resolver; produce no second stat implementation. Capture installed DesignId/recipe/stats in immutable encounter/launch frames or event facts where required; existing saved receipts remain their original facts.

- [ ] **Step1 RED:** `LoadoutCallSitesAgreeWithoutChangingAmebaOdds` tests Standard/Fast/Gun/Frigate live views, one volley, withdrawal, fleet-minimum travel, ameba travel forecasts, repair and request travel/cost estimates. Gun attack48 yields one46-damage shot against armor2. Frigate max180 repairs45 hull/full interval and shares M9 percentage bands; full Standard repairs25. Fast has installed base400 for travel/sentry escape; M7 risk queries/execution still agree on their established role input300. `RefitNeverRepricesCommittedTripOrPastVolley` pins old duration/receipts and changes only subsequent snapshots.
- [ ] **Step2 verify RED:** Focused(ShipLoadoutIntegrationTests) identifies actual role-profile fallback discrepancies; do not merely test the new helper in isolation.
- [ ] **Step3 implement:** Inventory every `RoleProfiles[...]`, role `.Speed`/`.Cost` use in affected systems with `rg`; classify as equipped behavior, build recipe, civilian legacy or intentional M7 risk source. Adapt equipped behavior once through VesselLoadouts. Carry own immutable design facts through GovernorPlanningQueries.OwnFrame and FleetPlanningQueries so forecast reconstruction does not drop identities. Keep cargo/founding eligibility role-bound, peaceful AI policies unchanged and committed travel/volley cohorts frozen. No whole-world/foreign catalogue access in preview contexts.
- [ ] **Step4 GREEN:** Focused + Suite, all current Combat/Fleet/Scout regressions, off/alternative-role sources. Assert identical uninterrupted/reloaded results and stable RNG counts. Store the audited call-site list with categories in task evidence; searches alone are not behavioral proof.
- [ ] **Step5 commit:** `feat: use installed loadouts across strategic ship behavior`.

## Task 11: Harden command, Ready and late-phase atomicity

**Files:** Modify CommandProcessor, RefitGuards/Resolver/ResolutionContext, ShipDesignStateValidator, TurnResolver and existing consent reconciliation; create `T/ShipDesignAtomicityTests.cs`.

**Interfaces:** Finalize the six ShipDesignResolutionPhase hooks and direct-Resolve/Ready validation. New ShipDesignCommand/RefitCommand join post-command full-state validation after the existing consent reconciliation, without bypassing scout/combat response authority.

- [ ] **Step1 RED:** `FinalReadyRefitFailureIsAtomic` injects failure at Capture/Economy/PostLoss/Refit/Observation/Validation and compares original state/readiness/IDs/queues/reserves/catalogue/jobs/all RNG streams. Repeat through ordinary final-human Ready and direct Resolve. `RefitLateInvalidationDoesNotSpendOrDoubleComplete` tests missing ship, changed source equipment and invalid/insufficient reserve without fake successful receipts. Test counter overflow, unsupported target kind, multi-target command with one locked ship and cancelled-job stale tokens.
- [ ] **Step2 verify RED:** Focused(ShipDesignAtomicityTests), using valid full-condition fixtures and targeted injected faults rather than invalid setup that fails before the intended phase.
- [ ] **Step3 implement:** Enforce all loadout/job invariants at command/publication, direct Resolve, Ready and writes. A new refit does not auto-acknowledge ameba/combat safety. Preserve first-human planning when another is not Ready and reject material shared conflicts without exposing foreign identity. Checked arithmetic/validation failure returns original state and the triggering Ready remains undone. No duplicate event IDs, catalogue IDs, payments, XP or restored deleted jobs on retry.
- [ ] **Step4 GREEN:** Focused + Suite. `Assert.Equal(beforeCanonical,afterFailureCanonical); Assert.Equal(beforeReady,afterFailureReady);` Include unchanged valid no-op fingerprints and all original failure-hook regressions; no production fixture invariant may be weakened to get a green test.
- [ ] **Step5 commit:** `test: enforce atomic ship-design and refit boundaries`.

## Task 12: Add strict writer11 persistence and prove genuine old continuation

**Files:** Create `S/Saves/ShipDesignSaveData.cs`; modify SaveGame, ExpansionSaveData, DevelopmentSaveData, FleetSaveData, ShipDesignContentWriter and ContentWriter; create `T/ShipDesignSaveTests.cs`, extend ShipDesignCompatibilityTests.

**Interfaces:** `ShipDesignSaveData.WriteEmpire(Utf8JsonWriter,ShipDesignEmpireState?,int rules)`, `ReadEmpire(EntryReader,EmpireState,int rules)`; `WriteVessel(Utf8JsonWriter,VesselState,int rules)`, `ReadVessel(EntryReader,VesselState,int rules)`. Integrate with existing strict codecs rather than invoking generation initializers after load. The writer emits schema11; reader explicitly enumerates the56 pairs.

- [ ] **Step1 RED:** `All56PairsAndOnlyThosePairsLoad` asserts prior46 plus11/1..10;10/10 rejects. `CatalogueQueueRequestAndRefitRoundTripContinueExactly` covers active/archive/deletable records, direct/queued targets, active/completed request original identity, jobs Active/Paused/insufficient reserve and first-human Ready. Reuse the Task1 expected old futures unchanged. Each supported phase runs three uninterrupted vs save/reload future turns.
- [ ] **Step2 verify RED:** Focused(ShipDesignSaveTests) + Focused(ShipDesignCompatibilityTests). The old futures must fail only for a real new-engine discrepancy; do not recapture or normalize anything except schema_version.
- [ ] **Step3 implement:** Require all M10 state/content under10 including sentries Off; omit/reject M10 fields/sections/technology flags under1..9 save layouts, while normal in-memory current disk content can still be used to explicitly start an older-rule test. Reject duplicate/dangling/foreign design references, recipe/cost/stat mismatch, unknown fields/enums, old ambiguous role-only custom targets, unearned unlocks, invalid archived new choices, contradictory jobs, invalid counters/bounds and duplicate port/ship jobs. Copied historical equipment facts include the recipe/stat snapshot and validate against embedded components without requiring a live catalogue row. An archived already-bound recipe is legal; completed historical request identity does not require current equipped equality.
- [ ] **Step4 GREEN:** Focused + Suite; verify every frozen raw hash,72/144 generated source/checkpoint claims and actual labelled supplements, plus all earlier1..8 archives. Corrupt read/write and replace failure preserve prior session/file; failed commands retain bytes across round trips. Embed altered disk content after loading to prove equipment/recipe/research independence. Include alternative role/component names and saved Frigate damage/band boundaries.
- [ ] **Step5 commit:** `feat: persist versioned ship designs and refit jobs`.

## Task 13: Complete private catalogue/job queries, reference counts and reports

**Files:** Complete ShipDesignViews/Queries/Reports; modify EmpireView/Projector, TurnReport/EmpireTurnReportView and own Fleet views; create `T/ShipDesignQueryTests.cs`.

**Interfaces:** Produce Task13 immutable pages/events and RefitView fields consumed by UI. Views include only own design/refit references; foreign vessel knowledge stays existing role/position facts.

- [ ] **Step1 RED:** `CataloguePagesAndReferencesRemainStable` arranges32 active/256 retained records and checks16-row pages, filter reset, zero/oversized page clamping, archive/delete disabled reasons and all reference classes. `HiddenRivalRecipeDoesNotChangeOwnPreview` compares own view JSON/tokens before/after hidden rival catalogue/technology/reserve/job changes. `CompletedRefitReportKeepsOriginalFactsAfterArchiveAndDelete` verifies copied recipes/labels/stats remain without a live record; events are bounded20, grouped/deduplicated and owner-only.
- [ ] **Step2 verify RED:** Focused(ShipDesignQueryTests).
- [ ] **Step3 implement:** Query reference counts once per permitted frame without using hidden world data. Refit reasons distinguish inactive port, insufficient reserve, paused, completed and cancelled; cancellation/completion exposes no fictional further authority. Use actual adjusted economic results for paid/unpaid reports. Update own research/build-picker availability; foreign observations do not gain installed components or research. Exclude irrelevant hidden state from material tokens.
- [ ] **Step4 GREEN:** Focused + Suite. Assert immutable-array results cannot mutate state; cloned own-only planning frames produce the same cost/time/profile as actual resolution. Test multiple humans, archived bound targets, duplicate names differing only by case, no-colony/empty-catalogue pages where allowed and ordinary old-rule UI view omission.
- [ ] **Step5 commit:** `feat: explain private ship designs refits and references`.

## Task 14: Build the real catalogue and component designer with opt-in runtime checks

**Files:** Create `G/Components/ShipDesignDialog.cs`, `ShipDesignEditor.cs`, `V/ShipDesignGraphicalValidation.cs/.tscn`; modify GameScreen/FleetCommandDialog, shared theme minimally and all validation runner/target/metadata files; update V/README.

**Interfaces:** `ShipDesignDialog(ContentSet,EmpireView)` emits `Action<ShipDesignAction>` and CloseRequested; `ShipDesignEditor(ContentSet,EmpireView,ShipDesign? copy=null)` emits `(string Name,ShipRecipe Recipe)` on explicit Save. GameScreen translates actions into Task5 commands with current turn/catalogue revision and refreshes immutable views. New scenario name `ShipDesign` selects its scene; normal checker expects10 scenario names and existing FrozenContent/ScoutUiControlChecks helpers only in opt-in code.

- [ ] **Step1 RED:** Actual `ShipDesignGraphicalValidation.DesignerControls` opens from Fleet Command at800x600/1920x1080, copies Standard, selects two pulse/no armor, observes2800/48/space6, saves through the actual button and verifies pinned state. Assert invalid7/6 capacity, locked component explanation,40/41 name limits, name conflict, Cancel/no mutation and32/256 cap behavior. Reach and activate Save after scrolling/keyboard focus. Use labelled pre-session fixtures here, not earned campaign claims.
- [ ] **Step2 verify RED:** Build compiling UI shells, then Runtime(ShipDesign) fails on missing/wrong actual control behavior. A missing node/compiler failure alone is not final behavioral RED.
- [ ] **Step3 implement:** Use named controls `OpenShipDesigns`, `DesignFrame`, `DesignEngine`, `DesignArmor`, `DesignBank1`, `DesignBank2`, `DesignName`, `SaveDesign`, `CopyDesign`, `ArchiveDesign`, `RestoreDesign`, `DeleteDesign`, `DesignPagePrevious`, `DesignPageNext`. Optional bank2/armor None is explicit; simulation computes all numbers/rejections. No local silent auto-save or recipe mutation. Page16, filter/selection stability, reachable controls and reference disclosure; shared theme/type variations/containers only.
- [ ] **Step4 GREEN:** Build + fresh Runtime(ShipDesign), including catalogue pages/archive after committed references and deleting an eligible archived record. Verify real opt-in negative control and ordinary10+2 exclusions on success/failure; keep full texts/screenshots and label canvas scaling/physical-input limits.
- [ ] **Step5 commit:** `feat: add ship catalogue and modular designer controls`.

## Task 15: Connect shipyard, request, research and refit controls

**Files:** Create RefitDialog; modify ColonyPanel/ColonyOrdersPanel, ConstructionQueueEditor, FleetCommandDialog, VesselPanel, TurnReportDialog, ResearchBar and GameScreen; extend ShipDesignGraphicalValidation/README; read shared theme before any minimal edit.

**Interfaces:** `RefitDialog(ContentSet,EmpireView,VesselId)` previews Task8 Start/Resume/Cancel and emits `Action<RefitAction,string>` with simulation token. Existing pickers carry stable DesignId metadata and build targets/AssemblyQuota, never resolve identity by displayed name.

- [ ] **Step1 RED:** `BuildRequestAndRefitControls` saves Gun, places finite ship queue entries, requests exactly that design across paged idle candidates and verifies paid fulfillment. At a repaired full-condition owned port, opens refit, inspects700 fee/manual pause/locks and applies; active-project and damaged-ship attempts show their exact blockers without clearing work. Cancel/Resume and archived-target-after-acceptance use actual handlers. Native snapshots compare current source stats, request identity and port reserve before/after actual buttons.
- [ ] **Step2 verify RED:** Build + Runtime(ShipDesign) at both window sizes, failing against the compiling but unwired/wrong behavior.
- [ ] **Step3 implement:** Show frame/design on own vessels/completion/queues; expose active refit from ship/Fleet Command and colony production. Block changed fleet/queue orders atomically; offer named Cancel rather than auto-cancel. Research shows four costs/prerequisites and enables only next-planning choices. Show price rounding/internal-vs-display detail and finite full-interval estimate. Do not add a bulk refit command or expand building-template preset semantics.
- [ ] **Step4 GREEN:** Build + Runtime(ShipDesign), plus affected Fleet/Combat/Scout scenarios with exact source scope. Verify retained32-row assembly paging/selections, filter correctness, manual favorite/workforce behavior, actual refit report reserve equality, keyboard focus/scroll and normal10+2 exclusions.
- [ ] **Step5 commit:** `feat: order exact designs and paid refits through game controls`.

## Task 16: Activate rules10 and prove four genuine paid campaigns

**Files:** Create `T/ShipDesignGenerationTests.cs`; modify Rules.CurrentVersion, GalaxyGenerator/NewGameSettings, G/ContentFiles/NewGameScreen, affected old-default test fixtures, ShipDesignGraphicalValidation and provenance capture.

**Interfaces:** After all preceding gates pass, current new games become10 and LoadCore requires ship-design definitions. Existing hazard/rival/species setup remains unchanged; normal current setup creates only the Standard catalogue and existing starting vessels.

- [ ] **Step1 RED:** `CurrentSetupCreatesRules10WithoutFreeEquipment` checks actual New Game defaults,4 human species ×3 core sizes, Standard/Off/Solo independence, invalid content/prior session retained and alternate configured role. `Assert.Equal(10,generated.State.RulesVersion); Assert.Equal(11,SaveGame.SchemaVersion);` Pin older fixture scenarios explicitly to their original rules rather than changing expected outcomes.
- [ ] **Step2 verify RED:** Focused(ShipDesignGenerationTests) and actual current New Game Runtime(ShipDesign) fail at current9 default/missing required section, not at a broken test fixture.
- [ ] **Step3 implement:** Activate the version only here; update setup summaries and keep invalid-current-content Load recovery. No schema/definition injection when opening rules9. Ensure all old HasX predicates, new-tech selectors and profile gates reflect explicit10 support without accidentally redefining rules9.
- [ ] **Step4 GREEN:** Suite + Build + fresh Runtime(ShipDesign). Run at least4 unmodified generated seed42 core campaigns, one per human species, using ordinary New Game, research, Save design, production, exact-design assembly, scouting, real sentry attack, withdrawal/repair, same-frame paid refit and a later volley/trip using changed stats. If a required unlock is not yet earned, play ordinary turns; no deadline, reroll, free reserve/XP/research, hull injection, healing, teleport or manufactured victory. Save actual Main checkpoints at catalogue, active queue, live request, active refit and paused refit; compare3 legal future turns to uninterrupted continuation. Paused/fault/cap/multi-human cases may be labelled pre-session supplements when not naturally produced; they do not replace the four generated paid cases. Retain start hashes, typed command trace, reserve/payment/research provenance and live-outcome-injection count0.
- [ ] **Step5 commit:** `feat: activate reviewed ship-design campaigns`.

## Task 17: Combined gates, one fresh implementation review and final disposition

**Files:** Actual reproduced correction/test paths only; create `reports/SHIP_DESIGN_VALIDATION_2026-10-29.md`, `reports/ship-design-execution/final-summary.json`, final ledger/review/disposition and update current status/requirements/acceptance indexes.

**Interfaces:** Consume every accepted task/manifest. Publish actual code source SHA, task gates, criterion evidence, executed counts,56-pair and old-future results, raw review/corrections, scope limitations and preservation audits. Do not predeclare passing counts.

- [ ] **Step1 combined baseline:** Full Suite, focused compatibility/atomicity, all10 native scenarios sequentially: ShipDesign, Combat, Fleet, Scout, Crisis, Colony, Rival, Species, Expansion, Legacy. Unique attempts, zero unresolved diagnostics, normal rebuild and actual10+2 opt-in/ordinary checks. Verify all raw baseline hashes and72/144 generated old-rule source/checkpoint provenance separately from actual supplemental totals; verify four new paid campaigns and labelled supplements separately. Audit final diff for approved10/11/56 and excluded scope.
- [ ] **Step2 one reviewer:** Use requesting-code-review at execution time; dispatch exactly one fresh whole-branch implementation reviewer on the most capable available model against accepted planning base..tested source. Supply spec/plan/actual reports and Native/no per-task/no second-review preference. The earlier design builder check does not count as this review and must not be repeated as an implementation review.
- [ ] **Step3 corrections:** Reproduce actual material findings with behavioral RED, make minimal corrections and run focused/Suite/affected native gates. Retain every failed attempt/raw finding. No automatic second reviewer. Disclose any unimplemented deviation and leave its acceptance criterion unverified; do not silently amend approved gameplay or weaken valid fixtures.
- [ ] **Step4 final evidence:** Map SHP-AC-001..011 to executed local automated proof and012 to ADR-022 design approval. Self-audit preservation of primary and execution checkouts/owner files/peaceful paragraph/old QA/evidence/unrelated refs/eight pre-existing worktrees plus the new execution worktree. Record real counts and source scopes if only affected scenarios were rerun after corrections. Keep17 parent rows/totals and human input/comprehension/accessibility/aesthetics/balance/long campaigns/cross-platform/security/export/release limits honest.
- [ ] **Step5 commit/handoff:** Stage exact corrected files/new evidence/current docs; `docs: record ship-design validation and review disposition`. Report isolated branch and concrete integration choice; no automatic main merge/push/release/cleanup. Keep all worktrees, branches and ignored/raw evidence under retained owner preservation instructions.

## Coverage and author self-review

| Approved contract | Owning tasks | Acceptance |
| --- | --- | --- |
| SHP-001 Catalogue/identity | 2,3,5,12,13,14 | 001,002,010,011 |
| SHP-002 Components/calculations | 2,4,10 | 001,003,009 |
| SHP-003 Research | 2,5,12,15,16 | 004,010,011 |
| SHP-004 Production/queues | 6,9,12,15 | 005,008,010,011 |
| SHP-005 Requests/officers | 7,9,10,12,15 | 006,009,010,011 |
| SHP-006 Refit commitment/price | 4,8,11,15 | 007,008,010,011 |
| SHP-007 Refit resolution/interaction | 9,10,11,12,16 | 008,009,010,011 |
| SHP-008 Presentation/privacy | 5,8,13,14,15,16 | 002,009,011 |
| SHP-009 Compatibility/atomicity | 1,2,3,6,7,8,9,10,11,12,13,16,17 | 009,010 |
| SHP-010 Acceptance/handoff | 14,15,16,17 | 011,012 |

Author self-review is performed here, without a new agent: every10 contract/12 criterion has an owner; each17 task has exact files/interfaces, behavioral checks and a commit boundary; five Review Focus conditions have named owning tests. Shared signatures/data flow must be checked against all later consumers before handoff. In particular do not: treat new IDs as content IDs/global entity IDs, omit own-frame reconstruction, spend a refit forecast before late eligibility checks, rewrite historical request identity, heal via initialization or alter ameba odds by passing travel modifiers. Keep source paths and current method signatures grounded in actual files, not historical plans.

The plan is for owner review. Retain Native inline; no execution-method question is needed. After confirmation that this concrete plan captures the approved slice, execute its17 tasks continuously with the isolated ledger and one final reviewer. No implementation worktree, genuine rules9 future capture, product edit, build/test/runtime run or final implementation review has occurred during this planning turn.
