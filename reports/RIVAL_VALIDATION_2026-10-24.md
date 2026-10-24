# Rival expansion validation — 2026-10-24

Current status: the original implementation, startup Load correction and both follow-up details are Verified for the bounded local automated contract. The follow-up has648 simulation tests, all four graphical gates and one fresh independent review with no findings. See [correction closure](RIVAL_POLISH_2026-10-24.md). Earlier runs/review dispositions below remain dated history; human/release acceptance is open.

## Scope and source

Owner-approved design/ten-task plan, retained Native execution; isolated `codex/rival-expansion` from approved main baseline4d2efb5384a100f2c75ce055d0233399f435e5d3. Functional checkpoint01757d39cc75fd2367b06e91e10da5019d1135ad contains Tasks1–9. Task10 adds acceptance harness/docs and the invalid-disk-content Load correction. Main is unchanged. The raw [execution ledger](rival-execution/progress.md) records task commits, RED/GREEN evidence and rulings; the [final review](RIVAL_FINAL_REVIEW_2026-10-24.md) pins reviewed candidate e36d836 and its dispositions. Corrected final source is ce6c29c1cf080893067ac96a43ca1e2366dd97e5.

New branch games use rules4/writer5, visible One/Harl/Peldra defaults, Solo and mirrors. The policy gets no discounts or extra resources: shared species forecasts/cargo/habitats/maturity and ordinary commands govern it. Policies plan privately before economy; full resolution/Ready publication is atomic. Contacts expose discovered identity and dated observations. Old rules1/2/3 retain original arithmetic and passive AI behavior; eleven explicit schema/rules pairs are tested.

## Executed gates

Windows x64, PowerShell, .NET SDK10.0.113/runtime8.0.32, net8.0; installed Godot4.7.2 .NET console with GL Compatibility/NVIDIA RTX3090. Actual Godot executable: `F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe`.

Task1 used normal dependency restore/audit successfully; later gates use existing dependencies with `--no-restore`. This does not establish a clean install or comprehensive security audit. No dependencies, art or theme styles were changed.

First full gate, [run1](rival-final-20261024-run1): simulation642/642; Legacy493 result rows (466 assertions+27 geometry observations),11 QA paths/16 PNGs; Expansion275 assertions,20 QA paths/10 PNGs; frozen Species1194 assertions,74 QA paths/29 PNGs; Rival1120 rows (1108 assertions+12 generated-provenance rows),62 QA paths/19 PNGs. Every process exited0, every results.json passed, normal rebuild0 warnings/errors and [metadata](rival-final-20261024-run1/assembly-metadata.log) excludes all four opt-in harness types. Final engine logs are checked separately from retained expected failing RED runs.

Final [run2](rival-final-20261024-run2) after the startup correction: simulation642/642; Rival1112 assertions+12 provenance rows (1124 total),63 QA paths/19 PNGs; Legacy466 assertions+27 geometry rows,11 QA paths/16 PNGs; Expansion275 assertions,20 QA paths/10 PNGs; Species1194 assertions,74 QA paths/29 PNGs. Every process exited0 and results.json passed. Final engine logs have zero ERROR/WARNING/SCRIPT ERROR entries; normal rebuild0warnings/errors, metadata excludes all four validation types. [Machine summary](rival-execution/final-gate-summary.json).

Commands: `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo`; `./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <path above> -Scenario <Rival|Legacy|Expansion|Species> -OutputDirectory reports/rival-final-20261024-run2/<scenario>` sequentially; `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild`; ordinary PE metadata inspection through the retained helper. Every runner also restores normal assemblies in finally.

## Generated acceptance

All twelve seed42 campaigns are generated unchanged through the actual new-game form, human Harl with each rival species × Small/Medium/Large (including Harl mirrors). Human project/research/scouting/settlement/development choices use visible controls and only the human projection. First scout route is the farthest public chart duration to exercise committed transit; subsequent unknown systems use nearest shared travel. No live outcomes/resources/population/technology/orders are injected, no map rerolled, and no foreign state drives human decisions. Harness reads foreign state only to assert rival outcomes.

