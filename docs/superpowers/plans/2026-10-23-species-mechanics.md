# Species Mechanics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (Native, the existing preference and recommendation here) or superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Owner approved this plan on 2026-10-23; execute inline using the retained Native preference.

**Goal:** Deliver four playable classic space-opera species with distinct economy/founding choices, explicit habitats and unchanged continuation of existing campaigns.

**Architecture:** Extend the existing plain .NET content/state/command/economy/projection pipeline and C# Godot UI. This is one coupled playable slice: data, knowledge, economy, cargo and persistence must agree before rules 3 becomes the default. Ten testable tasks retain the old rules and defer the default switch until the UI is ready.

**Tech Stack:** Existing C#, net8.0, Godot.NET.Sdk 4.7.2 / GL Compatibility, JSON and xUnit. No new dependencies, product scenes or asset pipeline.

**Spec:** [Owner-approved species design](../specs/2026-10-23-species-mechanics-design.md), approved 2026-10-23 in [ADR-008](../../DECISIONS/ADR-008-approved-species-mechanics.md), including the classic Master of Orion II direction. The charter's original setting/licenses and respect for real peoples/cultures/religions remain in force.

**Status:** Owner-approved for Native execution, 2026-10-23. Checkboxes track actual execution; see the plan-specific ledger. Source inventory anchor: main at `5958c25177e064d4aafb9a102fe6bc390dc59bd3` before this planning commit; only the two owner prose files were dirty.

## Global Constraints

- Four core IDs: peldra, harl, sulan, wenth. Colorful humanoid animals/monsters and fanciful biology are welcome. No compulsory cohort model, scientific explanation, mortality system or representational checklist.
- Preserve simulation/Godot separation, immutable permitted views, empire-issued commands, integer/fixed-point rules, existing PCG32, stable IDs, ordinal iteration, invariant parsing, snapshot phases and atomic publication.
- Do not change old embedded rules/content or the existing global starting fixture. New games eventually use rules 3, visible Harl default; old rules-1/2 campaigns remain unchanged.
- Peldra: population 6, 4/1/1, native capacity 10, support/unit 200, worker production/research 30000 bp, growth base/cap/divisor 5/15/20.
- Harl: population 6, 2/2/2, capacity 10, support 100, factors 10000 bp, growth 10/80/5.
- Sulan: population 6, 2/2/2, native/sealed capacities 12/8, support 100, factors 10000 bp, growth 10/40/10. Habitat Extension adds 2 to either.
- Wenth: population 4, 2/1/1, capacity 6, support 100, factors 20000 bp, growth 10/40/10. Extension raises capacity/maturity to 8.
- Role cost/cargo/minimum/full/mode: peldra_settler 3200/2/4/false/native; harl_settler 1600/1/3/false/native; settler 2400/2/4/false/native; harbor_settler 3600/2/4/false/sealed; daughter_seed 2400/3/6/true/native. All speed 200; scout remains 800/speed 300/cargo 0.
- Support worker rate is unmultiplied. Floor the aggregate worker term * bp / 10000, add flat buildings, then floor shortage scaling. Growth units are hundredths; newborns enter support; no same-resolution eligibility from births/completions.
- Environments: dry/open_ocean/shelf_sea, ordinal weighted sampling after existing generation. Weights by that order: Verdant 2/2/4, Forge 6/1/1, Crystal 4/3/1, Barren 8/0/0. Sample home then override to shelf_sea for all species. Equal seed/size has identical galaxy facts and RNG across species.
- Writer schema 4; only pairs (1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3). No implicit species/environment injection or old-campaign upgrade.
- Preserve the two uncommitted owner files, recovery stash/snapshot, all QA saves/evidence and registered worktrees. Do not stage docs/SPECIES_ROSTER.md or docs/research/faction-survey.md. Approved mechanical amendments are indexed through ADR/spec follow-ups without overwriting those files.
- No AI/combat/diplomacy, mixed populations, transport unloading, terraforming, archives, custom cultures, portraits, dependency upgrades, remote push or inferred cleanup. Human comprehension/release acceptance remain separate.

## Review Focus

1. Valid nondefault content: fractional worker factors, renamed/missing Harl default and edited role cargo/capacity must render their actual rules without overflow or hard-coded core values (Tasks 2/3/7/8).
2. Generation sequencing: zero-weight environments, home override and identical seed across species must preserve base galaxy facts and consume habitat RNG in exactly one ordered pass (Task 5).
3. Loaded but blocked projects: immature seeds/banked production are valid, while illegal native/sealed colonies or founding intents reject without losing cargo/session; capacity extension must change maturity (Tasks 2/4/6/7).
4. Rules-version transition: changing CurrentVersion/SchemaVersion must neither drop schema 3 reading nor disable rules-3 vessels/observations/pickers; old graphical scenarios must use frozen old campaigns (Tasks 4/7/8/9).
5. Hidden information and failure atomicity: changed unseen habitats/foreign overflow, failed save/load, ID overflow and competing founding intents must not leak or partially publish (Tasks 4/5/6/7/9).

## Execution setup and commands

