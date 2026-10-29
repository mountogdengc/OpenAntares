# OpenAntares validation report

**Documentation continuation,2026-10-29:** [Ship-design gameplay proposal/check](SHIP_DESIGN_GAMEPLAY_REVIEW_2026-10-29.md). Complete M10 draft and builder disposition; document links/IDs/arithmetic and exact preservation checked. No product change, build/gameplay test, runtime validation or genuine rules9 future capture in this turn. Product stays rules9/writer10/46 pairs; [M9 integration](COMBAT_INTEGRATION_2026-10-29.md) remains the latest executed gameplay evidence.

**Latest local integration,2026-10-29:** [Combat main integration](COMBAT_INTEGRATION_2026-10-29.md): owner-selected fast-forward; fresh1733 simulation tests,3271 Combat checks+93 separate provenance/12 generated campaigns,0diagnostics/build warnings/errors and actual9+2 opt-in/ordinary exclusions. Main rules9/writer10/46pairs. Owner files/raw evidence/QA/all8 worktrees preserved; no push/release/cleanup. Earlier checkpoints below are historical. Human/platform/balance/security/export/release limits remain open.

**Latest local integration,2026-10-24:** [Species main integration](SPECIES_INTEGRATION_2026-10-24.md): owner-selected fast-forward complete; fresh445 simulation,1181 Species,275 Expansion,466 Legacy assertions plus27 geometry observations; zero final diagnostics, normal rebuild0warnings/errors. Owner edits and recovery/QA evidence retained exactly. Earlier branch-only integration limits below are historical; human/platform/release acceptance remains open.


**Latest species implementation, 2026-10-23:** [Species acceptance](SPECIES_VALIDATION_2026-10-23.md) retains445 simulation,1181 species,275 expansion and466 legacy assertions plus27 geometry observations, on the isolated `codex/species-mechanics` branch. [Final review/handoff](SPECIES_FINAL_REVIEW_2026-10-23.md) records review disposition and preservation; integration and human/release acceptance remain separate.


**Latest integration, 2026-10-23:** [Main integration verification](EXPANSION_INTEGRATION_2026-10-23.md): index repaired, expansion integrated locally, existing edits preserved, fresh 281 simulation / 275 expansion / 466 legacy passes (+27 geometry observations). Earlier integration/index limits below describe the pre-integration state. Human/release limits remain open.

**Latest continuation:** [First Playable validation, 2026-10-22](FIRST_PLAYABLE_VALIDATION_2026-10-22.md): 463 graphical runtime assertions passed, 195 simulation tests passed again, retained picker/resize and legacy/corrupt UI-load evidence. This report below preserves the earlier adoption-run scope; its statements about checks not run then are historical. Human comprehension and full physical-input/readability acceptance remain pending.

**Run:** Adoption baseline, 2026-10-22 (America/Denver).
**Milestone:** M0 adoption; supports ongoing M1 First Playable validation.
**Repository:** `F:/Projects-PCGames/OpenAntares`, branch `main`, HEAD `6ab206d3ca4811cc9163ee6ea3d5c00ff4bb7b6b` plus existing uncommitted UI/config/prose changes. Documentation/report additions follow verification. This is a tested working-tree snapshot, not an independently certified clean commit or release.

## Environment and executed commands

- Windows NT 10.0.26300.0, x64; PowerShell.
- .NET SDK 10.0.113; target net8.0; installed Microsoft.NETCore.App 8.0.32 (also 10.0.13).
- Godot `4.7.2.stable.mono.official.ed1daf0bf`, matching root SDK 4.7.2. GL Compatibility configured; startup checks used the headless driver and therefore did not validate the configured graphical renderer.
- Existing restored dependencies/user caches. No restore, upgrade or dependency vulnerability audit was performed. The historical 2026-10-19 record's restore is historical evidence only.

### Solution build

```powershell
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
```

**Exit 0; 0 warnings, 0 errors.** Built simulation, Godot presentation and test assemblies in Debug. The initial restricted build exited 1 with `Build FAILED`, 0 warnings, 0 errors and no actionable diagnostic; the same command with normal local access succeeded. This environmental failure is not counted as a source-code failure. No code was altered to make the build pass.

### Automated suite

```powershell
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo --logger 'trx;LogFileName=adoption-2026-10-22.trx' --results-directory reports/test-results
```

**Exit 0; 195 passed, 0 failed, 0 skipped; total 195.** Executed after the successful fresh build with normal local process access. [Raw TRX](test-results/adoption-2026-10-22.trx) retains named test results and counters. Unit and simulation integration/file tests ran together; no separate graphical end-to-end suite ran.

### Godot startup

Actual executable: `F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe`.

```powershell
$godotConsole = 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
& $godotConsole --version
foreach ($size in @('small', 'medium', 'large')) {
    & $godotConsole --headless --path . --quit-after 60 --log-file "reports/adoption-2026-10-22-$size.log" -- "--new-game=${size}:42"
    if ($LASTEXITCODE -ne 0) { throw "$size startup failed: $LASTEXITCODE" }
}
```

