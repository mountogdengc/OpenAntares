namespace OpenAntares.Simulation.Diplomacy;
public sealed record DiplomacyDefinition
{
    public required string AlgorithmId {get;init;}
    public required long SetupCost {get;init;}
    public required long TradeOutput {get;init;}
    public required int Warmup {get;init;}
    public required int Minimum {get;init;}
    public required int Notice {get;init;}
    public required int OfferLifetime {get;init;}
    public required int Cooldown {get;init;}
    public required int StreakCap {get;init;}
    public required int StreakStep {get;init;}
    public required int CancellationPenalty {get;init;}
    public required int MemoryLifetime {get;init;}
    public required int AccordThreshold {get;init;}
    public required int TradeThreshold {get;init;}
}
