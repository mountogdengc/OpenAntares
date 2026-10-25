# Scout/space-ameba implementation plan review — 2026-10-25

**Full design approved; separate plan awaiting review.** [ADR-016](../docs/DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md), [approved design](../docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md), [15-task plan](../docs/superpowers/plans/2026-10-25-scout-survival-and-ameba.md).

## Authority and preservation

The owner replied “approved” after the full amended specification and principal ameba rules were presented. M7-D/SCT-AC-012 are Verified for full design approval. M7-P In Progress for this separate plan; M7-I/functional SCT acceptance Not Started. Native inline with one final fresh whole-branch reviewer/no per-task agents remains the retained execution method after plan review. No product code/content/default/writer change or implementation execution is authorized by this planning checkpoint.

Work continues in the existing clean registered `codex/scout-survival-design` checkout from approved design source738c68e; all seven worktrees and previous branches remain. Primary main42e1282 and its four owner files/index remain untouched. The approved pre-editorial specification hash `aee687d783c041f0625c6c08216f9d7e706de6830f3b032b963d1a7f48ba8956` is recorded in ADR-016; approval annotations change status, not numeric/behavioral contracts. No main merge, push, release, cleanup, stash application or save overwrite occurred.

## Author self-review

The writing-plans skill requires the author to self-review, not dispatch another plan agent. No subagent was spawned for this plan. The earlier fresh design builder check remains historical design evidence; it is not misrepresented as review of the implementation plan.

| Check | Result / correction |
| --- | --- |
| Approved spec coverage | All7 SCT requirements and15 acceptance IDs map to owning tasks; design SCT-AC-012 remains distinct from unexecuted functional criteria. |
| Dependency order | Task1 freezes genuine old behavior before product changes. Immutable state/token/event and command-record contracts move to Task3 so later projections/reports have no undefined forward types. |
| Step granularity | Each task has behavioral RED, implementation, focused GREEN/Suite and explicit commit/evidence gates; compile failures do not count as semantic RED. |
| Interface consistency | Exact file locations, command names, records, projection/preparation signatures, codec dispatch and phase context are named once and reused. EmpireState is correctly located in State/GameState.cs. |
| Projection recursion | An internal base projection avoids ScoutQueries calling public enriched projection recursively; validation uses effective-action checks only after structural validity. |
| Human consent | An automatic action token includes its effective route before materialization; executing that same route preserves consent, changed consequences do not. Cargo holds/closed ports cannot silently consume warning or authorize stationary death. |
| UI gate dependency | Fixed a proposed Task12/13 cycle: Task12 adds labelled actual control checks through the existing Crisis scenario, then Task13 independently adds the seventh full scenario. |
| Compatibility | Nullable explicit new setup preserves old call sites without inferring rules7 consent; genuine rules6/older fixtures and29-pair rejection matrix remain mandatory. Writer8 follows capture/codecs, current defaults follow working controls/gates. |
| Valid rare fixtures | Exposed is a successful scout Study state; slow200 is an embedded scout variant, not an invalid Exposed founder. Destroyed vessels use immutable retired report facts. |
| Review Focus | Five concrete consent/hold/research/last-colony/retired-state cases each have owning task assertions. |
| Proportion / scope | Plan fixes builder interfaces, literal reference values and execution evidence rather than writing product function bodies. No warfare/roaming/art/dependency scope or changed approved gameplay. |

## Read-only source inventory and independent reference arithmetic

[Catalog](scout-design/rules6-source-catalog.json) locates/hashes32 actual generated retained UI save files from M6's fresh main Crisis/Colony results. Both results files match the historical integration-summary hashes. Source paths/product SHA, source hashes, turns, schema/rules, crisis mode and entity counts are recorded. This reads existing bytes only. **No genuine rules6 future capture has run.** Task1 will preserve those sources, capture four old-engine future states/commands for each, add16 explicitly labelled unchanged-engine generated reference cases and a separate held-cargo case, before any product edit.

[Independent PCG vectors](scout-design/reference-vectors.json) use a standalone Python transcription of the read existing PCG32 algorithm. It matches the existing PCG42/54 six-value reference and computes placement/owner1/owner10 states, increments, raw uint outputs and bounded results, plus reachable threshold states49/50/74/75/94/95. For seed42 owner1 first risk values are55,85,11,23: a core75% Study and95% next escape both survive under those labelled inputs. No game assembly, live campaign RNG or application state was executed/mutated. Actual C# tests must independently verify these vectors during implementation.

## Document checks and limits

Retained checker commands from the documentation checkout:

```powershell
python ./reports/scout-design/verify-plan.py
& ./reports/scout-design/verify-documents.ps1
git diff --check
```

[Plan checks](scout-design/plan-checks.json) record task structure/count, all requirement/criterion coverage, exact existing target paths, local file links, PCG reference/source inventory and plan hash. [Document/preservation checks](scout-design/document-checks.json) record current spec hash, unchanged17 parent rows/counts, arithmetic, exact primary four hashes/status/empty index/main SHA and seven retained worktrees/branches. These are documentation/provenance/reference checks, not engine tests, future continuation acceptance, balance or release evidence. Fresh execution counts/exit codes are recorded after running these commands; no application build/test/graphical run has occurred in this planning turn.

Next action: owner reviews/confirms the complete separate plan. Do not re-ask design approval or Native method. After plan approval execute Task1 first and continue Native through all15 tasks and one final independent review; preserve every source/recovery/failed attempt. Local main integration remains a separate later owner choice.

Fresh Windows PowerShell verification: all three commands above exit0. Plan checks confirm15 tasks,34 existing target files,6 plan/approval links, all7 requirements/all15 criteria mapped and32 source saves hashed; document checks confirm21 specification/decision/review links, all17 parent rows/status counts unchanged, byte-exact four primary files, unchanged main/index and all seven worktrees/branches. Approved gameplay sections1..8 are byte-identical to the approved source after text decoding; only approval/status annotations changed. Repeat reference/check output is deterministic. No application build/test/runtime or actual future capture ran.
