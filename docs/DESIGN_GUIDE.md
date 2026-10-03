# OpenAntares Design Guide

**Status: Working draft for review.**

This guide translates the [project charter](CHARTER.md) and the research in [docs/research](research/) into practical guidance for designing OpenAntares. It is for contributors deciding how a system should behave, what the player should see, and how to judge whether a feature improves the game.

The immediate goal is a playable colony loop: inspect the galaxy, allocate workforce, choose construction and research, advance a turn, understand the results, and resume from a save. The broader game should grow from that loop through exploration, expansion, distinct civilizations, rival empires, and strategic conflict.

## Authority and decision status

The charter remains the source of truth for identity, scope, and design intent. [CLAUDE.md](../CLAUDE.md) supplies the project's implementation constraints. This guide explains how to apply them; it does not expand the current milestone.

Three labels distinguish established direction from new choices:

- **Established:** a charter or project constraint, already applicable to new work.
- **Proposed:** a recommended behavior introduced by this guide, requiring review before its implementation spec is settled.
- **Open:** a consequential choice that needs a focused system design or playtest.

Examples illustrate intent and do not establish content names, numerical balance, or implemented features. System specs should record formulas, timing, limits, edge cases, and accepted deviations from this guide. Research notes preserve evidence; they do not independently authorize features.

## The experience we are designing

**Established:** players should have several defensible choices, understand their consequences, and develop an empire with its own character. Complexity should arise from interacting systems. Routine maintenance should remain manageable as the empire grows.

For each proposed mechanic, describe the decision it creates. State what the player gives up, what changes their preferred answer, and how they can recognize those conditions. A mechanic that merely adds another mandatory improvement or recurring click needs revision.

For example, a development project might make the starting colony more productive while delaying an immediate research goal. A productive planet might require more support than a smaller, easier settlement. These choices should be legible from the information the player actually has.

The first playable milestone can demonstrate allocation and investment choices without opponents. Strategic pressure from rivals belongs to later milestones. Do not compensate for their absence by building diplomacy or combat early.

## What the research contributes

| Research observation | Proposed application | Scope |
| --- | --- | --- |
| MOO2 patch users gained saved build lists and bulk queue actions. | Preserve manual control while designing production commands that can later support batch use. | Single-colony controls now; bulk UI later. |
| The patch exposed missing values and corrected misleading displays. | Obtain displayed totals and explanations from the same calculations used for resolution. | First playable. |
| Research loss and economy interactions required fixes or alternate rules. | Define progress conservation and test combinations of rule settings. | First playable foundations; broader combinations as settings are added. |
| Alternate rulesets use configurable content and rules. | Keep base content distinct from overrides and identify the rules used by a save. | Core data now; mod selection later. |
| OpenOrion2 validates cross-entity relationships before reconstructing fleets. | Validate a loaded world before building derived indices or exposing it to play. | First playable. |
| Its planet filters and ownership caches reveal visibility concerns. | Apply empire knowledge to lists, filters, counts, and previews as well as map rendering. | Inspection now; richer exploration later. |
| Inspection screens coexist with an unimplemented end-turn action. | Measure milestone progress through a complete gameplay loop. | Every milestone. |

Sources: [MOO2 1.50 patch review](research/moo2-150-patch-review.md) and [OpenOrion2 technical survey](research/OPENORION2_SURVEY.md). These are qualitative references, not evidence that their balance values or exact mechanics are suitable for OpenAntares. The OpenOrion2 survey was source inspection rather than a playtest.

## First playable scope

**Established:** the milestone includes new game setup, galaxy generation, a galaxy map, star and planet inspection, one controlled starting colony, workforce allocation, production, research, turn advancement, and save/load. AI, diplomacy, combat, and graphical polish are outside it.

Use a small set of original content sufficient to exercise the loop. The number of buildings, technologies, or planet types is not a progress measure. Every displayed choice should either work or explain why it is unavailable.

