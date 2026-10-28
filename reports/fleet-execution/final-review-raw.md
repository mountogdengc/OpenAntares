**Technical verdict: not ready to merge.** I found four Important defects and four Minor gaps. No Critical findings.

This was a read-only review of the complete `c554797..2464d1f` feature range, binding design, plan contracts, ADR-018, every ledger `Ruling:`, production integration, relevant tests, and recorded validation. I also reproduced the four Important defects with isolated, in-memory probes against the existing compiled simulation. I did not run builds or mutation-producing suites, write files, or change Git state.

**Strengths**

- The implementation substantially follows the approved design. Own-view planning, finite queue authority, shared construction selection, physical officer assignments, and committed travel remain coherent architectural boundaries.
- Transactional command and turn handling has strong fault-injection coverage, including two-human preparation/receipt rollback. Snapshot bonuses, promotion thresholds, once-only XP, founding consumption, and Wenth source-body retention receive meaningful tests.
- The recorded combined evidence is unusually thorough: 1,529 passing tests, eight native gates, generated paid construction across all four species, and preserved legacy sources/futures. Labelled supplements and provenance are distinguished from assertions.
- I evaluated the ledger’s administrative-retirement ruling rather than excluding it: permanently ending an appointment after existing colony/source-body destruction has a real gameplay cost, but conservatively preserves factual identity without inventing relocation, resurrection, or a new biological-death rule.

**Important issues**

1. **[P1] Automatic neutral withdrawal hits the manual-detachment guard and blocks Ready.**  
   [ExpansionCommands.cs:23](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/src/OpenAntares.Simulation/Commands/ExpansionCommands.cs:23)

   When an Avoid-encounters scout in a named fleet encounters a foreign vessel and has a legal safe fallback, `ScoutQueries` proposes an ordinary `SetMoveOrderCommand`. `ScoutTurnCoordinator` submits it through the ordinary command processor, where this new guard rejects it because the scout is fleet/request bound. Fleet safety normalization never gets the opportunity to perform the authorized separation.

   A valid-state probe produced the retreat proposal, then rejected Ready with “Fleet and survival preparation could not be completed”; the underlying rejection was “Explicitly detach this ship from its fleet/request first.” The forbidden-destination cancellation path uses the same ordinary command form.

   **Minimal fix:** provide a narrowly trusted path for the already-validated scout preparation moves/cancellations, retaining ordinary route/consent validation and subsequent fleet/request safety normalization. Keep the manual-command detachment rule. Add grouped and request-bound neutral-withdrawal/cancellation regressions through Ready and direct Resolve.

2. **[P2] Both cancellation choices fail for safety-paused requests with blocked slots.**  
   [AssemblyCommandProcessor.cs:116](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/src/OpenAntares.Simulation/Fleets/AssemblyCommandProcessor.cs:116)  
   [FleetStateValidator.cs:90](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/src/OpenAntares.Simulation/Fleets/FleetStateValidator.cs:90)

   Cancellation changes the request to `Cancelled` but retains its `BlockedSafety` slots and their `ResumeStatus`. Validation requires every such slot to belong to a `SafetyPaused` request, including historical inactive requests, so the entire command rolls back.

   Starting with a valid safety-paused request, both KeepQueue and RemoveUnstarted rejected with the generic coherence error and preserved the original reference. The player cannot cancel the blocked commitment while these slots remain, despite the explicit cancellation contract.

   **Minimal fix:** define coherent cancelled safety history—either normalize slot progress when cancellation ends live authority or explicitly validate frozen historical safety records. Preserve living hulls, committed transit, active projects, reserve, and unrelated work. Cover both cancellation choices with queued/built/travelling slots and save/reload.

3. **[P2] Changing rally after partial assembly strands retained ships in the former fleet.**  
   [AssemblyCommandProcessor.cs:93](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/src/OpenAntares.Simulation/Fleets/AssemblyCommandProcessor.cs:93)  
   [FleetPlanner.cs:25](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/src/OpenAntares.Simulation/Fleets/FleetPlanner.cs:25)

   After one slot has arrived and joined the original rally fleet, changing to another safe rally with a new reserved fleet slot is accepted. The slot becomes `Built`, but its vessel remains a member of the old fleet. Request dispatch then skips it because membership is non-null; the old fleet is Holding. The retained-work preview nevertheless permits the change and supplies an arrival estimate.

   In the probe, the command succeeded, planned zero moves, and three subsequent turns left the ship in the old fleet at the old system, with the request Active and no Attention. The same mismatch can obstruct recovery through an explicitly changed safety rally.

   **Minimal fix:** include retained fleet membership in rally transition planning and application. Move an eligible retained cohort coherently, or require a precise, previewed release step before accepting the change. Preserve officer assignments and committed transit. Add partial-arrival rally-change and safety-resume regressions, including reload.

