# Species local integration — 2026-10-24

The owner selected **option1: merge into local main**. Main at `F:/Projects-PCGames/OpenAntares` fast-forwarded from `e32ada6d1c8fbef4fadd620a93139c6188447c27` to **`3f25a1463fe6b2b4f55b171b0d4f4646d954a32a`**, the completed/reviewed species branch. Product source is identical to the tested correction checkpoint713d305; this integration adds no gameplay rules or dependencies. The final integration handoff commit contains documentation/evidence only.

## Fresh main verification

| Gate | Result | Evidence |
| --- | --- | --- |
| Normal solution build |Exit0,0 warnings/errors |[Build log](species-integrated-2026-10-24-run1/build.log) |
| Full simulation |445 passed,0 failed/skipped |[Test log](species-integrated-2026-10-24-run1/tests.log), [named TRX](species-integrated-2026-10-24-run1/test-results/species-integration.trx) |
| Species graphical campaigns |1181 passing assertions;29 PNGs,61 retained QA paths |[Results](species-integrated-2026-10-24-run1/species/results.json), [engine log](species-integrated-2026-10-24-run1/species/godot.log) |
| Frozen expansion campaigns |275 passing assertions;10 PNGs,20 retained QA paths |[Results](species-integrated-2026-10-24-run1/expansion/results.json), [engine log](species-integrated-2026-10-24-run1/expansion/godot.log) |
| Original colony campaigns |466 assertions plus27 geometry observations passed;16 PNGs,11 retained QA paths |[Results](species-integrated-2026-10-24-run1/legacy/results.json), [engine log](species-integrated-2026-10-24-run1/legacy/godot.log) |
| Ordinary assembly |All3 validation classes absent after the final normal rebuild |[Assembly metadata check](species-integrated-2026-10-24-run1/assembly-check.json) |
| Preservation |Both owner hashes match, recovery stash/snapshots/worktrees retained; every92 final QA paths exists |[Verification record](species-integrated-2026-10-24-run1/verification.json), [preflight](species-integrated-2026-10-24-run1/preflight.json) |

All final engine logs were inspected: no false result rows or engine diagnostics. Each runner finally rebuilt the normal solution with0warnings/errors. The completed branch also passed445 tests immediately before integration; these main results are new runs, not copied branch counts. Environment: Windows x64, .NET SDK10.0.113/net8.0 and installed Godot4.7.2 .NET console/GL Compatibility. Existing restored dependencies; `--no-restore` is not a clean install or dependency-security audit.

Commands ran from the primary checkout:

```powershell
git -c maintenance.auto=false -c gc.auto=0 merge --ff-only codex/species-mechanics
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo --logger 'trx;LogFileName=species-integration.trx' --results-directory reports/species-integrated-2026-10-24-run1/test-results
# Each Scenario: Species, Expansion, Legacy; distinct output subdirectory per scenario.
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <installed-Godot-.NET-console> -Scenario <scenario> -OutputDirectory reports/species-integrated-2026-10-24-run1/<scenario>
```

Full runner logs and exact QA paths are in the evidence directory. Godot executable: `F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe`.

## Owner-change preservation and recovery

Before merging, ancestry and zero branch overlap with both dirty documents were verified. Exact byte copies plus a binary patch were retained at **`C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/species-integration-recovery-20261024T132901238962Z`**. The merge did not stash, reset, replace, stage or commit either owner document. SHA256 values match before/after:

| Owner document | SHA256 |
| --- | --- |
| docs/SPECIES_ROSTER.md |843fd758b497094b864b965ad7fff126464bc8fe93187b526bf7d27f0f853034 |
| docs/research/faction-survey.md |814d22e56cacd52885d947e16bc3e4e6123e4a35cf60297b8af58cf3a99463ba |

Prior recovery stash4175cbfe407e822997fe41937946416335045913 and the integration-recovery-20261023T143127Z snapshot remain preserved, not reapplied. Species, expansion and older spiral worktrees/branches are retained, including ignored scratch and generated Godot metadata. The owner’s menu choice authorized local integration; prior preserve-evidence instructions take precedence over generic finishing-skill cleanup. No branch/worktree deletion, remote fetch/pull/push, PR, history rewrite or release was performed.

## Handoff and limits

Species implementation/review/integration are complete for local automated acceptance. New games select four species with visible Harl default(rules3); writer4 retains the seven explicit supported schema/rules pairs and genuine old campaign continuations. All17 requirement rows/counts remain unchanged:12 Verified,3 Implemented/Unverified,2 Deferred. Select/specify the next bounded strategic milestone before new gameplay implementation.

The harness invokes actual UI signals/handlers/commands in process. Screenshots/layout checks do not establish physical mouse/keyboard delivery, accessibility or comfortable800x600 reading; uncoached comprehension remains Deferred under ADR-005. Balance/enjoyment, cross-platform equality and exported packaging/security acceptance remain open. This merge does not recruit a participant or authorize later AI/combat/diplomacy work.
