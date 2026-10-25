# Colony crises and emergency governance: first plague scenario

Date: 2026-10-24 America/Denver. Version 0.1, proposed for owner review.

**M6-D: In Progress. Full design approval pending. M6-P/M6-I: Not Started.** The owner requested drafting and gameplay review before implementation. None of the proposed numbers, rules6/writer7 targets or product changes is approved by that request. Main remains the verified rules5/writer6 baseline at fa2a07f. This drafting changes no product code, core content, dependencies or art.

## 1. Intent, authority and review choices

An emerging threat should create an understandable strategic decision while there is time to save a colony and contain spread. At scale the player sets authority and sees one empire report, rather than repeating routine orders on thirty planets. Critical developments remain visible.

Authority: [charter](../../CHARTER.md), [approved colony contract](2026-10-24-colony-development-design.md), [owner crisis/fleet directions](../../design-notes/2026-10-24-delegation-crises-and-fleet-intent.md), and [ADR-013 drafting authority](../../DECISIONS/ADR-013-colony-crisis-design-entry.md). Preserve existing architecture, species and creative decisions. This is fanciful space-opera disease, not a simulation of human medicine or a statement about real peoples.

The owner requested early warnings for colony-loss/spreading threats, configurable notifications, governors with limited authority, and cure/isolation/destruction choices with consequences. **All quantitative rules below are drafter proposals**, not previously approved or balanced facts. Main review choices:

1. A confirmed harmless warning gives two full planning turns before illness. Active illness has six-turn natural recovery; late neglect can still kill population or lose a small colony.
2. Early quarantine reliably contains the case, at economic and transport cost. Research can clear an entire owned outbreak sooner by diverting a chosen share of new empire research.
3. Destructive containment immediately kills half the colony's population and burns half its production reserve, with temporary empire-wide civic disruption. Governors cannot authorize it.
4. Emergency acknowledgement is required by default. An explicit saved opt-out is separate from local display preferences.
5. Spread uses actual population-carrying vessels within an empire. This first slice has no invisible trade graph or cross-empire transmission.

## 2. CRI-001 — Scope, occurrence and warning

Include one content-defined family: working name **Ash Bloom**, ID ash_bloom. Name/lore are proposals. All four currently playable species use the same crisis rules; their population-unit labels, habitats and economic profiles stay intact. No new resistance traits or culture redesign. Revolts, other disease families, trade, migration, diplomacy, warfare, fleet doctrine, peaceful-mode design, network transport and victory/defeat remain outside M6.

New rules6 games offer **Colony crises: Standard / Off**, default Standard. Save the selection; it cannot be changed mid-campaign. Off generates no episodes, flags, penalties or acknowledgement requirements and makes crisis commands unavailable. No crisis machinery runs for rules1..5.

Standard occurrence:

- Check after resolution into turns20,40,60,..., never before20; process empires in ascending ID.
- An eligible empire has no active episode, an expired cooldown and at least one owned colony without unexpired plague immunity. Sort candidates by colony ID.
- Give each eligible empire a25% check; on success choose one candidate uniformly. At most one spontaneous episode per empire per check; secondary cases can affect any number of its colonies in the same episode.
- Use an independent saved PCG32 stream per empire. Seed with Pcg32.FromSeed(galaxySeed XOR 0x4352495349530001, unsignedEmpireId XOR 0x504C414755450001), using unsigned64-bit operations and the existing PCG implementation. Do not draw from the existing generation/economy stream.
- Ineligible empires consume no draw. Eligible: one NextInt(100), success iff result<25, then one NextInt(candidateCount) on success. Existing rejection sampling may internally consume more draws. Views, forecasts, commands and transmission consume none.
- Allocate an empire-local monotonically increasing episode ID, never a global entity ID. Freeze cure cost at (6 + sourcePopulation) *100 stored research units. The first case is Warning with graceRemaining2, activeAge0, severity0.
- On episode end, next spontaneous eligibility is planning turn endTurn+30, still subject to the20-turn schedule. No postponed checks accumulate: endTurn26 permits turn60, not turn40.

