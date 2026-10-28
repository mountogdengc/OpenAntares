# Fleet combat draft review

2026-10-28. **M9-D and CBT-AC-012 In Progress; full owner gameplay review pending.** This is a documentation-only handoff for the [complete proposed combat design](../docs/superpowers/specs/2026-10-28-fleet-combat-damage-and-withdrawal-design.md), authorized by [ADR-019](../docs/DECISIONS/ADR-019-fleet-combat-design-entry.md).

## Grounding and scope

Read current project status, AGENTS/CLAUDE/charter, requirement/acceptance/decision indexes, the owner's combat and fleet-intent notes, approved M8/M7 contracts, current content, VesselState, TurnResolver, MovementResolver, FleetState, travel/bonus calculations, officer recovery and the sole rival planner. Repository inventory at main `63ec608` found no tracked/untracked changes, synchronized cached origin/main, and seven retained worktrees. No new worktree was needed for the documentation pass.

The user selected drafting after the recommendation to define damage, morale, withdrawal and repair. They did not select an exact opponent or approve any new numbers. A nonblocking control-preference question was offered while drafting; absent a reply, the automatic control model is explicitly a drafter proposal. It is not recorded as user acceptance. Proposals include a fixed automated sentry pocket, guarded founding access, a basic escort, automatic one-exchange-per-strategic-turn resolution, deterministic damage/cohesion, explicit withdrawal and Repair/Hold arrival orders, and future rules9/writer10. Peaceful rival relations and existing ameba behavior retain their authority.

## Fresh builder check

One fresh read-only design reviewer was dispatched using the brainstorming skill's builder-check prompt. It read the draft as an engineer preparing an implementation plan, without product changes or application tests. Its returned material questions were:

1. “Does every withdrawal automatically authorize repair on arrival, including the main fleet?” The initial text defined Return-and-repair without establishing when the main fleet selected that authority.
2. “Is detachment membership frozen before interception, or recalculated afterward?” The initial text froze escape exposure but ambiguously said to recompute launch cohorts after damage, leaving newly crippled ships and flagship loss unclear.

The reviewer found no material arithmetic error in the hull-band boundaries, Navigator rounding, cohesion example, repair timeline or 46 compatibility-pair arithmetic.

## Disposition

Both questions were operational gaps within the draft's explicitly proposed control model. The drafter resolved them, rather than representing either as an owner-approved gameplay choice:

- CBT-007 now requires visible persisted On arrival: Repair/Hold selection in attack commitment. Repair is proposed initially; it authorizes finite repair for every returning cohort. Hold returns without repair. The owner can change the selection before departure or override after arrival.
- CBT-008 now freezes detachment membership before interception. A newly crippled member stays with its original cohort and may slow it. After losses, only surviving launch speed/duration is recalculated. A lost main flagship is replaced from surviving main members; if none remain, its empty fleet dissolves while detached ships stay independent. No officer inheritance or automatic cohort merge is allowed.
- CBT-AC-005/006 and the owner-review defaults table explicitly retain these cases.

Read the final wording against both findings after the correction. No finding remains unanswered in the final document. This is an author disposition, not a claim that a second independent review or an implementation review ran. Material opponent/control/economic choices still belong to the owner's full gameplay review.

## Documentation verification and limits

Fresh scoped verification runs `verify_draft.py` from the retained `.combat-design-20261028` workspace evidence directory. It checks exact proposed arithmetic for simultaneous fire, crippled shots, hull boundaries, Navigator rounding, withdrawal comparisons, cohesion, repair timing and pair counts; uniqueness/coverage of13 child contracts and12 criteria; new relative document links; owner prose/backlog/combat-note preservation; intended documentation-only Git paths; unchanged product paths and baseline HEAD; and `git diff --check`. The retained machine result records the actual check count and timestamp. These are independent reference calculations and documentation checks, not product combat tests or balance evidence.

Fresh result: **25 scoped checks passed; 21 new local documentation links resolved; command exit0**, recorded at 2026-10-28T23:38:36.976353+00:00. Command: `python C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.combat-design-20261028/verify_draft.py`. The machine result and verification script are retained at that workspace evidence directory as `verification.json` and `verify_draft.py`. All four preserved owner prose/backlog/combat-note files match their pre-edit SHA256 values. The changed paths are precisely the intended ten documentation files; product source/content/tests and current gameplay rules are unchanged.

No application build, simulation/gameplay suite, Godot launch, graphical check, physical-input test, new old-engine-future capture or dependency audit ran for this documentation-only change. Existing M8's1,547-test result and native evidence are historical product validation, not fresh evidence for the proposal. Parent requirement totals and deferred human/platform/security/export/release limits remain unchanged. No product source/content/theme/default/schema changed.

Next action: review the full gameplay proposal, especially opponent/stakes, automatic versus tactical control, new stats, persistent cohesion, interception/detachment and time-only repair. Full approval leads to a separate implementation plan. No such plan or implementation is authorized by this drafting turn.
