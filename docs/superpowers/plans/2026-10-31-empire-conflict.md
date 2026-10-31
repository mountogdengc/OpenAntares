# Empire Conflict Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. The owner has already selected Native inline execution, no per-task implementation/review agents, and one fresh final whole-branch implementation reviewer. Do not re-ask the execution method. Steps use checkbox syntax for tracking.

**Goal:** Implement the approved limited-war milestone: bilateral war/peace, armed fleet combat and retreat, production blockades, fair paid military AI and clear player controls, preserving every older campaign.

**Architecture:** Add a plain-C# `Conflict` module beside existing Combat and Diplomacy. New-rules coordinators stage relationship decisions before economy, freeze effects, resolve two empire sides after health, and share existing movement/repair/officer accounting without inventing sentry actors. Requester-only projections drive commands, AI, forecasts and UI; the entire resolution remains one atomic transaction.

**Tech Stack:** Existing C#/.NET8 simulation/xUnit, JSON content, Godot4.7.2 .NET C# presentation/shared theme and opt-in native validation. No new dependency or art pipeline.

**Spec:** [Approved complete ECF design](../specs/2026-10-31-empire-conflict-design.md), full approval recorded in [ADR-026](../../DECISIONS/ADR-026-approved-empire-conflict-design.md). Read both before execution.

**Status, 2026-10-31:** M12-D/ECF-AC-012 Verified for design only; this separate22-task plan awaits concrete-plan review. M12-P In Progress; implementation/ECF-AC-001..011 Not Started. Planning baseline `76312d1`, product `873f320`, active11/12/67; no gameplay test/build/archive capture or execution checkout in this planning turn. Earlier counts retain their original evidence scope.

## Global Constraints

- The approved boundary is limited war, not conquest. Protect scouts/settlement vessels/passengers/colonies/population; no civilian shots, invasion, bombardment, capture, elimination, alliances, tactical screen, new economy, hulls, components or species bonuses. FR-001 remains Deferred.
- One active opponent per empire; disjoint wars allowed. Max32 armed participants/side and64 live empire encounters/empire. Latest20 completed encounters/owner and20 rounds/summary; live response/authority never expires through report trimming.
- New pair Peace; declaration effective next publication, no declaration-turn shot. Agreement endings at war publication preserve their last frozen effects; pending offer earmarks release before economy, no new setup fee/refund. Grievance30 during war and20 complete peaceful boundaries; no war trust; cancellation memory remains independent.
- Peace offer3, unconditional, answers before expiry, Withdraw beats queued answer. Resolve successful peace before economy/combat, with no final volley/interception/blockade penalty. Ten complete peaceful truce boundaries. AI-issued lifetime1 offers have a genuine published answer opportunity.
- Existing shot/band/armor/cohesion/doctrine/repair/officer rules stay exact. Both sides withdraw => no shots; unilateral equal/slower cohort => one enemy-only volley; local Break contact gives no immunity or movement. Civilians in mixed defending fleets stay protected; a civilian flagship gives no combat Navigator bonus.
- A blockade retains50% of ordinary positive net production, floor, after workforce/support/crisis penalties and before neutral trade. No stacking, reserve theft, support/research/growth change or implicit attack. New order first effective next interval; manual lift/peace stop current-boundary pressure. Arrivals/deaths affect the next frozen snapshot.
- AI: known own facts only, ordinary paid commands, no RNG/free hulls/omniscience. Peace/war armed targets2/4, cap6; peace target0 before second colony. One idle military colony; two grouping operations, fleet size<=4, one task/fleet. Healthy>=75% hull/cohesion>=60. Contact/shared-claim gates5; peace trigger5/15, inactivity10, outgoing cooldown5. Existing active work/manual authority/earmarks and civilian expansion remain respected.
- Proposed target rules12/writer13/79 explicit pairs: retain67, add only13/1..12. No12/12, implicit migration, retrofitted hostile old AI, altered old streams/IDs or old fields. Do not enable default12 before Task20. Capture genuine rules11 futures with unchanged873f320 before product edits.
- Simulation never references Godot. Use stable typed IDs, deterministic iteration, integer math/checked wide intermediates, invariant parsing, immutable requester projections and shared issuer-tagged command authority. No hidden foreign order/hub/bank/cohesion/officer/return-port in DTOs, errors, hashes or preview tokens.
- Preserve all10 worktrees/branches/stashes, protected owner files/exact peaceful paragraph, original QA, ignored workspace and all raw/frozen evidence. No overwrite of latest reports, expected futures or failed attempts; unique gate/output names. No merge/push/release/cleanup authority is implied by future execution approval.
- Full gameplay design approval does not authorize this plan's execution. Once separately approved, isolate execution and progress continuously through the ledger; no per-task agents and exactly one fresh final implementation review. Final owner approval/acceptance, human/platform/balance/security/export evidence remain separate.

## Review Focus

1. A private second peace proposal/withdrawal must not expose another human's unsubmitted intent or make receipt order change consent: Task6 `CrossedPrivatePeaceConsentIsCanonicalAndWithdrawWins`.
2. A refitting defender attacked on its completion boundary must retain its bank/installed equipment, pay0 and fire only once: Task11 `AttackedCompletingRefitKeepsReserveAndOldEquipment`.
3. A blockaded colony with neutral trade and a trade earmark must have identical actual/governor/rival/queue/refit/assembly production, with only conditional partner outcomes: Tasks12/13 `Blockade701PlusNeutralTrade100Equals450Everywhere`.
4. Mixed civilian/armed defense, sentry priority and simultaneous departing cohorts must never share/double-charge a loss, shoot cargo or roll an officer twice: Tasks8/10/11 `MixedDefenseAndSentryMatchingConsumeEachArmedHullOnce`.
5. A valid near-int-limit save or historical lost target must project/load without overflow, while forged live participants and old-rule fields reject: Task16 `HistoricalLossAndNearLimitDatesRoundTripWithoutUpgrade`.

## File map and shared contracts

Paths below use `S=src/OpenAntares.Simulation`, `T=tests/OpenAntares.Simulation.Tests`, `G=game`, `V=tests/OpenAntares.Game.Validation`. Expand prefixes and brace lists before staging. This is a file-ownership map, not permission to reorganize unrelated systems.

- `S/Conflict/ConflictContent*.cs` reads/writes/validates embedded knobs; `ConflictState*.cs` owns plain state/clone/invariants; `ConflictCommands.cs` declares intent; `ConflictCommandProcessor.cs` validates/manual authority; `ConflictViews/Queries/Tokens/Observations.cs` provide requester facts; `ConflictRelations/Peace/TreatyEffects/Attitudes.cs` implement diplomacy.
- `S/Conflict/ConflictMatching/Frame/Exchange/Withdrawal/Aftermath.cs` implement bounded two-sided military resolution; `ConflictBlockades/Economy/ForecastFrame.cs` own economic pressure; `ConflictPlanner/AiCoordinator/TurnCoordinator/ResolutionContext.cs` own deterministic preparation and atomic hooks; `ConflictReports.cs` owns filtered receipts/trim/Attention.
- Existing integration sites: `Rules.cs`, `State/{GameState,GameStateValidator}.cs`, `Commands/CommandProcessor.cs`, `Content/{ContentSet,ContentLoader,ContentWriter}.cs`, `Galaxy/GalaxyGenerator.cs`, `Turns/TurnResolver.cs`, `Economy/EconomyCalculator.cs`, `Knowledge/{EmpireView,EmpireViewProjector}.cs`, `Diplomacy/{DiplomacyCommandProcessor,DiplomacyResolver,DiplomacyAttitudes,DiplomacyTurnCoordinator}.cs`, `Development/GovernorPlanningQueries.cs`, `Rivals/{RivalTurnPlanner,RivalPlanningQueries,RivalColonyPlanner,RivalVesselPlanner}.cs`, `Fleets/{FleetCommandProcessor,FleetResolver,AssemblyPlanner,OfficerRecovery,OfficerProgression}.cs`, `Combat/{CombatCommandGuards,CombatResolutionContext,CombatRepair,CombatLossIntegration}.cs`, `ShipDesigns/{RefitResolver,RefitQueries,ShipDesignProduction}.cs`, `Movement/MovementResolver.cs`, `Saves/{SaveGame,ConflictSaveData}.cs`.
- UI: extend `G/Components/{DiplomacyDialog,DiplomacyContactPanel,FleetCommandDialog,ColonyPanel,ConstructionQueueEditor,RefitDialog,TurnReportDialog}.cs`, `G/Screens/{GameScreen,NewGameScreen}.cs`, `G/BreakdownText.cs`; create focused conflict forms named in Tasks17..19. Existing fleet detail is composed inside `G/Components/FleetCommandDialog.cs`; keep that component and extend its focused child panels, without inventing a missing FleetDetailPanel file.
- New validation: `V/EmpireConflictGraphicalValidation.{cs,tscn}`, `V/EmpireConflictCampaignChecks.cs`, `V/EmpireConflictCampaignUiChecks.cs`, `V/{Validation.targets,Run-Validation.ps1,Verify-NormalAssembly.ps1}`. All newly included files stay opt-in; ordinary project already excludes tests/tools.

