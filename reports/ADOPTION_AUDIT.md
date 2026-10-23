# OpenAntares adoption and reconciliation audit

**Follow-up:** The subsequent [2026-10-22 validation continuation](FIRST_PLAYABLE_VALIDATION_2026-10-22.md) adds retained runtime coverage and closes the legacy UI dialog-path gap. It does not close human understanding, complete physical-input/readability acceptance, integration or release. Findings below describe the original adoption snapshot; use `PROJECT_STATUS.md` for current next steps.

**Owner gate amendment:** [ADR-005](../docs/DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md) subsequently defers the unavailable uncoached session and authorizes expansion design before full acceptance. That missing participant is no longer a design blocker. New gameplay choices remain subject to design review.

**Date:** 2026-10-22, America/Denver. **Scope:** Existing-project adoption and documentation; no broad feature implementation, dependency upgrade, merge, commit, push, or cleanup. **Result:** A verified local technical baseline and actionable documentation exist. **Readiness: Not ready for broad feature implementation or release; ready for scoped First Playable validation closure.**

## Identity, authority, and reconstruction

The repository is `mountogdengc/OpenAntares`, an original desktop space 4X game, not an OpenJRPG engine. The owner's supplied workflow PDF is applicable as a process/template bundle; its project-specific RPG requirements and ADR-014/015 do not apply. Existing project spelling **OpenAntares** is retained. No rename or code migration was necessary.

Inspected sources include root engineering/licensing/project files, charter/design guide, detailed economy and spiral specifications, species roster, historical/current plans and playtest record, simulation state/commands/turn/session/content/save boundaries, Godot entry/screens/components, content fixtures, and the automated suite. No prior root README, AGENTS, PROJECT_STATUS, standard requirement register, or decision directory was found.

The charter remains authoritative for scope/intent, `CLAUDE.md` for engineering constraints, and existing subsystem specs for exact behavior. The design guide explicitly distinguishes established/proposed/open entries; it still describes some implemented economy choices as proposed, while the economy spec identifies its contract as implemented and subject to balance review. Reconciliation follows the specific implemented spec without treating every guide proposal as approved. Research does not authorize features. The species roster is accepted creative direction only.

## Verified technical baseline

| Check | Fresh result | Limit |
| --- | --- | --- |
| Solution build | Exit 0, 0 warnings, 0 errors | Existing restored cache, Windows x64 Debug |
| Simulation/file/integration suite | 195 passed, 0 failed, 0 skipped | Engine-independent suite; no player/UI acceptance |
| Godot .NET seeded startup | Small/Medium/Large seed 42, each exit 0, no errors/warnings in captured logs | Headless 60-frame launches; no visual/click assertions |
| Architecture boundary | Compiled simulation assembly has no Godot reference | UI no-direct-mutation remains an engineering convention |
| Git source/document preservation | Original tracked files and pre-existing untracked docs retained | Existing dirty work is not an integrated release |

Exact commands, evidence, environment, and scope are in [VALIDATION_REPORT.md](VALIDATION_REPORT.md). Tests cover content/state validation, generation and regions, PCG32, command rejection/ownership/readiness, exact economy examples and timing, forecast agreement, deterministic runs, schema-1 compatibility, failed writes/loads and reload continuation. No automated failures remain in the executed suite.

## Implementation reconciliation

| Area | Evidence and disposition |
| --- | --- |
| Simulation foundations | Existing plain .NET state, stable IDs, PCG32, checked integer economy and command/turn pipeline retained; verified local contracts REQ-001 through REQ-009 and REQ-012 |
| Content and persistence | JSON definitions and schema-2 embedded-content saves implemented; schema-1 read path tested; no namespacing or extension redesign |
| Godot loop | Screens, map/inspectors, workforce/project/research controls, reports and Save/Load implemented; fresh startup only; complete current graphical acceptance remains unverified |
| Construction follow-up | Local `ColonyPanel`/`GameScreen` fixes correspond to FP-001/002 and prior runtime evidence; retained, not reimplemented; temporary historical regression scenes were removed |
| First Playable | Existing owner-reported functional passes retained. Player understanding and explicit closure remain open; 195 tests do not close it |
| Exploration/colonization | Approved next design task after closure; no design file exists at the expected path and no scout/travel/founding command/state is present |
| Species | Four prose identities accepted; current local appearance/faction notes retained. No species selection, content schema or numeric modifiers implemented |
| Later strategic game | AI policy, diplomacy, ships/fleets/combat, events and victory remain later scope; a controller enum and multiplayer-ready commands are foundations, not delivered features |

[SPECIFICATIONS.md](../docs/SPECIFICATIONS.md) now tracks 17 requirements: 10 Verified, 3 Implemented, Unverified, 1 In Progress, 3 Deferred. No requirement is marked Blocked merely to represent an unattempted future subsystem. Blocking milestone gates are identified separately below.

## Repository, branches, and worktrees

Inspected on 2026-10-22. No Git refs were changed by adoption.

