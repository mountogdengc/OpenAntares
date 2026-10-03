using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using Xunit;
using static OpenAntares.Simulation.Tests.Fixtures;

namespace OpenAntares.Simulation.Tests;

public class CommandProcessorTests
{
    private readonly ContentSet _content = PrototypeContent();

    // --- Rejections leave state unchanged ---

    public static TheoryData<Command, string> RejectedCommands => new()
    {
        { new SetWorkforceCommand(new EmpireId(99), 0, HomeColony, new Workforce(2, 2, 2)), RejectionCodes.UnknownEmpire },
        { new SetWorkforceCommand(Empire, 1, HomeColony, new Workforce(3, 2, 1)), RejectionCodes.StaleTurn },
        { new SetWorkforceCommand(Empire, 0, new ColonyId(99), new Workforce(3, 2, 1)), RejectionCodes.UnknownColony },
        { new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(7, 0, -1)), RejectionCodes.NegativeWorkforce },
        { new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(3, 3, 3)), RejectionCodes.WorkforceSumMismatch },
        { new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(int.MaxValue, int.MaxValue, 2)), RejectionCodes.WorkforceSumMismatch },
        { new SetProjectCommand(Empire, 0, HomeColony, "no_such_building"), RejectionCodes.UnknownBuilding },
        { new SetProjectCommand(Empire, 0, HomeColony, "cultivation_hub"), RejectionCodes.PrerequisiteNotMet },
        { new SetResearchTargetCommand(Empire, 0, "no_such_technology"), RejectionCodes.UnknownTechnology },
    };

    [Theory]
    [MemberData(nameof(RejectedCommands))]
    public void RejectedCommandLeavesStateUnchanged(Command command, string expectedCode)
    {
        GameState state = StartingState();
        string before = Snapshot(state);

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, command);

        Assert.False(outcome.Accepted);
        Assert.Equal(expectedCode, outcome.Rejection!.Code);
        Assert.False(outcome.Changed);
        Assert.Same(state, outcome.State);
        Assert.Equal(before, Snapshot(state));
    }

    [Fact]
    public void CannotCommandAnotherEmpiresColony()
    {
        GameState state = StartingState();
        EmpireId other = AddEmpire(state, ControllerKind.Human);

        CommandOutcome outcome = CommandProcessor.Execute(_content, state,
            new SetWorkforceCommand(other, 0, HomeColony, new Workforce(3, 2, 1)));

        Assert.Equal(RejectionCodes.NotOwner, outcome.Rejection!.Code);
    }

    [Fact]
    public void CannotSelectCompletedBuilding()
    {
        GameState state = StartingState();
        state.Colonies[0].CompletedBuildingIds.Add("fabrication_hall");

        CommandOutcome outcome = CommandProcessor.Execute(_content, state,
            new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));

        Assert.Equal(RejectionCodes.AlreadyCompleted, outcome.Rejection!.Code);
    }

    [Fact]
    public void CannotSelectKnownTechnology()
    {
        GameState state = StartingState();
        state.Empires[0].KnownTechnologyIds.Add("cultivation_methods");

        CommandOutcome outcome = CommandProcessor.Execute(_content, state,
            new SetResearchTargetCommand(Empire, 0, "cultivation_methods"));

        Assert.Equal(RejectionCodes.AlreadyKnown, outcome.Rejection!.Code);
    }

    [Fact]
    public void TechnologyPrerequisitesAreEnforced()
    {
        var advanced = new TechnologyDefinition("advanced_cultivation", "Advanced Cultivation", 2000,
            ImmutableArray.Create("cultivation_methods"));
        ContentSet content = _content with { Technologies = _content.Technologies.Add(advanced.Id, advanced) };
        GameState state = StartingState();
        var command = new SetResearchTargetCommand(Empire, 0, "advanced_cultivation");

        Assert.Equal(RejectionCodes.PrerequisiteNotMet, CommandProcessor.Execute(content, state, command).Rejection!.Code);

        state.Empires[0].KnownTechnologyIds.Add("cultivation_methods");
        Assert.True(CommandProcessor.Execute(content, state, command).Accepted);
    }

    // --- Accepted planning commands ---

    [Fact]
    public void AcceptedCommandPublishesNewStateWithoutMutatingInput()
    {
        GameState state = StartingState();
        string before = Snapshot(state);

        CommandOutcome outcome = CommandProcessor.Execute(_content, state,
            new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(3, 1, 2)));

        Assert.True(outcome.Accepted);
        Assert.True(outcome.Changed);
        Assert.Equal(new Workforce(3, 1, 2), outcome.State.FindColony(HomeColony)!.Workforce);
        Assert.Equal(before, Snapshot(state));
    }

    [Fact]
    public void ProjectCanBeSelectedReplacedAndCleared()
    {
        GameState state = StartingState();
        state.Empires[0].KnownTechnologyIds.Add("cultivation_methods");

        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        Assert.Equal("fabrication_hall", state.FindColony(HomeColony)!.ProjectId);

        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "cultivation_hub"));
        Assert.Equal("cultivation_hub", state.FindColony(HomeColony)!.ProjectId);

        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, null));
        Assert.Null(state.FindColony(HomeColony)!.ProjectId);
    }

    [Fact]
    public void ResearchTargetCanBeSelectedAndCleared()
    {
        GameState state = StartingState();

        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "habitat_methods"));
        Assert.Equal("habitat_methods", state.FindEmpire(Empire)!.ResearchTargetId);

        state = Execute(state, new SetResearchTargetCommand(Empire, 0, null));
        Assert.Null(state.FindEmpire(Empire)!.ResearchTargetId);
    }

    [Fact]
    public void SelectionNeverSpendsReserves()
    {
        GameState state = StartingState();
        state.Colonies[0].ProductionReserve = 5000;
        state.Empires[0].ResearchReserve = 5000;

        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "cultivation_methods"));

        Assert.Equal(5000, state.FindColony(HomeColony)!.ProductionReserve);
        Assert.Empty(state.FindColony(HomeColony)!.CompletedBuildingIds);
        Assert.Equal(5000, state.FindEmpire(Empire)!.ResearchReserve);
        Assert.Empty(state.FindEmpire(Empire)!.KnownTechnologyIds);
    }

    // --- Readiness ---

    public static TheoryData<Command> ChangingPlanningCommands => new()
    {
        new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(3, 1, 2)),
        new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"),
        new SetResearchTargetCommand(Empire, 0, "habitat_methods"),
    };

    [Theory]
    [MemberData(nameof(ChangingPlanningCommands))]
    public void ChangedPlanningChoiceClearsReadiness(Command command)
    {
        GameState state = ReadyWithSecondHumanWaiting();

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, command);

        Assert.True(outcome.Changed);
        Assert.False(outcome.State.FindEmpire(Empire)!.Ready);
    }

    public static TheoryData<Command> UnchangedPlanningCommands => new()
    {
        new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(2, 2, 2)),
        new SetProjectCommand(Empire, 0, HomeColony, null),
        new SetResearchTargetCommand(Empire, 0, null),
        new SetReadyCommand(Empire, 0, true),
    };

    [Theory]
    [MemberData(nameof(UnchangedPlanningCommands))]
    public void UnchangedChoiceIsNoOpAndPreservesReadiness(Command command)
    {
        GameState state = ReadyWithSecondHumanWaiting();

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, command);

        Assert.True(outcome.Accepted);
        Assert.False(outcome.Changed);
        Assert.Same(state, outcome.State);
        Assert.True(outcome.State.FindEmpire(Empire)!.Ready);
    }

    [Fact]
    public void RejectedCommandPreservesReadiness()
    {
        GameState state = ReadyWithSecondHumanWaiting();

        CommandOutcome outcome = CommandProcessor.Execute(_content, state,
            new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(9, 9, 9)));

        Assert.False(outcome.Accepted);
        Assert.True(outcome.State.FindEmpire(Empire)!.Ready);
    }

    [Fact]
    public void ReadinessCanBeWithdrawn()
    {
        GameState state = ReadyWithSecondHumanWaiting();

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, new SetReadyCommand(Empire, 0, false));

        Assert.True(outcome.Changed);
        Assert.False(outcome.State.FindEmpire(Empire)!.Ready);
        Assert.Equal(0, outcome.State.Turn);
    }

    [Fact]
    public void SingleHumanMarkingReadyResolvesTheTurn()
    {
        GameState state = StartingState();

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, new SetReadyCommand(Empire, 0, true));

        Assert.True(outcome.Accepted);
        Assert.Equal(1, outcome.State.Turn);
        Assert.Equal(0, outcome.Report!.FromTurn);
        Assert.Equal(1, outcome.Report.ToTurn);
        Assert.False(outcome.State.FindEmpire(Empire)!.Ready);
        Assert.Equal(0, state.Turn);
    }

    [Fact]
    public void TurnWaitsForEveryHumanEmpire()
    {
        GameState state = StartingState();
        EmpireId second = AddEmpire(state, ControllerKind.Human);

        CommandOutcome first = CommandProcessor.Execute(_content, state, new SetReadyCommand(Empire, 0, true));
        Assert.Equal(0, first.State.Turn);
        Assert.Null(first.Report);
        Assert.True(first.State.FindEmpire(Empire)!.Ready);

        CommandOutcome last = CommandProcessor.Execute(_content, first.State, new SetReadyCommand(second, 0, true));
        Assert.Equal(1, last.State.Turn);
        Assert.NotNull(last.Report);
        Assert.All(last.State.Empires, empire => Assert.False(empire.Ready));
    }

    [Fact]
    public void AiEmpiresDoNotHoldUpResolution()
    {
        GameState state = StartingState();
        AddEmpire(state, ControllerKind.Ai);

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, new SetReadyCommand(Empire, 0, true));

        Assert.Equal(1, outcome.State.Turn);
    }

    [Fact]
    public void CommandsForThePreviousTurnAreStaleAfterResolution()
    {
        GameState state = Execute(StartingState(), new SetReadyCommand(Empire, 0, true));

        CommandOutcome outcome = CommandProcessor.Execute(_content, state,
            new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(3, 1, 2)));

        Assert.Equal(RejectionCodes.StaleTurn, outcome.Rejection!.Code);
    }

    [Fact]
    public void FailedResolutionRollsBackTheReadinessCommand()
    {
        GameState state = StartingState();
        // An invalid world: population above capacity makes resolution's final validation fail.
        ColonyState colony = state.Colonies[0];
        colony.Population = 11;
        colony.Workforce = new Workforce(5, 3, 3);
        string before = Snapshot(state);

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, new SetReadyCommand(Empire, 0, true));

        Assert.Equal(RejectionCodes.ResolutionFailed, outcome.Rejection!.Code);
        Assert.Same(state, outcome.State);
        Assert.False(outcome.State.FindEmpire(Empire)!.Ready);
        Assert.Equal(0, outcome.State.Turn);
        Assert.Equal(before, Snapshot(state));
    }

    // --- Determinism ---

    [Fact]
    public void SameStateAndCommandsGiveIdenticalResults()
    {
        Assert.Equal(Snapshot(RunScript()), Snapshot(RunScript()));
    }

    private GameState RunScript()
    {
        GameState state = StartingState();
        Command[] script =
        {
            new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(3, 1, 2)),
            new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"),
            new SetResearchTargetCommand(Empire, 0, "measurement_methods"),
            new SetReadyCommand(Empire, 0, true),
            new SetWorkforceCommand(Empire, 1, HomeColony, new Workforce(2, 2, 2)),
            new SetReadyCommand(Empire, 1, true),
        };

        foreach (Command command in script)
        {
            state = CommandProcessor.Execute(_content, state, command).State;
        }

        Assert.Equal(2, state.Turn);
        return state;
    }

    private GameState Execute(GameState state, Command command)
    {
        CommandOutcome outcome = CommandProcessor.Execute(_content, state, command);
        Assert.True(outcome.Accepted, outcome.Rejection?.Message);
        return outcome.State;
    }

    /// <summary>The starting empire is ready; a second human empire keeps the turn from resolving.</summary>
    private static GameState ReadyWithSecondHumanWaiting()
    {
        GameState state = StartingState();
        AddEmpire(state, ControllerKind.Human);
        state.Empires[0].Ready = true;
        return state;
    }
}
