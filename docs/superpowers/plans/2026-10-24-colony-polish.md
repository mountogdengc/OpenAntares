# Colony presentation corrections implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Native inline execution is retained; one final fresh whole-branch reviewer, no per-task agents.

**Goal:** Correct the three reviewed and now explicitly authorized M5 presentation gaps, verify and integrate locally with exact owner-change preservation.

**Architecture:** Keep the existing C#/Godot presentation and command boundary. Add structured command rejection references, render owner names in a scrollable dialog, clarify historical counts and distinguish routine Attention using existing theme variations. No gameplay rules, saved state/schema, content, dependency or AI changes.

**Tech Stack:** C#, .NET8, Godot4.7.2 .NET, xUnit, existing opt-in graphical harness.

**Spec:** `docs/superpowers/specs/2026-10-24-colony-development-design.md` COL-002/COL-006; three Minor findings in `reports/colony-execution/final-review.md`; owner's direct instruction to fix, verify and integrate supersedes their earlier deferral.

## Global constraints

- Retain rules5/writer6 and all16 compatibility pairs; no campaign/template upgrades or authoritative notification state.
- Native inline remains authorized. This is the concrete execution checklist for already described fixes, not a new design/method approval gate.
- Reuse clean `codex/colony-development-design`, fast-forwarded to main `fa99cf81604e878e6bd951a85bb6658b2f7c3faf`. Preserve seven worktrees, all old evidence/QA and recovery.
- Four owner files are backed up exactly; primary stays untouched until reviewed integration. For overlapping status, stash only status and restore exact owner paragraph unstaged; other three files byte-identical/unstaged. Commit authored status through a clean index blob. Final index empty and original four changes only.
- Use existing WarningLabel/SubtleLabel theme variations, containers and scoped own views; no new crisis acknowledgement or blocking turn flow.
- Routine action attention derives from latest pending or completed governor record; completion badges use the most recent viewer-scoped turn report held by the screen until next resolution. Those transient economic reports are not serialized today; do not invent saved completion history or infer completions from forecasts. Report Attention remains distinct in both construction and research completion rows. Keep issue badges derived, deduplicated and independent.

## Review Focus

1. A manually changed colony must show current issue badges separately from historical pending/completed report counts, including Off and reload.
2. Template references must list only issuer-owned colonies in deterministic ID order, distinguish inheritance-only/both, and preserve exact state/reference/readiness on rejection.
3. Thirty referenced colonies or renamed/missing presentation names must stay reachable at800/1920 without leaking foreign identities.
4. Summary→Attention→Off for routine actions and completions must visibly differ, never fabricate completion from forecasts, duplicate vessel completion events, or require additional acknowledgements.
5. Refresh, manual changes, next resolution, old campaigns and save/load must not mutate campaign state or misrepresent transient completion history as saved records.

### Task 1: Label historical conditions truthfully

**Files:** `game/Components/GovernorReportPanel.cs`; `tests/OpenAntares.Game.Validation/ColonyGraphicalValidation.cs`.

**Interfaces:** Existing `ShowReports(ContentSet,EmpireView)`, unchanged authoritative records; stable summary label name `GovernorReportSummary` for scoped UI checks.

- [ ] Add `PolishHistoryChecks`: pending report0 recorded issues while current shortage1; completed report1 recorded issue while current shortage0; manual override detail retained, Off leaves stored details accessible. Use labelled permitted projections/actual command preparation; no generated-campaign claim.
- [ ] Run Colony into unique task1-red output. Expected: history count currently says unresolved, test fails semantically.
- [ ] Render singular/plural “condition(s) recorded in this report”, independently of current badges. No record rewrite.
- [ ] Run full Colony; expected all rows true/error null/0 diagnostics, normal rebuild0 warnings/errors/five exclusions. Capture labelled history PNG and inspect.
- [ ] Commit `fix: distinguish recorded governor conditions from current issues`; task-done with full simulation Suite (expected984 passing).

### Task 2: Explain every template-deletion blocker

**Files:** `src/OpenAntares.Simulation/Commands/CommandOutcome.cs`, `DevelopmentCommandProcessor.cs`; new `game/Components/TemplateReferencesDialog.cs`; `game/Screens/GameScreen.cs`; `tests/OpenAntares.Simulation.Tests/ColonyCommandTests.cs`; Colony graphical harness.

**Interfaces:** Optional `CommandRejection.TemplateReferences` of immutable `GovernorTemplateReferences(ImmutableArray<ColonyId> Colonies,bool NewColonyInheritance)`; existing two-argument rejection construction preserved. Only command diagnostics change. Dialog consumes issuer `EmpireView` plus references, resolves own names with own-ID fallback, scrolls and lists inheritance separately.

