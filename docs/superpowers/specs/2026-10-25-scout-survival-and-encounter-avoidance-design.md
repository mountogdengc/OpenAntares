# Scout survival and encounter avoidance

2026-10-25. **Draft for owner gameplay review; no design or implementation approval.** REQ-017 / proposed M7-D In Progress. M7-P/M7-I Not Started. All exact choices below are drafter recommendations unless identified as existing authority. Current product remains rules6/writer7.

Authorities: [current status](../../../PROJECT_STATUS.md), [charter](../../CHARTER.md), [engineering rules](../../../CLAUDE.md), [recorded fleet intent](../../design-notes/2026-10-24-delegation-crises-and-fleet-intent.md), approved [exploration](2026-10-22-exploration-and-colonization-design.md), [rivals](2026-10-24-rival-expansion-design.md), [species](2026-10-23-species-mechanics-design.md) and [plague](2026-10-24-colony-crises-plague-design.md). [ADR-015](../../DECISIONS/ADR-015-scout-survival-design-entry.md) records drafting authority only.

## 1. Intent, scope and the survival boundary

The owner wants scouts to try to survive/escape danger, with encounters offering alternatives to unavoidable tactical battle and loss. Speed, warning range and escape chance were suggested factors, not approved formulas. Broader fleet goals, preferred/forbidden targets and retreat/hold orders remain recorded intent.

Existing rivals are peaceful expansion competitors. Stationary foreign vessels and colonies are observable presence, **not an established threat**. There are no armed interceptors, damage, destruction, hostile relations, sensor radii or transit encounters. This draft preserves that decision. It proposes a useful first slice: optional scout caution that avoids known foreign presence and withdraws from unwanted encounters through ordinary travel. It cannot establish survival against weapons that do not exist. Reports must never describe a peaceful sighting as an attack or claim a successful combat escape.

Success is understandable, bounded standing orders with a real exploration cost: cautious scouts may take longer routes, leave useful observation posts or wait for instructions. The player can instead choose continued observation without a hull-loss penalty in this slice. No artificial danger, free escape or new hull bonus is added to make the feature look complete.

| Approach | Consequence | Recommendation |
| --- | --- | --- |
| Optional avoidance and ordinary withdrawal under peaceful rules | Useful orders now; no lethal survival claim | This bounded slice |
| Add a territorial sentry, pirate or environmental hull-loss encounter | Requires new threat generation, warning, damage/loss and balance contracts | Separate design if the owner wants live danger next |
| Introduce hostile rivals and interception | Changes approved co-location, movement, information and warfare rules | Defer to warfare design |

Includes two scout postures, own-view route filtering, automatic withdrawal preparation, explanations, AI use and explicit compatibility. Excludes automated human exploration, fleet grouping, warships/design, escorts, hold-at-all-costs, combat, capture, pursuit, mid-route diversion, long-range detection, probability rolls, new species traits, diplomacy, territorial exclusion, peaceful-mode design, victory, networking, art and release. Population-carrying expeditions retain their existing movement/founding/crisis rules.

## 2. SCT-001: Scout authority and two postures

Eligible roles are existing valid non-founding, zero-cargo vessel definitions, including core `scout`; use role capabilities, not a species or role-ID special case. Core cost8 production/speed300 and existing founding-role costs/speed200 remain unchanged. No research gate, fuel, maintenance resource, speed upgrade or free replacement is introduced.

- **Observe (default):** retain existing manual travel, arrival survey and peaceful co-location. No automatic movement or exclusion. The inspector explains that current encounters cause no combat or hull loss.
- **Avoid encounters (explicit opt-in per scout):** reject a destination with known foreign presence, and, when currently sharing a system with foreign presence, prepare an ordinary withdrawal if no permitted manual departure exists. This authorizes this scout's movement only. It never enables colony governors, crisis authority, research choices or automatic outward exploration.

