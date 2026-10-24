# Established decision index

Created by reconciliation on 2026-10-22. No prior `docs/DECISIONS/` or ADR-014/015 was found. ADR-001 through ADR-004 recover existing OpenAntares choices; their date is the recovery date, not a claimed original approval date. ADR-005 through ADR-008 record subsequent explicit owner decisions. ADR-007 authorized drafting; ADR-008 subsequently approves the complete species design and requests a separate implementation plan.

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

The charter remains authoritative for scope; original specs retain exact behavior. Alternative evaluations are recorded only where sources contain them. New consequential decisions need their own record and actual approval status; do not reclassify research or a proposed design-guide paragraph as accepted.
