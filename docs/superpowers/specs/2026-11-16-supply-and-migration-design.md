# Supply and migration gameplay proposal

2026-11-16. **Status: Civilian behavior approved; concrete civilian plan and remaining later-delivery details for review.** The owner authorized this specification after the [November 12 direction note](../../design-notes/2026-11-12-tactical-combat-and-supply-management.md). That direction selects civilian and fleet logistics, natural migration and deliberate bulk transfers. All formulas, capacities, priorities and staging below are **assistant-proposed defaults**, not approved gameplay. Implementation and functional acceptance are Not Started. This document is a gameplay specification, not an implementation task plan or a new numbered milestone.

## Civilian approval and plan — 2026-11-16

The owner approved the summarized civilian behavior: policy-driven routine deliveries, explicit bulk transfers, population/capacity/habitat/quarantine limits and Wenth daughter-seed identity. [ADR-031](../../DECISIONS/ADR-031-approved-civilian-logistics-direction.md) records that bounded approval. The [civilian implementation plan](../plans/2026-11-16-civilian-logistics-and-migration.md) now supplies concrete integration choices for review. First delivery is provisions/transports/migration; development goods and fleet endurance remain later deliveries, and interception needs a separate design. This approval does not establish explicit owner confirmation of every numerical default or authorize code execution. The plan makes price units, opt-in setup and future rules/schema choices explicit.

## Resource direction — owner follow-up, 2026-11-16

When asked whether to retain development goods or begin with provisions/population transport alone, the owner replied: "Either is fine we'll end up doing them all anyway". The owner delegated that resource-selection choice. The assistant retains all three resource categories in the design: provisions, development goods and fleet supplies. The recommended civilian delivery starts with provisions/transport/migration, then adds development goods once that network is usable; fleet endurance follows under its own boundary. This records resource breadth and the assistant's staging recommendation, not approval of numerical defaults, the growth bonus, the full specification or implementation.

## Intent

Make location, specialization and transport capacity matter. A productive core can sustain a distant colony or expedition, but doing so commits ships, stock and time. Players choose priorities and acceptable risks; routine deliveries and natural migration follow those policies. Manual bulk transfers remain available for a deliberate change of strategy.

OpenAntares is an original space strategy game. Judge these mechanics by its own meaningful-choice, transparency, distinct-empires and depth-without-busywork pillars. No external game's resource model, interface or combat rules are a requirement.

Success means a player can explain a shortage, identify a useful response and see the cost of diverting transport to another purpose. A large empire must remain playable through policies and exception reports. Adding a mandatory shipment click every turn would be a failure.

## Baseline and boundaries

The inspected checkout is `F:/Projects-PCGames/OpenAntares`, branch `feature/planet-globes-modern`, baseline `e90b6b2`. Local main points to `d994d34`; the November 15 shader changes are on the feature branch. This proposal changes neither branch's gameplay.

- [EconomyCalculator](../../../src/OpenAntares.Simulation/Economy/EconomyCalculator.cs) currently calculates local support, shortage-scaled production/research and growth; support has no general stock or shipping network.
- [Species definitions](../../../content/core/species.json) establish different support requirements, habitats and growth. Wenth population means functional lobes of one colony body, not independent migrants.
- [CrisisTransport](../../../src/OpenAntares.Simulation/Crises/CrisisTransport.cs) already carries disease/inspection semantics for population-bearing vessels. General logistics must integrate with that model rather than creating a quarantine bypass.
- [Conflict design](2026-10-31-empire-conflict-design.md) protects civilians and applies an abstract production blockade. Current combat has no ordinary accuracy/evasion roll or tactical battlefield.
- Current simulation rules are 12; save writer schema is 13. New version identifiers and compatibility pairs must be allocated only in a later approved implementation plan. Existing rules retain their exact behavior.

## Approaches and proposed choice

| Approach | Benefit | Cost |
| --- | --- | --- |
| Empire-wide abstract supply pool | Few controls and little route bookkeeping | Distance, transport design and blockade running have little meaning |
| Manual order for every shipment | Maximum direct control | Repetitive work grows with every colony and fleet |
| Physical transports with policy-driven dispatch and optional manual missions | Location and scarce capacity matter; routine work can be automated | Requires visible reservations, manifests and good exception reporting |

The assistant recommends the third approach. It follows the owner's selected logistics/migration direction while leaving tactical interception for a separate specification. The owner has authorized drafting, not selected every default in this proposal.

