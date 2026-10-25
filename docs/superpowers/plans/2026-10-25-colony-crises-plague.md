# Colony Crises and Plague Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved first plague scenario, meaningful response choices and scalable emergency governance while preserving every genuine old campaign.

**Architecture:** Extend the existing plain C# simulation with owner-scoped crisis state, pure economic/attention/transport queries, validated commands and explicit deterministic resolution phases. Presentation consumes immutable own views and sends ordinary commands. Gate new behavior to rules6 and retain old rules/content exactly; activate the new default only after the feature and controls are verified.

**Tech Stack:** Existing .NET/C#, xUnit, Godot4.7.2 .NET, JSON content, PCG32; no new dependencies, assets or framework.

**Spec:** [Approved colony-crisis/plague design](../specs/2026-10-24-colony-crises-plague-design.md), full owner approval2026-10-25 in [ADR-014](../../DECISIONS/ADR-014-approved-colony-crisis-plague-design.md). Read both before executing.

**Execution gate:** The owner approved the full design, not this previously unwritten plan. M6-D/CRI-AC-012 Verified for design; M6-P In Progress for this plan's review; M6-I/CRI-AC-001..011 Not Started. Retain the established **Native inline** method: no implementer/per-task agents, one fresh whole-branch reviewer after all task completions. No method re-selection. No implementation in this planning turn.

## Global Constraints

These are binding throughout; full approved formulas/phase rules remain in the spec.

- “No crisis machinery runs for rules1..5.”
- “No virtual trade lanes, distance radius, empty/scout hulls or foreign contact transmit disease.”
- “Governors, AI and standing policies cannot issue destructive containment in M6.”
- “No automatic colony-count takeover.” Human crisis authority starts Manual; ordinary governors remain independent.
- “Local display preference cannot silently change this authoritative campaign rule.” Default saved Require acknowledgement precedes any human Ready preparation.
- “No-ops preserve original state/reference/Ready; real changes clear issuer Ready only.” Failures restore the whole before-request state.
- “Cure occurs before natural recovery/death/transmission at that boundary; that interval still pays snapshot economic penalties.”
- “Auto-release affects the next planning snapshot, never transport checks midway through resolution.”
- “Retain the existing16 explicit schema/rules pairs; add exactly (7,1)..(7,6),22 total.” No automatic migration or regenerated old expected traces.
- C# simulation has no Godot reference; deterministic integer/Int128 arithmetic, stable sorted IDs, invariant culture, existing PCG32 only. One separate saved crisis stream/empire; existing galaxy/economy stream and entity IDs unchanged.
- Exact approved core: checks20/40/... at25%; cooldown30; Warning2; Active severity20, open+20/quarantined+5, cap100; mortality1 at beginning severity>=80 except cure/recovery precedence; natural recovery sixth Active interval; immunity12; illness factor8000BP, quarantine7500BP; cure cost600+100*sourcePopulation; shares0/25/50/100; purge ceil(pop/2), retain floor(reserve/2); civic1000BP/level, cap3, expiry currentTurn+5.
- Shared theme/containers, existing file/content IDs and original species/culture. Uniform civic consequence is approved; culture-specific reactions, revolts, warfare/fleets, peaceful-mode design and other deferred features stay outside this plan.
- Preserve all primary owner changes, seven registered worktrees, old saves, branches, stashes and failed/successful evidence. No main merge, push, release, history rewrite or cleanup is authorized by plan execution alone.

## Review Focus

1. **A colony disappears after completing a project or embarking cargo:** reports, queue completion, selection and AI remain valid; neither lost assets nor the same population is credited twice. Tests T8/T10.
2. **Cure, natural recovery or auto-release coincides with transport:** ports use the frozen boundary snapshot; cured flags cannot reappear, natural recovery cannot erase an already infected departure, and no dangling closed-episode flag results. Tests T7/T8.
3. **A destructive preview becomes stale because another colony was purged or orders changed:** reject every stale consequence, including civic/last-colony/carrier facts, not just population/reserve; preserve exact state. Tests T5/T10.
4. **Small research totals and final cure spill:** round once at empire aggregate; retain normal reserves and cure precedence for a one-population case. Tests T4/T8.
5. **Digest/local Off, reload, another ready human or old unresolved receipts:** authoritative acknowledgement still blocks only the correct human; an explicit waiver never pretends the player read an event, and re-enabling cannot lose material facts. Tests T9/T11.

---

## Workspace, evidence and commands

Primary: F:/Projects-PCGames/OpenAntares, main fa2a07f33e6ef2e8d24549d0dcda635221068cd8, four owner changes/empty index. Current documentation worktree: C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design, branch codex/colony-crisis-design. At execution reuse this clean registered checkout and create codex/colony-crises from the approved plan commit, retaining the documentation branch and seven worktrees. Do not infer that an old integrated worktree is pending implementation.

Aliases below: S=src/OpenAntares.Simulation, T=tests/OpenAntares.Simulation.Tests, G=tests/OpenAntares.Game.Validation. Every listed path expands from the repo root. Run commands under retained Native Windows execution; ordinary builds/tests and common-Git mutations require the established escalated runner rather than the hanging restricted dotnet path. Use PYTHONUTF8=1 for any evidence helpers. No concurrent builds. Unique report folders/save names for each run; preserve failed runs.

- **Suite:** dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers
- **Focused(name):** same command plus --filter "FullyQualifiedName~name"; when names are joined A|B below, expand to --filter "FullyQualifiedName~A|FullyQualifiedName~B". Expected0 failures/skips and every named case executed, not an empty match.
- **Build:** dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
- **Runtime(scenario,folder):** ./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario scenario -OutputDirectory folder
- **Metadata:** ./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll'

Every task below means its own RED observed, implementation, focused GREEN and full Suite, then explicit-path commit and task-completion ledger entry with source SHAs/commands/output. A compile/import/invalid-fixture failure is not a semantic RED. Capture fixture-only missing-file RED in T1 before any product change. New flags/counters must be fully initialized in labelled fixtures before a session starts; never patch live state to make generated acceptance pass. Tests and fault seams belong to normal existing xUnit/internal patterns, not a second framework.

## File structure and interface ownership

| Unit | Files | Responsibility |
| --- | --- | --- |
| Frozen baseline | T/Fixtures/legacy-rules5-v6-{save,continuation,provenance}.json; T/Fixtures/rules5-content.json; T/CrisisCompatibilityTests.cs | Genuine pre-change capture and future equality |
| Definitions/models | S/Content/PlagueDefinition.cs; CrisisContentReader.cs; CrisisContentWriter.cs; S/Crises/CrisisState.cs; CrisisStateValidator.cs; CrisisInitializer.cs | Typed bounded content, owned mutable data/deep clone, setup/stream init |
| Persistence | S/Saves/CrisisSaveData.cs; existing SaveGame.cs | Strict versioned fields and22 pairs |
| Queries/economy | S/Crises/CrisisEconomy.cs; CrisisQueries.cs; CrisisViews.cs; CrisisPreviews.cs; existing EconomyCalculator.cs/EconomyResults.cs/Knowledge/EmpireView*.cs | Pure common calculations, owned immutable views, complete response consequences |
| Commands | S/Commands/CrisisCommands.cs; CrisisCommandProcessor.cs; existing CommandProcessor.cs/ExpansionCommands.cs | Atomic owner planning mutations and port-aware shared validation |
| Resolution | S/Crises/CrisisProgression.cs; CrisisOnset.cs; CrisisTransport.cs; CrisisResolutionContext.cs; CrisisReports.cs; existing Turns/TurnResolver.cs/Movement/MovementResolver.cs/Colonization/ColonizationResolver.cs/Development/DevelopmentCompletion.cs | Explicit health/cure/cargo phases and removed-colony-safe reports |
| Preparation | S/Crises/CrisisTurnCoordinator.cs; CrisisPlanner.cs; ICrisisPlanningContext.cs; existing Rivals/RivalTurnPlanner.cs/RivalPlanningQueries.cs/RivalVesselPlanner.cs/RivalColonyPlanner.cs | Own-view human/AI response with separate budgets/Ready gate |
| Presentation | game/Components/CrisisOverviewDialog.cs; CrisisResponseDialog.cs; CrisisAuthorityDialog.cs; CrisisNotificationDialog.cs; game/Presentation/CrisisNotificationPreferences.cs; existing GameScreen.cs/NewGameScreen.cs/ColonyList.cs/TurnReportDialog.cs | One overview, explicit consequences, acknowledgement and local preferences |
| Graphical verification | G/CrisisGraphicalValidation.cs/.tscn; existing Run-Validation.ps1/Validation.targets/Verify-NormalAssembly.ps1/FrozenContent.cs | Sixth opt-in scenario; genuine/generated/labelled evidence kept distinct |
| Activation | content/core/plagues.json; S/Rules.cs; game/ContentFiles.cs; S/Galaxy/GalaxyGenerator.cs | Late current6/Standard default and complete content loading |

