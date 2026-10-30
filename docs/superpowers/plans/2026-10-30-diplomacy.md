# Diplomacy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. The owner has retained Native inline execution, no per-task implementation agents, and one fresh final whole-branch implementation reviewer. Steps use checkbox syntax for tracking.

**Goal:** Implement the approved first-contact, attitude, settlement-accord and production-trade design without changing old campaigns.

**Architecture:** Add a small simulation-owned Diplomacy subsystem with immutable requester projections and ordinary issuer commands. A single reservation adapter feeds every production consumer; a frozen diplomacy frame supplies current-turn effects while accepted agreements become effective only next turn. Godot presents these queries and commands through existing navigation and theme.

**Tech Stack:** Existing C#/.NET8, xUnit, Godot4.7.2 .NET and embedded JSON; no new dependency, engine, currency, asset pipeline or RNG stream.

**Spec:** [Approved full design](../specs/2026-10-30-diplomacy-design.md), [ADR-024 approval](../../DECISIONS/ADR-024-approved-diplomacy-design.md), [builder check](../../../reports/DIPLOMACY_GAMEPLAY_REVIEW_2026-10-30.md). Full design approved by owner “Looks good, write the implementation plan.” This separate plan awaits review; all18 tasks are Not Started.

## Global Constraints

- Proposed rules11/writer12/67 explicit pairs: preserve56 existing pairs, add only schema12/rules1..11; no old-schema/rules11 pair or implicit upgrade. Active product remains10/11/56 during planning.
- Contact reveals reciprocal identity only, never remote location/home/map. Directed score =2*min(streak,10)-memory10; labels Strained<0, Neutral0..9, Receptive10..19, Cooperative20. Memory lasts10 completed boundaries.
- One pending offer/pair, one agreement/kind/pair; three answer boundaries; Withdraw defeats queued answer; Revoke Accept/Decline restores unanswered without extending expiry.
- Accord covers one named surveyed system; both parties stop further founding there. Existing colonies/passages/surveys continue. Only current legitimate own observations qualify; AI requires its own colony in the system.
- Trade setup500/output100 internal production, warmup3, minimum10 effective boundaries, notice2, AI offer cooldown5; thresholds accord0/trade10. No hidden bonuses, treasury, technology transfer or military obligations.
- Earmarks are part of the bank and unavailable to every other consumer. Activation counts its own reservation; bank500/earmark500 succeeds. Each payment is once, atomic, with no refund after activation.
- Accepted in T->T+1: first effects T+1->T+2; trade first earns T+4->T+5. Activated at T+1, minimum completes T+11; notice in T+11 ends T+13. Both notice boundaries retain effects.
- Trade adds net production after workforce/support penalties, before ordinary completions; no second penalty, multiplier, discount or research. Loss of a live hub ends both links before output, without blame/refund.
- Same issuer commands/Ready/atomic transaction for humans and AI; integer checked arithmetic, stable IDs/order/invariant parsing, PCG32 unchanged. Private hubs, partner eligibility, maps and revision details never leak.
- Preserve existing sentry combat, ameba, species/Wenth, fleet/admiral, queue/governor/refit rules and peaceful rivals outside explicit accord restrictions. War/non-aggression, barter, victory and FR-001 remain later scope.
- Preserve nine existing worktrees, owner prose/peaceful paragraph, prior refs/stashes, QA/raw evidence/frozen futures/recovery. No main product integration, remote push, release or cleanup is authorized by this plan.

## Review Focus

1. Exact-price bank already reserved: activation must count its earmark once. Task5 `ExactPriceEarmarkActivates` and Task8 `SetupPaymentCannotDoubleCharge`.
2. A queued founder becomes blocked while travelling: retain ship/cargo and show Attention rather than cancel or consume it. Task7 `AccordActivatesDuringFounderTransit`.
3. Answer revocation frees funds on the last valid boundary: original expiry survives Save/Load and released funds can finish construction. Tasks4/6/12 `RevokeAtExpiryDoesNotExtendOffer`.
4. Hub disappears in a valid live agreement: input remains loadable, boundary ends before payout and partner sees no private reference. Tasks3/9/12 `LiveMissingHubEndsWithoutValidationDeadlock`.
5. Another human changes hidden plans after Ready: own previews/revisions are unchanged; late failure restores original state/readiness/earmarks. Tasks10/11/13 `HiddenPartnerMutationDoesNotChangeOwnToken`.

## Execution baseline and gates

Planning baseline main a8d17dd (product3796315), clean on inspection. Previous M10 evidence2030/2030 is historical, not a fresh test run for M11. No execution checkout/capture or gameplay run in this planning turn.

