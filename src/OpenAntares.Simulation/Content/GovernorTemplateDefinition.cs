using System.Collections.Immutable;
using OpenAntares.Simulation.Development;
namespace OpenAntares.Simulation.Content;
public sealed record GovernorTemplateDefinition(string Id,string Name,ImmutableArray<string> FoundationIds,ImmutableArray<string> DevelopmentIds,SupportTargetMode SupportTarget,int ProductionWeight,int ResearchWeight);
