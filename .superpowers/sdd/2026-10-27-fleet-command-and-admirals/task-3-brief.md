## Task 3 — Validated state, own views and initial appointments

**Files:** new FleetState/FleetStateValidator/FleetViews/FleetQueries, remaining FleetInitializer; existing state, validator, projector/view and GalaxyGenerator; `T/FleetStateTests.cs`, `FleetQueryTests.cs`, `FleetFixtures.cs`.

- [ ] Add `FleetStateTests.CloneIsDeepAndLegacyMetadataRejected`: mutate nested copied requests/slots/queues/careers/streams and prove source unchanged; reject duplicate/foreign membership, incoherent transit, conflicting assignments, invalid deadlines/unlocks, caps and counters. `FleetQueryTests.ForeignRosterAndUnknownDangerNeverAppear` compares views/previews of worlds differing only in hidden enemy/habitat state. `StartsOnePostedNavigatorWithoutGlobalIdsOrOldRngDraws` checks all species, declared alternative IDs and Wenth source-body identity.
- [ ] Run `Focused(FleetStateTests)` and `Focused(FleetQueryTests)`; expect real clone/invariant/visibility failures after minimal contracts compile.
- [ ] Implement the shared state contracts and centralized validator delegation, strict legacy absence, immutable own projection, deterministic local IDs/candidates/catalog. Rules8 initializes after unchanged galaxy/ship/colony generation: human Navigator Posted at home, two mortal candidates; Wenth home-body Navigator without fictional independent candidates. AI assignment is completed in Task14. Mark new independent completions explicitly, without auto-creating named fleets; old vessels/markers stay legacy-compatible. All ID allocation checked, deterministic and separate from global NextEntityId.
- [ ] Run both focused classes, `Suite`, `Build`. Assert rules7 generation/futures/global IDs and all old streams unchanged, queries pure and no foreign identity leaked in rejections or reports.
- [ ] Commit only new state/view files, named state/generator/projector files/tests and task evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/FleetState.cs src/OpenAntares.Simulation/Fleets/FleetStateValidator.cs src/OpenAntares.Simulation/Fleets/FleetViews.cs src/OpenAntares.Simulation/Fleets/FleetQueries.cs src/OpenAntares.Simulation/Fleets/FleetInitializer.cs src/OpenAntares.Simulation/State/GameState.cs src/OpenAntares.Simulation/State/VesselState.cs src/OpenAntares.Simulation/State/GameStateValidator.cs src/OpenAntares.Simulation/Knowledge/EmpireView.cs src/OpenAntares.Simulation/Knowledge/EmpireViewProjector.cs src/OpenAntares.Simulation/Galaxy/GalaxyGenerator.cs tests/OpenAntares.Simulation.Tests/FleetStateTests.cs tests/OpenAntares.Simulation.Tests/FleetQueryTests.cs tests/OpenAntares.Simulation.Tests/FleetFixtures.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add validated fleet state and own views"
  ```
