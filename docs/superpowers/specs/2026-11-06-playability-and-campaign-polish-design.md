**M13 implementation approved, 2026-11-06:** Owner: “Looks good. Go ahead and implement M13 using the proposed scope and ten-task plan.” Design and task-plan approval are recorded in ADR-027. M13-D/M13-P Verified for approval scope; M13-I In Progress on isolated codex/playability-polish. Execute Native inline, no per-task agents, one final fresh whole-M13 implementation review. Keep rules12/writer13/79 pairs, src/content/schema and balance unchanged. Ledger: .superpowers/sdd/2026-11-06-playability-and-campaign-polish/progress.md. Preserve all prior documents, saves, evidence, branches/worktrees and the historical three-log exception. This approval does not authorize commits, merging, pushing, release or cleanup; strict committed-source acceptance remains a separately identified gate. Earlier proposal-pending paragraphs below are historical.

# M13: Playability and campaign polish — proposed scope

Date: 2026-11-06. Status: **Proposed; owner review pending.**

The owner selected M13 and requested a proposed scope and plan for review. This authorizes these documents, not implementation, balance changes, commits, integration or release. The accompanying implementation plan is provisional until this description and its task breakdown are approved. M13-D/M13-P: In Progress for proposal preparation; M13-I: Not Started. Existing parent requirement rows and totals retain their scopes.

## Purpose and definition of done

Make the existing game easier to begin, navigate, understand and resume over a sustained campaign. Preserve the adopted map-first shell and the completed conflict systems. A player should be able to answer: **What have I selected? What can I do here? What needs a response? What will happen next turn? What changed last turn?**

M13 has three connected deliverables: a more legible start-to-colony interaction flow, clearer turn guidance, and evidence from longer campaigns. It is a presentation and validation milestone. Campaign auditing may find balance concerns, but recording those concerns does not authorize retuning the simulation.

Automated completion means all proposed PLY criteria below pass against the final isolated source, the full unfiltered native suite and existing twelve graphical scenarios remain passing, and ordinary-assembly restoration is verified. Human playability acceptance is a separate outcome: completed owner-operated observations with no unresolved critical/major usability defects in the specified journeys. Do not label the entire milestone Verified from automation alone; report automated and human results separately. Approval of a later merge remains separate.

## Inspected baseline

Primary main was clean at `60044afd6132710b88c853b9b4c4943b5b9c29ce`. The combined UI/conflict acceptance record is `reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md`; its historical Windows 2402, layout 107, twelve rendered cases and four earned conflict campaigns are baseline evidence, not new M13 results.

Current source already has a viewport-sized scrolling setup form, a map/right console/bottom navigation shell, known-world system inspection, colonies/workforce/construction, vessel route/founding commands, reports, readiness and conflict interrupts. `GalaxyMap` accepts star clicks; vessel selection is currently through `VesselPanel`, which also contains the empire vessel picker and existing route controls. `StarPanel` is a text-led system/planet inspector. `ColonyPanel` has species-aware economy readouts, existing planet portraits and construction controls. We will improve these working paths rather than replace their simulation or command authority.

## How the supplied screenshots inform the proposal

The twenty-three owner-supplied Master of Orion II screenshots are visual references, not instructions embedded in a document and not an approved feature checklist. Preserve their originals and provenance in `docs/design-notes/m13-20261106/references/`; `reference-manifest.json` identifies both batches, filenames, byte hashes and the interpretation below. They are review material, not game assets or an art brief to copy.

| Screenshots, ascending timestamp | Useful interaction principle | Proposed M13 translation |
| --- | --- | --- |
| 23-27-23; 23-27-37 | Clear entry choices and compact setup | Improve the existing New Game/Load entry and group existing setup choices; no new title/menu screen required |
| 23-27-49 | Civilizations feel different before starting | Make the existing species summary, population terms and current mechanical differences readable |
| 23-28-01; 23-28-09; 23-28-22 | A sense of empire identity | Show the current original species/culture/home-system identity consistently; ruler naming, banner selection and renaming are deferred |
| 23-28-37 through 23-29-16 | Keep map context while inspecting ships, destinations and ETA | A compact selected-vessel summary and obvious existing route review, with selection distinct from command submission |
| 23-29-25 | Inspect a system without losing orientation | A grouped known-world overview with direct planet/owned-colony navigation; orbital animation is deferred |
| 23-29-35 | Colony status and actions belong together | A concise colony summary above workforce/construction; retain original planet artwork and detailed breakdowns |

