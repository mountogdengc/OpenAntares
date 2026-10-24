# Colony development, governors and planetary identity

**Status: In Progress — proposed full design for owner review, not approved for implementation.** Direction/drafting authority: [ADR-011](../../DECISIONS/ADR-011-colony-development-design-entry.md). M5-D is design only; M5-P/M5-I have not started. Current product remains rules4/writer5 at main10cb0b4. Proposed numbers, defaults, bounds and version changes below are the drafter's calls, open to owner amendment.

## 1. Intent and authority

The owner enjoys choosing foundational buildings that contribute without assigning population to every output, then giving worlds a purpose. Managing many worlds should allow delegation, with reports instead of constant routine prompts. Delegation should be useful as empire size grows, without taking control away from small empires or favorite worlds. Default templates must be editable/copyable and players must be able to create their own. Industrial, growth and research are suggested starting roles, not an exhaustive or mandatory taxonomy. The owner explicitly asks the designer to consider better alternatives rather than treating every example as a final decision.

The owner also enjoys inspecting planets and wants visual variety within a planet type. Revolts, plagues and other colony-loss/spreading threats should warn early enough to respond, interrupt by default and offer multiple responses with consequences; cultural reactions should vary. Fleet intent/retreat doctrines and scouts attempting escape are retained as [future requirements](../../design-notes/2026-10-24-delegation-crises-and-fleet-intent.md), not silently included in this implementation slice.

Preserve [charter](../../CHARTER.md), [engineering rules](../../../CLAUDE.md), accepted [species](2026-10-23-species-mechanics-design.md) and [rival](2026-10-24-rival-expansion-design.md) contracts, ordinary commands, integer economics, snapshot timing, atomic publication and embedded campaign content. Preserve owner roster/research edits and peaceful-mode backlog. This is an original classic space-opera game, with no copied commercial assets or names. Uncoached human acceptance remains Deferred; no participant recruitment.

### Bounded deliverable

A player can develop a colony personally, queue construction, copy a sensible template, customize it, delegate workforce and/or construction, inspect the reasons for governor actions and recognize individual worlds. The same workflow remains usable with thirty owned colonies. There is a small next tier of development to make construction priorities matter. This is not a complete technology tree or a completed strategic game.

Non-goals: new population biology, upkeep/currency/tax/trade systems, automatic colonization or vessel movement, automated empire research selection, AI difficulty/personality redesign, leaders with skill bonuses, revolt/plague simulation, destructive crisis responses, warfare/fleets/combat, diplomacy, multiplayer transport, victory, peaceful mode, packaging or release. No implementation/scaffolding/dependency/art changes are authorized by this draft.

## 2. COL-001 — Foundations and development choices

Keep all current building costs/effects, technology costs, worker/species rates, growth/capacity/support rules and starting assets. Start without free buildings. Buildings have ordinary construction cost and contribute flat effects without dedicated workers; they remain subject to existing support-shortage scaling. Species worker multipliers do not multiply flat building bonuses. No new upkeep system is implied by the owner's informal description of free points.

The default foundation list is Fabrication Hall, Cultivation Hub, Analysis Lab, in that order. A plan waits for or works around unavailable technology as defined in COL-003; foundations are a preference, not a forced prerequisite or a promise that all three are unlocked. Habitat Extension remains available. Players may reorder, omit or replace these preferences.

Proposed small content addition, all costs below in displayed points (multiply by100 in JSON):

| Technology ID / name | Cost | Existing prerequisites |
| --- | ---: | --- |
| industrial_coordination / Industrial Coordination | 24 | measurement_methods |
| cooperative_science / Cooperative Science | 24 | measurement_methods |
| adaptive_habitats / Adaptive Habitats | 24 | cultivation_methods and habitat_methods |

| Building ID / name | Cost | Required technology | Flat effects |
| --- | ---: | --- | --- |
| production_complex / Production Complex | 32 | industrial_coordination | +3 production |
| discovery_center / Discovery Center | 32 | cooperative_science | +3 research |
| habitat_network / Habitat Network | 28 | adaptive_habitats | +2 support; +2 capacity |

