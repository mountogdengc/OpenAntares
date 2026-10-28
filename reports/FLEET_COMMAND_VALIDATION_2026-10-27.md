# Fleet command and named admirals — final M8 validation

2026-10-28. Validated implementation source `36e6ec4144269f59d4056fc5ac834af03d76236d`, isolated `codex/fleet-command`. Approved ADR-018 design and17-task plan implemented; single fresh full-branch review and one regression-first correction pass complete. No open Critical/Important finding. M8-I is **Implemented, Unverified** for exact acceptance because three disclosed Minor contract gaps remain. No main merge/push/release/cleanup.

## Delivered behavior and verification

Central own Ships/Fleets/Requests/Reports, finite Normal/Urgent multi-yard requests, named Navigator/Surveyor/Organizer appointments,10/30XP passive unlocks/service records, editable recovery doctrine and equipment research are implemented. Wenth preserve whole-source-body identity. New campaigns rules8/writer9/37explicit schema/rules pairs retain Harl/One Peldra/Standard plague/Standard ameba; loaded rules1..7 do not upgrade. No(8,8). Future combat morale/retreat/damage remains a design note.

Corrected full Suite1545/1545, focused compatibility/late rollback73/73, normal Rebuild0warnings/errors and actual metadata8scenario+2helper exclusions pass. Five substantive fixes each have actual behavioral RED→GREEN: four neutral named/request Ready/direct cases, six safetyCancel cases, four partial-rally release/amendment cases, two malformed queued-token load cases, and the native priority before/after/conditional ETA/stale-confirmation case. See [verbatim review](fleet-execution/final-review-raw.md), [Root grading/disposition](fleet-execution/final-review.md), [machine summary](fleet-execution/final-summary.json). Reviewer initial technicalNo is retained; corrections are self-verified, no second independent review.

| Native scenario | Passed results-array records | Separate provenance records | Source scope | Evidence |
| --- | ---: | ---: | --- | --- |
| Legacy | 493 | 0 | Unchanged earlier combined | [legacy-01](fleet-execution/final/legacy-01/results.json) |
| Expansion | 275 | 0 | Unchanged earlier combined | [expansion-01](fleet-execution/final/expansion-01/results.json) |
| Species | 1194 | 0 | Unchanged earlier combined | [species-02](fleet-execution/final/species-02/results.json) |
| Rival | 1158 | 0 | Corrected fresh | [rival-green-01](fleet-execution/review/rival-green-01/results.json) |
| Colony | 670 | 0 | Unchanged earlier combined | [colony-01](fleet-execution/final/colony-01/results.json) |
| Crisis | 464 | 0 | Corrected fresh | [crisis-green-01](fleet-execution/review/crisis-green-01/results.json) |
| Scout | 237 | 9 | Corrected fresh | [scout-green-01](fleet-execution/review/scout-green-01/results.json) |
| Fleet | 1482 | 33 | Corrected fresh | [fleet-green-01](fleet-execution/review/fleet-green-01/results.json) |

Total 5973 results-array records in the selected8 gates; inline provenance is explicitly counted in machine summary and is not an extra assertion. Separate provenance rows likewise are not assertions. Initial pre-review combined8gates/1529Suite/5829records remain retained. Corrected Fleet1482checks+33provenance, Scout237, Crisis464 and Rival1158 are fresh after the simulation fix; unchanged Legacy493/Expansion275/Species1194/Colony670 remain earlier combined evidence. Exact per-run byte fingerprints and commands prevent claiming a same-head rerun of all8. Engine/process logs audited with no unexpected errors/unhandled exceptions in successful gates. Every runner verifies actual opt-in metadata rejection then normal rebuild0warnings/errors/8+2 absent; separate final normal check also passes.

Actual Fleet New Game workflows at800/1920 pay two Harl settlers, legally found3 colonies, grow3 productive shipyards and pay/assemble4 scouts across all3. Pending/completed requests use real Main Save/Load and compare3 future boundaries with an independent continuation cloned from the real save. All4 species separately complete paid construction/contribution pause-resume/culture/Explore and UI reload/futures. Untouched default setup independently checked. No live outcomes/resources injected or rerolled.

