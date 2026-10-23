# First Playable validation continuation - 2026-10-22

**Result:** Fresh graphical runtime checks pass; First Playable remains open for uncoached understanding and complete physical-input/readability acceptance. No gameplay source, rules, core content, theme, architecture or project configuration changed during this continuation.

**Subsequent owner decision:** [ADR-005](../docs/DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md) defers the unavailable uncoached session and authorizes expansion design now. The run results below remain unchanged; statements about closure preceding design describe the pre-deferral gate and are superseded for progression by that explicit instruction.

## Final accepted run

- Windows NT 10.0.26300.0 x64; Godot 4.7.2 .NET, native Windows display, GL Compatibility / OpenGL 3.3, NVIDIA RTX 3090 / driver 617.42.
- Source HEAD remains `6ab206d3ca4811cc9163ee6ea3d5c00ff4bb7b6b`, plus the previously existing local UI/config edits. A retained opt-in test harness was added under `tests/OpenAntares.Game.Validation/`.
- Runner: **exit 0**. **463 assertions passed, 0 failed**, plus 27 popup-geometry observations (490 JSON entries total). Geometry observations are not additional assertions.
- Normal solution rebuilt after the harness: **exit 0, 0 warnings/errors**. The opt-in class is absent from the ordinary game assembly.
- Final simulation regression: **195 passed, 0 failed, 0 skipped**, exit 0. [TRX](test-results/first-playable-2026-10-22-final.trx).
- Final Godot log contains no engine `ERROR:` or `WARNING:` diagnostics. [Log](first-playable-2026-10-22-run4/godot.log), [structured results and retained save paths](first-playable-2026-10-22-run4/results.json).

```powershell
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -OutputDirectory reports/first-playable-2026-10-22-run4
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo --logger 'trx;LogFileName=first-playable-2026-10-22-final.trx' --results-directory reports/test-results
```

For another run, choose a new output directory. The runner uses an opt-in MSBuild targets import, then rebuilds the normal solution in `finally`. No project-file edits or production hooks are required. [Runner documentation](../tests/OpenAntares.Game.Validation/README.md).

## Evidence method and limits

The harness instantiates the actual entry scene, configures the New Game form, and dispatches the real controls' signals and map GUI handler in-process. GameScreen's existing handlers issue commands, show reports, and execute Save/Load dialogs. Reflection reads the published session and map bounds for assertions; it does not change authoritative state. This exercises application wiring and graphical layout, **not Windows mouse/keyboard delivery or player comprehension**.

Native computer-use selection succeeded after an initial app-approval timeout. Its game capture remained blank after activation, and accessibility exposed only the window frame. The engine-generated framebuffer rendered the actual game correctly. No game input was guessed from a blank capture; the validation windows were closed without saving. This capture limitation was not treated as a product defect.

The popup cases deliberately shorten the side panel to force scrolling; ordinary production layout is unchanged. All 24 native cases compare report-opened popup position with reopening the same selector normally after layout. Native child windows are absent from parent framebuffer screenshots, and their GL texture readback failed. For supplementary visual evidence only, three extra fixtures request embedded subwindows, disable `ForceNative` on that test popup, assert actual embedding, then capture it. Those screenshots are explicitly **embedded fixtures**, not native Windows captures.

At 800x600, the existing 1920x1080 canvas downscales text substantially. Runtime checks establish layout/state stability; they do not establish comfortable readability. Include this in the human session before accepting small-window usability. No font/layout policy was changed.

## Runtime acceptance matrix