Each secondary exposure also starts its own full Warning2, including a newly founded colony. Warning is confirmed exposure, not a false-positive lottery: onset is certain unless cured or the colony is removed. There is no hidden incubation, same-boundary casualty or retrospective damage.

## 3. CRI-002 — Illness, recovery and colony loss

Only Active cases are infectious or economically harmful. Old Warning cases decrement grace2→1→0 at resolution. On reaching0, the returned case is Active20/activeAge0, but that completed interval has no illness penalty. Cases created at this boundary do not decrement grace.

For an old Active case, the interval's economy uses its beginning snapshot. Increment activeAge afterward; on its sixth Active interval, natural recovery occurs before mortality or spread. Recovery grants12 planning turns of immunity: recovery at turnR protects while currentTurn<R+12.

Before natural recovery, lose one whole population at a boundary if beginning severity>=80, otherwise none. Then set severity=min(100,oldSeverity+20) if open, or min(100,oldSeverity+5) if quarantined. Quarantine does not reset severity/age. A completed cure wins over this boundary's mortality but cannot undo prior losses. New cases do not age, cause damage or transmit at their creation boundary.

Mortality uses post-embark candidate population; the same worker cannot be embarked and killed. Remove casualties from Research, then Production, then Support until totals agree. Explain these changes as casualties, not governor choices. Active illness pauses growth earned but retains existing progress, subject to established capacity rules.

At zero population remove the colony, never save an invalid zero-population entity. Lose its buildings/project/reserve/assignment/queue. The unchanged planet can be recolonized under ordinary eligibility with a new ID. Keep private template libraries/inheritance and surviving vessels. Remove dangling live references; historical reports retain immutable owner metadata.

An empire with zero colonies remains valid and may use surviving founders. Show whether recovery by founding is possible; no free rescue population, automatic defeat/victory or replacement colony. Without a founder it may have no recovery route in this slice. Disclose last-colony consequences before a destructive order. Zero-output research and idle AI remain valid.

## 4. CRI-003 — Responses, economics and cure

Players may combine quarantine and research, change course, or knowingly wait. Responses do not change workforce, construction templates, ordinary research targets or existing normal research reserves.

| Response | Benefit | Cost/risk |
| --- | --- | --- |
| Observe, ports open | Retain transport access and normal research allocation | Active illness penalties, faster deterioration, infectious cargo and potential deaths |
| Quarantine and wait | Block local population-port transmission and slow deterioration; early containment survives to natural recovery | Additional25% production/research loss, paused growth, closed population transport; late quarantine may still lose people |
| Pursue cure, optionally quarantined | Clear every own case and carrier in this episode | Divert0/25/50/100% of new empire research; open ports may spread before completion; quarantine adds local costs |
| Destructive containment | Remove this colony's case immediately | Half population/reserve, growth reset, possible colony loss and civic disruption; other cases/carriers survive |

### Shared economic rules

Keep hundredths and basis-point arithmetic. From existing species/building/support-shortage results, apply80% to Active production/research, then75% if quarantined, then the empire's civic factor. Floor each stage to stored hundredths with checked Int128 intermediates. These reductions include flat building output. Support production/requirement are unchanged. Growth earned is0 while Active or quarantined; retain existing progress subject to normal capacity behavior.

Each factor has its own explained breakdown/division. Shared forecast, governor preview and resolver use these same results. Preserve ordinary reserves/one-completion limits and next-snapshot building/technology effects. Quarantine holds population-carrying vessel completion without spending its cost or losing banked production; report why. Buildings and non-cargo vessels may still complete.

### Cure investment

One episode means one cure target per empire. Apply its chosen research share once to aggregate new net research after local/civic factors and before ordinary research: wanted=floor(newNetResearch*share/100); diverted=min(wanted,cureCost-cureProgress). Diverted units enter episode progress; only the remainder enters ordinary technology reserve/completion. Never spend old normal reserve, invent workers or waste the excess above remaining cure cost.