At execution first use using-git-worktrees, inventory attached/registered worktrees and reuse/create a suitable isolated codex/diplomacy checkout from the accepted planning commit. Keep main and all existing worktrees intact. Use `.superpowers/sdd/2026-10-30-diplomacy/progress.md` as execution ledger, with final tracked copy `reports/diplomacy-execution/progress-final.md`. Each task records entry/source SHA, behavioral RED, focused GREEN, full suite, exact-path commit and unique attempt evidence. Never repeat completed tasks.

Task1 supplies `tools/DiplomacyValidation/Run-Gate.ps1 -Name <name> -OutputRoot <unique-path> -Command <exe> -Arguments <array>` by adapting the existing ShipDesign wrapper. Preserve all historical latest reports byte-exactly, relocate new emitted reports with a hash map, retain failed attempts, exit codes/logs and all QA saves. Run graphical scenarios sequentially; restore a normal build in finally even after failure. New/changed simulation behavior requires compiling fixtures and a behavioral failure; missing APIs/compiler errors are not behavioral RED.

Commands run from the execution checkout. Focused(Type) substitutes the exact test class listed in each task:

```powershell
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers --filter FullyQualifiedName~Type
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll'
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot '<verified Godot4.7.2 .NET console executable>' -Scenario Diplomacy -OutputDirectory reports/diplomacy-execution/task-17/attempt-01/runtime
```

Suite means the unfiltered test command; Build means the ordinary build; Normal means rebuild plus Verify-NormalAssembly. Locate the actual existing Godot executable before execution (previous location F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe). Restore only when required with normal audit enabled; no-restore is not a new security audit. Every native run proves the actual opt-in assembly fails the normal checker, then Normal must prove all11 scenarios and2 helpers absent after Diplomacy is added.

## File responsibilities and shared interfaces

S=`src/OpenAntares.Simulation`, T=`tests/OpenAntares.Simulation.Tests`, G=`game`, V=`tests/OpenAntares.Game.Validation`. These prefixes expand to exact repository-relative paths; brace lists enumerate individual files. All new subsystem files live in S/Diplomacy; read existing adapters fresh before minimal edits. EmpireState is declared inside S/State/GameState.cs.

Shared types below are explicit planning decisions. Mutable records implement deep Clone; requester views/events use immutable arrays. Nullable foreign details must stay absent, not zero-filled IDs. New code is namespace OpenAntares.Simulation.Diplomacy.