Canonical namespaces/types (define in the producing task; later tasks consume these names):

- Task2: `ConflictDefinition` on nullable `ContentSet.Conflict`; existing `Rules.ConflictVersion=12`, `Rules.HasConflict(int):bool`, with defaults/writer still11/12 until Task20/16 respectively.
- Task3: comparable `ConflictEncounterId(long Value)`, `PeaceOfferId(long Value)`, `RelationKind {Peace,War}`, `ConflictPhase {Pending,Active,WithdrawalQueued,ResponseRequired,Completed}`. `ConflictState` has sorted `Pairs`, `Owners`, `Encounters`, checked `NextEncounterId/NextEventId`. Reuse `DiplomacyPair` unordered keys. `ConflictPairState` stores relation/revision/war-turn/declarers/truce-end, directed grievance/claim/peace timers and pending relation/peace intents. `ConflictOwnerState` owns defensive preferences, observations, reports and sorted `BlockadeOrderState(FleetId Fleet,EmpireId Other,StarId Star,int AcceptedTurn,long Revision)` records. Declare `ConflictOrders(CombatDoctrine Doctrine,ColonyId? ReturnColony,StragglerPolicy Stragglers,CombatArrivalOrder OnArrival)` in this Task3 state file so state compiles before commands. Also declare the minimal `ConflictResolutionContext` and its `Capture(ContentSet,GameState)` with immutable original relation/intent inputs here, because Task6 consumes it; Task8 extends it for matching/military state. `ConflictEncounterState` stores pair/star/LowSide/HighSide/frozen membership/revision/phase/last-round and own orders/responses. References to private owners stay owner-scoped. `ConflictInitializer.Initialize(ContentSet,GameState):void`; `ConflictStateValidator.Validate(ContentSet,GameState):IReadOnlyList<string>`; `ConflictFixtures.Create(string species="harl",int humans=1):GameSession` creates labelled pre-session rules12 supplements only. `ConflictFixtures.Canonical(GameState):string` is a deterministic test-only snapshot of all persisted fields, counters and PRNG state/increment; use this (aliased Serialize in assertion snippets) for rollback/clone comparisons before Task16 enables official schema13, not SaveGame.Serialize on an unsupported rules12 save.
- Task4: `ConflictCommand(EmpireId Issuer,int ExpectedTurn,ConflictAction Action,string Token):Command`; `ConflictAction` variants: `DeclareWar(EmpireId Other)`, `RevokeDeclaration(EmpireId Other)`, `ProposePeace(EmpireId Other)`, `AnswerPeace(EmpireId Other,PeaceOfferId Offer,bool Accept)`, `WithdrawPeace(EmpireId Other,PeaceOfferId Offer)`, `RevokePeaceAnswer(EmpireId Other,PeaceOfferId Offer)`, `ConfigureDefense(VesselId Anchor,ConflictOrders Orders)`, `Attack(FleetId Fleet,VesselId Target,ConflictOrders Orders)`, `ChangeOrders(ConflictEncounterId Encounter,long Revision,ConflictOrders Orders)`, `Withdraw(ConflictEncounterId Encounter,long Revision)`, `Respond(ConflictEncounterId Encounter,long Revision)`, `Blockade(FleetId Fleet,EmpireId Other)`, `LiftBlockade(FleetId Fleet)`. `ConflictOrders` is the Task3 record using existing CombatDoctrine/StragglerPolicy/CombatArrivalOrder enums; null port denotes local fallback, rejected for voluntary Attack. No variant contains another side's route/order.
- Task4: `ConflictView` attached nullable to EmpireView, own encounters/relations/blockades/military observations/notices; `ConflictPreview(bool Allowed,string? RejectionCode,string Token)` includes own consequences, permitted observations and conditional forecasts. `ConflictQueries.Preview(ContentSet,EmpireView,ConflictAction):ConflictPreview`; `ConflictQueries.Project(ContentSet,GameState,EmpireId):ConflictView`; `ConflictObservations.Refresh(ContentSet,GameState):void`. `ConflictTokens.For(ContentSet,EmpireView,ConflictAction):string` binds only requester facts/own intent. Task7 adds the exact next-volley projection using Task9 pure calculators; never project foreign private cohesion.
- Tasks5/7: `ConflictCommandProcessor.Execute(ContentSet,GameState,ConflictCommand):CommandOutcome`; changed commands clone/update own authority/clear own readiness; valid no-op preserves ready/counters; rejects return original. Existing public Execute dispatches it; AI adapters call the same validation with existing all-human-ready allowance, never bypass ownership/tokens.
- Tasks6/8/12/15: `ConflictResolutionContext.Capture(ContentSet,GameState):ConflictResolutionContext` freezes original inputs and owns future wars/accepted peace pairs/frozen blockades/attack intents/new matches/consumed hulls/cancelled refit jobs/departures/filtered events; one-shot phase flags prevent reapplication. `ConflictRelations.Prepare(ContentSet,GameState snapshot,GameState candidate,ConflictResolutionContext):void`; `ConflictRelations.Finalize(ContentSet,GameState snapshot,GameState candidate,ConflictResolutionContext):void`. Preparation stages future declarations but activates current-boundary peace suppression, not war economy effects prematurely.
- Task8: `ConflictMatching.Resolve(ContentSet,GameState snapshot,GameState candidate,ConflictResolutionContext):void`; Task9 `ConflictExchange.Calculate(ConflictBattleFrame):ConflictExchangeResult`, `ConflictExchange.Apply(ContentSet,GameState snapshot,GameState candidate,ConflictResolutionContext):void`. `ConflictBattleFrame` contains Low/High arrays of existing `CombatShipFrame` (real empire vessels, never `SentryFrame`), Low/High orders, phase/turn/round; `ConflictExchangeResult` returns Low/High arrays of `CombatShipDamage`, shots and destroyed IDs. A private calculator frame may contain each side's own cohesion; public foreign DTOs may not.
- Task10: `ConflictWithdrawal.Resolve(ContentSet,GameState snapshot,GameState candidate,ConflictResolutionContext):void`; returns cohorts through context, not direct position changes. Task11 `ConflictAftermath.Apply(ContentSet,GameState snapshot,GameState candidate,FleetResolutionContext,ConflictResolutionContext):void` records losses/officer facts and refit interruptions; movement consumes context departures once, repair excludes context consumed hulls. Do not fake existing sentry EncounterId or add a second OfficerRecovery pass.
- Task12: `ConflictBlockades.Capture(ContentSet,GameState):ConflictBlockadeFrame`; `ConflictEconomy.Apply(ContentSet,GameState,ColonyState,ColonyEconomy):ColonyEconomy` calculates ordinary pressure once before trade; pure `Retained(long income,int percent):long` uses wide multiplication. Task13 `ConflictForecastFrame.Apply(ContentSet,GameState ownCalculatorFrame,EmpireView):void` populates calculator-only pressure from permitted immutable own facts; never publish/save/resolve it.
- Task14: `ConflictPlanner.Plan(ContentSet,EmpireView,Action<Command>):void`; `ConflictAiCoordinator.Prepare(ContentSet,GameState):RivalPlanningOutcome` orchestrates emergency/peace/declaration/military/remainder policies. Add owner-filtered preparation entry points to existing rival/diplomacy coordinators; do not call their whole-world planning twice. Recompute only own tokens after own accepted mutations; foreign decisions stay frozen.
- Task15 internal seam: `ConflictResolutionPhase {Planning,Snapshot,Relations,Payments,Economy,Health,Matching,Departure,Exchange,Aftermath,Movement,Repair,Publication,Observation,Reports,Validation}`; `TurnResolver.ResolveConflict(ContentSet,GameState,Action<ConflictResolutionPhase>):TurnResolution` and `CommandProcessor.ExecuteConflict(ContentSet,GameState,SetReadyCommand,Action<ConflictResolutionPhase>):CommandOutcome`. Hooks are parameter-scoped internal tests, never global mutable callbacks.

