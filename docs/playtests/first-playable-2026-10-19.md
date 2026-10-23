# First Playable Playtest — 2026-10-19

**Status:** Full acceptance open; uncoached session deferred by owner on 2026-10-22. Technical evidence includes owner-reported functional passes and 463 runtime assertions. [ADR-005](../DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md) authorizes expansion design now without claiming the human observations passed. Earlier entries below retain their historical meaning; use the latest owner disposition for progression.

## Environment and baseline

- Commit tested: `6ab206d3ca4811cc9163ee6ea3d5c00ff4bb7b6b` (`main`). The species roster and faction survey had unrelated local edits; the approved playtest plan was also untracked. None were used as game content or changed by this pass.
- OS: Windows NT 10.0.26200.0. Godot: `4.7.2.stable.mono.official.ed1daf0bf` (.NET build), GL Compatibility renderer. Startup screenshots: 1152 × 648 game window.
- Saves: the game uses `user://saves` via `game/SaveFiles.cs`. No test saves have been written yet. Future runs should use unique names such as `qa-20261019-small-42-pre` and `qa-20261019-small-42-post` to avoid overwriting existing saves.
- Restore: `NUGET_PACKAGES` pointed to the existing user cache; `dotnet restore OpenAntares.sln -p:NuGetAudit=false --nologo` exited 0. Audit was disabled only because NuGet network access is restricted in the sandbox.
- Build: `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers` exited 0 with 0 warnings and 0 errors.
- Tests: `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo` exited 0: **195 passed, 0 failed, 0 skipped**. Restore, build, and test needed normal local process access; the sandboxed solution build exited without diagnostics and its test host stalled before discovery.
- Editor: `Godot --editor --path . --quit-after 120` exited 0. The editor and three startup runs wrote no errors or warnings to the captured Godot logs.

## Startup visual checks

All three runs used seed `42`, started a new game through `--new-game=<size>:42`, saved a screenshot after ten frames, and exited 0. These prove startup rendering, not interactive behavior.

| Size | Expected systems | Evidence | Observation |
| --- | ---: | --- | --- |
| Small | 12 | [Screenshot](first-playable-small-42.png) | Spiral background, 12 named systems, selected home system, region and planet list, and colony population 6 visible. |
| Medium | 16 | [Screenshot](first-playable-medium-42.png) | Spiral background, 16 named systems, selected home system, and 2/2/2 workforce visible. |
| Large | 20 | [Screenshot](first-playable-large-42.png) | Spiral background, 20 named systems, selected home system, region and planet list, and colony population 6 visible. |

The map labels and selected system are readable in these captures. The side panel scrolls when the planet list is long. Background-point hit testing, window resizing, control use, and save/load have not yet been checked by interaction.

The small map also rendered at [800 × 600](first-playable-small-42-800x600.png) and [1600 × 900](first-playable-small-42-1600x900.png), with the home marker, labels, top controls, and scrollable side panel visible at both sizes. These are separate launches, so live resize behavior remains unverified.

## Charter acceptance matrix

"Startup observed" means the relevant screen appeared in the screenshots; it does not close that action's interactive check.

| # | Player action | Current evidence | Hands-on result |
| ---: | --- | --- | --- |
| 1 | Begin a new game | Startup command succeeded at all three sizes | Pass, owner report 2026-10-20 |
| 2 | Generate a galaxy | Three seeded maps rendered | Pass, owner report 2026-10-20 |
| 3 | View the galaxy map | Startup screenshots | Pass, owner report 2026-10-20 |
| 4 | Inspect stars and planets | Home inspector visible | Pass, owner report 2026-10-20 |
| 5 | Control one starting colony | Home colony visible | Pass, owner report 2026-10-20 |
| 6 | Allocate workforce | 2/2/2 controls visible on medium | Pass, owner report 2026-10-20 |
| 7 | Select a production project | Construction section visible | Pass; report follow-up issue recorded below |
| 8 | Select research | Research selector visible | Pass, owner report 2026-10-20 |
| 9 | End the turn | End Turn control visible | Pass, owner report 2026-10-20 |
| 10 | See population, production, and research advance | Simulation tests passed | Pass, owner report 2026-10-20 |
| 11 | Save and reload | Save and Load controls visible; save tests passed | Pass, owner report 2026-10-20 |

### Owner follow-up — 2026-10-20

The owner replied that each of the five requested test groups works: new-game/map interaction, workforce and support tradeoffs, growth and construction/research completion, save/reload continuation, and the checks across Small/Medium/Large. These are owner-reported passes rather than a new observer-run matrix. Exact run/save names and a legacy version-1 UI load were not supplied; legacy UI compatibility remains unverified.

