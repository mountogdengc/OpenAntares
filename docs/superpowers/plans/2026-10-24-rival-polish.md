# Rival deferred-detail corrections Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (retained Native) to implement this plan task-by-task. Steps use checkbox syntax.

**Goal:** Resolve the two disclosed review deviations against the already approved rival specification.
**Architecture:** Preserve existing C#/Godot controls and immutable viewer reports. Initialize alternate selectors distinctly; capture observed orbit in transient encounter facts and enrich viewer-only reports from the viewer contact record and public content. No save/rules change.
**Tech Stack:** C#, plain .NET8 simulation/xUnit, Godot4.7.2 .NET.
**Spec:** ../specs/2026-10-24-rival-expansion-design.md, RIV-001/RIV-005; reports/RIVAL_FINAL_REVIEW_2026-10-24.md.

## Global Constraints

- Preserve core Harl/Peldra defaults, independent later choices, mirrors and Solo. Alternative initial rival is ordinal first different species, or the sole species.
- Unknown history stays unknown. Say observed, with species/culture, star/orbit/date; first-contact has no invented planet.
- Known identity comes from viewer contacts, not live foreign state. Keep viewer filtering and exact own economy; report metadata is transient, no saved-state/schema/rules/AI changes.
- Preserve owner prose, peaceful backlog/status paragraph, all saves/recovery/worktrees. Use the owner-selected local integration workflow after review/green gates; no push/release/cleanup. Human acceptance remains Deferred.
- Inline Native, one fresh whole-branch review after green gates; one regression-first Important/Critical correction pass if needed.

## Review Focus

1. Missing Harl with Peldra present must not initialize a mirror; later manual mirrors must remain legal (Task1).
2. One/zero species, missing Peldra and renamed/reordered definitions must not break Load or stable initial choices (Task1).
3. Several foreign colonies in one star must have distinguishable observed orbit text (Task2).
4. Later surveys or hidden foreign identity/economy/planet changes must not rewrite dated report facts or reveal another viewer's events (Task2).
5. First contact from ships alone and old rules must not invent planet details or activate new legacy behavior (Task2).

### Task 1: Distinct alternative-content initialization

**Files:** game/Screens/NewGameScreen.cs; tests/OpenAntares.Game.Validation/RivalGraphicalValidation.cs.
**Interfaces:** Existing Configure/StartRequested unchanged; initial selectors only. Consumes no new Task2 API.
- [x] RED: replace the divergent graphical fallback assertion with literal human/rival pairs for missing Harl (Peldra/Sulan), missing Peldra (Harl/Sulan), neither (Sulan/Wenth), sole Peldra/Harl/Wenth and empty species/Load. Assert actual StartRequested choices and later independent mirrors. Run Rival in a unique reports/rival-polish-execution/task1-red directory; expect current missing-Harl case to fail.
- [x] GREEN: use the core pairing only when available, otherwise ordinal-first different rival. Run full Rival scene and full simulation; expect all pass, with zero legacy behavior change. Commit fix: initialize alternate rivals distinctly.

### Task 2: Dated encounter detail and final gates

**Files:** src/OpenAntares.Simulation/Turns/EncounterEvent.cs, Knowledge/ContactUpdater.cs, Knowledge/EmpireViewProjector.cs; game/Components/TurnReportDialog.cs; tests/OpenAntares.Simulation.Tests/RivalContactTests.cs; tests/OpenAntares.Game.Validation/RivalGraphicalValidation.cs; status/evidence.
**Interfaces:** Append nullable init properties SpeciesId/SpeciesName/CultureName/PlanetOrbit to EncounterEvent, retaining its existing positional constructor and EmpireTurnReportView.EncounterEvents type. ContactUpdater captures orbit from current PlanetObservation. ProjectReport enriches only viewer-filtered events using that viewer's contact record/public definitions. No GameState or save serialization change.
- [x] RED: typed report regression checks observed identity/orbit/date and first-contact null orbit; declarations may follow initial compile RED, then require observed semantic RED. Add actual report-dialog fixture with two observed planets in one system and hidden/later mutation invariance. Run focused RivalContactTests then graphical Rival in fresh directories; expect missing metadata/wording failures.
- [x] GREEN: capture orbit at observation, project known identity, render culture/species/star/orbit/date with observational wording and safe unknown fallback. Keep first-contact prefix used by retained scenario. Run focused then full simulation and normal build; expect all green.
- [x] Full gate: sequential Rival, Species, Expansion, Legacy scenes into unique rival-polish-final directory, normal rebuild and metadata exclusions. Inspect real result counts/logs/representative PNGs. No OS-input/readability/balance/platform/release claim.
- [x] Obtain one fresh read-only whole-branch review base69f7e1c..HEAD using all five focus cases and rulings. Re-grade and resolve Important/Critical regression-first; record any Minor/declined items explicitly, no re-review.
- [x] Reconcile RIV-AC-001/008 and M4-I only after actual gates/review; keep parent REQ-017 In Progress. Update PROJECT_STATUS with exact source, preservation and next integration decision; commit evidence/handoff.

## Authorization and scope

Owner continue-on follows the two disclosed minor fixes as the smallest next task; this plan transcribes approved behavior rather than introducing a new design or reopening method approval. Work on codex/rival-polish from main69f7e1c. Earlier integrated branches remain frozen. The prior explicit option1 local integration choice is retained for these bounded continuation fixes; apply locally only after fresh review/gates with exact owner-change preservation. No new remote/release/cleanup authorization.