At execution time inspect Git/status/registered worktrees, preserve current owner hashes and use the worktree skill. Reuse an appropriate isolated checkout or create one from the then-current reviewed main; do not reset the dirty primary or blindly apply its recovery stash. Keep a plan-specific execution ledger and record actual commands/exits/counts, not anticipated counts. Record all routine implementation rulings.

Run the README build/tests before source changes. Restore only if needed, with normal audit enabled. Current historical baseline: 281 simulation tests, 275 expansion and 466 legacy assertions plus 27 geometry observations; not rerun by writing this plan.

Full simulation gate after each simulation task:

```powershell
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo
```

Expected: exit 0, zero failures/skips; derive new count from actual output. Run normal solution build after UI/signature changes. Preserve each focused RED/GREEN log and commit only the task's reviewed paths. Each verification command below has an explicit expected result; each commit follows its green gate.

## File structure and responsibility

| Area | Existing files to extend | New focused files |
| --- | --- | --- |
| Content/profile definitions | src/OpenAntares.Simulation/Content/ContentSet.cs, Content/ContentLoader.cs, Content/ContentWriter.cs | Content/SpeciesDefinition.cs (also EnvironmentDefinition) |
| State and rule validation | State/GameState.cs, State/KnowledgeState.cs, State/GameStateValidator.cs, State/ExpansionStateValidator.cs, Rules.cs | Species/SpeciesRules.cs, State/SpeciesStateValidator.cs |
| Economy and embark | Economy/EconomyCalculator.cs, Economy/VesselProduction.cs, Economy/EconomyResults.cs, Economy/Breakdown.cs | Economy/EconomyProfile.cs, Economy/EmbarkationRules.cs |
| Generation/knowledge | Galaxy/GalaxyGenerator.cs, Knowledge/ObservationUpdater.cs, Knowledge/EmpireView.cs, Knowledge/EmpireViewProjector.cs | Galaxy/EnvironmentGenerator.cs |
| Founding and commands | Commands/ExpansionCommands.cs, Colonization/ColonizationResolver.cs, Turns/TurnResolver.cs | Colonization/FoundingRules.cs |
| Save compatibility | Saves/SaveGame.cs, Saves/ExpansionSaveData.cs | Saves/SpeciesSaveData.cs |
| UI/core activation | game/Screens/NewGameScreen.cs, game/Screens/GameScreen.cs, game/Components/ColonyPanel.cs, ColonyList.cs, VesselPanel.cs, StarPanel.cs; game/BreakdownText.cs; content/core/expansion.json, planet_types.json | content/core/species.json, content/core/environments.json |
| Verification | Existing xUnit suites and opt-in graphical runner/targets | Named Species*Tests below, SpeciesFixtures.cs, CompatibilityFixtures.cs, genuine old fixture/continuation/provenance, SpeciesGraphicalValidation.cs/.tscn |

Unprefixed paths in the simulation rows are relative to src/OpenAntares.Simulation/. No new test project/framework. Retain old positional record constructors where possible through nullable init properties.

---

### Task 1: Freeze genuine rules-2 save and original contracts

**Files:** Create tests/OpenAntares.Simulation.Tests/CompatibilityFixtures.cs, SpeciesCompatibilityTests.cs, Fixtures/legacy-rules2-v3-save.json, Fixtures/legacy-rules2-v3-continuation.json, Fixtures/legacy-rules2-v3-provenance.json. Modify existing generation/current-default tests only to make old-rule intent explicit when needed later; do not change their expected arithmetic.

**Interfaces:** CompatibilityFixtures.LoadRules2() returns LoadedGame with a fresh parsed state/content; Rules2Content() returns its immutable ContentSet. CanonicalWithoutSchema(ContentSet,GameState) returns canonical save JSON with only schema_version removed. ExpectedTrace() returns ImmutableArray<string> of four pre-species canonical continuations. These live only in tests; no product fixture access.

- [x] **RED:** Add a test that calls LoadRules2(), asserts RulesVersion 2, two colonies, no species/environment JSON keys, and compares four TurnResolver.Resolve results with ExpectedTrace(). Running before capture must fail on missing fixture/trace, not an invented old writer.
- [x] **Run RED:** `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --filter FullyQualifiedName~SpeciesCompatibilityTests`. Expected: nonzero, missing fixture/trace.
- [x] **Capture GREEN:** Before any writer/core mutation, copy the retained save ending qa-20261023-143542-124-small-two-colonies.json from the paths in reports/expansion-integrated-2026-10-23-run1/results.json. Planning-time read confirmed schema 3/rules 2/turn 9/two colonies/one vessel; SHA-256 bbab27e800c8aea17eda305a0502d8fefba6a5504afb903eef289f74277cd966. Verify the source before copying; retain it. Record hash/source/build and compute four successful continuations with the still-current resolver/writer. Canonical comparison excludes only schema_version, not content, RNG or rules_version. If that source is unavailable, explicitly capture with the unchanged pre-species build and record its provenance; never use a later writer to pretend an old fixture exists.
- [x] **Verify:** Repeat the focused command and full simulation gate. Expected: all green; genuine schema-1/2 fixtures retained and new rules-2 trace matches exactly.
- [x] **Commit:** Review diff; `git add` only the new fixture/helper/test paths, then `git commit -m "test: freeze pre-species campaign compatibility"`.

### Task 2: Species content, state and complete structural invariants