## Execution and verification conventions

Each task has five checkable steps: write the named test/assertions, demonstrate behavioral RED, implement the named contract, verify GREEN/regression, commit/ledger. Build a compiling fixture or minimal contract shape before RED; a compiler failure is not proof of missing behavior. For UI/evidence tasks use a genuinely new failing native/evidence assertion, not a previously green unit test labelled RED. Keep all failures/source IDs/logs. Do not write tautological tests of constants alone where behavior can be exercised.

At approved execution start read using-git-worktrees and create/reuse a clean isolated named worktree from the approved plan commit. Canonical ignored ledger: `.superpowers/sdd/2026-10-31-empire-conflict/progress.md`; keep task briefs, exact commit/evidence/actual results. Completed tasks never repeat. Existing sources/fixture counts below were inspected while planning; any missing expected artifact blocks capture rather than silently changing the oracle.

Commands (run sequentially through a new `tools/ConflictValidation/Run-Gate.ps1`, adapted from the existing diplomacy wrapper and preserving prior latest/scout reports):

- `Focused(X)`: `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers --filter FullyQualifiedName~X`.
- `Suite`: same command without `--filter`; report actual pass/fail/skip, never copy historical2140 as a fresh result.
- `Build`: `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers`.
- `Native(X,new-dir)`: `pwsh -NoProfile -File tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <verified Godot .NET console path> -Scenario X -OutputDirectory <new-dir>`. Existing executable: `F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe`; verify it still exists before execution. Runner always finally normal-rebuilds; if interrupted, normal-rebuild before play.
- `Normal`: `pwsh -NoProfile -File tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath .godot/mono/temp/bin/Debug/OpenAntares.dll -ReportPath <new-dir>/normal-metadata.json`. Real opt-in assembly must fail this checker (negative control); ordinary rebuild must pass with all12 scenario types+2 existing forbidden helpers absent. Do not equate other opted-in helper count with these two historically checked helpers; also ensure every new conflict helper is excluded by its test-only namespace/path.

Use `reports/conflict-execution/task-NN/attempt-NN/` and unique gate names. Do not run two builds/native scenarios against one checkout concurrently. Focused behavioral checks are mandatory after each change; run Suite at phase/state/persistence/activation boundaries and final closure, not repeatedly without new reason. UI tasks add their owning native scenario. Restore/audit only if actually needed; no blanket NuGetAudit disable or claim of security verification from --no-restore.

## Tasks

### Task 1: Freeze genuine rules11 futures and preserve execution inputs

**Files:** Create `tools/ConflictBaselineCapture/{ConflictBaselineCapture.csproj,Program.cs}`, `tools/ConflictValidation/Run-Gate.ps1`, `T/ConflictCompatibilityTests.cs`, `T/Fixtures/rules11-conflict-baselines/`; modify `.gitattributes` for new fixture text only.

**Interfaces:** Consumes unchanged SaveGame/GameSession/shared commands; produces source/command/checkpoint hashes and a replayable rules11 manifest before any product change.

- [ ] **Step1 — Write the failing test:** `ConflictCompatibilityTests.ArchiveWasCapturedBeforeRules12`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(32, manifest.GeneratedSources);
Assert.Equal(64, manifest.GeneratedCheckpoints);
Assert.All(manifest.Sources, s => Assert.Equal(11, s.Rules));
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Inventory all worktrees/refs/stashes/index/owner prose/exact peaceful paragraph/QA/ignored recovery per checkout and retain raw hashes where claimed. Retain the inspected fleet UI/enum paths below. Freeze four species × {start,pending-offer,queued-answer,blocked-founder,trade-earmark,warmup,accord-notice,trade-notice}.json from reports/diplomacy-integration-20261031/diplomacy/earned/<species>/ using unchanged product873f320, with typed real continuation commands and checkpoints3/10. Add labelled pre-session sentry/refit/officer/plague/ameba/Ready supplements separately; never inject live resources into earned sources. Preserve all older archives. Record current engine executable/build/source and baseline Suite. Gate must fail absent a real32/64 archive, then pass genuine capture, not a new-engine-generated expectation.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictCompatibilityTests), untouched Suite and Build; byte/hash recheck all captured source/checkpoint files and preservation inventory. No new-rules native claim. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `test: freeze genuine rules11 conflict futures`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 2: Load bounded conflict content behind inactive defaults

**Files:** Create `S/Conflict/Conflict{Content,ContentReader,ContentWriter}.cs`, `content/core/conflict.json`, `T/ConflictContentTests.cs`; modify `S/Content/{ContentSet,ContentLoader,ContentWriter}.cs`, `S/Rules.cs`, `G/ContentFiles.cs`.

**Interfaces:** Produces ConflictDefinition/nullable ContentSet.Conflict and explicit HasConflict12; public creation/default remains11, save writer12.

- [ ] **Step1 — Write the failing test:** `ConflictContentTests.StrictDefinitionsSupportAlternateValuesWithoutChangingOldRules`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(10, core.Truce); Assert.Equal(50, core.BlockadeRetainedPercent);
Assert.Equal(30, core.DeclarationGrievance);
Assert.Contains(errors, e => e.Contains("conflict.json") && e.Contains("Truce"));
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Define all approved knobs/recognized limited_conflict_v1 algorithm and exact defaults from Global Constraints; accept bounded alternate values, reject duplicate/unknown/missing required definitions with source/entry/field errors. Bound durations1..1000, percentages1..100, grievance0..1000, AI desired/cap hulls1..32, formation1..32, live encounter limit1..64, report limits1..20 and checked cross-constraints (peace target<=war target<=cap; formation<=32). Civilian capabilities cannot become attackable merely because a custom role carries weapons; classify protected founding/passenger/scout capabilities explicitly and reject an ambiguous content combination before new-rules creation. No species-ID switch. Do not require/serialize conflict definitions for old rules. Test a missing required definition in explicit rules12 Solo without changing default.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictContentTests), old content/embedded definition tests and Build. Assert old-rule serialized content/behavior unchanged. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: add validated conflict definitions`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 3: Add cloned state and strict current versus historical invariants

**Files:** Create `S/Conflict/Conflict{State,Initializer,StateValidator,ResolutionContext}.cs`, `T/{ConflictStateTests,ConflictFixtures}.cs`; modify `S/State/{GameState,GameStateValidator}.cs`.

**Interfaces:** Produces Task3 IDs/state/fixtures/initializer/validator, including defensive orders and pair-local peace intents. No loader auto-repair.

- [ ] **Step1 — Write the failing test:** `ConflictStateTests.CloneAndValidateLiveAuthorityWithoutRequiringHistoricalTargets`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.NotSame(original.Conflict, clone.Conflict);
Assert.Empty(ConflictStateValidator.Validate(content, validHistoricalLoss));
Assert.NotEmpty(ConflictStateValidator.Validate(content, duplicateLiveParticipant));
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Create defaults only for explicit rules12 initialization. Task3 creates the minimal ConflictResolutionContext.cs contract consumed by Task6; it does not implement military resolution before Task8. Deep-clone every nested order/side/observation/history/response collection. Validate relation symmetry, one-war/empire, current own references, side<=32, encounter<=64, sorted/counter/date bounds; permit legitimate historical targets to disappear without legitimizing a forged live participant. Add labelled helpers for both human sides, mixed defenders, neutral third parties, independent war pairs, refits and active sentries. New test fixtures switch to12 only before first publication and supply all required definitions; they are never earned campaign evidence. Keep state independent of view DTOs; ConflictOrders and the minimal relation context are explicitly produced here, not undeclared forward references.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictStateTests), Suite and Build; mutate clones and verify original serialization unchanged. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: add bounded conflict state and validation`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 4: Project legitimate military facts and private intent tokens

