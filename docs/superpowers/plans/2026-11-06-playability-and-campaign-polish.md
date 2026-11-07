**M13 implementation approved, 2026-11-06:** Owner: “Looks good. Go ahead and implement M13 using the proposed scope and ten-task plan.” Design and task-plan approval are recorded in ADR-027. M13-D/M13-P Verified for approval scope; M13-I In Progress on isolated codex/playability-polish. Execute Native inline, no per-task agents, one final fresh whole-M13 implementation review. Keep rules12/writer13/79 pairs, src/content/schema and balance unchanged. Ledger: .superpowers/sdd/2026-11-06-playability-and-campaign-polish/progress.md. Preserve all prior documents, saves, evidence, branches/worktrees and the historical three-log exception. This approval does not authorize commits, merging, pushing, release or cleanup; strict committed-source acceptance remains a separately identified gate. Earlier proposal-pending paragraphs below are historical.

# M13 Playability and Campaign Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for Native inline execution after approval. No per-task implementation agents; one fresh final M13 implementation reviewer. Steps use checkbox (`- [ ]`) syntax for tracking. This is a provisional review plan, not execution authorization.

**Goal:** Make the existing start→map→system→colony→orders→turn report→save/load loop legible and testable without changing simulation rules.

**Architecture:** Keep the existing map-first GameScreen and use requester projections for every summary/action. Add focused presentation models/components for selection, map labels and turn guidance; all actual orders continue through existing commands. Use a separately opted-in M13 harness, existing native regressions and actual-Main campaign replay to verify the final source.

**Tech Stack:** Existing Godot 4.7.2 C#/.NET 8, xUnit, original theme/art, PowerShell/Python evidence tools and the installed native shared queue; no dependencies added.

**Spec:** [M13 proposed scope](../specs/2026-11-06-playability-and-campaign-polish-design.md). Read both documents. Baseline inspected: main 60044af; historical tests are not M13 results.

**Review status:** M13-D/M13-P In Progress; M13-I Not Started. The owner requested scope and a plan together, so this plan is provisional until the full proposal and task breakdown are reviewed. No tasks below have run. No commits, product edits, imports/builds/tests or worktree creation are authorized by this draft.

## Global Constraints

- Keep rules 12/writer 13/79 supported pairs and genuine historical campaign behavior. No source edit under `src/`, no balance/content changes, no save fields/migrations and no PRNG consumption by presentation.
- Existing Godot 4.7.2 C#/.NET 8 stack, original assets/theme and plain-C# simulation separation remain. No new dependency, framework, engine upgrade, Docker or routine host desktop control.
- Preserve all earlier branches/worktrees/stashes, owner prose, saves/screenshots/raw and frozen evidence, renderer investigation and the exact historical three-log exception. Every new engine invocation gets a unique explicit log path. No cleanup or overwriting latest evidence.
- Test physical viewport sizes 800x600,1152x648,1600x900 and1920x1080. Preserve ANGLE,60FPS,VSync and the current window defaults.
- The opt-in M13 harness is isolated from the common twelve-scenario target. Extend ordinary metadata checks to detect it; preserve the existing twelve-type negative control and require the entire validation namespace to be absent after restoration.
- All builds/imports/tests use `F:/Tools/CodexTestRunner/runner.py`, project `openantares`, actual active source root, one disk-heavy job at a time. The native profile supersedes Docker advice. Do not put an internally queued graphical runner inside an outer queue job.
- Maximum five reproduced presentation/integration follow-up fixes. A sixth fix, simulation/rules/content/schema edit or new system needs revised scope; unresolved critical/major defects keep acceptance open. No implementation/commit/merge/push/release/cleanup authority is implied.

## Review Focus

1. Clicking an overlapping owned marker or refreshing a route form must never issue an order: Tasks 2/3 `SelectionAndPreviewNeverIssueCommands`.
2. A selected vessel/colony can disappear after publication or Load: Task 6 `MissingSelectionExplainsAndNeverFallsThroughToAnotherEntity`.
3. Hidden details must not leak through label suppression, guidance sorting/counts or tooltips: Tasks 2/4/7 `ForeignOrUnobservedFactsNeverEnterPresentation`.
4. Deferred focus restoration must not race popup focus or steal a text edit: Task 6 `ResizeDoesNotDoubleScrollOrStealEditedFocus`.
5. A completed80-turn driver can still lack an actual conflict witness or skip a failed save: Task 8 `IncompleteCampaignOrStageFailsClosed`; keep original earned conflict gates in Task 10.

