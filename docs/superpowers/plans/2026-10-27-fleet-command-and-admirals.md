# Fleet Command and Named Admirals Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the player one place to find and direct ships, place finite construction/assembly requests, and appoint named admirals with the approved bonuses, careers and recovery rules.

**Architecture:** Extend the deterministic simulation with rules8-only per-empire fleet, request and officer state. Preserve ordinary vessel movement, governor selection, cargo legality and scout consent as shared authorities. Prepare accepted finite orders on a private candidate, resolve from immutable snapshot facts, then validate and publish once. Present immutable own-view projections in a central theme-based window.

**Tech Stack:** Existing C#/.NET8 simulation and xUnit; Godot4.7.2 .NET; embedded JSON and existing PCG32. No dependencies, art requirement or combat framework.

**Spec:** [Approved complete gameplay specification](../specs/2026-10-27-fleet-command-and-admirals-design.md), [ADR-018 full owner approval](../../DECISIONS/ADR-018-approved-fleet-command-and-admirals-design.md), [readable gameplay summary](../../../reports/FLEET_COMMAND_GAMEPLAY_REVIEW_2026-10-27.md).

**Gate:** Owner “0approved” on2026-10-27 approves the full design, including the starter defaults. M8-D/FLT-AC-012 Verified for design only; M8-P Verified after owner approval of this separate plan at7d175ad; M8-I In Progress and functional acceptance pending. Use **superpowers:executing-plans, Native inline**, with **no per-task agents** and **one final fresh whole-branch implementation reviewer**. The method is already chosen; do not ask again. Plan review is complete; execute continuously from the ledger. No main merge, push, release or cleanup is authorized.

## Global Constraints

- Preserve every approved gameplay number and FLT-001..009 contract. Officers are optional; the command window and standing orders work without them. Broader design/upgrade automation and the combat/retreat/morale note remain future scope.
- “Only one controller owns any pending project/ship order.” Reuse ordinary commands, the existing governor selector and existing rival planner. Do not add a second construction or AI controller.
- “No fleet-wide implicit risk waiver.” Crisis/manual authority and scout safety outrank fleet goals. Never acknowledge, Study, Remain, change doctrine/posture or authorize cargo implicitly.
- “Do not automatically resend a withdrawing ship to that rally.” Safety-paused requests retain surviving bindings, require explicit legal resume, and earn no completion XP while blocked.
- Keep rules1..7 formulas, content, IDs, PRNG and futures unchanged. New content/state is mandatory only for rules8. Writer9 accepts exactly37 enumerated pairs: existing29 plus(9,1)..(9,8); never(8,8). No automatic campaign upgrade.
- Keep rules current7 until Task16 and writer8 until Task13. Task1 capture precedes **all** product/content/default changes. Never regenerate expected old futures to bless a difference.
- Checked local per-empire counters; immutable snapshot bonuses; deterministic ordinal/ID ordering; integer/Int128 arithmetic. No time, System.Random, GetHashCode or new global entity-ID allocation for fleet/request/officer records.
- Commands, Ready and resolution are copy/validate/publish transactions. Rejection/no-op preserves the original reference, Ready, counters, streams, receipts, queues and reports. A late error also preserves another human's accepted planning.
- Pure queries, window opening, save/load and preview consume no RNG or authority. Foreign facts come only from existing permitted views. Simulation references no Godot.
- Preserve main c554797, all four uncommitted owner files, empty main index, all seven worktrees/branches, frozen archives, QA saves/preferences and recovery artifacts. Reuse this isolated checkout; no deletion, stash replay, reset or owner-file staging.
- New UI follows the shared theme and scrolling/focus containers at800x600 and1920x1080. Automated controls/signals are local functional/layout evidence, not physical-input, comprehension, balance, export or release acceptance.

## Review Focus

1. **Danger discovered after dispatch or withdrawal:** an accepted rally becomes known unsafe, including after reload/arrival. Preserve living hull bindings and committed transit, stop retry/replacement, require explicit legal resume and suppress blocked completion XP. Tasks7/10/13/15 own tests.
2. **Priority plus restricted construction authority:** Urgent meets an active blocked project and an off governor with a stored template; Organizer assignment/pause changes price. Preserve tokens/reserve/unrelated order, select only finite authorized work, never grant template authority or a second completion. Tasks6/7/8 own tests.
3. **Two-human final Ready/direct Resolve failure:** one human already prepared and acknowledged risk; the second human's late phase fails. No double preparation, duplicate queue binding/XP or receipt change, and exact rollback to the enclosing call's original reference/bytes. Tasks9/10/11/12 own phase fault tests.
4. **Founding versus loss and identity:** consuming an occupied founder is not death; Wenth retains the original body, while actual mortal recovery without a colony becomes Missing. Colony loss during a countdown cannot teleport the officer. Tasks5/10/12/13 own tests.
5. **Same-boundary promotion/technology and dedup:** survey XP8→10 or28→30, concurrent surveys, research completion and assembly splitting use old snapshot bonuses; committed transit and hull odds remain unchanged. Tasks4/8/11/12/14 own literal tests.

---

## Baseline, commands and execution ledger

Reuse `.worktrees/colony-development-design`, currently branch `codex/fleet-command-design`, product identical to main c554797. After plan approval, retain the design branch and create execution branch `codex/fleet-command` from the approved plan commit in this checkout; if that branch already exists, inspect and reuse its actual progress rather than reset it. Track each task, semantic RED/GREEN, commands/exit codes and commits in `.superpowers/sdd/2026-10-27-fleet-command-and-admirals/progress.md`. No such execution has begun during planning.

Historical main evidence:1398 simulation tests, all7 native scenarios/4491 records, ordinary rebuild0 warnings/errors and exclusion of7 scenario classes plus `FrozenContent` and `ScoutUiControlChecks`. This is M7 evidence, not fleet validation. Genuine new rules7 future capture has **not** run.

Run from the execution checkout. The exact reusable commands are:

```powershell
# Focused(Type): substitute the test class named in the owning task.
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers --filter FullyQualifiedName~Type
# Suite (builds the tests; do not reuse a stale no-build assembly).
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers
# Build / final Normal respectively.
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll'
# Runtime: replace Fleet and the fresh output folder for each sequential scenario/attempt.
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario Fleet -OutputDirectory reports/fleet-execution/fleet-attempt-01
```

Verify the existing Godot executable first. Runtime scenarios share a build assembly and run sequentially. Keep normal dependency audit enabled; restore only when required and record it. A no-restore build is not a new vulnerability scan.

Write each task's verification record to `reports/fleet-execution/task-XX.md` (01..17) and retain its raw evidence under the same execution report directory. Exact staging commands below list only that task's planned files and the dedicated ledger/evidence folders; inspect the staged diff and leave any unrelated changes unstaged. Minimal theme or fresh Godot .uid additions may be staged only by their actual individual path when needed. A review correction in Task17 similarly stages only its actual reproduced fix/test paths, never a broad source folder.

Every task below includes five required steps: behavioral RED, exact focused invocation, minimal implementation, GREEN/regression evidence, explicit-path commit. `Focused(Type)`, `Suite`, `Build`, `Normal` and `Runtime` refer to the exact commands above. Do not claim counts in advance. Missing APIs/compile errors are not semantic RED: add only the declared minimal compiling contracts necessary to reach the behavioral assertion. Tests should exercise player-visible rules/transactions, not mirror helper implementation. Whole-suite success gates each completed simulation task; UI-only task uses Build plus its actual native gate. Preserve failed attempts too.

## File responsibility map

Paths are repository-relative. S=`src/OpenAntares.Simulation`, T=`tests/OpenAntares.Simulation.Tests`, G=`game`, V=`tests/OpenAntares.Game.Validation`. Each new path is owned by the numbered task; later tasks extend that path through the contract below.