**Files:** Create Content/SpeciesDefinition.cs, Species/SpeciesRules.cs, State/SpeciesStateValidator.cs, tests/OpenAntares.Simulation.Tests/SpeciesFixtures.cs, SpeciesContentTests.cs, SpeciesStateTests.cs. Modify Content/ContentSet.cs, Content/ContentLoader.cs, Rules.cs, State/GameState.cs, State/KnowledgeState.cs, State/GameStateValidator.cs, State/ExpansionStateValidator.cs, Knowledge/ObservationUpdater.cs.

**Interfaces:** SpeciesDefinition is an immutable record with every field in SPC-006: strings Id/Name/Description/CultureName/PopulationUnitLabel; int StartingPopulation/BaseCapacity, int? SealedBaseCapacity; Workforce StartingWorkforce; long SupportPerPopulation/SupportPerGrowthPoint; int ProductionBasisPoints/ResearchBasisPoints/BaseGrowth/MaxSurplusGrowth; ImmutableArray<string> NaturalEnvironmentIds/AllowedVesselIds; bool AllowsSealedHabitats. EnvironmentDefinition(string Id,string Name,string Description). ContentSet gains ordinal immutable Species/Environments maps; PlanetTypeDefinition gains ordinal EnvironmentGenerationWeights; VesselDefinition gains bool RequiresFullColony/FoundsSealedHabitat init properties. Modify the existing definition records in Content/ContentSet.cs, where they live; do not invent duplicate files/types.

Add nullable EmpireState.SpeciesId, PlanetState.EnvironmentId, ColonyState.HabitatMode and PlanetObservation.EnvironmentId init property (retain its existing constructor). HabitatModes.Native/Sealed are the strings native/sealed. SpeciesRules.BaseCapacity(ContentSet,GameState,EmpireId,string? habitatMode) returns int; CanBuildVessel(ContentSet,GameState,EmpireId,string roleId) and HabitatAllowed(ContentSet,GameState,EmpireId,string environmentId,string habitatMode) return bool. Rules.SpeciesVersion=3, IsSupported includes 3, HasExpansion(int) is true only for 2/3; keep CurrentVersion=2 until Task 8. Add Rules.ColonyCapacity(ContentSet,GameState,ColonyState), retaining the existing two-argument legacy helper. SpeciesStateValidator.Validate(ContentSet,GameState,List<string>) validates safely alongside existing validators.

- [ ] **RED:** SpeciesContentTests asserts all approved fields/IDs, nullable sealed capacity, duplicate/unknown references, invalid integer ranges/weight sums and an Int128-derived output bound that cannot fit long rejects with file/entry/field. SpeciesStateTests asserts clones preserve metadata; missing species/environment, illegal colony mode, disallowed vessel/target or false static/current observation reject. A Wenth population-5 banked Daughter Seed target is valid. Sulan native/sealed capacity is 12/8, extension 14/10; Wenth 6/8. Legacy metadata stays null and old validity unchanged.
- [ ] **Run RED:** focused filter `FullyQualifiedName~SpeciesContentTests|FullyQualifiedName~SpeciesStateTests` through the standard test command (quote the filter). Expected: nonzero from missing contracts/validation.
- [ ] **GREEN:** Read new optional content sections; add `ContentLoader.Load(IEnumerable<ContentSource>, bool requireSpecies = false)` while keeping existing callers valid. When the extension is present or required, validate all fields/references/reachable capacity/output bounds, including cargo fitting its role's founded-mode capacity. Extend legacy-free structural validation, new metadata cloning and physical observation copying. Use HasExpansion in base expansion validation/observation refresh; rules-3 pending founding mode/cargo/known target must already be structurally valid. Legacy fields/sections are not fabricated. SpeciesFixtures.Content()/AtHome(string) creates valid explicit rules-3 test fixtures; SpeciesFixtures.Snapshot(GameState) uses deterministic test-only state JSON until schema 4 exists, not an unsupported schema-3/rules-3 save. Keep production core JSON and UI defaults unchanged.
- [ ] **Verify:** focused command and full simulation gate, then `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers`. Expected: exit 0; no warnings/errors; legacy tests unchanged.
- [ ] **Commit:** explicit paths above; `git commit -m "feat: add species content and state invariants"`.

### Task 3: Shared species economy and inspectable rounding

**Files:** Create Economy/EconomyProfile.cs, tests/OpenAntares.Simulation.Tests/SpeciesEconomyTests.cs. Modify Economy/EconomyCalculator.cs, Economy/VesselProduction.cs, Economy/EconomyResults.cs, Economy/Breakdown.cs.

**Interfaces:** EconomyProfile.For(ContentSet,GameState,EmpireId) returns an immutable EconomyProfile with long SupportPerPopulation/SupportPerGrowthPoint; int ProductionBasisPoints/ResearchBasisPoints/BaseGrowth/MaxSurplusGrowth; string? SpeciesId; string PopulationUnitLabel. Resolve legacy fields without species when rules 1/2. EconomyCalculator.Calculate(ContentSet,GameState) retains its public signature; the new capacity overload from Task 2 resolves habitat. Add BreakdownKinds.Species; species adjustment lines use species ID and DivisionDetails, with base worker contribution plus adjustment summing to the aggregate multiplied worker total. Existing Quantity/ColonyEconomy/TurnEconomy remain the shared forecast/resolution outputs.

