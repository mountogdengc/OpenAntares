# Globe prototype checks, 2026-11-12

Requested scope: compile and run the existing globe prototype checks against the actual dirty primary checkout, main 240d16d5. No commits, integration, publishing or new gameplay.

Final run: `prototype-20261112-04/summary.json`; shared queue job `f9ce4670518f42d6b183dcfeaa627556`, exit 0. Native Godot 4.7.2 .NET, GL Compatibility/ANGLE on NVIDIA RTX 3090. Both opt-in game rebuild and mandatory ordinary rebuild pass with 0 warnings/errors. Prototype passes 68 checks. Ordinary validation namespace is empty. All 169 worker source-manifest entries match before/after and were rechecked against final files; OpenAntares.csproj SHA256 is retained separately in the run. No engine diagnostics in either log.

Coverage: published visual builder/seed/knowledge/preset invariants, bounded animation clock, paired asymmetric orientation at four phases/tilts, persistent viewport/material identities over 100 switches, three sphere layers, active/hidden/static update behavior, frozen image and explicit redraw, diagnostic captures and one layered capture.

Repairs required by the observed runs:

- `OpenAntares.csproj`: exclude `reports/**` from compilation, matching the existing src/tests/tools exclusions. Preserved source copies had caused 520 opt-in/513 ordinary build errors. No backup was moved or deleted.
- `PlanetGlobeChecks.cs`: replace the incorrect post-render `Disabled` node-property assertion with direct pixel checks proving a material change stays frozen until SetVisual requests a new frame. The existing request-mode assertion remains. Godot 4.7.2 source returns the cached requested mode: https://github.com/godotengine/godot/blob/4.7.2-stable/scene/main/viewport.cpp#L5256-L5265 . This changes validation only, with no globe runtime source edit.

Failed evidence is retained: run 01 failed compilation and ordinary restoration; runs 02/03 compiled and restored normally but failed the original static property assertion. A newline-sensitive patch did not apply before run 03, so that run repeated unchanged source. Run 04 is the final corrected-source result.

Limits: no full solution/simulation suite, unrelated UI scenarios or campaign replay was run. The game project and its simulation dependency were rebuilt. Cities GPU case, sidebar/colony integration, motion-preference integration, performance, swatch/transparency matrix, physical input/readability and owner aesthetics are outside this Prototype result. Captures show corresponding geography/orientation, but a visible 2D/3D brightness difference remains; passing orientation classification does not establish color parity or complete the plan's visual gate. UI integration remains pending.

Reproduce from F:/Projects-PCGames/OpenAntares, choosing a fresh evidence directory:

```powershell
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- pwsh -NoProfile -File tools/PlanetGlobes/Run-PlanetGlobes.ps1 -SourceRoot F:/Projects-PCGames/OpenAntares -Case Prototype -OutputDirectory reports/planet-globes-2026-11-10/prototype-FRESH
```