- [ ] Add unit assertions for colony-only/inheritance-only/both, reverse storage ordering, foreign references excluded and canonical/source/readiness unchanged. API absence may fail compilation first; expose shape without behavior, then witness semantic RED.
- [ ] Add actual Delete UI check for30 owned references plus inheritance, unique names, own-ID fallback, no foreign name, accessible Close/all entries800/1920; canonical state unchanged.
- [ ] Return sorted owned references with stable rejection code/message, show the scrollable dialog on this rejection; preserve old rejection path for unrelated failures.
- [ ] Run focused/full Suite and full Colony. Expected unit and graphical rows pass; failed delete changes no state; normal rebuild/five exclusions pass.
- [ ] Commit `fix: identify colonies blocking template deletion`; task-done with full Suite.

### Task 3: Give routine Attention a distinct treatment

**Files:** `GovernorReportPanel.cs`, `TurnReportDialog.cs`, `ColonyList.cs`, `GameScreen.cs`; Colony graphical harness.

**Interfaces:** Keep existing preferences/version. Add optional latest `EmpireTurnReportView` to `ColonyList.ShowColonies`; screen retains only its projected latest report. Action badge derives from stored pending/latest completed record. Keep latest report through ordinary refresh/manual/preset changes, replace on next resolved turn; no report is reconstructed on load.

- [ ] Add actual preference controls Summary→Attention→Off checks: routine report labels and latest-turn badges differ, no empty badge/forecast completion, retained current issue badge unchanged, no state/Ready change, owner preference file untouched. Check construction/research completions and one vessel counted once. Check next turn clears stale completion badge, governor saved history survives reload, legacy controls unchanged.
- [ ] Run task3-red Colony. Expected: Attention currently identical to Summary, fails semantic UI check.
- [ ] Use Attention prefix/WarningLabel for relevant recorded action/completion rows; Summary ordinary digest, Off suppresses routine notices but retains accessible stored governor details. Add nonblocking named action/completion badges; completion counts use actual viewer report only, not expansion-event duplicates. Research completion contributes once.
- [ ] Run full Suite and Colony, inspect800/1920 Attention captures. Expected all checks/metadata normal build pass, no campaign mutation.
- [ ] Commit `fix: distinguish routine attention notifications`; task-done with full Suite.

### Task 4: Verify the bounded correction branch

**Files:** execution ledger/evidence; `reports/COLONY_POLISH_2026-10-24.md`, `reports/COLONY_POLISH_INTEGRATION_2026-10-24.md`; standard current status/criteria/roadmap/entry docs.

**Interfaces:** Existing five-scenario runner/results schema, source SHAs, exact preservation inventories. New evidence must distinguish branch full gate from fresh affected integrated-main gates.

- [ ] Run Suite and sequential Colony/Rival/Species/Expansion/Legacy into fresh `reports/colony-polish-final-20261024-run1`; final ordinary rebuild/five metadata exclusions. Expected0 failed/skipped/errors/engine diagnostics, all QA paths exist; derive assertion/provenance/geometry counts from rows and inspect representative PNGs.
- [ ] Record all three corrected criteria as Implemented, Unverified pending review, historical deferrals retained as historical; parent REQ-017/counts/human/release limits unchanged. Commit evidence/docs; task-done with full Suite.
## Final review and authorized local integration

- [ ] One fresh read-only gpt-6-astra/xhigh whole-branch review from fa99cf8; all five Review Focus lines/rulings/actual gate evidence. No reviewer subagents. Re-grade, ONE regression-first Important/Critical correction pass if needed; defer new Minors, no re-review.
- [ ] After clean blocking disposition, fast-forward main with exact status-only recovery and owner paragraph restoration. Run fresh integrated-main Suite and full affected Colony scenario, ordinary rebuild/five metadata exclusions. Other four branch scenarios remain identified as branch evidence on unchanged reviewed source; broaden only for new failures/changes.
- [ ] Reconcile acceptance honestly: corrected COL-AC-002/007 and M5-I Verified only if all their details pass/review disposition allows; broader human/platform/release/REQ017 remain open. Commit only authored docs/evidence, clean status blob excludes owner paragraph; assert final four owner changes/empty index/exact hashes/seven retained worktrees. No push/release/cleanup.

## Self-review

Three requested findings map exactly to Tasks1–3. Task4 combines evidence/review/local handoff; no new feature/content/state-version work. Shared dependencies: Task1 summary is consumed by Task3 treatment; Task2 structured rejection remains independent; Task4 verifies all three. All five Review Focus classes have owning tests above. Scope and retained execution/integration authorization come from the direct latest owner request; no repeated confirmation needed.
