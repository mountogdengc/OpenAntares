## Task 10 — Resolve construction, cohort safety, arrivals and founding

**Files:** new FleetResolutionContext/FleetResolver; TurnResolver, MovementResolver, DevelopmentCompletion, scouting context/coordinator; `T/FleetResolutionTests.cs`.

- [ ] Add `FleetResolutionTests.CompletedHullWaitsUntilNextBoundaryAndBindsExactToken`, `UnknownRallyArrivalSafetyPausesBeforeCompletion`, `SafetySplitAtSixtyFourFleetsKeepsFlagshipCohort`, `FounderPostsOfficerWithoutLossOrChangingWenthSource`. Include retained already-launched transit, partial arrivals, blocked joins/full quota, destroyed flagship/survivor identity and empty fleet retirement.
- [ ] Run `Focused(FleetResolutionTests)`; require actual resolver RED showing same-turn dispatch, wrong binding/XP eligibility or incorrect founding/loss distinction.
- [ ] Add context/hook seam and integration phases exactly as mapped above. Bind new hull before queue consumption using snapshot colony/token/role/slot, then leave it independent and nonmoving until next preparation. Loss/safety separation retains compatible flagship cohort; other survivors become independent in VesselId order, never counted emergency fleets. AfterFounding posts the officer at the new colony with unchangedXP/source; no lost-ship/recovery event. Explicit request-founder detachment must precede founding. ObserveAssembly checks fresh known rally legality, pauses affected requests once, then joins legal co-located arrivals and completes only all Arrived/current full quota; completion Hold/report facts feed Task11. Missing or BlockedSafety never waived. Existing completed requests stay completed if later danger appears.
- [ ] Run `Focused(FleetResolutionTests)`, `Suite`, `Build`; force every currently integrated FleetResolutionPhase hook to throw and prove exact outer-state rollback/no report, including human1 already Ready. Preserve old scout/crisis hook coverage and cargo loss behavior.
- [ ] Commit named context/resolver/adapters/tests/evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetResolutionContext.cs src/OpenAntares.Simulation/Fleets/FleetResolver.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs src/OpenAntares.Simulation/Movement/MovementResolver.cs src/OpenAntares.Simulation/Development/DevelopmentCompletion.cs src/OpenAntares.Simulation/Scouting/AmebaResolutionContext.cs src/OpenAntares.Simulation/Scouting/ScoutTurnCoordinator.cs tests/OpenAntares.Simulation.Tests/FleetResolutionTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: resolve fleet assembly safety and founding"
  ```
