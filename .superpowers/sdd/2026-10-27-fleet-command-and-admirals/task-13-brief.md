## Task 13 — Strict writer9 and all continuation states

**Files:** new FleetSaveData; SaveGame/ExpansionSaveData/ContentWriter, FleetStateValidator; `T/FleetSaveTests.cs`, extend FleetCompatibilityTests.

- [ ] Add `FleetSaveTests.ExactThirtySevenPairsAndNoEightEight`, `LegacyFleetInjectionRejected`, `EveryPendingPhaseRoundTripsToIdenticalFuture`, `FailedLoadAndWritePreserveSessionAndDestination`. Exercise pending launch/committed cohort, partially built/arrived/missing/safety-paused request, priority/token/active-project preservation, Wenth founding, commission/recovery/Missing/dead careers, promotion/event credits and independent stream state.
- [ ] Run `Focused(FleetSaveTests)`; semantic RED uses expected unsupported(9,8) or round-trip invariant rather than compile failure.
- [ ] Activate writer9 and append exactly8 new explicit pairs, preserving the29 old accepted pairs. Strict codec requires rules8 fields including valid priority, stream/counters/assignment/provenance/embedded content and material scout receipt fingerprint; rejects injected new metadata/content under old rules, unknown enums, duplicates, stale/foreign references, invalid deadlines/XP/counters/cohort and queue bindings. Historical vessel facts are immutable facts, not live lookup requirements. Failed read leaves GameSession unchanged; existing atomic WriteFile retains destination on late failure. Writer9 old-rule saves omit all new content/state/receipt fields and replay identical futures. Leave CurrentVersion7 for now.
- [ ] Run `Focused(FleetSaveTests)`, `Focused(FleetCompatibilityTests)`, `Suite`, `Build`; replay all Task1 rules7/older futures and round-trip new states through actual saves/commands. Save/load causes zero preparation, acknowledgement, draws or priority reorder.
- [ ] Commit codec/save/validator files, tests and evidence; retain frozen bytes/expected values.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Saves/FleetSaveData.cs src/OpenAntares.Simulation/Saves/SaveGame.cs src/OpenAntares.Simulation/Saves/ExpansionSaveData.cs src/OpenAntares.Simulation/Saves/ScoutSaveData.cs src/OpenAntares.Simulation/Content/ContentWriter.cs src/OpenAntares.Simulation/Fleets/FleetStateValidator.cs tests/OpenAntares.Simulation.Tests/FleetSaveTests.cs tests/OpenAntares.Simulation.Tests/FleetCompatibilityTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: persist strict fleet campaign state"
  ```
