# Fleet Command and admirals design review — 2026-10-27

Current checkpoint: the owner-reviewed choices are consolidated in the [gameplay review summary](FLEET_COMMAND_GAMEPLAY_REVIEW_2026-10-27.md), with remaining starter defaults explicit for full written-design approval. A materially amended design check returned one unsafe-rally retry question; FLT-003 now safety-pauses requests, preserves surviving reservations and requires explicit legal resume. [Raw amended check](fleet-design/amended-builder-questions.md). This was a design planning-readiness check, not an implementation review or new runtime evidence. Initial and scoped checkpoints below retain their historical findings/results; later owner decisions supersede their earlier provisional choices.

Documentation-only design entry on isolated `codex/fleet-command-design` from integrated main c554797. [Full draft](../docs/superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md), [ADR-017](../docs/DECISIONS/ADR-017-fleet-command-and-admirals-design-entry.md), [owner direction](../docs/design-notes/2026-10-27-fleet-command-and-admirals.md). M8-D/FLT-AC-012 In Progress, plan/implementation/functional acceptance Not Started. The named officers/bonuses/service/XP and cultural-default direction are owner statements; numeric mechanics, unlocks, recovery/Wenth treatment and research relationship remain proposals for full review.

At the initial draft checkpoint, one fresh read-only builder check returned four questions; [raw report](fleet-design/builder-questions.md) is retained. No per-task agents or second review had been commissioned at that checkpoint. The later amended-design check is tracked above. The drafter resolved each initial question with explicit proposed behavior:

| Question | Disposition in the draft |
| --- | --- |
| Fleet cap can block production/safety | Only named fleets count. Independent ships are legal; emergency separations create independent ships. Requests reserve named-fleet capacity before accepting construction. Safety and ordinary hull completion never require an extra counted fleet. |
| Queue-only authority can leak into template work | Separate temporary finite-explicit-queue permission while original construction is off; no switch/template change or fallthrough. Already-enabled governors keep existing authority. One selector per colony. |
| Missing quota can be mistaken for completion | Every authorized slot must be Arrived/full quota fulfilled. Missing blocks completion/XP until an explicit replacement or reduced-quota amendment. |
| Founding consumes an occupied flagship | Successful founding posts the officer at the new colony, retains XP and Wenth source identity, makes no recovery/loss roll, and Holds surviving fleet or retires an empty one. |

Additional drafter checks make four new-hull requests and one four-hull request grant the same4 assembly XP, distinguish successful recovery without a destination from death, reserve rally-fleet membership, and preserve existing AI's sole order controller. These are written/reference checks, not executed gameplay tests.

Main remains c554797/rules7/writer8 and the four owner files remain byte-exact against this turn's before hashes; primary status/index and all seven worktrees/retained branches/QA/recovery remain. The source worktree moved from the retained scout-polish branch to the new design branch; no worktree was created/deleted. [Document checks/preservation hashes](fleet-design/document-checks.json) record actual results. The1398 simulation and seven native/4491-record main results are historical evidence from the completed M7 merge; no fresh C# build/test/runtime or officer validation is claimed. REQ-017/17-row totals and human/platform/security/export/release limits are unchanged.

Material proposed calls for owner review: finite requests versus ongoing reinforcement; slowest-speed coherent fleets; capped passive specialties and event XP; immediate doctrine editing with research improving equipment; provisional culture defaults; mortal recovery versus the Wenth whole-body link; bounded commissioning and modest AI scope. Full gameplay approval precedes a separate plan; no product/default/writer change or main merge/push/release/cleanup here.


## Owner-confirmed doctrine/research decision — 2026-10-27

The owner selected: “Let’s keep doctrine editable from the start and have research improve recovery equipment.” Doctrine changes therefore need no technology unlock; research improves equipment. This settles only that policy, superseding the earlier open research-gate alternative. It does not approve the full M8 design, cultural defaults, recovery probabilities/delays, ability/XP values, technology cost/prerequisites, AI or rules8/writer9. M8-D/FLT-AC-012 remain In Progress; plan/implementation remain Not Started. No code or primary-main change.

The [scoped decision checks](fleet-design/doctrine-decision-checks.json) verify the current wording, unchanged parent counts, documentation-only changes and exact primary owner-file preservation. No additional builder review or C# build/test/runtime was needed or claimed for recording this explicit choice.


## Owner-confirmed specialties and promotions — 2026-10-27

The owner selected Navigator, Surveyor and Organizer and the10/30 XP promotion thresholds. This supersedes the initial Recovery specialist, ameba-only Surveyor direction and20/60 thresholds. The active proposal now gives Surveyor first-survey research and Organizer a scoped request-ship cost reduction, matching the reviewed role descriptions. Exact5/10 percent bonuses,1/2 research,2/1/8 event XP, proposed30 XP cap and accounting details remain drafter proposals for full review. Recovery probabilities/delays and cultural defaults also remain pending; no recovery-specialty modifier remains. The already-confirmed editable doctrine/recovery-equipment policy is unchanged. This scoped choice does not approve the full M8 design, separate plan or implementation.

[Scoped specialty/threshold checks](fleet-design/specialty-decision-checks.json) cover active wording, reference arithmetic and owner preservation. The original builder report/document checks remain historical; no fresh builder review or C# gameplay validation is claimed for this amendment.


## Owner-confirmed recovery numbers — 2026-10-27

The owner accepted50% officer recovery,70% with recovery research, and a two-turn delay after successful recovery. These replace the provisional status of those mortal recovery values. Existing doctrine editability, equipment research and Navigator/Surveyor/Organizer10/30 XP decisions remain settled. Cultural defaults are the next review topic; the original Harl Last watch default is a drafter proposal challenged during discussion and has not been approved. Wenth-specific identity/link/equipment rules, specialty bonus magnitudes/event XP, technology cost/prerequisites and the rest of the M8 design remain pending full gameplay review. This choice does not approve the whole design or authorize planning/implementation. No code/main/owner-edit change.