All newly created human and AI scouts initially have Observe; initial creation is explicit saved state. Human posture is changed only by an ordinary empire-issued expected-turn command. New-rules AI subsequently selects Avoid through that same command. No global takeover threshold or inherited custom doctrine editor is added. No batch command is required in this slice.

Posture may change in transit, but cannot cancel, redirect or delay the committed route. The UI distinguishes 'applies to future planning' from a current travel change. Observe is also how a human explicitly elects to remain, revisit or investigate an excluded system; there is no one-leg waiver or hidden override lifetime.

Turning on Avoid previews and atomically clears an unstarted move if its destination is excluded by the issuing empire's current view. Other permitted pending moves remain. Turning it off leaves any pending move intact, including an already prepared withdrawal; the player can clear/replace it through ordinary controls. Posture changes never erase surveys or contacts. A true no-op preserves the original state/reference/readiness; a changed posture or order clears only the issuer's Ready.

## 3. SCT-002: What a scout can avoid

A star is excluded for an empire when either of these facts appears in its permitted published view:

1. A currently or last-seen foreign colony is recorded on any surveyed planet there. A stale observation is still grounds for caution, explicitly labelled with its observation turn; it is not a claim that the colony still exists.
2. At least one stationary foreign vessel is currently visible there through ordinary friendly presence.

No contact-only exclusion, culture-based hostility, unseen current occupancy, foreign destination/ETA, hidden colony count, foreign cargo/health, seed reconstruction or nearest-enemy query is permitted. Unknown stars remain legal outward travel targets. An empty stale observation means 'no foreign presence recorded', never 'safe'. Own colonies do not cancel a visible foreign presence in the same system.

Presence is system-wide for this voluntary scout policy only. It does not establish ownership of a star, impede another empire, close a port or stop an expedition. A foreign vessel sighting disappears when observation ends exactly as today. Do not retain its ID/role/location as an avoidance blacklist. Last-seen colony facts and contact identities persist by their existing rules. A new current empty observation replaces an old colony sighting normally.

Route previews say 'no known foreign presence', 'known foreign colony, last seen turn T' or 'foreign vessel currently observed'. They show distance/speed/duration from the existing integer calculator, information freshness and the committed-until-arrival restriction. They promise neither detection en route nor guaranteed safety. No additional sensor radius or PRNG stream is introduced.

## 4. SCT-003: Withdrawal and manual orders

For each stationary Avoid scout at the beginning of a planning preparation:

1. If it has a permitted manual pending departure, retain it. A normal human movement order to a non-excluded public star, including an unknown star, takes priority over automatic fallback.
2. If its current star is not excluded, do nothing. Avoid does not send a human scout outward, return it home merely because its survey finished, or move it because a contact exists somewhere else.
3. Otherwise choose a different retreat star with no known foreign presence. First consider own-colony systems; if none qualify, consider surveyed systems. Within the chosen group rank ordinary travel duration from the current star, then StarId. Surveyed fallback may be stale and must be labelled accordingly. Do not select an unvisited star automatically. Multiple scouts may use the same fallback.
4. If a candidate exists, issue the ordinary pending move through shared validation. A previously prepared departure to a now-excluded destination is cleared/replaced in this same preparation. If none exists, clear that excluded departure if present, remain stationary and publish persistent Attention: 'No known retreat destination; choose Observe or give a permitted destination.' There is no destruction, invented safe point, teleport, emergency speed boost or unbounded search.

An unstarted move that becomes excluded is handled at the next preparation even when the origin has no foreign presence: clear it; then withdraw only if the origin is also excluded. Ordinary selection of an excluded destination while Avoid returns a distinct own-knowledge rejection, without mutating an earlier order/readiness. Unknown/nonexistent or foreign vessel commands retain the existing generic validation/privacy behavior. Validate ownership before returning posture/presence-specific information.

