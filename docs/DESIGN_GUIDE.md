# OpenAntares Design Guide

Reconciled 2026-11-16. **Current design overview.** OpenAntares is an original turn-based space 4X game about distinct civilizations, understandable strategic tradeoffs and an empire that remains manageable as it grows. Its identity, mechanics and appearance stand on their own. The [charter](CHARTER.md) governs product intent; [engineering rules](../CLAUDE.md) govern implementation.

## How to read the design record

This guide is the starting point, not a second copy of every formula. Detailed subsystem specifications define exact behavior for their applicable rules versions. [Specifications](SPECIFICATIONS.md) index contracts; [acceptance criteria](ACCEPTANCE_CRITERIA.md) separate required evidence from actual results; [project status](../PROJECT_STATUS.md) identifies the inspected checkout and next action. [Decision records](DECISIONS/README.md) preserve why choices were made.

Use three distinctions consistently:

- **Established:** approved direction or implemented contract, with its applicable version and recorded evidence scope.
- **Proposed:** a concrete recommendation awaiting gameplay review. Writing a specification does not approve it.
- **Open or deferred:** a decision or feature still requiring design, observation or a later scope selection.

Dated reports, plans and old checkpoint paragraphs retain their original source and acceptance scope. A prior test count is not fresh verification of today's checkout. Historical research and third-party reference provenance remain research; they prescribe neither product identity nor a feature checklist. [ADR-030](DECISIONS/ADR-030-original-identity-and-design-reconciliation.md) records the current identity clarification.

## Player experience and design pillars

The strategic loop is **Explore → Expand → Develop → Research → Negotiate → Build → Compete → Adapt**. Each system should create a decision with more than one defensible answer. State the opportunity cost, the circumstances that change the answer and the information that lets the player recognize them.

The pillars are meaningful decisions, depth without busywork, distinct empires, strategic transparency, emergent stories, strong information design and moddability. Automation should handle routine execution within visible player policies; it must remain possible to inspect why it acted, override it and understand a blocked order.

OpenAntares uses an original space-opera setting. Colorful aliens, humanoid animals, monsters and fanciful biology are welcome. Memorable personality and enjoyable play may take priority over evolutionary plausibility. Treat real peoples, cultures, religions and ethnic groups respectfully. Species design does not require an external game's archetypes or rules.

## Current implemented strategic game

The First Playable established the colony loop; it is a historical foundation rather than today's feature ceiling. Rules 12 / writer 13 are current in inspected source. Earlier saves retain their embedded rules and content. These systems exist; this table does not assert complete human, balance, platform or release acceptance.

| System | Current design contract | Detailed authority |
| --- | --- | --- |
| Galaxy and exploration | Seeded galaxy, empire knowledge, survey/travel and settlement decisions; visibility governs map, lists and previews | [Galaxy](superpowers/specs/2026-10-05-spiral-galaxy-design.md), [expansion](superpowers/specs/2026-10-22-exploration-and-colonization-design.md) |
| Species | Peldra, Harl, Sulan and Wenth have different support, growth, labor and founding constraints | [Species](superpowers/specs/2026-10-23-species-mechanics-design.md) |
| Rival expansion | A rival uses paid ordinary actions and permitted knowledge; Solo remains available | [Rivals](superpowers/specs/2026-10-24-rival-expansion-design.md) |
| Colony development | Paid development, specialists, finite queues and optional governors with manual authority | [Development](superpowers/specs/2026-10-24-colony-development-design.md) |
| Crises | Plague progression, responses and population-bearing transport restrictions | [Crises](superpowers/specs/2026-10-24-colony-crises-plague-design.md) |
| Scouting | Survival, encounter avoidance and ameba behavior preserve useful noncombat exploration | [Scouting](superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md) |
| Fleet command | Named fleets, construction requests, officers, doctrine and cultural recovery rules | [Fleets](superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md) |
| Strategic combat | Sentry exchanges, persistent hull/cohesion, standing orders, withdrawal and paid port repair | [Combat](superpowers/specs/2026-10-28-fleet-combat-damage-and-withdrawal-design.md) |
| Ship design | Immutable exact designs, research unlocks, paid production and same-frame refits | [Ship design](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) |
| Diplomacy | Contact, visible attitudes, settlement accords and paid production trade | [Diplomacy](superpowers/specs/2026-10-30-diplomacy-design.md) |
| Empire conflict | War/peace, protected civilians, armed encounters, withdrawal, nonstacking production blockades and fair military AI | [Conflict](superpowers/specs/2026-10-31-empire-conflict-design.md) |

## Economy, population and timing

Local population support, industrial production and scientific research are distinct quantities. Current support is calculated separately per colony; it is not a physical intercolony food network. Existing shortage rules reduce production/research and pause ordinary growth. Expose support produced, required, surplus and deficit with the same calculations used by resolution.

Stored production and research are progress, not a shared treasury. Preserve their exact switching/completion/remainder rules. Queue, request, trade and refit reservations must not spend the same bank twice. Forecasts must explain workforce, species, environment, building, health, blockade and agreement effects in their applicable order.

Population units and growth progress are different. Wenth population denotes lobes of a single colony body; it must not become generic passenger cargo. Settlement/habitat restrictions remain species-aware. The [original economy contract](FIRST_PLAYABLE_ECONOMY.md) defines the foundation; later specifications explicitly extend it rather than silently redefining historical saves.

