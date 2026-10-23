# Exploration, travel, and a second colony

**Status:** Approved by the owner on 2026-10-22; see [ADR-006](../../DECISIONS/ADR-006-approved-expansion-milestone.md). The rules, numeric fixtures and bounded policies below are approved design, not implemented functionality or verified balance. [ADR-005](../../DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md) retains the separate human-playtest deferral. The separate implementation plan is written and awaiting plan review/execution choice.

**Sources:** [Charter](../../CHARTER.md), [engineering rules](../../../CLAUDE.md), [economy contract](../../FIRST_PLAYABLE_ECONOMY.md), [spiral specification](2026-10-05-spiral-galaxy-design.md), [design guide](../../DESIGN_GUIDE.md), [accepted creative roster](../../SPECIES_ROSTER.md), and the existing validation plan's task 6.

## Intent and boundaries

Add a small expansion loop to the validated colony game: send a scout, discover worlds with different economic strengths, decide whether to invest at home or prepare a settlement expedition, travel there, found a second colony, and understand the consequences across both colonies.

Keep the existing C#/Godot presentation and headless simulation, JSON content, stable IDs, integer/PCG32 rules, shared command validation, snapshot economy, lossless reserves, and save failure atomicity. The starting colony remains population 6, workforce 2/2/2 on `verdant_world`; existing economic formulas and building/technology values remain unchanged. New expansion costs are explicit additions, not silent changes to support or growth.

The milestone demonstrates managing two colonies; it does not impose an arbitrary permanent two-colony cap. Additional settlements using the same rules may work, but large-empire automation is outside acceptance. No AI policy, combat, diplomacy, ship designer, fleet composition, supply network, terraforming, hidden deposits, anomalies, random events, or species modifiers are introduced. Civilizations in the accepted roster are not yet selectable/mechanically implemented; this prototype remains the ordinary baseline fixture. In particular, this milestone does not claim to implement Wenth fission or Sulan habitat requirements.

**Success:** two contrasting maps or goals lead to different defensible settlement choices, players can see travel/economic consequences before committing, and state/knowledge continues identically after save/load. Avoid hidden arithmetic, automatic colonization, free duplicated settlers, premature rival systems, and a universal unexplained planet score.

## Alternatives and approved direction

| Approach | Consequence | Approved disposition |
| --- | --- | --- |
| Hide stars and planets, discover routes incrementally | Adds sensor radius/frontier/path discovery before the second-colony loop can work | Defer |
| Public stellar chart, discover planetary details by visiting | Retains current map geometry while making scouting reveal meaningful choices | Approved for this milestone |
| Keep all planets known and invent survey-only bonuses/deposits | Requires a new resource/content system to make scouting useful | Defer |

The public chart exposes star ID/name/position and charted core/arm/rim region. It does **not** expose planet count, orbit, type, yields, colonization eligibility, ownership or entities at an unvisited system. Charted region already signals a possible mix of worlds through existing generation weights, not a guaranteed yield. This is the approved new-game policy; legacy games retain their old visibility behavior.

No separate deposit mechanic is needed: current planet types already create support/industry/science tradeoffs. A later discovery system may add more information after its own design review.

## Approved prototype content values

Values are approved arithmetic fixtures for this milestone, not verified final balance. They belong in new JSON vessel/expansion definitions, loaded and embedded like existing content. Display production costs in points while storing hundredths.

| Definition | Approved value |
| --- | --- |
| `scout` | Cost 800 (8 points); speed 300 map units/turn; no carried population |
| `settler` | Cost 2400 (24 points); speed 200 map units/turn; carries 2 workforce units |
| Starting exploration unit | One free scout at the home system in a new expansion game; no free settler |
| Embark requirement | At settler completion, source population >= 4 and at least 2 support workers |
| New settlement | The 2 carried units become population 2, workforce 2/0/0; no buildings, reserves, growth progress or active project |
| Colony capacity | Existing base capacity 10 plus existing building rules |
| Regions and travel | No regional time/cost modifier in this milestone; regions affect existing world-type generation only |
| Colonizable fixture worlds | All four current types; explicit content flag and positive support rate required |

Vessel definitions are repeatable production choices, not buildings: they never enter `CompletedBuildingIds` or grant economic building effects. Neither initial vessel role has a technology prerequisite. Production/research/ship costs continue using checked integer arithmetic and content validation. Default values do not imply biological population counts for future species.