| Input | Exit | Captured evidence |
| --- | ---: | --- |
| Small, seed 42 | 0 | [Startup log](adoption-2026-10-22-small.log) |
| Medium, seed 42 | 0 | [Startup log](adoption-2026-10-22-medium.log) |
| Large, seed 42 | 0 | [Startup log](adoption-2026-10-22-large.log) |

All three console runs completed with only the Godot banner and no reported errors/warnings. The first attempt used the GUI-subsystem executable, which returned to PowerShell without a reliable `$LASTEXITCODE`; that attempt is not an accepted result. The console runs above supply the actual exit evidence. No QA saves were created or existing saves overwritten by the startup checks.

## Traceability and results

| Requirements / criteria | Executed evidence | Result and scope |
| --- | --- | --- |
| REQ-001 / AC-001 | `GalaxyGeneratorTests`; three Godot startups | Verified generation contract locally; headless startup only for UI |
| REQ-002 / AC-002 | `ContentLoaderTests`, core-content generation | Verified loader and fixture cases |
| REQ-003 / AC-003 | `ArchitectureTests`, `GameStateValidatorTests`, source/project inspection | Verified compiled separation and covered state invariants |
| REQ-004 / AC-004 | `CommandProcessorTests` | Verified validation/ownership/no-op/atomic publication |
| REQ-005 / AC-005 | `EconomyTests` | Verified exact examples, forecasts, support/growth/rounding/breakdowns |
| REQ-006 / AC-006 | Economy and project command tests | Verified reserve conservation and production timing |
| REQ-007 / AC-007 | Economy and research command tests | Verified ownership aggregation, conservation, unlock timing |
| REQ-008 / AC-008 | Readiness, overflow/rollback, save-readiness tests | Verified covered turn publication/failure paths |
| REQ-009 / AC-009 | `SaveGameTests`, generated-game play/save/reload test | Verified serialization, version handling, continuation and file failures |
| REQ-010 / AC-010 | Fresh startup; historical screenshots/owner report | Complete graphical criterion unverified in this audit |
| REQ-011 / AC-011 | Source inspection, fresh build/startup; historical owner and FP-001/002 reports | Current complete interaction criterion unverified |
| REQ-012 / AC-012 | PCG32, repeated commands/turns/generation, visual RNG isolation, reload tests | Verified local deterministic contract |
| REQ-013 / AC-013 | Windows local build/tests only | Cross-platform/parity/export criterion unverified |
| REQ-014 / AC-014 | Existing playtest record reviewed | In Progress; uncoached observation/closure absent |
| REQ-015 through REQ-017 / AC-015 through AC-017 | Design/charter/roster inspection | Deferred; design gates, not delivered gameplay acceptance |

## Historical evidence and remaining checks

[The 2026-10-19/20 playtest record](../docs/playtests/first-playable-2026-10-19.md) reports owner passes for all five functional groups, startup screenshots across sizes, and successful temporary Godot runtime checks for FP-001/002 and the default window size. The source diff is consistent with those fixes. These are retained as historical reports, not fresh observations by this adoption audit; their temporary regression harnesses are absent. Earlier paragraphs saying interaction/save tests had not run predate the later owner follow-up and should be read in that chronology.

Pending: complete current graphical matrix/popup positioning and UI save/error workflows; uncoached player comprehension; explicit First Playable closure; legacy schema-1 UI load; Linux/macOS/ARM parity; exported/clean-machine installation; dependency audit/attribution; delivery configuration. Do not infer any of these from the 195 passing tests. No fresh graphical screenshot, input automation, performance benchmark or security scan was performed.

The sandbox also denied initial read access to the secondary worktree. Normal local read-only Git access confirmed it clean with no commits outside main. `dotnet --info` partially failed under restricted Windows service access; version/runtime facts were confirmed using `--list-sdks`/`--list-runtimes` and successful build/test execution.

## Integration and release assessment

One local documentation commit is ahead of cached origin/main. Existing source/config/prose changes, the approved validation plan/playtest artifacts and new adoption outputs remain uncommitted. The spiral feature tip/clean detached worktree is already an ancestor of main; no pending feature merge or open PR was found. No Git integration, dependency change, cleanup or release was performed.

**Local technical baseline: pass within the scope above. First Playable acceptance: still open. Broad feature implementation: not ready until closure and reviewed next-system rules. Release: not ready.**

Documentation checks and preservation evidence are recorded in [the documentation check report](DOCUMENTATION_CHECK.md). The [adoption audit](ADOPTION_AUDIT.md) gives prioritized gaps; [PROJECT_STATUS.md](../PROJECT_STATUS.md) gives the next action.

## Subsequent owner decisions and planning - 2026-10-22

