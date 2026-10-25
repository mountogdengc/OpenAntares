# OpenAntares acceptance criteria

Established contracts translated into repeatable checks on 2026-10-22. Requirement definitions/statuses live in [SPECIFICATIONS.md](SPECIFICATIONS.md); current execution results and limits live in [the validation report](../reports/VALIDATION_REPORT.md). IDs are stable. A procedure written here is not evidence that it ran.

## Automated baseline

For AC-001 through AC-009 and AC-012, preconditions are the checked-out source/core content, compatible .NET SDK/runtime, restored dependencies, and a successful build. Run from the repository root:

```powershell
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo
```

The adoption ran the complete suite, including the named checks below, and retained [TRX results](../reports/test-results/adoption-2026-10-22.trx). Tests involving saves use their own temporary directory, not player saves.

### AC-001 / REQ-001: New-game generation

- **Procedure:** Run `GalaxyGeneratorTests`, including `GeneratedGameFollowsTheGalaxyRules`, `StartsOneHumanEmpireWithTheStartingColony`, `SupportedSizesAlwaysHaveSpiralRegionsAndValidSpacing`, `CoreContentGeneratesEverySize`, and unknown-size/impossible-spacing cases.
- **Expected:** Exact configured system counts/bounds/spacing, ordered planet orbits, all three regions, one human empire/unready at turn 0, population 6 and 2/2/2 on `verdant_world` for core content. Same seed reproduces state; invalid input returns a generation error.
- **Method/evidence:** Unit/integration suite plus startup commands in the validation report. Headless startup alone does not assert star counts or visual appearance.

### AC-002 / REQ-002: Content validation

- **Procedure:** Run `ContentLoaderTests`, including the core fixture, split sections, regional overrides/fallbacks, duplicates, cycles, invalid values, and located error messages.
- **Expected:** Core content loads; valid split data round-trips. Unknown regions, no eligible planet type, duplicate IDs, invalid references/costs/limits/cycles reject with source locations. The specified prototype rules match content.
- **Method/evidence:** Unit/integration suite. Ordinary value-edit behavior is supported by loader tests; mod discovery/override UX is outside this criterion.

### AC-003 / REQ-003: State integrity and boundary

- **Procedure:** Run `ArchitectureTests.SimulationLibraryDoesNotReferenceGodot` and `GameStateValidatorTests`; inspect state/entity references and root `ProjectReference`/compile exclusions.
- **Expected:** No compiled Godot dependency in simulation. Valid starting/capacity states accepted; corrupt IDs, links, workforce, growth, targets, and metadata rejected. State references entities by IDs and survives serialization.
- **Method/evidence:** Automated checks and source inspection. This does not mechanically enforce every future UI caller's no-direct-mutation rule.

### AC-004 / REQ-004: Command validation and atomicity

- **Procedure:** Run `CommandProcessorTests`: stale/unknown/foreign/invalid commands, prerequisite/completed selection, clone publication, choice replacement/clearing, unchanged no-ops, readiness effects, and resolution rollback.
- **Expected:** Invalid intent returns the documented rejection code with unchanged state/readiness. Valid changes publish a new state, never spend reserves on selection, and clear readiness only for changed choices.
- **Method/evidence:** Unit/integration suite; no network or AI agent is exercised.

### AC-005 / REQ-005: Economy and forecasts

- **Procedure:** Run `EconomyTests`, including `BalancedDevelopment`, `AllocationTradeoff`, shortage/rounding, surplus caps, births/capacity, planet rates, overflow, and `ForecastEqualsResolution`.
- **Expected:** Balanced 6-population 2/2/2 fixture yields support 600/600, production/research 400 each and growth 10. At population 7 with 2/3/2, net production/research are 514/342, growth pauses and its fraction remains. New workers join support; effects follow snapshot timing; breakdown amounts sum to totals and division identities hold. Forecasting does not mutate state.
- **Method/evidence:** Exact-result unit/integration suite. Full example expectations remain in [the economy spec](FIRST_PLAYABLE_ECONOMY.md).

### AC-006 / REQ-006: Production accounting

- **Procedure:** Run balanced development, replacement/clearing conservation, reserve overflow/remainder, one-completion, and capacity-timing tests in `EconomyTests` plus project-selection cases in `CommandProcessorTests`.
- **Expected:** Selecting/switching spends zero. `ending = starting + earned - cost`; sufficient production completes only at resolution, once per colony/turn, clears the target, and banks remainder. Fabrication Hall completes on turn 3 from balanced zero reserves and contributes its +100 production on turn 3-to-4.
- **Method/evidence:** Unit/integration suite.

