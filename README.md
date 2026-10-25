# OpenAntares

OpenAntares is an original, open-source, turn-based space 4X game built with Godot .NET and C#. See the [project charter](docs/CHARTER.md) for its scope and design pillars.

The first playable colony loop is implemented. **Milestone acceptance remains open**: the retained baseline originally passed 195 simulation tests and 463 graphical runtime assertions, while uncoached player comprehension and complete physical-input/readability acceptance require evidence. Start with [PROJECT_STATUS.md](PROJECT_STATUS.md) for the current handoff, [the validation continuation](reports/FIRST_PLAYABLE_VALIDATION_2026-10-22.md), and [the adoption audit](reports/ADOPTION_AUDIT.md).

The owner has deferred the unavailable uncoached session under ADR-005 and approved the [exploration and colonization specification](docs/superpowers/specs/2026-10-22-exploration-and-colonization-design.md) in ADR-006. The approved [implementation plan](docs/superpowers/plans/2026-10-22-exploration-and-colonization.md) has been executed using Native implementation: scouting, travel, vessel production, founding and multi-colony management are integrated into main. That expansion baseline used rules2. The owner-approved species implementation now uses rules3 for new games, with four selectable species and visible Harl default; old rules1/2 campaigns retain their embedded content. Writer schema4 explicitly reads the seven supported schema/rules pairs. See [species validation](reports/SPECIES_VALIDATION_2026-10-23.md) and [review/handoff](reports/SPECIES_FINAL_REVIEW_2026-10-23.md) for implementation/review results and limits. The owner-authorized species slice is now integrated into local main; [fresh integration verification](reports/SPECIES_INTEGRATION_2026-10-24.md) records445 simulation tests,1181 species and275 expansion assertions,466 legacy assertions plus27 geometry observations, and exact owner-edit preservation. See the [expansion report](reports/EXPANSION_VALIDATION_2026-10-22.md) for 281 simulation tests, 275 expansion assertions and 466 legacy assertions passing, review state and limits. The [integration report](reports/EXPANSION_INTEGRATION_2026-10-23.md) records fresh checks on main, repaired index and preserved owner changes.

The reviewed `codex/rival-expansion` branch, now integrated into local main under owner option1 authorization, implements the approved bounded rival slice: rules4 new games, one fair expansion rival by default (visible Solo retained), observed contacts, atomic deterministic planning and writer5 with eleven explicit compatibility pairs. [Rival integration verification](reports/RIVAL_INTEGRATION_2026-10-24.md) records fresh combined gates and exact owner-change preservation. See [rival validation](reports/RIVAL_VALIDATION_2026-10-24.md) and [the current status](PROJECT_STATUS.md) for actual gates, review state and limitations. Existing rules1/2/3 campaigns continue without activating old passive AI tags.

## Build and run

Use Godot **4.7.2 .NET**, the version referenced by `OpenAntares.csproj`. The projects target .NET 8; the adoption baseline used .NET SDK 10.0.113 and runtime 8.0.32 on Windows x64. A clean-machine install and other platforms have not been verified.

```powershell
dotnet restore OpenAntares.sln
dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers
dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --no-build --nologo
```

Open `project.godot` in the Godot .NET editor and run `game/Main.tscn`. With the Godot console executable on PATH, a startup check is:

```powershell
godot --headless --path . --quit-after 60 -- --new-game=small:42
```

`--new-game=SIZE:SEED` is a development launch option after `--`; supported sizes are `small`, `medium`, and `large`. Saves use Godot's `user://saves` directory. Preserve existing saves and use distinct `qa-...` names for tests.

For retained Godot control/layout/save regressions, see [the opt-in graphical validation runner](tests/OpenAntares.Game.Validation/README.md). For the remaining human understanding check, use [the observer sheet](docs/playtests/first-playable-observer-sheet.md).

## Repository

| Path | Responsibility |
| --- | --- |
| `src/OpenAntares.Simulation/` | Engine-independent rules, state, commands, generation, content validation, saves |
| `game/` | Godot presentation; C# controls and one shared theme |
| `content/core/` | JSON rules and prototype content |
| `tests/OpenAntares.Simulation.Tests/` | xUnit simulation and persistence tests |
| `docs/` | Authoritative designs, requirement index, decisions, plans |
| `reports/` | Adoption findings and dated validation evidence |

## Documentation

- [Agent instructions](AGENTS.md) and [existing engineering rules](CLAUDE.md)
- [Project concept](docs/PROJECT_CONCEPT.md), [architecture](docs/ARCHITECTURE.md), and [decision records](docs/DECISIONS/README.md)
- [Specifications](docs/SPECIFICATIONS.md) and [acceptance criteria](docs/ACCEPTANCE_CRITERIA.md)
- [Implementation plan](docs/IMPLEMENTATION_PLAN.md) and [validation report](reports/VALIDATION_REPORT.md)
- [Project workflow](docs/PROJECT_WORKFLOW.md), [adoption prompt](docs/MASTER_PROJECT_ADOPTION_PROMPT.md), and [implementation prompt](docs/MASTER_IMPLEMENTATION_PROMPT.md)

Code and content JSON are MIT; original assets are CC0; documentation is CC BY 4.0. See [LICENSING.md](LICENSING.md).

## Colony development implementation — 2026-10-24

All twelve approved Native tasks are implemented on codex/colony-development-design. Rules5/writer6 new games add paid foundations/specialists, opt-in editable governors/queues/inheritance, scalable management/reports and original varied planet portraits. Genuine old campaigns remain unchanged. Fresh982 simulation tests and all five graphical gates pass (Colony582+4 provenance, Rival1146+12 provenance, Species1194, Expansion275, Legacy466+27 geometry); ordinary build0 warnings/errors and all five harnesses excluded. Final independent review/local main integration are pending; read PROJECT_STATUS.md and reports/COLONY_VALIDATION_2026-10-24.md for exact evidence/limits rather than historical counts above. Human/balance/platform/release acceptance remains open. No future crisis/fleet scope or push/release/cleanup.
