# M14 implementation review — 2026-11-08

One fresh whole-M14 review of the uncommitted `codex/ui-visual-overhaul` worktree against base `800a6012a4edfd0eae7c0d07841e03171c4c18dd`. Scope includes tracked presentation changes, new components, theme/assets, validation scaffold and player acceptance packet. The adopted plan AC section and execution-ledger rulings govern; the owner's targeted-verification rule supersedes broad suite requirements.

**Disposition: changes required.** No Critical finding. Four Important findings and one Minor finding follow. Findings are established by source/control-flow inspection; this review did not run or reproduce them in a new game process.

## Important

### M14-R1 — Preserve legacy entry routes when moving Scouts and Contacts into tabs

`game/Screens/GameScreen.cs:334–339`, `game/Screens/GameScreen.VisualOverhaul.cs:104–112`.

On rules 4–10 campaigns, `OpenDiplomacy` is hidden while `OpenContactsWorkspace` is now inside tabs shown only for Diplomacy/Contacts. There is no visible route into either workspace, so the formerly available Contacts overview becomes inaccessible. Rules 7 similarly has Scouts but no fleet-command feature: its Fleets key routes to `Vessels`, where the new Scouts tab is hidden. The occasional guidance route is not a replacement for ordinary access to the overview.

Keep a visible parent route for campaigns with Contacts but no Diplomacy, and show/route the Scouts tab through the rules-7 Vessels fallback. The tabs' parent keys also need the same feature guards so they cannot invoke unavailable controllers. Check only these two affected legacy navigation states; no legacy campaign replay is needed. `CombinedUiNavigation.Reveal` must not pretend that emitting a hidden parent key proves player reachability.

### M14-R2 — Route physical hotkeys from the focused embedded workspace

`game/Components/CommandBar.cs:59–77`, `game/Components/WorkspaceFrame.cs:12–18`, `tests/OpenAntares.Game.Validation/VisualOverhaulValidation.cs` direct `_UnhandledKeyInput` calls.

The command bar belongs to the main viewport, while the converted management controllers remain embedded Windows. After a player clicks the fleet list or another ordinary workspace control, physical key events go to that Window's viewport. Godot treats an event sent to a focused embedded Window as handled before the parent viewport's unhandled-key stage; therefore the parent command bar's guard/route method is never reached. For example, click a Fleet ship row and press C: the advertised Colonies shortcut does not navigate. The harness currently calls the method directly, bypassing this dispatch boundary. This follows the documented [Godot input dispatch order](https://docs.godotengine.org/en/latest/tutorials/inputs/inputevent.html).

Provide an input relay in the ordinary workspace viewport (or an equivalent single guarded routing mechanism), retaining text-entry, popup, confirmation and repeat suppression. Verify a real injected key event after focus enters an embedded workspace, plus one text-field/popup suppression case. Do not infer Escape behavior solely from direct method calls; the existing AcceptDialog cancellation path may handle it independently.

### M14-R3 — Remove per-ship identity from the equivalence key

`game/Components/FleetCommandDialog.cs:125–130`; supporting definition `src/OpenAntares.Simulation/Combat/CombatViews.cs:32`.

The grouping key serializes the entire `ShipCombatView`, whose `Id` is unique for each vessel. In every rules-9+ campaign, even two otherwise equivalent idle ships at the same location have different keys and become separate one-ship groups. This defeats UIX-AC-006's actual stacking/count behavior and makes a large fleet longer than the previous flat list.

Serialize the explicit order-relevant combat attributes rather than the whole record, while retaining design, request, fleet/officer, cargo, damage and commitment distinctions. Keep exact vessel IDs on leaves. A small constructed pair that must group and a differing design/officer/request case are sufficient focused coverage.

### M14-R4 — Prevent report-close restoration from reopening an untracked window during navigation

`game/Screens/GameScreen.VisualOverhaul.cs:74–79`, `game/Screens/GameScreen.Playability.cs:157–164`, `game/Screens/GameScreen.cs:391–404`.

Concrete sequence: open Fleets, open Latest turn report from Game, then click the Galaxy command-bar key while the report is visible. `ShowWorkspace` calls `CloseVisualWorkspace`, which sets `_changingVisualWorkspace` and hides the report. The existing report `VisibilityChanged` callback synchronously calls `RestoreWorkspace(_reportOrigin)` and reopens Fleets. Because `_changingVisualWorkspace` is true, that restored Window's visual bookkeeping is suppressed. The outer close then clears `_visualWindow`, and the requested Galaxy route finishes with Fleets still visible but no longer tracked. Subsequent ordinary routes cannot reliably close it; Escape also sees an unrelated visible Window and refuses the GameScreen route.

Coordinate intentional route changes with the report-origin restoration handler so a toolbar transition cannot restore an old workspace behind the router's back. Retain normal Continue/Back restoration and report excursions. A focused Fleets → report → Galaxy transition, then ordinary Continue-to-origin, addresses this defect; broad report/campaign replays are unnecessary.

## Minor

### M14-R5 — Keep readiness wording conditional on the authoritative check

`game/Presentation/TurnGuidance.cs:15`, `game/Screens/GameScreen.VisualOverhaul.cs` End Turn empty-state copy.

“Ready for the next turn” / “Ready to proceed” now claims readiness from the absence of projected notices. The existing `PlayabilityGuidanceFixtures.EngineRejections` combat-forecast case explicitly demonstrates no Required guidance while automatic containment closes a return port and the authoritative End Turn command rejects. This is an unchanged, known behavior, not hypothetical risk. Restore wording such as “No urgent notices. End Turn checks readiness.” and a similarly conditional End Turn subtitle. No simulation change is needed. The old exact-copy assertions should be reconciled narrowly if the retained honest wording changes.

## Reviewed evidence and limits

- Reviewed current tracked diff and new UI code, asset/font provenance and licenses, style guard, focused harness and player packet. Git inspection found no tracked or untracked changes under `src/`, `content/` or `schema/`; save/rules code remains unchanged.
- Inspected PlayerFlow01's results and verification record (`passed: true`, milestone `accepted: false`) and its Galaxy/Colonies 800×600 and Fleets 1920×1080 screenshots. Those belong to that earlier source, not latest-source acceptance. Latest compact-label/worker/body-scroll adjustments were inspected in source; the earlier cramped render is not reported as an unfixed defect.
- No new build, import, test, desktop interaction, commit, integration or cleanup ran in this review. PlayerFlow02 is the implementer's separate pending check. Historical M13/full-suite results remain historical.
- Status reserves/deltas use the existing published economy; worker actions use the existing command path; no new styling override was identified in the changed presentation code. These are inspection conclusions, not a new runtime pass.
- The global rejection message and dismiss/review actions are direct GameScreen children, outside the hidden context console (`GameScreen.cs:320–323`); no hidden-feedback finding is made.
- The player packet uses player journeys and report-back prompts rather than technical QA tasks. The supplied build/launch details still need to be concrete in the eventual handoff.
- Physical drag ergonomics, latest-source layout at every size, actual keyboard dispatch, report navigation, required-response persistence during every excursion, large-fleet performance, owner comprehension, accessibility and release/platform acceptance remain unjudged by this read-only review. Corrections should use the smallest checks tied to the findings above, not a full suite or second whole-implementation review.
