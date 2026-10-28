## Task 5 — Commissioning, physical appointments and editable doctrine

**Files:** new OfficerCommands/OfficerCommandProcessor; FleetState/FleetViews/CommandProcessor; `T/FleetOfficerTests.cs`.

- [ ] Add `FleetOfficerTests.CommissionStoresTurnTwelveDeadlineAndCountsTowardFour` (turn10 acceptance records deadline12), `CannotTeleportOrServeTwoAssignments`, `WenthOneAppointmentPerSourceBodyWithoutPopulationCost`, `DoctrineFromTurnZeroAndBulkIsAtomic`. Cover64 lifetime bound, dismissal finality/catalog uniqueness, all culture defaults and explicit future LastWatch inheritance without retroactive propagation. Actual turn11/12 countdown is Task12's resolver test, not a knowingly failing dependency left in this task's suite.
- [ ] Run `Focused(FleetOfficerTests)`; behavioral RED for appointment/cap/location/doctrine authority. Lifecycle deadlines resolve through OfficerRecovery in Task12; tests here cover command legality/deadline state, with full countdown test completed there.
- [ ] Implement exclusive status/assignment transitions, fixed specialty/candidate selection, co-located board/remove/transfer/post, future-default override and explicit bulk doctrine edit. A request coordinator must be Posted at a participating colony before allocation, remain there and serve one request; removal forfeits completion XP. Wenth record references its original colony body and source identity regardless of organ location. No population fee or random personality behavior. Commands reconcile actual affected ameba consequences without implicitly acknowledging them; extend the rules8 fingerprint with the appointed officer/doctrine/recovery consequence. Test changed doctrine invalidates a material receipt and an identical repeated preview preserves it.
- [ ] Run `Focused(FleetOfficerTests)`, `Suite`, `Build`; all rejected/duplicate/no-op edits leave Ready/state/streams exact and ordinary manual controls need no officer.
- [ ] Commit officer command files and only named shared adapters/tests/evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerCommands.cs src/OpenAntares.Simulation/Fleets/OfficerCommandProcessor.cs src/OpenAntares.Simulation/Fleets/FleetState.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Scouting/ScoutQueries.cs tests/OpenAntares.Simulation.Tests/FleetOfficerTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add officer appointments and editable doctrine"
  ```
