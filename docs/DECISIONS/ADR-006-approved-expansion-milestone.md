# ADR-006: Approve exploration, travel and colonization design

- **Status:** Accepted owner decision, 2026-10-22.
- **Decision source:** The owner replied "approved" to the expansion design presented in this conversation, including its linked full specification and four highlighted gameplay/compatibility choices.
- **Specification:** [Exploration and colonization design](../superpowers/specs/2026-10-22-exploration-and-colonization-design.md).
- **Requirements:** REQ-015; EXP-001 through EXP-007 and EXP-AC-001 through EXP-AC-008 in that specification.

## Approved choices

Use a public star chart with planetary details revealed by scouting; direct deterministic travel committed until arrival; repeatable scouts/settlers with the specified production/speed fixtures; and settler completion transferring two existing support workers from the source colony, subject to the specified population/support eligibility.

Found colonies only through explicit orders at resolution, preserve snapshot timing, and manage colonies by their exact IDs. Retain the approved per-empire knowledge projection and current/stale observation boundaries. Regions retain their existing generation role without a travel penalty.

Preserve legacy campaign continuation without automatically granting expansion. New campaigns use schema 3/rules 2; supported old campaigns continue under rules 1 and can resave as schema 3/rules 1. No implicit upgrade, regenerated world, or merge of current content into an old embedded ruleset is authorized.

## Scope and consequences

The document's prototype numeric values and bounded policies are approved for this milestone, not demonstrated final balance or usability findings. The earlier builder-check clarification of foreign stationary-vessel visibility is part of the reviewed specification. No new engine/framework, AI, combat, diplomacy, species mechanics, or campaign upgrade tool is approved.

ADR-005's human-playtest deferral remains in force. This approval completes design review and authorizes preparing the separate implementation plan required by the workflow. It does not mark gameplay implemented, tests freshly passed, First Playable fully accepted, or a release ready. Plan review/execution setup precede broad gameplay coding; no Git integration or cleanup is implied.