| Rival | Size | Rival second colony | Human contact | Completion turn | Completed rival research |
| --- | --- | ---: | ---: | ---: | ---: |
| Peldra | Small | 8 | 2 | 14 | 1 |
| Peldra | Medium | 7 | 12 | 12 | 0 |
| Peldra | Large | 9 | 5 | 9 | 0 |
| Harl | Small | 6 | 2 | 6 | 1 |
| Harl | Medium | 5 | 12 | 12 | 3 |
| Harl | Large | 7 | 5 | 7 | 1 |
| Sulan | Small | 11 | 2 | 11 | 2 |
| Sulan | Medium | 10 | 12 | 12 | 2 |
| Sulan | Large | 11 | 5 | 11 | 2 |
| Wenth | Small | 7 | 2 | 7 | 1 |
| Wenth | Medium | 6 | 12 | 12 | 1 |
| Wenth | Large | 8 | 5 | 8 | 1 |

Every loop includes real human settlement, contact report, pending move/transit/founding saves through ordinary dialogs, canonical comparison after each actual End Turn, and three future turns equal after the completed-campaign reload. Completion turns are before the final three continuation turns. The approved80-turn bound concerns expansion/contact; per-species research is witnessed in Small campaigns, while actual other-map counts are retained. This is core fixture evidence, not universal balance/performance.

## Current acceptance mapping (follow-up closure included)

| Child criterion | Status | Evidence |
| --- | --- | --- |
| RIV-AC-001 Setup | Verified | RivalGenerationTests:16 pairings×3 sizes, explicit count/species errors; actual setup defaults, Solo, mirrors, independent selections, single/empty definitions and generation failure; follow-up verifies literal alternate-content initial pairs, real submission, singleton/empty definitions, renamed/reordered content and later manual mirrors |
| RIV-AC-002 Generation | Verified | Solo equality to rules3 facts/IDs/RNG, top-third rank/one bounded draw, single extra override, repeated seeds/no home leak |
| RIV-AC-003 Fair economy | Verified | RivalPolicyTests exact four opening traces, ordinary costs/cargo/parent-daughter conservation, renamed/edited definitions and scoped shared forecasts |
| RIV-AC-004 Viability | Verified | Generated real projects/research/second colonies for all four species; Peldra bridge/Harl recovery/Sulan modes/Wenth base+extended tests; no-candidate waiting unit fixtures; labelled native/sealed/extended UI fixtures |
| RIV-AC-005 Information | Verified | RivalPlanningQueryTests/RivalPolicyTests unseen-state invariance, scrubbed seed/counters; UI labels/tooltips invariant under foreign overflowing reserves/private orders |
| RIV-AC-006 Transaction | Verified | RivalTransactionTests final/nonfinal/no-op/stale Ready, two humans/multiple AI, rejected/bound/issuer/count/exception plans, output/ID/final validation rollback preserve original references/bytes/contacts/readiness/RNG |
| RIV-AC-007 Travel/race | Verified | New-vessel wait, snapshot maturity/capacity, same-planet lower-ID winner/loser cargo, distinct planets and stale-target retarget; labelled simultaneous UI race |
| RIV-AC-008 Contacts | Verified | RivalContactTests current/unknown/stale identity retention and viewer-only events, save corruption rejection; contact/culture/dated ownership UI passes; follow-up verifies observed species/culture, captured orbit/date, several worlds in one system, ship-only null planet, viewer filtering and dated hidden-state invariance |
| RIV-AC-009 Compatibility | Verified | Exhaustive eleven-pair matrix; actual pre-change rules3/passive-Ai captures and four canonical future states; legacy1/2 traces; omission/corrupt fields/failed load+write, frozen Species UI loops |
| RIV-AC-010 Integrated | Verified | Twelve generated loops, actual settings/controls/dialogs/future equality; eight all-human-species/Solo continuations; labelled Sulan/Wenth/race/renamed policy0/9 fixtures |
| RIV-AC-011 Retained gates | Verified | Both four-scenario gates green, including final second gate after startup correction; ordinary metadata excludes all harnesses |
| RIV-AC-012 Owner design gate | Verified | ADR-010 explicit full approval before planning; not a gameplay assertion |

Genuine rules3 capture source was retained QA `qa-20261024-133043-627-species-peldra-small-two-colonies.json`, SHA25632eceaa800bb1301956a27b87fc2a784dac9d227a62295b695871ca2d371fd2e. Passive-Ai3 setup is explicitly authored before capture, then continued by the unchanged old engine. Provenance and canonical states are tracked in tests/Fixtures; no expected result was regenerated from new rules4 behavior.

