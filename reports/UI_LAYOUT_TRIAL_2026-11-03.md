# UI layout trial — 2026-11-03

Status: **Verified** for scoped automation; **Implemented, Unverified** for rendered/physical-input/comfort acceptance. Owner requested a playable trial of the approved map/right-console/bottom-navigation arrangement to assess comfort. Baseline main `5df255b`, isolated branch `codex/ui-layout-trial`; changes remain uncommitted, no integration or commit authorized. No simulation, content, campaign rules, writer or saved selection format was changed. The ordinary build and imports are prepared for the owner to play.

## Scope and evidence

The retained opt-in `LayoutTrialValidation` exercises actual Godot controls under the headless display. It checks geometry and navigation purity in current and legacy campaigns at four logical desktop sizes; then workforce, construction, research, End Turn, construction report follow-up and save round trip. These checks do not establish framebuffer rendering, OS input, focus comfort or aesthetics. The runner restores the normal assembly and checks the new harness is absent.

All heavy build/test commands use the shared queue with this actual checkout as source/cwd. Pending empire-conflict and other worktrees retain their existing status and evidence.

- `red-01`: ordinary/opt-in builds succeeded; harness setup failed because a current campaign requires explicit species. Test fixture corrected; this is not product RED evidence.
- `red-02` / queue `da68a113723a412d9a84691f62680ab5`: expected failure `Missing trial control: MainNavigation` on unchanged product. Normal rebuild restored ordinary code.
- `green-01` / queue `4d5dfc2d710c41b8bc2d51b771de8efb`: current-campaign navigation/commands/report/save checks passed; legacy fixture incorrectly supplied settings unavailable to old rules. Fixture corrected using version-supported settings.
- `green-02` / queue `b024552df9764c7fb6df544931b6cc59`: retained reviewer regression failed at ordinary 800x600 production scaling. The earlier test changed `ContentScaleSize` and therefore did not exercise the ordinary player window. Corrected test changes window size only; the trial disables fixed-canvas shrinking so desktop controls use actual window coordinates.
- Portable installed profile / queue `0b12df0e956b4e6dbd698b9e5093b80a`: **2,134 passed, 0 failed, 0 skipped**, exit0. Source digest `07813d56c4519843e35d388871358ad5098709fbef5a7567bdfb3af2c2d8b778`; all1,784 captured files match this checkout after the UI corrections. The profile explicitly excludes six Windows/native-evidence cases. Full Windows/native/platform coverage is not claimed.
- `final-01` / queue `241c3f11ff2b45e99bf47060219d7211`: all106 retained headless assertions passed, including current/legacy actual-window geometry, rejected stale-order feedback, Contacts, report follow-up and actual Main Game-menu Save/Load. Ordinary/opt-in/normal-rebuild each0 warnings/errors; ordinary metadata excludes all12 scenarios and2 helpers. Shutdown RID/orphan diagnostics were then inspected, so this is not claimed as clean final evidence. An unparented legacy-only diplomacy button was assigned scene ownership; test teardown now collects released managed resources and checks orphan count, and the runner rejects engine errors/leak warnings.
- `final-02` / queue `009a01e39e6440c7aca381f76f19b770`: **107/107** assertions pass; all three builds0warnings/errors,0engine errors/leak warnings, no orphan controls after current/legacy/Main teardown. Metadata negative control detects the real trial assembly; final normal assembly excludes12 scenarios/2 helpers. All7 captured source hashes still match the final files.
- `preparation-01` / queue `293575d3ea384d7d8dd98c9b508219fc`: headless editor import and ordinary Main startup exit0, no startup diagnostics. Evidence imports are excluded with `reports/.gdignore`; generated Godot UIDs remain retained. This preparation precedes the final normal rebuild; the final107-check scenario separately exercises actual Main New Game and Game-menu Save/Load on final matching source after preparation.
- `python tools/Verify-LayoutTrialEvidence.py`: independently checks queue exit codes, result/TRX counters, clean Godot logs, normal/opt-in assembly metadata,7final source hashes and1784portable snapshot files. [summary.json](ui-layout-trial/summary.json) records the result and retained QA save. No prior failed evidence or QA save is deleted.

The new Game menu changes how the old graphical harnesses must navigate: several still search for standalone `Save`/`Load` toolbar buttons and operate detail panels without opening their workspace. Those historical scenarios were **not run or adapted for this isolated trial**. Their old results are not trial evidence. The new headless scenario exercises actual Main Save/Load from the Game menu; adapting the wider native harness and running rendered regression remain prerequisites to adopting the experiment into main.

One fresh review is recorded in [UI_LAYOUT_TRIAL_REVIEW_2026-11-03.md](UI_LAYOUT_TRIAL_REVIEW_2026-11-03.md). No second review, integration, commit or milestone promotion is implied. Current games remain rules11 / writer12 /67 accepted pairs; no simulation/content changes.

## Owner trial after automated work

Checkout: `C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ui-layout-trial`.

Prerequisites: Windows, .NET8 runtime and Godot4.7.2 .NET console at `F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe`. These are the installed executables used by the queued checks. Ordinary assembly: `.godot/mono/temp/bin/Debug/OpenAntares.dll`. Launch by double-clicking **`Play-UI-Layout-Trial.cmd` in this checkout**; it opens the ordinary Main/New Game screen without rebuilding. Alternatively open this checkout's `project.godot` in Godot .NET and run `game/Main.tscn`.

No rebuild is needed for the prepared trial. To rebuild/recheck later from this checkout, use a fresh output directory:

```powershell
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 1800 -- pwsh -NoProfile -File tools/Run-LayoutTrialChecks.ps1 -OutputDirectory reports/ui-layout-trial/recheck-unique
```

1. Start a Small seed42 campaign. Expect the star map to dominate, turn/research/selection to appear in the right console and navigation/End Turn along the bottom.
2. Select a system, inspect it, return to the map, open the colony and change one worker/project. Expect the map selection and orders to survive workspace switches. Open Fleets, scouting, diplomacy and designs if supported by the campaign; return without losing orders.
3. Advance through a project/research completion and follow the report action. Expect the correct workspace/control to be revealed before its picker opens. Save under a new `qa-ui-layout-...` name and reload; expect campaign continuity.
4. Resize through 800x600, 1152x648, 1600x900 and 1920x1080. Expect reachable navigation, scrolling details and no controls drawn outside the window.

Report back: window size/display scaling, selected page, action, expected/actual result, whether it felt easier or slower, and a screenshot for any clipping. Comfort feedback determines spacing/navigation refinements; report-follow-up or command failures require regression fixes before adopting the layout. Existing full human/platform/release acceptance remains separate.

## Owner adoption and integration — 2026-11-03

Owner reported “That looks a lot better” and then explicitly requested keeping the layout and merging it into main. The prior recommendations about broad graphical-harness adaptation are retained as disclosed Deferred coverage; owner adoption and local integration are now authorized. Full physical-input/accessibility/release acceptance is still separate. Fresh combined-source verification and preservation are recorded in [UI_LAYOUT_INTEGRATION_2026-11-03.md](UI_LAYOUT_INTEGRATION_2026-11-03.md).
