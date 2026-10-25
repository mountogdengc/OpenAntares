# Colony-crisis/plague design review — 2026-10-24

**Historical drafting check2026-10-24; complete design subsequently approved2026-10-25 in ADR-014.** Original methods/results below remain historical, not new application verification. M6-D/CRI-AC-012 are now Verified for design; separate plan review follows, implementation Not Started. [Full draft](../docs/superpowers/specs/2026-10-24-colony-crises-plague-design.md), [drafting authority](../docs/DECISIONS/ADR-013-colony-crisis-design-entry.md).

## Authority and baseline

The owner requested a colony-crisis specification starting with plague, with gameplay review before implementation. Existing owner intent supplies timely warnings, optional limited governors, configurable notices and multiple consequential responses. The drafter proposes exact timing/costs/transport/versioning; the owner has not approved them. Existing architecture, creative/species decisions and M5 contracts remain authoritative.

Main baseline fa2a07f33e6ef2e8d24549d0dcda635221068cd8, rules5/writer6. Reused a clean registered colony worktree, fast-forwarded it fromfc29d0b to main, then created documentation branch codex/colony-crisis-design. Kept codex/colony-development-design and every earlier branch/seven worktrees. Primary main and its four owner files remain untouched; fresh exact hashes/status/index are recorded in chat .colony-crisis-design-baseline.json. No main merge, product-code/content/art/dependency change, implementation plan, application build/test or runtime execution was performed for this design.

## Fresh independent builder check

A fresh read-only C#/Godot deterministic4X engineering agent, crisis_builder_check, received only the written design/cited authority and planning-ambiguity instructions. No code/build/Git mutation or further agent was authorized. It was asked for material unanswered questions, excluding explicit builder choices and nitpicks.

Reviewed draft SHA256 before adding its completion note: `c141980380dd6c49b7da0bd3ecb06d9f95a340f38207103f295cc12c84b8f565`.

Returned verdict, verbatim:

> nothing important left

No material builder question remains pending. This checks planning clarity, not player approval, balance, implementation correctness or release readiness. No product review, independent simulation validation or human playtest is claimed.

## Independent arithmetic

[Retained checker](colony-crisis-design/verify-arithmetic.py), [results](colony-crisis-design/arithmetic-results.json). Command: python reports/colony-crisis-design/verify-arithmetic.py; exit0; **17/17 independent proposed-rule arithmetic checks pass**. The script imports/executes no game code and consumes no game RNG. It checks the two severity curves/death totals, grace, N6 cure cost, compounded output factors, cure spill, purge population/reserve rounding, civic duration/refresh/cap, cooldown schedule and immunity boundary. These are documentation consistency examples, not17 new engine tests or proof of fun/balance. The existing988-test main baseline is historical evidence, not a fresh application run for this draft.

Document checks separately verify local links,12 unique CRI acceptance IDs,7 requirement IDs,17 unchanged parent statuses, documentary-only changed paths, primary exact hashes/status/empty index and seven retained worktrees. Their record is colony-crisis-design/document-checks.json. Written acceptance remains unexecuted.

## Drafter calls for owner review

- Two harmless warning turns, then20 severity; six Active intervals to natural recovery; disease mortality at80+; early quarantine safely bridges to recovery. Deliberately predictable response arithmetic; randomness is confined to spontaneous onset/target selection.
- Cure diverts new empire research, leaving old reserves/normal targets alone, and clears one whole owned episode. No permanent new technology or physical medicine supply chain.
- Actual population cargo transmits within an empire; quarantine closes its population port system-wide. No hidden trade or foreign transmission is invented.
- Emergency sterilization loses half population/reserve and triggers temporary10%-per-level civic disruption, capped30%. All four cultures initially share this baseline; culture-specific reactions remain Deferred. No autonomous destruction.
- Default saved acknowledgement requirement is distinct from local display; deliberate opt-out is explicit/saved, critical information remains visible. Crisis authority defaults Manual and is separate from ordinary governors.
- Standard/Off setup and proposed rules6/writer7/22-pair policy apply only to future implementation; all old campaigns remain exact.

The full draft specifies shared formulas, command/Ready rollback, AI own-view behavior, zero-colony removal/report facts, cases/carrier holds, persistence/privacy and literal acceptance cases. The owner may amend any proposed gameplay choice before approval. Full approval precedes a separate implementation plan; retained Native execution preference is not asked again. No broad revolt/culture/fleet/peaceful-mode/warfare/multiplayer scope is silently added.

Subsequent approval: [ADR-014](../docs/DECISIONS/ADR-014-approved-colony-crisis-plague-design.md) records full owner approval2026-10-25. [Separate plan](../docs/superpowers/plans/2026-10-25-colony-crises-plague.md) and [self-review](COLONY_CRISIS_IMPLEMENTATION_PLAN_REVIEW_2026-10-25.md) now govern the next gate. No new builder dispatch or application run is claimed.