**Proposed acceptance scenario:** start a seeded game, find the home colony, change workforce allocation, inspect the resulting outputs, select a project and a research target, and advance enough turns to observe growth and both forms of progress. Complete at least one project and one research target, inspect their effects, save, reload, and continue. Repeat from the same state and orders to verify identical outcomes.

This scenario checks behavior. A separate playtest asks whether the allocation and project choices remain interesting and whether the player can explain the results.

## Galaxy and inspection

**Established:** entities have stable IDs. **Proposed:** model planets, settlements, and ownership separately, with validated references. A physical planet should not disappear merely because its colony changes owner or ceases to exist.

**Proposed:** map selection and inspector selection share one selected entity. Selecting a star shows its known planets; selecting a planet exposes its environment, available economic potential, and settlement information where known. In the first playable, economic potential is the planet type's per-worker output, the same values the economy uses. Returning to the map preserves useful context.

Describe suitability through contributing factors rather than a single unexplained score. Later comparisons should show why a world is useful to this civilization: environmental support, productive potential, location, and known risks. Avoid presenting one universal planet ranking as an optimal answer.

**Established:** inspection is specific to an empire. **Proposed:** distinguish an unknown value from a known zero, and label stale observations when the exploration model begins retaining them. A world without known threats is not necessarily a world confirmed safe.

The first milestone does not need moving scouts. **Open:** its initial visible area and how much of the generated galaxy is inspectable. Choose a clearly described prototype visibility policy; retain the empire-specific query boundary so later exploration can replace the policy without rewriting screens.

## Colony economy and workforce

**Established:** colonies support population, industrial development, and scientific research. Important values need understandable explanations.

**Proposed:** begin with workforce assignments for population support, production, and research. Reassigning workforce should immediately update a forecast of the coming turn, including support balance, net production, research contribution, and growth. Resolution uses the same rule calculations as the forecast. Forecasts should state assumptions whenever later systems make an outcome uncertain.

Show gross output, costs, and net output separately. Population support available, population support required, and the resulting surplus or deficit should be visible together. Stored progress and recurring output are different quantities and should have distinct labels and units.

Use integer or fixed-point arithmetic with explicit rounding rules. Keep fractional progress in state where the design requires it; rounding a display must not silently erase earned progress.

**Proposed in the [economy specification](FIRST_PLAYABLE_ECONOMY.md):** local support without storage, shortages that pause growth and reduce output, and explicit growth and capacity rules. These choices remain subject to review and playtesting. Transport and population loss are outside this prototype.

## Production and development

**Proposed:** one colony has one active construction project in the first playable milestone. Show its cost, stored progress, expected contribution next turn, and estimated completion time. If the reserve already covers the cost, show completion at the next resolution even when current output is zero. Otherwise, an estimate with no positive production should say it is stalled rather than inventing a turn count.

Allow a choice to be changed before resolution. Define switching and cancellation costs explicitly; do not silently delete progress. Prefer preserving earned production in a colony reserve, subject to a documented capacity or conversion rule if later balance requires one.

On completion, account for the full input: applied progress, completed project, and any remainder. **Proposed in the [economy specification](FIRST_PLAYABLE_ECONOMY.md):** bank the remainder, clear the completed project, and complete at most one building per colony per turn. A queue and multiple completions per turn are later decisions, not milestone requirements.

Buildings should create a useful capability or tradeoff. For each, specify its beneficiaries, costs, prerequisites, and how its effects appear in the economy breakdown. Avoid an infrastructure chain that every colony must follow in the same order.

Later queue templates and batch actions should use the ordinary command path. A batch operation should report which colonies accepted it and why others did not. Automation should have a visible policy and allow manual override.

## Research

**Established:** research advances through data-defined technologies and prerequisites. **Proposed:** the empire selects one active target and receives the contributions of its colonies. The research view shows available choices, prerequisites, costs, progress, and the capabilities each choice enables.

A player should be able to distinguish available now, available after prerequisites, already known, and unavailable under the current rules. Browsing future options must not be mistaken for selecting them.

