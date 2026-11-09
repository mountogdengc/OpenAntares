# OpenAntares owner playtest UI revision — 2026-11-08

The owner confirmed the build runs and supplied concrete interaction changes. This is a newly authorized UI revision on the existing isolated codex/ui-visual-overhaul working copy, not an inferred extension of the previous six-fix allowance. No commit, main integration, publishing or cleanup.

Implemented:

- Removed the automatic campaign-start message and visible Back-to-X header buttons. Primary management workspace sizes are retained; Escape still returns through navigation context.
- Added Game → Main menu (ID5); IDs0–4 retain their previous meanings. Resume uses the exact same campaign session; New Game and Load Game use the existing root flows.
- Bottom empire keys toggle their workspace closed to Galaxy. Decisions toggles its Empire context. Galaxy always shows the map and, when already there, clears fleet selection and centers the home system.
- Research opens its workspace without popping its choice list.
- Explicit map fleet selection is separate from the inspector's automatic starting-ship selection. System glyph and fleet flag hit areas are separate. No selection means no ETA. Fleet flag → destination → Set course performs movement on the map using the existing atomic FleetAction.MoveSelection and current preview fingerprint. Whole named fleets are resolved as groups rather than detached; older campaigns retain their existing single-ship move command. Existing safety/consent/rejection authority remains in the command processor.
- Mouse-wheel cursor-anchored zoom, +/−/Fit controls and middle-drag panning share one draw/pick transform. Zooming back out restores the fitted map.
- Turn Report is a compact, scrollable popup:760×560 maximum, capped at80% of the game window, resized with the window. It is no longer fitted to the full management workspace.

## Change-scoped verification

Only the isolated native PlaytestFeedback case ran. The initial RED run reproduced the reported banner, toggle, dropdown, header, ETA, zoom, map-course, report-size and menu-option failures. The first implementation compile had one local-variable name collision in the pan handler; it was corrected and its failed output retained.

GREEN02 passed the requested interactions. GREEN03 adds the Decisions toggle, cursor anchoring, zoom-out-to-fit and pan checks and passes **71 checks with4 frames** at800×600 and1920×1080. It verifies the actual map course equals one existing atomic fleet command; navigation/inspection/selection/zoom issue no orders; Main menu/Resume preserves the exact serialized campaign. Source/DLL/restoration evidence verifier passes with no errors; development-only, not human acceptance. Engine logs have zero diagnostics. Normal restoration builds the game with0warnings/errors and excludes all validation types/helpers.

Queue job:7ecb05ebfc484df6b00a998230a878e6. Evidence: reports/visual-overhaul-execution/m14-owner-feedback-green03-PlaytestFeedback and its control directory. Styling boundary passes22/26 retained legacy occurrences, no new override/allowance. Git diff check passes. No changes under src/content/schema; the owner's project.godot editor changes are preserved.

No full Windows suite, unrelated scenario matrix, campaign audit/future replay, second whole-M14 reviewer, export, platform or physical OS-input acceptance ran. Older exhaustive results remain historical. Named-fleet resolution is preserved in the existing group-command path; this focused live movement comparison uses the initial owned scout rather than claiming an earned large-fleet campaign.

The updated player guide describes map movement and menu controls in player terms. Next: stop the previous Godot run and press F5 in the same OpenAntares project, then recheck the requested interactions. Human usability acceptance remains Pending for this revision. All prior evidence and branches/worktrees are retained.

Final ordinary build record: reports/visual-overhaul-execution/owner-feedback-build01/source.json; normal metadata is empty. Snapshot queuee474113425284e4da1bba89fc9044551. The earlier snapshot attempt was rejected by the preservation guard because its parent directory already retained a DLL archive; it did not overwrite that archive. A new directory was used successfully. The existing Play M14 shortcut now verifies this updated normal build; its prior launcher was retained separately. No new game window was forced open over the owner's Godot playtest.
