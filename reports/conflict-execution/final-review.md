# Independent empire-conflict implementation review

Reviewer: fresh `gpt-6-astra`, xhigh, read-only, no delegation. Exactly one implementation review. Product range: `5df255bd76e2c394765448223164744a007ddafe..a22729424fccc431166affe9437968ed9016abf8`, plus the explicit campaign-tool addendum. The review did not certify the later correction source. Author correction evidence is recorded separately below and in the canonical execution ledger.

## Findings as received

Critical: None established. Important findings follow verbatim; file references identify the reviewed source.

1. **Foreign private orders leak through blockade forecasts.**

   `EmpireViewProjector.cs:26`, `ConflictOrderReconciliation.cs:16`.
   Victim forecasts obtain pressure from the mutable world. An opponent’s unsubmitted Move, repair, refit, or regroup immediately removes its blockade, changing the victim’s forecast from450 to801 before publication. This exposes private planning as a certain outcome. Preserve immediate authoritative lift semantics, but derive foreign pressure in requester views from published facts until publication.

2. **Detached-straggler loss penalizes the clean escaping main cohort.**

   `ConflictExchange.cs:89`, `ConflictWithdrawal.cs:46`.
   Interception excludes clean cohorts from shots, but `Forecast` calculates cohesion losses across the entire side. A400-speed main cohort escaping a300-speed enemy loses20 cohesion when its detached20-hull straggler is destroyed. The existing test checks the main hull, not its cohesion. Apply interception cohesion only to the participating/exposed cohort, preserving M9 behavior.

3. **Save validation accepts a forged live flagship.**

   `ConflictStateValidator.cs:97`, `ConflictFrame.cs:23`.
   A live side’s flagship need only appear in its member lists; it need not equal the actual fleet flagship. Changing a mixed defender’s persisted `side.flagship` from its protected civilian flagship to an armed member passes these checks. The frame then permits the civilian flagship’s Navigator bonus and misidentifies flagship casualties/cohesion effects. Validate the exact live fleet/flagship identity without requiring historical lost anchors to remain live.

4. **The AI peace cooldown also restricts humans.**

   `ConflictPeace.cs:19`, `ConflictPeace.cs:41`.
   Withdrawn, declined, or expired offers set a cooldown enforced for every controller. A human who withdraws a published offer cannot propose again for five boundaries. ECF-003 permits unconditional peace at any time; ECF-009 assigns the outgoing cooldown specifically to AI. No approving ruling was found. Keep the restriction in AI policy, or obtain an explicit design change.

5. **Conflict reports omit the actual shot breakdown.**

   `TurnReportDialog.cs:73`, `ConflictBattlePanel.cs:25`.
   Both report surfaces display aggregate hull loss only. No conflict UI reads the retained `Shots`; previews likewise show resulting hulls without individual shots. Players cannot inspect which hull fired at which target or the applied damage, despite ECF-004’s required readback. Add permitted per-shot and per-hull details to current and retained reports.

6. **The unfinished evidence checker can accept nonexistent or stale acceptance evidence.**

   `Verify-Evidence.py:31`, `Verify-Evidence.py:55`.
   It hashes only the supplied source subset. Native `source`/`evidence` and campaign Main-save fields merely need to be truthy. A complete-looking summary containing one unrelated hashed file and nonexistent native/Main paths can pass. Before using this for Task22, require complete authoritative source coverage, actual artifacts, verified results, and source linkage. This is a defect in the current preflight—not a claim that it has already certified the milestone.

## Required acceptance gaps as received

`EmpireConflictGraphicalValidation.cs:26` currently exercises seeded politics, response/withdrawal, and blockade reports. It never clicks Attack, Change orders, Blockade, or Lift controls. It also does not drive the seven earned Main campaign stages.

Therefore, successful execution of today’s scenario would still leave coverage missing. Native Tasks18/19, four genuine Main/UI campaigns, the complete twelve-scenario runtime matrix, activation, and final evidence closure remain **required and unverified**. Compilation and engine rehearsals cannot replace them.

Minor: None separately raised. Declined to judge: Empty. No behavior was silently set aside as outside scope. Native rendering, physical input, and earned Main acceptance remain unverified due the stated execution guard.

Review readiness: **Ready to merge: No.** Material privacy, combat, persistence, and player-report issues remain, alongside mandatory acceptance gaps. Review was read-only; no edits, tests, builds, UI actions, or delegation were performed.

## Review Focus dispositions as received

| Focus | Disposition |
| --- | --- |
| 1. Canonical crossed private peace consent | Satisfied in the inspected implementation: separate private intents, canonical resolution, withdrawal precedence, and owner-filtered tokens. |
| 2. Completing refit attacked | Satisfied in the inspected implementation and retained regression: old equipment, zero payment, retained reserve/queue, one volley, no repair tick. |
| 3.450 production everywhere with conditional outcomes | Arithmetic and shared consumers are consistent. Privacy incomplete: finding1 exposes foreign lifting orders. |
| 4. Mixed defense/sentry/departure loss accounting | Armed-only shots, sentry priority, and shared officer accounting are supported. Cohort isolation incomplete: finding2. |
| 5. Historical/near-limit saves and forged-state rejection | Wide dates and historical losses are supported. Forged live authority rejection incomplete: finding3. |

## Author disposition and one correction pass

All six findings retain Important severity by their actual effect. There are no deferred minors or declined-to-judge rulings. No second reviewer was requested.

| Finding | Correction and regression evidence |
| --- | --- |
| 1 | Persist published victim pressure separately from live private blockade orders. Raw authoritative lift remains immediate; requester forecasts and saved planning views remain unchanged until publication. Both Lift and Move regressions failed450/801, then passed. |
| 2 | Calculate interception cohesion within each departure cohort. The clean-main regression failed100/80, then passed100/100. Ordinary whole-side volleys retain their existing behavior. |
| 3 | Require a live named side’s flagship to match its actual fleet flagship. The forged mixed-defense save passed validation before the fix and is now rejected. Historical lost anchors remain supported. |
| 4 | Human unconditional peace is always eligible; only AI policy uses its embedded outgoing cooldown. Human re-offer regression failed before the fix; human and AI-specific cooldown tests now pass. |
| 5 | Persist public attack/band/armor calculations with each shot, validate their arithmetic, and display individual shots plus each hull’s before/after/capped loss in current and retained reports. Readback absence failed before implementation; save tampering and readback regressions now pass. |
| 6 | Require authoritative Git source coverage, raw/canonical identities, real hashed native/source/execution/assembly/Main/screenshot artifacts and source links. Main saves are loaded and concrete future commands replayed through the real simulation. Complete-looking invalid-summary tests failed before the correction; seven negative controls pass. |

Behavioral RED: shared job `502b380f21e14b158571d83fdaea1250` (six failures); initial correction GREEN: `38658750dffe4bd48bf265b552f7c826` (94/94). Full corrected Windows suite: `3037b49ecb4c4a98b8b9a807cccabc2a` (2400/2400, zero skipped). Evidence negative controls: RED `ccfe8c5d335747bc85db5034e8a60acb`, successful-exit GREEN `65ce05fa516d46d7810ee3ca469d1850`; later complete compile/metadata/control runs are indexed in the execution report.

The native coverage omission is addressed in source by actual Attack/Change/Blockade/Lift supplements and an explicitly enabled four-species Main replay. That replay starts through Main, exercises real conflict and Ready controls, saves/loads all seven phases and compares captured uninterrupted branches for three boundaries. Its source compiles; its native execution is still required. The default remains rules11 until the approved gates permit Task20. This disposition does not turn an unrun native/Main criterion into Verified acceptance.
