# Native checks through the shared queue

Owner directive, 2026-11-03: use local Windows checks without Docker. Keep the shared one-heavy-job-at-a-time queue. This supersedes the earlier Docker-profile directions for OpenAntares and its linked worktrees. Do not start Docker or attempt container preparation for this project.

## Commands

From the active checkout:

```powershell
.\tools\Test-Queued.ps1
.\tools\Test-Queued.ps1 -Suite build
.\tools\Test-Queued.ps1 -SourceRoot 'C:/path/to/active/worktree'
python F:/Tools/CodexTestRunner/runner.py status
```

The default runs the complete Windows simulation xUnit project, including the six cases excluded by the old Linux selector. `build` builds OpenAntares.sln, including the Godot presentation project. The commands restore/build normally with dependency auditing; no stale `--no-build` assumption or audit-disable workaround is added. Requirements: Windows, Python3.13+, the installed .NET SDK/runtime; the build suite also needs the project's Godot .NET SDK. Docker is not a prerequisite.

`Test-Queued.ps1` submits the foreground Python native worker through `runner.py exec`. `.codex-tests.json` is schema2/native configuration for this wrapper, not input for the legacy `runner.py run` Docker path. The installed Docker profile is historical and is not used. SourceRoot defaults to the invoking checkout; an explicit worktree is resolved, checked for OpenAntares.sln, recorded and used as the actual command cwd. The invoking wrapper/config path is recorded separately, so an older worktree can be tested without silently substituting primary source.

## Evidence and preservation

Each run retains request/command/config path, before/after SHA256 source manifests, command.log, TRX counters and result.json under `reports/native-tests/<unique UTC run>/` in the tested checkout. The shared runner separately retains its exec result under `F:/Tools/CodexTestRunner/state/results/openantares/<job-id>/`. Source changes during a run, command failure, empty/missing TRX or failed tests cannot pass. Skipped tests produce a nonzero worker result and explicit incomplete coverage; the queue's exec result classifies nonzero child exits as failure. Inspect both records.

The full Windows suite generates two scouting evidence artifacts and updates their latest pointers. The native worker retains the new files/pointer copies in the run's scout-evidence directory and restores any pre-existing latest pointer byte-for-byte, after checking the new artifact belongs to this run. Original generated files and all existing/QA evidence are retained. No test assertions are changed to achieve a pass.

Other noninteractive heavy checks use the same queue, for example:

```powershell
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 1800 -- pwsh -NoProfile -File tools/Run-LayoutTrialChecks.ps1 -OutputDirectory reports/a-new-ui-check-directory
```

The existing UI/control runner performs its own opt-in/ordinary assembly restoration. Native simulation tests do not establish rendered UI, physical input, accessibility or release acceptance. Do not use desktop control for routine testing. Keep the existing manual/VM ownership guards, retained failed evidence and explicit recovery rules. Do not bypass a live slot, delete queue state/locks or launch detached heavy children. Native exec serializes work and owns its foreground process tree; it does not claim Docker CPU/RAM isolation.

Older linked worktrees may still contain the retired Docker wrapper. Invoke the current primary wrapper explicitly with the active root instead:

```powershell
& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot 'C:/path/to/active/worktree'
```

Verified native default run: queue `776b2c7404904f84942b71e74cffb0dd`, run `reports/native-tests/20261104-020318-83190a98`:2140passed,0failed/skipped;1838source hashes unchanged;both pre-existing scout pointers restored exactly. No Docker command or startup is part of this native path.

Native solution build also verified: queue `14a0594c799348bca8c33afb98d4b09b`, run `reports/native-tests/20261104-020514-dc9595cb`, exit0 with0warnings/errors and unchanged1838source hashes.

## Combined native graphical matrix

The owner authorized local rendered conflict acceptance without the unavailable VM. For this continuation, run the active checkout's `tools/ConflictValidation/Run-NativeMatrix.ps1` directly with a fresh `-RunName` and `-EarnedConflictCampaigns` for complete conflict acceptance. Its source root is the checkout containing the script. Do not wrap the whole graphical runner in `runner.py exec`: it submits its opt-in and mandatory ordinary rebuilds to the shared queue itself. Graphical processes use the same authorized local method as `Run-Validation.ps1`.

One selected matrix uses one queued opt-in build, actual graphical processes and unchanged assertions/turn limits (Rival now uses the72000 emergency-frame cap shared by longer campaigns; its incomplete7200-frame attempt is retained), individual committed source/DLL captures, actual twelve-type/two-helper negative metadata checks, both engine/console logs and all native assertions. The opt-in DLL must remain identical throughout execution. Every selected case remains unaccepted until the queued ordinary rebuild succeeds, all validation namespace types are absent, and final source/diagnostic checks finish. Each case retains its own final execution and ordinary metadata; the matrix summary states its selected scope. Complete milestone acceptance still requires all twelve scenarios and all four actual Main conflict campaigns, plus the separate layout/full Windows/evidence-replay gates. No physical-input, comfort, platform/export or release claim follows from these control signals.
