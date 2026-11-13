# Procedural Planet Globes Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` to implement this plan task by task, inline. The project's retained execution preference is native inline work without per-task agents. Checkboxes track implementation, not approval or acceptance.

**Goal:** Replace the selection-console planet illustrations with rotating procedural globes and add one persistent 3D globe to the colony page, with matching geography and population-driven night lights.

**Architecture:** A presentation-only builder consumes published inspection views and the existing saved appearance descriptor. One shared shader include supplies the surface, land mask, city sites and lighting to a canvas-item globe and a spatial globe. The colony component owns one SubViewport for its lifetime; ordinary colony refreshes update that component rather than recreating it.

**Tech stack:** Existing Godot 4.7.2 .NET, C#/.NET 8, GL Compatibility through the current Windows ANGLE setting, JSON visual presets, native shared queue, and an isolated opt-in Godot validation harness. No engine upgrade, renderer change, new package or GDScript.

**Spec:** “OpenAntares: Procedural Planet Globes Spec,” revision 2, dated 2026-11-10, supplied as [Pasted text.txt](https://chatgpt.com/library?file_id=libfile_fbdca103531881919fa6c090a588cded&account_id=personal), plus the owner's instruction to incorporate the local-source review. This document supplies the integration decisions and executable task order missing from that spec; implementers read both.

**Execution status,2026-11-12:** Local automated feature scope is complete:699 focused native checks, paired four/50-planet measurements, ordinary assembly restoration and one whole-feature review/correction pass. Owner appearance/readability/input observations remain Pending. Canonical execution ledger: reports/planet-globes-2026-11-10/progress.md; final report: reports/PLANET_GLOBES_IMPLEMENTATION_2026-11-10.md. The draft task checkboxes and earlier planning statements below are retained history, not fresh acceptance claims. No commit/merge/push/release/cleanup.

**Source grounding:** Existing files were read directly from `F:/Projects-PCGames/OpenAntares`. Git status could not be obtained under this session's access restrictions, so no clean-tree or exact-HEAD claim is made. The selection-console and fleet-roster reports record uncommitted follow-ups; execution must preserve them and recheck the actual working tree before editing. The plan itself is delivered separately and can be placed at `docs/superpowers/plans/2026-11-10-procedural-planet-globes.md` in the execution checkout.

## Global constraints

- Presentation only: no changes under `src/`, `content/core/`, rules, save writer, saved observations, save schema or command processing. No simulation randomness is consumed or reseeded.
- Reuse `KnownPlanetView.Appearance.Version`, `.Variant` and `.SurfaceSeed`. Do not regenerate a descriptor that already exists, change the saved version or persist PlanetVisual.
- Consume only `EmpireView`, its `StarView`/`KnownPlanetView`, and a matching `OwnedColonyView`. Never look up an authoritative foreign colony or recover hidden population from state, reports or UI caches.
- C# builds the production node tree. Scene files added by this plan are test entry scenes only. UI styling and dimensions use the existing theme; planet appearance belongs in visual JSON, not hard-coded C# colors.
- One shared shader include; one material instance per displayed 2D globe. No sidebar SubViewports and no per-globe per-frame script callbacks.
- At most one feature-owned 3D SubViewport exists in a GameScreen. Hidden pages stop its updates. No change to the galaxy map, fleet controls, selection ownership or command routing.
- Preserve Godot/ANGLE, 60 FPS, VSync, window defaults, owner theme edits, all worktrees/branches/stashes, existing saves and failed evidence. Do not start Docker.
- Queue heavy jobs one at a time. Test only the new or directly changed behavior. Do not run the full simulation suite, campaign matrix or unrelated historical acceptance by default.
- No external images, noise textures, modeled meshes, rings, moons, orbital structures, terraforming, race-specific city colors or settlement-history tracking.

## Decisions carried into implementation

| Concern | Decision |
| --- | --- |
| Saved identity | Use the inspection view's existing descriptor, including its structural variant. Derive independent terrain, cloud and city sub-seeds from that descriptor with fixed, documented salts. |
| Shader seed transport | Pack each 64-bit sub-seed into two signed 32-bit components preserving bit patterns, pass as `ivec2`, and reinterpret components as `uint` in the shared include. Never cast a complete seed to float. |
| Older saves | A surveyed planet lacking Appearance uses a neutral legacy placeholder with no city lights. An unsurveyed system remains “Not surveyed”; do not invent its planet count or type. No migration and no new saved descriptor. |
| Preset lookup | Combine `PlanetTypeId`, permitted `EnvironmentId`, and descriptor Variant. Extend `art/planets/planet_styles.json` without removing its existing keys or changing current portrait catalog behavior. New globe settings have a separately validated presentation format version. |
| Current content | Support `verdant_world`, `forge_world`, `crystal_world`, `barren_rock`; environment IDs are `dry`, `shelf_sea`, `open_ocean`. There is no current gas-giant type or planet-size category. Banded gas rendering is a labelled synthetic renderer fixture, not new gameplay content. |
| Population | Lights use only a matching owned colony's whole workforce-unit Population. Foreign and unknown population produce no population-driven lights. The existing ownership/knowledge text remains visible so darkness does not assert emptiness. |
| Development mapping | Initial visual reference is 20 workforce units for every planet. `level = sqrt(clamp(population / 20.0, 0, 1))`; zero population or missing colony gives zero. This is a drafter tuning default, not a population unit conversion. Capacity and species never enter the mapping. |
| Ocean eligibility | `open_ocean` is an environment category, not proof of a completely submerged surface. Initial globe presets must retain some land for each supported type/environment pairing. A synthetic explicitly all-water preset uses surface-wide city eligibility; never read a nonexistent simulation water-percentage field. |
| Barren/airless | `barren_rock` forces barren mode, no water, no clouds and no atmosphere. `dry` terrestrial worlds disable water but may retain a preset atmosphere/cloud cover. Do not equate every dry world with an airless world. |
| Animation | One shared presentation clock. All spin and cloud rates are integer cycle counts over a 4,096-second repeat period. Initial surface rates are 32–64 cycles per period (64–128 seconds per revolution), selected from the seed; cloud rate is a separate positive integer. This makes wrapping continuous for every rate. |
| Static mode | Add a persisted `planet_animation` boolean to the existing presentation settings, default true. Effective static mode is reduced effects OR animation disabled. Freeze the shared clock at its current phase; resume without advancing through the frozen interval. Settings never enter saves. |
| Pause and style | Drafter defaults for owner review: cosmetic motion continues during game pause unless static mode is active; restrained retro sampling; pale warm-white lights; sidebar clouds start at 64 displayed pixels. These are reversible visual defaults, not previously confirmed preferences. |
| Other illustrations | Keep `PlanetPortrait`, its cache, StarPanel and management-dialog thumbnails working unchanged. The matching-geography contract covers the two new globe views. Migrating all other portrait consumers is outside this pass; do not delete their existing renderer/cache. |

## File map and public contracts

All new names below are proposed implementation paths, not existing APIs. Use namespace `OpenAntares.Game.Presentation` for models/services and `OpenAntares.Game.Components` for controls.

| Path | Responsibility |
| --- | --- |
| New `game/Presentation/PlanetVisual.cs` | Immutable visual records and enums, including packed seeds, surface mode, palette, water enable, ice/cloud/atmosphere settings, spin/cloud cycles, tilt, phase, and city level/color. |
| New `game/Presentation/PlanetVisualCatalog.cs` | Load and validate the new globe settings from the existing visual JSON; handle unsupported type/environment/descriptor versions with a neutral fallback. |
| New `game/Presentation/PlanetVisualBuilder.cs` | Build exclusively from published views; map population; derive and pack stable sub-seeds. |
| New `game/Presentation/PlanetAnimationClock.cs` | One Main-owned clock, global shader value, static-mode transition, no simulation dependency. |
| New `game/Presentation/PlanetGlobeMaterialBinder.cs` | One uniform-binding implementation shared by the two components; shader parameter names live here once. |
| New `game/theme/planets/planet_common.gdshaderinc` | Pure shared surface/mask/site/lighting functions without canvas/spatial built-ins. |
| New `game/theme/planets/planet_canvas.gdshader` | Sphere reconstruction, screen-space edge filtering, halo and final alpha. |
| New `game/theme/planets/planet_surface.gdshader`, `planet_clouds.gdshader`, `planet_atmosphere.gdshader` | Spatial layers using the same shared functions. |
| New `game/Components/PlanetGlobe2D.cs` | Square shader control with `SetVisual(PlanetVisual visual)` and visibility-aware drawing; mouse filter Ignore. |
| New `game/Components/PlanetGlobe3D.cs` | Persistent viewport/tree/material ownership; `SetVisual(PlanetVisual visual)`, `SetActive(bool active)`, `SetStatic(bool isStatic)`. |
| Modify `art/planets/planet_styles.json` | Add validated globe settings; preserve existing static portrait keys. |
| Modify `game/Main.cs`, `project.godot` | Own one clock; declare the global shader clock uniform. Preserve every existing renderer/window setting. |
| Modify `game/Components/ColonyPanel.cs` | Separate persistent globe slot from rebuilt colony body; keep ShowColony/Clear behavior for selection, orders and focus. |
| Modify `game/Screens/GameScreen.cs`, `GameScreen.SelectionConsole.cs`, `GameScreen.VisualOverhaul.cs` | Supply views/catalog/settings, preserve selection controls, and route visibility/static-mode changes. |
| Modify `game/theme/openantares_theme.tres` | Planet-key variation and scoped metrics, without changing base Button icon sizes. |
| New `tests/OpenAntares.Game.Validation/PlanetGlobeValidation.cs`, `PlanetGlobeChecks.cs`, `PlanetGlobeValidation.tscn`, `PlanetGlobeValidation.targets`, `PlanetGlobeSampling.gdshader` | Isolated focused checks, a diagnostic sampling shader that includes the production common functions, and synthetic fixtures; all classes remain in the validation namespace. |
| New `tools/PlanetGlobes/Run-PlanetGlobes.ps1` | Case-selectable validation worker with source capture, diagnostic checks and ordinary-assembly restoration in finally. |
| Modify only affected checks in `tests/OpenAntares.Game.Validation/SelectionConsoleChecks.cs` | Update planet-key geometry expectations for child globe controls while preserving existing selection assertions. |
| New `docs/playtests/planet-globes-2026-11-10.md`, `reports/PLANET_GLOBES_IMPLEMENTATION_2026-11-10.md` | Focused owner observation checklist and actual execution results. |

Exact builder interfaces:

```csharp
public PlanetVisual Build(StarView star, KnownPlanetView? planet,
    OwnedColonyView? ownedColony);
public static float CityLevel(int population);
public static Vector2I PackSeed(ulong seed);
```

`PlanetVisualBuilder` receives a `PlanetVisualCatalog` in its constructor. Build verifies that the planet belongs to the supplied star and that an optional owned colony matches its PlanetId; invalid combinations fail without state access. Normal callers obtain ownedColony only from the current viewer's `EmpireView.Colonies`. PlanetVisual explicitly represents Unknown, LegacyPlaceholder or Known; placeholder values never carry a live hidden type, seed or population. Component interfaces return void and never issue commands.

## Review focus

1. Missing appearance or unknown environment must render a safe placeholder, not crash or expose live state: Task 2, `LegacyAndUnknownInputsUseNeutralFallback`.
2. Refreshing or switching colonies must retain the same viewport and mesh/material identities: Tasks 5/7, `RefreshAndSwitchRetainOneViewport`.
3. A 24-pixel inherited icon cap must not defeat the promised real globe size or steal clicks: Task 6, `PlanetKeysKeepSelectionAndMinimumGlobeSize`.
4. Long sessions, clock wrap and static/resume must preserve phase: Task 3, `ClockWrapAndStaticResumeAreContinuous`.
5. A mixed population/capacity/knowledge update must change only permitted lights: Tasks 2/8, `PopulationUpdatesDoNotMoveTerrainOrSites` and `ForeignAndStaleViewsDoNotReadLivePopulation`.

## Task 1: Establish the execution baseline and isolated validation entry

**Files:** New opt-in targets/scene, `PlanetGlobeValidation.cs`, `tools/PlanetGlobes/Run-PlanetGlobes.ps1`; no production changes yet.

**Interfaces:** Worker parameters: `-SourceRoot`, `-Godot`, `-OutputDirectory`, and `-Case` restricted to `Builder`, `Clock`, `Prototype`, `Layers`, `Sidebar`, `Colony`, `Cities`, `Performance`, `Feature`. Feature runs only these globe cases and the directly affected planet-selection checks.

- [ ] Read active AGENTS/CLAUDE/charter/test-runner guidance, obtain Git status/worktree inventory and inspect the current selection-console/fleet follow-up reports. Record source hashes and existing modifications. If an isolated checkout is used, preserve and include the current UI follow-ups; an old clean HEAD is not an equivalent baseline.
- [ ] Read the theme fresh; record current planet-key geometry, colony routes and normal metadata. Record the actual Godot .NET console executable and driver. Do not guess an installation path.
- [ ] Create the isolated harness and worker. The worker runs inside the shared queue, builds only the game with its opt-in target, checks that the metadata detector rejects that real opt-in assembly, launches the selected case, and rebuilds ordinary game code in finally. Do not nest queue invocations inside the worker.
- [ ] Require fresh output directories inside SourceRoot, source and assembly hashes, machine/renderer/viewport dimensions, results.json, engine/console logs and screenshots. Preserve failed output. Check source stability and both logs before accepting a case.
- [ ] Run `-Case Builder` with a bootstrap-only entry/exit assertion before the builder cases are added, to prove entry/exit/restoration. Ordinary metadata must contain zero validation-namespace types. An interrupted restoration remains a failed gate until the ordinary rebuild succeeds.

**Exit:** A focused check can run and restore normal play without invoking unrelated validation scenarios. No rendering feasibility claim yet.

## Task 2: Build the published-view visual definition and catalog

**Files:** PlanetVisual/catalog/builder; visual JSON; `PlanetGlobeChecks.cs`.

**Consumes/produces:** Existing published view types -> the builder interfaces above -> immutable PlanetVisual.

- [ ] Add failing Builder assertions: repeated inputs produce equal values; an existing SurfaceSeed/Variant is retained as input; changing population leaves terrain/cloud/city seed fields unchanged; mismatched planet/colony inputs are rejected.
- [ ] Pin `CityLevel(0) == 0`, `CityLevel(5) == 0.5`, `CityLevel(20) == 1`, and `CityLevel(40) == 1`; clamp negative synthetic input to zero. Capacity-only and foreign-colony changes cannot change the level. Test packed seeds differing in their low and high bits.
- [ ] Test the four current planet types across their allowed environments, dry/no-water behavior, airless disabling, missing descriptor/unknown descriptor version/unknown environment fallbacks, and independent sub-seeds. A stale StarView must use its supplied observation, never replace it with authoritative data.
- [ ] Implement immutable records, fixed salt derivation and catalog validation for finite values, valid modes/colors/ranges and a required neutral fallback. Keep existing JSON keys readable by PlanetStyleCatalog. Define a separate globe generator/catalog version without changing Appearance.Version.
- [ ] Run Builder. Serialize a fixture before/after repeated Build calls and assert exact equality, including stored RNG state; do not test determinism by calling the authoritative PRNG.

**Exit:** Builder cases pass without a simulation, content or save modification. Record the new catalog's concrete preset values.

## Task 3: Implement the bounded shared clock and coordinate contract

**Files:** Clock, binder, Main, project.godot, shared include and test checks.

**Interfaces:** `PlanetAnimationClock.SetStatic(bool value)` and read-only `double ClockTurn`; the service publishes global `planet_clock_turn`. Components read the service's cached value when requesting a static render; never query RenderingServer for it per frame.

- [ ] Add failing checks at 4,096 seconds minus epsilon, exactly 4,096, plus epsilon, and a synthetic seven-day time. Compare angular distance modulo one revolution for the minimum/maximum spin rates and cloud rate; tolerance is 0.0001 turns. Freeze, advance synthetic time and resume: the phase must remain at the frozen value until new active delta arrives.
- [ ] Implement a double-precision accumulated presentation time reduced to one bounded turn before sending a float to the shader. Use only integer cycle counts for every animated layer. No per-planet clock or per-frame uniform binding.
- [ ] Declare the global uniform without altering renderer defaults. Main owns one clock across screen changes; set process mode deliberately for cosmetic pause behavior. Stop clock advancement in effective static mode.
- [ ] Define common forward orientation `R = tilt-about-Z * spin-about-Y`; surface sampling uses inverse orientation, not the same forward transform. North is +Y; local +Z faces the camera at zero phase/tilt. Positive spin moves a centered surface feature left-to-right. Light direction and geometric normals share a fixed camera frame.
- [ ] Compile minimal canvas/spatial include users on the actual GL Compatibility renderer. Verify the packed integer seed path there; record any driver limitation before integrating UI.

**Exit:** Clock cases pass; minimal shaders compile. A plain elapsed float or arbitrary speed combined with modulo time does not satisfy this task.

## Task 4: Prove both renderers and transparent compositing before integration

**Files:** Canvas/surface shaders, shared include, both component shells, Prototype case.

**Interfaces:** `SetVisual` on each component and one uniform binder; prototype creates exactly one of each.

- [ ] Add the asymmetric diagnostic surface first: distinct north cap and three unequal markers at known coordinates. Freeze phase and compare orientation at zero, quarter-turn, nonzero tilt and a full wrap; record matched-center/limb screenshots.
- [ ] Implement 2D normal reconstruction with Y flip, 85% disc diameter, square aspect, derivative-based pixel antialiasing and a padded halo area. Quantize surface sampling only, retaining smooth geometric edge filtering.
- [ ] Implement the 3D surface with SphereMesh, fixed orthographic camera and private World3D/environment. Pass local position from the vertex stage, normalize in fragment and apply the same inverse orientation; do not double-rotate the mesh and sample coordinates.
- [ ] Add shared four-octave terrain and a normalized land mask. Additional colony detail may add at most two color/shading-only octaves. Use the diagnostic sampling shader to render common-function outputs at fixed unit-sphere points into a test texture; mask/site assertions compare those samples, not screenshots with different antialiasing. Use a small private logical resolution initially and nearest filtering for the proposed retro default.
- [ ] Include an intentionally simple transparent cloud shell and atmosphere shell in this prototype. Verify depth/culling/order, far-side occlusion and alpha over light and dark backgrounds before production UI work. Establish the output color-space/blending convention with known swatches; shared lighting code alone is not proof of matching displayed color.
- [ ] Measure a small fixed-seed renderer check with both views. Stop integration if matching geography, transparency or practical rendering cost fails. Do not change the renderer to pass.

**Exit:** Prototype screenshots/logs demonstrate orientation, land-mask agreement and clean transparent compositing. This is a technical gate, not owner aesthetic acceptance.

## Task 5: Complete reusable components and viewport lifecycle

**Files:** PlanetGlobe2D/3D, material binder and spatial layer shaders; Layers case.

**Consumes/produces:** PlanetVisual and shared clock -> controls implementing the public component interfaces.

- [ ] Extend prototype assertions for zero cloud/atmosphere values, resize, hidden ancestors, static rendering, static data updates and repeated SetVisual calls.
- [ ] Build the 3D tree, three meshes and materials once. SetVisual only changes uniforms/layer visibility; geometry and viewport identities must stay constant. Surface is opaque/depth-writing; transparent layers use explicit priorities/culling and preserve depth testing.
- [ ] Make SetActive(false) explicitly disable viewport updates. Animated active mode renders continuously; static active mode renders once after opening, planet/data/size changes, then disables updates again after the frame. A later event must request a new frame even if an earlier render-once request already completed.
- [ ] Size to displayed area using theme metrics: proposed logical maximum 512 square, absolute ceiling 768. Avoid resizing the render target unless the effective size changes. On reopening in static mode, render current data before accepting the retained texture as current.
- [ ] Implement hidden/no-draw behavior for the 2D control. It has no per-frame script work; static mode freezes motion but makes no sidebar performance-saving claim.
- [ ] Run Layers. Exercise 100 planet switches; live viewport/mesh/material counts stay bounded, and teardown drops feature ownership without disposing resources still bound to a visible control.

**Exit:** Component lifecycle passes independently of GameScreen integration.

## Task 6: Integrate the selection-console globes at a real usable size

**Files:** GameScreen.SelectionConsole.cs, GameScreen.cs, theme and affected SelectionConsole checks; Sidebar case.

**Interfaces:** Existing `SystemPlanet_<id>` Button remains the selectable control; it contains a noninteractive PlanetGlobe2D child.

- [ ] Add failing assertions that click planet keys and their globe areas, preserve selected PlanetId/pressed state and issue zero simulation commands. Verify unsurveyed systems do not display invented planet keys.
- [ ] Replace only the system-card key's cached image with a child globe using a new scoped theme variation. Keep existing control names, tooltip, orbit text, keyboard focus and selection handlers. Globe children ignore mouse input so the button receives clicks.
- [ ] Reserve a minimum 40-pixel square globe area, giving a 34-pixel disc at 85%; use the same 44-pixel minimum touch target already required by the UI. Read dimensions from scoped theme constants; do not raise base Button icon limits globally. Use the existing flow container to wrap at narrow widths.
- [ ] Bind each published planet and optional owned colony through PlanetVisualBuilder. Dispose replaced key/material ownership on the existing refresh path; no persistent cache of foreign dynamic data. Rebuilding the small sidebar on view refresh is allowed; per-frame rebuilding is not.
- [ ] Update only affected planet-key size assertions to inspect child globe bounds. Run Sidebar and retained selection assertions at 800x600 and 1920x1080. Capture 32/40/64/128-pixel standalone controls; small controls omit clouds below 64 pixels.

**Exit:** The actual selection UI shows readable globes, retains its selection behavior and fits both supported check sizes.

## Task 7: Retain one globe through colony refresh and navigation

**Files:** ColonyPanel.cs, GameScreen.cs, GameScreen.VisualOverhaul.cs; Colony case.

**Interfaces:** Preserve `ShowColony(ContentSet, OwnedColonyView, EmpireView?)` and `Clear()`. Introduce `ConfigureGlobes(PlanetVisualCatalog catalog)` and `SetGlobePresentation(bool pageActive, bool isStatic)` on ColonyPanel.

- [ ] Add failing identity assertions around ordinary Refresh, a workforce order, End Turn, 100 colony switches, Clear, page close and reopen. Check viewport identity and active update mode, not just total child count.
- [ ] Split ColonyPanel into one persistent globe host and a rebuildable body. Exclude the globe and its host from Clear's QueueFree loop. Clear deactivates/hides it when no colony is selected; ShowColony updates it. Create the actual 3D subtree lazily on the first visible colony page, not during hidden Refresh calls.
- [ ] Remove only this panel's old PlanetPortrait insertion. Keep ConfigurePortraits or adapt its callers safely for other consumers; do not remove the shared static portrait catalog/cache globally.
- [ ] Route real workspace visibility, hidden ancestors and static preferences to SetGlobePresentation. Opening another workspace or leaving GameScreen must disable viewport updates. Population updates received while hidden are applied on next opening.
- [ ] Supply current matching permitted planet/owned-colony views; keep workforce/construction commands and focus/scroll behavior intact. Do not require the owner to reselect a colony after every turn.
- [ ] Run Colony at both window sizes, including a no-selection state and an owned world lacking a legacy Appearance descriptor.

**Exit:** Ordinary refreshes update the globe in place; hidden colony pages perform no 3D viewport rendering.

## Task 8: Implement stable population-driven night lights

**Files:** Shared include, builder tuning and material binder; Cities case.

**Consumes/produces:** CityLevel from Task 2 + stable city sub-seed + shared surface eligibility -> emissive night-side sites.

- [ ] Add failing fixed-surface-sample checks for levels 0, 0.25, 0.5 and 1: increasing levels retain every previously eligible lit site; decreasing then restoring level recovers the same set. Compare site eligibility separately from screen-space filtering or cloud occlusion.
- [ ] Implement a stable sparse candidate field independent of terrain/cloud sub-seeds. Lower only its activation threshold as city level rises. Clamp the dense endpoint and retain an excluded part of eligible surface; level 1 must not become uniform land illumination. Record the actual tuned threshold range in visual JSON.
- [ ] Apply the shared land/explicit all-water eligibility rule, fixed night-side fade and warm light color. Skip site work at level zero. A geometry detail setting must never move potential sites or change eligibility.
- [ ] Capture both globe views at matched frozen phase. At small sizes allow cluster filtering differences while retaining the same underlying sites. Check small colonies over a full rotation; tune sparse cluster visibility without relocating sites according to the camera.
- [ ] Exercise live open-page population growth and loss through ordinary supported actions or clearly labelled pre-session fixtures; verify capacity-only changes do not dim lights. Do not mutate a live campaign to manufacture outcomes.
- [ ] Compare permitted foreign/stale view inputs with hidden state changed outside the published view: output must remain equal. Verify zero population-driven foreign lights and unchanged serialized state/RNG after repeated rendering and navigation.

**Exit:** Site sets are stable and monotonic, dynamic lights refresh while open, and the information boundary is preserved.

## Task 9: Wire motion preference and tune bounded rendering cost

**Files:** GameScreen.VisualOverhaul.cs, Main/clock/component settings, theme/visual JSON; Clock/Performance cases.

**Interfaces:** Existing presentation.cfg gains only the optional `display/planet_animation` boolean. Game menu gains a named check item; old settings files default to animation enabled.

- [ ] Persist the animation toggle through the current ConfigFile path and existing error-reporting pattern. Combine it with Reduced effects without modifying the stored animation preference. Wire clock/static-mode changes across visible and future screens.
- [ ] Verify static activation freezes current phase, switching planets remains deterministic, resize/data changes render once, and reenabling animation resumes smoothly. A save/load may reset the animation frame; geography and sites must remain stable within the visual generator/catalog version.
- [ ] Use labelled representative four-planet content and the loader-supported 50-planet stress system. Distinguish total controls from actually visible globes; ancestor visibility and offscreen clipping must prevent hidden draw work where required. Fix visibility/layout within these components instead of adding per-frame globe scripts.
- [ ] Measure warmed baseline versus feature at the same driver/window/scene, for 30 seconds each, with CPU/GPU frame statistics where available, live resource counts, actual globe dimensions and viewport sizes. Proposed budget: p95 frame time at most 16.7 ms when the paired baseline meets 60 FPS, with added cost at most 2 ms. If baseline already misses 60 FPS, report both and require at most 2 ms added cost; do not blame the globe for the existing baseline.
- [ ] If the feature misses the budget, first reduce logical colony resolution or color-only detail. Preserve the common land/site functions. Record the selected resolution and any measured limitations; do not silently remove clouds, cities or the 3D renderer.
- [ ] Present real-size screenshots for retro sampling, atmosphere strength, city visibility and small clouds. Leave physical readability/aesthetic approval explicitly Pending until the owner observes them.

**Exit:** Motion preferences survive restart, all update modes work, and the recorded measured cost meets the stated budget or remains an explicit unresolved gate.

## Task 10: Final focused verification and handoff

**Files:** Focused checks/worker, implementation report and playtest checklist. Update PROJECT_STATUS.md only for the actual verified scope after implementation.

- [ ] Run Feature once against final source: all globe cases plus the directly affected planet-key selection and colony route checks. Retain existing assertions; do not rerun fleet campaigns or broad simulation suites without a concrete regression reason.
- [ ] Confirm final ordinary game build succeeds, normal metadata has zero validation types, and engine/console logs contain no unexplained warnings/errors. Restore ordinary code after every opt-in run, including failed runs.
- [ ] Compare source scope and serialized before/after data. Confirm no src/content/core/save/schema change, no new texture/mesh assets, no simulation RNG movement, and older saves with/without Appearance load according to the documented placeholder policy.
- [ ] Verify no material/viewport accumulation and one clock service after repeated navigation and returning to Main. Record test-only fixture scope separately from actual game routes and physical-input observations.
- [ ] Self-review against the acceptance mapping below, write actual commands/results/source hashes and unresolved items into the report, and supply the owner with the focused playtest checklist. Do not claim human acceptance from emitted signals, screenshots or numeric checks. Do not commit/merge/push/clean up without applicable authorization.

**Exit:** Automated feature scope is evidenced; remaining owner visual/readability observations are clearly identified.

## Native commands and expected outcomes

Commands below are for future execution, not claims of checks performed while drafting. Resolve actual source and Godot paths in Task 1.

Normal game-only build from the active checkout:

```powershell
$globeSourceRoot = 'F:/Projects-PCGames/OpenAntares' # Or actual isolated checkout.
Push-Location -LiteralPath $globeSourceRoot
try {
    python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- dotnet build OpenAntares.csproj --nologo --disable-build-servers
} finally { Pop-Location }
```

Expected: exit 0, zero errors, and no new warnings. A solution build is available if a concrete change requires it, through the existing native entry point with `-Suite build -SourceRoot $globeSourceRoot`; do not substitute its default full simulation suite for focused checks.

New feature worker, after Task 1 creates it:

```powershell
$globeGodot = '<actual Godot 4.7.2 .NET console executable recorded in Task 1>'
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- powershell -NoProfile -File "$globeSourceRoot/tools/PlanetGlobes/Run-PlanetGlobes.ps1" -SourceRoot $globeSourceRoot -Godot $globeGodot -Case Prototype -OutputDirectory 'reports/planet-globes/prototype-unique-run'
```

Use a fresh output directory for each invocation and select only the affected Case while developing. Final `-Case Feature` covers this feature, not the whole game. The worker must check every child exit code and return nonzero if validation, evidence checks or ordinary rebuild/metadata restoration fail. It must not call the shared queue recursively.

## Acceptance mapping

| Requirement | Task / evidence |
| --- | --- |
| Stable saved appearance input, independent sub-seeds, no RNG/save changes | 2 / Builder plus before-after serialization |
| Same world, poles, tilt, spin, land mask and lighting in both views | 3–4 / asymmetric diagnostic and matched-phase captures |
| Clean layered transparency, airless disabling and ocean eligibility | 4–5, 8 / Layers and labelled all-water fixture |
| Existing planet selection, theme sizing and narrow-window behavior | 6 / Sidebar plus retained affected selection assertions |
| One persistent colony viewport and live permitted refresh | 5, 7–8 / identity counts, visibility modes and population updates |
| Stable monotonic city sites, no capacity-driven dimming or foreign-population leak | 2, 8 / Builder and Cities |
| Motion preference, wrap continuity and render-once invalidation | 3, 5, 9 / Clock and lifecycle checks |
| Real-size retro style and bounded maximum-system cost | 6, 9 / real-size captures and paired measurements |
| Old saves, ordinary assembly restoration and no unrelated product scope | 10 / source review, load checks and final metadata |
| Human readability, appearance and physical interaction | 9–10 / owner checklist; Pending until observed |

## Plan self-review

Checked against revision 2 and the six local-source corrections: all are assigned above. The prototype now includes a transparency/color-path gate before UI integration. Population reference and ownership restrictions use fields that exist today. Clock continuity has a concrete bounded scheme rather than an unbounded elapsed float. Legacy appearance handling, viewport retention, actual sidebar size and retained static-portrait consumers are explicit. Shader/noise quality, driver performance and owner aesthetics remain implementation/observation gates; none has been presented as already verified.
