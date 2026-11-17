# Fleet roster unification implementation plan

**Goal:** Finish the two owner-authorized M14 fleet presentation gaps in the existing uncommitted primary checkout.

**Architecture:** Reuse MapFleetRow for the Ships, Fleets and fleet-member lists. A shared expandable ship-stack control presents equivalent independent ships in the workspace and map fleet card. Shared projection-only grouping retains exact IDs and separates orders, request commitments, cargo, damage and attention. Retain every existing command/review boundary and filter/sort.

**Tech stack:** Existing Godot 4.7.2 C#/.NET project and single theme.

**Spec:** Owner's 2026-11-10 continuation and reports/M14_SELECTION_CONSOLE_2026-11-10.md items 2/10; this is the approved completion of previously reported gaps.

## Constraints

- Work in the existing uncommitted primary tree as requested. Preserve the earlier map-key patch and prior selection-console evidence. No commit, new gameplay, simulation/content/schema changes, cleanup or publishing.
- Native inline execution; no per-task agents. Queue all heavy checks through the installed openantares runner; no Docker or desktop automation.
- Run only the new fleet-roster native case plus the directly affected selection-console checks in the same process. Compile affected legacy adapters but do not execute their suites.

## Review focus

- Partial stack selections must retain exact ship IDs after sort/refresh.
- Expanded members must allow a single ship to be ordered/formed into a fleet, with Shift semantics matching existing rows.
- Different order/request/cargo/health/damage facts must not disappear in a stack.
- Old split/merge/course and Decisions/ESC behavior must remain usable.
- A 100-ship equivalent roster must remain compact and scrollable at 800x600 and 1920x1080, with member expansion and status/hull/cargo detail.

## Tasks and progress

- [x] 1. Add a focused native regression reproducing the separate Tree and unstacked map roster; capture RED evidence.
- [x] 2. Implement shared grouping and expandable MapFleetRow stacks; migrate workspace lists and map independent rows, adapt affected harness controls. Keep exact-ID compatibility methods for existing order adapters.
- [x] 3. Run the focused native case, affected adapter compilation, source/DLL verification and ordinary rebuild restoration. Inspect native frames, update status/report with exact results and human limits.

No commit step is authorized. Existing historical reports retain their original scopes.

Final scope verified: 191 checks/14 frames, queued fleet-roster-20261110-green04; ordinary build restored. Human acceptance remains Pending.