### Shared data contract (T2 owns unless another task is named)

All new domain types live in OpenAntares.Simulation.Crises unless noted. Use optional Crisis properties on GameState/EmpireState/ColonyState/VesselState: required nonnull explicit state under6, null/omitted under1..5. Data objects follow current mutable plain-state/deep-clone conventions; published DTOs/records are immutable.

- GameCrisisState: bool Enabled. NewGameSettings gains trailing bool CrisesEnabled=true; only6 consumes it, old rules retain absent metadata and behavior.
- EmpireCrisisState: Pcg32 Random; long NextEpisodeNumber=1; int NextEligibleTurn=0; CrisisEpisodeState? Episode; CrisisAuthority Authority=Manual; int AuthorityResearchShare=50; bool ResearchPolicyPaused; bool RequireAcknowledgement=true; int CivicLevel/CivicExpiresAt; pending/completed CrisisDigest lists (max10 completed); pending CrisisReceipt list; sorted List<CrisisLocation> LostColonyLocations. Keep retired locations only while referenced by retained governor/crisis history, an active episode source or pending receipt; never substitute a currently foreign live colony. CrisisAuthority enum is defined here (Manual,Contain,ContainAndResearch).
- CrisisEpisodeState: long Id; string FamilyId; CrisisLocation Source; int SourcePopulation; long CureCost/CureProgress; int ResearchShare; bool ResearchOverride. One/empire; never allocate global IDs.
- ColonyCrisisState: CrisisCaseState? Case; bool Quarantined; long? ReleaseEpisode; bool PolicyPaused; string? ImmunityFamilyId; int ImmuneUntilTurn. ReleaseEpisode binds to this local case; null means keep until ordered.
- CrisisCaseState: long EpisodeId; CrisisStage Stage (Warning,Active); int GraceRemaining/ActiveAge/Severity; long AttentionRevision/HandledRevision/WaivedRevision; CrisisLocation Origin; VesselId? SourceVessel. Revision1 at exposure; once/boundary on coalesced material events. Transport metadata uses owner-local episode plus colony key, not world-global disease IDs.
- VesselCrisisState: long? CarrierEpisode; int? InspectionDueTurn. Inspection without a flag is legal; cure clears flag without silently deleting a remaining port hold.
- Immutable CrisisDigest(int Turn,ImmutableArray<CrisisDigestEntry> Entries) and CrisisDigestEntry(CrisisKey Key,CrisisLocation Location,string Kind,long Revision,string Disposition,long ResearchDiverted,int PopulationLost,string Reason); define these in T2 so state/codec tasks have no forward dependency.
- Immutable CrisisKey(EmpireId Owner,long Episode,ColonyId Colony), CrisisLocation(ColonyId Colony,PlanetId Planet,StarId Star,string Name), CrisisReceipt(CrisisKey Key,long Revision,int Turn,CrisisLocation Location,string Reason). Lists sorted by key; pending receipts exist only for unresolved attention, not every historical lost colony. Waivers/acknowledgements retain provenance in digests.
- PlagueDefinition is a content record with named parameters from CRI-001..003, purging numerator1/denominator2, monetary units as long and bounded timing/factors as int. ContentSet.Plagues is an ImmutableSortedDictionary<string,PlagueDefinition>. ContentLoader.Load adds trailing bool requireCrises=false, preserving existing calls and implying existing development/rival/species requirements when true. JSON section name plagues; require exactly one supported family for6, including Off; additional families are outside the approved first slice and must fail usefully rather than silently selecting one. Mods may rename/retune that single definition without a hardcoded-ID branch. Core family remains ash_bloom.
- Plague JSON contract: id/name; first_check_turn20, check_interval20, check_chance_percent25, cooldown_turns30, grace_turns2, recovery_intervals6, initial_severity20, severity_cap100, open_severity_step20, quarantined_severity_step5, mortality_threshold80, mortality_units1, illness_factor_bp8000, quarantine_factor_bp7500, cure_base600, cure_per_population100, immunity_turns12, purge_numerator1, purge_denominator2, civic_factor_loss_bp1000, civic_max_level3, civic_turns5. PlagueDefinition properties use the corresponding PascalCase names; readers/writers preserve every explicit value.
- CrisisStateValidator.Validate(ContentSet,GameState,List<string>) and CrisisInitializer.Initialize(ContentSet,GameState,bool enabled) return void; initialization called after ordinary empires/IDs/appearance exist. Seed exactly as approved; require a rules6 galaxy seed, never reseed a loaded stream.

T4 owns CrisisView immutable aggregate (Viewer,Enabled,Authority,Share,RequireAcknowledgement,CivicLevel/Expiry,Episode summary,Cases,Carriers,PendingReceipts,History,LostColonyLocations); corresponding optional Crisis on EmpireView/OwnedColonyView/OwnedVesselView. CrisisCaseView contains key/location/material revision, grace/age/severity, quarantine/release/override/immunity, forecast impacts and loss risk; CrisisVesselView contains own flag/hold/port reasons. These DTOs never expose mutable state or foreign crisis facts. CrisisQueries.Project(ContentSet,GameState,EmpireId)->CrisisView; PreviewPurge(ContentSet,GameState,EmpireId,ColonyId)->PurgePreview; PreviewQuarantine(ContentSet,GameState,EmpireId,ImmutableArray<ColonyId>,bool enable,bool releaseOnRecovery)->QuarantinePreview. Preview records include every consequence and target/case revision; structural equality/recomputed expectations replace fragile hash-code tokens. T4 also defines immutable PurgeAssetFact(ImmutableArray<string> Buildings,ProductionTarget? Target,ImmutableArray<ProductionQueueEntry> Queue,GovernorTemplateSnapshot? Template,bool WorkforceEnabled,bool ConstructionEnabled,int GrowthProgress), included in PurgePreview; and VesselOrderFact(VesselId Id,StarId? Station,TransitState? Transit,StarId? Move,PlanetId? Founding,int? InspectionDueTurn), CrisisQueries.PortClosed(GameState,EmpireId,StarId)->bool and IsHeld(GameState,VesselId)->bool for T5 shared validation.

T4 owns CrisisEconomy.Scale(long amount,int factorBasisPoints)->long (checked floored Int128 division), CureAllocation(long Starting,long Cost,long Requested,long Diverted,long RemainingForTechnology,bool Completes), CrisisEconomy.ApplyColony(ContentSet,GameState,ColonyState,ColonyEconomy)->ColonyEconomy and SplitResearch(ContentSet,GameState,EmpireId,long newResearch)->CureAllocation. EmpireEconomy gains optional CureAllocation Cure while preserving its existing constructor. Integration in EconomyCalculator happens before production/vessel reserve completion and before empire technology reserve calculation, not as post-processing of already-completed projects.

