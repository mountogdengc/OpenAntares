# Rival expansion: another civilization in the galaxy

**Status: Full design approved 2026-10-24 in [ADR-010](../../DECISIONS/ADR-010-approved-rival-expansion-design.md). RIV-AC-012 is satisfied; no rival implementation yet. Separate [implementation plan](../plans/2026-10-24-rival-expansion.md) awaits owner review.**

REQ-017 / design milestone M4-D complete. The owner first selected competition with another empire in [ADR-009](../../DECISIONS/ADR-009-rival-expansion-design-entry.md), then approved this complete document after its fresh builder check in ADR-010. The accepted gameplay contract below governs the separate plan; it is not implemented functionality.

Authorities: [charter](../../CHARTER.md), [engineering constraints](../../../CLAUDE.md), [workflow](../../PROJECT_WORKFLOW.md), [expansion contract](2026-10-22-exploration-and-colonization-design.md), [species contract](2026-10-23-species-mechanics-design.md) and [integrated baseline](../../../reports/SPECIES_INTEGRATION_2026-10-24.md). Keep the original classic space-opera setting/species and accepted mechanics. The current product remains rules3/writer4 until a later approved implementation changes it.

## 1. Intent, alternatives and boundary

After establishing a second colony, the player should start reacting to another civilization: scouting a promising region, finding a rival expedition there, and deciding whether to settle now or invest at home. Another empire must actually explore, support its population, construct expeditions and found colonies. It receives the same costs, growth, habitats, cargo, travel and validation as the human.

Considered: deeper solo development/research first; a bounded expansion rival; or fleet design/warfare immediately. The owner selected the experience of competing with another empire. Recommend proving an expansion rival before adding diplomacy/warfare. Without combat or victory, this slice remains an expansion race, not a completed competitive campaign.

Include: new-game rival setup, deterministic legal planning, existing species economies/settlement, observed encounters, truthful rival labels/reports, save continuation and retained regressions. Exclude diplomacy/trade/alliances/attitudes, combat/invasions/blockades/territory exclusion, ship/fleet design, victory/defeat, difficulty bonuses, bespoke species personalities, new species/art/portraits, custom civilizations, multiplayer/network transport and broad automation tools. Ships may share systems and different empires may settle different planets in one system; only the same planet is exclusive.

Anti-goals: a decorative rival that never expands; an omniscient planner; free resources/population/ships; arbitrary delays pretending to be intelligence; opening a new UI or AI framework; or silently activating AI in an old campaign. The unavailable uncoached session stays Deferred. Exact traces prove reproducibility, not balance/enjoyment.

### Accepted calls from full owner review

These defaults were drafter proposals in the reviewed document; full owner approval in ADR-010 accepts them. Prospective implementation wording below describes the approved target, not current functionality.

| Call | Proposed behavior | Reason / limit |
| --- | --- | --- |
| Initial scale | One human and one active rival by default; a visible Solo option remains | Prove genuine interaction with bounded scope |
| Species | Existing four choices for each side; Harl human/Peldra rival core defaults; mirrors allowed | Reuse approved asymmetry; no new roster |
| Resources | Equal species-defined starts, no AI discounts or hidden bonuses | Same rules and ordinary commands |
| Behavior | One deterministic expansion policy, no difficulty/personality chooser | Observable activity before broad AI systems |
| Contact | Reveal species/culture identity only upon an observed encounter; retain last-seen facts | No free home/empire-status intelligence |
| Founding ties | Preserve existing EmpireId then VesselId priority | The human receives the earlier ID and therefore wins an exact simultaneous tie; this known prototype advantage is explicit, not a balance claim |
| Compatibility | New rules4/writer5; freeze all rules1/2/3 behavior, including passive AI tags | No implicit old-campaign upgrade |

## 2. RIV-001: New-game setup and deterministic starts

