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


### Approved M5 child requirements — implementation and final review

[ADR-012](DECISIONS/ADR-012-approved-colony-development-design.md) records the owner's approval of the complete [colony-development specification](superpowers/specs/2026-10-24-colony-development-design.md). COL-001..008 and COL-AC-001..012 define foundations/next-tier construction, editable templates, finite queues, scoped workforce policy, atomic preparation, management/reports, cosmetic planet identity and compatibility. M5-D/COL-AC-012 and M5-P are Verified for approved design/plan. All twelve Native tasks are implemented; all COL functional criteria and M5-I are Verified for local automated scope after authorized corrections. [Validation](../reports/COLONY_VALIDATION_2026-10-24.md) maps the original982-test gate and corrected984-test main/five runtime gates to criteria. Rules6/writer7 is now active on main; rules5/writer6 remains a preserved compatibility generation. Genuine old rules/content remain unchanged. [Earlier fleet intent](design-notes/2026-10-24-delegation-crises-and-fleet-intent.md) remains future scope. Parent REQ-017 In Progress and17-row totals remain unchanged.

Historical M5 review: reports/COLONY_FINAL_REVIEW_2026-10-24.md records the original findings and initial deferrals; current correction closure follows below.

Historical M5 correction closure (2026-10-24): all three authorized presentation fixes and the fresh review duplicate-vessel correction are verified and locally integrated. Functional source4a2e430 passes988 simulation tests; Colony665 assertions+4 provenance; Rival1146+12 provenance; Species1194; Expansion275; Legacy466+27 geometry. Fresh main Suite/Colony/normal rebuild/five harness exclusions pass separately. All twelve COL child criteria and M5-I are Verified for local automated/design scope. Parent REQ-017 and17-row totals remain12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred. See PROJECT_STATUS.md and reports/COLONY_POLISH_INTEGRATION_2026-10-24.md. Human/platform/delivery/release limits remain open. Next is bounded milestone selection/specification; no automatic implementation or cleanup.


### Approved M6 child requirements — colony crises/plague

The owner approved the complete [colony-crisis/plague specification](superpowers/specs/2026-10-24-colony-crises-plague-design.md) on2026-10-25. [ADR-014](DECISIONS/ADR-014-approved-colony-crisis-plague-design.md) satisfies M6-D/CRI-AC-012 for full design approval; ADR-013 remains historical drafting authority. CRI-001..007 cover occurrence/warning, health/loss, response economics/cure, actual owned cargo containment, delegation/attention/reporting, command/phase/AI transactions and persistence. The approved exact costs/timing/transport/culture deferral/rules6-writer7 contract is implemented and locally integrated into `main`. The [separate14-task plan](superpowers/plans/2026-10-25-colony-crises-plague.md) was approved and executed under Native inline. M6-P and M6-I/CRI-AC-001..011 are Verified for their respective design/plan and local automated technical scopes after one [independent review/correction pass](../reports/CRISIS_REVIEW_CORRECTIONS_2026-10-25.md); no second review. [Task14 validation](../reports/CRISIS_VALIDATION_2026-10-25.md) maps criteria to actual tests/runtime evidence; [fresh merged-main integration](../reports/CRISIS_INTEGRATION_2026-10-25.md) confirms the combined gates and owner-edit preservation. Parent REQ-017 and17-row totals remain unchanged; existing M5 technical acceptance remains Verified.

### Approved M7 child requirements — separate plan review pending

[SCT-001..007 and SCT-AC-001..015](superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md) are fully approved in [ADR-016](DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md): scout authority/knowledge/withdrawal, bounded AI/transactions/history/compatibility and stationary space-ameba warning/study/escape/loss. M7-D/SCT-AC-012 Verified for design only; the [separate15-task plan](superpowers/plans/2026-10-25-scout-survival-and-ameba.md) is ready for review, M7-P In Progress; M7-I/functional criteria Not Started. Native inline/one final fresh reviewer retained. Rules7/writer8 is the approved future target; current product remains rules6/writer7. All17 parent rows/counts, M6 and deferred human/platform/release limits remain unchanged; no inferred broader warfare/roaming/sensors/pursuit scope.

<!-- M7 current acceptance -->
## Current M7 implementation and acceptance — 2026-10-25

