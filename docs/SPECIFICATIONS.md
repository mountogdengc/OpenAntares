# OpenAntares requirement register

Reconciled 2026-10-22. IDs introduced by this audit are stable traceability labels, not new feature approvals. Detailed existing documents remain authoritative. `Verified` below means the stated automated contract passed on Windows x64; it does not imply graphical or cross-platform acceptance.

Status vocabulary: Not Started / In Progress / Implemented, Unverified / Verified / Blocked / Deferred. Counts in [PROJECT_STATUS.md](../PROJECT_STATUS.md) derive from the 17 rows below.

## Tracked requirements

| ID | Requirement | Priority / milestone | Status | Acceptance |
| --- | --- | --- | --- | --- |
| REQ-001 | Seeded new-game generation | Essential / First Playable | Verified | AC-001 |
| REQ-002 | Validated data-defined content | Essential / First Playable | Verified | AC-002 |
| REQ-003 | Plain state, stable IDs, simulation boundary | Essential / First Playable | Verified | AC-003 |
| REQ-004 | Shared atomic command validation | Essential / First Playable | Verified | AC-004 |
| REQ-005 | Workforce, support, growth, and forecasts | Essential / First Playable | Verified | AC-005 |
| REQ-006 | Conserved colony production | Essential / First Playable | Verified | AC-006 |
| REQ-007 | Conserved empire research | Essential / First Playable | Verified | AC-007 |
| REQ-008 | Readiness and deterministic turn phases | Essential / First Playable | Verified | AC-008 |
| REQ-009 | Save integrity and continuation | Essential / First Playable | Verified | AC-009 |
| REQ-010 | Galaxy map and star/planet inspection | Essential / First Playable | Implemented, Unverified | AC-010 |
| REQ-011 | Complete interactive colony loop and explanations | Essential / First Playable | Implemented, Unverified | AC-011 |
| REQ-012 | Repeatable simulation and visual-stream isolation | Essential / First Playable | Verified | AC-012 |
| REQ-013 | Portability and cross-machine determinism | Essential / delivery | Implemented, Unverified | AC-013 |
| REQ-014 | Understandable, meaningful First Playable choices | Essential / First Playable acceptance | Deferred | AC-014 |
| REQ-015 | Exploration, travel, and a second colony | Implemented expansion / local automated acceptance | Verified | AC-015 |
| REQ-016 | Distinct playable species | Species slice / local automated acceptance | Verified | AC-016 |
| REQ-017 | Remaining complete strategic game | M4-I Verified locally; M5-P review; rest later | In Progress | AC-017 |

## Functional contracts

### REQ-001: Seeded new-game generation

Inputs: validated content, supported size ID, unsigned 64-bit seed. Output: turn 0, one human empire, one owned colony on the starting planet type. Core sizes produce exactly 12/16/20 systems in 1000x700/1200x840/1400x980 bounds; current minimum separation is 120, with 1-4 planets per star. Rules2/3 new games also receive one content-defined scout and a home-only survey. Current rules3 requires explicit selected species(Harl visibly default in UI), with species starting population/workforce and an ordered weighted environment pass after base generation; home is sampled then overridden to Shelf Sea for every species. The integer spiral gives every system a core/arm/rim region with documented quotas. Region weights affect planet types; the home type remains content-defined. Unknown sizes and impossible spacing return generation errors rather than partial galaxies. Dependencies: REQ-002/003/012. Sources: `content/core/galaxy.json`, `rules.json`, [spiral design](superpowers/specs/2026-10-05-spiral-galaxy-design.md). AC-001.

### REQ-002: Validated data-defined content

Inputs: JSON rules, starting colony, galaxy/sizes, planet types, buildings, technologies. Definitions may be split across files; file processing is ordinal. Comments/trailing commas are currently accepted. Validate duplicate IDs, references, prerequisite cycles, supported modifiers, positive costs, nonnegative rates, region eligibility, and arithmetic bounds. Errors identify file, entry ID, and field and do not publish invalid content. Ordinary value/content edits must not require recompilation. Dependencies: none. Source: [economy spec](FIRST_PLAYABLE_ECONOMY.md), `ContentLoader`, `EntryReader`. AC-002. Package precedence and mod-selection UX are unspecified later work.