The preceding tables describe the original adoption run. Subsequent graphical/runtime evidence is in the [continuation report](FIRST_PLAYABLE_VALIDATION_2026-10-22.md); it records 195 simulation tests and 463 runtime assertions passing, with explicit physical-input/readability limits. ADR-005 subsequently deferred the unavailable uncoached session and permitted expansion design without full First Playable closure. ADR-006 now records owner approval of the expansion specification. Thus the earlier closure-before-design gate is superseded; full First Playable acceptance remains open.

The [expansion implementation plan](../docs/superpowers/plans/2026-10-22-exploration-and-colonization.md) is written/self-reviewed, not executed. No gameplay implementation, new application build/test result, dependency audit or release acceptance is claimed. REQ-014 is Deferred; REQ-015 is In Progress with approved design and plan review/execution choice pending. Fresh Windows/PowerShell Git inventory/whitespace checks and inline Python documentation checks exited 0; their scope and [results](expansion-plan-documentation-check-2026-10-22.json) are detailed in DOCUMENTATION_CHECK.md. Main/worktrees and the original five tracked changes remain retained; no integration or cleanup occurred.


## Native expansion implementation - 2026-10-23

The owner approved the concrete plan and Native execution. The isolated feature branch now implements the approved expansion loop; fresh final results are 281 simulation tests, 275 expansion runtime assertions and 466 retained legacy runtime assertions, all passing. Normal rebuilds have zero warnings/errors and final engine logs contain no diagnostics. Commands, environment, exact traceability, failed attempts, QA paths, review/integration state and limitations are in [EXPANSION_VALIDATION_2026-10-22.md](EXPANSION_VALIDATION_2026-10-22.md). Original baseline claims above remain historical. ADR-005 human acceptance remains Deferred; no automatic integration or release occurred.

## Species design draft - 2026-10-23

The owner authorized a [species-mechanics draft](../docs/superpowers/specs/2026-10-23-species-mechanics-design.md), then permitted creative revisions as proposals (ADR-007). No gameplay/code/content changes or new application build/test results are claimed. [Design review evidence](SPECIES_DESIGN_REVIEW_2026-10-23.md) records the independent builder check and 23 passing independent arithmetic fixtures; these are not engine tests or final balance evidence. REQ-016 is In Progress for owner review, with implementation gated on approval and a separate plan. Prior integrated 281/275/466 passes remain historical results for unchanged source.

## Approved species design and implementation planning - 2026-10-23

The owner approved the complete species design with the classic Master of Orion II direction; [ADR-008](../docs/DECISIONS/ADR-008-approved-species-mechanics.md) records that subsequent approval. The [ten-task implementation plan](../docs/superpowers/plans/2026-10-23-species-mechanics.md) is written/self-reviewed and awaits plan review. M3-D is complete for design; M3-P is In Progress, M3-I Not Started, and REQ-016 remains In Progress. Requirement totals remain 11 Verified, 3 Implemented, Unverified, 1 In Progress and 2 Deferred (17 total).

[Planning review](SPECIES_PLAN_REVIEW_2026-10-23.md) records coverage, precise contracts and corrections. The fresh [documentation/preservation audit](species-plan-documentation-check-2026-10-23.json) checks links, stable IDs/counts, eight ADRs, ten unchecked task groups, unchanged gameplay source/content/tests and exact owner roster/research hashes. No application build/test result or species functionality is claimed from planning. Original baseline and prior draft reports remain dated history; human/release acceptance is unchanged. The prior Native preference persists; no new execution or integration is inferred.

## Rival execution — 2026-10-24

Full design/plan approved for retained Native execution. Functional Tasks1–9 are implemented on isolated codex/rival-expansion, with642 simulation tests passing and rules4/writer5 active there. [Rival validation](RIVAL_VALIDATION_2026-10-24.md) maps all twelve child criteria, genuine legacy/passive-Ai traces, exact species policy tests, twelve generated campaigns, real UI save/load/future equality and the final regression gate. Initial full gate passed Legacy466+27 geometry, Expansion275, Species1194 and Rival1108+12 provenance result rows. A regression-first invalid-disk-content Load fix is under the second four-scenario gate; final independent review is pending. Read current status/review before claiming integration readiness. Main/owner edits/backlog/recovery/worktrees are unchanged; no merge/push/release/cleanup. Human, balance, physical-input, platform and release limits remain open.

Final rival correction source ce6c29c: single independent review completed; Important long-diagnostics recovery issue RED→GREEN, two Minor deviations Deferred. Final642 simulation; Rival1115+12 provenance, Species1194, Expansion275, Legacy466+27 geometry; all four scenarios exit0, engine diagnostics0, normal rebuild0warnings/errors, all harnesses excluded. [Current evidence and narrow open criteria](RIVAL_VALIDATION_2026-10-24.md), [review/dispositions](RIVAL_FINAL_REVIEW_2026-10-24.md). RIV-AC-001/008 retain the precise deferred minor details; broad REQ-017 stays In Progress. Main integration remains a separate owner choice.

## Subsequent owner-authorized local integration

