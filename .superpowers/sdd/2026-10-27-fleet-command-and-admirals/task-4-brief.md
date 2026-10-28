## Task 4 — Direct fleet commands and committed group travel

**Files:** new FleetCommands/FleetCommandProcessor/FleetTravel/OfficerBonuses (EffectiveSpeed only); CommandProcessor/ExpansionCommands/MovementResolver; `T/FleetCommandTests.cs`, `FleetTravelTests.cs`.

- [ ] Add `FleetCommandTests.BulkRejectsAllOnStaleForeignOrIllegalMember` and `NamedFleetCapDoesNotBlockIndependentConstructionOrSafety`; check create/rename/disband/merge/split/flagship,1..32 membership/1..80 name and64 active+reserved cap. `FleetTravelTests.Mixed300And200AtDistance301TakesTwoTurns` asserts both routes/durations and no mid-transit edit. `NavigatorSpeed315And330DoesNotChangeFrozenTransitOrAmebaOdds` tests planned bonus seam with labelled officer XP.
- [ ] Run `Focused(FleetCommandTests)` and `Focused(FleetTravelTests)`; RED must identify partial mutation, route duration/cap or no-op contract, not absent APIs.
- [ ] Implement atomic command variants and previews; explicit detachment before incompatible manual moves/founding. Aggregate direct move selection into per-fleet/co-located cohorts and ordinary independent ships; validate all before publishing. FleetTravel uses the minimum effective speed and existing positive integer duration; persist launch provenance matching ordinary vessel transit, never alter an already committed route. Losing officer/speed or opening a window cannot change it. Add stable rules8 receipt consequence fingerprint for actual cohort/detachment/duration changes, leaving legacy tokens and hull odds unchanged. Founding remains a separate existing planet command. Own-only names and foreign per-vessel observations stay unchanged.
- [ ] Run both focused classes, `Suite`, `Build`; assert same-order no-op keeps reference/Ready, actual changes clear only issuer, bounds/overflow rollback and all old travel/odds formulas unchanged.
- [ ] Commit named fleet command/travel files, adapters, tests and task evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetCommands.cs src/OpenAntares.Simulation/Fleets/FleetCommandProcessor.cs src/OpenAntares.Simulation/Fleets/FleetTravel.cs src/OpenAntares.Simulation/Fleets/OfficerBonuses.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Commands/ExpansionCommands.cs src/OpenAntares.Simulation/Movement/MovementResolver.cs src/OpenAntares.Simulation/Scouting/ScoutRecords.cs src/OpenAntares.Simulation/Scouting/ScoutQueries.cs tests/OpenAntares.Simulation.Tests/FleetCommandTests.cs tests/OpenAntares.Simulation.Tests/FleetTravelTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add atomic fleet control and group travel"
  ```