## File ownership and interface contracts

All new paths below are proposed files; existing paths were inspected. No module-wide refactor is included.

| Owner | Existing files to extend | New files and responsibility |
| --- | --- | --- |
| Setup/entry | `game/Screens/NewGameScreen.cs`, `game/Main.cs`, `game/Screens/GameScreen.cs` | No new title screen or persisted preferences |
| Map/selection | `game/Components/GalaxyMap.cs`, `game/Components/VesselPanel.cs`, `game/Screens/GameScreen.cs` | `game/Presentation/MapLabelLayout.cs`: deterministic label rectangles; `game/Components/SelectionSummaryPanel.cs`: selected owned-vessel/context card |
| System/colony | `game/Components/StarPanel.cs`, `game/Components/ColonyPanel.cs`, `game/Components/ColonyList.cs`, `game/BreakdownText.cs` | Reuse original `PlanetPortrait` and projected economy |
| Menu polish | `game/Components/ResearchBar.cs`, `game/Components/ConstructionQueueEditor.cs`, `game/Components/FleetCommandDialog.cs`, `game/Components/OfficerDetailDialog.cs`, `game/Components/DiplomacyDialog.cs`, `game/Screens/GameScreen.cs` | Visible existing-choice explanations and consistent action/return labels; no new systems |
| Navigation/turn guidance | `game/Screens/GameScreen.cs`, `game/Components/TurnReportDialog.cs`, relevant existing order dialogs only for reproduced defects | `game/Presentation/PlayabilityContext.cs`: ephemeral semantic navigation state; `game/Presentation/TurnGuidance.cs`: pure projected guidance; `game/Components/TurnGuidancePanel.cs`: links/rendering |
| Acceptance | Existing `LayoutTrialValidation.cs`, common scenario helpers only when navigation actually changed, `Verify-NormalAssembly.ps1` | `tests/OpenAntares.Game.Validation/PlayabilityGraphicalValidation.cs`, `.tscn`, `PlayabilityCampaignChecks.cs`, `PlayabilityValidation.targets`; `tools/ConflictValidation/Run-Playability.ps1` and `Verify-PlayabilityEvidence.py` |
| Owner observations | Existing general requirements remain historical | `docs/playtests/m13-playability-<date>.md`, `reports/playability-execution/<unique-run>/` evidence and defect ledger |

Proposed public contracts, defined by the producing tasks:

- Task 2: `MapLabelCandidate(StarId Id, Rect2 Bounds, bool Selected, bool Home)`; `MapLabelLayout.Place(IReadOnlyList<MapLabelCandidate> candidates, Rect2 viewport): IReadOnlyList<StarId>`. Bounds are measured before this pure layout helper; tie order: selected,home,StarId. `GalaxyMap.OwnedVesselsSelected` event `Action<StarId>` opens the stable owned-at-star chooser, not a movement command.
- Task 3: `SelectionSummaryPanel.ShowSelection(ContentSet content, EmpireView view, StarId? star, VesselId? vessel): void`; events `InspectSystemRequested: Action<StarId>` and `VesselSelected: Action<VesselId>`. The existing `VesselPanel.ShowVessels(...)`, `CommandIssued`, `DestinationSelected` and `FleetCommandRequested` remain the route/order boundary.
- Task 6: `PlayabilityContext` stores workspace key, `StarId?`, `PlanetId?`, `ColonyId?`, `VesselId?` and a return stack of semantic control keys/scroll offsets. It stores no Godot Node reference, campaign content or authority token. `ResetForCampaign(): void`; `Reconcile(EmpireView view): void` clears vanished selections, invalid return targets and stale focus keys without executing commands. GameScreen owns this instance and resolves semantic keys after layout.
- Task 7: `GuidanceSeverity { Required, Consider }`; `TurnGuidanceItem(string Key, GuidanceSeverity Severity, string Text, string Destination)`; `TurnGuidance.Build(EmpireView view): IReadOnlyList<TurnGuidanceItem>`. Destination is a fixed existing overview route, never executable command text. Required items are grouped by existing projected response type, with counts based on requester-visible notices; do not infer completeness of engine forecast-dependent readiness checks; optional items are grouped by kind. The panel emits `DestinationRequested: Action<string>`; GameScreen routes only an explicit allowlist. These are proposed presentation types, not simulation/save types.
- Task 8: the M13 evidence JSON records source/DLL identity, real-control checks, exact settings/seeds, committed boundary count, checkpoint Main paths/hashes, future hashes, paid-action provenance, observed events, diagnostics and normal restoration. No aggregate pass if a required field/case is absent. The verifier derives expected species/modes/checkpoints from the approved spec, not from whatever files happened to be generated.

