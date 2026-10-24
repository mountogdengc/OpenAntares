# OpenAntares agent instructions

## Start and authority

1. Read [PROJECT_STATUS.md](PROJECT_STATUS.md), inspect Git status, branches, and registered worktrees, and verify the next action against actual files.
2. Read [CLAUDE.md](CLAUDE.md), which remains the detailed engineering rulebook, and [docs/CHARTER.md](docs/CHARTER.md), the source of truth for identity, scope, and intent.
3. Read the relevant established subsystem specification, [requirement index](docs/SPECIFICATIONS.md), [acceptance criteria](docs/ACCEPTANCE_CRITERIA.md), and [decisions](docs/DECISIONS/README.md).
4. Use [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) for dependency order. Inspect evidence rather than inferring completion from old checkboxes or test counts.

Explicit owner instructions take precedence. Existing authoritative designs govern behavior; the new standard documents index them and do not silently supersede them. Proposed/open design-guide entries and research observations are not approved implementation requirements. If documents conflict, record the conflict and resolve material scope, architecture, UX, licensing, or save-compatibility choices with the owner.

## Current scope

The 2026-10-22 request and continuation authorize adoption, reconciliation, documentation, verification and retained regression checks. The owner deferred the unavailable uncoached playtest (ADR-005) and then approved the full exploration/colonization specification (ADR-006). **Do not block progression on recruiting a participant or closing First Playable, or reopen the approved design choices.** The owner approved the separate implementation plan and Native execution. Its nine tasks and independent review/corrections are complete. On 2026-10-23 the owner explicitly authorized lock repair and local integration: main now contains the expansion, the normal index works, and two separate owner prose edits remain uncommitted. Read current status; do not repeat completed implementation. Full First Playable acceptance stays open. Do not import OpenJRPG-specific requirements or ADR numbering from the workflow PDF.

Current continuation: the owner approved the species implementation plan (“yes”) and the retained Native execution preference. All ten tasks are complete on isolated `codex/species-mechanics`; one independent whole-branch review and its regression-first corrections are resolved. On2026-10-24 the owner selected option1: local main integration is complete, with fresh combined verification. Read PROJECT_STATUS and the species integration report before the next milestone. Preserve the classic space-opera direction, original owner roster/research edits, genuine old campaigns and all evidence. Do not repeat completed implementation/integration or infer remote push/release/cleanup authorization. Retain the species branch/worktree, owner edits and all recovery/QA evidence.

Current rival continuation: the owner approved the complete rival-expansion design (“approved”) after builder review, recorded in ADR-010. M4-D/RIV-AC-012 are satisfied. Read the approved spec and separate ten-task rival implementation plan; that plan is written/self-reviewed and awaits owner plan review. Native remains selected: inline implementation and one final fresh independent whole-branch review after later plan authorization. Do not ask for the method again or reapprove the design. No rival task has run; actual product remains rules 3/writer 4. Preserve owner roster/research edits, peaceful-mode backlog, all old saves/evidence/recovery/worktrees. After plan approval, isolate execution and capture genuine rules-3 traces before writer/default changes; no inferred main integration/push/release/cleanup.

## Engineering constraints

- C# throughout; Godot .NET presentation, plain .NET simulation, JSON content, xUnit tests.
- Never reference Godot from the simulation. UI reads published state and submits empire-issued commands through the shared command processor.
- Preserve deterministic integer/fixed-point rules, the existing PCG32, stable entity IDs, defined iteration order, invariant parsing, explicit turn phases, and atomic publication.
- Content IDs are existing strings such as `fabrication_hall`; do not add a namespace scheme as an adoption change.
- UI is built in C#. Style through `game/theme/openantares_theme.tres` and type variations; reread that shared file before minimal edits. Use containers and anchors.
- Keep ordinary content in JSON, validate at load time, and expose simulation-supplied calculation breakdowns.
- Current new games use rules3 with visible Harl default; code writes schema4 and supports exactly (schema,rules) pairs(1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3). ADR-006 and the approved species amendment govern legacy continuation; no implicit campaign upgrade. Preserve embedded content, rules-version checks and failure atomicity.
- Preserve the original setting and licenses. Record third-party additions in `LICENSING.md`; do not copy commercial game assets.

## Branches, worktrees, and integration

- Inventory existing changes before editing. Preserve unrelated user edits and evidence. The authorized integration retained an exact recovery snapshot/stash and restored the two prose edits; do not blindly reapply that stash or discard changes.
- Inspect existing worktrees before creating another. A clean worktree at an ancestor of `main` is not an outstanding feature merge.
- The earlier adoption scope excluded integration; subsequent explicit owner authorization completed local integration. Do not infer remote push, branch/worktree deletion or history-rewrite authorization from that integration.
- For later authorized code work, choose an appropriate branch/worktree without moving or discarding the owner's uncommitted changes. Review the final diff and validate the combined result before integration.
- Do not log credentials, read unrelated secrets, change production systems, or perform irreversible external actions without authorization.

## Verification and handoff

Use the build/test commands in [README.md](README.md). Restore only if required; prefer the normal dependency audit. The historical `NuGetAudit=false` workaround was for restricted network access, not a project default. Do not infer a clean restore or vulnerability scan from a `--no-restore` build.

For later simulation changes, update meaningful tests and execute the affected and regression suites. For Godot-only changes, retain reproducible interaction checks. A headless launch does not verify visual layout, clicking, player comprehension, or export packaging. Preserve saves using unique QA names.

The opt-in graphical harness is documented in `tests/OpenAntares.Game.Validation/README.md`. Run it only with the Godot .NET console executable and a new output directory. Its runner temporarily includes the test class via MSBuild, then rebuilds the normal assembly. Its in-process signals/handlers and embedded visual fixtures do not establish physical OS input or player understanding. Existing/QA saves are retained; do not delete them as inferred cleanup.

Record commands, environment, exit codes, test counts, and evidence in `reports/VALIDATION_REPORT.md`; distinguish fresh results, historical owner reports, and unverified behavior. Use these statuses exactly: Not Started / In Progress / Implemented, Unverified / Verified / Blocked / Deferred. Derive counts from the requirement register.

Before ending, update `PROJECT_STATUS.md` with actual changes, verification limits, decisions/blockers, integration state, and one concrete next action. Never mark a milestone closed merely because compilation and unit tests pass.
