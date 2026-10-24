# Species implementation validation — 2026-10-23

Owner-approved classic space-opera species slice, executed inline with the retained Native preference. Baseline: `e32ada6d1c8fbef4fadd620a93139c6188447c27`; branch `codex/species-mechanics`, isolated checkout `C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/species-mechanics`. This is local Windows automated acceptance, with integration and human/release acceptance separate.

## Executed gates

Windows x64, PowerShell, .NET SDK10.0.113/runtime8.0.32, net8.0, installed Godot4.7.2 .NET console/GL Compatibility. Existing restored dependencies; no dependency upgrades or clean-machine claim. The native worktree tool could not find a repository from the chat directory; a manual Git worktree preserves the dirty primary checkout.

| Gate | Result | Retained evidence |
| --- | --- | --- |
| Full simulation suite | 441 passed, 0 failed/skipped | [Test log](species-execution/task9-wide-growth-green.log); final task completion run in execution ledger |
| Species graphical scenario | 1,177 assertions passed; 61 unique retained QA paths | [Results](species-task9-final1-20261023/results.json), [engine log](species-task9-final1-20261023/godot.log), PNGs in the same directory |
| Frozen rules-2 expansion | 275 assertions passed; 20 retained QA paths | [Results](species-task9-expansion-20261023/results.json), [engine log](species-task9-expansion-20261023/godot.log) |
| Original colony scenario | 466 assertions plus 27 geometry observations passed; 11 retained QA paths | [Results](species-task9-legacy-20261023/results.json), [engine log](species-task9-legacy-20261023/godot.log) |
| Ordinary solution rebuild | Each runner finally rebuilds normal assemblies; inspected 0 warnings/errors | Runner logs in [execution evidence](species-execution/) |
| Assembly exclusion | All three validation types absent; inspected ordinary assembly metadata | Final verification log in execution evidence |

Commands from the isolated checkout:

```powershell
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <installed-Godot-.NET-console> -Scenario Species -OutputDirectory reports/species-task9-final1-20261023
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <installed-Godot-.NET-console> -Scenario Expansion -OutputDirectory reports/species-task9-expansion-20261023
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot <installed-Godot-.NET-console> -Scenario Legacy -OutputDirectory reports/species-task9-legacy-20261023
```

Exact executable and full outputs are retained in the ledger/runner logs. No temporary validation types may remain in the ordinary assembly.

## Acceptance trace

| Criterion | Executed contract and evidence |
| --- | --- |
| SPC-AC-001 | Four exact JSON profiles; actual picker, visible Harl default and explicit species/rules-3 Start; alternative missing-Harl and empty-list behavior; state/save validation fixes identity |
| SPC-AC-002 | SpeciesEconomyTests: approved opening outputs, aggregate fractional fixed-point rounding, flat buildings, shortage floors, blocked capacity and wide bounded growth; UI uses shared breakdowns |
| SPC-AC-003 | Exact Harl/Sulan/legacy growth traces, prior progress and discard rules; actual Harl one-unit daughter recovers in the campaign harness |
| SPC-AC-004 | Role data/allowed selection, all snapshot requirements, bank/cancel, no allocated ID while blocked, reserve/cargo conservation, birth boundary; each species builds/travels/founds through actual UI commands |
| SPC-AC-005 | Known/current/stale environments, hidden-state parity, native Shelf Sea versus sealed any-colonizable; both contrasting labelled maps produce native capacity12 and sealed capacity8 daughters without terraforming; saved harbor campaigns continue |
| SPC-AC-006 | Wenth banked immature saves, base6 maturity and parent3/seed3; extended threshold8, no same-snapshot growth unlock, parent5/seed3, daughter capacity6 and no inherited buildings; real save/load dialogs |
| SPC-AC-007 | Same seed/size geometry and RNG across species; ordinal weighted environment pass including sampled/overridden home; genuine old four-turn canonical continuation; future state after UI reload equals uninterrupted resolution |
| SPC-AC-008 | Explicit seven schema/rules pairs, invalid content/state/observations/habitat/cargo, old metadata omission, failed writes/loads, unknown/nonexistent parity, competing intents and checked ID/available-reserve overflow preserve state |
| SPC-AC-009 | Twelve generated seed42 species/size loops plus forced Sulan harbor; scout, survey, budget, build, embark, exact travel, explicit founding, two-colony allocation, save/load continuation. Supplementary initial fixtures are labelled synthetic; outcomes follow real commands. Physical windows800x600,1152x648,1280x720,1600x900,1920x1080; focus/scroll selectors and End Turn stay reachable and authoritative state is unchanged. All three regression scenarios and independent review are required final gates. |
| SPC-AC-010 | Owner design approval in ADR-008; latest owner “yes” approves the separate implementation plan. Exact mechanics establish reproducibility, not verified balance or enjoyment. |