The owner selected option1; main fast-forwarded to reviewed rival head70e91d9. [Fresh combined-main verification](RIVAL_INTEGRATION_2026-10-24.md) passed642 simulation,1115 Rival,1194 Species,275 Expansion and466 Legacy assertions+27 geometry; normal build0warnings/errors, all four harness types excluded, engine diagnostics0. Exact owner changes and all recovery/worktrees retained. Two Minor details and human/platform/release limits are unchanged. No push/release/cleanup. Earlier branch-stage integration-pending descriptions above remain historical.

## Follow-up closure — 2026-10-24

Both original Minor deviations are now corrected, with regression-first evidence and a fresh independent whole-branch review of69f7e1c..66e7548: no Critical/Important/Minor findings. Simulation648/648 and Rival1146 assertions+12 provenance, Species1194, Expansion275, Legacy466+27 geometry pass; normal build0 warnings/errors and four harness types excluded. RIV-AC-001/008 and M4-I are Verified for local automated scope; broader REQ-017 stays In Progress. [Correction report](RIVAL_POLISH_2026-10-24.md) supplies exact evidence and current integration state. Prior review/deferred descriptions above are historical, preserved without rewriting raw reviews.

## Local integration complete

The retained owner-selected local workflow fast-forwarded main to d1aa588. Fresh main648 simulation and Rival1146 assertions+12 provenance pass; ordinary rebuild0 warnings/errors and all four harness types excluded. Exact owner changes remain uncommitted; all worktrees/recovery/saves retained. [Integration report](RIVAL_POLISH_INTEGRATION_2026-10-24.md) records exact source, branch/main test distinction and remaining limits. No new implementation/remote/release/cleanup is authorized.

## Colony-development design — 2026-10-24

The owner selected the development/delegation direction and delegated judgment beyond examples. The [full proposed design](../docs/superpowers/specs/2026-10-24-colony-development-design.md) is builder-checked with no important unanswered questions; [review record](COLONY_DEVELOPMENT_DESIGN_REVIEW_2026-10-24.md) distinguishes7/7 independent arithmetic fixtures from engine tests. No code/core-content/art/dependency changes, new application build/test result or implementation plan. M5-D In Progress for full owner review; M5-P/M5-I Not Started. Prior main648/Rival and branch four-scenario evidence remains historical. Old campaigns, primary owner changes, all seven worktrees and recovery are retained. Human/release acceptance is unchanged.

## Approved colony design and implementation planning — 2026-10-24

[ADR-012](../docs/DECISIONS/ADR-012-approved-colony-development-design.md) records full owner approval; M5-D/COL-AC-012 Verified for design. The [twelve-task implementation plan](../docs/superpowers/plans/2026-10-24-colony-development.md) is written/self-reviewed, M5-P In Progress for review, M5-I Not Started. [Planning review](COLONY_IMPLEMENTATION_PLAN_REVIEW_2026-10-24.md) records dependency/coverage/interfaces and independent cosmetic-hash vectors, not application tests. The genuine rules4 QA source was located/read and its existing hash verified; future continuation capture has not run. No code/core-content/art/dependency, new application restore/build/test or gameplay acceptance. Existing main/owner changes and seven worktrees/recovery are preserved. Parent totals and human/release limits unchanged.

## Colony development implementation — 2026-10-24

All twelve approved Native tasks are implemented on codex/colony-development-design. Rules5/writer6 new games add paid foundations/specialists, opt-in editable governors/queues/inheritance, scalable management/reports and original varied planet portraits. Genuine old campaigns remain unchanged. Fresh982 simulation tests and all five graphical gates pass (Colony582+4 provenance, Rival1146+12 provenance, Species1194, Expansion275, Legacy466+27 geometry); ordinary build0 warnings/errors and all five harnesses excluded. The final review and blocking correction are resolved; local main integration is complete, with fresh combined gates pending; read PROJECT_STATUS.md and reports/COLONY_VALIDATION_2026-10-24.md for exact evidence/limits rather than historical counts above. Human/balance/platform/release acceptance remains open. No future crisis/fleet scope or push/release/cleanup.

Historical M5 review disposition (2026-10-24): all twelve tasks complete; one fresh whole-branch review and one regression-first Important correction are resolved at9db9933. Suite984/984 and affected Colony593 assertions+4 provenance pass with0 diagnostics/normal build0 warnings/errors/five exclusions. Three Minors remain deferred: historical condition wording, template-deletion affected-colony information, and routine Attention distinction. Therefore COL-AC-002/007 and M5-I remain Implemented, Unverified for exact full acceptance; other functional COL criteria are Verified for local automated scope, design COL-AC-012 already Verified. Parent REQ-017/counts and human/release limits unchanged. See reports/COLONY_FINAL_REVIEW_2026-10-24.md and PROJECT_STATUS.md. Retained authorized local integration is complete; fresh combined five-scenario gates are pending; no new scope or cleanup.

## Colony local main integration — 2026-10-24