New Game keeps galaxy size, seed and human species. Add Rival count(None/One), visibly One by default, and a Rival species selector enabled for One. Core defaults: human Harl, rival Peldra. For valid alternative content, human falls back to ordinal first species if Harl is absent; rival initially uses ordinal first species different from the human, or the same species if only one exists. After initial selection do not silently change the other species choice; identical species are legal. None passes no rival species. Load Game remains available even when current content cannot start a new campaign. Descriptions state that rivalry presently means exploration/settlement, with no warfare or negotiation.

Creation settings require explicit human species and, for One, explicit defined rival species. Proposed extra settings default to rivalCount0/rivalSpecies null for source-compatible programmatic callers; the actual UI explicitly passes its visible default1. Counts other than0/1 reject. A rival species supplied for None rejects. Old explicit rules1/2/3 generation accepts only count0/null; no AI backport. No schema/rules selector is exposed to players.

Rules4 requires the species definitions and rival policy definition even in Solo. Follow the existing explicit rules3 generation sequence first, with that ruleset’s RNG/ID semantics and selected human species. Then:

1. Solo adds no home, empire, entity or RNG call. Galaxy facts/IDs/final RNG equal the rules3 reference, apart from new rules4 state metadata.
2. For One, collect stars other than the human home with positive squared coordinate distance from it. Rank by descending squared integer distance, then ascending StarId. Keep the farthest third: ceil(eligibleCount/3), at least one. Select from that retained set in ascending StarId with exactly one existing bounded PCG32 NextInt(count) call, including count1.
3. Use that star’s lowest-orbit planet(tie PlanetId). Override its economic type to the same content-defined starting planet type and its environment to Shelf Sea. Do this after the original rules3 environment pass; do not resample, reroll other worlds or make another random home-planet draw.
4. Allocate rival EmpireId, ColonyId, starting VesselId in that order from the current NextEntityId, after the human’s existing entities. Apply the chosen species’ exact starting population/workforce/native capacity and ordinary starting scout role, zero reserves/progress/completions and normal research eligibility. Set its active policy as below.
5. Refresh each empire’s own home observations and contacts at turn0, validate, and publish only the complete valid game. Rival homes begin in different systems, so no initial contact is granted merely by setup.

If no positive-distance distinct star/planet exists, fail with an actionable setup error and no partially running campaign. Do not regenerate the seed, weaken constraints or put two home colonies on one planet. A one-system alternative map can start Solo. The farthest-third rule spreads starts without claiming equal strategic strength; each home has the same species-specific resources and starting economic type. Changing either selected species at equal seed/size/count does not change galaxy facts, chosen homes, entity allocation or final RNG. RivalCount1 deliberately advances the rules3 RNG by one bounded invocation and overrides one additional home; tests must model that exact sequence.

## 3. RIV-002: State, policy content and permitted planning

Keep the existing C#/Godot boundary, stable IDs, PCG32 and command architecture. Proposed additions are small plain state/content records; internal class/function layout belongs to the builder.

| Data | Contract |
| --- | --- |
| Empire AiPolicyId | Rules4 Human requires null; Rules4 Ai requires the supported embedded algorithm ID, expansion_v1. Rules1/2/3 require null and remain passive. No guessed/default policy on load. |
| Empire Contacts | Rules4 sorted unique records: other EmpireId, fixed SpeciesId, FirstObservedTurn. No self, unknown empire/species, mismatch with fixed identity, negative/future first-turn or duplicate. Rules1/2/3 remain empty and do not serialize this field. |
| Core rival_ai definition | New JSON section/file: algorithm_id expansion_v1, growth_support_workers1(allowed0/1), resurvey_min_age3(positive32-bit). Embed effective values in rules4 saves. Missing/duplicate/unknown algorithm or invalid numeric values rejects rules4 creation/load. No configurable resource bonus. |
| Contact/report projections | Immutable known identities/first encounter and observed facts for the requesting empire only. No raw foreign economy, orders, cargo, hidden home or complete colony count. |

