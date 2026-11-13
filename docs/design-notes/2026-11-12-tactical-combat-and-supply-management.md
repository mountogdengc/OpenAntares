# Tactical combat and supply management directions

2026-11-12. **Status: Recorded owner direction.** The owner requested adding the combat ideas discussed together and a supply management layer. The direction is accepted for design work; the exact rules, resource scope, defaults and implementation sequence still need a bounded specification. This note extends the earlier [fleet intent](2026-10-24-delegation-crises-and-fleet-intent.md) and [combat retreat](2026-10-27-combat-retreat-morale-and-damaged-ships.md) notes.

## Intended experience

Keep empire strategy central while making tactical decisions consequential. Manual control should reward planning and adaptation; competent automated captains should execute ordinary orders without requiring the owner to rescue ships from basic mistakes. Supply management should connect economic specialization, military reach, blockades and transport design while respecting the charter's depth without busywork pillar.

## Owner priorities

- Increase battlefield width and depth enough to support approach, maneuver, screening, disengagement and pursuit. Tune map scale together with weapon ranges, deployment and movement so larger maps do not merely add empty travel turns.
- Vary battlefields according to the encounter's actual location, including asteroids, nebulae and other space objects. The strategic choice of where to intercept or defend should affect the tactical circumstances.
- Preserve scouts' opportunity to detect danger and escape. Gathering useful information and returning can be a successful mission without defeating an enemy. Connect this to the existing scout survival direction rather than replacing it with compulsory combat.
- Give small, fast, maneuverable ships a meaningful defensive advantage, balanced against lower durability and other design costs. Distinguish travel speed, maneuverability and detectability; they need not improve together.
- Add maneuvering technologies and equipment choices that support distinct ship roles.
- Support blockade runners: ships designed to penetrate interception and deliver useful cargo, with costs and risks. Their success should depend on capabilities and circumstances, rather than a universal exemption from blockades.
- Add a supply management layer covering both colony food/civilian goods and fleet supplies. Exact goods and consumables remain to be specified.
- Include migration in the transport layer: natural migration between colonies alongside an explicit option to move migrants in bulk as a strategic action. Neither form should replace the other.

## Combat proposals retained from the discussion

These are design candidates from the assistant's suggestions, included in the owner's request to add the discussion. Their examples are not fixed mechanics or numerical approvals.

Captains should understand their equipment and assigned role: maintain suitable range, protect vulnerable ships, choose sensible targets, use special systems coherently and avoid unnecessary waste. Group orders could express escort, range, interception, disabling or capture intentions, with optional direct intervention.

Reduce battles decided before the opponent has a meaningful response. Evaluate deployment separation, long-range accuracy, defensive reactions, activation order and weapon recovery together. OpenAntares currently resolves strategic exchanges; this proposal does not assume a tactical map or initiative system already exists.

Positioning should change the available choices. Candidate terrain effects include asteroid obstructions and protected approaches, nebula interference with detection or targeting, and routes around planets or moons. Open space should also support a useful style of fleet combat. Terrain effects must be readable, navigable by the AI and grounded in consistent game rules.

Allow objectives that follow from the strategic situation: protect transports, cover a retreat, delay an attacker or capture a valuable vessel. Explain firing opportunities, expected consequences, incoming threats and unavailable abilities before commitment.

## Maneuvering and blockade running proposals

Candidate equipment includes maneuvering thrusters, engines, countermeasures and reduced sensor signature. Balance equipment through space, cost, cargo, armor, weapons and operational requirements rather than making every upgrade an automatic improvement. Accurate tracking, interception craft and detection coverage should offer counterplay.

A blockade runner should emerge from equipment, cargo capacity and orders. Possible designs include a small fast courier, a stealth transport and a tougher transport that accepts interception risk. These are examples, not an approved roster.

Treat penetrating a blockade as a sequence of detection, interception, possible combat or evasion, delivery and departure. A shipment may get through while the blockade remains strategically effective. Define cargo loss, aborted delivery and return behavior explicitly; a failed run must not silently become automatic civilian destruction. Reconcile this with the current protected-civilian conflict rules before adding attack or capture permissions.

## Supply management scope

The owner selected both colony and fleet logistics after considering these approaches:

1. Colony logistics: population support or food equivalents, civilian goods, storage and transport between colonies.
2. Fleet logistics: selected consumables, operational endurance, replenishment and repair support.
3. Both, with a shared transport and interdiction foundation and staged delivery. Both domains are owner-selected; shared infrastructure and staged delivery are assistant recommendations.

The assistant recommends automating routine deliveries inside player policies, with explicit priorities, reserves, route restrictions and risk limits. Players would decide which colonies or fleets to sustain, where to invest in capacity and escorts, and when a dangerous delivery is worth attempting. Detailed manual shipment control can be optional. This is a recommendation for review, not an owner-selected default.

Candidate consequences include delayed replenishment, reduced readiness, slower repairs and colony shortages. Exact resources, storage units, production, consumption, travel times, shortage progression and possible population loss require separate choices. Respect species-specific support needs and the game's existing crisis and civilian protections.

Show available stock, incoming deliveries, demand, capacity, expected endurance and the reason a route is delayed or blocked. Distinguish what is known from hidden threats. Give actionable warning and available responses before preventable severe shortages.

## Natural migration and bulk transfers

The owner wants natural migration and deliberate bulk migration to coexist. Natural migration provides ongoing population movement without ordering every shipment; bulk transfers let the player concentrate migrants for settlement, development or another chosen strategy. Exact eligibility, motivation, rates and limits remain open.

The assistant recommends that migration use the same underlying route and transport-capacity rules as civilian supplies, while retaining a distinct population manifest and accounting. Departure and arrival must conserve actual population, its species identity and in-transit occupants. Respect existing species settlement restrictions and special population semantics rather than treating every species as interchangeable cargo.

Proposed natural-migration drivers include available capacity, living conditions, opportunities and population pressure; these are candidates, not fixed formulas. Give players policies for incoming and outgoing migration, colony population targets or minimums, destination eligibility and acceptable travel risk. Natural migration should not silently undo a bulk transfer or drain a colony below a deliberately protected workforce.

Bulk transfers should preview the number and identity of migrants, origin workforce/support changes, destination capacity and support demand, available transport capacity, travel time and known route risks before commitment. Automated migration should obey the selected policies, available capacity, destination acceptance, quarantine and other lawful restrictions. Define cancellation, rerouting, blocked arrival, interception and evacuation separately; ordinary migration does not imply permission to sacrifice passengers.

Transport scarcity should create understandable choices between civilian supplies, fleet replenishment and passengers. The priority system and any reserved capacity require review. Example: sustain a dependent colony, replenish a distant fleet or move workers to a developing world; blockade runners may keep a limited critical flow open without ending the blockade.

## Current implementation baseline

The current main checkout calculates population support separately for each colony. Support shortage reduces production and research and pauses ordinary growth; surplus contributes to growth. This is not an existing intercolony food shipping network. See [EconomyCalculator](../../src/OpenAntares.Simulation/Economy/EconomyCalculator.cs) and [economy results](../../src/OpenAntares.Simulation/Economy/EconomyResults.cs).

Current ship and sentry exchanges apply damage from firepower and armor without an accuracy or evasion roll. Speed affects travel and withdrawal interception, but does not make a selected target dodge an ordinary shot. See [combat exchange](../../src/OpenAntares.Simulation/Combat/CombatExchange.cs), [empire exchange](../../src/OpenAntares.Simulation/Conflict/ConflictExchange.cs) and [withdrawal](../../src/OpenAntares.Simulation/Conflict/ConflictWithdrawal.cs).

Current blockades reduce retained colony production; they do not model physical supply shipments. See [blockade conditions](../../src/OpenAntares.Simulation/Conflict/ConflictBlockades.cs) and [economic effects](../../src/OpenAntares.Simulation/Conflict/ConflictEconomy.cs). A physical supply system needs an explicit relationship to that abstraction so the same disruption is not accidentally penalized twice.

## Next design decisions

The supply scope is both civilian and fleet logistics, with natural migration and deliberate bulk transfers. Next define the resource model, migration policies and the dependency order between transport and cargo, detection and interception, maneuvering and accuracy, and tactical battlefield control. Respect the [charter](../CHARTER.md): tactical combat remains optional relative to completing the strategic game.

Use deterministic simulation rules, the same command authority and permitted knowledge for humans and AI, transparent calculations and explicit save compatibility. Review a concrete gameplay specification before creating its implementation plan. Recording these directions does not change game rules, existing acceptance, save formats or milestone completion.