[Recovery decision checks](fleet-design/recovery-decision-checks.json) verify the explicit choice, draw/delay reference examples and owner preservation; no fresh builder review or C# runtime is claimed.


## Owner-confirmed cultural defaults — 2026-10-27

The owner selected recovery-focused defaults for all four cultures and Last watch as a manual choice. Peldra/Harl/Sulan use Recover command; Wenth use the equivalent Re-establish command link presentation. The old automatic Harl Last watch proposal is superseded. Last watch may be selected directly or through an explicitly player-chosen future-commission default; no culture chooses it automatically. Previously confirmed doctrine/research, specialties10/30 XP and recovery50%/70%/two-turn choices stay settled. The remaining-review checklist in the specification covers outstanding bonuses/event XP, fleet/request rules, commissioning/Wenth details, equipment technology and bounded AI scope. Full M8 approval, planning and implementation remain pending. No product/main/owner-file change.

[Culture decision checks](fleet-design/culture-decision-checks.json) verify active defaults, manual-only Last watch, remaining-review coverage and exact owner preservation. No fresh builder/C# test/runtime result is claimed for this documentation amendment.


## Owner-confirmed bonus values and XP awards — 2026-10-27

The owner accepted Navigator5%/10% travel speed, Surveyor1/2 displayed research per first-time survey and Organizer5%/10% production-cost reduction within their assigned construction request. They also accepted2 XP per first survey,1 XP per newly built ship in a completed assembly request, and8 XP for the successful first rewarded ameba study. Previously confirmed identities/10-30 promotions, editable doctrine/equipment research,50-70% recovery/two-turn delay and recovery-focused cultures/manual Last watch stay settled. Current review is fleet/construction operations; other unconfirmed details and full M8 approval remain pending. No implementation is authorized by this partial approval.

[Bonus/XP and combat-note checks](fleet-design/bonus-and-combat-note-checks.json) verify active decisions, reference arithmetic, future-only scope and owner preservation. No additional builder review or C# build/test/runtime is claimed.


## Fleet/construction review and request priority — 2026-10-27

After the fleet/construction walkthrough, the owner retained the proposed direction including Normal/Urgent priority. Normal is the initial priority and preserves existing explicit queue order. Urgent previews moving requested ships before unstarted work while preserving the active project; replacement remains a separate direct choice. The draft spells out priority-aware estimates, atomic later reordering, stable tokens/reserve, pause/cancel behavior and persisted priority. It retains coherent slowest-member travel, finite assembly, automatic rally dispatch, manual authority and explicit missing-slot amendments from the walkthrough. No new damage/morale/combat behavior is included. Remaining commissioning/Wenth/recovery-technology and other unconfirmed details/full M8 approval remain pending; plan/implementation Not Started.

[Priority decision checks](fleet-design/priority-decision-checks.json) cover active-project preservation, literal queue order, estimates, persistence and owner preservation. No additional builder review or C# gameplay validation is claimed.


## Owner-confirmed initial admiral availability — 2026-10-27

The owner accepted one starting Navigator, Surveyor/Organizer candidates, two-turn commissioning and four active admirals initially. Fleet controls/standing orders work without an officer; officers supply bonuses/progression. Ship-based travel and co-located transfers remain part of the reviewed availability direction, with Wenth availability tied to colony bodies. The additional proposed64-lifetime-commission bound was not separately confirmed by this answer. Remaining Wenth link-loss/equipment details, recovery technology, other unreviewed starter/accounting details and full M8 approval remain pending; no plan/implementation or main change.

[Availability decision checks](fleet-design/availability-decision-checks.json) cover active wording, unapproved lifetime bound, unchanged parent counts and owner preservation. No fresh builder review or C# gameplay validation is claimed.


## Owner-confirmed Wenth recovery and technology — 2026-10-27

The owner accepted Wenth link-restoration delays of2 turns normally and1 with recovery research; manually selected Last watch takes4/3 turns. The source body retains XP/history. Command Recovery costs24 displayed research and requires Industrial Coordination. Previously confirmed mortal50/70% recovery/two-turn delay and all other scoped choices remain settled. The consolidated gameplay review summary includes the remaining proposed XP/history/lifetime/coordination/AI defaults for one full written-design approval before a separate plan. This answer does not by itself approve the complete specification or authorize implementation. Main and all four owner edits remain unchanged.


## Amended-design planning check and handoff

After material owner amendments (Organizer cost/survey rewards, request priority and final recovery choices), one new read-only builder checked the amended full design. Its [raw question](fleet-design/amended-builder-questions.md) identifies one unsafe-rally retry gap; the other amended accounting/priority/authority rules supplied enough detail for planning. Drafter disposition: before request preparation/dispatch and after new arrival knowledge, a known-forbidden rally safety-pauses the request. Preserve surviving ship bindings and actual committed travel, block completion/assembly XP, issue deduplicated Attention, and require an explicit legal rally change/resume. Never repeatedly dispatch the same ship back into danger or invent a replacement. Existing live encounter consent remains; the paused request adds no extra Ready blocker.

This is an explicit application of the reviewed safety/manual-authority policy for the complete-design approval. No third builder check or implementation review/runtime was claimed. The [final handoff checks](fleet-design/gameplay-handoff-checks.json) cover current owner-confirmed choices, remaining unapproved defaults, written state/phase/loop prevention, links, reference arithmetic and exact preservation. Current M8-D/FLT-AC-012 remains In Progress until full written-design approval; plan/implementation remain Not Started.