Automatic decisions are computed when the human requests Ready, on the private candidate after the existing crisis/development preparation; views, forecasts, saves and non-Ready commands never prepare movement. A changed manual order can replace a prepared withdrawal before the empire becomes Ready again. A non-final human Ready may commit its own prepared move, as existing delegated planning does; final resolution failure returns the exact state before that final Ready request, preserving another human's earlier accepted orders.

Final-Ready preparation must not replan already-ready humans from other empires' later private choices. No observation refresh occurs during planning. A scout in transit never receives a move; a scout completed this resolution first becomes eligible in the next planning period. Arrival cannot cause a second departure during the same resolution.

## 5. SCT-004: Encounter timing, costs and reports

Retain the existing movement and observation boundary. Arrivals finish ordinary travel; final stationary presence reveals **all planets in that system**, ownership and visible stationary vessels, then establishes contacts normally. Avoid does not grant pre-arrival planet information, prevent a first contact already observed, or erase the survey because the scout intends to leave. If avoiding planetary discovery itself is desired, that is a separate change to the approved exploration model.

An unexpected foreign presence discovered at published turn T is an Attention fact for that scout. Earliest automatic departure begins in the next T-to-T+1 resolution after Ready; a duration2 withdrawal arrives at T+2. The scout supplies ordinary current observation while stationary at T. On departure its old system becomes last-seen unless another friendly vessel/colony remains. No intermediate transit position supplies observation, contact or interception. A foreign arrival after this scout departs does not create a transient same-system encounter.

Foreign vessels may depart simultaneously, leaving the scout to make an unnecessary withdrawal. Keep the order: it was based on permitted facts. Do not inspect their plans or refund/reverse a leg. Faster scouts shorten normal travel only; speed confers no unstated evasion roll. There is no hull-loss roll for Observe or Avoid, and no new currency, population or reserve cost. The actual cost is travel time and the observation/route opportunities forgone.

Use an owner-only scout-order summary alongside ordinary arrival/contact reports. Show scout ID, posture, triggering observed fact/freshness, retained manual departure or chosen fallback, route duration, any cancelled pending order, and a clear reason for waiting. Explain 'withdrawal ordered' versus 'travelling' versus 'arrived'; never say a ship escaped pursuit. Do not expose the other empire's private plans, reports, cargo, home or health. An observed neutral encounter is Attention, not an existential Crisis emergency; it creates no new Ready acknowledgement gate and cannot satisfy a plague receipt.

Keep latest preparation/arrival outcome and a ten-turn owner-only history across save/load. Deduplicate a persistent wait by scout/current-star/reason until its condition changes; it remains visible at Attention even if routine summaries are Off. A deliberate posture change, selected destination, actual arrival or changed reason can generate a new record. Thirty waiting scouts use one grouped summary, no modal per ship. History records immutable own facts and permitted observations, not live references; an old foreign-vessel sighting retained in an explicitly historical order explanation never becomes a current map marker or planner input.

## 6. SCT-005: AI, bounded work and transaction order

Only proposed rules7 extends `expansion_v1`. Before the existing vessel-choice stage, AI selects Avoid for each eligible scout with Observe via ordinary commands. It retains the existing one-scout replacement target and actual production costs, all species economies, crisis prepass, founder decisions and stale-knowledge rules. No scout spawns, bonuses, unseen threat scoring or new AI personality.

At vessel choice, first retain a permitted pending withdrawal/manual departure from an earlier accepted planning period; otherwise apply the withdrawal rule for current foreign presence. When not withdrawing/waiting, use the approved unknown-star or age-eligible resurvey ranking, filtering out excluded destinations before the existing own-scout destination deconfliction. If no eligible unknown target remains, try eligible resurvey candidates instead; if no candidates remain, wait. Under current knowledge rules, truly unknown stars carry no foreign presence fact and are therefore not excluded. Existing `resurvey_min_age` (core3) prevents immediate return to a now-stale vessel sighting's system; do not invent a permanent hidden-vessel memory. A scout may revisit once ordinary knowledge/age permits and then withdraw again. AI need not optimize around information it no longer has.

