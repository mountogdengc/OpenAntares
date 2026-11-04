# Layout trial — fresh review and disposition

One read-only review by a fresh agent examined the uncommitted UI trial against main5df255b and the approved mockup/plan. It ran no tests, edited no files and dispatched no other agents. Corrections were made inline and retained checks cover the affected behaviors; no second review was requested.

## Findings and dispositions

1. **Important — actual window scaling.** The prior1920x1080 fixed canvas made a real800x600 window shrink the wide arrangement; the test had hidden this by changing canvas dimensions. Trial `project.godot` now disables fixed-canvas shrinking. The regression changes only the window size and asserts that layout uses its physical coordinates. `green-02` retains the expected failure; final checks cover all four sizes/current+legacy.
2. **Important — invisible command errors.** Narrow detail pages hid the console that owned rejection feedback. `CommandFeedback` is now outside that console, immediately above persistent navigation. A stale real workforce control delivered after End Turn must reject visibly without changing the committed campaign. A separate legacy notice no longer overwrites that rejection. Alerts retain a navigation entry when the console is hidden.
3. **Important — lost Contacts route.** Current campaigns now expose Contacts alongside Diplomacy, preserving first-observed identities and known foreign vessel sightings from the existing requester projection.
4. **Important — empty legacy colony tools.** The unsupported all-colonies/plans/notifications link is hidden for pre-expansion campaigns. Direct legacy colony management remains available.
5. **Important — old harness compatibility.** Several old native scenarios assume standalone Save/Load toolbar buttons and always-visible details. This is explicitly disclosed as unexecuted/incompatible trial coverage, not silently counted as passing. A new actual-Main Game-menu Save/Load regression verifies the new path. The broader native helpers need adaptation before main adoption; the trial remains isolated until owner feel feedback.
6. **Preference isolation, regraded Important for reproducible verification.** Test screens now use missing/unique preference paths under their retained run folder. Existing owner completion/notification settings cannot suppress required follow-up controls; owner preferences are not written.

## Subsequent diagnostic correction

The first106-assertion run emitted shutdown diagnostics. Inspection found the diplomacy navigation button was never parented for legacy games. It is now always scene-owned and hidden when unsupported. The test tears down scenes, collects released managed resources and checks orphan-node count. The runner fails on engine-error/leak diagnostics. Final-02 passes107assertions with0engine diagnostics and no orphan controls, and restores the ordinary assembly. This was not a second review.

## Decisions retained for the trial

- Narrow management workspaces use the full window; the right console remains present on the galaxy map. Rejection text and an Alerts entry stay available across workspaces. Cost if this feels wrong: revise spacing/navigation after owner play feedback.
- Existing native suites are not mechanically rewritten merely to make old counts green. Cost: the trial is not ready for main adoption until those navigation helpers and rendered acceptance are updated/executed.

## Declined to judge

Rendered readability, physical input and whether the arrangement feels comfortable in play require the owner's trial. Headless control/geometry checks do not settle them. They are pending human acceptance, not a reason to block preparing the playable isolated experiment.