Owner approved the full amended ADR-016 design and separate15-task plan; earlier drafting/plan-pending statements above are historical. M7-D/M7-P Verified; M7-I **Implemented, Unverified for exact full acceptance; review M1 deferred** on isolated codex/scout-survival. SCT-AC-012 is design-only Verified; functional SCT-AC-001..011/013..015 have the executed criterion mapping in [validation/handoff](../reports/SCOUT_VALIDATION_2026-10-25.md), with review status: Complete; no open Critical or Important findings. Branch current rules7/writer8/29 pairs; primary main42e1282 remains rules6/writer7. Actual suite 1398; all7 fresh native scenarios, zero diagnostics and ordinary7/helper2 exclusions are recorded, with genuine older futures and generated/labelled provenance. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. No integration/push/cleanup; owner chooses next disposition.

<!-- Scout M1 closure2026-10-26 -->
## Scout M1 follow-up closure — 2026-10-26

The remaining omitted-habitat setup explanation is corrected on codex/scout-polish, validated source907207f, with actual native RED→GREEN and five custom-map boundaries. [Follow-up evidence](../reports/SCOUT_POLISH_2026-10-26.md): Scout237, full Suite1398/1398,0 diagnostics, ordinary build0 warnings/errors and actual7-scenario/2-helper exclusions. SCT-AC-013/M7-I are now Verified for their local automated scope; earlier M1 deferral statements are historical. No known review finding remains open. Other six native scenarios retain their prior-source evidence; no second independent review or fresh other-six run is claimed. Primary main42e1282/four owner edits/seven worktrees/old evidence remain preserved; no integration/push/release/cleanup. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. Next is owner disposition of the completed follow-up branch.


## Scout local main integration — 2026-10-27

The owner's “merge that” authorized local integration of the completed scout/space-ameba work and final M1 follow-up. Main now contains validated source `a52dfc61c9a16ec10e0091b52783029afac73e38`: rules7/writer8/29 pairs. [Fresh merged-main evidence](../reports/SCOUT_INTEGRATION_2026-10-27.md) passes1398/1398 simulation tests and all seven native scenarios/4491 records (Scout 237; Legacy 493; Expansion 275; Species 1194; Rival 1158; Colony 670; Crisis 464), with0 diagnostics, normal builds0 warnings/errors and actual7-scenario/2-helper exclusions. SCT functional criteria/M7-I remain Verified for local automated scope; prior pending-integration statements are historical. Checkout preservation is verified425/425; original owner edits remain uncommitted and all branches/seven worktrees/recovery/QA remain retained. No new review, push, release or cleanup. REQ-017/17-row totals and human/platform/security/export/release limits remain unchanged. Next is bounded milestone selection/specification before implementation.


## M8 Fleet Command and named admirals — design approved, plan review pending

Owner “0approved” accepts the complete [design and starter defaults](superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md) in [ADR-018](DECISIONS/ADR-018-approved-fleet-command-and-admirals-design.md). M8-D/FLT-AC-012 Verified for full design only. The separate [17-task plan](superpowers/plans/2026-10-27-fleet-command-and-admirals.md) is drafted/self-reviewed; M8-P In Progress pending owner review; M8-I/functional FLT-AC-001..011 Not Started. Native inline/no per-task agents/one final fresh whole-branch implementation reviewer remains chosen. No product code/content/default/schema or primary-main change, new baseline capture or fleet gameplay run occurred. Main rules7/writer8/29 pairs, parent REQ-017/17-row totals (12 Verified,3 Implemented, Unverified,1 In Progress,1 Deferred), M7/M6/human/platform/security/export/release limits and every owner file/worktree/branch/QA/recovery remain unchanged. No main integration/push/release/cleanup is inferred.

### Historical scoped decisions before complete approval

The chronological notes below retain their original checkpoint status; ADR-018 and the current plan-review gate supersede their earlier pending-design language.


M8 scoped decision2026-10-27: the owner confirms doctrine editable from the start and research improving recovery equipment (ADR-017/FLT-006). No research gate for doctrine changes remains open. Exact equipment/bonus/XP/culture values and the rest of the full M8 design remain pending gameplay review; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Documentation only; current main/product and owner edits remain unchanged.


