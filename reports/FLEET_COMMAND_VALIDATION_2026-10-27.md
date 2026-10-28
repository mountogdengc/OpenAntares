# Fleet command and named admirals — M8 validation

2026-10-28, Windows/.NET8/Godot4.7.2 .NET Compatibility/NVIDIA3090. Source `d60c7fe4da8d5480cbdded715f2b2e109e203238`. Owner approved complete design ADR-018 and separate17-task plan. Native inline tasks1..16 formally complete; combined Task17 gates pass, one fresh whole-branch review remains pending. M8-I and FLT functional acceptance remain In Progress until disposition. No merge/push/release/cleanup authority.

## Result and provenance

Rules8/writer9/37 explicit accepted schema/rules pairs active for new games, preserving Harl/One Peldra/Standard plague/Standard ameba. Older loaded campaigns retain their original rules/content; no(8,8) or implicit upgrade. Full Suite1529/1529, build0 warnings/errors. Every native runner performs real opt-in metadata negative control and normal rebuild0 warnings/errors, excluding all8 scenarios and both actual helpers. Both engine and process logs audited for errors/unhandled exceptions; zero on final successful runs. Native protocols dispatch actual controls/signals in process; they do not prove physical device input or player understanding.

| Fresh native scenario | Passed records | Separate provenance records | Evidence |
| --- | ---: | ---: | --- |
| Legacy | 493 | 0 | [legacy-01](fleet-execution/final/legacy-01/results.json) |
| Expansion | 275 | 0 | [expansion-01](fleet-execution/final/expansion-01/results.json) |
| Species | 1194 | 0 | [species-02](fleet-execution/final/species-02/results.json) |
| Rival | 1158 | 0 | [rival-01](fleet-execution/final/rival-01/results.json) |
| Colony | 670 | 0 | [colony-01](fleet-execution/final/colony-01/results.json) |
| Crisis | 464 | 0 | [crisis-01](fleet-execution/final/crisis-01/results.json) |
| Scout | 237 | 9 | [scout-02](fleet-execution/final/scout-02/results.json) |
| Fleet | 1338 | 27 | [fleet-01](fleet-execution/final/fleet-01/results.json) |

Total 5829 records in the eight final results arrays; separately named provenance rows are not extra assertions. Per-run source fingerprints and exact commands are in final-summary.json. Earlier failed runs remain retained. Task17 changed only stale current-default expectations in Species/Scout QA; unaffected Legacy/Expansion source/runtime behavior is unchanged. No product rules changed during combined validation.

Actual generated Fleet New Game workflows at800/1920 pay two Harl settlers, legally found3 colonies, grow all3 productive and pay/assemble4 new scouts across3 yards. Pending/completed quotas use actual Main Save/Load, followed by3 future boundaries equal to independent continuations cloned from the real save. All4 species separately pay/assemble a scout, pause/resume a contribution, inspect cultural defaults and use Explore, with UI reload/futures. Untouched default setup is independently checked. No live outcome injection, reroll or artificially granted resources.

Rare recovery thresholds/ranks, labelled pre-session Warning/officer/Wenth founding, retained unsafe-rally resume,30col/100ship/20fleet scale and151ship/70ordinary-cancelled-request pagination remain explicitly labelled. They are functional supplements, not generated economy, earned luck, balance or performance evidence. Scale mixes actual manual/governor controls and retains living request authority across3 reload futures. UI rows are bounded100 ships/32 requests/100 events per page; campaign history is retained. Career bounds32 highlights/10 completed-turn details/64 lifetime officers remain strict. Representative PNGs inspected at both sizes; the existing1920 canvas scales down at800, so comfortable reading is not claimed.

## Exact acceptance evidence map

| Criterion | Evidence checked | Status before review |
| --- | --- | --- |
| FLT-AC-001 central command/direct authority | FleetCommandTests, FleetQueryTests; native central entry/filters/sort/stale atomic confirmations/inspection | Implemented, Unverified |
| FLT-AC-002 persistent fleets/cohorts/founding | FleetStateTests/TravelTests/CommandTests/ResolutionTests/RecoveryTests/SaveTests; native explicit separate Wenth founding | Implemented, Unverified |
| FLT-AC-003 finite requests/safety/priority | FleetPlanningTests/RequestTests/EconomyTests/ReadyTests/ResolutionTests/SaveTests; actual3yard4scout quota and retained safety resume | Implemented, Unverified |
| FLT-AC-004 cargo/manual/governor authority | FleetOfficerTests/PlanningTests/RequestTests/EconomyTests; cargo consent, paused yards, active token/reserve/manual takeover and mixed scale controls | Implemented, Unverified |
| FLT-AC-005 standing orders/knowledge/consent | FleetQueryTests/ReadyTests/ResolutionTests/ProgressionTests; all-species Explore, material officer/hull consequences and exact acknowledgement | Implemented, Unverified |
| FLT-AC-006 named availability/identity | FleetContentTests/StateTests/OfficerTests/RecoveryTests/SaveTests; commissions, co-located transfers, Wenth source-body founding/history | Implemented, Unverified |
| FLT-AC-007 XP/bonuses/dedup | FleetTravelTests/EconomyTests/ProgressionTests/AiTests; pre-event ranks, once-only surveys/study/original hull XP, no reuse/cancel farming | Implemented, Unverified |
| FLT-AC-008 culture/loss/research | FleetContentTests/OfficerTests/RecoveryTests/SaveTests; basic/researched snapshot thresholds/deadlines, no-colony Missing, Last watch/Wenth no draw, displayed risk | Implemented, Unverified |
| FLT-AC-009 atomic readiness/resolution | FleetCommandTests/OfficerTests/RequestTests/ReadyTests/ResolutionTests/ProgressionTests/RecoveryTests;12 late fault phases,2humans/direct parity, exact original rollback | Implemented, Unverified |
| FLT-AC-010 saves/legacy/RNG | FleetCompatibilityTests/GenerationTests/SaveTests/AiTests;37pairs,560 genuine rules7 future boundaries, original rules1..6 futures, strict bad-load/write preservation, separate recovery PCG | Implemented, Unverified |
| FLT-AC-011 combined integration/scale | Suite1529; eight final native scenarios; generated and labelled provenance above; bounded UI/careers, source/hash/log/metadata audits | Implemented, Unverified |
| FLT-AC-012 full gameplay approval | ADR-018, full design/starter-default approval | Verified (design only) |

## Preservation and limitations

Real isolated-index checkout proves2431 historical files:2430 original pre-source proof hashes plus Task1's documentation-only completion note exactly as committed ated749a9 (its appended gate followed the initial checkout proof; original note remains95a0231). Every frozen source/future/raw archive is byte-exact. A first audit correctly stopped on that note's older checksum; the corrected audit proves the already-committed update rather than changing any historical artifact. Primary main remainsc554797; four owner whole-file hashes/status and empty main index unchanged; all7 worktrees, unrelated branches, recovery/stashes/old QA retained. See final-preservation.json. No primary write or historical expectation regeneration.

Parent REQ-017/17-row totals remain12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred. Physical input/focus, comfortable reading/accessibility/comprehension, aesthetics, balance/long campaigns, other platforms, hostile-input/security/dependency audits, clean install/export and release remain unverified/deferred. Future combat morale/retreat/degraded ships remains a design note, outside M8. Next: single fresh independent implementation review and one regression-first correction pass if required; then owner disposition of isolated branch.
