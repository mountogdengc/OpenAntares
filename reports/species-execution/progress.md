# SDD ledger — plan: docs/superpowers/plans/2026-10-23-species-mechanics.md

Owner approved plan 2026-10-23 (yes), Native execution retained. Branch codex/species-mechanics starts e32ada6; main and owner prose remain separate.
Ruling: Native app worktree creation returned Not a git repository for chat cwd — manual git worktree in the writable chat workspace is used because actual repo is external F: — cost if wrong: app attachment unavailable, Git inventory remains authoritative.
Ruling: Initial sdd-workspace helper ran in primary checkout before switching cwd, creating an empty ignored plan marker there — use this isolated checkout's ledger only; preserve that marker — cost if wrong: harmless unused scratch, no task incorrectly marked complete.

Pre-flight: 1->4 frozen old trace/canonical helper: compatible; schema-only exclusion required.
Pre-flight: 2->3/5/6/7 species/state/capacity contracts: compatible; old overload stays legacy, new overload carries owner/mode.
Pre-flight: 2->4 complete schema3 test snapshots are not supported: use deterministic test-only snapshot until schema4.
Pre-flight: 3->5/6/7 shared profile/breakdowns: compatible; aggregate rounding versus one-worker preview distinct.
Pre-flight: 4->5/8 version gates: compatible; writer4 before current rules3 activation.
Pre-flight: 5->7/8 observed potential/role views: compatible; reject unknown targets before physical lookup.
Pre-flight: 6->7/8 founding cargo/blocked requirements: compatible; selection/loading does not require eligibility.
Pre-flight: 8->9 old graphical scenarios: frozen rules2, new default tested only Species.
Pre-flight: 1-9->10 evidence/status: compatible; human acceptance stays Deferred.

Task 1: pending; Task 2: pending; Task 3: pending; Task 4: pending; Task 5: pending; Task 6: pending; Task 7: pending; Task 8: pending; Task 9: pending; Task 10: pending.

