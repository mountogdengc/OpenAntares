# Spiral Galaxy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate a visibly spiral galaxy whose core, arm, and rim systems have different planet mixes, while preserving deterministic play and older saves.

**Architecture:** A pure integer `SpiralShape` samples positions for playable systems and background stars. `GalaxyGenerator` owns playable placement and regional planet selection; Godot draws background points from a separate seeded stream. New galaxy metadata and region IDs live in state and version-2 saves, with explicit version-1 loading.

**Tech Stack:** C# / .NET, Godot 4.7 .NET, JSON content, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-05-spiral-galaxy-design.md`

## Global Constraints

- Simulation geometry uses whole-number or fixed-point math and OpenAntares's PCG32.
- Generation order and content iteration are stable across platforms.
- The content schema remains JSON and validates errors at load time.
- Background stars never alter game state or turn results.
- Only the `spiral` shape is offered in this release; no shape picker is added.
- Preserve the current unrelated `.gitignore` modification; stage only task files.

## Review Focus

1. A modded galaxy size with fewer than three stars should fail content validation with a clear message, because every new galaxy needs all three regions. Task 1 tests this.
2. A cramped map should return a placement error instead of hanging or silently dropping systems. Task 3 tests this.
3. Regional overrides that leave a region with no eligible planet type should fail when content loads, rather than fail at generation time. Task 1 tests this.
4. A version-1 save should load, retain its original coordinates and planets, and save again as version 2 without inventing regions. Task 2 tests this.
5. Generating presentation stars must leave `GameState.Random` and the serialized state unchanged. Task 5 tests this.

---

### Task 1: Region content contract

**Files:**
- Create: `src/OpenAntares.Simulation/Galaxy/GalaxyRegions.cs` (the three stable IDs and their default names/descriptions)
- Modify: `src/OpenAntares.Simulation/Content/ContentSet.cs`, `ContentLoader.cs`, `ContentWriter.cs`
- Modify: `content/core/galaxy.json`, `content/core/planet_types.json`
- Test: `tests/OpenAntares.Simulation.Tests/ContentLoaderTests.cs`

**Interfaces:**
- Produces: `GalaxyRegions.Ids`, `GalaxyRegions.IsKnown(string)`, `ContentSet.RegionDefinitions`, and `PlanetTypeDefinition.RegionGenerationWeights`. Add the last two as init properties with defaults so existing positional-record constructors and hand-built test fixtures still compile.
- Produces: `PlanetTypeDefinition.WeightForRegion(string regionId)`, returning the override or `GenerationWeight`.
- Data shape: `galaxy.regions` is an optional object keyed by `core`, `arm`, and `rim`, each with `name` and `description`; planet types may have `region_generation_weights` with zero or more of those keys.

- [ ] **Step 1: Add failing content tests.** Extend the existing `InvalidContent` cases and add a successful round-trip test. Pin these behaviors:

```csharp
Assert.Equal(2, content.PlanetTypes["forge_world"].WeightForRegion("arm")); // fallback
Assert.Equal(0, content.PlanetTypes["forge_world"].WeightForRegion("rim")); // explicit zero
Assert.Contains(result.Errors, e => e.Field == "region_generation_weights.rim"); // all-zero rim
Assert.Contains(result.Errors, e => e.Field == "star_count" && e.Message.Contains("three", StringComparison.Ordinal));
Assert.Contains(result.Errors, e => e.Field == "region_generation_weights.unknown");
```

- [ ] **Step 2: Run `dotnet test --filter FullyQualifiedName~ContentLoaderTests`; verify the new assertions fail.**
- [ ] **Step 3: Add the content model and parser.** Use an immutable, ordinally sorted map for weights and region definitions. For older JSON lacking `galaxy.regions`, supply the three standard definitions. Use the existing `EntryReader` error path and `RejectUnknownFields` pattern. In `CheckGeneration`, sum effective weights separately for each region with `long`; reject zero or sums exceeding `int.MaxValue`. Reject galaxy sizes below three systems.

```csharp
public static class GalaxyRegions
{
    public const string Core = "core";
    public const string Arm = "arm";
    public const string Rim = "rim";
    public static readonly string[] Ids = [Core, Arm, Rim];
    public static bool IsKnown(string id) => id is Core or Arm or Rim;
}

public sealed record GalaxyRegionDefinition(string Id, string Name, string Description);

public int WeightForRegion(string regionId) =>
    RegionGenerationWeights.TryGetValue(regionId, out int weight) ? weight : GenerationWeight;
```