T6 owns CrisisResolutionContext.Capture(ContentSet,GameState snapshot)->CrisisResolutionContext and its transient state: immutable beginning port/source facts and CrisisLocation map, ToTurn, cured owner/episode keys, boundary-sterile vessel IDs, collected CrisisExposure proposals, immutable report events. Expose named context members int ToTurn, ImmutableDictionary<ColonyId,CrisisLocation> Locations, HashSet<(EmpireId,long)> CuredEpisodes, HashSet<VesselId> SterileVessels, List<VesselId> ArrivedVessels, List<CrisisExposure> Exposures, List<CrisisEvent> Events, and closed-port membership facts; never enumerate a hash set to determine outcomes. It is transient and never saved. Signatures: CrisisProgression.Apply(ContentSet,GameState snapshot,GameState candidate,TurnEconomy,CrisisResolutionContext)->void; CrisisOnset.Apply(ContentSet,GameState candidate,CrisisResolutionContext)->void; CrisisReports.Finalize(GameState snapshot,GameState candidate,CrisisResolutionContext)->void. T7 adds void TagCompletion(ContentSet,GameState snapshot,GameState candidate,CrisisResolutionContext,ColonyId source,VesselState vessel); void PrepareDepartures(ContentSet,GameState snapshot,GameState candidate,CrisisResolutionContext); void HandleArrivals(ContentSet,GameState snapshot,GameState candidate,CrisisResolutionContext); void TransferFounding(ContentSet,GameState snapshot,GameState candidate,CrisisResolutionContext,VesselState consumed,ColonyState founded); void ApplyExposures(ContentSet,GameState snapshot,GameState candidate,CrisisResolutionContext). Mutate only the outer private candidate. T8 owns construction/calls of this context and published reports.

### Coverage ownership

CRI-001→T2/T6/T12/T13;002→T6/T8/T10;003→T4/T5/T8;004→T5/T7/T8;005→T9/T10/T11;006→T5/T8/T9;007→T2/T3/T14. AC001→T2/T6;002→T6/T8;003→T4;004→T4/T8;005→T5/T7;006→T5/T10;007→T9/T11;008→T10/T11;009→T4/T5/T8/T9;010→T1/T3/T13;011→T12/T13/T14;012 already satisfied by ADR-014, never relabelled runtime acceptance.

## Task 1: Freeze genuine rules5 continuation before product changes

**Files:** Create T/CrisisCompatibilityTests.cs and the four frozen fixture files in the structure table; execution ledger/provenance.

**Interfaces:** Consumes existing SaveGame, GameSession and CompatibilityFixtures.PathFor. Produces schema6/rules5 genuine save, embedded rules5-content.json and exactly four canonical future planning states, plus source/creator/capture hashes. No new crisis API required.

- [ ] **RED:** Add GenuineRules5CampaignContinuesExactly; assert fixture load succeeds, Turn24, Colonies.Count6, one expansion_v1 AI, and four stored future states. Focused(CrisisCompatibilityTests) fails for missing genuine fixture, not an invented behavior mismatch.
- [ ] **Capture under unchanged engine:** Locate retained actual UI save C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves/qa-20261025-033820-304-generated-colony-harl-developed.json. Expected SHA25633221d961e8c52722639beb43386d95b22331922dc61495d3f1faf772bbea4a3, source productfc29d0b, genuine metadata in reports/colony-polish-main-20261024-run1/colony/results.json. Copy without overwriting source. Verify no src/game/content diff versus mainfa2a07f. Using a retained temporary Native C# helper referencing the unchanged simulation, load it and submit four ordinary final-human Ready commands; capture actual serialized states and embedded content. Record command/helper/source/provenance hashes. If absent or mismatched, locate another genuine retained source explicitly; never manufacture expected old behavior with altered code.
- [ ] **GREEN:** Focused(CrisisCompatibilityTests), then Suite. Canonical comparison may normalize writer schema only once T3 changes it; rules/content/state remain exact. Preserve every older/passive-AI frozen byte.
- [ ] **Verify:** Fresh main four-file hashes/status/index, branch/worktree inventory and backup outside tracked authored paths. Capture helper and new QA are retained; no feature flag/default/writer/content change yet.
- [ ] **Commit:** test: freeze genuine rules5 crisis baseline; explicit fixture/test/evidence paths; task-done with Suite output.

T1 assertion contract:

```csharp
var baseline = SaveGame.ReadFile(CompatibilityFixtures.PathFor("legacy-rules5-v6-save.json"));
Assert.True(baseline.Succeeded);
Assert.Equal(24, baseline.Game!.State.Turn);
Assert.Equal(6, baseline.Game.State.Colonies.Count);
```

## Task 2: Define validated crisis content/state and seed initialization

**Files:** Create S/Crises/CrisisState.cs, CrisisStateValidator.cs, CrisisInitializer.cs, S/Content/PlagueDefinition.cs/CrisisContentReader.cs/CrisisContentWriter.cs; modify ContentSet.cs/ContentLoader.cs/ContentWriter.cs, Rules.cs, State/GameState.cs/State/VesselState.cs/State/GameStateValidator.cs, Galaxy/GalaxyGenerator.cs; create T/CrisisFixtures.cs/CrisisContentTests.cs/CrisisStateTests.cs/CrisisGenerationTests.cs and T/Fixtures/crisis-content.json. Do not add core plagues.json until T13.

**Interfaces:** Produce shared T2 contracts above; Rules.CrisisVersion=6/HasCrises(int), explicit supported predicates include6 while CurrentVersion remains5. CrisisInitializer.CreateRandom(ulong seed,EmpireId owner)->Pcg32. CrisisFixtures.Game(string species="harl",int population=6,bool enabled=true)->LoadedGame; Active(int severity=20,int age=0,string species="harl",int population=6,bool quarantine=false)->LoadedGame; Warning(int grace=2)->LoadedGame; TwoHumansAndAi()->LoadedGame; CargoArrival(bool quarantined=false,bool found=false)->LoadedGame (one owned flagged cargo at transit remaining1, an owned open/closed healthy destination or unowned surveyed founding planet, valid source episode and all required metadata). Fixtures are labelled, sorted valid full states before starting sessions; no source-state reversal against the established sorted-ID contract.

- [ ] **RED:** Pin defaults and explicit clone/ownership validation. Numeric seed test:

```csharp
var rng = CrisisInitializer.CreateRandom(42, new EmpireId(1));
Assert.Equal(11158500043467840445UL, rng.State);
Assert.Equal(11572142791786627073UL, rng.Increment);
Assert.Equal(new uint[]{1215802219,315086694,2652593244,2676242757},
    Enumerable.Range(0,4).Select(_ => rng.NextUInt32()).ToArray());
Assert.False(Rules.HasCrises(5)); Assert.True(Rules.HasCrises(6));
```

