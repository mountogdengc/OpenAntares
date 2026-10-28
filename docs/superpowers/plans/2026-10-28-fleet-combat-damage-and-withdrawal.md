# Fleet Combat, Damage and Withdrawal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. The owner has already selected Native execution, with no per-task agents and one fresh whole-branch reviewer at the end. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the approved first combat slice: a finite sentry encounter, persistent ship damage/cohesion, fleet withdrawal and straggler orders, and repairs in ordinary strategic turns.

**Architecture:** Add rules9-only combat content/state and pure exchange/withdrawal calculations to the existing command-driven simulation. Capture immutable encounter inputs before resolution, apply outcomes on the shared candidate, normalize losses before movement, then publish once after validation. Extend immutable own views and Fleet Command; preserve existing ameba, peaceful rivals, officer recovery and old-campaign behavior.

**Tech Stack:** Existing C#/.NET8, xUnit, Godot4.7.2 .NET, JSON content and integer travel/PCG32 infrastructure. No new dependency, tactical engine or asset requirement.

**Spec:** [Approved full combat design](../specs/2026-10-28-fleet-combat-damage-and-withdrawal-design.md), [ADR-020 design approval](../../DECISIONS/ADR-020-approved-fleet-combat-design.md). Read both before execution. ADR-019 remains drafting history.

## Global Constraints

- “new campaigns rules9; writer10”; exactly46 explicit schema/rules pairs, retaining37 existing pairs and adding only `(10,1)`..`(10,9)`; no `(9,9)` and no save migration.
- “1..32 members”; existing max64 fleets/empire, one generated pocket/two hazard hulls and one pending/active encounter per pocket.
- “one exchange per strategic turn”; no arrival attack, mid-volley damage retiming, transit attack, passive space healing, sentry repair/respawn or automatic re-attack.
- “Keep together and Repair are the proposed initial selections”; these defaults are now accepted by full design approval. Attack requires explicit return colony, arrival Repair/Hold, straggler policy and separate civilian/cargo consent.
- Escort cost2,400 internal production (24 displayed), hull100/attack24/armor4/speed300; sentries80/18/2/250; scouts60/0/0/existing300; all settlement hulls100/0/0/existing200.
- Integer band thresholds at50%/25%; firepower100/50/25%, speed100/75/50%; simultaneous shots, stable lowest-ID armed-first targeting, minimum1 damage for an armed shot, no overkill spill.
- Cohesion0..100 persists per ship. Loss per survivor:20 per destroyed friendly ship +10 per newly crippled survivor +10 if flagship destroyed, capped100. Cautious initially; Press threshold40, Hit-and-run one volley, Hold manual-only.
- “Freeze detachment membership before interception”; flagship remains in main cohort; empty main fleet dissolves while detached survivors remain independent. Strictly faster escape is clean; otherwise one shared sentry-only volley, followed by departure of all survivors.
- Repair at an own legal port: `ceil(maxHull/4)` hull and20 cohesion per full stationary Repair snapshot; no arrival tick, production charge or colony-output reduction.
- Retain M8 Navigator5/10%, Surveyor1/2 research, Organizer5/10%, XP2/1/8 and10/30 promotions; no combat XP. Mortal recovery50/70% and two subsequent turns; Wenth2/1 or4/3 link delays/source body unchanged.
- C# simulation without Godot; content in embedded JSON; ordinal/stable IDs, checked integer/Int128 intermediates, invariant parsing, snapshot phases and atomic command/Ready/turn/save publication. No new combat RNG draws or existing global-ID shifts for hazard actors.
- Preserve all owner prose, branches/worktrees, QA saves, raw reports and genuine old futures. No main integration, push, release or cleanup is part of execution approval for this isolated plan.

## Review Focus

1. A health change invalidates civilian consent or a return port after Ready: no silent revised attack or background retry; explicit next-period response. Tests Tasks5/7/9.
2. An interception cripples a ship or kills its flagship: preserve pre-volley cohorts, recalculate only surviving launch speed and never teleport an officer. Tests Tasks7/10.
3. A second human changes shared reservation/knowledge while another is Ready: preserve accepted planning, fail atomically and disclose no foreign identities. Tests Tasks5/9/11.
4. A current content folder contains combat-only roles while an old campaign is loaded: no escort availability, founding block, fields, formula or RNG drift in old rules. Tests Tasks2/12/15.
5. A repaired/returning ship remains bound to an old construction request: no duplicate completion XP, automatic resend or resurrected missing slot. Tests Tasks8/10/14.

## Execution baseline and verification vocabulary

This is a plan, not an execution ledger. All16 tasks below are Not Started. M9-D/CBT-AC-012 are Verified for design only; M9-P In Progress pending review; M9-I/functional CBT criteria Not Started. Retain Native; do not ask for the execution method again.

At execution, use the using-git-worktrees skill and inspect existing worktrees before establishing `codex/fleet-combat` from the accepted planning commit. Keep primary main and all seven current worktrees intact. Prefer native worktree creation where the task is attached to the actual repository; if unavailable for this container folder, use the established Git worktree fallback with a checked absolute path. Record source commit, compiler/engine paths, status/index, SHA256 preservation manifest and actual checkout hashes. Never copy or reset a dirty checkout implicitly.

Use `.superpowers/sdd/2026-10-28-fleet-combat/progress.md` for current task, commands, meaningful RED/GREEN, commit IDs and unresolved findings. Raw per-task evidence goes to new `reports/combat-execution/task-XX/` folders (01..16); every retry gets a new numbered subfolder. Historical M8 evidence is1,547 tests; it is not a fresh baseline for this execution. Run and retain the untouched current Suite as an execution-entry gate before adding Task1's deliberately failing manifest test.

Run from the execution checkout:

```powershell
# Focused(Type): replace Type with the exact class named by the task.
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers --filter FullyQualifiedName~Type
# Suite (build tests; never reuse an unbuilt stale test assembly).
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers
# Build / Normal.
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll'
# Runtime(Scenario): check the executable exists, replace Scenario/output per attempt.
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario Combat -OutputDirectory reports/combat-execution/task-14/combat-attempt-01
```

Restore only when needed, retain normal dependency audit and record actual exit/status; no-restore is not an audit. Graphical scenarios share their assembly and run sequentially. Task16 runs Combat plus existing Fleet/Scout/Crisis/Colony/Rival/Species/Expansion/Legacy, rebuilding normal code afterward. Required final metadata: all9 scenario classes and existing2 helpers absent; actual opt-in negative control must fail as intended.

Each simulation task has a focused behavioral RED, minimal implementation, GREEN and Suite gate. Missing API/compiler failures do not count as behavioral RED: add only minimal compiling contracts before demonstrating the wrong behavior. UI tasks use Build plus their actual graphical control scenario. Expected outcomes below are specifications, not predicted pass counts.

## File responsibility map

Paths are repository-relative. S=`src/OpenAntares.Simulation`; T=`tests/OpenAntares.Simulation.Tests`; G=`game`; V=`tests/OpenAntares.Game.Validation`.