4. **[P2] Strict loading accepts a Queued slot without its required queue token.**  
   [FleetStateValidator.cs:104](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/src/OpenAntares.Simulation/Fleets/FleetStateValidator.cs:104)

   Queue validation runs only when `QueueToken` is present. An active `Queued` slot with a null token bypasses the binding checks entirely. Clearing that token on an otherwise accepted request yielded zero validation errors and a successful serialize/deserialize round trip.

   This loads an unfulfillable reservation: no future completion can match its queue token, and preparation does not repair it. Remaining explicit construction can also become disconnected from the claimed slot.

   **Minimal fix:** require every effective `Queued` state—including safety-blocked slots whose retained progress is Queued—to have its participating colony, valid unique matching queue token, and no living vessel. Represent intentionally unallocated quota as Missing. Add malformed-save rejection and failed-load preservation cases.

**Minor issues**

1. **[P3] Return silently completes when its colony vanished during transit.**  
   [FleetPlanner.cs:42](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/src/OpenAntares.Simulation/Fleets/FleetPlanner.cs:42)

   Arrival equality produces Hold before the following line checks whether the Return destination remains an owned colony. Thus the first stationary boundary after arrival can conceal the lost destination instead of showing the specified Attention. Check Return ownership before treating arrival as successful completion; leave committed transit unchanged.

2. **[P3] The direct priority-edit control omits the promised queue/estimate preview.**  
   [AssemblyRequestDialog.cs:34](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/game/Components/AssemblyRequestDialog.cs:34)

   Set Normal/Urgent presents generic confirmation text and submits `SetPriority` directly. It does not show prior/proposed queue order or changed conditional estimates, although `PreviewRetained` supports the calculation. Use that preview for this control and display its consequences before confirmation. Underlying priority/authority preservation has good coverage; this is an information-contract gap.

3. **[P3] Fleet Command lacks the approved role filter.**  
   [FleetCommandDialog.cs:39](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/game/Components/FleetCommandDialog.cs:39)

   Role is available as a sort, but the filter contains only All/Idle/Unassigned/New/Travelling/Reserved/Attention. Players cannot isolate scouts or colony vessels before bulk selection as specified. Add a role filter alongside the existing status filter, preserving stable ordering.

4. **[P3] Assembly idle-candidate construction remains unbounded.**  
   [AssemblyRequestDialog.cs:64](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/colony-development-design/game/Components/AssemblyRequestDialog.cs:64)

   This method rebuilds one item for every eligible owned idle ship. The central ship list’s paging does not cover this separate assembly control, so the retained scale contract’s “no unbounded list/history construction” is incomplete. Page or virtualize these candidates while retaining explicit selections across pages, and exercise this path with the large roster. This is not a measured performance-failure claim.

**Recommendations**

Make the four Important fixes regression-first, preserving the existing evidence. Extend the relevant focused suites and ordinary combined gates as appropriate after those changes. Resolve the Minor gaps, or record their explicit acceptance disposition before claiming complete FLT conformance. No second architectural redesign is indicated.

Before the verdict, these are the behaviors I considered but did not grade as delivered M8 functionality:

- Combat damage, morale and future combat retreat; patrol/escort, refits/design policies, and automatic replenishment: explicitly deferred feature scope.
- New AI commissioning/request strategy and network transport: explicitly outside this slice; existing AI and human command parity were reviewed.
- Physical input reliability, human comprehension/comfortable 800×600 reading, performance targets, balance, and broader platform/export/release assurance: explicitly retained acceptance limits. Recorded layout/function evidence does not establish those outcomes.

I did **not** exclude strict persistence, ordinary scout safety withdrawal, colony-loss effects, or practical request recovery merely because the design was silent about a particular sequence.

**Ready to merge? No, technically.** The four Important defects should be fixed first. This verdict conveys no merge, push, release, or cleanup authorization.
