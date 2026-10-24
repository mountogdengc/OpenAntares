# Playable species: economy, habitats and founding

**Status: Owner-approved design and implementation plan, 2026-10-23; implemented candidate undergoing final independent review.**

REQ-016 / design milestone M3-D. ADR-008 records design approval; the owner subsequently approved [the plan](../plans/2026-10-23-species-mechanics.md) with “yes” and the retained Native preference. [Implementation validation](../../../reports/SPECIES_VALIDATION_2026-10-23.md) and [review/handoff](../../../reports/SPECIES_FINAL_REVIEW_2026-10-23.md) supply actual evidence and limits. The original [roster](../../SPECIES_ROSTER.md), including the owner's uncommitted appearance edits, remains unchanged. Numbers below are accepted reproducible rules, not verified balance.

Sources: [charter](../../CHARTER.md), [engineering constraints](../../../CLAUDE.md), [design guide](../../DESIGN_GUIDE.md), [economy contract](../../FIRST_PLAYABLE_ECONOMY.md), [expansion contract](2026-10-22-exploration-and-colonization-design.md) and [integrated baseline](../../../reports/EXPANSION_INTEGRATION_2026-10-23.md). Existing approved behavior governs rules-1/2 campaigns. This approved extension applies to rules-3 campaigns under the separately approved implementation plan.

## 1. Intent and scope

The owner's creative brief is a love letter to Master of Orion II and classic space 4X: colorful humanoid animals, monsters and broad alien archetypes are welcome even with fanciful biology. Prioritize memorable personality and enjoyable play while respecting real peoples, cultures, religions and ethnic groups. Scientific plausibility and a contemporary representational checklist are not creative requirements.

Make species choice change labor allocation, home development and preparation of the next settlement. Each species must have a viable opening and a visible opportunity cost. Use the existing scout/discovery/production/travel/founding/two-colony/save loop.

Scope: four selectable species, species-aware economic forecasts and settlement eligibility, one small surveyed environment classification, and persistence. Preserve C#/Godot separation, JSON content, stable IDs, integer/fixed-point rules, PCG32, ordered iteration, snapshot phases, atomic publication and embedded saves. Ordinary resource conservation, committed travel and existing building/technology effects and costs remain unless explicitly extended below.

Exclude rival AI, diplomacy, combat, mortality/age simulation, individual skills, archive objects, mixed-species colonies, migration routes, transport unloading, global food, full climate simulation, terraforming, custom civilizations, new portraits and new art tooling. Harl archives and cultures remain fiction. A Wenth speaking-limb never becomes a population token or leader.

Success: at least two sensible allocations or expansion choices differ by species; all four complete the expansion loop; every changed requirement/output/limit is inspectable. Exact examples establish reproducibility, not final balance or enjoyment.

## 2. Approach and creative amendments

Considered (A) small statistical bonuses, insufficiently distinct; (B) full life-cycle/climate/organism simulations, too broad; (C) bounded asymmetry in the current colony/founding loop, selected by the owner.

| Topic | Starting roster | Approved disposition |
| --- | --- | --- |
| Names/bodies | Peldra, Harl, shark-like Sulan, single-organism Wenth; animal-inspired silhouettes welcome | Keep. No appearance reversal or new species is needed |
| Population scale | Each unit is one person for three species | Keep a familiar population statistic; its literal headcount is unspecified. Use lobes as Wenth flavor. No mandatory cohort model or reproductive explanation is needed |
| Peldra opening | Ordinary 2/2/2 is underfed | Keep high support needs but begin at healthy 4/1/1. Learning should not require fixing a knowingly broken default |
| Harl | Avoid surplus because population fills the cap | Reframe as rapid recovery and inexpensive small founding parties. Growth offers opportunities and capacity/investment tradeoffs. Keep short lives/school archives as culture, without death or memory-loss rules |
| Sulan | Detailed water/shelf/salinity and a project making a sea | Replace with surveyed categories and a costlier harbor-equipped settler. It makes a colony-scale enclosed habitat, not a planet-wide ocean. Native shelves hold 12 units; sealed habitats 8, making depth versus reach a choice |
| Wenth | Smaller parent/daughter after fission, low coordination ceiling | Keep; require a mature body at current capacity before seed completion. Each colony is an autonomous body, the empire a kinship rather than one galaxy-wide biological mind |
| Culture | Separable from biology | Keep estates, schools, harbor houses and seed kinship as descriptive labels, without culture modifiers/customization |