Also test missing/duplicate family, unknown field, zero denominator/timing, factors>10000, bad stream increment, foreign episode link, illegal release binding and counter/expiry overflow. Focused(CrisisContentTests|CrisisStateTests|CrisisGenerationTests) fails on missing contracts.
- [ ] **Implement:** Shared definitions/readers follow existing partial Builder/ContentWriter pattern. Bound timing1..10000, grace at least1, severity/cap1..100 with initial<=cap and thresholds<=cap, factors0..10000, purge numerator1..denominator with denominator<=10000, civic level cap1..100/factor product<=10000, chance0..100, cure costs nonnegative and combined cost positive/bounded for maximum valid population. Checked wide arithmetic for durations/counters/costs; reject impossible combinations. Core exact values remain those approved. Validate owned cases/cargo against the active episode; Off requires dormant empty facts/default authority/attention metadata, streams saved but untouched. Zero-colony empires are legal. Deep clone every new mutable node/list/RNG.
- [ ] **GREEN:** Named tests/Suite. Rules6 Off and Standard generation preserve base rules5 galaxy/IDs/old RNG/appearance after explicit new-field/version removal; no draws while viewing. Compare actual C# vectors to reports/colony-crisis-plan/reference-values.json, including seed42/owner46 probability77/no selection draw. Literal vectors are independent planning arithmetic until this test runs.
- [ ] **Commit:** feat: define bounded owner-scoped crisis state and content; task-done.

## Task 3: Add strict schema7 persistence without upgrading old campaigns

**Files:** Create S/Saves/CrisisSaveData.cs, T/CrisisSaveTests.cs; modify SaveGame.cs/ContentWriter.cs/CrisisContentWriter.cs.

**Interfaces:** CrisisSaveData.WriteGame/WriteEmpire/WriteColony/WriteVessel(Utf8JsonWriter, corresponding CrisisState?,int rules)->void; matching ReadGame/ReadEmpire/ReadColony/ReadVessel(EntryReader,int rules)->corresponding state?. Serialize required null/default/empty properties explicitly under6; reject them under legacy rules rather than silently discarding injection. Writer SchemaVersion7 now; current new-game rules remain5 until T13.

- [ ] **RED:** Schema7RulePairsAreExplicit and MissingCrisisFieldNeverMeansConsent. Enumerate the existing16 pairs plus(7,1)..(7,6), assert22 accepted and every other pair from schemas0..8/rules0..7 rejected. Round-trip active warning/illness, held unflagged cargo, immunity, waived revisions, resolved receipt and civic expiry; each required-field deletion/foreign reference/injection rejects and leaves GameSession's original content/state references intact. Focused(CrisisSaveTests) RED.
- [ ] **Implement:** Version-gated embedded plagues and strict state codecs, including strings/enums/bounds/unknown fields. Preserve old rules1..5 writes with no crisis section under writer7. Keep the existing atomic temporary-file replacement protocol and report pending-receipt identities after colony deletion.
- [ ] **GREEN:** Focused(CrisisSaveTests|CrisisCompatibilityTests), full Suite. T1 four future states and all earlier frozen continuations equal after schema-only normalization; deliberately failed write/load leaves original file/session untouched. New nested clone changes cannot mutate the loaded source.
- [ ] **Commit:** feat: persist crises with explicit schema7 compatibility; task-done.

T3 assertion contract:

```csharp
var g = CrisisFixtures.Active();
string json = SaveGame.Serialize(g.Content,g.State);
using var document = System.Text.Json.JsonDocument.Parse(json);
Assert.Equal(7,document.RootElement.GetProperty("schema_version").GetInt32());
Assert.True(SaveGame.Deserialize(json).Succeeded);
```

## Task 4: Build shared economic forecasts and immutable own queries

**Files:** Create S/Crises/CrisisEconomy.cs/CrisisQueries.cs/CrisisViews.cs/CrisisPreviews.cs; modify Economy/EconomyCalculator.cs/EconomyResults.cs/Breakdown.cs/VesselProduction.cs and Knowledge/EmpireView.cs/EmpireViewProjector.cs; create T/CrisisEconomyTests.cs/CrisisQueryTests.cs.

**Interfaces:** Produce T4 shared contracts. PurgePreview contains key/revision/location, current/surviving population/workforce, old/kept reserve, growth reset, colony/asset loss, next civic level/expiry, owner colony count, owned carrier/case warnings. QuarantinePreview contains affected colony settings, full system-port set, sorted cancelled own vessel orders and release bindings. Both include Allowed/rejection reason and complete structural expected consequences, not a mutable reference/hashcode. CrisisQueries.PendingAttention(GameState,EmpireId)->ImmutableArray<CrisisReceipt> includes live and resolved pending revisions.

- [ ] **RED:** ExactFactorsAndAggregateCureSpill, NoCrisisViewReadsForeignFacts, QuarantineHoldsCargoBeforeCostOrEmbark, and ExistingReserveNeverFundsCure. Pin:

```csharp
Assert.Equal(486, CrisisEconomy.Scale(CrisisEconomy.Scale(CrisisEconomy.Scale(900,8000),7500),9000));
Assert.Equal(378, CrisisEconomy.Scale(CrisisEconomy.Scale(CrisisEconomy.Scale(700,8000),7500),9000));
var g = CrisisFixtures.Active(); var owner = g.State.Empires[0];
var episode = owner.Crisis!.Episode!;
episode.CureCost=1200; episode.CureProgress=900; episode.ResearchShare=50;
var allocation = CrisisEconomy.SplitResearch(g.Content,g.State,owner.Id,1000);
Assert.Equal(300, allocation.Diverted);
Assert.Equal(700, allocation.RemainingForTechnology);
episode.CureCost=2; episode.CureProgress=0;
var tinyAggregate = CrisisEconomy.SplitResearch(g.Content,g.State,owner.Id,2);
Assert.Equal(1, tinyAggregate.Diverted); // Aggregate1+1, not floor-per-colony0+0.
```

Scale is the same helper used by the explained production/research pipeline, not an independent reimplementation in tests. Also exercise complete ApplyColony/Calculate fixtures so a correct helper cannot conceal a misplaced integration hook. Add zero workers/flat foundations/all species/zero support and fractional-rounding tests. Named focused tests RED.
- [ ] **Implement:** Reuse existing Output/shortage/growth/reserve functions; insert local factors before Reserve/VesselProduction completion. Pause growth without changing prior progress/capacity rules. Add cure diversion breakdown before normal research reserve; retain total earned vs diverted vs ordinary contribution distinctly. Quarantine keeps a valid cargo target/queue/reserve with blocked reason quarantine; ordinary non-cargo production unaffected. Own projection slices state before any forecast; no foreign case/port/penalty data enters it. Include inspection-only/zero-colony forecasts.
- [ ] **GREEN:** Named tests/Suite; forecast calls preserve byte/reference/RNG equality and ready flags. Test foreign mutation equivalence, immunity/expired civic boundaries, arbitrary valid definition IDs and checked long-wide reductions. Purge/quarantine previews leave all state unchanged.
- [ ] **Commit:** feat: forecast crisis economics and owned response consequences; task-done.

## Task 5: Validate atomic crisis orders and full destructive consequences

**Files:** Create S/Commands/CrisisCommands.cs/CrisisCommandProcessor.cs, T/CrisisCommandTests.cs and S/Crises/CrisisLoss.cs; modify CommandProcessor.cs/CommandOutcome.cs/ExpansionCommands.cs and Development/DevelopmentStateValidator.cs.

**Interfaces:** CrisisCommand derives Command. Produce records: SetQuarantineCommand(EmpireId,int,QuarantinePreview Expected); SetCureShareCommand(EmpireId,int,long Episode,int Share); SetCrisisAuthorityCommand(EmpireId,int,CrisisAuthority,int Share); SetCrisisAcknowledgementCommand(EmpireId,int,bool Require); AcknowledgeCrisisCommand(EmpireId,int,ImmutableArray<CrisisReceipt> Expected); ResumeCrisisPolicyCommand(EmpireId,int,ImmutableArray<ColonyId> Colonies,bool Research); DecontaminateVesselCommand(EmpireId,int,VesselId,ImmutableArray<VesselOrderFact> ExpectedOrders); PurgeColonyCommand(EmpireId,int,PurgePreview Expected). VesselOrderFact is the T4 immutable own order snapshot. Route through existing partial CommandProcessor.ExecuteCrisis and its shared planning mutation path. T2 authority enum Manual/Contain/ContainAndResearch; AI forbidden from Purge command as well as from planning one. Produce internal CommandProcessor.ExecuteCrisisAutomatic(ContentSet,GameState,CrisisCommand)->CommandOutcome: allow only quarantine/share/decontamination, route the same validation/apply implementation with scoped automatic provenance, and do not pause manual policy channels. Public Execute remains manual; no public bypass or global flag.

