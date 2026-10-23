# First Playable Validation and Exploration Design Implementation Plan

> **For agentic workers:** Use this plan in order. The playtest findings determine which fixes are needed; do not invent code tasks before those findings exist. Use checkboxes (`- [ ]`) to track execution.

**Status:** Approved 2026-10-19; execution in progress. Baseline evidence is in [the playtest record](../../playtests/first-playable-2026-10-19.md).

**Continuation 2026-10-22:** A retained opt-in Godot harness now passes 463 assertions covering the runtime portion of Tasks 2/4, including FP-001/002, live resizing, save continuation and legacy/corrupt UI dialog paths. See [the continuation report](../../../reports/FIRST_PLAYABLE_VALIDATION_2026-10-22.md). Checkboxes below describe the full original task contracts; they are not all satisfied by in-process signal dispatch. Task 3 uncoached observations, complete physical-input/readability acceptance and Task 5 closure remain pending. Use [the observer sheet](../../playtests/first-playable-observer-sheet.md); do not repeat runtime tests as a substitute for human evidence. Task 6 was subsequently authorized and completed as recorded below.

**Subsequent owner amendment, 2026-10-22:** [ADR-005](../../DECISIONS/ADR-005-defer-uncoached-playtest-for-design.md) defers Task 3's unavailable uncoached session and authorizes Task 6 design now, without claiming Task 5 full acceptance. The earlier closure-before-design sequence is superseded for this transition. Design review before implementation remains required; do not keep asking for another participant before proceeding with design.

**Goal:** Verify the first playable loop in Godot and produce a reviewed exploration/second-colony design. Full acceptance still requires the documented evidence; ADR-005 permits design now while the unavailable uncoached session is deferred.

**Architecture:** Separate evidence gathering from changes. Record repeatable Godot runs and player observations first; turn each confirmed blocker into a scoped fix with its own regression check. Treat exploration, travel, and colonization as the next design milestone, with implementation planned only after its rules are settled.

**Tech Stack:** Godot 4.7 .NET, C#, JSON content, xUnit.

**Sources:** [Charter](../../CHARTER.md), [design guide](../../DESIGN_GUIDE.md), [first playable economy specification](../../FIRST_PLAYABLE_ECONOMY.md), and [spiral galaxy design](../specs/2026-10-05-spiral-galaxy-design.md).

## Global Constraints

- Keep simulation rules independent of Godot; state changes go through empire commands.
- Preserve deterministic integer simulation, stable IDs, content validation, and save compatibility.
- The current map exposes every generated system. An empire knowledge model belongs to the exploration design, not a silent first playable change.
- Do not add AI, diplomacy, combat, or final art to close the first playable milestone.
- Preserve unrelated working-tree changes, including the current species and research edits.

## Review Focus

1. A save made while production and research are in progress must reload with the same choices, reserves, workforce, turn, and galaxy framing.
2. A completed building or technology must appear once in the turn report, affect forecasts at the documented time, and remain complete after reload.
3. Small, medium, and large maps must show 12, 16, and 20 selectable systems respectively; background stars must never be selectable.
4. At different window sizes, the player must still distinguish the home system, selection, labels, regions, and available controls.
5. A player must be able to explain a workforce tradeoff and the next turn's result from the interface without reading the rules or source code.

---

### Task 1: Establish the test and playtest baseline

**Files:** Create `docs/playtests/first-playable-2026-10-19.md` as the run record. Do not alter game code.

- [x] Record the tested commit, Godot .NET version, OS, display resolution, and save directory (`user://saves`) in the run record. Keep test saves under unique `qa-<date>-<size>-<seed>` names so existing saves are not overwritten.
- [x] Restore and run the simulation suite from the repository root. In the Codex sandbox, set `NUGET_PACKAGES` to the existing user cache and use `-p:NuGetAudit=false` for restore; record the reason for any disabled audit. Record the exact command, exit code, and test totals. A passing historical count of 195 is not a substitute for this run.
- [x] Build `OpenAntares.sln` and open `project.godot` in the Godot 4.7 **.NET** editor. Record build and editor errors before playtesting. If the editor is unavailable, mark the manual checks blocked rather than claiming milestone acceptance.

### Task 2: Exercise the charter's eleven actions in Godot

**Files:** Append a per-size matrix and evidence to `docs/playtests/first-playable-2026-10-19.md`. Use `content/core/galaxy.json` for the expected counts.