## Proposed delivery boundaries

These are gameplay dependency boundaries, not approved implementation tasks or dates.

1. **Civilian network and migration:** establish owned-colony routes, provisions, a reusable civilian transport, automatic supply and natural migration, explicit bulk transfers, quarantine and persistence first. Add optional development goods once the network is usable. Both are retained in civilian design scope; the eventual implementation plan will separate those increments. No new permission to attack civilians.
2. **Fleet endurance:** fleet stores and replenishment using the same capacity/accounting model. Deliver only after the civilian model is approved and demonstrated understandable. No tactical battlefield is required.
3. **Interception and blockade runners:** separate design for detection, evasion, interception, cargo outcomes and legal targets. Then consider tactical maps, terrain, maneuvering equipment and optional manual battles. Do not make protected transports destructible as an incidental logistics change.

Each boundary requires its own approved scope and implementation plan. The full proposal preserves the eventual connection between them without authorizing all three at once. Peaceful logistics must be useful without warfare; this does not implement the separately deferred FR-001 peaceful mode.

## LOG-001: Resource units, production and storage

Resource categories retained through delegated judgment; units, purposes and quantities below remain proposed defaults:

| Resource | Purpose | Unit and proposed initial limit |
| --- | --- | --- |
| Provisions | Cover a colony's unmet population support | Existing integer support points; 100 points occupy one cargo slot; colony storage 2,000 points |
| Development goods | Optional civilian investment that accelerates ordinary growth | Whole crates; one crate occupies one slot; storage 20 crates |
| Fleet supplies | Endurance and replenishment in the second boundary | Whole crates; one crate occupies one slot; colony storage 20 crates |

Fleet-supply production, storage and consumption activate only in the second boundary. The civilian boundary must not expose a resource or project that has no delivered use.

Provisions represent species-appropriate support, not universal literal food. Existing per-species support demand remains authoritative: at current data rates 100 points support one Harl population unit for one boundary, while a Peldra unit needs 200. No new physical-person conversion is implied.

Local support production first covers local current demand. Consume stored provisions only for the remaining deficit. Calculate shortage-scaled output with local production plus provisions actually consumed. Stored provisions do not produce surplus growth. From any newly produced surplus, fill the policy's stock target first, up to free storage; only the remainder supplies existing surplus-growth calculations. Show this tradeoff in the forecast. At capacity, unused output remains explicitly unused; no silent overflow loss of existing stored stock occurs.

Default provision target is zero, preserving a locally self-sufficient opening until the player enables exports or a reserve policy. An enabled reserve target defaults to two boundaries of current support demand, capped by storage. A larger species demand may exceed capacity; show the achievable endurance rather than promising two turns.

Development goods and fleet supplies are proposed new production-project outputs, each costing 100 existing production points per crate. They use the ordinary finite colony queue, prerequisites and completion path; they never run as free background output. Produce at most one completion per colony per boundary, preserve excess production reserve and do not charge or duplicate another project. Automatic replenishment may repeat these projects only when the player explicitly allows that production policy; it cannot displace manual active work.

At acceptance reserve one storage slot for a queued crate completion, ahead of later incoming-cargo reservations. A completion without its legal reserved slot stalls without payment or output; retain the project and production reserve and explain the capacity conflict. It cannot skip to a second completion. Import reservations, produced goods and actual stock all share the same storage bound. Cancelling a project releases its slot.

Development goods are optional: consume at most one crate per colony per boundary for up to 10 extra growth progress, limited to the unused portion of that species' existing maximum surplus-growth allowance. Consume only when fully supported, below capacity and the full proposed bonus is usable without a capacity discard. Otherwise retain the crate. Goods cannot cure a crisis or override its existing growth restrictions. They do not grant population immediately, count as provisions or bypass Wenth daughter-seed rules. This is a proposed civilian-goods use for review, not an already requested numerical bonus.

Storage, costs and bonus amounts belong in validated content with nonnegative bounded integer values. No spoilage, credits, fuel commodity, per-weapon ammunition, player trading market or mixed-species diet economy in the first boundary.

## LOG-002: Transport and manifests

Propose one reusable unarmed civilian transport recipe, available through ordinary construction from the start of new logistics campaigns: cost 400 production points, capacity eight cargo slots, base travel speed equal to the current scout recipe before ordinary damage/fleet modifiers. The later implementation plan must assign its content ID and check the actual starter opening; there is no free starting hull or invisible empire freight pool.

