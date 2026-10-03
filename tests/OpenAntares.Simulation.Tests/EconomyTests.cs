using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;
using Xunit;
using static OpenAntares.Simulation.Tests.Fixtures;

namespace OpenAntares.Simulation.Tests;

/// <summary>The worked turns and verification list from docs/FIRST_PLAYABLE_ECONOMY.md.</summary>
public class EconomyTests
{
    private readonly ContentSet _content = PrototypeContent();

    // --- Worked turns ---

    [Fact]
    public void BalancedDevelopment()
    {
        GameState state = StartingState();
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "cultivation_methods"));

        TurnEconomy forecast = EconomyCalculator.Calculate(_content, state);
        Assert.Equal(600, forecast.Colony(HomeColony).SupportProduced.Total);
        Assert.Equal(600, forecast.Colony(HomeColony).SupportRequired.Total);
        Assert.Equal(400, forecast.Colony(HomeColony).NetProduction.Total);
        Assert.Equal(400, forecast.Colony(HomeColony).NetResearch.Total);
        Assert.Equal(10, forecast.Colony(HomeColony).GrowthEarned.Total);

        state = EndTurn(state);
        AssertColony(state, population: 6, growth: 10, production: 400, research: 400);

        state = EndTurn(state);
        AssertColony(state, population: 6, growth: 20, production: 800, research: 800);

        state = EndTurn(state, out var report);
        AssertColony(state, population: 6, growth: 30, production: 0, research: 0);
        Assert.Equal("fabrication_hall", report.Colony(HomeColony).Production.CompletedId);
        Assert.Equal("cultivation_methods", report.Empire(Empire).Research.CompletedId);
        Assert.Equal(new[] { "fabrication_hall" }, Colony(state).CompletedBuildingIds);
        Assert.Equal(new[] { "cultivation_methods" }, state.FindEmpire(Empire)!.KnownTechnologyIds);
        Assert.Null(Colony(state).ProjectId);
        Assert.Null(state.FindEmpire(Empire)!.ResearchTargetId);

        // The hall's bonus contributes first during turn 3 to 4.
        state = EndTurn(state, out report);
        AssertColony(state, population: 6, growth: 40, production: 500, research: 400);
        Assert.Null(report.Colony(HomeColony).Production.CompletedId);
        Assert.Contains(report.Colony(HomeColony).NetProduction.Lines,
            line => line is { Kind: BreakdownKinds.Building, SourceId: "fabrication_hall", Amount: 100 });

        // Researching Cultivation Methods makes the hub selectable but adds no support by itself.
        Assert.Equal(600, report.Colony(HomeColony).SupportProduced.Total);
        Assert.True(CommandProcessor.Execute(_content, state, new SetProjectCommand(Empire, 4, HomeColony, "cultivation_hub")).Accepted);
    }

    [Theory]
    [InlineData(2, 2, 2, 600, 400, 400, 10)]
    [InlineData(3, 1, 2, 900, 200, 400, 40)]
    [InlineData(2, 3, 1, 600, 600, 200, 10)]
    [InlineData(1, 3, 2, 300, 300, 200, 0)]
    [InlineData(0, 3, 3, 0, 0, 0, 0)]
    public void AllocationTradeoff(int support, int production, int research, long supportProduced, long netProduction, long netResearch, int growthAfter)
    {
        GameState state = StartingState();
        Colony(state).Workforce = new Workforce(support, production, research);

        ColonyEconomy result = EconomyCalculator.Calculate(_content, state).Colony(HomeColony);

        Assert.Equal(supportProduced, result.SupportProduced.Total);
        Assert.Equal(600, result.SupportRequired.Total);
        Assert.Equal(netProduction, result.NetProduction.Total);
        Assert.Equal(netResearch, result.NetResearch.Total);
        Assert.Equal(growthAfter, result.Growth.ProgressAfter);
        Assert.Null(result.Production.CompletedId);
    }

    [Fact]
    public void OverflowCarriesRemainderAndCompletionWaitsForResolution()
    {
        GameState state = StartingState();
        Colony(state).ProductionReserve = 1000;
        state.FindEmpire(Empire)!.ResearchReserve = 1000;
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "cultivation_methods"));

        state = EndTurn(state, out var report);

        ColonyEconomy colony = report.Colony(HomeColony);
        Assert.Equal(400, colony.NetProduction.Total); // no hall bonus yet
        Assert.Equal(200, Colony(state).ProductionReserve);
        Assert.Equal(200, state.FindEmpire(Empire)!.ResearchReserve);
        Assert.Equal(new[] { "fabrication_hall" }, Colony(state).CompletedBuildingIds);
        Assert.Equal(new[] { "cultivation_methods" }, state.FindEmpire(Empire)!.KnownTechnologyIds);
        Assert.Equal(
            new[] { (BreakdownKinds.Earned, "", 400L), (BreakdownKinds.CostPaid, "fabrication_hall", -1200L) },
            colony.Production.Change.Lines.Select(l => (l.Kind, l.SourceId, l.Amount)));
    }

    [Fact]
    public void ReplacingOrClearingAProjectConservesTheReserve()
    {
        GameState state = StartingState();
        Colony(state).ProductionReserve = 1000;
        state.FindEmpire(Empire)!.KnownTechnologyIds.Add("measurement_methods");
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));

        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "analysis_lab"));
        Assert.Equal(1000, Colony(state).ProductionReserve);

        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, null));
        state = EndTurn(state);
        Assert.Equal(1400, Colony(state).ProductionReserve);
        Assert.Empty(Colony(state).CompletedBuildingIds);
    }

    [Fact]
    public void BirthJoinsSupportAndProducesFromTheNextTurn()
    {
        GameState state = StartingState();
        Colony(state).Workforce = new Workforce(3, 1, 2);
        Colony(state).GrowthProgress = 80;

        state = EndTurn(state, out var report);

        ColonyEconomy result = report.Colony(HomeColony);
        Assert.Equal(40, result.GrowthEarned.Total);
        Assert.Equal(200, result.NetProduction.Total);
        Assert.Equal(400, result.NetResearch.Total);
        Assert.Equal(1, result.Growth.Births);
        Assert.Equal(7, Colony(state).Population);
        Assert.Equal(20, Colony(state).GrowthProgress);
        Assert.Equal(new Workforce(4, 1, 2), Colony(state).Workforce);
    }

    [Fact]
    public void ReachingCapacityDiscardsLeftoverGrowth()
    {
        GameState state = StartingState();
        Colony(state).Population = 9;
        Colony(state).Workforce = new Workforce(4, 3, 2);
        Colony(state).GrowthProgress = 80;

        state = EndTurn(state, out var report);

        ColonyEconomy result = report.Colony(HomeColony);
        Assert.Equal(300, result.SupportSurplus);
        Assert.Equal(40, result.GrowthEarned.Total);
        Assert.Equal(600, result.NetProduction.Total);
        Assert.Equal(400, result.NetResearch.Total);
        Assert.Equal(20, result.Growth.DiscardedGrowth);
        Assert.True(result.Growth.AtCapacity);
        Assert.Equal(10, Colony(state).Population);
        Assert.Equal(new Workforce(5, 3, 2), Colony(state).Workforce);
        Assert.Equal(0, Colony(state).GrowthProgress);
    }

    [Fact]
    public void CapacityExtensionTakesEffectNextTurn()
    {
        GameState state = StartingState();
        ColonyState colony = Colony(state);
        colony.Population = 10;
        colony.Workforce = new Workforce(4, 3, 3);
        colony.ProductionReserve = 1600;
        state.FindEmpire(Empire)!.KnownTechnologyIds.Add("habitat_methods");
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "habitat_extension"));

        state = EndTurn(state, out var report);

        ColonyEconomy result = report.Colony(HomeColony);
        Assert.Equal("habitat_extension", result.Production.CompletedId);
        Assert.Equal(0, result.GrowthEarned.Total);
        Assert.Equal(BreakdownKinds.CapacityLimit, result.GrowthEarned.Lines[^1].Kind);
        Assert.Equal(10, Colony(state).Population);
        Assert.Equal(0, Colony(state).GrowthProgress);
        Assert.Equal(12, Rules.ColonyCapacity(_content, Colony(state)));

        state = EndTurn(state, out report);
        Assert.Equal(30, report.Colony(HomeColony).GrowthEarned.Total); // surplus 200 -> 20 bonus + 10 base
        Assert.Equal(30, Colony(state).GrowthProgress);
    }

    [Fact]
    public void ShortageReducesOutputAndPausesGrowth()
    {
        GameState state = StartingState();
        Colony(state).Population = 7;
        Colony(state).Workforce = new Workforce(2, 3, 2);
        Colony(state).GrowthProgress = 20;

        state = EndTurn(state, out var report);

        ColonyEconomy result = report.Colony(HomeColony);
        Assert.Equal(100, result.SupportDeficit);
        Assert.Equal(514, result.NetProduction.Total);
        Assert.Equal(342, result.NetResearch.Total);
        Assert.Equal(0, result.GrowthEarned.Total);
        Assert.Equal(20, Colony(state).GrowthProgress);
        Assert.Equal(7, Colony(state).Population);

        Assert.Equal(
            new[]
            {
                new BreakdownLine(BreakdownKinds.Workers, "verdant_world", 3, 600),
                new BreakdownLine(BreakdownKinds.Shortage, "", null, -86, new DivisionDetails(360000, 700, 514, 200)),
            },
            result.NetProduction.Lines);
        Assert.Equal(-58, result.NetResearch.Lines[^1].Amount);
    }

    // --- Breakdown lines ---

    [Fact]
    public void SurplusGrowthFloorIsExplainedByDivisionDetails()
    {
        ContentSet content = WithPlanetType(new PlanetTypeDefinition("test_world", "Test World", 405, 0, 0, 0));
        GameState state = StartingState();
        state.Planets[0].PlanetTypeId = "test_world";
        Colony(state).Population = 1;
        Colony(state).Workforce = new Workforce(1, 0, 0);

        ColonyEconomy result = EconomyCalculator.Calculate(content, state).Colony(HomeColony);

        Assert.Equal(305, result.SupportSurplus);
        Assert.Equal(
            new[]
            {
                new BreakdownLine(BreakdownKinds.GrowthBase, "", null, 10),
                new BreakdownLine(BreakdownKinds.GrowthSurplus, "", null, 30, new DivisionDetails(305, 10, 30, 5)),
            },
            result.GrowthEarned.Lines);
        Assert.Equal(40, result.GrowthEarned.Total);
        Assert.Equal(0, result.UnusedSupport);
    }

    [Fact]
    public void SurplusBonusAboveTheMaximumIsCapped()
    {
        GameState state = StartingState();
        Colony(state).Workforce = new Workforce(5, 0, 1);

        ColonyEconomy result = EconomyCalculator.Calculate(_content, state).Colony(HomeColony);

        Assert.Equal(900, result.SupportSurplus);
        Assert.Equal(
            new[] { (BreakdownKinds.GrowthBase, 10L), (BreakdownKinds.GrowthSurplus, 90L), (BreakdownKinds.Cap, -50L) },
            result.GrowthEarned.Lines.Select(l => (l.Kind, l.Amount)));
        Assert.Equal(50, result.GrowthEarned.Total);
        Assert.Equal(500, result.UnusedSupport);
    }

    [Fact]
    public void ColonyUsesItsOwnPlanetTypeRates()
    {
        GameState state = StartingState();
        state.Planets[0].PlanetTypeId = "forge_world";

        ColonyEconomy result = EconomyCalculator.Calculate(_content, state).Colony(HomeColony);

        Assert.Equal(400, result.SupportProduced.Total);
        Assert.Equal(600, result.GrossProduction.Total);
        Assert.Equal(400, result.NetProduction.Total); // floor(600 * 400 / 600)
        Assert.Equal(266, result.NetResearch.Total); // floor(400 * 400 / 600)
        Assert.All(result.GrossProduction.Lines, line => Assert.Equal("forge_world", line.SourceId));
    }

    [Fact]
    public void EmpiresOnlyReceiveTheirOwnColoniesResearch()
    {
        GameState state = StartingState();
        EmpireId other = AddEmpire(state, ControllerKind.Ai);

        TurnEconomy economy = EconomyCalculator.Calculate(_content, state);

        Assert.Equal(400, economy.Empire(Empire).NetResearch.Total);
        Assert.Equal(HomeColony.ToString(), Assert.Single(economy.Empire(Empire).NetResearch.Lines).SourceId);
        Assert.Equal(133, economy.Empire(other).NetResearch.Total); // forge world 1/1/1: floor(200 * 200 / 300)
        Assert.DoesNotContain(economy.Empire(other).NetResearch.Lines, line => line.SourceId == HomeColony.ToString());

        state = EndTurn(state);
        Assert.Equal(400, state.FindEmpire(Empire)!.ResearchReserve);
        Assert.Equal(133, state.FindEmpire(other)!.ResearchReserve);
    }

    // --- Timing, completion, and conservation ---

    [Fact]
    public void ForecastEqualsResolution()
    {
        GameState state = StartingState();
        Colony(state).Workforce = new Workforce(3, 2, 1);
        Colony(state).ProductionReserve = 900;
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));

        TurnEconomy forecast = EconomyCalculator.Calculate(_content, state);
        GameState resolved = EndTurn(state, out var report);

        Assert.Equal(JsonSerializer.Serialize(forecast), JsonSerializer.Serialize(report));
        ColonyEconomy colony = forecast.Colony(HomeColony);
        Assert.Equal(colony.Production.Ending, Colony(resolved).ProductionReserve);
        Assert.Equal(colony.Growth.PopulationAfter, Colony(resolved).Population);
        Assert.Equal(colony.Growth.ProgressAfter, Colony(resolved).GrowthProgress);
    }

    [Fact]
    public void CompletedTargetIsClearedAndLaterTurnsBank()
    {
        GameState state = StartingState();
        Colony(state).ProductionReserve = 1200;
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));

        state = EndTurn(state, out var first);
        state = EndTurn(state, out var second);

        Assert.Equal("fabrication_hall", first.Colony(HomeColony).Production.CompletedId);
        Assert.Null(second.Colony(HomeColony).Production.CompletedId);
        Assert.Null(second.Colony(HomeColony).Production.TargetId);
        Assert.Null(second.Colony(HomeColony).Production.Estimate);
        Assert.Equal(400 + 500, Colony(state).ProductionReserve);
    }

    [Fact]
    public void OnlyOneCompletionPerColonyAndEmpirePerTurn()
    {
        GameState state = StartingState();
        Colony(state).ProductionReserve = 100_000;
        state.FindEmpire(Empire)!.ResearchReserve = 100_000;
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "cultivation_methods"));

        state = EndTurn(state);

        Assert.Single(Colony(state).CompletedBuildingIds);
        Assert.Single(state.FindEmpire(Empire)!.KnownTechnologyIds);
        Assert.Equal(100_000 + 400 - 1200, Colony(state).ProductionReserve);
        Assert.Equal(100_000 + 400 - 1200, state.FindEmpire(Empire)!.ResearchReserve);
    }

    [Fact]
    public void TechnologyResearchedThisTurnUnlocksBuildingsOnlyFromTheNextPlanningPeriod()
    {
        GameState state = StartingState();
        state.FindEmpire(Empire)!.ResearchReserve = 1200;
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "cultivation_methods"));

        Assert.False(CommandProcessor.Execute(_content, state, new SetProjectCommand(Empire, 0, HomeColony, "cultivation_hub")).Accepted);

        state = EndTurn(state);
        Assert.True(CommandProcessor.Execute(_content, state, new SetProjectCommand(Empire, 1, HomeColony, "cultivation_hub")).Accepted);
    }

    [Fact]
    public void ReservesSatisfyTheConservationIdentityEveryTurn()
    {
        GameState state = StartingState();
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "habitat_methods"));

        for (int turn = 0; turn < 8; turn++)
        {
            state = EndTurn(state, out var report);
            AssertConsistent(report);
        }
    }

    [Fact]
    public void OverflowFailsResolutionAndLeavesStateUnchanged()
    {
        GameState state = StartingState();
        Colony(state).ProductionReserve = long.MaxValue - 100;
        string before = Snapshot(state);

        CommandOutcome outcome = CommandProcessor.Execute(_content, state, new SetReadyCommand(Empire, 0, true));

        Assert.Equal(RejectionCodes.ResolutionFailed, outcome.Rejection!.Code);
        Assert.Contains("overflow", outcome.Rejection.Message);
        Assert.Equal(before, Snapshot(outcome.State));
    }

    [Theory]
    [InlineData(1200, 0, 400, 3L)]
    [InlineData(1201, 0, 400, 4L)]
    [InlineData(1200, 1000, 400, 1L)]
    [InlineData(1200, 1200, 0, 1L)]
    [InlineData(1200, 0, 0, null)]
    public void CompletionEstimate(long cost, long reserve, long perTurn, long? expectedTurns)
    {
        TurnEstimate estimate = EconomyCalculator.Estimate(cost, reserve, perTurn);

        Assert.Equal(expectedTurns, estimate.Turns);
        Assert.Equal(expectedTurns is null, estimate.Stalled);
    }

    [Fact]
    public void ManyTurnsAreDeterministic()
    {
        Assert.Equal(PlayTwelveTurns(), PlayTwelveTurns());
    }

    private string PlayTwelveTurns()
    {
        GameState state = StartingState();
        var log = new System.Text.StringBuilder();
        state = Execute(state, new SetWorkforceCommand(Empire, 0, HomeColony, new Workforce(3, 2, 1)));
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "habitat_methods"));
        for (int turn = 0; turn < 12; turn++)
        {
            state = EndTurn(state, out var report);
            log.AppendLine(JsonSerializer.Serialize(report));
        }

        return log.Append(Snapshot(state)).ToString();
    }

    // --- Helpers ---

    private static void AssertConsistent(TurnEconomy economy)
    {
        foreach (ColonyEconomy colony in economy.Colonies)
        {
            AssertReserve(colony.Production, colony.NetProduction.Total);
            foreach (var quantity in new[] { colony.NetProduction, colony.NetResearch, colony.GrowthEarned })
            {
                Assert.Equal(quantity.Total, quantity.Lines.Sum(l => l.Amount));
                foreach (DivisionDetails division in quantity.Lines.Select(l => l.Division).OfType<DivisionDetails>())
                {
                    Assert.Equal(division.Numerator, (Int128)division.Quotient * division.Divisor + division.Remainder);
                    Assert.InRange(division.Remainder, 0, division.Divisor - 1);
                }
            }
        }

        foreach (EmpireEconomy empire in economy.Empires)
        {
            AssertReserve(empire.Research, empire.NetResearch.Total);
        }
    }

    private static void AssertReserve(ReserveOutcome reserve, long earned)
    {
        long costPaid = reserve.CompletedId is null ? 0 : reserve.TargetCost!.Value;
        Assert.Equal(reserve.Starting + earned - costPaid, reserve.Ending);
    }

    private ContentSet WithPlanetType(PlanetTypeDefinition planetType) =>
        _content with { PlanetTypes = _content.PlanetTypes.Add(planetType.Id, planetType) };

    private static ColonyState Colony(GameState state) => state.FindColony(HomeColony)!;

    private static void AssertColony(GameState state, int population, int growth, long production, long research)
    {
        Assert.Equal(population, Colony(state).Population);
        Assert.Equal(growth, Colony(state).GrowthProgress);
        Assert.Equal(production, Colony(state).ProductionReserve);
        Assert.Equal(research, state.FindEmpire(Empire)!.ResearchReserve);
    }

    private GameState Execute(GameState state, Command command)
    {
        CommandOutcome outcome = CommandProcessor.Execute(_content, state, command);
        Assert.True(outcome.Accepted, outcome.Rejection?.Message);
        return outcome.State;
    }

    private GameState EndTurn(GameState state) => EndTurn(state, out _);

    private GameState EndTurn(GameState state, out TurnEconomy economy)
    {
        CommandOutcome outcome = CommandProcessor.Execute(_content, state, new SetReadyCommand(Empire, state.Turn, true));
        Assert.True(outcome.Accepted, outcome.Rejection?.Message);
        economy = outcome.Report!.Economy;
        return outcome.State;
    }
}
