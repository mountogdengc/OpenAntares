# Newer UI and empire conflict integration — 2026-11-04

Status: Merged locally into main. Combined automated acceptance and fresh actual-main checks passed.

The owner requested combining the newer UI with conflict in an isolated checkout, running fresh combined acceptance, then merging into main after passing. Push, release and cleanup remain outside scope.

## Combined source and behavior

Branch `codex/conflict-ui-integration` combines main `234b45e5eab968d768095d7c858c95bfa12e92f0` (including newer map-first UI `f2dc9bf`) with conflict `b8068216af74627324e250d17f5420fe2270f1a2`. Final recorded source is `d49e63c79e7e7389b134e9f015646a38d84562b6`; compiled game/UI inputs are identical to `b9cb837356c0aecfff32c58775ddaae7dcbf99c9`.

The map-first shell, right console, bottom navigation, management workspaces, theme and Research controls remain. Conflict orders/attention, battle/blockade reports, responses, readiness blocking, feedback and legacy-label ownership are integrated. ANGLE,60FPS and VSync remain unchanged. Simulation/content match the accepted conflict branch; default rules12/writer13/79 supported pairs remain active. Setup now scrolls within the physical viewport while keeping Start/Load reachable. The resize callback preserves existing focus without duplicating automatic scrolling for newly focused controls.

Harnesses navigate actual workspaces and Game-menu actions, reacquire controls after rebuilt panels and retain state/save/future/visibility assertions. The scout fixture no longer duplicates automatic focus scrolling in the same frame; its original reachability assertion remains. The responsive colony gallery adds24 bounds checks. No gameplay assertion or campaign turn/seed limit was relaxed.

## Passing combined acceptance

Full native Windows solution build passed. Full unfiltered simulation suite:2402 passed,0failed/skipped. Layout:107 passed. Legacy cleanup:rules8/9/11/12 passed. All native cases have zero engine/console diagnostics and matching committed source/DLL evidence; ordinary rebuild/metadata restoration passed, with every validation namespace type absent.

| Native scenario | Assertions | Result |
| --- | ---: | --- |
| Legacy | 493 | Passed |
| Expansion | 275 | Passed |
| Species | 1194 | Passed |
| Rival | 1194 | Passed |
| Colony | 706 | Passed |
| Crisis | 482 | Passed |
| Scout | 240 | Passed |
| Fleet | 1664 | Passed |
| Combat | 3191 | Passed |
| ShipDesign | 1983 | Passed |
| Diplomacy | 868 | Passed |
| EmpireConflict | 3479 | Passed |

Selected native records are `gate06-{Legacy,Scout,Colony}`, the eight passing `final07-*` cases and fresh `rival08-Rival`. Four actual Main conflict campaigns passed for Harl42,Peldra45,Sulan43,Wenth42, each retaining seven stages,three equal future boundaries per stage and observed expiry. The original authoritative verifier reloaded all28 Main saves and replayed their futures successfully. Its seven negative self-tests and nine additional source-equivalence negative controls passed.

Final gates and exact queue IDs: [final-gates09.json](conflict-ui-integration-20261104/final-gates09.json). Machine summary: [final-summary.json](conflict-ui-integration-20261104/final-summary.json). Invariants: [premerge-invariants.json](conflict-ui-integration-20261104/premerge-invariants.json).

## Retained failures and source equivalence

All initial/targeted failures remain under `reports/conflict-ui-integration-20261104`; they are not counted as passing. They exposed old navigation assumptions,800x600 setup clipping and a newly-focused picker being scrolled twice during resize. Diagnostic traces and subsequent passing regressions are retained.

`final07-Rival` exited at its7200-frame emergency cap without a result and remains incomplete. The two committed launchers changed only Rival's emergency cap to72000; fresh `rival08-Rival` then completed1194 assertions. Every other authoritative byte, including all game/UI/C# harness inputs, is identical. The explicit proof checks all408 authoritative inputs and the exact two-file Git delta. Recorded source identities remain honest; no evidence was relabeled. [Source equivalence](conflict-ui-integration-20261104/source-equivalence.json), [negative controls](conflict-ui-integration-20261104/source-equivalence-negative-controls.json).

## Preservation and scope