| Existing files to modify | Responsibility / owning tasks |
| --- | --- |
| `.gitattributes`; `T/CompatibilityFixtures.cs` | Raw archive bytes and genuine old futures,1/17 |
| `S/Rules.cs`; `S/Content/ContentSet.cs`, `ContentLoader.cs`, `ContentWriter.cs` | Explicit rules8 feature gating and embedded data,2/16 |
| `S/Commands/CommandProcessor.cs`, `S/State/GameStateValidator.cs`, `S/Economy/EconomyCalculator.cs`, `S/Rivals/ExpansionRivalPlanner.cs`, `G/Components/ResearchBar.cs` | Reject/hide rules8-only research in earlier rules,2/16 |
| `content/core/species.json`, `technologies.json`; `G/ContentFiles.cs` | Declared embodiment and recovery technology,2/16 |
| `S/State/GameState.cs`, `VesselState.cs`, `GameStateValidator.cs` | Optional rules8 slots, deep clone and validation,3 |
| `S/Knowledge/EmpireView.cs`, `EmpireViewProjector.cs` | Immutable own state,3/6/11 |
| `S/Galaxy/GalaxyGenerator.cs` | Rules8 initialization after unchanged ordinary generation,3/14/16 |
| `S/Commands/CommandProcessor.cs`, `ExpansionCommands.cs`, `DevelopmentCommandProcessor.cs` | Shared atomic commands, explicit detach/manual takeover and Ready,4/5/7/9 |
| `S/Movement/MovementResolver.cs` | Use rules8 captured cohort duration at launch; retain individual legacy/safety routes,4/10 |
| `S/Development/GovernorPlanner.cs`, `GovernorPlanningQueries.cs`, `GovernorTurnCoordinator.cs`, `DevelopmentCompletion.cs` | One selector, finite request authority and token consumption,6/7/9/10 |
| `S/Economy/VesselProduction.cs` | Snapshot request price through the ordinary production calculation,8 |
| `S/Scouting/ScoutTurnCoordinator.cs`, `ScoutReadyQueries.cs`, `AmebaResolutionContext.cs` | Safety cohort normalization and exact consequence receipts,9/10/11 |
| `S/Scouting/ScoutRecords.cs`, `ScoutQueries.cs`; `S/Saves/ScoutSaveData.cs` | Rules8-only material officer/cohort consequence fingerprint without changing hull odds or legacy tokens,4/5/11/13 |
| `S/Turns/TurnResolver.cs` | Snapshot, precise integration phases, fleet report and fault hooks,10/11/12 |
| `S/Rivals/RivalTurnPlanner.cs`, `RivalVesselPlanner.cs` | Existing planner's one-ship fleet adapter only,14 |
| `S/Saves/SaveGame.cs`, `ExpansionSaveData.cs` | Writer9, explicit matrix and optional rules8 codec dispatch,13 |
| `G/Screens/GameScreen.cs`, `NewGameScreen.cs`; `G/Components/VesselPanel.cs`, `TurnReportDialog.cs` | Central entry, existing inspection/consent/founding actions,15/16 |
| `V/Run-Validation.ps1`, `Validation.targets`, `Verify-NormalAssembly.ps1`, `README.md` | Eighth scenario and actual helper exclusions,15/17 |

New simulation files are in `S/Fleets/`: `FleetContent.cs`/`FleetContentLoader.cs`/`FleetInitializer.cs`(2, initialization extended3/14), `FleetState.cs`/`FleetStateValidator.cs`/`FleetViews.cs`/`FleetQueries.cs`(3), `FleetCommands.cs`/`FleetCommandProcessor.cs`/`FleetTravel.cs`/`OfficerBonuses.cs`(4, bonuses extended8/11), `OfficerCommands.cs`/`OfficerCommandProcessor.cs`(5), `IFleetPlanningContext.cs`/`FleetPlanningQueries.cs`/`AssemblyPlanner.cs`(6), `AssemblyCommands.cs`/`AssemblyCommandProcessor.cs`/`RequestQueueAuthority.cs`(7), `FleetPlanner.cs`/`FleetTurnCoordinator.cs`(9), `FleetResolutionContext.cs`/`FleetResolver.cs`(10), `OfficerProgression.cs`/`FleetReports.cs`(11), `OfficerRecovery.cs`(12). New codec `S/Saves/FleetSaveData.cs`(13). New content `content/core/fleet_command.json`(2). New UI `G/Components/FleetCommandDialog.cs`, `AssemblyRequestDialog.cs`, `OfficerDetailDialog.cs` and `G/Presentation/FleetNotificationPreferences.cs`(15); new `V/FleetGraphicalValidation.cs`/`.tscn`(15). Avoid another subsystem's generic framework or unrelated refactor.

New tests `T/Fleet{Content,State,Query,Command,Travel,Officer,Planning,Request,Economy,Ready,Resolution,Progression,Recovery,Save,Ai,Generation,Compatibility,Integration}Tests.cs` belong to the matching task. `T/FleetFixtures.cs` supplies explicitly labelled rare states; `T/Fixtures/fleet-content.json` is generated from reviewed embedded content, not a replacement old fixture. `T/Fixtures/rules7-fleet-baselines/` is immutable Task1 output. Capture tooling is a small isolated console project `tools/FleetBaselineCapture/{FleetBaselineCapture.csproj,Program.cs}` referencing the unchanged simulation, not included in the game or solution build.

## Shared contracts and ordering

These are planned APIs, not claims about current code. Types live in `OpenAntares.Simulation.Fleets`; use existing IDs, `ProductionTarget`, `Workforce`, `CommandOutcome`, `TurnReport`, `ContentSet`, `EmpireView` and `GameState` unchanged where possible.

- Local positive `readonly record struct FleetId(int Value)`, `RequestId(int Value)`, `OfficerId(int Value)`; keys include their owning EmpireId. `FleetEmpireState` owns checked next-ID/event counters, fleet/request/officer collections, candidate/catalog cursors, optional future-commission doctrine override, recovery PCG state/draw count and reward-credit sets. Add `EmpireState.Fleets` nullable and `VesselState.Fleet` nullable. All must be absent/null for rules1..7.
- `FleetRecord`: ID/name/member IDs/flagship/optional officer/order/pending or committed cohort provenance. `FleetOrderKind`: Hold/Assemble/Explore/Return; `FleetOrder(Kind, StarId? Destination)`. Store transit provenance once per fleet and validate each member's ordinary `VesselTransit` matches; safety-detached vessels retain ordinary individual transit and lose cohort provenance. Never allocate a global entity ID for a fleet.
- `AssemblyRequest`: ID/revision/name/rally/optional rallyFleet/reservedFleetSlot/priority/status/colonies/slots/coordinator/initialXpRecipient. `RequestStatus`: Active/Paused/SafetyPaused/Complete/Cancelled. `SlotStatus`: Queued/Built/Travelling/Arrived/Missing/BlockedSafety. A slot has stable ordinal/role, optional ColonyId/queue token/VesselId, original-built provenance/award key and explicit contribution pause. Safety blocking preserves the prior progress state in `ResumeStatus`, bindings and observed reason; Missing means genuinely lost/detached, never merely withdrawing. Request revision increments on actual edits.
- `OfficerRecord`: ID/profile/culture/embodiment/sourceColony/specialty/XP/doctrine/status/location/flagship/request/commission or recovery deadline, final summary and bounded history. `OfficerStatus`: Commissioning/Posted/Commanding/Coordinating/Recovering/Missing/Dead/Retired. Status is exclusive; mortal `sourceColony` is null, Wenth source is immutable. `OfficerDoctrine`: RecoverCommand/LastWatch. `OfficerSpecialty`: Navigator/Surveyor/Organizer. Rewards key actual first survey/habitat/original new hull, not a UI report ID.
- `FleetCommand : Command` wraps `FleetAction`; action variants Create/Rename/Disband/Merge/Split/SetFlagship/SetOrder/MoveSelection/DetachSelection/DismissNewMarker. Selection is immutable arrays of VesselId/FleetId, deduplicated and bounded by own vessels; validation never silently applies a subset. All mutation enters `CommandProcessor.Execute(ContentSet,GameState,Command)`. `FleetCommandProcessor.Execute` handles only fleet actions on a private candidate and returns ordinary `CommandOutcome`.
- `OfficerCommand : Command` wraps `OfficerAction`: Commission/Dismiss/Post/AssignFlagship/Transfer/Remove/SetDoctrine/ApplyDoctrineToSelection/SetFutureDoctrine. Commission carries explicit candidate profile or owned Wenth source/specialty/posting colony. Transfer names source/destination flagship; Post requires actual co-location, never teleports. `OfficerCommandProcessor.Execute` uses the same transaction contract.
- `AssemblyDraft(string Name, StarId Rally, FleetId? RallyFleet, ImmutableArray<string> Roles, ImmutableArray<ColonyId> Colonies, RequestPriority Priority, ImmutableArray<VesselId> AcceptedIdleVessels, bool AuthorizeCargo, OfficerId? Coordinator)`. Priority Normal/Urgent. `AssemblyCommand : Command` wraps Accept(draft,previewFingerprint)/Pause/Resume/SetPriority/ChangeRally/DetachSlot/Amend(revision,draft,fingerprint)/Cancel(KeepQueue or RemoveUnstarted). Edits carry RequestId/expected revision; Resume/ChangeRally/Amend use fresh preview fingerprints. A fingerprint represents exact own visible inputs, not a grant of future consent.
- `FleetQueries.Project(ContentSet,GameState,EmpireId) -> FleetEmpireView` (also embedded in EmpireView), immutable own fleets/ships/officers/requests/history. `FleetQueries.PreviewAction(ContentSet,EmpireView,FleetAction) -> FleetActionPreview` returns immutable consequences/rejections/fingerprint. Queries know no foreign world state. FleetQuery/preview record definitions live in `FleetViews.cs`.
- Keep officer/cohort encounter consequences structured in FleetViews for display; add optional `string? FleetConsequenceFingerprint { get; init; }` to AmebaAttentionToken, mandatory exactly when rules8 has a material fleet/officer consequence. Canonical own-fact fingerprint covers designated officer/XP/unlock prospect, doctrine/recovery equipment/delay, affected cohort/detachments and actual route duration/research consequence; no rolls or hidden world facts. Do not put fresh ImmutableArray reference equality into the receipt token. Repeated reads of identical consequences produce exactly equal tokens. ScoutQueries retains base role speed for existing hull-risk formulas; Navigator changes duration only. Rules1..7 token/codec shape remains unchanged; strict writer9 fleet/scout codec owns the new optional field.
- `IFleetPlanningContext` exposes only `ContentSet Content`, `EmpireView View`, and `ColonyEconomy? Forecast(ColonyId,Workforce,ProductionTarget?)`. `FleetPlanningQueries.Create(ContentSet,EmpireView)` builds it from own facts like the existing governor context; strips galaxy seed. `AssemblyPlanner.Preview(IFleetPlanningContext,AssemblyDraft,RequestId? replacing=null) -> AssemblyPreview`: immutable per-slot allocation, prior/proposed queue, cost/cargo forecast, nullable completion/arrival estimates, warnings, rejections and canonical fingerprint. No partial mutation. Completed-turn count estimates use current output/growth as explicitly conditional, never predictions of unknown quarantine/tech.
- `FleetTurnCoordinator.PrepareRequests(ContentSet,GameState,EmpireId) -> FleetPreparationResult(State,Succeeded)` binds only already accepted work; `PrepareOrders(...)` proposes fleet orders after governor selection; `NormalizeSafety(...)` normalizes prepared scout departures to compatible cohorts. Each takes the same3 arguments and preserves its original input on failure. `FleetPlanner.Plan(IFleetPlanningContext) -> FleetPlan` is pure and bounded. The governor selector consults `RequestQueueAuthority` for queue-only temporary authority and never dispatches request ships itself.
- `OfficerBonuses.EffectiveSpeed(ContentSet,GameState,VesselId) -> int`, `RequestShipCost(ContentSet,GameState,ColonyId,string roleId) -> long`, `SurveyResearch(ContentSet,GameState,EmpireId,OfficerId) -> long` all read the supplied snapshot, return base/zero outside rules8/scope, use checked intermediates. `FleetTravel.Duration(ContentSet,GameState,EmpireId,FleetId,StarId) -> int` delegates to existing integer `TravelCalculator.Duration` with minimum effective member speed. `FleetTravel.LaunchDuration(...)` also supports the one-ship AI fleet. Execution ruling: the owning EmpireId is explicit for local fleet/officer IDs; global vessel/colony IDs already identify their owner.
- `FleetResolutionContext.Capture(ContentSet,GameState)`: immutable pre-turn officer bonuses/doctrine/flagship, first-survey eligibility and request-token bindings, plus bounded mutable event facts accumulated during this candidate resolution. `FleetResolver.BindCompletion(content,snapshot,candidate,context,ColonyId,VesselState)`, `NormalizeAfterLosses(...)`, `AfterFounding(...)`, `ObserveAssembly(...)`; `OfficerProgression.Apply(content,snapshot,candidate,context)`; `OfficerRecovery.Apply(...)`; `FleetReports.Finalize(...)`. These internal methods mutate only the private candidate/context. `TurnReport.FleetEvents` is immutable and empty for old rules.