Progress survives response changes, save/load and source-colony loss. Secondary cases do not raise the frozen cost. Completion clears all owned Warning/Active cases and owned carrier flags for this episode, including transit, gives affected colonies immunity and closes the episode. Treat response protocols as an empire-wide abstraction, not a new physical medicine-cargo system. It is neither a permanent technology nor protection against every later episode. Cure occurs before natural recovery/death/transmission at that boundary; that interval still pays snapshot economic penalties.

After transport/contact/exposure processing, if all cases disappear and no owned carrier remains, close the episode even with unfinished cure: report/discard the partial investment without refund, reset response share0 and start cooldown. If cure and natural recovery coincide, report completed cure/spending once. No new episode/research target starts in that resolution except a scheduled onset allowed by the cooldown rules.

### Destructive containment

This is local emergency sterilization, not bombardment. It requires an owned Warning/Active case and an explicit per-colony confirmation showing exact casualties, reserve loss, last-colony consequences and civic effects. The command identifies expected turn, episode/case revision, population and reserve; stale consequences reject atomically. Dismiss/close sends nothing.

On acceptance during planning: lose ceil(population/2); retain floor(productionReserve/2); reset growth progress0; trim workforce in the casualty order; clear this case and grant immunity to a survivor. Surviving buildings/project/queue remain. Population1 means colony loss and all assets lost under CRI-002. Do not evacuate or destroy vessels automatically. Other infected colonies/carriers remain; explicitly warn that burning a colony does not clear them.

Each order increases civic-disruption level1, capped3, and sets expiry=currentTurn+5. While currentTurn<expiry, production/research empire-wide loses10% per level, maximum30%, after local factors. Additional orders increase the level up to cap and refresh expiry; expiry clears all levels. No extra support/growth penalty. This is temporary public/operational disruption, not a new morale resource. **All four cultures share this proposed baseline; culture-specific approval/outrage/resistance/diplomacy remain Deferred.** Existing culture labels grant no destructive permission or immunity.

Direct confirmation acknowledges only this order's resulting containment/loss revision, tagged direct_order. It does not acknowledge other cases or authorize future destruction. Governors, AI and standing policies cannot issue destructive containment in M6.

## 5. CRI-004 — Cargo transmission and quarantine

No virtual trade lanes, distance radius, empty/scout hulls or foreign contact transmit disease. Listed contacts cause certain exposure; show conditional certainty rather than invented odds. This slice has no disease deaths in transit.

An owned vessel with CarriedPopulation>0 acquires its owner's episode flag when:

1. It completes/embarks at an Active open colony in the economy snapshot. Tag at creation even if the source later naturally recovers/dies that boundary; cure can clear the tag.
2. It is stationary before departure or newly arrives in a system with an owned Active open colony, outside sterile inspection handling. This abstracts owned-system port contact; report the source colony. Foreign colonies do not confer flags.

One episode per empire prevents conflicting owned flags. Retain flags through travel until cure/decontamination/founding; no RNG. New vessels retain existing next-snapshot movement/founding eligibility.

An infectious arrival exposes all its eligible healthy owned colonies in that system, sorted by colony ID. A flagged founder transfers exposure/provenance to its new colony before being consumed; the new colony gets Warning2. Immunity and existing cases prevent duplicate exposure. Deduplicate simultaneous contacts to one colony; lowest source-vessel ID supplies its displayed cause. Newly created Warning cases never cause a within-turn chain.

A quarantined owned colony closes **that owner's population port for the entire star system**. Only the individually quarantined colony pays its economic penalty. Preview this system-wide scope and all cancelled own orders. Foreign vessels and empty/scout vessels are unaffected; no blockade/diplomacy is added.