| Area | Small / Medium / Large, seed 42 | Scope |
| --- | --- | --- |
| New Game / generation | Pass; 12 / 16 / 20 selectable systems, one colony/population 6 | Real New Game Start handler; size/seed configured programmatically |
| Map / inspection | Pass; each system's handler updates inspector; background point does not select | Handler dispatch plus rendered screenshots; physical input not tested |
| Workforce | Pass; shortage feedback and deficit 300, supported allocation growth 40, restored balanced allocation | Real workforce-button signals and forecast state |
| Production / research | Pass; both complete at turn 3; targets clear; hall contributes next snapshot | Existing selectors, End Turn and report handlers |
| Growth | Pass; balanced colony reaches population 7 at turn 10 | Existing End Turn control, no simulation-state mutation by harness |
| UI save/load | Pass before/after completion; exact content/state and map bounds retained; continued turn matches uninterrupted result | Existing Save/Load dialog confirmation handlers |
| Corrupt save | Pass; visible load error, same screen and authoritative state retained | Small run; isolated invalid QA file |
| Legacy schema-1 load | Pass; original layout/null metadata retained, renders and continues a turn | Small run; copied fixture through existing Load dialog |
| Live resize | Pass at 800x600, 1152x648, 1600x900, 1920x1080 | Actual window dimensions, nonempty map layout, authoritative state unchanged |
| Construction FP-001/002 | Pass in 24 native cases: 3 sizes x 4 dimensions x 2 prior selections | Opens, focus-scrolls, selects home, position matches normal reopening, next project reaches command path |
| Supplemental popup visuals | Pass in 3 actual embedded fixtures | Menu visibly attached below selector; does not establish native-window pixel capture |
| Research follow-up | Pass for all three sizes | Completion report opens research picker |
| Default window | Pass, actual 1920x1080 | Graphical root window assertion |

## Visual review

Sixteen final framebuffer PNGs are retained. Reviewed startup views show recognizable spirals, distinct selectable systems and home/selection markers, planet/region information and colony controls. The legacy view keeps its old non-spiral layout without invented region text. The supplemental embedded-popup views show the menu directly below construction after scrolling.

- Startup: [Small](first-playable-2026-10-22-run4/small-initial.png), [Medium](first-playable-2026-10-22-run4/medium-initial.png), [Large](first-playable-2026-10-22-run4/large-initial.png).
- Live resize examples: [Small 800x600](first-playable-2026-10-22-run4/small-resize-800x600.png), [Small 1600x900](first-playable-2026-10-22-run4/small-resize-1600x900.png).
- Scrolled embedded-menu fixtures: [Small](first-playable-2026-10-22-run4/small-800x600-other-embedded-construction-parent.png), [Medium](first-playable-2026-10-22-run4/medium-800x600-other-embedded-construction-parent.png), [Large](first-playable-2026-10-22-run4/large-800x600-other-embedded-construction-parent.png).
- [Legacy schema-1 loaded](first-playable-2026-10-22-run4/legacy-v1-loaded.png).

## Diagnostic runs retained

| Run | Disposition |
| --- | --- |
| `first-playable-2026-10-22-run1` | Initial graphical pass: 367 assertions, all passed; no live-resize assertions or full popup-window capture |
| `first-playable-2026-10-22-run2` | Failed in added test instrumentation: native popup texture had no readable GL data, followed by null image exception. Preceding gameplay checks passed; normal rebuild still ran. This was not a gameplay failure |
| `first-playable-2026-10-22-run3` | 460 assertions passed, plus 27 geometry observations. Supplemental embedding was requested but not proven, and the parent screenshots omitted the menu; superseded by explicit `ForceNative` handling/assertion in run4 |
| `first-playable-2026-10-22-run4` | Final accepted harness and evidence: 463 passing assertions, 27 geometry observations, actual supplemental embedding asserted |

Each attempt uses unique QA names and retains its own result/log evidence. The four runs created 28 new QA files in total (8/4/8/8), including deliberately invalid copies and legacy fixtures; no existing save was overwritten or deleted. Exact paths are in each run's `results.json`. The final run's eight files are sufficient to reproduce its gameplay state; prior diagnostic saves are not required for acceptance.

## Disposition and next step

No new gameplay defect was established by these runtime checks. FP-001/002 now have retained reproducible coverage instead of relying only on removed temporary scenes. The previous legacy UI-loading evidence gap is closed for the runtime dialog path; physical-input acceptance remains separate.

**First Playable remains still open.** Complete the uncoached participant session using [the observer sheet](../docs/playtests/first-playable-observer-sheet.md), including real controls, small-window readability and explanations before/after turns. Record any blocker/clarity findings and the owner closure decision. No such observations were supplied during this run, so AC-014 cannot be marked Verified. REQ-010/011 retain their complete-acceptance unverified status despite the passing runtime subset.

Cross-platform/export/dependency-audit/integration gaps remain as reported by adoption. Exploration and colonization design still follow closure; no broad gameplay work or Git integration occurred.
