# Empire-conflict design: fresh builder check and disposition

2026-10-31 America/Denver. One fresh read-only strategy-game software-engineer builder check of the [complete M12 proposal](../docs/superpowers/specs/2026-10-31-empire-conflict-design.md). This is a design check, not an implementation review. No code/gameplay tests, new implementation plan or product work.

Reviewer returned one material planning question:

> When an enemy attack interrupts a refit, does the colony retain all banked production with no refit payment, following M10 cancellation?

The initial draft incorrectly referred to paid partial progress. Existing M10 source/specification charges once at successful completion and has no spent partial-progress account. Author inspection identified the same mismatch while the check was underway. ECF-007 now explicitly cancels the unpaid job before its first defensive volley, preserves all reserve and installed equipment/unstarted queue, releases the production slot with automation paused, and grants no equipment, healing or second completion. Past completed refits remain paid and installed. This resolves the question from established project facts without a new economic mechanic or an owner question.

Author consistency refinements also specify military/ordinary planner scheduling and healthy-force thresholds, and normalize crossed peace intents to one canonical offer with explicit proposer Withdraw/respondent Revoke semantics. These are delegated drafter decisions, not additional independent review findings.

No second fresh review or reviewer reapproval is claimed. No unresolved owner-only planning question remains in the author's disposition. Full owner review of the complete proposal/defaults remains pending: M12-D/ECF-AC-012 In Progress; plan/implementation/functional acceptance Not Started. Numeric and strategic defaults are recommendations, not measured balance or approved gameplay.

Documentation consistency/link/status checks are recorded in [design validation](EMPIRE_CONFLICT_DESIGN_VALIDATION_2026-10-31.json). Active rules11/writer12/67 pairs and product873f320 remain unchanged. Earlier gameplay test counts retain their original source-scoped evidence; no fresh gameplay test is claimed here.