**One preparation path:** unready human false→true Ready or direct Resolve uses crisis Prepare → fleet PrepareRequests → existing governor Prepare → fleet PrepareOrders → scout Prepare → safety cohort normalization → receipt reconciliation/validation → Ready. Revalidate receipts after any material officer/fleet/order change; same exact action remains valid. Already-ready humans are validated, not prepared again. Existing AI crisis/planner call remains once. Internal planner-origin commands do not trigger manual takeover or recursive preparation.

**Resolution insertion points against current TurnResolver:** after preparation/clone capture fleet context beside ameba/crisis contexts; ordinary EconomyCalculator then production (BindCompletion before DevelopmentCompletion consumes tokens) and ordinary research; plague health; ameba ResolveExisting; immediately normalize losses and record lost flagship facts before movement; crisis departures; existing MovementResolver using captured group launch durations; existing cargo arrivals/founding then AfterFounding; existing departure validation/health exposure/onset/reports; reset Ready/DevelopmentCompletion/advance turn; existing ObservationUpdater/ContactUpdater/Ameba ObserveArrivals; ObserveAssembly must check newly known danger **before** completion; ordinary ameba research deposit; OfficerProgression deposits first-survey research after ordinary research/cure resolution; OfficerRecovery applies snapshot loss outcomes and pre-existing deadlines; finalize fleet/scout reports; final combined validation/publication. New completions never appear in the pre-turn movement list and cannot launch this boundary. New promotions/research never affect captured bonuses/loss outcomes or committed travel.

Add internal `FleetResolutionPhase` hooks after Preparation/Snapshot/Economy/Health/Loss/Transport/Founding/Observation/Rewards/Recovery/Reports/Validation, and `TurnResolver.ResolveFleet(ContentSet,GameState,Action<FleetResolutionPhase>)`. Preserve old crisis/scout test seams. Hook exceptions return the **enclosing original** state and no report, not an intermediate prepared candidate. No phase hook is a production save callback.

## Task 1 — Freeze genuine rules7 sources and old-engine futures

**Files:** new capture project, `T/FleetCompatibilityTests.cs`, immutable `T/Fixtures/rules7-fleet-baselines/**`, `.gitattributes`, execution ledger and `reports/fleet-execution/baseline-capture.json`. No product changes.

- [x] Add `FleetCompatibilityTests.GenuineRules7SourcesAndFuturesAreFrozen`: absent manifest is a meaningful assertion failure explaining the pre-change gate. Every captured case must load as rules7, match raw SHA256 and replay4 exact canonical futures through recorded ordinary commands; include save→load continuation at each boundary. Use existing `CompatibilityFixtures.CanonicalWithoutSchema`; only schema is excluded, never content/state/PRNG.
- [x] Run `Focused(FleetCompatibilityTests)`; expect the missing-manifest assertion RED. Record unchanged product/source hashes and `git diff` showing no product edits.
- [x] Add archive `-text` rules **before** capture/staging for the new baseline directory and raw `reports/fleet-execution/**`. Capture generated rules7 cases for each of4 species × every core size from embedded `content/core/galaxy.json` × ameba Standard/Off × rivals0/1, fixed seed42 and plague Standard. Also copy/hash every actual retained UI source listed by the7 main integration `results.json` files; label each generated versus pre-session supplement accurately. Require coverage of pending transit, Warning, Exposed study, active governor queue, plague and carried population/founding; supplement missing states through actual ordinary commands on unchanged rules7, keeping scripted rare fixtures separate. Use recorded exact acknowledgements/current encounter responses and Ready commands for4 boundaries, never force authoritative fields to fabricate capture. Store source path/hash, embedded content/schema/rules, capture program hash, pre-product commit, exact commands, canonical future files/hash and coverage inventory. Program invokes current SaveGame/GameSession/TurnResolver APIs only; fail on rejected input or unmet coverage. Run `dotnet run --project tools/FleetBaselineCapture/FleetBaselineCapture.csproj -- --capture --root . --output tests/OpenAntares.Simulation.Tests/Fixtures/rules7-fleet-baselines --ui-results reports/scout-integration-20261027/main --turns 4 --seed 42`; define these exact options in the small tool and refuse an existing nonempty output directory. No future files are captured with rules8 code.
- [x] Run `Focused(FleetCompatibilityTests)`, `Suite`, `Build`: all GREEN including49 rules6 cases/196 futures and existing old suites. Check an actual index checkout to a new private temporary directory reproduces raw hashes (use existing Git index, never touch main index or replace expected hashes); preserve its manifest/evidence. Capture counts are derived, not guessed. Record originals and copied UI files byte-exact. If a QA path is unavailable, generate replacement coverage on unchanged rules7 and explicitly record its provenance, never claim the missing original was captured.
- [x] Commit only `.gitattributes`, `tools/FleetBaselineCapture`, `tests/OpenAntares.Simulation.Tests/FleetCompatibilityTests.cs`, `tests/OpenAntares.Simulation.Tests/Fixtures/rules7-fleet-baselines`, the ledger and this task's named evidence. Product changes may begin only after this committed capture gate passes.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- .gitattributes tools/FleetBaselineCapture tests/OpenAntares.Simulation.Tests/FleetCompatibilityTests.cs tests/OpenAntares.Simulation.Tests/Fixtures/rules7-fleet-baselines .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "test: freeze genuine rules7 fleet baselines"
  ```

## Task 2 — Embed approved content and independent recovery vectors

**Files:** `S/Fleets/FleetContent.cs`, `FleetContentLoader.cs`, `FleetInitializer.cs` (only RecoveryRandom contract here); existing content loader/writer/set and rules predicates; `content/core/fleet_command.json`, `species.json`, `technologies.json`; `T/FleetContentTests.cs` and labelled fleet fixture content.

- [x] Add `FleetContentTests.LoadsApprovedValuesAndRejectsInvalidReferences`: assert24 displayed research=2400 internal after `industrial_coordination`,10/30 thresholds,5/10 bonuses,50/70 recovery,2/1 and4/3 delays, bounds32/64/32/32/4/64/history32/10, all culture defaults RecoverCommand. Alternate species/culture IDs must work with declared Mortal/ColonyBody embodiment; reject missing/duplicate profile references, wrong cultures, unordered thresholds, invalid percentages/costs/delays/specialties or capability. `IndependentRecoveryVectorsMatch` uses the frozen appendix values.
- [x] Run `Focused(FleetContentTests)`; require behavioral failure (missing/invalid data accepted or expected section unavailable), not a missing-type build failure.
- [x] Implement explicit `Rules.FleetVersion=8`, `HasFleets(version)==8`, append8 explicitly to existing feature predicates/IsSupported; leave CurrentVersion7. Add optional `requireFleets=false` loader argument and version-gated writer section. Embed appendix profiles/declared cultures/embodiment/values and `command_recovery`; rules1..7 serializers/content projections omit new section, embodiment fields and technology. Add `Rules.TechnologyAvailable(ContentSet,int rulesVersion,string technologyId) -> bool`: defined ordinary technologies remain available; the declared fleet recovery technology requires8. Use it in research commands, state validation, research forecast, existing ExpansionRivalPlanner research candidate filter and ResearchBar. This prevents even an explicitly generated old-rule game with the new full core ContentSet from selecting/advertising the new technology. Strict save loading rejects injected new content rather than silently stripping it. Existing embedded old content remains byte-canonically equivalent. `FleetInitializer.RecoveryRandom(ulong seed,EmpireId owner) -> Pcg32` uses frozen domains; no old generator draw. Do not alter owner roster prose or require new fields for legacy content.
- [x] Run `Focused(FleetContentTests)`, `Focused(FleetCompatibilityTests)`, `Suite`, `Build`; verify CurrentVersion7/writer8 and capture fingerprints unchanged. Independently compare appendix arithmetic to actual PCG32 tests.
- [x] Commit the explicit files above plus relevant ledger/evidence, using `git add -- <listed paths>`; never stage `docs/SPECIES_ROSTER.md` or owner research/backlog files.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Rules.cs src/OpenAntares.Simulation/Content/ContentSet.cs src/OpenAntares.Simulation/Content/ContentLoader.cs src/OpenAntares.Simulation/Content/ContentWriter.cs src/OpenAntares.Simulation/Fleets/FleetContent.cs src/OpenAntares.Simulation/Fleets/FleetContentLoader.cs src/OpenAntares.Simulation/Fleets/FleetInitializer.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/State/GameStateValidator.cs src/OpenAntares.Simulation/Economy/EconomyCalculator.cs src/OpenAntares.Simulation/Rivals/ExpansionRivalPlanner.cs content/core/fleet_command.json content/core/species.json content/core/technologies.json tests/OpenAntares.Simulation.Tests/FleetContentTests.cs tests/OpenAntares.Simulation.Tests/Fixtures/fleet-content.json game/Components/ResearchBar.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: embed fleet content and recovery stream contract"
  ```