The second batch is explicitly labelled **“Menues”** by the owner. Its menu references add these concrete review targets:

| Reference | M13 use | Boundary |
| --- | --- | --- |
| 23-33-17 research;23-33-59 construction | Show the selected existing choice, cost, prerequisites/effects and current progress/estimate visibly, not only in a tooltip | Existing technologies/projects and commands; no new disciplines or economy |
| 23-34-56 arrival notice | A concise outcome with an inspect link and predictable return to the map/report | Existing discovery and interruption policy |
| 23-35-21 colonies;23-35-30 planets | Scannable known/owned summaries and a clear link to the selected world | Known worlds only; no new omniscient planet catalogue or colonization automation |
| 23-35-39 fleets;23-35-50 officers | Distinguish selected fleet, ship and officer and make current legal actions easy to locate | Existing fleets/admirals, not a new colony-leader system |
| 23-36-00 relations;23-36-11 information | Clear no-contact states and links to existing diplomacy/reports/explanations | No espionage, statistical history graphs or encyclopaedia system |
| 23-36-44 Game;23-36-55 settings | Consistent menu hierarchy, labels, return behavior and existing notification preferences | No new audio controls,autosave,global settings system or unsupported toggles |

Do not infer tactical combat, more races, treasury/food systems, multiplayer, invasion, galaxy-age rules, new difficulty settings, cinematic colony cities or copied art/UI skins from these references. Existing original names, art and theme remain the foundation.

## Scope and proposed behavior

### 1. A clear start and first turn

Keep the current single setup screen and its existing settings. Group size/species/opponent choices first; group the existing seed and hazard options under a clearly labelled expandable section. Values retain their current defaults and meaning, and `NewGameScreen.Configure(...)` remains usable by the existing harnesses. A collapsed group never resets its selected values. Show the chosen settings in a wrapped summary before Start, including rules/mode and the actual seed. Retain existing random-seed behavior; do not invent a new deterministic default.

Keep Start and Load outside the scrollable form at every supported size. Invalid input keeps the form, choices and explanatory error visible. Activating Start once creates one campaign; changing a setting, opening a group, scrolling or dismissing an error cannot create a game. At first entry, show one concise orientation hint pointing to the selected home system, colony management and End Turn. It dismisses within this GameScreen session and is not a new save field or persistent tutorial system. Loading a campaign does not run new-game guidance or overwrite its rules.

### 2. Contextual map and vessel inspection

Preserve star-click selection. Add visible interaction with **owned, stationary** vessel markers using existing requester `GalaxyMapView.Units`; opening a marker selects/inspects and never issues a movement, founding, combat, peace or readiness command. In-transit/foreign markers remain informational. For multiple owned stationary vessels at one star, show an ID-stable chooser in the contextual panel; do not pick an arbitrary ship or use invisible hit targets. Within a click radius, nearest distance then stable ID breaks ties. Marker selection has priority over star selection only inside the drawn marker's own hit area.

The contextual panel shows one selected owned vessel, its original role/design, station or transit state and any already-known pending destination/ETA. Reuse existing route controls and legal command review in `VesselPanel`; do not add click-to-move, multi-ship order semantics or a second routing calculator. Inspecting another star can update a destination preview as it does today; only the existing explicit command button submits it. An owned vessel in transit remains selectable through the existing list for inspection. Keep that list as a keyboard-accessible alternative to map markers.

Reduce crowded map labels using a deterministic screen-space layout: reserve selected-system and home-system labels first, then place other charted names in StarId order when their measured rectangles fit the map and do not intersect an already placed label. Suppression hides only the drawing, never the selectable star or known-system list. The selected name is always available in the contextual panel; show a clipped/long name fully in its tooltip. Use existing marker shapes plus text/selection, not color alone. Do not derive labels or tooltips from hidden `GameState` or invent unexplored planet information. No zoom/pan rewrite in this milestone.

