# Independent M6 whole-branch review — 2026-10-25

Read-only fresh `gpt-6-astra`/xhigh review of `42439de037fa27bf90d24d6fb34a0a36d340236c..ed308b8911e7bd3b76d04e9b86052385076522b9` against the approved plague specification, ADR-014, and all fourteen plan tasks. The reviewer inspected product, tests, and retained evidence; made no builds, tests, edits, or subagent calls. The following records the raw findings before the executor's correction pass.

## Strengths and evidence

Frozen ports, cure suppression of retagging, post-embark mortality, delayed episode closure, and immutable removed-colony report locations handle the difficult phase boundaries. Purge confirmation compares full structural consequences, including other cases, civic state, assets, carriers, orders, and surviving founders. Cure allocation rounds once after empire aggregation and preserves excess research and existing reserves. Ownership filtering, private preparation, rollback hooks, explicit save pairs, and genuine rules5 continuation provide meaningful compatibility and atomicity evidence. The report distinguishes generated campaigns, labelled fixtures, automated controls, and unverified human/platform acceptance.

The reviewer checked six retained `results.json` hashes against the machine summary; all matched and reported passing, with all listed QA paths present. Logs showed1,264 full and283 Crisis-filter simulation tests passing, ordinary rebuild zero warnings/errors, and all six graphical harness classes absent from the ordinary assembly. Representative800/1920 overview, purge, and carrier-hold PNGs were inspected. These results did not cover the interaction combinations below.

## Critical

None.

## Important

1. **Lost-colony completion action targets a surviving colony.** `game/Components/TurnReportDialog.cs:123`, `game/Screens/GameScreen.cs:171,249`. A colony can complete a project then die in the same resolution, leaving its historical completion and a **Choose next project** action. If a survivor is selected, `SelectColony(lost)` silently returns and `ChooseProject` opens the survivor's picker. A reopened report after purge has the same risk. Suppress the obsolete action, retain historical completion and vessel navigation, and guard the callback. `CrisisReportTests.CompletionThenColonyLossStillReportsExactlyOnce` covered only the DTO; graphical N1 purge did not cover completion plus loss. Review Focus1 and CRI-AC-008 remained open.
2. **Waiver disables local Interrupt.** `game/Screens/GameScreen.cs:110,235`, `src/OpenAntares.Simulation/Crises/CrisisReports.cs:53,61`. With Interrupt retained and acknowledgement explicitly waived, new exposure/escalation creates no pending receipt. The screen derives new-material identity and deferred opening entirely from pending receipts, so it never interrupts. Display preference and saved gate must be independent. Waiver and FreshInterrupt graphical tests were separate. Review Focus5 and CRI-AC-007/008 remained open.
3. **Inspection and quarantine omit order consequences.** `game/Components/CrisisOverviewDialog.cs:42`, `game/Components/CrisisResponseDialog.cs:29`, `src/OpenAntares.Simulation/Commands/CrisisCommandProcessor.cs:115`. Clicking **Inspect vessel** immediately submits decontamination, clears pending move/founding and imposes a full hold, without a preview/cancel step or continuing open-port contamination warning. Quarantine preview shows only a cancellation count, despite exact `CancelledOrders`. Display every affected vessel/destination and obtain explicit confirmation. Existing command test injected facts directly; labelled port test began with an already held vessel. CRI-AC-005/008 remained open.
4. **Late quarantine omits imminent colony-loss warning.** `game/Components/CrisisOverviewDialog.cs:47`, `game/Components/CrisisResponseDialog.cs:28`, `src/OpenAntares.Simulation/Crises/CrisisQueries.cs:46`. An N1 Active severity80/age3 case with no finishing cure has `ColonyLossRisk=true`, but overview says only generic population risk and quarantine confirmation omits that the next resolution will destroy colony/assets. The approved late-quarantine example requires an explicit warning before Ready. CRI-AC-002/008 remained open.
5. **Edited content's civic penalty is misstated before purge.** `game/Components/CrisisResponseDialog.cs:19`, `src/OpenAntares.Simulation/Crises/CrisisEconomy.cs:28`. With valid `civic_factor_loss_bp=2000`, first purge confirmation promises10% (`NextCivicLevel*10`) while actual production/research penalty is20%. Include exact content-derived factor in immutable preview and render it. Core-default tests missed this supported content path. CRI-AC-003/006 remained open.

## Minor

1. Authority UI offered0% in `CrisisAuthorityDialog.cs:10`, while `CrisisCommandProcessor.cs:59` accepted only25/50/100; applying0 closed the dialog with a generic rejection. Keep0 only for manual cure allocation.
2. Carrier-only episode lost its Critical badge in `ColonyList.cs:24` after cases and receipts closed, even though an infectious vessel could spread the episode. Include carrier threats.
3. Current-condition row in `CrisisOverviewDialog.cs:47` called Warning2's six Active intervals “recovery6,” omitting two warning intervals, and omitted available immunity expiry. Clarify countdown and protection.

## Five review-focus dispositions before correction

1. Completion/embarkation then colony loss: simulation/report preservation strong; Important1 blocked UI closure.
2. Cure/recovery/auto-release and transport: no defect found; frozen ports, retained departure flags, cure suppression, sterile boundaries, episode closure are implemented/tested.
3. Stale destructive consequences: no command-validation defect found; full structural comparison covers cross-colony civic/port/carrier/order/asset/last-colony facts.
4. Aggregate rounding/final spill/N1 cure: no defect found; shared calculations and focused tests cover arithmetic/precedence.
5. Digest/local Off/reload/other humans/old receipts: authoritative gate, waiver provenance and reload preservation were sound; Important2 exposed display/gate coupling.

## Considered and set aside

Human comprehension, comfortable reading, accessibility, aesthetics and long-campaign balance remained outside automated acceptance and ADR-005 defers the uncoached playtest. Other platforms, physical-device input, clean export/install and release qualification were outside the supplied Windows evidence. Hostile-input fuzzing and a comprehensive security audit were not performed. Culture-specific civic reactions, additional crisis families, foreign transmission, warfare and automatic victory/defeat were explicitly deferred. Reconstructing every vessel's last infectious port after history expiry was an approved carrier-model limitation recorded in the execution ledger.

The review concluded **not ready for exact M6 acceptance or integration** before correcting the five Important presentation defects and explicitly fixing or deferring the three Minor gaps. No integration was authorized by the review.
