# OpenAntares Project Charter

## Project Identity

**OpenAntares** is an open-source, turn-based space 4X strategy game inspired by the depth, pacing, and player agency of classic 1990s strategy games while using an entirely original setting, codebase, interface, artwork, audio, and game systems.

The goal is not to recreate a specific legacy title feature-for-feature. The goal is to build a modern, extensible 4X game that captures the appeal of exploration, empire management, technological development, fleet design, diplomacy, and tactical decision-making without inheriting unnecessary technical or design limitations from older games.

## Core Player Fantasy

The player leads a spacefaring civilization from its early interstellar expansion into a major galactic power.

The player should feel that they are:

- discovering an unknown galaxy
- shaping the character and capabilities of a civilization
- making meaningful economic and technological tradeoffs
- designing fleets around strategic goals
- responding to rival empires rather than merely racing against them
- making decisions whose consequences emerge over many turns
- developing a distinct empire rather than following one optimal progression path

## Core Gameplay Loop

The primary strategic loop is:

**Explore → Expand → Develop → Research → Negotiate → Build → Compete → Adapt**

On a typical turn, the player may:

1. inspect colonies, fleets, intelligence, diplomacy, and research
2. allocate population and economic resources
3. choose construction and research priorities
4. move fleets and exploration vessels
5. interact with other civilizations
6. respond to events, discoveries, and threats
7. end the turn
8. observe the consequences of their decisions and those of other empires

Every major system should feed back into this loop.

## Design Pillars

### Meaningful Decisions

The game should emphasize tradeoffs rather than routine optimization.

A strong decision should have more than one defensible answer depending on the player's situation.

Examples include:

- research versus military production
- rapid expansion versus infrastructure
- specialized versus flexible ship designs
- diplomacy versus coercion
- immediate economic gain versus long-term development
- concentrated versus distributed defenses

### Depth Without Busywork

Complexity should come from interacting systems, not excessive clicking or repetitive maintenance.

The player should make important decisions while automation and quality-of-life tools handle routine tasks when desired.

Micromanagement may be available for players who enjoy it, but should not be mandatory for competent play.

### Distinct Empires

Different civilizations should meaningfully alter how the game is played.

Differences should extend beyond simple percentage bonuses.

Empire identity may emerge from combinations of:

- biology
- culture
- government
- environmental preferences
- technological tendencies
- economic systems
- military doctrine
- diplomatic behavior
- unique abilities or restrictions

Custom civilization creation should eventually be a major feature.

### Strategic Transparency

Players should be able to understand why something happened.

Economic outputs, combat effects, diplomatic modifiers, research bonuses, and other major systems should expose understandable explanations instead of hidden arithmetic.

Complex systems are acceptable. Unexplained systems are not.

### Emergent Stories

The simulation should create memorable situations without requiring a heavily scripted narrative.

Examples include:

- a previously friendly civilization becoming a strategic rival
- a marginal colony becoming economically critical
- a technological breakthrough changing military doctrine
- a small defensive fleet surviving against overwhelming odds
- alliances forming against an expanding power
- internal or external crises changing long-term plans

### Strong Information Design

A 4X game is fundamentally an information-management game.

The interface should make large amounts of information understandable through:

- consistent visual language
- useful filtering
- clear summaries
- drill-down detail
- tooltips
- comparison views
- alerts that distinguish important events from noise

The interface should remain usable at modern desktop resolutions without attempting to imitate a 1990s interface literally.

### Moddability by Design

Game content should be data-driven wherever practical.

Technologies, buildings, weapons, species traits, governments, events, and other content should not require engine changes for ordinary modifications.

The long-term goal is for users to create:

- custom civilizations
- new technologies
- new weapons
- buildings
- events
- balance modifications
- alternate rulesets
- total conversions

## Technical Principles

### Simulation First

Core game rules should be logically separated from presentation.

The strategic simulation should not depend heavily on Godot scenes or UI nodes.

