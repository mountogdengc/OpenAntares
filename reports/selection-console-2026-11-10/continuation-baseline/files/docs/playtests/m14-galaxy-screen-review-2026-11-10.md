# Galaxy screen review and fix list — 2026-11-10

Source: owner's integrated main build (M13/M14 plus uncommitted map-key changes), new Harl / Small / one Peldra rival game at turn 0, maximized on the owner's ultrawide monitor, plus the owner's four console screenshots (fleet selected, System, Empire, Fleets tabs). Review only: no code changed, no orders issued in the review game.

## Core finding: the console and the command bar do the same jobs

The 2026-11-08 mockup gave the right console System / Empire / Fleets tabs while the bottom bar already had Colonies, Fleets and Decisions. The build inherited that overlap:

| Job | Bottom command bar | Right console | Also |
| --- | --- | --- | --- |
| Fleet roster | Fleets (F) workspace | Fleets tab (system + vessel dropdowns) | Flag click replaces the console with "FLEETS AT …"; "Vessels / travel" and "Review vessel orders" buttons |
| Empire decisions | Decisions key (toggles the Empire tab) | Empire tab "Responses to review" | End Turn "2 to consider" |
| Colonies | Colonies (C) ledger | "Colony overview / alerts" button on every tab; "Manage colony" twice in System tab | Planet card "Manage colony" |
| Conflict | Diplomacy (P) | "Conflict orders" button on every tab | — |

**Rule for the fix:** the bottom bar opens empire-wide screens; the right console shows only the thing currently selected on the map. No tabs in the console. Selection decides what it shows:

- Nothing or a star selected → system card (planets, owners, fleets in this system).
- Planet selected → that planet/colony card.
- Fleet flag selected → fleet card (ships, status, orders, course).

Everything empire-wide moves to the bar: the Empire tab's content goes to Decisions (ideally the top-edge attention strip from the fleet-mode discussion); the Fleets tab is dropped in favour of the Fleets workspace and, later, the fleet-mode roster drawer; Conflict orders lives in Diplomacy and appears in the console only when the selected system has a conflict; Colony overview lives under Colonies.

## Fix list, in priority order

### P1 — structure and scale

1. **Console = selection only.** Remove the System/Empire/Fleets tabs and the always-on Conflict orders / Colony overview buttons, per the rule above. A flag click shows the fleet card in place instead of taking over the console layout.
2. **One fleet UI.** Delete the Fleets-tab dropdowns. The fleet card and the Fleets workspace use the same row component (role icon, name, status, destination, ETA). One naming scheme: today the same ship is "Scout · independent" in one place and "Scout 51" in another.
3. **UI scaling.** On the ultrawide monitor, labels, strip text and overlay keys are tiny and stars are ~4 px. Use a canvas stretch mode and/or a UI-scale option in the Game menu.
4. **Fit fills the map.** Fit currently scales by height, leaving two-thirds of a wide map black. Fit the galaxy's bounds to the whole map area.
5. **Decisions shows its items.** End Turn says "2 to consider" while the Empire tab says "No urgent notices" and hides both items behind "Consider (optional) +". Whatever the Decisions key opens lists them directly.

### P2 — map information

6. **Draw moving fleets** on their route lines (map markers already carry origin, destination and turns remaining).
7. **Own vs foreign flags with counts** in empire colours, not the selection cyan; several fleets at one star should show a count rather than one flag.
8. **Ownership always visible** (thicker ring or territory tint); Threat and Habitat layer on top instead of replacing it.
9. **Range overlay:** hide it until it works; a disabled key reads as broken.
10. **Fill the empty console** with what belongs to the selection: "Fleets in this system" under the planets; ship details in the fleet card.

### P3 — polish and bugs

11. **Stale map tooltip:** a "Lorvan" box stays on the map while hovering other stars (visible in every screenshot).
12. **Label collision:** "Lorvan [1]" overlaps the home star's rings and flag; the `[n]` count mixes own and foreign ships.
13. **Status strip:** three tiny readouts spread across the full width with large gaps; group them at the left, explain "0 (+4)" (stored / per turn), add the research target and progress bar.
14. **Command bar:** Game key has no frame; Diplomacy and Decisions share the warning-triangle icon; Diplomacy's "P" hint drops off its label after map clicks.
15. **System tab cards:** planet names wrap ("01 Crystal / World"); widen the text column. Remove the second Manage colony button.
16. **Chrome weight:** frames are nested three deep and headings use the thin cyan display face at low contrast. Flatten to one frame level inside the console and use the bolder weight for headings.
17. **Heading case:** "FLEETS AT Lorvan" mixes caps; use one style.

## Not verified

Only turn 0 of one small game at one resolution was seen; late-game crowding, other rules versions, 800×600 and keyboard paths were not checked. The mockup at the Galaxy Screen Mockup canvas should be revised to the selection-only console before it is used as a reference again.
