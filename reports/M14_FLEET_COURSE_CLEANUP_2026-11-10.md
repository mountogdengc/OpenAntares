# Fleet-course block cleanup — 2026-11-10

Removed the entire redundant block identified in the owner's screenshot: Fleet course heading, instructional/route readout and Deselect fleet button. The fleet card retains Ship orders and System card. Map click-to-course, map route overlay and ESC/right-click deselection remain; stale ship IDs are still pruned from selection.

Changed product files: `game/Screens/GameScreen.MapNavigation.cs`, `GameScreen.cs`, `GameScreen.FleetPanel.cs`. The removed control builder/text formatter is gone; selection reconciliation remains explicitly named. Pre-change copies are retained in `reports/fleet-course-cleanup-2026-11-10/before.zip` with hashes in `before.json`. Status files updated; nothing staged or committed.

Verification: normal `dotnet build OpenAntares.csproj` through the shared queue, job `b8930861852e40cb9b823d732897d148`, passed with 0 warnings/errors. Fresh ordinary metadata has no validation types/helpers. Source inspection confirms the removed control names/text are absent and order-submission/map-key methods plus selection-clearing bodies are identical to their pre-change copies. No new test, interaction suite, full test run or native matrix was warranted for this reversible UI removal. Previous 191-check fleet evidence remains historical coverage of its recorded source, not fresh coverage of this change.

The owner's running playtest was not stopped. Save if needed and relaunch the primary project `F:/Projects-PCGames/OpenAntares/project.godot` with F5 to see this ordinary build. Direct launch uses the installed Godot 4.7.2 Mono executable with `--path F:/Projects-PCGames/OpenAntares`. Human visual confirmation of the cleanup remains Pending.
