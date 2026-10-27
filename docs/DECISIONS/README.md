# Established decision index

Created by reconciliation on 2026-10-22. No prior `docs/DECISIONS/` or ADR-014/015 was found. ADR-001 through ADR-004 recover existing OpenAntares choices; their date is the recovery date, not a claimed original approval date. ADR-005 through ADR-014 record subsequent explicit owner decisions/drafting direction. ADR-007 authorized drafting; ADR-008 subsequently approves the complete species design and requests a separate implementation plan.

| ID | Established decision | Source/status |
| --- | --- | --- |
| [ADR-001](ADR-001-engine-and-simulation-boundary.md) | C#/Godot .NET presentation and independent simulation | Charter + CLAUDE.md; implemented |
| [ADR-002](ADR-002-deterministic-commands-and-economy.md) | Deterministic command-driven turns and prototype economy | CLAUDE.md + economy spec; implemented, balance subject to playtest |
| [ADR-003](ADR-003-json-and-save-compatibility.md) | JSON content, embedded saves, explicit legacy compatibility | Economy + spiral specs; implemented |
| [ADR-004](ADR-004-spiral-galaxy-and-scope-gates.md) | Integer spiral/regions, all-visible prototype, milestone gates | Spiral spec + approved validation plan; generation implemented, acceptance open |
| [ADR-005](ADR-005-defer-uncoached-playtest-for-design.md) | Defer the unavailable uncoached playtest and proceed to expansion design | Explicit owner instruction 2026-10-22; design-entry gate exception, no implementation approval |
| [ADR-006](ADR-006-approved-expansion-milestone.md) | Approve the exploration/travel/second-colony specification and its compatibility policy | Explicit owner approval 2026-10-22; proceed to separate implementation planning |
| [ADR-007](ADR-007-species-design-and-creative-review.md) | Authorize species draft and creative amendments for owner review | Historical drafting authority, superseded for design approval by ADR-008 |
| [ADR-008](ADR-008-approved-species-mechanics.md) | Approve classic space-opera species mechanics and request implementation planning | Explicit design approval2026-10-23; separate plan subsequently approved for Native execution; implementation/review evidence linked in ADR-008 |
| [ADR-009](ADR-009-rival-expansion-design-entry.md) | Select competition with another empire for the next design | Historical direction/drafting; full design subsequently approved in ADR-010 |
| [ADR-010](ADR-010-approved-rival-expansion-design.md) | Approve full rival-expansion design and proceed to separate implementation planning | Explicit full-design approval 2026-10-24; plan subsequently approved; Native plan/review and follow-up complete; all bounded child criteria Verified; owner option1 local integration complete; no push/release/cleanup |
| [ADR-011](ADR-011-colony-development-design-entry.md) | Colony-development/delegation direction and design entry | Historical drafting authority; full design subsequently approved in ADR-012; separate plan and all twelve Native tasks/review/correction complete |
| [ADR-012](ADR-012-approved-colony-development-design.md) | Approve full colony-development design and proceed to separate planning | Owner “approved” after full specification; M5-D/COL-AC-012 Verified for design; Native retained; separate plan approved, twelve tasks/review/correction and three subsequent presentation fixes verified/integrated; all bounded COL child criteria Verified |
| [ADR-013](ADR-013-colony-crisis-design-entry.md) | Authorize colony-crisis specification, starting with plague | Historical drafting authority; complete design subsequently approved in ADR-014 |
| [ADR-014](ADR-014-approved-colony-crisis-plague-design.md) | Approve complete colony-crisis/plague design and proceed to separate planning | Explicit owner “approved”2026-10-25; M6-D/CRI-AC-012 Verified for design; subsequent [implementation and review corrections](../../reports/CRISIS_REVIEW_CORRECTIONS_2026-10-25.md) and [owner-authorized local integration](../../reports/CRISIS_INTEGRATION_2026-10-25.md) are verified within automated scope |

The charter remains authoritative for scope; original specs retain exact behavior. Alternative evaluations are recorded only where sources contain them. New consequential decisions need their own record and actual approval status; do not reclassify research or a proposed design-guide paragraph as accepted.

