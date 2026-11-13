# Planet globe owner check

Prepared 2026-11-12 for the approved 2026-11-10 globe plan. This is the remaining human look/readability/physical-input check; automation does not establish those judgments.

## Exact project, build and launch

Use `F:/Projects-PCGames/OpenAntares/project.godot` in Godot 4.7.2 .NET with the project's existing ANGLE Compatibility settings. Close any older running game first. After the final globe validation worker has restored the ordinary build, press **F5** in that same project. No Docker or export is required.

The ordinary game build is the build retained by the worker. If it needs rebuilding later, from `F:/Projects-PCGames/OpenAntares` run:

```powershell
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- dotnet build OpenAntares.csproj --nologo --disable-build-servers
```

Keep that build in the shared queue. The final implementation report identifies the successful ordinary build and exact source evidence.

## Prerequisites

Use a disposable new Small game, seed42, Harl with one Peldra rival. This avoids changing an existing campaign while observing growth. Keep the current renderer, VSync and frame cap. Check both800x600 and1920x1080 if available; record Windows display scaling. Do not overwrite an existing save.

## Numbered observations

1. **System selection:** inspect the home system. Each observed planet should have a globe and an orbit number. Click directly on several globe discs, including your colony and an unclaimed planet. The corresponding planet stays selected; Manage colony is enabled only for your colony. An unsurveyed system should show its existing unknown information without invented planet buttons.
2. **Compare the same world:** select your colony, then turn off **Game → Animate planets**. Open **Manage colony**, then return to Galaxy and compare its small globe. Geography, poles, phase and lighting should describe the same world. Pixel detail can differ with size; the large dark-side brightness mismatch should be absent. Look for hard halo edges, dark rectangles or distracting cloud artifacts.
3. **Colony controls:** at800x600, scroll through the colony page. Population, workforce and construction remain readable and reachable below the globe. Move one worker with the existing controls and confirm the ordinary output update. The globe should not disappear or restart its geography.
4. **Growth and navigation:** use ordinary End Turn actions until population changes. Return to the same colony after any report. Its lights should develop without moving the underlying geography. Switch pages and colonies when another owned colony is available; the selected world should remain correct. Automation separately checks one-population lights over a full rotation and100 alternating owned observations.
5. **Motion preference:** turn Animate planets on and watch the rotation, then off and confirm it freezes. Turn Reduced effects on/off: it should freeze motion without rewriting your stored Animate planets choice. Visit Main menu, Resume campaign, and confirm the preference persists. Resizing or changing a selected planet while frozen should display current data.
6. **Appearance judgment:** judge the34-pixel sidebar discs and256-pixel colony control. Report whether terrain, cloud cover, atmosphere and warm city lights look clear and restrained. Say which planet/type/size needs tuning rather than treating a passing numeric test as aesthetic approval.

## Report back

Provide: project path; window size and display scaling; game seed/rules if known; planet name/orbit/type; Animate planets and Reduced effects states; numbered action; expected versus observed result; screenshot if helpful; whether the appearance and readability are acceptable.

A passing observation closes the human look/readability/input gate. A failed observation identifies a bounded presentation follow-up and its focused checks. It does not request a broad simulation suite or authorize commits, publishing or release.
