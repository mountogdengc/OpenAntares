### Task 4: Command bar and End Turn

**Files:** new `CommandBar.cs`, `PendingSummary.cs`; `GameScreen.cs`; `CombinedUiNavigation.cs` only if a route's control moves.
**Interfaces:** consume M13 `TurnGuidance`; produce Task 4 contract.

- [ ] Add failing cases `CommandBarGroupsAndHotkeysOpenRoutesOnly`, `ShortcutsAndDragsIssueAtMostOneExplicitCommand` (shortcut part), `ShortcutsIgnoredWhileEditingText`, `EndTurnShowsPendingAndRoutesToEachItem`, `HarnessRouteNamesStillResolve`.
- [ ] Build the bar: empire keys (Colonies C, Fleets F, Research R, Designs D, Diplomacy P) as icon+label `CommandKey`s with hotkey hints; alert `IconKey` with `AlertBadge` count; gear key hosting the existing Game menu (IDs 0/1/2 unchanged); wide End Turn key with the pending line. Scouts and Contacts keys remain until Task 8 moves them into workspace tabs, keeping their `Name`s.
- [ ] Rerun cases and Crisis/Combat/Diplomacy/EmpireConflict checks; Ready availability and rejections must match baseline exactly.