- Task2: `DiplomacyDefinition` contains AlgorithmId and integer fields SetupCost=500, TradeOutput=100, Warmup=3, Minimum=10, Notice=2, OfferLifetime=3, Cooldown=5, StreakCap=10, StreakStep=2, CancellationPenalty=10, MemoryLifetime=10, AccordThreshold=0, TradeThreshold=10. Amounts Int64; durations/scores Int32. AlgorithmId=`cooperation_v1`. `ContentSet.Diplomacy` nullable; content reader/writer follow existing embedded-definition patterns. Duration bounds1..1000, amounts1..1000000, score magnitudes0..1000, checked long intermediate/bound consistency; no species keyed tuning.
- Task3: comparable `DiplomacyPair(EmpireId Low,EmpireId High)`, `OfferId(long Value)` local to pair; enum `AgreementKind {Settlement,Trade}`; `DiplomacyState` on GameState nullable, with sorted pair records. Each `DiplomacyPairState` owns NextOfferId, PublicRevision, per-owner PrivateRevision, directed `AttitudeState(Streak,LastCounterpartNoticeTurn?)`, pending `DiplomacyOffer`, agreements and per-owner `NextAiOfferTurn`. Offer stores Id/proposer/recipient/kind/created/expiry, optional StarId, proposer/recipient private `HubCommitment(ColonyId Hub,long Reserved)`, optional queued answer and Withdraw flag. Agreement stores kind/star, private hubs, ActivatedTurn, notice owner dates/end and warmup derived from activated date. Activation copies `HubOrigin(ColonyId Hub,EmpireId Owner,int ActivationTurn)` for historical missing-hub validation. Last-result summary bounded one per pair/kind; no transcript. `DiplomacyInitializer.Initialize(ContentSet,GameState):void`; `DiplomacyStateValidator.Validate(ContentSet,GameState):IReadOnlyList<string>`. No loader repair/init. `DiplomacyEvent` (declared here for resolver use) follows Task13 field contract; views needed by Task10 can be declared with state now and implemented by Task13.
- Task4: `DiplomacyCommand(EmpireId Issuer,int ExpectedTurn,EmpireId Other,long PublicRevision,long PrivateRevision,DiplomacyAction Action):Command`; actions `OfferSettlement(StarId)`, `OfferTrade(ColonyId)`, `Answer(OfferId,bool Accept,ColonyId? Hub)`, `RevokeAnswer(OfferId)`, `Withdraw(OfferId)`, `Terminate(AgreementKind)`. `DiplomacyCommandProcessor.Execute(ContentSet,GameState,DiplomacyCommand):CommandOutcome`. Task4 creates Reserved/Spendable/CanCommit contracts from Task5 and implements the minimal pending-state sum needed by command tests. Task5 audits and wires all economic consumers; rules11 default stays disabled until Task16.
- Task5: `DiplomacyReservations.Reserved(GameState,ColonyId):long`, `Spendable(GameState,ColonyId):long`, `CanCommit(GameState,ColonyId,long):bool`. Central pure reservation adapter counts each pending authorization exactly once. `DiplomacyHubQueries.Eligibility(ContentSet,GameState,EmpireId,ColonyId,OfferId? existing):HubEligibility` returns private Allowed/Reason/Bank/Reserved/Spendable; existing excludes that matching earmark from additional-funding requirements. No partner raw-state query exposed to AI.
- Tasks6/8/9: `DiplomacyFrame.Capture(ContentSet,GameState):DiplomacyFrame` freezes effects effective at snapshot; `DiplomacyResolver.ResolveOffers(ContentSet,GameState snapshot,GameState candidate):ImmutableArray<DiplomacyEvent>` resolves future activations and setup payments; `PrepareEffective(ContentSet,GameState snapshot,GameState candidate,DiplomacyFrame):DiplomacyFrame` removes due/lost effects before income; `Finalize(ContentSet,GameState snapshot,GameState candidate,DiplomacyFrame):ImmutableArray<DiplomacyEvent>` advances streaks/notice/publication. `DiplomacyEconomy.TradeOutput(ContentSet,DiplomacyFrame,ColonyId):long` and `DiplomacyRules.BlocksFounding(DiplomacyFrame,EmpireId,StarId):bool` are the single effect sources.
- Task10: `DiplomacyPlanner.Plan(ContentSet,EmpireView,Action<DiplomacyCommand>):void` receives only own projection. `DiplomacyTurnCoordinator.Prepare(ContentSet,GameState):RivalPlanningOutcome` runs plan/normal processor privately in EmpireId order with <=3*contacts submitted diplomacy commands per AI. Distinguish this added diplomacy budget from existing expansion budget; extend allowed commands only inside this coordinator. Unexpected rejection fails transaction; legal unavailability skips beforehand.
- Task11: internal `DiplomacyResolutionPhase {Planning,Snapshot,Offers,Economy,Founding,Contact,Validation}` and `TurnResolver.ResolveDiplomacy(ContentSet,GameState,Action<DiplomacyResolutionPhase>):TurnResolution`, plus `CommandProcessor.ExecuteDiplomacy(ContentSet,GameState,SetReadyCommand,Action<DiplomacyResolutionPhase>):CommandOutcome` internal test seams. No global hooks.
- Task13: `DiplomacyView(ImmutableArray<ContactDiplomacyView> Contacts)` appended to own EmpireView; ContactDiplomacyView contains counterpart identity, both directed score breakdowns, public terms/offer/result, public revision and only requester's private revision/hub data. `DiplomacyQueries.Preview(ContentSet,EmpireView,EmpireId,DiplomacyAction):DiplomacyPreview` returns Allowed, private RejectionCode, current revisions, public terms, own HubEligibility, exact/conditional estimates, disclosure notice and advisory policy reason. `DiplomacyEvent` has explicit Viewer/Other/Turn/Kind, nullable public Star and private own Hub, copied reason and no foreign diagnostic context. Public revisions change only for public pair facts; private revisions only for issuer-visible hub/authorization facts. Do not use a whole-state hash.

Task3 adds T/DiplomacyFixtures.cs for labelled pre-session rules11 supplements, not generated campaign evidence. `Create(string species="harl",int humans=1):GameSession` and `WithContact(GameSession):GameSession` establish valid fixtures before publication; Task4 adds real command helpers. All new record constructor/property names above are the canonical cross-task contracts; implementers may add owner-view fields for demonstrated spec requirements, never change meanings silently.

## Tasks

### Task 1: Freeze unchanged rules10 futures and preservation

**Files:** Create tools/DiplomacyBaselineCapture/{DiplomacyBaselineCapture.csproj,Program.cs}, tools/DiplomacyValidation/Run-Gate.ps1, T/DiplomacyCompatibilityTests.cs, T/Fixtures/rules10-diplomacy-baselines/; modify .gitattributes only for fixtures.

**Interfaces:** Consume unchanged SaveGame/GameSession/commands; produce hashed manifest of genuine source saves, typed continuation commands and checkpoints3/10.