- [ ] **RED:** SpeciesEconomyTests asserts the four opening rows exactly: support produced/required, production, research, growth = Peldra (1200/1200,600,600,5), Harl/Sulan (600/600,400,400,10), Wenth (600/400,400,400,30). Assert Peldra 2/2/2 gross 1200, net 600, growth 0; Cultivation Hub growth 15; Fabrication Hall 700. Fractional factor fixture asserts 454 worker + 100 building = 554 gross, 369 after 2/3 support; breakdown sum and both division remainders explain totals. Multi-turn no-command traces assert Harl 4/1/1 capacity at T5/discard50, Harl 2/2/2 first birth T10/cap T15/discard70, ordinary rules-2 4/1/1 cap T8/discard0, Sulan 4/1/1 cap T12/discard0, Wenth default cap T6/workforce4/1/1/discard20. Assert shortages retain progress and aggregate Int128 bounds fail atomically.
- [ ] **Run RED:** standard test command with `--filter FullyQualifiedName~SpeciesEconomyTests`. Expected: nonzero with legacy-profile arithmetic differences.
- [ ] **GREEN:** Resolve profile once per colony from owner; multiply aggregate worker terms using checked Int128, floor, add flat effects, then floor shortage. Keep support workers ordinary. Use profile growth limits/divisor, habitat capacity and existing newborn/support/remainder phases. Update existing post-embark support calculation to the same profile now; eligibility changes follow in Task 6. No species-ID branches in the calculator, no RNG or duplicate UI arithmetic.
- [ ] **Verify:** focused command and full simulation gate. Expected: all green, Task 1 four-turn old trace unchanged, forecasts equal resolution and line totals.
- [ ] **Commit:** explicit task paths; `git commit -m "feat: calculate species economies with shared breakdowns"`.

### Task 4: Schema 4 and strict old-campaign continuation

**Files:** Create Saves/SpeciesSaveData.cs, tests/OpenAntares.Simulation.Tests/SpeciesSaveTests.cs. Modify Saves/SaveGame.cs, Saves/ExpansionSaveData.cs, Content/ContentWriter.cs and SpeciesCompatibilityTests.cs.

**Interfaces:** SaveGame public Serialize/Deserialize/WriteAtomic APIs stay unchanged; SchemaVersion becomes 4. Retain ContentWriter.Write(Utf8JsonWriter,ContentSet) for general complete content; add Write(Utf8JsonWriter,ContentSet,int rulesVersion) for version-specific save content. SpeciesSaveData owns explicit species/environment/habitat read/write helpers called inside existing state loops; keep their scope internal. Deserialize calls ContentLoader.Load(sources, requireSpecies: rulesVersion == Rules.SpeciesVersion). Use explicit HasExpansion rather than equality to 2 for required vessel/survey fields. A saved campaign's embedded content governs its continuation.

- [ ] **RED:** SpeciesSaveTests theory accepts exactly (schema,rules) (1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3), rejects other combinations including (3,3), and rejects missing/unknown species, habitat, physical environment or malformed current observations. Roundtrip four species, native/sealed Sulan, banked immature Wenth target, cargo/transit/founding intent, RNG, ID allocator, readiness and fixed identity. Reject new state metadata in legacy rules instead of assigning biology. Genuine old fixture tests compare complete embedded content/state and four continuations after removing only schema_version. Failed load and failed atomic replacement preserve published session/prior destination bytes.
- [ ] **Run RED:** standard test command with quoted `--filter "FullyQualifiedName~SpeciesSaveTests|FullyQualifiedName~SpeciesCompatibilityTests"`. Expected: nonzero for schema 4/new metadata; genuine old trace still green before mutation.
- [ ] **GREEN:** Explicitly retain schema 3 in the supported-pair check when changing the constant; do not use a check that accidentally accepts only 1/2/current. Schema 4 writes rules-1/2 embedded definitions unchanged without injecting species/environment sections or metadata; rules 3 writes all new definitions, weights, vessel fields and state identifiers. Keep parsing/validation before publication, ordinary flushed atomic save replacement and legacy positions/regions/RNG unchanged. No migration or upgrade-copy command.
- [ ] **Verify:** focused command and full simulation gate. Expected: zero failures; old continuation canonical trace identical, new uninterrupted versus save/reload continuation identical.
- [ ] **Commit:** explicit task paths; `git commit -m "feat: persist species campaigns without upgrading legacy saves"`.

### Task 5: Deterministic environments, species setup and permitted previews

**Files:** Create Galaxy/EnvironmentGenerator.cs, tests/OpenAntares.Simulation.Tests/SpeciesGenerationTests.cs, SpeciesKnowledgeTests.cs. Modify Galaxy/GalaxyGenerator.cs, Knowledge/ObservationUpdater.cs, Knowledge/EmpireView.cs, Knowledge/EmpireViewProjector.cs.