Fresh combined main93ffe8a passes984 simulation tests, Colony593+4 provenance, Rival1146+12 provenance, Species1194, Expansion275, Legacy466+27 geometry. All197 QA paths exist/134 PNGs retained,0 diagnostics; normal rebuild0 warnings/errors/all five harnesses excluded. One review Important correction is verified; three presentation Minors keep COL-AC-002/007 and M5-I Implemented, Unverified for exact full acceptance. Other COL functional criteria are Verified for bounded local automated scope. [Integration report](COLONY_INTEGRATION_2026-10-24.md) records exact recovery/status blob/owner preservation. Previous planning and pending-review passages above are historical checkpoints, not current instructions. Parent counts/human/release limits unchanged; no push/release/cleanup.

## Authorized colony presentation corrections — 2026-10-24

Historical pre-review correction checkpoint: source940fa73 passed988 simulation and Colony663+4 provenance; subsequent correction/integration follows below.


## Colony correction integration — 2026-10-24

Current correction closure (2026-10-24): all three authorized presentation fixes and the fresh review duplicate-vessel correction are verified and locally integrated. Functional source4a2e430 passes988 simulation tests; Colony665 assertions+4 provenance; Rival1146+12 provenance; Species1194; Expansion275; Legacy466+27 geometry. Fresh main Suite/Colony/normal rebuild/five harness exclusions pass separately. All twelve COL child criteria and M5-I are Verified for local automated/design scope. Parent REQ-017 and17-row totals remain12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred. See PROJECT_STATUS.md and reports/COLONY_POLISH_INTEGRATION_2026-10-24.md. Human/platform/delivery/release limits remain open. Next is bounded milestone selection/specification; no automatic implementation or cleanup. [Exact evidence](COLONY_POLISH_INTEGRATION_2026-10-24.md).


## Colony-crisis/plague specification draft — 2026-10-24

Owner-authorized documentation only: [M6 draft](../docs/superpowers/specs/2026-10-24-colony-crises-plague-design.md), [ADR-013](../docs/DECISIONS/ADR-013-colony-crisis-design-entry.md), [review record](COLONY_CRISIS_DESIGN_REVIEW_2026-10-24.md). Fresh read-only builder check returned “nothing important left”;17/17 independent arithmetic checks pass, no game code executed. Local link/ID/parent-count/owner-preservation checks are recorded separately. This is neither a new988-test suite run nor any crisis runtime/accepted balance. M6-D In Progress for full owner gameplay review; separate plan/implementation/CRI execution Not Started. Main rules5/writer6/M5 acceptance and exact four owner changes/seven worktrees remain unchanged. No main merge/push/release/cleanup.


## Colony-crisis design approval and implementation planning — 2026-10-25

The owner approved the complete plague-first design: [ADR-014](../docs/DECISIONS/ADR-014-approved-colony-crisis-plague-design.md), M6-D/CRI-AC-012 Verified for design. The [14-task plan](../docs/superpowers/plans/2026-10-25-colony-crises-plague.md) is written/self-reviewed for separate owner review, M6-P In Progress, implementation/remaining CRI execution Not Started. [Self-review](COLONY_CRISIS_IMPLEMENTATION_PLAN_REVIEW_2026-10-25.md) records exact dependencies/loss-history compatibility and five Review Focus tests. Independent PCG arithmetic3/3/reference source bytes match; the genuine rules5 source is located/hashed, but four future capture states are still a Task1 requirement, not executed here. No new C# unit/build/graphical result, product/core/default change, main integration/push/release/cleanup. Exact four primary owner changes/empty index/seven worktrees retained. Native method preserved; no redundant design/method approval.

## 2026-10-25 scout specification drafting

Documentation-only M7 drafting on codex/scout-survival-design from main42e1282. [Draft](../docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md) and [design review/preservation report](SCOUT_DESIGN_REVIEW_2026-10-25.md) distinguish proposed choices, written acceptance and fresh document checks from historical runtime evidence. No application build/test, gameplay execution, new survival validation, primary integration or release was run/claimed. Product remains rules6/writer7; M7-D In Progress, M7-P/M7-I Not Started. Existing technical and human/platform acceptance statuses remain unchanged.

M7 ameba design amendment2026-10-25: owner selected space-ameba danger; documentation now proposes stationary habitat, warning/survival/study/escape/loss and strict privacy/compatibility. Earlier peaceful-only design check is historical. Current amended checks and fresh read-only builder review are in SCOUT_DESIGN_REVIEW_2026-10-25.md and reports/scout-design/document-checks.json. No application build/test/runtime/new survival evidence or product/default/writer/main change; proposed arithmetic is not engine validation. Functional SCT acceptance remains Not Started.

## 2026-10-25 scout design approval and implementation planning

