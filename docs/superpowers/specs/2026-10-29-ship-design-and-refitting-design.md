# Ship design and refitting

2026-10-29. **Status: In Progress — complete M10 gameplay proposal for review.** The owner said “use your own judgement” in response to a proposal to draft this milestone, delegating the starting choices below. They are drafter decisions, not previously owner-confirmed numbers. M10-P/M10-I and functional acceptance are Not Started. [ADR-021](../../DECISIONS/ADR-021-ship-design-and-refitting-design-entry.md) records the drafting authority.

## Intent and current baseline

Make ship construction a choice about cost, concentration of firepower, protection and travel speed. A player should be able to create a useful design in a few selections, understand the tradeoffs and order a finite number of ships through existing colony queues or Fleet Command. Research broadens the choices. Existing vessels keep their equipment until the player pays to refit them.

The charter includes ship design and strategic warfare. M9 supplies automatic sentry battles, persistent hull damage/cohesion, withdrawal and repair, but every escort currently has the same profile: 100 hull, 24 attack, 4 armor, speed300 and cost2400 internal production (24 displayed). Main at `89ed49aacc13f52b3c722b8752d4208af9789120` uses rules9/writer10/46 explicit pairs; [its integration evidence](../../../reports/COMBAT_INTEGRATION_2026-10-29.md) records 1,733 simulation tests and 3,271 Combat checks. Those are existing results, not M10 validation.

Retain C#, Godot presentation, the plain .NET simulation, embedded JSON content, integer mathematics, shared commands and snapshot/atomic turn publication. Retain the original space-opera direction and existing species/cultural/officer rules, including Wenth whole-body command identity. This designer equips warships; it does not turn a Wenth colony body into a disposable individual pilot.

## Scope and alternatives

| Approach | Consequence | Choice |
| --- | --- | --- |
| Research simply upgrades every escort | Few controls, but owned ships change without refitting and production has little composition choice | Reject |
| Two hull frames with a small component catalogue | Concrete firepower/protection/speed tradeoffs while reusing automatic combat | Recommend |
| Full tactical component system | Needs range, accuracy, shields, missiles, weapon targeting, facing and countermeasures | Later separate design |

This slice includes warship designs, four research unlocks, construction by exact design, equipment-only refits, queries/reports and compatibility. Scouts and all settlement/cargo roles retain their present fixed definitions. Both warship frames retain the existing logical `escort` role for cargo restrictions, role filtering and existing armed-vessel rules; frame and design labels distinguish them in views. No new founding/cargo ability is attached to a frame. In core content the logical warship role ID is escort; an alternate pack explicitly binds its declared zero-cargo, non-founding armed vessel role to the starting recipe and frames. Use that binding rather than a core ID/name string as a feature gate. The core role-only compatibility alias below likewise applies to that declared role in an alternate pack.