Rules7 expansion planning budget is at most2C+2V+1 accepted commands per AI empire: existing workforce/production/research allowance, plus at most one posture and one movement/founding/cancellation command per owned vessel. If an excluded pending move needs replacement, submit the replacement once rather than clear then set. Posture activation may itself cancel a pending move atomically. Existing separate crisis budget remains C+V+4. Rules4/5/6 retain their original budgets and algorithm behavior.

Human preparation emits at most one movement/cancellation command per eligible scout. Candidate enumeration is finite over its own view's stars/vessels/colonies, sorted stably; no repeated search until a route succeeds, population enumeration, background agent or wall-clock budget. Pure queries use the same preview/ranking rules as execution.

Keep the accepted economic/health/cargo/founding order. Proposed rules7 transaction:

1. Human Ready: existing crisis acknowledgement and private crisis/development preparation, then own scout preparation, mark Ready; resolve only if final. Direct Resolve runs the same scout preparation for each human on its private candidate once before AI planning; this must be idempotent with previously prepared moves/reports.
2. AI empires in ascending EmpireId: existing crisis prepass, ordinary development/expansion choices plus scout posture/withdrawal/filtered exploration. Use the same pre-resolution observation boundary, refreshed only for own accepted planning changes.
3. Capture the planned snapshot. Run existing economy/completions, crisis progression, departure/cargo transport, ordinary movement and founding; then crisis exposure/onset/finalization. Scouts stay outside disease transport as today.
4. Advance once; refresh final observations/contacts; record newly observed scout encounter outcomes and own order reports; clear readiness, validate and publish once.

A command rejection unexpectedly emitted by preparation, bound violation, overflow, report/state invariant failure or late final validation error aborts the whole enclosing transaction. Preserve input reference/bytes, all readiness, both existing RNGs, IDs, observations, crises, governors, orders and reports. No partial evacuation, contact or cleared receipt. Public failure messages remain generic where AI/private facts are involved. A known 'no retreat' wait is a valid explained outcome, not failure. Preview/report reads consume no IDs or RNG draws.

## 7. SCT-006: Content, state, compatibility and UI

State adds an explicit posture on every eligible rules7 scout, plus saved owner-only order outcomes/history and wait-deduplication facts. Ineligible cargo/founding vessels carry no scout metadata. Store no route coordinates, foreign private state, danger score, cached safety guarantee or hidden enemy list. Deep clone mutable records; validate role eligibility, enum/algorithm, owner/vessel/star references, turns, order consistency, sorted uniqueness and bounded history. Old report references can use immutable retired own-entity facts rather than requiring a vessel to remain live.

Posture semantics, ranking and fixed ten-turn history are rule logic. Existing JSON owns role capabilities, cost/speed and AI resurvey age; no new balance scalar or unrelated content framework is needed. The precise field names, report entity bounds and DTO decomposition are builder choices to freeze in a separately reviewed plan. Bounds must retain all current unresolved waits and all legitimate events within the ten-turn window, without truncating critical existing crisis facts. There is no new fleet grouping entity.

Proposed compatibility target: **rules7/writer8**, active only after full design and separate plan approval plus implementation/verification. Retain the existing22 explicit pairs: (1,1),(2,1),(3,1..2),(4,1..3),(5,1..4),(6,1..5),(7,1..6). Add exactly (8,1..7), giving29. No earlier writer can claim rules7. Writer8 saves rules1..6 using their own embedded data/behavior, omitting scout posture/report fields even when null/empty; reject legacy injection. Rules7 requires explicit valid scout state even under Observe; missing is not consent or a silent default repair.