- Reject new own cargo movement into/out of the closed system and founding within it. Foreign quarantine must never become an information-revealing rejection.
- Enabling quarantine atomically cancels affected own pending departures/founding, including a pending move into that destination from a vessel still stationary elsewhere. Preview cancellations before acceptance. Already-transiting ships keep their exact route/duration and arrive into isolation without exposing colonies.
- Hold each cargo arrival there for one full further resolution for inspection; arrival itself does not count. Clear its flag at that later boundary. A closed port still prevents departure after decontamination.
- At any own-colony system, an explicit Decontaminate order on a stationary owned flagged vessel holds it for one full resolution without population/resource loss. Preview/cancel pending move/founding; while held it cannot move/found/expose. At an open port, an arrival contact has already happened before next planning's player response.
- Release does not restore cancelled orders or waive a remaining hold. After a completed sterile boundary, an Active open local colony can contaminate that ship again; show the continuing risk.

Per-colony release choice: Keep until ordered, or Release when this local case clears. The latter must bind to a current case and defaults for Quarantine this case and governor containment. Preventive healthy quarantine defaults Keep until ordered. Auto-release affects the next planning snapshot, never transport checks midway through resolution. Colony loss removes its quarantine; another own quarantined colony at that star still closes the port.

## 6. CRI-005 — Delegation, attention and scalable reports

Crisis authority is independent of construction/workforce governors. New human empires start **Manual**. Opt-in policies are Contain, or Contain and research with25/50/100% research share (suggest50%). Contain quarantines all own Warning/Active cases and authorizes port inspection. Contain and research also sets episode research share. Neither destroys, abandons, redirects fleets, spends old research reserves, enables colony governors or changes templates. No automatic colony-count takeover.

Run policy only on that human's Ready submission, inside the existing private preparation candidate, before colony governors. Manual case response changes pause automatic control for that case until Resume; manual research-share changes pause the policy's research control until Resume. Other cases/channels remain intact. Show proposed/applied/overridden/waiting actions truthfully. Views/save/load never enact policy. Ready=true no-op never repeats preparation; withdrawing retains accepted planning as today.

**Default saved attention requirement: Require acknowledgement.** Material events: new exposure, Warning→Active, first severity>=80, any casualty interval, colony loss/destructive outcome. Increment a case's revision once per boundary if any occur. Keep its newest unacknowledged revision with coalesced prior facts, rather than one modal per event. A grouped command can acknowledge an explicit set of own IDs/revisions; stale/missing/foreign membership rejects the entire batch. Acknowledgement means knowingly continuing, not cure/destructive consent.

An outstanding urgent revision blocks **that human's next false→true Ready before any preparation**, with owner-only explanation and exact unchanged state/readiness. Other humans may plan/submit Ready normally. Report close, local settings, governors, unrelated acknowledgement, reload and other empires cannot satisfy it. Resolved events retain pending acknowledgement; keep a compact resolved-case receipt until acknowledged/explicitly waived, even after10-turn history expires.

An empire-issued saved setting Allow continuing without crisis acknowledgement explicitly waives that gate after a consequence preview; default false. It records waived revisions at each boundary, never pretends they were read; waived resolved receipts need not remain pending beyond the normal history. Re-enabling Require acknowledgement requires current material revisions for all active cases/pending loss receipts. Local display preference cannot silently change this authoritative campaign rule.

Local crisis display: Interrupt(default), Attention, Digest. This controls panel opening, not records/gates. Digest still has a persistent Critical badge, endangered-world count, drilldown and visible Ready blocker where applicable; no invisible Off for colony-loss/spreading facts. Routine cure-progress/inspection messages may use existing Summary/Attention/Off. Explain downgrading Interrupt versus waiving acknowledgement. Reset/reload cannot erase saved facts.

Use one empire Crisis overview/digest, with grouped normal-response/acknowledgement operations, not one popup per colony. Show location/name/ID, grace/next harmful interval, severity/age, natural recovery, current/next casualty/economic consequences, cure cost/progress/share/opportunity cost, port/holds/cancellations, affected worlds, immunity and authority/overrides. Separate history from current conditions. Thirty exposed worlds need one grouped report and one explicit acknowledgement, not thirty dialogs. Destructive confirmations remain individual.

