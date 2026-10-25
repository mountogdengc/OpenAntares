# Godot runtime validation

This opt-in harness tests the legacy colony loop, frozen rules2 expansion and current rules3 species loop inside graphical Godot. It is excluded from ordinary compilation by the root project's `tests/**` exclusion and is added only through `Validation.targets` during this runner.

From the repository root, with restored dependencies and a Godot 4.7.2 **.NET console executable**:

```powershell
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'PATH/TO/Godot_v4.7.2-stable_mono_win64_console.exe' -OutputDirectory reports/my-unique-validation-run
```

The output directory must be inside the repository and must not already contain `results.json`. The runner builds the opt-in classes, runs the chosen scene, and rebuilds the normal solution in `finally`, removing the class from the ordinary game assembly. If the runner is forcibly terminated, rebuild `OpenAntares.sln -t:Rebuild` before normal play.

## Coverage and evidence

- Boots the real New Game screen, then loads a labelled generated rules-1 fixture through the real Load dialog for each legacy Small/Medium/Large loop. Current new-game defaults are independently covered by the Species scenario.
- Checks selectable counts, every system's map/inspector handler, and rejection of a background point.
- Exercises workforce buttons/visible shortage feedback, production/research selectors, End Turn, completions and population growth.
- Uses real Save/Load dialog confirmation handlers for pre/post-completion saves, compares full serialized content/state and restored map bounds, and checks continued play.
- Loads a deliberately corrupt QA file without replacing the current session; loads a copy of the schema-1 fixture and advances a turn.
- Checks live resizing at 800x600, 1152x648, 1600x900 and 1920x1080 without changing authoritative state.
- Repeats construction follow-up for all three sizes, four window dimensions, and home/other-system selections: 24 native-popup scenarios plus one embedded-popup visual scenario per size. Explicitly constrains the side panel to force focus scrolling; compares automatic popup coordinates with normal reopening and selects the next project through the existing command path.
- Checks the research follow-up and actual default 1920x1080 window dimensions.

`results.json` records assertions, popup geometry, requested/actual window mode, method, and retained QA save paths. `godot.log` records execution. Parent viewport PNGs omit native child windows; supplementary visual fixtures disable `ForceNative` only on their test popup, assert actual embedding, and capture the parent viewport. Ordinary native geometry checks run before that fixture change. Native popup texture readback was unavailable on the tested GL driver, so do not describe an embedded image as a native-window capture. Inspect images as well as numeric results.

This harness dispatches control signals and the map's GUI handler in-process. It does **not** prove Windows pointer/keyboard delivery, accessibility exposure, general usability, or player understanding. It reads the session/map internals for assertions without modifying production state; actions flow through the existing control handlers. The harness does not mutate live authoritative state; the separately labelled synthetic fixture is authored before session creation and validated. The approved implementation changes game source/content, while the harness remains excluded from normal play.

Saves use unique `qa-<UTC timestamp>-...` names in the game's normal `user://saves`. Existing saves are never overwritten. QA files are intentionally retained and listed in the results; no automatic deletion occurs. The invalid-save fixture will appear in the Load list and is expected to show a parse error. Use a fresh output folder for each run and preserve failed evidence.

## Expansion scenario

Pass `-Scenario Expansion` (the default remains `Legacy` for existing invocations):

```powershell
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'PATH/TO/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario Expansion -OutputDirectory reports/my-unique-expansion-run
```

Small/Medium/Large seed42 campaigns start through New Game and then load a uniquely saved frozen rules2 generated campaign, queue scout travel and settler production, save/reload pending movement and transit, repair source workforce after embarkation, travel/found explicitly, manage independent colony projects/research, resize, save pending founding and continue after a two-colony reload. Forecasts and actual published outcomes are compared. No economy outcomes are injected.

A separately labelled synthetic fixture covers two owned colonies plus an empty world in the same system, exact selection/founding, and visible-text/action/tooltip invariance under changed hidden planets, foreign ownership and units. Both retained genuine schema-1/2 saves load and continue through the real dialogs with the legacy banner and expansion controls hidden. The synthetic observations are produced by the same public observation updater as ordinary generation/resolution.

The final implementation evidence is indexed in [the expansion validation report](../../reports/EXPANSION_VALIDATION_2026-10-22.md). Automated signal dispatch and screenshots do not establish physical input, small-window comfort, player understanding or exported packaging. All QA saves and failed runs remain retained.


## Species scenario