M8 scoped owner choice2026-10-27: Navigator, Surveyor and Organizer; promotions at10/30 XP. The old Recovery specialist, ameba-only Surveyor proposal and20/60 thresholds are superseded in the active specification; historical review/check artifacts remain intact. Exact bonus magnitudes/event XP/proposed XP cap, Organizer accounting, recovery values/cultural defaults and full M8 acceptance remain pending review. Doctrine remains editable from the start; research improves recovery equipment. M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. No code/main/owner-file change.


M8 scoped owner decision2026-10-27:50% officer recovery,70% with recovery research and a two-turn successful-recovery delay are confirmed. Current review topic is cultural defaults; do not treat provisional Harl Last watch as approved. Other unreviewed rules/values and full M8 acceptance remain pending; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Main and four owner files remain unchanged.


M8 cultural-default decision2026-10-27: all four cultures begin recovery-focused; Wenth present Re-establish command link. Last watch is manual-only; the original Harl default is superseded. Do not reopen settled doctrine/research, specialties/10-30 XP,50-70% recovery/two-turn delay or cultural defaults. Use the specification's remaining-review checklist for unconfirmed details. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees/evidence remain preserved.


M8 bonus/XP decision2026-10-27: Navigator5/10%, Surveyor1/2 research per first survey, Organizer5/10% scoped request-ship cost reduction and2/1/8 XP awards are owner-confirmed; do not re-ask. Current review is fleet/construction operations, with other unconfirmed details/full design approval still pending. Preserve docs/design-notes/2026-10-27-combat-retreat-morale-and-damaged-ships.md as future owner direction (degrading capabilities, retreat/morale, limping home/detachment/hit-and-run, automation with possible manual control), not M8 damage/combat scope. Plan/implementation remain Not Started; main/four owner files/worktrees/evidence unchanged.


M8 fleet/construction review2026-10-27: owner retains the reviewed proposal and Normal/Urgent request priority. Normal preserves explicit queues; Urgent previews moving before unstarted work, never silently replacing the active project. Preserve finite authority, priority-aware estimates, tokens/reserve/manual control and current combat-note future-only scope. Next review: commissioning/Wenth details/recovery technology and other unconfirmed details, then full M8 approval before separate planning. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees unchanged.


M8 availability decision2026-10-27: starting Navigator with Surveyor/Organizer candidates,2-turn commissioning,4-active-admiral limit and co-located ship-based transfers confirmed; fleet controls remain usable without an officer and Wenth availability stays colony-body based. Do not re-ask those choices. The64-lifetime-commission bound remains a proposal. Next review covers Wenth link/equipment recovery, technology and remaining draft details before full M8 approval/separate plan. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner edits/worktrees unchanged.


M8 final scoped choices2026-10-27: Wenth recovery2/1 turns (basic/researched), manual Last watch4/3, and Command Recovery24 displayed research after Industrial Coordination confirmed. Read reports/FLEET_COMMAND_GAMEPLAY_REVIEW_2026-10-27.md and the complete specification for remaining starter defaults and full-design approval; do not re-ask settled gameplay numbers. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started until their gates. Preserve main/four owner files, seven worktrees/branches and evidence; future combat direction remains a note.


Current M8 implementation authorization2026-10-27: owner “approved” accepts the separate17-task plan at7d175ad. M8-P Verified; M8-I In Progress on isolated codex/fleet-command. Native inline continuously executes all17 tasks, no per-task agents and one final fresh whole-branch implementation reviewer. Follow .superpowers/sdd/2026-10-27-fleet-command-and-admirals/progress.md; completed tasks must not repeat. Task1 must freeze genuine rules7 futures before any product/content change. Fresh unchanged baseline Suite1398/1398 passes. Full gameplay approval remains ADR-018; earlier planning-only restrictions are historical. Main c554797/four owner files/seven worktrees/all old QA/recovery remain preserved. No main integration/push/release/cleanup authority.


