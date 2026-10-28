## Task 11 — Once-only XP, passive unlocks and bounded careers

**Files:** new OfficerProgression/FleetReports; FleetResolutionContext/FleetViews/TurnResolver/scout receipt consequences; `T/FleetProgressionTests.cs`.

- [ ] Add `FleetProgressionTests.SurveyPromotionUsesPreEventBonus` withXP8→10/research0 andXP28→30/research100; XP30 survey research200. `ConcurrentFirstSurveyCreditsLowestEligibleFlagshipOnce` and `FourNewHullsAndFourSingleRequestsBothAwardFour` include old hull reuse, split/cancel/missing requests, officer removal, unassigned prior survey and no waiting/resurvey farming. `StudyAddsEightXpAndOnlyOrdinarySixResearch` asserts600 internal research and unchanged75%/95% hull odds. Career bounds32 highlights/10 detailed turns/64 lifetime records retain totals/final summaries.
- [ ] Run `Focused(FleetProgressionTests)`; behavioral RED for duplicate/retroactive event award or wrong internal research units.
- [ ] Capture same first-survey event/eligible flagship recipient from snapshot; dedup per empire/system regardless of officer present, and empire/habitat for successful study. Deposit survey research after ordinary research/cure and existing study deposit; no extra same-turn completion/cure funding. Cap XP30, derive passive rank from thresholds; no respec/buttons. Award request1XP per original newly built hull included in completed quota only to initial still-assigned coordinator, once, never for existing hulls or blocked quota. Store immutable history facts for retired ships, aggregate counters checked, one event/promotion/report per actual outcome. Implement OfficerBonuses.SurveyResearch and extend the stable rules8 receipt fingerprint/display with actual reward/unlock consequences; speed bonuses do not alter hull odds. Repeated pure reads must preserve exact token equality.
- [ ] Run `Focused(FleetProgressionTests)`, `Focused(FleetReadyTests)`, `Suite`, `Build`; inject Rewards/Reports late failures and assert RNG/credit sets/history/other-human planning rollback. All literal2/4/8 event examples and snapshot semantics pass.
- [ ] Commit named progression/report/projection/receipt integration files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerProgression.cs src/OpenAntares.Simulation/Fleets/FleetReports.cs src/OpenAntares.Simulation/Fleets/OfficerBonuses.cs src/OpenAntares.Simulation/Fleets/FleetResolutionContext.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs src/OpenAntares.Simulation/Scouting/ScoutQueries.cs tests/OpenAntares.Simulation.Tests/FleetProgressionTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: record deduplicated officer progression"
  ```
