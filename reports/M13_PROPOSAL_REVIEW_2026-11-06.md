# M13 proposal builder check and plan self-review

Date: 2026-11-06. Status: proposal checked for review; owner approval pending. No M13 product behavior has been implemented or accepted.

## Scope of this work

The owner selected playability and campaign polish and requested both proposed scope and plan. The owner then added a second screenshot batch labelled “Menues.” Both batches are reference material for the draft. They do not authorize copying assets, adding every depicted system or implementing the proposal.

Inspected source: clean main `60044afd6132710b88c853b9b4c4943b5b9c29ce`. Existing M12/newer-UI combined acceptance remains historical evidence. Current work reads code and writes proposal/reference/status documents only; no build, gameplay test, engine launch, code edit or commit. Parent requirements, totals and prior status prose are preserved.

## Fresh builder check

The brainstorming skill requested a fresh builder check of the proposal. One reviewer inspected the written design, provisional plan and relevant existing code read-only. This was a design-planning check, not a repeat M12 implementation review and not the future M13 implementation review.

One consequential issue was raised: projected response notices do not necessarily enumerate every engine Ready rejection. Combat readiness can depend on crisis-preparation forecasts; scouting preparation and crisis acknowledgement also matter. A pure `EmpireView` summary cannot honestly promise exact readiness parity while simulation edits are excluded.

**Disposition:** retain the presentation-only boundary. Guidance shows requester-projected notices, and its empty state is exactly “No response notices. End Turn will check readiness.” Existing engine checks/rejections remain authoritative; show their exact message, route known reasons to the existing overview and never auto-retry or acknowledge. Add explicit forecast-only rejection, scouting and crisis cases. This is a drafter decision for owner review, not an inferred owner approval.

After that revision and the menu-reference addition, the same reviewer checked the changes and reported **“nothing important left.”** Existing marker data, vessel command events, colony projections and the journal-based save-proof verifier support the proposed reuse. No important builder questions remain outstanding; this is not a functional pass.

## Plan self-review

- All twelve PLY acceptance criteria map to tasks in the coverage table. Ten tasks cover baseline/evidence, map labels/selection, vessel context, setup/system/colony flow, menus, focus/context, guidance, campaigns, bounded fixes/owner observation and final regression/review.
- Producing-task interface names match consuming tasks. New presentation state is ephemeral; no new save or rules contract is proposed. Existing quick-selector commands and Game-menu IDs 0/1/2 are preserved.
- Menu scope names research, construction, colony/world lists, fleets/officers, contacts/diplomacy, arrivals/reports and Save/Load. New audio, autosave, general settings, history/reference systems and other screenshot-only mechanics are deferred.
- Five review-focus failure classes have named tests in owning tasks. In particular, selections must not submit commands; lost selections cannot fall through to another entity; privacy, asynchronous focus and incomplete campaign evidence are explicit cases.
- Fixed campaign settings are explicit: four species/seeds × Small with one rival or Solo, existing hazards On; 80 committed boundaries, five checkpoint turns and three future comparisons each. Thus eight sessions,40 checkpoints,120 futures. Existing four earned conflict campaigns/seven stages remain separate required regressions.
- A maximum of five reproduced presentation/integration follow-up fixes is proposed. Engine/schema/content changes, a sixth fix or a new system require revised scope; unresolved critical/major findings block acceptance rather than disappear into deferral.
- Full native suite, existing twelve native cases, M13 cases, source/DLL identities, verifier negative controls and ordinary restoration remain required. Human observations, automated acceptance and release are separate outcomes. Current test counts are not copied into new-result claims.
- The scope/plan are intentionally proposed together because the owner requested both. Neither is authorization to implement. No implementation checkout or new commit was created. Execution-source recording may require later explicit commit authority; the plan identifies that gate rather than fabricating source identity.

## Owner review choices

Review the proposed contextual/menu improvements, unchanged simulation boundary, campaign matrix and five-fix limit. Naming/banner customization, a broad visual remake, Peaceful Mode and numerical balance changes remain outside M13. Approving the design and provisional plan should name the versions/amendments reviewed; implementation is the next separately authorized step.
