# OpenAntares

OpenAntares is an original, open-source, turn-based space 4X game built with Godot .NET and C#. See the [project charter](docs/CHARTER.md) for its scope and design pillars.

New games on local main use rules5/writer6: four selectable species, one fair expansion rival by default (Solo retained), exploration/founding and colony development. Paid foundations/specialists, editable opt-in governors, finite queues, scalable management/reports and original planet portraits are connected. Old rules1/2/3/4 campaigns retain their embedded content and behavior across sixteen explicit compatibility pairs.

All twelve approved Native colony tasks and the one blocking review correction are implemented and locally integrated. Three presentation Minors remain disclosed, so exact full M5 acceptance stays Implemented, Unverified. Read [PROJECT_STATUS.md](PROJECT_STATUS.md), [colony validation](reports/COLONY_VALIDATION_2026-10-24.md), [review/disposition](reports/COLONY_FINAL_REVIEW_2026-10-24.md), and [integration evidence](reports/COLONY_INTEGRATION_2026-10-24.md) for current source, fresh gates and preservation. Human understanding/readability/physical input, general balance, other platforms and release remain open; the unavailable uncoached session is deferred under ADR-005.

Historical evidence remains in the [adoption audit](reports/ADOPTION_AUDIT.md), [First Playable continuation](reports/FIRST_PLAYABLE_VALIDATION_2026-10-22.md), [expansion integration](reports/EXPANSION_INTEGRATION_2026-10-23.md), [species integration](reports/SPECIES_INTEGRATION_2026-10-24.md), [rival integration](reports/RIVAL_INTEGRATION_2026-10-24.md), and [rival correction integration](reports/RIVAL_POLISH_INTEGRATION_2026-10-24.md). Their older versions/counts describe those checkpoints.

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
