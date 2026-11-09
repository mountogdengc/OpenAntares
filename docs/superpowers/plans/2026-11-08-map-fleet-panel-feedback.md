# Map fleet panel and docked report

Owner-authorized UI iteration, 2026-11-08. Range clarification: red means rejected by existing movement rules; no new fuel/range rule. No simulation/content/schema changes, commit, integration or second whole-M14 review.

1. Dock the real Turn Report controller in the right console, replacing its usual contents while open; Close report stays at the bottom above End Turn. Retain report links, excursions and ordinary return behavior. Use the same existing report content and command paths, with a narrow scrollable layout.
2. Keep one flag per occupied system. Clicking it lists named fleets individually and independent ships separately. A row selects only that fleet/ship; no selection is implied by opening the list. Right-click named fleet offers Split; a small reviewed split picker submits the existing Split command. Independent ships may explicitly Form fleet. Drag one named fleet onto another to merge into the target using the existing Merge command and preview fingerprint; no automatic creation/detachment/consent.
3. Actual system clicks with a selected fleet queue one existing move immediately, without Set course. Programmatic inspection never sends orders. Hover previews use the current move query; rejected destinations show a red line and rejection text, without a fabricated range limit.
4. Right-click on the galaxy canvas clears fleet, system and hover selections; right-click in the fleet panel remains its context action. Retain zoom/pan and separate system/flag hit testing.
5. One focused FleetPanel native case: one flag/multiple fleets, row selection, hover no orders, red rejection, immediate movement equivalence, right-click clear, split and drag merge equivalence, dock/close position at800×600 and1920×1080. No unrelated suites or campaign replay. Preserve owner edits and all raw failures/evidence.

Files: GameScreen MapNavigation/new FleetPanel partial, GalaxyMap/MapOverlays, TurnReportDialog, new MapFleetRow/MapFleetSplitDialog components, shared theme metrics/colors, isolated validation case and player guide/status. Old interaction records remain historical after the explicitly superseded Set course and popup-report behavior.


Owner follow-up: Shift+left click toggles a row in the current map fleet selection; plain click replaces selection. Highlight each selected row and retain the existing atomic multi-fleet MoveSelection destination command. Apply consistently to independent-ship rows. Verify only modifier toggles, row highlights, read-only selection/hover and one multi-fleet destination, not prior split/report or campaign scenarios.
