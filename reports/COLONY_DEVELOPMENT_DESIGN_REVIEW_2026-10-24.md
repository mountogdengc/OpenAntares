# Colony-development design review — 2026-10-24

Status: full proposed design drafted and builder-checked; owner review pending. No product implementation or implementation plan. [Design](../docs/superpowers/specs/2026-10-24-colony-development-design.md), [direction record](../docs/DECISIONS/ADR-011-colony-development-design-entry.md), [future crisis/fleet note](../docs/design-notes/2026-10-24-delegation-crises-and-fleet-intent.md).

## Evidence and authority

The owner describes foundational buildings, later focus, scalable delegation, planet variety and editable/custom templates. The owner explicitly delegates judgment beyond examples. Direction/drafting authority does not approve all proposed values, thresholds, ordering or version changes. M5-D In Progress; M5-P/M5-I Not Started. All current code/content/assets/dependencies and earlier accepted contracts remain unchanged.

A fresh-context read-only engineering builder checked the written proposal and relevant actual source. It reported only: **nothing important left**. [Raw result](colony-design-20261024/builder-check.txt). Its progress note said it checked narrow semantic edges against source. No product files, Git state or build artifacts were changed by the reviewer, and no review agents were delegated by it. No owner-only questions remained. While the check ran, the drafter clarified that finite player vessel orders remain permitted and that their workforce must respect ordinary embark cargo requirements; this applies the existing contract rather than allowing governors to invent vessel orders. Final wording also makes explicit that workforce control needs a template, revision history is bounded and automatic orders do not pause themselves as manual overrides; these clarify the stated semantics without adding product scope.

Independent integer calculation checked all seven literal workforce/output examples:7/7 matched. [Arithmetic](colony-design-20261024/arithmetic-fixtures.json). This is design arithmetic, not engine execution, runtime acceptance or balance evidence. No new application restore/build/test, visual sample or gameplay result is claimed. Retained M4 main648/Rival checks and branch four-scenario gates remain historical results at the prior source.

## Drafter calls for owner review

- Additional Balanced default beside the owner's suggested Industrial/Research/Growth; template copies/revisions/custom presets with separate optional automation controls.
- Nonmodal size guidance at6/11 colonies; no forced takeover. Explicit opt-in inheritance for future colonies.
- Foundation preferences and finite queues, retaining active projects and banked reserve; limited governor authority, predictable scoped support/weight policy and human Ready preparation.
- Small three-technology/three-building next tier with stated prototype costs/effects; no permanent role locks, new economy resource or exclusive tech picks.
- At least eight cosmetic surface signatures per type/environment, stable/private across reload; original bounded presentation, not new planet bonuses.
- Proposed rules5/writer6 with sixteen explicit accepted pairs and genuine old-campaign capture before implementation; no upgrades now.
- Crisis responses, culture, notification emergencies and fleet/scout intent remain future scoped designs. Current routine-governor notification preferences do not pretend to implement those threats.

These choices can be amended in full-design review; none is represented as already accepted owner detail. Build clarity does not substitute for that review.

## Preservation and process

Design-only codex/colony-development-design is isolated from main10cb0b4 in the existing external C .worktrees location. Native create_worktree returned Not a git repository for the C chat context; a scoped Git fallback was used, preserving all six previous worktrees. Seven are now retained. The external directory cannot be accidentally staged in F primary. A fresh snapshot records primary HEAD/status and exact hashes of the four owner files, with final comparison before commit. Native attachment may need repair later; this does not alter product code.

No worktree setup/application baseline run is needed for this documentation-only task: no implementation or dependencies are being prepared, and new test results would not validate a design. The ordinary prior648-test baseline is retained honestly as historical evidence. No new scope, migration, merge/push/release, recovery removal or cleanup is inferred. Human/platform/security/release limits remain open.

## Handoff

Review the complete design and drafter calls. Full design approval or amendments precede a separate implementation plan; retained Native method is not a new choice. Parent REQ-017 remains In Progress and17-row totals stay12 Verified,3 Implemented, Unverified,1 In Progress,1 Deferred. Design documentation can be committed for review; no product code begins at this stage.

## Subsequent full owner approval

The owner replied “approved” after the full specification/checkpoint67c2853 was presented. [ADR-012](../docs/DECISIONS/ADR-012-approved-colony-development-design.md) now satisfies M5-D/COL-AC-012; earlier review-pending statements above are historical. The separate [twelve-task plan](../docs/superpowers/plans/2026-10-24-colony-development.md) is written/self-reviewed and awaits owner plan review; no implementation has started. Native remains selected. This approval accepts design, not gameplay/balance/aesthetic/release evidence.