**Files:** Create `S/Conflict/Conflict{Commands,Views,Queries,Tokens,Observations}.cs`, `T/ConflictPrivacyTests.cs`; modify `S/Knowledge/{EmpireView,EmpireViewProjector}.cs`.

**Interfaces:** Produces Task4 command/order/view/preview contracts. Query projection is pure; no command execution yet. Foreign arrays omit private fields structurally.

- [ ] **Step1 — Write the failing test:** `ConflictPrivacyTests.HiddenOrdersNeverChangeAnotherViewOrToken`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(beforeToken, afterHiddenOrderToken);
Assert.Equal(beforePublicView, afterHiddenBankAndOfficerView);
Assert.False(staleTargetPreview.Allowed);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Expose current locally observed stationary armed equipment/hull/grouping and dated stale facts only. Owned colony presence observes its own star. Keep foreign cohesion/admiral/doctrine/route/queue/bank/hub absent, including exports/rejections. Record immutable identities for observed anchor targeting without binding foreign private fleet revisions. Pending own declaration/attack remains issuer-only; own tokens bind relevant own revisions and permitted local observations. Test pure civilian/ungrouped/mixed/third-party observer/Solo, remote transit and hidden Ready/peace-answer changes. Define all command variants exactly; previews unsupported before their owning handler return unavailable without allocating IDs/RNG.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictPrivacyTests) and existing diplomacy/knowledge privacy tests; repeated preview/project leaves state/allocators/streams byte-identical. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: project private conflict and observed military facts`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 5: Queue declarations with normal command authority

**Files:** Create `S/Conflict/Conflict{CommandProcessor,Relations}.cs`, `T/ConflictDeclarationTests.cs`; modify `S/Commands/CommandProcessor.cs`.

**Interfaces:** Produces Execute for DeclareWar/RevokeDeclaration; stages resolution relation choices through Prepare without early economy effects.

- [ ] **Step1 — Write the failing test:** `ConflictDeclarationTests.DeclarationsPublishOnceAndCanonicalPairWins`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(RelationKind.Peace, afterQueued.Pair.Relation);
Assert.Equal(RelationKind.War, afterBoundary.Pair.Relation);
Assert.Single(competingResult.Wars);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Reject self/unmet/war/truce/second-opponent/forged issuer/stale turn/Ready intents; all no-ops/rejections preserve proper readiness/state/IDs. Declaration stores own private intent, not a target-mutating reservation. Canonically normalize crossed declarations; competing shared-empire wars select sorted unordered pairs once, return loser generic private unavailability. No declaration-turn Attack/Blockade. Add public command dispatch plus AI allowance through existing transactional guards, not privileged mutations. Test both submission orders, two human Ready/unready and independent disjoint wars.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictDeclarationTests), command/readiness regressions and Build. Boundary tests call Prepare/Finalize directly until Task15 wires production resolution. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: validate and stage war declarations`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 6: Resolve peace, treaty breach and transparent grievances

**Files:** Create `S/Conflict/Conflict{Peace,TreatyEffects,Attitudes}.cs`, `T/{ConflictPeaceTests,ConflictTreatyTests}.cs`; modify `S/Diplomacy/{DiplomacyCommandProcessor,DiplomacyResolver,DiplomacyAttitudes}.cs` and conflict relation/command handlers.

**Interfaces:** Consumes Task3 pair intent state/Task5 staging. Produces full Prepare/Finalize relationship effects, peace command handlers and wartime ordinary-offer gates.

- [ ] **Step1 — Write the failing test:** `ConflictPeaceTests.CrossedPrivatePeaceConsentIsCanonicalAndWithdrawWins`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.False(result.FinalVolley); Assert.False(result.BlockadeCharged);
Assert.Equal(41, peacePublishedAt31.TruceEndTurn);
Assert.Equal(0, failedWarActivation.NewSetupCharged);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Implement pending peace life/answer/decline/withdraw/revoke and canonical crossed consent without revealing private unsubmitted intents. Existing published offer retains proposer/ID; new same-snapshot proposals remain private per-owner intents until resolver normalization, lowerEmpireId canonical if crossed. Retain per-owner proposed/withdrawn/revoked evidence until normalization so lower proposer Withdraw cancels crossed consent, respondent Revoke leaves original unanswered, and processing order cannot resurrect consent. Only published recipient-visible terms/timers affect partner tokens. Test both orders and withdrawal at expiry; lifetime1 AI offers start at their first publication. Accepted peace marks bilateral current-boundary suppression and10 complete truce intervals; future declaration ends agreements only at publication, keeps current frozen trade/accord, cancels pending ordinary offer/answer and releases earmarks before economy. Directed streak reset, grievance30 during war/+20 peaceful boundaries, prior cancellation memory independent, no duplicate penalty/refund. Explicitly test start20->21 and peace30->31 dates, third-party trade, both declarers, ordinary notices/minimum and near-limit arithmetic.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictPeaceTests), Focused(ConflictTreatyTests), Suite and Build; compare pre/post reservation banking and privacy tokens. Do not claim combat suppression integrated before Task15. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: resolve wartime treaties peace and truce`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 7: Validate military orders and defensive preferences

**Files:** Create `S/Conflict/ConflictEligibility.cs`, `T/ConflictOrderTests.cs`; extend ConflictCommandProcessor/Queries; modify `S/{Combat/CombatCommandGuards,Fleets/FleetCommandProcessor}.cs`.

**Interfaces:** Produces Attack/ConfigureDefense/ChangeOrders/Withdraw/Respond/Blockade/Lift handlers, own order exclusivity and Task7 previews (math added Task9).

- [ ] **Step1 — Write the failing test:** `ConflictOrderTests.ObservedAnchorCanBeAttackedButProtectedCargoCannot`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.True(armedAnchorPreview.Allowed); Assert.False(civilianAnchorPreview.Allowed);
Assert.Equal(originalBytes, Serialize(rejected.State));
Assert.Null(ungroupedDefault.ReturnColony);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Own attackers all-armed named fleet1..32, stationary, valid explicit return port, no new hull/transit/repair/refit/assembly/sentry/ameba conflict. Target anchor current/local/hostile/armed; grouped and ungrouped defense legal. Refitting/repairing enemies remain targets. Defender defaults Cautious/local Break contact; protected civilian flagship supplies no combat Navigator. Do not lock/mutate foreign group until resolution; own accepted attack/order is exclusive. Normal Move/Return and refit/repair/grouping commands cannot bypass own active combat; Blockade lift accompanying own move/repair/refit/disband is explicit and timely. Implement future blockade order acceptance without current income; immediate Lift clears current income input. All altered commands share preview/issuer/turn/Ready/no-op semantics.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictOrderTests), old fleet/combat/refit guard tests and Build; assert military/civilian classification works for alternate content capabilities. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: validate armed conflict orders and defense settings`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 8: Match two-sided encounters deterministically

