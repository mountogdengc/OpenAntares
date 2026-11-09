# M14 UI Visual Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for Native inline execution after approval. No per-task implementation agents; one fresh final M14 implementation reviewer. Steps use checkbox (`- [ ]`) syntax for tracking. This is a provisional review plan, not execution authorization.

**Goal:** Replace the stock-Godot look and modal-heavy flow with an original, chamfered "command console" presentation (a retro homage to the 1990s space-4X feel, not a copy of any game) and a map-hub layout, without changing simulation rules, content or saves.

**Architecture:** All visual styling lands in `game/theme/openantares_theme.tres` as StyleBoxes, fonts, icons and named type variations; C# only assigns `ThemeTypeVariation` and builds layout. The existing map-first GameScreen gains a top status strip, a context console driven by selection, a grouped command bar and full-height workspaces that replace most `Window` dialogs. New pure presentation models (status readouts, fleet grouping, command-bar pending summary) are unit-testable without Godot. Every order still flows through the existing `CommandIssued`/`Execute` path.

**Tech Stack:** Existing Godot 4.7.2 C#/.NET 8, xUnit, the native shared queue and existing graphical-validation harness pattern. New assets only: SIL OFL fonts and original CC0 UI art. No new code dependency, framework or engine upgrade.

**Design source:** the owner-reviewed UI overhaul write-up "OpenAntares UI Overhaul: Retro MoO2 Homage" (Claude doc, 2026-11-08) and its galaxy-screen/colony-ledger mockup. Neither is yet a repository spec. Before approval, either (a) accept this plan's Acceptance Criteria section as the M14 spec, or (b) ask for `docs/superpowers/specs/2026-11-08-ui-visual-overhaul-design.md` to be written first. Charter rule retained: do not reproduce a legacy game's interface feature-for-feature; MoO2 is a mood reference only.

**Sequencing:** M14 starts only after M13 (Playability and Campaign Polish) is implemented and locally integrated. M14 restyles and builds on M13's `SelectionSummaryPanel`, `MapLabelLayout`, `TurnGuidance`/`TurnGuidancePanel` and `PlayabilityContext` rather than duplicating them. If M13's approved shape differs from its 2026-11-06 draft, Task 1 reconciles these contracts before any UI edit.

**Review status:** M14-D/M14-P In Progress; M14-I Not Started. No tasks below have run. No commits, product edits, imports/builds/tests or worktree creation are authorized by this draft.

## Decisions needed before approval

| ID | Question | Drafter default |
| --- | --- | --- |
| D1 | Stars have no spectral class in simulation or content today. Tint/size stars by class? | Defer. M14 gives every star glow, ownership rings and fleet flags but one neutral tint. Star classes need a separate content/simulation scope. No presentation-side fake classes (they would show facts the game does not have). |
| D2 | Where does the CRT/scanline toggle (and UI scale, if added) persist? | A presentation-only settings file under `user://` (never in saves). Default On for scanlines; honors a reduced-effects option. |
| D3 | Fonts | Oxanium (display), Barlow (body), Share Tech Mono (numbers), all SIL OFL 1.1, bundled under `art/fonts/` with licenses recorded in LICENSING.md. |
| D4 | Navigation merges | Scouts becomes a tab of the Fleets workspace; Contacts becomes a tab of the Diplomacy workspace. Game moves to a gear key. Existing control `Name`s that harnesses use stay reachable (see constraints). |
| D5 | Follow-up fix budget | Maximum six reproduced presentation follow-up fixes, as in M13's bounded-fix model (M13 used five; M14 touches more screens). |

## Global Constraints