## Task 3 — Validated state, own views and initial appointments

**Files:** new FleetState/FleetStateValidator/FleetViews/FleetQueries, remaining FleetInitializer; existing state, validator, projector/view and GalaxyGenerator; `T/FleetStateTests.cs`, `FleetQueryTests.cs`, `FleetFixtures.cs`.

- [x] Add `FleetStateTests.CloneIsDeepAndLegacyMetadataRejected`: mutate nested copied requests/slots/queues/careers/streams and prove source unchanged; reject duplicate/foreign membership, incoherent transit, conflicting assignments, invalid deadlines/unlocks, caps and counters. `FleetQueryTests.ForeignRosterAndUnknownDangerNeverAppear` compares views/previews of worlds differing only in hidden enemy/habitat state. `StartsOnePostedNavigatorWithoutGlobalIdsOrOldRngDraws` checks all species, declared alternative IDs and Wenth source-body identity.
- [x] Run `Focused(FleetStateTests)` and `Focused(FleetQueryTests)`; expect real clone/invariant/visibility failures after minimal contracts compile.
- [x] Implement the shared state contracts and centralized validator delegation, strict legacy absence, immutable own projection, deterministic local IDs/candidates/catalog. Rules8 initializes after unchanged galaxy/ship/colony generation: human Navigator Posted at home, two mortal candidates; Wenth home-body Navigator without fictional independent candidates. AI assignment is completed in Task14. Mark new independent completions explicitly, without auto-creating named fleets; old vessels/markers stay legacy-compatible. All ID allocation checked, deterministic and separate from global NextEntityId.
- [x] Run both focused classes, `Suite`, `Build`. Assert rules7 generation/futures/global IDs and all old streams unchanged, queries pure and no foreign identity leaked in rejections or reports.
- [x] Commit only new state/view files, named state/generator/projector files/tests and task evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetState.cs src/OpenAntares.Simulation/Fleets/FleetStateValidator.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Fleets/FleetQueries.cs src/OpenAntares.Simulation/Fleets/FleetInitializer.cs src/OpenAntares.Simulation/State/GameState.cs src/OpenAntares.Simulation/State/VesselState.cs src/OpenAntares.Simulation/State/GameStateValidator.cs src/OpenAntares.Simulation/Knowledge/EmpireView.cs src/OpenAntares.Simulation/Knowledge/EmpireViewProjector.cs src/OpenAntares.Simulation/Galaxy/GalaxyGenerator.cs tests/OpenAntares.Simulation.Tests/FleetStateTests.cs tests/OpenAntares.Simulation.Tests/FleetQueryTests.cs tests/OpenAntares.Simulation.Tests/FleetFixtures.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add validated fleet state and own views"
  ```

## Task 4 — Direct fleet commands and committed group travel

**Files:** new FleetCommands/FleetCommandProcessor/FleetTravel/OfficerBonuses (EffectiveSpeed only); CommandProcessor/ExpansionCommands/MovementResolver; `T/FleetCommandTests.cs`, `FleetTravelTests.cs`.

- [x] Add `FleetCommandTests.BulkRejectsAllOnStaleForeignOrIllegalMember` and `NamedFleetCapDoesNotBlockIndependentConstructionOrSafety`; check create/rename/disband/merge/split/flagship,1..32 membership/1..80 name and64 active+reserved cap. `FleetTravelTests.Mixed300And200AtDistance301TakesTwoTurns` asserts both routes/durations and no mid-transit edit. `NavigatorSpeed315And330DoesNotChangeFrozenTransitOrAmebaOdds` tests planned bonus seam with labelled officer XP.
- [x] Run `Focused(FleetCommandTests)` and `Focused(FleetTravelTests)`; RED must identify partial mutation, route duration/cap or no-op contract, not absent APIs.
- [x] Implement atomic command variants and previews; explicit detachment before incompatible manual moves/founding. Aggregate direct move selection into per-fleet/co-located cohorts and ordinary independent ships; validate all before publishing. FleetTravel uses the minimum effective speed and existing positive integer duration; persist launch provenance matching ordinary vessel transit, never alter an already committed route. Losing officer/speed or opening a window cannot change it. Add stable rules8 receipt consequence fingerprint for actual cohort/detachment/duration changes, leaving legacy tokens and hull odds unchanged. Founding remains a separate existing planet command. Own-only names and foreign per-vessel observations stay unchanged.
- [x] Run both focused classes, `Suite`, `Build`; assert same-order no-op keeps reference/Ready, actual changes clear only issuer, bounds/overflow rollback and all old travel/odds formulas unchanged.
- [x] Commit named fleet command/travel files, adapters, tests and task evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetCommands.cs src/OpenAntares.Simulation/Fleets/FleetCommandProcessor.cs src/OpenAntares.Simulation/Fleets/FleetTravel.cs src/OpenAntares.Simulation/Fleets/OfficerBonuses.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Commands/ExpansionCommands.cs src/OpenAntares.Simulation/Movement/MovementResolver.cs src/OpenAntares.Simulation/Scouting/ScoutRecords.cs src/OpenAntares.Simulation/Scouting/ScoutQueries.cs tests/OpenAntares.Simulation.Tests/FleetCommandTests.cs tests/OpenAntares.Simulation.Tests/FleetTravelTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add atomic fleet control and group travel"
  ```

## Task 5 — Commissioning, physical appointments and editable doctrine

**Files:** new OfficerCommands/OfficerCommandProcessor; FleetState/FleetViews/CommandProcessor; `T/FleetOfficerTests.cs`.