**Proposed:** unused research, including research earned with no selected target, is preserved in an explicit empire reserve. Completion consumes the stated cost and accounts for the remainder. The [economy specification](FIRST_PLAYABLE_ECONOMY.md) proposes unlimited reserves, switching without loss, and at most one technology completion per empire per turn. If a capacity or loss rule is introduced for balance later, explain it before resolution.

Completion should expose what changed and where it matters. Prefer technologies that enable different plans. Exact research topology, exclusive choices, pacing, and numeric costs need a separate design; the legacy tree is not the default.

## Orders and turn resolution

**Established:** every state-changing action is a command issued by an empire and checked by the simulation. Presentation, AI, and future remote clients use the same validation path. Turn processing is deterministic and follows explicit phases.

**Proposed:** ending the turn is also a command. Each empire marks itself ready, and the turn resolves when every human-controlled empire is ready. Every new turn starts unready. Changing a planning choice clears that empire's readiness; repeating an unchanged choice preserves it. The triggering readiness command and resolution succeed or fail together. With one human empire this behaves like an ordinary end-turn button, without placing a single-player assumption outside the command path.

**Proposed first playable timing:** workforce and target-selection commands update the planning state after validation. Replacing such a choice before ending the turn recalculates forecasts but earns or spends no turn output. These edits do not promise a general undo system for all future commands.

The following phase order is a proposal for the economy spec:

1. Validate the planning state and capture the inputs for the turn.
2. Calculate support, production, research contributions, and growth inputs from that snapshot.
3. Resolve support and population changes using the chosen shortage and growth rules.
4. Apply the previously calculated production and research, resolve completions, and account for remainders.
5. Validate the resulting state, advance the turn number, and publish a structured report.

Under this proposal, new population, buildings, and technologies affect economic output on the following turn. Calculated output is not recomputed midway through resolution. This avoids accidental differences caused by colony processing order.

Later movement, diplomacy, combat, and events require an explicit extension of this order. Their timing must be specified before implementation rather than attached to whichever callback happens to run first.

**Proposed:** warn about outcomes that cost the player something, such as support deficits, with direct links to the affected choices. Show idle production or unselected research as a status rather than a warning when the rules preserve that output; it becomes a warning only if a rule makes waiting costly. Allow intentional waiting under valid rules. Block invalid state or an invalid command, not every strategically questionable choice.

## Information design and feedback

**Established:** the interface uses clear summaries, drill-down detail, comparisons, and alerts. It adapts to desktop resolutions using containers and anchors; styling comes from one theme.

**Proposed:** an important number has three levels of explanation: a readable total, a short breakdown, and detailed rule information where needed. Breakdown entries identify their source, units, contribution, and rounding or caps, and their integer contributions sum exactly to the total. Fractional rounding losses use explanatory integer division metadata rather than unrepresentable fractional contributions. The [economy specification](FIRST_PLAYABLE_ECONOMY.md) defines a common breakdown line shape for later systems to reuse. Presentation formats the result; it does not reconstruct a second version of the formula.

For example, a production forecast might show workforce contribution, building contribution, environmental modifiers, operating costs, and the final amount added to construction. Its components must reconcile with the total actually applied during resolution.

Turn reports should emphasize decisions that now need attention: completed projects, research unlocks, shortages, and interrupted plans. Routine progress remains available without requiring a modal dismissal for every colony. Selecting a report item should navigate to its relevant entity.

Filters and sorting use permitted knowledge. Hidden facts must not change visible result counts, order, button states, or tooltips. Changing the viewed empire affects the information view, not authoritative simulation facts.

## State and persistence

**Established:** simulation state is plain serializable data, independent of Godot. Simulation calculations use whole numbers or fixed point, a stable seeded PRNG with saved state, and defined iteration order. Localized strings and presentation caches are outside authoritative state.