One policy supports all four species by definitions/allowed roles and shared calculations, not species-ID branches. Algorithm structure is rule logic; the small tuning values are content. Unknown algorithms reject rather than falling back. The desired live founding pipeline and ordinary scout target are each one; this algorithm does not expose a broad configuration framework. Existing excess owned vessels/projects in a valid loaded rules4 state are handled legally, not deleted to enforce a quota.

The planner receives its own permitted EmpireView and public content, plus narrowly scoped own-colony/known-world queries. It never receives GameState, another empire’s view, global PRNG/NextEntityId, setup identities, or a way to regenerate hidden worlds from the seed. No foreign pending orders, reserves or raw world lookup may enter a goal score. Callback/service representation is a builder choice; authoritative state never contains delegates or Godot objects.

Queries share the existing economy/support/capacity/embark/founding calculations on unpublished own-only frames. They may forecast an own workforce/target or compare the post-completion effect of a legal public building; this is a hypothetical comparison, never a current-turn bonus. A known-world settlement query uses only its observed type/environment and own legal role’s cargo/mode; it provides the ordinary all-support first-snapshot quantities without needing an actual ship already there. Unknown/nonexistent targets return the same unavailable result before hidden existence/occupancy checks. A distant stale empty world is treated as last-seen empty, not checked against current hidden truth. Overflowing/invalid hypothetical choices are unavailable, not partially applied. No RNG/ID is consumed by queries.

## 4. RIV-003: One atomic planning-and-resolution transaction

Preserve the existing human command path. A non-final readiness command, a no-op, a rejected/stale command, saving/loading, resizing and forecast/view queries do not run AI. When the last human becomes ready, or an explicit simulation Resolve call resolves a rules4 snapshot, perform exactly one transaction:

1. Validate the input under the existing resolution rules and clone into private working state. Keep the original input reference for every failure return.
2. Plan active AI empires in ascending EmpireId. Each receives only its own planning facts from the same pre-resolution knowledge boundary; refresh its own view after its accepted private planning changes. Earlier rivals’ hidden plans must not become visible. Do not refresh observations or advance time during planning.
3. Submit only ordinary SetWorkforce, SetProductionTarget, SetResearchTarget, SetMoveOrder and SetFoundingOrder commands through the same validation/apply path used by humans. ExpectedTurn remains the input turn. The planner cannot issue SetReady, recursively Resolve, allocate entities or write state directly.
4. Use the fully planned private state as the economic snapshot, then run the existing growth/completion, movement and explicit founding phases once. Newly completed ships wait until the next planning opportunity; growth/buildings cannot unlock same-snapshot embarkation. Preserve existing ascending EmpireId/VesselId founding conflicts: one winner, losing cargo/vessel retained, failed intent cleared.
5. Advance once, reset human readiness as before, refresh observations, update contacts/observed encounter events, validate and publish the complete candidate/report.

AI execution belongs in the simulation resolution path, so direct Resolve tests and actual SetReady share behavior; a UI-only controller is insufficient. Rules1/2/3 follow their original path exactly, including Ai-tagged empires remaining passive. General simulation support for multiple human/AI records is retained; only this milestone’s New Game UI limits the selected rival count to0/1.

An unexpected AI command rejection, planner bound violation, checked arithmetic/ID overflow or invalid final state aborts the entire transaction. No readiness change, human/AI planning mutation, income, cargo, contact or RNG advance may leak through. The last-human command failure returns the original pre-Ready session, as today. Public AI-planning failure messages are generic and do not quote hidden empire/object IDs, goals or values. Invalid hypothetical choices are skipped before submission; an unexpected rejected submitted command is an implementation failure, not permission to silently apply a partial AI turn. Existing ordinary validation/load diagnostics remain separate.