Before changing product defaults/writer, capture genuine current rules6/writer7 campaigns and their unchanged-engine future states, including all four species, Solo/One, Observe-equivalent movement, plague Off/Standard, genuine crisis cargo/hold continuation and AI scout routes, with source/content/save/future hashes. Retain all older/passive-AI fixtures. Under equivalent settings and after stripping only new rules7/scout metadata, generation must preserve rules6 galaxy, appearance, entities, economic opening, homes and both PRNG states. No scout-feature draw is consumed. Observe human commands in Solo must retain equivalent rules6 economics/travel/observations/crises. AI Avoid is an explicit new rules7 behavior, so do not claim full rival future parity there. No old campaign auto-upgrades.

UI uses existing map, owned-vessel inspector/list, contacts, route preview and theme. Provide posture control with explicit movement authority and a simulation-supplied consequence preview, pending versus committed labels, fallback/wait explanation and grouped Attention. Founders never show these controls. Current/last-seen labels and privacy apply equally to maps, lists, inspector, previews, sorting and reports. The own scout's planned route may be shown; foreign routes may not. Use shared theme/container layout at800x600 and1920x1080; no new visual style or tactical screen. Display settings do not change saved orders or posture. Load/save/resize cannot prepare movement or advance time.

## 8. Literal acceptance examples

These are written fixtures, not executed gameplay evidence. Coordinates use the existing integer travel rule.

| Case | Required result |
| --- | --- |
| Default peaceful contact | Observe scout arrives at foreign-colony system at T+1: ordinary full survey/contact, no automatic departure, combat or loss. |
| Known destination excluded | Avoid scout at A orders B with a stale foreign colony: own-view rejection; prior permitted order/Ready unchanged. Switching Observe allows ordinary travel. |
| Unknown destination | Avoid scout orders unvisited B, duration2: no pre-arrival facts; T+1 transit cannot redirect; T+2 ordinary survey/contact; earliest withdrawal launch in T+2-to-T+3. |
| Retreat tie | Scout at(0,0), speed300; two eligible own-colony stars at(300,0),(0,300): both1 turn, lower StarId wins even if a nearer surveyed non-colony exists. |
| Travel cost | No eligible own colony; surveyed fallback at(301,0), speed300:2 turns, first advance on next resolution, no instant return or speed roll. |
| No fallback | Every other surveyed star has known foreign presence and remaining stars are unknown: wait with persistent Attention; manual unknown destination permitted; Observe can stay. |
| Stale fallback | A surveyed empty star last seen at3 is chosen at10: label stale/no known foreign presence; hidden foreign arrival changes neither preview nor order. Actual presence is discovered on arrival. |
| Simultaneous departures | Known foreign scout leaves while own scout withdraws: keep own committed route, no plan leak/reversal. Crossing transits cause no encounter. |
| Manual authority | Permitted manual pending departure survives automatic preparation. Changing to Observe keeps a prepared withdrawal until explicitly cleared/replaced. Transit posture change never alters ETA. |
| Shared star/crisis | Own colony plus foreign vessel still excludes star for Avoid scout; cargo vessels retain founding/quarantine/inspection behavior. Empty scout neither spreads plague nor opens a closed cargo port. |
| AI candidate fallback | No unknown stars remain; one resurvey of sufficient age has no recorded foreign presence while other surveyed candidates are excluded: select it under ordinary age/travel/StarId ranking. No allowed candidate means legal wait; no free replacement. |
| Projection/failure | Alter unseen foreign orders/cargo/colonies: same own preview/plan. Force late failure: exact input/readiness/RNG/IDs/orders/reports/receipts restored. |

## 9. Acceptance and review gates

SCT-AC-001..011 execution and M7-I are **Not Started**. SCT-AC-012/M7-D are **In Progress** for drafting, not Verified for owner approval. Parent REQ-017/17-row counts, M6 acceptance and all human/platform/release limits remain unchanged.

