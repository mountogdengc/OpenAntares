# ADR-004: Spiral galaxy, prototype visibility, and milestone gates

- **Status:** Established design, recovered 2026-10-22. Spiral implementation merged in PR #22; First Playable acceptance remains open.
- **Sources:** [CHARTER.md](../CHARTER.md), [spiral specification](../superpowers/specs/2026-10-05-spiral-galaxy-design.md), [design guide](../DESIGN_GUIDE.md), [approved validation plan](../superpowers/plans/2026-10-19-first-playable-validation-and-exploration-design.md).
- **Requirements:** REQ-001, REQ-010, REQ-012, REQ-014, REQ-015.

**2026-10-22 amendment:** [ADR-005](ADR-005-defer-uncoached-playtest-for-design.md) records the owner's decision to defer the unavailable uncoached playtest and authorize exploration/colonization design now. The original closure-before-design sequence below is superseded for that progression step. Full acceptance remains open; the design-review-before-implementation gate is unchanged.

## Context and choice

Use one integer spiral family for 12-20 playable systems, content-defined core/arm/rim planet weights, and a separate deterministic unselectable visual starfield. Preserve simulation RNG. Save shape/seed/bounds/regions. All generated systems remain inspectable in the current prototype.

Close First Playable only after the eleven charter actions work and are understandable. Then design knowledge, exploration, travel, and a second colony; review that design before its implementation plan. AI, diplomacy, combat, and final art do not belong in closure work.

## Alternatives and consequences

The spiral spec compares directly adapting floating-point source geometry, shaping only the few playable systems, and the chosen shared integer geometry with visual background. It rejects imported Rust/trigonometry and large campaign graphs. Shape selection and regional travel effects are deferred.

The current all-visible map is deliberate. Future visibility must cover maps, inspectors, lists, previews, and saves, with an explicit legacy policy. Regions do not currently change travel. No fog, scout, colony-ship, or founding-cost choice is settled by this record.