- [ ] **Step1 — Write behavioral test:** `DiplomacyCompatibilityTests.FrozenRules10ArchivePredatesRules11`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.True(manifest.AllSourcesRules10); Assert.Equal(24, manifest.GeneratedSources); Assert.Equal(48, manifest.GeneratedCheckpoints);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyCompatibilityTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Before any product/content change, inventory main/index/refs/worktrees/stashes/protected owner files/QA/ignored evidence hashes per checkout. Capture all24 generated saves from reports/ship-design-integration-20261030/shipdesign-01/generated10 using unchanged engine (verify actual inventory before pinning24). Run each through10 boundaries, using recorded real acknowledgements/orders and checkpoints3/10. Retain inherited rules1..9 fixtures; add labelled queued/refit/Ready/contact supplements with exact actual counts. Run untouched Suite first. The new manifest test fails for absent capture, then passes with genuine old-engine output. Never generate an old oracle using modified rules.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyCompatibilityTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-01/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "test: freeze genuine rules10 diplomacy futures"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 2: Load bounded diplomacy definitions

**Files:** Create S/Diplomacy/Diplomacy{Content,ContentReader,ContentWriter}.cs, content/core/diplomacy.json, T/DiplomacyContentTests.cs; modify S/Content/{ContentSet,ContentLoader,ContentWriter}.cs.

**Interfaces:** Produce Task2 DiplomacyDefinition and nullable ContentSet.Diplomacy; loading has explicit requireDiplomacy=false gate until Task16.

- [ ] **Step1 — Write behavioral test:** `DiplomacyContentTests.CoreDiplomacyDefinitionIsExactAndStrict`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(500, d.SetupCost); Assert.Equal(100, d.TradeOutput); Assert.Equal(3, d.Warmup); Assert.Equal(10, d.Minimum); Assert.Equal(2, d.Notice);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyContentTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Implement exact shared definition/defaults. Reject duplicate/missing/unknown algorithm and invalid/overflowing bounds with source/entry/field errors, embed noncore valid values, and omit definitions in old-rule serialization. Missing definition rejects rules11 Solo too.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyContentTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-02/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: add validated diplomacy content"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 3: Add pair state, clones and historical invariants

**Files:** Create S/Diplomacy/Diplomacy{State,Initializer,StateValidator}.cs, T/{DiplomacyStateTests,DiplomacyFixtures}.cs; modify S/State/{GameState,GameStateValidator}.cs.

**Interfaces:** Consume Task2; produce shared Task3 types/initializer/validator and labelled Create/WithContact fixtures.

- [ ] **Step1 — Write behavioral test:** `DiplomacyStateTests.LiveMissingHubEndsWithoutValidationDeadlock`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Empty(Validate(liveMissingHub)); Assert.NotEmpty(Validate(pendingMissingHub)); Assert.NotSame(state.Diplomacy, state.Clone().Diplomacy);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyStateTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Canonical pair order, checked counters, at most one pending/two kinds, bounded history, deep clones, exact timestamps and ownership validation. Live historical hub missing/foreign is permitted only with a valid activation origin; pending commitments reject. Rules<=10 require absent feature. Expose no rules11 default yet.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyStateTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-03/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: model bounded diplomacy state"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 4: Implement offer and answer commands

**Files:** Create S/Diplomacy/{DiplomacyCommands,DiplomacyCommandProcessor}.cs, T/DiplomacyCommandTests.cs; modify S/Commands/CommandProcessor.cs.

**Interfaces:** Consume Tasks2/3; produce Task4 command/action/processor and reservation call contracts.

- [ ] **Step1 — Write behavioral test:** `DiplomacyCommandTests.RevokeAtExpiryDoesNotExtendOffer`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Null(revoked.Pending.Answer); Assert.Equal(originalExpiry, revoked.Pending.Expiry); Assert.Equal(0, recipientReserved); Assert.Equal(500, proposerReserved);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyCommandTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Route all actions through ordinary issuer/ExpectedTurn/readiness/revision/clone path. Test contact authority, crossed offers, identical retry no-ops, stale/forged/foreign requests, recipient-only Revoke for both answers, Withdraw after queued Accept and permanent closed IDs. Settlement/trade activation waits for resolution. Use compiling contracts before behavioral RED.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyCommandTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-04/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: add atomic diplomacy commands"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 5: Make production reservations authoritative everywhere

**Files:** Create S/Diplomacy/{DiplomacyReservations,DiplomacyHubQueries}.cs, T/DiplomacyReservationTests.cs; modify S/Economy/{EconomyCalculator,VesselProduction,EconomyResults}.cs, S/ShipDesigns/{RefitQueries,RefitEconomy,RefitCommandProcessor}.cs, S/Development/{GovernorPlanningQueries,DevelopmentCompletion}.cs, S/Fleets/{FleetPlanningQueries,AssemblyPlanner,RequestQueueAuthority}.cs, S/Commands/DevelopmentCommandProcessor.cs.

**Interfaces:** Produce Task5 Reserved/Spendable/CanCommit/Eligibility; consume Task4 pending authorizations.

