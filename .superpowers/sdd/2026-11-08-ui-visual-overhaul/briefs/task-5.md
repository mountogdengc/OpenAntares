### Task 5: Top status strip

**Files:** new `StatusStrip.cs`, `StatusReadouts.cs`; `GameScreen.cs`, `ResearchBar.cs` (research target selector moves into the strip or the Research workspace; its command behavior is unchanged).

- [ ] Add failing `StatusTotalsEqualProjectedEconomy` (every species, positive/short support, blockade pressure, zero-colony), `BreakdownTooltipMatchesBreakdownText`, `StripFitsAllSizes` (800x600 collapses labels to icons, never clips values).
- [ ] Implement the strip: empire name/emblem, turn, readouts with delta, research target with progress and turns-remaining from existing estimates (conditional estimates shown as conditional). Remove the duplicated turn/count labels from the console.
- [ ] Rerun cases plus Species/Colony regressions.