### 3. Connected system and colony overviews

Keep system/planet selection and colony selection distinct. The system inspector identifies the selected star, observation freshness and only the known worlds. Group each world's known environment/habitability/ownership and existing inspection action. An unobserved system says that details are unknown rather than showing blank fabricated values. A foreign or unowned world never gets an owned-colony management action.

Above the existing colony controls, present its original identity, population/capacity, support outcome, production, research, current project/available estimate and relevant pressure/attention from the same `OwnedColonyView` already used below. Retain species vocabulary and existing detailed tooltips/forecasts. Do not label support as food, invent currency, sum incompatible units, replace conditional estimates with promised completion dates or expose foreign production/banks. Use existing `PlanetPortrait`; no new asset pipeline.

Actions from map, system inspector, colony list and reports reach the same selected object. Back/close returns to the invoking workspace and sensible focus without advancing a turn. If the object is lost or disappears after a turn/load, clear it and explain its unavailability; never silently act on another colony/vessel. UI navigation state is local to the active GameScreen, not serialized into campaign saves. A Load/new campaign starts with that campaign's valid home selection and no stale popup/action context.

### 3a. Menu clarity across the existing game

Include an explicit menu audit, not just map/colony screens. Research, construction, colony/system lists, fleet/officer, contacts/diplomacy, reports, Save and Load all participate in the same selection/return/focus contracts. Preserve the adopted bottom navigation and original theme. Do not add dead menu entries just to resemble the references.

Research shows its current target/progress and a visible description of the selected existing option: cost, existing prerequisite/availability explanation and effects available in current content. Construction shows the selected existing project or queue entry, cost, existing production/conditional completion estimate and queue meaning. Existing quick selectors retain their current explicit-selection command semantics; browsing/opening/focusing a list never commits, and choosing an enabled option once issues at most its existing command once. No new prerequisite tree, cost formula, research field or production model. Long details wrap in a scrollable region without burying the primary action. Inapplicable estimates say why they are unavailable rather than inventing a number.

Colony/world summaries remain known/owned-scoped. Fleet and officer menus identify which selected object each existing action affects; unavailable authority has a visible explanation. Contact/diplomacy menus distinguish no contact, known relationship and own pending response without revealing private foreign intentions. Arrival and report notices link to the existing permitted system view and return without triggering orders.

The Game menu keeps its current Save game,Load game andLatest turn report functions and stable IDs 0/1/2 for existing harnesses. Improve discoverability and predictable closing/return behavior; existing notification preferences stay at their current supported entry points with clear labels. A general Settings screen,New Game/Quit flow,audio sliders,autosave,history graphs and reference browser are separate future proposals. This milestone does not add settings that have no implemented effect.

### 4. Turn guidance and reports

Add a compact **Turn guidance** panel that links to existing controls and uses only the current `EmpireView`. Two explicit categories: **Response required** for existing requester-projected response notices; **Consider** for optional issues such as absent research, idle construction and existing nonblocking attention. Do not create extra blockers, suppress mandatory interrupts or auto-select research/projects/orders. An empty required category says "No response notices. End Turn will check readiness." Keep Consider collapsible. This is deliberately not a guarantee that Ready will succeed: forecast-dependent combat/scouting preparation and crisis acknowledgement remain the existing engine's responsibility.

Project each item from existing visible facts, preserving their authority and freshness. Stable item identity is a kind plus the relevant owned entity/encounter ID and revision where available. Order required items before optional items; within each class use a fixed kind order and then stable ID. Aggregate repeated optional items by kind with a count and a link to the existing overview, so the console does not grow with empire size. Do not infer private rival intent. Required response details continue to live in the authoritative existing dialogs.

Keep current End Turn/Ready/cancel-waiting semantics. An already disabled End Turn has its existing visible reason and response link. An engine rejection after activation shows the exact existing reason; link to its existing overview when the reason has a known route, otherwise retain the full message and focus its safe dismiss control. Never retry, acknowledge or fabricate a response automatically. Refresh never acknowledges a report or clears a requirement. A report link opens the relevant owned/known subject, and closing it returns to the report at its previous scroll/focus if still valid. Group the latest report into required responses, changes/outcomes and routine details, retaining every material existing explanation and conditional/private-information boundary. The existing "no research target means research is saved" behavior must remain explicit.

