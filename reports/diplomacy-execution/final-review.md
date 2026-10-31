# One independent whole-branch implementation review

Reviewer: `/root/diplomacy_final_review`, fresh context, GPT-6 Astra/xhigh. Range `ada622f..403883a`; product `9ed0cbda82e213ce540f9e5a89adf2cd9222ed02`. Read-only checkout review, with isolated TEMP reproductions. Reviewer inspected approved design/plan/rulings, product, saves, projections/UI and supplied evidence. No test/build rerun in this checkout, no checkout changes, no additional reviewers.

## Strengths

Centralized production reservations correctly count exact-price earmarks once, atomically charge both sides, and preserve accounting identities. Activation, warmup, minimum and notice are separate phases. Requester projections generally preserve partner hubs/queued answers/third-party relations. Compatibility and earned/Main Save-Load evidence is substantial. Recorded product hashes match reviewed files; supplied logs show2130 tests and actual opt-in/ordinary11+2 checks.

## Important findings

1. **I1: multi-contact AI can abort the whole turn.** `DiplomacyTurnCoordinator.cs:23`, `DiplomacyTokens.cs:16`. Frozen-view revisions become stale after the first own trade reservation. A valid isolated three-empire reproduction planned OfferTrade then Answer; the second rejected `diplomacy_stale`, and coordinator/actual turn failed. Preserve frozen decisions while binding later commands to prior authorized own mutations; test multiple contacts/AIs.
2. **I2: no reservation-release construction alternative.** `DiplomacyReservations.cs:18`, `DiplomacyPreviews.cs:26`. Forecasts always hold pending earmarks and expose no conditional alternative. An unanswered final-boundary trade forecast did not complete fabrication_hall, but actual expiry released funds and completed it. Add requester-aware release assumptions without leaking hidden answers; extend the existing expiry forecast test.
3. **I3: live trade disappears from hypothetical forecasts.** `GovernorPlanningQueries.cs:58`, `RivalPlanningQueries.cs:77`. Own-only frames omit diplomacy; isolated live-trade own production500 became400 in unchanged-workforce governor/rival forecasts. Assembly estimates use governor forecasts. Carry permitted own contribution into those frames.
4. **I4: missing lifecycle information.** `DiplomacyCommandProcessor.cs:51`, `DiplomacyResolver.cs:46`, `TurnReportDialog.cs:120`. Incoming AI offers lack events; immediate withdrawal discards the normal withdrawn result; activation failure drops specific owner eligibility reasons. Publish bounded lifecycle results, owner-private versus partner-generic explanations, and contact-specific actionable report links.
5. **I5: ambiguous/misleading activation dates.** `DiplomacyPreviews.cs:17`, `DiplomacyResolver.cs:46`. Previews omit effective/first-payout dates. Published T+1 activation report incorrectly says next planning turn. Show explicit conditional acceptance/effective/earning boundaries.
6. **I6: own answer/hub cannot be reviewed.** `DiplomacyDialog.cs:27`. OwnAccept/OwnHub DTOs are unused; queued Accept and Decline look identical, and active trade omits income destination. Show exact own answer, hub name/location and payment/output terms without partner data.
7. **I7: accepted one-boundary mod lifetime is unanswerable for AI offers.** `DiplomacyCommandProcessor.cs:28`, `DiplomacyResolver.cs:36`. OfferLifetime1 AI offer expires in its creation/preparation boundary before publication. Isolated successful turn left no pending offer, only trade_expired. Every accepted lifetime needs a recipient planning opportunity.

## Reviewer Minor findings

1. **M1: supported near-limit save projection crashes.** `DiplomacyQueries.cs:24`. Validator-valid Save/Load at int.MaxValue-4 with newly active accord throws from ActivatedTurn+Minimum; warmup same pattern. Safe date arithmetic/coherent validation needed. Reviewer graded Minor due extreme date.
2. **M2: contact introduction/list summaries absent.** `DiplomacyContactPanel.cs:10`, `DiplomacyDialog.cs:24`. Generic text omits existing descriptions/peaceful settlement context. Contact rows omit both attitudes, pending status and agreements.
3. **M3: internal trade identifiers in breakdowns.** `BreakdownText.cs:49`. diplomacy_trade/diplomacy_setup_paid fall through as raw labels; use readable income/setup labels. Amounts themselves correctly separated.

## Five plan focus dispositions

1. Exact-price activation/earmark once: sound.
2. Queued founder retains intent/cargo, Attention once; travel remains legal. Named transit test is a disclosed stationary-arrival supplement because original schema prohibits transit plus pending founding.
3. Revoke releases earmark, preserves expiry and Save/Load; I2 is remaining forecast discrepancy.
4. Historical missing hubs load and terminate before income, owner-private/partner-generic reasons: sound.
5. Hidden partner plans preserve own revisions; seven failure seams retain original input: supported. I1 concerns own sequential mutations, not a foreign oracle.

Pair-local offer ID collisions were examined: owner+hub binding and unique-earmark validation prevent demonstrated false top-level preview approval/double payment. No invented finding.

## Declined to judge

- Literal simultaneous committed transit and PendingFounding is invalid under retained schema; assessed reachable travel/stationary blocked-founder behavior instead.
- Physical input/accessibility usability/balance/other platforms/packaging/security certification are outside supplied automated scope; no acceptance claim.

## Verdict

No Critical findings. **With fixes**: core accounting/privacy/rollback/legacy preservation is strong, but AI turn failure and forecast/report/UI gaps prevent full functional acceptance. One authorized regression-first correction pass, affected checks and source-specific evidence required.

## Primary severity/disposition before corrections

I1..I7 retained Important. M1 upgraded Important because a supported loaded state must open without crashing. M2 upgraded Important for multi-contact pending-action/attitude discovery; approved cultural context belongs to that contact presentation. M3 upgraded Important because nontechnical players must understand the paid production entries. These are functional acceptance gaps, not optional polishing; all enter the single pass. No deferred Minor findings.

I2 interpretation: the base conservative held-fund forecast must not consult hidden partner answers. Add an explicitly conditional release branch; do not turn private queued decline/acceptance into a forecast oracle. Current spendable funds remain earmarked until actual release.

Declined-scope rulings: preserve original transit/founding schema and disclosed supplement (cost if wrong: absent compound travel-intent behavior would need a separately approved model change); keep human/platform/balance/security/release acceptance unverified (cost if wrong: defects outside automated scope can remain). No merge/push/release/cleanup.
