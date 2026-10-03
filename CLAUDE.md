# OpenAntares

Open-source, turn-based space 4X game built in Godot 4.7 (GL Compatibility renderer, Jolt physics). The setting, code, art, and systems are all original.

[docs/CHARTER.md](docs/CHARTER.md) is the source of truth for scope and design intent. Read it before starting a new system. If a request conflicts with the charter, point that out instead of silently following either one.

## Architecture rules

These come from the charter's Technical Principles. Treat them as hard constraints for every change.

### Keep the simulation separate from presentation

- Layering: **Simulation → Game State → Presentation**. Dependencies only point that way.
- Simulation code must not reference scenes, `Node`s, UI controls, input, or rendering. Write it as plain classes (`RefCounted`/`Resource`-style objects or plain data) that run headless.
- Presentation reads game state and sends commands. It never changes game state directly.
- Game state is plain, serializable data: no `Node` references, no callables, no engine handles. Refer to entities by stable IDs, not object references.

### Route every action through commands

- Every change to game state, whether from the local player, the AI, or a remote player, is a **command**: a serializable description of intent, tagged with the issuing empire.
- All commands go through the **same** validation and turn-processing path. AI empires get no shortcuts and no hidden access to state they shouldn't see.
- Never assume a single human player. There is no "the player" global inside the simulation, only empires, some of which are controlled by humans.
- Network transport (if it ever exists) only carries commands and state. It contains no game rules.

### Make turn processing deterministic

- The same state, commands, and seed must produce the same result.
- Inside the simulation, use only seeded RNG stored in or derived from game state. Never use global `randf()`/`randi()`/`randomize()`, wall-clock time, frame time, or anything else that varies by machine or run.
- Iterate in a defined order (sorted by ID, or insertion order you control). Never let the outcome depend on hash order, object instance IDs, or timing.
- Process turns in explicit, documented phases. Don't use signals or frame callbacks to drive the simulation.

### Put content in data files

- Technologies, buildings, weapons, ship components, species traits, governments, events, planet types, and similar content belong in data files, not hard-coded.
- Ordinary mods (new content, balance changes) must be possible without editing engine or simulation code.
- Keep logic in code only where a rule genuinely needs it. Prefer generic, data-parameterized mechanics over special cases keyed to one specific item.
- Reference content by string IDs from the data, not by enums baked into code.

### Explain the numbers

- Strategic transparency is a design pillar. When the simulation calculates an important value (output, growth, research, combat odds, diplomatic attitude), it should be able to return a breakdown of the contributing modifiers and their sources, not just the final number. Design calculations so the UI can show "why".

### Test the game logic

- Core simulation systems need automated tests that run without UI: resource calculations, population growth, production, research progression, movement, combat math, and tech prerequisites.
- Add or update tests with every simulation change. Determinism makes exact-result tests possible, so use them.
- No test framework has been chosen yet. Raise the choice the first time tests are needed instead of picking one silently.

## Scope discipline

- The current target is the **First Playable Milestone** in the charter: new game, galaxy generation, galaxy map, star and planet inspection, one colony, workforce allocation, production, research, end turn, save/load. It does not include AI, diplomacy, or combat.
- Don't build ahead of the milestone. Leave room for later systems (the command path, IDs, data-driven content), but don't implement them early.
- The strategic game comes before tactical combat and graphical polish.

## Content and licensing

- Code is MIT. Original assets are CC0. Docs are CC BY 4.0. See [LICENSING.md](LICENSING.md).
- Never add copyrighted assets, names, or text from commercial games. Don't copy a legacy game's interface or mechanics feature-for-feature.
- Any third-party file must keep its license, and that license must be recorded in LICENSING.md.
- `icon.svg` is the placeholder Godot logo (CC BY 4.0). Replace it, don't build on it.

## Open decisions

These haven't been settled yet. Ask instead of assuming:

- scripting language (GDScript vs. C#)
- data file format for content (JSON, Godot resources, etc.)
- test framework
- project folder layout