- [x] Add `FleetOfficerTests.CommissionStoresTurnTwelveDeadlineAndCountsTowardFour` (turn10 acceptance records deadline12), `CannotTeleportOrServeTwoAssignments`, `WenthOneAppointmentPerSourceBodyWithoutPopulationCost`, `DoctrineFromTurnZeroAndBulkIsAtomic`. Cover64 lifetime bound, dismissal finality/catalog uniqueness, all culture defaults and explicit future LastWatch inheritance without retroactive propagation. Actual turn11/12 countdown is Task12's resolver test, not a knowingly failing dependency left in this task's suite.
- [x] Run `Focused(FleetOfficerTests)`; behavioral RED for appointment/cap/location/doctrine authority. Lifecycle deadlines resolve through OfficerRecovery in Task12; tests here cover command legality/deadline state, with full countdown test completed there.
- [x] Implement exclusive status/assignment transitions, fixed specialty/candidate selection, co-located board/remove/transfer/post, future-default override and explicit bulk doctrine edit. A request coordinator must be Posted at a participating colony before allocation, remain there and serve one request; removal forfeits completion XP. Wenth record references its original colony body and source identity regardless of organ location. No population fee or random personality behavior. Commands reconcile actual affected ameba consequences without implicitly acknowledging them; extend the rules8 fingerprint with the appointed officer/doctrine/recovery consequence. Test changed doctrine invalidates a material receipt and an identical repeated preview preserves it.
- [x] Run `Focused(FleetOfficerTests)`, `Suite`, `Build`; all rejected/duplicate/no-op edits leave Ready/state/streams exact and ordinary manual controls need no officer.
- [x] Commit officer command files and only named shared adapters/tests/evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerCommands.cs src/OpenAntares.Simulation/Fleets/OfficerCommandProcessor.cs src/OpenAntares.Simulation/Fleets/FleetState.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Scouting/ScoutQueries.cs tests/OpenAntares.Simulation.Tests/FleetOfficerTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add officer appointments and editable doctrine"
  ```

## Task 6 — Pure finite-request preview and deterministic estimates

**Files:** new IFleetPlanningContext/FleetPlanningQueries/AssemblyPlanner; FleetViews; GovernorPlanningQueries own-frame adapter; `T/FleetPlanningTests.cs`.

- [x] Add `FleetPlanningTests.OffersIdleShipsThenRanksConditionalCompletionAndArrival`:3 colonies/4 scout slots, idle candidate acceptance explicit; finite completion+arrival wins over null estimate, ties ColonyId then slot. `UrgentPreviewRetainsBlockedActiveProjectAndRecomputesTail` asserts prior/proposed queues, aggregate cargo and per-colony sequential estimates after every assigned slot. `HiddenHabitatDoesNotChangePreviewButKnownUnsafeRallyRejects` covers privacy.
- [x] Run `Focused(FleetPlanningTests)`; expect deterministic allocation/estimate/knowledge assertions RED.
- [x] Implement context from own immutable facts only; reuse ColonyEconomy calculation through own forecast, including request-bound prices and declared species cargo limits. Estimate finite unstarted work sequentially with at least1 turn/project, active project first, fixed current output and labelled conditional forecasts; unknown/infinite estimates stay blocked. Preview reserves full composition/fleet capacity,32 queue bound/request bound/participating colonies and no colony/rally conflict; no ID/queue token/RNG allocation in preview. Include exact active/queue/project/reserve/assignment/knowledge/priority inputs in canonical fingerprint to reject stale acceptance. Copying a role string or accepting a matching old hull cannot silently authorize cargo.
- [x] Run `Focused(FleetPlanningTests)`, `Suite`, `Build`; same preview repeated100 times leaves canonical state/streams/Ready unchanged; alternative content and zero-output estimates valid.
- [x] Commit the listed context/planner/projection adapter files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/IFleetPlanningContext.cs src/OpenAntares.Simulation/Fleets/FleetPlanningQueries.cs src/OpenAntares.Simulation/Fleets/AssemblyPlanner.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Development/GovernorPlanningQueries.cs tests/OpenAntares.Simulation.Tests/FleetPlanningTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: preview finite assembly allocation"
  ```

## Task 7 — Accept, reprioritize, pause and amend finite construction

**Files:** new AssemblyCommands/AssemblyCommandProcessor/RequestQueueAuthority; FleetState/CommandProcessor/DevelopmentCommandProcessor/GovernorPlanner/GovernorPlanningQueries; `T/FleetRequestTests.cs`.

- [x] Add `FleetRequestTests.PriorityEditPreservesActiveTokenReserveAndUnrelatedOrder`, `OffGovernorStoredTemplateNeverGainsTemplateAuthority`, `CargoNeedsExplicitAuthorization`, `SafetyPauseRetainsLivingBindingsAndRequiresLegalResume`, `MissingSlotBlocksCompletionUntilFiniteAmendment`. Check same-priority no-op, paused edit stays paused,32-entry atomic overflow, colony/rally exclusive use and reserved fleet slot release.
- [x] Run `Focused(FleetRequestTests)`; semantic RED must reproduce a queue/authority/safety error with literal token/reserve assertions.
- [x] Implement revision/fingerprint-checked accept/edit transactions. Accept allocates each finite slot exactly once and inserts Normal tail/Urgent after active, preserving existing queue tokens. Temporary request authority makes the **existing** selector scan legal explicit entries only through the last authorized request entry while ConstructionOff; if none legal bank output, never use stored template or toggle switches/workforce. ConstructionOn retains its sole ordinary selector. Pause/manual takeover ends contribution selection/dispatch but preserves active project; keep cancellation converts entries in place, remove cancellation removes only unstarted bound entries. Explicit detach/loss marks Missing; safety pause records BlockedSafety with prior status/bindings, no replacement/retry; active projects may finish and wait. Change rally/resume must re-preview current legal knowledge and preserve committed travel. Releasing/replacing a reserved named-fleet slot is atomic; disbanding a rally pauses rather than discards it.
- [x] Run `Focused(FleetRequestTests)`, `Focused(FleetPlanningTests)`, `Suite`, `Build`; verify full cancel/finite-amend/ordinary-manual matrices, cargo all4 species/quarantine and no extra governor controller. No-op and failure preserve fingerprints, IDs, reserve and issuer Ready.
- [x] Commit named request/queue files, shared adapters, tests and evidence only.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/AssemblyCommands.cs src/OpenAntares.Simulation/Fleets/AssemblyCommandProcessor.cs src/OpenAntares.Simulation/Fleets/RequestQueueAuthority.cs src/OpenAntares.Simulation/Fleets/FleetState.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Commands/DevelopmentCommandProcessor.cs src/OpenAntares.Simulation/Development/GovernorPlanner.cs src/OpenAntares.Simulation/Development/GovernorPlanningQueries.cs tests/OpenAntares.Simulation.Tests/FleetRequestTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add finite request authority and priority"
  ```

## Task 8 — Snapshot Organizer prices through ordinary economy

**Files:** extend OfficerBonuses with RequestShipCost; VesselProduction; own forecast adapters; `T/FleetEconomyTests.cs`.

- [x] Add `FleetEconomyTests.OrganizerScoutCosts760And720OnlyWhileActiveCoordinator`: base800, no bonus800, XP10→760, XP30→720; ordinary ships/buildings/population unchanged. `PauseRemoveCancelAndPromotionRepriceOnlyNextSnapshotWithoutRefund` checks exact reserve and before/after cost previews, including blocked cargo and stored output. `CeilingAndOverflowRejectAtomically` uses non-divisible alternative costs.
- [x] Run `Focused(FleetEconomyTests)`; require cost/reserve/completion assertions RED.
- [x] Implement checked ceil `max(1,(baseCost*(100-discount)+99)/100)` via Int128 and checked long. Only current active request-bound token/role/colony with its assigned active coordinator qualifies at supplied snapshot; paused/safety-paused/cancelled/unbound/ordinary targets do not. Reuse this method in VesselProduction and own-view preview, preserving EmbarkationRules, one completion/colony/turn and unchanged unspent production reserve. Promotion later this boundary cannot change the price already paid. Snapshot discount never grants construction authority itself.
- [x] Run `Focused(FleetEconomyTests)`, `Focused(FleetPlanningTests)`, `Suite`, `Build`; ensure ordinary/mismatched target costs and rules1..7 futures unchanged.
- [x] Commit named bonus/economy/forecast files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerBonuses.cs src/OpenAntares.Simulation/Fleets/FleetPlanningQueries.cs src/OpenAntares.Simulation/Development/GovernorPlanningQueries.cs src/OpenAntares.Simulation/Economy/VesselProduction.cs tests/OpenAntares.Simulation.Tests/FleetEconomyTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: apply scoped organizer production bonuses"
  ```

## Task 9 — Bounded preparation, standing orders and Ready

**Files:** new FleetPlanner/FleetTurnCoordinator; CommandProcessor, governor/scout coordinators and ready queries; `T/FleetReadyTests.cs`.