- Presentation only. No source edit under `src/`, no content/balance/schema change, no save fields/migrations, no PRNG consumption by presentation. Rules 12/writer 13/79 supported pairs and historical campaign behavior are unchanged.
- CLAUDE.md UI rules are hard constraints: UI built in C#; no `Add*Override` or hard-coded colors/fonts/sizes/styleboxes in C#; variants are theme type variations; containers/anchors, not absolute positions. Read the theme fresh before each edit and add to it minimally, preserving owner tweaks.
- One documented exception: the scanline/vignette `.gdshader` (a theme cannot express shaders). It lives in `game/theme/` beside the theme, reads its colors from shader uniforms set once from theme constants, and is recorded in docs/DESIGN_GUIDE.md.
- All new art is original and CC0 under `art/ui/`; fonts are OFL with license files. No third-party UI kits, no assets from commercial games.
- Harness compatibility: existing graphical scenarios and `CombinedUiNavigation.cs` find controls by `Name` (`OpenFleetCommand`, `OpenScoutOverview`, `OpenContactsWorkspace`, `OpenDiplomacy`, `OpenResearch`, `OpenShipDesigns`, `GameMenu`, `EndTurn`, `OpenColonyWorkspace`, `ReturnToGalaxy`, `InspectSelectedSystem`, `OpenVesselWorkspace` and others). Keep each name on a control that performs the same route, or update the shared navigation helper in the same task with a pinned regression. Never silently break the twelve-scenario matrix.
- Test physical viewport sizes 800x600, 1152x648, 1600x900 and 1920x1080, plus 2560x1440 for asset crispness. Preserve ANGLE, 60 FPS, VSync and current window defaults.
- Keyboard shortcuts never fire while a text field, SpinBox or popup has focus, and never issue a command by themselves; they only open routes.
- The opt-in M14 harness is isolated from the common twelve-scenario target, mirroring M13's isolation and metadata checks.
- All builds/imports/tests use `F:/Tools/CodexTestRunner/runner.py`, project `openantares`, actual active source root, one disk-heavy job at a time. Preserve all branches/worktrees/stashes, owner prose, raw evidence and the historical three-log exception. No merge/push/release/cleanup authority is implied.

## Review Focus

1. A restyle can silently change hit areas or hide controls at 800x600: Task 3 `EveryInteractiveControlReachableAtAllSizes`.
2. A hotkey or drag gesture can issue an order the player did not confirm: Tasks 4/8 `ShortcutsAndDragsIssueAtMostOneExplicitCommand`.
3. Converting dialogs to workspaces can lose return context or strand a required response: Task 8 `WorkspaceExitNeverAnswersOrDropsRequiredResponse`.
4. Status-strip totals can disagree with the per-colony numbers they summarize: Task 5 `StatusTotalsEqualProjectedEconomy`.
5. Fleet stacking can merge ships that differ in a way orders care about (design, officer, request membership): Task 7 `StacksOnlyMergeOrderEquivalentShips`.
6. Theme work can leak into C# overrides over time: Task 1 static check `NoStylingOverridesInGameCode`, run in every gate.

## Acceptance criteria (proposed; becomes the spec if D-review option (a) is chosen)

| ID | Criterion |
| --- | --- |
| UIX-AC-001 | Every panel, button, tab, list, dialog and workspace renders from theme StyleBoxes with chamfered corners and the approved palette; no control shows the default Godot grey style. |
| UIX-AC-002 | Display, body and mono fonts apply through the theme; numbers in readouts and tables use the mono face. |
| UIX-AC-003 | A status strip shows turn, Production, Research and Population with per-turn change, a breakdown tooltip for each, and research target progress; values equal the existing projected economy. |
| UIX-AC-004 | The command bar groups empire screens with visible hotkey hints, a gear Game key, an alert badge with count and a wide End Turn key showing pending items from M13 turn guidance. |
| UIX-AC-005 | Selecting a star or owned fleet fills the context console (system card with planet portraits/owners/inline actions; fleet card); no modal opens for inspection. |
| UIX-AC-006 | The fleet list groups by location and role, stacks order-equivalent ships with counts, shows status, has no pagination, and keeps row order stable across turns. |
| UIX-AC-007 | Colonies, Research, Fleets (with Scouts), Designs and Diplomacy (with Contacts) open as workspaces with a back key and Esc; only confirmations remain modal. |
| UIX-AC-008 | The colony ledger shows workers as figures per role; reassigning (drag or keyboard) issues exactly one existing workforce command per action and shows its result. |
| UIX-AC-009 | The map shows star glow, a backdrop, ownership rings, fleet flags with counts, the selected route with ETA from existing calculations, and owner/range/threat/habitat overlays only where existing views supply the data. |
| UIX-AC-010 | Player-facing copy uses in-world labels and correct plurals; rules explanations move to tooltips; no spec-voice strings remain in the main loop. |
| UIX-AC-011 | Text meets 4.5:1 contrast (3:1 for 24px+); status colors differ in lightness, not hue only; scanlines can be turned off. |
| UIX-AC-012 | All prior native scenarios, layout checks, the full Windows suite and M13 acceptance still pass on the final source; no simulation/content/save change. |
| UIX-AC-013 | Owner physical-play acceptance of look and comprehension (human scope; separate from automation). |

