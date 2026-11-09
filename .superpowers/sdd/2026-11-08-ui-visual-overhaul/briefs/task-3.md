### Task 3: Theme pass across every existing screen

**Files:** `openantares_theme.tres`; C# limited to `ThemeTypeVariation` assignments and container structure needed to host frames (e.g. wrapping a page in a `PanelContainer`).
**Interfaces:** produce the Task 3 vocabulary.

- [ ] Add failing harness cases `EveryControlUsesThemedStyle` (no control resolves to the engine default stylebox), `EveryInteractiveControlReachableAtAllSizes` (compared with the Task 1 inventory), `FocusIsVisibleOnEveryKey`.
- [ ] Define palette constants and StyleBoxes: `StyleBoxFlat` with `corner_detail = 1` for 45-degree chamfers (radii on top-left/bottom-right for primary frames), borders/shadows/expand margins for depth; `StyleBoxTexture` nine-patches where bevels or greebles are needed. Fill Button/TabContainer/TabBar/ItemList/Tree/LineEdit/SpinBox/OptionButton/PopupMenu/ScrollBar/ProgressBar/Window/AcceptDialog/TooltipPanel base types, then the variations.
- [ ] Assign variations in existing components (status labels, readouts, keys). Do not restructure layout yet.
- [ ] Rerun the cases plus the full native scenario matrix (restyle must not break control discovery). Retain before/after frames for owner comparison.