A conceptual structure might be:

**Simulation → Game State → Presentation Layer**

This separation should make testing, modding, AI development, save compatibility, and future interface changes easier.

### Data-Driven Content

Where practical, content should be defined through structured data rather than hard-coded logic.

Examples include:

- technologies
- buildings
- weapons
- ship components
- species traits
- governments
- events
- planet types

Game rules that genuinely require logic may remain code-driven.

### Deterministic Turn Processing

Where practical, turn resolution should be deterministic when given the same game state, inputs, and random seed.

This improves:

- debugging
- automated testing
- save reliability
- multiplayer possibilities
- AI evaluation
- reproducibility of bugs

### Multiplayer-Ready Architecture

Core simulation and turn resolution should not assume a single human player.

Player commands, AI commands, and remote-player commands should pass through the same validation and turn-processing systems.

Network transport should remain separate from game rules.

This keeps multiplayer possible later without restructuring the simulation, and it ensures AI empires play by the same rules as human players.

### Testable Systems

Core simulation systems should support automated tests independently of the UI.

Important early candidates include:

- resource calculations
- population growth
- colony production
- research progression
- movement
- combat calculations
- technology prerequisites

## Version 1.0 Scope

Version 1.0 should provide a complete playable strategic game rather than attempting to contain every feature imaginable.

The target feature set includes:

- procedural galaxy generation
- star systems and planets
- exploration
- colonization
- population growth
- food or equivalent population support
- industrial production
- scientific research
- technology tree
- buildings and planetary development
- multiple playable civilizations
- computer-controlled empires
- fleet creation and movement
- ship construction
- ship design
- strategic warfare
- diplomacy
- victory and defeat conditions
- save and load
- basic events
- game setup options

Tactical ship combat may be included in version 1.0 only if it does not prevent completion of the strategic game.

## First Playable Milestone

The first milestone should prove the game's fundamental loop.

The player should be able to:

1. begin a new game
2. generate a galaxy
3. view the galaxy map
4. inspect stars and planets
5. control one starting colony
6. allocate population or workforce
7. select a production project
8. select research
9. end the turn
10. see population, production, and research advance
11. save and reload the game

This milestone does not require:

- rival AI
- diplomacy
- combat
- advanced graphics
- final content
- polished animation

If this loop is enjoyable and technically sound, additional systems can be added incrementally.

## Explicit Non-Goals

OpenAntares should not initially attempt to:

- reproduce any existing commercial game exactly
- use copyrighted assets from legacy games
- mimic a legacy interface pixel-for-pixel
- support every conceivable 4X mechanic
- implement real-time strategy gameplay
- become an MMO or persistent online game
- require online connectivity for single-player
- depend on proprietary backend services
- prioritize graphical spectacle over strategic gameplay

## Art Direction

OpenAntares should have its own visual identity.

The intended direction is:

- readable
- atmospheric
- science-fictional
- slightly retro-futuristic
- visually clean
- suitable for large amounts of strategic information

The game should evoke classic science-fiction strategy without directly reproducing another game's visual design.

AI-assisted assets may be used as part of the project's creative workflow, with human selection, editing, composition, and direction used to establish a consistent visual language.

## Licensing

The intended licensing model is:

**Source code:** MIT License

**Original game assets:** CC0 1.0 Universal unless otherwise noted

**Documentation:** CC BY 4.0

Third-party components retain their respective licenses.

The project will not distribute copyrighted assets from other commercial games.

## Success Criteria

OpenAntares succeeds if:

- the first several turns immediately present interesting choices
- different empire builds encourage different strategies
- the player can understand the consequences of major decisions
- large empires remain manageable
- AI opponents behave coherently enough to create strategic pressure
- adding new game content does not require rewriting core systems
- the game remains enjoyable without nostalgia for any particular predecessor
- the project can grow without requiring a fundamental architectural rewrite
