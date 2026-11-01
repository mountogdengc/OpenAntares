# Empire-conflict in-game handoff

The implementation and test harness are on `codex/empire-conflict` in:

`C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/empire-conflict`

Default New Game remains rules11. Rules12 loads explicitly; writer13 supports79 pairs. Task20 activation is withheld until required feature checks pass. Engine rehearsals and compilation do not establish native or Main acceptance.

## Prerequisites and exact build

Use .NET8 and the installed Godot4.7.2 Mono build. Retain existing saves and choose new QA names. All heavy builds/imports/tests use `F:/Tools/CodexTestRunner/runner.py` with this actual source root. Do not use the older checkout mutex gate.

From PowerShell:

```powershell
Set-Location -LiteralPath 'C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/empire-conflict'
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 600 -- dotnet build OpenAntares.csproj -c Debug -t:Rebuild --nologo
```

The ordinary game assembly is `.godot/mono/temp/bin/Debug/OpenAntares.dll` beneath that worktree. It must exclude all validation namespace types. For an owner-operated physical check, launch:

```powershell
& 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --path 'C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/empire-conflict'
```

No host desktop automation is authorized. Agent-run graphical checks need an explicitly configured automation connection to an exclusively owned VM. That connection/ownership is not presently available. Record the VM’s actual source, engine and queue paths before using it; do not test a stale primary checkout or copied old DLL.

## Owner-operated physical checks

The positive engine rehearsal inputs are retained under `reports/conflict-execution/task-21/`:

| Species | Rehearsal directory |
| --- | --- |
| Harl | `probe-harl42-20261101-policy4-corrected02` |
| Peldra | `probe-peldra45-20261101-policy4-corrected02` |
| Sulan | `probe-sulan43-20261101-policy4-corrected02` |
| Wenth | `probe-wenth42-20261101-policy4-corrected02` |

Each contains `pending-declaration`, `pending-attack`, `active-battle`, `queued-withdrawal`, `effective-blockade`, `queued-peace-answer` and `truce` files ending `.engine-save.json`. These are explicitly engine rehearsal inputs. Copying one into the save directory does not make it an earned Main campaign.

To inspect an input, copy it with a fresh `qa-ecf-manual-...json` name to `C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves`, then choose it through Main’s Load dialog. Keep the originals and existing saves.

Repeat the relevant display/focus checks at800×600 and1920×1080:

1. Load `pending-declaration`. Open Diplomacy. Confirm the declaration is visibly queued and may be revoked; opening, closing and resizing do not publish it. End Turn publishes at the stated boundary without an immediate declaration-turn attack.
2. Load `pending-attack`. Open Empire conflict orders. Confirm the owned force, observed armed target, doctrine, different-system owned return port, Keep/Detach and arrival choices are explicit. Opposing equipment/hull may be shown; opposing cohesion, doctrine, route, officer and bank must remain absent. Changing a field disables a previous Apply until another review.
3. Use Tab/Shift+Tab and keyboard activation to review and apply one eligible command. Confirm one application, a clear rejection for unavailable authority, and no automatic submission from reopening, resize or Load. A voluntarily initiated attack needs a legal owned port in another system.
4. Load `active-battle`. Review changed orders and withdrawal. Local Break contact must say it stays in the same system without healing or immunity. Withdrawal estimates must remain conditional on opposing movement/command effects. End Turn allows at most one exchange or interception per participant; inspect the actual result rather than assuming an unknown enemy choice.
5. Inspect the report and fleet history. Every actual shot should name source, armed target, damage/band/armor calculation and each hull’s before/after/capped loss, with no spill or retargeting. Repair/return/system links must open an owned or permitted destination. Report acknowledgement must not accept revised-force or peace orders.
6. Load `effective-blockade`. Review Lift and apply it once. The blockader sees status/system/opponent and effective date, without the target’s private production or reserve. Target production retains the published-pressure forecast until publication. The labelled701+neutral-trade100 native supplement must independently reconcile450, and private foreign lifting orders must not change the victim’s planning view early.
7. Load `queued-peace-answer`. Confirm the exact own answer, a separate revoke action and no automatic answer from Ready. On accepted peace there is no final volley or blockade charge and no automatic healing. A human may offer unconditional peace again immediately after withdrawing an offer; the AI’s outgoing cooldown remains a policy limit.
8. Load `truce`. Confirm the dated ten-complete-boundary declaration restriction. Save with a fresh name, return to Main and Load it again. Orders, damage, dates and private own answer must persist without replaying a report or submitting a command.
9. Load a genuine rules11 archive through a fresh QA copy. It must retain its old rules and peaceful rival behavior. Rules12 Solo must invent no rival or war. The dedicated labelled mixed-defense/revised-force fixture must preserve protected civilians and require only its own revised-force response before Ready; if that VM fixture has not run, record this check as pending.

These physical checks supplement the required automated native corpus. They do not replace it or authorize a rules12 default flip.

## Required VM sequence and the work it unlocks

After an explicit VM automation connection and exclusive worker ownership are configured, use its actual checkout and the same shared queue for noninteractive builds/imports. `Run-Validation.ps1` now queues its builds internally. Do not put the graphical runner itself inside `runner.py exec`.

1. Run the EmpireConflict feature scenario without `-EarnedConflictCampaigns`, plus affected Fleet/Combat/Diplomacy/Colony checks, on the VM. Example from the recorded checkout:

   ```powershell
   pwsh -NoProfile -File tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario EmpireConflict -OutputDirectory reports/conflict-execution/native/fresh-feature-run
   ```

   Expected: all actual political, Attack/Change/Blockade/Lift, response/withdrawal and blockade-report assertions pass at both sizes, with Main saves and zero diagnostics. Real opt-in12+2 detection and ordinary rebuild/exclusions must pass. This unlocks the remaining Tasks17–19 runtime gates.
2. Complete Task20’s approved rules12 default boundary and current native setup expectations, then rerun its suite/build/setup compatibility checks. Do not flip the default before step1 passes. Old rules11 archives remain unchanged.
3. Run all twelve scenarios sequentially: Legacy, Expansion, Species, Rival, Colony, Crisis, Scout, Fleet, Combat, ShipDesign, Diplomacy, EmpireConflict. Use a fresh directory for every run. For the final EmpireConflict run, add `-EarnedConflictCampaigns`. This mode deliberately requires actual default rules12 Main New Game; it cannot pass against the currently gated default11.
4. Expected final EmpireConflict output: four untouched Main new-game campaigns, genuinely paid militaries on both sides, all seven actual Main Save/Load stages, three uninterrupted-versus-loaded boundaries per stage, actual battle/repair/blockade/peace and complete truce/grievance expiry. Ordinary preparation commands are distinguished from real conflict/Ready control signals. Source manifests, retained executed assemblies, execution records, raw logs, screenshots and Main copies must agree.
5. Repair any failures within the authorized scope, retain all attempts, rerun affected checks and complete the final source/preservation/evidence audit. Only then can Task22 and the milestone be Verified. No merge, push, release or cleanup is authorized by these checks.

## Report-back fields

Record source commit and any working changes; actual machine/VM connection and exclusive owner; source root; Godot/.NET versions; build/launch command; resolution; scenario/mode; starting save and species/seed; turn before/after; exact control actions; expected/actual result; rejection/diagnostic text; relevant own fleet/encounter/target IDs; Main save names/paths; screenshot/log/result paths; and pass/fail/pending for each numbered check. Give failures and missing evidence explicitly. These records determine whether to finish feature gates, activate Task20, rerun campaigns or close Task22.
