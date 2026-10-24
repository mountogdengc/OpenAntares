using System;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Species;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Economy;

/// <summary>One owner's economic parameters, independent of presentation and random state.</summary>
public sealed record EconomyProfile(long SupportPerPopulation, long SupportPerGrowthPoint,
    int ProductionBasisPoints, int ResearchBasisPoints, int BaseGrowth, int MaxSurplusGrowth,
    string? SpeciesId, string PopulationUnitLabel)
{
    public static EconomyProfile For(ContentSet content, GameState state, EmpireId owner)
    {
        if (state.RulesVersion != Rules.SpeciesVersion)
            return new(content.Rules.SupportPerPopulation, EconomyCalculator.SupportPerGrowthPoint, 10000, 10000,
                content.Rules.BaseGrowth, content.Rules.MaxSurplusGrowth, null, "population");
        var sp = SpeciesRules.Definition(content,state,owner) ?? throw new InvalidOperationException("Species rules require a valid owner species.");
        return new(sp.SupportPerPopulation,sp.SupportPerGrowthPoint,sp.ProductionBasisPoints,sp.ResearchBasisPoints,
            sp.BaseGrowth,sp.MaxSurplusGrowth,sp.Id,sp.PopulationUnitLabel);
    }
}
