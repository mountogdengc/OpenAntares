using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Economy;

/// <summary>
/// The first playable economy (docs/FIRST_PLAYABLE_ECONOMY.md). A pure calculation over a state
/// snapshot: it never modifies state and consumes no randomness. Turn resolution applies its result;
/// presentation shows the same result as the forecast.
/// </summary>
public static class EconomyCalculator
{
    /// <summary>Support surplus needed per hundredth of a workforce unit of bonus growth.</summary>
    public const long SupportPerGrowthPoint = 10;

    /// <summary>Growth progress that becomes one workforce unit.</summary>
    public const int GrowthPerBirth = 100;

    public static TurnEconomy Calculate(ContentSet content, GameState state)
    {
        ImmutableArray<ColonyEconomy> colonies = state.Colonies
            .OrderBy(c => c.Id)
            .Select(colony => CalculateColony(content, state, colony))
            .ToImmutableArray();

        ImmutableArray<EmpireEconomy> empires = state.Empires
            .OrderBy(e => e.Id)
            .Select(empire => CalculateEmpire(content, empire, colonies))
            .ToImmutableArray();

        return new TurnEconomy(colonies, empires);
    }

    /// <summary>
    /// Turns to complete a target of <paramref name="cost"/> from <paramref name="reserve"/> at
    /// <paramref name="perTurn"/> output: one if the reserve already covers it, stalled if output is zero.
    /// </summary>
    public static TurnEstimate Estimate(long cost, long reserve, long perTurn)
    {
        if (reserve >= cost)
        {
            return new TurnEstimate(1);
        }

        if (perTurn <= 0)
        {
            return new TurnEstimate(null);
        }

        Int128 remaining = (Int128)cost - reserve;
        return new TurnEstimate(checked((long)((remaining + perTurn - 1) / perTurn)));
    }

    private static ColonyEconomy CalculateColony(ContentSet content, GameState state, ColonyState colony)
    {
        PlanetState planet = state.FindPlanet(colony.PlanetId)
            ?? throw new InvalidOperationException($"{colony.Id} refers to missing {colony.PlanetId}.");
        PlanetTypeDefinition planetType = content.PlanetTypes[planet.PlanetTypeId];
        EmpireState empire = state.FindEmpire(colony.EmpireId)
            ?? throw new InvalidOperationException($"{colony.Id} refers to missing {colony.EmpireId}.");
        List<BuildingDefinition> buildings = colony.CompletedBuildingIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => content.Buildings[id])
            .ToList();
        Workforce workforce = colony.Workforce;

        // Support and outputs.
        Quantity supportProduced = Output(planetType.Id, workforce.Support, planetType.SupportPerWorker, buildings, EffectKinds.Support);
        Quantity supportRequired = Quantity.From(new BreakdownLine(
            BreakdownKinds.Population, string.Empty, colony.Population, checked(colony.Population * content.Rules.SupportPerPopulation)));
        long supportSurplus = Math.Max(0, supportProduced.Total - supportRequired.Total);
        long supportDeficit = Math.Max(0, supportRequired.Total - supportProduced.Total);
        long supportSatisfied = Math.Min(supportProduced.Total, supportRequired.Total);

        Quantity grossProduction = Output(planetType.Id, workforce.Production, planetType.ProductionPerWorker, buildings, EffectKinds.Production);
        Quantity grossResearch = Output(planetType.Id, workforce.Research, planetType.ResearchPerWorker, buildings, EffectKinds.Research);
        Quantity netProduction = ApplyShortage(grossProduction, supportSatisfied, supportRequired.Total);
        Quantity netResearch = ApplyShortage(grossResearch, supportSatisfied, supportRequired.Total);

        // Growth uses the snapshot capacity; buildings completed this turn don't change it.
        int capacity = Rules.ColonyCapacity(content, colony);
        Quantity growthEarned = Growth(content.Rules, supportSurplus, supportDeficit, colony.Population >= capacity);
        GrowthOutcome growth = ApplyGrowth(colony, capacity, checked((int)growthEarned.Total));
        long unusedSupport = Math.Max(0, supportSurplus - checked(content.Rules.MaxSurplusGrowth * SupportPerGrowthPoint));

        // Production reserve and project completion.
        BuildingDefinition? project = colony.ProjectId is { } projectId && content.Buildings.TryGetValue(projectId, out var definition)
            ? definition
            : null;
        bool projectLegal = project is not null
            && !colony.CompletedBuildingIds.Contains(project.Id)
            && Rules.PrerequisitesKnown(empire, project.PrerequisiteTechnologyIds);
        ReserveOutcome production = Reserve(colony.ProductionReserve, netProduction.Total, project?.Id, project?.Cost, projectLegal);