## EXP-001: Empire knowledge and observations

Each expansion empire knows its starting system's planets and owns one scout there. All other systems initially show only the public stellar chart. Arrival reveals all planets in that system at once; there is no extra Survey button or survey turn.

Knowledge persists per empire in stable star/planet-ID order. Each surveyed system records its last observation turn and observations of planet orbit/type plus any colony/owner visible then. Static planet facts remain known. Dynamic ownership observations become **last seen**, never silently current, after the last friendly unit/colony leaves. Owning a colony or having a stationary friendly vessel provides current observation of the entire system; passing between systems provides no continuous sensor coverage. Last-observed turn is the published turn after resolution.

For synthetic multi-empire cases, a friendly stationary vessel/colony also reveals foreign vessels **currently stationary in that same system**, including ID, owner and role only. Never expose foreign cargo, pending orders, destination or transit. Foreign vessel sightings are not retained after observation ends; absence from a stale view means unknown, not destroyed. Owned vessels remain known throughout travel. This bounded projection clarification is included in the owner-approved specification; it does not introduce playable rivals or diplomacy.

Do not store localized text or redundant calculated yield totals in knowledge. Derive known type rates from the save's embedded content. Unknown is distinct from zero and from a known empty world. An unvisited system has no visible planet count, no empty-colony claim and no settlement suitability list. Region-based guesses must be labelled as general chart information.

Introduce one simulation-side empire knowledge projection used by the map, inspectors, lists, route previews, alerts and action availability. UI code must not query hidden planet/state facts directly. Reject unknown-target planet IDs before checking hidden existence/occupancy, with the same `TargetNotKnown` reason; foreign unit commands return a generic not-controlled result without exposing location. Public star targets are valid travel destinations even when their planets are unknown.

The full offline save remains trusted local game state with all empires' data. This design does not claim encryption or a secure multiplayer client/server boundary. Future AI must use permitted knowledge and the same commands; no AI is implemented now.

## EXP-002: Vessels, travel, and commands

Use a minimal plain-data vessel entity with stable ID, owner empire ID, role content ID, and either a stationary star or an active transit record. A settler also stores its carried population. This is sufficient movement state, not a full fleet or ship-design model.

At a stationary star, a move command sets/replaces a pending destination. Setting the same destination is a no-op; clearing a pending move is free before resolution. Changing an order clears issuer readiness through the existing planning path. Destination equal to current star is rejected as `AlreadyThere`. No travel occurs merely by clicking a destination, saving or loading.

**Approved restriction:** no redirection, stopping, or return order while in transit. Reject `VesselInTransit` and keep the route/state unchanged. A route preview warns that the launch commits until arrival. This deliberately avoids mid-route coordinates, fuel and rerouting in the first expansion slice.

Travel is direct between charted stars, without lanes, a fuel radius or obstacles. For displacement `(dx,dy)`, speed `s > 0`, duration is the smallest integer `n >= 1` satisfying `n*n*s*s >= dx*dx + dy*dy`. Use checked Int128 comparisons/integer search, never floating-point square roots. A 300-unit route takes one turn at speed 300; 301 takes two. A 300-unit route takes two at speed 200. Distinct stars cannot share coordinates under the current generator.

Resolution starts a pending route and advances its first turn in that same resolution. Duration 2 ordered in planning turn T arrives in published state T+2; duration 1 arrives at T+1. Persist origin/destination, total and remaining turns; do not consume PRNG draws or recalculate remaining travel from a rendered marker. An arrival clears transit and places the vessel at the destination. New vessels completed that resolution cannot depart until the next planning period.

Pending move validation checks issuing empire/turn, ownership, stationary state and the known public destination before changing anything. A valid non-null move or founding selection atomically replaces the vessel's other **unstarted** order; no vessel can hold both. Invalid replacement retains the previous order, state and readiness. Clearing an order clears only its own kind.

## EXP-003: Producing an expedition without changing old accounting

Extend production target identity to `(kind, content ID)`, with kinds `building` and `vessel`. Existing projects are `building`; IDs alone must not accidentally select a vessel of the same name. Keep the colony's one active project, unlimited reserve, lossless switching, cost conservation and at-most-one-completion-per-colony/turn. Vessel targets can repeat; building targets remain once per colony.