Current M8 combined-validation checkpoint2026-10-28: tasks1..16 formally complete on isolated codex/fleet-command; Task17 combined Suite1529/1529, build0 warnings/errors and all8 native scenarios pass, with actual opt-in negative control/ordinary8+2 exclusions. Current branch rules8/writer9/37 pairs; primary main c554797 remains7/8/29. Read reports/FLEET_COMMAND_VALIDATION_2026-10-27.md and reports/fleet-execution/final-summary.json. Actual generated3col/4scout/all-species Main SaveLoad/futures and separately labelled rare/scale/paging evidence retained. M8-I In Progress and FLT functional criteria Implemented, Unverified pending the ONE fresh whole-branch review, not yet dispatched at this checkpoint. Native retained/no per-task or second reviewers; no main integration/push/release/cleanup authority. All4 owner files/main index/all7worktrees/branches/raw QA retained;2431 actual-checkout hashes verified. Earlier planning/implementation-start notes are historical; trust this plan's ledger and do not repeat completed work. Future combat note remains outside M8; parent counts/human/release limits unchanged.


M8 final implementation/review checkpoint2026-10-28: approved17tasks complete on isolated codex/fleet-command, validated source36e6ec4. ONE fresh fullbranch review c554797..2464d1f and ONE regression-first5-finding correction pass done; no openCritical/Important/no second review. Corrected Suite1545/1545, focused compatibility/rollback73/73, Fleet1482+33/Scout237/Crisis464/Rival1158 native fresh, unchanged earlier Legacy/Expansion/Species/Colony evidence retained,0diagnostics/build0/actual8+2 exclusions. Branch8/writer9/37pairs; primary mainc554797 stays7/8/29. M8-I and FLT-AC-001/005/011 remain Implemented, Unverified for exact acceptance: rolefilter missing, lost-Return Attention missing, separate idle-candidate list unpaged. Other8functionalcriteria Verified localautomated,012design Verified, M8-D/P Verified. Reports/FLEET_COMMAND_VALIDATION_2026-10-27.md, fleet-execution/final-summary.json/final-review.md/progress ledger are authoritative. All4ownerfiles/mainindex/7worktrees/branches/stashes/oldQA/futures preserved,2431index-checkout hashes and closing recheck pass. Futurecombat remains note. Parent17-row counts/human/platform/security/export/release limits unchanged. No merge/push/release/cleanup authority. Next owner disposition of isolatedbranch/threeMinors; do not repeat tasks/reopen design/method/review. Earlier pending checkpoints are historical.

M8 remaining-gap closure2026-10-28: owner explicitly requested fixing all3 remaining Minors before merge. Source `035877a40d45b39b1e764980e386023f706dd1dd` closes M1 lost-Return Attention (including persisted reason/report/badge with routineOff), M3 owned-role filtering, M4 bounded32-row assembly idle paging with explicit cross-page selections/stale-removal disclosure. FullSuite1547/1547, fresh Fleet 1666 result records + 39 separate provenance entries, Scout 237 result records + 9 separate provenance entries, Crisis 464 result records (including inline provenance); normalbuild0warnings/errors/actual8+2exclusions. M8-I/FLT-AC-001/005/011 now Verified for localautomated scope; all12FLT criteria Verified within automated/design limits. Read reports/FLEET_COMMAND_FOLLOWUP_2026-10-28.md and fleet-execution/minor-summary.json; earlier3-Minor deferrals/final-summary are historical. Original17tasks stay complete; no second reviewer. Mainc554797/four ownerfiles/index/all7worktrees/unrelatedrefs/6053 oldreports/346 recordedQA saves preserved. Branch remains unmerged; no push/release/cleanup or new subsystem. Parentcounts/human/platform/security/export/balance limits unchanged. Next: local integration when requested.

M8 local integration2026-10-28: owner “merge” explicitly authorizes local main integration of codex/fleet-command `6bcdc2dac0a09ecb5de9289af110ef20a19f8700` with committed owner documentation `a58cc671bdfbba9c59e0340613f9f04410c65197`. Product source035877a; all3 remaining findings are closed, M8-I/all12FLT criteria Verified within local automated/design scope. Fresh combined main Suite1547/1547, Fleet1666checks+39separateprovenance,0engine diagnostics, ordinary rebuild0warnings/errors and actual8+2exclusions pass. Only PROJECT_STATUS conflicted: latest fleet status and exact peaceful-mode paragraph retained; other3ownerfiles exact. Main now rules8/writer9/37pairs; old rules1..7 retain embedded behavior. Read reports/FLEET_INTEGRATION_2026-10-28.md and fleet-integration-20261028/summary.json. Prior branch-only/uncommitted-owner checkpoint wording is historical; those4files were committed separately ata58cc67 before merge. All7worktrees/unrelatedrefs/fleetbranch/recovery/oldreports/QA/futures retained, recoverybranch codex/fleet-premerge-20261028. Single review/Native preference retained; no second review or repeated completed tasks. Parent17-row counts/human/platform/security/export/balance limits unchanged. No push/release/cleanup or new subsystem. Next: choose the next bounded milestone before design/implementation.


