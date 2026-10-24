# Colony Development Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the approved colony-development slice: foundational and specialist buildings, editable templates, finite queues, optional governors, scalable management/reports and recognizable individual planets.

**Architecture:** Extend the existing plain C# simulation with version-gated development state, scoped pure policy queries and empire-issued commands. Prepare Human governor orders inside the Ready transaction; retain expansion_v1 as the only AI controller. Godot consumes viewer projections and local presentation preferences; procedural portraits never affect economic state or RNG.

**Tech Stack:** Existing C#/.NET8 simulation/xUnit, Godot4.7.2 .NET and JSON; no new dependency/project/framework.

**Spec:** [2026-10-24-colony-development-design.md](../specs/2026-10-24-colony-development-design.md), fully approved in [ADR-012](../../DECISIONS/ADR-012-approved-colony-development-design.md). The separate [future direction note](../../design-notes/2026-10-24-delegation-crises-and-fleet-intent.md) does not authorize crisis/fleet implementation.

## Global Constraints

- **Native is already selected.** Use executing-plans inline, no implementer/per-task reviewer agents; one fresh whole-branch reviewer at the end on the most capable available model. Do not ask for the execution method again. This plan awaits owner review; no tasks have run.
- Keep all current building costs/effects, technology costs, worker/species rates, growth/capacity/support rules and starting assets. Start without free buildings. Species worker multipliers do not multiply flat building bonuses.
- At most one project completes per colony per turn, regardless of banked reserve. Completed effects apply from the next snapshot. Explicit finite player vessel orders retain ordinary cost, cargo, maturity and founding rules.
- Each colony independently enables workforce automation and construction automation; both start off. Workforce automation requires an assigned template; construction automation may use an explicit queue without a template. No forced takeover at6/11 colonies.
- Templates: four defaults Balanced/Industrial/Research/Growth,0..32 IDs per list, no repeats across lists, name1..80 characters, weights0..100 with positive total, at most64 custom templates. Queue at most32 entries; report history last10 completed turns. Import at most256KiB and64 templates, with whole-batch rollback.
- Governors do not invent vessel orders, choose empire research, move/found vessels, scrap anything or make destructive crisis decisions. Do not add plagues/revolts, warfare/fleet doctrine, culture-reaction systems, exclusive colony roles, currency/upkeep or a comprehensive technology tree.
- Rules5/writer6 are approved targets, not current product behavior. Current rules4/writer5 stays default until Task11. Preserve eleven old pairs and add exactly(6,1),(6,2),(6,3),(6,4),(6,5), sixteen total. Old rules1..4 omit/reject injected development fields; no automatic campaign upgrade. The complete allowed set is(1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3),(5,1),(5,2),(5,3),(5,4),(6,1),(6,2),(6,3),(6,4),(6,5).
- At most one changed target and one changed workforce command per owned colony (budget2*C). Checked integer/Int128 economics, stable local counters, ordinal/ID ordering, no governor RNG. Only the stateless cosmetic hash deliberately wraps64 bits.
- Unsurveyed planets stay unknown; stale illustrations retain observed descriptors. Foreign resources, identity, structures, orders and live appearance must not influence own policy/UI. Cache at most128 portraits.
- Preserve owner roster/research/peaceful backlog/status paragraph, all saves/recovery/worktrees and failed QA evidence. Human comprehension remains Deferred; no recruitment. No planning-time product changes, merge/push/release/cleanup.

## Review Focus

1. A manual intervention after governor preparation, followed by withdrawing/resubmitting Ready or reloading, must retain player authority and report each actual governor action once (Tasks3,5,6,9,12).
2. A queue whose first entry is locked, whose active vessel cannot embark, or whose assigned template has since changed must preserve active work/reserve and explain waiting without secretly choosing ships or research (Tasks3,4,5,8,12).
3. One-population and extreme-population colonies, zero support rate, flat-only output, Sulan habitat and Wenth changing capacity must yield bounded deterministic supported allocations, including explicit vessel cargo needs (Tasks2,4,5,12).
4. Missing/renamed content, invalid local presets/preferences, a full template library, counter exhaustion or mixed-owned bulk selection must reject atomically while Load/manual management remains usable (Tasks2,3,6,8,9,11).
5. After a version/default change, old passive/active AI traces, stale planet portraits and hidden overflowing foreign economies must stay compatible/private; a late preparation/AI/validation failure must publish nothing (Tasks1,5,6,7,10,11,12).

## Execution entry and evidence

Planning source: product main10cb0b4e86ac2d16d6ec6621eb6ca841d7c5fe2d; design branch checkpoint67c285342f27f71132f67cd21bf61b31b674fcfb. Approval/plan commits follow without product changes. Reuse the suitable isolated codex/colony-development-design checkout only if its state still matches the approved plan; otherwise create a new isolated execution checkout from the latest approved plan commit, not from primary main which lacks this documentation. Inventory first; do not reset/delete an existing branch or checkout. Seven worktrees and the primary four owner changes are currently retained. Native worktree tooling cannot resolve this chat's non-repository C directory; if still necessary, use the recorded scoped Git fallback after checking branch/path collisions, without touching primary owner files.

At execution entry run ordinary restore if needed, normal build and full simulation once. Keep normal dependency audit enabled; no routine NuGetAudit=false. Record the real engine/runtime paths, commands, exits and counts in unique reports/colony-execution/ artifacts and this plan's executing-plans ledger. Prior648 simulation tests, all four branch scenarios and integrated-main Rival results are historical, not newly executed colony tests.

Common commands, referenced as **Focused**, **Suite**, **Build**, **Runtime** below:

```powershell
# Focused: replace ClassName literally with the task's test class.
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --filter FullyQualifiedName~ClassName
# Suite
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo
# Build
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
# Runtime: use the actual retained Godot .NET console executable and a unique directory.
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <Godot-console-path> -Scenario Colony -OutputDirectory reports/colony-execution/<fresh-task-run>
# Normal metadata after ordinary rebuild (script delivered in Task8)
./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath .godot/mono/temp/bin/Debug/OpenAntares.dll
```

Focused/Suite GREEN means exit0, zero failed/skipped and actual counts recorded. Build GREEN means exit0 with zero warnings/errors. Runtime GREEN means exit0, results.passed=true/all rows passed, no final engine ERROR/WARNING/SCRIPT ERROR, successful ordinary rebuild in finally. Initial API compilation RED is allowed, but after declarations require meaningful semantic RED before implementing behavior. Do not replace frozen expected traces with outputs from changed code. Commit explicit task paths and evidence after gates; never git add . or stage owner changes. Read shared theme fresh before each minimal UI styling edit.

This is one coherent delivery plan because templates, queues, Ready behavior, persistence and planet identity share the approved version boundary and management UI. Portrait work has its own testable task; unrelated crisis/fleet projects remain deferred. No application tests/builds have run while writing this plan.

## File structure and shared interfaces

