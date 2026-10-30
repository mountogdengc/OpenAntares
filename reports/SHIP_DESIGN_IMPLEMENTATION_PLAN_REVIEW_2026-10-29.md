# Ship design implementation plan author review

2026-10-29. The owner approved the complete [M10 gameplay description](../docs/superpowers/specs/2026-10-29-ship-design-and-refitting-design.md) and requested its implementation plan. [ADR-022](../docs/DECISIONS/ADR-022-approved-ship-design-and-refitting-design.md) records the exact pre-approval source/hash. The separate [17-task plan](../docs/superpowers/plans/2026-10-29-ship-design-and-refitting.md) is ready for review. This is the author's self-review required by writing-plans, not a fresh subagent or implementation review.

## Spec coverage and task boundaries

All10 SHP contracts and12 acceptance criteria map to owning tasks in the plan's coverage table. Tasks1..17 each have exact files, consumed/produced interfaces, five checkable steps and a commit boundary. Task1 captures genuine old-engine rules9 sources/futures before any product edit. Tasks2..13 cover content, state, one resolver, catalogue, production/requests, refit acceptance/resolution, transactions, strict persistence and private views. Tasks14/15 prove actual UI controls, Task16 activates10 only after prior gates and proves four generated paid campaigns, and Task17 runs the combined gates plus one final fresh implementation reviewer/correction pass.

The plan retains Native/no per-task agents. No new execution-method choice or final-review dispatch occurs while planning. Whole-branch integration/push/cleanup remain later owner decisions under the existing preservation instruction.

## Step and interface scan

The five Review Focus failures each have named owning tests: archive after commitment; completed request history after refit; forgotten role-profile consumers/M7 risk inputs; late refit invalidation/payment; hidden foreign changes and final-Ready rollback. The tasks include exact numbers from the approved tables and representative executable assertions, rather than unspecified test promises. Missing APIs/compiler failures do not count as meaningful behavioral RED.

Self-review corrections before handoff:

- The final source-path audit found EmpireState declared inside `State/GameState.cs`. The plan now modifies that class in place rather than naming a nonexistent EmpireState.cs file; the initial audit failure is retained separately.

- The content loader needs derived recipe validation before the later resolver task. Pure Calculate/data records therefore belong to Task2; Task4 adds the equipped state/view resolver and checked refit price, avoiding a circular dependency or duplicate calculator.
- Task3 creates only the Session fixture. SaveDesign is added with Task5's real command, so earlier tasks do not depend on a future dispatcher API.
- Paused jobs still reserve the production slot and enter the snapshot for missing-ship cleanup. Only eligible Active jobs can pay/install, and Resume is explicit.
- Refit forecasts bank production provisionally. The late phase returns adjusted actual TurnEconomy for the receipt; a lost/paused job cannot charge provisionally or permit another completion.
- Completed request slots retain historical original design facts. A later paid refit neither invalidates them nor awards XP. Active bindings still demand exact current design.
- The resolver is applied to actual combat/travel/repair/forecast paths and copied through own-only planning frames. Established M7 risk-speed inputs and frozen trips/volleys remain explicit exceptions; hidden rival state never enters own tokens.
- Copied event facts include recipe/stat snapshots, so strict persistence validates them without requiring a deleted live catalogue record. Exact quotas/targets and all56 save pairs are defined, with no10/10 inference.

No gameplay values, scopes, research choices or compatibility targets were changed from the approved specification. Type names/signatures/counter ownership and compile-time introduction order were checked across task consumers. The plan is about one coherent ship-design/refit subsystem; splitting unrelated new projects or rewriting existing architecture is unnecessary.

## Verification and preservation

[Machine verification](ship-design-plan-20261029/verification.json) records the actual task/checkbox/coverage/link checks, source-path inspection, unchanged parent17 rows, exact pre-approval hash and current product predicates/schema. [Before manifest](ship-design-plan-20261029/preservation-before.json) retains actual primary checkout hashes, four protected owner files, peaceful paragraph, QA hashes, refs/worktrees/stashes. All pre-existing files outside the nine named documentation edits are checked byte-exact; no raw historical report/fixture or gameplay source/content changed. All8 worktrees, unrelated refs and recovery/QA remain.

This planning turn ran document/preservation checks only. It did not build/run the game or test suite, capture genuine future behavior, create an implementation worktree, change current rules9/writer10/46 or introduce product code/dependencies/assets. Existing M9 validation retains its original source/count scope. Human input/comprehension/accessibility/aesthetics, balance/long campaigns, cross-platform equality, security/dependency audits and export/release remain open. No push, release or cleanup.

M10-D/SHP-AC-012 are Verified for design only; M10-P is In Progress pending review of this concrete plan; M10-I/functional criteria Not Started. Next is owner confirmation that the plan captures the accepted slice, then retained Native execution. The writing-plans skill explicitly requires waiting for that review before implementation.