- [ ] **Step 4: Write all three region definitions and these regional weights to JSON.** In `content/core/planet_types.json`, use core/arm/rim weights `1/3/1` for `verdant_world`, `3/2/4` for `forge_world`, `3/2/2` for `crystal_world`, and `1/2/3` for `barren_rock`. This gives the arm more support worlds, the core more research worlds, and the rim more production worlds alongside more barren worlds. Keep the existing `generation_weight` as the fallback for mods and older embedded content. Make the serializer emit keys in ordinal order.
- [ ] **Step 5: Run `dotnet test --filter FullyQualifiedName~ContentLoaderTests`; verify pass. Commit only the files in this task** with `feat: define galaxy regions and planet weights`.

### Task 2: State and save compatibility

**Files:**
- Modify: `src/OpenAntares.Simulation/State/GameState.cs`, `GameStateValidator.cs`
- Modify: `src/OpenAntares.Simulation/Saves/SaveGame.cs`
- Test: `tests/OpenAntares.Simulation.Tests/SaveGameTests.cs`
- Test fixture: `tests/OpenAntares.Simulation.Tests/Fixtures/legacy-v1-save.json`

**Interfaces:**
- Produces: nullable `GameState.GalaxySeed`, `GalaxyShapeId`, `GalaxyWidth`, `GalaxyHeight`; nullable `StarState.RegionId`.
- `null` galaxy metadata means a legacy layout; `null` star region means unclassified/neutral. New generated games set all fields.
- `SaveGame.SchemaVersion` becomes 2; the reader accepts schemas 1 and 2 only.

- [ ] **Step 1: Add failing tests for metadata, invalid state, and the legacy fixture.** Before changing the writer, serialize `Fixtures.StartingState()` with the current schema-1 writer; remove the newly added `galaxy.regions` from its embedded content and keep the resulting JSON at `tests/OpenAntares.Simulation.Tests/Fixtures/legacy-v1-save.json`. Assert a version-2 round trip preserves seed, shape, bounds, and region. Assert a version-1 load keeps `null` fields, old coordinates, and existing planet types, then serializes as version 2. Assert mixed/partial bounds and an unknown region are rejected.

```csharp
Assert.Equal((ulong)42, reloaded.State.GalaxySeed);
Assert.Equal("spiral", reloaded.State.GalaxyShapeId);
Assert.All(reloaded.State.Stars, star => Assert.True(GalaxyRegions.IsKnown(star.RegionId!)));
Assert.Null(legacy.State.GalaxySeed);
Assert.All(legacy.State.Stars, star => Assert.Null(star.RegionId));
```

- [ ] **Step 2: Run `dotnet test --filter FullyQualifiedName~SaveGameTests`; verify new tests fail.**
- [ ] **Step 3: Add the state properties and clone behavior.** Validate that new metadata is all present or all absent, shape is `spiral`, dimensions are positive, generated star coordinates are within saved dimensions, and generated stars have known regions. Legacy state permits null region IDs and retains the previous checks.
- [ ] **Step 4: Update the writer and reader.** The writer always emits schema 2 and writes nullable galaxy metadata plus each star's nullable region. The reader branches explicitly on schema 1 versus 2; version 1 reads the old field set, version 2 reads the new fields, and any other version returns a clear incompatibility error. Keep `Rules.CurrentVersion` at 1. Update the existing corrupted-schema test to try version 3, not 2. Reject unknown fields in both branches.

```csharp
if (schema is not (1 or SchemaVersion))
    return Fail($"The save uses unsupported schema version {schema}.");
GameState? state = root.RequiredObject("state") is { } body
    ? ReadState(body, (int)rulesVersion, (int)schema)
    : null;
```

- [ ] **Step 5: Run `dotnet test --filter FullyQualifiedName~SaveGameTests`; verify pass. Commit only the task files** with `feat: save galaxy shape and regions`.

### Task 3: Integer spiral shape and playable placement

**Files:**
- Create: `src/OpenAntares.Simulation/Galaxy/SpiralShape.cs`
- Modify: `src/OpenAntares.Simulation/Galaxy/GalaxyGenerator.cs`
- Test: `tests/OpenAntares.Simulation.Tests/GalaxyGeneratorTests.cs`