## File ownership and interface contracts

All new paths are proposed. Existing paths were inspected at main 60044af-era source (2026-11-08).

| Owner | Existing files to extend | New files and responsibility |
| --- | --- | --- |
| Assets | `LICENSING.md`, `project.godot` (import defaults only if required) | `art/fonts/*` (+ OFL texts); `art/ui/frames/*.png` nine-patches at 2x; `art/ui/icons/*.svg` resource/role/alert icons; `art/ui/README.md` provenance |
| Theme | `game/theme/openantares_theme.tres` | `game/theme/console_screen.gdshader` (D2 exception) |
| Shell/layout | `game/Screens/GameScreen.cs`, `game/Main.cs` | `game/Components/StatusStrip.cs`, `game/Components/CommandBar.cs`, `game/Components/ContextConsole.cs`, `game/Components/WorkspaceFrame.cs` |
| Presentation models | `game/BreakdownText.cs`, `game/Format.cs` | `game/Presentation/StatusReadouts.cs`, `game/Presentation/FleetGrouping.cs`, `game/Presentation/PendingSummary.cs` (wraps M13 `TurnGuidance`), `game/Presentation/UiSettings.cs` (D2) |
| Console cards | M13 `SelectionSummaryPanel.cs`, `StarPanel.cs`, `VesselPanel.cs`, `PlanetPortrait.cs` | `game/Components/SystemCard.cs`, `game/Components/FleetCard.cs`, `game/Components/EmpireOutliner.cs` |
| Fleets | `FleetCommandDialog.cs`, `ScoutOverviewDialog.cs`, `AssemblyRequestDialog.cs` | `game/Components/FleetList.cs` (virtualized grouped list) |
| Colonies | `ColonyList.cs`, `ColonyPanel.cs`, `ColonyOrdersPanel.cs`, `EmpireManagementDialog.cs` | `game/Components/ColonyLedger.cs`, `game/Components/WorkerFigureRow.cs` |
| Workspaces | `ResearchBar.cs`, `ShipDesignDialog.cs`, `DiplomacyDialog.cs`, `ContactsPanel.cs`, `TurnReportDialog.cs` | Content moved into workspace pages; dialogs kept only as thin confirmation shells |
| Map | `game/Components/GalaxyMap.cs` | `game/Presentation/MapOverlays.cs` (pure overlay selection from existing view data) |
| Simulation tests | none (no `src/` change) | `tests/OpenAntares.Simulation.Tests` unchanged; presentation-model tests go in a new `tests/OpenAntares.Presentation.Tests` project only if the models stay Godot-free, else inside the M14 harness |
| Acceptance | `LayoutTrialValidation.cs`, `CombinedUiNavigation.cs`, `Verify-NormalAssembly.ps1` | `tests/OpenAntares.Game.Validation/VisualOverhaulValidation.cs`, `.tscn`, `VisualOverhaulValidation.targets`; `tools/ConflictValidation/Run-VisualOverhaul.ps1`, `Verify-VisualOverhaulEvidence.py`; `tools/Check-NoStylingOverrides.ps1` |

