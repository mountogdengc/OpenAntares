# Scout survival / encounter-avoidance design review — 2026-10-25

**Drafting evidence only. Full gameplay approval pending.** [Complete proposed rules](../docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md), [ADR-015 drafting authority](../docs/DECISIONS/ADR-015-scout-survival-design-entry.md).

## Baseline and authority

The owner requested continuation from PROJECT_STATUS.md, bounded scout survival/encounter-avoidance rules grounded in recorded fleet intent and existing exploration/rivals, preservation of current decisions/uncommitted edits, and gameplay review before implementation. M6 is integrated/locally verified on main42e1282b9f38fd8d51d4c99ed0f7481437efcee1. Product remains rules6/writer7; no active combat/interception model exists.

The clean registered `.worktrees/colony-development-design` at M6 sourced8bef99 was reused by creating `codex/scout-survival-design` from main42e1282. All seven registered worktrees and existing branches, including `codex/colony-crises`, remain. No new worktree, main integration, push, release, cleanup or product-code/content/art/dependency/default/writer change occurred. Existing source files were read to confirm movement, observations, AI and crisis phase boundaries; no application build/test/runtime was executed.

Primary status before drafting was exactly three tracked modifications (`PROJECT_STATUS.md`, `docs/SPECIES_ROSTER.md`, `docs/research/faction-survey.md`) and untracked `docs/FEATURE_REQUESTS.md`, with an empty index. Its status modification is solely the owner's peaceful-mode paragraph. All four complete file SHA256 hashes were recorded before branch creation/drafting and are checked afterward. The primary status file is not updated by this draft; this branch's PROJECT_STATUS.md holds the continuation record and does not copy/commit that owner paragraph. Prior backups/stashes and genuine old campaigns/QA remain untouched.

## Fresh independent builder check

The brainstorming skill explicitly requires a fresh builder reviewer. One read-only deterministic C#/Godot 4X engineering subagent, `scout_builder_check`, received the written specification and cited authority, and instructions to report only material unanswered planning questions. No edits, product implementation, build, Git mutation or further agent was authorized.

Verdict, verbatim:

> nothing important left

After the review, a self-check clarified an unreachable illustrative case: a truly unknown star has no recorded foreign presence under the existing knowledge rules. The AI fallback example now uses no remaining unknown stars and a permitted age-eligible resurvey, rather than implying known presence at an unknown star. No new scope or owner decision was introduced; no second review was commissioned. This review assesses planning clarity, not owner approval, implemented behavior, balance or human comprehension.

## Drafter choices for owner review

- Keep rival encounters peaceful. This is a first movement/authority slice; it does not demonstrate survival against lethal danger. Actual threats, sensors, interception and speed/chance escape formulas need a material scope/design decision.
- Keep Observe as the initial posture. Avoid encounters is explicit per-scout movement authority; human outward exploration remains manual. AI opts into the same posture through shared commands only in proposed rules7.
- Exclude known current/last-seen foreign colonies and currently visible stationary foreign vessels. Unknown destinations remain legal; no invisible threat intelligence or lasting foreign-ship tracking.
- Retreat first toward a permitted own-colony system, otherwise a surveyed system, using travel duration then StarId. A permitted manual departure takes precedence; no fallback produces an explained wait.
- Preserve ordinary full arrival survey/contact before any next-planning withdrawal, direct committed travel, speed/costs, colony governors and disease transport. Opportunity cost is elapsed travel and lost current observation, without invented hull-loss risk.
- Proposed rules7/writer8 explicitly adds seven pairs to the existing22, preserving29 total and genuine rules1..6 continuation. These versions are prospective; no defaults or writer changed.

The owner can amend any proposal. M7-D/SCT-AC-012 In Progress; M7-P/M7-I and functional SCT acceptance Not Started. Full gameplay approval precedes a separate implementation plan; Native preference remains retained after those gates.

## Fresh document verification and limits

[Retained checker](scout-design/verify-documents.ps1) and [machine results](scout-design/document-checks.json) verify the local links in the new specification/ADR/report, six unique child requirement IDs, twelve unique child acceptance IDs, the unchanged17-row parent register/counts, prospective29-pair arithmetic, ordinary1/2-turn route examples, documentation-only diff, original primary file hashes/status/empty index, and retained seven-worktree paths/main baseline.

Fresh command in Windows PowerShell from the drafting checkout: `& ./reports/scout-design/verify-documents.ps1`; exit0. Results:15 local links,6 child requirements,12 acceptance IDs,17 unchanged parent rows (12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred),29 proposed compatibility pairs and4 independent travel examples; all four original primary file hashes exact, original primary status/empty index/main SHA unchanged, seven worktree paths and prior branches retained. `git diff --check` also exited0. These checks ran after reconciling the builder verdict and the unknown-star example.

The checker executes no game code and no gameplay PRNG. Its arithmetic and written scenarios are document consistency checks, not application tests or survival/balance evidence. Existing1,265-test/six-scenario M6 results are historical retained evidence, not a fresh run for this draft. Human/platform/release limits and existing acceptance statuses remain open/unchanged.
