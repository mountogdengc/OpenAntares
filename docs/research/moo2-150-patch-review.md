# Research Note: The MOO2 1.50 Community Patch

A review of the unofficial 1.50 patch for *Master of Orion II* (1996) and the rulesets that ship with it, based on the [1.50 patch manual](https://moo2mod.com/manual/MANUAL_150.html) published at moo2mod.com.

The patch is useful to us because it records, in one place, twenty years of player frustration with a classic 4X game. It shows which repetitive chores players wanted automated, which rules confused them, and which balance problems they tried to correct.

This is a study of player needs, not a feature list to reproduce. Per the [charter](../CHARTER.md), OpenAntares does not copy a legacy game's interface or mechanics feature-for-feature.

## 1. Usability improvements: what repetitive work did players need help managing?

Most of these deal with doing the same thing many times across many colonies or ships.

- **Filling build queues on every colony.** This was the biggest chore. The patch adds 11 saved build lists bound to hotkeys. Each list can be added to the end of a queue, the front, or merged in. Lists can include commands such as buy, partial buy, unbuy, clear refits and stop. A modifier key applies a list to every colony at once.
- **Keeping ship designs current.** The patch adds a sixth design slot and a hotkey that refreshes every slot with the AI's current designs.
- **Moving fleets one at a time.** One hotkey tells all ships to wait, and another measures distances between stars.
- **Mistakes you couldn't undo.** Pressing Buy a second time cancels the purchase and refunds the money.
- **Missing numbers on colony screens.** The screens now show colonists already on the way, population growth including the fraction normally rounded away, and stored production against the item's cost. Leader experience is now visible too.
- **Safety filters.** The "no enemy presence" planet filter now only lists planets you've visited, so you don't send ships to an unexplored world that turns out to hold a monster.
- **Autosave.** The game now saves every turn before combat instead of every four turns.
- **Multiplayer waiting.** New settings close battle notices and boarding-result pop-ups automatically, so one player isn't left waiting on a dialog the other player has to dismiss. They were added for the Melee mod (see part 3).

## 2. Bug fixes and mechanical explanations: what rules produced surprising or unclear outcomes?

### Combat that was unpredictable or happened in an odd order

- **Initiative rounding.** The old formula, `trunc(beam_attack / 10) + speed`, dropped small attack bonuses entirely. The patch uses `beam_attack + speed × 10`, and settles ties with a fixed seven-step order instead of semi-randomly.
- **Area weapons** had a targeting range that didn't match their damage range. Both are now measured from the firing ship's center to the target's edge.
- **Damaged and repaired systems.** A destroyed weapon now loses its remaining shots right away, not at the end of the turn. Repaired systems start working again mid-battle.
- **Auto-designed ships** picked weapons using the tactical-combat damage column, which produced bad designs for the auto-resolved (strategic) battles they actually fought. Bombers also did zero damage in strategic combat.

### Gaps that broke population and economy rules

- **Population cap leaks.** An extra android, or colonists delivered by freighter, could push a colony past the cap of 42.
- **Ghost colonies.** Colonies with buildings but no people could not be invaded. Colonies with neither now disappear unless colonists are on the way.
- **Lost research.** Research points from turn 0 vanished if no tech had been chosen. One tech tier showed a cost that didn't match what was actually charged.
- **Spy exploit.** Scrapping extra spies paid out 100 BC. It now refunds half the build cost.
- **Double income from Trade Goods.** The Melee mod's multi-build setting makes a colony set to Trade Goods earn both money and production each turn. It's a known bug, and it makes the setting unusable for any mod with a normal economy. One setting can quietly break the economy.

### Displays that gave wrong numbers

The heavy-gravity penalty showed −50% when it was really −25%. The racial missile defense bonus was shown at half its value, and the drive speed shown left out a bonus. Defense bonuses were shown where they didn't apply. Several of these were display bugs only: the rules were right but the screen was wrong.

### Multiplayer

Many desync and stall bugs are fixed. Causes included techs gained mid-battle, random mutations, boarding raids with zero marines, and fleets of more than 99 ships.

### Explanation chapters

Separately from the fixes, the manual includes "Notes on…" chapters that lay out the hidden formulas. They cover population growth, spying (bonuses, roll chances, spy vs. spy), beam weapon accuracy and damage, missile defenses, anti-missile rockets, area damage, shields/armor/structure, raiding, and orbital assault. The community had to write these because the original game never showed any of this.

## 3. Optional balance mods: what changes attempt to make more strategies viable?

The patch keeps the standard ruleset separate from the alternate ones. Players choose a ruleset in a launcher, and picking none gives the standard patch.

### Standard patch vs. the Improved mod (150i)

| | 1.50 standard patch | 1.50 Improved mod (150i) |
| --- | --- | --- |
| Purpose | Fixes bugs and adds conveniences. Default settings and no balance changes. | Curated balance changes added on top of the standard patch |
| Racial picks | Original costs | Point costs adjusted slightly so more race builds are competitive |
| AI | Original logic, with bugs fixed | Better ship designs, better use of colony bases, researches Automated Factory early |
| Weapons and specials | Original stats | Tuned stats. The full list is only in the mod's config files and a companion spreadsheet. |
| Tactical rules | Original rules, with fixes | Updated tactical combat rules turned on |
| Research | Extra research beyond what's needed is lost | Up to 2× the research cost carries over to the next tech |
| Terraforming | Toxic planets can't be changed | One building turns Toxic planets into Barren ones |

### Melee mod: an alternate ruleset, not a balance mod

The Melee mod is an experimental, separate game mode: a two-player tactical combat arena built on the same engine.

- **Goal.** It removes the 4X game and keeps only ship design and tactical combat.
- **Map.** Four systems: Orion, two home systems and one neutral system where the battles happen.
- **Loop.** Each player builds 30 command points of ships, and the fleets fight at the neutral system with no retreat allowed. The loser designs ships to counter what won and rebuilds. The winner tops up their surviving fleet back to 30 points, and can use new designs.
- **Command point cost by ship size:** Frigate 1, Destroyer 2, Cruiser 6, Battleship 10, Titan 15, Doom Star 30. One Doom Star uses the whole budget, so players must choose between one huge ship and many small ones.
- **Settings it requires.** Small galaxy, exactly two players, and tactical combat on. Other player counts and the highest starting tech level crash the map generator. Random events and the Antaran raiders are pushed back 127 turns, which in practice turns them off.

### The config system underneath

All of these rulesets are possible because the patch moved game rules into plain-text config files. Those files cover buildings, weapons, techs, population, spying, AI and events, and players can edit them without programming. 150i is the same engine with different values.

## Lessons for OpenAntares

The charter already covers the main lessons. This review is evidence for each one.

- **Explain the numbers.** The community had to write chapters of formulas and fix numbers shown wrongly on screen. Breakdowns of every important value, with their sources, would make that unnecessary.
- **Plan for bulk management.** Build lists that apply to every colony were the most important usability fix. Colony and production commands should allow for batch use early, even if the interface for it comes later.
- **Keep content in data.** A balance ruleset like 150i is just different values on the same game. That matches our JSON content and modding approach, and our rulesets should stay clearly separate from the base game in the same way.
- **Test cross-setting interactions.** The Trade Goods double-income bug came from a setting nobody tested against the normal economy. Data-driven rules need tests that combine settings, not just test each one alone.
- **Determinism matters in multiplayer.** Many fixes were desyncs, where two machines disagreed about the outcome. That supports the charter's rules on whole-number maths, the seeded PRNG and fixed iteration order.
- **Keep combat separable.** Melee shows that players wanted to practise ship design against each other without playing a whole campaign. Tactical combat comes after the strategic game in our plans, but its rules should be separate enough that a scenario like this could run on its own.
