## Task 14 — Existing rival planner adapter and generated invariants

**Files:** FleetInitializer/FleetTravel/FleetTurnCoordinator; RivalTurnPlanner/RivalVesselPlanner; `T/FleetAiTests.cs`, `FleetGenerationTests.cs`.

- [ ] Add `FleetAiTests.SingleExistingPlannerCommandsStartingNavigatorFleet`: same initialNavigator/XP/travel rules/costs, no extra order controller/recruitment/requests/free hulls/hidden knowledge. `LegacyRivalFuturesRemainExact` replays old sources. `FleetGenerationTests.AllSpeciesSizesModesPreserveGalaxyAndExistingRng` covers each4 species/core sizes, rivals0/1, plague Standard/Off and ameba Standard/Off with explicit rules8; declared alternate species too.
- [ ] Run both focused classes; require a real duplicate-order/knowledge/bonus/generation assertion RED.
- [ ] Assign AI's initial Navigator to its existing starting scout in a one-ship fleet through rules8 local state, consuming no global ID/RNG. Existing expansion_v1 decides routes once; adapter applies ordinary fleet travel without overwriting human standing orders or multiplying candidate budget. Subsequent AI ships remain independent; AI never Study/Remain or recruitment/requests. Use RecoverCommand/Wenth link and ordinary earnedXP. Old rules keep their original planner and exact budgets/futures.
- [ ] Run both focused classes, `Focused(FleetCompatibilityTests)`, `Suite`, `Build`; compare old-rule ship/global IDs and all RNG streams, and own-view paired-hidden-world AI decisions.
- [ ] Commit only named AI/init/travel adapters, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetInitializer.cs src/OpenAntares.Simulation/Fleets/FleetTravel.cs src/OpenAntares.Simulation/Fleets/FleetTurnCoordinator.cs src/OpenAntares.Simulation/Rivals/RivalTurnPlanner.cs src/OpenAntares.Simulation/Rivals/RivalVesselPlanner.cs tests/OpenAntares.Simulation.Tests/FleetAiTests.cs tests/OpenAntares.Simulation.Tests/FleetGenerationTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: adapt existing rival planner for admirals"
  ```
