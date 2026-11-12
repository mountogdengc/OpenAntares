# M14 fleet roster unification — 2026-11-10

Both owner-authorized gaps are **completed and verified for the focused automated scope**. All work remains uncommitted in `F:/Projects-PCGames/OpenAntares` on `main`. Human physical-input, comprehension and reading comfort remain Pending.

## Result

- Ships, named fleets and fleet-member choices in the Fleets workspace use the same `MapFleetRow` presentation as console rows. The separate ship Tree has been removed; exact-ID adapter methods retain the existing order/controller boundaries.
- Equivalent independent ships form counted, initially collapsed stacks in the workspace and map fleet card. `+` exposes individual ship rows; `−` collapses them. Clicking a stack selects every member. Plain/Shift member selection can choose an exact subset; a partial count and full status remain in row text/tooltips.
- Shared grouping separates role/design, location/transit, pending move/founding, fleet/request commitment, new markers, cargo, relevant damage/health/scout facts and active attention identity. No grouping decides game eligibility or grants orders.
- Member right-click actions remain exact; named-fleet split/merge behavior remains. Course submission and reviewed workspace orders use the existing commands with the exact chosen IDs.
- Large selected rosters summarize equivalent detail groups instead of printing one identical combat line per ship. Status, hull, cargo and differing facts remain available. Named/member-list heights retain their previous 120/100 values through two theme metrics; no styling allowance was expanded.

## Focused validation

Final queue job `971de19577404ff9bcd3ae47d6210f37`, run `fleet-roster-20261110-green04`:

| Check | Result |
| --- | --- |
| Native fleet-roster plus directly affected selection-console controls | **191 passed, 0 failed**, **14 rendered frames** at 800x600 and 1920x1080. |
| Large roster | Labelled pre-session 102-ship fixture: one two-ship named fleet created through an ordinary command, plus 100 equivalent independent ships. Workspace and fleet-card stack behavior checked; exact leaves remain addressable. This is functional coverage, not a performance benchmark. |
| Selection and ordering | Stack click selects 100; Shift removes one exact member; sorting preserves 99 selected IDs and expanded access. Expanded map members select two exact IDs. Both map course submission and reviewed workspace move match independent ordinary-command/reference sessions; preview alone leaves the game unchanged. |
| Group boundaries | Separate projection supplements for request commitment, carried population, damage and queued move each remain outside the equivalent idle stack. |
| Readability | Selected 100-ship workspace readout is compact and retains hull/status. Native frames inspected at both sizes, including actual roster/member/detail areas. Existing outer workspace scrolling is still needed at 800x600. Narrow card status may clip visually; complete row tooltips and the selected detail card retain its information. |
| Direct dependencies | Selection cards, Decisions, conflict entry visibility, naming, ESC sequence, system stacking, split/merge and exact selection/order routes retain fresh coverage in this same selected case. |
| Affected adapter compilation | Common legacy and M13 validation targets both compile with **0 warnings/errors**. Their full suites/campaigns were not executed. |
| Builds and restoration | Isolated opt-in build and mandatory ordinary rebuild both **0 warnings/errors**; all validation scenario/helper/namespace arrays empty in the ordinary assembly. |
| Evidence and diagnostics | Independent source/DLL/frame/evidence verifier passes; executed DLL stable; no warning/error diagnostics in native logs. Development evidence correctly says `accepted=false` because source is uncommitted; this is not milestone acceptance. |
| Theme guard | Pass, 20 retained legacy occurrences against the original 26-entry allowance; no new override or allowance growth. |

Evidence is under `reports/visual-overhaul-execution/fleet-roster-20261110-green04-FleetRoster/` and `fleet-roster-20261110-green04-control/`: results, source/DLL capture, native logs, PNGs/control inventories, verifier, compile/build logs and ordinary metadata.

Only this selected case and necessary adapter compilation ran. No full simulation suite, unrelated native matrix, campaign/future replay, export/platform/security/accessibility audit or extra reviewer ran. Simulation/content/schema/game rules and save code were not changed.

## Preservation and retained attempts

`reports/fleet-roster-2026-11-10/before.zip`, `before.json`, `before.patch` and `before-status.txt` preserve the initial dirty tree; 32 initial changed/source files were archived without loose compilable copies. The earlier map-key patch remains SHA256 `354779061769ce6ba86cb1ca9104ebd4509538af78a422209991d56ead748c67`. GalaxyMap and map-navigation bytes match the starting manifest; removing only the two new height-token lines reproduces the starting theme exactly. No staging, commit, branch/worktree deletion or evidence cleanup occurred.

