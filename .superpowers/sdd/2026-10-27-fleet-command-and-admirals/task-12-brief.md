## Task 12 — Officer loss, recovery and commissioning clocks

**Files:** new OfficerRecovery; FleetInitializer/FleetResolutionContext/TurnResolver; `T/FleetRecoveryTests.cs`, extend FleetOfficerTests.

- [ ] Add `FleetRecoveryTests.MortalDraw49Succeeds50FailsAnd69Succeeds70Fails`, `LossAtTenReturnsAtThirteen`, `WenthLinkTwoOneAndLastWatchFourThreeWithoutDraw`, `MissingRecoveryStartsOnlyWithOwnColonyAndRestartsIfDestinationLost`. Distinguish founder consumption/no roll, another member's loss, LastWatch permanent death, failed recovery permanent death, cargo remains destroyed, next-snapshot technology and exact independent RNG advancement/order.
- [ ] Run `Focused(FleetRecoveryTests)`; semantic RED for threshold/timer/draw behavior. Use reachable PCG seeds/states producing labelled draws; record draw count and validate state, never treat forced rare fixtures as generated campaigns.
- [ ] Apply actual destroyed designated flagship events ordered EmpireId/OfficerId, captured doctrine/technology/flagship/location. Only eligible mortal Recover draws once from its own recovery stream; success countdown starts after loss and completes after2 **subsequent** boundaries. No-colony success Missing has no running deadline; later owned colony chooses lowestColonyId and starts same delay; losing that destination returns Missing without reroll. Wenth retains source-body identity/history/XP and uses snapshot2/1 or4/3 delay without draw. Dead/Retired never revive. Advance pre-existing commissioning/recovery deadlines once; new events cannot tick immediately. Commission accepted turn10 becomes Posted12. Snapshot Command Recovery researched this turn applies only to later losses. Hull/cargo effects remain ordinary outcomes.
- [ ] Run `Focused(FleetRecoveryTests)`, `Focused(FleetOfficerTests)`, `Suite`, `Build`; fault every phase including Recovery/Validation and compare entire original bytes/reference plus all existing/random streams. Previews/invalid/no-op/reload/transit/Wenth/LastWatch draw zero.
- [ ] Commit named recovery/clock integration files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerRecovery.cs src/OpenAntares.Simulation/Fleets/FleetInitializer.cs src/OpenAntares.Simulation/Fleets/FleetResolutionContext.cs src/OpenAntares.Simulation/Turns/TurnResolver.cs tests/OpenAntares.Simulation.Tests/FleetRecoveryTests.cs tests/OpenAntares.Simulation.Tests/FleetOfficerTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: resolve officer recovery and appointment clocks"
  ```