- [ ] **RED:** PurgeUsesExactApprovedCosts, StaleCrossColonyCivicPreviewRejectsAtomically(Focus3), GroupedForeignAcknowledgementRejectsAll, and QuarantineCancelsOnlyDisplayedOwnOrders. Pin N5/S2/P2/R1/reserve901→N2/S2/P0/R0/reserve450/growth0/civic1 expiry turn+5. N1 removes colony; N2 retains1. Capture preview, purge a different own colony or change one pending transport order, submit stale preview: same state reference/bytes/Ready/RNG, no partial loss/ack. Named focused RED.
- [ ] **Implement:** Compare recomputed complete preview, including civic expiry, colony count and carrier/cancel facts; expected population/revision alone is insufficient. Batch validation before mutation, stable ID ordering; check allowed shares/owner/episode/current turn. Purge removes only that case, trims workers R→P→S, keeps survivor buildings/queue, records direct_order acknowledgement and civic cap/refresh; leaves other cases/carriers. Create shared CrisisLoss.Remove(GameState candidate,ColonyId colony,CrisisLocation location)->void: retain the certified own location, remove the colony, and preserve referenced pending/completed governor/crisis history. Under6 only, DevelopmentStateValidator permits a historical entry whose matching retired own metadata exists; still reject a foreign/live mismatched ID. Rules5 validation stays exact. T6 mortality reuses this helper. Authority/response override provenance must not pause ordinary governors. Waiver records dispositions; re-enable renews material attention. Real planning changes clear issuer Ready; no-op preserves exact reference. Add planning movement/founding restrictions for own closed population ports/holds only, preserving clear/cancel commands and old rules. Test a purged colony with pre-existing governor history round-trips through schema7 before T8 exists.
- [ ] **GREEN:** Focused(CrisisCommandTests), full Suite. Test overflow at episode/revision/expiry and mixed ownership, missing/stale target, copied foreign preview, dismissal/no command, empty/scout/foreign exclusions; no AI/automatic purge. Report/receipt creation must also roll back.
- [ ] **Commit:** feat: validate atomic crisis response commands; task-done.

T5 assertion contract on the specified N5/S2/P2/R1/reserve901 fixture:

```csharp
var preview = CrisisQueries.PreviewPurge(g.Content,g.State,owner,sourceId);
var result = CommandProcessor.Execute(g.Content,g.State,new PurgeColonyCommand(owner,g.State.Turn,preview));
Assert.True(result.Accepted);
Assert.Equal(2,result.State.FindColony(sourceId)!.Population);
Assert.Equal(450,result.State.FindColony(sourceId)!.ProductionReserve);
```

## Task 6: Implement deterministic onset, health and episode lifecycle

**Files:** Create S/Crises/CrisisResolutionContext.cs/CrisisProgression.cs/CrisisOnset.cs/CrisisReports.cs, T/CrisisProgressionTests.cs/CrisisOnsetTests.cs.

**Interfaces:** Produce shared context/Apply methods, immutable CrisisEvent(Key,Turn,Location,Kind,Revision,reason/cost/casualty fields) and CrisisExposure(owner,episode,destination colony,source vessel/location). Context owns ToTurn/closed-port/source facts/cured set/sterile set/exposure collection/report locations; final events are sorted/coalesced. CrisisReports.CloseFinishedEpisodes(ContentSet,GameState,CrisisResolutionContext)->void runs only after T7 exposures. No independently published intermediate state.

- [ ] **RED:** GraceAndSixIntervalCourse, NaturalCloseWaitsForDepartingCarrier, CurePrecedesN1Mortality, and SourceCostAndCooldownRemainFrozen. Pin grace2→1→Active20, no retrospective modifier; open severities20/40/60/80/100/100, losses on4/5 only, recovery on6; early quarantine20/25/30/35/40/45/no deaths; N1/severity80/age3 dies without completed cure and survives with it. Focused(CrisisProgressionTests|CrisisOnsetTests) RED.
- [ ] **Implement:** Exact health order, immunity until R+12, casualty trimming and zero-colony removal. Resolve cure from T4 allocation before natural/mortality; preserve snapshot penalties and existing tagged cargo unless cured. Onsets run only at ToTurn multiples20 after secondary exposure collection; eligible own sorted candidates, chance/selection draws exactly per spec, frozen600+100*N cost/local ID, no global ID draw. Final natural closure only when cases/carriers truly empty; reset share/discard partial progress/report/cooldown+30. Keep unresolved attention receipts separate from10-turn history. Waived resolved receipts do not accumulate indefinitely; active/receipt identities bounded by their actual saved entity/local counters, not an arbitrary low empire cap.
- [ ] **GREEN:** Named tests/Suite; pin probability19/target0 for seed42/owner1/three candidates; seed42/owner46 probability77 and no selection call. Test no pre20 checks, ineligible/Off/no-view draws, independent empire streams, no queued missed checks, recovery26→next check60, immunity expires38, field/counter overflow and sorted-state contract.
- [ ] **Commit:** feat: resolve plague onset and health deterministically; task-done.

T6 assertion contract:

```csharp
var g = CrisisFixtures.Warning(); var candidate = g.State.Clone();
var context = CrisisResolutionContext.Capture(g.Content,g.State);
CrisisProgression.Apply(g.Content,g.State,candidate,EconomyCalculator.Calculate(g.Content,g.State),context);
Assert.Equal(1,candidate.Colonies[0].Crisis!.Case!.GraceRemaining);
```

## Task 7: Implement cargo contact, frozen quarantine ports and founding provenance

**Files:** Create S/Crises/CrisisTransport.cs, T/CrisisTransportTests.cs; modify Movement/MovementResolver.cs, Colonization/ColonizationResolver.cs and Economy/VesselProduction.cs at minimal hooks.

**Interfaces:** Produce T7 hooks in the shared table. Existing MovementResolver.Apply/ColonizationResolver.Apply keep their signatures/callers via optional trailing CrisisResolutionContext? context=null or preserving overloads. Use actual vessel completion/arrival/founding facts, not textual event parsing or hidden routes. MovementResolver only records actual arrived vessel IDs in context; T8 calls HandleArrivals once afterward. ColonizationResolver calls TransferFounding exactly once before consuming a successful founder; T8 does not repeat that transfer. Consume the T4 PortClosed/IsHeld queries for shared command/planning use; do not invent a second port predicate.

- [ ] **RED:** ArrivalAtClosedPortGetsFullExtraInterval, CuredFlagCannotReappear, RecoveredSourceStillExportsAlreadyTaggedDeparture(Focus2), FoundingTransfersWarningBeforeVesselRemoval, and DeduplicatedContactsUseLowestSourceVessel. Explicit fixture: source Active/open with severity100/age5; cargo already pending departure; source naturally clears this boundary but departure flag remains, episode stays open, later eligible arrival gives Warning2. A completing cure instead clears/suppresses the tag. Named tests RED.
- [ ] **Implement:** Source snapshot flags at creation/departure; cured/sterile boundary sets suppress retagging. Arrivals check frozen closed ports and still-existing Active owned local sources after health; Warning→Active can confer an arrival contact only after its full two warning intervals, never through newly exposed cases created later. Thus departure sources are snapshot-based, arrival sources are post-health, and new exposure application is deferred. Closed arrival schedules due=ToTurn+1; existing holds due at this boundary clear flags/mark sterile before transport. Collect before creating cases. Transfer a consumed founder's flag/source metadata into a proposal for the new colony. Auto-release/death never changes frozen boundary ports. Commands must reject new held/closed cargo orders but always permit cancellation.
- [ ] **GREEN:** Named tests/Suite. Test warning source vs Active, two own colonies/one quarantined port, foreign/empty/scout exclusion, preventive quarantine, in-transit continuation, release without restoring cancelled orders, completed inspection followed by next-boundary recontamination, lost arrival source, immune destination, same-boundary cure/founding, source dying after embark, Off parity and original travel/cargo amounts.
- [ ] **Commit:** feat: contain and transmit plague through actual cargo; task-done.

