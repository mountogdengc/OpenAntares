## Task 6 — Pure finite-request preview and deterministic estimates

**Files:** new IFleetPlanningContext/FleetPlanningQueries/AssemblyPlanner; FleetViews; GovernorPlanningQueries own-frame adapter; `T/FleetPlanningTests.cs`.

- [ ] Add `FleetPlanningTests.OffersIdleShipsThenRanksConditionalCompletionAndArrival`:3 colonies/4 scout slots, idle candidate acceptance explicit; finite completion+arrival wins over null estimate, ties ColonyId then slot. `UrgentPreviewRetainsBlockedActiveProjectAndRecomputesTail` asserts prior/proposed queues, aggregate cargo and per-colony sequential estimates after every assigned slot. `HiddenHabitatDoesNotChangePreviewButKnownUnsafeRallyRejects` covers privacy.
- [ ] Run `Focused(FleetPlanningTests)`; expect deterministic allocation/estimate/knowledge assertions RED.
- [ ] Implement context from own immutable facts only; reuse ColonyEconomy calculation through own forecast, including request-bound prices and declared species cargo limits. Estimate finite unstarted work sequentially with at least1 turn/project, active project first, fixed current output and labelled conditional forecasts; unknown/infinite estimates stay blocked. Preview reserves full composition/fleet capacity,32 queue bound/request bound/participating colonies and no colony/rally conflict; no ID/queue token/RNG allocation in preview. Include exact active/queue/project/reserve/assignment/knowledge/priority inputs in canonical fingerprint to reject stale acceptance. Copying a role string or accepting a matching old hull cannot silently authorize cargo.
- [ ] Run `Focused(FleetPlanningTests)`, `Suite`, `Build`; same preview repeated100 times leaves canonical state/streams/Ready unchanged; alternative content and zero-output estimates valid.
- [ ] Commit the listed context/planner/projection adapter files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/IFleetPlanningContext.cs src/OpenAntares.Simulation/Fleets/FleetPlanningQueries.cs src/OpenAntares.Simulation/Fleets/AssemblyPlanner.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Development/GovernorPlanningQueries.cs tests/OpenAntares.Simulation.Tests/FleetPlanningTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: preview finite assembly allocation"
  ```