The owner approved these amendments on 2026-10-23. Reconcile affected economy/expansion contracts through explicit rules-3 amendments during planned implementation; preserve the owner's local roster edits and historical text. Approval is recorded in ADR-008 rather than silently rewriting the roster.

## SPC-001. Setup and identity

Future New Game exposes four species with their actual tradeoffs, starting allocation, effective rates, growth and settlement options. Harl is the approved visibly preselected first-game choice; any species remains selectable.

Rules 3 requires an explicit valid species_id on each empire, fixed throughout that campaign. Missing/unknown IDs reject before publication. Loaded rules-1/2 games get no invented species. Colonies and vessels derive biology from immutable owner identity; no second population pool or cargo-species field is needed while capture/trade/mixed populations do not exist.

Keep one human empire, one starting colony on verdant_world, one free scout, turn 0, no buildings, zero growth/reserves and existing research choices. Species changes the starting population/workforce/capacity below. IDs and galaxy generation are independent of species selection. Culture text has no mathematical effect.

## SPC-002. Exact economy and core profiles

Economic quantities use hundredths: 100 = one displayed support/production/research point. Growth 100 = one population unit. Cost tables explicitly use displayed points. Floor nonnegative divisions; use checked integers and Int128 intermediates. Species economy consumes no RNG.

| Species | Starting population; support/production/research | Base capacity | Required support per unit | Production/research worker factor | Base growth | Surplus-growth cap | Support hundredths per bonus growth hundredth |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Peldra (peldra) | 6; 4/1/1 | 10 | 200 | 30000 basis points (3x) | 5 | 15 | 20 |
| Harl (harl) | 6; 2/2/2 | 10 | 100 | 10000 (1x) | 10 | 80 | 5 |
| Sulan (sulan) | 6; 2/2/2 | 12 native / 8 sealed | 100 | 10000 (1x) | 10 | 40 | 10 |
| Wenth (wenth) | 4; 2/1/1 | 6 | 100 | 20000 (2x) | 10 | 40 | 10 |

Production/research factors are separate content fields, equal in the core profiles. Support-worker rates remain ordinary planet rates. Existing planet rates and building/technology costs are unchanged. Do not alter the old rules/starting_colony fixture: rules 3 resolves new parameters from species definitions; rules 1/2 keeps its embedded global rules.

For snapshot population N and S/P/R workers:

1. Support produced = S * planet support rate + completed flat support effects. Required = N * species support requirement. Calculate surplus/deficit from these totals.
2. Worker production = floor(P * planet production rate * species production basis points / 10000); research analogously. Multiply the aggregate worker term before flooring, not each worker separately.
3. Add ordinary flat building effects after the worker multiplication; they are not multiplied. Apply the existing shortage factor afterward: floor(gross * min(produced,required) / required). Break down the base worker term, species adjustment, building effect, shortage and rounding.
4. Capacity = the species base for the colony habitat mode (base_capacity for native, sealed_base_capacity for sealed) + completed capacity effects. Sulan native/sealed bases are 12/8; other species use their native base. Wenth is not permanently capped at 6: Habitat Extension adds 2, interpreted as improved coordination infrastructure. All other effects keep their ordinary meaning.
5. Shortage pauses growth and retains progress. Otherwise potential growth = base + min(surplus cap, floor(surplus / species divisor)). At snapshot capacity, earned growth and progress are 0; expose the capacity limit. Below capacity, add progress, convert complete units up to capacity, assign all new units to support, retain the remainder unless capacity is reached, then discard that remainder.
6. Completions affect the next snapshot. Post-embark support uses this same species-aware calculation, not the rules-2 global requirement.

Unused support below capacity = max(0, surplus - surplus cap * divisor). At capacity show blocked potential growth separately. Do not add automatic reassignment, support storage, population death or reserve decay.

