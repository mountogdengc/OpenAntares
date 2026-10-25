# Colony-crisis implementation-plan self-review — 2026-10-25

**Status: full gameplay design approved; separate14-task plan ready for owner review.** [ADR-014](../docs/DECISIONS/ADR-014-approved-colony-crisis-plague-design.md), [approved specification](../docs/superpowers/specs/2026-10-24-colony-crises-plague-design.md), [implementation plan](../docs/superpowers/plans/2026-10-25-colony-crises-plague.md).

M6-D/CRI-AC-012 are Verified for design only. M6-P In Progress for plan review; M6-I/CRI-AC-001..011 Not Started. Native inline preference is preserved, one fresh final reviewer after later execution; no plan-review subagent was dispatched because the writing-plans workflow calls for author self-review.

## Scope and decomposition

Fourteen tasks: genuine unchanged rules5 capture; content/state; strict persistence; economics/own queries; atomic orders; health/onset; transport; complete turn/report transaction; human/AI preparation/attention; response overview; notification/reload; generated/labelled scenarios; late current6/core setup; full six-scenario isolated gate/acceptance reconciliation. Post-plan review/corrections and any later owner-authorized integration are explicit separate gates.

No gameplay rule or culture decision was redesigned during planning. Internal decomposition adds retained own-colony metadata because the existing DevelopmentStateValidator requires governor-history entries to reference a live owned colony, and ProjectReport/DevelopmentCompletion currently dereference that colony. Those checks are extended only under6, before purge/mortality becomes active, preserving old rules5 exactly. Purged/removed location records are kept only while referenced by saved history/receipts/active source. UI never reconstructs lost owner facts from a foreign live colony.

## Self-review performed

- All7 CRI contracts and12 criteria have named owning tasks; full design approval AC012 is already satisfied, every runtime criterion unexecuted.
- Every task has files, consumed/produced APIs, named failing cases/literal assertions, focused/full checks and a commit. T14 is a verification task rather than new product behavior. Future task completions precede final review.
- Corrected forward dependencies during self-review: T2 owns authority/history records; T4 owns port queries/order snapshots/complete preview facts; T5 introduces retired-colony validation with purge and the restricted automatic-command path; T6 reuses loss removal; T7 owns transport execution hooks; T8 calls each once. No stub interface is left to a later task that an earlier one needs.
- Pure Scale and full economy tests pin factor order, aggregate tiny research and final spill. Display examples use Format.Points(450)=4.5, not raw450 as player-visible units.
- Arrival contact uses surviving post-health Active sources, with frozen departure/port facts; warning-to-Active transition has already had its two full warning opportunities. New exposures are created only afterward. Cure/inspection suppress retagging; natural closure waits for transport. This is explicit phase interpretation, not a new spread channel.
- The five Review Focus classes each map to exact tests: removed colony/completion; boundary port/cure/recovery; cross-colony stale destructive preview; aggregate rounding/final cure; local display/reload/other-human/receipt attention.
- Activation is intentionally late. T12 explicit6 generated turn0 sources have no mid-run injection; T13 repeats from actual current New Game Start. Existing current Colony/Rival economic loops choose Off through real controls, while genuine old snapshots prove unchanged historical behavior. Existing Rival80-turn bound is retained; new crisis scenarios have120-turn bound.
- Existing six-scenario opt-in/final ordinary assembly exclusion and real metadata negative control are explicit; currently there are five classes and the sixth does not exist yet.
- Plan contains6309 words; approved spec contains4010. The plan chooses boundaries/tests/contracts and omits implementation bodies; actual feature work remains for approved execution.

## Reference arithmetic and genuine source

[Independent values](colony-crisis-plan/reference-values.json), [reproducer](colony-crisis-plan/check-reference.py), [check result](colony-crisis-plan/reference-checks.json). Reproducer exits0:3/3 independent PCG vector cases match their stored planning references, and retained source bytes/metadata match. This is a Python arithmetic transcription of the existing PCG32 contract, not a C# unit-test run or proof of future crisis behavior. T2 will compare actual C# outcomes to those literals.

Located source: qa-20261025-033820-304-generated-colony-harl-developed.json, schema6/rules5, turn24, six colonies, one active expansion_v1 AI. SHA25633221d961e8c52722639beb43386d95b22331922dc61495d3f1faf772bbea4a3. Creator productfc29d0b with actual UI provenance in retained main Colony results. T1 must copy/preserve it and capture four ordinary-command future states under unchanged code before any rules/writer/core change. **That future capture has not executed in this planning turn.**

## Preservation and honest limits

Main remains fa2a07f33e6ef2e8d24549d0dcda635221068cd8 with exactly its original four owner changes and empty index. No product source/content/art/dependency/test implementation/default change; no application build/test/runtime; no main merge/push/release/cleanup. All seven worktrees, original branches, QA/saves/stashes/recovery retained. [Document/preservation check](colony-crisis-plan/document-checks.json) verifies current links,14 tasks,5 review-focus entries, named interfaces, parent17-row counts and exact primary hashes/status. Final commit/preservation audit is retained in chat .colony-crisis-plan-final.json.

Existing988-test/M5 runtime evidence remains historical and is not presented as a fresh crisis result. Independent arithmetic/self-review do not prove balance, comprehension, physical input/accessibility, other platforms, clean install/security/export or release readiness. Their earlier deferrals/limits remain unchanged. Next: owner review of the separate plan, preserving the chosen Native method.
