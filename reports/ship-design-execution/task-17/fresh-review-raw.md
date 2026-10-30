### Strengths

The implementation has a sound core: immutable owner-local designs, explicit construction identities, shared checked calculations, late refit payment, and copied historical equipment facts. The changes preserve the existing command transaction and snapshot boundaries instead of introducing a separate mutation path.

The retained evidence is substantial and appropriately qualified. I inspected the reported **2,026/2,026 simulation**, **32/32 compatibility/atomicity**, and **11,384 native validation records plus 153 separate provenance entries**, together with relevant tests and source. I did not run builds/tests or alter files, the index, HEAD, or branches.

The ledger’s early interface handoffs and copied historical facts are justified by their consumers. I found no material departure in refit pricing, combat shot semantics, ownership boundaries, or old-campaign activation.

### Issues

#### Critical

None found.

#### Important

**I1 — Archiving an active build makes its project picker display “No project.”**

- **Source:** [ColonyPanel.cs:223](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/game/Components/ColonyPanel.cs:223), [ShipDesignQueries.cs:18](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/ShipDesignQueries.cs:18).
- **Trigger:** Select a nonstarter design as the colony’s active project, then archive that design.
- **Effect:** `BuildChoices` removes the archived design. Consequently, no row selects the still-active project, and the picker retains its initial “No project” selection. Production remains committed and can spend reserve to complete the ship. The progress text below shows numbers but does not restore the missing design identity. This confirms the author’s stated uncertainty.
- **Fix:** Retain a clearly labelled, selected-only “archived; committed” representation for the current project while continuing to exclude it from new choices. Keep explicit clearing/replacement available.
- **Regression:** At both supported window sizes, archive an active direct and queued design; assert the displayed identity and metadata still match the committed target, another colony cannot newly select it, and it completes exactly once.

#### Minor

**M1 — Weapon banks are not canonicalized when saving a recipe.**

- **Source:** [ShipDesignCommandProcessor.cs:23](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/ShipDesignCommandProcessor.cs:23), [ShipDesignStateValidator.cs:85](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/ShipDesignStateValidator.cs:85).
- **Trigger:** Save an unlocked Frigate with banks `pulse_bank, lance_bank`.
- **Effect:** The input order is stored and serialized unchanged, contrary to SHP-001’s ordinal canonical ordering. The opposite bank order is considered a different recipe by catalogue equality even though refit equality correctly treats those orders as equivalent.
- **Fix:** Canonicalize banks before preview comparison and publication, and enforce the same representation for embedded starting recipes and persisted recipes.
- **Regression:** Both bank orders produce the same stored canonical recipe; submitting the same name/equipment with reversed banks at the current revision remains unchanged and allocates nothing.

**M2 — Optional component arithmetic can escape content-load validation.**

- **Source:** [ShipDesignContentReader.cs:63](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/ShipDesignContentReader.cs:63), [ShipDesignContentReader.cs:70](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/ShipDesignContentReader.cs:70).
- **Trigger:** Set the nonstarting `vector_drive.cost` to `long.MaxValue`, leaving the starting recipe unchanged.
- **Effect:** The component passes its nonnegative check, and only the starting recipe receives checked aggregate validation. The pack therefore loads, but every design using that engine overflows when selected. Saving safely rejects; the defect is late detection and missing actionable load diagnostics for an unusable component.
- **Fix:** Apply declared arithmetic bounds or validate legal nonstarting combinations sufficiently to reject definitions that cannot produce representable stats/costs.
- **Regression:** A nonstarting overflowing engine cost and an overflowing engine/frame speed combination fail content load with the relevant definition/field identified; a valid basic-only pack still loads.

**M3 — Inspection and refit comparison omit the actual equipment summary.**

- **Source:** [ShipDesignDialog.cs:41](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/game/Components/ShipDesignDialog.cs:41), [RefitDialog.cs:43](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/game/Components/RefitDialog.cs:43), [VesselPanel.cs:31](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/game/Components/VesselPanel.cs:31).
- **Trigger:** Inspect a saved or installed design, or compare a refit target.
- **Effect:** These views show names, frames and aggregate statistics, but omit engine, armor package and weapon-bank identities/counts. Inspecting the recipe requires opening a copy in the editor. SHP-006/008 explicitly call for equipment summaries and component changes before refit acceptance.
- **Fix:** Render the existing immutable recipe facts in catalogue/ship inspection and show old-to-target equipment changes in the refit preview, including “None” and repeated-bank counts.
- **Regression:** Inspect Standard, Gun and a mixed-bank Frigate—including an archived installed design—and assert component labels/counts appear without entering edit mode. Verify the refit comparison names both removed and installed components.

