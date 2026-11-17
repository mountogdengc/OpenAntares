# Civilian logistics plan self-review — 2026-11-16

**Disposition:** Ready for owner review of the concrete14-task plan. Author self-review only; no second builder/implementation reviewer and no gameplay execution.

## Source grounding and authority

Read current source from F:/Projects-PCGames/OpenAntares, feature/planet-globes-modern at e90b6b2. Local main is d994d34. The preceding20-document refresh remains pending and must be preserved. Inspected Rules.CurrentVersion=12, SaveGame.SchemaVersion=13 and the actual explicit79-pair loader table. The plan proposes13/14 and92 pairs for later execution, not active versions today.

ADR-031 records approval of the summarized civilian behavior only. Development goods, fleet endurance and tactical interception remain separate later deliveries; no full approval of all numerical/later defaults is inferred from the owner's short reply. Native inline execution preference is retained. The implementation task plan itself is not yet approved.

## Self-review checklist

1. **Spec coverage:** All selected provision/transport/route/migration/health/protection/state/visibility contracts map to tasks in the plan's coverage table. Goods growth and fleet supply consumption are explicitly outside this increment. No nonfunctional future-resource UI is scheduled.
2. **Step scan:** Fourteen independently verifiable deliverables have named interfaces, actual existing/new file locations and finite scoped checks. Tests precede product changes; baseline capture precedes version/economy changes. No commits are scheduled without separate authority.
3. **Type consistency:** Commands use the existing Command/CommandOutcome path. Passenger count remains VesselState.CarriedPopulation; new provenance has no duplicate mutable passenger count. CombatProfile.Attack and nonpartial SaveGame were checked against declarations. Vessel/expansion parsing is in ContentLoader.cs, not an invented ExpansionContentReader.cs.
4. **Review Focus:** Reservation loss after health/growth, new-role civilian profile/availability, health/passenger identity, return-leg/generic-fleet guards and capability/save compatibility all have named owning tests. Task12 owns its minimal native red/green scaffold; Task13 expands/verifies journeys without a backwards task dependency.
5. **Proportion:** The plan records integration decisions, APIs and assertions rather than product function bodies. Detailed implementation helpers/DTOs remain builder choices. The broader gameplay spec remains linked and travels with the plan.

## Planning ambiguities resolved explicitly

- Existing movement is direct coordinate-based travel, not a starlane/pathfinding graph. The first route is one legal known direct leg; no new navigation system.
- The transport must have a zero-attack civilian profile because current CombatInitializer/VesselLoadouts require one for every role. Resolve it from existing scout hull/armor/speed before combat-content validation; conditionally omit all logistics role/profile data from old saves.
- New version13 must explicitly inherit earlier capability gates; changing CurrentVersion alone would disable some systems. Normal setup remains12 with opt-in logistics On selecting13, pending plan review.
- The draft's “production points” price was ambiguous against hundredth-based engine/content units. The plan proposes literal400 internal units=4 displayed points for the transport; this interpretation is a concrete review choice, not a claim of explicit owner numeric approval. Later goods prices are not normalized/implemented in this increment.
- Writer14 must support13 new pairs because it also writes old rules. Preserve79 historical pairs and add14/1..13, exactly92; no broad `schema >= rules` shortcut.
- Insert transfer after current movement/founding/arrival handling but before the one crisis exposure application. No second movement, health or economy engine. Load after economy; arrivals affect only next-turn outputs; one transfer per original hull.
- Mission-owned hulls cannot be generically moved/merged/refitted out from under cargo. Preserve finite cancellation/reroute and current civilian immunity; no incidental interception implementation.

## Verification run for this planning change

Only documentation links/headings, plan structure/coverage references, existing-source path declarations, exact79+13 pair arithmetic, preservation of prior pending files and changed-file scope are checked. The session retains manifests, original document backups and scoped verification JSON. No baseline executable, game build, xUnit/native case, performance test or full suite has run. Required future commands/results in the plan are obligations, not evidence.

## Handoff

Review the concrete plan, particularly its optional setup/version choices and explicit price-unit normalization. Then use Native inline execution if approved. No execution-method question, duplicate old-milestone tasks, commit/integration or release is needed to review this plan.
