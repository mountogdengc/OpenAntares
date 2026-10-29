# Ship design and refitting gameplay proposal check

2026-10-29. The owner delegated the detailed proposal with “use your own judgement.” The complete [M10 design](../docs/superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) is ready for full review. [ADR-021](../docs/DECISIONS/ADR-021-ship-design-and-refitting-design-entry.md) records drafting authority; this is a documentation-only milestone entry, not product implementation or an accepted implementation plan.

## Fresh builder check

One fresh read-only C# strategy-game builder check used `gpt-6.1-sol` at high reasoning effort. Its prompt asked only for material unanswered questions that would prevent planning, excluding builder discretion and wording issues. The reviewer could consult the established M8/M9/colony authorities and source; it could not change files, build, test or repeat the completed M9 implementation review.

[Exact input version](ship-design-20261029/builder-input.json) records the proposal SHA256. [Raw response](ship-design-20261029/builder-check.md): “nothing important left.” There are no builder questions requiring disposition and no additional preliminary owner questions. One subsequent clarification makes the alternate-content warship-role binding explicit; it does not change the core gameplay defaults. No second builder check is claimed. The final design hash and document verification are in [verification](ship-design-20261029/verification.json).

## Drafter choices for review

- Two frames: Escort100 hull/6 space and Frigate180 hull/10 space. Frigates trade cost and travel speed for capacity and durability.
- Exactly one engine, one/two weapon banks and optional armor. Weapon-bank attack sums into the existing single shot; no tactical targeting system is added.
- Standard Escort exactly matches today's2400-cost/100-hull/24-attack/4-armor/speed300 ship. All new numbers are proposed prototype values, not demonstrated balance.
- Four manually researched unlocks: Focused Emitters, Reinforced Laminates, Vector Drives and Frigate Construction.
- Immutable saved designs,32 active/256 retained records, explicit archive/delete restrictions, pinned construction and exact-design fleet-request quotas. Research never upgrades old ships or orders.
- Same-frame refits require full hull/cohesion and a legal owned port, occupy its otherwise idle production slot and cost one quarter of the target's undiscounted build price, rounded up. They preserve vessel/officer identity and give no free repair, XP, salvage or Organizer discount. Cancellation preserves old equipment and banked production.
- Proposed new-campaign rules10/writer11/56 explicit pairs, with all genuine rules1..9 behavior preserved. Current product remains9/10/46; no migration is active.

## Verification and limits

The document audit checks the component/cost/rounding examples,46-current/56-proposed pair arithmetic,10 child contracts/12 acceptance rows, local links and unchanged17 parent requirement rows. The preservation capture and closing verification cover the four protected whole owner files, peaceful-mode paragraph, all8 registered worktrees/refs/stashes,10,700 unchanged tracked files and6,736 existing QA saves. The seven intentionally edited historical documentation/index files are separately listed in the audit; prior raw reports, fixtures, implementation sources and product content remain byte-exact.

No product code/content/default/schema change, gameplay build/test, native run, genuine rules9 future capture or separate implementation plan occurs in this drafting turn. Existing M9 gameplay evidence retains its original source scope. Human input/comprehension/accessibility/aesthetics, balance/long campaigns, cross-platform equality, dependency/security audits and export/release remain open. No new dependency, external asset, push, release or cleanup.

M10-D/SHP-AC-012 remain In Progress for full written design review; M10-P/M10-I and functional SHP criteria are Not Started. The owner's delegated judgment settles the starting defaults without another questionnaire. Next is review of this complete proposal before its separate implementation plan, retaining Native inline and one final implementation reviewer after later gates.
