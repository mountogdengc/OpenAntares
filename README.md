# OpenAntares

OpenAntares is an original, open-source, turn-based space 4X game built with Godot .NET and C#. Explore an unknown galaxy, develop a distinct civilization and make economic, technological, diplomatic and fleet decisions with understandable consequences. See the [charter](docs/CHARTER.md) and [current design guide](docs/DESIGN_GUIDE.md).

Implemented systems include exploration/founding, four playable species, rival empires, colony development/crises, scout survival, fleet command, strategic combat, ship design/refitting, diplomacy and empire conflict. M13/M14 campaign polish and presentation are integrated, followed by shared fleet controls, orbital inspection and procedural planets. Current simulation rules are12 and save writer13; historical campaigns retain their embedded behavior.

The inspected November16 checkout is feature/planet-globes-modern at e90b6b2; local main is d994d34. Modern planet appearance is accepted in its dated report, while Windows shader timing/affected v2 checks remain pending. Overall player comprehension/input, balance, other-platform and release acceptance remain separate. See [PROJECT_STATUS.md](PROJECT_STATUS.md) for evidence scopes and current next work.

Civilian [supply and migration behavior](docs/superpowers/specs/2026-11-16-supply-and-migration-design.md) is approved in its recorded scope; the [concrete implementation plan](docs/superpowers/plans/2026-11-16-civilian-logistics-and-migration.md) is ready for review, with later resource/fleet/interception deliveries separate. No tactical battlefield, generalized shipping network or migration system is claimed implemented.

## Build and run

Use Godot **4.7.2 .NET** and the installed .NET SDK compatible with the project's .NET8 targets. Open project.godot and run the project with F5. Clean-machine installation and target platforms other than the recorded Windows environment are not established by these instructions.

Use the [native shared test queue](docs/TEST_RUNNER.md) for necessary noninteractive builds/tests; do not start Docker. From the active checkout, a solution build is:

```powershell
.\tools\Test-Queued.ps1 -Suite build
```

For a linked worktree, pass its actual source root to the primary wrapper:

```powershell
& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot 'C:/path/to/active/worktree' -Suite build
```

Run only checks needed for the changes. The wrapper's default test invocation runs the full simulation project and requires the owner's explicit request. Other necessary noninteractive heavy commands use runner.py exec --project openantares; see TEST_RUNNER.md for source/evidence requirements and focused workers.

Development launch supports --new-game=SIZE:SEED after --, with small/medium/large sizes. Saves use Godot user://saves. Preserve existing saves and use distinct QA names when testing. For player observation use the [M14 journeys](docs/playtests/m14-player-acceptance-2026-11-08.md) and feature-specific handoffs; dated results remain tied to their tested source.

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
- [Current design guide](docs/DESIGN_GUIDE.md) and [supply/migration proposal](docs/superpowers/specs/2026-11-16-supply-and-migration-design.md)
- [Project concept](docs/PROJECT_CONCEPT.md), [architecture](docs/ARCHITECTURE.md), and [decision records](docs/DECISIONS/README.md)
- [Specifications](docs/SPECIFICATIONS.md) and [acceptance criteria](docs/ACCEPTANCE_CRITERIA.md)
- [Implementation plan](docs/IMPLEMENTATION_PLAN.md) and [validation report](reports/VALIDATION_REPORT.md)
- [Project workflow](docs/PROJECT_WORKFLOW.md), [adoption prompt](docs/MASTER_PROJECT_ADOPTION_PROMPT.md), and [implementation prompt](docs/MASTER_IMPLEMENTATION_PROMPT.md)

Code and content JSON are MIT; original assets are CC0; documentation is CC BY 4.0. See [LICENSING.md](LICENSING.md).
