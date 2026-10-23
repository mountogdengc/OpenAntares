# OpenAntares project concept

Reconciled 2026-10-22 from [CHARTER.md](CHARTER.md). This is an adoption summary of the existing project, not a new product proposal.

## Identity and outcomes

- **Name:** OpenAntares (the repository's established spelling).
- **Type:** Existing application; original open-source turn-based space 4X game.
- **Opportunity:** Provide meaningful strategic decisions, understandable consequences, distinct civilizations, and extensible content without mandatory busywork or a dependency on nostalgia.
- **Users:** Desktop strategy players; later content creators and contributors. No narrower audience or commercial plan is established.
- **Platforms:** Windows, Linux, macOS. Web is excluded by existing project guidance.
- **Loop:** Explore, expand, develop, research, negotiate, build, compete, adapt.

The supplied OpenJRPG workflow is a reusable process reference. Its RPG systems, authoring tools, package policies, and ADR-014/015 are not OpenAntares decisions. No engine rename, rewrite, or new framework is needed for this adoption.

## Scope

| Classification | Established scope |
| --- | --- |
| Essential now | The charter's eleven First Playable actions: new game, galaxy generation/map, star/planet inspection, one colony, workforce, production, research, end turn, visible advancement, save/reload |
| Active milestone | Empire knowledge, exploration, travel and a second colony; full design and Native plan approved, expansion implemented locally with independent review/corrections complete; human playtest remains deferred under ADR-005 |
| Later version 1.0 | Multiple civilizations, AI, fleets/ships/design, strategic warfare, diplomacy, events, setup options, victory/defeat, broader technology and development |
| Optional | Tactical combat only if it does not impede completing the strategic game |
| Excluded initially | Real-time strategy, MMO/persistent online world, proprietary backend dependence, mandatory online single-player, copied commercial assets or interfaces |

[SPECIES_ROSTER.md](SPECIES_ROSTER.md) records accepted creative direction for Peldra, Harl, Sulan, and Wenth, explicitly without implemented species mechanics or numerical modifiers. Its current local edits are preserved.

## Constraints and success

Use the existing C#/Godot .NET architecture, headless deterministic simulation, commands, stable IDs, data-defined content, and theme-based UI. See [ARCHITECTURE.md](ARCHITECTURE.md) and [CLAUDE.md](../CLAUDE.md). Code/content are MIT; original assets CC0; docs CC BY 4.0.

First Playable succeeds when all eleven actions work, saved games continue consistently, consequences are understandable, and workforce/project choices are meaningful in play. Technical tests alone do not establish enjoyment or comprehension. [Acceptance criteria](ACCEPTANCE_CRITERIA.md) provide measurable checks without changing the existing rules.

Budget, schedule, hardware minimums, packaging/update policy, performance budgets, localization coverage, and accessibility targets are not established in the inspected documents. Record them as later decisions where relevant; do not invent delivery commitments.