Completing a scout consumes its industrial cost and creates one owned scout at the colony's star. Completing a settler also moves 2 workforce units from the colony's support assignment into carried population; it is a transfer, not a newly created population. The source has population reduced by 2, support workers reduced by 2, and its existing growth fraction/reserves/buildings retained. The transferred workers already contributed to the turn's economic snapshot and require support at the source through that resolution; they affect the following snapshot only, like existing newly grown workers.

Use **snapshot** population/support for embark eligibility. Births during that resolution cannot make an ineligible settler complete early. The forecast must explicitly show population/workforce after transfer, both next-turn support demand and output changes, and the cost of delaying a building. At a 6-population 2/2/2 colony, completing a settler leaves 4 population and 0/2/2; warn that reassignment will be needed next planning period. Do not silently take workers from other jobs.

If the reserve covers the cost but snapshot population/support is insufficient, keep banking, retain the project and all population, and show `Awaiting embarkation: requires population 4 and 2 support workers`. Do not spend cost, duplicate a unit, fail the entire turn, or discard growth. Reallocating workers later can satisfy the requirement. Changing or clearing the project still preserves the industrial reserve. New colonies below population 4 cannot immediately reproduce settlement cargo.

All economic/embark effects must come from the same forecast/resolution result; presentation does not recompute them. A vessel completion report identifies the new entity and its source/workforce transfer. No recurring modal prompt is added merely because production banks with no target.

## EXP-004: Founding, ownership, and timing

The owner chooses a known, uncolonized planet in the same system as a stationary owned settler. Eligibility requires the content's colonizable flag, positive support output and capacity sufficient for the 2 carried workers. A scout cannot found, an in-transit settler cannot found, and no reserve is charged again at founding: industrial/population cost was paid at vessel completion.

The Found Colony command records intent during planning; no colony appears until resolution. A move before launch explicitly replaces that pending founding intent, and cancelling founding leaves the settler/cargo intact. Unknown/foreign/occupied/unsupported targets reject without mutation. Repeated identical valid intent is a no-op. Pending founding targets owned by the same empire are reserved against duplicate settlers; the second order rejects `TargetReserved` before resolution.

On successful resolution, allocate one new colony ID, consume exactly that settler/cargo, and publish population 2 assigned to support, capacity 10 under current base rules, zero reserves/growth, no project/buildings, ownership by the issuer. The new colony earns no output, growth or research in the resolution that creates it. It contributes from the next planning period's forecast and following turn snapshot.

Although the UI milestone has one human empire, synthetic multiple-empire conflicts must be defined. Resolve founding by empire ID then vessel ID. If a valid queued destination becomes occupied by an earlier founder, retain the losing settler/cargo, clear its founding order and report the now-visible conflict; do not charge, destroy it, or fail other valid actions. Units at that system observe final ownership after resolution. No military control or combat rule is implied.

## EXP-005: Resolution and integrity

Extend explicit turn phases while retaining the old economy's internal snapshot contract:

1. Validate planning state; capture economic, unit/order and knowledge inputs.
2. Resolve existing-colony support/output/growth/reserves/buildings/research, plus eligible vessel completion/embarkation from snapshot inputs. Existing economy examples retain their exact results when no expansion project is selected.
3. Start pending moves and advance transits of units present in the snapshot, in stable vessel-ID order; resolve arrivals. Newly completed units wait until next planning period.
4. Resolve founding orders that were eligible in the initial planning snapshot. A unit arriving this resolution cannot also found without a later planning command. Newly founded colonies wait for the following economic snapshot.
5. Refresh observations from final owned colonies/stationary vessels, construct reports, clear readiness, validate the entire candidate and publish turn+1 once.

An unexpected validation/arithmetic failure rolls back the entire resolution and triggering ready command, as today. The ordinary known founding-conflict outcome above is a handled result, not corruption. Use no random draws for travel, reveal or founding. Sort everything that could affect ID allocation or conflicts.

New invariants include valid role/owner/origin/destination IDs; positive speeds; stationary XOR transit; transit remaining in 1..total; no transit plus pending station order; scout cargo 0; settler cargo exactly its embedded definition; no lost/duplicated cargo on founding; knowledge restricted to valid public/surveyed IDs with observation turn <= current turn; valid production kind/ID; and no colony/workforce below existing economy limits. Validate aggregate potential output/capacity bounds with the larger legal colony set, not only one colony.

