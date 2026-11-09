# Map fleets and right-panel report — 2026-11-08

Implemented the owner’s second playtest revision in the same OpenAntares checkout. One system flag opens individual named fleets and independent ships. Rows explicitly select ships; hovering previews without orders, and an actual destination click submits the existing move immediately. Rejected current-rule moves use a red route; no range/fuel rule was added. Right-click the galaxy canvas clears selection. Named fleet context offers Split with a member picker; dragging a named fleet onto another submits the existing Merge command, retaining the destination fleet. Independent ships can explicitly Form fleet first. Existing restrictions and fingerprints remain authoritative.

Turn Report now occupies the right console, keeping the map visible and Close report near End Turn. Existing report links and return behavior remain. Narrow-panel content wraps and scrolls. This supersedes the prior Set course button and compact floating report.

## Verification

Only the focused FleetPanel native case ran at800×600 and1920×1080. Final green03 passes: actual move/split/merge state equals the existing command reference; hover is read-only; one flag lists multiple fleets; rejected transit fixture shows blocked presentation; map right-click clears selection; report dock, close position and wrapping pass. Four layout captures were retained and visually inspected for the report and fleet list. The fleet roster is an explicitly labelled pre-session fixture, not an earned campaign. Physical mouse ergonomics and player comprehension remain unverified.

Retained attempts: red01 was a harness namespace compile correction; red02 reproduced missing requested behavior. Green01 passed interactions but visual inspection found report clipping. Green02 exposed fixed-width content and an overbroad label assertion; green03 corrects the minimum widths and checks report-content labels only. No failed attempt is counted as passing.

Final queued job8b59eecc8146454784f44fd6d816d729 passes. Evidence verifier passes with stable source/DLL and ordinary code restored. Normal game-only rebuild:0warnings/errors; validation namespace absent. Shared style guard passes with22/26 frozen allowances and no new overrides. No full suite, unrelated matrix, campaign replay or second reviewer. Simulation/content/schema roots unchanged. No commit, merge, push, release or cleanup. All old evidence retained. Human M13/M14 acceptance remains Pending.

Evidence: reports/visual-overhaul-execution/m14-fleet-panel-green03-summary.json and m14-fleet-panel-green03-FleetPanel/. Player guide: docs/playtests/m14-player-acceptance-2026-11-08.md. Stop the prior run and press F5 in the same OpenAntares project to try these changes.


## Flag spacing follow-up, 2026-11-08

Owner asked to move the flag slightly farther from its system. Increased the shared theme gap12→20pixels. Both drawing and clickable bounds derive from the same marker rectangle; static check confirms the ordinary-star marker now starts25pixels from center, beyond the21pixel system hit radius. Theme-only change: no rebuild, runtime scenario, full suite or campaign run. Owner visual/click acceptance Pending. The bare [n] label counts MapVesselMarker entries stationed at a system, both owned and foreign; it is not a colony or fleet count. No count behavior changed. The referenced marked image was not attached, so removal of the exact unwanted panel section awaits identification. Existing one-ship Shift deselection can be tried immediately; group selection needs another ship.