**Files:** Create `S/Conflict/Conflict{Matching,Frame}.cs`, `T/ConflictMatchingTests.cs`; extend the Task3 `S/Conflict/ConflictResolutionContext.cs`; modify `S/Combat/CombatResolutionContext.cs` only for new-rules participation adapter.

**Interfaces:** Produces Capture and Resolve matching plus used-vessel set and private side frames; consumes own intents/current world inside transaction only.

- [ ] **Step1 — Write the failing test:** `ConflictMatchingTests.MixedDefenseAndSentryMatchingConsumeEachArmedHullOnce`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Single(crossed.Encounters);
Assert.Equal(1, matches.Count(e => e.Contains(armedAnchor)));
Assert.DoesNotContain(civilian, match.LowSide.Members.Concat(match.HighSide.Members));
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Keep active encounters first; sort new intents star/pair/attackerLowestVesselId. Rebind defender anchor to its actual stationary group; split/rename cannot shield it. Freeze armed members<=32/side; loser duplicate/competing intents privately Hold. Active sentry force unavailable, pending sentry attack yields to new empire attack; mixed civilians preserved outside damage subset. Enforce max64/empire, no one-side double battle, no private foreign revision reservation. Health loss cancels pending match without shots or substitute target; active revised-force creates owner response and pauses both sides once without revealing foreign loss cause. Every phase idempotent by context/round, not history length.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictMatchingTests), matching/permutation/property checks over at least three forces and independent pairs; existing sentry regressions and Build. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: match deterministic bounded empire encounters`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 9: Calculate simultaneous empire volleys and doctrine triggers

**Files:** Create `S/Conflict/ConflictExchange.cs`, `T/ConflictExchangeTests.cs`; extend ConflictQueries next-volley conditional DTOs; reuse `S/Combat/CombatMath.cs`.

**Interfaces:** Produces pure Calculate and context Apply, Low/High CombatShipDamage arrays and factual receipts with no sentry ID coercion.

- [ ] **Step1 — Write the failing test:** `ConflictExchangeTests.OperationalAndCrippledEscortsFireSimultaneously`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(80, ordinary.Low[0].After); Assert.Equal(80, ordinary.High[0].After);
Assert.Equal(0, crippled.Low[0].After); Assert.Equal(98, crippled.High[0].After);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Freeze actual equipment/bands and target lowest opposingVesselId; each armed actor one shot, armor/min1, no spill/retarget, destroyed actors still fire. Aggregate checked wide; apply hull/cohesion and designated pre-volley participating flagship loss once. Reuse exact M9 Cautious/Press/Hit-run/Hold thresholds; no cohesion damage bonus or kill XP/RNG. Public preview exact observed shot damage may be shown; hidden opponent cohesion/doctrine/consent and whole-battle outcome stay unknown/conditional. Health/peace/used-vessel guards prohibit extra exchange; mutual destruction closes safely, protected civilians untouched.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictExchangeTests), CombatExchange/ShipDesign regression tests and Build; permutation-invariant receipts and no new RNG/global IDs. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: resolve simultaneous armed fleet exchanges`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 10: Handle both-side withdrawal and local break contact

**Files:** Create `S/Conflict/ConflictWithdrawal.cs`, `T/ConflictWithdrawalTests.cs`; extend ConflictResolutionContext departure DTOs/preview.

**Interfaces:** Produces Resolve/cohort outputs consumed by Task11 movement; uses frozen actual two-sided frame and accepted own routes.

- [ ] **Step1 — Write the failing test:** `ConflictWithdrawalTests.BothDepartWithoutShotsButSpeedTieTakesOne`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Empty(bothDepart.Shots);
Assert.Single(equalSpeed.Shots); Assert.Equal(20, equalSpeed.Shots[0].Damage);
Assert.Null(localBreak.DepartureDestination);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Departure intents (including newly attacked defender ordinary Move/Return) take precedence over ordinary volley. Both depart => no shots. One departs => strict speed comparison, clean cohorts excluded, enemy-only interception once, no ordinary volley then launch at post-damage speed. Reuse Keep/Detach flagship custody, keep mixed civilians with main cohort and protected; civilian flagship no Navigator. Null/missing/illegal own port uses disclosed local Break contact after exposure, ends encounter at same star with Hold/Attention/no immunity, no guessed port. Test co-located return rejection, quarantine, slow civilian affecting speed, crippled flagship, ungrouped defense, independent routes and peace suppressing every departure shot.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictWithdrawalTests), existing CombatWithdrawal/fleet travel tests and Build. Verify local break never creates Transit/IDs/repair ticks. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: resolve bilateral retreat and local disengagement`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 11: Integrate losses, refit interruption, movement and repair

**Files:** Create `S/Conflict/ConflictAftermath.cs`, `T/ConflictAftermathTests.cs`; modify `S/{Movement/MovementResolver,Combat/CombatRepair,Combat/CombatLossIntegration,ShipDesigns/RefitResolver,Fleets/FleetResolver}.cs` and task-owned context adapters.

**Interfaces:** Produces Apply and consumption/interrupt adapters; records actual loss/officer facts for the single existing OfficerRecovery phase. Append optional `ConflictResolutionContext? conflict=null` to existing `MovementResolver.Apply(...)`, `CombatRepair.Apply(...)` and `RefitResolver.Apply(...)` signatures; old call sites preserve identical behavior with null. RefitResolver still returns RefitApplyResult. Context identity authorizes interrupted jobs; no arbitrary missing-job tolerance.

- [ ] **Step1 — Write the failing test:** `ConflictAftermathTests.AttackedCompletingRefitKeepsReserveAndOldEquipment`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(0, interrupted.Paid); Assert.Equal(oldDesign, survivor.Design);
Assert.Equal(bankBefore + earnedIncome, port.ProductionReserve);
Assert.Equal(1, recovery.DrawsForLostFlagship);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Cancel an attacked defender refit only when encounter actually activates, before its first shot. Remove unpaid job/active port refit, leave queue/bank/installed design intact, construction paused, no second completion. A frozen provisional RefitPlan is marked interrupted/coveredfalse; adapt RefitResolver to recognize context-proven cancellation instead of throwing committed-job-disappeared. If target vanished before activation, do not cancel its surviving unrelated refit. Armed repair participant excluded from that boundary repair tick. Feed warship destruction/flagship facts into Fleet normalization and existing officer stream once; preserve mortal/Wenth source/Last-watch behavior and do not create sentry IDs. Movement applies each surviving cohort once; returning damage speed frozen at launch, no arrival repair. Both-sided protected civilians survive even if final escort dies; no phantom fleet/officer transfer or cargo death.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictAftermathTests), Suite and Build; focused officer/refit/repair/ameba/plague loss regressions and injected rollback. Native Combat/ShipDesign once full Task15 integration is ready. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: integrate conflict aftermath without duplicate payments or loss`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 12: Apply frozen nonstacking production blockades

**Files:** Create `S/Conflict/Conflict{Blockades,Economy}.cs`, `T/ConflictBlockadeTests.cs`; modify `S/Economy/EconomyCalculator.cs` before trade addition.

**Interfaces:** Produces Capture/Apply/Retained and owner-safe status facts; future accepted orders separate from current frozen order effects.

