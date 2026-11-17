# OpenAntares project concept

Reconciled 2026-11-16 from the [charter](CHARTER.md), implemented subsystem specifications and inspected local Git history. This is the current product summary; exact formulas and evidence remain in their versioned sources.

## Identity and outcomes

- **Name:** OpenAntares.
- **Type:** Original open-source, turn-based space 4X strategy game.
- **Experience:** Meaningful choices, understandable consequences, distinct civilizations and manageable empires, with an original setting and visual identity.
- **Users:** Desktop strategy players; later content creators and contributors.
- **Platforms:** Windows, Linux and macOS are targets; Windows is the recorded native development environment. Web is excluded. Targeting a platform is not release verification.
- **Loop:** Explore, expand, develop, research, negotiate, build, compete, adapt.
- **Creative tone:** Colorful original space opera; humanoid animals, monsters and fanciful biology are welcome. Respect real peoples, cultures and religions.

## Current scope

| Classification | Current position |
| --- | --- |
| Implemented foundation | New game, seeded galaxy, inspection, workforce/economy, research, deterministic turns and save/load |
| Implemented strategic extensions | Exploration/founding, four species, rival expansion, colony development, crises, scout survival, fleets/officers, strategic combat, ship design/refits, diplomacy and empire conflict |
| Implemented presentation | M13/M14 UI and campaign polish, shared fleet/selection controls, orbital system inspection and procedural planets |
| Current branch follow-up | November 15 modern planet shaders on feature/planet-globes-modern, beyond local main d994d34; appearance accepted in its report, Windows performance/affected checks pending |
| Current gameplay design | Proposed civilian/fleet supply and migration; concrete assistant defaults await owner review, implementation Not Started |
| Further design/acceptance | Victory/defeat completion, broader events/content, balance, custom civilizations and deferred peaceful mode |
| Optional | Tactical combat only if it does not impede completing the strategic game |
| Excluded initially | Real-time strategy, persistent MMO, proprietary-backend dependence, mandatory online single-player and copied commercial assets/interfaces |

[DESIGN_GUIDE.md](DESIGN_GUIDE.md) is the readable current overview. The [supply/migration proposal](superpowers/specs/2026-11-16-supply-and-migration-design.md) develops the [November 12 directions](design-notes/2026-11-12-tactical-combat-and-supply-management.md). Recording it does not authorize gameplay changes or assign a new numbered milestone.

## Constraints and acceptance

Retain C#/Godot .NET presentation, a headless deterministic simulation, ordinary empire commands, stable IDs, JSON content and shared-theme UI. Current source uses rules 12 and writer 13; previous saves retain their embedded behavior. No rules or schema version changes are part of this documentation work.

The four species are Peldra, Harl, Sulan and Wenth. Their [mechanical specification](superpowers/specs/2026-10-23-species-mechanics-design.md) governs implemented behavior; the [roster](SPECIES_ROSTER.md) retains creative background. Wenth colonies are individual bodies, a material constraint for future migration.

Acceptance remains source- and scope-specific. M13 has recorded automated acceptance, and M13/M14 integration is recorded; overall human comprehension/input acceptance remains open. Later visual acceptance and tests cover only their reported feature/source. See [acceptance criteria](ACCEPTANCE_CRITERIA.md) and [current status](../PROJECT_STATUS.md). No fresh game tests ran for this reconciliation.

Code/content are MIT, original assets CC0 and docs CC BY 4.0; third-party licenses/provenance remain intact. Budget, schedule, minimum hardware, delivery/update policy, localization and accessibility targets have not been established here. The adopted project workflow does not import another project's product requirements.
