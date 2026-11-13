**Fleet follow-up, 2026-11-10:** The owner subsequently authorized and completed the separate Fleets workspace row unification and fleet-card independent-ship stacking. The gaps recorded below are historical for the previous check. See [fleet roster verification](M14_FLEET_ROSTER_UNIFICATION_2026-11-10.md). Human acceptance remains Pending; no commit was made.

**Continuation result, 2026-11-10:** Normal game build and final focused native validation pass (115 checks/6 frames at 800x600/1920x1080), with ordinary assembly restored. Selection-card sizing was corrected; moved-route checks adapted. Human acceptance, complete Fleets row unification and fleet-card independent-ship stacking remain outstanding. All work remains uncommitted. See [continuation verification](M14_SELECTION_CONSOLE_VALIDATION_2026-11-10.md). The original uncompiled handoff below is retained as history.

# Selection-only console — implementation handoff, 2026-11-10

**Status: Implemented, Unverified. Not compiled, not run.** Written without access to `dotnet` or Godot. The first task for whoever picks this up is a normal game build; expect to fix compile errors before anything else. No commit, merge or push was made. The edits sit in the primary working tree on top of the uncommitted 2026-11-09 map-key changes.

Source: the revised Galaxy Screen Mockup (2026-11-10) and `docs/playtests/m14-galaxy-screen-review-2026-11-10.md`. Scope chosen by the owner: fix-list items 1, 2, 5 and 10 only.

## What changed

| Item | Change |
| --- | --- |
| 1 Console = selection only | The `ContextConsole` TabContainer (System / Empire / Fleets) is gone. The console now holds a selection header, the fleet card, the fleet-course block, and a system card. A star shows the system card; a fleet flag or a fleet row shows the fleet card in place. The always-on Conflict orders and Colony overview buttons and the attention badges left the console. |
| 2 One fleet UI | The Fleets-tab dropdowns (`SelectionSummaryPanel`) are no longer used by the screen. `MapFleetRow.Describe` is the one row layout (role icon, name, status) for the fleet card and the new "Fleets in this system" list. `Presentation/FleetNaming` is the one naming and status vocabulary: a ship is "Scout 51" everywhere, including Fleet command and the split dialog (previously "Scout · independent", "Scout 51" and "Scout #51"). |
| 5 Decisions shows its items | The Decisions key opens a new `Decisions` workspace page. It lists required and optional guidance directly (no "Consider (optional) +" toggle), then the attention badges, Review combat response, Conflict orders and Colony overview. Pressing the key again returns to the map. |
| 10 Fill the console | The system card lists planets as a row of toggle keys, a card for the chosen planet or colony (output, project, progress), and "Fleets in this system" with own fleets, stacked independent ships and observed foreign ships. The fleet card lists the selected ships with status, cargo and hull, plus "Ship orders" and "System card". |

Also changed, because the mockup's console header says so: on the map, Escape now clears the chosen ships first and then the system or fleet card (same path as right-click).

## Files

- New: `game/Screens/GameScreen.SelectionConsole.cs`, `game/Presentation/FleetNaming.cs`
- Edited: `game/Screens/GameScreen.cs`, `GameScreen.VisualOverhaul.cs`, `GameScreen.FleetPanel.cs`, `GameScreen.Playability.cs`, `game/Components/MapFleetRow.cs`, `TurnGuidancePanel.cs`, `FleetCommandDialog.cs`, `MapFleetSplitDialog.cs`
- Not touched: the theme file, `src/`, `content/`, saves, tests. Only existing theme variations and icons are used.
- `reports/selection-console-2026-11-10/pre-change-uncommitted-game.patch` is `git diff -- game` taken before these edits (the map-key work), for separating the two change sets.

## Build and check

1. Normal game build (`dotnet build OpenAntares.csproj` or Godot build). Fix any errors; none of this code has been through a compiler.
2. F5, new game. On the map check, in order:
   - Home star selected: header reads SELECTED · STAR SYSTEM; planet keys, planet or colony card, Manage colony / Inspect system, Fleets in this system.
   - Click another planet key: the card below changes; Manage colony disables for a world that is not yours.
   - Click a row under Fleets in this system, or a fleet flag: the fleet card replaces the system card, no system ring. Select a row, click a star: course is set as before. Shift-click, right-click split and drag-merge still work.
   - Ship orders opens the Vessels page for the selected ship; System card returns to the system.
   - Decisions key: the page lists the same number of items End Turn reports; pressing it again returns to the map.
   - Escape and right-click clear the selection; the console reads NOTHING SELECTED.
3. Things most likely to need adjusting once seen: planet keys are 44 px high because `ApplyTouchTargets` forces every button's height, so the portrait icon is small; `ExpandIcon` sizing on fleet rows; text length of the colony readout line at console width 340.

## Known consequences

- **Validation harness needs adapting.** These opt-in checks assume the old structure and will fail or not find controls: `PlaytestFeedbackChecks` (ContextConsole tab toggling), `CombinedUiNavigation` and `PlayabilityCampaignControls` (`OpenVesselWorkspace`, `OpenColonyOverview`, `KnownSystemPicker` reached from the galaxy console), `PlayabilityNavigationChecks` (`SelectionReviewOrders`), `PlayabilityGuidanceChecks` live-Main cases (optional toggle), `VisualOverhaulValidation` routes, `PlayabilityMenuChecks` (the `OpenConflictOrders` button still exists with the same tooltip but is hidden unless the selected system is in conflict), and `EmpireConflict*` checks that click `OpenConflictOrders` (use `DecisionsConflictOrders`, or select the conflict system first). Control names kept: `InspectSelectedSystem`, `ManageSelectedColony`, `OpenConflictOrders`, `OpenColonyOverview` (now on Decisions), `OpenVesselWorkspace` (now on the fleet card), `MapFleetRow_*`, `MapShipRow_*`, `OpenAttentionConsole`.
- Fleet command's ship rows changed from `Name #51 | …` to `Name 51 | …`; fleet member and split-dialog rows use the same form.
- `SelectionSummaryPanel.cs` remains in the project, unused by `GameScreen`, because the harness constructs it directly. Delete it when those checks are retired.
- The Fleets workspace still uses its tree list. It shares names with the console rows but not the row control; replacing the tree is a larger change than this pass.
- The fleet card still lists independent ships one row each (the system card stacks them). With 100+ ships at one star that list is long; that belongs with the fleet-mode roster work.
- The quick colony list that sat in the Empire tab is gone; the Colonies ledger covers it.
- Not done here: fix-list items 3, 4, 6–9, 11–17 (item 17's heading case is fixed only for "FLEETS AT …").
- `PROJECT_STATUS.md` and `CLAUDE.md` status paragraphs were not updated; add one after the build result is known.