**Interfaces:** Append string? SpeciesId = null to NewGameSettings(string GalaxySizeId,ulong Seed,int RulesVersion = Rules.CurrentVersion,...), retaining old call sites. Explicit rules-3 settings must specify a valid ID; UI supplies it in Task 8. EnvironmentGenerator.Apply(ContentSet,GameState,PlanetId home) assigns environments. KnownPlanetView gains nullable EnvironmentId and PlanetPotentialView? Potential init properties. PlanetPotentialView contains Quantity SupportPerWorker/ProductionPerWorker/ResearchPerWorker, int NativeCapacity, int? SealedCapacity, bool NativeHabitatAllowed/SealedHabitatAllowed. EmpireView adds own SpeciesId/PopulationUnitLabel and ImmutableArray<string> AvailableVesselRoleIds init properties; OwnedColonyView adds these own-species fields and role IDs. Keep existing positional constructors and base economic planet-rate fields. Projector computes pure previews using only permitted observations and own profile; no UI multiplication.

- [ ] **RED:** SpeciesGenerationTests covers four species x Small/Medium/Large, seed 42, plus repeated seeds: correct population/workforce, native home Shelf Sea, one scout, zero reserves/progress/buildings. Compare same seed/size across species for identical geometry, IDs, types, environments and final RNG. An independent one-pass reference clones post-base-generation PCG and asserts exact ordinal sampling/home draw consumed before override, zero-weight categories never drawn and home override occurs for all species. Explicit rules 1/2 still match their fixtures; missing species rejects rules 3.
- [ ] **Run RED:** standard test command with quoted filter `FullyQualifiedName~SpeciesGenerationTests|FullyQualifiedName~SpeciesKnowledgeTests`. SpeciesKnowledgeTests mutates unseen environments/foreign species and overflowing foreign economy: own view, forecast, errors and available controls stay identical. Unknown planets expose no potential or suitability summary; arrival/current/stale observation tests verify permitted environment lifetimes. Expected: nonzero for missing generation/previews.
- [ ] **GREEN:** Finish existing generation including home selection, sample each ascending PlanetId with one existing Pcg32.NextInt(totalWeight) call and ordinal environment intervals, override home to Shelf Sea, then apply starting profile with no further RNG/IDs. Keep CurrentVersion 2. Project only known planets; preserve owner-filtered forecast calculation. Per-worker previews use a one-worker aggregate and shared EconomyProfile/breakdown machinery, with actual capacity/mode permissions; actual multi-worker totals remain Task 3 aggregate calculations.
- [ ] **Verify:** focused command and full simulation gate. Expected: all green, no hidden-state dependence, identical final RNG across all four species and old trace unchanged.
- [ ] **Commit:** explicit task paths; `git commit -m "feat: generate surveyed environments and species setup"`.

### Task 6: Species projects, snapshot maturity and conserved embarkation

**Files:** Create Economy/EmbarkationRules.cs, tests/OpenAntares.Simulation.Tests/SpeciesEmbarkationTests.cs. Modify Economy/VesselProduction.cs, Economy/EconomyResults.cs, Commands/ExpansionCommands.cs, Turns/TurnResolver.cs.

**Interfaces:** EmbarkationRequirement(string Code,int Actual,int Required); EmbarkationEligibility(bool Eligible,ImmutableArray<EmbarkationRequirement> Unmet). EmbarkationRules.Evaluate(ContentSet,GameState,ColonyState,VesselDefinition) returns eligibility solely from snapshot population/support/effective capacity and role fields. ReserveOutcome keeps BlockedReasonCode = awaiting_embarkation and gains ImmutableArray<EmbarkationRequirement> BlockedRequirements. Existing VesselCompletionPlan/EmbarkOutcome remain; post-embark support uses Task 3's profile. SetProductionTargetCommand uses SpeciesRules.CanBuildVessel for rules 3, retaining old rules-2 availability.

- [ ] **RED:** SpeciesEmbarkationTests asserts every approved role/cost/cargo/minimum/speed; selecting a disallowed project returns project_not_available_for_species with byte-identical target/reserve/readiness. Peldra reserve2600 +600 -3200 =0; parent4/workforce2/1/1/progress5, next support600/800 and outputs450 each. Sulan unchanged opening completes native expedition after6 resolutions and harbor after9 (400 production/resolution). Wenth mature6 reserve2000 +400 -2400 =0, parent3/workforce1/1/1, next support300/300, outputs400 each, growth10. Extended Wenth requires8 (not6), transfers3 leaving5; daughter remains unbuilt cap6. Immature/birth-to-maturity/same-turn capacity completion cannot qualify; extension completing this turn does not disqualify a snapshot-full parent. Assert all unmet conditions, bank/remainder/target retention, no ID use while blocked, at most one cost/cargo transfer, cancellation/switching, and atomic ID-overflow failure. New vessels cannot move/found until later planning.
- [ ] **Run RED:** standard test command with `--filter FullyQualifiedName~SpeciesEmbarkationTests`. Expected: nonzero for role permission/maturity/cargo differences.
- [ ] **GREEN:** Calculate snapshot eligibility and funded completion once; apply existing growth then transfer exactly existing support cargo in the candidate, pay once and retain excess reserve. Fullness means snapshot population equals effective capacity, including completed extensions. Keep blocked projects valid in state/save validators. Forecasts expose current unmet requirements without unsupported completion estimates. Preserve rules-2 behavior and role content; no special Wenth/Peldra ID branches.
- [ ] **Verify:** focused command and full simulation gate. Expected: all green; species accounting conserved, genuine old continuation unchanged.
- [ ] **Commit:** explicit task paths; `git commit -m "feat: enforce species expedition and maturity rules"`.

