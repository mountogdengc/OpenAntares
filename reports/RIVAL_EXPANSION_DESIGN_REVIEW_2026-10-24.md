# Rival-expansion design review — 2026-10-24

**Status: Builder check complete; full owner design approval received in [ADR-010](../docs/DECISIONS/ADR-010-approved-rival-expansion-design.md). Separate implementation-plan review pending.**

Baseline main `c3b01dbd49d022d526d5319ca8ab34a4ba5e0729`. The owner selected competition with another empire as the next experience; [ADR-009](../docs/DECISIONS/ADR-009-rival-expansion-design-entry.md) records direction only. The [full proposed design](../docs/superpowers/specs/2026-10-24-rival-expansion-design.md) defines a bounded rival-expansion slice. This report records design review, not gameplay implementation.

## Builder check

A fresh-context read-only builder (`rival_builder_check`) reviewed the complete draft and the actual species/expansion contracts. Its remit was to report only remaining questions whose wrong answers would materially change the implementation plan. It returned exactly: **“nothing important left”**. No material question, owner clarification round or design correction was requested. The review did not edit files or run product builds.

The author reconciled milestone/requirement/decision records and checked design links, unique requirement/acceptance IDs, the compatibility matrix and preservation boundaries. At the original handoff this check made the design ready for owner review; it did not itself satisfy RIV-AC-012 or verify future functionality. The subsequent owner “approved” satisfies RIV-AC-012 through ADR-010; no gameplay is verified by that approval.

## Calls subsequently accepted in full design review

- One rival by default, with Solo retained; existing four species on each side, Harl/Peldra defaults and mirrors allowed.
- One deterministic expansion policy using ordinary commands and species rules; no resource or information bonuses.
- Explore, develop, research and settle; contacts disclose only observed facts. Diplomacy and warfare are later milestones.
- Preserve existing founding priority, including the human’s earlier-ID advantage on an exact simultaneous tie.
- New rules 4/writer 5 for the new design; old rules 1/2/3 campaigns retain behavior, including passive AI tags.

These originated as drafter proposals: the directional “yes” did not approve the detailed calls. The subsequent full-design “approved” accepts them in ADR-010. The [separate ten-task plan](../docs/superpowers/plans/2026-10-24-rival-expansion.md) is now written/self-reviewed, with owner plan review pending and Native retained.

## Preservation and evidence

Current product remains rules 3/writer 4. Last executed main-integration evidence remains 445 simulation tests, 1,181 species assertions, 275 expansion assertions and 466 legacy assertions plus 27 geometry observations. No build/test was rerun for this documentation-only draft; none of those historical results verifies rivals.

Both owner roster/research SHA256 values remain unchanged. The separately recorded peaceful-mode backlog file and its existing status paragraph are preserved outside this design commit. Recovery snapshots/stash, saves, all worktrees and source/content/test files remain untouched. No push, release or cleanup is included.

## Documentation verification

Fresh checks before handoff: 130 local document links resolve; RIV-001..007 and RIV-AC-001..012 are unique and complete; all eleven proposed schema/rules pairs match the written matrix. The 17 parent requirements reconcile to 12 Verified, 3 Implemented, Unverified, 1 In Progress and 1 Deferred. Git whitespace checking passes. No source, content or test changes are present. Both owner prose hashes match the baseline. The unrelated backlog file/status paragraph remain outside the staged design changes.