### REQ-003: Plain state, stable IDs, simulation boundary

State uses plain serializable data, typed entity IDs, content string IDs, and persisted ID allocation. Simulation must run without Godot; presentation reads state and submits commands. Relationship validation catches invalid/duplicate IDs, ownership, workforce, targets, growth, and galaxy metadata. Invalid saves/new games must not become published play state. Dependencies: REQ-002. Sources: [CLAUDE.md](../CLAUDE.md), `State/`, `Ids.cs`, `GameSession`. AC-003. State mutability means the UI mutation prohibition remains a coding rule.

### REQ-004: Shared atomic command validation

Inputs: issuing empire, expected turn, and workforce/project/research/ready intent. Validate issuer, ownership, IDs, counts, prerequisites, completion, and turn. Successful planning edits publish a clone and clear issuer readiness; identical valid choices are no-ops preserving readiness. Rejections preserve the entire original state and report structured reasons. Dependencies: REQ-002/003. Source: [economy commands/errors](FIRST_PLAYABLE_ECONOMY.md), `Commands/`. AC-004. AI and remote transport are not implied implementations.

### REQ-005: Workforce, support, growth, and forecasts

Input: colony population, assignments, planet rates, completed bonuses, rule parameters. Assignments are nonnegative and sum to population. Economic units are integer hundredths. Support is recurring/local, with no stockpile. Deficits proportionally floor production/research and pause growth without reducing population; legacy fully supported growth is base10 plus capped surplus bonus40; rules3 uses each species profile for base/cap/divisor, support requirement and habitat capacity. Births join support next planning period; capacity discards excess growth. Forecast and resolution share exact calculations and ordered breakdowns; overflow fails atomically. Dependencies: REQ-002/003/008. Full formulas, rounding, units, and examples: [economy spec](FIRST_PLAYABLE_ECONOMY.md). AC-005.

### REQ-006: Conserved colony production

Inputs: net production, existing colony reserve, optional legal project. Selection/replacement/clearing spends nothing. Each resolution banks output, pays a covered cost, completes at most one typed building/vessel target, clears it on completion, and preserves remainder. Expansion/species vessel targets use shared snapshot embark eligibility and conserve population; blocked targets retain their bank and selection. No selected project means banking. Bonuses apply from the next snapshot. Current four building definitions/costs remain in JSON; no queues or upkeep are added. Unknown/duplicate/prerequisite-invalid targets reject through REQ-004. Dependencies: REQ-002/004/005/008. Source: [economy production contract](FIRST_PLAYABLE_ECONOMY.md). AC-006.

### REQ-007: Conserved empire research

Inputs: owned-colony research contributions in stable ID order, reserve, optional target. Bank even without a target; switching does not lose progress. Complete at most one technology per empire/turn, spend its cost, preserve remainder, clear target. A newly known prerequisite enables a building in the following planning period. No foreign colony credits, repeat research, or auto-selection. Dependencies: REQ-002/004/005/008. Source: [economy research contract](FIRST_PLAYABLE_ECONOMY.md). AC-007.

### REQ-008: Readiness and turn phases

Every human empire marks ready through commands; all ready triggers resolution. Snapshot calculations precede growth and completion; readiness resets and a validated candidate advances turn once. Failed arithmetic/validation rolls back both resolution and its triggering ready command. No save-load side effect advances the turn. Dependencies: REQ-003/004/005/006/007. Source: [economy resolution contract](FIRST_PLAYABLE_ECONOMY.md), `Turns/TurnResolver.cs`. AC-008.

### REQ-009: Save integrity and continuation

Inputs: valid planning state and effective content; UI save name maps to `user://saves`. Writer schema4 embeds content, rule version, RNG, IDs, choices/readiness, reserves, completions, galaxy metadata, expansion vessels/surveys and rules3 species/environments/habitat modes. Explicitly support only(schema,rules) pairs(1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3). Read schema 1/2 as rules-1 campaigns without regenerating positions, assigning invented regions or injecting current expansion content; reject unsupported schema/rules, corrupt content/state, and invalid references. Writes use flushed temporary replacement; failed writes preserve prior destination and failed loads preserve session. Continuation matches uninterrupted play. Dependencies: REQ-002/003/008/012. Sources: [economy](FIRST_PLAYABLE_ECONOMY.md), [spiral compatibility](superpowers/specs/2026-10-05-spiral-galaxy-design.md), `Saves/SaveGame.cs`. AC-009; graphical dialogs additionally AC-011.