## Hands-on run script

Run separately at `small`, `medium`, and `large` with seed `42`. Restart the game between sizes; preserve existing saves.

1. Start through the New Game form. Select another star and a planet, return home, resize the window, and try selecting a dim background point. Record star/planet readability and whether only named systems select.
2. At home, note population, workforce, growth, support balance, production, and research forecast. Shift one worker toward support and one toward production or research. Check whether the displayed forecast explains the tradeoff. Include one support shortage and one supported allocation.
3. Choose Fabrication Hall and Cultivation Methods (or record the alternatives chosen). End turns until population grows, a project completes, and a technology completes. Compare each turn report with the prior forecast; continue one turn after completion to check the effect's timing.
4. Save under a unique `qa-...-pre` name while progress is unfinished. Reload through the in-game Load control and compare turn, workforce, reserves, targets, colony, and map framing. Continue a turn. Save again after a completion under `qa-...-post`, reload, and check the completed item remains.
5. Record pass/fail for each charter action, screenshot or concise evidence, seed, size, expected versus actual result, and steps for any failure. If a legacy version-1 save is loaded in the UI, record that separately; the automated format test alone does not prove UI handling.

## Player understanding session

At least one player unfamiliar with the source should run from New Game through save/load without coaching. Before End Turn, ask what they expect for support, growth, production, and research. After the report, ask why those values changed and when a completed item helps. Record their words, repeated clicks, screen switching, and confusing labels. Ask them to choose between two workforce allocations and two goals; note whether changing the goal changes their choice.

## Findings and decision

The initial automated pass could not exercise Windows input: the UI controller located `OpenAntares (DEBUG)` but window activation timed out, then capture failed with `FrameArrived timed out`. It was stopped without sending game input. The owner's later hands-on results supply the interaction evidence recorded above.

### FP-001: Construction report action does not open the next-project picker

- Classification: **clarity**. Owner: Codex. Reported 2026-10-20.
- Reproduction: complete a building, then click **Choose next project** in the turn report. The owner did not specify the seed or size; the runtime reproduction uses Small, seed 42, Fabrication Hall and Cultivation Methods, initial 2/2/2 workforce, and three turns.
- Expected: close the report, select the colony's star, reveal the construction selector, and open its dropdown so the next project can be chosen.
- Actual: the report closes and the star is selected, but the construction dropdown remains closed. Research already opens its normal dropdown correctly.
- Cause: `GameScreen` connected the construction action only to `GalaxyMap.Select`; unlike research, it had no picker-opening action.
- Fix: retain the colony's construction picker, select the colony's star, wait one frame for the rebuilt panel's layout, then focus and open the picker. The side-panel scroll container follows focus to reveal the control.
- Regression evidence: a temporary C# Godot runtime scene reproduced the original failure, then passed after the fix. It completes the building and technology, changes map selection, resizes to 800 × 600, clicks the actual report button, checks that the report closes and the construction popup opens with its control visible, and selects Cultivation Hub through the existing command path. It also verifies that the research report action still opens its dropdown. The harness is removed after verification; the steps below remain the repeatable manual regression check.
- Manual regression: repeat the reproduction with the home system selected, then with another star selected and the colony panel scrolled at a small window size. Confirm **Choose next project** opens the visible construction dropdown and selecting a project updates construction. Confirm **Choose next research** still opens research. Repeat across all three galaxy sizes after any further UI changes.
- Disposition: opens and selects correctly; headless and graphical runtime verification passed. The owner's visual recheck exposed a positioning issue, recorded as FP-002 below.

### FP-002: Construction popup detached from its selector after focus scrolling