The planner is synchronous and bounded by input records; no background threads, network/model calls, wall-clock timing or stochastic searches. At most2C+V+1 planning commands per AI empire(C owned colonies,V owned vessels): one target and workforce change per colony, one travel/founding/cancellation per vessel, one research change. Emit changes only when needed. Minimum-support searches use a monotone binary search, at most31 steps for32-bit population, not enumeration over population squared. Candidate work is finite over known planets, owned sources and permitted roles/buildings; no unbounded “try until valid” loops. Core acceptance uses operation counts, not an invented cross-machine millisecond target.

## 5. RIV-004: expansion_v1 behavior

At each planning boundary choose research and colony/expedition goals, issue the corresponding own target/workforce changes, then orders for already existing ships. No persistent secret objective or planner cache is required; policy ID, ordinary projects/orders and observations reproduce decisions after load. Every tie below is explicit and uses integer/shared quantities. Do not spend banked production merely to keep a unit busy.

### Research and local development

Keep a legal selected research target. If none, select the cheapest available unresearched technology with all prerequisites known; tie ordinal TechnologyId. If none, retain the research reserve without a fictional target.

For colonies not selected to build the one desired expedition, keep a legal existing building project unless support emergency requires a legal support improvement. When choosing a new building, consider only unbuilt/prerequisite-legal definitions. Rank these categories, then cost, then ordinal BuildingId: support improvement that reduces minimum support labor(or addresses an all-support deficit); capacity improvement when at current capacity; production improvement; research improvement; other positive support improvement. No speculative capacity building when not capacity-bound. An otherwise unclassified/no-useful-effect project is not selected automatically. If no useful legal goal exists, bank production. Benefits are compared with shared hypothetical owned calculations; completed effects start at the ordinary next snapshot.

Maintain the one ordinary starting-scout role if there are no owned scouts and none being built: the lowest-ColonyId non-emergency productive colony builds it before starting a new founder. Do not spawn replacements or continually add scouts. Existing multiple scouts are all operated legally.

### Expansion targets and production

An expansion candidate is a surveyed, last-seen unowned, colonizable known world suitable for a permitted founding role. Exclude owned/currently observed foreign worlds and known roles whose cargo cannot fit the unbuilt destination. Require the shared all-support daughter preview to meet support demand; do not knowingly launch an initially starving Peldra colony on a support-poor world. Stable slow-development worlds remain possible; the policy is not an optimal universal planet ranking.

If a live owned founder exists, start no new founder project. Operate all existing ones in VesselId order and reserve distinct own destinations/planets where possible. If none exists, retain the lowest-ColonyId existing founder project that still has a compatible known candidate and attainable current-capacity requirements; cancel surplus founder targets through ordinary replacement/clearing commands with their banks retained. Otherwise choose one source/role/world combination. Source current capacity must admit the role’s minimum/cargo, and its proposed allocation must either earn positive net production or have a reserve already covering the cost. Immature Wenth projects may be selected/banked while growth reaches full current capacity.

Rank combinations by source-to-world travel duration, then role cost, then PlanetId, ColonyId, ordinal RoleId. Current-system travel rank is zero. Native Sulan and harbor candidates use their actual roles/costs/capacities; a nearer dry world can therefore justify a harbor while a Shelf Sea world can use the cheaper native role. The chosen source replaces its ordinary project with that role. Known-target loss can cause a later legal replacement; no bank is lost. Do not check an unseen rival’s current occupancy or founding order to avoid a race.

### Workforce

Use shared support previews to find minimum support workers S for the current population; if none can fully support it, S=population and treat this as a support emergency. For the chosen founder project, raise S to at least its required support-worker cargo, clamped to current population. Do not treat a birth or capacity project as already completed.

If content growth_support_workers is1, another support worker increases the current shared growth forecast, and at least one economic worker would remain, add that worker. Do not add it at capacity, when growth is capped, or when it would consume the sole economic worker. Divide the remaining workers: production gets ceil(remaining/2), research gets the rest. With exactly one remaining worker, prefer production for an active project, otherwise research for an available target, otherwise production banking.

