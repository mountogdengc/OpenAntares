### Task 6: Context console and outliner

**Files:** new `ContextConsole.cs`, `SystemCard.cs`, `FleetCard.cs`, `EmpireOutliner.cs`; M13 `SelectionSummaryPanel.cs`, `StarPanel.cs`, `VesselPanel.cs`, `GameScreen.cs`.

- [ ] Add failing `SelectionFillsConsoleWithoutModal`, `SystemCardShowsOnlyKnownFacts` (unknown/stale/foreign planets show no fabricated detail), `InlineActionsUseExistingCommands`, `OutlinerOrderStableAcrossTurns`.
- [ ] Console tabs System / Empire / Fleets. System card: header, planet portraits (existing `PlanetPortrait`), owners, habitability where known, inline Manage/Colonize/Send-fleet actions mapped to existing routes. Fleet card: role icons, counts, orders, destination/ETA from existing calculations. Empire tab: outliner of colonies and alerts. Replace the Inspect/Manage/Vessels/Colony-overview button stack while keeping their `Name`s on the equivalent actions.
- [ ] Rerun cases, M13 Selection/Navigation cases and Expansion/Fleet/Scout scenarios.
