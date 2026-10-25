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