Owner approved the complete amended scout/space-ameba design in ADR-016; M7-D/SCT-AC-012 Verified for design only. The separate15-task docs/superpowers/plans/2026-10-25-scout-survival-and-ameba.md and reports/SCOUT_IMPLEMENTATION_PLAN_REVIEW_2026-10-25.md are ready for plan review, M7-P In Progress. Read-only inventory hashes32 actual retained generated UI saves; standalone PCG/document checks do not run game code or capture their future behavior. M7-I/functional criteria Not Started. No application build/test/runtime/default/writer change, implementation, primary merge/push/release/cleanup; primary four files/main/index and seven worktrees/branches are preserved. Historical M6 runtime evidence and human/platform/release limits remain unchanged.

<!-- M7 current acceptance -->
## Current M7 implementation and acceptance — 2026-10-25

Owner approved the full amended ADR-016 design and separate15-task plan; earlier drafting/plan-pending statements above are historical. M7-D/M7-P Verified; M7-I **Implemented, Unverified for exact full acceptance; review M1 deferred** on isolated codex/scout-survival. SCT-AC-012 is design-only Verified; functional SCT-AC-001..011/013..015 have the executed criterion mapping in [validation/handoff](SCOUT_VALIDATION_2026-10-25.md), with review status: Complete; no open Critical or Important findings. Branch current rules7/writer8/29 pairs; primary main42e1282 remains rules6/writer7. Actual suite 1398; all7 fresh native scenarios, zero diagnostics and ordinary7/helper2 exclusions are recorded, with genuine older futures and generated/labelled provenance. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. No integration/push/cleanup; owner chooses next disposition.

<!-- Scout M1 closure2026-10-26 -->
## Scout M1 follow-up closure — 2026-10-26

The remaining omitted-habitat setup explanation is corrected on codex/scout-polish, validated source907207f, with actual native RED→GREEN and five custom-map boundaries. [Follow-up evidence](SCOUT_POLISH_2026-10-26.md): Scout237, full Suite1398/1398,0 diagnostics, ordinary build0 warnings/errors and actual7-scenario/2-helper exclusions. SCT-AC-013/M7-I are now Verified for their local automated scope; earlier M1 deferral statements are historical. No known review finding remains open. Other six native scenarios retain their prior-source evidence; no second independent review or fresh other-six run is claimed. Primary main42e1282/four owner edits/seven worktrees/old evidence remain preserved; no integration/push/release/cleanup. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. Next is owner disposition of the completed follow-up branch.


## Scout local main integration — 2026-10-27

The owner's “merge that” authorized local integration of the completed scout/space-ameba work and final M1 follow-up. Main now contains validated source `a52dfc61c9a16ec10e0091b52783029afac73e38`: rules7/writer8/29 pairs. [Fresh merged-main evidence](SCOUT_INTEGRATION_2026-10-27.md) passes1398/1398 simulation tests and all seven native scenarios/4491 records (Scout 237; Legacy 493; Expansion 275; Species 1194; Rival 1158; Colony 670; Crisis 464), with0 diagnostics, normal builds0 warnings/errors and actual7-scenario/2-helper exclusions. SCT functional criteria/M7-I remain Verified for local automated scope; prior pending-integration statements are historical. Checkout preservation is verified425/425; original owner edits remain uncommitted and all branches/seven worktrees/recovery/QA remain retained. No new review, push, release or cleanup. REQ-017/17-row totals and human/platform/security/export/release limits remain unchanged. Next is bounded milestone selection/specification before implementation.


## M8 Fleet Command and named admirals — design approved, plan review pending

Owner “0approved” accepts the complete [design and starter defaults](../docs/superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md) in [ADR-018](../docs/DECISIONS/ADR-018-approved-fleet-command-and-admirals-design.md). M8-D/FLT-AC-012 Verified for full design only. The separate [17-task plan](../docs/superpowers/plans/2026-10-27-fleet-command-and-admirals.md) is drafted/self-reviewed; M8-P In Progress pending owner review; M8-I/functional FLT-AC-001..011 Not Started. Native inline/no per-task agents/one final fresh whole-branch implementation reviewer remains chosen. No product code/content/default/schema or primary-main change, new baseline capture or fleet gameplay run occurred. Main rules7/writer8/29 pairs, parent REQ-017/17-row totals (12 Verified,3 Implemented, Unverified,1 In Progress,1 Deferred), M7/M6/human/platform/security/export/release limits and every owner file/worktree/branch/QA/recovery remain unchanged. No main integration/push/release/cleanup is inferred.

### Historical scoped decisions before complete approval

The chronological notes below retain their original checkpoint status; ADR-018 and the current plan-review gate supersede their earlier pending-design language.


M8 scoped decision2026-10-27: the owner confirms doctrine editable from the start and research improving recovery equipment (ADR-017/FLT-006). No research gate for doctrine changes remains open. Exact equipment/bonus/XP/culture values and the rest of the full M8 design remain pending gameplay review; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Documentation only; current main/product and owner edits remain unchanged.


M8 scoped owner choice2026-10-27: Navigator, Surveyor and Organizer; promotions at10/30 XP. The old Recovery specialist, ameba-only Surveyor proposal and20/60 thresholds are superseded in the active specification; historical review/check artifacts remain intact. Exact bonus magnitudes/event XP/proposed XP cap, Organizer accounting, recovery values/cultural defaults and full M8 acceptance remain pending review. Doctrine remains editable from the start; research improves recovery equipment. M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. No code/main/owner-file change.