### Task 7: Habitat-aware founding and the complete expansion path

**Files:** Create Colonization/FoundingRules.cs, tests/OpenAntares.Simulation.Tests/SpeciesFoundingTests.cs. Modify Commands/ExpansionCommands.cs, Colonization/ColonizationResolver.cs, Turns/TurnResolver.cs, State/ExpansionStateValidator.cs, State/SpeciesStateValidator.cs, Knowledge/EmpireView.cs, Knowledge/EmpireViewProjector.cs and any remaining expansion equality gates in Saves/SaveGame.cs.

**Interfaces:** FoundingPreview(PlanetId Target,string HabitatMode,int Population,int Capacity,string? RejectionCode) gains FoundingEconomyPreview? Economy init property; FoundingEconomyPreview(Quantity SupportProduced,Quantity SupportRequired,Quantity GrowthEarned) contains the first economic snapshot of an unbuilt all-support daughter on that known world. Rejected previews have null Economy; accepted previews have the shared species calculation, not UI arithmetic. FoundingRules.Preview(ContentSet,GameState,EmpireId,VesselId,PlanetId) returns the pure outcome of existing command eligibility, using Task 2 habitat rules and role cargo. SetFoundingOrderCommand and projection share this helper; no mutation in preview. OwnedVesselView gains ImmutableArray<FoundingPreview> FoundingPreviews init property for permitted known targets of owned founding vessels only. ColonizationResolver.Apply retains its signature and records HabitatMode on newly founded colonies. Existing command rejection codes remain except the spec's explicit new codes.

- [ ] **RED:** SpeciesFoundingTests uses two surveyed maps: Verdant/Dry vs Forge/Shelf Sea, then Verdant/Shelf Sea vs Forge/Dry. Sulan native accepts only Shelf Sea; harbor accepts either colonizable world including Shelf Sea, preserving physical environment and base capacities12/8. Assert Peldra two-unit native previews: Verdant support600/400/growth15, Forge400/400/growth5; Sulan Forge400/200/growth30. All species complete build/travel/arrival/found using real commands. Harl daughter is1/workforce1/0/0, no founding-turn economy, next growth50 and second snapshot population2. Wenth daughter is3/cap6, extended parent remains5. Edit role cargo in valid test content and assert preview/actual workforce match it. Unknown-existing and nonexistent target IDs both return target_not_known before physical queries. Habitat rejection preserves orders/cargo/readiness; competing valid intents use established stable-ID winner, no duplication; invalid candidate/ID overflow publishes nothing.
- [ ] **Run RED:** standard test command with `--filter FullyQualifiedName~SpeciesFoundingTests`. Expected: nonzero on habitats/rules-3 founding; old cases remain green.
- [ ] **GREEN:** Check known membership before hidden existence/occupancy; derive mode from role and capacity from owner's species. Validate native/sealed permission, colonizability and cargo, then preserve ordinary reservations/ordered conflicts. Found colonies with all cargo assigned to support and no founding-turn economy. Audit every exact rules-2 gate in commands, state, observations, movement/turns, production and persistence; use HasExpansion only for behavior shared by 2/3. Do not broaden to arbitrary future versions. Structural loaded-intent validation must agree without making blocked projects invalid or leaking unknown facts through errors.
- [ ] **Verify:** focused command and full simulation gate. Expected: all green; tests compare permitted previews to commands and subsequent snapshots, old canonical trace unchanged. Search remaining `Rules.ExpansionVersion` uses and classify each intentional old-only case in the execution ledger.
- [ ] **Commit:** explicit task paths; `git commit -m "feat: found species habitats through shared permitted rules"`.

### Task 8: Activate core content and complete the playable UI

**Files:** Create content/core/species.json, content/core/environments.json, tests/OpenAntares.Game.Validation/SpeciesGraphicalValidation.cs and SpeciesGraphicalValidation.tscn. Modify content/core/expansion.json, content/core/planet_types.json, src/OpenAntares.Simulation/Rules.cs, game/Screens/NewGameScreen.cs, game/Screens/GameScreen.cs, game/Components/ColonyPanel.cs, ColonyList.cs, VesselPanel.cs, StarPanel.cs, game/BreakdownText.cs, tests/OpenAntares.Game.Validation/Run-Validation.ps1, Validation.targets and ExpansionGraphicalValidation.cs. Add core-loading assertions to SpeciesContentTests.

**Interfaces:** NewGameScreen.Configure(string sizeId,ulong seed,string? speciesId = null) retains old callers; StartRequested supplies explicit SpeciesId with rules 3. Core UI visibly defaults to Harl. For valid alternative content lacking Harl, select ordinal first species; empty species list disables New Game with a useful content error, leaving load available. This is a routine content-fallback ruling, not a change to the approved core default. Display immutable own views, PlanetPotentialView, FoundingPreviews, BlockedRequirements and shared breakdowns from earlier tasks. Runner adds Scenario Species alongside Legacy/Expansion; opt-in targets compile the new harness only for validation builds.

