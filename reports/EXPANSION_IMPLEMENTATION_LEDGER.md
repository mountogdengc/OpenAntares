# SDD ledger — plan: docs/superpowers/plans/2026-10-22-exploration-and-colonization.md

Owner approved plan and Native execution, 2026-10-22. Implement inline; one independent whole-branch review at end. Feature branch codex/exploration-colonization; parent 6ab206d. Primary checkout preserved; baseline manifest in reports.

Pre-flight: Tasks 1→2: versioned state/content consumed by explicit save reader/writer; consistent, keep schema/rules separate.
Pre-flight: Tasks 1→3: cloned vessel/survey fields consumed by projection/generation; consistent; defaults stay legacy until Task 7.
Pre-flight: Tasks 3→4/6: observations and allowed views consumed by travel/founding; unknown planet validation precedes hidden truth.
Pre-flight: Tasks 4→5/6: events and snapshot vessel iteration shared by production/founding; completion units excluded from initial IDs.
Pre-flight: Tasks 1/5→7: typed targets and embark forecast consumed by Godot controls; successful production clears target, stalled target retained.
Pre-flight: Tasks 3/4/6→7/8: permitted DTOs and private report events consumed by UI/runtime harness; do not pass raw world to views.
Pre-flight: Tasks 2/7→8: both genuine legacy formats render rules-1 mode; no synthetic upgrade.
Pre-flight: Tasks 1–8→9: regression/evidence identifiers and requirement statuses; design approval does not verify gameplay.
Ruling: Native worktree path is outside original sandbox writable roots — use reviewed escalated file/build calls in that worktree — cost if wrong: isolated files only, original remains intact.

Setup restore: dotnet restore OpenAntares.sln --nologo → exit 0, normal audit enabled, no vulnerability warnings. No clean-machine claim.

Setup baseline: build exit 0, 0 warnings/errors; full simulation tests exit 0, 195/195 passed, 0 skipped. Genuine schema-2 fixture captured/hash verified.