- [ ] **Step1 — Write the failing test:** `ConflictBlockadeTests.Blockade701PlusNeutralTrade100Equals450Everywhere`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(350, ConflictEconomy.Retained(701, 50));
Assert.Equal(450, blockaded.NetProduction.Total);
Assert.Equal(normal.NetResearch, blockaded.NetResearch);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Current valid war/order, observed target colony, >=1 operational blockader with cohesion>40, no opposing stationary armed hull. Repair/refit/sentry armed defenders contest, civilians do not. Any own fleet count nonstacking. Insert named negative production line after existing ordinary/crisis penalties and before neutral trade, reserve/project/queue/refit/assembly calculation. Preserve bank/earmarks/support/research/growth. New activation first following snapshot; arrival/death changes next snapshot; manual Lift and accepted peace suppress current interval, attacked blockader enters combat and clears future order without repricing already-frozen income. Reports/status never disclose target output. Test701/zero/near-long-max percentages, third-party co-colony, multiple blockaders and snapshots T/T+1/T+2.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictBlockadeTests), economy/reservation/refit regressions, Suite and Build; exact breakdown sums and no doubled penalty. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: calculate explicit frozen colony blockades`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 13: Use requester-only blockade inputs in every forecast

**Files:** Create `S/Conflict/ConflictForecastFrame.cs`, `T/ConflictForecastTests.cs`; modify `S/{Development/GovernorPlanningQueries,Rivals/RivalPlanningQueries,Fleets/AssemblyPlanner,ShipDesigns/RefitQueries}.cs` and ConflictQueries.

**Interfaces:** Produces Apply to calculator-only own frame and consistent current/conditional estimates; no world callback into planning context.

- [ ] **Step1 — Write the failing test:** `ConflictForecastTests.Blockade701PlusNeutralTrade100Equals450Everywhere`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.All(forecasts, f => Assert.Equal(450, f.NetProduction.Total));
Assert.Equal(beforeToken, hiddenPartnerAnswerToken);
Assert.Equal(bank - earmark, own.SpendableProduction);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Populate only own affected colonies and permitted current pressure facts in synthetic frames, alongside existing DiplomacyForecastFrame. Ensure governor hypothetical workforce/project, rival production, ordinary queue, refit, fleet assembly and conditional setup release all calculate pressure before trade/reserve through the same pure adapter. Foreign pending Move/peace/answer is conditional, not inferred certainty; own accepted Lift changes own hypothetical outcome without mutating world. Neutral trade still100, no repeated shortage/blockade application. Test changing hidden partner hub/answer/order with identical own projection yields identical forecasts/tokens.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictForecastTests), governor/rival/assembly/refit forecast and diplomacy conditional-release regressions; Build. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: keep blockade planning forecasts private and consistent`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 14: Plan paid military rivals without double preparation

**Files:** Create `S/Conflict/Conflict{Planner,AiCoordinator}.cs`, `T/ConflictAiTests.cs`; modify `S/Rivals/{RivalTurnPlanner,RivalColonyPlanner,RivalVesselPlanner}.cs`, `S/Diplomacy/DiplomacyTurnCoordinator.cs` and command AI adapters.

**Interfaces:** Produces Plan/Prepare; owner-filtered legacy coordinators preserve old global entry paths for rules1..11.

- [ ] **Step1 — Write the failing test:** `ConflictAiTests.MilitarySlotsArePaidFiniteAndDoNotPreemptExpansion`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.InRange(plan.GroupingCommands, 0, 2);
Assert.Equal(0, plan.FreeHulls); Assert.True(activeProjectUnchanged);
Assert.Equal(4, definition.WarHullTarget);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Order emergency/peace response, declaration eligibility, military slot/orders, remaining diplomacy/expansion. One idle lowest-ID positive-production colony, targets0-before-second/2-peace/4-war, cap6 counting existing+committed. Let active projects/queues/assembly/refits finish, then reevaluate; no planning slot double claim. Cheapest known legal armed design then attack/armor/speed/ID; two groups<=4 and normal grouping cost/slots, no free admiral. Repair75/cohesion60 priority, relief/defense/local observed attacks then spare investigation/blockade; leave defender if another spare can act. Contact/current shared-claim5/opinion<=0/no agreement/pending/truce gates for declaration. Exact observed attack strength thresholds; peace accepts all offers and proposes triggers5/15/inactivity10/cooldown5. Track only own permitted visible activity and claim continuity. Bound commands; stale own token refresh only after own accepted authority, not foreign facts; expected legal skips not whole-turn failure. Old expansion/scout/diplomacy must run exactly once per appropriate owner and keep civilian orders/military fleets out of each other.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictAiTests), Suite, deterministic replay/all-four-species and hidden-world permutation tests. Exercise asymmetric multi-contact ownership and available fleet slots without live resource injection. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: add fair deterministic rival military policy`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 15: Wire the complete atomic resolution transaction

**Files:** Create `S/Conflict/ConflictTurnCoordinator.cs`, `T/ConflictAtomicTurnTests.cs`; modify `S/Turns/TurnResolver.cs`, `S/Commands/CommandProcessor.cs`, relevant context adapters.

**Interfaces:** Produces ResolveConflict/ExecuteConflict hooks and production integration of prior task contracts; old rules take their exact original path.

- [ ] **Step1 — Write the failing test:** `ConflictAtomicTurnTests.EveryInjectedPhaseFailureReturnsTheWholeOriginal`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(originalBytes, Serialize(failed.State)); Assert.Null(failed.Report);
Assert.Equal(originalRandom, failed.State.Random); // full serialized equality also covers named subsystem streams
Assert.Equal(1, successfulBoundary.AdvancedTurns);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** At preparation choose ConflictAiCoordinator only for12 (not existing rival+diplomacy again). Capture immutable conflict/economy inputs alongside existing fleet/scout/combat/crisis/refit contexts. Relations.Prepare before diplomacy offer payments: suppress accepted peace current war/blockades, stage declarations, release newly war-cancelled offers before ordinary completions while retaining live pre-declaration trade/accord through current interval. Economy on candidate with correct banks and frozen effects; health/ameba/normalization retain original relative order. After health: matching, departing decision/interception versus normal volley (mutually exclusive), aftermath; active sentry/consumed sets exclude double participant and pending sentry losing match. Existing MovementResolver then founding/crisis exposure; repair excludes conflict consumed; refit recognizes valid interrupts. After one turn increment finalize war/truce/grievance/order dates and ordinary treaty endings, refresh legitimate observations/contact, record reports, run existing officer progression/recovery exactly once, validate entire candidate. The context must never masquerade as a saveable ordinary world. Use phase hooks listed above through every branch including two-human final Ready. Re-resolving failure never leaks partial observations/payments/rounds/RNG.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictAtomicTurnTests), Suite, Build; Native Combat/ShipDesign/Diplomacy/Rival affected current scenario regressions and Normal. Assert declaration/peace/blockade/refit examples with the actual public GameSession boundary, not only isolated helper methods. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: resolve empire conflict atomically with existing turn phases`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 16: Persist rules12 while replaying every older campaign

**Files:** Create `S/Saves/ConflictSaveData.cs`, `T/ConflictSaveTests.cs`; modify `S/Saves/SaveGame.cs`, content embedded serialization and test schema-pair assertions.

**Interfaces:** Produces schema13 DTO mapping/79 explicit supported pairs; default new-game remains11 until Task20.