Proposed contracts (defined by the producing task):

- Task 3 theme vocabulary (type variations, base type in brackets): frames `ConsoleFrame`, `ScreenInset`, `StatusStrip`, `CommandBar`, `WorkspaceFrame` [PanelContainer]; keys `CommandKey`, `TabKey`, `IconKey`, `PrimaryKey` [Button], with existing `EndTurnButton` retained and restyled; readouts `ReadoutValue`, `ResourceProduction`, `ResourceResearch`, `ResourcePopulation`, `DeltaPositive`, `DeltaNegative` [Label]; labels `DisplayTitle`, `StencilLabel`, `AlertBadge` [Label]; icons on type `Icons` (`production`, `research`, `population`, `alert`, `gear`, role icons). Existing variations (`TitleLabel`, `HeaderLabel`, `SubtleLabel`, `WarningLabel`, `ConsoleHeading`, `PanelPadding`, `ConsolePadding`, `NavigationPadding`, `GalaxyMap/*`) keep their names and are restyled.
- Task 5: `StatusReadout(string Key, int Value, int Delta, string BreakdownText)`; `StatusReadouts.Build(EmpireView view, ContentSet content): IReadOnlyList<StatusReadout>`. Values come from existing projected economy; if no empire-level production projection exists, sum per-colony projected values here and test equality against them. No new simulation interface.
- Task 4: `PendingSummary.Build(IReadOnlyList<TurnGuidanceItem> items): (int Required, int Consider, string Line)`. Clicking a part routes through M13's destination allowlist only.
- Task 7: `FleetGroupKey(StarId? Location, string RoleId, string DesignId, bool Named, string? RequestId)`; `FleetGrouping.Build(EmpireView view): IReadOnlyList<FleetGroup>` sorted by location then role then design then lowest VesselId. Ships that differ in officer, request or design never share a stack.
- Task 8: `WorkspaceFrame.Open(string key, Control content, string title)`; `Back` restores the M13 `PlayabilityContext` semantic return; Esc equals Back. `GameScreen.ShowWorkspace` remains the single router.

## Queue commands used by the tasks

Same pattern as M13, with `$sourceRoot` the isolated checkout created after approval and every evidence directory new:

```powershell
& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot $sourceRoot -Suite build
& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot $sourceRoot
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- pwsh -NoProfile -File tools/Run-LayoutTrialChecks.ps1 -OutputDirectory reports/visual-overhaul-execution/UNIQUE-layout
```

The future `tools/ConflictValidation/Run-VisualOverhaul.ps1 -RunName UNIQUE -Case <Theme|CommandBar|Status|Console|Fleets|Workspaces|Ledger|Map|Copy|Contrast|All>` queues its own opt-in/ordinary builds, writes explicit Godot logs and captures a screenshot matrix (each case x five sizes). A selected-case pass is never milestone acceptance.

### Task 1: Baseline, harness and the no-override guard

**Files:** M14 harness/targets/runner/verifier, `tools/Check-NoStylingOverrides.ps1`, extend `Verify-NormalAssembly.ps1`. Evidence in a fresh `reports/visual-overhaul-execution/`. No product change.

- [ ] Confirm M13 is integrated; record main HEAD, status, refs/stashes/worktrees and owner-file hashes; create the isolated checkout via `superpowers:using-git-worktrees`. Reconcile M13's final component contracts with this plan; stop for owner review if they differ materially.
- [ ] Write `Check-NoStylingOverrides.ps1`: fails on `AddTheme*Override`, `Color(`/`Colors.` literals used for styling, `new StyleBox*`, `FontFile`/`SystemFont` construction or `CustomMinimumSize` used as styling in `game/**`. Allowlist existing occurrences found at baseline with file:line and owner-visible justification; the allowlist may only shrink. Prove it fails on a planted violation.
- [ ] Capture the baseline screenshot matrix (all current screens x five sizes) and a control inventory (Name, type, visible, rect) per screen. These are the "before" frames and the reachability baseline.
- [ ] Run baseline queued build, full suite, layout checks and M13 acceptance; record exact counts. Exercise opted-in vs restored ordinary DLL metadata as in M13 Task 1.

