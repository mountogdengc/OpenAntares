OpenAntares: Procedural Planet Globes Spec
Nov 10, 2026 · @David Morris
Goal and scope
Planets render as spinning globes generated entirely in code, with no modeled or painted assets. The game stays 2D; the globes are the only 3D-looking element.
Revision 2. It incorporates Codex's review of 2026-11-10. The architecture is unchanged; the guarantees, the city growth rule, and the data boundaries are tightened.
Two renderers share one planet definition:
• Sidebar globe: a small globe per planet when a system is selected. Drawn by a 2D shader that fakes a sphere.
• Colony globe: one large globe on the colony page. Drawn as real 3D in a SubViewport. Only one exists at a time.
In scope: rotating surface, atmosphere glow, day and night sides, and city lights that spread as the colony grows.
Out of scope for this pass: rings, moons, terraforming transitions, orbital structures, settlement anchors with true outward expansion, race-based light colors, and any change to the galaxy map.
Proposed style: a restrained retro look, with limited rendering resolution and controlled shading, to fit the Master of Orion 2 direction. The globe math stays independent of that choice. David to confirm before antialiasing and glow are tuned.
Next step is a small rendering prototype (build steps 0 to 2), not full UI integration.
Planet visual data
Both renderers read the same PlanetVisual, so the sidebar globe and the colony globe are recognizably the same world.
PlanetVisual lives in the presentation layer only. The simulation gains no Godot dependency. It is derived by one builder function and never saved.
Field
Purpose
Proposed source
seed
Offsets the noise fields
Stable planet identity, optionally combined with the world seed
surface_mode
Selects the algorithm: terrestrial, barren (no water), or banded gas giant
Planet-type preset
palette
Deep water, shallow water, lowland, highland, polar colors
Planet-type preset
sea_threshold
Height cutoff between water and land
Preset, tuned by eye
ice_cap
Polar cap extent, 0 to 1
Preset
cloud_cover
0 to 1; 0 for airless worlds
Preset
atmosphere_color
Rim glow tint
Preset
atmosphere_density
Rim glow strength; 0 disables it
Preset
rotation_speed
Radians per second
Derived from seed and preset
axial_tilt
Radians
Derived from seed and preset
rotation_phase
Starting rotation angle
Derived from seed
city_light_level
Colony development, 0 to 1
Permitted colony population; see City lights
city_light_color
Light tint
One configurable default this pass
Rules for the data:
• sea_threshold is a height cutoff, not a percentage of surface. Actual water coverage varies by seed.
• Barren worlds turn water off through surface_mode. They do not rely on the noise never dropping below a threshold.
• Terrain, clouds, and cities each get their own sub-seed derived from seed. Changing cloud generation later must not move cities.
• The seed never comes from the planet's name, owner, population, position in a mutable list, or GetHashCode().
• Presets live in a presentation-only data file. They stay out of any ruleset or content snapshot that gets serialized into saves.
• Presets may be overridden only by planet properties that exist today and that the viewer is permitted to see.
Source names in the table are proposals. Step 0 of the build order confirms the real fields.
Sidebar globe (2D shader)
Each sidebar planet is one flat square quad with a canvas_item shader. No 3D nodes are involved.
The quad has two regions: the planet disc, and a padded ring around it for the atmosphere halo. Pixels are discarded only when they fall outside both.
How the shader fakes a sphere:
1. Map the quad's UV to disc coordinates, flipping Y so that up on screen is north.
2. Reconstruct the sphere normal for that pixel: z = sqrt(max(0, 1 - x*x - y*y)). The clamp keeps the square root valid at and beyond the edge.
3. Rotate a copy of that point by spin and tilt to get the planet-local point.
4. Sample shared terrain, clouds, and city fields at the planet-local point.
5. Light the pixel using the unrotated normal and the fixed light direction. The terrain turns under a day and night boundary that stays put.
6. Add the atmosphere: a rim glow inside the disc and a soft halo in the padded ring.
7. Add city lights on the night side (see City lights).
Requirements:
• Disc diameter is 85% of the quad's width. The quad keeps a square aspect ratio.
• Edge antialiasing width is based on screen pixels, so the edge stays crisp at 32 px.
• All PlanetVisual fields are shader uniforms. One shared shader, one material instance per planet.
• A reusable PlanetGlobe2D component wraps the quad and takes a PlanetVisual. It follows the existing system-card planet selection and adds no selection state of its own.
• Clouds: proposed as a simplified cloud mask at 64 px and larger, omitted below that. To be judged at real sizes.
• If the retro style is confirmed, the shader quantizes its sampling coordinates to a fixed grid. No per-planet viewport is added.
• It must read correctly from 32 px to 128 px, judged over a full rotation.
Colony globe (3D in a SubViewport)
The colony page shows one real 3D planet, rendered to a texture and displayed in the 2D UI.
PlanetGlobe3D is a reusable component. Whether C# builds its node tree or it has a thin scene wrapper follows the project's existing convention.
• SubViewport with its own 3D world, a transparent background, and its own environment
    ◦ Camera3D, fixed and orthographic, to match the sidebar's projection
    ◦ Surface: SphereMesh, opaque, writes depth
    ◦ Clouds: a slightly larger sphere, alpha blended, rotating at a different speed
    ◦ Atmosphere: a larger sphere again, additive, bright at the rim and clear at the center