Resolution uses snapshots, ordered deterministic phases and one atomic candidate-state publication. The current [turn resolver](../src/OpenAntares.Simulation/Turns/TurnResolver.cs) incorporates diplomacy, economy, health, combat, movement/founding and aftermath. The old five-phase economy-only description is not the entire current turn. Define the exact integration point of every new system before implementation; new arrivals, workers or completions cannot retroactively earn output.

## Information, commands and persistence

Every state-changing action, including AI action, uses the same empire-tagged command validation. Rejection preserves state. AI may neither inspect hidden information nor obtain free resources or special command authority.

Obtain display values and forecasts from simulation calculations. Important values need a readable total, a short source breakdown and detailed timing/rounding where needed. Unknown is different from zero; stale knowledge is different from current observation. Hidden facts must not change visible counts, sorting, enabled controls or preview tokens.

Use plain serializable state, stable IDs, integer/fixed-point rule calculations, the saved project PRNG and stable iteration. Save/load validates content and relationships before replacing a session; a failure preserves the current session. Explicit rules/schema compatibility retains old campaign behavior. Presentation settings and caches stay outside authoritative saves.

## Current interface and visual direction

M13 campaign polish and M14 presentation are integrated. The current interface uses a map-centered console, selection cards, shared fleet rows/stacks, explicit multi-selection, click-to-course, Decisions and a compact Turn Report. System inspection opens an orbital window over the map. Colony controls and population/growth labels should describe player decisions, not diagnostic implementation details.

UI controls are built in C# and styled in `game/theme/openantares_theme.tres`. Preserve owner theme edits. Original UI assets, fonts and their provenance remain in [art/ui/README.md](../art/ui/README.md) and [LICENSING.md](../LICENSING.md). Shader-based screen effects and procedural planet materials are documented presentation exceptions; this does not authorize moving ordinary UI styling into code.

Procedural planets share saved appearance inputs across sidebar, colony and orbital views. They use only published permitted information; current owned population may drive city lights, while unknown or stale foreign population must not. Reduced effects and Animate planets are presentation preferences outside campaign saves.

The November 15 feature branch changes planets to a stylized modern look, accepted visually by the owner in its [report](../reports/planet-globes-v2-2026-11-15/PLANET_GLOBES_V2_2026-11-15.md). This supersedes the original proposed retro globe style. The subsequent shader-size optimization addresses observed first-use pauses, but Windows ANGLE compile timing, current parity/city checks and rendering budgets remain unverified for that revision. The original 699-check result and prior timings apply to the November 12 implementation, not automatically to v2. Acceptance of that appearance does not close overall M14 comprehension/input acceptance.

## Next gameplay design: supply and migration

The owner-selected direction includes civilian/colony and fleet supply, natural migration and deliberate bulk transfers. The [supply and migration proposal](superpowers/specs/2026-11-16-supply-and-migration-design.md) supplies concrete assistant defaults for resources, physical transport, policies, reservations, population identity, health and timing. It proposes civilian logistics/migration first, fleet endurance next and a separate interception/blockade-running design later. The owner has approved the summarized civilian behavior in [ADR-031](DECISIONS/ADR-031-approved-civilian-logistics-direction.md). The [civilian plan](superpowers/plans/2026-11-16-civilian-logistics-and-migration.md) covers provisions/transports/migration first and awaits concrete-plan review; numerical/integration choices are explicit there. Development goods, fleet endurance and interception remain later deliveries; no gameplay implementation is authorized yet.

Tactical direction remains recorded in the [November 12 note](design-notes/2026-11-12-tactical-combat-and-supply-management.md): larger location-dependent battlefields, scouts that can escape, speed/size/maneuvering defenses, equipment roles, blockade runners and competent automation. Ordinary current shots do not use an accuracy/evasion roll. A tactical battlefield is not implemented by recording that intent. Tactical scope must not prevent completion of the strategic game.

Victory/defeat completion, broader events/content, balance, custom civilizations and the [deferred peaceful-mode request](FEATURE_REQUESTS.md#fr-001-peaceful-mode) still need their applicable design and acceptance. Solo setup alone is not acceptance of a complete peaceful progression mode.

## Review and acceptance

For each design explain its meaningful choice, information requirements, timing, edge cases, conservation, large-empire workload and persistence. Include zero output/capacity, cancellation, blocked routes, partial failure and conflicting commitments where applicable. Compare alternative approaches against the pillars, rather than treating a reference game's behavior as a target.

Automated verification establishes only its recorded scope and source. Human observation establishes whether players can understand and comfortably use the system. Preserve open physical-input, comprehension, accessibility, balance, platform and release limits. Use the [M14 journeys](playtests/m14-player-acceptance-2026-11-08.md) and feature-specific handoffs where relevant, without asking players to inspect source inventories or diagnostic logs.

Run the smallest checks necessary for changed behavior through the [native shared queue](TEST_RUNNER.md); no Docker and no full suite by default. Documentation reconciliation runs document/link/preservation checks, not gameplay tests. The immediate next action is owner review of the concrete civilian logistics/migration implementation plan. Native inline execution is retained after approval; later resource and tactical deliveries remain separate.