Retain a current pending record plus10 completed-turn owner-only digests; bounds depend on actual entities. Removed-colony reports use immutable owner metadata. Views, forecasts and generic public errors cannot read foreign private crisis facts. Foreign observations/portraits receive no live infection/penalty/acknowledgement overlay, even at visible systems. Preserve survey/contact disclosure.

C# containers/anchors and shared theme;800x600 actions/consequences/confirmation/close reachable,1920x1080 readable overview. Exact composition is a builder choice, not fixed by this text. Signals/screenshots do not establish physical-device, accessibility, comprehension or aesthetic acceptance.

## 7. CRI-006 — Commands, phases, AI and failure atomicity

Use the shared empire-issued expected-turn pipeline for quarantine/release/override, cure share/override, authority/resume, attention requirement, exact-revision acknowledgement, vessel decontamination and destructive confirmation. Batch normal containment/ack commands are atomic. No-ops preserve original state/reference/Ready; real changes clear issuer Ready only. Validate ownership, IDs, revisions, consequences, port scope and bounds before publication. Destruction is immediate planning mutation; health progression/cure/deaths are resolution effects.

Rules6 human Ready: check acknowledgement, prepare crisis policy, prepare existing construction/workforce, mark Ready, and resolve if final. All form one rollback boundary. Any failure restores the exact state before this request, including another human's accepted planning, episode IDs/revisions, both RNGs, holds/flags/immunity/research/civic facts, queues/reports/counters/readiness. No partial cancellations, reports or acknowledgements. Save/load never prepares policy.

Stable-ID resolution phases:

1. Prepare AI crisis decisions, then ordinary expansion against the resulting own view. Crisis budget at mostC+V+4 accepted operations per AI empire, separately from existing expansion budget. Unexpected rejection aborts; known quarantine/hold unavailability is a legal explained wait.
2. Freeze prepared economy/port snapshot. Calculate support and modified outputs/paused growth; split aggregate new research. Apply ordinary growth/completions/embarkation; capture source-colony report facts and newborn infectious cargo flags.
3. Complete cures first. Then advance old Warning/Active cases, natural recovery and mortality. Remove lost colonies, retain report tombstones, update immunity/auto-release markers. Cure wins over recovery/death; snapshot economic costs remain. Natural recovery/death does not clear previously tagged cargo.
4. Tag unheld pre-existing cargo at infectious open departure systems from the beginning snapshot. Cure suppresses retagging; finished holds clear flags and mark that vessel sterile for this boundary. Apply movement, arrival port/contact handling, then founding with flag provenance. Collect exposures before creating cases. Ports closed in the snapshot stay closed for all checks even if phase3 removed/recovered their colony. Lost sources give no new arrival contact; pre-existing departure contact counts unless cured.
5. Create deduplicated secondary Warning cases in destination-colony/source-vessel order. Then create eligible scheduled spontaneous episodes; existing cases/carriers count for eligibility. Close naturally finished episodes only now, after all tags/exposures are known; then finalize revisions/digests/receipts/cooldowns/expiry, existing observations/contact privacy, reset human readiness and advance once. New cases consume no age here. Validate complete candidate before publication.

AI uses only its own projected facts and the same commands/costs/holds. Rules6 expansion_v1 gets a crisis prepass: quarantine own cases,50% cure share while an episode exists, legal vessel decontamination, case-bound auto-release. Never purge, obtain human acknowledgement, enable human governor state or read foreign facts. Avoid repeated blocked move/build orders, retain lawful waiting/reserves, support zero-colony idling. Gate this extension explicitly to rules6; genuine rules4/5 AI remains unchanged.

## 8. CRI-007 — Content, persistence and compatibility

JSON family parameters define schedule/chance/cooldown, grace/recovery/severity/mortality, local factors, cure base/per-source-population cost, immunity, purge fractions and civic bounds/duration. Values above are the proposed core defaults; bounded numeric mods are allowed, not scripts. Policy shares and transport semantics are generic engine rules. Validate references/percentages/finite positive timings/divisors/severity and checked numeric bounds before creation/load. Preserve ordinary Load recovery when current disk content is invalid.