| Files | Responsibility / tasks |
| --- | --- |
| `tools/CombatBaselineCapture/{CombatBaselineCapture.csproj,Program.cs}`, `T/CombatCompatibilityTests.cs`, `T/Fixtures/rules8-combat-baselines/`, `.gitattributes` | Genuine old-engine source/commands/futures and byte preservation,1/12/16 |
| `S/Combat/CombatContent.cs`, `CombatContentReader.cs`, `CombatContentWriter.cs`; `S/Content/{ContentSet,ContentLoader,ContentWriter}.cs`; `S/Rules.cs`; `content/core/{combat,expansion}.json` | Rules9 feature, profiles/bounds and old-role availability,2/15 |
| `S/Combat/{CombatState,CombatStateValidator,CombatInitializer}.cs`; `S/State/{GameState,VesselState,GameStateValidator}.cs`; `S/Galaxy/GalaxyGenerator.cs` | State/clone/generation,3/15 |
| `S/Combat/{CombatMath,CombatExchange,CombatOrders,CombatViews,CombatQueries}.cs` | Pure calculations and immutable preview contracts,4/6/7/13 |
| `S/Combat/{CombatCommands,CombatCommandProcessor,CombatReadyQueries,CombatTurnCoordinator}.cs`; `S/Commands/CommandProcessor.cs` | Commands/consent/private preparation,5/9 |
| `S/Combat/{CombatResolutionContext,CombatResolver,CombatWithdrawal,CombatRepair}.cs` | Frozen ordinary/interception outcomes, cohorts, repair,6/7/8/9 |
| `S/Turns/TurnResolver.cs`; `S/Movement/MovementResolver.cs`; `S/Fleets/{OfficerBonuses,FleetTravel,FleetResolutionContext,FleetResolver,FleetTurnCoordinator,OfficerRecovery,RequestQueueAuthority}.cs` | Precisely ordered integration, travel, exactly-once loss/officer/request paths,4/9/10 |
| `S/Combat/{CombatObservations,CombatReports}.cs`; `S/Knowledge/{EmpireView,EmpireViewProjector,ObservationUpdater}.cs`; `S/Colonization/FoundingRules.cs`; `S/Rivals/{IRivalPlanningContext,RivalPlanningQueries,RivalVesselPlanner}.cs` | Own knowledge, guarded founding and one existing AI controller,11/13 |
| `S/Saves/{CombatSaveData,SaveGame,ExpansionSaveData}.cs`; `S/Species/SpeciesRules.cs`; `S/Economy/VesselProduction.cs`; `S/Scouting/ScoutInitializer.cs` | Strict codecs/matrix, content gates and new hull initialization,2/3/12 |
| `G/Components/{CombatOrdersDialog,CombatReportPanel}.cs`, `G/Presentation/CombatNotificationPreferences.cs`; existing `FleetCommandDialog,VesselPanel,TurnReportDialog,StarPanel` | Combat previews/actions/reports/findability,14 |
| `G/Screens/{GameScreen,NewGameScreen}.cs`, `G/ContentFiles.cs`, `G/theme/openantares_theme.tres`; `V/{CombatGraphicalValidation.cs,CombatGraphicalValidation.tscn,Run-Validation.ps1,Validation.targets,Verify-NormalAssembly.ps1,README.md}` | UI/setup activation and opt-in ninth scenario,14/15/16 |

Do not broadly refactor old subsystems. New combat-only code stays in S/Combat; minimal adapters own old-method changes. All new path/class names above are plan decisions. New test classes/files are named in their tasks.

## Shared interfaces and data contracts

All new simulation types live in `OpenAntares.Simulation.Combat` unless specified otherwise. Public IDs are comparable record structs: `PocketId(int Value)`, `SentryId(int Value)` (local to a pocket), `EncounterId(long Value)`. No existing entity IDs are allocated for hazards. State classes are mutable only inside ordinary candidate transactions and implement deep `Clone()`.

Task3 defines `CombatGameState` (setting, counters, pockets, active/pending encounters), `CombatEmpireState` (observations, pending responses, finite histories, event/lifetime counters), `ShipCombatState` (Hull,Cohesion, independent Return/Repair state), `PocketState` (ID,star,sentries,cleared), `SentryState` (local ID,profile,hull), and `EncounterState` (ID,owner,fleet,pocket,revision,members,orders,phase,round,last-resolved-turn/phase,consent revision). Add nullable Combat properties to GameState/EmpireState/VesselState; they must be absent under rules1..8 and mandatory under9, including Off. Validate phases Pending/Active/WithdrawalQueued/BlockedResponse; terminal encounters move to immutable summaries, releasing the single reservation.

Task2 defines immutable `CombatDefinition` (role profiles, sentry profile, generation/history/band/cohesion/repair limits) and `CombatProfile(int MaxHull,int Attack,int Armor)`. Existing VesselDefinition supplies speed/cost; add `bool CombatOnly {get;init;}` defaultfalse solely to identify new-role availability, never key behavior to the literal `escort` string. CombatDefinition maps each playable role ID to a profile; it requires exact complete role coverage. Hazard definitions supply their own pursuit speed. Task3 freezes numeric bounds: positive Int32 stats/speeds, nonnegative armor/attack/cohesion, checked Int64 event/round counters, positive local IDs; round at most currentTurn+1; all incoming sums and distance comparisons use checked Int128 and reject unrepresentable results.

Task4 defines immutable `CombatShipFrame` (VesselId,role,hull,maxHull,cohesion,attack,armor,effectiveSpeed,flagship,cargo), `SentryFrame` (SentryId,hull,maxHull,attack,armor,pursuitSpeed), `CombatFrame` (nullable EncounterId for a not-yet-committed attack,turn,revision,owner,fleet, sorted empire/sentry arrays and orders), `CombatExchangeResult` (per-actor hull-after, actual loss, shots and destroyed IDs) and `CombatDeparture(ImmutableArray<VesselId> Members,StarId Origin,StarId Destination,int Speed,int TotalTurns,CombatArrivalOrder OnArrival)`. `CombatStats` is an immutable record of integer Firepower/Armor/Speed and `CombatDamageBand {Operational,Damaged,Crippled}`. All arrays are ImmutableArray; no state lists escape in views.

Orders: `CombatDoctrine {Cautious,Press,HitAndRun,Hold}`, `StragglerPolicy {KeepTogether,DetachCrippled}`, `CombatArrivalOrder {Repair,Hold}`, `CombatOrders(CombatDoctrine Doctrine,ColonyId ReturnColony,StragglerPolicy Stragglers,CombatArrivalOrder OnArrival)`. Doctrine Hold is combat-only, distinct from existing M8 Hold. Default attack orders are Cautious/KeepTogether/Repair with an explicitly selected return colony.

Task5 defines abstract `CombatAction` with `AttackPocket(FleetId Fleet,PocketId Pocket,CombatOrders Orders,bool AuthorizeCivilianRisk)`, `ChangeCombatOrders(EncounterId Encounter,long Revision,CombatOrders Orders,bool AuthorizeCivilianRisk)`, `WithdrawCombat(EncounterId Encounter,long Revision)`, `SetRepair(ImmutableArray<VesselId> Vessels,bool Enabled)`, `RespondCombat(EncounterId Encounter,long Revision,CombatResponse Response)`. `CombatResponse` is AcceptRevisedForce or RemainWithBlockedReturn; neither is a generic acknowledgement. `CombatActionCommand(EmpireId Issuer,int ExpectedTurn,CombatAction Action,string PreviewToken) : Command` is the single ordinary dispatcher entry.