One whole migrant population unit occupies two slots. Slot use is `ceil(provisionPoints / 100) + developmentCrates + fleetSupplyCrates + 2 * passengers`; calculate each resource's aggregate once per manifest. Partial provision loads are legal, with exact integer point accounting. Passenger identity, origin and destination are retained separately from goods. Unoccupied capacity produces nothing.

An automated route assignment reserves its nominated hull for repeated origin-to-destination service: after unloading, return empty to the same origin on the next legal movement boundary, then seek its next load. There is no automatic search for a different origin. A player can release the assignment or redirect it through the normal command path. A blocked/unknown return route parks the vessel at the current legal port and reports Attention; it never teleports or uses hidden route facts. Manually assigned missions complete at destination and leave the ship idle unless an explicit return was part of the accepted order. Passenger missions can use the same opted-in route loop.

Use current interstellar movement costs, lanes/route legality, knowledge and committed-travel rules. A loaded civilian transport cannot gain permission to fight, found a colony or exchange passengers with a settler. Existing settlers and Wenth daughter seeds keep their current purpose and cost. No cargo mass speed modifier in this first proposal.

Each mission records a stable ID, owner, vessel, origin, destination, manifest, reservations and state: planned, loaded, in transit, waiting at port, completed or cancelled. One vessel owns at most one mission. Manifest mutations and inventory changes are atomic commands. Rejected commands do not change stocks, readiness, passenger counts or reservations.

## LOG-003: Routes, reservations and automation

Only existing colonies of the issuing empire may be endpoints in the first boundary. Both endpoints must be legally usable for the manifest, and the route must use permitted knowledge. No foreign, allied, newly founded empty-planet or mid-flight rendezvous endpoints. The player may choose a route and policy; automation cannot invent a route through unknown space.

Policies specify stock minimum/target, export permission, priority, eligible vessels, passenger permission and acceptable route risk. All begin disabled. Enabling a supply policy does not silently enable natural migration or goods production. Default enabled route behavior refuses a known effective hostile blockade; an explicit manual delivery may proceed under current protected-civilian rules with a truthful warning. Unknown threats are not silently treated as known risks.

Dispatch priority is emergency provisions for a forecast support deficit, normal provisions below reserve target, explicitly reserved bulk transfers, then optional development goods and natural migration. In the fleet boundary, fleets marked Urgent follow emergency civilian provisions and precede ordinary reserves; other fleet requests follow ordinary reserves. The player can reorder the non-emergency classes and reserve specific hulls; emergency priority never commandeers a manually assigned or passenger-only vessel.

Within a class order by player priority, oldest unfilled request turn, destination ID, origin ID, vessel ID, then request ID. Export only stock exceeding the origin minimum; do not reserve future production. Reserve destination storage or population capacity at acceptance, preventing two missions from promising the same space. Reservations grant no cargo or population and are released atomically on cancellation. Existing queued founding/population commitments take precedence; migration cannot spend already committed population.

Origin reservations exclude competing mission loads; they do not prevent the local colony from consuming provisions for support or suffering ordinary health losses. Before loading an accepted mission, revalidate against the post-economy/health state. If its full manual manifest is no longer feasible, move it to waiting at origin, release its stock/passenger/destination reservations and require a new preview before recommitment; keep the vessel assigned, with an actionable notice. Automatic requests release stale reservations and retry a feasible new manifest at the next boundary, without moving cargo this boundary. There is no indefinite reservation of lost population or consumed goods.

For automatic deliveries choose the shortest legal known route, breaking equal route costs by stable star-ID sequence. Refresh permitted route risk and endpoint legality before loading. A changed route never uses undiscovered enemies as a private dispatch signal.

## LOG-004: Loading, travel, unloading and cancellation

Goods leave origin stock only when loaded. Passengers leave origin population only when loaded; planning reserves availability without changing the workforce. Loading happens after this boundary's economy/health phases, so departing people contribute to that economy and are then removed from support workforce first, production next, research last. Show the resulting next-turn forecast before acceptance. Reject a manual order if its full passenger count cannot depart legally; automatic requests load only the fully revalidated feasible whole-unit amount.

