# Shift selection for map fleets — 2026-11-08

Owner requested Shift+left-click to add or remove fleets so one destination click can order several fleets together. Plain click replaces selection; Shift-click toggles the complete fleet or independent-ship row. Every selected row remains highlighted. Existing MoveSelection submits one atomic group order with the same published preview/fingerprint; no simulation or range-rule changes.

The modifier is taken from the row input event rather than global key polling. Rows rebuilt during refresh retain their highlight from explicit ship selection. The previous split/merge/report behavior is otherwise unchanged.

Focused native MultiFleet green02 passes: add/remove, remove last, plain-click replacement, independent-ship toggle, read-only selection and hover, two selected fleets moved by one destination click with exact existing-command state equivalence and an unselected ship untouched. Captures at800×600/1920×1080 retained; compact selected-row presentation visually inspected. Fixture is explicitly generated before session, not an earned campaign. Physical mouse ergonomics and human acceptance remain Pending.

Retained failures: red01 corrected harness helper names; red02 reproduced missing Shift-add behavior. Green01 exposed that injected modifier key events do not establish the polled key state used by that implementation/check path. Green02 uses the modifier on the actual row click event in both product and check. No failed attempt is counted as passing.

Shared queue job766b4a341f8a4bf189869f4fbd7bf5a2 passes; native and evidence verifier pass, ordinary code restored. Game-only build0warnings/errors; normal namespace clean; no engine diagnostics; shared style guard passes. Diff whitespace check passes; src/content/schema unchanged. No full suite, split/report rerun, unrelated scenarios or campaigns. No commit, merge, push, release or cleanup.

Evidence: reports/visual-overhaul-execution/m14-multi-fleet-green02-summary.json. Stop the old run and press F5 in the same OpenAntares project. Open a system flag, plain-click one fleet, Shift+left-click another, then click a destination. Both selected rows should highlight and both fleets should receive the destination. Shift-click a selected row to remove it; plain-click selects only one. Report any mismatch and whether it prevented you continuing. Human acceptance remains separate.