## Queue commands used by the tasks

Let `$sourceRoot` be the isolated checkout actually created after approval; set the working directory there. Each evidence directory must be new. The current primary wrapper can test a linked checkout:

```powershell
& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot $sourceRoot -Suite build
& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot $sourceRoot
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- pwsh -NoProfile -File tools/Run-LayoutTrialChecks.ps1 -OutputDirectory reports/playability-execution/UNIQUE-layout
```

The future `tools/ConflictValidation/Run-Playability.ps1 -RunName UNIQUE` queues its own opt-in/ordinary builds and records explicit Godot logs; call it directly, exactly like `Run-NativeMatrix.ps1`. During implementation its `-Case` selector must accept `Setup`, `Map`, `Selection`, `Colony`, `Navigation`, `Guidance`, `Campaigns` and default `All`. A selected-case pass is never full milestone acceptance. The campaign batch uses one opt-in build and separate fresh actual-Main sessions; compilation/restoration are serialized. Match the original native runner's authorization/ownership guard; do not request desktop control or invent a VM connection.

### Task 1: Freeze the baseline and establish honest M13 evidence

**Files:** new M13 harness/targets/runner/verifier from the file map; extend `tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1`. Evidence under a fresh `reports/playability-execution/` directory. No product behavior change.

- [ ] After approval, create the isolated execution checkout using `superpowers:using-git-worktrees`; record actual main HEAD, status, refs/stashes/worktrees and owner-file hashes. Record preservation baselines and separate genuine current/legacy canonical fixtures before UI edits. Stop for reconciliation if the inspected source differs materially from the reviewed one.
- [ ] Add verifier negative cases for missing source/input inventory, missing result, changed DLL, missing Main save/future checkpoint, diagnostic log and failed restoration. Run through the shared queue and confirm they fail closed before implementing acceptance logic.
- [ ] Define the Task 8 evidence contract in the runner/verifier; implement selected-case reporting, authoritative tracked-source inventories before/after, retained opt-in DLL, explicit logs, opt-in negative metadata and mandatory ordinary rebuild in `finally`. Include all new runner/verifier inputs in source capture. Keep common `Validation.targets` unchanged; add the one M13 scenario to ordinary forbidden-type detection, preserving existing twelve-present checks.
- [ ] Run baseline queued build/full suite and existing layout checks; record exact current counts. Exercise a deliberately opted-in M13 DLL (negative metadata control) and restored ordinary DLL (empty validation namespace). Selected baseline checks pass; PLY functionality remains Not Started.

### Task 2: Make crowded maps selectable and labels legible

**Files:** `GalaxyMap.cs`, new `MapLabelLayout.cs`, `PlayabilityGraphicalValidation.cs`.
**Interfaces:** produce Task 2 contracts above; consume only `GalaxyMapView`/existing requester scouting markers.

- [ ] Add failing real-control/layout cases named `CrowdedLabelsAreStableAndBounded`, `OwnedMarkerChooserIsStableAndStarFallbackWorks`, `ForeignOrUnobservedFactsNeverEnterPresentation`. Include equal-distance stars, collocated owned vessels, a foreign marker, transit marker, very long names and each of the four physical sizes. Assert unchanged canonical state, PRNG and zero issued commands after every selection.
- [ ] Run `Run-Playability.ps1 -RunName UNIQUE -Case Map`; verify the added behavior-specific assertions fail on the old implementation, while baseline initialization succeeds.
- [ ] Implement measured label placement and owned stationary marker hit areas; tie break and overlap policies must match the spec. Provide the unchanged star/list keyboard route. No transit/foreign click-order behavior or map zoom rewrite.
- [ ] Rerun Map plus existing layout checks; verify exact drawing bounds, discoverable suppressed names and no new facts/commands. Retain before/after frames.