Loaded cargo belongs solely to its vessel manifest. It is never simultaneously available to the colony. Unloading transfers the exact available quantity to destination stock or population and reduces the manifest by the same amount. Partial goods unloading is allowed; passengers disembark only as whole units into reserved legal capacity. Arriving people enter support workforce and affect economy starting with the next boundary.

One transfer operation (loading or unloading) per vessel per boundary. Newly loaded ships begin ordinary movement on the following boundary; vessels arriving this boundary can unload at its end but cannot immediately reload or depart. Travel cannot become an instant relay of a resource across multiple stars.

Cancellation before loading releases reservations. After loading, cancel means stop automatic dispatch at the next legal port and offer return/reroute; it never refunds cargo into a distant origin. Return uses ordinary travel and rechecks origin capacity. If an endpoint becomes illegal, quarantined or full, hold the manifest on the vessel and report the reason. Do not discard passengers, create an overcrowded colony, silently found a new colony or teleport stocks.

Passengers are in protected transit with included life support in the first boundary; no attrition or civilian destruction is added. Cargo remains accounted for on a stranded ship. Migration cannot remove the last population unit. No colony abandonment or forced evacuation in this first specification.

## LOG-005: Natural migration and explicit bulk transfer

Natural migration is opt-in per origin and destination. It operates only among same-empire, same-species ordinary colonies with compatible existing habitat. Mixed-species colonies, citizenship, conquest and cross-empire migration need another design. Starting origin minimum is four population units; the player can set a higher floor. Explicit bulk transfer may use a lower selected floor of at least one, with a preview and its full finite passenger count.

Proposed automatic driver: an origin is at least 80% full, has more population than its protected minimum and is not forecast short of support. Destination is below 60% capacity, accepts migrants and can support the arriving population from its next-turn forecast including legitimately reserved provisions. For positive capacity `C`, compare `100 * population` with `80 * C` or `60 * C` using checked wide integer arithmetic. Do not use a hidden desirability score.

Select at most one whole population unit per origin per boundary. Choose the destination with greatest unreserved free capacity, breaking ties by shortest legal route then destination ID; ordinary mission priority then selects a vessel. Publish a specific reason if no move is possible. Both source and destination retain their exact species identity and existing settlement/habitat eligibility.

For every automatic or manual passenger proposal, preview support after the specified workforce removal at origin, using the remaining population and stock after all higher-priority reservations. Automatic migration refuses a resulting origin deficit; an explicit bulk transfer may accept that disclosed deficit with its own confirmation. Destination support is tested with current population plus all reserved arrivals including this mission, and its currently known workforce/support plan plus legitimately reserved provisions. Test both at acceptance and loading-time revalidation. No forecast assumes future births, production or uncommitted deliveries are guaranteed before arrival; changing plans can still create a later shortfall, which must be warned about without erasing passengers.

Explicit bulk transfers do not require the 80/60% thresholds; they do require the origin floor, destination reservation, permitted support and transport capacity. Automatic migration cannot use reserved bulk-transfer people or destination space. On bulk-transfer acceptance, pause automatic migration out of its destination until the player resumes it; the preview names this policy change. This prevents immediate automatic reversal without introducing hidden passenger cooldowns.

Wenth lobes cannot participate in ordinary passenger migration in either mode: each colony is one living body. Wenth use existing daughter-seed founding and benefit from supply/development deliveries. A distinct inter-body transfer mechanic would need a later species design; labeling this unavailable must explain why. This exception is an assistant proposal to preserve current Wenth identity, and is highlighted for owner review.

## LOG-006: Health, protection and blockade interaction

Quarantine, inspections, illness and source-body rules take precedence over loading and unloading. Capture source health/episode identity with passengers, and feed arrival exposures into the existing crisis system exactly once. A mixed goods/passenger manifest follows passenger health restrictions; goods-only cargo creates no new disease vector in the first boundary. Refusal to unload releases no passenger identity or hidden rival health information. No goods-delivery action cures a colony or clears a carrier.

Until the separate interception specification is approved, retain the existing abstract blockade production penalty exactly. Do not also halve cargo or provisions. Protected manual civilian deliveries can arrive without removing the production blockade; this is not the future blockade-runner mechanic. Automatic routes decline a known effective blockade by default, with an actionable policy explanation. Supplies do not make neutral civilian ships attackable, and arming a hull must not silently opt its passengers into combat.

## LOG-007: Fleet endurance (second boundary proposal)

