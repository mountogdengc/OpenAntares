using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;

namespace OpenAntares.Simulation.Commands;

/// <summary>
/// The single validation and application path for every command. Never mutates the state it is
/// given: changes are applied to a copy, which becomes the published state only if the whole
/// command succeeds.
/// </summary>
public static class CommandProcessor
{
    public static CommandOutcome Execute(ContentSet content, GameState state, Command command)
    {
        EmpireState? issuer = state.FindEmpire(command.Issuer);
        if (issuer is null)
        {
            return CommandOutcome.Reject(state, RejectionCodes.UnknownEmpire, $"Empire {command.Issuer} does not exist.");
        }

        if (command.ExpectedTurn != state.Turn)
        {
            return CommandOutcome.Reject(state, RejectionCodes.StaleTurn,
                $"Command was issued for turn {command.ExpectedTurn}, but the current turn is {state.Turn}.");
        }

        return command switch
        {
            SetWorkforceCommand c => SetWorkforce(state, c),
            SetProjectCommand c => SetProject(content, state, issuer, c),
            SetResearchTargetCommand c => SetResearchTarget(content, state, issuer, c),
            SetReadyCommand c => SetReady(content, state, issuer, c),
            _ => throw new ArgumentException($"Unsupported command type {command.GetType().Name}.", nameof(command)),
        };
    }

    private static CommandOutcome SetWorkforce(GameState state, SetWorkforceCommand command)
    {
        if (!TryGetOwnedColony(state, command.Issuer, command.Colony, out ColonyState? colony, out CommandOutcome? rejection))
        {
            return rejection;
        }

        Workforce workforce = command.Workforce;
        if (workforce.HasNegativeCount)
        {
            return CommandOutcome.Reject(state, RejectionCodes.NegativeWorkforce, "Workforce counts must not be negative.");
        }

        long total = (long)workforce.Support + workforce.Production + workforce.Research;
        if (total != colony.Population)
        {
            return CommandOutcome.Reject(state, RejectionCodes.WorkforceSumMismatch,
                $"Workforce assigns {total} units, but {command.Colony} has population {colony.Population}.");
        }

        if (workforce == colony.Workforce)
        {
            return CommandOutcome.NoChange(state);
        }

        return ApplyPlanningChange(state, command.Issuer, candidate => candidate.FindColony(command.Colony)!.Workforce = workforce);
    }

    private static CommandOutcome SetProject(ContentSet content, GameState state, EmpireState issuer, SetProjectCommand command)
    {
        if (!TryGetOwnedColony(state, command.Issuer, command.Colony, out ColonyState? colony, out CommandOutcome? rejection))
        {
            return rejection;
        }

        string? buildingId = command.BuildingId;
        if (buildingId is not null)
        {
            if (!content.Buildings.TryGetValue(buildingId, out BuildingDefinition? building))
            {
                return CommandOutcome.Reject(state, RejectionCodes.UnknownBuilding, $"Building '{buildingId}' is not defined.");
            }

            if (colony.CompletedBuildingIds.Contains(buildingId))
            {
                return CommandOutcome.Reject(state, RejectionCodes.AlreadyCompleted,
                    $"{command.Colony} has already completed '{buildingId}'.");
            }

            if (!Rules.PrerequisitesKnown(issuer, building.PrerequisiteTechnologyIds))
            {
                return CommandOutcome.Reject(state, RejectionCodes.PrerequisiteNotMet,
                    $"'{buildingId}' requires technologies the empire does not know.");
            }
        }

        if (buildingId == colony.ProjectId)
        {
            return CommandOutcome.NoChange(state);
        }

        return ApplyPlanningChange(state, command.Issuer, candidate => candidate.FindColony(command.Colony)!.ProjectId = buildingId);
    }

    private static CommandOutcome SetResearchTarget(ContentSet content, GameState state, EmpireState issuer, SetResearchTargetCommand command)
    {
        string? technologyId = command.TechnologyId;
        if (technologyId is not null)
        {
            if (!content.Technologies.TryGetValue(technologyId, out TechnologyDefinition? technology))
            {
                return CommandOutcome.Reject(state, RejectionCodes.UnknownTechnology, $"Technology '{technologyId}' is not defined.");
            }

            if (issuer.KnownTechnologyIds.Contains(technologyId))
            {
                return CommandOutcome.Reject(state, RejectionCodes.AlreadyKnown, $"The empire already knows '{technologyId}'.");
            }

            if (!Rules.PrerequisitesKnown(issuer, technology.PrerequisiteTechnologyIds))
            {
                return CommandOutcome.Reject(state, RejectionCodes.PrerequisiteNotMet,
                    $"'{technologyId}' requires technologies the empire does not know.");
            }
        }

        if (technologyId == issuer.ResearchTargetId)
        {
            return CommandOutcome.NoChange(state);
        }

        return ApplyPlanningChange(state, command.Issuer, candidate => candidate.FindEmpire(command.Issuer)!.ResearchTargetId = technologyId);
    }

    private static CommandOutcome SetReady(ContentSet content, GameState state, EmpireState issuer, SetReadyCommand command)
    {
        if (issuer.Ready == command.Ready)
        {
            return CommandOutcome.NoChange(state);
        }

        GameState candidate = state.Clone();
        candidate.FindEmpire(command.Issuer)!.Ready = command.Ready;

        bool allHumansReady = candidate.Empires
            .Where(empire => empire.Controller == ControllerKind.Human)
            .All(empire => empire.Ready);
        if (!allHumansReady)
        {
            return new CommandOutcome(candidate, Rejection: null, Changed: true, Report: null);
        }

        // The readiness change and the resolution it triggers succeed or fail together.
        TurnResolution resolution = TurnResolver.Resolve(content, candidate);
        if (!resolution.Succeeded)
        {
            return CommandOutcome.Reject(state, RejectionCodes.ResolutionFailed,
                "Turn resolution failed: " + string.Join("; ", resolution.Errors));
        }

        return new CommandOutcome(resolution.State, Rejection: null, Changed: true, resolution.Report);
    }

    /// <summary>Applies a planning change to a copy of the state and clears the issuer's readiness.</summary>
    private static CommandOutcome ApplyPlanningChange(GameState state, EmpireId issuer, Action<GameState> change)
    {
        GameState candidate = state.Clone();
        change(candidate);
        candidate.FindEmpire(issuer)!.Ready = false;
        return new CommandOutcome(candidate, Rejection: null, Changed: true, Report: null);
    }

    private static bool TryGetOwnedColony(
        GameState state,
        EmpireId issuer,
        ColonyId colonyId,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ColonyState? colony,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out CommandOutcome? rejection)
    {
        colony = state.FindColony(colonyId);
        if (colony is null)
        {
            rejection = CommandOutcome.Reject(state, RejectionCodes.UnknownColony, $"Colony {colonyId} does not exist.");
            return false;
        }

        if (colony.EmpireId != issuer)
        {
            rejection = CommandOutcome.Reject(state, RejectionCodes.NotOwner, $"{issuer} does not own {colonyId}.");
            colony = null;
            return false;
        }

        rejection = null;
        return true;
    }
}