Task4's `CombatPreview` contains Allowed, RejectionCode, Token, affected ships/cargo, permitted enemy facts, shot/band/cohesion forecasts, departures/escape exposure, return/arrival authority and emergency explanations. Tokens hash canonical permitted material facts/order/turn/revision, sorted ordinally; never GetHashCode, wall time or hidden foreign details. `CombatQueries.Preview(ContentSet,EmpireView,CombatAction) : CombatPreview` is pure. Task13 completes its report/status details without changing Task5's command contract.

## Task 1: Preserve the rules8 baseline and genuine futures

**Files:** Create `tools/CombatBaselineCapture/{CombatBaselineCapture.csproj,Program.cs}`, `T/CombatCompatibilityTests.cs`, `T/Fixtures/rules8-combat-baselines/manifest.json`; modify `.gitattributes` only for these raw JSON archives; create ledger/evidence folders above.

**Interfaces:** Consume current `GalaxyGenerator.CreateNewGame(ContentSet,NewGameSettings)`, `CommandProcessor.Execute(ContentSet,GameState,Command)`, `SaveGame.Serialize(ContentSet,GameState)` and `SaveGame.Deserialize(string)`; produce `Capture(ContentSet,NewGameSettings,IReadOnlyList<BaselinePolicy>,string outputDirectory):void` (`BaselinePolicy` is HoldReady or LegalOwnViewPlay) in the capture tool, plus a manifest with compiler/source/product hashes and each source/script/checkpoint SHA256. Compare futures using the existing `CompatibilityFixtures.CanonicalWithoutSchema(ContentSet,GameState)` contract (tests call that internal helper; the standalone capture tool implements an equivalent wrapper without referencing the xUnit assembly): remove only schema_version from serialized JSON, preserving rules_version, embedded content and the entire state; retain raw original saves separately.

- [ ] **Step1 RED:** `CombatCompatibilityTests.FrozenRules8ManifestExistsAndMatchesSource` asserts manifest sources/futures exist and hash-match, no combat fields, schema9/rules8. It fails on the absent manifest, not on a compile error.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.True(File.Exists(CompatibilityFixtures.PathFor("rules8-combat-baselines/manifest.json")));
```

- [ ] **Step2 verify RED:** `Focused(CombatCompatibilityTests)`; expect the named manifest assertion to fail. Use the untouched Suite result from the execution-entry gate; do not call a suite containing this deliberately failing manifest test the untouched baseline.
- [ ] **Step3 implement:** Build capture tool against the unchanged rules8 source (dedicated frozen project reference/checkout recorded in manifest). Capture48 seed42 starts: four species × three sizes × Crises on/off × Amebas on/off, each one Peldra rival. Capture two scripts (all-human Hold+Ready, and own-view legal economy/scout/settlement actions+Ready), at3/10-turn checkpoints:192 genuine future checkpoints. Each script's decision is lowest own ColonyId then lowest known legal target ID/ordinal role, with explicit safe departure/required acknowledgement from permitted previews; no hidden-state selector or random draws. All commands and consent receipts come from old public views; stop with diagnostic on a rejected command instead of editing state. Seed the source inventory with actual `reports/fleet-integration-20261028/fleet/generated-ui-800.json`, `generated-ui-1920.json`, `minor-return-800.json`, `minor-return-1920.json`, `review-safety-cancel-0-800.json` and `review-safety-cancel-1-800.json`; record their original provenance and label forced/synthetic setup as such. Add pending queue/request/travel/officer/cargo UI sources from the retained Fleet/Scout/Crisis evidence, selecting only format=openantares-save/schema9/rules8 and recording actual paths/hashes before capture. These supplemental sources never count among48 generated starts. Preserve every pre-existing archive byte-exact.
- [ ] **Step4 GREEN:** `Focused(CombatCompatibilityTests)` and Suite; verify48 sources/192 generated checkpoints, source/method/command hashes, untouched product paths and raw archive attributes. No rules9 product edits until this gate passes.
- [ ] **Step5 commit:** Stage only named tool/test/archive/attribute files and task01 ledger/evidence. Commit `test: freeze genuine rules8 combat baselines`.

## Task 2: Add version-gated combat content and role availability

**Files:** Create `S/Combat/CombatContent{,Reader,Writer}.cs`, `content/core/combat.json`, `T/CombatContentTests.cs`; modify `S/Rules.cs`, `S/Content/{ContentSet,ContentLoader,ContentWriter}.cs`, `content/core/expansion.json`, `S/Species/SpeciesRules.cs`, `S/Economy/VesselProduction.cs` and role filtering in `S/Knowledge/EmpireViewProjector.cs`.

**Interfaces:** Produce `Rules.CombatVersion=9`, `Rules.HasCombat(int):bool`, `Rules.VesselAvailable(ContentSet,int,string):bool`, nullable `ContentSet.Combat`, and `CombatContentReader.Read`/`CombatContentWriter.Write` using existing EntryReader/Utf8JsonWriter conventions. Add trailing `bool requireCombat=false` to `ContentLoader.Load`; do not change CurrentVersion yet. Extend existing feature predicates explicitly to include9; retain their old-version membership exactly.

- [ ] **Step1 RED:** `CombatContentTests.CoreProfilesAndLegacyRoleGate` asserts escort2400/100/24/4/300, sentry80/18/2/250, scout60 and settler100; rules8 cannot build/select/queue/embed a CombatOnly role even when current disk content contains it. Assert duplicate/missing role profiles, invalid band/repair/history values and overflow reject with file/entry/field.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.True(Rules.HasCombat(9));
Assert.False(Rules.VesselAvailable(core, 8, "escort"));
```

- [ ] **Step2 verify RED:** `Focused(CombatContentTests)` fails for missing profiles/old-rule availability assertion.
- [ ] **Step3 implement:** Use exact approved bands/cohesion/repair20/history20/round20/pocket1/sentry2 in JSON; only escort has CombatOnly=true. Validate complete profiles and old-rule content injection. ContentWriter's rules-aware path omits combat definitions/roles/metadata for1..8. Do not append CombatOnly:false fields to canonical old embedded definitions. Keep unsupported/old callers working with their exact frozen content.
- [ ] **Step4 GREEN:** `Focused(CombatContentTests)`, `Focused(FleetEconomyTests)`, `Focused(CombatCompatibilityTests)`, Suite. Confirm no new default, sentry spawn or writer change yet.
- [ ] **Step5 commit:** Stage exact paths named above and task02 evidence; commit `feat: add gated combat profiles and escort role`.

## Task 3: Create combat state, validation and deterministic setup