        return new ColonyEconomy(
            colony.Id, colony.EmpireId, planetType.Id,
            supportProduced, supportRequired, supportSurplus, supportDeficit, unusedSupport,
            grossProduction, grossResearch, netProduction, netResearch,
            growthEarned, growth, production);
    }

    private static EmpireEconomy CalculateEmpire(ContentSet content, EmpireState empire, ImmutableArray<ColonyEconomy> colonies)
    {
        // Colonies are already in ID order; only this empire's colonies contribute.
        Quantity netResearch = Quantity.From(colonies
            .Where(c => c.Empire == empire.Id)
            .Select(c => new BreakdownLine(BreakdownKinds.Colony, c.Colony.ToString(), null, c.NetResearch.Total))
            .ToImmutableArray());

        TechnologyDefinition? target = empire.ResearchTargetId is { } targetId && content.Technologies.TryGetValue(targetId, out var definition)
            ? definition
            : null;
        bool targetLegal = target is not null
            && !empire.KnownTechnologyIds.Contains(target.Id)
            && Rules.PrerequisitesKnown(empire, target.PrerequisiteTechnologyIds);
        ReserveOutcome research = Reserve(empire.ResearchReserve, netResearch.Total, target?.Id, target?.Cost, targetLegal);

        return new EmpireEconomy(empire.Id, netResearch, research);
    }

    /// <summary>Workers times rate, then each completed building's bonus of <paramref name="effectKind"/>.</summary>
    private static Quantity Output(string planetTypeId, int workers, long ratePerWorker, List<BuildingDefinition> buildings, string effectKind)
    {
        var lines = ImmutableArray.CreateBuilder<BreakdownLine>();
        lines.Add(new BreakdownLine(BreakdownKinds.Workers, planetTypeId, workers, checked(workers * ratePerWorker)));
        foreach (BuildingDefinition building in buildings)
        {
            foreach (BuildingEffect effect in building.Effects)
            {
                if (effect.Kind == effectKind)
                {
                    lines.Add(new BreakdownLine(BreakdownKinds.Building, building.Id, null, effect.Amount));
                }
            }
        }

        return Quantity.From(lines.ToImmutable());
    }

    /// <summary>
    /// Scales output by the share of support satisfied, flooring to stored hundredths. The deduction,
    /// rounding included, is one <see cref="BreakdownKinds.Shortage"/> line carrying the division.
    /// </summary>
    private static Quantity ApplyShortage(Quantity gross, long satisfied, long required)
    {
        if (satisfied >= required)
        {
            return gross;
        }

        DivisionDetails division = DivisionDetails.Divide((Int128)gross.Total * satisfied, required);
        long deduction = checked(division.Quotient - gross.Total);
        return Quantity.From(gross.Lines.Add(new BreakdownLine(BreakdownKinds.Shortage, string.Empty, null, deduction, division)));
    }

    private static Quantity Growth(RulesParameters rules, long supportSurplus, long supportDeficit, bool atCapacity)
    {
        if (supportDeficit > 0)
        {
            // A shortage pauses growth; existing progress is kept.
            return Quantity.From(new BreakdownLine(BreakdownKinds.Shortage, string.Empty, null, 0));
        }

        var lines = ImmutableArray.CreateBuilder<BreakdownLine>();
        lines.Add(new BreakdownLine(BreakdownKinds.GrowthBase, string.Empty, null, rules.BaseGrowth));

        DivisionDetails surplusBonus = DivisionDetails.Divide(supportSurplus, SupportPerGrowthPoint);
        lines.Add(new BreakdownLine(BreakdownKinds.GrowthSurplus, string.Empty, null, surplusBonus.Quotient, surplusBonus));
        if (surplusBonus.Quotient > rules.MaxSurplusGrowth)
        {
            lines.Add(new BreakdownLine(BreakdownKinds.Cap, string.Empty, null, rules.MaxSurplusGrowth - surplusBonus.Quotient));
        }

        if (atCapacity)
        {
            long earned = Quantity.From(lines.ToImmutable()).Total;
            lines.Add(new BreakdownLine(BreakdownKinds.CapacityLimit, string.Empty, null, -earned));
        }

        return Quantity.From(lines.ToImmutable());
    }

    private static GrowthOutcome ApplyGrowth(ColonyState colony, int capacity, int growthEarned)
    {
        int population = colony.Population;
        if (population >= capacity)
        {
            return new GrowthOutcome(capacity, population, population, 0, colony.GrowthProgress, 0, 0, colony.Workforce);
        }

        int progress = checked(colony.GrowthProgress + growthEarned);
        int births = Math.Min(progress / GrowthPerBirth, capacity - population);
        int populationAfter = population + births;
        int remaining = progress - births * GrowthPerBirth;

        // Reaching capacity discards the leftover growth.
        bool reachedCapacity = populationAfter == capacity;
        int progressAfter = reachedCapacity ? 0 : remaining;
        int discarded = reachedCapacity ? remaining : 0;

        // New workers are assigned to support; they produce and consume from the next turn.
        Workforce workforceAfter = colony.Workforce with { Support = colony.Workforce.Support + births };

        return new GrowthOutcome(capacity, population, populationAfter, births, colony.GrowthProgress, progressAfter, discarded, workforceAfter);
    }

    private static ReserveOutcome Reserve(long starting, long earned, string? targetId, long? targetCost, bool targetLegal)
    {
        var lines = ImmutableArray.CreateBuilder<BreakdownLine>();
        lines.Add(new BreakdownLine(BreakdownKinds.Earned, string.Empty, null, earned));

        long available = checked(starting + earned);
        string? completed = null;
        if (targetLegal && available >= targetCost!.Value)
        {
            lines.Add(new BreakdownLine(BreakdownKinds.CostPaid, targetId!, null, -targetCost.Value));
            completed = targetId;
        }

        Quantity change = Quantity.From(lines.ToImmutable());
        TurnEstimate? estimate = targetCost is { } cost ? Estimate(cost, starting, earned) : null;
        return new ReserveOutcome(starting, change, checked(starting + change.Total), targetId, targetCost, completed, estimate);
    }
}