Pass `-Scenario Species` with a new output directory. Actual New Game selects each of four species across Small/Medium/Large seed42 campaigns. Gameplay loads explicit rules-3 campaigns through ordinary dialogs after checking the current setup. All twelve loops scout/survey, build/embark, travel/found, allocate two colonies and continue after UI save/reload; a thirteenth forces Sulan harbor. Separately labelled initial fixtures exercise contrasting native/sealed maps, hidden environment/foreign-overflow UI parity and extended Wenth8→5+3 maturity with daughter capacity6. Missing-Harl, empty-list and renamed/edited-cargo content checks are included. Resize/focus checks cover800x600,1152x648,1280x720,1600x900,1920x1080. See [species validation](../../reports/SPECIES_VALIDATION_2026-10-23.md) for current counts, failures/corrections and human limits. All four validation types remain opt-in and are absent after ordinary rebuild.


## Rival scenario

Pass `-Scenario Rival` and a new output directory. Current New Game explicitly sends rules4, visible One/Harl/Peldra defaults, independent mirrors and optional Solo. Setup checks include alternative species lists, missing definitions with Load available, failed generation without session replacement, and five window sizes. Observed identity/ownership labels and tooltips are checked against hidden rival resource/order changes.

Twelve unmodified seed42 generated campaigns (human Harl, four rival species × three sizes) use real project/research/scouting/settlement controls and End Turn. Each requires actual rival second colony plus observed contact within80 core turns, real human settlement/development, pending/transit/founding saves through ordinary dialogs, and three future turns equal to uninterrupted canonical continuation. The first scout route is the farthest charted system to exercise committed transit; subsequent choices use only the human's view. Per-species research completion is witnessed in the Small campaigns; other loops record their actual completed-research counts without imposing an unapproved per-map research deadline. Additional all-human-species/Solo continuations and labelled pre-session Sulan/Wenth/race/renamed-policy fixtures remain separate from generated acceptance.

The80-turn bound verifies these core fixtures, not general balance. The harness never repairs live income/population/orders or rerolls maps. It invokes signals in process; it does not establish physical input, comfortable reading, player comprehension, cross-platform equality or release packaging. All five scenarios remain excluded from ordinary builds. See [rival validation](../../reports/RIVAL_VALIDATION_2026-10-24.md).

## Colony-development validation

Use `-Scenario Colony` with a unique output directory. The scenario uses labelled embedded rules5 campaigns through ordinary Load and actual control signals; it does not establish physical input, human readability or balance. Its initial group exercises templates, snapshots, independent switches, finite queues, explicit replacement, portable presets, the64-template bound, blocked Wenth cargo and old-campaign controls at800/1920. Retain all QA saves and failed runs. The runner verifies that metadata inspection rejects the real opt-in assembly, then always rebuilds ordinary code and requires all five harness types absent. `Verify-NormalAssembly.ps1 -AssemblyPath .godot/mono/temp/bin/Debug/OpenAntares.dll` can be run independently.

The Colony management group separately labels1/6/11/30-colony fixtures. It exercises bulk selection with a manual favorite, exception/template filters, clearable guidance, local notification paths, malformed/write-failure recovery, notification Off with derived conditions retained, twelve ordinary UI turns/ten reports, actual save/load and a two-human pending/manual-override journal. The normal owner preference path is never written by the harness; every Apply uses a unique path inside its run directory.

The portrait group captures24 labelled gallery pages: all4 types ×3 environments ×8 signatures at800/1920, including combinations with zero generation weight. It checks stable save/load pixels, unknown neutrality, stale privacy, generic modded-type structures,180 extra cache requests and surviving visible textures after eviction/Clear. PNG inspection is visual evidence, not owner aesthetic approval. One catalogue/cache is injected from each GameScreen into inspection and management; the cap is128. Original procedural source/data add no dependency or third-party art.

## Colony development implementation — 2026-10-24

All twelve approved Native tasks are implemented on codex/colony-development-design. Rules5/writer6 new games add paid foundations/specialists, opt-in editable governors/queues/inheritance, scalable management/reports and original varied planet portraits. Genuine old campaigns remain unchanged. Fresh982 simulation tests and all five graphical gates pass (Colony582+4 provenance, Rival1146+12 provenance, Species1194, Expansion275, Legacy466+27 geometry); ordinary build0 warnings/errors and all five harnesses excluded. Final independent review/local main integration are pending; read PROJECT_STATUS.md and reports/COLONY_VALIDATION_2026-10-24.md for exact evidence/limits rather than historical counts above. Human/balance/platform/release acceptance remains open. No future crisis/fleet scope or push/release/cleanup.

The final Colony group adds four unchanged seed42 Small current-form campaigns, all four human species/one Peldra rival, actual research/building/template/queue/founding controls, no live outcome injection or rerolls. Every UI turn matches an independent canonical continuation. Each campaign completes all foundations/specialist before success and compares three future turns after UI reload. A separate labelled two-human fixture covers pending Ready Save/Load; scale/gallery/cargo fixtures remain explicitly labelled. See the retained machine summary for provenance rows and exact counts.