**Interfaces:**
- Produces: `SpiralShape.Sample(Pcg32 random, string regionId) -> (int X, int Y)` in a normalized `[0, 1024]` square; it is pure integer math apart from consuming the supplied PRNG.
- Produces: `SpiralShape.ToMap(int x, int y, int width, int height) -> (int X, int Y)` using integer scaling and clamping.
- `GalaxyGenerator.CreateNewGame` sets seed, shape ID, and bounds, then places stars by region quota before generating planets.

- [ ] **Step 1: Add failing generation tests.** For every core size and seeds `0..199`, assert exactly the requested system count, valid bounds, minimum squared distance, known region IDs, and quota `q = max(1, (3*N + 10)/20)` for both core and rim. Repeat one seed and compare `Fixtures.Snapshot` exactly. Keep the existing impossible-spacing test and add a modded three-star cramped map case.

```csharp
int q = Math.Max(1, (3 * size.StarCount + 10) / 20);
Assert.Equal(q, state.Stars.Count(s => s.RegionId == GalaxyRegions.Core));
Assert.Equal(q, state.Stars.Count(s => s.RegionId == GalaxyRegions.Rim));
Assert.Equal(size.StarCount - 2 * q, state.Stars.Count(s => s.RegionId == GalaxyRegions.Arm));
```

- [ ] **Step 2: Run `dotnet test --filter FullyQualifiedName~GalaxyGeneratorTests`; verify the new tests fail.**
- [ ] **Step 3: Implement `SpiralShape` with an integer quarter-wave lookup and fixed-point coordinates.** Use 64 angle steps per turn and mirror this 17-value quarter-wave table: `[0,100,200,297,392,483,569,650,724,792,851,903,946,981,1004,1019,1024]`. Core candidates have an integer radius within 0–190 and a full-turn angle. Arm candidates choose one of three phases (`0`, `21`, `43` steps), radius 120–465, and angle `phase + radius*96/465`, then add bounded integer scatter. Rim candidates have radius 380–495 and a full-turn angle. Multiply the Y offset by `860/1024` for an elliptical view. Clamp normalized points to `[8,1016]`; all multiplications that may overflow `int` use `long`. Do not call `Math.Sin`, `Math.Cos`, `Math.Sqrt`, or use floats/doubles.

```csharp
int angle = (phase + radius * 96 / 465 + jitter) & 63;
int x = 512 + (int)((long)radius * Cos1024(angle) / 1024);
int y = 512 + (int)((long)radius * Sin1024(angle) * 860 / (1024 * 1024));
```

- [ ] **Step 4: Replace uniform `PlaceStars` sampling with region quotas.** Iterate core, arm, rim in a fixed order. For each requested system, draw at most `PlacementAttemptsPerStar` candidates and retain the existing squared-distance check and useful failure message. Keep name shuffling, planet creation, starting colony creation, and validation in their current order. Use the existing game-state PCG32; do not add a second simulation RNG.
- [ ] **Step 5: Run `dotnet test --filter FullyQualifiedName~GalaxyGeneratorTests`; tune only integer scatter/radius bounds if supported sizes fail.** Run `dotnet test`, verify the simulation suite passes, then commit only the task files with `feat: place systems in a deterministic spiral`.

### Task 4: Regional planet generation

**Files:**
- Modify: `src/OpenAntares.Simulation/Galaxy/GalaxyGenerator.cs`
- Test: `tests/OpenAntares.Simulation.Tests/GalaxyGeneratorTests.cs`

**Interfaces:**
- Consumes: `StarState.RegionId` and `PlanetTypeDefinition.WeightForRegion` from Tasks 1–3.
- Produces: planets selected from the effective weight list for their star's region, in ordinal planet-type ID order.

- [ ] **Step 1: Add failing tests.** Give one planet type zero weight in `rim` but positive fallback elsewhere, generate across seeds `0..99`, and assert no rim planet uses it. Assert the starting colony's forced planet type remains the configured type. A separate test with all region overrides absent must still generate all sizes.

