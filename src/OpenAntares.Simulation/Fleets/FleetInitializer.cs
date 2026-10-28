using OpenAntares.Simulation.Random;

namespace OpenAntares.Simulation.Fleets;

public static class FleetInitializer
{
    public static Pcg32 RecoveryRandom(ulong seed, EmpireId owner) => Pcg32.FromSeed(seed ^ 0x4F46464943455201UL, (ulong)owner.Value ^ 0x5245434F56455201UL);
}