| ID | Required observable procedure before implementation acceptance |
| --- | --- |
| SCT-AC-001 Authority | New scouts Observe; explicit per-scout opt-in, role-capability eligibility/all species, manual precedence, toggle/cancellation/no-op/readiness and foreign/stale command failures. |
| SCT-AC-002 Knowledge | Current/stale colonies, current-only foreign ships, unknown-star legality, own/foreign shared star, identical projection/plan under unseen mutations; no hidden transit/health/identity disclosure. |
| SCT-AC-003 Withdrawal | Own-colony priority, surveyed fallback, duration/StarId ties, excluded stale facts, no fallback, permitted manual order, invalidated pending move and multi-scout destination sharing. |
| SCT-AC-004 Timing | Literal1/2-turn routes, no redirection/instant escape, full arrival survey/contact before response, simultaneous departure/crossing, newly completed scouts wait, no duplicated departure/arrival. |
| SCT-AC-005 Tradeoffs | Display actual elapsed travel and lost current observation; Observe peaceful co-location unchanged; no hull-loss roll, speed bonus, resource/cargo change or tactical battle. |
| SCT-AC-006 AI | All four species/core sizes under rules7; same-cost scout production; own-view posture/filtered exploration/withdrawal, age fallback/waits, exact budget, no founder/governor/crisis scope expansion. |
| SCT-AC-007 Transaction | Human non-final/final Ready and direct Resolve; no hidden observations during preparation; atomic rollback through every late phase; duplicate preparation/report purity; other human's accepted state retained. |
| SCT-AC-008 Reports/UI | Actual posture/move/Ready/save/load controls at800/1920; accurate timestamps and history/dedup/grouped Attention, no foreign-plan leak, no crisis receipt substitution, no per-vessel modal storm. |
| SCT-AC-009 Persistence | Posture/pending/committed/arrival/wait states and ten-turn history survive reload; deep clones/strict malformed-reference/enum/legacy-injection failures preserve session/file. |
| SCT-AC-010 Compatibility | Exact29-pair matrix; genuine rules6 and all older futures unchanged; rules7 generation/Observe Solo parity; existing AI and crisis PRNG isolation; no implicit upgrades. |
| SCT-AC-011 Integration | Real generated scout encounter/withdrawal and Observe comparison, save/load canonical future equality, all six retained graphical regressions, full simulation suite/ordinary build/harness exclusion. Clearly label forced rare fixtures. |
| SCT-AC-012 Owner design gate | Owner reviews/amends complete peaceful scope, Observe default/opt-in, exclusion/fallback/timing, information/tradeoffs, AI, compatibility and explicitly approves before separate implementation planning. |

## 10. Review handoff and later survival design

Builder chooses efficient pure-query/index representations, class/file boundaries, command/DTO names, report storage within the stated bounds, theme-based control arrangement and meaningful test organization. A separate plan must freeze concrete valid fixtures, late-failure hooks, report bounds and genuine rules6 captures before product changes; this draft contains no implementation task sequence.

If the owner wants actual danger now, amend scope **before approval**: choose a threat source, hostility authority, detection/disclosure timing, at least one meaningful warning/response opportunity, interception conditions, retreat destination and duration, pursuit limits, escape calculation and PRNG ownership/draw order, loss/damage/cargo consequences, known-odds explanation, AI symmetry, rollback and old-save policy. Those cannot be inferred from a neutral sighting or the owner's suggested factors. Warship hold-at-all-costs orders and combat-fleet doctrine remain separate material decisions.

Fresh read-only builder check returned "nothing important left"; [review/evidence](../../../reports/SCOUT_DESIGN_REVIEW_2026-10-25.md) records the scope and the subsequent clarification that unknown stars cannot have an existing foreign-presence exclusion under current knowledge rules. This establishes planning clarity only. Owner gameplay review is the next gate; full design approval would lead to a **separate implementation plan**, with retained Native preference afterward. No implementation, default/writer change, primary integration, push, release, cleanup or new gameplay acceptance is authorized or claimed by this draft.
