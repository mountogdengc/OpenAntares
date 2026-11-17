# ADR-031: Approve civilian logistics behavior and prepare its plan

Date: 2026-11-16.

**Status:** Accepted for the civilian gameplay behavior summarized below. Concrete implementation plan awaiting review; implementation Not Started.

## Owner decision and scope

The owner first delegated whether to begin with development goods or just provisions/migration, observing that all resources would eventually be implemented. The assistant retained provisions, development goods and fleet supplies, recommending provisions/transports/migration first, development goods next and fleet endurance afterward.

The assistant then summarized the initial behavior: automatic routine deliveries within player priorities/reserves; explicit bulk-transfer orders; migration respecting population floors, destination capacity, habitat and quarantine; and Wenth retaining daughter-seed founding rather than moving lobes as passengers. The owner replied: "approved".

This approves that civilian behavior and permits writing the [separate implementation plan](../superpowers/plans/2026-11-16-civilian-logistics-and-migration.md), using the [supply/migration specification](../superpowers/specs/2026-11-16-supply-and-migration-design.md) as its detailed design context. It does not claim that the owner separately confirmed each assistant-drafted numerical value, all later fleet penalties or a tactical/interception model. The concrete plan identifies its unit, opt-in setup and version/compatibility choices for review.

## Retained constraints

Use the existing Native inline execution preference after plan approval, without per-task agents and with one final fresh whole-implementation review. Scoped native verification and the shared queue remain mandatory; no full suite by default and no Docker. Preserve the newer UI, shader source, owner documents/theme, saves, fixtures/evidence, branches/worktrees and old-rules behavior.

No numbered milestone is assigned. Civilian design behavior is Verified for this approval scope; LOG-D remains In Progress for later unapproved resource/fleet/interception details. LOG-P is In Progress for concrete-plan review; LOG-I and functional acceptance remain Not Started. No gameplay implementation, commit, merge, push, release or cleanup is authorized by this approval record.