M8 scoped owner decision2026-10-27:50% officer recovery,70% with recovery research and a two-turn successful-recovery delay are confirmed. Current review topic is cultural defaults; do not treat provisional Harl Last watch as approved. Other unreviewed rules/values and full M8 acceptance remain pending; M8-D/FLT-AC-012 In Progress, plan/implementation Not Started. Main and four owner files remain unchanged.


M8 cultural-default decision2026-10-27: all four cultures begin recovery-focused; Wenth present Re-establish command link. Last watch is manual-only; the original Harl default is superseded. Do not reopen settled doctrine/research, specialties/10-30 XP,50-70% recovery/two-turn delay or cultural defaults. Use the specification's remaining-review checklist for unconfirmed details. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees/evidence remain preserved.


M8 bonus/XP decision2026-10-27: Navigator5/10%, Surveyor1/2 research per first survey, Organizer5/10% scoped request-ship cost reduction and2/1/8 XP awards are owner-confirmed; do not re-ask. Current review is fleet/construction operations, with other unconfirmed details/full design approval still pending. Preserve docs/design-notes/2026-10-27-combat-retreat-morale-and-damaged-ships.md as future owner direction (degrading capabilities, retreat/morale, limping home/detachment/hit-and-run, automation with possible manual control), not M8 damage/combat scope. Plan/implementation remain Not Started; main/four owner files/worktrees/evidence unchanged.


M8 fleet/construction review2026-10-27: owner retains the reviewed proposal and Normal/Urgent request priority. Normal preserves explicit queues; Urgent previews moving before unstarted work, never silently replacing the active project. Preserve finite authority, priority-aware estimates, tokens/reserve/manual control and current combat-note future-only scope. Next review: commissioning/Wenth details/recovery technology and other unconfirmed details, then full M8 approval before separate planning. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner files/worktrees unchanged.


M8 availability decision2026-10-27: starting Navigator with Surveyor/Organizer candidates,2-turn commissioning,4-active-admiral limit and co-located ship-based transfers confirmed; fleet controls remain usable without an officer and Wenth availability stays colony-body based. Do not re-ask those choices. The64-lifetime-commission bound remains a proposal. Next review covers Wenth link/equipment recovery, technology and remaining draft details before full M8 approval/separate plan. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started. Main/four owner edits/worktrees unchanged.


M8 final scoped choices2026-10-27: Wenth recovery2/1 turns (basic/researched), manual Last watch4/3, and Command Recovery24 displayed research after Industrial Coordination confirmed. Read reports/FLEET_COMMAND_GAMEPLAY_REVIEW_2026-10-27.md and the complete specification for remaining starter defaults and full-design approval; do not re-ask settled gameplay numbers. M8-D/FLT-AC-012 In Progress; plan/implementation Not Started until their gates. Preserve main/four owner files, seven worktrees/branches and evidence; future combat direction remains a note.


Current M8 implementation authorization2026-10-27: owner “approved” accepts the separate17-task plan at7d175ad. M8-P Verified; M8-I In Progress on isolated codex/fleet-command. Native inline continuously executes all17 tasks, no per-task agents and one final fresh whole-branch implementation reviewer. Follow .superpowers/sdd/2026-10-27-fleet-command-and-admirals/progress.md; completed tasks must not repeat. Task1 must freeze genuine rules7 futures before any product/content change. Fresh unchanged baseline Suite1398/1398 passes. Full gameplay approval remains ADR-018; earlier planning-only restrictions are historical. Main c554797/four owner files/seven worktrees/all old QA/recovery remain preserved. No main integration/push/release/cleanup authority.


Current M8 combined-validation checkpoint2026-10-28: tasks1..16 formally complete on isolated codex/fleet-command; Task17 combined Suite1529/1529, build0 warnings/errors and all8 native scenarios pass, with actual opt-in negative control/ordinary8+2 exclusions. Current branch rules8/writer9/37 pairs; primary main c554797 remains7/8/29. Read reports/FLEET_COMMAND_VALIDATION_2026-10-27.md and reports/fleet-execution/final-summary.json. Actual generated3col/4scout/all-species Main SaveLoad/futures and separately labelled rare/scale/paging evidence retained. M8-I In Progress and FLT functional criteria Implemented, Unverified pending the ONE fresh whole-branch review, not yet dispatched at this checkpoint. Native retained/no per-task or second reviewers; no main integration/push/release/cleanup authority. All4 owner files/main index/all7worktrees/branches/raw QA retained;2431 actual-checkout hashes verified. Earlier planning/implementation-start notes are historical; trust this plan's ledger and do not repeat completed work. Future combat note remains outside M8; parent counts/human/release limits unchanged.