### Task 3: Connect selected-vessel context to existing commands

**Files:** new `SelectionSummaryPanel.cs`, `VesselPanel.cs`, `GameScreen.cs`, M13 validation.
**Interfaces:** consume Task 2 owned-at-star event; produce Task 3 panel contracts; retain existing `CommandIssued` authority.

- [ ] Add `SelectionAndPreviewNeverIssueCommands` and `MultipleVesselsRequireExplicitStableSelection`: inspect, change star, preview destination, resize and close; assert zero move/found/Ready commands and unchanged canonical state. The existing explicit order button must submit exactly one ordinary command and display its actual rejection/result.
- [ ] Run Selection to establish RED for the missing contextual path. Implement the compact card, stable chooser and full vessel-list alternative. Display current station/transit/pending route using existing view/calculations, including unavailable/conditional ETA text.
- [ ] Rerun Selection and affected Expansion/Fleet/Scout native scenarios using their actual-source runner; verify paid/legal actions, preservation and normal restoration. No selected-case result substitutes for Task 10's full matrix.

### Task 4: Clarify setup, system inspection and colony decisions

**Files:** `NewGameScreen.cs`, `Main.cs`, `GameScreen.cs`, `game/Components/StarPanel.cs`, `game/Components/ColonyPanel.cs`, `game/Components/ColonyList.cs`, `BreakdownText.cs`, M13 validation.
**Interfaces:** retain `Configure(...)`, `StartRequested`, existing selection/command events and original projection types; consume Task 3 selection path.

- [ ] Add failing `GroupedSetupPreservesSettingsAndStartsOnce`, `UnknownWorldShowsNoFabricatedDetails`, `ColonySummaryMatchesProjectedEconomyForEverySpecies`. Include collapsed/nondefault options, invalid seed, Standard/Solo, known/stale/unknown/foreign worlds, positive/short support, blockade pressure and conditional project estimates. At all four sizes assert Start/Load and essential content are reachable.
- [ ] Run Setup/Colony selected cases and retain RED. Group existing settings, add wrapped settings review and session-only first-turn orientation; implement grouped system/colony summaries above existing detailed controls. Reuse original portraits/theme; do not add identity customization or stored UI fields.
- [ ] Rerun selected cases plus Species/Colony/Legacy regressions. Assert displayed numbers/units match the existing views, loaded saves retain rules and opening navigation never creates a command.

### Task 5: Make the existing menus readable and consistent

**Files:** `game/Components/ResearchBar.cs`, `game/Components/ConstructionQueueEditor.cs`, `game/Components/ColonyList.cs`, `game/Components/StarPanel.cs`, `game/Components/FleetCommandDialog.cs`, `game/Components/OfficerDetailDialog.cs`, `game/Components/DiplomacyDialog.cs`, `game/Components/TurnReportDialog.cs`, `game/Screens/GameScreen.cs`, M13 validation. Extend only existing presentation; no new content or game settings.
**Interfaces:** preserve `CommandIssued`, current selection/report events and Game-menu action IDs 0/1/2. Consume the same requester views and content already used by each menu; expose no new simulation interface.

- [ ] Add `MenusPreserveAuthorityAndContext`, `ResearchAndProductionDetailsMatchExistingChoices`, `NoContactAndUnavailableActionsExplainWithoutLeaking`. Cover every menu named in spec3a at four sizes, long names/descriptions, locked research, absent/conditional build estimates, multiple owned fleets/officers and no-contact/own-response diplomacy. Assert opening,browsing,Tab and Escape issue zero commands; an explicit enabled choice issues its existing command at most once. Keep current quick-selector semantics; do not silently switch to an Apply workflow.
- [ ] Run the selected Setup/Colony/Selection/Navigation cases as appropriate and establish RED for missing visible summaries/return behavior. Add visible selected-option explanations, clear object/action labels, concise arrival links and consistent closing/return paths. Preserve Game-menu IDs and all existing preference effects; add no unsupported Settings/audio/autosave/history/help entries.
- [ ] Rerun these menu cases and affected Species/Colony/Fleet/ShipDesign/Diplomacy native regressions. Verify known/owned privacy, exact research/production values, paid commands, no accidental submissions and ordinary restoration. Task 6 handles semantic return/focus lifecycle for these routes; pin the menu-specific assertions here and include them in the final Navigation case.

