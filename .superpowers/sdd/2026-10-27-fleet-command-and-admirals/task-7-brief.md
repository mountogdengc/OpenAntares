## Task 7 — Accept, reprioritize, pause and amend finite construction

**Files:** new AssemblyCommands/AssemblyCommandProcessor/RequestQueueAuthority; FleetState/CommandProcessor/DevelopmentCommandProcessor/GovernorPlanner/GovernorPlanningQueries; `T/FleetRequestTests.cs`.

- [ ] Add `FleetRequestTests.PriorityEditPreservesActiveTokenReserveAndUnrelatedOrder`, `OffGovernorStoredTemplateNeverGainsTemplateAuthority`, `CargoNeedsExplicitAuthorization`, `SafetyPauseRetainsLivingBindingsAndRequiresLegalResume`, `MissingSlotBlocksCompletionUntilFiniteAmendment`. Check same-priority no-op, paused edit stays paused,32-entry atomic overflow, colony/rally exclusive use and reserved fleet slot release.
- [ ] Run `Focused(FleetRequestTests)`; semantic RED must reproduce a queue/authority/safety error with literal token/reserve assertions.
- [ ] Implement revision/fingerprint-checked accept/edit transactions. Accept allocates each finite slot exactly once and inserts Normal tail/Urgent after active, preserving existing queue tokens. Temporary request authority makes the **existing** selector scan legal explicit entries only through the last authorized request entry while ConstructionOff; if none legal bank output, never use stored template or toggle switches/workforce. ConstructionOn retains its sole ordinary selector. Pause/manual takeover ends contribution selection/dispatch but preserves active project; keep cancellation converts entries in place, remove cancellation removes only unstarted bound entries. Explicit detach/loss marks Missing; safety pause records BlockedSafety with prior status/bindings, no replacement/retry; active projects may finish and wait. Change rally/resume must re-preview current legal knowledge and preserve committed travel. Releasing/replacing a reserved named-fleet slot is atomic; disbanding a rally pauses rather than discards it.
- [ ] Run `Focused(FleetRequestTests)`, `Focused(FleetPlanningTests)`, `Suite`, `Build`; verify full cancel/finite-amend/ordinary-manual matrices, cargo all4 species/quarantine and no extra governor controller. No-op and failure preserve fingerprints, IDs, reserve and issuer Ready.
- [ ] Commit named request/queue files, shared adapters, tests and evidence only.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/AssemblyCommands.cs src/OpenAntares.Simulation/Fleets/AssemblyCommandProcessor.cs src/OpenAntares.Simulation/Fleets/RequestQueueAuthority.cs src/OpenAntares.Simulation/Fleets/FleetState.cs src/OpenAntares.Simulation/Commands/CommandProcessor.cs src/OpenAntares.Simulation/Commands/DevelopmentCommandProcessor.cs src/OpenAntares.Simulation/Development/GovernorPlanner.cs src/OpenAntares.Simulation/Development/GovernorPlanningQueries.cs tests/OpenAntares.Simulation.Tests/FleetRequestTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: add finite request authority and priority"
  ```