M8 final implementation/review checkpoint2026-10-28: approved17tasks complete on isolated codex/fleet-command, validated source36e6ec4. ONE fresh fullbranch review c554797..2464d1f and ONE regression-first5-finding correction pass done; no openCritical/Important/no second review. Corrected Suite1545/1545, focused compatibility/rollback73/73, Fleet1482+33/Scout237/Crisis464/Rival1158 native fresh, unchanged earlier Legacy/Expansion/Species/Colony evidence retained,0diagnostics/build0/actual8+2 exclusions. Branch8/writer9/37pairs; primary mainc554797 stays7/8/29. M8-I and FLT-AC-001/005/011 remain Implemented, Unverified for exact acceptance: rolefilter missing, lost-Return Attention missing, separate idle-candidate list unpaged. Other8functionalcriteria Verified localautomated,012design Verified, M8-D/P Verified. Reports/FLEET_COMMAND_VALIDATION_2026-10-27.md, fleet-execution/final-summary.json/final-review.md/progress ledger are authoritative. All4ownerfiles/mainindex/7worktrees/branches/stashes/oldQA/futures preserved,2431index-checkout hashes and closing recheck pass. Futurecombat remains note. Parent17-row counts/human/platform/security/export/release limits unchanged. No merge/push/release/cleanup authority. Next owner disposition of isolatedbranch/threeMinors; do not repeat tasks/reopen design/method/review. Earlier pending checkpoints are historical.

M8 remaining-gap closure2026-10-28: owner explicitly requested fixing all3 remaining Minors before merge. Source `035877a40d45b39b1e764980e386023f706dd1dd` closes M1 lost-Return Attention (including persisted reason/report/badge with routineOff), M3 owned-role filtering, M4 bounded32-row assembly idle paging with explicit cross-page selections/stale-removal disclosure. FullSuite1547/1547, fresh Fleet 1666 result records + 39 separate provenance entries, Scout 237 result records + 9 separate provenance entries, Crisis 464 result records (including inline provenance); normalbuild0warnings/errors/actual8+2exclusions. M8-I/FLT-AC-001/005/011 now Verified for localautomated scope; all12FLT criteria Verified within automated/design limits. Read reports/FLEET_COMMAND_FOLLOWUP_2026-10-28.md and fleet-execution/minor-summary.json; earlier3-Minor deferrals/final-summary are historical. Original17tasks stay complete; no second reviewer. Mainc554797/four ownerfiles/index/all7worktrees/unrelatedrefs/6053 oldreports/346 recordedQA saves preserved. Branch remains unmerged; no push/release/cleanup or new subsystem. Parentcounts/human/platform/security/export/balance limits unchanged. Next: local integration when requested.

M8 local integration2026-10-28: owner “merge” explicitly authorizes local main integration of codex/fleet-command `6bcdc2dac0a09ecb5de9289af110ef20a19f8700` with committed owner documentation `a58cc671bdfbba9c59e0340613f9f04410c65197`. Product source035877a; all3 remaining findings are closed, M8-I/all12FLT criteria Verified within local automated/design scope. Fresh combined main Suite1547/1547, Fleet1666checks+39separateprovenance,0engine diagnostics, ordinary rebuild0warnings/errors and actual8+2exclusions pass. Only PROJECT_STATUS conflicted: latest fleet status and exact peaceful-mode paragraph retained; other3ownerfiles exact. Main now rules8/writer9/37pairs; old rules1..7 retain embedded behavior. Read reports/FLEET_INTEGRATION_2026-10-28.md and fleet-integration-20261028/summary.json. Prior branch-only/uncommitted-owner checkpoint wording is historical; those4files were committed separately ata58cc67 before merge. All7worktrees/unrelatedrefs/fleetbranch/recovery/oldreports/QA/futures retained, recoverybranch codex/fleet-premerge-20261028. Single review/Native preference retained; no second review or repeated completed tasks. Parent17-row counts/human/platform/security/export/balance limits unchanged. No push/release/cleanup or new subsystem. Next: choose the next bounded milestone before design/implementation.


## M9 combat design review 2026-10-28

[Design review and documentation verification](FLEET_COMBAT_DESIGN_REVIEW_2026-10-28.md) records this documentation-only continuation and fresh builder-check disposition. M9-D/CBT-AC-012 In Progress; functional criteria/plan/implementation Not Started. No new application build, simulation/gameplay test, graphical acceptance, balanced-combat claim or old-engine future capture is made. Existing M8 integration evidence remains historical product validation.


## M9 full design approval and implementation planning 2026-10-28

[ADR-020](../docs/DECISIONS/ADR-020-approved-fleet-combat-design.md) records full gameplay approval; [16-task plan self-review](FLEET_COMBAT_IMPLEMENTATION_PLAN_REVIEW_2026-10-28.md) records this documentation-only continuation. M9-D/CBT-AC-012 Verified for design; M9-P In Progress pending plan review; M9-I/functional CBT criteria Not Started. No fresh application build/test/runtime/future-capture evidence is claimed; parent counts/product8/9/37 and deferred limits unchanged.