Paths use **S/**=src/OpenAntares.Simulation/, **T/**=tests/OpenAntares.Simulation.Tests/, **G/**=tests/OpenAntares.Game.Validation/. Expand these prefixes; do not create S/T/G directories. Every other path is repository-relative. In a comma-separated file list, a bare filename inherits the preceding explicit directory; ./README.md means repository-root README. The shorthand .cs/.tscn names the two files already listed in the table.

| Responsibility | Existing files | New focused files |
| --- | --- | --- |
| Frozen compatibility | T/CompatibilityFixtures.cs | T/ColonyCompatibilityTests.cs; T/Fixtures/legacy-rules4-v5-save.json, legacy-rules4-v5-continuation.json, legacy-rules4-v5-provenance.json, rules4-content.json |
| Definitions/state | S/Rules.cs; S/Content/ContentSet.cs, ContentLoader.cs; S/State/GameState.cs, GameStateValidator.cs, KnowledgeState.cs | S/Content/GovernorTemplateDefinition.cs, DevelopmentContentReader.cs; S/Development/DevelopmentState.cs, DevelopmentStateValidator.cs; S/Appearance/PlanetAppearanceDescriptor.cs; T/ColonyFixtures.cs, ColonyContentTests.cs, ColonyStateTests.cs; T/Fixtures/colony-development-content.json |
| Commands/policy | S/Commands/CommandProcessor.cs | S/Commands/DevelopmentCommands.cs, DevelopmentCommandProcessor.cs; S/Development/GovernorPlanningQueries.cs, IGovernorPlanningContext.cs, GovernorPlanner.cs, GovernorPlan.cs; T/ColonyCommandTests.cs, GovernorPlanningTests.cs |
| Atomic turns | S/Commands/CommandProcessor.cs; S/Turns/TurnResolver.cs; S/Colonization/FoundingRules.cs | S/Development/GovernorTurnCoordinator.cs, DevelopmentCompletion.cs; T/GovernorTransactionTests.cs |
| Persistence/presets | S/Saves/SaveGame.cs; S/Content/ContentWriter.cs | S/Saves/DevelopmentSaveData.cs; S/Content/DevelopmentContentWriter.cs; S/Development/TemplatePresetCodec.cs; T/ColonySaveTests.cs, TemplatePresetTests.cs |
| Appearance/projections | S/Galaxy/GalaxyGenerator.cs; S/Knowledge/ObservationUpdater.cs, EmpireView.cs, EmpireViewProjector.cs; S/State/ExpansionStateValidator.cs | S/Appearance/PlanetAppearance.cs; T/PlanetAppearanceTests.cs, ColonyKnowledgeTests.cs |
| Orders UI | game/Components/ColonyPanel.cs; game/Screens/GameScreen.cs | game/Components/ColonyOrdersPanel.cs, GovernorTemplateDialog.cs, ConstructionQueueEditor.cs, TemplatePresetDialog.cs |
| Management/reports | game/Components/ColonyList.cs, TurnReportDialog.cs; game/Screens/GameScreen.cs | game/Components/EmpireManagementDialog.cs, GovernorReportPanel.cs, ColonyNotificationDialog.cs; game/Presentation/ColonyNotificationPreferences.cs |
| Portraits | game/Components/StarPanel.cs, ColonyPanel.cs; game/theme/openantares_theme.tres | game/Components/PlanetPortrait.cs; game/Presentation/PlanetPortraitRenderer.cs, PlanetPortraitCache.cs, PlanetStyleCatalog.cs; art/planets/planet_styles.json |
| Activation/acceptance | content/core/buildings.json, technologies.json; game/ContentFiles.cs; game/Screens/NewGameScreen.cs; G/Run-Validation.ps1, Validation.targets, SpeciesGraphicalValidation.cs, RivalGraphicalValidation.cs, README.md | content/core/governor_templates.json; G/ColonyGraphicalValidation.cs, ColonyGraphicalValidation.tscn, FrozenContent.cs, Verify-NormalAssembly.ps1; reports/COLONY_VALIDATION_2026-10-24.md, COLONY_FINAL_REVIEW_2026-10-24.md |

S/State/API names below are fixed for cross-task consistency; constructor/init defaults may follow existing conventions, but their meaning must not drift:

- `SupportTargetMode { Minimum, CappedGrowth }`; `ProductionOrigin { None, Manual, Queue, Template }` in S/Development.
- `GovernorTemplateDefinition(string Id,string Name,ImmutableArray<string> FoundationIds,ImmutableArray<string> DevelopmentIds,SupportTargetMode SupportTarget,int ProductionWeight,int ResearchWeight)` in S/Content. `ContentSet.GovernorTemplates` is `ImmutableSortedDictionary<string,GovernorTemplateDefinition>` with ordinal ordering, empty for older embedded content.
- `GovernorTemplateDraft(string Name,ImmutableArray<string> FoundationIds,ImmutableArray<string> DevelopmentIds,SupportTargetMode SupportTarget,int ProductionWeight,int ResearchWeight)` and `GovernorTemplateSnapshot(string Id,int Revision,GovernorTemplateDraft Definition)` in S/Development. Defaults use revision1; latest custom revisions live in an empire library, assigned snapshots are immutable copies.
- `ProductionQueueEntry(int Token,ProductionTarget Target)` and input `QueueEntryDraft(int? Token,ProductionTarget Target)`. Missing input token allocates a new local token; a supplied token must already belong to that colony. No entity-ID allocation.
- `ColonyDevelopmentState`: Template nullable snapshot; WorkforceEnabled/ConstructionEnabled; List<ProductionQueueEntry> Queue; NextQueueToken initially1; ActiveQueueToken nullable; Origin. `ColonyState.Development` is null for rules1..4, required for5; AI has only disabled/empty state.
- `NewColonyPlan(GovernorTemplateSnapshot Template,bool WorkforceEnabled,bool ConstructionEnabled)`; `EmpireDevelopmentState`: CustomTemplates latest snapshots, NextTemplateNumber initially1, nullable NewColonyPlan, nullable PendingReport, CompletedReports last10. `EmpireState.Development` follows the same version requirement; AI libraries/inheritance/reports stay empty.
- `GovernorIssue(string Code,string? ContentId)`; `GovernorReportEntry(ColonyId Colony,Workforce? BeforeWorkforce,Workforce? AfterWorkforce,ProductionTarget? BeforeTarget,ProductionTarget? AfterTarget,ImmutableArray<GovernorIssue> Issues,bool WorkforceOverridden,bool ConstructionOverridden)`; `GovernorTurnReport(int Turn,ImmutableArray<GovernorReportEntry> Entries,int UnchangedCount)`. Turn denotes the planning turn whose resolution produced the report.
- `PlanetAppearanceDescriptor(int Version,int Variant,ulong SurfaceSeed)`; nullable Appearance init properties on PlanetState, PlanetObservation and KnownPlanetView. Rules5 requires a supported descriptor in planet and surveyed facts; legacy fields are absent.
- Add init-only `OwnedColonyView.Development` as immutable `ColonyDevelopmentView(GovernorTemplateSnapshot? Template,bool WorkforceEnabled,bool ConstructionEnabled,ImmutableArray<ProductionQueueEntry> Queue,int? ActiveQueueToken,ProductionOrigin Origin)`. `EmpireView.Development` is immutable `EmpireDevelopmentView(ImmutableArray<GovernorTemplateSnapshot> CustomTemplates,NewColonyPlan? NewColonyPlan,GovernorTurnReport? PendingReport,ImmutableArray<GovernorTurnReport> CompletedReports)` of only its viewer's data. Keep existing positional constructors. No mutable state object reaches UI/policy.

### Task 1: Freeze genuine rules-4 continuation under unchanged code

**Files:** Modify T/CompatibilityFixtures.cs; create the four frozen files above and T/ColonyCompatibilityTests.cs. No product code or current-content edits.

**Interfaces:** Add `CompatibilityFixtures.LoadRules4() -> LoadedGame`, `Rules4Trace() -> ImmutableArray<string>`, `Rules4Content() -> ContentSet`. Reuse existing `CanonicalWithoutSchema(ContentSet,GameState) -> string`; only schema_version may be normalized.

- [ ] **RED:** Add `GenuineActiveAiRules4ContinuesExactly` asserting the fixture exists, rules4/schema5 provenance, source turn14/five colonies/one active expansion_v1 AI and exactly four canonical future states; each future is checked before/after save/reload. Focused ColonyCompatibilityTests must fail because the genuine fixture is absent, not because an arbitrary expectation was manufactured.
- [ ] **Capture:** Before ANY writer/rules/content/default change, copy retained actual QA `C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves/qa-20261024-194131-796-rival-peldra-small-expanded.json` without overwriting it. SHA256 must be `9f9b812a5ce5102bf5fbf7ae3d6a1e8fedf64ce91e0d4ef287280100999a0354`. Record product source10cb0b4, original path/hash, no injected live state. With the unchanged engine execute four ordinary last-human Ready commands from this state and store actual canonical results/provenance. Extract its embedded content verbatim to rules4-content.json. If source is unavailable, stop capture and locate another genuine retained rules4 QA with explicit provenance; never synthesize old expected behavior using changed code.
- [ ] **GREEN:** Focused ColonyCompatibilityTests then Suite; expect all pass with zero failures/skips. Freeze existing rules1/2/3/passive-AI files untouched. Retain capture helper/commands in this plan's execution workspace.
- [ ] **Check:** Demonstrate no S/, game/, content/, art/ diff and no primary file/hash change. Full tests cannot substitute for that before-change capture.
- [ ] **Commit:** `test: freeze genuine rules4 campaign continuation` with explicit fixture/test/ledger paths.

Representative literal assertions for the preceding task:

```csharp
LoadedGame old = CompatibilityFixtures.LoadRules4();
Assert.Equal(4, old.State.RulesVersion);
Assert.Equal(5, old.State.Colonies.Count);
Assert.Equal(4, CompatibilityFixtures.Rules4Trace().Length);
```

### Task 2: Add gated development definitions and deeply cloned state

**Files:** The Definitions/state row above plus S/Knowledge/EmpireView.cs for declarations only; modify relevant hard-coded supported-version checks in S/State/SpeciesStateValidator.cs, S/State/RivalStateValidator.cs and S/Rules.cs. Create T/ColonyContentTests.cs, ColonyStateTests.cs, ColonyFixtures.cs and the proposed complete test-only content fixture. Do not yet edit live core JSON or Rules.CurrentVersion.

**Interfaces:** Produce all shared types above; `Rules.DevelopmentVersion=5`, `HasDevelopment(int)->bool`; include5 in explicit expansion/species/rival predicates and IsSupported while CurrentVersion stays4. `DevelopmentStateValidator.Validate(ContentSet,GameState,List<string>) -> void`; extend `ContentLoader.Load(IEnumerable<ContentSource>,bool requireSpecies=false,bool requireRivals=false,bool requireDevelopment=false)` without breaking existing callers. `ColonyFixtures.Game(string species="harl",int population=6) -> LoadedGame` and `WithFoundations(string species,int population,string templateId) -> LoadedGame` make a labelled valid rules5 test state with Manual switches, appropriate biology and appearance, never a claimed genuine old capture. WithFoundations has Hall/Hub/Lab completed, all six fixture technologies known, the selected snapshot/workforce switch on and construction off; no Habitat Extension unless the test adds it before starting. Test-only setup is not a repaired live campaign.

- [ ] **RED:** `ColonyContentTests` asserts four default names/IDs (`balanced`,`industrial`,`research`,`growth`), exact foundation/development arrays and support/weights; `ColonyStateTests` asserts new fields clone independently and rules1..4 reject even null/empty development injection. Watch missing API RED, add declarations, then require semantic validation RED.
- [ ] **Implement:** Parse templates with file/entry/field errors, all limits and references from the approved spec; add the new three technologies/buildings only to T/Fixtures/colony-development-content.json now. Literal costs: techs2400 each; Production Complex3200/+300production, Discovery Center3200/+300research, Habitat Network2800/+200support/+2capacity. Preserve existing numbers. Defaults map exactly to spec COL-002; ContentSet constructors remain compatible.
- [ ] **Validate:** Tests cover empty valid building-free Balanced alternative, renamed/reordered IDs, wrong/missing references, duplicate lists/IDs, weights0/0 and101, lengths0/81, custom64/65, queue32/33, duplicate/local token exhaustion, AI nonempty controls, descriptor version/ranges, private report turns/order/history limits and deep clone of every nested list. No triple enumeration for extreme population. Populate explicit test-only state without silently upgrading old games.
- [ ] **GREEN:** Focused ColonyContentTests and ColonyStateTests, Suite, Build. Expect exit0/zero diagnostics; default still4 and Task1 traces unchanged.
- [ ] **Commit:** `feat: add gated colony development definitions and state`.

Representative literal assertions for the preceding task:

```csharp
var game = ColonyFixtures.Game();
Assert.Equal(5, game.State.RulesVersion);
Assert.Equal(4, game.Content.GovernorTemplates.Count);
Assert.False(game.State.Colonies[0].Development!.WorkforceEnabled);
Assert.False(game.State.Colonies[0].Development!.ConstructionEnabled);
```

### Task 3: Implement atomic template, queue and inheritance commands

**Files:** S/Commands/DevelopmentCommands.cs, DevelopmentCommandProcessor.cs, CommandProcessor.cs, CommandOutcome.cs; T/ColonyCommandTests.cs.

**Interfaces:** All following commands derive existing `Command(Issuer,ExpectedTurn)` and return existing CommandOutcome via Execute:

`UpsertGovernorTemplateCommand(EmpireId,int,string? TemplateId,GovernorTemplateDraft Draft)`; `DeleteGovernorTemplateCommand(EmpireId,int,string TemplateId)`; `AssignGovernorTemplateCommand(EmpireId,int,ImmutableArray<ColonyId> Colonies,string? TemplateId,bool WorkforceEnabled,bool ConstructionEnabled,bool ReplaceActive=false)`; `SetColonyAutomationCommand(EmpireId,int,ColonyId Colony,bool WorkforceEnabled,bool ConstructionEnabled)`; `SetColonyQueueCommand(EmpireId,int,ColonyId Colony,ImmutableArray<QueueEntryDraft> Entries,bool ReplaceActive=false)`; `SetNewColonyPlanCommand(EmpireId,int,string? TemplateId,bool WorkforceEnabled,bool ConstructionEnabled)`; `ImportGovernorTemplatesCommand(EmpireId,int,ImmutableArray<GovernorTemplateDraft> Templates)`.

TemplateId=null on Upsert means create/copy, never edit a built-in. Assign/Inheritance resolve the current own library/default revision and copy it; later edits do not propagate. SetAutomation resumes/pauses the retained assigned snapshot without resolving a new revision. Built-ins are immutable. New custom IDs are custom_<NextTemplateNumber>, skipping defined/local collisions deterministically; allocation/counter exhaustion rolls back. Tokens follow the same checked local rule.

- [ ] **RED:** Add `CopiesAreIndependentAndAssignmentIsASnapshot`, `MixedOwnershipBulkAssignmentChangesNothing`, `ReferencedTemplateCannotBeDeleted`, `ManualOrdersPauseOnlyTheirChannel`, `QueueEditCannotSilentlyReplaceActiveWork`, `StructurallyEqualCommandsAreNoOps`. Assert same reference/canonical bytes/readiness on reject/no-op, not only error strings. Focused ColonyCommandTests must fail semantically.
- [ ] **Implement:** Validate complete batches before copy/publication in colony-ID order; human issuer/owned colonies only. A true change clears only issuer Ready. Upsert with structurally identical draft does not increment revision. Queue preserves stable tokens; active replacement/removal requires ReplaceActive, retains production reserve and clears only affected binding/target. Manual SetWorkforce/SetProject/SetProductionTarget changes under5 pause their channel, set appropriate origin and mark any pending auto action overridden; no-op selections do not pause. Import, deletion and inheritance obey all limits/ref constraints atomically.
- [ ] **Pin focus edges:** Test old assigned revision after library edit/saveable snapshot, both switches independently, null-template workforce rejection but queue-only construction acceptance, active manually pinned project on Resume, deleting an inherited template, duplicate mixed batch IDs, counter=int.MaxValue, foreign token, repeated vessels versus duplicate building, locked-but-defined queue target, and every new command rejected under rules1..4. Derived state cannot contain completed building entries; Task5 owns ordinary completion cleanup.
- [ ] **GREEN:** Focused ColonyCommandTests then Suite/Build; no active governor scheduling yet, no direct state mutation by UI.
- [ ] **Commit:** `feat: add atomic colony plans and construction queues`.

Representative literal assertions for the preceding task:

```csharp
var game = ColonyFixtures.Game();
var own = game.State.Colonies[0];
var result = CommandProcessor.Execute(game.Content, game.State,
    new AssignGovernorTemplateCommand(own.EmpireId, game.State.Turn,
        ImmutableArray.Create(own.Id, new ColonyId(int.MaxValue)), "industrial", true, true));
Assert.False(result.Accepted);
Assert.Same(game.State, result.State);
```

### Task 4: Implement scoped deterministic construction/workforce policy

**Files:** S/Development/IGovernorPlanningContext.cs, GovernorPlanningQueries.cs, GovernorPlan.cs, GovernorPlanner.cs; S/Knowledge/EmpireView.cs, EmpireViewProjector.cs for immutable own-development projections; T/GovernorPlanningTests.cs. Leave rival policy interfaces/behavior intact; reuse their permitted-own-frame calculation pattern rather than cloning full-world state.

**Interfaces:** `IGovernorPlanningContext` exposes `ContentSet Content`, `EmpireView View`, `ColonyEconomy? Forecast(ColonyId,Workforce,ProductionTarget?)`. `GovernorPlanningQueries.Create(ContentSet,EmpireView) -> IGovernorPlanningContext`; `GovernorColonyOrder(ColonyId Colony,ProductionTarget? Target,Workforce Workforce,int? QueueToken,ProductionOrigin Origin,ImmutableArray<GovernorIssue> Issues)`; `GovernorPlan(bool Succeeded,ImmutableArray<GovernorColonyOrder> Orders,string? Error)`; `GovernorPlanner.Plan(IGovernorPlanningContext) -> GovernorPlan`. All inputs are immutable own projections/public definitions, scrubbed seed/counters; no GameState escapes into policy. Populate own development/library/inheritance/pending/history projections in this task so the policy and Task5 do not depend on later portrait work. Issue codes are support_shortage, construction_locked, embarkation_wait and plan_complete, with optional relevant content ID; stable issue identity is colony/code/content ID. construction_locked is Attention only when no eligible work exists; earlier locked entries otherwise remain visible in queue preview without repeated Attention. Unknown issue codes reject on load.

- [ ] **RED:** In GovernorPlanningTests add literal table assertions: HarlN6 Balanced2/2/2 outputs500/500; Industrial2/3/1=700/300; Research2/1/3=300/700; Growth3/2/1=500/300,growth90; fullN10 Growth3/4/3=900/700,growth0; PeldraN6 Industrial4/2/0=1300/100; Harl Industrial+Complex=1000/300. Quantities are hundredths. Watch semantic failure after API declarations.
- [ ] **Implement:** Preserve active valid project; else first legal explicit queue entry, else first legal missing foundation then development entry, else null/bank. Locked entries remain queued and diagnosed. Construction-disabled targets stay unchanged. Support is the exact clamped ceiling rule from COL-004, with0-rate branch, full-capacity growth fallback and explicit player vessel cargo floor. Largest-remainder allocation ties to production; production floor only when selected/active project has zero flat production. Workforce-disabled assignments stay unchanged. No invented ship/research/founding choices or RNG.
- [ ] **Pin focus edges:** Assert locked-first/legal-second, entirely locked, exhausted plan, existing manual project after template change, unembarkable vessel retained, Peldra shortage, one population, zero-rate fully flat-supported versus impossible support, population=int.MaxValue with correspondingly valid capacity/content and bounded arithmetic, Sulan native/sealed, Wenth capacity6→8/full-source/cargo3, custom0:100 weights with factory absent/present. Mutate hidden foreign reserves to long.MaxValue, orders/identity and unseen planet facts; own plan must remain identical without a global calculator call. Own overflow yields an unsuccessful plan, not partial orders.
- [ ] **GREEN:** Focused GovernorPlanningTests then Suite/Build; repeated projections/plans are pure and consume no IDs/RNG.
- [ ] **Commit:** `feat: plan colony governors from scoped forecasts`.

Representative literal assertions for the preceding task:

```csharp
var game = ColonyFixtures.WithFoundations("harl", 6, "industrial");
var view = EmpireViewProjector.Project(game.Content, game.State, game.State.Empires[0].Id);
var plan = GovernorPlanner.Plan(GovernorPlanningQueries.Create(game.Content, view));
Assert.True(plan.Succeeded);
Assert.Equal(new Workforce(2, 3, 1), Assert.Single(plan.Orders).Workforce);
```

### Task 5: Integrate governors into Ready, completion and reporting atomically

**Files:** S/Development/GovernorTurnCoordinator.cs, DevelopmentCompletion.cs; S/Commands/CommandProcessor.cs, DevelopmentCommandProcessor.cs; S/Turns/TurnResolver.cs; S/Colonization/FoundingRules.cs; T/GovernorTransactionTests.cs.

**Interfaces:** Add test helper `ColonyFixtures.TwoHumansAndAi() -> LoadedGame` here, a valid labelled rules5 state with sorted unique entities and disabled AI development. `GovernorTurnCoordinator.Prepare(ContentSet,GameState,EmpireId) -> GovernorPreparationResult(GameState State,bool Succeeded)`; internal `CommandProcessor.ExecuteGovernor(ContentSet,GameState,Command,int? queueToken,ProductionOrigin origin) -> CommandOutcome` permits only the prepared issuer's enabled target/workforce changes with all ordinary legality checks and no Ready recursion. `DevelopmentCompletion.Apply(ContentSet,GameState candidate,TurnReport report) -> void` owns documented resolver effects, not player orders. Add internal `Prepare(ContentSet,GameState,EmpireId,Func<IGovernorPlanningContext,GovernorPlan>) -> GovernorPreparationResult` for fault tests as existing TurnResolver does; no production test switches.

- [ ] **RED:** Add `LastHumanFailureRestoresExactPreReadyState`, `NonfinalReadyPublishesOnlyItsOwnOrders`, `WithdrawAndResubmitDoesNotDuplicateActions`, `ManualInterventionWinsAfterPreparation`, `OneCompletionConsumesOnlyBoundQueueEntry`, `NewColonyInheritanceWaitsUntilNextPlanning`. Use two humans plus AI and deep/reference equality. Rejection/budget/overflow faults must fail on old behavior before integration.
- [ ] **Implement:** Under5, Ready false→true prepares only its human owner before setting Ready; no-op true skips preparation. Count at most2*C changed target/workforce commands, automatic provenance preserves switches and validates queue binding/origin. Nonfinal publishes planning only; final wraps preparation+existing rival/turn transaction. On failure return exact before-Ready original. AI remains exclusively expansion_v1; never invoke human governors over its colonies. Old1..4 path remains byte-identical.
- [ ] **Complete:** Existing economic completion still occurs once/turn. Consume only active matching queued vessel; remove satisfied queued building when it completes through any origin; clear completed binding/origin and choose no next target until next Ready. Founding assigns disabled development state or an explicit human inheritance snapshot after ordinary founding output, without changing same-turn effects. Pending report is keyed by planning turn/colony/action: preserve first-before/latest-after, replace same-key decisions, no-op re-preparation neither duplicates nor erases actual pending changes; manual changes mark that channel overridden. Successful resolution seals one report and retains last10; failure seals nothing. Summaries include unchanged count and deduplicated issue codes; never publish foreign failure details.
- [ ] **GREEN:** Focused GovernorTransactionTests, existing RivalTransactionTests, then Suite/Build. Cover every new command no-op, nonfinal/final/withdraw/retry and injected late AI/final-validation rejection, ID/counter/output overflow. Assert state references/canonical bytes/readiness/counters/reports/RNG unchanged on failure and already-accepted other-human orders preserved.
- [ ] **Commit:** `feat: resolve governor orders in the ready transaction`.

Representative literal assertions for the preceding task:

```csharp
var game = ColonyFixtures.TwoHumansAndAi();
var issuer = game.State.Empires.First(e => e.Controller == ControllerKind.Human).Id;
var outcome = CommandProcessor.Execute(game.Content, game.State,
    new SetReadyCommand(issuer, game.State.Turn, true));
Assert.True(outcome.Accepted);
Assert.Null(outcome.Report);
Assert.Equal(game.State.Turn, outcome.State.Turn);
Assert.True(outcome.State.FindEmpire(issuer)!.Ready);
```

### Task 6: Add schema6 persistence and reusable preset codec

**Files:** S/Saves/DevelopmentSaveData.cs, SaveGame.cs; S/Content/DevelopmentContentWriter.cs, ContentWriter.cs; S/Development/TemplatePresetCodec.cs; T/ColonySaveTests.cs, TemplatePresetTests.cs; existing T/SaveGameTests.cs and RivalSaveTests.cs for explicit version-matrix expectations only. Keep CurrentVersion4 and live core content unchanged.

**Interfaces:** Focused save/content helpers follow existing partial reader/writer style using Utf8JsonWriter/EntryReader; `TemplatePresetCodec.Serialize(ImmutableArray<GovernorTemplateDraft>) -> string`; `Deserialize(string json,ContentSet) -> TemplatePresetResult(ImmutableArray<GovernorTemplateDraft> Templates,ImmutableArray<string> Errors)`. File I/O remains UI's job. Add explicit schema6 writing; SaveGame public Serialize/Deserialize/ReadFile/WriteFile signatures stay intact.

- [ ] **RED:** `ColonySaveTests` exhaustively covers all sixteen allowed pairs and unsupported pairs, exact old trace, pending Ready/queue/template revisions/history/inheritance/appearance round-trip; old-format injections and missing rules5 fields reject unchanged. `TemplatePresetTests` rejects256KiB+1,65 templates, lists33, invalid refs/weights/JSON, repeated JSON fields, duplicate building references and injected campaign IDs/counters atomically; empty library import is a no-op. Watch real missing-format/semantic RED.
- [ ] **Implement:** Rules5 serialization uses required `content.governor_templates`, `empire.development`, `colony.development`, `planet.appearance`, surveyed `appearance`. Explicit empty objects/lists/counters are written. Descriptor v1 has version/variant/surface_seed; pending/history and queue origin/binding must agree with content and current state. Legacy writes omit all these fields, not null placeholders. Reader rejects those fields under old rules, checks refs/limits/ownership/turn order and deeply clones nested values. Do not consult current disk content while loading.
- [ ] **Presets:** Format `{format:"OpenAntaresGovernorTemplates",version:1,templates:[drafts]}` has no campaign IDs/counters, code, paths or live assignment. UTF8 byte-length bound applies before parsing. Decode yields validated drafts; Import command from Task3 allocates new own IDs only after every item passes. Preserve existing state and destination file on load/write failure; test file failure against an isolated test path, never player saves.
- [ ] **GREEN:** Focused ColonySaveTests/TemplatePresetTests, Task1 ColonyCompatibilityTests, Suite/Build. Task1 canonical future states may normalize schema_version only. Genuine1/2/3/passive-AI checks remain unchanged and green. Move existing unsupported-schema6 probes to7 and retain their rejection purpose; never weaken a failure assertion merely because6 is now supported.
- [ ] **Commit:** `feat: persist development campaigns and template presets`.

Representative literal assertions for the preceding task:

```csharp
var game = ColonyFixtures.Game();
var loaded = SaveGame.Deserialize(SaveGame.Serialize(game.Content, game.State));
Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
Assert.Equal(5, loaded.Game!.State.RulesVersion);
Assert.False(loaded.Game.State.Colonies[0].Development!.WorkforceEnabled);
```

### Task 7: Generate immutable visual identity and private development views

**Files:** S/Appearance/PlanetAppearance.cs; S/Galaxy/GalaxyGenerator.cs; S/Knowledge/ObservationUpdater.cs, EmpireView.cs, EmpireViewProjector.cs; S/State/ExpansionStateValidator.cs; T/PlanetAppearanceTests.cs, ColonyKnowledgeTests.cs.

**Interfaces:** `PlanetAppearance.Create(ulong galaxySeed,PlanetId planet) -> PlanetAppearanceDescriptor`; pure query `GovernorPlanningQueries.Preview(ContentSet,EmpireView,ColonyId,GovernorTemplateSnapshot?,bool workforceEnabled,bool constructionEnabled) -> GovernorColonyOrder?` delegates to Task4 policy without publishing. Views use immutable shared types; reports expose only the viewer's entries/history. No positional projection constructor changes.

- [ ] **RED:** `PlanetAppearanceTests.StableVectorsDoNotAdvanceRandomOrIds` pins UTF8 bytes of invariant decimal `OpenAntares/planet/v1|<seed>|<planetID>`, FNV-1a64 offset14695981039346656037/prime1099511628211 with explicit unchecked wrap after each byte. Descriptor=(1,hash&7,hash>>3). Literal vectors (seed,ID→hashHex/variant/surface_seed):0,1→2e1eaf3dcb28649f/7/415410781872786579;42,7→85645c20235b92bf/7/1201488599727174231;42,8→85646120235b9b3e/6/1201489286921941863;42,2147483647→852aba3eaba57aca/2/1199460841799593817;18446744073709551615,1→ba5c254115ff07a2/2/1678581143798014196. Add current/unknown/stale portrait/view privacy tests; initial unknown API then semantic RED.
- [ ] **Implement:** For explicit rules5 generation, initialize valid disabled development states and assign descriptors after base generation/entity allocation, before first observations. No extra RNG draws; existing rules4 base geometry/IDs/RNG match after removing only new metadata/rules tag. Copy descriptors into PlanetObservation on survey; validator checks current matching immutable facts. Legacy no appearance generation/write. Project only surveyed/owned appearance and viewer-owned library/reports; unknown is null. Foreground selection uses only KnownPlanetView, never raw foreign PlanetState.
- [ ] **Pin focus edges:** Alter unseen foreign appearance/resources/identity/buildings/orders and verify exact own plan/visible data equality; stale illustration stays last-seen, own preview is read-only and disabled/unknown/foreign colony preview rejects. Rules5 Solo, all16 species pairs×3 sizes, renamed/reordered content and added planets preserve established descriptors; missing/invalid appearance fails atomically. Old passive/active AI remains unchanged.
- [ ] **GREEN:** Focused PlanetAppearanceTests/ColonyKnowledgeTests, Suite/Build. Compare base galaxy facts using frozen rules4 content and otherwise identical biological/galaxy parameters, not new content accidentally changing the reference.
- [ ] **Commit:** `feat: project stable planetary identity and governor views`.

Representative literal assertions for the preceding task:

```csharp
Assert.Equal(new PlanetAppearanceDescriptor(1, 7, 1201488599727174231UL),
    PlanetAppearance.Create(42UL, new PlanetId(7)));
```

### Task 8: Build editable template and queue controls with actual UI regressions

**Files:** game/Components/ColonyOrdersPanel.cs, GovernorTemplateDialog.cs, ConstructionQueueEditor.cs, TemplatePresetDialog.cs; game/Components/ColonyPanel.cs; game/Screens/GameScreen.cs; G/ColonyGraphicalValidation.cs/.tscn, Run-Validation.ps1, Validation.targets, Verify-NormalAssembly.ps1; minimal shared theme edits after fresh read.

**Interfaces:** New panels consume ContentSet/EmpireView/OwnedColonyView and raise `event Action<Command> CommandIssued`; no mutable session is passed for edits. `ColonyOrdersPanel.ShowColony(ContentSet,EmpireView,OwnedColonyView) -> void`; dialog Open methods take those views and current validated draft. Preview invokes Task7 pure Preview and shows before/after/work retention; preset dialogs read/write only the explicitly selected path and submit Task3 Import. Runtime adds `-Scenario Colony`, with all five validation types opt-in/excluded after normal rebuild. Verify-NormalAssembly.ps1 uses System.Reflection.Metadata/PEReader to inspect the supplied ordinary assembly for those five exact class names, exiting1 if any is present,0 only if all are absent; no new product/test project or dependency.

- [ ] **RED:** Add actual-control tests against a labelled schema6/rules5 initial campaign loaded through ordinary Load: see/copy four defaults, edit/create custom, apply snapshot then edit library without live propagation, toggle each channel, queue locked-first/legal-next, explicit Replace versus retain active, export/import preset and show atomic invalid-import diagnostics. Old UI must fail missing controls before adding them. Retain real results.json and PNGs in unique directories; no live state repairs.
- [ ] **Implement:** Use stable control Names GovernorTemplatePicker, WorkforceGovernorToggle, ConstructionGovernorToggle, ConstructionQueue, ApplyTemplate and ReplaceActiveProject in the owning panels. Build dialogs from existing containers/anchors/theme, with finite queue reorder/remove controls, visible unmet prerequisites, origin/paused status, explicit replace preview and read-only default originals. Same-project/manual no-op does not pause. Preserve invalid disk-content Load recovery. Disable controls on old rules with their existing legacy banner; do not upgrade a loaded campaign.
- [ ] **Pin focus edges:** At800x600 exercise focus/scroll to every action and keep confirmation/close reachable; then1920. A full64-template library, invalid names/weights, wrong preset IDs, queued vessel cargo blockage and edited-content defaults must produce useful messages without destroying current session. Files use unique QA/preset names; preserve any existing local preferences/files exactly.
- [ ] **GREEN:** Runtime Colony initial UI group, Build/Suite; ordinary rebuild must exclude GraphicalValidation, ExpansionGraphicalValidation, SpeciesGraphicalValidation, RivalGraphicalValidation and ColonyGraphicalValidation. Record real counts, not target assertion totals. Once, run the metadata checker against the real opt-in assembly and observe failure, then rebuild ordinary and require success; retain normal rebuild in finally.
- [ ] **Commit:** `feat: add colony template and construction queue controls`.

### Task 9: Add scalable management, governor digest and local notifications

**Files:** game/Components/EmpireManagementDialog.cs, GovernorReportPanel.cs, ColonyNotificationDialog.cs; game/Presentation/ColonyNotificationPreferences.cs; game/Components/ColonyList.cs, TurnReportDialog.cs; game/Screens/GameScreen.cs; G/ColonyGraphicalValidation.cs; theme read/minimal edits.

**Interfaces:** `EmpireManagementDialog.ShowEmpire(ContentSet,EmpireView) -> void`, same CommandIssued event; read-only filters/selection never issue orders. `GovernorReportPanel.ShowReports(ContentSet,EmpireView) -> void`. `NotificationLevel { Summary, Attention, Off }`; `ColonyNotificationPreferences` maps known routine categories to levels, with `static Load(string path) -> ColonyNotificationPreferences`, `Save(string path) -> void`, `LevelFor(string category) -> NotificationLevel` and `WithLevel(string category,NotificationLevel) -> ColonyNotificationPreferences` confined to game presentation. Store immutable Levels and read-only Diagnostics; the dialog edits copies, then saves only on explicit Apply. Normal path is user://colony-notifications.json; harness injects unique QA path rather than overwriting owner preferences. Malformed/missing preferences revert to safe defaults with diagnostics, never campaign failure.

- [ ] **RED:** Real UI tests load separately labelled valid1/6/11/30-colony fixtures. Assert bulk selection/apply, independent manual favorite, template/exception filters, one clearable nonmodal size suggestion, thirty healthy worlds without thirty completion acknowledgements, expand exact worker/project reasons and ten-turn history after real turns/save/reload. Exercise category changes and Off without deleting reports or hiding derived dashboard exceptions. Watch missing management/digest/filters RED.
- [ ] **Implement:** Use stable control Names ManageColonies, ManagementColonyList, ManagementFilter, BulkApplyTemplate and GovernorDigest. Show own template/status/current-next work/output/issues and a neutral portrait slot that Task10 connects; do not reference its future class before it exists; batch commands remain atomic. Group healthy/unchanged worlds, sort issue drilldown by colonyID/reason and retain clearable prompts. Routine actions/completions default Summary; unresolved shortage, locked plan and embarkation default Attention. No fabricated Emergency occurrence or crisis acknowledgement state. Local settings affect display only; derived unresolved facts remain accessible after reset/load and no foreign history is visible.
- [ ] **Pin focus edges:** Mixed-owned or stale-turn selected batch fails all; a manual intervention after governor preparation is shown as overridden, not attributed to governor success. Filter/reopen/resize/load cannot issue an order, advance RNG or rerun preparation. Exported presets are portable across compatible campaigns and incompatible IDs explain rejection.
- [ ] **GREEN:** Runtime Colony management group at800/1920 and real save/reload, Suite/Build; inspect representative screenshots. No physical-input/readability/accessibility claim from in-process signals.
- [ ] **Commit:** `feat: add empire governor management and reports`.

### Task 10: Render varied original planetary portraits with bounded resources

**Files:** game/Components/PlanetPortrait.cs; game/Presentation/PlanetPortraitRenderer.cs, PlanetPortraitCache.cs, PlanetStyleCatalog.cs; art/planets/planet_styles.json; game/Components/StarPanel.cs, ColonyPanel.cs, EmpireManagementDialog.cs; G/ColonyGraphicalValidation.cs; shared theme read/minimal edits, ./LICENSING.md only if needed.

**Interfaces:** `PlanetPortrait.ShowPlanet(KnownPlanetView? planet) -> void`; owned portraits receive the corresponding permitted planet view. `PlanetStyle(string Id,int Version,ImmutableArray<Color> SurfacePalette,Color OceanColor,Color CloudColor,string PatternId)` and `PlanetStyleCatalog.Load(string resourcePath) -> PlanetStyleCatalog`/`Resolve(string planetTypeId,string? environmentId,int variant) -> PlanetStyle`; `PlanetPortraitRenderer.Render(PlanetAppearanceDescriptor,PlanetStyle,Vector2I size) -> Image`; `PlanetPortraitCache.Get(PlanetAppearanceDescriptor descriptor,PlanetStyle style,Vector2I size) -> ImageTexture`, `int Count { get; }`, `Clear() -> void`, Count≤128. GameScreen owns one catalogue/cache for its mounted view; `PlanetPortrait.Configure(PlanetStyleCatalog,PlanetPortraitCache) -> void` injects them into all portraits, with no global service locator or separate128-item cache per thumbnail. No Godot/image types in simulation. Styles/palettes come from versioned original presentation resource data, no hard-coded theme colors in C#; generic environment-consistent fallback supports renamed/custom type IDs.

- [ ] **RED:** Actual graphical gallery enumerates each of four core types×three known environments×eight signatures (96), including valid labelled combinations whose generation weight is0. Assert descriptor repeatability/unknown-neutral behavior and cache cap, but retain PNGs for actual visual inspection; a pixel checksum alone is not sufficient aesthetic evidence. Initial missing portrait/image output is semantic RED.
- [ ] **Implement:** Procedural original globe/surface patterns vary recognizable land/sea or dry geological structure, palette and clouds, with no resource/hazard/night-light implications. Cache by descriptor/style-version/size; deterministic LRU with key tie break, release evicted/replaced ownership and bound128. Eviction drops cache ownership without disposing a texture still bound to a visible control; controls release their own replaced textures and GameScreen clears its cache on exit. Render only survey/own descriptors. Stale illustration does not change with hidden foreign activity; unknown planet is neutral. Image/rendering never mutates authoritative state or calls RNG. No external library/shader/download needed.
- [ ] **Pin focus edges:** Visit>128 distinct portraits and resize/reopen repeatedly; inspect Count/resources bounded. Load/save-as preserves same signature, hidden appearance changes do not change unknown/stale portraits, and generic modded styles still produce eight meaningful signatures without content-load failure. At800 and1920 capture all twelve labelled gallery pages with eight distinct signatures each, plus owned/survey/management views. S/State/RNG snapshots remain identical before/after rendering.
- [ ] **GREEN:** Runtime Colony portrait group; Build/Suite and normal exclusion check; visually inspect gallery pages for genuine structural variety and environment consistency. Record human aesthetic approval as unverified, not inferred from images.
- [ ] **Commit:** `feat: add stable varied planet portraits`.

### Task 11: Activate approved content and rules5 defaults without old-campaign drift

**Files:** content/core/buildings.json, technologies.json, governor_templates.json; S/Rules.cs, S/Galaxy/GalaxyGenerator.cs; game/ContentFiles.cs, Screens/NewGameScreen.cs, Main.cs if needed for content errors; G/SpeciesGraphicalValidation.cs, RivalGraphicalValidation.cs, FrozenContent.cs; T/ColonyContentTests.cs, ColonyCompatibilityTests.cs; existing T/GalaxyGeneratorTests.cs, ContentLoaderTests.cs, RivalGenerationTests.cs, RivalContentTests.cs only where expectations intentionally describe current defaults/content. Preserve explicit version/economy fixture assertions.

**Interfaces:** `ContentFiles.LoadCore()` keeps its public signature and calls ContentLoader.Load with requireSpecies/requireRivals/requireDevelopment=true for current5, validating complete current content. Saved old-content loading still chooses flags from that saved rules version. `G/FrozenContent.Rules4() -> ContentSet` reads Task1 rules4-content.json through existing content loader; old isolated species/expansion fixtures can use their captured embedded content. New Game sends explicit5, retains One/Harl/Peldra, Solo and independent manual mirrors; initial colony is Manual with selected Balanced suggestion, no free construction.

- [ ] **RED:** Actual current NewGame Start must send5 and load all six technologies/seven buildings/four templates. Add current-core tests for exact new IDs/costs/prerequisites/effects and all16 species pairings×3 sizes, with AI disabled governor state. Before activation tests must fail on explicit current4/absent new core definitions.
- [ ] **Implement:** Now add the exact approved three technologies/buildings and template JSON to core; make Rules.CurrentVersion5 and wire initialization/required development loading. Keep old fixture assertions explicitly pinned to their actual rules/embedded content rather than changing expected legacy economics. Frozen Species rules3 gameplay loops remain on captured content; setup tests assert current5 separately. Rival actual generated12 default loops now assert5/current content with the same expansion/contact/control/future-equality contract; additionally preserve genuine frozen4 traces. Do not weaken the accepted80-turn core Rival bound or insert live resources/outcomes to make a loop pass. New effects are already handled by ordinary data-ranked expansion_v1, not a second AI governor.
- [ ] **Pin focus edges:** Current fallback selector cases missing Harl/Peldra, singleton and empty definitions/Load remain valid; malformed default templates leave Load recovery visible, even with80 errors at800. All old schema/rules combinations load without development fields or automatic upgrade, current5 Solo remains valid, all AI species can legally use new development effects via existing policy.
- [ ] **GREEN:** Focused ColonyContentTests/ColonyCompatibilityTests then Suite/Build; Runtime Colony actual-start smoke and Rival setup/generated loops; full retained scenarios run in Task12. Record any genuine compatibility failure as a bug, never regenerate frozen expected outcomes from new code.
- [ ] **Commit:** `feat: enable colony development in new campaigns`.

### Task 12: Exercise integrated gameplay and complete five-scenario gates

**Files:** G/ColonyGraphicalValidation.cs, G/README.md; ./README.md; reports/COLONY_VALIDATION_2026-10-24.md; execution ledger/evidence; current requirement/status/roadmap documents.

**Interfaces:** The completed Colony scene writes standard results.json with method, passed/error, results and retainedQaSavePaths. Keep generated acceptance, labelled synthetic scale/gallery fixtures and genuine legacy captures clearly separate. Do not create a second test framework.

- [ ] **RED/coverage:** Before adding each acceptance group, identify its missing observable test/behavior; a retained regression may pass immediately, but never claim a witnessed RED for it. Four generated seed42 Small games must start unchanged through the actual current form, one for each human species with one Peldra rival. Use real research/construction/governor/queue controls to complete all three foundations and a specialist: Harl/Industrial Production Complex, Peldra/Research Discovery Center, Sulan/Growth Habitat Network, Wenth/Balanced Production Complex plus a literal capacity-change case. Add at least one ordinary second colony and manual favorite versus delegated colony, and save queued/paused/founding state through actual dialogs. Nonfinal Ready save/reload is a separately labelled two-human fixture: a one-human generated game resolves immediately and cannot honestly demonstrate a paused Ready=true planning state.
- [ ] **Integrate:** Human decisions use only permitted projections/shared previews; choose research through the real empire picker. No post-start injection of population, reserves, research, projects, IDs, orders or foreign outcomes; no rerolls. Bound each generated loop at160 turns, a deterministic QA guard, not a balance claim. Require all named completion/founding outcomes before success, then three future turns equal to uninterrupted canonical continuation after UI reload. If guard fails, diagnose rules/fixture/plan conflict openly; do not silently lower requirements or make the planner omniscient. Keep30-colony/cargo/rare/96-image fixtures explicitly pre-session labelled.
- [ ] **Final gates:** Run Suite, then sequential Colony, Rival, Species, Expansion, Legacy Runtime scenarios into a fresh reports/colony-final-<date>-runN tree. Every runner restores the normal build in finally. End with ordinary solution Rebuild and metadata verification excluding all five validation classes. Inspect actual results arrays, failed rows, logs, save paths and representative PNGs, including800/1920 and all gallery pages. Derive assertion/provenance/geometry counts, never copy previous totals. Preserve failed runs and all QA files.
- [ ] **Map evidence:** COL-AC-001..011 map to named tests/scenes/paths with exact source SHA and limits; COL-AC-012 is already satisfied by ADR-012, not by runtime. Keep parent REQ-017 In Progress, human/aesthetic/balance/platform/security/release limits open; only mark child functionality Verified after actual gates and final review disposition. Snapshot exact primary owner files/status/index/worktrees before any integration.
- [ ] **Commit:** `test: verify integrated colony development campaigns` after green gates. Complete the task ledger using Suite. All task results are evidence, not a substitute for the final independent review below.

## Final review, correction and local handoff

After twelve task completion lines, create the whole-branch review package from the actual execution merge base (approved-plan checkpoint, not HEAD~1). One fresh read-only reviewer on the most capable available model gets this plan, approved spec, all five Review Focus lines verbatim, ledger rulings, actual source range and exact gate evidence. No implementer agents or per-task reviews; no reviewer subagents. Request severity/file-line/effect, exhaustive declined-to-judge lines and a clear merge assessment. Re-grade by user impact. One regression-first correction pass for Important/Critical findings: witness RED, fix, GREEN/final Suite and any affected full gates; no re-review. Record minors as deferred, explicit rulings/reasons/costs for every declined item, and distinguish review from human/release acceptance.

Retain the owner's local-integration preference when finishing this approved plan: only after the concrete reviewed result, green gates and fresh exact preservation audit. No integration during planning. Re-read latest owner instructions/state first; preserve all branches/worktrees/saves/recovery and every unrelated change. If PROJECT_STATUS overlaps, make an exact backup/patch and scoped recovery stash for only that file; restore its owner's exact FR-001 paragraph into updated status, leaving it unstaged. Other owner files must remain byte-identical and unstaged. Do not apply a broad old stash. Use fast-forward when possible; never reset/discard to obtain one. If main has advanced, reconcile the combined result explicitly and review/test actual changes rather than silently overwriting them.

After local integration, fresh combined main Suite and all five graphical gates, normal rebuild and five-harness exclusions must pass. Record branch/main source and results separately in reports/COLONY_INTEGRATION_2026-10-24.md. Stage only authored documentation/evidence; if committing status, use a clean index blob excluding the owner paragraph while preserving it in the working file. Final index empty; only original owner changes remain. Do not push, publish, rewrite history, drop recovery or delete worktrees. Update PROJECT_STATUS with actual next action and an honest remaining-scope boundary.

## Self-review and owner handoff

Writing-plan self-review is inline, not an agent dispatch. Check every COL section/criterion against the tasks, exact public names/types across Interfaces, all five Review Focus test owners, frozen-before-change order, gated activation, proportion and commands/expected outcomes. Record this self-review in reports/COLONY_IMPLEMENTATION_PLAN_REVIEW_2026-10-24.md. This plan is proposed execution detail under the approved design; no task is checked or run during planning. Owner review of the plan precedes implementation, using the already selected Native method.