**Files:** Create `S/Combat/{CombatState,CombatStateValidator,CombatInitializer}.cs`, `T/{CombatFixtures,CombatStateTests,CombatGenerationTests}.cs`; modify `S/State/{GameState,VesselState,GameStateValidator}.cs`, `S/Galaxy/GalaxyGenerator.cs`, vessel completion in `S/Turns/TurnResolver.cs`.

**Interfaces:** Produce shared state contracts above; `CombatInitializer.Initialize(ContentSet,GameState,bool enabled):string?`, `InitializeVessel(ContentSet,int rules,VesselState):void`, `CombatStateValidator.Validate(ContentSet,GameState):IReadOnlyList<string>`. Add trailing `bool? SentriesEnabled=null` to NewGameSettings; explicit rules9 requires it, old rules require null. `CombatFixtures.Game(species="harl",enabled=true):LoadedGame` uses explicit9/settings; `CombatFixtures.Bytes(GameState):string` serializes complete state including streams/counters for rollback checks; `CombatFixtures.Duel(int ownHull=100,int sentryHull=80):LoadedGame` and `Convoy():LoadedGame` create labelled pre-session valid fixtures with legal fleets/ports and validated state, not manipulated live campaigns.

- [ ] **Step1 RED:** Generation tests assert one pocket/two sentries, no start/ameba overlap, maximum-minimum squared distance/tie StarId, same precombat RNG/global allocator as corresponding8 start, and Off has zero pockets but complete combat state. State tests mutate clone hull/members/observations without original changes; wrong references/ranges/phase conflicts and old-rule injected state reject.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
var clone = original.Clone();
clone.Vessels[0].Combat!.Hull = 1;
Assert.NotEqual(1, original.Vessels[0].Combat!.Hull);
```

- [ ] **Step2 verify RED:** `Focused(CombatGenerationTests)` and `Focused(CombatStateTests)`; expect behavioral absence/clone-validation failures.
- [ ] **Step3 implement:** Initialize after current plague/ameba/fleet generation and before final validation. Query environment eligibility without source population/affordability or RNG. No eligible system returns a located Standard setup error; never reroll. Initialize new constructed ships too. Complete counters/bounds and deep clones from shared contracts; new rules9 projected combat fields remain unavailable until Task11/13, but validator can inspect authoritative state now.
- [ ] **Step4 GREEN:** Both focused classes, existing GalaxyGeneratorTests/FleetGenerationTests, compatibility and Suite. Verify all species/sizes, alternative content and no sentinel collisions.
- [ ] **Step5 commit:** Explicit task paths/evidence; commit `feat: initialize bounded combat state and sentries`.

## Task 4: Define pure damage, targeting and preview calculations

**Files:** Create `S/Combat/{CombatMath,CombatExchange,CombatOrders,CombatViews,CombatQueries}.cs`, `T/CombatMathTests.cs`; modify `S/Fleets/{OfficerBonuses,FleetTravel}.cs` for rules9 speed only.

**Interfaces:** Produce shared immutable frame/view/order contracts; `CombatMath.Stats(CombatProfile,int hull,int baseSpeed,int navigatorBonus):CombatStats`, `CombatExchange.Calculate(CombatFrame,bool interception,ImmutableArray<VesselId> exposed):CombatExchangeResult`, `CombatQueries.Preview(ContentSet,EmpireView,CombatAction):CombatPreview`. Task5 defines CombatAction; add its compiling type contract now, leaving command behavior to5. For AttackPocket, Preview constructs an uncommitted frame only from own fleet and permitted pocket fields in EmpireView; no authoritative-state argument reaches the UI query. Internal `CombatQueries.Frame(ContentSet,GameState,EncounterId):CombatFrame` captures only a validated committed encounter's legitimate local information for the resolver.

- [ ] **Step1 RED:** `CombatMathTests.SimultaneousExchangeAndBoundaryStats`: assert escort86/sentry58, crippled dying sentry shot1 leaves escort99, non100 scout30/15 bands, escort51/50/26/25 transitions and Navigator315/236/157. Assert ordering with reversed collections, no overkill retarget, unarmed0 and minimum1. Preview is pure and matches exact calculated next exchange.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
var stats = CombatMath.Stats(new CombatProfile(100, 24, 4), 50, 300, 5);
Assert.Equal((12, 236), (stats.Firepower, stats.Speed));
```

- [ ] **Step2 verify RED:** `Focused(CombatMathTests)` fails on the named numeric/target assertion with minimal compiling records in place.
- [ ] **Step3 implement:** Freeze frames then calculate all shots before applying totals, armed-first/lowest stable ID. Checked Int128 aggregates, integer floors, exact ratio products. EffectiveSpeed applies damage before Navigator; use fleet slowest member while leaving committed provenance untouched. Return explicit overflow/rejection, never a partial result. No randomness or hidden weakest-hull targeting.
- [ ] **Step4 GREEN:** Focused math, FleetTravelTests, compatibility and Suite; prove identical results under alternate dictionary insertion/order and cultures. Old rules keep previous speed formulas.
- [ ] **Step5 commit:** Exact paths/evidence; commit `feat: calculate deterministic combat exchanges and damage speed`.

## Task 5: Implement commands, finite authority and consent

**Files:** Create `S/Combat/{CombatCommands,CombatCommandProcessor,CombatReadyQueries}.cs`, `T/CombatCommandTests.cs`; modify dispatcher and transaction guards in `S/Commands/CommandProcessor.cs`, existing fleet/officer/assembly/scout command guards only where required.

**Interfaces:** Produce shared CombatActionCommand/actions/responses; `CombatCommandProcessor.Execute(ContentSet,GameState,CombatActionCommand):CommandOutcome`; `CombatReadyQueries.ValidateResponse(ContentSet,GameState,EmpireId):ImmutableArray<string>`. Preview tokens use canonical SHA256 of permitted material facts and accepted authority. Preserve standard `CommandOutcome` accepted/no-op/rejection/readiness semantics.

