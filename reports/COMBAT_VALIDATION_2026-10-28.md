# Combat implementation validation — 2026-10-28

The approved implementation and review corrections are verified on isolated `codex/fleet-combat`. New campaigns now use rules9, writer10 and46 explicitly supported schema/rules pairs. Main remains at `dc190937`, rules8/writer9/37 pairs. Automatic sentry battles, standing orders, persistent damage/cohesion, withdrawal cohorts and finite port repair are implemented. Optional tactical controls remain later scope.

Final product source: `4176996fbda516849a9f5e24691c451e89960019`. Final gates ran2026-10-29 America/Denver; the filename follows the approved plan date. One fresh review found three Important issues, all closed by one regression-first correction pass; no Critical findings or deferred Minors. All16 approved tasks are complete; final task gate1733/1733 passed. Integration, release and human usability acceptance remain separate.

## Executed evidence

- [Task summaries and raw gates](combat-execution/) retain behavioral RED/GREEN, failures, fixes and exact commands. [Activation proof](combat-execution/task-15-summary.md) records12 paid generated campaigns, four actual Main reload stages and three-turn deterministic continuations per stage.
- Initial task16 simulation suite passes1688/1688, including alternate embedded role names across every saved combat phase, strict corrupt-load tests, all46 pairs, all13 rollback hooks and genuine old futures. [Suite log](combat-execution/gates/task16-suite-01.log).
- The initial nine graphical scenarios pass sequentially under Godot4.7.2 Windows x64: Legacy493, Expansion275, Species1194, Rival1194, Colony682, Crisis482, Scout240, Fleet1680 and Combat3163 result records (9403 total). Scout/Fleet/Combat additionally retain9/39/89 separate provenance records; some older scenarios include provenance in their result arrays. Every successful gate has0 engine diagnostics,0 build warnings/errors, an actual opt-in negative control and normal9-scenario/2-helper exclusions. See [machine summary](combat-execution/final-summary.json).
- The original rules8 capture has48 generated sources/192 generated checkpoints, ten supplemental sources and232 total future checkpoints. Expected futures were never regenerated; canonical comparisons remove only schema_version. Old rules1..7 fixtures also remain in the suite.
- Task16 hashes all293 raw archived rules8 files against their original manifest. Colony01/02 and Crisis01/02 retain failed former-current setup comparisons; final Colony03/Crisis03 explicitly load original8 sources through Main. The latter preserves all generated health/cargo/development assertions and corrects stale provenance labels. Dedicated current9 setup checks remain. The coarse runner's provisional summary counts only a checks array; final-summary audits each scenario's actual results/checks array and supersedes those provisional counts. Colony03's inherited prose says current form in one provenance entry; its actual explicit8 source/load/checks and JSONL are authoritative.
- [Preservation audit](combat-execution/task16-preservation.json) verifies7345 historical branch evidence/fixture hashes and all four protected owner files byte-exact. The primary checkout remains clean at accepted base. Of7821 worktree-entry hashes,7675 primary files match exactly;146 differ only by independently proved CRLF checkout conversion. A separate exact primary closing baseline is retained. All eight worktrees and existing recovery/branch references remain.

## Review closure

[Raw independent review](combat-execution/final-review.md) covers dc190937..24df8ea. [Disposition](combat-execution/final-review-disposition.md) records all three corrections and the explicitly unverified areas. No second review was performed under the selected workflow.

- I1: persisted civilian/cargo encounters require the saved authorization. Nine legitimate pending/active/queued cargo, empty-scout and armed-cargo cases reject malformed loads, writes, Ready and direct Resolve without mutation.
- I2: preview and execution share post-order phase selection, including unchanged queued orders, actual Press/Hold overrides, return/arrival/straggler edits and unarmed defensive forces choosing a newly legal port.32 matrix cases and the defensive-return regression compare exact next-boundary shots/damage/cohesion/departures.
- I3: the selector/report says Hold despite losses; reviewed previews explain ignored loss/cohesion thresholds and any actual cancellation of queued withdrawal. Native controls verify both window sizes before Apply.
- Corrected fullSuite **1733/1733** and focused review regressions **45/45** pass. [Latest suite](combat-execution/gates/final-review-suite-01.log), [regressions](combat-execution/gates/final-review-green-02.log). Original failures, the invalid empty-founder test fixture and its corrected legitimate-scout RED are retained; no production invariant was weakened.
- Affected native Combat rerun passes **3271 checks +93 separate provenance**, including12 paid campaigns and both new control cases. No engine diagnostics; both builds0 warnings/errors, real opt-in negative control and normal9+2 exclusions pass. Other eight scenarios retain their explicitly scoped pre-correction evidence. Latest combined scoped total is9511 records/141 separate provenance; this is not a claim that the other eight were rerun after correction.
- All twelve new start **and cleared** saves are byte-exact against the pre-correction generated campaigns; all four three-turn reload continuations pass again. [Comparison](combat-execution/final-review-campaign-comparison.json).
- Closing preservation verifies7821 exact primary closing hashes,7345 historical worktree hashes, all four protected owner files, unrelated refs and all eight worktrees. [Closing audit](combat-execution/task16-closing-preservation.json). It does not manufacture a missing pre-execution primary byte manifest.