[ADR-015: Scout survival/encounter-avoidance drafting](ADR-015-scout-survival-design-entry.md) records explicit owner drafting authority2026-10-25. M7-D/SCT-AC-012 In Progress; proposed gameplay rules await review. No design/plan/implementation approval, default/writer change or warfare scope is inferred.

ADR-015 scope amendment: owner selected a space ameba as actual danger; the peaceful-only recommendation is superseded while rival empires remain peaceful. Exact stationary habitat/warning/escape/study/reward/authority/setup/compatibility rules are proposed for review, not fully approved or implemented.

[ADR-016: Approve full scout survival and space-ameba design](ADR-016-approved-scout-survival-and-ameba-design.md) records owner “approved”2026-10-25. M7-D/SCT-AC-012 Verified for full design; the separate15-task plan is ready for review, M7-P In Progress, implementation Not Started. ADR-015 remains historical drafting/amendment authority; retained Native follows plan review.


[ADR-017: Fleet Command and named-admiral design entry](ADR-017-fleet-command-and-admirals-design-entry.md) records owner direction2026-10-27 and the complete proposed draft. M8-D/FLT-AC-012 In Progress; separate plan/implementation Not Started. Numeric bonuses/XP, fleet/request authority, cultural loss defaults, Wenth identity, recovery/research and rules8/writer9 await full gameplay review. Main remains rules7/writer8; all owner edits/worktrees/evidence preserved.


M8 scoped decision2026-10-27: the owner confirms doctrine editable from the start and research improving recovery equipment (ADR-017/FLT-006). No research gate for doctrine changes remains open. Exact equipment/bonus/XP/culture values and the rest of the full M8 design remain pending gameplay review; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Documentation only; current main/product and owner edits remain unchanged.


M8 scoped owner choice2026-10-27: Navigator, Surveyor and Organizer; promotions at10/30 XP. The old Recovery specialist, ameba-only Surveyor proposal and20/60 thresholds are superseded in the active specification; historical review/check artifacts remain intact. Exact bonus magnitudes/event XP/proposed XP cap, Organizer accounting, recovery values/cultural defaults and full M8 acceptance remain pending review. Doctrine remains editable from the start; research improves recovery equipment. M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. No code/main/owner-file change.


M8 scoped owner decision2026-10-27:50% officer recovery,70% with recovery research and a two-turn successful-recovery delay are confirmed. Current review topic is cultural defaults; do not treat provisional Harl Last watch as approved. Other unreviewed rules/values and full M8 acceptance remain pending; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Main and four owner files remain unchanged.


M8 cultural-default decision2026-10-27: all four cultures begin recovery-focused; Wenth present Re-establish command link. Last watch is manual-only; the original Harl default is superseded. Do not reopen settled doctrine/research, specialties/10-30 XP,50-70% recovery/two-turn delay or cultural defaults. Use the specification's remaining-review checklist for unconfirmed details. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees/evidence remain preserved.


M8 bonus/XP decision2026-10-27: Navigator5/10%, Surveyor1/2 research per first survey, Organizer5/10% scoped request-ship cost reduction and2/1/8 XP awards are owner-confirmed; do not re-ask. Current review is fleet/construction operations, with other unconfirmed details/full design approval still pending. Preserve docs/design-notes/2026-10-27-combat-retreat-morale-and-damaged-ships.md as future owner direction (degrading capabilities, retreat/morale, limping home/detachment/hit-and-run, automation with possible manual control), not M8 damage/combat scope. Plan/implementation remain Not Started; main/four owner files/worktrees/evidence unchanged.


M8 fleet/construction review2026-10-27: owner retains the reviewed proposal and Normal/Urgent request priority. Normal preserves explicit queues; Urgent previews moving before unstarted work, never silently replacing the active project. Preserve finite authority, priority-aware estimates, tokens/reserve/manual control and current combat-note future-only scope. Next review: commissioning/Wenth details/recovery technology and other unconfirmed details, then full M8 approval before separate planning. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees unchanged.