- [ ] **Step1 RED:** `CombatCommandTests.AttackRequiresLegalFleetAndExactCivilianConsent`: assert legal stationary1..32 fleet/armed member/current pocket, Cautious/KeepTogether/Repair visible, return port selected, explicit civilian checkbox, stale/foreign/revised token rejects byte-exact. Two humans contend for a pocket: first accepted reservation survives, second receives generic unavailable, cancellation releases it. Reject active-combat membership/flagship/transfer/manual movement/founding/Study bypasses; identical valid choice preserves readiness.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.False(rejected.Accepted);
Assert.Same(original, rejected.State);
Assert.Equal(beforeBytes, CombatFixtures.Bytes(rejected.State));
```

- [ ] **Step2 verify RED:** `Focused(CombatCommandTests)` fails because ordinary dispatcher has no accepted finite attack authority.
- [ ] **Step3 implement:** Pending reservation uses new encounter counter/revision and accepted fleet member IDs. Lock members, not unrelated ships. Replace only the fleet's displayed Hold; preserve all other humans' planning. SetRepair accepts own stationary legal ships and excludes any active/pending combat or conflicting order. Revised force/civilian/blocked-return responses bind encounter+revision+material facts; unrelated report clicks cannot satisfy them. Define `CombatRejectionCodes` constants `combat_action_unavailable`, `combat_stale_preview`, `combat_reserved`, `combat_response_required` and `combat_port_closed`, preserving ordinary NotOwner/StaleTurn codes for those preconditions. No rejection reveals foreign IDs or hidden statistics.
- [ ] **Step4 GREEN:** Focused commands, FleetCommandTests/ScoutingCommandTests, Suite and canonical rollback checks for every rejected action.
- [ ] **Step5 commit:** Stage exact task files and actual guarded command paths; commit `feat: validate combat orders reservations and consent`.

## Task 6: Resolve persistent normal exchanges and cohesion

**Files:** Create `S/Combat/{CombatResolutionContext,CombatResolver}.cs`, `T/CombatResolutionTests.cs`; extend CombatState/Queries from earlier tasks.

**Interfaces:** `CombatResolutionContext.Capture(ContentSet,GameState):CombatResolutionContext`; `CombatResolver.ResolveExchanges(ContentSet,GameState snapshot,GameState candidate,CombatResolutionContext):void`. Context exposes consumed VesselIds, exactly-once destruction facts, proposed departures and repair capture; snapshots officer facts by reference to the existing FleetResolutionContext integration in9/10. No new product turn entry point yet; tests call the internal resolver on a candidate.

- [ ] **Step1 RED:** `CombatResolutionTests.OneRoundUpdatesPersistentState`: assert simultaneous hulls, one exchange perturn, Cautious/Press/HitAndRun/Hold behavior, middle70-cohesion ship dies leaving80/60 survivors at50/30 after one new cripple; no destroyed+crippled double count. Assert normal victory, own loss and mutual destruction clear/release exact actors/pocket without granting ownership/XP. Last-warship loss/civilians queue withdrawal and Emergency.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Equal(86, candidate.FindVessel(escort)!.Combat!.Hull);
Assert.Equal(58, candidate.Combat!.Pockets[0].Sentries[0].Hull);
```

- [ ] **Step2 verify RED:** `Focused(CombatResolutionTests)` fails on unchanged candidate hull/cohesion or missing queued retreat.
- [ ] **Step3 implement:** Use Task4 result, stable loss application, per-ship persisted cohesion, immutable round receipts and snapshot flagship. Freeze current exchange firepower, queue retreat only for following boundary. Precombat health loss invalidates pending attack or pauses active attack with new response as approved; never repeat a volley using stale civilian revision. Finite encounter state advances/terminates idempotently by encounter/round/phase, independent of report trimming.
- [ ] **Step4 GREEN:** Focused resolution/math/commands and Suite; duplicate application rejects atomically; rename/merge/save-independent clone cannot reset cohesion.
- [ ] **Step5 commit:** Exact task paths/evidence; commit `feat: resolve one combat exchange and persistent cohesion`.

## Task 7: Resolve withdrawal and frozen straggler cohorts

**Files:** Create `S/Combat/CombatWithdrawal.cs`, `T/CombatWithdrawalTests.cs`; extend CombatResolutionContext/Queries/ReadyQueries.

**Interfaces:** `CombatWithdrawal.Preview(ContentSet,CombatFrame):ImmutableArray<CombatDeparture>` and `CombatWithdrawal.Resolve(ContentSet,GameState snapshot,GameState candidate,CombatResolutionContext):void`. Context returns post-interception `ImmutableDictionary<VesselId,CombatDeparture> Departures`; each member maps to the same immutable cohort record, with exact speed/duration/arrival authorization.

- [ ] **Step1 RED:** `CombatWithdrawalTests.FrozenGroupsAndSingleInterception`: flagship300/straggler150 versus sentry250; together exposed, split flagship clean/straggler exposed, equality exposed. Newly crippled stays with original cohort and lowers launch speed. Lost main flagship replaced from main only; empty main dissolves, detached survivor remains independent. One interception across all exposed cohorts, no shots from withdrawers, strict clean exemption. Pre-first-volley cancellation causes ordinary travel without interception.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Contains(departures, d => d.Members.Length == 1 && d.Speed == 300);
Assert.Contains(departures, d => d.Members.Length == 1 && d.Speed == 150);
```

- [ ] **Step2 verify RED:** `Focused(CombatWithdrawalTests)` fails on missing departure or incorrect cohort/fire count.
- [ ] **Step3 implement:** Compare pre-interception frozen cohort speed versus fastest sentry; apply Task4 sentry-only result to exposed ships. Delete dead members; recompute speed/duration only for surviving original groups. Use existing integer TravelCalculator; preserve no-change committed transits. Known illegal port blocks Ready; a newly closed port this resolution instead performs one normal defensive exchange, publishes Emergency and requires revised route/explicit continued-risk response before the next Ready. No substituted port, repeat unsafe retry or conversion to Hold.
- [ ] **Step4 GREEN:** Focused withdrawal/commands/math, Suite. Test quarantine cargo and hull100->crippled interception with no second escape check; no hidden reselection after another human changes a reservation.
- [ ] **Step5 commit:** Exact task paths/evidence; commit `feat: withdraw fleets with frozen straggler authority`.

## Task 8: Implement finite return-and-repair orders

**Files:** Create `S/Combat/CombatRepair.cs`, `T/CombatRepairTests.cs`; extend CombatCommandProcessor/State/ResolutionContext.

**Interfaces:** `CombatRepair.Capture(ContentSet,GameState):ImmutableArray<VesselId>`; `CombatRepair.Apply(ContentSet,GameState snapshot,GameState candidate,CombatResolutionContext):void`; `CombatRepair.ObserveArrivals(GameState candidate,CombatResolutionContext):void` stores next-snapshot Repair or Hold without granting arrival healing. Record independent survivor authority outside a named fleet as required by empty-main outcomes.

- [ ] **Step1 RED:** `CombatRepairTests.ArrivalThenFourFullRepairTicks`: escort46/30 arrives turn12; remains46/30 then71/50,96/70,100/90,100/100 at16 and Hold. Assert OnArrival Hold gives no tick, mid-repair override cancels, own port/cargo/quarantine checks, new completion no tick and no output/production loss. Detached and main use the same selected arrival policy.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Equal((71, 50), (afterFirstFullRepair.Combat!.Hull, afterFirstFullRepair.Combat.Cohesion));
Assert.Equal((100, 100), (afterFourthFullRepair.Combat!.Hull, afterFourthFullRepair.Combat.Cohesion));
```

- [ ] **Step2 verify RED:** `Focused(CombatRepairTests)` fails on missing repair or arrival/authority assertion.
- [ ] **Step3 implement:** Capture eligible already-stationary Repair ship IDs before resolution; require continued eligibility after health/combat and no movement/fire/founding/study. Increment capped hull/cohesion once; end finite Repair only when both full. Arrival sets next-snapshot authority; no space/foreign healing. Preserve ship/cargo/health/history and manual M8 safety priority; completed requests never reopen.
- [ ] **Step4 GREEN:** Focused repair, FleetRequestTests, CrisisTransportTests and Suite. Test30 returning ships, zero economic mutation and no duplicate completion XP/resend.
- [ ] **Step5 commit:** Exact task paths/evidence; commit `feat: repair returned ships with finite authority`.