- [ ] Start separate new games for `small`, `medium`, and `large` with seed `42`. Record whether setup succeeds, the map reads as a spiral, the star counts are 12/16/20, core/arm/rim appear, and stars and planets can be inspected. Check selection, labels, home marker, and resize behavior; confirm background points cannot be selected.
- [ ] In each game, inspect the starting colony and record its initial population and workforce. Change workforce between support, production, and research; write down the forecast before ending the turn and the reported values afterward. Test one support shortage and one supported allocation so the tradeoff is visible.
- [ ] Select a construction project and a research target. Advance until population changes, one project completes, and one technology completes; continue at least one turn after each completion to inspect when its effect appears. Do not impose a fixed turn count if the chosen allocation needs more time.
- [ ] Save once with unfinished progress and once after a completion. Reload each save through the in-game Load control, then compare turn, colony, workforce, reserves, targets, completed items, and map framing with the recorded pre-load state. Record which system the UI selects after load, but do not require presentation selection to be saved. Continue for one turn after reload.
- [ ] Mark each of the charter's eleven actions pass/fail with a screenshot or concise observation. Run one legacy version-1 save through the UI if a suitable fixture can be loaded without modifying it; otherwise record that UI compatibility remains unverified while the automated save test covers the format.

### Task 3: Test whether the loop is understandable

**Files:** Append anonymized observation notes to `docs/playtests/first-playable-2026-10-19.md`.

- [ ] Have at least one player who has not read the source play from a fresh game through a save and reload while an observer records actions without coaching. The observer may explain controls only after recording where the player got stuck.
- [ ] Before each end turn, ask what the player expects to happen to support, production, research, and growth. Afterward, ask why the reported values changed and when a new building or technology will help. Record the player's explanation in their own words, plus repeated clicks, screen switching, and confusing labels.
- [ ] Ask the player to choose between two workforce allocations and between two available goals. Record whether the preferred choice changes with a stated goal or map situation. Distinguish a confusing presentation from a rule or balance problem.

### Task 4: Triage findings and repair milestone blockers

**Files:** Record each finding and disposition in `docs/playtests/first-playable-2026-10-19.md`; modify source and tests only for confirmed fixes.

- [ ] Classify each finding as **blocker** (one of the eleven actions fails or state is lost), **clarity** (the action works but its consequence cannot be understood), or **later** (outside the first playable). Include reproduction steps, seed/size/save, expected result, actual result, and owner.
- [ ] For each blocker or clarity finding, identify its cause before changing code. Add a focused regression test where the simulation or save contract is involved; for Godot-only behavior, add a repeatable manual check to the matrix. Implement and review each fix as a separate scoped change.
- [ ] Rerun the failing scenario and the complete per-size matrix after fixes. Rerun `dotnet test`; record exit codes and totals. Keep later-scope requests in the backlog rather than adding them to this milestone.

### Task 5: Decide whether the first playable milestone is closed

**Files:** Add a decision section to `docs/playtests/first-playable-2026-10-19.md`; update `docs/DESIGN_GUIDE.md` and `docs/FIRST_PLAYABLE_ECONOMY.md` only if their recorded status or rules change.

- [ ] Confirm every charter action has passing Godot evidence, no blocker or clarity finding remains, save/reload continuation works, and the current automated suite passes. Record any platform or legacy-save checks that were not run as explicit limits.
- [ ] Record a **closed** or **still open** decision with the evidence and unresolved findings. Do not close based only on passing unit tests or a successful launch. If still open, finish the remaining fixes and repeat this gate.

### Task 6: Design exploration, travel, and the second colony

**Files:** Create `docs/superpowers/specs/2026-10-22-exploration-and-colonization-design.md` under the ADR-005 design-entry exception. This task produces a design for review, not gameplay code.

- [x] Define what a new empire knows at turn one, what a scout reveals, when knowledge becomes stale, and what every map, inspector, list, preview, and save may show. State how legacy all-visible saves behave.
- [x] Define scout ownership, movement commands, travel time or range, arrival and discovery timing, invalid orders, and deterministic turn phase. Decide whether `core`, `arm`, and `rim` change travel; if so, put exact integer values in content and require a player-facing breakdown.
- [x] Define settlement eligibility, any colony ship or founding cost, colonization command and timing, new ownership, initial workforce, and how the UI selects and manages two colonies. Include failures such as an occupied destination, insufficient resources, and a scout redirected before arrival.
- [x] Work two contrasting map examples through the proposed rules. Show why they produce different defensible expansion choices and how the player sees the relevant information. Check the design against the charter's command, determinism, modding, and save constraints.
- [x] Review the design with the project owner. Once accepted, write a separate implementation plan with exact state, command, content, save, UI, and test changes. Do not start AI, diplomacy, or combat in that plan.

**Task 6 completion, 2026-10-22:** The full specification, exact worked examples and builder check are complete; the owner approved the design in [ADR-006](../../DECISIONS/ADR-006-approved-expansion-milestone.md). The separate [implementation plan](2026-10-22-exploration-and-colonization.md) is written/self-reviewed and awaits plan review/execution choice. No gameplay coding has started. Tasks 2-5 retain their stated human acceptance limits.

## Completion evidence

The design portion is complete when exploration/colonization rules are reviewed and ready for a separate implementation plan. Full First Playable acceptance remains an independent deferred obligation under ADR-005 and must not be relabeled Verified. Existing defects still require verification; the unavailable participant session does not block design progression.