```csharp
Assert.DoesNotContain(state.Planets, p =>
    state.FindStar(p.StarId)!.RegionId == GalaxyRegions.Rim && p.PlanetTypeId == "barren_rock");
Assert.Equal(content.StartingColony.PlanetTypeId, state.FindPlanet(state.Colonies.Single().PlanetId)!.PlanetTypeId);
```

- [ ] **Step 2: Run `dotnet test --filter FullyQualifiedName~GalaxyGeneratorTests`; verify the new exclusion assertion fails.**
- [ ] **Step 3: Build three ordered effective-weight lists once per generation.** Select the list by `star.RegionId` in the existing planet loop; sum and roll with `Pcg32.NextInt`. Preserve the existing random planet-count call and home-colony override. Do not iterate an unordered dictionary.

```csharp
int weight = type.WeightForRegion(star.RegionId!);
// Skip zero weights; use the existing PickWeighted loop on the selected region list.
```

- [ ] **Step 4: Run `dotnet test --filter FullyQualifiedName~GalaxyGeneratorTests` and `dotnet test`; verify pass. Commit only the two task files** with `feat: vary planet types by galaxy region`.

### Task 5: Map rendering and region inspection

**Files:**
- Create: `src/OpenAntares.Simulation/Galaxy/GalaxyBackgroundPoints.cs` (seeded presentation coordinates; no state mutation or Godot dependency)
- Modify: `game/Components/GalaxyMap.cs`, `game/Components/StarPanel.cs`, `game/Screens/GameScreen.cs`
- Modify: `game/theme/openantares_theme.tres`
- Test: `tests/OpenAntares.Simulation.Tests/GalaxyGeneratorTests.cs` (background stream invariant)

**Interfaces:**
- Consumes: `GameState.GalaxySeed`, `GalaxyShapeId`, width/height, `StarState.RegionId`, and region display definitions.
- `GalaxyMap.ShowGalaxy` receives the complete state or explicit galaxy metadata in addition to playable stars and home IDs; `GameScreen` passes those from its session.
- Background points are generated through `SpiralShape.Sample` with `Pcg32.FromSeed(seed, a dedicated background stream constant)`, never with `state.Random`.

- [ ] **Step 1: Add a failing deterministic stream test around `GalaxyBackgroundPoints`.** Generate points twice from the same seed and assert equal sequences, and assert a snapshot of `GameState` before/after the call is identical. The helper returns only coordinate pairs and never stores them in state.

```csharp
string before = Fixtures.Snapshot(state);
var first = GalaxyBackgroundPoints.Generate(state.GalaxySeed!.Value, state.GalaxyWidth!.Value, state.GalaxyHeight!.Value);
Assert.Equal(first, GalaxyBackgroundPoints.Generate(state.GalaxySeed!.Value, state.GalaxyWidth!.Value, state.GalaxyHeight!.Value));
Assert.Equal(before, Fixtures.Snapshot(state));
```

- [ ] **Step 2: Run `dotnet test --filter FullyQualifiedName~GalaxyGeneratorTests`; verify the new test fails.**
- [ ] **Step 3: Implement the helper and renderer.** Generate about 1,200 small, dim background points with a fixed mix of core, arm, and rim samples. Cache them per seed and dimensions in `GalaxyMap`; render before selectable stars. Give new games fixed world bounds `[0,width] × [0,height]`; retain the existing fit-to-stars transform for legacy state. Use the same transform for background and playable points. Keep hit testing restricted to `GameState.Stars`.
- [ ] **Step 4: Add a region label/description in `StarPanel` for classified systems.** Use `ContentSet.RegionDefinitions`; omit it for a legacy unclassified system. Add only theme entries for visual styling, with no C# color/font/style overrides. Avoid per-star `Min`/`Max` scans in the drawing loop by computing map bounds once when `ShowGalaxy` is called.
- [ ] **Step 5: Run `dotnet test` and `dotnet build OpenAntares.sln`; verify pass.** Open the Godot project and visually inspect small, medium, large, one loaded new save, one legacy save, selection, labels, and resizing. Commit only task files with `feat: render spiral galaxy and show regions`.

## Final verification

- [ ] Run `dotnet test` and `dotnet build OpenAntares.sln` from the repo root; record exit codes.
- [ ] Confirm `git status --short` contains no new task changes and the pre-existing `.gitignore` edit remains untouched.
- [ ] Review the branch diff against the design spec, including save-version handling and all three regions.