### 5. Consistent input and small-window behavior

Test physical viewport sizes 800x600,1152x648,1600x900 and1920x1080. Preserve ANGLE,60FPS,VSync and the current window defaults. Keep primary actions and navigation reachable without horizontal form scrolling; use wrapped text, vertical scrolling and existing responsive column behavior. Essential actions/reasons must be visible without hovering; tooltips supplement them. Preserve current theme typography unless an observed readability defect justifies a local adjustment.

Tab/Shift+Tab traverses visible enabled controls, Enter/Space activates the focused control once, and Escape closes the top dismissible transient UI with no command side effect. Escape does not dismiss an engine-required response by acknowledging/answering it. Resizing or closing a popup restores valid focus and does not duplicate automatic scrolling. Do not steal focus from an in-progress edit on a routine refresh. When a focused control must be rebuilt, restore its semantic identity after layout settles; if it no longer exists, focus the nearest safe navigation control and explain lost context.

### 6. Sustained campaigns and a small defect budget

Run eight actual-Main campaigns: Harl/42,Peldra/45,Sulan/43,Wenth/42, each with Small/Standard and Small/Solo settings. For this proposed audit, Standard means one rival (Peldra for Harl/Sulan/Wenth; Harl for Peldra), while Solo means zero rivals. Both use colony crises, space ameba and sentries at their existing Standard/On values, rules 12 and original core content. These are fixed audit profiles, not new game-mode controls or changes to setup defaults. Advance 80 committed turn boundaries per campaign. Capture at 0,10,25,50,80 with actual Main Save/Load under unique QA names; compare the authoritative canonical campaign state and three uninterrupted-versus-loaded future boundaries at each checkpoint. This yields40 checkpoints and120 future comparisons. Use deterministic legal actions and existing paid commands; never inject assets, reveal hidden state to decisions, skip a required response or mark an unreachable target as passed. A reached turn count measures endurance, not balance.

The Standard driver uses the already earned M12 campaign policies for contact, paid military preparation and conflict, with the current setup UI. The Solo driver uses current colony/research/build/movement commands and never manufactures a rival/war. The resulting playtest record must state which encounters actually happened. Retain the four earned conflict campaigns and seven stages/three futures acceptance as a separate mandatory regression; the80-turn matrix cannot replace those witnesses. Balance observations include idle periods, repeated maintenance, shortages, travel/waiting and recovery; record actual context and uncertain causes.

Allow at most **five** reproduced, scoped presentation/integration defects discovered by this audit or owner playtesting, after the listed UI work. Each needs evidence, a regression, a fix and retest. Severity: critical prevents/resubmits actions or corrupts/losses state; major blocks a required journey or gives materially false guidance; minor adds avoidable friction. Stop and revise the milestone if a sixth fix, simulation/rules/content/schema edit or a new system is needed. An unresolved critical/major defect keeps acceptance open; it is never deferred merely to fit the budget. Numerical balance proposals remain a separate backlog regardless of count.

## Alternatives considered and drafting decisions

| Approach | Tradeoff | Recommendation |
| --- | --- | --- |
| Audit/checklist only | Cheapest, but leaves known disconnected/verbose interaction paths intact | Insufficient for the selected polish milestone |
| Focused flow and guidance improvements, then campaign audit | Improves the whole existing loop while protecting rules and accepted systems | **Proposed M13** |
| Broad visual remake plus identity customization/new strategy systems | Larger art/schema/gameplay scope and weaker bounded acceptance | Defer |

The grouped setup, contextual owned-vessel card, collision-aware labels, turn-guidance panel,80-turn/eight-campaign matrix and five-fix limit are **drafter proposals**, not prior owner approvals. Supplied screenshots support the interaction direction; they do not establish these exact decisions. The review can change them before execution.

## Boundaries and compatibility