All prior branches/worktrees/stashes,owner prose,saves/raw evidence and local conflict-document edits are retained. No second implementation reviewer was dispatched: the prior conflict implementation review and separate UI review remain. Parent17 requirement rows/totals are unchanged. All current Godot launches use explicit log files. The earlier owner-approved exception for exactly three historical diagnostic logs remains unchanged;8219 other original QA files and all saves/screenshots were preserved in that audit. No complete8222-file survival claim is made.

This establishes combined local automated acceptance through real rendered controls/signals and saved-game replay. It does not establish physical input, human readability,accessibility,platform/export,balance,security or release acceptance; their existing checklist and scopes remain separate. No push,release or cleanup.

## Main integration and fresh verification

Fast-forwarded clean main from `234b45e5eab968d768095d7c858c95bfa12e92f0` to `d9e1fd16393e0e6cb516b9edc73d09fa740cf390` after isolated acceptance passed. Recovery branch: `codex/conflict-ui-premerge-20261104`. Both newer UI and conflict commits remain ancestors. All408 authoritative source inputs match the tested source byte for byte;2259 committed raw acceptance/native artifacts matched their Git objects. No newer UI was replaced by the older conflict layout.

Fresh checks executed from `F:/Projects-PCGames/OpenAntares`: solution build passed; full Windows2402/2402,0failed/skipped passed; ordinary assembly metadata confirms all13 scenario types,both helpers and the entire validation namespace are absent. Source snapshots before/after each native job match. [Postmerge verification](conflict-ui-integration-20261104/postmerge/verification.json), [merge identity](conflict-ui-integration-20261104/postmerge/merge.json).

The isolated acceptance summary retains its original premerge source/branch identity. This postmerge record establishes the exact source relationship and fresh main checks; no recorded evidence was relabeled. All prior branches/worktrees and the isolated integration checkout remain. No push,release or cleanup.

## Separate owner-operated physical follow-up

Prerequisites: .NET8,the installed Godot4.7.2 Mono build,the ordinary assembly verified above,and fresh QA save names. Keep the retained saves/evidence unchanged. These checks assess physical input and human understanding; the automated merge gate above is complete.

Build from main with `& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot 'F:/Projects-PCGames/OpenAntares' -Suite build`. Launch the ordinary game from PowerShell:

```powershell
$qaLog = Join-Path $env:TEMP ('OpenAntares-combined-ui-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
& 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --path 'F:/Projects-PCGames/OpenAntares' --log-file $qaLog
```

1. At800x600,use mouse and keyboard on New Game. Scroll every setting and activate Start. Expected:Start/Load remain reachable,default rules12 Standard opens on the map,and no control is clipped.
2. Visit each bottom-navigation workspace,open a picker,scroll and Tab/Shift+Tab,then resize between800x600 and1920x1080. Expected:focused controls remain visible,popups stay reachable,return restores the map,and navigation/resize does not advance the campaign or submit an order.
3. Copy `F:/Projects-PCGames/OpenAntares/reports/conflict-ui-integration-20261104/final07-EmpireConflict/earned/harl/main-active-battle.json` to a fresh timestamped `qa-combined-human-...json` name under `C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves`; retain the original. Load that copy through Main. Open conflict orders,battle/blockade reports and permitted system/fleet links. Expected:readable controls and clear own authority/response requirements,no private opposing orders/equipment assumptions,and no automatic command from opening or resizing.
4. Physically review and apply one legal order,acknowledge only its intended report,and advance one boundary. Save under another fresh QA name and reload. Expected:one submission,clear outcome,retained orders/damage/dates and no duplicate report/action. The dated truce and other six saved stages are available beside the active-battle input for the corresponding checks in the retained historical checklist; use these actual Main saves as inputs.
5. Load a fresh QA copy of a genuine rules11 archive. Expected:the older rules and peaceful rival behavior remain;opening current conflict controls creates no war or orphan label. Return to New Game and verify the current default remains rules12.

Report back:main commit,engine/display size,input method,QA filename/seed,action,expected/actual result,pass/fail and screenshot/log path. Those results unlock any human-usability/accessibility corrections and their focused regressions;platform/export,balance,security and release remain separately scoped.
