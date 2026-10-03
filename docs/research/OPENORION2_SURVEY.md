# OpenOrion2 Technical Survey

OpenOrion2 is a useful reference for inspecting a complex 4X game state, validating relationships, and presenting exploration information. It provides little evidence about a working strategic loop or game balance. For OpenAntares, the strongest lessons are to validate state relationships, keep empire knowledge separate from world state, and build a playable loop before expanding the interface.

Surveyed on October 2, 2026, against revision `dd428f1b7c66bf758d56f8276ec644538bc73641` (December 5, 2025, “Implement Info screen Tech Review page”). This was a source inspection of a shallow checkout, not a build or playtest. Runtime usability, correctness, save compatibility, and performance were not verified. Recommendations below are our interpretation, guided by the [OpenAntares charter](../CHARTER.md).

## Project scope and implemented behavior

The [README](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/README.md) describes a partial savegame viewer with no gameplay. It requires original MOO2 LBX data and existing saves. The source supports that description:

| Area | Evidence at the inspected revision | Value for OpenAntares |
| --- | --- | --- |
| Save loading | Binary records are loaded, validated, and assembled into fleet groups. | State validation and reconstruction examples. |
| Galaxy and planet inspection | Map widgets, system views, planet filters, and sorting are implemented. | Information navigation and comparison ideas. |
| Research | Browsing and help exist; selecting research displays an unimplemented message. | Browsing patterns, not research progression. |
| Leaders | Lists and assignment prompts exist; the assignment action is unimplemented. | Consequence previews, not a working leader system. |
| Strategic turn | The galaxy end-turn handler is a stub. | No demonstrated turn simulation. |
| Information pages | History, race information, and technology review have drawing code; turn summary and reference pages mainly draw their shells. | Inspect each page rather than treating class names as finished features. |

Sources: [galaxy.cpp](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/galaxy.cpp), [tech.cpp](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/tech.cpp), [officer.cpp](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/officer.cpp), [info.cpp](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/info.cpp).

## State representation and validation

[gamestate.h](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/gamestate.h) separates planets, colonies, colonists, stars, empires, leaders, ship designs, and ships. Many relationships use array indices. Fixed capacities and content enums reflect the original game; several fields remain explicitly unidentified.

**Adapt:** keep physical planets distinct from settlements and empire ownership. A planet can exist before colonization and after a colony disappears. Use stable entity IDs and validated references in our own schema. Do not adopt its fixed capacities or legacy numeric content identities as OpenAntares requirements.

[GameState::load and GameState::validate](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/gamestate.cpp) check counts, coordinates, owners, reciprocal wormhole links, star and planet relationships, colony and planet relationships, and ship references. Validation precedes fleet reconstruction. Some legacy inconsistencies generate warnings rather than exceptions.

**Adopt the principle:** successful parsing does not establish a valid world. For the first playable milestone, check entity uniqueness, references, ownership, workforce totals, and valid production and research selections after loading. Build derived indices only from validated state. Define our own invariants; reciprocal wormholes and symmetric diplomacy are examples from this project, not universal design requirements.

## Empire knowledge and information leaks

The state model distinguishes unexplored stars, name-only knowledge, charted systems, and visited systems. `isStarExplored` evaluates knowledge for a specified empire. However, `setActivePlayer` also rewrites a star ownership cache in the shared state. The planet list's enemy filter consults orbiting fleets and carries a comment acknowledging that hidden fleets should be ignored. These are source observations, not a reproduced runtime exploit. Sources: [gamestate.h](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/gamestate.h), [gamestate.cpp](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/gamestate.cpp), [galaxy.cpp](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/galaxy.cpp).

**Adapt:** separate authoritative facts, empire knowledge, and presentation caches. Changing the viewed empire should not modify simulation facts. A filter, sort order, count, tooltip, or disabled action can reveal concealed information even when the map hides it. When exploration is implemented, every inspection query should use the requesting empire's permitted information. Later, AI should receive the same permitted view.

## Comparison and navigation

The planet list filters environmental suitability, gravity penalties, minerals, and hostile presence; it sorts by climate, minerals, or maximum population. Hovering a listed planet highlights its star on the minimap. Range filtering and colony or outpost dispatch remain unimplemented. Source: [PlanetsListView](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/galaxy.cpp).

**Adapt:** connect comparison lists to spatial context. Let players assess planets through several understandable attributes rather than one opaque desirability score. Suitability should explain the current empire's constraints. A reusable selection model can keep list selection and map selection consistent. Empire-wide candidate filtering belongs after the one-colony milestone; basic map-to-inspector navigation belongs in it.

## Architecture and interface limitations

OpenOrion2 has separate source files for state, graphics, widgets, archive loading, and platform services, but these do not form a fully isolated simulation library. `gamestate.cpp` includes language, archive, and technology UI headers; some state methods obtain display strings from global language services. Fleets hold a parent state pointer, and state arrays are publicly accessible. Sources: [gamestate.cpp](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/gamestate.cpp), [gamestate.h](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/gamestate.h).

**Keep our existing direction:** plain serializable state, stable IDs, commands for changes, and a headless simulation. Return semantic results and calculation breakdowns; presentation supplies localized text. The survey does not establish a reusable command pipeline, deterministic turn processor, or AI implementation.

The [screen abstraction](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/src/screen.h) uses a 640 by 480 logical canvas, and screens contain explicit coordinates and archive sprite references. [Build configuration](https://github.com/nextghost/openorion2/blob/dd428f1b7c66bf758d56f8276ec644538bc73641/configure.ac) uses C++, Autotools, SDL2, and SDL2_mixer. These choices serve a reconstruction project. OpenAntares should retain Godot containers, one shared theme, and original assets.

## Priorities for the design guide

1. **Now:** document state invariants, loading validation, and the distinction between planets and colonies.
2. **During map and inspection work:** define empire knowledge and ensure queries cannot reveal hidden facts; connect map selection to the inspector.
3. **After the first playable loop:** add planet comparisons and filters with explicit reasons and permitted information.
4. **Keep out of scope:** LBX compatibility, original save offsets, legacy capacities, interface reconstruction, and combat details.

The amount of inspection UI alongside an unimplemented end-turn action illustrates why we should measure progress by an executable gameplay loop. It does not explain the project's development history or imply that its chosen scope is a failure.

This survey adds no upstream code or assets. Source headers identify GPL version 2 or later. Any future reuse would need a separate licensing assessment under OpenAntares' licensing policy.