If S equals population and a legal support-improving building is selected, try the bounded bridge allocation(population−1 support,1 production,0 research), for population at least2. Use it only if the shared forecast yields positive net production; its temporary shortage and paused growth are ordinary costs. Otherwise keep all support and wait. This lets a supported but labor-tight daughter develop when global research unlocks a support building. Unsupported or unsuitable alternate-content states may stall legally; no policy claims to solve every possible mod, guarantees optimal growth, or grants a recovery subsidy.

### Existing ships

- A committed transit is never changed. Existing ships only receive orders in later planning after arrival; a new completion cannot be directed in the same resolution.
- A stationary founder selects a compatible known candidate using travel duration then PlanetId, excluding planets already reserved by an earlier own founder. At that system, use the ordinary permitted FoundingPreview and explicitly found if allowed. Otherwise set course. If a fresh survey shows the world occupied, retarget another observed candidate; preserve cargo. If no candidate exists, cancel a stale planning intent and wait rather than exploring blindly with cargo. Pending compatible orders may be retained; policy may legally replace others at planning time.
- A stationary scout first chooses an unsurveyed charted star by travel duration then StarId. Avoid own scout destinations already claimed by in-transit/pending/earlier-planned scouts when another target exists. If no unknown stars remain, revisit a non-current survey old enough under resurvey_min_age, ranking oldest observation, travel duration, then StarId. If none exists, wait. Compute observation age in a wide type. All travel uses the existing role speed/shared calculator.

Fixed policy behavior is intentionally understandable rather than an optimal solver. Replanning consumes no PRNG. New content can tune the two embedded fields; broader policies/personality/difficulty are separate designs.

## 6. RIV-005: contacts, sightings and player explanations

An encounter occurs when an empire has current presence in a system containing a foreign colony or stationary foreign vessel, using the existing presence/observation rules. There is no en-route interception or long-range sensor model. At generation and after ordinary observation refresh, create any missing contacts in ascending viewer/other EmpireId order. Record first observed turn0..currentTurn and the encountered empire’s fixed species identity; both sides independently qualify by what they can observe. Do not share surveys or reveal the other home.

For each viewer, publish a first-contact event exactly once per contacted empire. At a later current survey, publish an observed-colony event when a foreign ownership fact is new or changed relative to that viewer’s previous observation. Say “Observed a [species] colony at [star/orbit]”, not “They just founded a colony” when only a later survey establishes it. Unknown history stays unknown. A stale sighting remains labelled with its last-observed turn; a vanished ship marker is not evidence of destruction.

Known identities/culture labels may be shown with existing map/inspector/report controls and a small contacts list. Identity retention is not continuous intelligence: show only observed colony facts with dates, never total foreign colony/population counts, production/research/reserves/cargo/routes/ETAs, private policy decisions or unseen new settlements. A known species label comes from the viewer’s contact record; no presentation-side raw foreign-state lookup. The pre-game human explicitly chooses a rival species, but campaign contact information still follows this encounter model; the AI does not receive the human’s setup choice as a shortcut.

Owned reports retain their exact own economy/completion/travel/founding details and add only that viewer’s encounter events. Other empires’ private completion/planning events remain filtered. First-contact lists/events must not alter ID allocation or RNG. Validate sorted contacts and require a contact for every currently observed foreign owner; current observations must agree with authoritative current facts as today. Old campaigns acquire no new contact list or encounter behavior.

The player can recognize competition by finding observed rival ships/colonies and by an actual contested world becoming occupied. There is no aggression/reputation label, diplomatic promise or game-over claim. Layout uses current containers/anchors/theme; no new product scene or portraits. Actual controls, content-driven labels, focus/scroll and documented800/1280/1920 windows remain part of automated acceptance; screenshots do not prove comfortable reading or physical input.

## 7. RIV-006: persistence, version boundaries and failures

Propose Rules.RivalVersion4 and writer schema5. Explicit feature predicates must recognize species behavior for3/4, expansion behavior for2/3/4, and rival planning/contacts only for4. Audit existing equality-to-SpeciesVersion gates across generation, economy, state validation, content/save writer, founding, projections and UI; simply changing CurrentVersion would disable accepted species mechanics.