### Task 2: Fonts, frame art and icons

**Files:** `art/fonts/*`, `art/ui/**`, `LICENSING.md`, `art/ui/README.md`.

- [ ] Add the three D3 font families (needed weights only) with OFL license texts; record each in LICENSING.md with source URL and version.
- [ ] Produce original nine-patch frames at 2x (outer frame plate with highlight/shadow bevel, screen inset, command key normal/hover/pressed/disabled/focus, tab latched/unlatched, End Turn key) and the icon set (production, research, population, alert, gear, back, each vessel role in `content/core` roles, fleet status: idle/moving/combat/damaged). Record authorship and CC0 dedication in `art/ui/README.md`.
- [ ] Verify imports at 1x and 2x (filter, mipmaps off for UI), file sizes, and that no asset is referenced by C# path except through the theme or the existing icon loader.

### Task 3: Theme pass across every existing screen

**Files:** `openantares_theme.tres`; C# limited to `ThemeTypeVariation` assignments and container structure needed to host frames (e.g. wrapping a page in a `PanelContainer`).
**Interfaces:** produce the Task 3 vocabulary.

- [ ] Add failing harness cases `EveryControlUsesThemedStyle` (no control resolves to the engine default stylebox), `EveryInteractiveControlReachableAtAllSizes` (compared with the Task 1 inventory), `FocusIsVisibleOnEveryKey`.
- [ ] Define palette constants and StyleBoxes: `StyleBoxFlat` with `corner_detail = 1` for 45-degree chamfers (radii on top-left/bottom-right for primary frames), borders/shadows/expand margins for depth; `StyleBoxTexture` nine-patches where bevels or greebles are needed. Fill Button/TabContainer/TabBar/ItemList/Tree/LineEdit/SpinBox/OptionButton/PopupMenu/ScrollBar/ProgressBar/Window/AcceptDialog/TooltipPanel base types, then the variations.
- [ ] Assign variations in existing components (status labels, readouts, keys). Do not restructure layout yet.
- [ ] Rerun the cases plus the full native scenario matrix (restyle must not break control discovery). Retain before/after frames for owner comparison.

### Task 4: Command bar and End Turn

**Files:** new `CommandBar.cs`, `PendingSummary.cs`; `GameScreen.cs`; `CombinedUiNavigation.cs` only if a route's control moves.
**Interfaces:** consume M13 `TurnGuidance`; produce Task 4 contract.

- [ ] Add failing cases `CommandBarGroupsAndHotkeysOpenRoutesOnly`, `ShortcutsAndDragsIssueAtMostOneExplicitCommand` (shortcut part), `ShortcutsIgnoredWhileEditingText`, `EndTurnShowsPendingAndRoutesToEachItem`, `HarnessRouteNamesStillResolve`.
- [ ] Build the bar: empire keys (Colonies C, Fleets F, Research R, Designs D, Diplomacy P) as icon+label `CommandKey`s with hotkey hints; alert `IconKey` with `AlertBadge` count; gear key hosting the existing Game menu (IDs 0/1/2 unchanged); wide End Turn key with the pending line. Scouts and Contacts keys remain until Task 8 moves them into workspace tabs, keeping their `Name`s.
- [ ] Rerun cases and Crisis/Combat/Diplomacy/EmpireConflict checks; Ready availability and rejections must match baseline exactly.

### Task 5: Top status strip

**Files:** new `StatusStrip.cs`, `StatusReadouts.cs`; `GameScreen.cs`, `ResearchBar.cs` (research target selector moves into the strip or the Research workspace; its command behavior is unchanged).