- [ ] **RED:** Add the first Species graphical scenario using the actual NewGameScreen: assert four loaded choices/descriptions, Harl preselection and actual rules-3 Harl launch. On current UI it fails (record named assertion). SpeciesContentTests loads actual content/core and asserts every approved numeric/role/environment value. Add alternative-content harness cases without Harl and with renamed role labels/cargo; selectors/previews must reflect content. Do not silently drop current tests when the default changes.
- [ ] **Run RED:** `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --filter FullyQualifiedName~SpeciesContentTests`, then the graphical command below with Scenario Species and a unique RED output directory after the runner knows the scenario. Expected: nonzero for missing core definitions/default UI assertion, with retained logs. The runner's normal rebuild must still occur after failure.
- [ ] **GREEN:** Add exact approved JSON profiles and ordinal weights; leave old rules.json, buildings, technologies and economy costs unchanged. Flip Rules.CurrentVersion to 3 now that all prerequisites are present. Implement species picker, actual setup tradeoffs/culture text, habitat/environment labels, effective rate/capacity previews, allowed project choices, every unmet embark requirement and post-embark shortage warning. Explain native versus sealed Sulan opportunities and Wenth current-capacity maturity; no hard-coded population4/cargo2 or UI math. Update version gates for rules-3 colony list/vessels/pickers. Preserve scroll/focus/resize handling. The old Expansion graphical scenario must explicitly load/start frozen rules-2 content for each size; do not assert rules2 after using the new default. The new default belongs to Species scenario; keep Legacy genuine old fixtures.
- [ ] **Verify:** full simulation gate, normal solution build and Species scenario. Expected: all pass, usable four-species setup and core JSON validated, zero diagnostics, ordinary assembly excludes all three validation classes. Retain outputs/screenshots; assertions exercise real UI signals and public commands, not hand-edited completed live state.
- [ ] **Commit:** explicit task paths; `git commit -m "feat: expose four playable species and habitat decisions"`.

Graphical command template (run from the execution checkout; unique directories each attempt):

```powershell
$speciesRunTag = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssffffZ')
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario Species -OutputDirectory "reports/species-$speciesRunTag"
```

Expected GREEN: runner exit0, results.json all assertions passing, zero final errors, screenshots and uniquely named QA saves retained, normal rebuild0warnings/errors. Inspect results.json; process exit alone is insufficient. Re-locate the installed Godot executable if this historical path is unavailable; no engine upgrade is implied.

### Task 9: Retained full species/legacy acceptance and failure paths

**Files:** Extend SpeciesGraphicalValidation.cs, SpeciesGraphicalValidation.tscn, SpeciesCompatibilityTests.cs and the named Species*Tests where coverage belongs. Create reports/SPECIES_VALIDATION_2026-10-23.md and uniquely named runtime evidence directories at execution time (use actual date if later). Keep earlier failed attempts and saves.

**Interfaces:** Harness uses the real GameSession/NewGameScreen/GameScreen and commands. Every runtime result records criterion, actual value, failure and artifact path. No second calculator or privileged raw-state presentation. Setup-only synthetic fixtures may establish known-map/failure cases; playable outcomes must follow real commands/resolution and permitted UI. Existing runner's finally rebuild stays intact.

- [ ] **RED:** Add named runtime assertions for all four species across the three generated sizes (12 seeded loops), both Sulan expedition modes on contrasting surveyed maps, Wenth banked/full/extended maturity, Peldra shortage warning, Harl small-daughter recovery, multiple owned colonies and the actual save/load dialogs. Test default and smaller documented windows (1920x1080,1280x720,800x600) for clipped critical selectors, blocked conditions and habitat/breakdown text. Use existing layout conventions; screenshot/layout assertions do not prove comfortable human readability. Add corrupt rules-3 load preserving current session, simultaneous intent conflicts and unknown-target parity as needed in the owning simulation suite. Do not claim mandatory failure if this acceptance extension already passes; record first observed result honestly.
- [ ] **Run initial check:** full simulation gate and unique Species runner. Expected: each new assertion executes; any uncovered behavior failure is recorded before correction. If all are green, retain that evidence rather than inventing a defect.
- [ ] **GREEN:** Correct only observed defects with focused reproductions. Generated loops scout, inspect, budget actual forecasts, select/build, embark, travel, found, allocate two colonies, save/reload and compare future canonical continuations. Worker changes go through real controls/commands; do not overwrite live reserves/population/RNG to shortcut the loop. Retain tests for current/stale/unknown observations, banked immature saves and no midcampaign species mutation.
- [ ] **Verify:** run full simulation gate, Species, Expansion and Legacy scenarios in three distinct unique output directories using Task 8's template with Scenario changed. Expected: zero failures/skips, every required criterion has executed evidence, no final engine diagnostics; final normal rebuild0warnings/errors and reflection check excludes GraphicalValidation/ExpansionGraphicalValidation/SpeciesGraphicalValidation. Compare old trace after all changes. Report actual new counts, never reuse planning's historical counts as fresh evidence.
- [ ] **Commit:** explicit test/harness/report/evidence paths after checking for user saves/private machine paths; `git commit -m "test: retain species campaigns and legacy regression evidence"`. QA saves themselves remain preserved; follow existing evidence practice rather than blindly adding all user data.

