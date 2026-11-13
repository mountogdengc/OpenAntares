# Procedural planet globes implementation

Completed2026-11-12 for the approved2026-11-10 plan. Verified local automated/rendered scope; owner physical/readability/aesthetic acceptance remains Pending. No new numbered milestone is assigned and M14/parent requirement statuses are not closed by this feature. Work remains uncommitted in the owner's primary checkout.

## Delivered behavior

- Existing SystemPlanet buttons now contain40px canvas globes, giving34px discs, with orbit labels, existing selection/keyboard behavior and mouse-input pass-through. Unsurveyed systems invent no planet keys. Compact layout preserves the globe's own minimum height.
- Colony management lazily creates one256px 3D viewport with persistent surface/cloud/atmosphere nodes, mesh resources and materials. It survives ordinary refresh, workforce/turn updates,100 alternating owned-world observations and page reopening. Hidden pages disable viewport work. The remaining static portraits/cache continue serving system inspection and management.
- One Main-owned bounded presentation clock survives screen changes. Game → Animate planets persists in presentation.cfg and defaults on for old settings. Reduced effects OR disabled animation freezes motion without rewriting the stored preference. Static data/size changes request a fresh frame.
- Both views share seeded terrain, masks, city sites, orientation and linear-light shading. Known current owned population drives warm night lights against a stable20-unit reference; capacity does not dim lights. Stale mixed inputs, foreign/unknown population and legacy appearance fallbacks do not infer current population. No simulation/data schema was added for foreign lights.
- Airless layers disable correctly; black/white halo compositing is checked. Four actual visual presets plus labelled synthetic banded/all-water examples and32/40/64/128px controls are captured. Same-seed structural variants1/5 are now distinct. City site sets are sparse/monotonic and regrowth restores them.

## Final evidence and scope

Queue job `e3cb4aed7f1d423fbb686da3a8268903`, exit0; native Godot4.7.2.NET, GL Compatibility/ANGLE on NVIDIA RTX3090. Final run: `planet-globes-2026-11-10/finish-20261112/feature03`.

**699 focused checks pass.** Both opt-in game rebuild and ordinary game rebuild have0warnings/errors. The ordinary assembly has zero validation-namespace types and no forbidden harness/helpers. Engine and console logs have no diagnostics. All 171 worker source hashes match before/after and final files. All 287 protected src/content source inputs match the start-of-continuation hashes. Project and genuine old-save input hashes are additionally retained.

Checks include actual planet-globe-area input at800x600/1920x1080; selection/ownership/unknown-state behavior; workforce and End Turn routes; current save/load globe reconstruction; genuine legacy-v1/v2 save routes and unchanged canonical data/RNG; one shared clock and settings across Main menu/Resume;100 permitted population refreshes;100 alternating two-owned-world published fixtures with updated displayed seed/preset and all three node/mesh/material identities retained; inactive/hidden/static/resize/re-entry behavior; nine color comparisons; halo transparency; GPU land/site monotonicity/regrowth; and one-population lights sampled at24 phases at40/128px.

The historical colony portrait resize/save-load assertions were retained using the same owned world's StarPanel portrait. Its adapter and shared native-validation dependencies compile with the globe opt-in build. Historical campaign scenarios themselves were not rerun. The only executed scene is the globe Feature scene. No full solution/xUnit suite, unrelated fleet/campaign matrix, export/platform/security/balance campaign or second implementation review was run.

## Rendering measurements

Each row is a warmed30-second cached-static-portrait baseline and30-second globe measurement on the same actual GameScreen/driver/window/layout. Four- and50-planet configurations are loader-validated synthetic campaigns made entirely in memory, with explicit hazard choices. Total stress controls50;27 actually intersect the clipped sidebar. The colony viewport is256x256 and there is at most one. Driver, VSync and frame cap are unchanged. Both modes retain the shared clock service; this is a matched rendering-cost baseline, not an old-source executable comparison.