Required rules6 state: setup selection; per-empire crisis PRNG/counter/cooldown; one episode with source metadata/frozen cost/progress/share/override; cases with grace/age/severity/revisions/current response/release binding; colony immunity/preventive quarantine; cargo flags/inspection due turns; authority/overrides/waiver provenance; civic level/expiry; current/10-turn reports and unresolved receipts. Deep clone mutable nested data. Sort/validate unique IDs, owner references and bounded values. Removed entities require immutable report facts, not live references. Exact types/files are builder choices.

Proposed **rules6/writer7**, only after approval and implementation. Retain the existing16 explicit schema/rules pairs; add exactly (7,1)..(7,6),22 total. Earlier schemas cannot claim rules6. Old rules1..5 omit crisis fields even under writer7, preserve embedded content/AI/economy/RNG/governor/appearance and never auto-upgrade. Rules6 requires explicit setup, streams/default counters/empty lists/attention setting; absence is not consent. Reject unsupported pairs, legacy injections, malformed references/flags/holds/health/counters/content atomically.

Explicitly include6 in development/species/rival/expansion predicates without changing old behavior. Generation preserves identical underlying rules5 galaxy facts, normal IDs, existing RNG and appearance after stripping only new version/crisis metadata. No free population/buildings/technology. Off consumes no crisis draws and otherwise matches rules5 economics for equivalent commands/content. Canonical save/load continuation must match, not merely screenshots.

Before changing writer/default, a separate implementation plan must capture a genuine current rules5 main save and future states under the unchanged engine, with hashes/source provenance. Preserve genuine older/passive-AI traces and all QA/recovery. This draft does not claim that future capture or any crisis runtime has executed.

## 9. Literal acceptance examples

Amounts are stored hundredths unless stated. These are design arithmetic fixtures, not engine-test evidence. Campaign fixtures must recalculate base output after population/technology changes.

| Case | Required result |
| --- | --- |
| Grace | Returned turn20/remaining2;20→21 remaining1/no illness;21→22 Active20/no retrospective illness;22→23 first harmful interval. |
| Open natural recovery | Active20/N6: beginning severities20,40,60,80,100,100. Deaths at intervals4/5 only; natural recovery before death on6. Final N4; immunity begins at recovery. |
| Early quarantine | Same case, severities20,25,30,35,40,45; recovery6/no mortality. |
| Factor ordering | Post-shortage P900/R700, Active +quarantine +civic1:720/560→540/420→486/378. Support unchanged/growth0. |
| Cure split | New research1000/share50%,cost1200/progress900: wanted500/diverted300, normal technology gets700. Existing normal reserve stays except ordinary completion. |
| Cure precedence | N1/severity80/age3 plus completing cure: survive N1; interval still pays Active factors. |
| Late quarantine | N1/severity80/age3/no completed cure: quarantine does not reset severity; next mortality loses colony. Warn before Ready. |
| Purge | N5/S2/P2/R1,reserve901: lose3;N2/S2/P0/R0,reserve450,growth0; survivor buildings/project/queue retained;civic1. N1 is removed. |
| Civic expiry | Purge at planning10 applies intervals10→11 through14→15; expires at planning15. Another at12 gives level2/expiry17. |
| Cargo | Flagged open-port arrival exposes eligible own colonies once, Warning2. Closed-port arrival gives no exposure; next full held interval clears flag; exact travel time retained. |
| Founding | Flagged founder transfers Warning2 before removal; no same-boundary illness; normal founding population/capacity/cost exact. |
| Privacy/failure | Mutate unseen foreign crisis facts: own views/plans/stream identical. Forced late failure returns exact input bytes/reference/RNG/readiness without acknowledgement/report. |

## 10. Acceptance and review gates

