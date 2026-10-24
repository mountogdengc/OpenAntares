# Colony implementation-plan self-review — 2026-10-24

Status: complete, inline self-review; owner plan review pending. [Full design approved in ADR-012](../docs/DECISIONS/ADR-012-approved-colony-development-design.md). [Twelve-task plan](../docs/superpowers/plans/2026-10-24-colony-development.md) is not executed. The owner-approved Native method is retained, with one fresh whole-branch review after implementation. No planning reviewer/implementer agents were dispatched.

## Coverage and dependencies

| Approved contract | Owning tasks | Acceptance |
| --- | --- | --- |
| COL-001 foundations/new tier |2 definitions/fixtures;11 core activation;12 actual gameplay |001,011 |
| COL-002 templates/delegation |2 models;3 commands;8 editing;9 bulk management;12 |002,006,011 |
| COL-003 finite queues |3 ownership/edit;4 priority;5 completion;6 persistence;8/12 controls |003,011 |
| COL-004 scoped workforce |4 literal values, extreme/flat/zero-rate/species/cargo;5 atomic scheduling |004,005 |
| COL-005 Ready/atomicity |3 no-op/manual authority;5 Ready integration/reports;6 reload;12 |005,010,011 |
| COL-006 views/reports/preferences |4 private data;5 history;6 persistence;8/9 controls;12 |006,007,009,011 |
| COL-007 visual identity |2 descriptors;6 persistence;7 stateless generation/knowledge;10 portraits;12 |008,009,011 |
| COL-008 compatibility |1 genuine capture before change;2 gating;6 sixteen pairs/presets;7 old generation;11 default switch;12 |010,011 |
| COL-AC-012 full design approval |ADR-012 records owner's approved after the presented full document |Verified for design; no runtime assertion |

All COL-AC-001..011 have named unit/actual-control/generated/labelled evidence owners. Their implementation status remains Not Started. Twelve tasks each have Files/Interfaces and five unchecked action/result steps. Pure policy, atomic publication, persistence, UI and appearance have separately testable deliverables but share one approved compatibility/management slice. Independent future crises/fleet work remains deferred.

## Corrections made during self-review

1. Move immutable own-development projection population into Task4 with policy, rather than leaving an undeclared dependency on later appearance Task7. Define exact view/draft/snapshot/queue/report types once, preserving old positional constructors.
2. Pin Ready preparation versus ordinary resolution boundaries, internal provenance/injected preparation signature, first-before/latest-after pending records, manual override and no-op semantics; use two-human fixture for a saved nonfinal Ready state, not a one-human game that resolves immediately.
3. Make finite explicit vessel orders distinct from governor-invented vessels; retain cargo/maturity/full-capacity/blocked-target rules. Literal Peldra/Harl/Growth outputs and extreme/zero-rate cases remain scoped and bounded.
4. Keep live core JSON and CurrentVersion4 unchanged until Task11; create complete proposed content only in test fixtures first. Capture genuine current rules4 before any source/default changes, and pin existing compatibility expectations rather than editing their results.
5. Define exact FNV-1a64 encoding/version and five independent vectors, descriptor ownership/survey boundaries, twelve type/environment gallery pages of eight signatures each, and the128 portrait resource bound.
6. Add an explicit reproducible ordinary-assembly metadata script for all five harnesses, including a real opt-in negative check; no permanent extra test project/dependency or reliance on an external four-harness helper.
7. Keep the public save API, extend the exact sixteen schema/rules pairs, move unsupported-schema6 probes to7 rather than deleting them, and make preset import version/UTF8 byte-size/references/counters explicit.
8. Normalize task headings for extraction, file-prefix/inherited-directory rules and stable UI control names; avoid implementation bodies where names/assertions already decide behavior. Existing GUI helpers/models remain the pattern rather than introducing a framework.

No material spec gap or additional owner design decision was found. The plan fixes execution details expressly left to the builder and does not reopen the approved scope. Later plan approval is distinct from reapproving the design or Native method.

## Review Focus

The plan records five conditions verbatim for its final reviewer: manual intervention/re-ready/load; locked/active queues and changed snapshots; tight/extreme/species workforce; alternative content/preset/counter/mixed ownership failures; and legacy/private/late-failure behavior. Each names its owning tasks and has concrete tests there. Read-only preview, no extra RNG/IDs, owner-only reporting and failed-transaction invariance are explicit, not inferred from happy-path completion.

## Planning verification and preservation

The retained genuine rules4 QA expanded source was located/read: schema5/rules4, turn14, five total colonies, SHA2569f9b812a5ce5102bf5fbf7ae3d6a1e8fedf64ce91e0d4ef287280100999a0354. No future engine continuation/capture has run: that is Task1 after plan approval. Five hash vectors were calculated independently as integer planning examples, [record](colony-plan-20261024/appearance-hash-vectors.json), not application tests.

Documentation checks verify task order/checkboxes/criterion mapping/links, approval state and unchanged parent totals. Primary main10cb0b4, exact four owner hashes/status/index and all seven worktrees are compared to the preserved snapshot before the documentation commit. No code/core-content/art/dependency/build/test/campaign migration, merge/push/release or cleanup is included. Historical648 simulation and prior runtime results stay historical. Human comprehension, aesthetics, balance, physical input, platform/security/release acceptance remain open.

## Handoff

Review the separate plan and confirm it captures the approved design. Retain Native; do not ask for another method/design choice. The writing-plans workflow requires plan review before implementation. Execution begins with preservation inventory/baseline and genuine capture, then follows the twelve tasks without per-task permission stops once the plan is approved.
