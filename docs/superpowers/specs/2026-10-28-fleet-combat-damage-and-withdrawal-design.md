# Fleet combat, damage and withdrawal design

2026-10-28. **Status: Draft for owner review. M9-D In Progress; M9-P and M9-I Not Started.** The owner authorized this written proposal after M8 was merged and pushed at `63ec608`. This authorizes drafting, not the gameplay defaults below or implementation. Every new rule and number below is a drafter recommendation unless explicitly identified as existing owner direction. Full design approval precedes a separate implementation plan.

## Intent and authority

Make deciding when to fight, withdraw and repair interesting. Ships should become less effective before they die; a damaged ship can slow its companions, and surviving ships should carry consequences home. Routine execution should follow understandable fleet orders rather than demand repeated per-ship input.

The owner's [combat direction](../../design-notes/2026-10-27-combat-retreat-morale-and-damaged-ships.md) calls for capability degradation, morale/retreat, limping home, possible detachment, hit-and-run and substantial automation with a possible manual option. Those goals are established; the exact combat/control model remains open. [Fleet intent](../../design-notes/2026-10-24-delegation-crises-and-fleet-intent.md), [approved M8](2026-10-27-fleet-command-and-admirals-design.md), the [charter](../../CHARTER.md) and [engineering rules](../../../CLAUDE.md) govern the proposal.

Current product facts: rules8, writer9, 37 accepted schema/rules pairs; peaceful rival expansion, stationary ameba, ordinary scout/settlement hulls, named fleets and admirals. There are no warships, damage statistics or combat phases. The M8 integration report records 1,547 passing simulation tests; this design does not claim a new application test run or balanced combat.

## Recommended boundary and alternatives

The recommended first encounter is an optional attack on a fixed pocket of **automated raider sentries**, with one exchange per strategic turn. Sentries guard an uncolonized system. They permit passing and surveying but prevent founding there until defeated. This creates a reason to clear the pocket while preserving peaceful relations with expanding empires. Sentries are finite generated hazards, not an empire receiving free ships or a new species/culture. Their machine nature explains their lack of crew morale. The name and presentation are original provisional content.

| Approach | Value | Cost or limit | Recommendation |
| --- | --- | --- | --- |
| Fixed sentry encounter, automatic exchanges and fleet-level orders | Exercises construction, damage, retreat, repair and officer loss in the ordinary campaign | A deliberately small combat testbed; no war between empires | First slice |
| Limited hostile relations between existing empires | Direct strategic competition | Also needs declaration/peace, civilian targeting, defense, enemy construction and loss behavior | Later separate design |
| Tactical battlefield with direct ship orders | Fine positional control | Adds a second movement/time model and substantial UI before strategic rules are established | Keep open for later; do not make this compulsory |