- [x] Add `FleetReadyTests.PreparesEachHumanOnceAcrossReadyRetryAndDirectResolve`, `ExploreUsesOwnKnowledgeFiveTurnResurveyAndStableRanking`, `SafetyOverridesGoalsWithoutConsent`, `LateSecondHumanFailurePreservesFirstHumansPreparedOrders`. Explicitly cover Ready=true no-op, false→true retry,2 humans+AI, withdrawn Ready and stale exact member receipts after officer/reward changes.
- [x] Run `Focused(FleetReadyTests)`; demonstrate duplicate binding or unsafe/order/readiness assertion RED.
- [x] Wire the exact single preparation path above into both CommandProcessor and direct TurnResolver. PrepareRequests works only within accepted quota/priority/cargo/fleet reservations; PrepareOrders uses one proposal per member. Explore scout-only: unsurveyed then ≥5-turn resurvey, rank fleet duration/StarId, exclude own-known guarded/forbidden, no target→Hold once. Assemble/Return become Hold at completion; disappeared Return home blocks at stationary boundary. Respect active crisis holds/manual orders; scout preparation may separate incompatible cohorts through bounded ID order. Internal command origin avoids manual-takeover interception. Budget≤32*32 slots/empire and one movement proposal/member; never recursive Ready.
- [x] Run `Focused(FleetReadyTests)`, `Suite`, `Build`; compare canonical bytes/reference, receipts, queue IDs and reports across repeated readiness and failure. No new Ready blocker for a paused request; ordinary encounter consent still blocks.
- [x] Commit planner/coordinator and named Ready adapters/tests/evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetPlanner.cs src/OpenAntares.Simulation/Fleets/FleetTurnCoordinator.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs src/OpenAntares.Simulation/Development/GovernorTurnCoordinator.cs src/OpenAntares.Simulation/Scouting/ScoutTurnCoordinator.cs src/OpenAntares.Simulation/Scouting/ScoutReadyQueries.cs tests/OpenAntares.Simulation.Tests/FleetReadyTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: prepare fleet orders through shared readiness"
  ```

## Task 10 — Resolve construction, cohort safety, arrivals and founding

**Files:** new FleetResolutionContext/FleetResolver; TurnResolver, MovementResolver, DevelopmentCompletion, scouting context/coordinator; `T/FleetResolutionTests.cs`.

- [x] Add `FleetResolutionTests.CompletedHullWaitsUntilNextBoundaryAndBindsExactToken`, `UnknownRallyArrivalSafetyPausesBeforeCompletion`, `SafetySplitAtSixtyFourFleetsKeepsFlagshipCohort`, `FounderPostsOfficerWithoutLossOrChangingWenthSource`. Include retained already-launched transit, partial arrivals, blocked joins/full quota, destroyed flagship/survivor identity and empty fleet retirement.
- [x] Run `Focused(FleetResolutionTests)`; require actual resolver RED showing same-turn dispatch, wrong binding/XP eligibility or incorrect founding/loss distinction.
- [x] Add context/hook seam and integration phases exactly as mapped above. Bind new hull before queue consumption using snapshot colony/token/role/slot, then leave it independent and nonmoving until next preparation. Loss/safety separation retains compatible flagship cohort; other survivors become independent in VesselId order, never counted emergency fleets. AfterFounding posts the officer at the new colony with unchangedXP/source; no lost-ship/recovery event. Explicit request-founder detachment must precede founding. ObserveAssembly checks fresh known rally legality, pauses affected requests once, then joins legal co-located arrivals and completes only all Arrived/current full quota; completion Hold/report facts feed Task11. Missing or BlockedSafety never waived. Existing completed requests stay completed if later danger appears.
- [x] Run `Focused(FleetResolutionTests)`, `Suite`, `Build`; force every currently integrated FleetResolutionPhase hook to throw and prove exact outer-state rollback/no report, including human1 already Ready. Preserve old scout/crisis hook coverage and cargo loss behavior.
- [x] Commit named context/resolver/adapters/tests/evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetResolutionContext.cs src/OpenAntares.Simulation/Fleets/FleetResolver.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs src/OpenAntares.Simulation/Movement/MovementResolver.cs src/OpenAntares.Simulation/Development/DevelopmentCompletion.cs src/OpenAntares.Simulation/Scouting/AmebaResolutionContext.cs src/OpenAntares.Simulation/Scouting/ScoutTurnCoordinator.cs tests/OpenAntares.Simulation.Tests/FleetResolutionTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: resolve fleet assembly safety and founding"
  ```

## Task 11 — Once-only XP, passive unlocks and bounded careers

**Files:** new OfficerProgression/FleetReports; FleetResolutionContext/FleetViews/TurnResolver/scout receipt consequences; `T/FleetProgressionTests.cs`.

- [x] Add `FleetProgressionTests.SurveyPromotionUsesPreEventBonus` withXP8→10/research0 andXP28→30/research100; XP30 survey research200. `ConcurrentFirstSurveyCreditsLowestEligibleFlagshipOnce` and `FourNewHullsAndFourSingleRequestsBothAwardFour` include old hull reuse, split/cancel/missing requests, officer removal, unassigned prior survey and no waiting/resurvey farming. `StudyAddsEightXpAndOnlyOrdinarySixResearch` asserts600 internal research and unchanged75%/95% hull odds. Career bounds32 highlights/10 detailed turns/64 lifetime records retain totals/final summaries.
- [x] Run `Focused(FleetProgressionTests)`; behavioral RED for duplicate/retroactive event award or wrong internal research units.
- [x] Capture same first-survey event/eligible flagship recipient from snapshot; dedup per empire/system regardless of officer present, and empire/habitat for successful study. Deposit survey research after ordinary research/cure and existing study deposit; no extra same-turn completion/cure funding. Cap XP30, derive passive rank from thresholds; no respec/buttons. Award request1XP per original newly built hull included in completed quota only to initial still-assigned coordinator, once, never for existing hulls or blocked quota. Store immutable history facts for retired ships, aggregate counters checked, one event/promotion/report per actual outcome. Implement OfficerBonuses.SurveyResearch and extend the stable rules8 receipt fingerprint/display with actual reward/unlock consequences; speed bonuses do not alter hull odds. Repeated pure reads must preserve exact token equality.
- [x] Run `Focused(FleetProgressionTests)`, `Focused(FleetReadyTests)`, `Suite`, `Build`; inject Rewards/Reports late failures and assert RNG/credit sets/history/other-human planning rollback. All literal2/4/8 event examples and snapshot semantics pass.
- [x] Commit named progression/report/projection/receipt integration files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerProgression.cs src/OpenAntares.Simulation/Fleets/FleetReports.cs src/OpenAntares.Simulation/Fleets/OfficerBonuses.cs src/OpenAntares.Simulation/Fleets/FleetResolutionContext.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs src/OpenAntares.Simulation/Scouting/ScoutQueries.cs tests/OpenAntares.Simulation.Tests/FleetProgressionTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: record deduplicated officer progression"
  ```

## Task 12 — Officer loss, recovery and commissioning clocks

**Files:** new OfficerRecovery; FleetInitializer/FleetResolutionContext/TurnResolver; `T/FleetRecoveryTests.cs`, extend FleetOfficerTests.

- [x] Add `FleetRecoveryTests.MortalDraw49Succeeds50FailsAnd69Succeeds70Fails`, `LossAtTenReturnsAtThirteen`, `WenthLinkTwoOneAndLastWatchFourThreeWithoutDraw`, `MissingRecoveryStartsOnlyWithOwnColonyAndRestartsIfDestinationLost`. Distinguish founder consumption/no roll, another member's loss, LastWatch permanent death, failed recovery permanent death, cargo remains destroyed, next-snapshot technology and exact independent RNG advancement/order.
- [x] Run `Focused(FleetRecoveryTests)`; semantic RED for threshold/timer/draw behavior. Use reachable PCG seeds/states producing labelled draws; record draw count and validate state, never treat forced rare fixtures as generated campaigns.
- [x] Apply actual destroyed designated flagship events ordered EmpireId/OfficerId, captured doctrine/technology/flagship/location. Only eligible mortal Recover draws once from its own recovery stream; success countdown starts after loss and completes after2 **subsequent** boundaries. No-colony success Missing has no running deadline; later owned colony chooses lowestColonyId and starts same delay; losing that destination returns Missing without reroll. Wenth retains source-body identity/history/XP and uses snapshot2/1 or4/3 delay without draw. Dead/Retired never revive. Advance pre-existing commissioning/recovery deadlines once; new events cannot tick immediately. Commission accepted turn10 becomes Posted12. Snapshot Command Recovery researched this turn applies only to later losses. Hull/cargo effects remain ordinary outcomes.
- [x] Run `Focused(FleetRecoveryTests)`, `Focused(FleetOfficerTests)`, `Suite`, `Build`; fault every phase including Recovery/Validation and compare entire original bytes/reference plus all existing/random streams. Previews/invalid/no-op/reload/transit/Wenth/LastWatch draw zero.
- [x] Commit named recovery/clock integration files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerRecovery.cs src/OpenAntares.Simulation/Fleets/FleetInitializer.cs src/OpenAntares.Simulation/Fleets/FleetResolutionContext.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs tests/OpenAntares.Simulation.Tests/FleetRecoveryTests.cs tests/OpenAntares.Simulation.Tests/FleetOfficerTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: resolve officer recovery and appointment clocks"
  ```

