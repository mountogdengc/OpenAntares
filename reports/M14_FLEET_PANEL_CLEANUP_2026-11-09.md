# Fleet panel cleanup


## Fleet-list navigation cleanup, 2026-11-09

The owner supplied the missing marked screenshot. Hide the System/Empire/Fleets context tabs, Vessels/travel contents, Conflict orders and Colony overview/alerts buttons, plus their separators while the map fleet list is open. Retain the fleet rows, selected-fleet course summary, and independent attention/status controls. The normal context navigation is restored on clearing the map selection, clicking a system without a fleet selected, or opening Decisions; feature-specific buttons still respect the current rules version. No simulation, count semantics, split/merge or selection behavior changed. Verification is limited to source-path inspection and queued normal game builds; no new automated tests, full suite or campaign run for this reversible visibility edit. Physical presentation remains owner-observed. No commit/integration/cleanup.

The initial game build passed. Source inspection then identified that hiding the context tabs also required restoring them when Decisions is opened or a system is inspected without a fleet selection. Those directly affected paths were corrected, and the final game-only build passed with0warnings/errors. No automated interaction scenarios ran.

Final queued job8a7dfb1a904346f58453c6621937c122 passed. Ordinary validation namespace absent; normal source/DLL recorded under fleet-panel-cleanup-build01 and existing launcher refreshed. Diff whitespace check passed. Owner next step: stop old run, F5 in the same project, click the system flag. Expect fleet rows/course details without the marked navigation block. Right-click the map to restore the normal system view. Report whether the boxed controls are gone and the fleet view is clearer.
