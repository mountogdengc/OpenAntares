using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Species;

public static class HabitatModes
{
    public const string Native = "native";
    public const string Sealed = "sealed";
}
public static class SpeciesRules
{
    internal static SpeciesDefinition? Definition(ContentSet content, GameState state, EmpireId owner) =>
        Rules.HasSpecies(state.RulesVersion) && state.FindEmpire(owner)?.SpeciesId is { } id && content.Species.TryGetValue(id, out var profile) ? profile : null;
    public static int BaseCapacity(ContentSet content, GameState state, EmpireId owner, string? habitatMode)
    {
        var sp = Definition(content, state, owner);
        return sp is null ? content.Rules.BaseColonyCapacity : habitatMode == HabitatModes.Sealed ? sp.SealedBaseCapacity ?? 0 : sp.BaseCapacity;
    }
    public static bool CanBuildVessel(ContentSet content, GameState state, EmpireId owner, string roleId) =>
        Rules.VesselAvailable(content, state.RulesVersion, roleId) &&
        (!Rules.HasSpecies(state.RulesVersion) || Rules.HasCombat(state.RulesVersion) && content.Vessels[roleId].CombatOnly || Definition(content,state,owner)?.AllowedVesselIds.Contains(roleId) == true);
    public static bool HabitatAllowed(ContentSet content, GameState state, EmpireId owner, string environmentId, string habitatMode)
    {
        var sp = Definition(content,state,owner);
        return sp is not null && content.Environments.ContainsKey(environmentId) &&
            (habitatMode == HabitatModes.Native ? sp.NaturalEnvironmentIds.Contains(environmentId) : habitatMode == HabitatModes.Sealed && sp.AllowsSealedHabitats);
    }
}