### Exact openings: Verdant, no buildings/projects

| Species/default assignment | Support produced / required | Net production | Net research | Growth |
| --- | ---: | ---: | ---: | ---: |
| Peldra 4/1/1, population 6 | 1200 / 1200 | 600 | 600 | 5 |
| Harl 2/2/2, population 6 | 600 / 600 | 400 | 400 | 10 |
| Sulan 2/2/2, population 6 | 600 / 600 | 400 | 400 | 10 |
| Wenth 2/1/1, population 4 | 600 / 400 | 400 | 400 | 30 |

Peldra 2/2/2 instead has support 600/1200, gross production/research 1200 each, net 600 each after shortage and growth 0. Returning to 4/1/1 buys growth without losing opening net output. A Cultivation Hub at 4/1/1 gives support 1400/1200 and growth 15. A Fabrication Hall raises production 600 to 700, not 900.

Rounding fixture: 3 production workers, base rate 101, factor 15000 gives floor(303 * 15000 / 10000) = 454 before buildings. A flat 100 makes 554. At 2/3 support, net = floor(554 * 2 / 3) = 369. Retain both rounding stages.

### Harl: industry now or population investment

On Verdant, population 6, zero progress, no buildings/projects, no commands, new births automatically entering support:

- Harl 4/1/1 reaches population 10 at published turn 5 and discards 50 growth then; it retains only one production/research worker until changed.
- Harl 2/2/2 first grows at turn 10, reaches 10 at turn 15 and discards 70 then. It earns twice the opening production/research while population investment waits.
- Ordinary rules-2 growth at 4/1/1 with capacity 10 reaches 10 at turn 8 with zero discarded remainder. A native Sulan at 4/1/1 instead reaches its larger capacity 12 at turn 12 with zero discarded remainder.

Capacity investment and new small colonies let Harl use recovery speed. Surplus remains useful before capacity; no forced starvation or population-control button is added. These traces do not prove a winning strategy.

## SPC-003. Production and conserved founding cargo

Keep one project per colony, the existing reserve and typed production command. All species can build scout (8 production points, speed 300, cargo 0). Approved founding roles all use speed 200:

| Species | Role ID / display | Cost (displayed points) | Cargo | Minimum snapshot population | Full snapshot capacity? | Founded habitat |
| --- | --- | ---: | ---: | ---: | --- | --- |
| Peldra | peldra_settler / Estate Expedition | 32 | 2 population units | 4 | No | Native |
| Harl | harl_settler / Settlement Launch | 16 | 1 population unit | 3 | No | Native |
| Sulan | settler / Shelf Expedition | 24 | 2 population units | 4 | No | Native; Shelf Sea only |
| Sulan | harbor_settler / Harbor Expedition | 36 | 2 population units | 4 | No | Sealed |
| Wenth | daughter_seed / Daughter Seed | 24 | 3 lobes | 6 | Yes | Native |

Store costs as 3200/1600/2400/3600/2400. Only owned species' allowed roles are selectable. Disallowed role commands reject project_not_available_for_species without changing target/reserves/readiness. Preserve the old settler content ID for embedded legacy definitions; add no namespace scheme.

Eligibility uses only the beginning snapshot: population >= minimum; support workers >= cargo; remaining population >= 1; and, when required, population equals effective capacity including completed buildings. Growth or capacity effects completing this resolution cannot newly qualify/disqualify embarkation. Bank production and keep the target while blocked, without paying cost, allocating IDs or transferring population. Explain every unmet condition; do not predict an unsupported completion turn.

A selected project may be blocked: an immature Wenth seed project or insufficient embark workers is a valid saved target with banked reserve. Completion eligibility is not a requirement to select or load the project.

When eligible/funded, old snapshot workers supply that resolution's output. Apply growth, then remove exactly cargo units from support/population in the candidate, create one vessel and pay its cost once. New births cannot establish eligibility. Retain excess reserve and clear the completed target. The vessel cannot launch/found until later planning. Cancelling/switching an incomplete project retains reserve and transfers nobody; completed cargo is not refunded or silently returned.