- [ ] **Step1 — Write behavioral test:** `DiplomacyReservationTests.ExactPriceEarmarkActivates`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(500, Reserved(state, hub)); Assert.Equal(0, Spendable(state, hub)); Assert.True(Eligibility(content,state,owner,hub,offer).Allowed);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyReservationTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Audit every ProductionReserve reader/spender, including building/vessel/design queues, request costs, refits and forecasts, and record call-site checklist. Prevent double earmarks, setup theft, discounts, implicit queue clearing or auto overwrite; existing earmark counts toward its own activation. Setup hub excludes active refit/assembly but ordinary projects continue. Test source500 reserved500, queued project499/500 costs, command/manual workforce changes, no-op and bank overflow.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyReservationTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-05/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: reserve trade setup production across consumers"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 6: Resolve offers before economy with future activation

**Files:** Create S/Diplomacy/DiplomacyResolver.cs, T/DiplomacyOfferResolutionTests.cs.

**Interfaces:** Consume Tasks3..5; produce ResolveOffers and DiplomacyFrame.Capture skeleton with immutable current/future agreement distinction.

- [ ] **Step1 — Write behavioral test:** `DiplomacyOfferResolutionTests.ThirdBoundaryAnswerWinsExpiryAndWithdrawalWinsAnswer`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.True(validThirdAnswer.ActivatesNextTurn); Assert.Empty(withdrawn.NewAgreements); Assert.Equal(0, expired.Reserved);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyOfferResolutionTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Stable pair order: Withdraw, answers, expiry. Revalidate own observed terms and own hub conditions; expected changed eligibility reports failed offer, internal inconsistency aborts. Decline/expiry/failure release before ordinary completions. Test same-boundary project affordability after release and conditional forecast when hidden partner decision is unknown.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyOfferResolutionTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-06/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: resolve diplomatic offers deterministically"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 7: Enforce settlement accords for humans and rivals

**Files:** Create S/Diplomacy/DiplomacyRules.cs, T/DiplomacySettlementTests.cs; modify S/Colonization/{FoundingRules,ColonizationResolver}.cs, S/Rivals/{ExpansionRivalPlanner,RivalPlanningQueries}.cs and own founding view.

**Interfaces:** Consume frozen DiplomacyFrame; produce BlocksFounding(frame,owner,star).

- [ ] **Step1 — Write behavioral test:** `DiplomacySettlementTests.AccordActivatesDuringFounderTransit`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.False(founder.Consumed); Assert.Equal(beforeCargo, founder.Cargo); Assert.Equal("diplomacy_settlement_accord", result.AttentionReason); Assert.False(BlocksFounding(frame,thirdEmpire,star));
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacySettlementTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Apply block only from first effective boundary in common preview/validation/resolution. Existing intents pause with Attention and cargo intact; human can redirect and AI skips blocked target without rejected-order loop. Passage/survey/existing colonies remain legal. Current-own-observation terms only; explicit response disclosure never exposes other systems or counts. Test agreement ends and prior intent can resolve normally if still legal.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacySettlementTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-07/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: enforce mutual settlement accords"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 8: Charge trade setup once and earn after warmup

**Files:** Create S/Diplomacy/DiplomacyEconomy.cs, T/DiplomacyTradeTests.cs; modify S/Diplomacy/DiplomacyResolver.cs, S/Economy/{EconomyCalculator,EconomyResults}.cs.

**Interfaces:** Consume reservations/frozen frame; produce TradeOutput and complete ResolveOffers setup payment branch.

- [ ] **Step1 — Write behavioral test:** `DiplomacyTradeTests.SetupPaymentCannotDoubleCharge`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(500, eachSetupPaid); Assert.Equal(0, EarnedAt(T+4)); Assert.Equal(100, EarnedAt(T+5)); Assert.Equal(starting+ordinaryNet+100-cost, ending);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyTradeTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Revalidate each hub before completion using its own matching earmark, deduct both or neither. Add income after net workforce/support penalties before ordinary completion; explicitly separate setup payment from project cost and reserve breakdown. Preserve production accounting identity. Test zero ordinary output after activation, active refit after setup, exact warmup, repeated resolve from same immutable input, failed partner activation and loss of active project affordability.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyTradeTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-08/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: account for paid production trade"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 9: Finalize attitudes, notices and hub loss

**Files:** Create S/Diplomacy/DiplomacyAttitudes.cs, T/DiplomacyDurationTests.cs; complete S/Diplomacy/DiplomacyResolver.cs.

**Interfaces:** Produce PrepareEffective and Finalize; directed score helper Calculate(DiplomacyDefinition,int streak,int? lastNotice,int turn):int.