- [ ] Add failing `StatusTotalsEqualProjectedEconomy` (every species, positive/short support, blockade pressure, zero-colony), `BreakdownTooltipMatchesBreakdownText`, `StripFitsAllSizes` (800x600 collapses labels to icons, never clips values).
- [ ] Implement the strip: empire name/emblem, turn, readouts with delta, research target with progress and turns-remaining from existing estimates (conditional estimates shown as conditional). Remove the duplicated turn/count labels from the console.
- [ ] Rerun cases plus Species/Colony regressions.

### Task 6: Context console and outliner

**Files:** new `ContextConsole.cs`, `SystemCard.cs`, `FleetCard.cs`, `EmpireOutliner.cs`; M13 `SelectionSummaryPanel.cs`, `StarPanel.cs`, `VesselPanel.cs`, `GameScreen.cs`.

- [ ] Add failing `SelectionFillsConsoleWithoutModal`, `SystemCardShowsOnlyKnownFacts` (unknown/stale/foreign planets show no fabricated detail), `InlineActionsUseExistingCommands`, `OutlinerOrderStableAcrossTurns`.
- [ ] Console tabs System / Empire / Fleets. System card: header, planet portraits (existing `PlanetPortrait`), owners, habitability where known, inline Manage/Colonize/Send-fleet actions mapped to existing routes. Fleet card: role icons, counts, orders, destination/ETA from existing calculations. Empire tab: outliner of colonies and alerts. Replace the Inspect/Manage/Vessels/Colony-overview button stack while keeping their `Name`s on the equivalent actions.
- [ ] Rerun cases, M13 Selection/Navigation cases and Expansion/Fleet/Scout scenarios.

### Task 7: Fleet list rework

**Files:** new `FleetGrouping.cs`, `FleetList.cs`; `FleetCommandDialog.cs`, `ScoutOverviewDialog.cs`.

- [ ] Add failing `StacksOnlyMergeOrderEquivalentShips`, `GroupOrderStableAndIdOrdered`, `SelectionOfStackExpandsToExplicitShips` (orders target explicit VesselIds, never "the stack"), `ThousandShipListStaysResponsive` (frame-time budget recorded, not asserted against wall clock in the simulation).
- [ ] Implement grouping per the Task 7 contract, a virtualized list (Tree with lazy children or recycled rows), filter chips, multi-select by Shift/drag, status lights. Remove pagination. Existing request/officer/admiral detail routes stay reachable.
- [ ] Rerun cases plus Fleet/Scout/ShipDesign/Combat scenarios.

### Task 8: Workspaces replace dialogs; colony ledger

**Files:** new `WorkspaceFrame.cs`, `ColonyLedger.cs`, `WorkerFigureRow.cs`; `GameScreen.cs`, `ColonyList.cs`, `ColonyPanel.cs`, `ResearchBar.cs`, `ShipDesignDialog.cs`, `DiplomacyDialog.cs`, `ContactsPanel.cs`, `FleetCommandDialog.cs`, `ScoutOverviewDialog.cs`, `CombinedUiNavigation.cs`.

- [ ] Add failing `WorkspaceExitNeverAnswersOrDropsRequiredResponse`, `BackAndEscRestoreSemanticContext`, `OnlyConfirmationsRemainModal`, `LedgerReassignIssuesOneWorkforceCommand` (drag and keyboard paths), `LedgerShowsRejectionVerbatim`, `ShortcutsAndDragsIssueAtMostOneExplicitCommand` (drag part).
- [ ] Build `WorkspaceFrame` (title, back key, Esc) on the existing `ShowWorkspace` router. Move Research, Fleets (+Scouts tab), Designs and Diplomacy (+Contacts tab) content into workspace pages; reduce the old windows to confirmation shells or remove them where unused. Keep route `Name`s.
- [ ] Build the ledger: one row per colony with worker figures per role, outputs in resource colors, build item with progress; drag or keyboard reassign issues the existing workforce command once and shows its actual result. Keep the detailed colony panel reachable from each row.
- [ ] Rerun cases, the full twelve-scenario matrix and M13 Navigation; record updated layout-check counts honestly.