- Classification: **clarity**. Owner: Codex. Reported 2026-10-20 with a screenshot showing the menu above the construction selector, overlapping the colony workforce section.
- Reproduction: Small, seed 42, initial workforce 2/2/2. Research Cultivation Methods with no project for three turns, then build Cultivation Hub on turn four using the stored production. Constrain the side panel so the construction selector requires scrolling, then click **Choose next project**. Exercise both the home-system selection and another star selected before clicking.
- Cause: `GrabFocus()` triggers the scroll container's deferred focus scrolling, but `ShowPopup()` ran immediately. The menu was positioned using the selector's coordinates before the scroll finished. The original regression check asserted visibility and command behavior, but did not compare the popup's final position with the selector.
- Fix: focus the selector, wait for the next process frame so focus scrolling finishes, then open the popup. Check that the same selector still exists and is visible before opening, so a panel refresh during that wait cannot open a stale control.
- Regression evidence: a temporary graphical C# Godot scene compared the report-opened popup position with reopening the same selector normally after layout settled. Before the fix it failed: automatic `(1475, 786)` versus normal `(1475, 506)`, with a 280-pixel focus scroll. After the fix both positions were `(1475, 506)`. All six scenarios passed (three requested window sizes, each with home/other-star selection), with the side panel explicitly constrained to require scrolling because canvas scaling otherwise kept the logical layout tall enough. A saved screenshot was visually inspected and shows the menu attached directly below the construction selector. Temporary harness files were removed after verification.
- Manual regression: after a completion, click **Choose next project** with construction initially below the side panel's visible area. Confirm scrolling finishes and the menu stays attached to its selector; compare with opening the selector directly. Repeat with another star selected and with construction already in view.
- Disposition: fixed; graphical position comparison and screenshot verification passed. Simulation rules and save data are unchanged.

### Validation of the follow-up changes — 2026-10-20

- The requested default window size is now 1920 × 1080 in `project.godot`. A graphical Godot runtime assertion verified the actual root window dimensions before the regression scene resized it. A headless run cannot verify real window dimensions; the initial attempt to assert those dimensions headlessly failed, while the graphical run passed.
- Graphical runtime check: Godot 4.7.2 .NET with GL Compatibility, exit 0. Verified the default window size, construction picker visibility/selection at 800 × 600, and research picker opening. The temporary scene and script were removed afterward.
- Solution build: `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers`, exit 0, zero warnings/errors.
- Simulation tests: `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo`, exit 0: **195 passed, 0 failed, 0 skipped**.
- Build/test used the existing user NuGet cache and normal local process access because the sandbox could not update existing build-cache timestamps. No simulation rules or save format changed.

**Milestone decision: still open.** The owner reports the interactive loop passing, and FP-001 has a passing runtime regression check. The uncoached player-understanding session remains unverified; legacy version-1 UI compatibility is an explicit testing limit. Exploration and colonization design begins after the closure decision gate.

## Continuation - 2026-10-22

See [the complete continuation report](../../reports/FIRST_PLAYABLE_VALIDATION_2026-10-22.md), [final runtime results](../../reports/first-playable-2026-10-22-run4/results.json), and [retained runner](../../tests/OpenAntares.Game.Validation/README.md).

- Graphical runtime: **463 assertions passed, 0 failed**, plus 27 coordinate observations. All three seed-42 sizes were exercised through the actual control signals/handlers, including 12 live resizes, all selectable map systems/background rejection, workforce feedback, production/research completion, population growth, pre/post-completion UI save/load and continuation.
- FP-001/002: **24 native cases pass** across three sizes, four window dimensions and home/other-system selection. Focus scrolling occurs, the picker is visible, automatic popup coordinates match normal reopening, and the next project reaches the command path. Three supplementary fixtures assert actual embedding and provide visible-menu screenshots. Native-window pixels were not captured.
- Legacy schema-1 fixture: copied to a unique QA name, loaded through the existing Load dialog handler, rendered with original layout and null regions/metadata, then advanced a turn. Corrupt-save dialog handling retains the existing screen/state and displays an error. This closes the runtime dialog-path legacy gap from the prior entry.
- Normal solution rebuilt with zero warnings/errors after opt-in checks; **195 simulation tests pass** again. No game code/configuration/rules/content/theme was changed by this continuation.
- Native computer-use capture was blank despite correct engine framebuffer rendering; accessibility exposed only the window frame. These results use in-process control signals/handlers, not OS clicking. Small-window text is visibly downscaled by the existing 1920x1080 canvas, so comfortable readability needs human review.
- Final run retained eight unique QA files; diagnostic runs retained twenty more. Names/paths are in each run's JSON. Existing player saves were not overwritten or deleted.

**Current decision: still open.** Use [the observer sheet](first-playable-observer-sheet.md) with an uncoached participant for real control use, readability and explanations of choices/results. No participant observations have been supplied for this continuation. Record any blocker/clarity findings and the owner closure decision before exploration/colonization design.

## Subsequent owner disposition - 2026-10-22

The owner explicitly deferred the uncoached session because no unfamiliar participant is handy aside from the owner. See [ADR-005](../DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md). Human comprehension is **Deferred, not Verified**; full First Playable acceptance remains open. The owner nevertheless authorizes proceeding with exploration/colonization design now. This supersedes the earlier closure-before-design instruction for that transition. Do not keep asking for another participant or repeat technical tests as an invented replacement. Design review and separate implementation planning remain required before gameplay coding.