All seven buildings remain one per colony, independently legal; there is no exclusive specialization slot, demolition requirement or permanent colony class. Choosing a focus changes priorities and workforce, not construction permissions. All three additional technologies can eventually be researched; the tradeoff is time/resources and which opportunity is pursued first. Existing breakdowns show every contribution. Core content remains JSON, embedded with the campaign; no effect requires a special case keyed to its name.

These are playable prototype values, not proven balance. Verify conservation/species/capacity interactions, including Wenth maturity at its new current capacity, rather than asserting universal optimality. The new tier is not retroactively inserted into embedded older campaigns.

## 3. COL-002 — Templates and delegation authority

Provide four data-defined defaults. Balanced is an additional drafter recommendation for ordinary worlds; it is not claimed an owner-selected role.

| Template | Support target | Production:research worker weights | Development preferences after foundations |
| --- | --- | --- | --- |
| Balanced | minimum support | 1:1 | habitat_extension, production_complex, discovery_center, habitat_network |
| Industrial | minimum support | 3:1 | production_complex, habitat_extension |
| Research | minimum support | 1:3 | discovery_center, habitat_extension |
| Growth | capped-growth support below capacity; minimum at capacity | 1:1 | habitat_extension, habitat_network |

Defaults are suggestions, not governors that know a universally best strategy. A template contains a stable local ID, display name, ordered foundation/development building lists, support-target mode and two integer weights0..100 with positive total. Names are1..80 characters; lists each0..32 IDs with no repeats across both lists. Defaults are immutable originals: editing copies one to a named custom template. At most64 custom templates per empire; deleting a referenced template rejects with affected colonies shown. Its library is private to that empire. Editing a template creates a new revision; colonies keep their assigned snapshot until an explicit atomic Apply to selected colonies confirms the before/after preview. No hidden live propagation.

Template IDs, revisions and queue tokens use deterministic local counters, not wall time, GUIDs or the global entity-ID allocator. Counter overflow rejects unchanged. References must resolve against that campaign's embedded content. A valid alternative content pack supplies appropriate defaults or a building-free Balanced template; no Harl/Peldra or core-building fallback is hard-coded into governor logic. Unknown references in a declared content template are actionable validation errors; existing Load recovery remains available even if disk content is invalid.

Workforce automation requires an assigned template; construction automation may use an explicit queue without a template. Libraries retain the latest custom revision plus assigned snapshots, not an unbounded revision history.

Each colony independently enables workforce automation and construction automation; both start **off**. A selected template can remain visible while a colony is manual. Enabling, pausing or resuming never grants economic bonuses. Players can delegate at any empire size and retain favorite worlds manually. At6 owned colonies show a nonmodal suggestion; at11 highlight bulk application in the same management view. These are guidance thresholds, not a forced switch, paid unlock or repeated modal prompt.

Empire new-colony inheritance is off initially. A player may explicitly choose an assigned template snapshot and which automation switches future colonies inherit. A newly founded colony first uses the already-approved founding/cargo/workforce contract; inherited orders can act only at its next planning/Ready boundary. Existing colonies do not change when inheritance changes.

Governors may choose permitted buildings, reassign their own colony's workers, skip unavailable optional work, bank production, and report exceptions. They cannot pick empire research, construct population-carrying vessels, embark population, move/found with vessels, scrap anything or carry out destructive measures. A finite vessel queue entry or retained manual vessel target is such a player order; it may complete through ordinary rules, while governors never invent vessel orders. Manual workforce changes pause workforce automation only; manual project changes pause construction automation only. Queue/template assignment is retained. Resume explains whether an existing manually chosen project will finish first; it never silently cancels it.

## 4. COL-003 — Construction queues and priority

Add a per-colony ordered queue of at most32 finite entries. Entries have unique stable local tokens and typed targets (building or vessel). Unknown IDs, invalid kinds, duplicate building entries, already-completed buildings, invalid ownership, stale turns, oversized lists or repeated tokens reject the whole edit. Repeated vessel entries are legal because each is an explicit finite player instruction. No repeat-forever or automatic replenishment in this slice. Population/cargo/role requirements remain ordinary completion rules; templates may contain buildings only.

Show locked but defined targets and their prerequisites. Locked building entries may be queued for later. On preparation, retain a valid active manual/queued project until completion or explicit replacement. Otherwise choose the first currently legal explicit queue entry. If none is legal, examine missing foundation buildings in template order, then development preferences; choose the first legal one. A locked earlier item stays pending, is labelled with its unmet prerequisite and does not prevent a later legal item. Explicit queued work always outranks template work. Never replace an active vessel merely because embarkation is currently blocked.