### AC-007 / REQ-007: Research accounting

- **Procedure:** Run `EmpiresOnlyReceiveTheirOwnColoniesResearch`, completion/conservation tests, research selection tests, and `TechnologyResearchedThisTurnUnlocksBuildingsOnlyFromTheNextPlanningPeriod`.
- **Expected:** Only owned colonies contribute; switching preserves reserves. Balanced Cultivation Methods completes on turn 3 with no remainder, clears once, and enables Cultivation Hub next planning period without itself adding support.
- **Method/evidence:** Unit/integration suite, including synthetic multiple empires.

### AC-008 / REQ-008: Readiness and turn publication

- **Procedure:** Run single-human/multiple-human readiness, withdrawal/no-op behavior, `FailedResolutionRollsBackTheReadinessCommand`, overflow, and load-readiness tests.
- **Expected:** All human empires must be ready; successful resolution increases turn once and resets readiness. Changed orders clear readiness, unchanged/rejected orders preserve it. Failure preserves state and the triggering empire's earlier readiness. Loading never resolves a turn.
- **Method/evidence:** Unit/integration suite.

### AC-009 / REQ-009: Persistence and failure paths

- **Procedure:** Run all `SaveGameTests`, including embedded content, RNG, readiness, canonical serialization, uninterrupted-vs-reloaded continuation, schema-1 fixture, invalid/incompatible input, failed file write, and failed session load.
- **Expected:** Complete state/content round-trip; continued states match. Schema 1/2 retains legacy rules/content/layout/planets/null metadata and resaves in schema4/rules1; rules2 campaigns retain vessels, orders, transit and surveys in schema4. Rules3 additionally preserves fixed species, physical environments and colony habitat modes. Explicit readers retain schema3 rules1/2; only the seven approved schema/rules pairs are accepted. Unsupported rules/schema reject clearly. Failed write/load preserves the prior save/session and leaves no temporary file after the covered cases.
- **Method/evidence:** Unit/integration/file tests. UI filenames, dialogs, overwrite prompts, and legacy UI loading require AC-011; not inferred from serializer tests.

### AC-012 / REQ-012: Local deterministic replay

- **Procedure:** Run `Pcg32Tests`, `SameStateAndCommandsGiveIdenticalResults`, `ManyTurnsAreDeterministic`, `SameSeedGivesTheSameGalaxy`, `BackgroundStarsAreStableAndDoNotAdvanceSimulationRandom`, and save continuation tests.
- **Expected:** Reference PCG sequence matches, repeat commands/seeds serialize identically, reload continuation matches, visual point generation leaves simulation PRNG untouched.
- **Method/evidence:** Unit/integration suite on the recorded Windows host. Cross-machine parity is AC-013.

## Graphical and manual acceptance

### AC-010 / REQ-010: Map, inspection, and resizing

- **Preconditions:** Godot 4.7.2 .NET graphical run, core content, unique QA save names, Small/Medium/Large seed 42 games.
- **Procedure:** Start each size through New Game. Count 12/16/20 selectable systems; inspect stars, planets, per-worker outputs, and region text. Select the home/another system; attempt to select dim background points. Exercise 800x600, 1152x648, 1600x900 and the current 1920x1080 default, including live resize. Save/reload and compare map framing; separately check a legacy layout if a safe fixture is available.
- **Expected:** Playable labels/home/selection are distinguishable and controls accessible; background points never select. Correct planets/rates/region text appear. Save/reload retains world framing, resize only rescales, and all-visible inspection remains the prototype policy.
- **Method/evidence:** Graphical/manual matrix with size/seed/window/evidence. Historical owner passes are in [the playtest record](playtests/first-playable-2026-10-19.md). The [2026-10-22 continuation](../reports/FIRST_PLAYABLE_VALIDATION_2026-10-22.md) adds fresh framebuffer, map-handler, live-resize and restored-bounds checks. Physical input and comfortable small-window readability remain unverified.

### AC-011 / REQ-011: Eleven actions, reports, picker regressions, and UI saves

