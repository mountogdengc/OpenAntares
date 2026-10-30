# Fresh M10 implementation review brief

Scope: the full approved17-task ship design/refitting implementation, from accepted planning base4c58403b107063a616645905b6f6f86f86676ca6 to the tested source HEAD named in the dispatch. Current new games10; writer11; only56 explicit schema/rules pairs. Retained rules1..9 keep their old semantics and raw future oracles.

Plan: docs/superpowers/plans/2026-10-29-ship-design-and-refitting.md

Spec: docs/superpowers/specs/2026-10-29-ship-design-and-refitting-design.md

Canonical ledger: .superpowers/sdd/2026-10-29-ship-design-and-refitting/progress.md. Read every Ruling line; decisions were carried forward across tasks. The owner chose Native inline, no per-task agents, exactly one fresh final implementation reviewer, one material regression-first fix pass, and no automatic re-review. This is that single review. Do not delegate. Do not modify files, index, branch, HEAD or raw evidence; do not run builds/tests that could collide with the gate runner. Inspect existing raw results and source. Do not inspect whole multi-megabyte baseline JSON in a single output; manifests and specific relevant entries suffice.

The skill-generated full review package includes large retained raw archives. Read its header/commit/stat sections and the companion code-only diff, then inspect source directly as necessary. All scripts use Powershell in this environment. Read AGENTS.md and CLAUDE.md. Protected owner prose must remain unchanged. No merge/push/release/cleanup is authorized.

## Review focus (verbatim)

1. An archived design is still queued or targeted by an accepted refit: finish its pinned recipe, refuse new selection and never delete referenced data. `ArchiveAfterCommitStillFinishesPinnedRecipe` in Tasks6/9/12.
2. A completed request's vessel is later refitted: keep its historical original design/XP, while active slots still demand exact current design. `CompletedRequestSurvivesLaterRefitWithoutNewXp` in Tasks7/9/12.
3. A new engine or second bank reaches a forgotten role-based call site: preview/combat/travel/repair must use installed stats, while M7 odds and frozen trips keep their existing contracts. `LoadoutCallSitesAgreeWithoutChangingAmebaOdds` in Tasks4/10/16.
4. A refit becomes invalid after economic forecasting: preserve banked output, make no provisional charge/completion, pause/cancel honestly and do not let the colony finish another target. `RefitLateInvalidationDoesNotSpendOrDoubleComplete` in Tasks9/11/12.
5. A second human changes a hidden catalogue/job or ownership context while another is Ready: tokens/views reveal nothing foreign; stale commands and late failure roll back every counter/reserve/consent. `HiddenRivalRecipeDoesNotChangeOwnPreview` and `FinalReadyRefitFailureIsAtomic` in Tasks8/11/13.


## Evidence classes
- Task1 frozen old engine:72 genuine stage sources/144 future checkpoints,11 labelled supplements plus1 action source; total84/168, hashes locked. Never regenerate.
- Task12 full56 pair grid and strict embedded codecs; no initializer on load; completed request and historical copied facts survive paid refit/legal deletion.
- Task15 actual paid queue/request/refit controls at800/1920, labelled prepared cases, regression for old Start preview after Cancel.
- Task16 four untouched actual Main New Game Small42 Solo species campaigns, research7200, two advanced hulls5600, two refits1400, automatic sentry battle/withdrawal/repair, later trip/volley using changed profile. Actual Main catalogue/queue/request/active-refit checkpoints match3 uninterrupted future turns. Paused Main checkpoint is a separate labelled supplement.
- Task17 combined suite, focused compatibility/atomicity, ten native scenarios sequentially. Each native runner captures actual opt-in10+2 negative control and ordinary rebuild/exclusion. Read final baseline index and per-scenario JSON/log for actual status/counts.
- Task17 preservation audit separately hashes primary10720 tracked files, execution prior files outside authorized diffs, protected four owner files, original6736 QA saves, both84/168 archive manifests; verifies peaceful text, seventeen parent rows, all9 worktrees and unrelated refs/stashes.

Automated Windows controls/signals do not establish physical input, comfortable reading, accessibility, comprehension, aesthetics, balance, long campaigns, cross-platform behavior, security/dependency audit or export/release readiness. Those are explicit limitations, not claims. Judge functional defects by their actual effect even where spec is silent. Report Critical/Important/Minor with exact path/line, triggering input, effect, and suggested regression; list every considered behavior you decline to judge with reason, or explicitly none. Do not turn deferred owner UX/balance acceptance into falsely claimed completed acceptance.
Additional author uncertainty to inspect: when a currently bound ship-design production target is archived, confirm the project picker still displays the committed identity while excluding it from new selection; compare the production details and picker rather than relying only on the saved target remaining pinned.
Final status/index documents and the final validation report are Task17 disposition outputs, pending this review and its one permitted fix pass. Existing older planning paragraphs remain historical; do not infer a claim of completed review from them. The product code and executed test evidence are the subject of this review.