Each armed vessel has an individual supply store; proposed capacity 12 crates and demand one crate per resolved boundary, including idle time and transit. Civilian transports use the included support rule above; they are not charged a second fleet-supply demand. An armed vessel built under new fleet-logistics rules begins empty and can replenish only from actual colony stock. Its commissioning preview must disclose stock availability; no free recurring construction supplies are minted.

At a friendly owned port, automatic replenishment can transfer existing colony fleet-supply crates up to the target before that boundary's consumption, obeying colony export floors and deterministic vessel-ID order. At a remote fleet, transfer requires a stationary transport at the same star, explicit receiver capacity and a valid mission. Replenishment consumes the transport's one transfer operation. No in-transit rendezvous or remote repair teleportation.

Consume up to one available crate, then mark the vessel supplied for this boundary. If none is available, proposed consequences are: half ordinary effective firepower rounded down with existing armed-shot minimum retained, no initiation of a new attack or effective blockade, and no repair progress. Defense, withdrawal and travel remain legal; speed, hull and population are unchanged. Existing hull/cohesion modifiers apply once before the supply firepower factor. Voluntarily losing supply never resets an encounter, heals a ship or removes a civilian protection.

Attack-command acceptance conservatively requires at least one crate already on every participating armed ship; a promised port allocation cannot satisfy this planning check. An empty ship must actually replenish and give its attack order in a later planning phase. At resolution the frozen supplied-for-this-boundary flag governs activation and existing blockade effectiveness: consuming the last crate still counts as supplied this boundary. Remote delivery after consumption can enable only a following boundary. A failed supply revalidation cancels a new attack privately into Hold, without reserving or changing the foreign target; it never disables necessary defensive response.

Show each ship's store and the fleet's earliest shortage, not a misleading pooled average. Shared colony stock belongs to neither fleet until actually transferred. Fleet supplies cannot pay refit costs or accelerate existing repair. These rules need direct combat/repair/port acceptance when that second boundary is approved; civilian-network acceptance alone cannot establish them.

## LOG-008: Deterministic turn integration and accounting

Retain atomic candidate-state publication in [TurnResolver](../../../src/OpenAntares.Simulation/Turns/TurnResolver.cs). Proposed new-rules integration, without reordering old-rules execution:

1. Validate commands and prepare automation through the ordinary empire command authority. Freeze priorities, stock, missions, reservations and permitted observations.
2. For the fleet boundary, allocate stock at existing eligible ports, then freeze supply availability/consumption and combat effects. No future arrival or newly made crate is counted as present.
3. Calculate economy using current population, local support, existing stock actually consumed, reserve fill and eligible development goods. Apply existing growth, production/research completion, health, diplomacy and conflict rules with the explicitly specified logistics factors.
4. Resolve current exchanges/withdrawals and ordinary movement/founding in their existing relative order. Newly completed transports do not load or move until a later boundary. Newly produced goods become stock but cannot be consumed or loaded this boundary.
5. Revalidate and perform one eligible transfer per pre-existing vessel, unloading arrivals before considering loading at origins. Respect the health result of this boundary and existing founding commitments. Newly loaded missions cannot move until the next boundary.
6. Reconcile inventories, migrants, reservations, reports, knowledge and next-turn forecasts. Validate the entire candidate; publish once. Failure returns the untouched original state, PRNG and readiness.

Use integer/fixed-point quantities, explicit overflow diagnostics, stable ordering and the saved project PRNG only if a later approved extension actually introduces a random outcome. Initial logistics introduces no random rolls. Do not consume RNG merely to choose equal-priority requests.

For each resource, `opening stocks + opening manifests + produced = closing stocks + closing manifests + consumed`; reservations appear in neither side. For ordinary migrants, births and existing crisis losses are accounted separately: loading/unloading alone must conserve population across colonies and manifests. Growth progress stays with its colony; transferring whole people cannot move or multiply fractional birth progress. Failed/rerolled transfers cannot duplicate production or cancel recorded losses.

## LOG-009: State, compatibility, commands and AI

New authoritative state comprises per-colony inventories and policies, per-vessel manifests and supply stores, mission/reservation IDs, passenger origin/species/health identity and player-requested routes. Saves embed the effective resource definitions/parameters. Reports are not an alternate source of current inventory. Never derive passengers from a UI cache.