Labelled pre-session rare recovery/rank/Warning/officer/Wenth-founding cases,30col/100ship/20fleet mixed manual/governor scale,151ship/70ordinary-cancelled-request paging and partial-rally/safety cancellation supplements remain separate from generated economy/luck. Central visible pages100ships/32requests/100events and careers32highlights/10completed-turn details/64lifetime officers are verified. Assembly idle candidates remain the disclosed unpaged exception. Corrected native controls actually cancel both safety choices, reject a partial-rally retarget until explicit ordinary disband retains its hull, then resume and fulfill the quota with original+paid new hulls. Priority edits show before/after work/reserve/population/queues/conditional ETA and reject a stale same-turn confirmation atomically. Representative PNGs inspected atbothsizes; functional scroll geometry does not establish comfortable reading/physical input.

## Exact acceptance disposition

| Criterion | Evidence | Status |
| --- | --- | --- |
| FLT-AC-001 central command | Command/query tests and native all-own/status/sort/bulk/stale controls; role filter absent M3 | Implemented, Unverified |
| FLT-AC-002 fleets/cohorts/founding | State/travel/commands/resolution/recovery/save tests; native separate founding and retained-rally workflow | Verified |
| FLT-AC-003 finite requests | Planning/request/economy/ready/resolution/save tests; paid3yard4scout quota, priority preview, bothCancel/safety/explicitrelease/resume | Verified |
| FLT-AC-004 cargo/manual/governors | Officer/planning/request/economy tests; cargo consent, paid active work/tokens/reserve, manual override, mixed scale | Verified |
| FLT-AC-005 standing orders/knowledge | Query/ready/resolution/progression; corrected neutralReady/direct; missing lost-Return destination Attention M1 | Implemented, Unverified |
| FLT-AC-006 officers/identity | Content/state/officer/recovery/save; fixed commissions/physicaltransfer/Wenth source identity/founding/history | Verified |
| FLT-AC-007 XP/bonuses | Travel/economy/progression/AI; snapshot ranks, original once-only survey/study/hull awards, no farming | Verified |
| FLT-AC-008 loss/culture/research | Content/officer/recovery/save; thresholds/snapshot equipment/clocks/Missing/Lastwatch/Wenth no-draw; native consequences | Verified |
| FLT-AC-009 transactions | Command/officer/request/Ready/12phase faults/progression/recovery; exact enclosing original and direct/finalReady parity | Verified |
| FLT-AC-010 persistence/legacy/RNG | Strict37pairs/bad-load/write/futures; effectiveQueued binding required, unchanged560 genuine rules7 future boundaries and earlier rules1..6 | Verified |
| FLT-AC-011 combined/scale | All8 selected native gates/1545suite; generated and labelled evidence; unpaged separate idlechooser M4 | Implemented, Unverified |
| FLT-AC-012 full design | ADR-018 owner full gameplay/starter-default approval | Verified (design only) |

Verified means local automated/design scope, not the human/platform/release limits below. M8-I stays Implemented, Unverified; parent REQ-017 and17-row totals remain12Verified/3Implemented,Unverified/1InProgress/1Deferred. [All implementation rulings/costs](fleet-execution/implementation-decisions.md) include factual administrative retirement after existing colony/source-body loss, not an invented biological fate; and explicit rally release, not automatic commander displacement.

## Preservation and remaining limits

Primary main remainsc554797/rules7/writer8/29pairs. Four owner files retain exact SHA256/status, empty main index,7worktrees/unrelatedbranches/stashes retained. Actual isolated-index checkout2431historicalfiles:2430original pre-source checksums plus Task1's already committeded749a9completion prose note; all frozen sources/futures/raw archives unchanged. Fresh closing recheck passes, with 346 retained initial+corrected QA save files hashed, including deliberate invalid fixtures. [Original checkout proof](fleet-execution/final-preservation.json), [closing preservation](fleet-execution/closing-preservation.json), [QA hashes](fleet-execution/closing-qa-file-hashes.json), [corrected native hashes](fleet-execution/review-evidence-hashes.json).

Physical device input/focus, comfortable reading/accessibility/comprehension, aesthetics, balance/long campaigns, measured performance targets, other platforms, hostile-input/security/dependency audits, clean install/export and release remain unverified/deferred. No restore or vulnerability-audit claim from --no-restore builds. No main integration/push/release/cleanup inferred. Next action is owner disposition of the isolated branch and three disclosed Minor gaps before exact M8 closure/integration.
