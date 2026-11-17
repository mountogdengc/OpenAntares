# Planet globes v2 (stylized-modern look), 2026-11-15

Owner request: planets looked low-detail; replace the retro look with a stylized-modern one.

## What changed

Branch `feature/planet-globes-modern`. Two files:

- `game/theme/planets/planet_common.gdshaderinc`: generator rewritten as v2.
- `game/theme/planets/planet_canvas.gdshader`: the `show_clouds` uniform moved into the shared include, so the
  surface can skip cloud shadows when a small globe hides its clouds.

No C#, theme, catalogue or JSON changes. The uniform contract, the binder and every shader entry point used by
the game and the validation shader (`planet_terrain`, `planet_land`, `planet_sites`, `planet_surface`,
`planet_cloud_mask`, `planet_glow`, `planet_halo`, `planet_illumination`, `planet_local`, colour adapters and
the diagnostic path) keep their names and signatures. `planet_halo`, `planet_illumination`, `planet_land` and
the colour adapters are unchanged.

Look changes:

- Sampling is continuous. `sampling_grid` is still declared and bound but no longer snaps the surface.
- Terrain is domain-warped with rotated octaves, three extra fine octaves and ridged highlands. The first four
  octaves keep the v1 amplitudes, so sea thresholds keep roughly their land share.
- Detail fades by pixel footprint, so 32 to 64 pixel globes stay clean instead of shimmering.
- Land has relief shading, moisture and height biomes, a pale shore band, snow lines and noisy ice-cap edges.
- Water has a depth gradient, a bright shelf at the coast and a sun glint.
- Clouds are layered, warped streaks with a feathered edge and cast shadows on the surface.
- Airless worlds have impact craters. Banded worlds have turbulent belts and oval storms.
- The atmosphere rim is stronger on the lit limb and has a thin bright edge line.
- City sites are unchanged at small sizes; on large globes each site breaks into individual lights.
- Structural variants 1 and 5 sample a rotated, warped lattice (v1 showed grid-aligned creases once the
  surface was no longer quantized).

## Verified

Rendered in Godot 4.7.2 (GL Compatibility, Mesa llvmpipe, Linux) in a standalone scene that loads these shader
files unchanged: canvas globes at 40, 128, 380 and 780 pixels, and the three-layer spatial globe at 280 pixels.
All five shaders compile and render with no shader errors. Images are in this folder.

## Not verified

- The game itself was not built or run, and no validation suite was run. `PlanetGlobeChecks`,
  `PlanetGlobeFinishChecks` and `PlanetGlobeIntegrationChecks` need a run on Windows. The checks most likely to
  need attention are the canvas/spatial brightness parity bounds, the asymmetric diagnostic pattern match, the
  city coverage bounds and the 4/50-planet rendering budgets.
- Shader cost. On software rendering, two 512 pixel globes took about 4.7 times as long per frame as v1
  (181 ms against 38 ms). Real GPU cost was not measured.
- Windows ANGLE/D3D shader compilation, which the game uses, was not exercised.
- Saved planet appearance is seeded as before, but coastlines differ from v1, so existing planets look different.

## Follow-up, same day: load lag

Owner ran the branch in the game: the look is accepted, but starting a new game and opening the colony view
each paused noticeably. The likely cause is first-use compilation of the larger shader (canvas shader at new
game, the three spatial shaders at the colony view); this was not confirmed on the PC.

Change: relief shading now takes the height gradient from screen-space derivatives instead of two extra terrain
samples, and craters are evaluated once instead of three times. The surface shader's inlined size drops to
roughly a third for terrain and craters. Every derivative now sits ahead of any early return or branch, and the
canvas cloud mask is evaluated unconditionally for the same reason. The look is unchanged apart from slightly
crisper relief on large globes.

Measured on software rendering only (Mesa llvmpipe, two 512 pixel globes): v1 59 ms per frame, first v2
200 ms, this version 132 ms. First-frame times were too noisy to rank reliably. The Windows ANGLE/D3D compile
time, which is what the owner experienced, was not measured.
