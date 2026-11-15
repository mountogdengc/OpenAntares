# Globe brightness correction, 2026-11-12

The substantial canvas/spatial brightness mismatch is corrected for the project's native GL Compatibility path. Main working tree remains uncommitted. No gameplay, palette, simulation, content schema, sphere geometry or alpha-blending mode was changed.

## Cause and correction

The canvas globe explicitly decoded authored sRGB colours, performed custom lighting in linear space and encoded the result. Spatial layers assumed their source_color uniforms and ALBEDO were already linear. In Compatibility they are sRGB. Custom shading therefore multiplied display values and skipped the matching output conversion, crushing the spatial nightside.

The shared colour adapter now uses OUTPUT_IS_SRGB for spatial shaders. Surface, clouds and atmosphere explicitly encode ALBEDO when that renderer requires it. Forward+/Mobile retain their linear contract, but those renderer branches were not executed in this task. Existing non-HDR canvas behaviour is retained. The population/terrain/site functions are unchanged.

Primary engine references: [spatial output convention](https://docs.godotengine.org/en/4.7/tutorials/shaders/shader_reference/spatial_shader.html), [4.7.2 Compatibility fragment path](https://github.com/godotengine/godot/blob/4.7.2-stable/drivers/gles3/shaders/scene.glsl#L2203), [engine colour approximations](https://github.com/godotengine/godot/blob/4.7.2-stable/drivers/gles3/shaders/tonemap_inc.glsl#L11).

## Fresh verification

Final queued Prototype job dcaa1dd3c0e64748a3fca2aad25fc07e exits 0. Godot 4.7.2 .NET, GL Compatibility, ANGLE/RTX 3090. 87 checks pass, including nine new paired colour comparisons: diagnostic colours, uniform gray lighting and fully layered globes, each over black/gray/white. Each comparison samples 2,979 interior pixels and independently bounds whole-disc, day and night mean absolute RGB error. Complete screen and raw viewport captures are retained. Captures were visually inspected for broad brightness alignment. This is automated/rendered technical evidence, not owner aesthetics or physical acceptance.

Both opt-in and ordinary game rebuilds have 0 warnings/errors. The ordinary validation namespace is empty. All 169 source-manifest entries are unchanged through the run and match current files; the project-file hash is separately retained. No diagnostics in either engine log. Existing prototype checks remain intact.

Comparison below uses mean absolute channel error in 0-255 colour levels. Black/gray/white interior metrics are identical because these samples are inside the opaque disc.

| Fixture | Before mean | After mean | Before night | After night |
| --- | ---: | ---: | ---: | ---: |
| diagnostic | 48.66 | 0.10 | 66.00 | 0.00 |
| midtone | 16.92 | 0.71 | 28.56 | 2.07 |
| layers | 17.62 | 1.96 | 34.29 | 3.86 |

The uniform gray whole-disc difference falls from 16.92 to 0.71 levels; its nightside falls from 28.56 to 2.07. Tiny remaining differences are expected: Compatibility applies a cubic sRGB-to-linear approximation and re-encoding internally, and the spatial mesh interpolates normals/sampling coordinates. Analytic evaluation of the engine formula independently predicts up to 2.503 levels of round-trip difference for this swatch, before quantization. An example dark pixel is canvas28/spatial26; the day center is117/117. This is much smaller than the original mismatch.

## Regression and retained evidence

- brightness-20261112-red01: all nine comparisons failed before any runtime fix; ordinary game restoration succeeded. Queue acf130f382b24ea280d355fedbba5a01.
- brightness-20261112-green01: shader fix removed the large discrepancy. Six comparisons passed; the gray swatch's original arbitrary 0.008 bound failed at0.00813008 on the nightside. Ordinary restoration succeeded. Queue587317ab6ca94e39a0a1849ae78f21d0.
- brightness-20261112-green02: unchanged runtime correction; gray-swatch bound calibrated to four colour levels, accounting for the independently predicted2.503-level engine error plus quantization/interpolation. Diagnostic0.008 and layered0.025 bounds were unchanged. All87 checks and nine comparisons pass. Failed runs and pre-edit source copies remain preserved.

Final detailed records: brightness-20261112-green02/summary.json, results.json, brightness-metrics.json, source-before.json/source-after.json, ordinary-metadata.json and build/engine logs. Source backups: brightness-20261112-before/.

## Scope limits

No full solution/simulation suite, unrelated UI case, campaign replay, Cities GPU case or performance campaign ran. Only the game and direct simulation project dependency were rebuilt. No claims for other renderers/drivers/HDR2D, full halo/swatch/transparency acceptance, physical readability, sidebar/colony/menu integration or feature completion. Existing screenshot captures expose compositing for follow-up, but numeric brightness metrics intentionally cover the disc interior only. No commit, push or cleanup.

Run the existing queued worker with Case Brightness for only the new regression, or Prototype for it plus the existing prototype checks, always using a fresh evidence directory.
