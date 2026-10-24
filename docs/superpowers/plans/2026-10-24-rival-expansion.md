# Rival Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one fair, deterministic civilization that explores, develops and settles alongside the player, with observed contacts and exact old-campaign continuation.

**Architecture:** Keep AI inside the plain .NET simulation and route its intents through the existing command processor before the ordinary economic snapshot. Give the policy a sanitized own-empire view and bounded shared-calculator queries; publish planning, resolution and contact updates as one transaction. Extend explicit version readers and projections rather than replacing accepted species/expansion rules.

**Tech Stack:** C#/.NET 8, Godot 4.7.2 .NET presentation, existing JSON content, xUnit and the opt-in graphical validation runner; no new packages or services.

**Spec:** [Approved rival-expansion design](../specs/2026-10-24-rival-expansion-design.md), accepted in [ADR-010](../../DECISIONS/ADR-010-approved-rival-expansion-design.md). Read both, plus AGENTS.md and CLAUDE.md. This plan defines implementation details; the approved design supplies gameplay rules.

**Status:** Owner “continue” approved execution after plan presentation. Native execution is In Progress on isolated codex/rival-expansion; Tasks 1 through 5 are verified. Read reports/rival-execution/progress.md and Git commits before resuming. Native remains the selected execution method: implement inline, then one fresh independent whole-branch review and regression-first corrections. Do not ask for the method again.

## Global Constraints

- One human and one active rival by default; a visible Solo option remains. Harl human/Peldra rival core defaults; mirrors allowed. None passes no rival species.
- Equal species-defined starts, no AI discounts or hidden bonuses. Keep all four accepted species mechanics, roles, costs, habitats, growth and conserved cargo.
- New rules 4/writer 5; freeze all rules 1/2/3 behavior, including passive AI tags. Supported (schema,rules) pairs are exactly (1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3),(5,1),(5,2),(5,3),(5,4).
- Rules 4 Human requires null policy; rules 4 Ai requires `expansion_v1`. Rules 1/2/3 require null policy and empty contacts. Old formats reject even null/empty injected new fields.
- Policy content: `algorithm_id: expansion_v1`, `growth_support_workers: 1` (allowed 0/1), `resurvey_min_age: 3` (positive 32-bit). Embed effective policy values; no fallback to current disk content on load.
- At most `2C+V+1` planning commands per AI empire; minimum-support searches at most 31 steps for 32-bit population. No PRNG consumption, wall clock, network/model calls or unbounded trial loops in planning.
- Preserve existing ascending EmpireId/VesselId founding priority, including the human's earlier-ID advantage in exact simultaneous ties; do not rebalance silently.
- C# throughout; no Godot reference in simulation. Integer/fixed-point arithmetic, current PCG32, stable IDs, ordinal ties and invariant parsing. UI uses permitted projections and shared calculations; current containers/anchors/theme remain authoritative.
- Preserve owner roster/research edits, peaceful-mode backlog, genuine saves, all recovery material/worktrees and failed QA evidence. Human comprehension remains Deferred; do not recruit participants. No inferred merge, push, release or cleanup.

## Review Focus

These five failure classes need explicit tests in their owning tasks, beyond the normal expansion loop:

1. Renamed or reduced alternative content, one species, no Harl/Peldra, no sustainable target, or extreme population: deterministic fallback/waiting, bounded work and Load remain usable (Tasks 2, 6, 7, 9).
2. A writer/default change that disables species formulas or activates old Ai-tagged empires: genuine pre-change canonical traces and explicit feature predicates preserve old behavior (Tasks 1–4, 9).
3. Unknown/stale targets and overflowing foreign reserves: policy commands and own UI disclose no extra facts; whole-world resolution may still fail for real global overflow (Tasks 5–8).
4. Last-human readiness followed by an AI rejection/bound/ID/output failure: the entire original session, orders, readiness, contacts and RNG survive unchanged (Task 8).
5. Labor-tight Peldra daughters, tiny Harl recovery, dry Sulan targets, and Wenth births/capacity changes: ordinary forecasts drive viable legal choices with snapshot timing intact (Tasks 6–8, 10).

## Baseline, isolation and verification commands

Planning baseline: main `d90138cabbde2889447311607388f6677c30201e`; source remains checkpoint `713d305c201d51e1e7612d7b11b6913c95c5cae1`. Approval/plan documentation commits follow without product changes. Actual current product is rules 3/schema 4. Historical executed main evidence is 445 simulation tests, 1,181 Species assertions, 275 Expansion assertions and 466 Legacy assertions plus 27 geometry observations; these are not rival results.

At execution entry, inventory branches/worktrees, owner edits and index. Use the worktree skill and native managed tools first, reusing only a suitable isolated checkout. The retained species/expansion/spiral checkouts are historical evidence, not empty rival workspaces. If native worktree creation cannot access the actual F: repository, use the scoped fallback `git worktree add -b codex/rival-expansion C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/rival-expansion main` after inspecting branch/path collisions; do not delete or reset an existing checkout. Create from the latest approved plan commit, not this older planning hash. Keep the primary owner edits in place; copy only explicitly required evidence into the isolated checkout, never blindly reapply recovery stashes.

Run the ordinary restore/build/test baseline once at execution entry, retaining commands, exit codes and output in a unique `reports/rival-execution/` ledger. Restore only when needed, with normal dependency audit enabled; no routine `NuGetAudit=false`. F: writes and executable runs may need sandbox escalation. Record tool/runtime paths from actual environment; do not install a replacement SDK/engine during planning.

Full simulation gate after each simulation task:

```powershell
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo
```

For each task, first run its focused `--filter FullyQualifiedName~<test-class>` command and retain meaningful RED/GREEN evidence; then run the full gate. RED must demonstrate the missing behavior, not a deliberately fabricated failure. Fixture absence is the honest initial RED in Task 1. A declaration-only compiler failure may be initial feedback, but rerun behavioral assertions as soon as the API exists. Each GREEN means exit 0 and zero failed/skipped tests; derive counts from real output. Build after cross-layer signature/UI changes:

```powershell
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
```

After each green gate, review the diff and commit only that task's paths plus its execution ledger. Prefer a commit-message file with `git commit -F <message-file>` on this Windows host. Do not stage owner changes or unrelated generated metadata. The task steps name intended commit subjects; no task completion is inferred from writing this plan.