T7 assertion contract after ordinary MovementResolver.Apply records the arrival in the defined CargoArrival(true) fixture:

```csharp
CrisisTransport.HandleArrivals(g.Content,g.State,candidate,context);
Assert.Equal(context.ToTurn+1,candidate.Vessels.Single().Crisis!.InspectionDueTurn);
Assert.Empty(context.Exposures);
```

## Task 8: Integrate the complete turn transaction and removed-colony-safe reports

**Files:** Modify S/Turns/TurnResolver.cs, Development/DevelopmentCompletion.cs/DevelopmentStateValidator.cs, Knowledge/EmpireViewProjector.cs/EmpireView.cs, Crises/CrisisReports.cs; create T/CrisisTurnTests.cs/CrisisReportTests.cs.

**Interfaces:** TurnReport gains optional crisis events and immutable colony report locations captured before health/removal. Existing constructor remains. ProjectReport uses captured own locations under6, retaining old fallback only for1..5; never FindColony(deletedId) to reconstruct resolved facts. Crisis context is created after AI planning/before economy; apply five exact spec phases using T4/T6/T7, only publish after final validation. Internal test hook overload receives CrisisResolutionPhase enum (Economy,Health,Transport,Finalize,Validation), Action<CrisisResolutionPhase> hook; production public Resolve uses no hook and existing rival injection seam remains intact.

- [ ] **RED:** CompletionThenColonyLossStillReportsExactlyOnce(Focus1); AggregateOneUnitCureSavesN1(Focus4); LateFailureRestoresWholeOriginal; NaturalClosureWaitsUntilContactEnds. Simultaneously complete a building/embark eligible cargo then lose source: report keeps correct original location/earned/spent/casualty facts, no null reference, no retained live queue for a deleted colony, no duplicate population allocation. One-unit final cure allocation prevents severity80/N1 mortality without erasing the interval's penalty. Named tests RED.
- [ ] **Implement:** Apply economy/completions first, attach birth flags, health/cure next, transport/exposure next, scheduled onset/closure/final receipts last. Finish development queue bookkeeping for surviving colonies only while historical pending governor records remain readable through LostColonyLocations. Trim retired locations only after referenced history/receipts/active sources are finalized; persist the exact bounded surviving set. Validate and present old governor entries after source loss rather than silently deleting approved history. Candidate source metadata serves lost-colony reports; mutable current UI selections must resolve deletion safely. Generic public failures reveal no foreign facts. Guard checked turn/global/local counters and all phase arithmetic inside rollback, including ID allocation and PRNG updates.
- [ ] **GREEN:** Named tests/Suite. Inject every late failure after real candidate mutations: exact input reference/canonical bytes/readiness/queues/RNG/counters/receipts. Multi-human prior planning survives final Ready failure. Project own report after colony loss and zero-colony empire; no foreign tombstone data. Same snapshot forecast matches applied economic allocations even when later health removes the entity.
- [ ] **Commit:** feat: publish crisis turns and loss-safe reports atomically; task-done.

T8 assertion contract on the specified late-failure fixture:

```csharp
Assert.False(resolution.Succeeded);
Assert.Same(original,resolution.State);
Assert.Equal(originalJson,SaveGame.Serialize(content,resolution.State));
Assert.Equal(originalRandomState,resolution.State.Random.State);
```

## Task 9: Add explicit authority, acknowledgement gate and fair AI preparation

**Files:** Create S/Crises/ICrisisPlanningContext.cs/CrisisPlanner.cs/CrisisTurnCoordinator.cs, T/CrisisPlanningTests.cs/CrisisReadyTests.cs/CrisisAiTests.cs; modify CommandProcessor.cs and Rivals/RivalTurnPlanner.cs/RivalPlanningQueries.cs/RivalVesselPlanner.cs/RivalColonyPlanner.cs.

**Interfaces:** ICrisisPlanningContext contains ContentSet Content, CrisisView View and int Turn/EmpireId Owner only, no GameState. CrisisPlanner.Plan(Func<ICrisisPlanningContext> current,Func<Command,bool> submit,bool ai)->CrisisPlanningResult(bool Succeeded,int CommandCount). CrisisTurnCoordinator.Prepare(ContentSet,GameState,EmpireId)->CrisisPreparationResult(GameState State,bool Succeeded); PrepareAi(ContentSet,GameState)->same result, invoking each own AI in ID order. CrisisQueries.PendingAttention from T4 is the gate. Separate C+V+4 accepted-operation crisis budget; preserve ordinary rival2*C+V+1 budget/allowlist. Submit automatic response commands via T5 ExecuteCrisisAutomatic; planner callbacks obtain fresh own queries after each accepted change. No global automatic mode flag or validation bypass.

- [ ] **RED:** AcknowledgementBlocksBeforeAnyPreparation(Focus5), AnotherReadyHumanIsUnchanged, ManualOverridesOnlyItsCrisisChannel, and AiQuarantinesWithoutHiddenAccess. Default exposure receipt blocks false→true Ready with exact original state; true→true no-op does not prepare; withdraw retains existing planning. Disable gate explicitly: dispositions Waived rather than Handled, reload exact; re-enable renews current material revisions. Named tests RED.
- [ ] **Implement:** Human Ready checks acknowledgement first, prepares crisis policy, then existing GovernorTurnCoordinator; final resolver wraps all effects and prior AI planning. Default Manual never acts. Contain quarantines current cases with case-bound release; ContainAndResearch also uses selected share unless paused. Resume is explicit. AI prepass uses own immutable context/normal commands with quarantine/50% research/decontamination and no purge/human governor; normal expansion consults port/block reasons and treats them as waits rather than repeatedly rejected commands. Zero-colony AI plans safely; no crisis path under1..5.
- [ ] **GREEN:** Named tests/Suite; Human/Human/AI, no-op/withdraw/retry, budget/foreign/stale/unexpected reject/faults, source input context mutation cannot leak into published state. Mutate hidden rival health/research/waiver/cargo: own plans identical. Genuine old AI traces unchanged. Policy actions and ack transactions are separately saved/audited.
- [ ] **Commit:** feat: coordinate crisis authority and emergency readiness; task-done.

T9 assertion contract on a valid human fixture with an unhandled exposure:

```csharp
var result = CommandProcessor.Execute(g.Content,g.State,new SetReadyCommand(owner,g.State.Turn,true));
Assert.False(result.Accepted);
Assert.Same(g.State,result.State);
Assert.False(result.State.FindEmpire(owner)!.Ready);
```

## Task 10: Present one crisis overview and explicit response confirmations

**Files:** Create game/Components/CrisisOverviewDialog.cs/CrisisResponseDialog.cs/CrisisAuthorityDialog.cs; modify Screens/GameScreen.cs, Components/ColonyList.cs/TurnReportDialog.cs/GovernorReportPanel.cs and the shared theme only if a genuinely missing variation is required; extend T/CrisisQueryTests.cs for display DTOs and introduce G/CrisisGraphicalValidation.cs/.tscn with smoke/response groups. Update G/Validation.targets/Run-Validation.ps1/Verify-NormalAssembly.ps1 to include Crisis and count **six** forbidden harness classes.

