# Generated artifact cleanup — 2026-11-09

Owner explicitly authorized removing generated reports and test binaries from Git, preserving local copies, adding ignore rules and committing/pushing. Cleanup applies to current main; older branches/history and every local archive remain preserved.

Removed19,302 generated report files from the index only, including79 compiled validation DLL copies, raw JSON results/campaign captures, screenshots, logs, TRX results, hash lists and an archive. Their2,164,561,036 local bytes were SHA256-verified before and after. Written Markdown reports, authoring/check scripts, source, assets and licenses remain tracked. No personal user-data campaign save was touched. Raw save captures under reports remain locally available and ignored.

A historical scouting regression test directly requires256 report artifacts. Copied those exact inputs (42,655,936bytes, no DLLs) into tests/OpenAntares.Simulation.Tests/Fixtures/scout-acceptance-20261025. The test now resolves only its historical reports/... references there; source/document references still use the repository. All existing assertions, expected counts and SHA256 checks are unchanged. All256 new fixture Git blobs equal their original blobs. Added -text/-whitespace attributes to preserve original bytes through checkout, and .gdignore to prevent importing fixture screenshots. Existing regression fixture blobs are unchanged.

reports/.gitignore excludes generated output by default while allowing Markdown, source/check scripts and report configuration. Root ignores standalone-tool Godot sidecars. Only generated files under reports were removed from the index; no tracked files outside reports were removed. No compiled DLL/PDB remains tracked. Source/game/content/schema/art and LICENSING.md retain their prior indexed blobs.

## Verification

Shared queue ae28500e5ab144bab768478e3863e3fa: local preservation hashes, ignore coverage, protected index/blob checks and fixture-byte copies pass. Follow-up staged fixture blobs match all256 originals. Shared queue9fb71ec3ddbb46db8ede7c5be4775b07: ScoutingIntegrationTests.FinalAcceptanceMapsEveryCriterionToExecutedArtifactsAndPreservation passes1/1, zero failed/skipped. Test compilation emits an unrelated existing nullable warning in ConflictReviewRegressionTests.cs:76; this cleanup does not change that file. Staged whitespace check passes. No full suite, game build, campaign replay or graphical test ran.

Local verification manifests/scripts are retained in the source worktree reports/repository-cleanup-20261109; test output remains in primary reports/repository-cleanup-20261109. These generated files are not committed.

## Evidence and old saves

The preserved regression saves and their expected continuations are important compatibility contracts: keep them versioned. Personal campaign saves represent player progress: keep them and make ordinary backups. Historical run saves and raw logs/screenshots are useful for diagnosing an old failure or reproducing an acceptance record, but most duplicates do not need source-control tracking. Frozen evidence that a regression test actually consumes stays tracked in Fixtures. Summary reports and tooling retain the explanation and provenance.

This is a normal cleanup commit, not history rewriting. The current tracked tree loses roughly2.12GB net after retaining the required fixture bundle; old commits still contain the original artifacts, so full-history clone/storage size will not immediately shrink. No local archives, branches, worktrees, stashes or saved campaigns were deleted. Push follows this authorized cleanup commit and remote equality is checked before reporting completion.