Task 1: RED missing expansion contracts (task1-red.log); GREEN focused 100/100 and full 221/221. Fixed a test-only dictionary overload after new API compiled.
Task 1: Ruling: normalize the original core-content equality assertion to legacy fields — core now includes approved expansion data, old economic values still compared unchanged — cost if wrong: new expansion tests independently assert embedded definitions.
Task 1: complete (commits e3cb04e..c9df73d, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo → Passed!  - Failed:     0, Passed:   221, Skipped:     0, Total:   221, Duration: 244 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task 2: RED 8 expected schema/extension/round-trip failures after fixture path correction; GREEN save suite 51/51. Existing version rejection moved from schema 3 to 4; legacy fields/assertions retained.
Task 2: complete (commits c9df73d..d194fea, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo → Passed!  - Failed:     0, Passed:   239, Skipped:     0, Total:   239, Duration: 268 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Ruling: exclusive index lock remains held outside identifiable current process; continue tested source tasks without Git mutations until it releases, preserve staged Task 2. No reset or force cleanup. Task completion markers require checkpoint recovery.

Task 3: RED missing Knowledge APIs; GREEN focused 27/27, full suite recorded in task3-full.log. Source implementation done; checkpoint deferred with index lock.
Task 3: Ruling: compute owner forecast from filtered cloned colonies/empires — foreign overflow must not affect visible output — cost if wrong: forecast regression tests catch owner accounting differences.

Task 4: RED missing Movement APIs; GREEN 43 focused tests and full suite recorded in task4-full.log. Snapshot-only movement, readiness input exception and observation publication implemented; checkpoint pending lock.

Task 5: RED missing general production command/embark forecast; GREEN affected tests 36/36 and full suite in task5-full.log. Transfer, blocked banking, birth/capacity snapshots, kind collisions and ID rollback verified. Checkpoint pending Git lock.

Ruling: preserve inaccessible default-index lock and use a plan-local alternate index copied from the reviewed staged Task 2 index — no source/index changes discarded — cost if wrong: final worktree index synchronization may remain pending.

Task 3: complete (commit 78dab7a; focused 27/27, pre-next-task full 246/246).
Ruling: Tasks 4–6 checkpoint together after lock recovery — coupled turn-phase code was already tested in sequence, preserve coherent green commit — cost if wrong: larger review unit.
Task 4: complete (commit 6ed43b6; focused 43/43, pre-next-task full 257/257).
Task 5: complete (commit 6ed43b6; focused 36/36, pre-next-task full 263/263).
Task 6: complete (commit 6ed43b6; focused 63/63; final full suite task6-full.log).

Task 7: RED actual graphical New Game defaults to rules 1 (exit 1, retained ui-red evidence), plus missing permitted report projection test.
Task 7: Ruling: add immutable turn/known-tech/readiness/owned-colony-name metadata to DTOs — control signatures require command context, no raw state fallback — cost if wrong: view/command tests protect behavior.
Task 7: Ruling: add opt-in expansion test scene/targets early for the required graphical RED step — test-only setup from Task 8, no product scene — cost: shared harness checkpoint spans Tasks 7/8.

Task 7 runtime RED: all three generated loops completed, then synthetic same-system empty world selection failed; Refresh fallback reselected an occupied colony. Fixed selection fallback only when no known planet is selected. Also corrected two new UI source files accidentally written in Windows-1252 (runtime UTF-8 loader diagnostics); no source-encoding failures may remain. Evidence run1 retained.

Task 7: observation integrity RED 3/3 malformed-current-survey saves accepted; strengthened the existing rules-2 invariant to require complete current observations wherever own presence grants them. Uses the same observation producer as projection/refresh, no new gameplay.

Task 8: final run3 caught an outdated synthetic fixture after stricter current-observation validation: the added foreign empire had no own current survey. Refresh that newly authored fixture through ObservationUpdater before validation; player state/rules are unchanged. Preserve run3 failed evidence.

Task 7: complete (commit 5826235; build 0 warnings/errors; 278/278 simulation tests; final expansion runtime 238/238; legacy runtime 466/466 + 27 geometry observations; no engine diagnostics). Normalized duplicate CR introduced while correcting two new files UTF-8; no behavior changed.

Task 8: complete (commit afa2ee1; final expansion run4 exit 0, 238 assertions/9 PNGs/14 QA saves; legacy run1 exit 0, 466 assertions + 27 observations/16 PNGs/11 QA saves; both 0 engine diagnostics and normal rebuild 0 warnings/errors). Failed runs 1/3 and earlier red/default evidence retained; run2 passed before added save-invariant check.

Task 9: complete (fresh full normal build 0 warnings/errors; 278/278 tests; final graphical evidence 238/238 expansion and 466/466 legacy with zero diagnostics; documentation audit 41 Markdown/236 local links/17 requirements, zero errors; original three UI/config source hashes match primary snapshot; ordinary game excludes validation class names). Final independent review follows; no integration performed.
Ruling: retain the execution workspace/evidence rather than infer cleanup — owner instructions preserve evidence — cost if wrong: ignored local scratch remains.

Final review: independent read-only review of 6ab206d..db53145 completed; no Critical findings.
Final: Ruling: route replacement is Important; nondefault-content instructions and impossible historical colony references are re-graded Important — valid embedded content must describe the actual population tradeoff, and malformed saves must not publish fictitious colony identities — cost if wrong: three bounded regression fixes instead of deferring the latter two.
Final: Ruling: physical input, comprehension, small-window comfort and balance remain outside automated acceptance — retained runtime controls cannot establish human delivery/understanding; owner deferral stands — cost if wrong: manual usability issues remain undiscovered.
Final: Ruling: cross-platform parity, exports, installation and release readiness remain unverified — Windows-only execution supplies no target-platform evidence — cost if wrong: delivery failures remain possible.
Final: Ruling: default-index repair and integration remain separate — source/commit comparison is verified but the lock is retained and primary owner edits must survive — cost if wrong: local Git integration needs additional repair.
Final: Ruling: future conquest/destruction historical-observation policy remains deferred — the current fix validates identity/planet existence without requiring current ownership equality — cost if wrong: later strategic systems will need an explicit compatibility policy.

Final RED: historical observation missing/wrong-planet references accepted (2 failures, 1 stale-owner preservation pass). Graphical route replacement fails after actual destination selection following reload; retained separate red run.

Final RED→GREEN progress: saved-observation focused tests 3/3 pass; route replacement controls/preview/reload now pass all three generated sizes. Nondefault-content valid fixture reaches expected RED on blocked instructions. Corrected blocked requirements and founding population to selected embedded role/owned cargo. Whole-suite and graphical GREEN checks follow.

Final: fixed queued-route replacement — GeneratedLoop destination preview/Set course/reload checks RED→GREEN across Small/Medium/Large; suite 281/281, expansion 275/275, legacy 466/466 + 27 observations.
Final: fixed nondefault-content requirements/founding instructions — NondefaultContentInstructions RED→GREEN; suite 281/281, expansion 275/275, legacy 466/466 + 27 observations.
Final: fixed impossible historical colony references — HistoricalObservationRejectsImpossibleColonyReference RED→GREEN (2 cases), stale ownership preservation GREEN; suite 281/281.
Final: no deferred Minor findings after effect-based re-grading. Final normal rebuilds 0 warnings/errors; no final engine diagnostics.

Final correction checkpoint: 2d989d632ec2a176efc36aea9c7a5cd3b454a627.
Final: Ruling: retain the local feature branch/worktree rather than offer automatic integration or cleanup — owner scope preserves the dirty primary and excludes those operations — cost if wrong: integration remains a separate owner action.
Final handoff: all nine tasks, independent review and one regression-first correction pass complete; 281 simulation / 275 expansion / 466 legacy + 27 observations pass. Human acceptance and integration/release remain open.

Final documentation verification: 42 Markdown files / 252 local links / 17 requirements, zero errors; counts 11 Verified, 3 Implemented, Unverified, 3 Deferred. Protected primary hashes unchanged; ordinary assembly excludes test classes. Local branch retained; default index reconciliation remains pending.