## Task 13 — Strict writer9 and all continuation states

**Files:** new FleetSaveData; SaveGame/ExpansionSaveData/ContentWriter, FleetStateValidator; `T/FleetSaveTests.cs`, extend FleetCompatibilityTests.

- [x] Add `FleetSaveTests.ExactThirtySevenPairsAndNoEightEight`, `LegacyFleetInjectionRejected`, `EveryPendingPhaseRoundTripsToIdenticalFuture`, `FailedLoadAndWritePreserveSessionAndDestination`. Exercise pending launch/committed cohort, partially built/arrived/missing/safety-paused request, priority/token/active-project preservation, Wenth founding, commission/recovery/Missing/dead careers, promotion/event credits and independent stream state.
- [x] Run `Focused(FleetSaveTests)`; semantic RED uses expected unsupported(9,8) or round-trip invariant rather than compile failure.
- [x] Activate writer9 and append exactly8 new explicit pairs, preserving the29 old accepted pairs. Strict codec requires rules8 fields including valid priority, stream/counters/assignment/provenance/embedded content and material scout receipt fingerprint; rejects injected new metadata/content under old rules, unknown enums, duplicates, stale/foreign references, invalid deadlines/XP/counters/cohort and queue bindings. Historical vessel facts are immutable facts, not live lookup requirements. Failed read leaves GameSession unchanged; existing atomic WriteFile retains destination on late failure. Writer9 old-rule saves omit all new content/state/receipt fields and replay identical futures. Leave CurrentVersion7 for now.
- [x] Run `Focused(FleetSaveTests)`, `Focused(FleetCompatibilityTests)`, `Suite`, `Build`; replay all Task1 rules7/older futures and round-trip new states through actual saves/commands. Save/load causes zero preparation, acknowledgement, draws or priority reorder.
- [x] Commit codec/save/validator files, tests and evidence; retain frozen bytes/expected values.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Saves/FleetSaveData.cs src/OpenAntares.Simulation/Saves/SaveGame.cs src/OpenAntares.Simulation/Saves/ExpansionSaveData.cs src/OpenAntares.Simulation/Saves/ScoutSaveData.cs src/OpenAntares.Simulation/Content/ContentWriter.cs src/OpenAntares.Simulation/Fleets/FleetStateValidator.cs tests/OpenAntares.Simulation.Tests/FleetSaveTests.cs tests/OpenAntares.Simulation.Tests/FleetCompatibilityTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: persist strict fleet campaign state"
  ```

## Task 14 — Existing rival planner adapter and generated invariants

**Files:** FleetInitializer/FleetTravel/FleetTurnCoordinator; RivalTurnPlanner/RivalVesselPlanner; `T/FleetAiTests.cs`, `FleetGenerationTests.cs`.

- [x] Add `FleetAiTests.SingleExistingPlannerCommandsStartingNavigatorFleet`: same initialNavigator/XP/travel rules/costs, no extra order controller/recruitment/requests/free hulls/hidden knowledge. `LegacyRivalFuturesRemainExact` replays old sources. `FleetGenerationTests.AllSpeciesSizesModesPreserveGalaxyAndExistingRng` covers each4 species/core sizes, rivals0/1, plague Standard/Off and ameba Standard/Off with explicit rules8; declared alternate species too.
- [x] Run both focused classes; require a real duplicate-order/knowledge/bonus/generation assertion RED.
- [x] Assign AI's initial Navigator to its existing starting scout in a one-ship fleet through rules8 local state, consuming no global ID/RNG. Existing expansion_v1 decides routes once; adapter applies ordinary fleet travel without overwriting human standing orders or multiplying candidate budget. Subsequent AI ships remain independent; AI never Study/Remain or recruitment/requests. Use RecoverCommand/Wenth link and ordinary earnedXP. Old rules keep their original planner and exact budgets/futures.
- [x] Run both focused classes, `Focused(FleetCompatibilityTests)`, `Suite`, `Build`; compare old-rule ship/global IDs and all RNG streams, and own-view paired-hidden-world AI decisions.
- [x] Commit only named AI/init/travel adapters, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetInitializer.cs src/OpenAntares.Simulation/Fleets/FleetTravel.cs src/OpenAntares.Simulation/Fleets/FleetTurnCoordinator.cs src/OpenAntares.Simulation/Rivals/RivalTurnPlanner.cs src/OpenAntares.Simulation/Rivals/RivalVesselPlanner.cs tests/OpenAntares.Simulation.Tests/FleetAiTests.cs tests/OpenAntares.Simulation.Tests/FleetGenerationTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: adapt existing rival planner for admirals"
  ```

## Task 15 — Central command window and native control evidence

**Files:** new3 UI dialogs/FleetNotificationPreferences, named GameScreen/VesselPanel/TurnReportDialog adapters; new FleetGraphicalValidation scene/class and validation runner/targets/metadata checker/README.

- [x] Add failing native checks for central entry without map selection; all own vessels/idle/new/travelling/reserved/attention filters, stable sort and known-system destination; stale atomic bulk preview, separate planet founding, request Normal/Urgent queues/cargo/conditional estimates, pause/cancel/manual override, unsafe-rally legal resume and retained bindings, officer commissioning/transfer/doctrine/career. Test actual reachable controls/signals at800x600 and1920x1080, not helper calls standing in for UI. Until default activation, initialize a real rules8 campaign through GalaxyGenerator with explicit parameters and mount its GameSession in the ordinary GameScreen; label this provenance. Task16 owns the default New Game button workflow. Extend ValidateSet/opt-in scene registration first; missing class alone is not behavioral RED.
- [x] Run `Runtime(Fleet,reports/fleet-execution/ui-red-01)`; save actual missing/incorrect control assertion and screenshots. The runner must still restore/rebuild ordinary assembly even on failure.
- [x] Reread `G/theme/openantares_theme.tres` immediately before any minimal variation; prefer existing components/containers. Implement tabs/scrolling/focus, selectable ship/fleet rows and optional map locator; construction dialog shows full before/after work/reserve/population and rejected effects; officer detail shows specialty/effective bonuses/deadlines/whole-body language/history. Every confirmation submits an ordinary command/preview fingerprint; no order in frame/read/save callbacks. Display Summary routine, persistent Attention blocked/missing/death/recovery and existing live Emergency; local preferences cannot erase authoritative badges/permission. Existing inspection/founding/ameba response controls remain accessible. Add Fleet scenario type to both inclusion and ordinary-exclusion checks, retaining `FrozenContent`/`ScoutUiControlChecks`; if a new helper is actually needed, enumerate its exact type in both checks too.
- [x] Run Build and `Runtime(Fleet,reports/fleet-execution/ui-green-01)`, then Normal. Inspect PNGs yourself at both sizes, include scale/scroll/focus cases and actual control IDs/commands; audit engine and process logs for errors/unhandled exceptions. Verify opt-in assembly intentionally triggers metadata negative control, ordinary rebuild excludes all8 scenario classes and every actual helper. This does not establish OS physical input or comfortable reading.
- [x] Commit only named UI/validation files (and minimal theme change if required) plus uniquely named reports/screenshots/hashes/ledger; never stage existing QA saves or preferences from user directories.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- game/Components/FleetCommandDialog.cs game/Components/AssemblyRequestDialog.cs game/Components/OfficerDetailDialog.cs game/Presentation/FleetNotificationPreferences.cs game/Screens/GameScreen.cs game/Components/VesselPanel.cs game/Components/TurnReportDialog.cs tests/OpenAntares.Game.Validation/FleetGraphicalValidation.cs tests/OpenAntares.Game.Validation/FleetGraphicalValidation.tscn tests/OpenAntares.Game.Validation/Run-Validation.ps1 tests/OpenAntares.Game.Validation/Validation.targets tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 tests/OpenAntares.Game.Validation/README.md .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add central fleet command controls"
  ```

## Task 16 — Activate new campaigns and complete generated/scale acceptance

**Files:** Rules.CurrentVersion; ContentFiles/NewGameScreen adapters; `T/FleetIntegrationTests.cs`, generation tests; Fleet native scenario supplements and reports.

- [x] Add `FleetIntegrationTests.NewDefaultUsesRulesEightWithoutChangingSetupChoices` expecting rules8/writer9 while Harl/One Peldra/plagueStandard/amebaStandard remain; old loaded rules7 stays7. Run a real generated3-colony/4-scout request via ordinary commands/UI (including legal colony founding), never forced state. Add separately labelled30-colony/100-vessel/20-fleet pre-session scale case with several finite requests and mixed manual/governor controls; scenarios must save/reload and continue.
- [x] Run `Focused(FleetIntegrationTests)` for meaningful default RED and record generated Fleet native workflow failure if any.
- [x] Set CurrentVersion8 and requireFleets only for new core loading; initialize new campaigns with approved content/roster, leave other setup choices unchanged. Extend generated all-species workflows to exercise request construction/arrival, manual takeover, culture/loss text and normal exploration. Rare loss thresholds/rank states remain labelled pre-session supplements. At scale find all idle ships, request build, pause one contribution, locate arrivals, inspect career, resume using central controls; test bounds and bounded list/history behavior without asserting an unmeasured performance target.
- [x] Run integration/generation focused tests, Suite, Build and `Runtime(Fleet,reports/fleet-execution/generated-green-01)`; inspect both-size PNGs and actual save/future provenance. Re-run all genuine rules7/older compatibility tests after default activation. Record exact counts only from outputs, no acceptance inference from an artificial scale fixture.
- [x] Commit explicit activation/integration/native supplement files/tests/evidence; M8-I remains In Progress until final gate/review disposition.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Rules.cs game/ContentFiles.cs game/Screens/NewGameScreen.cs tests/OpenAntares.Simulation.Tests/FleetIntegrationTests.cs tests/OpenAntares.Simulation.Tests/FleetGenerationTests.cs tests/OpenAntares.Game.Validation/FleetGraphicalValidation.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: enable approved fleet campaigns"
  ```