No empire warfare, diplomacy, invasion, bombardment, colony destruction, sentry respawning, new enemy, ameba combat, victory condition, tactical controls, combat XP, salvage, maintenance currency, trade/export of designs or automatic upgrades/replenishment. Peaceful rival behavior remains unchanged. [Peaceful mode](../../FEATURE_REQUESTS.md#fr-001-peaceful-mode) remains a separate deferred request. Existing Standard/Off hazard choices remain independent; Off still permits constructing/designing ships.

## SHP-001 Catalogue and design identity

Every rules10 empire has a private design catalogue. A saved design consists of a stable empire-local DesignId, a name, frame ID, engine ID, zero/one armor package and one/two weapon banks. Repeated bank IDs are allowed; their list is canonically sorted by ordinal content ID. There is no empty armed design, cargo module, hidden species slot or free attribute allocation. Space use must not exceed the frame capacity. Names are 1..40 trimmed characters, with case-insensitive ordinal uniqueness among that empire's retained designs. Names carry no gameplay effects.

Saving validates ownership, expected turn, all referenced definitions, unlocked technologies, space and checked stat/cost arithmetic. A successful save allocates the next local integer ID; rejection and an identical retried command allocate nothing. A client supplies an expected catalogue revision to prevent accidental duplicate publication. UI drafts remain local until Save. Editing an existing design creates a copy with a new ID and name; its saved recipe never mutates. Ship loadouts, queued construction, accepted requests and refits never follow a floating “latest revision.”

Maximum32 active designs and256 retained designs per empire, including the starter. Archive hides a design from new-build/refit pickers without altering existing references; Restore is explicit and subject to32 active. Delete is permitted only for an archived design with no vessel, colony active/queued target, active refit or retained assembly-slot reference. Historical event facts copy labels/stat summaries rather than requiring a catalogue reference. IDs are never reused; counter/revision overflow rejects unchanged. At the retained cap, explain which archived records are still referenced and permit deleting an eligible unused design. No destructive automatic eviction.

All new rules10 empires begin with exactly one permanent active **Standard Escort**, whose recipe is Escort frame + Conventional drive + Pulse bank + Plated armor. It cannot be archived/deleted. It reproduces the current escort exactly. No free new hulls, reserve, technology or officer is created. Additional suggested loadouts may be shown as unsaved drafts, but become records only through explicit Save after eligibility checks. No automatic catalogue churn on research completion. Rival catalogues remain private; the present peaceful AI does not start building warships or making refit decisions in this milestone.

## SHP-002 Components and exact calculations

These are prototype starting values, not demonstrated balance. Costs are internal hundredths of displayed production. Armour values below use the code's `Armor` statistic; it is a flat per-shot reduction under M9.

| Frame ID / label | Hull | Space | Hull cost | Travel factor | Availability |
| --- | ---: | ---: | ---: | ---: | --- |
| `escort_frame` / Escort | 100 | 6 | 1200 | 100% | Starting |
| `frigate_frame` / Frigate | 180 | 10 | 2400 | 75% | Frigate Construction |

| Component ID / label | Space | Cost | Effect | Availability |
| --- | ---: | ---: | --- | --- |
| `conventional_drive` / Conventional drive | 0 | 400 | Engine speed300 | Starting |
| `vector_drive` / Vector drive | 0 | 800 | Engine speed400 | Vector Drives |
| `pulse_bank` / Pulse bank | 3 | 600 | Attack24 | Starting |
| `lance_bank` / Lance bank | 4 | 1000 | Attack36 | Focused Emitters |
| `plated_armor` / Plated armor | 2 | 200 | Armor4 | Starting |
| `reinforced_armor` / Reinforced armor | 3 | 500 | Armor8 | Reinforced Laminates |
| No armor package | 0 | 0 | Armor0 | Starting |

Exactly one engine is mandatory and its zero space cost does not permit a second engine. At most one armor package. Banks have no range, ammunition, accuracy, damage type or independent target. Total attack is the sum of their attack values; each ship still fires one M9 shot using that total. This avoids silently granting a second target or a second application of armor reduction.

Base cost = frame cost + engine cost + armor cost + sum(bank costs). Max hull = frame hull. Base armor = selected package armor or0. Base travel speed = max(1, floor(engine speed * frame travel factor /100)). Apply M9 hull-band attack/speed reductions and the approved Navigator bonus in the existing order: floor base speed by the damage percentage, then floor that damaged speed by (100+Navigator bonus)/100, minimum1. Fleet travel uses its slowest effective member; already committed travel keeps its frozen speed/duration. Armor does not degrade at a hull threshold, matching M9.

All arithmetic is checked with wide intermediates; no float, random draw or new RNG stream. Core content is under existing string-ID conventions. The effective catalogue of frames/components and prerequisites is embedded in saves. Alternate content must declare a valid starting escort recipe and legal bounded component definitions; do not hard-code the core names into mechanics. Missing, contradictory, negative/overflowing, duplicate-ID or unresolvable definitions fail content load with an actionable path/field error. Keep Load available when disk content is invalid. A pack can omit optional advanced technology/components together; any declared reference must resolve.

### Worked choices

| Loadout | Cost | Hull / attack / armor | Base speed | Space |
| --- | ---: | --- | ---: | --- |
| Standard Escort: conventional, one pulse, plated | 2400 (24 displayed) | 100 /24 /4 | 300 | 5 of6 |
| Fast Escort: vector, one pulse, plated | 2800 | 100 /24 /4 | 400 | 5 of6 |
| Gun Escort: conventional, two pulse, no armor | 2800 | 100 /48 /0 | 300 | 6 of6 |
| Armored Frigate: conventional, pulse+lance, reinforced | 4900 | 180 /60 /8 | 225 | 10 of10 |

An Escort with a lance and reinforced armor needs7 space and rejects. Two lances plus reinforced armor on a Frigate need11 and reject. Unused space grants no hidden bonus. A healthy Standard Escort hits a sentry with armor2 for22. A healthy Gun Escort hits it once for46, not twice for22. The Gun Escort's armor0 takes18 from each healthy attack18 sentry; Standard takes14. A 50/100 Standard Escort is Damaged, attack12 and base damaged speed225; at25/100 it is Crippled, attack6 and speed150. A healthy Frigate with a conventional drive has speed225, below sentry pursuit250, despite its larger hull. These examples expose tradeoffs, not optimal fleet advice.

## SHP-003 Research

| New technology ID / label | Internal research cost | Prerequisites | Unlock |
| --- | ---: | --- | --- |
| `focused_emitters` / Focused Emitters | 2400 (24 displayed) | `measurement_methods` | Lance bank |
| `reinforced_laminates` / Reinforced Laminates | 2400 (24 displayed) | `industrial_coordination` | Reinforced armor |
| `vector_drives` / Vector Drives | 3600 (36 displayed) | `industrial_coordination` | Vector drive |
| `frigate_construction` / Frigate Construction | 3600 (36 displayed) | `industrial_coordination` | Frigate frame |

Use the existing manual research selector, reserve, prerequisites and one-completion-per-empire boundary. Unlocks are available from the next planning snapshot. They never change an equipped ship, a saved recipe or a committed trip. Locked choices may be inspected in the designer with prerequisites/costs; saving an unavailable recipe rejects, and new-build/refit targets must reference a design already legally saved by that owner. No weapon upgrade is silently applied to old designs. These new technologies exist only in rules10 effective content; older campaign research remains unchanged.

## SHP-004 Construction and colony queues

New construction explicitly selects an owner-local saved DesignId. Active project, finite queue token, forecast, completion plan and created vessel carry the same pinned identity. A warship's economic role remains `escort`, but no queue slot resolves its recipe by mutable name. Before selection show the full undiscounted cost, effective request discount if any, reserve/output estimate, space/stat breakdown and owner-visible unavailable reasons. Reordering/replacing a project uses existing manual authority, preserves colony reserve and identifies any affected request. Completing a ship pays once, creates one full-hull/cohesion100 independent vessel or its exact reserved request binding, and clears only the matching project/queue token. No same-boundary departure, battle or extra completion is granted.

Retain at most one building, vessel or refit completion per colony per boundary and the32-entry finite queue bound. Repeated design entries mean an explicit finite number of ships. An archived but already bound design may finish; new selection of it rejects until restored. A template still contains buildings only. Governors never invent a design, build arbitrary warships, refit, research or upgrade a queue. Manual project changes pause construction automation as before; manually ordered ship queues and existing workforce automation remain under M5 authority. No portable design format is added to building-template presets.

For a rules10 command/API that still specifies only the existing `escort` role, resolve and pin that empire's Standard Escort at command acceptance; it is never a best/latest design selector. This applies to compatible role-only colony targets and request inputs. UI must display the resolved recipe before Apply. Civilian/scout targets are unchanged. In saved rules10 state all warship build references are explicit design references; an ambiguous persisted role-only custom build rejects. The implementation plan will choose the typed target representation while preserving these semantics.

## SHP-005 Fleet Command and assembly requests

The player requests a finite quota of exact designs, alongside unchanged civilian/scout quotas. All accepted warship slots pin DesignId. Existing idle candidates must match the exact design, not merely the `escort` role, before they can fulfill that slot. A different loadout may be offered as a separately labelled candidate only after an explicit request edit that changes the quota; never silently substitute it, refit it or buy it. Maintain explicit cross-page selections and stale-candidate removal disclosures.

Normal/Urgent priorities, participating colonies, rally, manual protection, active-project retention, reservations, safety pauses, finite authority and current slot limits remain M8. Update all estimates and bind/reconcile paths for full design identity. Request edits show what remains built/committed and what changes only unstarted slots. Existing completed ships never change equipment from a request edit. If the player wants another design after a hull is already built, use an explicit quota edit/release or a separate later refit; no free fulfillment conversion. Archive never changes accepted request identity; referenced catalogue records cannot be deleted. Once a request is Complete, its original slot recipe is a historical fulfillment fact: a later explicit refit may change that vessel without invalidating the completed request, granting another XP award or rewriting the slot. Active request bindings must still match their exact design.

The Organizer discount remains5/10% only for eligible request-bound new construction at the resolution snapshot. Apply ceiling once to the full design cost: max(1, (cost*(100-discount)+99)/100). Standard Escort is2280/2160; Gun Escort is2660/2520. No component-level rounding, discount stacking, discount on refits or new XP event. Existing one-XP-per-eligible-new-hull fulfillment and all deduplication/forfeiture rules remain. Role filters still match both frames as warships; show design/frame labels and an optional own-design filter without revealing rival recipes.

## SHP-006 Refit commitment and price

A refit replaces components within the same frame. Cross-frame conversions reject, even if cheaper. The vessel must belong to the issuer, be stationary at a selected owned colony system, have zero cargo, full current hull and cohesion100, and be outside transit, pending move/founding, ameba activity, pending/active/withdrawal/return combat, Repair and active assembly/rally reservations. The selected target design must be owned, active, unlocked, valid and have the same frame. Identical equipment is “already equipped”; another design name/ID alone does not justify a paid refit or a fake completion event.

An own named fleet is allowed only while in Hold. The refitting vessel keeps its fleet, flagship and officer identity. Commands that move it, include it in another task, change its membership, change its flagship/officer assignment or start combat reject until explicit Cancel or completion; a fleet order encompassing it fails atomically. Unrelated eligible members may be split out through existing ordinary controls. A refit never posts, recovers, replaces or teleports an officer. Cultural recovery/Last watch and Wenth source identity remain unchanged.

Use one active refit at a time per colony and one per vessel. The selected colony must have no active production project. If it has one, reject with “finish or explicitly clear the current project first”; never clear work or take over an assembly contribution implicitly. After acceptance, refit occupies that colony's active production slot, pauses its construction automation and retains its unstarted queue/template/workforce settings. Refits are manual active projects only, not deferred queue entries or automatic standing orders. Other colonies at the same system can operate their own slots independently.

Refit price = max(1, ceil(target undiscounted design build cost /4)), charged to the selected colony production reserve once at completion. Charge for replacing/installing equipment, without refunds or salvage credit for removed equipment. It applies to upgrading, downgrading and lateral changes, and never receives Organizer or other new discounts. Full hull/cohesion eligibility prevents using refits as healing; same frame preserves max hull. Cancel consumes nothing because there is no separate spent partial-progress account: production is banked in the existing colony reserve until the one completion payment.

Preview names the ship, old/target designs and exact component/stat changes, port, cost, forecast, production-slot use and locks. Its revision binds vessel equipment/condition, target recipe, port project/ownership, relevant technologies and turn; do not include unrelated hidden state. Apply revalidates; no stale preview, partial bulk acceptance or late target substitution. Repeating the identical active refit is an accepted no-op preserving readiness and IDs. A request for a different refit while one is active rejects until Cancel. Ordinary changed commands clear issuer readiness; all-human Ready remains atomic.

## SHP-007 Refit resolution, cancellation and interaction

At acceptance, pay nothing and install nothing. An eligible ship already present for the resolution snapshot can finish no sooner than the next boundary, after one full stationary interval and enough production. The port banks its ordinary net production. Its refit shares the ordinary one-completion slot; it cannot also finish a building/ship. Snapshot availability/cost/condition drive the forecast. Refitting makes no random draw, grants no hull/cohesion change and has no XP/officer recovery effect.

Treat the planned payment as provisional until the refit completion phase, after existing health/loss/combat/movement/repair results and before final observations/report validation/publication. Revalidate that the same ship survives, remains eligible at that port with that job and unchanged old equipment, and the candidate reserve covers the frozen cost. Then deduct exactly once and replace only the DesignId/equipment reference. A completed engine/armor change affects only the next snapshot: no earlier volley, committed movement, withdrawal, repair or previous estimate is repriced. Existing combat receipts remain immutable facts of the loadout actually used.

Archiving a target after refit acceptance does not invalidate that bound job or require restoring it; the recipe remains immutable and referenced. Insufficient reserve means the job continues, old equipment remains and an updated finite estimate is shown. If a supported health/loss or authoritative cancellation removes its ship, cancel that job without a refit payment, retain banked reserve at a surviving port and publish one owner-only Attention fact. Other eligibility loss pauses the job with its reason and no payment; a new explicit Resume checks a fresh preview. No automatic retargeting or substitute vessel. This does not add colony destruction/capture; any such future subsystem must define the fate of the colony and its own reserve. Invalid candidate state or arithmetic failure fails the entire turn, preserving the original state/readiness/RNG/IDs/reserves and all jobs.

Cancel unlocks the ship, retains its old design/identity/full condition and all banked production, clears only that manual refit project and leaves construction automation paused. It neither deletes unstarted queue entries nor automatically moves/attacks/resumes a fleet. Explicit movement, project replacement and queue actions must first cancel the refit; the UI offers that named action rather than silently cancelling through another command. Completion similarly ends in Hold with construction automation paused; resuming a retained queue/template is a separate explicit choice. Reports distinguish accepted, blocked/paused, cancelled and completed without requiring repeated acknowledgement.

Example: refitting Standard to Fast Escort costs700 internal production (7 displayed). With reserve300 and net output200, acceptance at turn10 installs nothing; turn11 ends at reserve500, old speed300; turn12 banks to700, pays700, keeps the same VesselId/hull/cohesion/officer and installs speed400 for subsequent snapshots. Cancelling at turn11 leaves reserve500 and the Standard Escort. Refitting a damaged vessel rejects until ordinary time-only repair has actually restored hull/cohesion; a newly arrived ship receives no free refit interval in its arrival resolution. A4900-cost target refit costs1225, not1224; no hidden display rounding changes payment.

## SHP-008 Presentation, scale and privacy

Add a **Ship designs** entry from Fleet Command and the colony warship picker. The catalogue shows name/frame, equipment summary, cost, speed, hull/attack/armor, active/archive status and reference counts. Use bounded pages of16 designs with explicit filters and stable local ordering; retain selection when possible. Open a design to inspect or Copy and edit. A local draft chooses frame/engine/armor and up to two banks, with live simulation-supplied space/cost/stat breakdowns, locked prerequisites and errors. Save is explicit and disabled with a readable reason if invalid. Back/Cancel discards an unsaved draft without changing the game.

Build/request pickers show exact saved identity and count; a completion/report identifies its chosen design and frame. Archive/Delete provide affected-reference information; Delete is disabled when referenced. Owned ship inspection shows installed equipment, including archived designs, damage bands and actual effective speed/firepower. A refit picker compares old/target on the same frame and shows port work/cost/locks before Apply. Refit jobs are visible from Fleet Command and colony production with Cancel/Resume where legal. Existing30-ship bulk repair remains repair, not an implicit bulk-refit command.

At800x600 and1920x1080 keep controls within scroll/container bounds, keyboard focus reachable and the final Apply/Save actions accessible after scrolling. Use the shared theme/type variations with no new hard-coded styling or external art. Screenshots and in-process UI signals support automated checks; they do not establish physical input, comfortable reading, accessibility or aesthetics. Layout details remain builder discretion within these semantic requirements; no mockup is an accepted pixel specification.

Own queries expose only own catalogue/jobs/references/production and ordinary observed combat facts. Rival observations retain existing disclosure: no catalogue, bank/engine/armor recipe, research, queue, refit, admiral or hidden current condition merely because a ship is observed. Historical observed role/appearance knowledge is not upgraded by inspecting our designer. Compare otherwise equal owner views after changing hidden rival recipes/reserves/refit orders/technology; results must be identical. No new remote sensor, recipe intelligence or fleet access.

## SHP-009 Rules, saves, determinism and transactions

Proposed new campaigns: **rules10; writer11**. Preserve all46 accepted pairs unchanged and add only `(11,1)` through `(11,10)`, totaling56. Do not add `(10,10)` or other inferred cross-products. Writer11 may save rules1..9, omitting every M10-only definition/state/field. Old rules9 escorts keep their original role-based profile/cost/speed, research, commands and future outcomes; they do not acquire a catalogue or refit path on load. No automatic upgrade/import of an old campaign. A rules10 game with sentries Off still requires valid ship-design content/state.

Persist effective frame/component definitions and references, catalogue IDs/names/archive flags/recipes/counters/revisions, per-vessel design identity, pinned project/queue/request references and complete refit job/resume/price/port/ship/turn/revision state. Derived stats/costs are recomputed from embedded definitions; any persisted summary/receipt must agree with its relevant frozen source. Old historical receipts preserve the facts they recorded rather than being recomputed from later equipped designs. Clones and every planning/context/view copy must preserve the added identity; role-only fallback must not leak into custom-profile queries, travel, founding restrictions, repair or combat.

Strictly reject duplicate/unowned/dangling design IDs, invalid frame-component combinations, unearned unlocks, overcapacity, inconsistent production/request targets, cargo-capable custom warships, invalid bounds/counters/revisions, multiple jobs sharing port/ship, active jobs with contradictory installed/target equipment or unsupported schema/rules. An archived reference is valid if previously bound; archive affects new acceptance only. Corrupt writes/loads retain existing session/files under current atomic save rules. Empty/Off/alternate content and all generated species must receive consistent initialization without using species-name string branches.

All new commands use issuer, expected turn and relevant revisions through the common processor. Rejection and valid no-ops leave state/readiness, ID counters and every RNG stream unchanged; changed planning edits publish a validated clone and clear only issuer readiness. Direct Resolve and every Ready path validate the same job/equipment/consent invariants. Repeated Ready/load/replay cannot double-charge, generate a new design/ship, duplicate XP or complete twice. All new phase failures restore the entire original state, including a Ready command that triggered resolution.

Before any future product change, freeze genuine current rules9 campaigns and their futures using this unchanged engine. Include Standard/Off, all four human species/core sizes, pending/active/withdrawing/returning combat, damage/cohesion/repair, live queues/requests/officers, cargo/plague/ameba, and alternative embedded role names. Reuse previous genuine rules1..8 archives. Compare old future states after removing only the intentional schema-version field; never manufacture expected old behavior with the new implementation or rewrite oracle hashes. Preserve original source saves, failed runs, raw reports, QA saves, all8 worktrees/branches and recovery material.

## SHP-010 Acceptance and handoff

Functional rows are **Not Started**. M10-D and SHP-AC-012 are **In Progress** until the full written gameplay description is reviewed. M10-P/M10-I are **Not Started**. Parent REQ-017 and the17 parent-row totals do not change through this draft.

| Criterion | Required proof before local automated acceptance |
| --- | --- |
| SHP-AC-001 Content/setup | Exact core tables/starting recipe, original escort equality, alternate content and all human species/Standard-Off; missing/invalid content leaves Load available. |
| SHP-AC-002 Catalogue | Save/copy/archive/restore/delete,32/256 bounds, unique names, immutable pinned identity and references, repeated/stale commands, no ID/RNG effects on rejection/no-op. |
| SHP-AC-003 Calculations | Every worked choice; illegal capacity; summed banks fire once; armor once; max hull/damage percentages, Navigator order, mixed fleet minimum and frozen travel; boundary/overflow cases. |
| SHP-AC-004 Research | Four exact costs/prerequisites; no same-boundary unlock effect; locked save rejection; no automatic refit/queued upgrade, old research unchanged. |
| SHP-AC-005 Production | Pinned direct/queued/role-only targets, paid exact design, finite repeat counts, active-project retention, archive after acceptance, one completion/colony and retained reserve/queue/token authority. |
| SHP-AC-006 Requests/officers | Exact-design quotas/idle matching,32-row assembly paging/selection, priority/edits/safety/cancellation, single Organizer ceiling, immutable built slots, original XP and Wenth policies. |
| SHP-AC-007 Refit acceptance | Same-frame/full-condition/zero-cargo/owned port gates, fleet Hold/locks, idle production slot/manual pause, stale preview/no-op, no implicit repair/detach/cancel/discount. |
| SHP-AC-008 Refit economics | 700-cost two-turn example,1225 ceiling, minimum full interval, provisional payment/revalidation, cancellation/reserve preservation, blocked Resume, no free hull/cohesion/XP and no second colony completion. |
| SHP-AC-009 Integration/privacy | All equipped-profile call sites, all combat phases/repair/transit, identical owner views under hidden rival changes, peaceful AI remains bounded, no retroactive receipts/equipment. |
| SHP-AC-010 Persistence/atomicity | All56 explicit pairs/no10-10, strict corrupt states, actual Main reload for catalogue/queue/request/refit states with three future-turn equality, every new command/phase/Ready rollback and genuine old futures. |
| SHP-AC-011 Runtime/scale | Actual designer/build/request/refit controls at800/1920;32 active/256 retained catalogue boundary, archive references, current assembly paging and refit blocked/Cancel/Resume; all9 retained scenarios plus new opt-in ShipDesign scenario, real negative control and ordinary10+2 exclusions. |
| SHP-AC-012 Full design | Read-only builder check with disposition and owner review of the complete gameplay description, explicitly distinguishing drafter defaults from prior confirmed choices. |

Later implementation evidence must separate genuine generated paid campaigns from labelled pre-session supplements. Require at least four generated rules10 core campaigns, one per human species: paid research, saving an advanced design, production and exact design request fulfillment, sentry combat with the resulting profile, normal repair, paid same-frame refit and a subsequent volley/trip using the changed profile. Use actual controls/commands and ordinary Main Save/Load; do not inject earned research, production, hulls, repairs, resources, wins or unlocks. The finite maps may be supplemented for caps, unusual losses, unsupported mutation/failure hooks and multi-human/privacy cases; label those honestly. No combat result, research deadline or economic pace here claims general balance.

Keep one full simulation gate and affected/full native regression gates in the later plan. Add a distinct opt-in ShipDesign runtime scenario, bringing ordinary exclusion checks to10 scenarios and the existing2 helpers; preserve the real opt-in negative-control requirement and rebuild the normal solution even on failed validation. One final fresh whole-branch implementation review/one correction pass and retained Native execution follow the separate plan's approval; this design's builder check is not that implementation review.

Builder discretion covers type/file organization, indexes/query DTOs, exact typed target representation, accessibility labels, focus order and container layout, original component names, and grouping of tests. It does not change the numeric defaults, loadout limits, shot model, refit price/authority, compatibility or owner-confirmed M8/M9 choices. No product implementation or separate implementation plan is part of this drafting turn. Next is review of the complete proposal; the owner's delegated judgment settles the proposed defaults without another round of preliminary questions.
