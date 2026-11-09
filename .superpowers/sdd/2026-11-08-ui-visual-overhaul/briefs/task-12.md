### Task 12: Bounded findings, owner playtest packet, final regression and review

- [ ] Triage findings with screenshot/log/source/steps/severity; up to the D5 budget of reproduced fixes, each with a regression established RED first. Anything needing `src/`, content or a new system goes to a separate scope.
- [ ] Prepare the owner physical-play packet: queued build path, installed Godot path, fresh log, numbered journeys (map selection, console actions, fleets, ledger drag, each workspace, End Turn pending items, save/load) at 800x600 and 1920x1080, with look-and-feel questions and report-back fields.
- [ ] Final gates on one source: queued build, full Windows suite, layout checks, all M14 cases, all M13 cases, `Run-NativeMatrix.ps1 -EarnedConflictCampaigns`, the no-override check and the M14 verifier (before/after frame matrix complete, no missing cases). Ordinary validation namespace empty after restoration.
- [ ] One fresh whole-M14 implementation review; fix required findings within scope; rerun invalidated gates. Final report with per-criterion automated/human status. Hand off for owner disposition; no merge/push/release/cleanup.

## Coverage

| Criteria | Tasks |
| --- | --- |
| UIX-AC-001/002 | 2, 3, 12 |
| UIX-AC-003 | 5, 12 |
| UIX-AC-004 | 4, 12 |
| UIX-AC-005 | 6, 12 |
| UIX-AC-006 | 7, 12 |
| UIX-AC-007/008 | 8, 12 |
| UIX-AC-009 | 9, 12 |
| UIX-AC-010 | 10, 12 |
| UIX-AC-011 | 10, 11, 12 |
| UIX-AC-012 | 1, 12 |
| UIX-AC-013 | 12 and owner acceptance |

Review choices: D1-D5, the spec route (a or b above), sequencing after M13, the presentation-only boundary and the fix budget. Native inline method is retained. Nothing has been implemented by writing this plan.