### Task 9: Map dressing and overlays

**Files:** `GalaxyMap.cs`, new `MapOverlays.cs`, theme `GalaxyMap/*` constants and icons.

- [ ] Add failing `MapShowsNoUnknownFacts` (rings/flags/overlays only from requester-visible data), `RouteEtaMatchesExistingCalculation`, `OverlayTogglesIssueNoCommands`, `LabelsStillUseM13Layout`.
- [ ] Draw star glow and a backdrop derived from existing `GalaxyBackgroundPoints` (no new randomness); ownership rings and territory wash in empire colors; fleet flags with counts; selected-route dashed line with ETA badge. Overlays: Owners, Range, Threat, Habitat, each enabled only when the existing view provides its data. Single neutral star tint per D1.
- [ ] Rerun cases plus Rival/Expansion/Conflict scenarios; capture frames at all sizes.

### Task 10: Copy pass, scanline effect and settings

**Files:** user-facing strings in touched components; `console_screen.gdshader`; `UiSettings.cs`; `docs/DESIGN_GUIDE.md` (exception note and voice guide).

- [ ] Add failing `NoSpecVoiceStringsInMainLoop` (allowlisted phrase list from the baseline inventory), `PluralsAreCorrect`, `ScanlinesToggleAndReducedEffectsPersistOutsideSaves`.
- [ ] Rewrite labels to short in-world instrument language with correct plurals; move rules explanations to tooltips without dropping information. Add the scanline/vignette shader on `ScreenInset` areas only, with the D2 toggle.
- [ ] Rerun cases; confirm saves are byte-identical to baseline for the same commands.

### Task 11: Contrast, readability and accessibility pass

- [ ] Add `TextContrastMeetsThresholds` (computed from theme colors for every label/background pairing in use), `StatusColorsDifferInLightness`, `MinimumTargetSize` (44 px at 1920x1080, documented scale-down at 800x600).
- [ ] Fix failures in the theme only. Record results per size.

### Task 12: Bounded findings, owner playtest packet, final regression and review

- [ ] Triage findings with screenshot/log/source/steps/severity; up to the D5 budget of reproduced fixes, each with a regression established RED first. Anything needing `src/`, content or a new system goes to a separate scope.
- [ ] Prepare the owner physical-play packet: queued build path, installed Godot path, fresh log, numbered journeys (map selection, console actions, fleets, ledger drag, each workspace, End Turn pending items, save/load) at 800x600 and 1920x1080, with look-and-feel questions and report-back fields.
- [ ] Final gates on one source: queued build, full Windows suite, layout checks, all M14 cases, all M13 cases, `Run-NativeMatrix.ps1 -EarnedConflictCampaigns`, the no-override check and the M14 verifier (before/after frame matrix complete, no missing cases). Ordinary validation namespace empty after restoration.
- [ ] One fresh whole-M14 implementation review; fix required findings within scope; rerun invalidated gates. Final report with per-criterion automated/human status. Hand off for owner disposition; no merge/push/release/cleanup.

## Coverage

| Criteria | Tasks |
| --- | --- |
| UIX-AC-001/002 | 2, 3, 12 |
| UIX-AC-003 | 5, 12 |
| UIX-AC-004 | 4, 12 |
| UIX-AC-005 | 6, 12 |
| UIX-AC-006 | 7, 12 |
| UIX-AC-007/008 | 8, 12 |
| UIX-AC-009 | 9, 12 |
| UIX-AC-010 | 10, 12 |
| UIX-AC-011 | 10, 11, 12 |
| UIX-AC-012 | 1, 12 |
| UIX-AC-013 | 12 and owner acceptance |

Review choices: D1-D5, the spec route (a or b above), sequencing after M13, the presentation-only boundary and the fix budget. Native inline method is retained. Nothing has been implemented by writing this plan.