- Primary/active branch: `main`, HEAD `6ab206d3ca4811cc9163ee6ea3d5c00ff4bb7b6b` (`docs: update first playable design status`). Cached `origin/main` is `9d4a654`; local main is ahead by one commit. This is a cached-ref comparison, not an assertion that the remote head was freshly fetched.
- Cached `origin/feat/spiral-galaxy`: `acf5f06a5920a27bccd89c0cde178d5128d294ac`. `main...origin/feat/spiral-galaxy` is 4/0 commits. Spiral changes were already merged through PR #22; the feature tip has no commits outside main.
- Registered primary worktree: `F:/Projects-PCGames/OpenAntares`.
- Registered secondary worktree: `C:/Users/david/.codex/worktrees/spiral-galaxy/OpenAntares`, detached at `acf5f06`. Read-only status is clean; `main..HEAD` has no commits. No pending feature merge found. Do not delete it as part of adoption.
- `git stash list` is empty; no release tags found. No in-progress merge/conflict reported by status. No Codex task artifacts are attached.
- Live GitHub connector search returned **0 open PRs**. Paginated branch search returned `main` and `feat/spiral-galaxy` only. No PRs were opened, modified or attached for this audit.

Pre-existing tracked modifications:

| Path | Existing work preserved |
| --- | --- |
| `docs/SPECIES_ROSTER.md` | Animal-inspired visual direction and shark-like Sulan description |
| `docs/research/faction-survey.md` | Matching research interpretation |
| `game/Components/ColonyPanel.cs` | Retained/focused construction picker; popup after focus scrolling |
| `game/Screens/GameScreen.cs` | Construction report action and focus-following scroll container |
| `project.godot` | 1920x1080 viewport default |

Pre-existing untracked material: approved `2026-10-19-first-playable-validation-and-exploration-design.md` plan; the `docs/playtests/` record, five PNGs, three import sidecars and seven Godot logs. These have not been automatically staged or committed. The research directory is ignored for new files, but three existing research Markdown files are tracked; ignore rules do not make their tracked edits disappear.

The one local commit, intended UI changes, playtest/plan evidence, new adoption docs/results, and unrelated creative edits require deliberate review when preparing an integrated baseline. No actual merge conflict was found; future integration conflicts have not been predicted or tested.

## Prioritized gaps and decisions

| ID | Finding | Impact / next resolution |
| --- | --- | --- |
| GAP-01 | Uncoached player-understanding observation and closure decision absent | **Blocks First Playable closure.** Execute AC-014; record explanations, findings and closed/still-open decision |
| GAP-02 | Current complete graphical matrix not freshly reproduced; prior temporary UI harnesses removed | **Acceptance gap.** Preserve owner passes, rerun AC-010/011/FP-001/002 with repeatable evidence; do not relabel headless startup visual acceptance |
| GAP-03 | Pending uncommitted UI/evidence/docs and local commit | **Blocks a clean versioned baseline/release.** Review intended changes, integrate deliberately, retest final state; no automatic merge/reset |
| GAP-04 | Knowledge, travel, founding and legacy visibility rules unsettled | **Blocks expansion implementation.** After closure, complete task 6/AC-015 and obtain design review |
| GAP-05 | Species mechanics lack schema/numbers/setup/save/settlement criteria | Deferred, nonblocking for current fixture. Keep creative direction; specify separately before implementation |
| GAP-06 | Linux/macOS/ARM parity and export/clean install unverified | Blocks target-platform/release claims, not local validation. Define support/package matrix and execute AC-013 |
| GAP-07 | No CI/export presets/global SDK pin/release tags; root solution Release maps to Debug | Delivery preparation. Confirm intended Release/package behavior before changing build configuration |
| GAP-08 | No fresh restore or dependency vulnerability/license inventory review | Existing cache suffices for baseline only. Run a normal clean restore/audit and document shipped dependency attribution before distribution |
| GAP-09 | Existing plans contain stale unchecked execution steps; playtest record has earlier-stage prose | Operational confusion risk. New status/plan point to later evidence; original historical records preserved. Update their execution bookkeeping when validation resumes |
| GAP-10 | Performance/minimum hardware, accessibility/localization and large/untrusted-input limits not specified | Later acceptance specifications required before claiming those qualities; no new thresholds inferred |

Outstanding owner/product choices are: First Playable closure after comprehension evidence; subsequent exploration/travel/founding/visibility compatibility design; later species mechanics and delivery policy. No material architecture decision is required to retain and validate the current loop. Selecting another framework or rewriting the engine would contradict the adoption scope.

## Documentation delivered and continuation

Created filled-in README/AGENTS/status, concept/architecture, requirement/criterion register, dependency plan, workflow/adoption/implementation prompts, four recovered decision records/index, audit and validation report with test/startup artifacts. Existing authoritative docs and working code were preserved byte-for-byte. No empty template scaffolding or application code was added.

**Next action:** Resume the existing First Playable playtest record with current graphical picker regressions and uncoached player-understanding evidence. Triage only observed defects, record closure, review/integrate the intended baseline, then undertake exploration/colonization design. Broad implementation remains gated. [PROJECT_STATUS.md](../PROJECT_STATUS.md) is the operational handoff.

## Continuation reconciliation

The retained Godot harness now passes the runtime portion of GAP-02: all three sizes, live resize, FP-001/002, dialog save/reload/continuation, corrupt-save recovery and legacy schema-1 loading. Twenty-four native picker scenarios match normal opening positions; supplementary embedded fixtures supply visual menu evidence. GAP-01 remains open for participant observations; GAP-02 remains open for complete physical-input/readability acceptance. GAP-03 through GAP-10 retain their original integration/design/delivery meanings.

The continuation adds tests and updates validation/status documentation deliberately; the original adoption pass's byte-for-byte preservation statement applies to that earlier pass. Current preservation checks confirm all 89 pre-existing tracked files outside `docs/` remain unchanged from the adoption hash snapshot, including the owner's modified game files/configuration and all original simulation tests. Species/faction edits and authoritative charter/economy/spiral rules remain retained.