- Existing Godot 4.7.2 C#/.NET 8 stack, original assets/theme and plain-C# simulation separation remain. No new dependency, framework, engine upgrade, Docker or routine host desktop control.
- Keep rules 12/writer 13/79 supported pairs and genuine historical campaign behavior. No source edit under `src/`, no balance/content changes, no save fields/migrations and no PRNG consumption by presentation.
- Preserve all earlier branches/worktrees/stashes, owner prose, saves/screenshots/raw and frozen evidence, renderer investigation and the exact historical three-log exception. Every new engine invocation gets a unique explicit log path. No cleanup or overwriting latest evidence.
- The opt-in M13 harness is isolated from the common twelve-scenario target. Extend ordinary metadata checks to detect it; preserve the existing twelve-type negative control and require the entire validation namespace to be absent after restoration.
- Material uncovered engine defects trigger a separate scoped proposal with compatibility work; a presentation milestone is not permission to patch historical rules.
- FR-001 Peaceful Mode, empire customization, expanded diplomacy, conquest/invasion, tactical combat, automation policies, general tutorial/help systems, new art/audio and packaging/release remain deferred.

## Acceptance criteria and evidence

| ID | Observable contract | Required proof |
| --- | --- | --- |
| PLY-AC-001 | Existing setup settings/defaults survive grouping; Start/Load/error/first-turn flow are clear | Real controls at all four sizes; settings before/after; single-start assertion |
| PLY-AC-002 | Owned stationary marker/list selection is unambiguous and inspection never submits orders | Overlap/multiple-vessel/foreign/transit fixtures; command/state/PRNG no-change assertions |
| PLY-AC-003 | Map labels remain bounded and selected context discoverable without revealing unknown facts | Crowded/long-name layouts, known-fact comparisons, ID-stable screenshots |
| PLY-AC-004 | System/colony/research/construction/menu summaries use authoritative existing projections and correct units | Species/unknown/stale/foreign/pressure/menu fixtures; exact displayed-vs-projected values and existing action semantics |
| PLY-AC-005 | Navigation, reports, popup close/resize and lost-selection recovery preserve valid context | Round trips, removed-control fixtures, focus/scroll/command-count checks |
| PLY-AC-006 | Required guidance matches projected response notices; forecast-only rejection remains authoritative and optional guidance never changes readiness | Projected/forecast-only/optional/ready/cancel-waiting/revision tests; privacy assertions |
| PLY-AC-007 | Primary tasks remain reachable with keyboard and pointer at four sizes | Rendered bounds/control assertions plus separate physical-input observations |
| PLY-AC-008 | Eight campaigns/40 checkpoints/120 futures finish honestly with no injected assets | Current-source actual-Main saves, canonical/replay proofs, paid-action provenance and observed-event ledger |
| PLY-AC-009 | All existing twelve native scenarios, four earned conflict campaigns and full suite retain their contracts | Fresh complete queued results and source/DLL identities; no missing/skipped coverage |
| PLY-AC-010 | UI-only scope and ordinary assembly remain clean | No src/content/schema/rules delta; whole validation namespace absent; final build and diagnostics |
| PLY-AC-011 | Findings are bounded, preserved and either resolved or explicitly block acceptance | Defect ledger, up to five fixes, honest scope-specific disposition |
| PLY-AC-012 | Owner review distinguishes approved design, automated evidence and human acceptance | Approval record plus owner-operated report; no inferred usability/balance/release pass |

Initial state for every PLY criterion: **Not Started**. Saving this proposal is not functional acceptance. M13-D/PLY-AC-012 may later receive design-only approval without promoting human acceptance or implementation.

## Delivery and review gates

1. Review this proposed description and the provisional task plan together; resolve scope changes in the documents. Approval must identify the version reviewed.
2. After explicit implementation approval, establish an isolated checkout from then-current main and freeze source/refs/owner evidence and canonical campaign fixtures before changes. If main moved, reconcile it; do not silently test stale60044af.
3. Execute Native inline, no per-task implementation agents, with one final independent M13 implementation review after the work is built. The earlier M12 review is complete and is not being repeated. A proposal builder check is separate from implementation review.
4. Finish all independently executable automated checks through the shared queue before requesting physical observations. Provide exact actual-root build/launch, prerequisites, numbered tasks, expected outcomes and report-back fields in the playtest packet.
5. Record automated/human outcomes independently. Do not merge, push, release or delete anything without the owner's later authority.