### REQ-010: Galaxy map and inspection

Input: published state, selected system, viewer empire. Show selectable named systems/home marker and known prototype planet types/output rates, plus region labels. Legacy rules 1 permits all-system inspection; expansion rules2/3 expose the public star chart with planet information only from permitted current or last-seen observations. Background points are visual/unselectable; saved dimensions preserve framing, legacy saves fit existing positions, resize changes only presentation. Use shared theme and container layout. Dependencies: REQ-001/003/009. Sources: [spiral design](superpowers/specs/2026-10-05-spiral-galaxy-design.md), `GalaxyMap`, `StarPanel`. AC-010. The 2026-10-22 continuation verifies the runtime handler/layout/resize subset; complete physical-input and small-window readability acceptance remain open.

### REQ-011: Interactive loop and explanations

Inputs: new-game form, map/colony controls, construction/research selectors, End Turn, Save/Load. Display forecast support/growth/output, progress/estimates, shortages, reserve accounting, and one completion report from simulation results. Follow-up actions open the next picker, including after focus scrolling. Report rejection/save/load errors without state loss. New games default to the current 1920x1080 viewport setting; controls must remain usable at the documented smaller test sizes. Dependencies: REQ-001 through REQ-010. Sources: [charter eleven actions](CHARTER.md), [economy reports](FIRST_PLAYABLE_ECONOMY.md), [FP-001/002](playtests/first-playable-2026-10-19.md). AC-011. Uncommitted fixes are preserved. A retained opt-in harness now verifies UI signal wiring, picker geometry, dialog save/load, legacy/corrupt input and continuation; complete OS-input/readability acceptance remains open.

### REQ-012: Repeatability and isolated visual randomness

Same content/state/orders/seed must reproduce authoritative results using integer rules, PCG32, defined ordering, and invariant parsing. Saved RNG resumes identically. Background generation must never advance simulation RNG. Dependencies: REQ-003/008/009. Sources: [CLAUDE.md](../CLAUDE.md), [spiral design](superpowers/specs/2026-10-05-spiral-galaxy-design.md). AC-012 establishes local repeatability; AC-013 covers cross-machine evidence.

### REQ-013: Portability and cross-machine determinism

Target Windows/Linux/macOS and x64/ARM consistency where supported. Run identical content/seed/orders on target environments and compare canonical serialized state; build/run and exercise save continuation on each target. C# web export is not a target. Dependencies: REQ-001 through REQ-012; platform packaging/toolchains. Sources: [charter](CHARTER.md), [CLAUDE.md](../CLAUDE.md). AC-013. Supported OS versions, hardware budgets, package formats, signing/update policy, and exact platform matrix remain unspecified; this row is not delivery acceptance.

### REQ-014: Understandable and meaningful choices

An uncoached player unfamiliar with source should complete the loop, explain expected/actual support, growth and output, describe when completions help, and articulate workforce/goal tradeoffs. Record observations and unresolved clarity defects rather than treating arithmetic tests as usability proof. Dependencies: REQ-010/011 and a participant. Source: approved [validation plan, tasks 3-5](superpowers/plans/2026-10-19-first-playable-validation-and-exploration-design.md). AC-014. The owner explicitly deferred this unavailable participant session in [ADR-005](DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md); it no longer blocks expansion design and is not marked Verified. No new enjoyment score or balance threshold is invented.

## Expansion and future specification gaps

### REQ-015: Exploration, travel, second colony