## Task 9: Integrate preparation, phases and atomic turn publication

**Files:** Create `S/Combat/CombatTurnCoordinator.cs`, `T/{CombatReadyTests,CombatTurnTests}.cs`; modify `S/Turns/TurnResolver.cs`, `S/Commands/CommandProcessor.cs`, `S/Fleets/FleetTurnCoordinator.cs`, `S/Movement/MovementResolver.cs` and context adapters.

**Interfaces:** `CombatTurnCoordinator.PrepareHuman(ContentSet,GameState,EmpireId):CombatPreparationResult` (State,Succeeded); internal `CombatResolutionPhase {Preparation,Snapshot,Health,Exchange,LossNormalization,Withdrawal,Movement,Founding,Repair,Observation,OfficerRecovery,Reports,Validation}` and `TurnResolver.ResolveCombat(ContentSet,GameState,Action<CombatResolutionPhase>):TurnResolution` for fault injection. Automatic planning actions pass through the same validated command handlers with an internal private-preparation adapter; no public bypass.

- [ ] **Step1 RED:** `CombatTurnTests.IntegratedDeathCannotReachMovementLookup` and Ready tests assert real SetReady/directResolve gives exactly one volley, one turn advance and same result; combat-consumed vessel absent in candidate never causes missing-vessel exception. Two-human private preparation preserves prior human planning. Inject failure at every declared phase and assert original reference/canonical bytes/readiness/RNG/all counters/consents unchanged.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Same(original, failed.State);
Assert.Null(failed.Report);
Assert.Equal(beforeBytes, CombatFixtures.Bytes(failed.State));
```

- [ ] **Step2 verify RED:** `Focused(CombatTurnTests)` and `Focused(CombatReadyTests)` fail on absent integrated exchange/rollback assertion.
- [ ] **Step3 implement:** Keep existing economy->health/ameba order; capture combat after accepted planning, resolve health invalidations, normal/interception outcomes then normalize losses before movement. Extend MovementResolver with an optional combat context: skip combat consumed IDs before lookup; select a combat departure even when snapshot PendingMove is null, before existing pending-move/Fleet launches; take its postdamage duration and cohort provenance. Retain ordinary snapshot departures and cargo-port checks. Prevent stale FleetResolutionContext.Launches from overriding combat launches. Repair after movement/founding only from captured eligibility; existing observations/XP/officer recovery/reports/final validate follow approved semantic order. New arrivals/completions cannot fight.
- [ ] **Step4 GREEN:** Both focused classes, FleetReadyTests/ScoutingReadyTests/CrisisReadyTests, compatibility and Suite. Assert new closed-port fallback is one ordinary exchange, next-period response persists, and old direct Resolve/Ready futures are identical.
- [ ] **Step5 commit:** Exact task paths/evidence; commit `feat: integrate combat into atomic strategic turns`.

## Task 10: Reconcile officer, cargo and construction losses exactly once

**Files:** Create `T/CombatIntegrationTests.cs`; modify `S/Fleets/{FleetResolutionContext,FleetResolver,OfficerRecovery,RequestQueueAuthority}.cs`, combat destruction adapters and existing cargo/crisis cleanup where actually required.

**Interfaces:** Reuse existing `FleetResolver.NormalizeAfterLosses` and captured OfficerSnapshot/FlagshipLoss; add internal `CombatLossIntegration.Apply(ContentSet,GameState snapshot,GameState candidate,FleetResolutionContext,CombatResolutionContext):void` in new `S/Combat/CombatLossIntegration.cs`. Maintain a consumed-loss set so health normalization then combat normalization cannot duplicate ship/officer/request events.

- [ ] **Step1 RED:** `CombatIntegrationTests.FlagshipCargoAndRequestLossOnce`: mortal recovery50/70% uses snapshot tech even on same-turn completion, delay two full subsequent turns; LastWatch no recovery roll; Wenth body/XP retained with2/1 or4/3. Destroyed settler cargo does not subtract source colony again; health history cleaned without cure. A reserved bound hull's destroyed slot becomes Missing and is not replaced/recredited/resubmitted automatically.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Equal(50, officer.RecoveryPercentAtLoss);
Assert.Equal(2, officer.AvailableAtTurn - officer.RecoveryStartedTurn);
```

- [ ] **Step2 verify RED:** `Focused(CombatIntegrationTests)` fails on missing/duplicate loss or wrong recovery-stream/timer assertion.
- [ ] **Step3 implement:** Feed each combat destruction through existing loss facts once, preserving officer whereabouts and M8 replacement flagship semantics constrained to the frozen main cohort. Keep separate cargo destruction facts and remove dangling health/order/consent references. Existing survey/request/study XP stay unchanged; no combat credit. Safety-pause unsafe old rally bindings; historical destroyed IDs become immutable facts, never live foreign references.
- [ ] **Step4 GREEN:** Focused integration, FleetRecoveryTests/FleetProgressionTests/CrisisTransportTests/ScoutingResolutionTests, Suite. Test mixed health+combat loss and empty main with detached survivors; compare recovery RNG exact advancement to existing equipment golden behavior.
- [ ] **Step5 commit:** Stage only actual adapter/test paths/evidence; commit `fix: preserve officer cargo and request contracts in combat losses`.

## Task 11: Enforce observation privacy, guarded founding and peaceful AI

**Files:** Create `S/Combat/CombatObservations.cs`, `T/CombatKnowledgeTests.cs`; modify `S/Knowledge/{EmpireView,EmpireViewProjector,ObservationUpdater}.cs`, `S/Colonization/FoundingRules.cs`, `S/Rivals/{IRivalPlanningContext,RivalPlanningQueries,RivalVesselPlanner}.cs`.

**Interfaces:** `CombatObservations.Refresh(ContentSet,GameState,int observedTurn):void`, `CombatQueries.Project(ContentSet,GameState,EmpireId):CombatView`; add nullable `EmpireView.Combat` and own hull/cohesion status without foreign hull state. Extend permitted rival founding views with observed pocket blockage; the existing sole vessel planner consumes that field, not authoritative hidden pockets.