**Proposed:** saves record schema version, turn, entity IDs and relationships, population and progress, current orders, empire knowledge, PRNG state, and enough ruleset information to reproduce calculations. A display name for a mod is insufficient to establish identical rules. **Proposed in the [economy specification](FIRST_PLAYABLE_ECONOMY.md):** embed the effective content definitions and rule parameters in the save, and version the rules implementation separately.

Loading should parse, validate content compatibility and world invariants, then rebuild derived indices. On failure, keep the current session intact and explain the affected file, entity, or rule. Do not repair corrupted state silently.

Milestone invariants include unique entity IDs, valid ownership and parent references, legal workforce totals, valid targets, legal population and capacity values, and accounted-for resource progress. Later systems add their own invariants; do not copy constraints such as symmetric diplomacy merely because a reference project used them.

## Content and alternate rules

**Established:** technologies, buildings, planet types, species traits, and other ordinary content live in JSON, referenced by string IDs. Content validation reports file, entry, and field. Generic logic belongs in code when data alone cannot express a rule.

**Proposed:** separate core definitions from selected overrides and report missing references or incompatible combinations before starting play. New content should use existing mechanics where possible. A change to rule timing or resource semantics deserves explicit ruleset treatment rather than being disguised as a numeric adjustment.

Test interactions as options are introduced. A production mode that works by itself can still duplicate income when paired with another mode. Include extreme valid values and boundary states, especially where rounding, capacity, and progress conversion interact.

## Direction for later systems

These are charter-aligned design criteria, not authorization to implement ahead of the milestone.

| System | Direction | Evidence needed before committing to a mechanic |
| --- | --- | --- |
| Exploration and expansion | Discoveries should change settlement priorities; travel and support should make location matter. | Different map situations produce different defensible expansion plans. |
| Civilizations | Abilities and constraints should alter decisions and combinations of viable strategies. | Several builds play differently without one dominating ordinary situations. |
| AI empires | Use permitted information and ordinary commands; pursue understandable strategic goals. | Coherent pressure without illegal knowledge or command shortcuts. |
| Diplomacy | Agreements and attitudes should reflect visible interests and consequences. | Negotiation can be useful in situations where coercion also has a cost. |
| Fleet design and strategic warfare | Design choices should serve roles, costs, and counters. | Several compositions remain useful; strategic results have explainable causes. |
| Tactical combat | Preserve separation from campaign state and support reproducible scenarios if developed. | Its scope does not prevent completion of the strategic game. |
| Events and victory | Events should create responses; victory conditions should reward distinct long-term plans. | Players can recognize progress and respond before outcomes become inevitable. |

## Review and playtest criteria

Before accepting a system design, answer:

- What meaningful choice does it create, and when does the preferred answer change?
- What information lets the player judge it, and what remains legitimately unknown?
- How does it behave at zero output, at capacity, after cancellation, and on completion?
- When do its effects apply, and do forecasts match that timing?
- How much repeated work does it require across a large empire?
- Can it be reproduced from the same state, commands, rules, and seed?
- Which data defines it, which state persists it, and which invariants validate it?
- Does it belong in the current milestone?

Verify the first playable with calculation and boundary tests, command rejection without partial mutation, deterministic repeat runs, save/load continuation, and agreement between forecasts, breakdowns, and applied results. Include visibility checks when inspection restrictions are introduced.

Playtests should ask players to explain an allocation choice and a turn result without reading source code. Observe unnecessary screen switching, repeated clicks, unclear shortages, and whether project choices converge on one routine sequence. Record those findings before adding content to mask a weak loop.

## Next design decisions

The [first playable economy and turn specification](FIRST_PLAYABLE_ECONOMY.md) proposes support and growth, planet output rates, production switching and overflow, research reserves, breakdown lines, readiness, and completion timing together, with worked turns and expected state. Review these interacting choices before implementation.

Galaxy generation and the initial visibility policy need a focused specification alongside that work. Save compatibility follows once state and ruleset representation are concrete. Broader systems remain directional until the colony loop demonstrates both understandable results and worthwhile choices.