**M4 — The planned colony-side designer entry is absent.**

- **Source:** [ColonyPanel.cs:98](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/game/Components/ColonyPanel.cs:98), [GameScreen.cs:114](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/game/Screens/GameScreen.cs:114).
- **Trigger:** Open colony construction and look for the SHP-008 “Ship designs” entry from the warship picker.
- **Effect:** The colony exposes existing designs but offers no route to create or inspect one there. The implemented entry exists only through Fleet Command.
- **Fix:** Add the colony-side entry and connect it to the existing catalogue flow.
- **Regression:** Open the catalogue from colony construction at both supported sizes, save a design, and verify it becomes selectable without changing the colony’s current project or reserve.

These are source-confirmed findings. The suggested regressions were not executed during this read-only review.

### Review-focus disposition

1. **Archived commitments — simulation condition stands; presentation has I1.** Existing queued targets retain their identity; new choices are rejected; live references prevent deletion. Accepted refits allow archived targets at completion. See [ShipDesignTargets.cs:24](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/ShipDesignTargets.cs:24), [ShipDesignCatalogue.cs:11](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/ShipDesignCatalogue.cs:11), and [RefitResolver.cs:19](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/RefitResolver.cs:19).

2. **Completed requests after refit — stands.** Retained slots keep the original design; exact current-design equality applies to active bindings. Refit completion changes the vessel’s design without routing through new-hull fulfillment/XP. See [FleetStateValidator.cs:87](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/Fleets/FleetStateValidator.cs:87) and [RefitResolver.cs:46](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/RefitResolver.cs:46).

3. **Installed-stat integration — stands for the traced live paths.** Combat, withdrawal, repair and actual travel resolve installed profiles; committed transit retains its frozen values. M7 risk calculations deliberately retain role speed. The remaining role-only scout planning helper has no live `TravelTurns` caller. See [VesselLoadouts.cs:11](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/VesselLoadouts.cs:11), [CombatWithdrawal.cs:18](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/Combat/CombatWithdrawal.cs:18), and [AmebaResolver.cs:18](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/Scouting/AmebaResolver.cs:18).

4. **Late refit invalidation — stands.** Forecasting banks output without a payment. Late checks distinguish loss cancellation, eligibility pause and inconsistent accounting; no fallback colony completion is attempted. See [RefitEconomy.cs:8](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/RefitEconomy.cs:8), [RefitResolver.cs:25](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/RefitResolver.cs:25), and [TurnResolver.cs:207](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/Turns/TurnResolver.cs:207).

5. **Privacy, stale commands and final-Ready rollback — stands within the simulation boundary.** Tokens derive from permitted owner facts; stale ownership/material changes reject. New-job allocation invalidates cancelled-start replays. The retained tests exercise every new phase through direct resolution and final-human Ready. See [RefitQueries.cs:72](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/src/OpenAntares.Simulation/ShipDesigns/RefitQueries.cs:72) and [ShipDesignAtomicityTests.cs:20](C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/ship-design/tests/OpenAntares.Simulation.Tests/ShipDesignAtomicityTests.cs:20).

### Recommendations

Fix I1 in the authorized correction pass. Resolve or explicitly retain the Minor deviations rather than claiming exact full-spec acceptance while they remain open. Add the proposed focused regressions, then run the required suite and affected native checks; no additional reviewer is needed.

### Declined to judge

- Physical mouse/keyboard operation, reading comfort, accessibility, comprehension and aesthetics: control signals and geometry evidence do not establish these.
- General balance, optimal loadouts and long-campaign pacing: prototype values and four bounded campaigns do not establish them.
- Linux/macOS, ARM and cross-platform runtime determinism: no corresponding execution evidence was supplied.
- Security and dependency health: this was an implementation review, not a security/dependency audit; `--no-restore` gates do not provide one.
- Export packaging, distribution and release readiness: outside the authorized work and supplied verification.
- The additional own-design fleet filter: treated as optional under SHP-005, not a required missing feature.
- Independent certification of every preserved file’s bytes: I inspected the preservation audit and archive evidence but did not independently rehash all primary files, old QA saves and frozen checkpoints.
- New gameplay outside M10—automatic upgrades, cross-frame conversion, empire warfare, peaceful-mode redesign and automatic repair during a refit: explicitly excluded or governed by retained contracts.

### Assessment

**Ready to merge? With fixes.**

The core simulation, transaction and compatibility work is strong, and no Critical defect was found. Correct the misleading archived-project display and explicitly dispose of the four Minor deviations before presenting the milestone as fully conformant; this assessment does not authorize integration, push, release or cleanup.