Embedded preview images show the form/label; full warning/override text is retained from actual Label controls. Images inherit canvas scaling and do not establish visibility of every scrolled line or human comprehension. Existing native Apply focus/scroll checks pass at both sizes.

## Acceptance mapping

Each functional row is Verified for local automated scope after the correction gates; CBT-AC-012 is design-only Verified. The following named classes and actual native evidence define the automated proof boundary; they do not replace human acceptance.

| Criterion | Status | Executed evidence |
| --- | --- | --- |
| CBT-AC-001 Setup/content  | Verified (local automated) | CombatContentTests, CombatGenerationTests, CombatStateTests; native current Standard/Off, invalid Standard/prior-session retention and12 species/size starts |
| CBT-AC-002 Production  | Verified (local automated) | CombatIntegrationTests, Fleet request/production regressions; twelve paid two-warship campaigns with exact reserve conservation |
| CBT-AC-003 Damage  | Verified (local automated) | CombatMathTests, CombatResolutionTests, CombatTurnTests; exact frozen shots/bands and post-interception launch |
| CBT-AC-004 Cohesion/orders  | Verified (local automated) | CombatCommandTests, CombatResolutionTests, CombatReportTests; native order changes, revised-force/blocked-return responses and Routine Off |
| CBT-AC-005 Withdrawal  | Verified (local automated) | CombatWithdrawalTests, CombatReadyTests, CombatTurnTests; clean/exposed return, blocked ports and actual reload futures |
| CBT-AC-006 Detachment  | Verified (local automated) | CombatWithdrawalTests, CombatIntegrationTests; labelled native main/detached cohort supplements |
| CBT-AC-007 Repair  | Verified (local automated) | CombatRepairTests, CombatReportTests; generated arrival/no-tick/full-stationary repair and30-ship bulk repair/Hold supplement |
| CBT-AC-008 Integration/privacy  | Verified (local automated) | CombatKnowledgeTests, CombatReadyTests, existing scout/crisis/rival/fleet suites; native competing-observation privacy supplement |
| CBT-AC-009 Officers  | Verified (local automated) | CombatIntegrationTests and unchanged Fleet recovery/XP suites; explicit mortal thresholds/snapshot equipment and Wenth whole-body link identity |
| CBT-AC-010 Transactions/compatibility  | Verified (local automated) | CombatSaveTests, CombatCompatibilityTests, CombatTurnTests, CombatReadyTests; genuine frozen archives,46 pairs, every failure phase and atomic file retention |
| CBT-AC-011 Runtime/usability  | Verified (local automated) | Native Combat generated12 and labelled supplements at800/1920; Fleet/Scout/Crisis/Colony/Rival/Species/Expansion/Legacy retained regressions |
| CBT-AC-012 Full design  | Verified (design) | Verified for design only in ADR-020; owner approved full design and separate plan before implementation |

## Limits and disposition

UI signals exercise real controls/handlers, focus, scrolling and geometry. Embedded screenshots are supplementary and inherit canvas scaling. They do not prove physical input, player comprehension, accessibility, aesthetics, balance or long campaigns. Cross-platform determinism, dependency/security audits and export/release remain unverified. No new dependency was introduced. Parent REQ-017 and17 parent-row totals remain unchanged.

Main integration requires the owner's next disposition. No merge, push, release, evidence deletion or worktree cleanup is authorized by this implementation approval.

Final completion: [task16 gate](combat-execution/gates/task16-done-01.log) passes1733/1733. [Final ledger](combat-execution/progress-final.md), [all36 rulings with reasons/costs](combat-execution/rulings.md). Deferred minors: none. The isolated branch/worktree and all evidence remain; no integration/push/release/cleanup.