Use one calculation for completion and post-embark support/capacity forecasts. Cargo does not work, grow or consume colony support. It must fit its species' unbuilt capacity in the resulting habitat mode. Movement, cancellation/no-op/readiness and turn order retain the approved expansion contract.

Peldra fixture: population 6, Verdant 4/1/1, progress 0, reserve 2600, Estate Expedition selected. Earn 600, pay 3200, transfer 2; ending reserve 0, parent population 4/workforce 2/1/1/progress 5. Next support is 600/800 and net production/research 450 each. Warn rather than auto-reassign; support investment or sacrificing another assignment is the player's choice.

Harl fixture: a launch founds population 1/workforce 1/0/0, without founding-turn economy. On Verdant its first economic snapshot has 300/100 support, growth 50 and zero production/research. Population becomes 2 after two such snapshots. A source birth from 2 to 3 cannot unlock a banked launch until the following planning period.

## SPC-004. Sulan environments, preparation and knowledge

Rules-3 planets gain a static environment ID independent of economic planet type: shelf_sea (suitable shallow/saline shelf), open_ocean (water without the right shelf), or dry (no suitable surface water). These are coarse survey facts, not oxygen/salinity/temperature/coastline simulations. A Verdant World can be Dry; a Forge World can contain a Shelf Sea. Existing economic rates do not change with this label.

Peldra, Harl and Wenth can use all three environments when ordinary colonizable/support/capacity checks pass. Their normal expeditions include baseline habitation support; this slice adds no further restrictions for them. Sulan native colonies require Shelf Sea. A Harbor Expedition can found a sealed colony on any otherwise colonizable environment, including Shelf Sea if the player spends the kit there. It carries an enclosed harbor; it does not change the physical environment or benefit other colonies. The native/sealed capacity difference is specified below; no upkeep, orbital preparation order or separate building project is added.

Sulan home has native habitat and unchanged ordinary worker/support/growth rules, but base capacity 12. A sealed colony uses those rates/growth rules with base capacity 8. Habitat Extension adds 2 to either (14 native, 10 sealed). This approved capacity advantage/restriction revises the roster's ordinary home so the species offers more than penalties. The extra 12 production points are an up-front opportunity cost, equal to one current Fabrication Hall. From the unchanged 2/2/2 Sulan opening with zero reserve, the native expedition takes 6 resolutions and harbor expedition 9 if conditions/assignments stay fixed. Both transfer 2 existing support workers and can require later reassignment.

### Deterministic generation

Finish existing geometry, economic planet types and IDs before assigning environments. In ascending PlanetId order make one call per planet to the project's bounded PCG32 sampler, using integer weighted intervals in ordinal environment-ID order (dry, open_ocean, shelf_sea). Sample home too, then override home to Shelf Sea for every species, then apply chosen starting population/workforce. Thus equal seed/size produces identical stars, economic types, environments and final RNG state across species. Environment sampling advances RNG relative to rules 2; final RNG across rule versions need not match.

| Existing planet type | Dry weight | Open Ocean weight | Shelf Sea weight |
| --- | ---: | ---: | ---: |
| verdant_world | 2 | 2 | 4 |
| forge_world | 6 | 1 | 1 |
| crystal_world | 4 | 3 | 1 |
| barren_rock | 8 | 0 | 0 |

All rows sum to 8. No second-world habitat guarantee or reroll: a Sulan harbor can prepare any colonizable world, so inconvenience creates investment pressure rather than generation failure. Home/other physical facts remain observable only under the existing knowledge policy.

### Observation and founding

Arrival surveys environment with existing planet type/orbit/ownership. Unknown planets expose no environment, suitability count, filtered list, tooltip, hint or hidden-existence check. Static remembered environment remains valid when observations become stale. Current presence requires complete accurate environment observations alongside other physical facts. Presentation never reads raw hidden state.

Simulation supplies viewer/role-specific eligibility and effective-rate previews from permitted observations. SetFoundingOrderCommand retains known-planet membership as its first target check, before hidden existence/occupancy. Then validate ownership/station/role/capacity/colonizability, environment/prepared mode and reservations. Native Sulan on a known Dry/Open Ocean target rejects habitat_not_suitable; unsupported colony mode rejects habitat_mode_not_available. Preserve existing orders/cargo/readiness on rejection. Unknown-valid and nonexistent planet IDs still share target_not_known. A Harbor Expedition needs no preceding prepare command.