## EXP-006: Presentation contract

Retain the map and theme. Unknown systems show public chart data and a clear `Not surveyed` message, with no hidden counts/planet placeholders. Surveyed inspectors show discovered world types, per-worker rates and observation freshness. Do not invent a universal desirability number. The map lists only owned/observed units; transit indicators convey destination and remaining turns without implying intermediate exploration.

An owned vessel list supports selection, destination, duration breakdown and pending/committed status. Route preview shows known distance/speed/duration and the no-in-transit-redirection warning. Colony construction can choose buildings or vessels with explicit population/support requirements and the normal production forecast. When selecting a known planet with a stationary settler, a Found action previews ownership, population and when output begins.

Add a compact owned-colony list. Selecting a colony selects its exact star **and planet/colony ID**, so two colonies in the same system are distinguishable; `GameScreen` must stop choosing the first colony in a star. Workforce/project commands target the chosen colony. Empire research sums owned-colony outputs once, using existing ownership rules. Do not copy one colony's production or buildings into another.

Reports link arrivals/discoveries to their system, vessel completions to their unit/source, and new settlements to their colony. All views/tooltips/counts/sorts/action availability use the permitted knowledge projection. Current independent save/load/default-window/theme behavior remains regression scope. Layout work must include the known small-window readability concern; no mockup or new styling is approved by this text.

## EXP-007: Approved save and rules compatibility

New expansion games use schema 3 / rules version 2, embedding the new vessel/expansion definitions, production target kinds, unit/cargo/orders/transits and per-empire observations. Persist remaining travel and pending founding; loading neither advances nor resolves anything. Legacy/schema/invalid content failures still preserve the current session and existing save file.

**Approved policy:** continue loading schema 1/2 rules-1 games as legacy colony campaigns with their original positions, null regions where absent, complete visibility, economic rules and embedded content. They can continue/save in the newer application but do not gain scouts/expansion controls automatically. Label this mode clearly: `Legacy colony game - start a new game for exploration`. Save legacy campaigns as schema 3 retaining rules version 1 and legacy mode; accept that exact combination on reload.

| Read combination | Approved behavior |
| --- | --- |
| Schema 1 / rules 1 | Explicit legacy reader; preserve original layout and null metadata |
| Schema 2 / rules 1 | Explicit legacy reader; preserve saved spiral metadata/regions |
| Schema 3 / rules 1 | Continue already-resaved legacy colony campaign |
| Schema 3 / rules 2 | Continue expansion campaign with complete units/knowledge/orders |
| Other combinations | Reject with an explicit compatibility error |

This adds an explicit supported legacy-rules path; do not simply relabel an old state as rules 2 or start rejecting existing supported saves. The economy implementation can be shared where its contract is identical; legacy turns have no expansion phases or newly injected content. New commands must reject in legacy mode. Rules/schema validation remains explicit, not permissive fallback.

An **opt-in upgrade of an old campaign to exploration** is deferred. It would require a separate explicit decision on newly supplied expansion content, starter units, knowledge, and saved compatibility; silently merging current core files into an old embedded ruleset would violate the existing contract. The approved milestone preserves old-loop continuation without an upgrade-copy feature.

## Two worked strategic examples - hypothetical maps

These are exact arithmetic examples for approved rules, not claims about seed 42 or observed enjoyment. Home is `(0,0)`. The scout and eventual settler start there; map dimensions are irrelevant to route arithmetic. Each candidate contains an unoccupied colonizable world with the stated current type.

### A: A nearby forge versus a farther verdant world

- Forge at `(300,0)`: scout 1 turn, settler 2 turns. Two new support workers make 400 support against demand 200; base+surplus growth = `10 + min(40,200/10) = 30` hundredths, so a third worker appears after 4 supported turns. At population 3, allocation 2/1/0 supports everyone and produces 300/turn. The player can reach industry quickly in travel terms, but initially has no production until the third worker is assigned.
- Verdant at `(0,900)`: scout 3 turns, settler 5 turns. Initial support 600 against demand 200 gives growth 50 hundredths, so a third worker appears after 2 supported turns. At population 3, 1/2/0 exactly supports everyone and produces 400/turn; 2/1/0 instead grows faster and produces 200.

