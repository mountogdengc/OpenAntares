# Fleet combat implementation plan review

2026-10-28. **M9-D/CBT-AC-012 Verified for full design; M9-P In Progress pending owner review; implementation/functional criteria Not Started.** [ADR-020](../docs/DECISIONS/ADR-020-approved-fleet-combat-design.md) records the owner's “looks good” approval. This report accompanies the separate [16-task plan](../docs/superpowers/plans/2026-10-28-fleet-combat-damage-and-withdrawal.md).

## Planning decisions and source checks

The plan preserves Native inline execution, no per-task agents and one final fresh whole-branch implementation reviewer. No method or gameplay re-approval is requested. Implementation begins only after review of the concrete plan. The plan's first task freezes genuine rules8 behavior before product changes; genuine future capture has not run during planning.

Inspected current generation/settings/initialization, Rules feature predicates, ContentSet/load/write, GameState/VesselState/clone, shared command/Ready paths, FleetTurnCoordinator, FleetResolutionContext/FleetResolver, travel/bonuses, officer recovery, founding validation, sole rival planning context, observation/view projections, strict FleetSaveData and root/expansion save codecs, content loading/NewGame/debug launch, baseline helpers and the opt-in Godot runner/targets/metadata checker. Current main `e457166` had no local changes at planning entry and retained seven worktrees; its local documentation commit was ahead of cached origin/main by one. No fresh remote fetch is claimed.

Material adapter decisions pinned by actual source:

- `Rules.HasFleets` currently recognizes8 only; rules9 must explicitly retain every older feature predicate without changing old-version membership.
- `SaveGame.Deserialize(string)` and `ReadFile(string)` are the existing read APIs. Legacy comparisons use `CompatibilityFixtures.CanonicalWithoutSchema`, removing only schema_version while preserving embedded content and full state. The capture tool implements the equivalent canonical wrapper without referencing the xUnit assembly.
- `MovementResolver` currently reads snapshot PendingMove and throws for an unexpectedly missing candidate vessel. Combat consumed IDs must be skipped first; explicit combat departures must be recognized even when snapshot PendingMove is null, using post-interception speed rather than stale captured M8 launch values.
- Existing FleetResolver/OfficerRecovery normalization and snapshot equipment must receive each combat loss once. Empty-main/detached-survivor outcomes cannot inherit an officer or create a replacement fleet automatically.
- The existing guarded ameba founding check occurs after knowledge checks. Sentry founding validation must preserve that information boundary and expose only owned observations to the one existing rival controller.
- Current normal-build exclusions cover eight graphical scenario classes and two helpers. Combat adds the ninth scenario while preserving actual opt-in negative-control and normal-assembly checks. Root project already excludes `tools/**` from compilation, so a small baseline tool does not need a presentation-project change.

## Author self-review

This is the writing-plans skill's required author review; no planning reviewer agent or product implementation review was dispatched.

| Check | Disposition |
| --- | --- |
| Spec coverage | All13 CBT contracts and12 acceptance criteria map to task owners; setup/content, combat/withdrawal/repair, officers/cargo, knowledge/AI, transactions, compatibility and actual UI campaigns have explicit evidence gates. |
| Step scan | All16 tasks have independently testable outcomes and explicit files/interfaces/verification. Tasks1..15 include representative executable behavioral assertions; Task16 is the combined verification/review/disposition gate. No missing-method compiler failure is claimed as behavioral RED. |
| Type consistency | Shared actions/orders/frames/departures/state are defined once. Uncommitted attack frames allow nullable EncounterId. Departure Members/Speed and repair arrival choice are pinned; public previews consume only EmpireView. |
| Review Focus | Five concrete failure/input classes map to named owning tests: post-Ready health invalidation, frozen interception groups/flagship loss, competing human reservations, old saves with current content, and stale assembly bindings. |
| Proportion | The plan describes files, interfaces, numeric assertions and boundaries rather than full implementation bodies; its additional detail is execution order and verification. |
| Baseline integrity | Planned48 generated rules8 source campaigns and192 old-engine checkpoints are separate from retained UI and synthetic sources. Canonical comparison removes only writer schema numbering; no expected hashes are regenerated with the new engine. |

The author corrected ambiguous API naming, made the uncommitted-preview frame explicit, and pinned canonical legacy comparison and source inventory before handoff. The first provenance check failed because several initially listed JSON artifacts were view snapshots rather than save documents. Inspecting their format/schema/rules metadata located the mismatch. The source list now names actual schema9/rules8 saves; view snapshots are excluded as baseline save inputs. This was a documentation/provenance failure, not a product test failure; the original files were not changed. The original fresh design builder check and its two resolved findings remain separate historical evidence.

## Verification and limits

Scoped documentation verification checks exact task/step/coverage structure, relative links/approval status, declared existing integration paths and core numeric reference values; preservation of owner species/faction/backlog/combat-note files; intended documentation-only changes and unchanged product paths. The retained `.combat-plan-20261028` workspace evidence directory contains the actual machine result and commands. A plan assertion describes a future test; none was executed against a new combat implementation.

Fresh corrected verification: **48 documentation checks passed;26 new relative links resolve;29 existing integration paths and6 actual schema9/rules8 save inputs were confirmed; exit0**. Recorded at 2026-10-28T23:56:06.091257+00:00. Command: `python C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.combat-plan-20261028/verify_plan.py`. The same directory retains `verification.json`, the verification script and `initial-verification-failure.json`. Six save inputs were located/read for provenance only; no old-engine futures were captured. All four protected owner prose/backlog/combat-note files remain SHA256-identical to planning entry; all13 changed paths are documentation.

No application build, simulation/gameplay suite, graphical runner, baseline capture, new worktree or product source/content/default/schema change occurred during this approval/planning turn. Existing1,547-test M8 evidence remains historical. Full implementation, balance, physical-input/comprehension, other platforms, dependency/security audit and export/release remain unverified or deferred as previously recorded.

Next action: owner reviews the concrete16-task plan and confirms it captures the approved slice. Retain the previously chosen Native execution method after that review.