Proposed command intents are configure policy, assign/cancel mission, reserve/commit bulk transfer and choose a legal reroute. Each validates issuer ownership, published revision/token, stock, population, habitat, destination capacity, quarantine and vessel commitment. Preview tokens must not disclose foreign orders or hidden threats. Automatic and rival logistics use the same commands and permitted observations; no free AI goods, passengers or transport capacity.

Loading an old save preserves its historical local support, protection, production, movement and RNG behavior. Do not backfill new stock or enable new consumption into existing rules 1-12. No automatic rules conversion is proposed. New-game defaults and any optional logistics setup switch require explicit approval in the later plan, along with new rules/writer identifiers, exact supported pairs and genuine old-save continuation evidence. This document allocates none of those identifiers.

## LOG-010: Player information and control

Use the existing map, colony, fleet and Decisions/Turn Report surfaces. A colony summary should read, for example: `Support needed 600; local 500; reserve used 100; reserve after 200; incoming 300 in 2 turns`. Incoming is not counted as already available. Include capacity, reserved capacity, origin export floor and the limiting cause of a stalled request.

A bulk-transfer preview names people/species, origin population/workforce after departure, target after arrival, transport/slots, earliest arrival, known restrictions and paused outward migration. Confirming commits a finite mission, not an indefinite policy. A route card names origin, destination, purpose, priority and status; selecting a warning navigates to the affected choice.

Separate certain current costs from conditional arrivals and unknown future threats. Aggregate repeated shortages by colony/route without hiding the first blocked delivery or a material change. Ordinary successful deliveries are available in detail without a modal each turn. Wenth controls explain the body limitation instead of showing a generic disabled button.

## Proposed acceptance criteria

These describe required evidence after implementation; none has run. Civilian design behavior is Verified for the approval scope recorded in ADR-031. LOG-D remains In Progress for the remaining broader defaults; LOG-P is In Progress for civilian plan review; LOG-I is Not Started. Functional LOG-AC-001..010 are Not Started; LOG-AC-011 is In Progress for design review only.

| ID | Required evidence |
| --- | --- |
| LOG-AC-001 | Exact support/stock/growth arithmetic, storage limits, overflow handling, species demand, optional goods caps and reserve-versus-growth tradeoff |
| LOG-AC-002 | Paid construction, finite manifests, one-transfer clock, committed travel, partial goods unload and exact goods conservation |
| LOG-AC-003 | Deterministic priority, origin floors, capacity reservations, no commandeering and no promised future goods |
| LOG-AC-004 | Automatic thresholds, finite bulk transfer, workforce timing, population conservation, Wenth exclusion and habitat checks |
| LOG-AC-005 | Quarantine/exposure, blocked arrivals, cancellation/return, full ports and surviving passenger identity |
| LOG-AC-006 | Protected civilian deliveries and existing blockade economics without a second penalty or new attack permission |
| LOG-AC-007 | Later fleet stock/consumption, finite replenishment, zero-supply combat/repair effects and no retroactive arrival benefit |
| LOG-AC-008 | Command rollback, stale preview rejection, fair AI knowledge, stable ordering and identical saved continuation |
| LOG-AC-009 | Genuine historical save futures unchanged, new save validation and explicit version/compatibility matrix |
| LOG-AC-010 | Affected native controls at 800x600/1920x1080 plus owner comprehension of a deficit, reroute, migration and Wenth explanation |
| LOG-AC-011 | Owner review of the full proposed scope/defaults, especially goods purpose, migration rules and civilian protection |

For the first boundary, LOG-AC-007 is explicitly outside delivered scope, not a passing fleet criterion. Use only directly affected simulation cases and native views through the shared native queue. A full suite needs the owner's explicit request; never reuse old planet/UI counts as fresh logistics evidence. Manual journey details belong in the eventual approved implementation handoff, after independent authorized work is exhausted.

## Owner review points and next step

All three resource categories are retained following the owner's delegated choice. The most consequential remaining proposed choices are the optional growth use for development goods; finite policy-driven physical transports; opt-in same-species migration with protected floors and Wenth exclusion; and retaining civilian protection until a separate interception design. Fleet consumption/penalties are a later boundary of this proposal, not a prerequisite to drafting civilian logistics.

Review the separate civilian implementation plan next, including explicit price units, opt-in setup, save versions, content IDs and source-scoped validation. Later development-goods and fleet/interception choices remain for their own review. No gameplay code, commit, merge, push, release or acceptance closure is authorized by this document.