**Interfaces:** Overview.ShowCrisis(ContentSet,CrisisView) and response dialogs ShowPurge(PurgePreview)/ShowQuarantine(QuarantinePreview); emit event Action<Command> CommandRequested and location-navigation IDs through existing GameScreen execution path. No view reads GameState. Names: CrisisOverview, CrisisCaseList, CrisisResearchShare, CrisisQuarantinePreview, CrisisPurgePreview, CrisisConfirmPurge, CrisisAuthorityPicker. Semantic control text identifies source location/population/cost/carriers/civic effects; no style overrides. Preserve normal assembly exclusion and real opt-in negative control even after failed UI runs.

- [ ] **RED:** Actual labelled pre-session N5/reserve901 response fixture: preview says loss3/kept reserve4.5 displayed points (450 stored)/civic1/expiryT+5; cancel leaves canonical bytes unchanged. Change another case's civic/order facts using ordinary commands, then press old confirmation: stale rejection, no loss (Focus3). N1 confirmation states colony/assets/recovery consequences. Lost selected colony closes or navigates safely and report location remains visible (Focus1). Run new Runtime(Crisis,unique-red-folder); witness semantic missing-control/behavior RED.
- [ ] **Implement:** One overview with current cases/carriers, filter/select/batch normal response, read-only consequences, explicit individual destructive confirmation, Manual/Contain/ContainAndResearch authority/Resume. Use pure previews both individually/batch. Lists use own names/IDs with fallback; response refresh never issues a command. Resolve deleted own governor/report entries through retired own metadata with name/ID fallback and no live foreign lookup. Retain next-project and vessel navigation in ordinary completion reports without duplicate notices. At zero colonies give explicit surviving-founder/recovery information.
- [ ] **GREEN:** Full current Crisis scene plus Suite/Build/metadata. Actual controls at800/1920 with1/6/11/30 pre-session labelled cases; last rows/actions/confirm/close reachable. Inspect representative response/overview/loss PNGs. Canonical/Ready unchanged for opening/cancel/navigation; command effects match DTO economics. Settings/QA path injection avoids touching owner preferences.
- [ ] **Commit:** feat: expose scalable crisis responses and consequences; task-done.

T10 assertion contract after actual preview/cancel controls in the named graphical fixture:

```csharp
Check(Descendants<Label>(dialog).Any(l=>l.Text.Contains(Format.Points(450),StringComparison.Ordinal)),"Preview shows kept reserve4.5 points");
Check(before==Snapshot(Session(screen)),"Cancel preserves exact campaign");
```

## Task 11: Connect local notifications, saved acknowledgement and reload

**Files:** Create game/Presentation/CrisisNotificationPreferences.cs and Components/CrisisNotificationDialog.cs; modify GameScreen.cs/ColonyList.cs/TurnReportDialog.cs and G/CrisisGraphicalValidation.cs; add T/CrisisAttentionTests.cs if pure attention queries need further coverage.

**Interfaces:** CrisisNotificationPreferences provides Default Interrupt, category routine Summary/Attention/Off, versioned Load/Save with the existing local-settings approach and injectable GameScreen.CrisisPreferencesPath. Critical modes enum Interrupt/Attention/Digest; authoritative RequireAcknowledgement is always shown from CrisisView, changed only by SetCrisisAcknowledgementCommand. Named controls CrisisCriticalBadge, CrisisRequireAcknowledgement, CrisisAcknowledgeSelected, CrisisDisplayLevel, CrisisRoutineLevel.

- [ ] **RED:** Actual controls Interrupt→Attention→Digest plus routine Off still show Critical badge/endangered count/Ready reason. A grouped30-case acknowledgement uses exact displayed revisions once, preserves another human Ready, and no report-close/reopen/settings/reload acknowledges implicitly (Focus5). Save pending/resolved receipt older than10-turn history as an explicitly labelled valid fixture; load retains its blocker. Run Runtime(Crisis,new-red-folder) semantic RED.
- [ ] **Implement:** Open at most one overview for new material own revisions under Interrupt; persistent critical information under every level. Explain local display vs saved waiver and record waived provenance. Re-enable renews current facts, not a fabricated read. Keep current conditions independent of historical wording/actions. Save/load under normal UI restores cases/overrides/gates/receipts; no render callback acts for governors. Per-session popup tracking cannot erase saved attention. Ten-turn digests do not discard unresolved receipts.
- [ ] **GREEN:** Full Crisis scene/Suite/Build/metadata; actual Save/Load/next Ready, stale mixed-revision batch rejection, another-human isolation, active-to-resolved pending receipt, explicit waive/re-enable, cleared-critical badge only after facts resolve/handled. Inspect800/1920 captures; changing only local settings preserves canonical bytes/Ready/RNG.
- [ ] **Commit:** feat: surface and persist emergency attention correctly; task-done.

T11 assertion contract after actual Digest/routine-Off settings and a denied Ready:

```csharp
Check(Descendants<Label>(screen).Any(l=>l.Name=="CrisisCriticalBadge"&&l.Visible),"Digest retains Critical badge");
Check(Session(screen).State.Turn==turnBeforeReady,"Unacknowledged emergency blocks next turn");
```

## Task 12: Build reproducible generated, labelled and failure campaigns

**Files:** Extend G/CrisisGraphicalValidation.cs and T/CrisisIntegrationTests.cs; update G/README.md; retain generated provenance/QA paths/results in execution evidence. No second framework or product scenario scripting.

**Interfaces:** Standard existing results.json schema (passed/error/results/retainedQaSavePaths); generated provenance rows name source, species,seed,rivals,rules,initial-state hash, turns/actions/outcome. Optional extra provenance does not count as assertions. Pure scalar/failure/scale fixtures are explicitly labelled before session creation.

- [ ] **RED:** Four explicit6/Standard core-compatible generated states at turn0/seed42, all human species, one rival; source content is actual core files plus the exact plague fixture pending T13 activation. Load these untouched generated initial saves through actual UI, then use real controls. Require spontaneous human onset at20, two-warning timing, useful containment/cure, actual Save/Load and three future canonical turns equal to an uninterrupted session. No new state/resources/outcomes injected after start. Missing end-to-end behavior must fail semantically.
- [ ] **Implement:** All four campaigns explicitly enable ordinary Industrial governor/workforce/foundations and normal research using controls, then set crisis response when actual onset occurs. Seed42/owner1 roll19 guarantees eligible first check; select target from actual own candidates. At least one Harl generated neglect campaign remains open/no cure to show actual deterioration/deaths/natural recovery; at least one generated Harl cargo/founding campaign uses actual colony preparation, allowed cargo completion/departure after Active, and an owned destination/new colony to show real secondary Warning2. Keep source/cargo constraints/distance and genuine resource availability; maximum120 resolved turns for the new crisis campaigns. Preserve the prior Rival80-turn bound. If a campaign fails, fix the actual rule/strategy or document the real blockage, never grant live resources. Alternate UI seeded choices can be captured before the plan task starts, not fabricated after failure.
- [ ] **GREEN:** Focused(CrisisIntegrationTests)/Suite and full Runtime(Crisis,...). Add labelled rare cases for mortality/embark/cure/hold/last-colony/failure/multi-human/30-world scale that are not reached naturally. Record how each was produced. Every QA path exists; screenshots/actual controls include port waits, source loss, combined report, policy override and saved acknowledgement. Compare only ordinary subsequent commands to uninterrupted reference, not a regenerated expected state.
- [ ] **Commit:** test: exercise generated crisis response campaigns; task-done.