All CRI-AC execution is **Not Started**. Written checks and independent arithmetic cannot establish implementation, balance or acceptance. CRI-AC-012 is future full owner approval.

| ID | Required observable evidence |
| --- | --- |
| CRI-AC-001 Occurrence | Schedule/grace/cooldown/eligibility; independent seed/draw vectors; no pre20 events; one own episode; Off/legacy no draws; ordered selection/edited content. |
| CRI-AC-002 Health | Literal timing/recovery/deaths; early/late quarantine;N1/last-colony loss;workforce/capacity/growth;Wenth embark/Sulan sealed;no dangling/zero-pop colony. |
| CRI-AC-003 Economy | Exact factors/rounding/flat bonuses/zero workers/support shortage/all species; forecast equality; held cargo/reserve conservation;civic stacking/expiry/overflow. |
| CRI-AC-004 Cure |0/25/50/100 aggregate splits,old reserve preservation,spill,frozen multi-case cost,partial natural close,cure/death/embark/founding order,no foreign cure/RNG. |
| CRI-AC-005 Containment | Real command previews/cancellations;preventive/case release;transit/full inspection/travel/cargo;no scout/foreign/virtual transmission;dedup/founding provenance. |
| CRI-AC-006 Destruction | Explicit current confirmation/stale/foreign rejection;ceil/floor N1/N2/N5/large;surviving assets/queues,last colony,carrier warning,civic costs;no AI/governor purge. |
| CRI-AC-007 Delegation/Ready | Manual default,separate controls/Resume,no rendering actions;atomic grouped ack,pre-preparation gate,escalation,saved waiver,two humans+AI,no hidden shortcut. |
| CRI-AC-008 Reports/UI | Actual1/6/11/30-colony overview/grouped ack;all levels/critical badge;current/history/removed-colony facts;10-turn logs+receipts/reload;800/1920 reachable controls. |
| CRI-AC-009 Privacy/atomicity | Own-view equivalence under foreign changes;no live foreign port leak;command/prep/economy/transmission/ID/RNG/validation rollback;invalid Load/write atomicity. |
| CRI-AC-010 Compatibility |22 pairs/rejection matrix;genuine rules5/older/passive-AI future equality;strict fields/injections;unchanged galaxy/IDs/RNG/appearance;Off parity;deep clones/bounded receipts. |
| CRI-AC-011 Integration | Generated Standard core campaigns all species with real seeded onset,responses/neglect/contact/cure/quarantine,actual save/load/future equality;labelled rare/scale cases separate;all prior scenarios/ordinary harness exclusions. |
| CRI-AC-012 Owner design | Explicit approval/amendment of complete scope,timing,costs,spread,authority,acknowledgement,culture deferral,version policy before separate planning. |

Human playtest remains Deferred under ADR-005; owner feedback is useful without being relabelled uncoached evidence. Automation cannot close physical input/focus, reading/accessibility/comprehension, aesthetics, long-campaign enjoyment/balance, other platforms, clean delivery/security or release.

## 11. Builder choices and next gate

Builder chooses class/file/DTO decomposition, efficient indexing, cached pure queries, theme-based layout, diagnostic wording and test organization. A later plan freezes literal seed/draw vectors, full valid fixtures, entity-based report/receipt bounds, phase failure hooks and genuine rules5 capture before code. It cannot silently change costs/timing, add virtual spread, weaken warnings, grant destructive authority, rewrite cultures or drop compatibility.

A fresh read-only builder check precedes handoff; reconcile already answered/material ambiguities here. The next gate is **owner review of these gameplay rules**, then a separate implementation plan after full approval. The drafting request authorizes no implementation.


Builder check complete: fresh read-only deterministic C#/Godot engineering reviewer returned “nothing important left.” [Review/evidence](../../../reports/COLONY_CRISIS_DESIGN_REVIEW_2026-10-24.md) records the reviewed document hash, scope and17 independent arithmetic checks. This establishes planning clarity and arithmetic consistency only; full owner design approval, application implementation and gameplay verification remain pending.
