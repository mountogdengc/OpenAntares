using OpenAntares.Simulation.Crises;
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
            .Select(empire => CalculateEmpire(content, state, empire, colonies))
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

    internal static ColonyEconomy CalculateColony(ContentSet content, GameState state, ColonyState colony)
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
        EconomyProfile profile = EconomyProfile.For(content, state, colony.EmpireId);

        // Support and outputs.
        Quantity supportProduced = Output(planetType.Id, workforce.Support, planetType.SupportPerWorker, buildings, EffectKinds.Support);
        Quantity supportRequired = Quantity.From(new BreakdownLine(
            BreakdownKinds.Population, profile.SpeciesId ?? string.Empty, colony.Population, checked(colony.Population * profile.SupportPerPopulation)));
        long supportSurplus = Math.Max(0, supportProduced.Total - supportRequired.Total);
        long supportDeficit = Math.Max(0, supportRequired.Total - supportProduced.Total);
        long supportSatisfied = Math.Min(supportProduced.Total, supportRequired.Total);

        Quantity grossProduction = Output(planetType.Id, workforce.Production, planetType.ProductionPerWorker, buildings, EffectKinds.Production, profile);
        Quantity grossResearch = Output(planetType.Id, workforce.Research, planetType.ResearchPerWorker, buildings, EffectKinds.Research, profile);
        Quantity netProduction = ApplyShortage(grossProduction, supportSatisfied, supportRequired.Total);
        Quantity netResearch = ApplyShortage(grossResearch, supportSatisfied, supportRequired.Total);

        // Growth uses the snapshot capacity; buildings completed this turn don't change it.
        int capacity = Rules.ColonyCapacity(content, state, colony);
        Quantity growthEarned = Growth(profile, supportSurplus, supportDeficit, colony.Population >= capacity);
        GrowthOutcome growth = ApplyGrowth(colony, capacity, checked((int)growthEarned.Total));
        Int128 unused = (Int128)supportSurplus - (Int128)profile.MaxSurplusGrowth * profile.SupportPerGrowthPoint;
        long unusedSupport = unused > 0 ? checked((long)unused) : 0;

        // Production reserve and project completion.
        BuildingDefinition? project = colony.ProjectKind == ProductionKinds.Building && colony.ProjectId is { } projectId && content.Buildings.TryGetValue(projectId, out var definition)
            ? definition
            : null;
        bool projectLegal = project is not null
            && !colony.CompletedBuildingIds.Contains(project.Id)
            && Rules.PrerequisitesKnown(empire, project.PrerequisiteTechnologyIds);
        ReserveOutcome production = new(colony.ProductionReserve,Quantity.Zero,colony.ProductionReserve,project?.Id,project?.Cost,null,null);

        var result = new ColonyEconomy(
            colony.Id, colony.EmpireId, planetType.Id,
            supportProduced, supportRequired, supportSurplus, supportDeficit, unusedSupport,
            grossProduction, grossResearch, netProduction, netResearch,
            growthEarned, growth, production) { PopulationUnitLabel = profile.PopulationUnitLabel };
        result = CrisisEconomy.ApplyColony(content,state,colony,result);
        if(Rules.HasDiplomacy(state.RulesVersion))
        {
            long trade=Diplomacy.DiplomacyEconomy.TradeOutput(content,Diplomacy.DiplomacyFrame.Capture(content,state),colony.Id);
            if(trade>0)result=result with{NetProduction=Quantity.From(result.NetProduction.Lines.Add(new("diplomacy_trade","trade_agreement",null,trade)))};
        }
        result = result with {Production=Diplomacy.DiplomacyReservations.CalculateReserve(state,colony,result.NetProduction.Total,project?.Id,project?.Cost,projectLegal)};
        result=VesselProduction.Calculate(content,state,colony,planet,planetType,buildings,result);
        result=ShipDesigns.ShipDesignProduction.Calculate(content,state,colony,result);
        return ShipDesigns.RefitEconomy.Calculate(content,state,colony,result);
    }

    private static EmpireEconomy CalculateEmpire(ContentSet content, GameState state, EmpireState empire, ImmutableArray<ColonyEconomy> colonies)
    {
        // Colonies are already in ID order; only this empire's colonies contribute.
        Quantity netResearch = Quantity.From(colonies
            .Where(c => c.Empire == empire.Id)
            .Select(c => new BreakdownLine(BreakdownKinds.Colony, c.Colony.ToString(), null, c.NetResearch.Total))
            .ToImmutableArray());

        TechnologyDefinition? target = empire.ResearchTargetId is { } targetId && Rules.TechnologyAvailable(content, state.RulesVersion, targetId) && content.Technologies.TryGetValue(targetId, out var definition)
            ? definition
            : null;
        bool targetLegal = target is not null
            && !empire.KnownTechnologyIds.Contains(target.Id)
            && Rules.PrerequisitesKnown(empire, target.PrerequisiteTechnologyIds);
        CureAllocation? cure=Rules.HasCrises(state.RulesVersion)&&state.Crisis is{Enabled:true}?CrisisEconomy.SplitResearch(content,state,empire.Id,netResearch.Total):null;
        ReserveOutcome research = Reserve(empire.ResearchReserve, cure?.RemainingForTechnology??netResearch.Total, target?.Id, target?.Cost, targetLegal);
        if(cure is{Diverted:>0})
        {
            var lines=research.Change.Lines.SetItem(0,new(BreakdownKinds.Earned,string.Empty,null,netResearch.Total));
            lines=lines.Insert(1,new(BreakdownKinds.CrisisCure,empire.Crisis!.Episode!.FamilyId,null,-cure.Diverted,DivisionDetails.Divide((Int128)netResearch.Total*empire.Crisis.Episode.ResearchShare,100)));
            research=research with{Change=Quantity.From(lines)};
        }
        return new EmpireEconomy(empire.Id, netResearch, research){Cure=cure};
    }

    /// <summary>Workers times rate, then each completed building's bonus of <paramref name="effectKind"/>.</summary>
    internal static Quantity Output(string planetTypeId, int workers, long ratePerWorker, List<BuildingDefinition> buildings, string effectKind, EconomyProfile? profile = null)
    {
        var lines = ImmutableArray.CreateBuilder<BreakdownLine>();
        Int128 workerProduct = checked((Int128)workers * ratePerWorker);
        if (profile?.SpeciesId is { } speciesId && effectKind is EffectKinds.Production or EffectKinds.Research)
        {
            int basisPoints = effectKind == EffectKinds.Production ? profile.ProductionBasisPoints : profile.ResearchBasisPoints;
            DivisionDetails division = DivisionDetails.Divide(checked(workerProduct * basisPoints), 10000);
            if (workerProduct > long.MaxValue)
            {
                // A reducing factor can make valid output from a raw subtotal too wide for a line.
                // Keep that contribution together; the division retains the exact wide calculation.
                lines.Add(new BreakdownLine(BreakdownKinds.SpeciesWorkers, speciesId, workers, division.Quotient, division));
            }
            else
            {
                long baseOutput = checked((long)workerProduct);
                lines.Add(new BreakdownLine(BreakdownKinds.Workers, planetTypeId, workers, baseOutput));
                lines.Add(new BreakdownLine(BreakdownKinds.Species, speciesId, workers, checked(division.Quotient - baseOutput), division));
            }
        }
        else lines.Add(new BreakdownLine(BreakdownKinds.Workers, planetTypeId, workers, checked((long)workerProduct)));
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

    private static Quantity Growth(EconomyProfile rules, long supportSurplus, long supportDeficit, bool atCapacity)
    {
        if (supportDeficit > 0)
        {
            // A shortage pauses growth; existing progress is kept.
            return Quantity.From(new BreakdownLine(BreakdownKinds.Shortage, string.Empty, null, 0));
        }

        var lines = ImmutableArray.CreateBuilder<BreakdownLine>();
        lines.Add(new BreakdownLine(BreakdownKinds.GrowthBase, rules.SpeciesId ?? string.Empty, null, rules.BaseGrowth));

        DivisionDetails surplusBonus = DivisionDetails.Divide(supportSurplus, rules.SupportPerGrowthPoint);
        lines.Add(new BreakdownLine(BreakdownKinds.GrowthSurplus, rules.SpeciesId ?? string.Empty, null, surplusBonus.Quotient, surplusBonus));
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

    internal static GrowthOutcome ApplyGrowth(ColonyState colony, int capacity, int growthEarned)
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

    internal static ReserveOutcome Reserve(long starting, long earned, string? targetId, long? targetCost, bool targetLegal)
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
