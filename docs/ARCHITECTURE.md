# OpenAntares architecture

Reconciled 2026-10-22. This records the architecture already established by [CHARTER.md](CHARTER.md), [CLAUDE.md](../CLAUDE.md), [FIRST_PLAYABLE_ECONOMY.md](FIRST_PLAYABLE_ECONOMY.md), and the [spiral specification](superpowers/specs/2026-10-05-spiral-galaxy-design.md). It does not replace their detailed contracts.

## Stack and components

| Component | Technology and responsibility | Permitted dependency |
| --- | --- | --- |
| Presentation, root project | Godot.NET.Sdk 4.7.2, C#, net8.0, GL Compatibility; `game/Main.tscn` enters `game/Main.cs` | References simulation via `ProjectReference` |
| Simulation | Plain .NET net8.0 library; content, state, commands, economy, generation, persistence | No Godot reference |
| Tests | xUnit 2.9.3, test SDK 17.12.0, runner 3.0.2 | Simulation; no Godot needed |
| Graphical validation | Opt-in Godot runtime harness under `tests/OpenAntares.Game.Validation/`; control/layout/save regressions | Actual presentation and simulation; excluded from normal compilation |
| Content | JSON in `content/core/` | Loaded into validated immutable content definitions |
| UI theme | `game/theme/openantares_theme.tres` | Root inheritance and named type variations |

Godot project settings also select Jolt physics. No dependency upgrade was undertaken. Root compilation excludes `src/**` and `tests/**`; their `.gdignore` files keep Godot from importing build output. The simulation treats warnings as errors, uses checked arithmetic, and applies banned-API analyzers. `ArchitectureTests.SimulationLibraryDoesNotReferenceGodot` checks the compiled assembly boundary.

## Runtime contracts

1. `ContentFiles` reads `res://content/core` through Godot file APIs; `ContentLoader` validates sections, references, costs, bounds, cycles, and regional weights in ordinal file order.
2. `GalaxyGenerator.CreateNewGame` accepts size and seed, producing validated plain state with one human empire and one starting colony.
3. `GameSession` owns published content/state. Presentation submits `Command` records carrying issuer and expected turn.
4. `CommandProcessor` validates ownership, prerequisites, turn, and workforce before cloning/publishing changes. Rejection retains the original state.
5. `EconomyCalculator` supplies forecasts and breakdowns. `TurnResolver` uses the same results, applies changes to a candidate, validates, then publishes once.
6. Godot screens display state, issue commands, and format simulation results. They do not recompute economic contributions or mutate authoritative state.

The actual code dependency points from presentation to simulation. The charter's conceptual `Simulation -> Game State -> Presentation` describes responsibility/data flow, not permission for simulation to reference the UI.

State classes are mutable plain data, and command application clones them. The no-direct-UI-mutation rule remains a convention supported by source review, not an immutable type boundary. `GameSession` construction assumes valid content/state; generation and save loading provide validation at their boundaries.

## Data, determinism, and persistence

- Entity references use typed stable integer IDs allocated from state. Content references use existing string IDs, not a newly adopted namespacing scheme.
- State includes empires, stars, planets, colonies, next ID, turn, PCG32 state, rules version, and optional galaxy seed/shape/dimensions/regions.
- Rules use integers and hundredths, checked arithmetic, Int128 multiplication intermediates, defined order, and invariant parsing. Presentation may use floats and localized formatting.
- Turn phases are snapshot/calculation, growth, reserves/completions, readiness reset/validation/turn publication. New effects apply to the following snapshot. The triggering ready command rolls back if resolution fails.
- Saves are JSON with format `openantares-save`, schema 2, a separate rules version, effective content, and full planning state. Writes flush a temporary file before replacement; failed loads retain the session.
- Schema 1 is read explicitly. It retains old positions and absent galaxy metadata/regions; loading does not regenerate the map. Resaving writes schema 2 while retaining that legacy layout. Unsupported schema/rules versions are rejected, not implicitly migrated.
- UI saves live in `user://saves`; filenames are sanitized by `SaveFiles`. Ordinary tests use temporary paths.

## Boundaries and quality requirements

The current map intentionally permits inspection of all generated systems. An empire knowledge model is pending design, not a missing filter to add during adoption. Networking, AI policy, travel, combat, species modifiers, mod discovery/overrides, and external authoring tools are not implemented contracts.

Content and saves are validated input; no script-loading package mechanism was found. The adoption does not assert a sandbox for arbitrary third-party code or comprehensive hostile-input/resource-limit testing. The game has no required online backend. Accessibility, localization, minimum hardware, performance budgets, distribution/update policy, and full third-party dependency attribution need specifications before relevant delivery claims.

Windows x64 builds and tests pass at adoption. Linux/macOS/ARM determinism and delivery remain unverified. No CI workflow, export presets, SDK pin (`global.json`), or release tags were found. The root Godot project's solution Release configuration currently maps to Debug; this is a release-preparation gap, not a change made by adoption.

## Decisions

See the [decision index](DECISIONS/README.md). These records recover established choices and known consequences; they do not manufacture approvals or alternatives absent from the source documents.
