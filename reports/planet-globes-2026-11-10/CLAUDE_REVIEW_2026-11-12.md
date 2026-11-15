# Planet globes — read-through review and pre-build fixes, 2026-11-12

Scope: a static read of the uncompiled globe work (Tasks 2–5/8) before its first native run, at the owner's request ("do whatever you can"). **Nothing here was compiled or run.** No dotnet or Godot is available in the reviewing session. Every change below is UNCOMPILED/UNRUN and carries no passing claim. No Git writes. Pre-edit byte copies of all eight touched files are in `claude-review-2026-11-12/before/`.

## Changes made

| File | Change | Why |
| --- | --- | --- |
| `game/Components/PlanetGlobe2D.cs` | Quad is parented and its material bound in the constructor; `_ExitTree` removed. | `_ExitTree` cleared the material but `_Ready` runs once, so a control removed and re-added stayed blank. The field-created quad also leaked if the control never entered the tree. |
| `game/Components/PlanetGlobe3D.cs` | Viewport and image parented in the constructor. `_ExitTree` now only disables viewport updates; new `_EnterTree` re-evaluates the update mode. Texture, meshes and material overrides are no longer cleared. | Same once-only `_Ready` problem. Task 7 keeps this globe alive across navigation. |
| `game/Components/PlanetGlobe3D.cs` | Atmosphere shell radius 1.07 → 1.10. Image uses a `CanvasItemMaterial` with premultiplied-alpha blending. | Matches the 2D halo extent. Alpha-blended layers on a transparent viewport leave premultiplied colour in the texture; straight-alpha display would darken the halo. |
| `planet_common.gdshaderinc` | New `planet_color`, `planet_encode`, `planet_glow`, `planet_halo`. All colour-uniform reads go through `planet_color`. | See colour and atmosphere notes below. |
| `planet_canvas.gdshader` | Defines `PLANET_CANVAS_SRGB`; lights in linear, encodes each layer, then blends clouds; halo from `planet_halo`. | Mirrors the spatial pipeline's order of operations. |
| `planet_surface.gdshader`, `planet_clouds.gdshader` | Add `planet_glow(normal)` to ALBEDO. | On-disc limb glow now lives in these two layers. `mix(s+g, c+g, a) = mix(s,c,a)+g`, so it equals the 2D "add after clouds". |
| `planet_atmosphere.gdshader` | `blend_add` → `blend_mix`; draws only the off-disc halo via `planet_halo(length(sphere_position.xy))`. | Additive blending is believed to leave destination alpha unchanged, so the halo beyond the limb would be invisible on the transparent viewport. The old rim used the shell's own normal, so it was brightest at the shell edge and cut off hard, unlike the 2D fade. |
| `tests/.../PlanetGlobeChecks.cs` | `ClockChecks` frees the clock node in `finally` instead of `using`. | Disposing a Node wrapper does not free the native node. |

## Assumptions to verify on the first native run

1. **Colour convention.** Assumed: in GL Compatibility, canvas shaders get `source_color` uniforms as authored sRGB with no output encoding, while spatial shaders get them linearized and the scene shader encodes output. If the Task 4 swatch check shows the canvas globe now too bright or washed out, delete the `#define PLANET_CANVAS_SRGB` line in `planet_canvas.gdshader`; that restores the previous canvas behaviour without touching anything else.
2. **Premultiplied viewport texture.** Assumed mix blending on the transparent target yields `rgb = src*a`, `a = a`. If the 3D halo looks too bright over light backgrounds, remove the `Material` line on `_image`.
3. **Unchanged test assertion, likely wrong.** "Static frame completes" (PlanetGlobeChecks.cs, after `SetActive(true)`) expects `RenderTargetUpdateMode` to read back `Disabled` after a render-once. The node property is believed to keep reporting `Once`; only the rendering server switches. Production code re-requests a frame either way. Left untouched so the native run decides.

## Not changed — for the implementer

- **Dry-world clouds:** `cloud_cover` 0.12 puts the cloud threshold at 0.88–0.96 of a value-noise field that rarely gets there, so dry terrestrial worlds will show almost no cloud. Tuning call.
- **Small-globe city shimmer:** the candidate field at frequency 92 is about one noise cell per pixel on a 34-pixel disc, so lit sites will probably flicker as the planet turns. Task 8 already allows cluster filtering at small sizes; expect to need it.
- **`AllWater`** is always false from the builder; only a synthetic fixture can reach that path, as the plan intends.

## Open owner decision: lights on planets the viewer does not own

The plan's Population decision and the builder give zero lights to any planet without a matching owned colony. The owner has since said he wants unowned planets lit, driven by **last-observed population snapped to coarse bands** (an estimate, not an exact count).

This cannot be done in presentation alone: `KnownPlanetView` and `PlanetObservation` carry no population. It needs a simulation and save change, which the current plan forbids ("no src/content/core/save/schema change"). A possible shape, for owner approval before any work:

- At observation time, record a coarse band (for example 0–3) on `PlanetObservation`, derived from the colony's population against content-defined thresholds. Stale observations keep the band they were recorded with.
- Publish it on `KnownPlanetView`; the builder maps band → city level when there is no owned colony.
- Consequences: new saved field, rules/writer version step, determinism and knowledge-leak tests, and plan acceptance rows 191–192 reworded ("reveals no *current* population").

Until the owner decides, the implemented behaviour (no foreign lights) stands.

## Still blocking

Unchanged from the ledger: the shared queue cannot write `F:/Tools/CodexTestRunner/state/requests`, so Builder/Prototype have never run.