- **Preconditions:** Same graphical environment; preserve existing saves. Follow [the existing hands-on run script](playtests/first-playable-2026-10-19.md).
- **Procedure:** For each size, inspect the colony, change assignments through one supported and one shortage case, record forecasts, choose Fabrication Hall/Cultivation Methods, advance through growth and completions plus a following turn. Save before/after completion under distinct QA names; load through the UI and compare turn, assignments, reserves, targets, completed items and framing; continue a turn. Exercise a corrupt save error without losing the session. Run the existing FP-001 and FP-002 steps with home/other-star selection and a side panel constrained to scroll; compare report-opened construction popup position to normal reopening and check research still opens. Verify actual default window dimensions graphically.
- **Expected:** All eleven charter actions pass. Forecast values and timing match reports, completions appear once and targets clear into banking. Construction follow-up reveals/focuses/opens the correct selector after scrolling, attached to that control; choosing a project uses commands. Reload restores gameplay, failed load leaves it intact. No requirement to persist presentation selection is invented. Legacy schema-1 UI loading is a separate explicit result/limit.
- **Method/evidence:** Reproducible graphical run record. The [retained runtime harness](../tests/OpenAntares.Game.Validation/README.md) and [final results](../reports/first-playable-2026-10-22-run4/results.json) now reproduce picker/focus-scroll regressions, save/load continuation, corrupt-save UI error, schema-1 dialog loading and default window checks through actual UI signals/handlers. Native popup positions match normal reopening in 24 cases; supplementary embedded fixtures supply menu images. This does not establish Windows pointer/keyboard delivery, readability or player comprehension. Keep complete acceptance unverified pending the human session.

### AC-013 / REQ-013: Target-platform parity

- **Preconditions:** Defined supported Windows/Linux/macOS configurations, required .NET/Godot toolchains and representative architectures. Packaging and support matrix still need definition.
- **Procedure:** Build/test each target; run matching core content, seeds/settings/orders, save/reload continuation, and compare canonical serialized authoritative results across machines. Exercise exported startup/content/save workflow once export policy is settled.
- **Expected:** Supported configurations run successfully and identical inputs yield identical authoritative results. No hidden platform dependency or changed rule outcome.
- **Method/evidence:** Per-platform build/test/interaction logs and compared outputs. Not executed beyond local Windows build/tests; delivery/performance thresholds remain incomplete specifications.

### AC-014 / REQ-014: Understanding and closure

- **Preconditions:** AC-010/011 functional checks and an uncoached participant unfamiliar with source. Use the approved validation plan tasks 3-5.
- **Procedure:** Observe from new game through save/load. Before/after turns record the participant's own explanations of support, growth, output, and completion timing; ask for a choice between two allocations and two goals. Record repeated clicks, confusing labels, and whether goals change their preference. Triage blocker/clarity/later findings and retest corrections. Record an explicit closed/still-open decision.
- **Expected:** Evidence supports explaining consequences and defensible tradeoffs; no unresolved blocker/clarity finding remains for closure. If evidence is insufficient or confusing, retain still-open status and list concrete follow-up. This is a review gate, not an invented numerical enjoyment threshold.
- **Method/evidence:** Anonymized observation and owner milestone decision. Use [the observer sheet](playtests/first-playable-observer-sheet.md). Observations are not yet supplied; this cannot be automated by reading code or dispatching control signals.

**Disposition, 2026-10-22:** Deferred by explicit owner instruction, [ADR-005](DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md). The session is not a pass, but its absence no longer prevents exploration/colonization design. Owner functional feedback remains useful and is not automatically relabeled an uncoached session.

## Deferred design-readiness gates

AC-015 records approved expansion and executed child criteria. AC-016 has approved design/plan gates and executed local automated species acceptance; AC-017 remains In Progress for the broader strategic envelope; all twelve bounded rival child criteria and M4-I are Verified for local automated acceptance; remaining strategic systems stay Deferred. Human/release acceptance is recorded separately.

### AC-015 / REQ-015: Exploration/colonization specification gate

Design review was satisfied by ADR-006; the owner also approved the separate plan and Native execution. EXP-AC-001..008 now have executed [simulation/runtime evidence](../reports/EXPANSION_VALIDATION_2026-10-22.md): 281 simulation tests, 275 expansion assertions and 466 legacy assertions, with explicit method/limits. REQ-015 is Verified for the executed automated Windows contract after [independent review and corrections](../reports/EXPANSION_REVIEW_2026-10-23.md); owner playtest deferral and separate full human/platform acceptance are unchanged. Future acceptance must preserve genuine old campaigns, hidden-knowledge filtering and snapshot/population accounting.

