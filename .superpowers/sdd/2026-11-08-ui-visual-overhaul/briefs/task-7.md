### Task 7: Fleet list rework

**Files:** new `FleetGrouping.cs`, `FleetList.cs`; `FleetCommandDialog.cs`, `ScoutOverviewDialog.cs`.

- [ ] Add failing `StacksOnlyMergeOrderEquivalentShips`, `GroupOrderStableAndIdOrdered`, `SelectionOfStackExpandsToExplicitShips` (orders target explicit VesselIds, never "the stack"), `ThousandShipListStaysResponsive` (frame-time budget recorded, not asserted against wall clock in the simulation).
- [ ] Implement grouping per the Task 7 contract, a virtualized list (Tree with lazy children or recycled rows), filter chips, multi-select by Shift/drag, status lights. Remove pagination. Existing request/officer/admiral detail routes stay reachable.
- [ ] Rerun cases plus Fleet/Scout/ShipDesign/Combat scenarios.