No bombardment, invasion, colony destruction, capture, surrender, salvage, boarding, fuel, upkeep currency, component designer, weapon tech tree, escort interception, interstellar pursuit, allied battles, automatic replenishment or victory condition is included. The existing ameba remains governed by M7 and cannot be attacked with this system. No permanent peaceful-mode feature is silently delivered; [FR-001](../../FEATURE_REQUESTS.md#fr-001-peaceful-mode) remains separate.

## CBT-001: Campaign setup, sentries and information

Proposed new-game setting: **Sentries: Standard / Off**, initially Standard. Standard generates exactly one pocket containing two sentries. Off generates none; warship construction, repair and combat-capable state still exist under the new rules. Explain that Standard blocks founding at one guarded system but never turns an ordinary scout arrival into a battle. Existing Ameba and rival/Solo settings remain independent.

Choose from stars containing at least one planet that at least one configured playable species could settle under its existing environmental rules, ignoring source-population and construction affordability. Exclude every starting-colony system and any ameba habitat. Rank by greatest minimum squared distance to all starting-colony stars, then lowest StarId; choose the first. Evaluate only after the existing galaxy/species/ameba placement. Use integer distances and no existing gameplay RNG draws. If no eligible star exists, return a located setup error explaining that Standard cannot place a pocket; the owner can choose Off. Do not silently change the setting or regenerate the galaxy.

Pocket and sentry IDs use new deterministic local counters, never consume the existing global entity allocator, and are distinct from empire VesselIds. Sentry hulls are hazard actors, with no economy, population, officer, colony ownership or empire orders. They stay at their pocket, never respawn, never repair, never expand and never attack peaceful traffic. Persist destroyed/escaped facts and remaining hull points; repeated attacks cannot reset attrition. A cleared pocket remains cleared.

The public star chart does not reveal hidden pockets. Normal own presence/survey reveals the pocket, current sentry count and public stat definitions, hull/damage state, and the founding restriction. Record last-observed facts with their turn; without own current presence, show stale facts without silently refreshing them after another empire acts. A fleet present at the pocket has current observations. No remote preview discloses hidden battles, another empire's ships, casualties or officers. A stale attack order validates against currently permitted observations at commitment; unexplained changes produce a generic unavailable-target result, not foreign details.

Any active pocket blocks founding at its star for every empire, even while a human battle is underway. Show the reason in the existing founding preview. The sole existing rival planner skips a guarded founding candidate only after its own observation reveals that fact; it retains its existing expansion policy, pays ordinary costs and neither builds warships nor attacks pockets in this slice. Shared command validation enforces the same block for humans and AI. Clearing opens existing founding rules; it grants no ownership, population, research or guaranteed claim.

## CBT-002: Hulls and construction

All proposed values are core JSON content, embedded in new-rules saves and validated with source/entry/field errors. Costs use current internal hundredths: 2,400 internal production is 24 displayed production.

| Role | Max hull | Attack per exchange | Armor per incoming shot | Base travel/escape speed | Construction |
| --- | ---: | ---: | ---: | ---: | --- |
| `escort` (new basic warship) | 100 | 24 | 4 | 300 | 2,400 production; no embarkation or founding |
| `scout` | 60 | 0 | 0 | Existing 300 | Existing cost and eligibility |
| All existing settlement roles | 100 | 0 | 0 | Existing 200 | Existing species/cargo/eligibility unchanged |
| `raider_sentry` (hazard actor) | 80 | 18 | 2 | 250 pursuit rating | Two generated at a Standard pocket; not buildable |

Escort construction is available from the start, to every species, through ordinary project and finite M8 assembly controls. No free starting escort or new technology is granted. Organizer discounts, queue priority, output reserves, one completion per colony per turn and newly-completed launch restrictions remain unchanged. No new combat-only admiral specialty, XP award or combat bonus is added; M8 travel, recovery and existing earned progression continue in their approved scopes.

Every new-rules empire vessel stores hull points and command cohesion, initially max hull and 100. Civilian hull statistics matter only if the player explicitly takes those ships into an attack. Civilian traffic outside that commitment remains safe from sentries. Sentry stats are separate actor definitions, not counterfeit empire vessels.

## CBT-003: Persistent damage and speed

Use three hull bands. Test bands by integer products against max hull; never round a percent and then classify it.

| Hull remaining | Firepower factor | Travel/escape speed factor | Label |
| --- | ---: | ---: | --- |
| More than 50% | 100% | 100% | Operational |
| More than 25%, at most 50% | 50% | 75% | Damaged |
| More than 0%, at most 25% | 25% | 50% | Crippled |
| 0 | Cannot fire | Cannot travel | Destroyed and removed from live actors |

Per shot: `effectiveAttack = floor(baseAttack * firepowerFactor / 100)`; unarmed ships never fire. `damage = max(1, effectiveAttack - targetArmor)` for an armed attack. Armor is not consumed. Hull after the exchange is `max(0, hullBefore - sumIncomingDamage)`. Excess damage is lost; it never spills to another ship. Cap actual hull loss at hullBefore for reporting. There are no hit/miss or critical-hit rolls in this slice.

For an empire ship, compute `effectiveSpeed = max(1, floor(floor(baseSpeed * damageSpeedFactor / 100) * (100 + applicableNavigatorBonus) / 100))`. A fleet travels at its slowest surviving member's effective speed. Use this for planning previews, committed travel and retreat comparison. A rank-one Navigator gives an escort speeds 315 / 236 / 157 across the three bands. Sentries use their damage factor on their 250 pursuit rating without officer bonuses.

Damage at the end of a volley changes firepower from the next volley, not retroactively. Damage cannot change an already committed transit duration. No attacks occur in transit and repairs cannot occur there, so new damage never appears halfway through a normal journey. Losing a vessel is irreversible; no disabled-but-alive zero-speed state, tug or scuttle authority is introduced.

## CBT-004: Attack commitment and battle lifecycle

An **Attack pocket** command requires an own named fleet, 1..32 members, stationary and co-located at an observed active pocket, at least one armed ship, and no active transit, founding, assembly/rally reservation or ameba Study/Remain. Existing ameba safety gates remain authoritative. The fleet must Hold before committing; preview explicitly replaces Hold with the finite attack order. A move order never means attack. Arrival may reveal the pocket but cannot start combat that same resolution.

The preview lists every participating hull, civilian passengers, hull bands, firepower, cohesion, possible losses, one-turn volley forecast, doctrine, return destination and officer-recovery policy. Taking unarmed ships into the attack requires an explicit separate civilian/cargo checkbox, naming each affected ship and passenger total. Its absence rejects the commitment. This authorizes those ships' combat risk, not destroying or abandoning them as a separate order. Amendments require a new preview and consent revision.

A pocket admits one pending/active attacking fleet at a time. The first accepted command reserves it in authoritative planning state; competing submissions receive an unavailable result with no foreign identities. Cancelling before the first volley releases the reservation. A committed encounter persists until victory, destruction or completed withdrawal. Other fleets cannot join, fire, repair participants, transfer officers/flagships or supply reinforcements during it. They may pass or wait safely. This explicit first-slice restriction prevents undefined multi-empire initiative and target pooling.

At the next resolution, an accepted pending attack becomes active and performs its first exchange. Later snapshots perform exactly one exchange unless a withdrawal action applies. Encounter members are fixed except combat destruction and authorized withdrawal detachment. No changing membership, disbanding, role/posture automation, M8 Return/Explore/Assemble, manual movement, founding or officer transfers bypasses active-combat restrictions. A player can change attack doctrine/return destination through the combat command, or order withdrawal. Commands use ordinary issuer, expected-turn, ownership, observation and revision validation.

If pre-combat plague/ameba loss removes a committed member, invalidate that encounter revision, cancel a pending first attack without firing and require a fresh preview. For an already active encounter, normalize the losses, retain surviving members and set persistent Emergency; before the next volley a human must choose withdrawal or explicitly accept the revised force. Do not silently substitute another hull or continue under stale civilian consent. Resolution-time invalidation skips that encounter's volley for that boundary; no ship can fire from the invalidated encounter. Loss facts still publish atomically.

## CBT-005: Automatic exchanges and targeting

At exchange start, freeze hull bands, firepower, participants and target choices. Each living armed actor fires one shot. Each side targets the lowest stable actor ID among opposing armed actors; if none remain, target the lowest ID opposing unarmed actor. Hazard IDs and VesselIds are ordered only within their own side. All shooters on a side may focus the same target. No automatic weakest-hull targeting or access to remote/hidden state is used.

Both sides' shots apply simultaneously. A ship destroyed this exchange still fires its frozen shot; crossing a damage band does not reduce an already frozen shot. No retargeting after another shot, damage spill, extra reaction shot or initiative advantage. Sum damage using checked wide intermediates, then apply hull loss and destruction in stable ID order. This rule also applies to a withdrawal interception volley, except that withdrawing ships never fire.

If all sentries are destroyed, victory closes the encounter and removes the founding block at publication. Surviving ships stay at the star in Hold with their persistent damage/cohesion. If both sides are destroyed simultaneously, the pocket is still cleared and the empire lost its entire participating force; do not award survivor credit. If the attacking force is destroyed while sentries survive, close the encounter, release its reservation and retain the damaged sentries. No reward rolls or loot exist.

If the last armed empire ship is destroyed but civilians survive, queue mandatory withdrawal at the next boundary using their authorized return route; they remain at risk until it happens. An ordinary exchange can target them on a later turn if departure is blocked. The report must make that danger explicit. No fresh attack can begin with an unarmed force.

## CBT-006: Cohesion and standing orders

Use **command cohesion**, a 0..100 integer stored per empire vessel; present it as coordination under pressure rather than assuming human crew biology for Wenth. It is not a population morale system. New ships begin at 100. Existing colony culture gives no hidden modifier. Transfer, split, merger, rename, new encounter and save/load preserve each surviving ship's value.

After an ordinary/interception volley, every surviving empire participant loses `min(100, 20 * friendlyShipsDestroyedThisVolley + 10 * friendlyShipsNewlyEnteringCrippledBand + 10 * flagshipDestroyedThisVolley)`, clamped at zero. Count newly crippled only for ships surviving at 1..25%; a destroyed ship is not also newly crippled. Count the designated pre-volley flagship once. Departed clean-escape cohorts are not participants in the interception volley and take no cohesion loss from it. Sentries are machines and have no cohesion/morale state. Hull damage remains their only degradation.

The fleet's displayed cohesion is the minimum of its surviving members, with the contributing ship shown. It changes automatic retreat behavior, never shot damage or speed. There is no hidden morale roll or uncommanded surrender.

| Fleet combat order | Automatic behavior | Override |
| --- | --- | --- |
| Cautious (initial) | After a volley queue withdrawal if any survivor is crippled, any friendly ship is destroyed, or fleet cohesion is at most 40 | Direct withdrawal or explicit switch to another order |
| Press the attack | Continue; queue withdrawal when fleet cohesion is at most 40 or no armed ship survives | Direct withdrawal or explicit switch |
| Hit and run | One normal volley, then queue withdrawal regardless of its result | Direct withdrawal may leave before that volley |
| Hold despite losses | Ignore hull/cohesion retreat thresholds; continue while armed ships survive | Manual opt-in warning and direct withdrawal always available |

Hold despite losses is never a culture, officer or new-fleet default. No order authorizes scuttling, capture, sacrifice of a detached ship or a change to officer Last watch. The player may explicitly cancel a queued threshold withdrawal by changing to Press/Hold when its conditions permit. Such a change explains that it overrides the current retreat warning; acknowledgement alone never changes doctrine. If no armed ship remains, mandatory withdrawal cannot be cancelled into an unarmed attack.

## CBT-007: Withdrawal, destinations and interception

Attack commitment requires an explicit owned return-colony system, a Keep together / Detach crippled stragglers policy, and **On arrival: Repair / Hold**. Keep together and Repair are the proposed initial selections; all three values are visible persisted parts of the accepted preview. Repair authorizes finite Return-and-repair for the main fleet and every detached survivor. Hold authorizes the same return journey followed by Hold for all cohorts, with no automatic repair tick. Either choice may be changed through a new preview before departure, or overridden normally after arrival. A return system must be currently known, owned and legal for all participating cargo under existing quarantine/port rules; it must not be the encounter star. No destination is silently guessed or substituted. Changing it uses a preview. Show estimated travel duration and whether an interception volley would occur for each proposed cohort, with the observations and their turn.

A direct withdrawal before the first combat volley cancels the attack reservation and submits ordinary travel under the displayed return authorization; sentries do not shoot a force that has never fired. After any normal volley, withdrawal occurs at a subsequent strategic boundary. Automatic withdrawal chooses the same previously authorized return destination. It is not a free mid-volley teleport.

For each proposed departing cohort, compare its slowest member's current effective speed with the fastest surviving sentry's effective pursuit rating, both frozen before the interception volley. Strictly faster cohorts escape without another shot. Slower or equal cohorts are exposed to **one** simultaneous sentry-only volley. Sentries choose targets among exposed ships with CBT-005 priority; they do not shoot clean-escape cohorts. Exposed ships do not fire back. All exposed survivors then depart regardless of new damage. Recompute each surviving cohort's launch speed/duration after that damage; do not re-run the escape check, fire twice, or pursue across stars. If all sentries were destroyed, no interception exists.

The existing integer travel calculator commits the journey; its departure boundary counts as the first travel tick as in M8. A one-turn journey arrives at that boundary. New hull bands and Navigator apply at launch and remain recorded as cohort provenance. No player or sentry can intercept the resulting interstellar transit in M9.

Validate withdrawal feasibility at planning/Ready and again after earlier health phases. A known illegal return route blocks Ready until the owner selects a legal destination or explicitly accepts remaining for an ordinary defensive exchange. If a route becomes illegal only during the resolution, no departure occurs, the fleet instead performs one ordinary exchange and receives Emergency explaining the blocked withdrawal. This contingency is stated in the attack/withdrawal preview. The next planning period blocks unchanged Ready until a new destination or explicit continued-risk response is chosen. Do not repeatedly auto-retry a blocked retreat in the background or suppress existing plague consent. A blocked withdrawal never converts to Hold despite losses.

## CBT-008: Keep together and detach stragglers

Keep together withdraws every survivor as one fleet at the slowest member's speed. Detach crippled stragglers, if explicitly selected, separates surviving non-flagship ships at or below 25% hull into independent return journeys. The flagship always stays in the main fleet, carrying its officer; no officer transfer or new fleet slot is required. The main fleet contains the flagship plus all other non-detached survivors. If the flagship was lost, M8 chooses the lowest surviving VesselId as replacement before calculating detachment. If no survivors remain, close the encounter.

Detached ships remain owned, keep cargo, damage, cohesion and histories, and each receives the same return destination and On arrival: Repair/Hold authorization as the main fleet. They are not abandoned, sacrificed or scuttled. They may individually suffer an interception volley if exposed; the main cohort can escape cleanly while a straggler is exposed. The common targeting rule applies across all exposed cohorts, not once per cohort. Preview all departures and explain that a crippled flagship prevents this policy from making its main group faster. Direct manual reorganization is available after combat and co-location permit it; no mid-volley flagship reassignment.

Freeze detachment membership before interception. A ship newly crippled by that volley remains in its original cohort and can slow it; do not detach it again, merge cohorts or rerun the escape comparison. After interception, remove dead members and recompute only each original cohort's surviving launch speed/duration. A previously clean-escape cohort remains exempt from interception; no new shots are introduced. If the main flagship dies, choose the lowest VesselId among surviving main-cohort members as replacement, apply the lost officer's existing recovery once, and keep original detached ships independent. If the whole main cohort dies, dissolve its now-empty fleet; detached survivors continue independently with their own finite return authorizations. They never inherit the main fleet's officer or re-form that fleet automatically. Existing M8 assembly requests must mark destroyed bound slots Missing and safety-pause any route back to the pocket; a Return/Repair order never silently resends a survivor into attack.

## CBT-009: Repairs and recovery

Repair is a fleet or independent-ship order at an owned colony system, available without a new building or technology. Each full resolution spent stationary there under Repair restores `ceil(maxHull / 4)` hull (capped at max) and 20 cohesion (capped at 100). It costs time and consumes that ship's turn; it does not spend production, reduce colony output, consume population or create a new currency. Multiple ships may repair simultaneously. Damage-free ships may recover cohesion through Repair.

Only ships already at a legal owned port with Repair selected in the resolution snapshot qualify. A ship cannot fire, depart, found, study or be in transit while earning that repair tick. Arrival at a port, new construction, or victory this resolution never earns an immediate repair tick. If earlier health closes the port for that ship/cargo, repair is blocked and reported; existing quarantine takes precedence. No passive healing in empty space or at foreign colonies. Sentries do not heal.

Return-and-repair first performs the committed return journey. Once arrived, it authorizes Repair from the following planning snapshot without another per-ship order. It ends in Hold when hull and cohesion both reach their caps. The owner can cancel/override before a repair tick. M8 persistent safety blocks and manually accepted orders take priority; no recurring rally or re-attack is created. A repaired ship retains its ID, officer history, damage/repair totals and cargo.

## CBT-010: Admirals, civilian losses and existing systems

Nav/survey/organizer values and XP events remain exactly M8. No combat XP farming, extra promotion, automatic new officer, replacement hull or resurrection. Hull-destruction events feed the existing fleet-loss/service-record path exactly once, including civilians and the designated flagship. Record encounter, shots, actual hull lost, destroyed vessels, withdrawal and repair as immutable facts, without expanding the approved specialty awards.

On flagship destruction, apply existing Recover command / Last watch and 50%/70% equipment recovery once, with existing two-full-subsequent-turn mortal timing. Wenth retain their source body, identity and XP, with existing 2/1 or 4/3 link delays. Recovery equipment comes from the pre-resolution appointment snapshot, not technology completed later that turn. Keep officer loss doctrine separate from fleet Hold/withdrawal doctrine. A survivor does not automatically rescue or relocate the officer. No colony is destroyed or captured by M9, so no new Wenth source-body fate is invented.

Destroying an embarked settlement vessel loses its carried population; it does not remove population again from the source colony. Reuse applicable existing cargo/crisis cleanup without pretending combat is an ameba study or granting its reward/XP. A surviving infected hull retains its health state in withdrawal and repair; no free cure. Ameba hazard generation, warning, study, guaranteed prompt departure, research reward, cargo loss and RNG streams retain their existing contracts. Warships cannot kill an ameba, grant escort immunity or absorb another vessel's ameba loss.

## CBT-011: Turn phases, authority and transactions

Use the existing C# simulation, JSON content, issuer-tagged command processor, private human Ready preparation, sole rival planner, checked integer math and clone/validate/publish architecture. Automatic combat actions use the same authorization/validation path; the Godot UI does not mutate state or run encounters. Hazard firing is a bounded turn rule, analogous to existing ameba resolution, not a second omniscient empire AI controller.

The later implementation plan must map these semantic cut points to exact files/methods and failure hooks:

1. Validate planning snapshots and current combat/health consents for every human, including direct Resolve; capture combat membership, officer equipment, return authority, repairs and stat inputs. Compose with existing human preparation and rival planning; do not discard another human's accepted changes.
2. Resolve existing snapshot economy, growth/completions, research and earlier plague/ameba outcomes in their established order. New escort completions cannot join a captured encounter or launch now. Normalize existing losses and validate affected encounter revisions before combat.
3. Resolve one normal or withdrawal interception volley per eligible encounter, in PocketId order, from its frozen exchange inputs. Apply simultaneous damage, cohesion, cargo destruction and exactly-once loss facts. Queue threshold withdrawals for the next planning snapshot, never another volley now.
4. Normalize combat fleet/request/officer references before ordinary movement reads live snapshot vessels. Mark combat-destroyed vessels as consumed so MovementResolver cannot throw on missing actors. Derive legal withdrawal cohorts/committed travel only after the interception outcome; no stale pre-damage launch context may override them.
5. Resolve ordinary movement, arrivals and founding under the existing rules. A pocket cleared earlier in this boundary permits only a founding intent that was already validly committed; an active pocket rejected such an intent at planning, so ordinary play cannot retroactively found on the clearing boundary. Earliest new founding is the following planning period. No arriving fleet starts a new combat exchange.
6. Apply eligible snapshot repair ticks to surviving stationary ships, then existing observations, progression/recovery, reports and final state validation. Capture/restore complete streams, counters and accepted human planning on every failure. Advance the strategic turn exactly once.

Known planning hazards block Ready per the relevant system's ordinary permissions. Unanticipated health invalidation and blocked retreat produce explicit next-period responses; clicking a report is not consent to keep fighting. An unchanged active fight with valid standing orders needs no modal every turn. Direct Resolve and multiple-human paths must enforce the same policies.

Any malformed content, stale/foreign command, overflow, quota violation, missing consent, generation/phase/report failure or invalid final relationship leaves the original authoritative state, readiness, all RNGs/counters, snapshots, reservations and reports exactly unchanged. Save/load/opening the UI never advances combat, repairs, travel or timers.

## CBT-012: Views, reports and interruption

Extend Fleet Command with role `Warship` and statuses In combat, Withdrawing, Damaged, Crippled and Repairing. Show hull/cohesion with text and numbers; color alone is insufficient. Fleet detail offers Attack pocket, doctrine, return destination, straggler policy and Withdraw through previews. A read-only exchange forecast lists each permitted target, per-shot calculation, combined hull loss, resulting bands/cohesion, escape exposure and officer/cargo risks. It forecasts exactly the next exchange from currently permitted information, without claiming the whole battle is certain or exposing another human's unsubmitted plans.

The turn report summarizes losses, surviving damage, doctrine trigger, returning/detached cohorts and expected repair times, with expandable calculations. Default normal battle progress/repair completion is Summary; new damage, retreat, blocked repair and officer recovery/death remain persistent Attention. A previously unacknowledged civilian-risk amendment, unexpected force change, blocked retreat or destruction of the last warship with civilians remaining requires Emergency response before that empire's next Ready. Existing crisis and ameba Emergency controls remain independent. Explicit continued-risk responses must name their affected encounter/revision; unrelated acknowledgement cannot satisfy them.

Show fresh versus stale pocket observations. Clearing, total-loss and mutual-destruction reports distinguish ownership from access to an unclaimed planet. Detached ships remain easy to find and receive finite return/repair summaries. No repeated modal per shot, no automatic Last watch, no hidden default tactical screen. Use current C# containers/anchors and shared theme variations; no style constants in controls. Verify narrow and large windows, keyboard/focus flows and saved pending responses separately; automated signal checks cannot establish physical-input comfort or player comprehension.

## CBT-013: Persistence, compatibility and bounds

Proposed future target: **new campaigns rules9; writer10**. Retain the existing 37 explicit accepted schema/rules pairs unchanged and add only `(10,1)` through `(10,9)`, for **46** total. There is no `(9,9)`. Writer10 can save rules1..8 while omitting all combat-only content/state; older rules never receive sentries, escorts, damage, cohesion, founding blocks or changed speed/AI merely by loading. An Off rules9 campaign still uses combat-aware schemas and stats. Do not retrofit existing saves or redefine their embedded role data.

New state includes setting, bounded pocket/hazard IDs and hulls, clear facts, own current/stale observations, per-ship hull/cohesion, pending/active encounter reservation/membership/revision/consent, rounds, standing orders, queued withdrawals, explicit return ports, straggler cohorts, repair state and finite report facts. Every collection/counter has a declared content/state bound: max 1 generated pocket, max 2 hazard hulls, one pending/active encounter per pocket, existing max32 members/fleet and max64 fleets/empire. Keep latest 20 encounter summaries per empire, last 20 rounds per summary, and aggregate lifetime counters separately; monotonic checked event IDs and last-applied phase/round identifiers provide idempotence independently of trimmed report rows. Resolved ancient details may expire; live consent and current-block reasons never expire with report trimming. The implementation plan chooses DTO/class names and freezes the exact validator table and counter ranges within these limits.

Reject missing combat content under rules9, injected fields under old rules, wrong/duplicate ownership/membership, live references to destroyed actors, impossible hull/cohesion ranges, invalid band-derived previews, duplicate encounter reservations, out-of-range rounds/counters, false clear flags, malformed observations, incompatible transit cohorts and Repair/transit/fire conflicts. Deep-clone every mutable new collection. Failed load retains the session; failed write retains the previous destination. Rollback includes sentry attrition and existing officer-recovery RNG. Combat damage, targeting, placement and morale consume no RNG and must not perturb the existing galaxy, plague, ameba or officer streams.

Before implementation, freeze genuine current rules8 campaigns/futures, including all species, core sizes, sentry-unaware founding/AI, damaged-state absence, pending fleet/queue/travel/officer/cargo/plague cases. Preserve all previous rules1..7 fixtures, worktrees, branches, recovery files and QA saves. New writer10 old-rule continuation must match those genuine futures; do not generate an old expectation using the new engine or rewrite expected hashes to obtain a pass.

## Exact proposed examples

These are specification arithmetic fixtures, not claims that product code implements combat.

1. **Simultaneous shot:** operational escort100 attacks sentry80: `24-2=22`; sentry attacks escort: `18-4=14`. End hulls are escort86/sentry58. Both fire once. In a separate case the sentry starts at hull10: its crippled attack is `floor(18*25/100)=4`, so armor reduces its shot to the minimum1. Although destroyed, it still fires that frozen shot; end hulls are escort99/sentry0.
2. **Band boundary:** escort hull50 is Damaged, attack12/speed225 without an officer; hull25 is Crippled, attack6/speed150. Hull51 is Operational and hull26 Damaged. Apply identical ratio classification to non-100 hulls (scout30 Damaged/scout15 Crippled).
3. **Threshold choice:** an escort starting hull60 takes14 from an operational sentry, ending46. On the following exchange it fires12 before armor, not24. Cautious does not yet withdraw merely at46; Press also continues. At hull25, Cautious queues withdrawal while Press may continue if cohesion remains above40.
4. **Cost of keeping together:** fleet has healthy flagship escort speed300 and crippled non-flagship escort speed150; surviving operational sentry pursuit250. Keep together exposes both, one sentry shot targets the lowest-ID armed exposed ship, then survivors travel at their new slowest speed. Detach policy lets flagship300 escape cleanly and exposes only the crippled escort150. A crippled flagship remains with the main group, so detaching a different slow ship cannot accelerate that flagship.
5. **Cohesion:** three ships start cohesion80,70,60. One non-flagship dies and a second surviving ship newly becomes crippled: each survivor loses30, becoming50 and30. Displayed cohesion30 queues Press withdrawal. No extra20 is charged for the crippled survivor; ship death and newly crippled are disjoint counts. Fleet renaming/re-creation cannot reset those values.
6. **Repair clock:** an escort arrives home at turn12 with46 hull/30 cohesion. No arrival tick. Repair during12->13 yields71/50; then96/70, then100/90, then100/100 at16. Hold resumes at16. No production is spent, and no construction output is reduced.
7. **Research timing:** Command Recovery completes on the same turn as mortal flagship loss. The snapshot lacks that equipment, so the existing50% recovery applies, not70%. Wenth retain source-body identity and their existing link timer instead of a mortal-death roll.
8. **Clearing and claims:** the last sentry and last attacking escort destroy each other. Pocket clears; neither side has a surviving combatant; no colony is granted and the human officer may be lost/recover under M8. A different empire can subsequently found only through its own ordinary observed/eligible command.

## Acceptance criteria and review gates

All proposed functional criteria below are **Not Started** until implementation. CBT-AC-012 is **In Progress** for this draft/design review, not an implementation result. Parent REQ-017 remains In Progress; the 17-row parent totals stay unchanged.

| ID | Required evidence before claiming acceptance |
| --- | --- |
| CBT-AC-001 Setup/content | Standard/Off, core sizes/species and alternate-content no-eligible failure; deterministic pocket placement without old RNG/allocator shifts; two persistent sentries; invalid stats/references/overflow/bounds reject with locations. |
| CBT-AC-002 Production | Real escort construction through ordinary and Normal/Urgent requests; all species; Organizer cost/one-completion/newly-built restriction; no free ships or hidden content gates; existing cargo eligibility preserved. |
| CBT-AC-003 Damage | Exact band boundaries, non-100 hulls, simultaneous fire/destruction, stable targets, overkill/no spill, minimum damage and speed rounding; committed travel never retimed; ship removal/cargo cleanup exactly once. |
| CBT-AC-004 Cohesion/orders | Exact survivor losses, flagship and newly-crippled distinctions, minimum aggregation, all four orders and manual override; no culture/rename/transfer/save reset; unarmed survivors and civilian emergency response. |
| CBT-AC-005 Withdrawal | Pre-contact cancellation, strict faster/equal/slower comparisons, one shared interception volley, clean versus exposed cohorts, post-damage launch speed, Repair/Hold arrival authorizations for main/detached ships, blocked-return health races, explicit new response and no unsafe retry loop. |
| CBT-AC-006 Detachment | Keep-together versus split outcomes; pre-interception membership frozen, newly crippled members retained, lost main flagship replaced only within its cohort, empty main fleet dissolved with detached survivors independent; officer location and return authority intact; no extra shots, fleet-slot dependence, missing-member crash, abandonment or auto re-attack. |
| CBT-AC-007 Repair | Exact four-tick example, arrival/completion/in-transit exclusions, closed/quarantined ports, cancelled orders, simultaneous multi-ship repair, finite completion; cargo/health/history intact, no economic minting. |
| CBT-AC-008 Integration/privacy | Current/stale observations and no foreign leaks; single-reservation multiple-human commands; peaceful AI owns its observations and skips known guarded founding; ameba/governor/plague/request safety unchanged; no ownership reward. |
| CBT-AC-009 Officers | Existing specialties/XP unchanged; combat loss/service facts once; mortal50/70% snapshot gear and two-turn timing; Last watch opt-in; Wenth2/1 or4/3 link recovery/whole-body identity; no survivor officer teleport. |
| CBT-AC-010 Transactions/compatibility | All46 explicit pairs including no9/9; genuine rules8 and older futures; every command/Ready/direct-Resolve and late-phase failure hook restores state/readiness/RNG/counters/consents/observations; corrupt loads/writes retain prior data. |
| CBT-AC-011 Runtime/usability | Actual new-game Standard/Off, build/assemble, observe, attack, change orders, withdraw both policies, repair and save/load/continue through UI; small/large-window/focus evidence; forced rare cases separately labelled from generated campaigns. Human comprehension/balance/platform/export remain separately unverified. |
| CBT-AC-012 Full design | Fresh builder check/disposition, owner reviews the complete draft and explicitly accepts/amends opponent/control model, numbers, scopes and compatibility; separate implementation-plan review follows. |

## Material decisions for the owner

The owner has approved drafting only. The following are the drafter's calls for full gameplay review, not approvals inferred from silence:

| Topic | Proposed default |
| --- | --- |
| First opponent and stakes | One fixed automated raider-sentry pocket guarding founding access; peaceful rivals and existing ameba preserved |
| Direct control | Automatic one-volley-per-strategic-turn battles, fleet orders and direct withdrawal; no tactical battlefield in this slice |
| Setup | Standard initially, optional Off; independent of Solo/rivals/Ameba |
| Construction/stat model | Basic escort24 displayed production,100 hull/24 attack/4 armor/300 speed; two80/18/2/250 sentries; no new research gate |
| Damage | Three bands with100/50/25% firepower and100/75/50% speed; armor per shot; deterministic simultaneous hits |
| Retreat/morale | Persistent per-vessel command cohesion; Cautious initial, Press/Hit-and-run/Hold choices; one possible interception volley, no interstellar pursuit |
| Stragglers | Keep together initial; optional independent return for crippled non-flagships, retaining cargo and ownership |
| Repair | On arrival: Repair initially selected, with Hold available in the same preview; applies to every returning cohort. Time-only at own legal ports, quarter-max-hull and20 cohesion/full turn; no free arrival tick or space healing |
| Scope/legacy | Rules9/writer10/46 pairs for later implementation; no migration, empire warfare, combat XP, new culture penalties or compulsory tactical UI |

Builder discretion covers names/layout within these controls, query/index/DTO organization, efficient bounded report storage and test grouping. Builder discretion does not change targeting, time/consent semantics, opponent/control model, economics, compatibility or existing owner-approved culture/Wenth behavior. After owner approval, a separate implementation plan pins exact files/interfaces, valid fixtures, phase hooks, genuine-future capture and all regression/runtime evidence before product changes.
