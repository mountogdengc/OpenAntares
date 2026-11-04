# Empire-conflict local acceptance - 2026-11-03

Product source: `8cfe8a3ca9bed990ca6625a437db3f01c33169de` on `codex/empire-conflict`. M12-P/M12-I and ECF-AC-001..012 are Verified for the approved local automated/design scope. Task20 activation and Task22 evidence closure are complete. New games use rules 12; writer 13 supports 79 explicit pairs. Old campaigns retain their recorded rules.

The full Windows simulation suite passed 2,402/2,402 with zero failures/skips. All twelve native scenarios passed with zero engine/console diagnostics, stable source hashes, real opt-in detection and ordinary assembly restoration. The final evidence verifier passed, including authoritative saved-future replay.

| Scenario | Assertions | Diagnostics |
| --- | ---: | ---: |
| Legacy | 493 | 0 |
| Expansion | 275 | 0 |
| Species | 1194 | 0 |
| Rival | 1194 | 0 |
| Colony | 682 | 0 |
| Crisis | 482 | 0 |
| Scout | 240 | 0 |
| Fleet | 1680 | 0 |
| Combat | 3307 | 0 |
| ShipDesign | 2017 | 0 |
| Diplomacy | 924 | 0 |
| EmpireConflict | 3569 | 0 |

Four untouched actual Main campaigns earned paid military fleets on both sides, battle/damage/repair, contested/effective/lifted blockades, peace and complete truce/grievance expiry. Each retained seven actual Main Save/Load stages with screenshots and three uninterrupted-versus-loaded future boundaries. No live resource or outcome injection supplies the earned evidence.

| Human species | Seed | Main stages | Equal futures per stage |
| --- | ---: | ---: | ---: |
| harl | 42 | 7 | 3 |
| peldra | 45 | 7 | 3 |
| sulan | 43 | 7 | 3 |
| wenth | 42 | 7 | 3 |

Exactly one independent implementation review and its correction pass remain the review disposition; no second review was dispatched. Final continuation corrected orphan conflict-label ownership, console diagnostic capture, setup screenshot handling and legacy evidence metadata. The Expansion execution budget increased from 7,200 to 72,000 frames after a retained timeout; assertions, seed/turn budgets and required outcomes were not weakened.

## Preservation and explicit exception

8,219 of 8,222 original QA files remain byte-identical. Three historical Godot diagnostic logs were rotated by focused cleanup launches without an explicit log path and could not be recovered. The owner explicitly approved documenting those three losses. Two affected entries were restored from byte-identical retained copies; the newer log was retained separately before restoration. The original audit failure and every recovery search remain evidence. This is not a claim that all original QA files survived.

All saved campaigns, screenshots, protected owner prose, original stash entries and ref names, eleven worktrees, and 121 frozen rules 11 files remain. Concurrent primary/UI integration and its added preservation stash are audited and retained. This acceptance work did not integrate into main.

[Approved three-log exception](conflict-execution/preservation-exception-20261103.json), [raw final preservation audit](conflict-execution/preservation-audit-20261103-final.json), [machine summary](conflict-execution/final-summary.json), [final verifier result](conflict-execution/acceptance-resume-20261103/final-evidence-verification.json).

## Scope

This closes the local automated functional/design milestone. Physical keyboard/mouse input, human comprehension/readability, fun/balance, platform/export, security, delivery and release acceptance remain separate. Existing 17 parent rows/totals, peaceful backlog prose and earlier evidence retain their original scopes. No merge, push, release or cleanup is included.