When neither queue nor template offers legal work, bank production using the existing reserve; do not secretly pick research, build arbitrary ships or invent a substitute project. Report the specific missing prerequisites, finished plan or blocked embarkation. Queue selection operates only if construction automation is enabled; a manual colony can still edit a queue and explicitly enable queue-only execution without workforce control or an active template. Queue-only execution is the construction switch with no template assigned.

Completion consumes exactly the matching active queued entry when it came from that queue; other queued entries remain. Completing a building manually also removes its now-satisfied pending building entry. At most one project completes per colony per turn, regardless of banked reserve. Next selection is at the next preparation boundary, never a same-turn cascade. Clearing or replacing a project retains the existing colony production reserve. Pausing does not erase pending orders. Queue edits that replace the active entry require a preview and explicit Replace action; preserving an active entry retains its binding.

## 5. COL-004 — Exact workforce policy and examples

Use own-colony facts and the shared scoped forecast. No empire-wide calculator that reads hidden foreign economies is permitted for governor decisions. All arithmetic is checked integer/Int128; governor decisions consume no RNG. Let N be snapshot population, d support required per population, b flat completed-building support and r planet support per worker. Choose the minimum support-worker count S in0..N meeting the target. For r>0 use the clamped ceiling of (target-b)/r; for r=0 use S=0 if b meets target, otherwise S=N. If all workers cannot meet support, assign all to support and report an unresolved shortage, with no invented death or revolt mechanic.

Minimum target=N*d. Capped-growth target below current capacity=N*d + species.MaxSurplusGrowth*species.SupportPerGrowthPoint. At or above current capacity, Growth uses minimum target; it does not waste workers chasing blocked growth. Completed capacity effects and Sulan habitat mode feed the existing capacity calculation. No deficit is deliberately accepted to maximize output.

