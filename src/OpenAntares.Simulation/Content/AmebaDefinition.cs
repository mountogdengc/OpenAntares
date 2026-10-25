namespace OpenAntares.Simulation.Content;

public sealed record ProbabilityRule(int BasePercent,int RelativeSpeedCoefficient,int MinimumPercent,int MaximumPercent);
public sealed record AmebaDefinition(string Id,string Name,int PursuitSpeed,ProbabilityRule LateEscape,ProbabilityRule Study,long ResearchReward);