Baseline: restore/build exit0, 0warnings/errors; 281/281 tests. Task1 RED missing genuine fixture, GREEN1/1 and suite282/282. Exact original save SHA verified; four prechange continuations captured.
Task 1: complete (commits e32ada6..40915c7, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo → Passed!  - Failed:     0, Passed:   282, Skipped:     0, Total:   282, Duration: 313 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task2 RED: species sections/fields unrecognized; content validation18/22 failing, then actual zero-weight case rechecked after fixture enumeration fix; state17/18 failing. GREEN40/40 focused. Test setup weight-overflow initially changed Barren8/0/0 to intMax/0/0 (valid); corrected to intMax/1/0 to exercise overflow. No product defect inferred from that setup error.
Task2 Ruling: extend ContentLoader through private partial Builder in Content/SpeciesContentReader.cs, plus test-only Fixtures/species-test-content.json — loader already large, focused parser avoids unrelated refactor and later core activation — cost if wrong: one extra focused file, all existing loading tests retained.
Task2 Ruling: growth-bound proof includes99 possible prior progress and home requires the defined Shelf Sea natural environment — prevents accepted content failing on growth/startup — cost if wrong: rejects unreachable extreme growth settings and species unable to use the guaranteed native home.
Task 2: complete (commits 40915c7..c2d9bd7, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo → Passed!  - Failed:     0, Passed:   322, Skipped:     0, Total:   322, Duration: 329 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task2 checkpoint delay resolved without lock removal; commit c2d9bd7 exists. Initial postcommit helper overlapped Task3 test creation and picked up its intended RED tests (11 failures); temporarily held the untracked Task3 test outside test compilation and reran Task2 exact suite322/322. Future dependent operations wait for completion before source/test changes.

Task3: confirmed RED9/13 expected arithmetic/post-embark differences after fixing structural record equality in the test; GREEN13/13. Base+species delta preserves line sums; both division remainders retained. Full suite335/335. No species-ID branches/RNG.
Task 3: complete (commits c2d9bd7..d0b1cc0, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo → Passed!  - Failed:     0, Passed:   335, Skipped:     0, Total:   335, Duration: 283 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task4 Ruling: plan calls atomic API WriteAtomic but repository exposes SaveGame.WriteFile; preserve that existing public API, and put species content writing in a focused partial Content/SpeciesContentWriter.cs — avoids unnecessary rename/large writer growth — cost if wrong: two internal helper files rather than one. Existing writer-version assertions updated3->4 and unsupported-version fixture4->5, retaining original legacy arithmetic.

Task4: confirmed RED12/50 and genuine old trace green; GREEN50/50 focused, suite385/385. Seven pairs explicitly accepted, schema3 retained, species/weights/role fields roundtrip, old continuation unchanged after schema-only resave. Atomic file failure throws UnauthorizedAccessException on this Windows host (also accept IOException); prior file and session retained. Bash helper requires escalated Windows named-object access; default read invocation failed before task start and was rerun successfully escalated.

Ruling: Git commit had already published e4538f8 but waited on its own auto-maintenance child77552(parent27988) — stop only that optional child and use per-command maintenance.auto=false/gc.auto=0 for subsequent commits — cost if wrong: automatic local housekeeping deferred; no files, locks or history removed.
Task 4: complete (commits d0b1cc0..e4538f8, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo → Passed!  - Failed:     0, Passed:   385, Skipped:     0, Total:   385, Duration: 380 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Task5: RED8/11 absent environments/identity/previews; GREEN11/11, suite396/396, normal build0warnings/errors. Twelve species-size cases plus four reference sampling seeds preserve base geometry/types and one environment draw per planet including home. Unseen environment/foreign species-overflow changes leave own view unchanged. Added owned HabitatMode to permitted colony DTO so later UI can name actual native/sealed mode without raw-state reads (ordinary interface completion). CurrentVersion stays2.
Git delay follow-up: optional maintenance stop eventually released checkpoint; trace showed later status scan completed0.23s. All pending diagnostic sessions drained. No relocation, lock deletion or global Git setting change was needed.
Task 5: complete (commits e4538f8..94ac4db, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo → Passed!  - Failed:     0, Passed:   396, Skipped:     0, Total:   396, Duration: 402 ms - OpenAntares.Simulation.Tests.dll (net8.0))

Ruling: Windows Git-Bash skill helpers intermittently stall even reading the plan marker — use equivalent native Python brief/ledger operations when needed, retaining exact BASE, RED/GREEN logs and full-suite gate — cost if wrong: helper-format bookkeeping differs, product tests and completion contract do not. Task6 BASE94ac4db; actual brief read from the plan.

Task6: RED18/18 role gate/selection failures; GREEN18/18 and suite414/414. All species/role combinations, numeric costs/cargo/minima, Wenth6/8 maturity, all unmet conditions, banking/cancel, birth boundary, Peldra next shortage, Sulan6/9 investment and ID-overflow rollback pass.
Task6 Ruling: one colony has one production target, so a local capacity building cannot also complete alongside its seed in the same resolution — retain snapshot-only shared evaluation and test completed extension raising maturity to8 plus births not unlocking the current resolution; do not invent a second completion pipeline to realize that plan sentence — cost if wrong: future multi-project systems must add their own simultaneous-effect test before introduction.

Task 6: complete (commits 94ac4db..8c1a546, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo -> Passed!  - Failed:     0, Passed:   414, Skipped:     0, Total:   414, Duration: 315 ms - OpenAntares.Simulation.Tests.dll (net8.0))
Task7 BASE8c1a5462cd273ab4e7616dc0594d05ba87bf6226.

Task7 test correction: extended parent is5 immediately after embarkation, then regrows to6 during the build/travel/founding trace. Assert both actual phase points; do not freeze ordinary growth to preserve5 until arrival. Added simultaneous two-world versions of both approved Sulan maps and rejected-habitat route/readiness preservation.

Task7: RED18/19 absent previews/habitat/resolution; GREEN22/22 after parent timing correction and extra choice/preservation tests; suite436/436, normal build0warnings/errors. Exact species build/travel/found loops, Sulan modes and both maps, Harl1-unit recovery, extended Wenth daughtercap6, edited cargo, unknown-target parity, conflicts and overflow all pass. No authoritative RNG/ID changes in preview.
Task7 Ruling: expose existing CalculateColony internally for an unpublished one-colony preview frame rather than calculate a fictitious empire reserve — shares exact formulas and avoids foreign/own research reserve overflow contaminating a habitat preview — cost if wrong: one internal visibility increase, no public state or alternate rules.
Task7 version audit: remaining simulation ExpansionVersion mentions only Rules constants/support predicate and CurrentVersion2 (deliberately untilTask8). Three UI equality gates remain in ColonyList, ColonyPanel and VesselPanel, explicitly owned byTask8. No movement/save/command/observation branch is still rules2-only.

Task 7: complete (commits 8c1a546..0470a11, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo -> Passed!  - Failed:     0, Passed:   436, Skipped:     0, Total:   436, Duration: 299 ms - OpenAntares.Simulation.Tests.dll (net8.0))
Task8 BASE0470a1109b267f809a08cf8c4f0635e58926481e.

Task8 RED: four actual core profiles absent; graphical New Game exposes four species choices failed as intended; runner finally normal rebuild0warnings/errors. Core activation initial suite had4 ImmutableArray reference-comparison test errors (same sequences) and historical core-fixture equality needing explicit removal of new extension fields. Compare sequences structurally and preserve old base-data assertion with species/environment fields projected away; new core behavior is independently checked by species tests.

Task8 Ruling: carry PopulationUnitLabel on ColonyEconomy into reports as well as owned views — Wenth lobes should be named consistently without a raw-state lookup or a second rules calculation — cost if wrong: one immutable presentation metadata property; save/state formulas unchanged. New Game summaries call the existing generator/calculator on an unpublished preview, preserving authoritative RNG and avoiding UI economy math. Details have a bounded scroll area so Start and Load remain reachable.

Task8: core RED4 missing profiles; UI RED missing species chooser, finally ordinary rebuild green. Core/UI GREEN: suite440/440, two16-assertion graphical passes including defaultHarl3, missingHarl fallback, emptySpecies load access and renamed/cargo-edited harbor. Final green2 zero engine diagnostics; normal rebuild0warnings/errors; normal assembly metadata excludes all3 validation classes. Default nowrules3, writer4; old scenarios deliberately use genuine frozen rules1/2 definitions. PNGs inspected; long world lists use existing side-panel scrolling. No art/dependencies/theme changes.

Task 8: complete (commits 0470a11..7449d34, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo -> Passed!  - Failed:     0, Passed:   440, Skipped:     0, Total:   440, Duration: 368 ms - OpenAntares.Simulation.Tests.dll (net8.0); Species GUI green2 16/16, ordinary rebuild0warnings/errors)
Task9 BASE7449d34e3132d9662ef8f18bb1c507c1c5541277.

Task9 observed regression: at1152x648 after800x600, alreadyFocused=true, picker y1141..1175 outside scroll y86..1064, scroll0; releasing/reacquiring focus moves picker y1030..1064 and scroll111. Root cause: FollowFocus is focus-change driven, not resize-driven. GameScreen now waits one layout frame on side-scroll resize then ensures its current focused descendant is visible. No domain state changes. RED initial and explicit-refocus diagnostic retained in separate runs.

Task9 arithmetic regression: valid one-species content with divisor1, bounded support near long.MaxValue, base growth1000 and cap40 loaded/validated but resolution overflowed while summing base+raw surplus before the negative cap line. Quantity.From now sums lines in checked Int128 and converts only the final stored total to long. RED ValidLargeSupportGrowthIsCappedBeforeStoredTotalOverflow retained; ordinary reserve overflow still uses the original checked available-reserve gate. This follows the approved wide-intermediate/capped-growth contract.

Task9 gates: 441/441 simulation; Species1177/1177 (61 QA paths,29 PNGs); Expansion275/275 (20 QA paths,10 PNGs); Legacy466 assertions plus27 geometry observations (11 QA paths,16 PNGs). All run records passed with zero false rows and no final engine diagnostics. Each normal rebuild0warnings/errors; metadata excludes all three validation classes. Owner-file hashes match original exact values. Inspected800 setup and extended daughter images; small text remains a human-readability limit. Supplemental labelled fixtures passed first execution; no fabricated RED.

Task 9: complete (commits 7449d34..bb9a2c5, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo -> 441 passed,0 failed/skipped; all three GUI gates inspected).
Task10 BASEbb9a2c517466aae8aee27076015d586e8a4e8527.
Task10 Ruling: retain raw runner/engine logs including six trailing-space PASS lines — evidence fidelity is preferable to editing observed output; source/docs formatting is checked separately — cost if wrong: six harmless whitespace warnings in evidence only.

Task 9: complete (commits 7449d34..bb9a2c5, tests: dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo -> 441 passed,0 failed/skipped; all three GUI gates inspected).
Task10 BASEbb9a2c517466aae8aee27076015d586e8a4e8527.
Task10 Ruling: retain raw runner/engine logs including six trailing-space PASS lines — evidence fidelity is preferable to editing observed output; source/docs formatting is checked separately — cost if wrong: six harmless whitespace warnings in evidence only.
