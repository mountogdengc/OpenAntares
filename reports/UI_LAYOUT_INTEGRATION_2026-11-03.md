# UI layout — local main integration, 2026-11-03

Status: **Verified** for local integration and scoped automation. Owner explicitly requested keeping the approved layout and merging it into main. Main fast-forwarded from `5df255bd76e2c394765448223164744a007ddafe` to layout commit `f2dc9bf51919cac6d06f53d8b70d79dfd0e8e91d`; a following documentation/evidence commit records verification. The completed fresh implementation review was retained; product C# is unchanged from the accepted trial.

## Fresh merged-primary verification

All jobs used the shared queue and actual root `F:/Projects-PCGames/OpenAntares`.

- UI/control job `65edd46d5c1849c9ad43362f13a74a27`: **107/107** headless Godot checks, current+legacy campaigns, four ordinary window sizes, real commands, rejection feedback, report follow-up, Game-menu Main Save/Load and no orphan controls. Restore/build/opt-in/normal rebuild succeeded; builds0warnings/errors, Godot0engine diagnostics. Actual opt-in metadata is detected, then all12 scenarios/2helpers are absent from the normal assembly.
- Installed portable-profile attempt `2cd8b2716b7441b480976e514c4b037b`: **infrastructure failure before tests**. Docker Desktop Linux named pipe was missing; image build never connected to its daemon. CLI `docker desktop start --timeout 120` timed out with no backend started. After verifying the build process had exited and no daemon request was in flight, `runner.py recover --confirm-stopped` safely archived the failed prepare record through the official queue. No queue state/lock files were manually removed; no desktop test control was used.
- Complete Windows simulation job `d47cb66d60f745f397058b003db3b9a5`: **2,140 passed,0failed,0skipped**, exit0,43seconds. This includes all six cases excluded by the portable selector. It used the ordinary Debug test assembly from the fresh main rebuild, without restore/build side effects. Source/assembly hashes are retained before/after; all1,784simulation-source files and the test assembly remain unchanged.
- Independent `verify.py`:7final UI/test source hashes match current bytes;1,784simulation-source hashes and TRX counters match; ordinary/opt-in metadata and clean logs agree; all11 owner files are preserved and77original UI raw evidence files match the feature commit byte-for-byte. [Machine summary](ui-layout-integration-20261103/summary.json) and [preservation](ui-layout-integration-20261103/preservation.json).

The full Windows suite's `ScoutingIntegrationTests.Proof` writes two scout latest pointers. Both new generated/parity artifacts and copies of their pointers were retained under [scout evidence](ui-layout-integration-20261103/windows-suite-01/scout-evidence/). Their original new files also remain in reports/scout-execution. Both original owner latest pointers were then restored exactly from the premerge backup; assertions/test source were not changed. See [side-effect record](ui-layout-integration-20261103/windows-suite-01/scout-side-effects.json).

## Preservation and integration

Exact backups, hashes, original working patch, refs/stashes/worktree inventory remain at `C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.ui-layout-integration-20261103/before.json` and `owner-before/`. All11 pre-existing files remain uncommitted. Eight nonoverlapping files are byte-identical; the two status/validation files retain the original owner body byte-for-byte around the new integration notes, and project.godot retains all original owner bytes except the approved stretch-mode change. ANGLE,60FPS and VSync remain in the working project exactly as before.

Recovery branch: `codex/ui-layout-premerge-20261103`. Preservation stash: `45074528d817f06404130d95de55c8e887b8dde9`. Its three overlapping files are **already restored; do not reapply it**. All prior refs/stashes and all worktrees, including codex/ui-layout-trial and pending empire-conflict, remain retained. Generated UIDs and every prior QA/raw artifact remain intact. No remote push, release or cleanup.

The owner reported the layout looks much better and explicitly selected adoption. That does not establish the entire rendered/physical-input, accessibility, platform/export, balance/security or release matrix. Older graphical harnesses still have known toolbar/workspace assumptions; adaptation is **Deferred**, and historical counts are not current merged-layout evidence. Parent milestone/requirement statuses and totals are unchanged; no new gameplay/rules/writer/content was introduced.

## Use the merged version

Open `F:/Projects-PCGames/OpenAntares/project.godot` in the installed Godot4.7.2 .NET editor and run `game/Main.tscn`, or run `F:/Projects-PCGames/OpenAntares/Play-UI-Layout-Trial.cmd`. The ordinary assembly is built at `.godot/mono/temp/bin/Debug/OpenAntares.dll`. Installed .NET8 runtime and Godot .NET are the prerequisites already used by the checks. This opens the adopted map/right-console/bottom-navigation layout on main. Continue normal play; report window size/display scaling, page/action and any remaining readability or navigation issue for a bounded follow-up. No additional owner test is needed to complete the requested local merge.

To recheck the recorded source/evidence/preservation, run `python reports/ui-layout-integration-20261103/verify.py --check-only` from main. Fresh rebuild/control checks use the queue with `tools/Run-LayoutTrialChecks.ps1` and a new output directory. The retained full Windows command, TRX, manifests and queue results describe exactly what ran; it is not replaced by the unavailable Docker attempt.