### Task 6: Preserve semantic context, focus and report return paths

**Files:** new `PlayabilityContext.cs`; `GameScreen.cs`, `game/Components/TurnReportDialog.cs`; affected existing dialogs only if required by the named test; M13 validation.
**Interfaces:** produce Task 6 context methods; consume Tasks 2–5 paths. Use stable semantic keys, never retain freed Node references.

- [ ] Add `ReportRoundTripRestoresValidScrollAndFocus`, `ResizeDoesNotDoubleScrollOrStealEditedFocus`, `MissingSelectionExplainsAndNeverFallsThroughToAnotherEntity`, `LoadResetsTransientContextWithoutCommands`. Test popup-open-then-resize, typing during refresh, lost selected object, new campaign, Load, hidden controls, Tab/Shift+Tab, Escape and activation once.
- [ ] Establish RED through Navigation. Implement ephemeral context, reconciliation and deferred semantic focus restoration. Keep the previous double-scroll regression pinned; preserve edits or explain invalidation. Close/Escape never answers required responses or activates the object that replaced a vanished one.
- [ ] Rerun Navigation plus current layout 107 baseline contracts and Legacy/Scout native checks. Record actual updated counts; no blanket replacement of bounds/visibility assertions.

### Task 7: Explain turn readiness and retain report meaning

**Files:** new `TurnGuidance.cs`, `TurnGuidancePanel.cs`; `GameScreen.cs`, `game/Components/TurnReportDialog.cs`, M13 validation.
**Interfaces:** consume current `EmpireView`; produce Task 7 pure model/panel events and fixed destination allowlist.

- [ ] Add `RequiredGuidanceMatchesProjectedNotices` and `ForecastOnlyRejectionRemainsAuthoritative`, `OptionalGuidanceDoesNotBlockReadyOrSpendResearch`, `WaitingCancelAndRevisionRemainAuthoritative`, `ForeignOrUnobservedFactsNeverEnterPresentation`. Test no research target/idle work as optional, projected combat/conflict responses as required, response revision changes, missing destinations and repeated refresh. Include crisis-preparation changes to combat readiness, scouting preparation and unacknowledged crisis cases: the projected notice list can be empty while the real Ready command rejects. Assert the exact rejection remains visible with no automatic retry/acknowledgement, and the empty-state text is exactly "No response notices. End Turn will check readiness."
- [ ] Establish RED via Guidance. Implement deterministic grouping/links and compact required/consider presentation; group report display while retaining existing explanatory rows and report action ownership. Refresh/reorder cannot acknowledge a report or submit a command. Existing mandatory interruption behavior remains authoritative.
- [ ] Rerun Guidance and affected Crisis/Combat/Diplomacy/EmpireConflict checks. Compare actual Ready availability/rejections and exact own responses with baseline; assert no PRNG/state effects from guidance and no optional blocker.

### Task 8: Audit sustained actual-Main campaigns and save continuity

**Files:** `PlayabilityCampaignChecks.cs`, M13 harness/runner/verifier; evidence ledger only. Reuse existing `tools/ConflictCampaignProbe` as-is for canonical replay; do not edit simulation/probe authority to make outcomes pass.
**Interfaces:** consume Tasks 1/4/7; produce the fixed Task 8 evidence contract and observed-event/defect ledger.

- [ ] Add verifier cases `IncompleteCampaignOrStageFailsClosed`, `WrongSeedModeOrCheckpointFails`, `InjectedAssetsOrChangedSourceFails`, `MissingOrUnequalFutureFails`. Run verifier self-tests through the queue; each negative fixture must be rejected.
- [ ] Implement eight fresh actual-Main drivers: Harl42/Peldra45/Sulan43/Wenth42 × Small Standard/Solo,80 committed boundaries. Standard has one rival (Peldra except Peldra faces Harl); Solo has zero. Both keep crises/ameba/sentries Standard/On, rules 12/core content; record every actual setting. These are audit profiles, not new modes. Reuse earned M12 legal Standard policy and current paid Solo commands; record every required response and paid-action provenance, not editor-injected state.
- [ ] Capture actual Main saves at0/10/25/50/80, verify exact canonical round trips and three future boundaries at every checkpoint. Run Campaigns with unique output names. Require 8 complete sessions,40 checkpoint saves and120 equal futures, source/DLL stability, diagnostics inspection and normal restoration.
- [ ] Record measured waiting/idle/shortage/recovery observations and which events actually occurred. Retain failures as failures. Conflict stages/expiry remain separately required in Task 10; balance adjustments and engine issues go to a separate proposed scope.