## Task 17 — Combined regression, one final independent review and handoff

**Files:** final reports/manifests, progress ledger, PROJECT_STATUS/AGENTS/specification/acceptance/implementation tracking only; scoped regression fixes if the single review finds defects.

- [ ] Run Suite/Build and all8 native scenarios sequentially: Legacy, Expansion, Species, Rival, Colony, Crisis, Scout, Fleet, each a fresh `reports/fleet-execution/final/<scenario-attempt>` directory. Preserve per-run results/commands/engine and process logs, QA saves, screenshots/raw hashes, source commit and loaded schema/rules. Normal-rebuild/exclusion checks must run from actual assemblies, not source grep. Audit diagnostics from both log sources even if assertions pass. Record source changes before deciding which affected gates need a new run.
- [ ] Run Normal; confirm0 warnings/errors and actual exclusion of8 scenario types plus all helpers. Reproduce raw archive SHA256 after a real index checkout and replay rules1..7 futures. Verify main c554797/four owner SHA256/status/empty index, seven worktrees and retained branches/QA/recovery. A failed preservation/check is unresolved work, not a documentation warning to waive.
- [ ] Complete FLT-AC-001..011 evidence map against exact approved design, using generated/rare/scale provenance separately. Request **one fresh whole-branch implementation review** through the requesting-code-review workflow, authorized by retained Native method, only after combined validation. Give reviewer approved spec/plan/full base-to-head diff and evidence; no per-task reviewers or automatic second review. Save actual findings/dispositions verbatim. For each substantive finding, reproduce a behavioral regression RED, make minimum correction, run focused+Suite/affected native gates and relevant late rollback/compatibility checks. No code change is needed if no actionable finding exists.
- [ ] Self-check corrected branch against every finding/criterion. Record exact final source/commands/counts, historical versus fresh evidence, unresolved/deferred items and human/platform/security/export limits in `reports/FLEET_COMMAND_VALIDATION_2026-10-27.md`, `reports/fleet-execution/final-summary.json`, `reports/fleet-execution/final-review.md`, `reports/VALIDATION_REPORT.md` and ledger. No automatic milestone closure if a functional criterion/finding remains unresolved. Parent REQ-017/counts remain unchanged. Commit only explicit corrected files and named documentation/evidence paths after verification.
- [ ] Hand off completed isolated branch with what changed, tests and remaining limits. Update PROJECT_STATUS with actual M8-D/P/I/FLT statuses and one next action; keep main/owner edits intact. Stop for owner disposition before integration; no merge/push/release/cleanup inferred. Do not request another design/Native approval or repeat a completed task.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- PROJECT_STATUS.md AGENTS.md docs/SPECIFICATIONS.md docs/ACCEPTANCE_CRITERIA.md docs/IMPLEMENTATION_PLAN.md reports/VALIDATION_REPORT.md reports/FLEET_COMMAND_VALIDATION_2026-10-27.md .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "docs: record fleet validation and final review"
  ```

## Frozen implementation details

### Recovery stream v1 vectors

Use existing `Pcg32.FromSeed(galaxySeed ^ 0x4F46464943455201UL, (ulong)owner.Value ^ 0x5245434F56455201UL)`. These distinct ASCII-derived domains identify officer/recovery version1 and do not consume another stream. Existing PCG reference seeding and unchecked64-bit wrap are unchanged. Independent integer arithmetic generated the following planning vectors; they are **not a product test run**. Task2 must compare these to actual Pcg32, including seeded State/Increment and4 successive NextUInt32 values (then independently NextInt(100) modulo-rejection behavior).

| Galaxy seed | Owner | Initial State | Increment | NextUInt32 sequence |
| --- | --- | --- | --- | --- |
|0|1|1783446105968468059|11856436985063973889|50353123,3417059036,1585724331,4047853392|
|42|1|10822750475600051645|11856436985063973889|1038021008,2897589185,1792309901,4126569821|
|42|2|12114079671261706449|11856436985063973895|4047523869,1149529208,2343501704,871191071|
|18446744073709551615|1|8606524431442022100|11856436985063973889|3245324364,1208278847,3848589321,447352628|

Only actual eligible mortal RecoverCommand flagship losses draw, sorted EmpireId/OfficerId. Stream is present under rules8 even amebaOff; no losses means no draws. Store algorithm version1/state/odd increment/draw count. Draw49/50 and69/70 are labelled threshold supplements, not claims these four sequences hit every threshold. Preview/reload/Wenth/LastWatch never draws.

### Initial original profiles and content placement

Use `fleet_command.json` for ordinary values, profiles and culture default keys, and declare species `command_embodiment` as `mortal` or `colony_body` in rules8 embedded content. Culture reference keys explicitly map existing culture names; no code branch on species ID/name. No changes to the owner's roster or biological prose.

| Existing culture / embodiment | Navigator profile | Surveyor profile | Organizer profile |
| --- | --- | --- | --- |
| Estates / mortal | `estates_navigator`: Vara Eshen, they/them; deliberate route planner | `estates_surveyor`: Oren Tal, he/him; patient observer | `estates_organizer`: Mira Vell, she/her; careful coordinator |
| League of Schools / mortal | `schools_navigator`: Tavi Sen, they/them; methodical pilot | `schools_surveyor`: Relo Iven, he/him; curious recorder | `schools_organizer`: Sera Venn, she/her; practical scheduler |
| Harbor Houses / mortal | `harbors_navigator`: Neri Vale, she/her; cautious pilot | `harbors_surveyor`: Taren Oss, they/them; attentive chart maker | `harbors_organizer`: Olan Riss, he/him; steady coordinator |
| Seed Kinship / colony_body | `kinship_navigator`: source-colony body's displayed name | `kinship_surveyor`: source-colony body's displayed name | `kinship_organizer`: source-colony body's displayed name |

These are original presentation text within builder latitude, not hidden modifiers or new biology. Mortal per-specialty name catalog starts with the row's authored name; deterministic decimal suffix2,3,… after reuse, driven by stored checked per-profile commission ordinal (no suffix for first). Candidates have fixed specialty/profile, no RNG/rerolls; after commissioning a candidate, next same-specialty catalog entry is offered only within active/lifetime bounds. No duplicate career reuse. Wenth name is `<source body's colony name> — Command appointment <local officer ID>`; no limb persona or independent recruit. Default doctrine all RecoverCommand, Wenth label Re-establish command link. Personality text never changes authorization. Alternative content supplies its own validated catalog/default/embodiment.

### Plan coverage and handoff

| Approved criterion | Owning tasks |
| --- | --- |
| FLT-AC-001 window/direct authority |3/4/7/15/16|
| FLT-AC-002 fleet/cohort/founding |3/4/9/10/12/13|
| FLT-AC-003 finite requests/safety/priority |6/7/8/9/10/13/15/16|
| FLT-AC-004 cargo/manual/governor authority |5/6/7/8/9/10|
| FLT-AC-005 standing orders/knowledge/consent |3/4/9/10/11/15|
| FLT-AC-006 officers/identity/availability |2/3/5/10/12/13/15|
| FLT-AC-007 XP/bonuses/dedup |4/8/10/11/14|
| FLT-AC-008 culture/loss/equipment |2/5/10/12/13/15|
| FLT-AC-009 transactions/readiness |4/5/7/9/10/11/12/13|
| FLT-AC-010 saves/legacy/RNG |1/2/3/13/14/17|
| FLT-AC-011 integration/scale/regressions |14/15/16/17|
| FLT-AC-012 full design approval |ADR-018; already Verified for design, no runtime claim|

Self-review evidence is in [implementation-plan review](../../../reports/FLEET_COMMAND_IMPLEMENTATION_PLAN_REVIEW_2026-10-27.md). This document is the concrete separate plan for owner review. Its task boxes remain unchecked; no implementation, baseline-future capture, new gameplay tests or native fleet validation ran while writing it.