## M9 bounded combat draft

[ADR-019](DECISIONS/ADR-019-fleet-combat-design-entry.md) records owner-authorized [combat drafting](superpowers/specs/2026-10-28-fleet-combat-damage-and-withdrawal-design.md) after M8 integration/push. CBT-001..013 are proposed child contracts of REQ-017; functional CBT-AC-001..011 are Not Started and CBT-AC-012/full design is In Progress. No new gameplay defaults are approved or active. The 17 parent rows and their12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred totals are unchanged.


M9 full gameplay approval2026-10-28: [ADR-020](DECISIONS/ADR-020-approved-fleet-combat-design.md) accepts all CBT-001..013 contracts and defaults in the full design. CBT-AC-012/M9-D Verified for design only; CBT-AC-001..011/M9-I Not Started. [Separate16-task plan](superpowers/plans/2026-10-28-fleet-combat-damage-and-withdrawal.md) written/self-reviewed; M9-P In Progress pending confirmation. Parent17-row totals unchanged; gameplay remains8/9/37.


M9 implementation checkpoint2026-10-28: the owner approved the separate16-task plan; M9-P Verified. Tasks1..15 are complete on isolated codex/fleet-combat, rules9/writer10/46 pairs. M9-I and functional CBT-AC-001..011 are Implemented, Unverified pending task16 combined native gates and the ONE fresh whole-branch review. FullSuite1688/1688 includes exact compatibility/frozen futures and alternate embedded-role continuation. CBT-AC-012 remains design-only Verified in ADR-020. See reports/COMBAT_VALIDATION_2026-10-28.md; all earlier planning-only checkpoints are historical. Parent17-row totals and human/platform/security/export/release limits unchanged. Main and all owner prose/evidence/worktrees retained; no integration/push/cleanup.

M9 final product verification2026-10-29 supersedes prior pending checkpoints: source `4176996fbda516849a9f5e24691c451e89960019`, one fresh review/one correction pass, all3 Important findings closed, no Critical/Minor. FullSuite1733/1733,45 review regressions, initial all9 native9403 records and corrected Combat3271/93 provenance/generated12 pass. M9-I and functional CBT-AC-001..011 Verified for local automated scope; CBT-AC-012 remains full-design Verified in ADR-020. All16 approved tasks are complete; final task gate1733/1733 passed. [Final evidence](../reports/COMBAT_VALIDATION_2026-10-28.md) records exact sources, limitations and preservation;17 parent rows/totals unchanged. No integration/push/release/cleanup.