### Task 9: Resolve the bounded findings and prepare owner-operated playtesting

**Files:** only presentation/harness files for reproduced issues; `docs/playtests/m13-playability-<date>.md`; unique defect/evidence records. No speculative fixes.

- [ ] Triage each audit finding with screenshot/save/log, actual source, steps, expected/actual outcome and critical/major/minor severity. Count unique fixes against the five-fix limit; merge duplicates without losing reports.
- [ ] For each of up to five authorized in-scope fixes, add a regression that reproduces the observed failure, establish RED, implement the minimum fix and rerun affected checks. Any sixth fix, new system or src/content/schema requirement triggers scope revision. An unresolved critical/major remains blocking.
- [ ] When independently executable work is exhausted, supply the owner a single physical-input packet: actual-root queued build, exact installed Godot path, fresh explicit log, prerequisites/QA copies, numbered journeys for setup/map/vessel/colony/Ready/report/save/load at 800x600 and1920x1080, expected results and report-back fields. Use current actual-Main checkpoint copies, not injected rehearsal files.
- [ ] Report-back fields: source/engine/resolution/input method, species/seed/mode/checkpoint, action, expected/actual, severity/pass/fail, screenshot/log/save path and whether the player understood the consequence. Distinguish absence of feedback from a pass; human confirmation unlocks human acceptance only, not automatic merge/release.

### Task 10: Final whole-source regression, review and acceptance handoff

**Files:** final M13 report, machine summary, criterion/defect ledger and current-status prefixes; no unrelated historical rewrites.

- [ ] Run final queued solution build, complete unfiltered Windows suite, layout checks and all M13 cases against the same final committed source (commits only if separately authorized). If source cannot be committed under current authority, preserve it and report source-capture gate pending; never fabricate commit identity.
- [ ] Run `tools/ConflictValidation/Run-NativeMatrix.ps1 -RunName UNIQUE -EarnedConflictCampaigns` directly for all twelve existing graphical scenarios and four earned conflict campaigns. Keep the existing original verifier, real twelve-type negative control, zero-diagnostic requirement and required normal restoration. Source inventory must cover every new tracked input; no old branch pass establishes acceptance here.
- [ ] Run the M13 verifier through the queue: fixed 8/40/120 campaign coverage, all PLY contracts, current source/DLL/log identities and empty ordinary validation namespace. Evidence with missing/failed/skipped cases is incomplete. Preserve historical three-log exception exactly.
- [ ] Obtain one fresh whole-M13 implementation review, triage required findings and fix with reproductions inside approved scope. Rerun only invalidated checks plus the final required gates. This is not a second M12 review.
- [ ] Produce the final report with per-criterion automated/human status, exact source and counters, unresolved findings and scope limits. Keep parent17-row statuses/totals unchanged unless separately justified. Hand off for owner disposition; no merge/push/release/cleanup.

## Coverage and review handoff

| Criteria | Tasks |
| --- | --- |
| PLY-AC-001 | 4,9,10 |
| PLY-AC-002/003 | 2,3,10 |
| PLY-AC-004 | 4,5,10 |
| PLY-AC-005/007 | 5,6,9,10 |
| PLY-AC-006 | 7,10 |
| PLY-AC-008 | 1,8,10 |
| PLY-AC-009/010 | 1,10 |
| PLY-AC-011/012 | 9,10 and owner design approval |

Review choices: the proposed interaction scope, unchanged simulation boundary, eight 80-turn campaigns, five-fix limit and separate human acceptance. Native inline method is retained; do not re-ask an already settled execution-method choice. Approval should name both reviewed documents and any amendments. Nothing has been implemented by writing this plan.
