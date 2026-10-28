## Task 9 — Bounded preparation, standing orders and Ready

**Files:** new FleetPlanner/FleetTurnCoordinator; CommandProcessor, governor/scout coordinators and ready queries; `T/FleetReadyTests.cs`.

- [ ] Add `FleetReadyTests.PreparesEachHumanOnceAcrossReadyRetryAndDirectResolve`, `ExploreUsesOwnKnowledgeFiveTurnResurveyAndStableRanking`, `SafetyOverridesGoalsWithoutConsent`, `LateSecondHumanFailurePreservesFirstHumansPreparedOrders`. Explicitly cover Ready=true no-op, false→true retry,2 humans+AI, withdrawn Ready and stale exact member receipts after officer/reward changes.
- [ ] Run `Focused(FleetReadyTests)`; demonstrate duplicate binding or unsafe/order/readiness assertion RED.
- [ ] Wire the exact single preparation path above into both CommandProcessor and direct TurnResolver. PrepareRequests works only within accepted quota/priority/cargo/fleet reservations; PrepareOrders uses one proposal per member. Explore scout-only: unsurveyed then ≥5-turn resurvey, rank fleet duration/StarId, exclude own-known guarded/forbidden, no target→Hold once. Assemble/Return become Hold at completion; disappeared Return home blocks at stationary boundary. Respect active crisis holds/manual orders; scout preparation may separate incompatible cohorts through bounded ID order. Internal command origin avoids manual-takeover interception. Budget≤32*32 slots/empire and one movement proposal/member; never recursive Ready.
- [ ] Run `Focused(FleetReadyTests)`, `Suite`, `Build`; compare canonical bytes/reference, receipts, queue IDs and reports across repeated readiness and failure. No new Ready blocker for a paused request; ordinary encounter consent still blocks.
- [ ] Commit planner/coordinator and named Ready adapters/tests/evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetPlanner.cs src/OpenAntares.Simulation/Fleets/FleetTurnCoordinator.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs src/OpenAntares.Simulation/Development/GovernorTurnCoordinator.cs src/OpenAntares.Simulation/Scouting/ScoutTurnCoordinator.cs src/OpenAntares.Simulation/Scouting/ScoutReadyQueries.cs tests/OpenAntares.Simulation.Tests/FleetReadyTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: prepare fleet orders through shared readiness"
  ```