### Task 10: Reconcile contracts, review the branch and hand off

**Files:** Modify docs/SPECIFICATIONS.md, docs/ACCEPTANCE_CRITERIA.md, docs/FIRST_PLAYABLE_ECONOMY.md, docs/IMPLEMENTATION_PLAN.md, docs/PROJECT_CONCEPT.md, PROJECT_STATUS.md and this plan's checkboxes; add execution ledger/review evidence in reports/. Link the approved species specification as the rules-3 amendment to the expansion specification without rewriting its historical rules-2 contract. Update continuation guidance in AGENTS.md/docs/MASTER_IMPLEMENTATION_PROMPT.md if factual status changed. Do not edit/stage the two owner prose files.

**Interfaces:** REQ-016 stays In Progress until implemented criteria have real evidence; automated local verification never promotes human REQ-014 or cross-platform REQ-013. SPC-AC-010 is the already approved design gate, not proof of implementation. Record actual baseline/final SHAs, hashes, commands, test counts, runtime limitations, remaining worktrees and owner edits.

- [ ] **Reconcile:** Update rules-3 formulas/setup/habitats/save/version/UI amendments and coverage links while retaining old-campaign behavior and historical evidence. Check all SPC/AC rows below, actual execution checkboxes and five Review Focus cases; leave unexecuted criteria explicit.
- [ ] **Final review:** Under the preserved Native workflow, obtain one independent whole-branch review after implementation, scoped to approved spec, invariants, tests and unintended changes. This is an execution-time review, not permission to delegate implementation. Address findings with regression-first fixes and rerun affected gates; retain reviewer conclusions and unresolved limits. Do not seek a human participant to close the deferred session.
- [ ] **Final verification:** On the exact reviewed candidate, full simulation suite, normal build/rebuild and all three graphical regressions if review fixes touched relevant behavior; documentation links/IDs/counts and owner-file hashes must pass. Expected: complete actual evidence, zero unresolved correctness findings, no unintended gameplay/content changes, no validation classes in normal assembly. If no code changes follow Task 9 gates, cite that exact candidate evidence instead of repeating unchanged expensive tests.
- [ ] **Commit and handoff:** Commit only reviewed milestone paths. Report final SHA, branch/worktree, implemented versus unverified criteria and concrete remaining risks. Stop before merge/push/release/cleanup unless separately authorized for this milestone. Earlier expansion integration permission is not a blanket species merge authorization.

## Coverage and planning self-review

| Approved contract / acceptance | Implementing tasks and evidence |
| --- | --- |
| SPC-001 / SPC-AC-001 Setup and four identities | 2,5,8,9: all profiles, 12 generation cases, visible Harl default, fixed saved identity |
| SPC-002 / SPC-AC-002 Exact economy and growth | 3,9: opening/rounding/flat building/shortage/multiturn fixtures |
| SPC-003 Production/cargo | 2,6,7,9: approved roles, snapshot eligibility, reserve/cargo conservation, extended Wenth |
| SPC-004 Sulan habitats and meaningful choices | 2,5,7,9: both contrasting maps, native/sealed capacities, no terraforming |
| SPC-005 Wenth fission | 2,3,6,7,9: maturity, extended capacity, conserved lobes and daughter state |
| SPC-006 Data/state/persistence | 1,2,4,5,7: bounds, metadata, genuine old trace and version matrix |
| SPC-007 Player explanations | 5,7,8,9: permitted previews, real controls, content labels and layout evidence |
| SPC-008 Acceptance gates | 1–10 and the exact criterion mapping below |
| SPC-AC-003 Growth traces | 3: Harl T5/T15, rules-2 T8, Sulan T12 and exact discards |
| SPC-AC-004 Allowed roles/cargo | 2,6,7: selection, bank/cancel, birth boundary and conservation |
| SPC-AC-005 Sulan habitat/knowledge | 4,5,7,9: both maps/modes/capacities, saved sealed colony and no leaks |
| SPC-AC-006 Wenth maturity/fission | 3,6,7,9: T6, 6-to-3+3, 8-to-5+3 and conflict/overflow |
| SPC-AC-007 Determinism | 1,4,5,9: base geometry, sampling/RNG and continuation |
| SPC-AC-008 Validation/compatibility | 1,2,4,6,7,9: bounds, corrupted states, old fixtures and failure atomicity |
| SPC-AC-009 Integrated scenarios | 8,9,10: generated loops, windows, all retained regressions and review |
| SPC-AC-010 Owner design review | ADR-008 records approval; this plan awaits its separate review |

Self-review performed when writing the plan: all eight SPC sections/ten criteria have task coverage; future interfaces agree; five Review Focus risks map to named checks; actual file paths checked against the source inventory (including game/BreakdownText.cs and Commands/ExpansionCommands.cs). No function bodies or speculative extra subsystem were added. Implementation details that remain ordinary coding choices stay with the implementer. The plan is a single coupled slice with ten separately verifiable commits, not ten independent products.

**Plan-review handoff:** Read this plan and its approved spec together. Confirm the plan captures the intended classic space-opera species direction before execution. Preserve the owner's Native preference; no new method selection or design reapproval is needed. No implementation step has been executed by preparing this document.