Founding consumes the correct vessel and records colony habitat_mode (native or sealed). Native mode requires the environment in the species' natural set; sealed mode requires species permission. Validate on load rather than inferring from names. Worlds retain their physical environment; a sealed harbor does not make a Dry world Shelf Sea.

### Two contrasting surveyed maps

Equal travel duration, unoccupied worlds: A is Verdant/Dry (300/200/200 rates), B Forge/Shelf Sea (200/300/200). Sulan's cheaper native expedition can found B, not A; a harbor can found either. Green economic appearance does not establish a habitat. Peldra can choose either, but support efficiency matters: at a two-unit all-support start, A gives support 600/400/growth 15; B 400/400/growth 5. Sulan B gives support 400/200/growth 30. These are shared pure previews, not UI calculations.

In another map A is Verdant/Shelf Sea and B Forge/Dry. Sulan may take A at 24 production points or invest 36 for B's stronger industry. No research prerequisite forces this choice. Travel/scouting/buildings still compete for the same production.

## SPC-005. Wenth fission and timing

A colony is one body; its units are functional lobes. The starting 4-lobe body has coordination capacity 6; each production/research lobe supplies 2x the ordinary worker term. Support work is not tripled. Maturity means current effective capacity, not a campaign turn or an idle target.

A Daughter Seed may be selected/banked before maturity. Completion requires snapshot population at capacity, population >= 6, at least 3 support lobes, and sufficient reserve. This extends production eligibility through content, not a separate fission command/resource. Project selection never cuts the parent.

Without buildings/projects and with default assignments, reach population 6 at published turn 6, workforce 4/1/1, progress 0, discarding 20 growth then. A banked seed cannot complete on the resolution first growing 5 to 6; it may complete on the next resolution. At population 6/workforce 4/1/1/reserve 2000, earn 400, pay 2400, leave parent 3 at 1/1/1 and a 3-lobe seed. Next parent support is 300/300, production/research 400 each, growth 10. Faster healing takes a lobe away from other work into support.

Founding makes daughter population 3 at 3/0/0, zero progress/reserves/buildings and no founding-turn economy. Parent 3 + cargo/daughter 3 conserves 6; fission creates no units, later ordinary growth can. Daughter begins at capacity 6 and inherits empire technology, not parent buildings/reserves/progress. It must mature before making a seed. No shared capacity or telepathic workforce transfer exists.

Completed Habitat Extension makes the parent capacity 8; seed completion then requires 8. Cargo remains 3, leaving parent 5 and daughter/cargo 3, not an equal half. Parent retains its capacity building; daughter still has base 6. Raising developed potential can therefore delay the next fission. The owner approved this policy with the other numbers in ADR-008.

Preserve phases: snapshot economics/eligibility; growth; production/research completions and cargo transfer; movement for snapshot vessels; founding for snapshot intents; readiness reset and turn increment; observations refreshed with that new turn; final validation/atomic publication. Newly completed vessels do not launch/found and newly founded colonies have no economy that resolution. Founding conflicts retain empire/vessel-ID order, losing cargo is kept and failed intent cleared. New resolution conditions consume no RNG.

## SPC-006. Data, validation and persistence

Behavioral fields below define the approved contract. Internal C# names/factoring are later builder choices. No Godot types enter simulation. Ordinary content uses JSON, not species-ID switch statements. Original global numeric rules remain authoritative for old campaigns.

### Content

New species entries contain: id, name, description, descriptive culture_name, population_unit_label, starting_population, starting_workforce, base_capacity, sealed_base_capacity, support_per_population, production_basis_points, research_basis_points, base_growth, max_surplus_growth, support_per_growth_point, natural_environment_ids, allows_sealed_habitats and allowed_vessel_ids. Use existing starting_colony.planet_type for every species. Culture text is metadata, not a formula input.

