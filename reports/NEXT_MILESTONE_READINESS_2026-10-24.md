# Next milestone readiness — 2026-10-24

**Subsequent owner direction:** The owner answered “yes” to competing with another empire. [ADR-009](../docs/DECISIONS/ADR-009-rival-expansion-design-entry.md) now records rival expansion selected for drafting; [the full proposed design](../docs/superpowers/specs/2026-10-24-rival-expansion-design.md) has completed its builder check and awaits full owner review. The earlier selection-open statements below are retained assessment history, not a reason to ask the direction again. No detailed gameplay approval is claimed.


**Status: Recommendation for owner direction; no new gameplay specification or implementation approved.**

Continuation baseline: local main `c41baaddb6ecefb019a7685878beaedaae1ec138`, with the two preserved owner prose edits. Species implementation, independent review and local integration are complete. The owner asked to continue; this assessment advances selection of the next bounded milestone under REQ-017/AC-017, without treating that broad envelope as an approved feature list.

## Recommendation

**Recommend designing rival expansion next:** encounter another civilization that develops colonies and explores/settles using the same rules. This is an agent recommendation, not an owner decision. It would give the existing scouting, species and expansion choices an external reason to matter, consistent with the charter’s wish to respond to other empires.

The owner’s requested classic Master of Orion II direction remains authoritative. Preserve the four original species and accepted mechanics; do not copy commercial assets or introduce a new biological/representation checklist. The current open question is the desired player experience after founding a second colony. That answer may change the recommended order.

## What exists and what is missing

| Inspected fact | Consequence for the next design |
| --- | --- |
| [GalaxyGenerator](../src/OpenAntares.Simulation/Galaxy/GalaxyGenerator.cs) creates one human empire and one starting colony/scout | Multiple starting empires, home selection, spacing and starting fairness need an explicit contract; they are not already implemented setup options. |
| [GameState](../src/OpenAntares.Simulation/State/GameState.cs) includes Human/Ai controller kinds and empire-owned resources | An Ai enum is groundwork, not an active decision-maker. No AI planning subsystem was found in the source inventory. |
| [SetReady](../src/OpenAntares.Simulation/Commands/CommandProcessor.cs) resolves immediately when all human empires are ready | AI planning order and failure atomicity must be designed before inserting automatic commands. A controller loop must not partially publish orders or accidentally advance twice. |
| [EmpireView](../src/OpenAntares.Simulation/Knowledge/EmpireView.cs) exposes owned forecasts, permitted observations and visible foreign vessels | Use permitted information and the ordinary command path. Seeing a foreign owner does not automatically authorize access to its species, research, reserves, home or hidden orders. |
| Existing movement, founding conflicts, species support and Wenth/Sulan requirements are implemented | Reuse these rules; an opponent cannot receive free ships, population, illegal habitats or privileged founding. Handling species constraints is part of viable AI, not a cosmetic personality label. |
| [Technologies](../content/core/technologies.json) has three independent prerequisite-free technologies; [buildings](../content/core/buildings.json) has four prototype developments | Colony/research depth is a genuine alternative next milestone, but meaningful alternatives need design rather than merely increasing the content count. |
| Existing vessels have movement/founding roles; no ship-design or combat subsystem exists | Fleet design/warfare is a larger coupled step requiring roles, targets, costs, resolution and information rules. Do not fold it into a small AI milestone. |

Sources of authority: [charter](../docs/CHARTER.md), [engineering constraints](../CLAUDE.md), [requirements](../docs/SPECIFICATIONS.md), [acceptance](../docs/ACCEPTANCE_CRITERIA.md), [workflow](../docs/PROJECT_WORKFLOW.md), and the accepted species/expansion specifications. Proposed Design Guide passages and research notes remain advisory. Their historical first-playable/all-visible/exploration-next wording does not supersede the completed expansion/species contracts or ADR-005’s human-session deferral.

## Alternatives and tradeoffs

| Candidate | New player decision | Main design cost | Assessment |
| --- | --- | --- | --- |
| Rival expansion | Which worlds to scout/develop/settle before another empire does | Fair multi-empire starts; a deterministic legal planner; observed rival activity; scheduling and compatibility | Recommended next design because it connects already implemented systems and begins the charter’s strategic interaction. |
| Colony specialization and research choices | Which colony should produce, grow or research, and which development path to pursue | Concrete opportunity costs, unlock graph, useful divergent choices and balance fixtures | Strong alternative if the owner wants internal empire development before competition. |
| Fleet design and strategic conflict | Which fleet roles/compositions and deployments answer a known threat | Ship definitions, construction, fleet state, targeting, combat, loss accounting and war policy | Defer the combined implementation; establish a separate bounded design when selected. |

This comparison sets no opponent count, difficulty bonuses, technology roster, diplomatic model, victory condition or rules/schema version. Those would be new product choices.

## Candidate rival-expansion boundary

If selected, draft a specification for a small new-game scenario with a human civilization and a limited rival presence. The exact opponent count and whether to allow a solo option remain owner-facing design choices. Define observable exploration, colony support/development, research and settlement activity using existing species permissions and commands. Define what an encounter reveals and how reports distinguish observed facts from unknown or stale information.

Leave diplomacy, trade, alliances, combat, invasions, fleet/ship design, custom civilizations, multiplayer, new portraits and broad AI personality/difficulty systems outside that first slice. Without warfare or victory this would be an expansion-race milestone, not a completed competitive campaign.

Before planning implementation, settle these contracts:

- Deterministic home assignment and viable starts for all species, with explicit impossible-map failures and no silent edits to existing campaigns.
- The exact human/AI planning and resolution boundary, stable order/tie-breakers, bounded planner work and rollback on failure.
- Information available to the planner and player at current/stale/unknown contacts; hidden-truth changes must not alter decisions or displays illegally.
- Legal budgets and support recovery, including Harl small settlements, Peldra support cost, Sulan habitat choice and Wenth maturity.
- Save/rules compatibility. Existing rules3 saves can contain AI-tagged test empires that are currently passive; loading them must not silently enable a new behavior policy. Versioning/persisted policy needs an explicit decision.
- Named acceptance cases for visible rival development, no cheats/leaks, contested founding, repeatable commands, invalid setup, failed turns and save/reload continuation, while retaining all existing scenarios.

## Readiness and handoff

The architecture is suitable for a design investigation; none of the three alternatives is yet implementation-ready. REQ-017 and AC-017 remain Deferred until the bounded milestone is selected and specified. No new child requirements, accepted ADR, implementation plan or product code is created by this assessment.

Last executed baseline is the [species main integration](SPECIES_INTEGRATION_2026-10-24.md):445 simulation tests,1181 species assertions,275 expansion assertions,466 legacy assertions plus27 geometry observations. These are retained integration results, not tests rerun for this documentation-only assessment. Requirement totals stay12 Verified,3 Implemented/Unverified,2 Deferred. Human comprehension remains Deferred, and physical input/readability/platform/release acceptance remains open.

Next action: use the owner’s desired next-turn experience to select the milestone, then draft its full design and run the workflow’s builder check before presenting it for review. Only an approved full design proceeds to implementation planning. The owner may delegate that directional choice explicitly; an agent-chosen default must remain labelled as such.

Working code, core JSON, owner prose files, saves, recovery snapshots/stash and all worktrees remain untouched. No new build/test, merge, push, release or cleanup is claimed.
