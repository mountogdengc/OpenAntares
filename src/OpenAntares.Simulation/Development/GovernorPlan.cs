using System.Collections.Immutable;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Development;
public sealed record GovernorColonyOrder(ColonyId Colony,ProductionTarget? Target,Workforce Workforce,int? QueueToken,ProductionOrigin Origin,ImmutableArray<GovernorIssue> Issues);
public sealed record GovernorPlan(bool Succeeded,ImmutableArray<GovernorColonyOrder> Orders,string? Error);