Lighting and color:
• All three materials are unshaded and call the same shared lighting function as the sidebar. There is no light node, so nothing gets lit twice and colors match the sidebar by construction.
• The environment has fixed exposure and tone mapping. The component does not depend on global glow settings.
• City light glow is a soft falloff drawn by the shader. If engine glow composites cleanly on the project's renderer it may be used, but the look must not require a renderer change.
Geometry and layering:
• The vertex stage passes the mesh's local position through a varying; the fragment stage normalizes it and samples noise there. Fragment-stage VERTEX is view space and must not be used for sampling.
• Layer order is set explicitly with render priority and face culling. Concentric spheres share a center, so default transparency sorting cannot be trusted.
• Far-side cloud and atmosphere layers must not show through the planet.
• The halo must composite cleanly over both light and dark UI backgrounds.
• Cloud and atmosphere layers hide themselves when cloud_cover or atmosphere_density is 0.
Behavior:
• One instance. set_planet(visual) only updates uniforms and layer visibility. Nothing is rebuilt per planet.
• While a page stays open, the globe refreshes its dynamic parameters when colony data changes, through the existing presentation update path. A population update never regenerates geography or city sites.
• No external textures or meshes. Everything is primitives plus shaders.
Shared noise and consistency
Both shaders include one .gdshaderinc file holding terrain, masks, palette lookup, and lighting. Its functions use no canvas-only or spatial-only built-ins.
The land mask is shared exactly. Both renderers use the same base terrain function, the same octave count, and the same normalization to decide land, water, and city eligibility. The colony globe may add detail octaves, but only to surface color and shading. Detail never decides whether a point is land.
Must match between views
May differ by display size
Continents and major coastlines
Fine surface detail
Orientation and spin direction
Detail filtering and antialiasing
Palette and planet-type identity
Strength of small water highlights
Potential city locations
How tiny city clusters are drawn
Axial tilt and rotation phase
Cloud detail and atmosphere quality
The target is the same world with the same geography, allowing normal differences in sampling and presentation.
Noise is computed in the shader. No noise textures are generated or stored, unless step 0 finds the renderer needs them.
Coordinate and lighting contract
Both renderers follow one convention, so shared noise cannot come out mirrored, flipped, or spinning the wrong way.
• North is +Y in planet-local space and up on screen.
• At phase zero with no tilt, planet-local +Z faces the camera.
• UI coordinates increase downward; the 2D shader flips Y before reconstructing the sphere.
• Spin is applied first, about the planet's own north axis. Tilt is applied second.
• Positive rotation moves surface features left to right on screen.
• The starting angle is rotation_phase. Both shaders compute the angle from the same phase, speed, and clock.
• One light direction constant is shared by both shaders. Lighting uses the geometric normal in the fixed frame, never the rotated sampling point.
The prototype proves this with an obviously asymmetric test pattern before any noise is used.
City lights and colony growth
City lights are driven by one number, city_light_level, from 0 to 1.
• A separate noise field, with its own sub-seed, marks potential city sites. A point is lit when its value is above a threshold.
• The threshold drops as the level rises. Raising the level keeps every existing lit site and reveals more. Existing clusters may expand, and new clusters may appear elsewhere.
• If population falls, sites stay fixed and the lit extent shrinks. Regrowth reuses the same sites.
• Level 1 is a dense settlement pattern with a capped coverage. It never lights every land pixel.
• Lights appear only on eligible surface and only on the night side, fading in across the day and night boundary.
• Lights are emissive, so they stay bright in shadow.
• At level 0 the shader skips the city light work.
Growth mapping, done in the builder and not in the shaders:
• The level is population measured against a stable visual reference, proposed as a fixed value per planet-size category. It is not population divided by current capacity, because a capacity upgrade would then dim a growing colony.
• The curve is eased so a new colony shows a visible cluster. It is tuned at real sidebar size over a full rotation, since a small colony's only lit sites could sit on the far side.
• No historical maximum is saved to prevent shrinking.
Open point: if fully ocean-covered worlds can be colonized, a land-only rule would make them look empty. Proposed fallback is to treat the whole surface as eligible on those worlds.
Information and simulation boundaries
A visual can still leak information or disturb the simulation. These rules prevent both.
• The builder receives only the planet and colony data the current viewer is permitted to inspect, through the existing inspection boundary.
• Unknown, partly surveyed, and stale planets each have a defined look. Proposed: an unsurveyed planet uses a neutral placeholder globe, and stale data renders the last known state.
• When population is unknown, lights are driven by the last known value or are absent. The UI's existing unknown indicators carry the meaning, so dark never reads as confirmed empty.
• Building, opening, closing, or regenerating a globe never consumes or reseeds the simulation's random-number generator.
• Within the same generator and preset version, save and load reconstruct the same geography and city sites. The exact animation frame does not survive a restart.
• Save contents and format are unchanged.
Performance rules
• Sidebar: one quad and one draw per planet, no viewports, no per-planet per-frame script work.
• This feature owns at most one SubViewport. Other systems remain free to use their own.
• The colony render target is sized to its display area. 768 px square is a ceiling, not a default; it has 2.25 times the pixels of 512 px. A retro style would use a lower logical resolution, scaled up without smoothing.
• Hidden globes submit no rendering work and run no per-globe animation updates. Retained resources still use memory.
• Performance is checked twice: a small check on the project's renderer during the prototype, and again on the finished feature with the largest supported system view.
Colony viewport lifecycle:
Event
Expected behavior
Colony page opens
Apply planet parameters and size, then enable rendering
Planet changes
Update uniforms and layer visibility; recreate nothing
Colony page closes
Explicitly disable viewport updates
Static mode enabled
Freeze surface and cloud phases, render once, keep the result
Data or size changes in static mode
Render once again
Static mode is a motion preference first. On the colony globe it also saves rendering. On the sidebar it stops rotation but the shader still runs, so no performance gain is promised there without measurement.
Animation clock:
• Rotation uses one shared presentation clock, set once per frame as a global shader value. This is the only per-frame script work the feature adds.
• The built-in shader TIME is avoided because it wraps after an hour by default and would make planets jump.
• No rotation jump is allowed at any clock wrap.
• Proposed: globes keep rotating while the game is paused, since they are cosmetic. David to confirm.
Acceptance criteria and build order
Done means:
[ ] Selecting a system shows a spinning globe for each planet in the sidebar
[ ] Opening a colony page shows a 3D globe with surface, clouds, and atmosphere
[ ] With rotation frozen, both views agree on geography, orientation, and potential city sites
[ ] Adding colony detail does not change the shared land mask
[ ] Each planet type is visually distinct, and two planets of the same type differ by seed
[ ] Airless worlds show no atmosphere or clouds
[ ] City lights appear on the night side and spread as population grows; existing sites stay in place
[ ] A population update changes the lights while the colony page stays open
[ ] A capacity-only change does not dim the lights
[ ] Unknown or stale inspection data reveals no undiscovered conditions or current population
[ ] Opening globes does not advance simulation RNG state or alter serialized data
[ ] Repeated colony switching does not accumulate viewports or materials
[ ] Hidden and static update modes behave as the lifecycle table says
[ ] A clock wrap produces no rotation jump
[ ] Small colonies, airless worlds, gas giants, and any colonizable ocean worlds read clearly at real display sizes
[ ] No image or mesh asset files were added
[ ] Older saves load and look correct, and save format is unchanged
Build order. Steps 0 to 2 are the prototype and come before any UI integration.
Step
Deliverable
Exit check
0. Confirm the integration contract
Engine and renderer, permitted inspection inputs, stable identity, owning components, target sizes
Real source locations and field mapping are written down; a minimal shader compiles on the project's renderer
1. Build the visual definition
Builder, presentation presets, shared terrain and masks
Same inputs rebuild the same parameters without touching simulation randomness
2. Prove both renderers together
One 2D globe and one surface-only 3D globe
With phase frozen, geography, poles, spin direction, and lighting match, first on an asymmetric test pattern
3. Integrate the UI
Sidebar component and colony component
Selection, resizing, visibility, and ownership work
4. Add atmosphere and clouds
Layers in both views
Airless worlds disable layers; transparent edges composite correctly
5. Add city lights
Shared site field and growth mapping
Growth keeps sites; a turn updates the open page; capacity changes follow the rule
6. Tune and verify
Presets, readability at real sizes, performance
Largest supported system view and chosen colony resolution pass
Open questions for review
Codex reviewed this spec without the current checkout. These are for step 0, against the real code:
[ ] Confirm engine and renderer. The older project guide lists Godot 4.7 .NET, GL Compatibility, C# presentation code, and a separate plain .NET simulation.
[ ] Confirm shader includes, in-shader 3D noise, and a transparent SubViewport on that renderer.
[ ] What are the actual planet and colony types and fields? Finalize the mapping to PlanetVisual.
[ ] How is stable planet identity represented, and does it survive save and load?
[ ] What inspection or knowledge model exists per empire, and what states must the globe handle?
[ ] What update notifications exist for colony changes while a page is open?
[ ] Where do the sidebar and colony components attach, and at what sizes?
[ ] What are population units and normal ranges, to set the stable visual reference?
[ ] Can fully ocean-covered worlds be colonized?
[ ] Do saves embed planet-type definitions? If so, where must visual presets live to stay out of them?
For David to confirm. Each has a proposed answer from the review:
[ ] Style: restrained retro with limited resolution, chosen from examples at real UI sizes.
[ ] Milestone: its own small visual milestone after the M14 selection-console work.
[ ] City light color: one pale warm white for now; race-based color deferred.
[ ] Globes keep rotating while the game is paused.
[ ] Sidebar clouds: simplified at 64 px and up, omitted below.