The five plan Review Focus cases map respectively to alternative-content/fractional/overflow tests; generation ordering tests; blocked save/extended-maturity tests; seven-pair compatibility and frozen legacy runtime scenarios; and hidden-UI/foreign-overflow/atomicity/conflict tests.

## Observed failures and corrections

Earlier attempts remain intact. [Task8 RED](species-task8-red-20261023/results.json) failed the absent four-species chooser; the two Task8 green runs passed16 assertions each after core/UI activation. [Initial Task9](species-task9-initial-20261023/results.json) and [focused reproduction](species-task9-focus-repro-20261023/results.json) caught an already-focused construction selector outside its scroll area after resizing800→1152. Explicit refocusing rescued it, identifying a layout/focus notification issue. GameScreen now waits one layout frame on side-scroll resize and reveals its currently focused descendant. [First corrected matrix](species-task9-focus-green1-20261023/results.json) passed1,117 assertions before adding the supplemental cases; the final1,177 run includes them.

The new `ValidLargeSupportGrowthIsCappedBeforeStoredTotalOverflow` test failed for valid extreme support content because a temporary sum overflowed long before the negative growth-cap line. Quantity.From now accumulates in checked Int128 and checks the final stored long total. Its RED/GREEN logs are retained; actual available-reserve overflow still rejects atomically. Other fixture/test-authoring corrections and operational rulings are retained verbatim in the execution ledger.

## Preservation and limits

The primary remains `F:/Projects-PCGames/OpenAntares`, `main` at the baseline. Two owner prose files are excluded from all implementation staging: `docs/SPECIES_ROSTER.md` and `docs/research/faction-survey.md`. Expected SHA256 values: `843fd758b497094b864b965ad7fff126464bc8fe93187b526bf7d27f0f853034` and `814d22e56cacd52885d947e16bc3e4e6123e4a35cf60297b8af58cf3a99463ba`. Final Task9 checks confirmed these exact hashes again.

Recovery stash `4175cbfe407e822997fe41937946416335045913`, recovery snapshot, previous expansion/spiral worktrees, QA saves and earlier failed evidence remain preserved. Godot-generated import metadata is retained unstaged rather than folded into product changes. The genuine rules-2 fixture was captured before writer changes from the existing two-colony QA save, with source hash/provenance and four future canonical states; only schema_version is excluded from old trace comparison.

No species merge, push, release or worktree cleanup is performed. No AI, combat, diplomacy, custom civilizations, new portraits/assets/dependencies or campaign upgrades are added. Rules1/2 retain embedded content; writer4 supports only(1,1),(2,1),(3,1),(3,2),(4,1),(4,2),(4,3).

The harness invokes actual control signals/handlers and commands in process. It does not demonstrate physical Windows mouse/keyboard delivery, accessibility exposure, comfortable small-window reading, uncoached comprehension, balance, cross-platform equality or exported packaging. The800 screenshots were inspected: critical controls remain present but canvas scaling makes text small. The unavailable human session remains Deferred under ADR-005. Final review disposition and exact candidate/checkpoint IDs belong in [the handoff report](SPECIES_FINAL_REVIEW_2026-10-23.md).