- [ ] **Step1 — Write behavioral test:** `DiplomacyDurationTests.MinimumAndTwoBoundaryNoticeHaveExactDates`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(T+11, firstLegalNoticeTurn); Assert.Equal(T+13, agreementEnd); Assert.Equal(20, scoreAtStreak10); Assert.Equal(-10, scoreAtZeroStreakWithMemory);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyDurationTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Count only frozen effective agreements, one streak increment regardless of two kinds. No agreement resets streak. Notice memory starts publication, lasts10, does not stack; both notices affect both directions. Lost historical hub ends bilateral trade before income without penalty/refund/private disclosure. Test memory expiry, warmup counts as cooperation, notice rejection at9 completed boundaries, and saved notice resumes exact remaining effects.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyDurationTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-09/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: finalize diplomacy duration and attitudes"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 10: Add conservative bounded rival diplomacy

**Files:** Create S/Diplomacy/{DiplomacyPlanner,DiplomacyTurnCoordinator}.cs, T/DiplomacyAiTests.cs; modify S/Rivals/RivalTurnPlanner.cs only at integration adapter.

**Interfaces:** Consume own EmpireView plus Task13 DTO contract introduced now as compiling internal projection; produce Plan/Prepare shared signatures.

- [ ] **Step1 — Write behavioral test:** `DiplomacyAiTests.RivalUsesOwnKnowledgeThresholdsAndCooldown`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(0, policy.AccordThreshold); Assert.Equal(10, policy.TradeThreshold); Assert.True(submitted<=3*contacts); Assert.Equal(beforeRandom, afterRandom);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyAiTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Responses before new offer priority trade then accord; lowest eligible colony/StarId; AI requires own colony for accord, no unsolicited termination, no fake funds. Five-boundary failed-offer cooldown, incoming responses unaffected. Expansion separate budget retained. Test unseen foreign mutation cannot change decisions/tokens, same policy all4species, multiple AI plan isolation, and ordinary command rejection aborts rather than partial continue.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyAiTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-10/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: plan legal rival diplomacy"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 11: Integrate frozen phases and mutual contact atomically

**Files:** Modify S/Turns/TurnResolver.cs, S/Commands/CommandProcessor.cs, S/Knowledge/ContactUpdater.cs; create T/DiplomacyTurnTests.cs and S/Diplomacy/DiplomacyReports.cs.

**Interfaces:** Consume Tasks6..10; produce internal ResolveDiplomacy/ExecuteDiplomacy hooks and reciprocal identity contact/events.

- [ ] **Step1 — Write behavioral test:** `DiplomacyTurnTests.FinalReadyDiplomacyFailureRestoresOriginalState`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Same(original, failed.State); Assert.Equal(beforeBytes, Save(failed.State)); Assert.Equal(1, firstContactNoticesPerViewer); Assert.Null(reciprocalNotice.Star);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyTurnTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Insert exactly approved phase sequence: planning, frozen snapshot, offer/setup phase, due/lost effect filtering, economy, existing movement/combat/founding, finalize/advance, observed contact, validation/publication. Use the post-offer candidate banks as economic starting reserves, while effects come only from the pre-offer frozen frame: payments cannot be overwritten by old snapshot banks. New agreement cannot alter this turn founding/income; new contact waits next turn to negotiate. Failure-inject all named seams including after both payments/founding/contact. Public Resolve and final human Ready agree, nonfinal Ready/no-op/save never plans AI. Test multi-human pending private mutations do not change another requester token.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyTurnTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-11/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: resolve diplomacy in atomic turns"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 12: Persist strict rules11 state and legacy futures

**Files:** Create S/Saves/DiplomacySaveData.cs, T/DiplomacySaveTests.cs; modify S/Saves/SaveGame.cs and embedded content codec adapters.

**Interfaces:** Consume full state/definitions; produce writer12 and exact67 pair table while new-game default still10 until Task16.

- [ ] **Step1 — Write behavioral test:** `DiplomacySaveTests.All67PairsAndLiveDiplomacyStagesRoundTrip`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(67, acceptedPairs.Count); Assert.False(Accepts(11,11)); Assert.Equal(beforeCanonical, afterCanonical);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacySaveTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Save offered/Accept/Decline/revoked/withdrawn/earmarked/warmup/minimum/notice/cooldown/historical-loss states and exact futures. Strictly reject malformed bounds/duplicates/asymmetry/extra earmarks/overflow/unsupported algorithm and field/rules combinations. Omit diplomacy for old rules; replay all genuine rules1..10 captures from Task1 plus inherited manifests; old contacts stay one-sided without migration. Earmark restoration cannot overdraw or double charge.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacySaveTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-12/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: persist diplomacy without changing old campaigns"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 13: Publish private views, previews and reports

**Files:** Create S/Diplomacy/{DiplomacyViews,DiplomacyQueries}.cs, T/DiplomacyPrivacyTests.cs; modify S/Knowledge/{EmpireView,EmpireViewProjector}.cs and S/Diplomacy/DiplomacyReports.cs.

**Interfaces:** Produce full Task13 view/preview/event contract; replace Task10 compiling projection with authoritative requester projection.