- `fleet-roster-20261110-red01` reproduced the old Tree and unstacked card, retaining eight failed conditions.
- `...-green01` stopped at the styling guard because the compact-size breakpoint appeared inline in a sizing expression. Factoring the existing themed size into a variable fixed the guard without changing dimensions.
- `...-green02` exposed two compile errors from an old Tree `GetScroll` validation API. The affected checks now read the shared scroll position.
- `...-green03` passed 170 checks/12 frames before the additional selected-state/command checks.
- `...-selection-red01` stopped when a workspace action became hidden during capture. A traced rerun kept those captures visible; no product cause was established for that isolated attempt, and it remains failed evidence.
- `...-selection-red02` reproduced the 100-line selected-detail problem. Its final extra command check also needed an explicit return to the remembered Ships tab. The readout was fixed and test navigation corrected; no game tab behavior was changed.
- `...-green04` is the final passing source/evidence epoch above. Every earlier result retains its own original scope.

## Files changed in this follow-up

- `CLAUDE.md`
- `PROJECT_STATUS.md`
- `game/Components/FleetCommandDialog.cs`
- `game/Components/FleetList.cs`
- `game/Components/FleetRowList.cs`
- `game/Components/FleetShipStack.cs`
- `game/Presentation/FleetRoster.cs`
- `game/Screens/GameScreen.FleetPanel.cs`
- `game/Screens/GameScreen.SelectionConsole.cs`
- `game/theme/openantares_theme.tres`
- `reports/M14_FLEET_ROSTER_UNIFICATION_2026-11-10.md`
- `reports/M14_SELECTION_CONSOLE_2026-11-10.md`
- `reports/M14_SELECTION_CONSOLE_VALIDATION_2026-11-10.md`
- `reports/visual-overhaul-execution/Run-QueuedReview.ps1`
- `tests/OpenAntares.Game.Validation/CombatGraphicalValidation.cs`
- `tests/OpenAntares.Game.Validation/FleetGraphicalValidation.cs`
- `tests/OpenAntares.Game.Validation/FleetRosterChecks.cs`
- `tests/OpenAntares.Game.Validation/PlayabilityCampaignControls.cs`
- `tests/OpenAntares.Game.Validation/SelectionConsoleChecks.cs`
- `tests/OpenAntares.Game.Validation/ShipDesignCampaignChecks.cs`
- `tests/OpenAntares.Game.Validation/VisualOverhaulReviewChecks.cs`
- `tests/OpenAntares.Game.Validation/VisualOverhaulValidation.cs`
- `tests/OpenAntares.Game.Validation/VisualOverhaulValidation.targets`
- `tools/ConflictValidation/Run-VisualOverhaul.ps1`
- `tools/ConflictValidation/Verify-VisualOverhaulEvidence.py`

The source files above are distinguished from inherited selection-console/map-key edits. Local plan, preservation archive and generated native evidence are retained under the report directories.

## Owner check

Use the ordinary primary project `F:/Projects-PCGames/OpenAntares/project.godot` in Godot 4.7.2 Mono and press F5 after stopping an older run. Verified normal assembly: `F:/Projects-PCGames/OpenAntares/.godot/mono/temp/bin/Debug/OpenAntares.dll`. Direct launcher: `F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe --path F:/Projects-PCGames/OpenAntares`. A source change requires the normal queued game build first: from the primary root, `python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- dotnet build OpenAntares.csproj`.

Prerequisites for physical stack checks: an existing campaign with at least two equivalent independent ships at one star; a named multi-ship fleet for member/split checks. A fresh one-ship game cannot demonstrate a multi-ship stack. Automated 102-ship fixtures are labelled test supplements, not an earned campaign or a production save.

1. Open Fleets → Ships. Expect the same row style and ship names as the console. Expand a counted row, choose one member, Shift-add/remove another and change sort. Expect the intended exact selection to remain; use the existing outer scroll area at 800x600.
2. Open Fleets → Fleets and select a named fleet. Expect matching row styling for that fleet and its member choices. Review an intended move/split only when you want that order; retain the existing confirmation boundary.
3. Return to the map and open the own fleet flag. Expect equivalent independent ships in one counted row. Expand it, choose one or several exact members, collapse it and inspect the selected details.
4. Use Ship orders for a single chosen member, and set a course only when intended. Expect that exact ship/subset to be affected. Different pending orders or damage should separate rows; equivalent idle ships stay counted together.
5. Compare 800x600 and 1920x1080: check expansion discoverability, row/tooltips, detail readability, keyboard focus and scrolling comfort. Report any concrete failure instead of inferring full acceptance from the automated counts.

Report back: resolution/UI scale, campaign/save and turn, selected ship/fleet IDs, numbered step, expected versus observed behavior and any screenshot/diagnostic. Successful human observations enable acceptance/disposition of this uncommitted UI; a concrete failure enables a bounded follow-up. Neither authorizes committing or a broader test run.