If the goal is nearer industrial presence, the forge's shorter trip is defensible. If the goal is rapid population growth and flexible labor, the farther verdant world may justify delayed arrival. Do not sum scout and settler durations as an automatic total: the player can prepare production during scouting, and may know one destination before another.

### B: The same types reverse their travel advantage

Move the verdant world to `(0,300)` and the forge to `(900,0)`. The verdant now takes scout 1 / settler 2 turns, while the forge takes scout 3 / settler 5. For early expansion, the nearby verdant is attractive for both timing and growth. A later industry-specialization goal can still prefer the forge's 300 output per production worker once population/support can sustain it; this is a changing goal, not a rule declaring forge always best.

Compare another discovery using existing types: crystal at `(300,300)` takes scout 2 / settler 3 turns, with 300 research per research worker versus forge's 200. A science goal can therefore change the preferred project and destination. Barren is colonizable under the approved rules but its initial balanced support yields only base growth 10; a later location need might justify it. The current no-opponent milestone cannot prove strategic value from military position and must not pretend otherwise.

## Approved acceptance criteria, not executed results

These stable EXP/EXP-AC IDs provide approved subsystem traceability within REQ-015. They do not claim any implementation or executed result; the top-level register continues to track the parent requirement.

| Criterion | Required evidence after implementation |
| --- | --- |
| EXP-AC-001 / EXP-001 | Two synthetic empires start with home-only planetary knowledge; every map/inspector/list/count/tooltip/preview hides unobserved facts. Unknown IDs return indistinguishable not-known failures; current versus last-seen observations remain distinct across save/load |
| EXP-AC-002 / EXP-002 | Distances at speed boundaries (300/301 at scout speed 300), duration 1/2, cancel/replace before launch, rejected mid-transit redirect, owner/stale-turn rejection, arrival timing and saved continuation all match exact integer results |
| EXP-AC-003 / EXP-003 | Vessel construction conserves industrial reserve, repeats legally, transfers exactly 2 support/population only once at eligible settler completion, stalls without spending when not eligible, and never grants a building effect or spends on selection |
| EXP-AC-004 / EXP-004 | Legal founding consumes one settler and creates one correctly owned colony at resolution; unknown/occupied/foreign/transit/invalid worlds and conflicting settlers preserve resources according to the stated rejection/conflict rules |
| EXP-AC-005 / EXP-005 | Newborn workers do not satisfy snapshot embark eligibility; newly completed units cannot move; arriving units cannot found immediately; new colonies contribute only next snapshot; overflow rolls back the whole turn and ready command; repeat orders/seed/save produce identical results |
| EXP-AC-006 / EXP-006 | New Game -> scout -> discover -> produce settler -> travel -> found -> manage two colonies -> save/reload -> continue works through Godot controls, with correct per-colony commands, empire research, selection, explanations and freshness |
| EXP-AC-007 / EXP-007 | Both existing legacy fixtures/campaign formats continue with unchanged economy/layout and clear mode; new schema-3 expansion round-trips orders/transit/knowledge; unsupported combinations reject; failed load/write leaves prior state/save intact |
| EXP-AC-008 / scope | All 195 existing simulation tests and retained relevant graphical regressions pass or are explicitly adapted with equivalent old-loop coverage; the deferred uncoached playtest is not mislabeled Verified |

## Approved choices and builder discretion

The owner approved the public-star/hidden-planets discovery model, committed travel, settlement preparation/population transfer, legacy-campaign policy and numeric fixtures under ADR-006. Do not reopen those choices as unanswered questions without new evidence or an explicit change request.

The builder may choose focused class/file decomposition, efficient integer search, collection/index/cache implementations and theme variation names while respecting these contracts. Do not redesign the architecture, invent fuel/AI/species mechanics, change current economic formulas, or alter compatibility policy. The separate implementation plan is now authorized; gameplay coding follows the workflow's plan review and execution setup.

## Builder check

A fresh read-only software-engineer review completed before owner approval on 2026-10-22. It identified four owner choices and one additional specification gap: foreign-vessel observations. EXP-001 was clarified before the owner approved the full document under ADR-006. The reviewer did not itself approve product choices or implementation. The separate [implementation plan](../plans/2026-10-22-exploration-and-colonization.md) is now written for plan review.