- [ ] **Step1 — Write behavioral test:** `DiplomacyPrivacyTests.HiddenPartnerMutationDoesNotChangeOwnToken`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(before.PublicRevision, after.PublicRevision); Assert.Equal(before.PrivateRevision, after.PrivateRevision); Assert.DoesNotContain(foreignHubId, renderedOwnDiagnostics);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyPrivacyTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Both directed attitude reasons/remaining durations, own hub/bank only, named offered accord system with explicit disclosure, generic partner failure. Public revision excludes private orders; private revision covers only own material facts. No map/colony-count/bank/hidden-presence oracle. Conditional release estimates and deterministic AI advice are labelled advisory. Include third-party filtered reports/exports, no mutable state escaping, query no RNG/ID change and bounded summaries.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyPrivacyTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-13/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: expose truthful private diplomacy views"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 14: Build contact and agreement navigation

**Files:** Create G/Components/{DiplomacyDialog,DiplomacyContactPanel}.cs; modify G/Screens/GameScreen.cs, G/Components/{ContactsPanel,TurnReportDialog}.cs and G/theme/openantares_theme.tres; create V/DiplomacyGraphicalValidation.cs initial scenario.

**Interfaces:** Consume Task13 immutable projections/preview and real Task4 commands; UI local selections never mutate simulation.

- [ ] **Step1 — Write behavioral test:** `DiplomacyPrivacyTests.ContactNavigationShowsOnlyKnownIdentities`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Empty(soloView.Contacts); Assert.Equal(knownCount, shownRows); Assert.Null(remoteFirstContactLocation);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyPrivacyTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Add reachable Diplomacy navigation, Solo/unmet explanation, culturally original introduction, both reason breakdowns, public dates and per-owner controls. Start opt-in native control assertions for navigation, repeated open/resize, reports not replayed and narrow windows800/1920. Reread shared theme; use type variations/containers/anchors/text labels/keyboard focus. Build/Normal plus existing relevant scenarios.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyPrivacyTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-14/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: add diplomacy contact and agreement screens"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 15: Build offers and production-disclosure controls

**Files:** Create G/Components/DiplomacyOfferDialog.cs; modify G/Components/{DiplomacyDialog,ColonyPanel,ConstructionQueueEditor,RefitDialog,VesselPanel}.cs, G/Screens/GameScreen.cs and V/DiplomacyGraphicalValidation.cs.

**Interfaces:** Consume Task13 preview/revisions, own HubEligibility and commands; normal command rejection refreshes view without losing valid local selection.

- [ ] **Step1 — Write behavioral test:** `DiplomacyCommandTests.OfferControlsUseCurrentOwnTermsAndExplicitAnswers`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.True(acceptRequiresOwnHub); Assert.False(readyAutoAccepts); Assert.Equal("500 reserved; 0 spendable", exactPriceReadout);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyCommandTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Wire explicit named-system disclosure confirmation; trade hub/payment/warmup/conditional estimates; Accept/Decline/Revoke/Withdraw/Terminate with exact dates/no silent answers. Add Ready informational badge, founding Attention/link and earmarked/spendable reserve across queues/refits. Native test real controls for stale revision rejection, expiry while open, saved answer reload, cancellation warning and focus/scroll800px. Do not infer physical input/accessibility acceptance.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyCommandTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-15/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: add diplomatic offer and trade controls"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 16: Enable new campaigns and validation isolation

**Files:** Modify S/Rules.cs, S/Galaxy/GalaxyGenerator.cs, G/ContentFiles.cs, G/Screens/NewGameScreen.cs, V/{Run-Validation.ps1,Validation.targets,Verify-NormalAssembly.ps1,README.md}; create V/DiplomacyGraphicalValidation.tscn and T/DiplomacyGenerationTests.cs.

**Interfaces:** Consume initializer/codec/fully integrated commands; enable rules11 new-game default and definition requirement only now.

- [ ] **Step1 — Write behavioral test:** `DiplomacyGenerationTests.Rules11GenerationPreservesGalaxyIdsAndRandom`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(rules10Galaxy, rules11Galaxy); Assert.Equal(rules10Ids, rules11Ids); Assert.Equal(rules10Random, rules11Random); Assert.Equal(11,newDefault.RulesVersion);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyGenerationTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Generate Solo/One/all4species with identical prior geometry/IDs/RNG aside from new metadata, no new setup chooser. Require definition even Solo; old explicit rules creation/loads unchanged. Register opt-in11th scenario and preserve2helpers; prove actual opt-in assembly fails Normal checker, finally rebuild and prove11+2 absent. Build/reload all content paths.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyGenerationTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-16/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "feat: enable rules11 diplomacy campaigns"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 17: Prove earned campaigns and full regression

**Files:** Complete V/DiplomacyGraphicalValidation.cs; create reports/DIPLOMACY_VALIDATION_2026-10-30.md and execution evidence; modify T/DiplomacyCompatibilityTests.cs only for final archive verification.