Supported(schema,rules) pairs are exactly(1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3),(5,1),(5,2),(5,3),(5,4). Keep explicit readers for all old supported pairs. Writer5 serializes old rules1/2/3 using their own embedded-content/state field sets, omitting rival policy/contacts, and does not enable an old Ai tag. Rules4 requires the effective rival_ai content and explicit ai_policy_id(nullable for Human)/contacts list per empire. Unknown or misplaced new fields reject, including injected null/empty metadata in old formats. Do not silently repair a policy, fabricate contacts, assign new species, regenerate worlds or alter old RNG/IDs.

Before implementation changes the writer/default, capture a genuine current rules3 two-colony QA save with source hash/provenance and several exact future canonical states under the unmodified engine. Also capture a validated, explicitly labelled rules3 fixture with a passive Ai-tagged empire and its actual old-engine continuation. Preserve existing genuine rules1/2 traces. Comparisons may normalize schema_version only when resaving with5; rules/content/state differences are failures. Keep all source QA files/evidence.

FirstObservedTurn/SpeciesId and active policy must round-trip; duplicate/missing/foreign/self/future contacts and unsupported algorithms fail before replacing a session. Contact identity must match the target empire’s fixed species. Invalid references, unreachable setup and bad counts are actionable errors. Writes keep the existing flushed temporary replacement API and failure cleanup. Save/read does not plan AI or advance a turn. No automatic migration of old campaigns into rival games.

## 8. RIV-007: concrete traces and acceptance

Expected numbers below are proposed future fixtures, not executed results. All species formulas/roles are the approved existing ones. For an initial core Verdant AI colony with no known expansion candidate, no existing target/technology, zero reserves/progress and legal Fabrication Hall, the policy chooses that building and Cultivation Methods. Its first snapshot should be:

| AI species | Chosen support/production/research | Support produced/required | Net production/research | Growth earned |
| --- | --- | --- | --- | --- |
| Peldra |5/1/0 |1500/1200 |600/0 |20 |
| Harl |3/2/1 |900/600 |400/200 |70 |
| Sulan |3/2/1 |900/600 |400/200 |40 |
| Wenth |3/1/0 |900/400 |400/0 |50 |

All values are stored hundredths; current technology/building costs remain unchanged. This illustrates legal choices, not species bonuses added by AI. Harl one-unit Verdant daughters still earn50 growth when all support and reach2 after two economic snapshots. An AI Wenth colony at population5/capacity6 with a covered seed cost cannot complete on the snapshot that grows it to6; it may complete at the following planning/resolution. With a live candidate, population6/workforce3/2/1, bank2400 and seed target, production800 pays2400, retains800, and transfers3 existing support lobes into seed3, leaving parent3/workforce0/2/1; next planning must repair support by ordinary allocation. Extension raises maturity to8 and daughter remains unbuilt capacity6. Existing approved Sulan native/sealed12/8 and Peldra costs remain exact.