## File structure and responsibilities

Paths below are relative to `src/OpenAntares.Simulation/` unless prefixed with `game/`, `content/`, `tests/`, `docs/` or `reports/`. The test root abbreviated **T/** is `tests/OpenAntares.Simulation.Tests/`; the graphical root abbreviated **G/** is `tests/OpenAntares.Game.Validation/`. Expand these prefixes literally; do not create T/G directories. Within a comma-separated file list, a bare filename inherits the directory of the preceding explicitly qualified file (for example Content/RivalAiDefinition.cs, RivalContentReader.cs are both under Content/).

| Unit | Existing files to extend | New focused files |
| --- | --- | --- |
| Frozen continuation | T/CompatibilityFixtures.cs; old tests that accidentally assume the current default | T/RivalCompatibilityTests.cs, Fixtures/legacy-rules3-v4-{save,continuation,provenance}.json, Fixtures/passive-ai-rules3-v4-{save,continuation,provenance}.json |
| Rules/content/state | Rules.cs; Content/ContentSet.cs, ContentLoader.cs; State/GameState.cs, GameStateValidator.cs; all species equality gates enumerated in Task 2 | Content/RivalAiDefinition.cs, RivalContentReader.cs; State/RivalStateValidator.cs; T/RivalFixtures.cs, RivalContentTests.cs, RivalStateTests.cs |
| Persistence | Saves/SaveGame.cs, SpeciesSaveData.cs; Content/ContentWriter.cs | Saves/RivalSaveData.cs, Content/RivalContentWriter.cs, T/RivalSaveTests.cs |
| Generation | Galaxy/GalaxyGenerator.cs | Galaxy/RivalHomeGenerator.cs, T/RivalGenerationTests.cs |
| Contacts/projections | Knowledge/EmpireView.cs, EmpireViewProjector.cs; Turns/TurnResolver.cs | State/ContactState.cs, Knowledge/ContactUpdater.cs, Turns/EncounterEvent.cs, T/RivalContactTests.cs |
| Own planning queries | Existing economy, species, founding and travel helpers remain authoritative | Rivals/RivalPlanningQueries.cs, Rivals/IRivalPlanningContext.cs, T/RivalPlanningQueryTests.cs |
| Policy | Shared projections/queries only | Rivals/ExpansionRivalPlanner.cs, RivalColonyPlanner.cs, RivalVesselPlanner.cs, RivalPlanningResult.cs; T/RivalPolicyTests.cs, RivalSpeciesPolicyTests.cs |
| Atomic orchestration | Turns/TurnResolver.cs, Commands/CommandProcessor.cs (retain public APIs) | Rivals/RivalTurnPlanner.cs, Properties/AssemblyInfo.cs (test friendship only if needed), T/RivalTransactionTests.cs |
| Player setup/feedback | game/Screens/NewGameScreen.cs, GameScreen.cs; game/Components/StarPanel.cs, VesselPanel.cs, TurnReportDialog.cs, ColonyPanel.cs; game/Main.cs; content/core/ | game/Components/ContactsPanel.cs, content/core/rival_ai.json |
| Runtime acceptance | G/Run-Validation.ps1, Validation.targets, SpeciesGraphicalValidation.cs, README.md; README.md and project evidence/status | G/RivalGraphicalValidation.cs/.tscn, reports/RIVAL_VALIDATION_2026-10-24.md, reports/RIVAL_FINAL_REVIEW_2026-10-24.md |

No new framework/test project, global service locator, planner state in saves or broad source reorganization. Retain existing positional record constructors with init properties for projections.

---

### Task 1: Freeze genuine rules-3 continuation before any writer/default change

**Files:** T/CompatibilityFixtures.cs; create the six fixture/provenance files above and T/RivalCompatibilityTests.cs. Record capture commands in the execution ledger; no product code change.

**Interfaces:** Add `CompatibilityFixtures.LoadRules3() -> LoadedGame`, `LoadPassiveAiRules3() -> LoadedGame`, `Rules3Trace(bool passiveAi) -> ImmutableArray<string>`. Reuse `CanonicalWithoutSchema(ContentSet, GameState) -> string`; it removes only `schema_version`, never rules/content/RNG/state. Keep the existing rules-2 helper/fixtures intact. Core assertions for the named test:

```csharp
LoadedGame old = CompatibilityFixtures.LoadRules3();
Assert.Equal(3, old.State.RulesVersion);
Assert.Equal(2, old.State.Colonies.Count);
Assert.Equal(4, CompatibilityFixtures.Rules3Trace(false).Length);
Assert.Equal(4, CompatibilityFixtures.Rules3Trace(true).Length);
```

- [x] **RED:** Add `GenuineRules3AndPassiveAiContinueExactly()`: require schema 4/rules 3 fixtures, two colonies, four successful old-engine future states, and complete canonical equality with the retained traces. The passive fixture must include an Ai-tagged empire with an ordinary colony/scout and have null policy/empty contacts after later fields exist; its choices remain passive. Run `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --filter FullyQualifiedName~RivalCompatibilityTests`; expected initial failure: missing real fixture/trace.
- [x] **Capture:** Before product mutation, verify and copy `C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves/qa-20261024-133043-627-species-peldra-small-two-colonies.json`. Planning read verified SHA256 `32eceaa800bb1301956a27b87fc2a784dac9d227a62295b695871ca2d371fd2e`, schema 4/rules 3; source is indexed in `reports/species-integrated-2026-10-24-run1/species/results.json`. Record source bytes/hash, source/build revision, turn, content and capture command. Capture four actual successful continuations with the unchanged resolver/writer. If unavailable, recapture using the unchanged pre-rival build and label provenance; never use a later writer to manufacture a historical save.
- [x] **Capture passive case:** Author a separately labelled pre-session fixture from that copied state, retaining a human and assigning an ordinary colony/scout to a new Ai empire with the same existing species. Use valid IDs/ordinary workforce/projects, refresh observations, require old `GameStateValidator` success, and serialize/advance four times under the unchanged engine. Record every fixture edit and actual source revision. This fixture is authored; do not call it a genuine player save. Preserve pending decisions and prove old passive behavior even if ordinary income/completions occur.
- [x] **GREEN:** Run the focused command and full simulation gate; require all four canonical traces for both fixtures and retained rules-1/2 traces. Commit subject: `test: freeze pre-rival campaign continuation`.

### Task 2: Add explicit feature predicates, policy content and state invariants

**Files:** Rules.cs; Content/ContentSet.cs, ContentLoader.cs, ContentWriter.cs; create Content/RivalAiDefinition.cs, RivalContentReader.cs, RivalContentWriter.cs, State/RivalStateValidator.cs, State/ContactState.cs, T/RivalFixtures.cs, RivalContentTests.cs, RivalStateTests.cs; extend State/GameState.cs and GameStateValidator.cs. Update existing species equality gates in the files listed below. Create content/core/rival_ai.json here so normal content remains valid; keep default rules 3 until Task 9.

**Interfaces:** `Rules.RivalVersion = 4`, `HasSpecies(int version) -> bool` for 3/4, `HasRivals(int version) -> bool` for 4; `HasExpansion` for 2/3/4 and `IsSupported` for 1/2/3/4. `RivalAiDefinition(string AlgorithmId, int GrowthSupportWorkers, int ResurveyMinAge)`; nullable `ContentSet.RivalAi` init property. Append `bool requireRivals = false` to `ContentLoader.Load(IEnumerable<ContentSource>, bool requireSpecies = false, ...)`; directory content recognizes/validates any supplied rival definition. `ContactState(EmpireId OtherEmpireId, string SpeciesId, int FirstObservedTurn)`; `EmpireState.AiPolicyId` nullable string and `List<ContactState> Contacts`, cloned independently. `RivalStateValidator.Validate(ContentSet, GameState, List<string>)` joins the existing validator pipeline. `RivalFixtures.Content() -> ContentSet` loads core through existing ContentLoaderTests.CoreContentDirectory(); `AtHome(string speciesId) -> GameState` is a test-only explicit rules-4 frame: human Harl plus one Ai of the requested species, separate two-star Verdant/Shelf Sea homes, exact starts/ordinary scouts, no known unowned candidate and valid policy/surveys. Author the small fixture directly before generation exists; do not switch old SpeciesFixtures to rules 4.

```csharp
Assert.Equal(new RivalAiDefinition("expansion_v1", 1, 3), RivalFixtures.Content().RivalAi);
Assert.True(Rules.HasSpecies(3)); Assert.True(Rules.HasSpecies(4));
Assert.False(Rules.HasRivals(3)); Assert.True(Rules.HasRivals(4));
Assert.Empty(GameStateValidator.Validate(RivalFixtures.Content(), RivalFixtures.AtHome("peldra")));
```

- [x] **RED:** `RivalContentTests.PolicyValuesAreExplicitAndEmbeddedReady()` loads the three core values `(expansion_v1,1,3)` and rejects missing-required/duplicate policy, unsupported algorithm, growth workers -1/2 and age 0/negative/out-of-int-range. `RivalStateTests.CloneAndValidateMetadata()` checks independent contacts, Human/null, Ai/known-policy, old rules/null+empty, sorted unique non-self known contacts, fixed identity match and first turn in 0..current. Require a contact for each currently observed foreign owner. Use `--filter FullyQualifiedName~RivalContentTests` and then `~RivalStateTests`; expect nonzero before implementation.
- [x] **GREEN:** Implement reader diagnostics using existing EntryReader/ContentError patterns. Rules 4 requires species and policy even in Solo; old embedded content may lack both new policy/state fields. Update every behavior predicate in Rules.cs; Species/SpeciesRules.cs; Economy/EconomyProfile.cs, EmbarkationRules.cs; State/SpeciesStateValidator.cs, ExpansionStateValidator.cs; Colonization/FoundingRules.cs, ColonizationResolver.cs; Knowledge/EmpireViewProjector.cs; Saves/SpeciesSaveData.cs; Content/ContentWriter.cs; Galaxy/GalaxyGenerator.cs; game/Components/ColonyPanel.cs and VesselPanel.cs. Keep the unversioned ContentWriter.Write complete for supplied optional content, while versioned old-rule output excludes policy. Task 3 owns the full save reader/writer addition. Search `SpeciesVersion`, numeric rules comparisons and `CurrentVersion` across source/game to distinguish intentional frozen-3 references from feature gates; do not bulk-replace legacy tests or the old explicit preview until Task 9.
- [x] **Tests:** `FeaturesKeepExactOldEconomyAndEnableSameSpeciesAt4()` compares equivalent 3/4 species economy, capacities, embarkation, founding previews, roles and explanations for all four species, including extended Wenth and Sulan sealed mode. Test one-species/renamed definitions and unchanged old rules-1/2 mechanics. Run both focused classes, full simulation gate and normal build. Commit subject: `feat: define rival rules and validated policy metadata`.

### Task 3: Writer 5 and the eleven-pair compatibility boundary

**Files:** Saves/SaveGame.cs, SpeciesSaveData.cs; Content/ContentWriter.cs and RivalContentWriter.cs; create Saves/RivalSaveData.cs, T/RivalSaveTests.cs; extend T/RivalCompatibilityTests.cs.

**Interfaces:** Keep `SaveGame.Serialize(ContentSet, GameState) -> string`, `Deserialize(string) -> SaveLoadResult`, `WriteFile(string, ContentSet, GameState) -> void`, session load/save and `ContentWriter.Write(Utf8JsonWriter, ContentSet, int rulesVersion)`. Set `SaveGame.SchemaVersion = 5`. Internal `RivalSaveData.WriteEmpire(Utf8JsonWriter, EmpireState, int rulesVersion)` and `ReadEmpire(EntryReader, EmpireState, int rulesVersion) -> void` integrate into existing empire loops. `RivalContentWriter` is a partial ContentWriter helper; use HasSpecies and HasRivals independently. Retain old positional constructors and explicit reader branches.

```csharp
var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("wenth");
var loaded = SaveGame.Deserialize(SaveGame.Serialize(content, state));
Assert.True(loaded.Succeeded);
Assert.Equal(SaveGame.Serialize(content, state), SaveGame.Serialize(loaded.Game!.Content, loaded.Game.State));
Assert.Equal(5, SaveGame.SchemaVersion);
```

- [x] **RED:** `RivalSaveTests.AcceptsExactlyElevenSchemaRulesPairs()` accepts the exact matrix in Global Constraints and rejects the other schema/rules combinations, notably (4,4). `PolicyContactsAndAllSpeciesStateRoundTrip()` checks both controller kinds, all four species, cultures via embedded content, current/stale surveys, contacts, cargo, pending/transit/founding orders, readiness, reserves, RNG and NextEntityId. `OldFormatsRejectInjectedRivalFields()` injects null/empty policy/contacts and rival content in old formats and requires failure, not silent consumption. Run focused `~RivalSaveTests`; expect failure for schema 5/new fields.
- [x] **GREEN:** Require rules-4 effective policy, explicit nullable `ai_policy_id` and `contacts`; reject missing, malformed, duplicate, self/foreign/future/mismatched contacts before publication. Call ContentLoader with both correct required-feature flags. Writer 5 omits every rival field/section for old rules 1/2/3 and retains their embedded content exactly. Serialize under original rules; never upgrade on load or resave.
- [x] **Failure tests:** `FailedLoadAndAtomicWritePreserveSessionAndPriorBytes()` exercises malformed policy/contact JSON, invalid references and failed replacement; same session/content/state reference and destination bytes survive. Reuse the existing flushed temporary-file replacement and cleanup behavior. `GenuineOldCampaignsResaveAndContinueWithoutOtherDifferences()` compares Task 1 and genuine older traces after normalizing only schema_version; save/load never plans AI or advances a turn. Run focused save/compatibility commands and full simulation gate. Commit subject: `feat: persist rivals while freezing legacy campaign behavior`.

### Task 4: Deterministic one-rival generation and explicit Solo

**Files:** Galaxy/GalaxyGenerator.cs; create Galaxy/RivalHomeGenerator.cs, T/RivalGenerationTests.cs. Keep Rules.CurrentVersion at 3 and UI untouched until Task 9.

**Interfaces:** Append to `NewGameSettings(string GalaxySizeId, ulong Seed, int RulesVersion = Rules.CurrentVersion, string? SpeciesId = null, int RivalCount = 0, string? RivalSpeciesId = null)`. Preserve `GalaxyGenerator.CreateNewGame(ContentSet, NewGameSettings) -> NewGameResult`. Internal `RivalHomeGenerator.Add(ContentSet, GameState, string rivalSpeciesId) -> string?` operates on unpublished generated state; null means success, non-null actionable generation error. It does not publish or reroll. The rules-3 generation sequence remains the reference path; separate requested final rules metadata from that reference behavior.

```csharp
var result = GalaxyGenerator.CreateNewGame(RivalFixtures.Content(), new("small", 42, 4, "harl", 1, "peldra"));
Assert.True(result.Succeeded);
Assert.Equal(2, result.State!.Empires.Count); Assert.Equal(2, result.State.Colonies.Count);
Assert.Equal("expansion_v1", result.State.Empires.Single(e => e.Controller == ControllerKind.Ai).AiPolicyId);
```

- [x] **RED:** `RivalGenerationTests.AllSpeciesPairsAndSizesHaveExactStarts()` covers 16 human/rival pairs × three core sizes, seed 42, repeats and varied seeds; require different home stars, correct species workforce/population/native capacity/scout, zero reserves/projects/research/completions and sorted IDs. `SoloMatchesRules3FactsIdsAndRandom()` compares normalized galaxy facts/IDs/RNG exactly, not just repeated rules-4 output. `HomeDrawMatchesIndependentFarthestThirdReference()` calculates positive squared distances in a wide integer, descending distance/StarId, ceil(N/3), then StarId pool and exactly one bounded PCG32 NextInt including singleton pool. Run focused `~RivalGenerationTests`; expect failure for rival setup.
- [x] **GREEN:** After the existing rules-3 environment pass, choose the farthest-third rival star and lowest orbit/PlanetId, override only that planet to StartingColony.PlanetTypeId/Shelf Sea, allocate empire/colony/scout in that order after all human entities. Use shared species starts, no free technology/cargo/buildings, active expansion_v1; refresh own home surveys. Different selected species at same seed/size/count keep identical galaxy facts, home selections, IDs and RNG. Do not mark hidden rival home in the human view.
- [x] **Failure tests:** `BadCountSpeciesOrNoDistinctHomePublishesNothing()` covers counts -1/2, Solo plus species, One plus null/unknown species, absent required content, old explicit rules with rival input, and one-system or collocated-only maps. Solo one-system succeeds; failed One returns no state with actionable error and never rerolls. Run focused and full gates. Commit subject: `feat: generate deterministic rival and solo campaigns`.

### Task 5: Observed contacts, dated sightings and filtered report projections

**Files:** create Knowledge/ContactUpdater.cs, Turns/EncounterEvent.cs, T/RivalContactTests.cs; extend Knowledge/EmpireView.cs, EmpireViewProjector.cs, Galaxy/GalaxyGenerator.cs and Turns/TurnResolver.cs. State/ContactState.cs exists from Task 2.

**Interfaces:** `ContactView(EmpireId OtherEmpireId, string SpeciesId, string SpeciesName, string CultureName, int FirstObservedTurn)`; `EncounterEvent(string Kind, EmpireId Viewer, EmpireId OtherEmpireId, StarId StarId, PlanetId? PlanetId, int ObservedTurn)` with kinds `first_contact` / `observed_colony`. TurnReport and EmpireTurnReportView gain immutable EncounterEvents init properties; EmpireView gains immutable Contacts. `ContactUpdater.Refresh(GameState beforeRefresh, GameState candidate) -> ImmutableArray<EncounterEvent>` reads previous observations, requires ordinary fresh observations already present on candidate, creates contacts/events in viewer/other ID order, and never allocates IDs/draws RNG. At generation, supply an otherwise identical pre-refresh state with empty old surveys; separated starts grant no contact. At resolution, invoke after turn advancement/ObservationUpdater.Refresh and before final validation.

```csharp
var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("peldra");
var human = state.Empires.Single(e => e.Controller == ControllerKind.Human);
Assert.Empty(EmpireViewProjector.Project(content, state, human.Id).Contacts);
Assert.Empty(ContactUpdater.Refresh(state, state.Clone()));
```

- [x] **RED:** `RivalContactTests.OnlyCurrentPresenceCreatesViewerContact()` covers a stationary foreign vessel, foreign colony, both viewers independently qualifying, no en-route contact, no hidden identity/home marker, repeated encounter only once and persistence after leaving. `StaleColonyDiscoveryIsObservedNotNewlyFounded()` compares prior/fresh ownership observations and checks dated event data with no invented founding turn. Run focused `~RivalContactTests`; expect missing projections/updater behavior.
- [x] **GREEN:** Project identity only from the requesting empire's contact records and public definitions; never fetch a foreign economy or global colony count for a label. Preserve exact owned economic/completion events and filter encounter events by Viewer. Old rules get empty new projections, no contacts/events and unchanged ordinary results. Ensure the current-observation contact invariant from Task 2 is satisfied at generation and turn publication, not repaired while loading corrupt saves.
- [x] **Privacy tests:** `UnseenForeignChangesDoNotChangeOwnViewOrReport()` mutates foreign identity/resources/orders/hidden worlds, including overflowing foreign research reserve, with identical permitted facts; own rendering/projection stays identical. `KnownContactsRetainOnlyDatedFacts()` covers current/stale planet ownership, disappearing ships, duplicate/missing/future identity data rejected on load, and round-trip first-observed turn. Run focused and full gates. Commit subject: `feat: project observed rival contacts and truthful sightings`.

### Task 6: Shared, own-only planning queries

**Files:** create Rivals/IRivalPlanningContext.cs, Rivals/RivalPlanningQueries.cs, T/RivalPlanningQueryTests.cs. Reuse EconomyCalculator.CalculateColony, EconomyProfile, SpeciesRules, EmbarkationRules, FoundingRules and TravelCalculator; avoid parallel arithmetic implementations.

**Interfaces:** Public narrow `IRivalPlanningContext` exposes read-only `EmpireView View`, `ContentSet Content`, `RivalAiDefinition Policy`; `ColonyEconomy? Forecast(ColonyId colony, Workforce workforce, ProductionTarget? target, string? completedBuildingId = null)`; `FoundingPreview? PreviewSettlement(PlanetId target, string roleId)`; `int? TravelTurns(StarId origin, StarId destination, string roleId)`. Factory `RivalPlanningQueries.Create(ContentSet content, GameState working, EmpireId owner) -> IRivalPlanningContext` adapts state at the orchestration boundary; the returned object exposes no raw state, global ID allocator/RNG, other views, setup identities or callbacks to retrieve them. Build a sanitized own-only calculation frame, copying known planet observations rather than looking up hidden target truth. Use a fixed non-consumed placeholder PRNG where the existing plain frame type requires one, never the actual global RNG.

```csharp
var content = RivalFixtures.Content(); var state = RivalFixtures.AtHome("sulan");
var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai);
var context = RivalPlanningQueries.Create(content, state, ai.Id);
Assert.Null(context.View.Galaxy.Seed);
Assert.Null(context.PreviewSettlement(new PlanetId(int.MaxValue), "harbor_settler"));
```

- [x] **RED:** `RivalPlanningQueryTests.QueriesMatchAuthoritativeOwnCalculators()` compares every returned quantity/breakdown/capacity/cargo effect with existing helpers for all four species; an optional hypothetical completed building applies only in that private comparison. `UnknownAndUnseenTargetsReturnSameUnavailableResult()` uses missing and hidden IDs with null result, no differing message/timing-dependent search; stale known ownership/type is the observation. Run focused `~RivalPlanningQueryTests`; expect missing scoped query API.
- [x] **GREEN:** Sanitize `View.Galaxy.Seed` to null before policy access: the existing EmpireView contains the public human-facing seed, which must not become an AI regeneration shortcut. Keep chart coordinates and own home markers, omit any other setup identity. Forecast only the owner's relevant colony/empire/buildings/technology. Use role-defined cargo/habitat for a hypothetical all-support daughter without inventing a live ship, ID or current occupancy. Unknown/invalid/overflowing hypotheses are unavailable; input references and bytes remain unchanged. Same-system travel ranking is 0 explicitly; other travel uses shared TravelCalculator.Duration (which otherwise has a minimum of 1).
- [x] **Boundary tests:** `HiddenStateAndGlobalCountersCannotChangePlanningFacts()` changes unseen planets, other species/reserves/orders, global RNG/NextEntityId with all permitted facts fixed and requires equal queries/context. Test invalid workforce, foreign colony, unowned role, impossible cargo/capacity, checked arithmetic overflow, nondefault costs/factors/cargo and renamed definitions. Count minimum-support query evaluations in Task 7's fake context; no population enumeration. Run focused and full gates. Commit subject: `feat: provide bounded private planning forecasts`.

### Task 7: Deterministic expansion policy through ordinary commands

**Files:** create Rivals/ExpansionRivalPlanner.cs, RivalColonyPlanner.cs, RivalVesselPlanner.cs, RivalPlanningResult.cs; T/RivalPolicyTests.cs, RivalSpeciesPolicyTests.cs. Keep policy decomposition internal; the orchestrator consumes one public entry point.

**Interfaces:** `RivalPlanningResult(bool Succeeded, int CommandCount)`; `ExpansionRivalPlanner.Plan(Func<IRivalPlanningContext> current, Func<Command, bool> apply) -> RivalPlanningResult`. The policy obtains only Task 6 contexts; after each accepted ordinary command it requests refreshed own facts through current(). The apply callback's bool carries no hidden rejection text. Rejection or command bound failure returns Succeeded false; the outer transaction owns rollback. No persistent planner cache. Colony/vessel helpers return goals/intents internally rather than exposing more framework APIs.

- [x] **RED:** Add named tests with these concrete assertions (test helpers may use a fake context/apply recorder): `FirstSnapshotUsesExactSpeciesEconomy`, `GoalAndWorkforceTieOrderIsStable`, `ScoutAndFounderPipelineStayBounded`, `ShipTargetsUseOnlyObservedWorlds`, `PolicyIsInvariantUnderHiddenMutations`. Run `~RivalPolicyTests` then `~RivalSpeciesPolicyTests`; expect missing policy behavior.

| First-snapshot case | Workforce S/P/R | Support produced/required | Net P/R | Growth |
| --- | --- | --- | --- | --- |
| Peldra | 5/1/0 | 1500/1200 | 600/0 | 20 |
| Harl | 3/2/1 | 900/600 | 400/200 | 70 |
| Sulan | 3/2/1 | 900/600 | 400/200 | 40 |
| Wenth | 3/1/0 | 900/400 | 400/0 | 50 |

The table assumes core Verdant home, no candidate, reserves/technology/target empty. Assert legal Fabrication Hall and Cultivation Methods choices. All numbers are hundredths where applicable.

For `FirstSnapshotUsesExactSpeciesEconomy`, the Peldra assertions after applying the recorded policy commands are:

```csharp
var ai = state.Empires.Single(e => e.Controller == ControllerKind.Ai);
var colony = state.Colonies.Single(c => c.EmpireId == ai.Id);
Assert.Equal(new Workforce(5, 1, 0), colony.Workforce);
var economy = EconomyCalculator.Calculate(content, state).Colony(colony.Id);
Assert.Equal(600L, economy.NetProduction.Total); Assert.Equal(20L, economy.GrowthEarned.Total);
Assert.Equal("fabrication_hall", colony.ProjectId);
Assert.Equal("cultivation_methods", ai.ResearchTargetId);
```

- [x] **GREEN—colony/research:** Implement the exact section-5 ordering: retain legal research else cost/ordinal ID; keep useful legal building except support emergency; new category then cost/ID. Maintain one starting-role scout if none owned/building. Retain one viable founder project by ColonyId or choose source/role/world by travel, cost, PlanetId, ColonyId, RoleId; a live founder prevents new starts, surplus projects clear via ordinary target commands retaining banks. Compute final goals before emitting, so each colony uses at most one target and one workforce command. Reject knowingly unsupported daughter worlds; immature Wenth may bank an attainable expedition. Search minimum support monotonically in at most 31 evaluations using a wide midpoint; one optional growth worker only when it improves growth and leaves an economic worker. Split remaining ceil-half production/research with the exact single-worker rule. Support bridge `(population-1,1,0)` only for a legal support improvement and positive shared net production; otherwise all support/wait.
- [x] **GREEN—ships:** Treat every non-founding permitted vessel as a scout; production replacement uses the content StartingVesselId, never a role-name special case. Do not change transit; existing stationary founders choose compatible known worlds by travel/PlanetId, reserve distinct own targets, explicitly found when permitted, retarget observed occupancy and preserve cargo. No blind cargo exploration. Scouts choose unknown stars then old-enough noncurrent surveys by age/travel/StarId, avoiding own claimed destinations where alternatives exist. Cancel stale orders with the appropriate ordinary move/founding command; at most one such command per vessel. No SetReady/recursive Resolve. Replanning emits no-op-free legal intents with ExpectedTurn equal to input turn, at most `2C+V+1` commands; count in a wide value before comparing the int result, and never spin after rejection.
- [x] **Species/bounds tests:** `PeldraSupportBridgeUsesOrdinaryShortage()` and `HarlOneUnitRecoversInTwoSnapshots()` pin actual output/growth (Harl 50+50→2). `SulanChoosesNearHarborOrSuitableNativeWithoutIdBranches()` checks actual role costs/capacity 8/12. `WenthBanksThenEmbarksAtCurrentMaturity()` checks pop 5 birth to 6 cannot complete same snapshot; next mature S/P/R 3/2/1 with bank 2400 earns 800, spends 2400, retains 800 and yields parent 3/S0P2R1 plus seed cargo 3; next planning repairs support. Extended capacity 8 keeps daughter capacity 6. `ExtremePopulationAndEmptyCandidatesStayBounded()` uses a fake monotone forecast to assert ≤31 binary-search iterations and the command bound, including no legal expansion/building/research. Count any bounded all-support feasibility preflight separately from binary iterations; no linear population scan. Embedded age/growth tuning, renamed species/roles and mirrored setups do not change tie discipline. Run focused classes and full gate. Commit subject: `feat: plan legal deterministic rival expansion`.

### Task 8: Atomic AI planning inside simulation resolution

**Files:** create Rivals/RivalTurnPlanner.cs, T/RivalTransactionTests.cs; extend Turns/TurnResolver.cs and only the needed CommandProcessor.cs readiness boundary. If needed, add Properties/AssemblyInfo.cs with `InternalsVisibleTo("OpenAntares.Simulation.Tests")` for internal fault-injection overloads; no public testing switches or saved delegates.

**Interfaces:** `RivalPlanningOutcome(GameState State, bool Succeeded)`; `RivalTurnPlanner.Prepare(ContentSet content, GameState snapshot) -> RivalPlanningOutcome`. It validates/clones private state, processes active Ai by EmpireId and uses Task 7's Plan with a refreshed Task 6 factory and ordinary `CommandProcessor.Execute` callback. Failure returns the original snapshot. Keep `TurnResolver.Resolve(ContentSet, GameState) -> TurnResolution` and public CommandProcessor/session APIs unchanged. One internal Prepare overload may accept the same Plan delegate for rejection/bound tests; an internal Resolve overload may accept a Prepare delegate. These transient test seams live only in code, not authoritative state.

For `FailureAfterPlanningPreservesOriginalReferenceAndBytes`, after arranging a valid input with next-turn ID exhaustion or economic overflow:

```csharp
string before = SaveGame.Serialize(content, state);
var outcome = CommandProcessor.Execute(content, state, new SetReadyCommand(human.Id, state.Turn, true));
Assert.False(outcome.Accepted); Assert.Same(state, outcome.State); Assert.Null(outcome.Report);
Assert.Equal(before, SaveGame.Serialize(content, outcome.State));
Assert.False(outcome.State.FindEmpire(human.Id)!.Ready);
```

- [x] **RED:** `RivalTransactionTests.LastHumanReadyAndDirectResolvePlanExactlyOnce()` tests non-final Ready, unchanged Ready, stale/rejected commands, forecast, save/load (zero plans), final Ready/direct Resolve (one plan per active AI). Include two humans and labelled multiple AI with stable ascending IDs. `FailureAfterPlanningPreservesOriginalReferenceAndBytes()` checks pre-Ready original, all human/AI orders/readiness/contacts/RNG/NextEntityId, no report and generic public error. Run focused `~RivalTransactionTests`; expect current resolver to lack the rival phase.
- [x] **GREEN:** Insert Prepare after input validation and before economic calculation. Its input validation must use `allowAllHumansReady: true`, matching the existing resolver's final-Ready boundary. Treat its final private state as the snapshot for economy, movement and founding; clone that snapshot for outcome application. Do not apply growth from original unplanned state. Preserve one completion per colony/empire, new-ship wait, current-capacity snapshot maturity, explicit founding and existing tie order. Advance/reset/observe/contact/update/validate once; publish only total success. Earlier AI hidden plans never refresh observations or enter another AI's permitted facts. Rules 1/2/3 retain the exact original path and passive Ai behavior.
- [x] **Fault/privacy tests:** Force an actually rejected submitted command and a command-bound failure through internal seams; test ID exhaustion, economic overflow after accepted private plans, invalid final state, and last-human rollback through the real SetReady/GameSession path. Distinguish expected hypothetical unavailable choices (skip) from unexpected real rejection (abort). Use fixed generic AI-planning failure text with no hidden IDs/values. Test own-view/command invariance under foreign-overflow states separately from legitimate whole-world resolution failure. Keep initial validation/load diagnostics distinct.
- [x] **Travel/race tests:** `CompletedVesselWaitsUntilNextPlanning`, `GrowthAndExtensionCannotUnlockSameSnapshotEmbark`, `SamePlanetRaceKeepsIdPriorityAndLoserCargo`, `StaleOccupiedTargetRetargetsAfterObservation` pin existing timing and lower-ID winner, distinct same-system planets allowed and no foreign-order peeking/cancellation. Run focused, Task 7 policy classes, full simulation and normal build; genuine old canonical traces must remain exact. Commit subject: `feat: resolve rival planning and turns atomically`.

### Task 9: New-game default, observed-rival UI and frozen Species regression

**Files:** Rules.cs (CurrentVersion activation only); game/Screens/NewGameScreen.cs, GameScreen.cs; game/Main.cs; game/Components/StarPanel.cs, VesselPanel.cs, TurnReportDialog.cs, ColonyPanel.cs; create game/Components/ContactsPanel.cs. G/SpeciesGraphicalValidation.cs, create G/RivalGraphicalValidation.cs/.tscn, extend G/Run-Validation.ps1 and Validation.targets. Update existing tests that intentionally assert current defaults; keep frozen rules explicit.

**Interfaces:** Extend `NewGameScreen.Configure(string sizeId, ulong seed, string? speciesId = null, int? rivalCount = null, string? rivalSpeciesId = null)` without changing old callers; omitted values preserve visible current selection. Name controls `RivalCountPicker`, `RivalSpeciesPicker`, keep SpeciesPicker/SpeciesSummary. Add `ContactsPanel.Show(EmpireView view) -> void`. Existing GameScreen refresh/report flow consumes contact/encounter init properties from Task 5. Runner adds `-Scenario Rival` while Legacy remains its invocation default. Set `Rules.CurrentVersion = RivalVersion` only now; UI explicitly passes visible 1 or 0, never programmatic default 0 accidentally.

The actual `StartRequested` signal in `RivalGraphicalValidation` captures a `NewGameSettings? selected` local; after pressing visible Start, use the existing harness `Check(bool,string)` assertion pattern:

```csharp
Check(selected is { RulesVersion: 4, RivalCount: 1, SpeciesId: "harl", RivalSpeciesId: "peldra" }, "Visible defaults reach simulation unchanged");
```

- [x] **RED:** Author executable graphical checks for actual visible One/Harl/Peldra default and Start settings `(rules4,rival1,harl,peldra)`, Solo/null rival, mirrors, independent selectors, missing Harl/Peldra fallback, single species and invalid/missing definitions while Load remains available. Check actionable generation error with no replaced session. Run the new Rival scenario to a fresh `reports/rival-execution/task9-red-*` directory; retain the honest missing-feature failure. Ordinary compiler feedback alone is insufficient UI RED evidence once the scene compiles.
- [x] **GREEN:** Implement One/None count selection and enabled rival selector with content-driven names; no auto-swap on later human selection. Give a short expansion-race explanation with current lack of warfare/negotiation. Seed tooltip states that equal seed, size and rival count reproduce galaxy facts across species, avoiding a false same-map claim across Solo/One. Update opening preview to the selected rules/count/species and compute only the human's own forecast/projection, never a global foreign forecast; do not leak rival home/choice beyond the explicit setup selector. Display known culture/species from ContactView, dated observed ownership/stale facts, stationary seen ships and viewer-only encounter wording. No foreign totals/reserves/routes/cargo/private policy/status. Preserve old loaded-game UI; reread theme if a minimal variation change is needed, otherwise inherit existing styles.
- [x] **Retained Species behavior:** Freeze Species scenario gameplay loops to explicit rules-3 campaigns saved/loaded through ordinary dialogs, as Legacy/Expansion already freeze older campaigns. Keep all 12 old species/size loops, forced harbor, alternatives/maturity/privacy/focus assertions. Transfer current-default expectations to Rival; make retained setup checks assert the actual new settings where they still instantiate the current NewGameScreen. Do not relabel a newly generated rules-4 campaign as the old rules-3 fixture. The old baseline fixture/continuation tests remain genuine.
- [x] **Verify:** Full simulation gate and normal solution build. Run focused Rival setup and full Species scenarios with new directories; inspect actual control/layout/screenshots at 800x600, 1152x648, 1280x720, 1600x900 and 1920x1080. Require visible controls/focus/scroll and filtered text under hidden mutations; do not claim physical input or comfortable reading from signals/screenshots. Confirm runner's finally block restores the ordinary assembly with all four validation types excluded. Commit subject: `feat: expose rival setup and observed competition`.

### Task 10: Generated rival acceptance, full regressions and independent review

**Files:** G/RivalGraphicalValidation.cs and G/README.md; T/Rival*Tests only for meaningful acceptance gaps/corrections; README.md; reports/RIVAL_VALIDATION_2026-10-24.md, reports/RIVAL_FINAL_REVIEW_2026-10-24.md, reports/VALIDATION_REPORT.md, reports/rival-execution/progress.md, PROJECT_STATUS.md and relevant requirement/plan/decision indices. Keep QA saves/results in unique retained directories.

**Interfaces:** Retain the existing `results.json` top-level passed/error/results/retainedQaSavePaths contract; add scenario-specific loop/turn/provenance and operation-count evidence without removing old fields. Rival runtime scene drives actual New Game, controls/signals, End Turn, map choices, shared commands, save/load dialogs and canonical continuation. Label pre-session synthetic fixtures; never modify live authoritative outcomes to reach acceptance.

Within each generated loop, retain `int resolvedTurns`, the live `GameSession session`, `EmpireId rivalId`, `EmpireId humanId`; pin completion using the same harness assertion helper:

```csharp
Check(resolvedTurns <= 80, "Generated loop stays within core turn bound");
Check(session.State.Colonies.Count(c => c.EmpireId == rivalId) >= 2, "Rival founded a real second colony");
Check(session.View(humanId).Contacts.Any(c => c.OtherEmpireId == rivalId), "Human observed the rival");
```

- [ ] **RED:** Add `TwelveGeneratedRivalsExpandAndAreEncounteredWithin80Turns`: core seed 42, human Harl, rival four species × Small/Medium/Large including mirrors. Each loop requires actual rival second colony plus observed encounter within 80 resolved turns, actual player scouting/settlement/development choices, observed contact/report, pending/transit/founding saves and resumed future equality. Empty-candidate worlds wait legally; no injected production/population/tech/orders, map rerolls or seed changes. Initial failure means the specific invariant/loop that failed; if all behavior already passes, record that acceptance was added green rather than inventing RED. Separate labelled fixtures cover both Sulan modes, extended Wenth, forced simultaneous race, no-candidate and rollback/contact corruption.
- [ ] **Run/repair:** Execute the Rival scenario in a fresh retained directory, inspect failed evidence, use regression-first corrections within approved scope and rerun affected checks. The 80-turn bound is this deterministic core fixture, not a universal balance guarantee. If correct approved policy cannot meet it, report the concrete mismatch for an approved design amendment; do not cheat, relax the assertion or rewrite acceptance silently. Add all-human-species setup/continuation and Solo checks, plus renamed/nondefault embedded policy continuation.
- [ ] **Full gate:** Run the full simulation suite, normal build and the four runtime scenarios sequentially. Use the actual Godot .NET console path recorded at execution entry:

```powershell
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot '<verified Godot .NET console path>' -Scenario Legacy -OutputDirectory reports/rival-final-<unique-run>/legacy
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot '<verified Godot .NET console path>' -Scenario Expansion -OutputDirectory reports/rival-final-<unique-run>/expansion
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot '<verified Godot .NET console path>' -Scenario Species -OutputDirectory reports/rival-final-<unique-run>/species
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot '<verified Godot .NET console path>' -Scenario Rival -OutputDirectory reports/rival-final-<unique-run>/rival
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
```

Expected: every process exit 0, each results.json passed true, zero failed runtime assertions and no unresolved final engine diagnostics. Record actual counts and screenshots, saves, commands, runtime/environment and limitations. Verify ordinary assembly metadata excludes GraphicalValidation, ExpansionGraphicalValidation, SpeciesGraphicalValidation and RivalGraphicalValidation. If an unrelated external diagnostic remains, record it explicitly; do not claim a clean gate.

- [ ] **Independent review:** After inline implementation and green gates, use the requesting-code-review skill to dispatch exactly one fresh reviewer for the complete branch. Supply approved design/plan, base/head, diff/evidence and review focus: information boundary, atomicity, legacy traces, all four species and real runtime behavior. No task-by-task agent implementation or repeated speculative review rounds. Verify each finding against code; add a failing regression before a correction, rerun affected/full gates, and retain report/correction evidence. A material scope change returns to the owner; small implementation fixes stay within the approved contract.
- [ ] **Handoff/commit:** Self-check all RIV acceptance rows against evidence, update executed checkboxes/actual source hash and counts, requirement status and PROJECT_STATUS without closing human/balance/platform/release gates. Mark RIV-AC-012 satisfied by ADR-010; design approval is not a gameplay test. Commit subject: `test: verify rival expansion and retain regression evidence`. Report branch readiness, preservation and remaining limits. Do not merge/push/delete a worktree or reapply owner edits; integration is a separate owner action.

## Coverage and self-review

| Design contract | Task/evidence owner |
| --- | --- |
| RIV-001 / AC-001..002 setup/generation | 2, 4, 9; exact draw/order, 16×3 setups, real visible defaults/fallback/errors |
| RIV-002 policy content/permitted facts | 2, 3, 6; embedded fields, scoped forecast/daughter queries, scrubbed seed |
| RIV-003 / AC-006..007 transaction/travel/race | 7, 8; budget, failure reference/bytes, direct/final-Ready, snapshot and priority |
| RIV-004 / AC-003..005 policy/fair species/information | 6–8, 10; exact four traces, all habitats/maturity, renamed values, hidden mutations |
| RIV-005 / AC-008 contacts/UI | 2, 3, 5, 9, 10; validated identity retention, dated observed facts, filtered UI/reports |
| RIV-006 / AC-009 compatibility | 1–4, 8–10; genuine captures, eleven pairs, exact old continuation, failed load/write |
| RIV-007 / AC-010..011 integrated/retained gates | 9, 10; twelve generated loops, four runtimes, real save/load, normal rebuild |
| RIV-AC-012 full design approval | ADR-010; satisfied before planning, not counted as executed gameplay |

Self-review completed against the approved spec: every RIV requirement/acceptance row has an owner above; task interfaces match their consumers; fixture capture precedes any writer/default changes; all five Review Focus cases have named tests. The plan keeps rule logic in code and tunable fields in JSON, carries exact species traces/version pairs, and avoids prescribing ordinary function bodies. New-game activation is last among functional tasks. No implementation, tests/builds, branch/worktree creation or independent code review has run during this planning task.

## Execution handoff

Review this plan before implementation. Native is retained and is not a new choice. Once the owner confirms this plan captures the desired work, begin with isolation/baseline and Task 1, implement inline in order, and obtain the final independent whole-branch review in Task 10. Approval of this plan does not authorize local main integration, remote publication, release or cleanup.

## Execution checkpoint

Owner continue approved this plan for Native execution. Tasks1–9 are complete and committed; Task10 final regression/review/handoff is in progress. Read tracked execution ledger and PROJECT_STATUS for actual source/gates. Planning-stage descriptions above remain historical.
