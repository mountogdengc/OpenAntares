# Colony-crisis main integration — 2026-10-25

The owner authorized merging the completed M6 colony-crisis/plague expansion while preserving existing changes. Local `main` fast-forwarded from `fa2a07f33e6ef2e8d24549d0dcda635221068cd8` to reviewed/corrected source `d8bef99ead4a8db5ba882912744032dec60ed6ce` (`codex/colony-crises`). The approved rules6 Standard/Off and writer7 slice is active on main. No redesign or new feature work was added during integration.

## Fresh merged-main gate

- Simulation: 1,265 passed; zero failed/skipped.
- Sequential graphical scenarios: Crisis 398 assertions +6 provenance; Colony 666 +4; Rival 1,146 +12; Species 1,194; Expansion 275; Legacy 466 +27 geometry. Total: 4,145 passing assertions, 22 generated provenance rows and 27 geometry rows.
- All 230 referenced QA paths exist; 161 PNGs retained; zero Godot ERROR lines.
- Each graphical run exercised opt-in validation and then an ordinary solution rebuild: all six harness classes were present only under opt-in and absent in the ordinary assembly; ordinary builds reported zero warnings/errors.

The [machine summary](crisis-execution/main-integration-summary.json) records per-scenario run IDs, result hashes, counts and the retained ignored runtime folders under `.superpowers/sdd/2026-10-25-colony-crises-plague/runtime-main-integration/`. [Criterion mapping](CRISIS_VALIDATION_2026-10-25.md) and [independent review/correction dispositions](CRISIS_REVIEW_CORRECTIONS_2026-10-25.md) establish the bounded M6 acceptance basis. The earlier branch runs remain historical evidence.

## Preservation and state

Before the fast-forward, `main` had exactly four owner edits: the peaceful-mode paragraph in `PROJECT_STATUS.md`, modified `docs/SPECIES_ROSTER.md` and `docs/research/faction-survey.md`, and untracked `docs/FEATURE_REQUESTS.md`. Exact copies, a patch, original status, and SHA256 hashes were retained in the ignored `integration-owner-backup/` directory within the crisis worktree. The status paragraph was restored byte-for-byte after the fast-forward and excluded from this documentation commit; the other three files match their pre-merge hashes exactly. All four remain uncommitted owner work, and the main index is clean after this integration record.

The crisis worktree and all seven worktrees were retained. Existing saves, failed and successful QA evidence, recovery files and branch history remain. There was no push, release or cleanup.

M6-I and CRI-AC-001..011 are Verified for the stated local automated scope; CRI-AC-012 and M6-D remain Verified for approved design. Parent REQ-017 and the 17-row totals are unchanged. Physical input/readability, uncoached comprehension, balance and long-campaign play, Linux/macOS/ARM, hostile-input/dependency review, clean install/export and release acceptance remain open or deferred.