Define environments with ID/name/description, plus environment_generation_weights on planet types. Extend vessels with requires_full_colony and founds_sealed_habitat alongside existing fields. Native founding checks owner natural environments. Only Sulan permits sealed habitats; only Harbor Expedition creates sealed habitat. Other species naturally allow all three environments. Core allowed-role lists are scout plus the species' rows in SPC-003.

Require rules-3 fields in embedded definitions; invent none in legacy content. sealed_base_capacity is required/positive when allows_sealed_habitats is true, otherwise null. Growth, load validation and previews use the mode-specific capacity. References/lists are ordinal, duplicate-free and defined. Cost/speed stay positive. Support requirement/divisor, capacity/population and worker factors are positive; base/surplus growth/weights are nonnegative. Starting population <= base capacity and assignments sum correctly. Weight sums are positive, bounded integers. Starting habitat/scout are valid. Founding cargo > 0, minimum source population > cargo, cargo <= allowed species' unbuilt capacity in the role's founded habitat mode, and minimum/full-capacity conditions must be attainable. Scouts carry 0, cannot found and require neither full capacity nor sealed habitats. Prepared roles require their allowed species' sealed permission.

Extend the existing arithmetic-bound proof over each species' maximum reachable capacity, worker factors and flat effects. Use checked Int128 and reject bounds that cannot fit stored outputs/growth/capacity; do not wait for mid-turn overflow. Runtime reserve/ID overflow still rolls back atomically. Errors name file/entry/field. Unknown IDs, non-integral data, missing fields and invalid references reject at load. Reuse existing type/bound policies rather than add arbitrary balance caps.

### State and permitted views

Rules 3 adds empire species_id, planet/planet-observation environment_id and colony habitat_mode. Clone/canonically save these and expose through relevant immutable views. Economic, completion/embark and report paths share species-aware pure functions. Owned role/cargo already determines founding population. Do not add an unseen foreign-species/economic/cargo roster: retain observed foreign vessel role visibility and existing filtered forecast boundaries.

Validate empire species, role availability, physical environments and complete current observations; colony mode against owner biology; workforce/population/growth/capacity/cargo/orders and existing relationships. Historical ownership may be stale; colony identity/planet and static environment remain valid. Never reinterpret a valid rules-1/2 game through these requirements.

### Approved version policy

New games use rules 3; writer uses schema 4. Supported (schema,rules): (1,1), (2,1), (3,1), (3,2), (4,1), (4,2), (4,3). Reject all other combinations clearly. Schema-4/rules-1/2 retains old state/content shape and absent new metadata, without injecting definitions, rescaling populations or sampling environments. Schema-4/rules 3 requires all new fields.

Existing campaigns retain embedded economy, roles, observations, RNG/layout and costs. Assign neither Harl nor an implicit upgrade. Resaving may change schema only; continuation must match the old resolver. Retain the rules-1 banner; rules-2 loads can show a short ordinary-expansion compatibility note, not a demand to restart. No upgrade-copy tool.

Before changing current writer/core content, capture a genuine schema-3/rules-2 save/embedded fixture from the integrated build, record source/hash and uninterrupted continuation. Retain genuine schema-1/2 fixtures; never synthesize an old fixture with the new writer. Keep save/write/load atomicity, explicit version pairs, all-ready validation isolation and unique QA names.

New-game UI offers rules 3 after implementation. Old modes remain accessible through saves and frozen regression fixtures, not a new mode selector. Update current-default tests deliberately while retaining original economic/expansion outcomes under explicit old-rule/content fixtures.

## SPC-007. Player explanations and interaction

Use existing C# controls, containers/anchors and shared theme. No product scene or portrait dependency is required. Exact card/picker arrangement is left to the builder; these behaviors are not:

- Before Start, display species/culture flavor, population-unit meaning, opening allocation/capacity, support, worker contributions and settlement choices. Distinguish flavor/rules and make the approved Harl default visible.
- Colony forecasts name species as a calculation source, showing gross/shortage-adjusted output, growth/base/surplus/capacity terms, blocked embark conditions and actual post-transfer population/support. Wenth uses lobes, others population. Speaking-limbs are never leaders or stored population.
- Surveyed inspection shows environment and viewer-adjusted rates/eligibility through simulation-supplied permitted previews; underlying planet rates remain inspectable. Unknown planets stay absent. Do not calculate species multipliers in presentation.
- Production lists only allowed roles and their exact cost/cargo/minimum/full-capacity conditions. No hard-coded population-4/support-2 or founding-population-2 text. Wenth maturity/capacity and Sulan native-versus-harbor suitability are explicit.
- Retain queued-route replacement, exact selected planet/colony IDs, explicit founding, cancel/no-op/error preservation, report navigation, Save/Load and resize/focus/scroll behavior. No mid-campaign species switch.
- Failed commands/loads preserve the published session. Control disabling agrees with command validation, but never substitutes for validation.

## SPC-008. Acceptance and review gates

These are approved future procedures, not executed species functionality. Preserve the original 281 test contracts and 275/466 graphical scenarios under their corresponding rules/content. Counts will grow. Independent arithmetic checks of this draft are not engine tests or balance evidence.

| ID | Required future evidence |
| --- | --- |
| SPC-AC-001 | Small/Medium/Large with all four species; exact opening table, one scout/home-only survey; validated/displayed species/size/seed; invalid/missing species publishes nothing |
| SPC-AC-002 | Exact formula order/flooring, Peldra healthy/underfed/building cases, nondefault valid content and rounding fixture; forecast equals resolved economy/embark, including post-completion support/capacity |
| SPC-AC-003 | Harl 4/1/1 cap turn 5/discard 50; 2/2/2 cap turn 15/discard 70; ordinary rules-2 turn 8/discard 0 and native Sulan capacity 12 at turn 12/discard 0. Births enter support; no death/archive resource |
| SPC-AC-004 | Allowed/disallowed projects for every species, conserved cargo, bank/cancel behavior, minimum/full-capacity boundaries and birth not unlocking embark; rejection preserves orders/readiness/state |
| SPC-AC-005 | Both Sulan maps; same economic type/different habitat; native rejection/sealed success; ordinary home worker/growth rules and native/sealed capacity 12/8 (14/10 with extension); sealed save/load with unchanged physical environment; hidden truth cannot affect unknown UI/filter/tooltip/counts |
| SPC-AC-006 | Wenth turn-6 maturity/no same-resolution eligibility, 6-to-3+3 and capacity-8-to-5+3 transfers, daughter base capacity and no founding-turn economy; no duplication/loss under ID overflow/conflict |
| SPC-AC-007 | Identical galaxy facts/RNG across species for equal seed/size; same base geometry/types as rules 2; ordered habitat sampling/home override; deterministic commands/save continuation |
| SPC-AC-008 | Content references/arithmetic bounds; malformed species/environment/habitat/cargo/order/current-observation rejection; original genuine fixtures/pairs and rules-2 capture continue after schema-4 resave; save/load failures preserve prior file/session |
| SPC-AC-009 | Graphical new-game-to-second-colony-to-reload for all species; native/sealed Sulan, Harl one-unit colony, Wenth seed; retained exact selection/routes/reports/workforce/pickers/window sizes; no engine diagnostics |
| SPC-AC-010 | Explicit owner acceptance/amendment of creative/unit changes, numbers, Sulan investment, Wenth maturity/capacity and version/new-campaign policy before implementation planning; no invented human/balance pass |

### Accepted owner review checklist

1. Accept/amend retained names/bodies and familiar population labels, viable Peldra start and positive Harl recovery identity.
2. Accept/amend the numeric profile/role tables, especially full-capacity Wenth fission and capacity-extension interaction.
3. Accept/amend coarse surveyed environments and Sulan's native/sealed tradeoff (24/36 production points, 12/8 base capacity), without terraforming.
4. Accept/amend Harl as visible New Game default and schema-4/rules-3 new-campaign separation, without converting old campaigns.

The owner accepted all four groups above on 2026-10-23 and requested the separate dependency-ordered implementation plan. SPC-AC-010 is satisfied for design review through ADR-008; SPC-AC-001..009 remain unexecuted. Arithmetic and builder checks are not engine/balance evidence. The implementation plan is written for review; no gameplay code/core JSON, assets/dependencies or migration has been implemented.
