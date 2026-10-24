# OpenAntares validation report

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
