# Godot runtime validation

This opt-in harness tests the existing First Playable presentation inside graphical Godot. It is excluded from ordinary compilation by the root project's `tests/**` exclusion and is added only through `Validation.targets` during this runner.

From the repository root, with restored dependencies and a Godot 4.7.2 **.NET console executable**:

```powershell
./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'PATH/TO/Godot_v4.7.2-stable_mono_win64_console.exe' -OutputDirectory reports/my-unique-validation-run
```

The output directory must be inside the repository and must not already contain `results.json`. The runner builds the opt-in class, runs its scene, and rebuilds the normal solution in `finally`, removing the class from the ordinary game assembly. If the runner is forcibly terminated, rebuild `OpenAntares.sln -t:Rebuild` before normal play.

## Coverage and evidence

- Starts each of Small/Medium/Large through the real New Game screen's Start handler with seed 42.
- Checks selectable counts, every system's map/inspector handler, and rejection of a background point.
- Exercises workforce buttons/visible shortage feedback, production/research selectors, End Turn, completions and population growth.
- Uses real Save/Load dialog confirmation handlers for pre/post-completion saves, compares full serialized content/state and restored map bounds, and checks continued play.
- Loads a deliberately corrupt QA file without replacing the current session; loads a copy of the schema-1 fixture and advances a turn.
- Checks live resizing at 800x600, 1152x648, 1600x900 and 1920x1080 without changing authoritative state.
- Repeats construction follow-up for all three sizes, four window dimensions, and home/other-system selections: 24 native-popup scenarios plus one embedded-popup visual scenario per size. Explicitly constrains the side panel to force focus scrolling; compares automatic popup coordinates with normal reopening and selects the next project through the existing command path.
- Checks the research follow-up and actual default 1920x1080 window dimensions.

`results.json` records assertions, popup geometry, requested/actual window mode, method, and retained QA save paths. `godot.log` records execution. Parent viewport PNGs omit native child windows; supplementary visual fixtures disable `ForceNative` only on their test popup, assert actual embedding, and capture the parent viewport. Ordinary native geometry checks run before that fixture change. Native popup texture readback was unavailable on the tested GL driver, so do not describe an embedded image as a native-window capture. Inspect images as well as numeric results.

This harness dispatches control signals and the map's GUI handler in-process. It does **not** prove Windows pointer/keyboard delivery, accessibility exposure, general usability, or player understanding. It reads the session/map internals for assertions without modifying production state; actions flow through the existing control handlers. The root entry scene, game source, rules, content and project settings remain unchanged.

Saves use unique `qa-<UTC timestamp>-...` names in the game's normal `user://saves`. Existing saves are never overwritten. QA files are intentionally retained and listed in the results; no automatic deletion occurs. The invalid-save fixture will appear in the Load list and is expected to show a parse error. Use a fresh output folder for each run and preserve failed evidence.
