# Fleet Command and admirals — consolidated gameplay review

2026-10-27. **Full written design approved**, including the starter-default table, by owner “0approved” in [ADR-018](../docs/DECISIONS/ADR-018-approved-fleet-command-and-admirals-design.md). The [separate implementation plan](../docs/superpowers/plans/2026-10-27-fleet-command-and-admirals.md) is ready for review. M8-D/FLT-AC-012 Verified for design; M8-P In Progress; M8-I/functional criteria Not Started. This remains documentation, not implemented gameplay or new runtime evidence.

[Complete specification](../docs/superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md), [decision record](../docs/DECISIONS/ADR-017-fleet-command-and-admirals-design-entry.md), [builder questions/dispositions and document checks](FLEET_COMMAND_DESIGN_REVIEW_2026-10-27.md).

## Fleet command and construction — reviewed direction

One command window lists all own ships, fleets, orders, completed/unassigned ships, construction and exceptions. Bulk commands and destination selection work without searching the map or hiring an admiral. Independent ships remain usable. Fleets form/merge at co-location and travel together at the slowest member's effective speed; the route is committed during transit. Admirals ride flagships and transfer at co-location. Hold, Assemble, Explore and Return are standing orders; known dangers and manual control retain priority. Risky ameba Study/Remain still requires the existing explicit command and consent.

Finite assembly requests specify composition, participating colonies and a rally system. Offer suitable idle ships first with explicit acceptance, preview finite construction and timing, dispatch completions automatically and report when the full quota arrives. Missing hulls need explicit finite amendments; paused/cancelled work preserves reserve, existing ships and unrelated work. Population-carrying construction needs explicit cargo authorization and existing species/quarantine rules. No automatic founding or endless replacement.

The amended builder check identified an unsafe-rally retry gap. The drafter's clarification pauses that request for direction, preserves the surviving ships/reservations and requires an explicit legal rally change/resume. It never sends withdrawing ships repeatedly back into the same danger, invents replacement hulls, redirects committed travel or grants completion XP for a blocked assembly. This applies the already-reviewed safety/manual-authority policy; see FLT-003 for exact saved-state and completion rules.

Normal priority preserves existing explicit queues; Urgent previews moving request entries before unstarted work. Active projects are preserved unless directly replaced. Both preserve queue tokens/unrelated order and clear Ready only on a real edit. Approved bounds remain32 ships/fleet,64 named fleets,32 active requests and32 participating colonies/request; independent ships/emergency separation do not consume named-fleet slots, so construction/safety cannot fail solely at that cap.

## Named officers and progression — scoped choices accepted

Start with one Navigator and Surveyor/Organizer candidates; commissioning takes2 turns, with4 active officers initially. Fleet controls/standing orders work without an officer. Wenth command identities belong to colony bodies, never independent speaking-limb people.

| Specialty | Experienced at10 XP | Veteran at30 XP total |
| --- | --- | --- |
| Navigator | +5% fleet travel speed | +10% |
| Surveyor | +1 displayed research per first-time own system survey | +2 |
| Organizer |5% lower ship production cost within their coordinated request |10% |

XP awards:2 for a first survey;1 for each newly built ship in a completed assembly request;8 for the successful first rewarded ameba study. No XP farming through waiting, resurveying, repeated Ready or request splitting. Promotion applies next snapshot. Service records retain achievements, assignments, promotions, ships lost and recovery/retirement; ships/cargo never return merely because their officer survives.

## Recovery and culture — scoped choices accepted

Doctrine is editable from the start; research improves equipment. All four cultures start recovery-focused; Wenth present Re-establish command link. Last watch is an explicit manual choice, never a culture-imposed default. Peldra preserve expertise, Harl experience/testimony, Sulan pilot knowledge/safe return, and Wenth the whole-body connection.

| Outcome after flagship destruction | Basic | With Command Recovery |
| --- | --- | --- |
| Peldra/Harl/Sulan Recover command |50% officer survival |70% |
| Successful mortal recovery delay |2 turns |2 turns |
| Wenth Recover command link delay |2 turns |1 turn |
| Wenth manual Last watch link delay |4 turns |3 turns |

Mortal failed recovery or manual Last watch permanently kills the officer. Successful survivors retain XP; Wenth source bodies survive link loss and retain XP/history. Command Recovery costs24 displayed research, prerequisite Industrial Coordination. New effects use the next snapshot; existing ameba hull/cargo outcomes and once-only600 research study reward are unchanged.

## Approved starter defaults

| Default | Approved choice |
| --- | --- |
| Simple progression | Cap XP at30; fixed passive tracks, no respec/active ability buttons in this slice. Career achievement counters continue. |
| Lifetime/history bounds |64 lifetime commissions;32 recent highlights and10 detailed turn reports per officer; retain final career summaries. |
| Construction coordinator | An officer posted at a participating colony coordinates one request, without simultaneously commanding a fleet. Organizer cost changes are previewed and follow the explicit snapshot/reserve rules. |
| Rival AI | Same starting Navigator/rules/bonuses, ordinary costs and existing sole expansion planner; new recruitment/request strategy is deferred. |

These bounds/details are accepted as part of the explicit full-design approval. The complete specification also defines ownership/knowledge filtering, atomic rollback, strict saves, legacy futures, rules8/writer9/37 explicit pairs, scale cases and acceptance criteria. These are approved written contracts awaiting implementation and verification, not executed results.

## Scope and next gate

[Future combat direction](../docs/design-notes/2026-10-27-combat-retreat-morale-and-damaged-ships.md) preserves retreat/morale, degrading capabilities, limping home/slow-ship separation and hit-and-run with automation/manual-control questions. No combat/damage/morale/repair mechanics, warship designs/refits, broader diplomacy or new economy enter M8. Current main remains c554797/rules7/writer8; original owner edits and all worktrees/branches/QA/recovery stay preserved.

Full approval of this summary and complete specification closes M8-D/FLT-AC-012 for design only. Next is owner review of the separate17-task implementation plan, retaining Native inline execution after its approval. Parent REQ-017/17-row counts and human/platform/security/export/release limits remain unchanged.