- [ ] **Step1 — Write the failing test:** `ConflictSaveTests.HistoricalLossAndNearLimitDatesRoundTripWithoutUpgrade`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(79, acceptedPairs.Count); Assert.DoesNotContain((12,12), acceptedPairs);
Assert.Equal(beforeCanonical, afterLoadCanonical);
Assert.Equal(11, oldContinuation.State.RulesVersion);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Round-trip declaration/revocation, published/new crossed peace/answer/expiry/truce/grievance, military orders/observations, active/withdraw/response/localbreak/blockade/contested/refit-interruption phases with own consent/revisions. Reject duplicate/forged/current illegal participants, negative/future/counter exhaustion, >bounds, old injected fields, unknown algorithm. Preserve historically lost target identity without requiring present colony/vessel. Derived dates use long and command-range guards; near-int valid saves project without overflow. Run every Task1 genuine rules11 future through new writer13 to checkpoints3/10 and inherited archives; no old-rule hostile AI/content injection. Test malformed load retains session, failed write retains destination bytes, and three futures at every live new-rules save phase.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictSaveTests), Focused(ConflictCompatibilityTests), Suite and Build; all79 explicit positive pairs and all other candidate schema/rules pairs negative. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: persist conflict without upgrading old campaigns`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 17: Expose war and peace through diplomacy controls

**Files:** Create `G/Components/ConflictDeclarationDialog.cs`, `G/Components/ConflictPeacePanel.cs`, `V/EmpireConflictGraphicalValidation.{cs,tscn}`; modify `G/Components/{DiplomacyDialog,DiplomacyContactPanel}.cs`, `G/Screens/GameScreen.cs`, `V/{Validation.targets,Run-Validation.ps1,Verify-NormalAssembly.ps1}`.

**Interfaces:** Consumes Task4/6 requester previews/commands; adds native EmpireConflict scenario and filtered report navigation entry.

- [ ] **Step1 — Write the failing test:** `EmpireConflictGraphicalValidation.WarPeaceAndOwnPendingAnswersUseRealControls`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal("War", contact.RelationText);
Assert.True(terms.Contains("10 complete"));
Assert.Equal(0, duplicateOpen.CommandCount);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Add Peace/War/Truce-date status, clearly separated declaration confirmation with effective interval/treaty/setup consequences, own revoke and incoming peace Accept/Decline/outgoing Withdraw/exact queued answer/Revoke. No automatic human peace acceptance. Preserve Solo/unmet empty state; old rules keep peaceful introduction,12 explains limited war. UI never touches raw state or displays enemy private intent. Use shared theme/containers/focus/scroll800 and1920, actual commands/Ready/Main saves. Expand opt-in selector/types from11 to12; normal negative-control checker must also detect new scenario, new helpers compiled only through Validation.targets and excluded ordinarily.
- [ ] **Step4 — Verify GREEN:** New native scenario fails missing controls then passes through actual signals; Native Diplomacy plus EmpireConflict(new directory), normal rebuild and Normal. Supplement simulation command/offer/privacy tests; no physical-input claim. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: add explicit war and peace diplomacy controls`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 18: Expose attacks, defensive orders and safe withdrawal

**Files:** Create `G/Components/{ConflictOrdersDialog,ConflictBattlePanel}.cs`, `V/EmpireConflictCampaignUiChecks.cs`; modify `G/Components/{FleetCommandDialog,VesselPanel}.cs`, `G/Screens/GameScreen.cs`, native conflict scenario.

**Interfaces:** Consumes observed-anchor queries and own attack/defense/withdraw/response commands; one selected own encounter revision per action.

- [ ] **Step1 — Write the failing test:** `EmpireConflictGraphicalValidation.MixedDefenseAndLocalFallbackAreExplained`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.True(defenseText.Contains("protected"));
Assert.True(fallbackText.Contains("same system"));
Assert.False(foreignDetails.Contains("cohesion"));
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Show Armed wartime target, protected civilians, own doctrine/port/Keep-or-Detach/on-arrival, local fallback and observed enemy equipment/hull with unknown hidden-intent caveats. Voluntary attack requires valid own return; configure defensive Cautious/localbreak defaults for new and ungrouped warships. Display next-shot/withdrawal exposure, not certain unknown enemy retreats. Expose Attack/Change/Withdraw/revised-force response with current own revision, clear blocked reasons and no duplicate submit on resize/reopen/load. Keep two independent human perspectives isolated. Show battle consequences and repair/return links without forcing a tactical view or treating civilian safety as combat opt-in.
- [ ] **Step4 — Verify GREEN:** Native EmpireConflict, Fleet and Combat as affected, Normal; keyboard focus/scroll800/1920 and save-loaded pending response assertions. Retain simulation/privacy regressions. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: add fleet conflict orders and battle readback`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 19: Present blockade pressure and actionable filtered reports

**Files:** Create `S/Conflict/ConflictReports.cs`, `G/Components/ConflictBlockadePanel.cs`; modify `G/Components/{ColonyPanel,ConstructionQueueEditor,RefitDialog,TurnReportDialog}.cs`, `G/BreakdownText.cs`, `G/Screens/GameScreen.cs`, own TurnReport/EmpireView projections and native conflict scenario.

**Interfaces:** Produces requester-local event/round/notices with bounded20x20 history and contact/system/fleet-specific navigation; no global transcript.

- [ ] **Step1 — Write the failing test:** `EmpireConflictGraphicalValidation.BlockadeIncomeAndReportsAgreeWithActualTurn`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(450, ownBreakdown.NetProduction);
Assert.False(blockaderText.Contains(targetBank.ToString()));
Assert.Equal(expectedContact, openedReportContact);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Blockade/Lift confirmation and effective/contested dates; owner production breakdown named reduction/current+conditional forecasts across colony/queue/refit/assembly. Blockader sees status/star/opponent, never hidden output/bank/project delay/hub/population. Add persistent Attention for declaration/new danger/loss/localfallback/blockade change, own revisedforce Emergency, summary unchanged ticks/volley. Report acknowledgement never changes orders or peace consent; trim only completed summaries and leave live references/responses. Each action opens the correct permitted contact/fleet/system after save/load/history expiry; friendly labels no raw policy strings. No fake first-contact popups from opening UI.
- [ ] **Step4 — Verify GREEN:** Native EmpireConflict/Colony/Diplomacy affected checks and Normal; Focused(ConflictPrivacyTests), history-trim/Attention tests with saved still-active wars. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: show blockade pressure and filtered conflict reports`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 20: Activate only the approved new-game rules boundary

**Files:** Create `T/ConflictActivationTests.cs`; modify `S/{Rules,Galaxy/GalaxyGenerator}.cs`, `G/Screens/NewGameScreen.cs`, `T/GalaxyGeneratorTests.cs`; update current native setup expectations only.

**Interfaces:** Consumes all implemented/persistable contracts; enables default rules12 explicitly, writer13/79 already tested.

- [ ] **Step1 — Write the failing test:** `ConflictActivationTests.NewGamesUse12OldSavesRemainPeaceful11`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(12, generated.RulesVersion);
Assert.Equal(11, loadedOld.RulesVersion);
Assert.Equal(79, acceptedPairs.Count);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Initialize conflict only after ordinary valid new-game content/state, existing species/maps/rival/Solo/sentries/ameba unchanged and no PRNG draw/globalID side effects. Explain rivals can declare limited war; no peaceful-mode toggle. Missing conflict definition fails explicit12 Solo while Load remains available. Current rules11 save opens unchanged with peaceful AI/old agreements, and current12 Solo has no invented enemy. Update existing native scenarios only where they intentionally create current default games; retain frozen old embedded rule fixtures and expectations.
- [ ] **Step4 — Verify GREEN:** Focused(ConflictActivationTests), full Suite, Build, native setup/Diplomacy/Rival/EmpireConflict and Normal; compare generation old streams/IDs. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `feat: enable approved rules12 empire conflict for new games`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 21: Earn complete campaigns and capture native compatibility matrix

**Files:** Create `V/EmpireConflictCampaignChecks.cs`, `tools/ConflictCampaignProbe/{ConflictCampaignProbe.csproj,Program.cs}`; extend conflict scenario/Validation.targets, `T/ConflictEvidenceTests.cs`; create unique `reports/conflict-execution/task-21/` evidence only.

**Interfaces:** Consumes actual Main New Game/GameSession/normal UI commands; emits typed concrete command journal, actual Main saves/screenshots, stage hashes/future comparisons/production ledgers. Probe references live simulation source, no stale copied DLL acceptance.