| Criterion | Required future evidence |
| --- | --- |
| RIV-AC-001 Setup |Actual UI One/default Harl/Peldra, Solo, mirror species and alternative-content fallback; explicit settings errors. Unit generation all16 human/rival species pairings ×3 core sizes, plus impossible one-system rivalry and valid Solo. |
| RIV-AC-002 Generation |Rules4 Solo equals rules3 galaxy/IDs/RNG; one-rival top-third selection, one bounded call and one extra override; equal seed/count facts across species; no reroll/partial publication/hidden home marker. |
| RIV-AC-003 Fair economy |All four first-snapshot traces above; no discounts/free units; cargo/reserves/population conserved; nondefault costs/cargo/basis points and renamed definitions honored without species-ID branches. |
| RIV-AC-004 Policy viability |Scout discoveries, real project/research completion and actual second colony for each rival species; Peldra initial-starvation avoidance/support bridge, Harl recovery, Sulan native/harbor alternatives, Wenth base/extended maturity. Empty candidate sets wait legally. |
| RIV-AC-005 Information |Identical permitted planning facts produce identical AI commands despite changed unseen planets, foreign species/resources/orders and global RNG/ID counters; no seed regeneration. Own rendering remains invariant even with overflowing foreign research reserve. Global turn resolution need not succeed identically when real foreign income changes; distinguish planning privacy from whole-world outcomes. |
| RIV-AC-006 Transaction |One last-human Ready resolves once; non-final/no-op/stale/forecast/save/load do not plan. AI rejection, ID/output overflow, invalid candidate and bound failure preserve original session/readiness/state/RNG/contacts. Direct Resolve uses same rule4 planning; multi-AI order stable in labelled fixtures. |
| RIV-AC-007 Race and travel |Existing ship travel timing and new-ship wait; no automatic founding on arrival. Competing human/rival intents use existing ID priority, loser retains cargo; stale occupied target retargets only after permitted information. No cross-empire cancellation or foreign-order peeking. |
| RIV-AC-008 Contact |Unknown/current/stale facts, one first-contact event per viewer, saved retention, no false founding-history claims, no unseen ship/route/colony-count leak; corrupt/missing contacts reject. |
| RIV-AC-009 Compatibility |Eleven-pair matrix, genuine rules1/2/3 canonical traces including passive Ai3, old field omission, invalid policies/metadata, failed writes/loads and rules3 UI continued behavior. |
| RIV-AC-010 Integrated acceptance |New Rival graphical scenario: actual new game, End Turn, sightings/reports, ordinary human choices/settlement, rival development, real save/load and future canonical continuation. Twelve generated seed42 loops(rival4 species×3 sizes, human Harl including mirror) must show a rival second colony and encounter within80 resolved core turns, without injected live income/population/orders. Separate labelled fixtures cover both habitat choices, extended Wenth and forced conflicts. Solo and all human species get setup/continuation checks. |
| RIV-AC-011 Retained gates |Full simulation suite; Legacy/Expansion/Species scenarios use genuine frozen old campaigns, while Rival owns the new default4 assertions. All4 runtime scenarios in unique retained directories; no final engine diagnostics; normal rebuild excludes every opt-in validation class. Actual result counts replace expectations; no fabricated mandatory failure. Physical input/human/platform/release limits remain open. |
| RIV-AC-012 Owner gate |Explicit approval/amendment of the complete design including proposed setup, policy, contact, tie and version calls before implementation planning. Directional “yes” alone does not satisfy this full-design gate. |

The80-turn generated fixture is a bounded core regression, not a universal balance/performance guarantee. Alternate content with no sustainable unowned worlds must produce a legal waiting policy; no hidden map/resource repair is allowed to make an acceptance trace pass.

## 9. Handoff, builder choices and current evidence

Builder choices: internal file/type layout, own-only query representation, sparse display organization using existing containers, exact error wording/localization labels, and test helper organization. These may not change the defined ordering, budgets, public information, compatibility or policy. No function bodies or implementation task sequence are prescribed here; a separately reviewed plan follows full design approval.

Builder check: complete. A fresh-context, read-only reviewer returned “nothing important left”: no material unanswered design questions were raised. See [the design review record](../../../reports/RIVAL_EXPANSION_DESIGN_REVIEW_2026-10-24.md). The owner subsequently approved the complete design in ADR-010. The builder check itself did not approve gameplay or verify implementation.

Design baseline is main c3b01db plus preserved owner prose. Current source remains species rules3/schema4. Last executed verification is the retained species integration(445 simulation,1181 species,275 expansion,466 legacy assertions+27 geometry observations); none of those counts verifies this proposed rival system. This design task changes documentation only. Keep both owner prose hashes, all QA saves, recovery snapshots/stash and worktrees; no push/merge/cleanup or product dependency/art change is authorized by drafting.
