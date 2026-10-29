### Strengths

- Combat remains isolated from Godot and largely contained in its own simulation subsystem. Simultaneous targeting, damage rounding, cohesion, and post-interception travel use deterministic calculations without new RNG draws.
- Withdrawal preserves pre-volley cohorts, detaches ships before flagship replacement, and recomputes surviving launch speed without repeating interception.
- Repair uses snapshot eligibility, preventing arrival healing. Existing request completion, officer recovery, and cargo-loss paths are reused.
- Compatibility evidence is unusually thorough: genuine old-engine futures, explicit schema pairs, immutable archives, alternate role names, and actual generated campaigns.
- Retained logs show **1,688/1,688 simulation tests** and **41/41 focused tests** passing. I checked all nine native result files: their counts total **9,403**, and each reports success. Normal/opt-in metadata confirms the expected nine scenario and two helper exclusions.

### Issues

#### Critical — Must Fix

None found.

#### Important — Should Fix

**I1. Saved encounters do not enforce civilian-risk authorization**

File: [CombatStateValidator.cs:55](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/fleet-combat/src/OpenAntares.Simulation/Combat/CombatStateValidator.cs:55)

The validator checks membership, orders, armed survivors, and revision bounds, but never requires `CivilianRiskAuthorized` when committed members include unarmed ships or embarked population. Ready validation only checks response/revision and return-route conditions.

I confirmed this using an in-memory fixture against the existing compiled assembly:

- A valid pending fleet containing an escort and settler passes validation with authorization.
- Changing only its authorization to `false` still produces no validation errors.
- Serialization and deserialization succeed.
- Direct resolution succeeds and fires round 1.

This is a malformed-save acceptance gap, rather than a bypass through the ordinary Attack command. It violates the required saved-consent invariant and can resume civilian combat without the saved authorization.

**Fix:** Validate civilian/cargo authorization against participating members and the permitted response state. Add corrupt-save and direct-resolution regressions proving rejected input retains the previous session/state.

**I2. Changing queued withdrawal orders forecasts the wrong next exchange**

Files: [CombatQueries.cs:99](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/fleet-combat/src/OpenAntares.Simulation/Combat/CombatQueries.cs:99), [CombatCommandProcessor.cs:39](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/fleet-combat/src/OpenAntares.Simulation/Combat/CombatCommandProcessor.cs:39)

The preview selects withdrawal calculations for `WithdrawCombat` or an unarmed force. It does not derive the resulting phase of `ChangeCombatOrders`. The command processor can retain `WithdrawalQueued`, while its accepted preview predicts an ordinary exchange.

Confirmed with the retained generated `harl-small/active.json`:

1. Issue ordinary Withdraw.
2. Change orders to Cautious, with On arrival: Hold.
3. The allowed preview predicts **two own shots**.
4. The accepted encounter remains `WithdrawalQueued`.
5. Actual resolution produces **zero own shots and one departure group**.

The “exact planning preview” therefore misrepresents firing, damage/cohesion consequences, and repair estimates during an ordinary supported operation.

**Fix:** Share the post-action phase decision between preview and execution. Cover changed return/arrival/straggler orders, unchanged queued orders, and Press/Hold changes that actually cancel withdrawal. Compare forecasts with actual next-boundary outcomes.

**I3. The dangerous combat Hold doctrine lacks its required warning**

Files: [CombatOrdersDialog.cs:21](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/fleet-combat/game/Components/CombatOrdersDialog.cs:21), [CombatQueries.cs:109](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/fleet-combat/src/OpenAntares.Simulation/Combat/CombatQueries.cs:109)

The selector labels this doctrine simply **“Hold”**, and the preview repeats that name without explaining that it ignores damage/cohesion retreat thresholds. Switching a queued withdrawal to Hold also resumes combat without explaining that it overrides withdrawal.

CBT-006 explicitly requires **Hold despite losses**, a manual opt-in warning, and an explanation when overriding a retreat warning. This matters because ordinary fleet Hold and arrival Hold mean stopping; combat Hold instead continues fighting despite losses.

**Fix:** Use an explicit label and show the consequences before Apply, including any cancellation of queued withdrawal. Add native text/control assertions for that warning. An additional confirmation dialog is unnecessary if the existing reviewed-command flow clearly states the consequences.

#### Minor — Nice to Have

None recorded.

### Review-focus assessment

1. **Health changes and return consent:** Missing-force invalidation, private quarantine forecasting, one defensive fallback, and specific next-period responses are covered. I1 remains a saved-authority gap.
2. **Interception and flagship loss:** Frozen cohorts, clean-cohort exemption, post-damage launch, main-only replacement, and existing officer recovery are implemented and tested.
3. **Multiple humans:** Reservation rejection is generic; preparation preserves earlier accepted planning; rollback tests cover the declared phase hooks.
4. **Old campaigns/current content:** Rules gates, role availability, omitted combat fields, and genuine continuation tests support compatibility. No observed old-rule formula/RNG change.
5. **Old construction requests:** Active reservations conflict with combat/repair; completed provenance stays historical; Missing slots and loss integration remain finite/idempotent. No automatic resend or duplicate completion credit found.

### Plan and ruling assessment

The recorded rulings are generally justified:

- Earlier introduction of shared types/projections and typed response flags supports dependencies without changing approved gameplay.
- Private quarantine forecasting, independent combined responses, and mandatory civilian withdrawal address real authority problems.
- Rejecting active request/combat overlap is consistent with the approved commitment restrictions; the separately labelled request-loss supplement is an honest proof boundary.
- Reattaching one embedded combat definition avoids conflicting receipt definitions.
- Explicit rules8 Main Load adapters preserve historical campaign assertions while separate current9 campaigns exercise new setup. They establish historical continuation, not repetition of the former Start path.
- Archive/CRLF and ledger-location rulings concern preservation and bookkeeping. The disclosed absence of an earlier exact primary-byte manifest should remain explicit.

None of these rulings justifies I1–I3.

### Recommendations

Address the three findings in the authorized regression-first correction pass. Keep affected acceptance criteria **Implemented, Unverified** until their regressions and relevant simulation/native gates pass.

This review was read-only. I ran no builds or writing tests; the two reproductions used the existing DLL and memory-only state. HEAD remains `24df8eae5d75a0d4054c502874ff0d7ef7c6bb28`, with a clean worktree.

### Declined to judge

- Tactical combat, empire warfare, additional rewards, and new opponent systems — explicitly excluded from this slice.
- Balance and long-campaign enjoyment — retained short campaigns do not establish these.
- Physical input comfort, human comprehension, accessibility, and aesthetics — native signals/text/geometry evidence cannot establish human acceptance.
- Cross-platform determinism — inspected execution evidence is Windows-specific.
- Dependency/security audit and export/release readiness — not established by these builds and gameplay checks.

No ordinary correctness issue was set aside merely because the specification did not enumerate its exact trigger.

### Assessment

**Ready to merge? With fixes — not at the reviewed HEAD.**

The core implementation and preservation work are strong, but civilian save validation, queued-order forecasts, and the missing Hold warning need correction before claiming full automated acceptance.
