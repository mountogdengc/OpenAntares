# OpenAntares project concept

Current continuation (2026-10-24): the owner subsequently approved the rival plan with continue. All ten Native plan tasks/review are complete on isolated codex/rival-expansion (rules4/writer5), source ce6c29c. Final gates pass; one Important correction is verified and two Minor acceptance deviations remain Deferred. [PROJECT_STATUS](../PROJECT_STATUS.md) governs the next action. Earlier planning-only statements below are historical. The owner subsequently chose option1: local main integration is complete at70e91d9 and combined verification passes; see current project status. Push/release/cleanup remain separate owner actions.


Reconciled 2026-10-22 from [CHARTER.md](CHARTER.md). This is an adoption summary of the existing project, not a new product proposal.

## Identity and outcomes

- **Name:** OpenAntares (the repository's established spelling).
- **Type:** Existing application; original open-source turn-based space 4X game.
- **Opportunity:** Provide meaningful strategic decisions, understandable consequences, distinct civilizations, and extensible content without mandatory busywork or a dependency on nostalgia.
- **Users:** Desktop strategy players; later content creators and contributors. No narrower audience or commercial plan is established.
- **Platforms:** Windows, Linux, macOS. Web is excluded by existing project guidance.
- **Loop:** Explore, expand, develop, research, negotiate, build, compete, adapt.
- **Creative tone:** A love letter to Master of Orion II and classic space opera. Colorful humanoid animals/monsters and fanciful biology are welcome; designs respect real peoples, cultures and religions.

The supplied OpenJRPG workflow is a reusable process reference. Its RPG systems, authoring tools, package policies, and ADR-014/015 are not OpenAntares decisions. No engine rename, rewrite, or new framework is needed for this adoption.

## Scope

| Classification | Established scope |
| --- | --- |
| Essential now | The charter's eleven First Playable actions: new game, galaxy generation/map, star/planet inspection, one colony, workforce, production, research, end turn, visible advancement, save/reload |
| Active milestone | M5-P colony-development plan review: full design approved in ADR-012; twelve-task plan written/self-reviewed, owner plan review pending. M4-I locally verified/integrated; M5 implementation Not Started |
| Later version 1.0 | Multiple civilizations, AI, fleets/ships/design, strategic warfare, diplomacy, events, setup options, victory/defeat, broader technology and development |
| Optional | Tactical combat only if it does not impede completing the strategic game |
| Excluded initially | Real-time strategy, MMO/persistent online world, proprietary backend dependence, mandatory online single-player, copied commercial assets or interfaces |

[SPECIES_ROSTER.md](SPECIES_ROSTER.md) records the original Peldra, Harl, Sulan and Wenth creative starting point; current owner edits remain preserved. The owner permitted creative changes under ADR-007 and subsequently approved the full [species mechanics](superpowers/specs/2026-10-23-species-mechanics-design.md) in [ADR-008](DECISIONS/ADR-008-approved-species-mechanics.md). That rules3 amendment governs implemented mechanics without rewriting historical roster text. The owner approved the [implementation plan](superpowers/plans/2026-10-23-species-mechanics.md); [execution evidence](../reports/SPECIES_VALIDATION_2026-10-23.md) and the final review report describe actual local acceptance. New games visibly default to Harl; old campaigns retain their original rules/content.

## Constraints and success

Use the existing C#/Godot .NET architecture, headless deterministic simulation, commands, stable IDs, data-defined content, and theme-based UI. See [ARCHITECTURE.md](ARCHITECTURE.md) and [CLAUDE.md](../CLAUDE.md). Code/content are MIT; original assets CC0; docs CC BY 4.0.

First Playable succeeds when all eleven actions work, saved games continue consistently, consequences are understandable, and workforce/project choices are meaningful in play. Technical tests alone do not establish enjoyment or comprehension. [Acceptance criteria](ACCEPTANCE_CRITERIA.md) provide measurable checks without changing the existing rules.

Budget, schedule, hardware minimums, packaging/update policy, performance budgets, localization coverage, and accessibility targets are not established in the inspected documents. Record them as later decisions where relevant; do not invent delivery commitments.