If the active explicitly player-ordered vessel carries population, raise S to at least min(N, that role's embark population) before splitting, as required by ordinary embarkation. This does not bypass minimum population, current-capacity maturity or other completion conditions. If those remain unmet, retain the project and report why; do not steal population or cancel it.

Allocate remaining N-S workers between production and research by the template weights using largest remainders: floor both shares; give the remaining worker to the larger fractional remainder, ties to production. While an active or selected legal project has zero flat production, reserve one available non-support worker for production if the weighted allocation otherwise gives it zero. The floor is applied by moving one research worker, never by taking required support. If no project is active/eligible, no production floor applies. Flat bonuses require no assigned worker. Unassigned template weights and thresholds have no species-ID special cases.

Exact fixtures, Verdant core rates and ordinary completed foundation buildings (Hall+Hub+Lab), all technologies needed for the compared project known:

| Case | S/P/R | Net production/research (displayed) | Explanation |
| --- | --- | --- | --- |
| Harl N6, Balanced | 2/2/2 | 5 / 5 | flat support2 means S2 covers required6 |
| Harl N6, Industrial | 2/3/1 | 7 / 3 | 3:1 across four remaining workers |
| Harl N6, Research | 2/1/3 | 3 / 7 | 1:3 across four remaining workers |
| Harl N6, Growth, capacity10 | 3/2/1 | 5 / 3 | support11, required6; growth bonus capped at0.80, total0.90 |
| Harl N10, Growth, capacity10 | 3/4/3 | 9 / 7 | minimum support at capacity; no growth |
| Peldra N6, Industrial | 4/2/0 | 13 / 1 | flat effects are not multiplied by3 |
| Harl N6, Industrial plus Production Complex | 2/3/1 | 10 / 3 | extra flat3 after worker factor |

Support0-rate, already-supported0-support-worker, one-population, insufficient support, missing project, locked plan, edited content, Sulan native/sealed and Wenth capacity-change cases must have literal expected results in tests. Algorithms may use derived breakpoints rather than enumeration; do not enumerate every workforce triple as population grows. The shared formula and ordering above are the behavior, not an implementation suggestion.

## 6. COL-005 — Commands, preparation and failure atomicity

Add empire-issued, expected-turn-checked operations for creating/updating/deleting templates, assigning snapshot plans and switches to selected owned colonies, editing queues, changing new-colony inheritance and resuming automation. Validate an entire batch before publication, in colony-ID order; a foreign/missing colony or invalid reference rejects every member. Preview is read-only. Repeated no-op commands preserve state/reference/readiness as existing commands do. Real planning changes clear that issuer's Ready as today. Internal type/file names and decomposition are builder choices; all semantic operations use the shared command processor.

**Preparation boundary:** under proposed rules5, a Human issuer's transition from Ready=false to Ready=true prepares only that empire's enabled colonies, in colony-ID order, on a private candidate before marking it ready. Select construction, then calculate workforce against the selected project, using ordinary validated commands. At most one changed target and one changed workforce command per owned colony (budget2*C); command no-ops are omitted. Queue bindings and template metadata changes are validated within the same transaction; normal completion/queue advancement is a resolver effect. Automatically generated orders preserve the enabled switches; only direct manual changes pause the affected automation channel. Shared command ownership, turn, legality and cost validation is never bypassed to distinguish that provenance. No recursive Ready commands. A Ready=true no-op never prepares again. Withdrawing readiness leaves already-published planning orders, matching current planning semantics; resubmitting recalculates from current state without duplicate reports.

A nonfinal human Ready publishes that human's prepared planning plus readiness. On the last human Ready, preparation, existing rival planning, economy, completions, growth, movement, founding, observations, reports and final validation are one transaction. A failure returns the exact state before that Ready request, including readiness, queue tokens/bindings, reports, workforce/projects, ID counters and RNG. Another human's previously accepted planning is part of that original state and remains intact. All existing turn timing remains unchanged after preparation. No UI callback, rendering or save/load operation issues governor actions implicitly.

Existing AI expansion_v1 remains the sole owner of AI orders. AI colonies cannot enable these player governor switches/queues in this slice; commands and saves reject that conflict. New rules5 AI can use all ordinarily legal new technologies/buildings via its existing data/effect-ranked policy; it receives no free content, information or new personality. HasRivals must explicitly include rules4 and5; species/expansion predicates also include5. Old rules4 retains its exact policy/input contracts. Do not run two controllers over the same colony. Tests cover active rules5 AI, rules4 genuine traces and Human/Human/AI readiness.

Unexpected rejected automatic commands, budget/ownership violations, arithmetic/counter overflow or final invalid state abort preparation/turn. Public failures are generic and reveal no other empire's facts. Legal but unavailable work is an expected waiting result, not a failed turn. Preserve shared validation rather than accepting a partial plan to keep the turn moving.

## 7. COL-006 — Management, reporting and notifications

Extend the colony list into an empire management view showing each own colony's planet portrait, current template/manual status, workforce/construction switches, current/next project, output and exceptions. Support selection, filters and atomic bulk plan assignment; keep individual inspection and direct control. Show the effect of a proposed template on workforce, project and existing reserve before application. All four species and alternate content remain readable without relying solely on color or icons.

A governor report says what changed, why, what could not be done, and what needs player direction. Aggregate unchanged/healthy colonies; expand details on demand. Queue completion, capped growth, support correction, unmet technology and waiting embarkation are existing facts, not fabricated crises. Preserve a structured, owner-only last10 completed-turn governor reports in the rules5 campaign; changes are ordered by colonyID and action kind. Re-preparation in one planning turn replaces that turn's pending action summary, never appends duplicates. Persistent unresolved issues are deduplicated by colony/reason and clear when resolved. Failed transactions add no report. Missing human attention must not be disguised as success.

Provide local notification preferences per category: Summary (digest), Attention (persistent badge/digest) and Off for routine completion/governor-management messages. Defaults: routine actions/completions Summary; unresolved shortages, blocked plans and embarkation Attention. These current conditions do not destroy colonies under existing rules and do not block End Turn by default. Preferences do not change the authoritative simulation or erase report records. Store them in the local user settings, not as empire facts available to rivals; resetting/loading a campaign cannot suppress its derived unresolved issues.

The owner's Emergency policy is preserved in the linked future brief: potentially losing a colony or spreading harm interrupts before the next turn by default and leaves meaningful reaction time. Plagues/revolts and their acknowledgement/state machinery are not implemented here merely to exercise a dormant warning framework. Do not advertise active crisis protection before those mechanics exist. Their later design must honor configurable notification levels without silently weakening the default or allowing a governor to make unapproved destructive decisions.

Human UI uses current theme variations/containers/anchors. At800x600 controls remain reachable by scrolling, with confirmation/close controls visible; at1920x1080 use available space without stretching text into unreadable lines. Exact component layout and visual hierarchy are builder choices within these constraints. Automated signals/rectangles/screenshots are not physical-input, accessibility or comfortable-reading acceptance.

## 8. COL-007 — Planetary visual identity

Add original, lightweight globe/surface illustrations to surveyed planet inspection and owned-colony inspection, with recognizable thumbnails in management. Within each core planet-type/environment combination, provide at least eight visibly distinct surface signatures: vary land/sea or dry-surface pattern, appropriate palette, geological markings and cloud arrangement. Variation must be more than a tiny tint change. Ocean/shelf-sea/dry appearances must agree with the known environment; illustrations must not imply new resources, hazards, climate or habitability modifiers. Planets of the same type still have the same existing mechanics unless stated by actual known data.

Use a versioned appearance descriptor assigned deterministically to each new rules5 planet using a specified stateless stable hash of galaxy seed, planet ID and appearance-version salt. No System.Random/GetHashCode, RNG draw, global ID allocation, machine-dependent hash or access to private empire state. The implementation plan must freeze the exact hash and literal seed/ID vectors before coding. Eight is the minimum vocabulary, not a guarantee every small galaxy contains every signature. Provide a deterministic full-vocabulary gallery as acceptance evidence.

Persist the descriptor/version in planet state and copy it into viewer survey facts when observed. Publish it only with surveyed KnownPlanetView/owned colony data. Unsurveyed worlds have a neutral unknown image. Stale observations retain their last-seen illustration; unseen foreign buildings/ownership/population/research do not change it. No night lights, damage or other live state overlay leaks hidden facts. Reload/resizing/save-as preserves the signature; adding a planet never reassigns existing signatures. Descriptors are cosmetic and do not affect galaxy RNG, economy, AI or founding legality.

Procedural C#/Godot drawing with bounded cached results is the recommended first implementation; original raster art is also permitted if it meets the same descriptors/privacy/licensing/bounds. This is not a new rendering framework, shader dependency or asset-generation mandate. Bound the cache to128 portraits with deterministic eviction independent of game state; release replaced UI resources. Art constants/style belong to versioned presentation resources/theme, not simulation economics. Full appearance palettes/pattern composition are builder choices; no copied legacy art. Record any third-party source/license.

## 9. COL-008 — Data, persistence and compatibility

Proposed new games use rules5; proposed writer6 adds the new campaign contracts. This is a recommendation requiring full-design approval, not a change already made. Preserve the existing eleven accepted schema/rules pairs and add exactly (6,1),(6,2),(6,3),(6,4),(6,5), sixteen in total. Earlier schemas may not claim rules5. Reject unsupported pairs before publishing any campaign. Old games keep their embedded content/rules/AI and are never automatically upgraded. New fields are absent from rules1..4 writes, including under schema6; old canonical future traces may normalize schema version only.

Rules5 saves require embedded default template definitions, private per-empire custom libraries/counters/new-colony inheritance, assigned colony template snapshots and switches, queues/local token counters/active bindings, bounded governor report history, and planet/survey appearance descriptors/version. Default counters and explicit empty lists are serialized; omission is not silently treated as consent. Clone all mutable nested objects. Validate ownership, unique IDs/revisions/tokens, bounds, references, active binding/project agreement, no duplicate/completed queued building and report viewer/order/turn consistency. Do not accept active governor state on an AI-owned colony. Writer failure leaves destination/original session unchanged, as current behavior.

Templates can be exported/imported as a small versioned JSON preset through user-chosen files, allowing reuse in other campaigns. No automatic import, path execution, scripts or campaign replacement. Import resolves building IDs against the destination embedded content and rejects missing IDs atomically with useful diagnostics; no silent ID guessing. Imported templates receive a new local ID and can be renamed. Cap file size at256KiB, templates64 and each list32; invalid/duplicate/oversized input changes nothing. No cloud service is needed.

Before implementation changes writer/default, capture genuine current rules4 main campaign continuations under the unchanged engine, with source hash and canonical future states. Preserve genuine rules1/2/3 and passive-AI fixtures and all existing QA saves. Core rules5 generation must preserve the same rules4 seed's base galaxy facts, ordinary entity IDs and RNG state; compare after removing only explicitly new metadata/rules tag. On legacy load, retain the old text inspection if no appearance exists; do not invent saved descriptors or consume RNG. UI-only cosmetic fallback may be neutral, never new authoritative facts.

## 10. Acceptance and review gates

All criteria below are proposed until the full design is approved. No application test or gameplay acceptance is claimed by drafting them.

| ID | Required observable evidence |
| --- | --- |
| COL-AC-001 Foundations | Ordinary costs and flat bonuses, including zero production/research workers, existing shortage scaling, all four species and no species multiplication of buildings; new content literal costs/prerequisites/effects. |
| COL-AC-002 Templates | Four visible defaults, copy/edit/create/delete/revision/assignment preview, independent switches, limits, no live edit propagation, private libraries; renamed/reordered/empty valid templates and invalid references/Load recovery. |
| COL-AC-003 Queues | Finite mixed queue, legal skips of locked targets, retained active project/blocked vessel, exact active entry completion, manual completion, reserve conservation, one completion/turn, pause/resume/manual override/bulk rollback and overflow. |
| COL-AC-004 Workforce | Literal table in COL-004 plus zero-rate/one-population/missing-project/locked-work/shortage, growth-cap and Sulan/Wenth cases; no intentional shortage; edited content and no RNG. |
| COL-AC-005 Transactions | Two-human/AI Ready ordering; no-op/nonfinal/final/withdraw/retry; unexpected command/budget/overflow/final validation failures preserve exact references/canonical bytes/readiness/queues/reports/counters/RNG. No rival/governor conflict. |
| COL-AC-006 Manage scale | Actual UI manual one-colony then6/11/30-colony valid labelled fixtures, bulk application/filter/exception drilldown, one clearable nonmodal size suggestion; thirty healthy worlds need no individual completion acknowledgement. No automatic takeover. |
| COL-AC-007 Reports | Actual changed workforce/project reasons, idle bank/locked work/support/embarkation Attention, category filters, persistent issue deduplication, owner-only10-turn history across reload; no crisis or human-comprehension claim. |
| COL-AC-008 Appearance | Actual deterministic gallery with eight visibly different signatures per core type/environment; surveyed and owned views/thumbnails at800/1920; reload/resize/machine-independent descriptor vectors; no economy/RNG/ID change. Human aesthetic approval remains separate. |
| COL-AC-009 Privacy | Change unseen foreign resources/identity/appearance/structures/orders while own projections/decisions/portraits/reports remain identical; unknown image and stale illustration do not reveal live state. |
| COL-AC-010 Compatibility | All sixteen explicit pairs and invalid pairs/missing/corrupt new fields; genuine rules4/3/2/1/passive-AI continuation, failed load/write atomicity, no silent campaign/template upgrade, preset import/export/limits. |
| COL-AC-011 Integrated regression | Generated core campaigns all four human species, real foundation/specialist research and construction, custom/default governors, save pending queue/Ready and canonical continuation; retained Rival/Species/Expansion/Legacy scenes, ordinary build/excluded harness metadata, logged counts/QA paths/PNG inspection; labelled scale/rare cases separate. |
| COL-AC-012 Owner design gate | Explicit approval/amendment of this complete scope/defaults/governor authority/ordering/content/version policy before a separate implementation plan. Direction and delegation of drafting judgment alone do not satisfy it. |

Builder check completed: a fresh read-only C#/Godot engineering reviewer returned "nothing important left"; [record](../../../reports/COLONY_DEVELOPMENT_DESIGN_REVIEW_2026-10-24.md). This establishes planning clarity, not gameplay or design approval. Full owner review then precedes a separately reviewed implementation plan. Retained Native execution applies only after those gates; do not request the same method again. Windows automated fixtures do not close physical input, uncoached comprehension, aesthetics, general balance, target-platform equality, security/export or release. Broader REQ-017 remains In Progress. No merge/push/cleanup is inferred from design drafting.