**Interfaces:** Consume Tasks1..16; produce source-scoped machine summary, generated saves/commands/screenshots, full criterion map.

- [ ] **Step1 — Write behavioral test:** `DiplomacyCompatibilityTests.GenuineLegacyFuturesRemainExact`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(0, legacyFutureMismatches); Assert.Equal(4, earnedSpeciesCampaigns); Assert.True(allStageReloadsMatch);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyCompatibilityTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Run at least4 genuinely generated rules11 campaigns (one per human species) through paid settlement/ship progression, shared-system observation/accord, five trust boundaries, two earned500 setups, warmup/payout and later notice/founding. Search finite documented seeds with ordinary commands; no injection of colonies/payments/research/trust. Save pending/answer/earmark/warmup/notice stages and replay through actual Main Save/Load. Label multi-human/loss/overflow supplements. Run full Suite and all11 native scenarios sequentially, actual negative control/ordinary11+2 exclusion,0engine diagnostics/clean normal builds. Preserve historical latest outputs via wrapper; failed attempts remain.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyCompatibilityTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-17/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "test: validate earned diplomacy campaigns and regressions"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

### Task 18: Run one final review and close implementation evidence

**Files:** Modify reports/DIPLOMACY_VALIDATION_2026-10-30.md, execution ledger/machine summary, PROJECT_STATUS.md, docs/{SPECIFICATIONS,ACCEPTANCE_CRITERIA,IMPLEMENTATION_PLAN}.md only to match actual results.

**Interfaces:** Consume approved design/plan, final product diff and exact source-scoped evidence; produce one independent whole-branch review, one regression-first correction pass and honest statuses.

- [ ] **Step1 — Write behavioral test:** `DiplomacyCompatibilityTests.FinalEvidenceReferencesActualVerifiedSource`. With a valid compiling fixture, pin these results (adapt variable binding to actual test fixture; assertion values are fixed):

```csharp
Assert.Equal(finalProductSha, summary.ProductSource); Assert.Empty(summary.UnresolvedRequiredCriteriaWhenClaimedVerified);
```

- [ ] **Step2 — Prove RED:** Run Focused(DiplomacyCompatibilityTests) through the gate wrapper; require the named behavioral assertion to fail for the missing behavior, not a build error. For UI/evidence tasks, add equivalent actual scenario assertion and capture its failing result; previously green simulation assertions are retained regressions.
- [ ] **Step3 — Implement:** Dispatch ONE fresh whole-branch implementation reviewer after all tasks/gates, no per-task agents or second review. Resolve findings regression-first within approved scope; rerun affected focused/Suite/native gates and final Normal after changes. Do not call implementation Verified with omitted required criteria. Preserve17 parent totals unless independently justified. Record human/platform/balance/security/release limits, preserve branch/worktree/QA/recovery and hand off for owner disposition; do not merge/push/release/cleanup.
- [ ] **Step4 — Verify GREEN:** Run Focused(DiplomacyCompatibilityTests), Suite and Build; require exit0/all tests passed. Run the owning native scenario for UI/phase integration changes, then Normal. Task1 instead runs untouched baseline plus manifest checks; Task17 owns all11 scenarios; Task18 reruns affected gates only after corrections. Keep every command/source/exit/log in task-18/attempt-NN.
- [ ] **Step5 — Commit:** Stage only the exact created/modified paths listed above (expand S/T/G/V prefixes and brace lists before staging) and `git commit -m "docs: record diplomacy validation and final review"`; update the canonical ledger with commit SHA, evidence and actual result. No claims based on historical counts.

## Coverage and handoff

| Approved criterion | Owning tasks |
| --- | --- |
| DIP-AC-001 Contact | 3,11,13,14,16,17 |
| DIP-AC-002 Attitude | 9,13,14,17 |
| DIP-AC-003 Offers | 4,6,11,12,15 |
| DIP-AC-004 Accord | 6,7,10,15,17 |
| DIP-AC-005 Trade | 5,6,8,12,15,17 |
| DIP-AC-006 Termination | 9,11,12,15,17 |
| DIP-AC-007 Rival | 7,10,11,17 |
| DIP-AC-008 Persistence/content | 1,2,3,12,16,17 |
| DIP-AC-009 Atomicity/privacy | 4,5,10,11,12,13,17 |
| DIP-AC-010 Earned campaign | 17 |
| DIP-AC-011 Native/regression | 14,15,16,17,18 |
| DIP-AC-012 Full design approval | ADR-024; satisfied for design only |

All approved DIP-001..008 sections have owners above. This plan is self-reviewed in the separate planning review report; it has not been executed. Next: owner review of this concrete18-task plan. Execution method remains Native inline with one final fresh implementation reviewer; do not ask the owner to choose the method again.
