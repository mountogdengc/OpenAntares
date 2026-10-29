# Combat implementation validation — 2026-10-28

The approved16-task plan is executing on isolated `codex/fleet-combat`. New campaigns now use rules9, writer10 and46 explicitly supported schema/rules pairs. Main remains at `dc190937`, rules8/writer9/37 pairs. Automatic sentry battles, standing orders, persistent damage/cohesion, withdrawal cohorts and finite port repair are implemented. Optional tactical controls remain later scope.

This report is a task16 checkpoint. Independent review and final acceptance disposition are pending. Do not infer integration, release or human usability acceptance from these automated checks.

## Executed evidence

- [Task summaries and raw gates](combat-execution/) retain behavioral RED/GREEN, failures, fixes and exact commands. [Activation proof](combat-execution/task-15-summary.md) records12 paid generated campaigns, four actual Main reload stages and three-turn deterministic continuations per stage.
- Task16 simulation suite passes1688/1688, including alternate embedded role names across every saved combat phase, strict corrupt-load tests, all46 pairs, all13 rollback hooks and genuine old futures. [Suite log](combat-execution/gates/task16-suite-01.log).
- All nine graphical scenarios pass sequentially under Godot4.7.2 Windows x64: Legacy493, Expansion275, Species1194, Rival1194, Colony682, Crisis482, Scout240, Fleet1680 and Combat3163 result records (9403 total). Scout/Fleet/Combat additionally retain9/39/89 separate provenance records; some older scenarios include provenance in their result arrays. Every successful gate has0 engine diagnostics,0 build warnings/errors, an actual opt-in negative control and normal9-scenario/2-helper exclusions. See [machine summary](combat-execution/final-summary.json).
- The original rules8 capture has48 generated sources/192 generated checkpoints, ten supplemental sources and232 total future checkpoints. Expected futures were never regenerated; canonical comparisons remove only schema_version. Old rules1..7 fixtures also remain in the suite.
- Task16 hashes all293 raw archived rules8 files against their original manifest. Colony01/02 and Crisis01/02 retain failed former-current setup comparisons; final Colony03/Crisis03 explicitly load original8 sources through Main. The latter preserves all generated health/cargo/development assertions and corrects stale provenance labels. Dedicated current9 setup checks remain. The coarse runner's provisional summary counts only a checks array; final-summary audits each scenario's actual results/checks array and supersedes those provisional counts. Colony03's inherited prose says current form in one provenance entry; its actual explicit8 source/load/checks and JSONL are authoritative.
- [Preservation audit](combat-execution/task16-preservation.json) verifies7345 historical branch evidence/fixture hashes and all four protected owner files byte-exact. The primary checkout remains clean at accepted base. Of7821 worktree-entry hashes,7675 primary files match exactly;146 differ only by independently proved CRLF checkout conversion. A separate exact primary closing baseline is retained. All eight worktrees and existing recovery/branch references remain.

## Acceptance mapping

Each functional row is Implemented, Unverified pending the final review disposition. The following named classes and actual native evidence define the automated proof boundary; they do not replace human acceptance.

| Criterion | Executed evidence |
| --- | --- |
| CBT-AC-001 Setup/content | CombatContentTests, CombatGenerationTests, CombatStateTests; native current Standard/Off, invalid Standard/prior-session retention and12 species/size starts |
| CBT-AC-002 Production | CombatIntegrationTests, Fleet request/production regressions; twelve paid two-warship campaigns with exact reserve conservation |
| CBT-AC-003 Damage | CombatMathTests, CombatResolutionTests, CombatTurnTests; exact frozen shots/bands and post-interception launch |
| CBT-AC-004 Cohesion/orders | CombatCommandTests, CombatResolutionTests, CombatReportTests; native order changes, revised-force/blocked-return responses and Routine Off |
| CBT-AC-005 Withdrawal | CombatWithdrawalTests, CombatReadyTests, CombatTurnTests; clean/exposed return, blocked ports and actual reload futures |
| CBT-AC-006 Detachment | CombatWithdrawalTests, CombatIntegrationTests; labelled native main/detached cohort supplements |
| CBT-AC-007 Repair | CombatRepairTests, CombatReportTests; generated arrival/no-tick/full-stationary repair and30-ship bulk repair/Hold supplement |
| CBT-AC-008 Integration/privacy | CombatKnowledgeTests, CombatReadyTests, existing scout/crisis/rival/fleet suites; native competing-observation privacy supplement |
| CBT-AC-009 Officers | CombatIntegrationTests and unchanged Fleet recovery/XP suites; explicit mortal thresholds/snapshot equipment and Wenth whole-body link identity |
| CBT-AC-010 Transactions/compatibility | CombatSaveTests, CombatCompatibilityTests, CombatTurnTests, CombatReadyTests; genuine frozen archives,46 pairs, every failure phase and atomic file retention |
| CBT-AC-011 Runtime/usability | Native Combat generated12 and labelled supplements at800/1920; Fleet/Scout/Crisis/Colony/Rival/Species/Expansion/Legacy retained regressions |
| CBT-AC-012 Full design | Verified for design only in ADR-020; owner approved full design and separate plan before implementation |

## Limits and disposition

UI signals exercise real controls/handlers, focus, scrolling and geometry. Embedded screenshots are supplementary and inherit canvas scaling. They do not prove physical input, player comprehension, accessibility, aesthetics, balance or long campaigns. Cross-platform determinism, dependency/security audits and export/release remain unverified. No new dependency was introduced. Parent REQ-017 and17 parent-row totals remain unchanged.

Main integration requires the owner's next disposition. No merge, push, release, evidence deletion or worktree cleanup is authorized by this implementation approval.