- [ ] **Step1 — Write the failing test:** `ConflictEvidenceTests.EarnedCampaignsContainNoInjectedAssetsAndReplayExactly`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(4, earned.Count); Assert.All(earned, c => Assert.True(c.PaidBothMilitaries));
Assert.All(savedStages, s => Assert.Equal(3, s.EqualFutureBoundaries));
Assert.Equal(12, nativeMatrix.Count);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** Predeclare focused setup: all four human species, one Wenth rival, Small seed42 first; sentries/ameba/plague Off only in earned core diplomacy/conflict campaigns, with enabled hazards tested separately. Permit at most8 consecutive seeds42..49 and160 core turns per candidate; retain each attempted setup/seed/policy/source/failure journal. Use own projections/real commands and UI, never repair a live map/bank/ship/contact. Earn human+rival second colonies and armed ships, legitimate contact/declaration, actual paid battle/damage/repair, effective/contested/lifted blockade and peace/truce. Save via actual Main at pending declaration, queued peace answer, pending attack, active battle, queued withdrawal, effective blockade and truce (7 stages/species); replay at least3 whole boundaries per stage and extra continuation through complete truce/grievance expiry outside core budget. Blockade owner and target production totals must reconcile with ordinary production+neutral trade-costs-bank; no target-private numbers shown in opponent UI. Rare mixed/two-human/three-empire/independent-war/refit/sentry/health/near-limit/mod supplements explicitly labelled. Run all12 scenarios Legacy, Expansion, Species, Rival, Colony, Crisis, Scout, Fleet, Combat, ShipDesign, Diplomacy, EmpireConflict sequentially at recorded source; real opt-in rejection/ordinary normal rebuild after every run. Set the new EmpireConflict runner frame budget to72000 (like existing long Combat/ShipDesign runs), retaining explicit160-turn/8-seed campaign budgets; a quit/time-limit without results is a failed run, never acceptance. If budget fails, preserve evidence, diagnose without weakening required criterion or injecting assets, record any justified policy-only revision and rerun earned evidence with fresh IDs.
- [ ] **Step4 — Verify GREEN:** New evidence assertions first fail absent earned corpus, then all campaigns/stages/3-futures/supplements/full12 native matrix pass with zero engine diagnostics; Suite/Build/Normal. Source-scope every run, save and ledger, no claim that in-process controls prove physical input/fun/balance. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `test: validate earned conflict campaigns and native regressions`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

### Task 22: Review the whole branch, correct regressions and close evidence

**Files:** Create `tools/ConflictValidation/Verify-Evidence.py`, `T/ConflictFinalEvidenceTests.cs`, `reports/EMPIRE_CONFLICT_VALIDATION_2026-10-31.md`, `reports/conflict-execution/{final-review,execution-ledger,rulings}.md`, `reports/conflict-execution/final-summary.json`; modify project status/requirement/acceptance indexes with actual results only.

**Interfaces:** Consumes all task commits, manifest, raw logs/matrix/campaign ledgers; produces source-scoped complete-review disposition and exact verified product identity. Documentation commit may follow unchanged product.

- [ ] **Step1 — Write the failing test:** `ConflictFinalEvidenceTests.VerifiedClaimReferencesActualSourceAndEveryRequiredCriterion`. Arrange the specified valid fixture using Task3 helpers (Task1 uses unchanged rules11 sources); pin the behavior below, not guessed implementation internals.

```csharp
Assert.Equal(actualProductSource, summary.ProductSource);
Assert.Empty(summary.UnresolvedRequiredCriteria);
Assert.Equal(1, summary.IndependentImplementationReviews);
```

- [ ] **Step2 — Verify behavioral RED:** Run the owning Focused test through the gate; require its named assertion to fail because behavior is absent. For a native/evidence deliverable use its new failing scenario/evidence check. Record the exact failure and source.
- [ ] **Step3 — Implement:** After all implementation tasks pass, dispatch exactly one fresh whole-branch reviewer on the most capable available allowed model, read-only, approved baseline..HEAD plus evidence, with no implementation-history assumptions. Review against complete ECF spec and this plan, not green counts alone. Record all findings verbatim/severity, reproduce material findings with regression-first RED, apply one coherent correction pass and document minor/discretion rulings honestly. No second reviewer or per-task agents; retained Native method. Required omissions remain Implemented, Unverified until actually fixed, never waive an approved criterion as a minor detail. Rerun affected tests/native campaigns after changes, full Suite/build/metadata/frozen replay at corrected product; explicitly retain unaffected earlier matrix evidence at original source rather than attributing it to the new commit. Verify raw/canonical source identity across Git checkout line endings, exclude stale DLL evidence, assert no required criteria missing. Re-audit owner files/QA/raw evidence/refs/stashes/all11 worktrees (including new execution tree) against Task1 and retain failed audits honestly. Update canonical ledger22/22 only with actual complete gates; active parent17 statuses/totals unchanged. Keep execution branch/worktree for owner disposition; no merge/push/release/cleanup.
- [ ] **Step4 — Verify GREEN:** First prove evidence verifier rejects missing/source-mismatched/unresolved-required summary; then fresh full Suite, exact old replay, Build, affected Native/Normal and evidence checker GREEN. Record actual counts/sources/limitations and check clean intended branch diff; do not infer security/export/human acceptance. Require exit0 and the named assertions passing; preserve source-scoped commands/logs and meaningful regressions.
- [ ] **Step5 — Commit and record:** Stage only this task's named paths plus its retained evidence; commit `docs: record verified empire conflict implementation and review`. Update the canonical ledger with commit/result/evidence and any path-only ruling; do not advance over a failed required check.

## Dependency and acceptance map

Tasks are sequential, with the stated cross-task contracts; no feature is enabled before20. Task1 gates all product work. Tasks2..4 establish definitions/state/requester facts;5..6 relationship authority;7..11 military;12..13 economy/forecasts;14 AI;15 actual transaction;16 persistence;17..19 UI;20 default boundary;21 full earned/matrix evidence;22 final independent review/correction/evidence. No task parallelization is assumed.

| Approved contract | Owning tasks | Acceptance |
| --- | --- | --- |
| ECF-001 Relations | 3,4,5,15,17 | ECF-AC-001 |
| ECF-002 Agreements/attitudes | 6,12,15,17 | ECF-AC-002 |
| ECF-003 Peace/truce | 6,10,15,16,17 | ECF-AC-003 |
| ECF-004 Targets/visibility | 2,4,7,8,18 | ECF-AC-004 |
| ECF-005 Matching | 8,9,11,15 | ECF-AC-005 |
| ECF-006 Exchanges/defense | 7,9,11,18 | ECF-AC-005, ECF-AC-006 |
| ECF-007 Departure/aftermath | 10,11,15,18 | ECF-AC-006 |
| ECF-008 Blockades | 7,12,13,15,19 | ECF-AC-007 |
| ECF-009 Rival | 14,15,21 | ECF-AC-008 |
| ECF-010 Commands/atomicity | 5,7,11,15 | ECF-AC-009 |
| ECF-011 Content/state/compatibility | 1,2,3,16,20 | ECF-AC-009 |
| ECF-012 Player experience | 17,18,19,21 | ECF-AC-010, ECF-AC-011 |
| Earned native/closure | 21,22 | ECF-AC-010, ECF-AC-011 |
| Full design approval | ADR-026, not implementation tasks | ECF-AC-012 design-only Verified |

## Handoff

Read the [author self-review](../../../reports/EMPIRE_CONFLICT_IMPLEMENTATION_PLAN_REVIEW_2026-10-31.md). This plan does not report executed feature tests. Current product remains11/12/67; genuine rules11 capture and isolated execution have not started. Review this concrete22-task plan before implementation; use the already retained Native method after approval. No new gameplay choice or execution-method question is required.