| Child contract under REQ-017 | Status | Evidence |
| --- | --- | --- |
| CBT-001 Setup/pockets/founding information | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-002 Hulls/construction/content | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-003 Damage/speed | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-004 Commitment/lifecycle | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-005 Exchange/targeting | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-006 Cohesion/standing orders | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-007 Withdrawal/ports | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-008 Frozen detachment | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-009 Finite repair | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-010 Officer/cargo/old-system integration | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-011 Phases/transactions | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-012 Views/reports/UI | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |
| CBT-013 Compatibility/bounds | Verified for local automated scope | [Combat acceptance mapping](../reports/COMBAT_VALIDATION_2026-10-28.md#acceptance-mapping) |


M9 local integration, 2026-10-29: the owner selected option1, authorizing a local fast-forward of `codex/fleet-combat` (`fa11f96e2e35973d533c0a85630348461bae07a9`) into main from `dc190937ab94a464ba81823d25738164db538e73`. Product source remains `4176996fbda516849a9f5e24691c451e89960019`; main now uses rules9/writer10/46 explicit pairs. Fresh main Suite1733/1733 and native Combat3271 checks+93 separate provenance/12 generated campaigns pass; both builds have0 warnings/errors,0 engine diagnostics, actual opt-in9+2 detection and ordinary9+2 exclusions. All72 generated stage saves match the corrected branch byte-exactly. One fresh implementation review/one correction pass remains complete with no open findings; no second review. M9-I and functional CBT criteria remain Verified for local automated scope, design/plan approvals remain Verified, and the17 parent rows/totals are unchanged. Four protected owner files, exact peaceful paragraph, all8 worktrees/branches/stashes and prior raw evidence/QA saves remain. Recovery: `codex/combat-premerge-20261029`. See [integration evidence](../reports/COMBAT_INTEGRATION_2026-10-29.md) and [machine summary](../reports/combat-integration-20261029/summary.json). Earlier pending/branch-only statements are historical. No push, release or cleanup. Next: choose the next bounded milestone before further design or implementation.


## M10 approved ship design and refitting

M10 drafting,2026-10-29: owner “use your own judgement” delegates the full ship-design/refitting proposal after local M9 integration. Read [the complete design](../docs/superpowers/specs/2026-10-29-ship-design-and-refitting-design.md), [ADR-021](../docs/DECISIONS/ADR-021-ship-design-and-refitting-design-entry.md) and [builder-check disposition](../reports/SHIP_DESIGN_GAMEPLAY_REVIEW_2026-10-29.md). Two frames, limited weapon/armor/engine choices, four research unlocks, immutable saved designs, exact-design queues/requests and paid same-frame full-condition refits are drafter defaults for full review. The starting Standard Escort matches M9 exactly. M10-D/SHP-AC-012 In Progress; M10-P/M10-I and functional SHP-AC-001..011 Not Started. Product remains rules9/writer10/46 pairs; proposed10/11/56 is inactive. No product changes, gameplay test run, genuine rules9 future capture, separate implementation plan, new worktree, push/release/cleanup or repeated M9 review. Native/no per-task implementation agents/one final implementation reviewer retained after later plan approval. Parent17-row totals, four owner files, exact peaceful paragraph, all8 worktrees/branches, old saves/QA/reports/recovery remain preserved. Next: review the complete proposal before its separate implementation plan.

| Child contract | Proposed scope | Status |
| --- | --- | --- |
| SHP-001 | Catalogue and immutable design identity; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-002 | Frames/components and exact calculations; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-003 | Manual research unlocks; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-004 | Pinned production and colony queues; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-005 | Exact-design fleet requests and existing officers; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-006 | Refit acceptance and price; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-007 | Refit resolution, cancellation and interaction; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-008 | Presentation, scale and privacy; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-009 | Compatibility, determinism and transactions; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |
| SHP-010 | Acceptance and handoff; [full contract](superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) | Proposed |


M10 full approval/planning,2026-10-29: owner “Looks good, write the implementation plan” accepts the complete ship-design/refitting description at71ec87b (pre-approval SHA256 a54ba0c275f0c8345b57fba4dc247bd6453133344a92eeb1cc384a9f2791252c). [ADR-022](../docs/DECISIONS/ADR-022-approved-ship-design-and-refitting-design.md) records full gameplay/default approval. M10-D/SHP-AC-012 Verified for design only. The separate [17-task plan](../docs/superpowers/plans/2026-10-29-ship-design-and-refitting.md) and [author self-review](../reports/SHIP_DESIGN_IMPLEMENTATION_PLAN_REVIEW_2026-10-29.md) are ready for owner review; M10-P In Progress, M10-I/functional SHP criteria Not Started. Retain Native inline/no per-task agents/one final fresh whole-branch implementation reviewer; do not re-ask method or settled choices. Current product remains rules9/writer10/46 pairs; proposed10/11/56 is inactive. No new worktree, genuine rules9 future capture, product changes, build/gameplay/native validation or implementation-review dispatch occurred while planning. Preserve four protected owner files, exact peaceful paragraph, all8 worktrees/branches, old reports/fixtures/QA/recovery and17 parent rows/totals. No push/release/cleanup or future product integration. Next: review this concrete plan before execution.