### AC-016 / REQ-016: Species mechanics acceptance

The owner approved the complete [species specification](superpowers/specs/2026-10-23-species-mechanics-design.md) and then the separate ten-task plan with the retained Native preference. ADR-008 satisfies SPC-AC-010, the design gate. [Species validation](../reports/SPECIES_VALIDATION_2026-10-23.md) maps SPC-AC-001..009 to exact profiles, fixed-point economy/growth, allowed cargo/snapshot eligibility, Sulan habitats, Wenth maturity/fission, generation/RNG, compatibility/failure atomicity and integrated UI campaigns. Twelve generated species/size loops plus forced harbor and labelled supplemental fixtures passed; all three regression scenarios are retained. Final independent review is in [the handoff](../reports/SPECIES_FINAL_REVIEW_2026-10-23.md).

These local automated checks do not establish balance, comfortable800x600 reading, physical OS input, uncoached comprehension, cross-platform equality or release packaging. The owner prose files remain unchanged. REQ-014 remains Deferred and REQ-013 remains Implemented, Unverified.

### AC-017 / REQ-017: Later strategic milestone gate

Given the charter's version 1.0 envelope, split the chosen next subsystem into stable child requirements, dependencies, state/command/data/save contracts, normal/failure cases, and observable acceptance procedures before coding. Expected: approved bounded milestone; AI/network/combat scope is never inferred from architecture hooks. Method: planning/owner review; In Progress for the bounded rival slice. RIV-AC-001..012 are child criteria in the [approved design](superpowers/specs/2026-10-24-rival-expansion-design.md); RIV-AC-012 is satisfied by full owner approval in [ADR-010](DECISIONS/ADR-010-approved-rival-expansion-design.md). The separate [implementation plan](superpowers/plans/2026-10-24-rival-expansion.md) was approved for Native execution. [Rival evidence](../reports/RIVAL_VALIDATION_2026-10-24.md) maps executed child criteria; final regression/review is complete. The original Important correction and both later Minor details are regression-verified; RIV-AC-001/008 and all other child criteria have executed local automated evidence. [Follow-up review and closure](../reports/RIVAL_POLISH_2026-10-24.md) records648 simulation and all four runtime gates with no new review findings. Broader REQ-017 stays In Progress; human/platform/release limits remain open.


### Approved colony-development criteria / REQ-017

[COL-AC-001..012](superpowers/specs/2026-10-24-colony-development-design.md#10-acceptance-and-review-gates) are the approved procedures. COL-AC-012 is Verified for full design approval in [ADR-012](DECISIONS/ADR-012-approved-colony-development-design.md); COL-AC-002/007 are Implemented, Unverified for the three disclosed presentation Minors; other COL functional criteria are Verified for bounded local automated scope, with passing [mapped evidence](../reports/COLONY_VALIDATION_2026-10-24.md). All twelve approved Native tasks are implemented;984 simulation tests and all five fresh main graphical gates pass. M5-P Verified, M5-I Implemented, Unverified for those details; local integration and fresh combined verification complete. Parent counts and human/release limits are unchanged.

Current M5 review disposition (2026-10-24): all twelve tasks complete; one fresh whole-branch review and one regression-first Important correction are resolved at9db9933. Suite984/984 and affected Colony593 assertions+4 provenance pass with0 diagnostics/normal build0 warnings/errors/five exclusions. Three Minors remain deferred: historical condition wording, template-deletion affected-colony information, and routine Attention distinction. Therefore COL-AC-002/007 and M5-I remain Implemented, Unverified for exact full acceptance; other functional COL criteria are Verified for local automated scope, design COL-AC-012 already Verified. Parent REQ-017/counts and human/release limits unchanged. See reports/COLONY_FINAL_REVIEW_2026-10-24.md and PROJECT_STATUS.md. Retained authorized local integration is complete; fresh combined984-test Suite/all five graphical gates/build/metadata pass; no new scope or cleanup.

Current authorized correction (2026-10-24): owner requested fixing all three deferred presentation details, verification and integration. All three are implemented at940fa73, Suite988/988 and affected Colony663 assertions+4 provenance pass. COL-AC-002/007 and M5-I remain Implemented, Unverified pending the fresh complete branch gate/review; their earlier deferral is historical. See reports/COLONY_POLISH_2026-10-24.md and current PROJECT_STATUS. Retain Native and exact owner preservation; no new feature/release scope.