- [ ] **Step1 RED:** `CombatKnowledgeTests.PocketKnowledgeIsCurrentOnlyWithOwnPresence`: unknown versus current versus stale views, opponent clearing unseen preserves stale own facts; hidden battle/hull/officer changes leave own text/tokens unchanged. Founding blocked at live pocket for all controllers, but foreign hidden changes do not leak reasons. After clearing, no automatic founding/ownership and earliest normal commitment is next planning period.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Equal(JsonSerializer.Serialize(staleBefore), JsonSerializer.Serialize(staleAfterHiddenBattle));
```

- [ ] **Step2 verify RED:** `Focused(CombatKnowledgeTests)` fails on absent guarded-founding rule or privacy assertion.
- [ ] **Step3 implement:** Observe only ordinary own current presence/survey; update after combat/movement. Pocket stats are public once observed; hidden state is not refreshed remotely. Add founding block after knowledge gate; never target-check hidden existence first. Single rival planner skips only its own known guarded candidate, does not build escort/attack, uses shared commands and ordinary cost/knowledge. No global reveal, second AI controller or cheat route.
- [ ] **Step4 GREEN:** Focused knowledge, RivalPlanningQueryTests/RivalPolicyTests/SpeciesFoundingTests, compatibility and Suite. Test multiple humans, pocket busy generic response, known stale clear and unchanged ameba founding block.
- [ ] **Step5 commit:** Exact task paths/evidence; commit `feat: project combat knowledge and guarded founding safely`.

## Task 12: Persist rules9 with the explicit writer10 matrix

**Files:** Create `S/Saves/CombatSaveData.cs`, `T/CombatSaveTests.cs`; modify `S/Saves/{SaveGame,ExpansionSaveData}.cs`, content codecs, `T/CombatCompatibilityTests.cs` and raw manifest references only to add new comparisons (never rewrite old expected futures).

**Interfaces:** `CombatSaveData.WriteGame(Utf8JsonWriter,GameState):void`, `ReadGame(EntryReader,int rules):CombatGameState?`; analogous WriteEmpire/ReadEmpire and WriteVessel/ReadVessel paired with the exact existing FleetSaveData call sites. Writer10 dispatches by rules, not schema alone. Required fields include explicit nulls and strict enum names; codecs reject duplicate/unknown/missing shape before deep state validation.

- [ ] **Step1 RED:** `CombatSaveTests.Exact46PairsAndLiveStateRoundTrips`: enumerate original37 +10/1..9, reject9/9 and other pairs. Round-trip pending reservation/civilian consent, active/queued withdrawal, post-interception cohort, blocked return, detached independent Repair/Hold, officer recovery, history-trimmed event idempotence and Off. Corrupt each live-reference/range/required-content field; previous session/file remains unchanged on failure.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Equal(10, JsonDocument.Parse(json).RootElement.GetProperty("schema_version").GetInt32());
Assert.True(SaveGame.Deserialize(json).Succeeded);
```

- [ ] **Step2 verify RED:** `Focused(CombatSaveTests)` fails on writer9/rules9 incompatibility and strict state assertions.
- [ ] **Step3 implement:** Bump writer only now; keep supported old schemas exact and add nine pairs explicitly. Embed combat content and all state only under9, omit CombatOnly roles for old rule writers and reject old-rule injection even if unknown-field parsing elsewhere is permissive. Use strict canonical shape/ID converters like FleetSaveData, preserving explicit root/empire/vessel field ownership. Restore every mutable array/collection independently; loading never heals/fires/moves/rolls.
- [ ] **Step4 GREEN:** Focused saves/compatibility and Suite. Replay Task1's genuine48 sources/192 checkpoints and retained UI futures using new writer's old-rule output; compare canonical behavior/embedded content against original old-engine expectations. Preserve genuine old1..7 fixtures and failed load/write evidence.
- [ ] **Step5 commit:** Exact codec/test paths/evidence; commit `feat: persist combat with explicit writer10 compatibility`.

## Task 13: Complete immutable previews, bounded reports and responses

**Files:** Create `S/Combat/CombatReports.cs`, `T/{CombatQueryTests,CombatReportTests}.cs`; extend CombatQueries/Views/ReadyQueries and `S/Knowledge/EmpireViewProjector.cs`, `S/Turns/TurnResolver.cs` TurnReport projection.

**Interfaces:** `CombatReports.Finalize(ContentSet,GameState snapshot,GameState candidate,CombatResolutionContext):void`; CombatView contains owned ship status, permitted PocketObservation views, active encounter/authority/previews, attention/required responses and immutable histories. `CombatQueries.Preview` remains the same pure contract from4/5; complete all public material fields without changing token semantics.

- [ ] **Step1 RED:** Query/report tests assert exact shot->band->cohesion->escape->repair forecast, convoy cargo/officer risk, stale observations, all new statuses and report explanation of authority. Trim21 encounters to20 and21 rounddetails to20 while live response and last-applied keys survive; aggregate counters remain correct and no duplicate XP/loss facts occur. Unchanged valid combat never adds a per-volley modal.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Empty(CombatReadyQueries.ValidateResponse(content, unchangedValidBattle, owner));
Assert.Equal(beforeBytes, CombatFixtures.Bytes(unchangedValidBattle));
```

- [ ] **Step2 verify RED:** `Focused(CombatQueryTests)` and `Focused(CombatReportTests)` fail on missing projection/bound/report assertion.
- [ ] **Step3 implement:** Aggregate routine progress, preserve persistent Attention for damage/retreat/blocked repair/officer loss and encounter/revision-specific Emergency for revised risk, blocked retreat and unarmed civilian survivors. Separate preferences from authoritative facts. Forecast only legitimate local/current or tagged-stale facts; no distant foreign plan/hull leaks. Store bounded summaries and checked lifetime counters independently from exactly-once application keys.
- [ ] **Step4 GREEN:** Both focused classes, privacy/Ready tests and Suite; verify opening/querying/report filtering is pure and unrelated acknowledgement cannot satisfy combat consent.
- [ ] **Step5 commit:** Exact task paths/evidence; commit `feat: explain combat forecasts outcomes and required responses`.

## Task 14: Build Fleet Command controls and the combat graphical gate

**Files:** Create `G/Components/{CombatOrdersDialog,CombatReportPanel}.cs`, `G/Presentation/CombatNotificationPreferences.cs`, `V/{CombatGraphicalValidation.cs,CombatGraphicalValidation.tscn}`; modify existing FleetCommandDialog/VesselPanel/TurnReportDialog/StarPanel, GameScreen, shared theme and V runner/targets/metadata checker/README.

**Interfaces:** UI consumes only EmpireView/CombatView/CombatPreview and emits CombatActionCommand through GameSession. Dialog `Open(ContentSet,EmpireView,CombatAction,Action<CombatActionCommand>)` renders exact permitted preview and returned rejection; `CombatReportPanel.SetView(CombatView)` is read-only. Add `-Scenario Combat` to the existing runner and one opt-in class to targets/checker; do not include any test helper in ordinary builds.

- [ ] **Step1 RED:** Add actual control assertions for Warship filter; In combat/Withdrawing/Damaged/Crippled/Repairing; Attack Orders/defaults, explicit cargo opt-in, Withdraw, Repair/Hold, pending responses and save/load. Assertions fail on missing controls/behavior, not merely a missing Godot class. First use explicit rules9 saved fixtures while current New Game still defaults8; label every pre-session forced setup.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
if (!visibleText.Contains("On arrival") || !visibleText.Contains("Withdraw"))
    throw new InvalidOperationException("Combat authority controls are missing.");
```