T12 assertion contract on each generated Standard campaign after onset and UI reload:

```csharp
Check(Session(screen).State.Turn==20,"Seeded eligible human onset is turn20");
Check(loadedCanonical==uninterruptedCanonical,"Actual UI load preserves future canonical state");
```

## Task 13: Activate complete core content and current rules6 setup

**Files:** Create content/core/plagues.json (same approved values as fixture); modify S/Rules.cs, Galaxy/GalaxyGenerator.cs, game/ContentFiles.cs/Screens/NewGameScreen.cs and G/CrisisGraphicalValidation.cs/ColonyGraphicalValidation.cs/RivalGraphicalValidation.cs/FrozenContent.cs; add current-start assertions in T/CrisisGenerationTests.cs/CrisisContentTests.cs.

**Interfaces:** Rules.CurrentVersion=6 only now; ContentFiles.LoadCore requires species/rivals/development/crises. NewGameScreen.Configure keeps old calls valid and adds trailing bool? crisesEnabled=null; actual Standard/Off picker persists into NewGameSettings and preview without consuming crisis draws. Add FrozenContent.Rules5() reading T1 embedded rules5-content.json; older frozen scenarios keep their intended content/rules. Generic old fixture helpers using implicit CurrentVersion must be pinned explicitly to their historical rules or clearly converted to current Off for the same ordinary behavior.

- [ ] **RED:** Actual New Game Start defaults6/Standard/One/Harl/Peldra and Manual crisis authority/default required ack; Off selection yields6/dormant streams. Current core must require/load exact ash_bloom parameters and equal the T12 fixture. Starting population/buildings/IDs/appearance/old RNG equal rules5. Previous runtime current-start assertions are updated to6 rather than weakening them. Named focused tests/current-start UI RED.
- [ ] **Implement:** Core values exactly CRI-001..003; no new tech/building/free resources. Run T12 generated Standard campaigns now from actual New Game Start rather than the pre-activation loaded turn0 source, maintaining the same provenance method. Existing Colony/Rival ordinary generated scenarios select Off using actual new setup controls, retaining their economic/AI purpose and accepted bound; separate genuine rules5 capture and frozen older scenes prove legacy behavior. Never run crises accidentally inside an unrelated legacy expectation or regenerate its expected numbers. Updated alternate-content Load recovery remains independent of invalid current disk definitions.
- [ ] **GREEN:** Focused(CrisisContentTests|CrisisGenerationTests|CrisisCompatibilityTests)/Suite/Build; Runtime Crisis, Colony and Rival current/setup paths. Verify source definition equality, base galaxy metadata/RNG parity and Rules6 Off equivalent economy. All direct/GUI old-campaign loads remain unchanged and omit crisis controls.
- [ ] **Commit:** feat: enable approved plague rules in new games; task-done.

T13 assertion contract after actual Standard/Off New Game controls:

```csharp
Check(Session(standardScreen).State.RulesVersion==6,"Actual current Start uses rules6");
Check(Session(standardScreen).State.Crisis!.Enabled,"Standard is explicit default");
Check(!Session(offScreen).State.Crisis!.Enabled,"Off setup remains dormant");
```

## Task 14: Verify the complete isolated implementation and reconcile acceptance

**Files:** reports/CRISIS_VALIDATION_2026-10-25.md, runtime/unit evidence, ledger/progress, PROJECT_STATUS.md and standard acceptance/roadmap/README/entry documents. The report filename identifies this planning milestone; its run timestamps/dates follow actual execution and must not be backdated. Spec and ADR retain their approval dates.

**Interfaces:** Consume six standard scenarios and every CRI test; machine summary derives counts from actual result classes, separates assertions/provenance/geometry and distinguishes frozen/generated/labelled evidence. No review/integration completion is hidden inside this task.

- [ ] **Run:** Fresh Suite; sequential Runtime Crisis, Colony, Rival, Species, Expansion, Legacy into reports/crisis-final-<actual-date>-runN; final ordinary Build and Metadata. Expected0 failures/skips/build errors/warnings/engine diagnostics, all actual QA paths and screenshots present, opt-in negative control detects all six real harness classes and ordinary build excludes six. Retain failures/retries rather than replacing evidence.
- [ ] **Inspect:** Representative800/1920 response/attention/30-world/removed-colony/cargo PNGs, reports/history/current wording, actual saves/provenance/canonical futures. Do not recount old gallery images or claim physical input/human balance from automated evidence. Match each CRI-AC-001..011 to exact tests/results; leave missing details Implemented, Unverified rather than infer full acceptance.
- [ ] **Reconcile:** All14 tasks complete on isolated branch, design AC012 already Verified; runtime criteria remain Implemented, Unverified pending final review/disposition. Parent17-row totals/REQ-017, deferred human/platform/security/delivery/release gates unchanged. Record any implementation rulings/costs/failures honestly and exact original owner preservation/seven worktrees. No cleanup/merge/push/release.
- [ ] **Commit:** docs: record isolated crisis validation and review readiness; task-done with fresh full Suite. Ensure all14 completion lines before whole-branch review.

## Post-plan review and handoff gates

- [ ] Generate a whole-branch review package from approved planning baseline to final task HEAD. ONE fresh read-only gpt-6-astra/xhigh reviewer, product/test/evidence passes, all five Review Focus lines, no reviewer subagents. No per-task reviewers in retained Native execution.
- [ ] Re-grade findings against player consequences and the approved spec. One regression-first correction pass for Important/Critical findings, affected gates/full Suite; record new Minor findings for owner disposition rather than silently expand scope. No automatic re-review. Never assert the later correction was independently re-reviewed.
- [ ] Reconcile child acceptance only after findings/verification support it; preserve separate human/runtime/platform limits. Present source SHA, results, all rulings/costs, remaining findings and exact preservation. Leave clean isolated branch ready for owner integration choice. No main integration/remote action/cleanup until separately authorized; old M5 integration authorization does not apply to M6.

## Plan self-review (2026-10-25)

- Spec coverage: all seven CRI contracts and12 criteria map to explicit tasks above; AC012 already fulfilled by full design approval. Core rollout, version changes and generated actual-start validation have explicit late owners.
- Shared-interface check: T2 state/content→T3 codecs and T4 queries/economy; T4 consequences→T5 commands; T6 transient lifecycle context→T7 transport hooks→T8 integration; T4/T5→T9 planners→T10/T11 UI; T12 generates against explicit6 before T13 turns it into current/default. No later task defines a prerequisite consumed by earlier code. T5 uses T4 port queries/preview-order facts before transport hooks; T7 completes their execution integration. Historical owned-location support is defined in T2, persisted T3, introduced with purge/validator in T5, reused by mortality T6 and finalized/projected T8; no deleted-colony validation gap is left for a later task.
- Phase check: begin-snapshot departures/ports vs surviving post-health arrival sources, cured/sterile suppression, final natural closure after exposures, removed source metadata and ordinary development completion are explicitly pinned; no intermediary state is published.
- Step scan: each task names files, exact produced/consumed APIs, literal assertions/fixtures, focused/full verification and a commit; functions' bodies remain implementation work. No new runtime code/scaffolding is created in this planning turn.
- Review Focus: F1→T8/T10;F2→T7/T8;F3→T5/T10;F4→T4/T8;F5→T9/T11, all with explicit tests. No empty assurance-only review item.
- Proportion: the plan selects file/API/test/dependency boundaries, not a second copy of the full program. Generic fixture construction/DTO layout and internal helper extraction remain ordinary builder work; gameplay values do not.
- Method/gates: retained Native is already chosen. Self-review is performed here, not delegated to another agent. Owner review of this separate plan is the next gate; no implementation has started.