| Planets | Page | Visible canvas globes | Baseline p95 frame ms | Globe p95 frame ms | Added frame ms | Added Godot process-monitor ms |
| ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 4 | Sidebar | 4 | 37.578 | 37.414 | -0.164 | -0.238 |
| 4 | Colony | 0 | 31.840 | 32.019 | +0.179 | +0.244 |
| 50 | Sidebar | 27 | 38.026 | 37.250 | -0.776 | -0.092 |
| 50 | Colony | 0 | 31.817 | 32.074 | +0.256 | +0.168 |

All four pairs meet the stated fallback added-cost budget of at most2ms. The largest added p95 wall-frame time is0.256ms and process-monitor delta0.244ms. Baseline p95 is31.8–38.0ms and already misses the60FPS budget, so this is not a claim that the complete game achieves60FPS. CPU rendering/setup, frame counts, resource/node counts and raw timing summaries are retained in performance.json and eight per-window timing records. GPU timestamp measurements returned unavailable/null on this driver; no GPU-time budget is claimed. CPU/process/wall readings have their recorded engine meanings and are not substituted for an unavailable GPU measurement.

Halo mean absolute RGB error is below0.001 over both black/white backgrounds. One-population lights produced 117 aggregate positive small-globe sample pixels and 1058 detail pixels across24 phases; these are visibility samples, not physical readability approval. Small residual canvas/spatial color differences from engine approximation/mesh sampling remain within documented bounds, as in BRIGHTNESS_CORRECTION_2026-11-12.md.

## One whole-feature review and correction pass

One fresh read-only review by globe_final_review found no Critical issue. All three Important findings were closed: a direct mixed stale/population guard; true two-world switching and all-layer identity assertions; and the retained owned-portrait regression adapter. Two small original-plan completions were also closed: focused orbit-label/globe width assertions and a distinct fixed-seed structural5 field. Review red01 records staleSafe=false/variantsDiffer=false before production corrections; feature03 records both true. Existing switching behavior already worked; its finding was missing coverage, now exercised by400 alternating-switch assertions across the two window sizes. No second review ran.

Reviewer set aside owner appearance/input judgment, foreign-population/new gas/all-water gameplay, and other hardware/renderer performance. Disposition: owner checklist handles the first; the other items remain outside this presentation-only plan. No hidden population field or new content type was added.

## Decisions and preservation

- Continued the established owner-authorized dirty-primary arrangement rather than replacing current UI work with a HEAD-only checkout. Fresh before copies and protected-source hashes are in finish-20261112. Cost: the feature shares the existing working tree; no isolated/clean-tree claim is made.
- Closed the two reversible small review items in the same pass under the owner's explicit finish request, completing planned geometry/structural-variety requirements. Cost: unpublished globe signature5 changes its appearance; saved descriptor/schema and old portrait renderer stay unchanged.
- Used matched static portraits on the actual current layout for cost comparison, retaining the clock in both modes; this isolates rendering overhead. It is explicitly not a baseline binary of the old UI. Existing low baseline frame rate is reported rather than attributed to globes.
- Earlier draft bootstrap/checkbox/history statements remain historical. Native build/entry/exit/restoration evidence supersedes the original queue-permission blocker; repeating a bootstrap or full unchanged simulation suite would add no directly affected coverage.

All prior failed evidence, backups, worktrees/branches, protected source, old saves and reports remain. No commit, merge, push, release or cleanup.

## Retained development failures

sidebar-red01: missing globe integration; integration-green01: compact target rule shrank globe buttons. integration-green02 passes105 after the scoped height fix. layers01 passes95. feature01: synthetic setup omitted required ameba choice, before any timing. feature02: first sidebar pair passed, then a test retained wrappers replaced by legitimate navigation refresh. review-red01 reproduces stale/variant findings. Every failing worker's ordinary rebuild completed; those records remain failures. Only feature03 is the complete final Feature pass.

## Handoff

Stop the old game and press F5 in `F:/Projects-PCGames/OpenAntares/project.godot`. The worker has restored the ordinary assembly. Follow `docs/playtests/planet-globes-2026-11-10.md` for exact build prerequisites, numbered physical/appearance observations and report-back fields. A passing owner observation closes that remaining gate; a failed observation identifies a bounded visual follow-up. Source, controls and native functional evidence are ready for that observation.
