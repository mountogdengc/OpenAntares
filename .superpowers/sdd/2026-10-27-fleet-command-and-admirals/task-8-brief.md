## Task 8 — Snapshot Organizer prices through ordinary economy

**Files:** extend OfficerBonuses with RequestShipCost; VesselProduction; own forecast adapters; `T/FleetEconomyTests.cs`.

- [ ] Add `FleetEconomyTests.OrganizerScoutCosts760And720OnlyWhileActiveCoordinator`: base800, no bonus800, XP10→760, XP30→720; ordinary ships/buildings/population unchanged. `PauseRemoveCancelAndPromotionRepriceOnlyNextSnapshotWithoutRefund` checks exact reserve and before/after cost previews, including blocked cargo and stored output. `CeilingAndOverflowRejectAtomically` uses non-divisible alternative costs.
- [ ] Run `Focused(FleetEconomyTests)`; require cost/reserve/completion assertions RED.
- [ ] Implement checked ceil `max(1,(baseCost*(100-discount)+99)/100)` via Int128 and checked long. Only current active request-bound token/role/colony with its assigned active coordinator qualifies at supplied snapshot; paused/safety-paused/cancelled/unbound/ordinary targets do not. Reuse this method in VesselProduction and own-view preview, preserving EmbarkationRules, one completion/colony/turn and unchanged unspent production reserve. Promotion later this boundary cannot change the price already paid. Snapshot discount never grants construction authority itself.
- [ ] Run `Focused(FleetEconomyTests)`, `Focused(FleetPlanningTests)`, `Suite`, `Build`; ensure ordinary/mismatched target costs and rules1..7 futures unchanged.
- [ ] Commit named bonus/economy/forecast files, tests and evidence.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Fleets/OfficerBonuses.cs src/OpenAntares.Simulation/Fleets/FleetPlanningQueries.cs src/OpenAntares.Simulation/Development/GovernorPlanningQueries.cs src/OpenAntares.Simulation/Economy/VesselProduction.cs tests/OpenAntares.Simulation.Tests/FleetEconomyTests.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: apply scoped organizer production bonuses"
  ```