## Failures, corrections and review limits

Task9 actual UI RED captured the missing selector before implementation. Task10 acceptance was added against existing implementation: early harness failures exposed a premature research stop, an unapproved every-size research deadline and nearest-neighbor routes that skipped committed transit. The harness now matches the approved contract and records actual zero research counts rather than claiming universal pacing. No policy/gameplay change was made to satisfy those failures.

A final coverage audit reproduced a real startup gap: invalid disk content removed Load Game although saved campaigns carry valid embedded content. [RED evidence](rival-execution/task10-invalid-content-red/results.json) precedes the minimal Main change: initialize Load before disk validation and offer it on the error screen. GREEN final gate passes the actual Load button/dialog and exact embedded rules3 campaign equality. No new product API/dependency was introduced.

Five setup window PNGs (800x600,1152x648,1280x720,1600x900,1920x1080), observed culture UI and representative generated/labelled game images were visually inspected. Scroll/focus and bounds assertions supplement screenshots; they do not prove physical keyboard/mouse delivery, comfortable small-window reading, accessibility or comprehension. Uncoached playtest stays Deferred (no recruitment). Balance, Linux/macOS/ARM equality, exports/packaging, hostile-input/performance budgets, clean-machine/dependency-security and release acceptance remain open. Broader REQ-017 stays In Progress.

## Preservation

[Exact audit](rival-execution/preservation-final.json) verifies primary main4d2efb5 unchanged, owner roster/faction hashes unchanged, peaceful backlog/status paragraph unchanged. Recovery stash4175cbfe407e822997fe41937946416335045913, old saves, all worktrees and failed/successful QA evidence are retained. No merge, push, release, cleanup, stash reapplication or owner-prose staging occurred.

## Final post-review correction gate

Final corrected source `ce6c29c1cf080893067ac96a43ca1e2366dd97e5`, after the one fresh review of candidate e36d836. [Correction gate](rival-review-final-20261024-run1):642/642 simulation, Rival1115 assertions+12 provenance rows(1127 total)/63 QA paths/20 PNGs; Species1194/74 QA/29 PNGs; Expansion275/20 QA/10 PNGs; Legacy466+27 geometry/11 QA/16 PNGs. Every process exit0, no failed rows or final engine diagnostics, normal rebuild0warnings/errors, all four opt-in types absent from ordinary metadata. [Machine summary](rival-execution/final-review-gate-summary.json).

The review's Important long-error recovery defect was reproduced with eighty actual loader failures at800x600 before correction, then verified GREEN with viewport bounds, real scrolling, Load dialog and exact original embedded rules3 equality. Its screenshot was visually inspected. Two Minor deviations remain Deferred: initial distinct rival fallback for alternative content, and encounter-report identity/orbit detail. Those exact portions of RIV-AC-001/008 are not claimed satisfied; other tested contracts remain evidenced. Parent REQ-017 remains In Progress. No second review or scope expansion.

## Subsequent owner-authorized local integration

The owner selected option1; main fast-forwarded to reviewed rival head70e91d9. [Fresh combined-main verification](RIVAL_INTEGRATION_2026-10-24.md) passed642 simulation,1115 Rival,1194 Species,275 Expansion and466 Legacy assertions+27 geometry; normal build0warnings/errors, all four harness types excluded, engine diagnostics0. Exact owner changes and all recovery/worktrees retained. Two Minor details and human/platform/release limits are unchanged. No push/release/cleanup. Earlier branch-stage integration-pending descriptions above remain historical.

## Follow-up closure — 2026-10-24

Both original Minor deviations are now corrected, with regression-first evidence and a fresh independent whole-branch review of69f7e1c..66e7548: no Critical/Important/Minor findings. Simulation648/648 and Rival1146 assertions+12 provenance, Species1194, Expansion275, Legacy466+27 geometry pass; normal build0 warnings/errors and four harness types excluded. RIV-AC-001/008 and M4-I are Verified for local automated scope; broader REQ-017 stays In Progress. [Correction report](RIVAL_POLISH_2026-10-24.md) supplies exact evidence and current integration state. Prior review/deferred descriptions above are historical, preserved without rewriting raw reviews.
