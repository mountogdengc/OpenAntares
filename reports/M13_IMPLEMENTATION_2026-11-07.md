# M13 implementation and development verification — 2026-11-07

**Implemented and verified for current development scope. Recorded acceptance and human acceptance remain Pending.** Isolated branch `codex/playability-polish`, HEAD60044af plus uncommitted M13 changes. No commit/merge/push/release/cleanup authority is inferred.

M13 implements measured map labels and explicit owned-vessel selection; selected-vessel context; grouped existing setup; authoritative colony/system/research/queue summaries; semantic focus/scroll/selection and report/menu returns; honest response notices and collapsible optional Consider guidance. Existing command authority, Game-menu IDs0/1/2 and original map-first theme remain. Simulation/content/schema/balance, rules12/writer13/79 save pairs and ANGLE/60FPS/VSync are unchanged.

The one fresh whole-M13 review was completed and its three findings were reproduced and repaired. All six scoped product repairs are development-verified. Owner approved the sixth in ADR-028: the Wenth/Solo7 rendering crash. Controlled isolation showed console scrollbar/wrapped-text feedback; the final repair is only `ConsoleScroll.VerticalScrollMode=Reserve`. Report-width experiments were disproved and reverted to exact pre-sixth canonical source. No report content, required response or acknowledgement was hidden/waived. All failed attempts, raw logs, source/DLL records and QA saves remain retained.

## Current-source evidence

| Check | Result |
| --- | --- |
| Solution build |0warnings/errors;2028source files stable; `reports/native-tests/20261107-154505-ca004855` |
| Unfiltered Windows suite |2402passed,0failed/skipped;2028source files stable; `reports/native-tests/20261107-154514-701ffd74` |
| Existing headless layout/control checks |107pass; `task10-sixth-final-layout-01` |
| Native M13 seven-case run |Setup69,Map67,Selection13,Colony193,Navigation150,Guidance120,Campaigns9275; **9887 assertions**, all pass; `task10-sixth-final-all-01` |
| Sustained actual-Main audit |8fixed campaigns,640audit boundaries plus24proof-tail boundaries;40actual Main saves;120equal future boundaries |
| Independent replay |39checkpoints through unchanged external probe;1explicit native crisis-command extension; all40 accounted for; `task10-sixth-external-replay-01` |
| Evidence controls |67synthetic negative/positive controls pass; no gameplay claim from synthetic fixtures |
| Identity/diagnostics/restoration |438authoritative inputs, one source/DLL epoch, complete seven-case verifier passed, zero engine diagnostics, ordinary validation namespace empty |

Four physical sizes800x600/1152x648/1600x900/1920x1080 are covered. Native control signals and synthetic keyboard events do not establish physical-input comfort or human readability. Setup images include labelled standalone component fixtures over unchanged Main; they are not claimed normal setup screenshots. The earned Wenth7 report is captured at all four sizes, with no state/PRNG/acknowledgement changes from relayout.

Audit observations use only the first80 boundaries, not the proof tail. Idle queues/untargeted research remain permitted and bank resources; short support shortages cleared on following observed boundaries. See final-observations.json for actual counts. These observations are descriptive, not balance acceptance. Actual event witnesses never replace the separately required four earned M12 conflict campaigns.

## Acceptance status

| Criteria | Development disposition | Remaining gate |
| --- | --- | --- |
| PLY-AC-001 |Setup/settings/single Start/error/Load/orientation pass |Committed evidence; owner clarity observations |
| PLY-AC-002/003 |Measured labels, owned selection, nearest/tied markers and no-order inspection pass |Committed evidence; physical selection/readability |
| PLY-AC-004 |Authoritative species/menu/economy summaries and capacity units pass |Committed evidence; human understanding |
| PLY-AC-005 |Semantic context, lost selection, menu/report return, focus/scroll/Load pass |Committed evidence; owner journey |
| PLY-AC-006 |Projected notices, forecast-only rejection, optional banking, Ready/cancel and Consider collapse pass |Committed evidence; owner explanations |
| PLY-AC-007 |Four-size bounds/native keyboard/scrollbar-threshold behavior pass |Physical input remains Pending |
| PLY-AC-008 |Current8/40/120 and independent replay pass for development |Fresh committed-source evidence; no inferred balance pass |
| PLY-AC-009 |Windows2402pass; affected intermediate regressions retained |Original strict all12 plus four earned conflict campaigns on committed final source |
| PLY-AC-010 |UI-only boundary, diagnostics, ordinary namespace and preservation pass |Final committed-source attestation |
| PLY-AC-011 |6of6authorized fixes resolved, no known critical/major unresolved |Owner playtest findings; seventh product repair needs revised scope |
| PLY-AC-012 |Design/plan and sixth amendment explicitly approved; outcomes separated |Owner-operated report/final disposition |

## Preservation and handoff

All121 frozen genuine rules11 saves/futures/provenance files,3primary owner documents, prior refs/stashes/worktrees and protected simulation/content/theme/config/common-target/probe/strict-runner files remain unchanged. Forty actual Main Load traces confirm a different screen instance, closed Load dialog and no retained hint/dialog. Parent17 requirement rows/totals remain unchanged. The historical exception still covers exactly3old diagnostic logs; there is no new log-loss exception. Current preservation record: `task10-sixth-final-preservation/result.json`.

Next: separate authorization to commit this concrete reviewed M13 source. Then run fresh strict committed-source build/Windows/layout/M13 and the original twelve-scenario/four-earned-conflict acceptance gates, retaining source/DLL/log identity and ordinary restoration. Existing development results are explicitly `accepted:false`; no prior-source pass is promoted. The prepared owner-operated packet covers mouse/keyboard journeys, actual QA copies and report-back fields; no feedback means Pending. No merge/push/release/cleanup follows automatically.

[Machine record](M13_IMPLEMENTATION_2026-11-07.json) · [Review](M13_IMPLEMENTATION_REVIEW_2026-11-07.md) · [Sixth amendment](M13_SCOPE_AMENDMENT_2026-11-07.md) · [Owner playtest](../docs/playtests/m13-playability-2026-11-07.md) · [Canonical ledger](../.superpowers/sdd/2026-11-06-playability-and-campaign-polish/progress.md).
