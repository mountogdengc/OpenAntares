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

[COL-AC-001..012](superpowers/specs/2026-10-24-colony-development-design.md#10-acceptance-and-review-gates) are the approved procedures. COL-AC-012 is Verified for full design approval in [ADR-012](DECISIONS/ADR-012-approved-colony-development-design.md); all COL functional criteria are Verified for local automated scope after authorized corrections, with passing [mapped evidence](../reports/COLONY_VALIDATION_2026-10-24.md). All twelve approved Native tasks are implemented;988 simulation tests and the full post-correction branch gates plus fresh affected main gates pass. M5-P and M5-I Verified for the local automated contract; local integration and fresh combined verification complete. Parent counts and human/release limits are unchanged.

Historical M5 review: reports/COLONY_FINAL_REVIEW_2026-10-24.md records the original findings and initial deferrals; current correction closure follows below.

Historical M5 correction closure (2026-10-24): all three authorized presentation fixes and the fresh review duplicate-vessel correction are verified and locally integrated. Functional source4a2e430 passes988 simulation tests; Colony665 assertions+4 provenance; Rival1146+12 provenance; Species1194; Expansion275; Legacy466+27 geometry. Fresh main Suite/Colony/normal rebuild/five harness exclusions pass separately. All twelve COL child criteria and M5-I are Verified for local automated/design scope. Parent REQ-017 and17-row totals remain12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred. See PROJECT_STATUS.md and reports/COLONY_POLISH_INTEGRATION_2026-10-24.md. Human/platform/delivery/release limits remain open. Next is bounded milestone selection/specification; no automatic implementation or cleanup.


### Approved colony-crisis/plague criteria / REQ-017

[CRI-AC-001..012](superpowers/specs/2026-10-24-colony-crises-plague-design.md#10-acceptance-and-review-gates) are the approved procedures. CRI-AC-012 is Verified for full owner design approval in [ADR-014](DECISIONS/ADR-014-approved-colony-crisis-plague-design.md). CRI-AC-001..011 are Verified for local automated scope after independent whole-branch review, one regression-first correction pass and fresh merged-main verification; no second review was commissioned. The [14-task plan](superpowers/plans/2026-10-25-colony-crises-plague.md), [criterion-by-criterion evidence map](../reports/CRISIS_VALIDATION_2026-10-25.md), and [review dispositions/fresh correction gates](../reports/CRISIS_REVIEW_CORRECTIONS_2026-10-25.md) record the technical basis and limits. The [merged-main integration report](../reports/CRISIS_INTEGRATION_2026-10-25.md) records the fresh suite, six graphical scenarios and preservation checks. M5/parent statuses and human/release limits remain unchanged.

### Approved scout criteria / REQ-017 — functional checks not executed

[SCT-AC-001..015](superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md#9-acceptance-and-review-gates) are approved in [ADR-016](DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md). SCT-AC-012/M7-D Verified for full design approval only; functional SCT-AC-001..011/013..015 Not Started. The [15-task implementation plan](superpowers/plans/2026-10-25-scout-survival-and-ameba.md) maps every criterion, with M7-P In Progress for owner plan review. Arithmetic/reference/document checks are planning evidence, not implemented hazard/balance/human/release acceptance. Existing parent statuses/M6 evidence remain unchanged.

<!-- M7 current acceptance -->
## Current M7 implementation and acceptance — 2026-10-25

Owner approved the full amended ADR-016 design and separate15-task plan; earlier drafting/plan-pending statements above are historical. M7-D/M7-P Verified; M7-I **Implemented, Unverified for exact full acceptance; review M1 deferred** on isolated codex/scout-survival. SCT-AC-012 is design-only Verified; functional SCT-AC-001..011/013..015 have the executed criterion mapping in [validation/handoff](../reports/SCOUT_VALIDATION_2026-10-25.md), with review status: Complete; no open Critical or Important findings. Branch current rules7/writer8/29 pairs; primary main42e1282 remains rules6/writer7. Actual suite 1398; all7 fresh native scenarios, zero diagnostics and ordinary7/helper2 exclusions are recorded, with genuine older futures and generated/labelled provenance. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. No integration/push/cleanup; owner chooses next disposition.

<!-- Scout M1 closure2026-10-26 -->
## Scout M1 follow-up closure — 2026-10-26

The remaining omitted-habitat setup explanation is corrected on codex/scout-polish, validated source907207f, with actual native RED→GREEN and five custom-map boundaries. [Follow-up evidence](../reports/SCOUT_POLISH_2026-10-26.md): Scout237, full Suite1398/1398,0 diagnostics, ordinary build0 warnings/errors and actual7-scenario/2-helper exclusions. SCT-AC-013/M7-I are now Verified for their local automated scope; earlier M1 deferral statements are historical. No known review finding remains open. Other six native scenarios retain their prior-source evidence; no second independent review or fresh other-six run is claimed. Primary main42e1282/four owner edits/seven worktrees/old evidence remain preserved; no integration/push/release/cleanup. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. Next is owner disposition of the completed follow-up branch.


## Scout local main integration — 2026-10-27

The owner's “merge that” authorized local integration of the completed scout/space-ameba work and final M1 follow-up. Main now contains validated source `a52dfc61c9a16ec10e0091b52783029afac73e38`: rules7/writer8/29 pairs. [Fresh merged-main evidence](../reports/SCOUT_INTEGRATION_2026-10-27.md) passes1398/1398 simulation tests and all seven native scenarios/4491 records (Scout 237; Legacy 493; Expansion 275; Species 1194; Rival 1158; Colony 670; Crisis 464), with0 diagnostics, normal builds0 warnings/errors and actual7-scenario/2-helper exclusions. SCT functional criteria/M7-I remain Verified for local automated scope; prior pending-integration statements are historical. Checkout preservation is verified425/425; original owner edits remain uncommitted and all branches/seven worktrees/recovery/QA remain retained. No new review, push, release or cleanup. REQ-017/17-row totals and human/platform/security/export/release limits remain unchanged. Next is bounded milestone selection/specification before implementation.


## M8 Fleet Command and named admirals — design approved, plan review pending

Owner “0approved” accepts the complete [design and starter defaults](superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md) in [ADR-018](DECISIONS/ADR-018-approved-fleet-command-and-admirals-design.md). M8-D/FLT-AC-012 Verified for full design only. The separate [17-task plan](superpowers/plans/2026-10-27-fleet-command-and-admirals.md) is drafted/self-reviewed; M8-P In Progress pending owner review; M8-I/functional FLT-AC-001..011 Not Started. Native inline/no per-task agents/one final fresh whole-branch implementation reviewer remains chosen. No product code/content/default/schema or primary-main change, new baseline capture or fleet gameplay run occurred. Main rules7/writer8/29 pairs, parent REQ-017/17-row totals (12 Verified,3 Implemented, Unverified,1 In Progress,1 Deferred), M7/M6/human/platform/security/export/release limits and every owner file/worktree/branch/QA/recovery remain unchanged. No main integration/push/release/cleanup is inferred.

### Historical scoped decisions before complete approval

The chronological notes below retain their original checkpoint status; ADR-018 and the current plan-review gate supersede their earlier pending-design language.


M8 scoped decision2026-10-27: the owner confirms doctrine editable from the start and research improving recovery equipment (ADR-017/FLT-006). No research gate for doctrine changes remains open. Exact equipment/bonus/XP/culture values and the rest of the full M8 design remain pending gameplay review; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Documentation only; current main/product and owner edits remain unchanged.


M8 scoped owner choice2026-10-27: Navigator, Surveyor and Organizer; promotions at10/30 XP. The old Recovery specialist, ameba-only Surveyor proposal and20/60 thresholds are superseded in the active specification; historical review/check artifacts remain intact. Exact bonus magnitudes/event XP/proposed XP cap, Organizer accounting, recovery values/cultural defaults and full M8 acceptance remain pending review. Doctrine remains editable from the start; research improves recovery equipment. M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. No code/main/owner-file change.


M8 scoped owner decision2026-10-27:50% officer recovery,70% with recovery research and a two-turn successful-recovery delay are confirmed. Current review topic is cultural defaults; do not treat provisional Harl Last watch as approved. Other unreviewed rules/values and full M8 acceptance remain pending; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Main and four owner files remain unchanged.


M8 cultural-default decision2026-10-27: all four cultures begin recovery-focused; Wenth present Re-establish command link. Last watch is manual-only; the original Harl default is superseded. Do not reopen settled doctrine/research, specialties/10-30 XP,50-70% recovery/two-turn delay or cultural defaults. Use the specification's remaining-review checklist for unconfirmed details. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees/evidence remain preserved.


M8 bonus/XP decision2026-10-27: Navigator5/10%, Surveyor1/2 research per first survey, Organizer5/10% scoped request-ship cost reduction and2/1/8 XP awards are owner-confirmed; do not re-ask. Current review is fleet/construction operations, with other unconfirmed details/full design approval still pending. Preserve docs/design-notes/2026-10-27-combat-retreat-morale-and-damaged-ships.md as future owner direction (degrading capabilities, retreat/morale, limping home/detachment/hit-and-run, automation with possible manual control), not M8 damage/combat scope. Plan/implementation remain Not Started; main/four owner files/worktrees/evidence unchanged.


M8 fleet/construction review2026-10-27: owner retains the reviewed proposal and Normal/Urgent request priority. Normal preserves explicit queues; Urgent previews moving before unstarted work, never silently replacing the active project. Preserve finite authority, priority-aware estimates, tokens/reserve/manual control and current combat-note future-only scope. Next review: commissioning/Wenth details/recovery technology and other unconfirmed details, then full M8 approval before separate planning. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees unchanged.


M8 availability decision2026-10-27: starting Navigator with Surveyor/Organizer candidates,2-turn commissioning,4-active-admiral limit and co-located ship-based transfers confirmed; fleet controls remain usable without an officer and Wenth availability stays colony-body based. Do not re-ask those choices. The64-lifetime-commission bound remains a proposal. Next review covers Wenth link/equipment recovery, technology and remaining draft details before full M8 approval/separate plan. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner edits/worktrees unchanged.


M8 final scoped choices2026-10-27: Wenth recovery2/1 turns (basic/researched), manual Last watch4/3, and Command Recovery24 displayed research after Industrial Coordination confirmed. Read reports/FLEET_COMMAND_GAMEPLAY_REVIEW_2026-10-27.md and the complete specification for remaining starter defaults and full-design approval; do not re-ask settled gameplay numbers. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started until their gates. Preserve main/four owner files, seven worktrees/branches and evidence; future combat direction remains a note.


Current M8 implementation authorization2026-10-27: owner “approved” accepts the separate17-task plan at7d175ad. M8-P Verified; M8-I In Progress on isolated codex/fleet-command. Native inline continuously executes all17 tasks, no per-task agents and one final fresh whole-branch implementation reviewer. Follow .superpowers/sdd/2026-10-27-fleet-command-and-admirals/progress.md; completed tasks must not repeat. Task1 must freeze genuine rules7 futures before any product/content change. Fresh unchanged baseline Suite1398/1398 passes. Full gameplay approval remains ADR-018; earlier planning-only restrictions are historical. Main c554797/four owner files/seven worktrees/all old QA/recovery remain preserved. No main integration/push/release/cleanup authority.


Current M8 combined-validation checkpoint2026-10-28: tasks1..16 formally complete on isolated codex/fleet-command; Task17 combined Suite1529/1529, build0 warnings/errors and all8 native scenarios pass, with actual opt-in negative control/ordinary8+2 exclusions. Current branch rules8/writer9/37 pairs; primary main c554797 remains7/8/29. Read reports/FLEET_COMMAND_VALIDATION_2026-10-27.md and reports/fleet-execution/final-summary.json. Actual generated3col/4scout/all-species Main SaveLoad/futures and separately labelled rare/scale/paging evidence retained. M8-I In Progress and FLT functional criteria Implemented, Unverified pending the ONE fresh whole-branch review, not yet dispatched at this checkpoint. Native retained/no per-task or second reviewers; no main integration/push/release/cleanup authority. All4 owner files/main index/all7worktrees/branches/raw QA retained;2431 actual-checkout hashes verified. Earlier planning/implementation-start notes are historical; trust this plan's ledger and do not repeat completed work. Future combat note remains outside M8; parent counts/human/release limits unchanged.


M8 final implementation/review checkpoint2026-10-28: approved17tasks complete on isolated codex/fleet-command, validated source36e6ec4. ONE fresh fullbranch review c554797..2464d1f and ONE regression-first5-finding correction pass done; no openCritical/Important/no second review. Corrected Suite1545/1545, focused compatibility/rollback73/73, Fleet1482+33/Scout237/Crisis464/Rival1158 native fresh, unchanged earlier Legacy/Expansion/Species/Colony evidence retained,0diagnostics/build0/actual8+2 exclusions. Branch8/writer9/37pairs; primary mainc554797 stays7/8/29. M8-I and FLT-AC-001/005/011 remain Implemented, Unverified for exact acceptance: rolefilter missing, lost-Return Attention missing, separate idle-candidate list unpaged. Other8functionalcriteria Verified localautomated,012design Verified, M8-D/P Verified. Reports/FLEET_COMMAND_VALIDATION_2026-10-27.md, fleet-execution/final-summary.json/final-review.md/progress ledger are authoritative. All4ownerfiles/mainindex/7worktrees/branches/stashes/oldQA/futures preserved,2431index-checkout hashes and closing recheck pass. Futurecombat remains note. Parent17-row counts/human/platform/security/export/release limits unchanged. No merge/push/release/cleanup authority. Next owner disposition of isolatedbranch/threeMinors; do not repeat tasks/reopen design/method/review. Earlier pending checkpoints are historical.


M8 final exact-acceptance disposition (localautomated/design scope):

| Criterion | Status |
| --- | --- |
| FLT-AC-001 | Verified |
| FLT-AC-002 | Verified |
| FLT-AC-003 | Verified |
| FLT-AC-004 | Verified |
| FLT-AC-005 | Verified |
| FLT-AC-006 | Verified |
| FLT-AC-007 | Verified |
| FLT-AC-008 | Verified |
| FLT-AC-009 | Verified |
| FLT-AC-010 | Verified |
| FLT-AC-011 | Verified |
| FLT-AC-012 | Verified |

See the final validation report for the original checkpoint and the2026-10-28 follow-up report for closure of all3minor exceptions; parent requirement counts are unchanged.

M8 remaining-gap closure2026-10-28: owner explicitly requested fixing all3 remaining Minors before merge. Source `035877a40d45b39b1e764980e386023f706dd1dd` closes M1 lost-Return Attention (including persisted reason/report/badge with routineOff), M3 owned-role filtering, M4 bounded32-row assembly idle paging with explicit cross-page selections/stale-removal disclosure. FullSuite1547/1547, fresh Fleet 1666 result records + 39 separate provenance entries, Scout 237 result records + 9 separate provenance entries, Crisis 464 result records (including inline provenance); normalbuild0warnings/errors/actual8+2exclusions. M8-I/FLT-AC-001/005/011 now Verified for localautomated scope; all12FLT criteria Verified within automated/design limits. Read reports/FLEET_COMMAND_FOLLOWUP_2026-10-28.md and fleet-execution/minor-summary.json; earlier3-Minor deferrals/final-summary are historical. Original17tasks stay complete; no second reviewer. Mainc554797/four ownerfiles/index/all7worktrees/unrelatedrefs/6053 oldreports/346 recordedQA saves preserved. Branch remains unmerged; no push/release/cleanup or new subsystem. Parentcounts/human/platform/security/export/balance limits unchanged. Next: local integration when requested.

M8 local integration2026-10-28: owner “merge” explicitly authorizes local main integration of codex/fleet-command `6bcdc2dac0a09ecb5de9289af110ef20a19f8700` with committed owner documentation `a58cc671bdfbba9c59e0340613f9f04410c65197`. Product source035877a; all3 remaining findings are closed, M8-I/all12FLT criteria Verified within local automated/design scope. Fresh combined main Suite1547/1547, Fleet1666checks+39separateprovenance,0engine diagnostics, ordinary rebuild0warnings/errors and actual8+2exclusions pass. Only PROJECT_STATUS conflicted: latest fleet status and exact peaceful-mode paragraph retained; other3ownerfiles exact. Main now rules8/writer9/37pairs; old rules1..7 retain embedded behavior. Read reports/FLEET_INTEGRATION_2026-10-28.md and fleet-integration-20261028/summary.json. Prior branch-only/uncommitted-owner checkpoint wording is historical; those4files were committed separately ata58cc67 before merge. All7worktrees/unrelatedrefs/fleetbranch/recovery/oldreports/QA/futures retained, recoverybranch codex/fleet-premerge-20261028. Single review/Native preference retained; no second review or repeated completed tasks. Parent17-row counts/human/platform/security/export/balance limits unchanged. No push/release/cleanup or new subsystem. Next: choose the next bounded milestone before design/implementation.


## M9 combat design criteria

The [proposed CBT acceptance table](superpowers/specs/2026-10-28-fleet-combat-damage-and-withdrawal-design.md#acceptance-criteria-and-review-gates) defines CBT-AC-001..012 under REQ-017. CBT-AC-001..011 are Not Started; CBT-AC-012 is In Progress for owner gameplay review after the [fresh builder check](../reports/FLEET_COMBAT_DESIGN_REVIEW_2026-10-28.md). A criterion written here is not an executed test or an approved mechanic. M8 acceptance and parent totals remain unchanged.


M9 design approval2026-10-28 supersedes the earlier drafting status: CBT-AC-012 Verified for full design in [ADR-020](DECISIONS/ADR-020-approved-fleet-combat-design.md). CBT-AC-001..011 remain Not Started. The [implementation plan](superpowers/plans/2026-10-28-fleet-combat-damage-and-withdrawal.md) and [self-review](../reports/FLEET_COMBAT_IMPLEMENTATION_PLAN_REVIEW_2026-10-28.md) do not claim executed combat tests.


M9 implementation checkpoint2026-10-28: the owner approved the separate16-task plan; M9-P Verified. Tasks1..15 are complete on isolated codex/fleet-combat, rules9/writer10/46 pairs. M9-I and functional CBT-AC-001..011 are Implemented, Unverified pending task16 combined native gates and the ONE fresh whole-branch review. FullSuite1688/1688 includes exact compatibility/frozen futures and alternate embedded-role continuation. CBT-AC-012 remains design-only Verified in ADR-020. See reports/COMBAT_VALIDATION_2026-10-28.md; all earlier planning-only checkpoints are historical. Parent17-row totals and human/platform/security/export/release limits unchanged. Main and all owner prose/evidence/worktrees retained; no integration/push/cleanup.
