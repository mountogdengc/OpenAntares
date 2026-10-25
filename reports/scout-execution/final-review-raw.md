### Strengths

- Shared preview and execution logic preserve exact consent when an automatic route becomes a pending order. Changed consequences invalidate receipts.
- Resolution correctly isolates owner RNG streams, guarantees no-roll Warning departure, removes consumed vessels before transport/founding, and deposits study research after ordinary research/cure.
- Save handling explicitly gates rules7/writer8, rejects missing or injected metadata, and preserves immutable history after vessel loss.
- Compatibility and validation evidence is substantial: the recorded suite has 1391 passes, all seven native scenarios passed with zero diagnostics, and normal assemblies exclude validation helpers. Reviewed source hashes match the evidence inventory.
- The three recorded implementation rulings are justified corrections within the approved scope.

### Issues

#### Critical

None found.

#### Important

**I1. Acknowledgement prevents an otherwise legal quarantine command**

- **References:** `src/OpenAntares.Simulation/Commands/CommandProcessor.cs:22`; `src/OpenAntares.Simulation/Commands/CrisisCommandProcessor.cs:143`; `src/OpenAntares.Simulation/Scouting/ScoutStateValidator.cs:108`.
- **Problem:** Crisis commands validate their changed candidate before the outer command pipeline reconciles ameba receipts. Cancelling an acknowledged cargo departure makes the old receipt inconsistent, so validation rejects the whole quarantine command.
- **Confirmed reproduction:** In a valid labelled cargo Warning state with a pending route to its open home port, quarantine previews one permitted cancellation. Without an ameba receipt, the command succeeds. After acknowledging that exact departure, the identical quarantine preview remains allowed but execution rejects with `invalid_crisis`. This was reproduced in memory using the existing simulation assembly; no files were written.
- **Effect:** Reviewing an ameba response unexpectedly removes the player’s ability to carry out a valid health response. Purge or other crisis changes that alter retreat consequences warrant the same regression coverage.
- **Fix:** Reconcile changed scout consequences before final candidate validation, once per command. Manual quarantine should cancel the route, retire its receipt and require a new response. Automatic quarantine during Ready must still roll back the enclosing Ready request when the resulting encounter lacks consent or a legal response.

**I2. Human scout preparation issues ordinary cargo cancellations outside its authority**

- **References:** `src/OpenAntares.Simulation/Scouting/ScoutQueries.cs:55`; `src/OpenAntares.Simulation/Scouting/ScoutQueries.cs:58`; `src/OpenAntares.Simulation/Scouting/ScoutTurnCoordinator.cs:22`.
- **Problem:** The cancellation branch runs for every owned vessel, including cargo with no scout metadata and no ameba encounter. The coordinator then budgets commands solely by scout count.
- **Confirmed reproduction:** A labelled rules7/Ameba Off state containing one cargo vessel with a pending route into a quarantined own port passes validation and a serialize/deserialize round trip. Preparation emits one cancellation. With zero scouts, Ready rejects with `resolution_failed`; adding an unrelated scout makes Ready succeed and clears the cargo route.
- **Qualification:** This is an accepted saved/custom-state reproduction. Ordinary quarantine and decontamination commands already cancel their affected routes; I did not establish this state through the normal generated command sequence.
- **Effect:** An accepted campaign’s ability to advance depends on the number of unrelated scouts, and scout preparation changes cargo orders despite having no encounter to resolve.
- **Fix:** Restrict human automatic movement/cancellation to eligible scouts. Assess cargo departure legality for encounter blocking without inventing cargo commands; preserve the existing crisis transport behavior outside encounters. Add coverage with zero and nonzero scout counts.

**I3. Automatic departure consent does not display the route being acknowledged**

- **References:** `game/Components/ScoutOverviewDialog.cs:41`; `game/Components/ScoutOverviewDialog.cs:48`; `src/OpenAntares.Simulation/Scouting/ScoutQueries.cs:68`.
- **Problem:** The Warning overview says “response departure” and offers “Queue departure” and grouped acknowledgement, but never displays the effective destination or travel duration. The destination appears only in the optional Exposed confirmation. The automatic retreat report also supplies generic wording and never explains when survival overrides neutral Avoid.
- **Effect:** A player can acknowledge and commit an automatic multi-turn route without seeing where it goes. An Avoid scout can travel toward peaceful foreign presence without the required explanation. Exact internal tokens do not provide informed player consent by themselves.
- **Fix:** Project and display the effective destination, ordinary duration, relevant observation freshness, and whether survival overrides neutral caution. Display the actual `response_required` state when no route exists. Assert these visible consequences in the actual-control tests.

#### Minor

**M1. Custom-map setup does not explain omitted habitat placement**

- **Reference:** `game/Screens/NewGameScreen.cs:124`.
- **Problem:** The summary always says Standard “may place” a danger. When fewer than two eligible habitat candidates remain, it does not explain that this campaign cannot place one, as SCT-007 requires.
- **Fix:** Use the completed setup preview to display a concise insufficient-habitat explanation, without revealing candidate locations.

### Review Focus results

1. **Quarantine during Ready:** The no-roll/no-spent-warning protection is present. Response validation and downstream departure verification prevent stationary safety from being published. However, the current tests do not directly exercise delegated quarantine during Ready, and I1 exposes the adjacent manual-command ordering defect.
2. **Exact automatic-route receipt:** Simulation behavior passes inspection: materializing the identical route preserves its token, changed routes invalidate consent, and already-ready humans are skipped during final preparation. Player-facing disclosure remains incomplete under I3.
3. **Study research timing:** Pass. Awards are deposited after normal technology/cure processing; the reserve and cure tests support the ordering. No second completion or current-boundary diversion was found.
4. **Neutral fallback/last colony:** Selection and privacy protections pass inspection. Known habitats remain excluded, neutral caution yields only when necessary, and only AI may select the explicit blind emergency route. The missing override explanation is covered by I3.
5. **Slow Exposed scout/retired vessel:** Pass. Slow fixtures modify embedded scout speed; Exposed validation requires scout capability and a successful study claim. History and save handling retain immutable retired facts instead of requiring a live vessel.

### Recommendations

Resolve I1–I3 in the planned regression-first correction pass. Keep M1 explicitly deferred if it is not included. Rerun the affected simulation and Scout/Crisis control gates, then the required final acceptance checks.

### Declined to judge

No considered gameplay behavior was set aside as outside the plan or specification.

Physical OS input, human comprehension/accessibility, subjective balance, other platforms, hostile-input/security audits, clean export and release remain unverified under the already documented evidence limits.

### Assessment

**Ready to merge? With fixes.**

The core simulation, compatibility and persistence work is well supported, but the three Important findings prevent final acceptance. This was a read-only review of the pinned range: no checkout changes, report writes, writing test runs, or additional reviewers were used.