- [ ] **Step2 verify RED:** Build and Runtime(Combat) in a fresh task14 output folder; retain the failing control assertion and normal-assembly restoration.
- [ ] **Step3 implement:** Theme-based containers, readable numeric+text statuses, scrollable previews, finite bulk repair and selected-owned filters. Reports distinguish access from ownership, show stragglers without automatic resend and keep critical badges visible with routine preferences Off. Reread shared theme before minimal variations; preferences use unique QA paths in tests. A miniature tactical battlefield is out of scope.
- [ ] **Step4 GREEN:** Build and Runtime(Combat),800x600/1920x1080 with focus/scroll/text snapshots, pending combat Ready save/reload, two-human selection/privacy,30 ships and repair overrides. Prove actual opt-in assembly fails negative control then all9 scenario types +2 helpers are absent after Normal. Distinguish dispatched signals from physical-input/comprehension acceptance.
- [ ] **Step5 commit:** Exact UI/test/theme/.uid paths only, task14 evidence; commit `feat: expose combat orders forecasts and repairs in Fleet Command`.

## Task 15: Activate rules9 setup and run generated campaign paths

**Files:** Modify `S/Rules.cs`, `G/Screens/NewGameScreen.cs`, `G/ContentFiles.cs`, new-game callers in `G/Main.cs` if present; extend `V/CombatGraphicalValidation.cs` and `T/CombatGenerationTests.cs`; update old fixtures/test constructors only to explicitly retain their intended rules8 or earlier generation.

**Interfaces:** Set `Rules.CurrentVersion=CombatVersion`; LoadCore requiresCombat:true; NewGameScreen sends explicit SentriesEnabled true/false, Standard initial, alongside existing independent crisis/ameba/rival settings. All legacy selection controls remain hidden/disabled under old saves. Direct debug launch uses explicit sentry choice for9 without changing legacy campaign loads.

- [ ] **Step1 RED:** Generation/runtime checks assert actual New Game uses9/writer10 with explicit Standard; Off still permits escort/repair and has no block. An invalid Standard placement fails without replacing existing session and Load remains available. Rules8 campaigns loaded against current content retain old role menus/writer behavior and no hull/cohesion/sentries.

Representative executable assertions for the named test (setup and additional cases are specified above):

```csharp
Assert.Equal(9, generated.State!.RulesVersion);
Assert.True(generated.State.Combat!.SentriesEnabled);
```

- [ ] **Step2 verify RED:** `Focused(CombatGenerationTests)` and actual New Game Runtime(Combat) fail at the rules8 setup default/absence of explicit Sentries control.
- [ ] **Step3 implement:** Activate only after codec/phase/UI gates pass. Retain original Standard/Off, no rerolls, correctly named alternative roles and species fallbacks; do not hard-code roleIDs in generic rules. Audit old tests using default NewGameSettings and pin them to their original rules rather than weakening expectations. Old runtime scenarios may load their frozen campaigns; separately verify that current setup exposes9.
- [ ] **Step4 GREEN:** Suite, Build and fresh Runtime(Combat): untouched seed42 starts for every4 human species ×3 sizes; observe via own scout, pay/build/assemble escorts, attack real generated pocket, withdraw, repair, return and eventually clear it with legal ordinary controls. Save/reload pending/active/return/repair and compare3 future turns to uninterrupted continuation. Retain original start hashes and commands; no live outcome injection, free production, teleport, reroll or manufactured officer rank. Rare mutual loss/cargo/quarantine/flagship/empty-main cases remain explicitly labelled supplements.
- [ ] **Step5 commit:** Exact activation/runtime/test paths/evidence; commit `feat: activate reviewed combat campaign setup`.

## Task 16: Combined regression, one fresh review and disposition

**Files:** Modify only actual reproduced correction/test paths, docs status/requirements/criteria/validation; create `reports/COMBAT_VALIDATION_2026-10-28.md`, `reports/combat-execution/final-summary.json` and ledger final-review/disposition records. Never broad-stage source or historical reports.

**Interfaces:** Consume all prior contracts and manifests. Publish actual source SHA, task gates, exact acceptance status/evidence links, executed suite/runtime counts,46-pair result, genuine-future comparisons, normal assembly metadata, reviewer findings/disposition and preserved checkout/owner-file hashes. No promised counts in advance.

- [ ] **Step1 verify baseline deliverable:** Full Suite, explicit compatibility/failure-hook classes, all9 graphical scenarios sequentially in new task16 folders, Normal build and actual exclusion/negative controls. Verify48/192 generated genuine futures plus retained archives, current12 generated UI campaigns and forced supplements distinctly. Review all diffs and ensure default/schema/source match approved9/10/46.
- [ ] **Step2 one fresh reviewer:** Use requesting-code-review skill at execution time; dispatch exactly one independent whole-branch reviewer against accepted base..current source. Give spec/plan, actual reports and preserved Native preference. No per-task reviewer agents and no automatic second reviewer.
- [ ] **Step3 correction cycle:** Reproduce each actual material finding with a failing behavior regression, make minimal correction, rerun affected/Suite/runtime gates as justified. Explain any unimplemented deviation; do not silently narrow spec or mark full acceptance while a criterion fails. Preserve raw review and failed attempts. No new gameplay design or values without owner direction.
- [ ] **Step4 final verification/handoff:** Match every CBT-AC-001..011 to fresh executed evidence, CBT-AC-012 to ADR-020 design approval. Update PROJECT_STATUS/AGENTS/current-next-action, requirement child statuses without changing17 parent totals, and verification limitations. Preserve all QA/owner prose/worktrees. Record unverified physical input, comprehension, aesthetics, balance/long campaigns, cross-platform determinism, dependency/security audits, export/release; compilation is not milestone acceptance by itself.
- [ ] **Step5 commit:** Explicit corrected paths and new final docs/evidence; commit `docs: record combat validation and review disposition`. Present isolated branch result and concrete next integration decision; do not merge/push/clean automatically.

## Coverage and author self-review

| Approved contract | Owning tasks |
| --- | --- |
| CBT-001 Setup/pockets/founding information | 3,11,15 |
| CBT-002 Hulls/construction/content | 2,3,10,15 |
| CBT-003 Damage/speed | 4,6,7,9 |
| CBT-004 Commitment/lifecycle | 5,6,9 |
| CBT-005 Exchange/targeting | 4,6,9 |
| CBT-006 Cohesion/standing orders | 5,6,13 |
| CBT-007 Withdrawal/ports | 5,7,9 |
| CBT-008 Frozen detachment | 7,9,10 |
| CBT-009 Finite repair | 8,9,14 |
| CBT-010 Officer/cargo/old systems | 9,10,12 |
| CBT-011 Phases/transactions | 5,9,12,16 |
| CBT-012 Views/reports/UI | 11,13,14,15 |
| CBT-013 Compatibility/bounds | 1,2,3,12,13,16 |

Self-review checks: all13 contracts and12 acceptance IDs have owners; all16 tasks have testable deliverables and exact paths; new public command/query/cohort types are defined once and consumed consistently; each of five Review Focus conditions has named owning tests. Current source paths were inspected rather than copied from historical reports. Existing `HasFleets(version)==8`, strict required save shape, captured M8 launches and MovementResolver's missing-vessel lookup require the explicit adapters named here. No new gameplay decision is delegated as an unspecified implementation detail.

This plan is ready for owner review. Preserve Native inline execution preference; execution begins after confirmation of this concrete plan. Genuine rules8 baseline capture, product edits, new engine runs and the final independent implementation review have not begun during planning.