Implemented under the owner-approved [specification](superpowers/specs/2026-10-22-exploration-and-colonization-design.md), ADR-006 and Native [implementation plan](superpowers/plans/2026-10-22-exploration-and-colonization.md). EXP-001..007 and EXP-AC-001..008 cover per-empire observations, deterministic travel, vessel production/embarkation, explicit founding, multi-colony UI and legacy continuation. Fresh Windows simulation and graphical control acceptance passes are mapped in [the expansion report](../reports/EXPANSION_VALIDATION_2026-10-22.md). All independent review findings are addressed in [the correction report](../reports/EXPANSION_REVIEW_2026-10-23.md); integration and full human/platform acceptance remain separate. ADR-005's uncoached session stays Deferred.

### REQ-016: Distinct playable species

The roster supplied the starting point; creative amendments were permitted under ADR-007 and the complete [species mechanics specification](superpowers/specs/2026-10-23-species-mechanics-design.md) is now approved in [ADR-008](DECISIONS/ADR-008-approved-species-mechanics.md). It defines exact profiles, habitats, conserved founding, JSON/state/view contracts, compatibility and SPC-AC-001..010. Design review SPC-AC-010 and separate plan approval are satisfied. The owner-approved [ten-task plan](superpowers/plans/2026-10-23-species-mechanics.md) has been executed inline through the integrated acceptance gates; final review is recorded in the handoff report. [Species evidence](../reports/SPECIES_VALIDATION_2026-10-23.md) maps SPC-AC-001..009 to exact simulation tests and actual-control generated campaigns. REQ-016 is Verified for the executed local automated contract after independent review/corrections; full human/release acceptance remains open. Preserve owner prose edits and old-campaign fixtures. Species customization, final balance and human acceptance remain outside this slice.

### REQ-017: Remaining strategic game

Track the charter's later AI, diplomacy, ships/fleets/design, warfare, events, victory/defeat, expanded technology/development/setup as a deferred scope envelope. Tactical combat remains conditional. These are not independently implementation-ready: create stable child requirements and concrete failure/acceptance cases when each milestone is approved. The owner selected rival expansion for design in [ADR-009](DECISIONS/ADR-009-rival-expansion-design-entry.md). Its child contracts RIV-001..007 and RIV-AC-001..012 are in the [approved full design](superpowers/specs/2026-10-24-rival-expansion-design.md). [ADR-010](DECISIONS/ADR-010-approved-rival-expansion-design.md) satisfies the full design gate; the [ten-task plan](superpowers/plans/2026-10-24-rival-expansion.md) was subsequently approved for Native execution. Parent In Progress records this bounded slice; the remaining strategic envelope stays Deferred. The Native plan/review and bounded follow-up are complete;648 simulation and final four-scenario gates pass. Alternative fallback and encounter identity/orbit details are corrected and independently reviewed with no findings. All twelve child criteria and M4-I are Verified for the local automated contract; broader parent scope and human/release acceptance remain open. See [follow-up closure](../reports/RIVAL_POLISH_2026-10-24.md). See [rival evidence](../reports/RIVAL_VALIDATION_2026-10-24.md). Networking readiness does not authorize a network implementation. Source: [CHARTER.md](CHARTER.md). AC-017 is a planning gate.

Cross-cutting gaps before relevant delivery: accessibility/input coverage, localization, performance/resource budgets, maliciously large inputs, clean install/export, dependency/security audit, and third-party attribution. Existing decisions do not settle those policies. See [adoption findings](../reports/ADOPTION_AUDIT.md) and the [implementation plan](IMPLEMENTATION_PLAN.md).


### Approved M5 child requirements — execution not started

[ADR-012](DECISIONS/ADR-012-approved-colony-development-design.md) records the owner's approval of the complete [colony-development specification](superpowers/specs/2026-10-24-colony-development-design.md). COL-001..008 and COL-AC-001..012 define foundations/next-tier construction, editable templates, finite queues, scoped workforce policy, atomic preparation, management/reports, cosmetic planet identity and compatibility. M5-D/COL-AC-012 are Verified for design; COL-AC-001..011 and M5-I Not Started. The separate [twelve-task plan](superpowers/plans/2026-10-24-colony-development.md) is written/self-reviewed, M5-P In Progress for owner plan review; retained Native requires no new method selection. No implementation/application test result. [Future crisis/fleet intent](design-notes/2026-10-24-delegation-crises-and-fleet-intent.md) remains recorded without authorizing those systems. Parent REQ-017 In Progress and17-row totals unchanged.
