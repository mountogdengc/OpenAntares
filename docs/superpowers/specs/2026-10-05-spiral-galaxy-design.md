# Spiral Galaxy Generation for OpenAntares

**Status:** Design for review
**Date:** 2026-10-05

## Purpose

New games should show a recognizable spiral galaxy while keeping every selectable star system useful to the 4X game. The map should communicate a dense center, sweeping arms, and a sparse outer rim even at the first playable milestone's 12–20 systems. Regions should shape the mix of planets now and provide a stable place for exploration and travel rules when those systems arrive.

This design takes the cosmographic layering idea from Slatemark's galaxy generator. It does not port Slatemark's Rust, floating-point calculations, campaign graph, thousands of generated locations, or narrative pipeline into OpenAntares.

## Scope and sequence

This change implements one spiral shape, with variation by seed. It adds three region IDs (`core`, `arm`, `rim`) to generated systems and uses those IDs for planet-type generation weights. It draws a faint background starfield to make the shape legible. The new-game screen continues to offer size and seed; shape selection is deferred.

Exploration and travel effects are part of the intended use of regions, but neither system exists in the current simulation. They will be specified and implemented with those systems. This change does not add movement rules, resource deposits, AI, or special economic modifiers. Existing planet types already differ in support, production, and research output; regional planet mixes create the first resource difference without inventing a second resource model.

## Alternatives considered

1. **Adapt Slatemark's star positions directly.** Its shape is proven visually, but its floating-point trigonometry and separate xorshift stream violate OpenAntares's cross-platform deterministic simulation rules. Its scale also does not match a 12–20-system play map.
2. **Shape the playable systems only.** This is small, but a dozen labeled systems cannot reliably convey spiral arms, especially with a minimum spacing rule.
3. **Generate playable systems and a visual starfield from one integer shape model (chosen).** The gameplay layout stays small and deterministic while the map visibly reads as a spiral. Background stars have no game-state identity, selection, or rules effect.

## Generation model

`GalaxyGenerator` remains the entry point. The existing PCG32 stream and seeded new-game behavior remain authoritative. The generator uses integer or fixed-point coordinates and a fixed integer lookup or control-point representation of a three-arm spiral. It must not use runtime trigonometry, floating-point calculations, `System.Random`, or unordered iteration to choose simulation outcomes.

For a size with `N` playable systems, each of the core and rim receives `max(1, floor((3N + 10) / 20))` systems; the arms receive the remainder. Arm candidates are sampled along the spiral with bounded scatter; core and rim candidates use their own bounded distributions. Candidate coordinates are scaled to the size's existing whole-number map dimensions. The existing minimum-distance check and bounded retry/error behavior still apply. A failed placement reports a clear error instead of silently reducing the requested system count. Different seeds vary orientation, position within the regions, and scatter while retaining one recognizable spiral family.

Each accepted system receives the region ID of the distribution that placed it. The quotas guarantee that every region has at least one playable system at the current supported sizes. Systems keep their existing stable integer IDs, names, coordinates, planets, and starting-colony behavior. The starting planet is still set to the content-defined starting type regardless of its region.

The background starfield is derived from the saved galaxy seed, shape ID, and dimensions with a separate deterministic visual stream. It uses the same spiral geometry but never advances the game-state PRNG. Its points are presentation data: they are dim, unlabeled, unselectable, absent from `GameState.Stars`, and omitted from saves. Drawing them may use floating-point screen coordinates because presentation has no simulation effect.

## Regions and content

The generated `StarState` stores `RegionId` as a stable string: `core`, `arm`, or `rim`. Region display names and short descriptions live in content JSON so the map and star-inspection UI can explain them. The loader supplies the three standard definitions when older content omits them. Mod-added planet types can supply optional generation weights by region; if a region weight is absent, the existing `generation_weight` is used. A region weight of zero excludes that type in that region. Content loading validates recognized region IDs, nonnegative weights, and a positive total weight for each region.

Core content should make the three mixes perceptibly different without making any region categorically better. The final numeric weights belong in content JSON and are checked with generation tests. Planet counts per system remain governed by the existing galaxy rules. There is no separate resource-deposit mechanic in this change.

Future exploration and travel rules should read the stored region ID through the same simulation state and expose the reason for any regional modifier in the UI. They should use explicit content/rule values, not infer a region from a rendered star color or screen position.

## State, saves, and compatibility

New game state records the original galaxy seed, shape ID (`spiral`), map width, and map height. This allows the same background starfield and fixed map framing after save/load. New stars record their region IDs. Save writing, loading, cloning, and validation include these fields. The effective content embedded in a save includes region labels and regional planet weights.

New saves use schema version 2. The reader accepts versions 1 and 2 explicitly. For version-1 saves, it leaves galaxy metadata and star regions absent (`null`), keeps the original star positions and planets, and uses the previous fit-to-stars map framing. Absent regions get neutral/default behavior when future exploration and travel rules are introduced. Loading never regenerates a saved galaxy or infers a region from its old coordinates. Saving a loaded version-1 game writes version 2 with the legacy layout and absent regions preserved. The rules implementation version remains unchanged because this change affects new-game generation, not existing turn formulas.

## Map and interface

The galaxy map uses the saved map dimensions as its world bounds for new spiral games. Resizing the window changes only screen scaling. The background starfield sits behind playable systems with sufficient contrast that selection rings, names, and the home system remain clear. Region identity appears in star inspection and can be indicated subtly on the map through theme-controlled styling; all colors, sizes, and fonts remain in the Godot theme. Legacy saves retain the current map presentation.

The new-game screen still asks only for size and seed. Internal shape identification makes future shape presets possible without introducing a selector for a choice that does not yet exist.

## Testing and acceptance

Simulation tests cover every supported galaxy size, several seeds, minimum spacing, exact playable-system count, valid coordinates, guaranteed region presence, and identical complete state for identical content/settings/seed. Tests verify that changing the seed changes the layout, that regional weight zero excludes a planet type in that region, that fallback weights work for existing/mod content, and that the starting planet remains valid. Content tests reject unknown region IDs and regions with no eligible planet type. Save tests cover a new spiral game round trip and a version-1 legacy fixture.

Visual review in Godot checks that small, medium, and large maps read as spirals, labels and selections remain legible, map framing is stable after save/load and resize, and background stars cannot be selected. The simulation and test suite must pass with `dotnet test`.

## Design constraints

- Simulation geometry uses whole-number or fixed-point math and OpenAntares's PCG32.
- Generation order and content iteration are stable across platforms.
- The content schema remains JSON and validates errors at load time.
- Background stars never alter game state or turn results.
- The first playable milestone stays focused on generation, inspection, colony play, and saves.
- Future shapes may replace the spiral candidate distribution, but they use the same system, region, state, and presentation boundaries.
