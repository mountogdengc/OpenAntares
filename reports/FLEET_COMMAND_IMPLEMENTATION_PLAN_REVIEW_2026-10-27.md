# Fleet Command/admiral implementation-plan self-review

2026-10-27. **Documentation review complete; owner plan review pending.** [ADR-018](../docs/DECISIONS/ADR-018-approved-fleet-command-and-admirals-design.md) records the owner's complete gameplay approval at149c40c. The separate [17-task plan](../docs/superpowers/plans/2026-10-27-fleet-command-and-admirals.md) has been checked against the approved FLT-001..009/FLT-AC-001..012 specification and actual current source. No implementation, future capture, new C# tests, native fleet run or implementation code review occurred here.

The writing-plans skill calls for self-review of the plan; no additional agent was commissioned. Native inline, no per-task agents and one final fresh whole-branch implementation reviewer remain the chosen execution method after separate plan approval. The earlier builder reports remain design evidence, not implementation review.

## Source and task contracts checked

Current integration seams were read in CommandProcessor, TurnResolver, MovementResolver, governor planner/context/coordinator, VesselProduction/EconomyCalculator, state/view/content/save code, existing PCG/scout seed derivation, GameSession, GameScreen/NewGameScreen and the native validation runner/checker. The plan names current files and the exact new responsibilities/entry points; it does not assume a nonexistent SaveGameSerializer, free shipyard pool or separate admiral AI.

| Check | Disposition |
| --- | --- |
| Cohesive scope | One dependent fleet/request/officer slice,17 testable tasks; future combat/design/refit/diplomacy automation stays outside. |
| Pre-change evidence | Task1 must capture actual rules7 sources and4 old-engine futures before any product/content change, preserving all older archives; no new capture claimed now. |
| Construction authority | Existing governor selector alone; temporary queue-only authority never reaches stored template; active project/token/reserve/priority and ordinary cargo legality pinned. |
| Shared turns | Exact crisis→request→governor→fleet→scout preparation and existing resolution insertion points, bounded work,12 phase fault hooks and enclosing-call rollback. |
| Safety/manual authority | Known/observed unsafe rally pauses before dispatch/completion; retains living bindings/transit and no retry/replacement; explicit legal resume, no implicit encounter consent. |
| Gameplay accounting | Literal travel315/330, price760/720, survey0/100/200, XP2/1/8, recovery49/50 and69/70, full subsequent-turn timing and Wenth/source-body founding distinction. |
| Information/transactions | Own immutable context, paired hidden-world tests, atomic stale bulk actions, no-op reference/Ready and two-human rollback tests. |
| Saves/defaults | Writer9/exact37 pairs, old injected metadata rejected, no(8,8), current-rules activation late, no automatic upgrade/new research in old campaigns. |
| Data/randomness | Original profiles/catalog/culture capabilities fixed, independent PCG domains/vectors published and per-empire local IDs, no old RNG or global-ID shifts. |
| Presentation/evidence | Actual central controls at800x600/1920x1080, generated campaigns separate from rare/scale fixtures, all8 native gates/normal assembly exclusions/log and PNG audits. |
| Review method | Self-review now; one final fresh implementation reviewer after combined validation, regression-first corrections, no repeated method/design approval. |
| Status honesty | Only M8-D/FLT-AC-012 Verified; M8-P In Progress and M8-I/functional criteria Not Started; old1398/4491 results remain historical M7 evidence. |

## Gaps corrected during self-review

- New research could leak into an explicitly generated earlier-rule game using full updated core content. Added one version-aware availability contract and named command/validator/economy/AI/UI filters, while old saved content stays unchanged.
- Navigator's bonus test preceded the planned bonus helper. Assigned EffectiveSpeed to the fleet-travel task, request prices to the economy task and SurveyResearch to progression; no future helper dependency is left as a knowingly failing suite.
- Commissioning command tests initially required a resolver countdown implemented later. The command task now asserts the exact recorded deadline; the recovery/lifecycle task owns actual turn11/12 execution.
- Native UI evidence precedes default activation. The first window gate explicitly mounts an actual generated rules8 GameSession; the later activation gate owns the default New Game UI workflow.
- Existing ameba attention tokens did not contain officer/cohort consequences. Added a rules8-only canonical own-fact fingerprint, exact repeat-read equality, next-snapshot unlock/recovery display, legacy codec absence and strict new-codec validation. Hull-risk formulas still use base role speed; Navigator duration does not improve hull survival.
- Corrected the actual existing harness helper inventory to `FrozenContent` and `ScoutUiControlChecks`; the eighth Fleet type must be included/excluded in real assemblies, with any genuinely added helper explicitly enumerated.

Each of the plan's five Review Focus cases maps to named behavioral tests in its owning tasks. All12 FLT acceptance IDs have a coverage row; the design criterion is already approved and is not represented as runtime evidence. Class/DTO/phase contracts are specified without prescribing full code bodies or unrelated refactors. Failed tests, failed native attempts and raw archives are retained rather than overwritten.

## Verification and limits

[Machine document checks](fleet-plan/document-checks.json) pass89/89: task/step/reference/number/authority checks, main HEAD/status/owner SHA256 verification, seven-worktree preservation and unchanged product source. These are documentation/arithmetic/preservation checks, **not simulation tests**. The [first checker attempt](fleet-plan/document-checks-attempt-01.json) is retained:87/89 passed; two overly literal wording matches were corrected to the plan's equivalent wording, without a gameplay change. `git diff --check` passes; final staged-path/diff review is required before this documentation commit. Parent requirement register stays17 rows:12 Verified,3 Implemented, Unverified,1 In Progress,1 Deferred; no M7/M6/human/platform/security/export/release limit changes.

Main stays c554797/rules7/writer8/29 pairs. All four owner edits remain byte-exact and uncommitted; main index stays empty. All seven worktrees/branches and old QA/recovery remain. New recovery vectors were calculated by independent integer arithmetic during planning, not by running new gameplay code. New source fixtures/futures and all17 implementation task checkboxes remain unexecuted.

Next: owner review of the concrete separate plan. The complete design and execution method are already approved. No implementation or main integration/push/release/cleanup is inferred from design approval.
