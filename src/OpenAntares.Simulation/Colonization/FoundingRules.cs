using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Species;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Colonization;

public sealed record FoundingEconomyPreview(Quantity SupportProduced, Quantity SupportRequired, Quantity GrowthEarned);
public sealed record FoundingPreview(PlanetId Target, string HabitatMode, int Population, int Capacity, string? RejectionCode)
{
    public FoundingEconomyPreview? Economy { get; init; }
}
public static class FoundingRules
{
    public static FoundingPreview Preview(ContentSet content, GameState state, EmpireId issuer, VesselId vesselId, PlanetId target)
    {
        string mode=HabitatModes.Native;int population=0,capacity=0;
        FoundingPreview Reject(string code) => new(target,mode,population,capacity,code);
        if(!Rules.HasExpansion(state.RulesVersion)) return Reject("expansion_unavailable");
        var vessel=state.FindVessel(vesselId);
        if(vessel is null || vessel.EmpireId != issuer) return Reject(RejectionCodes.NotOwner);
        if(vessel.Transit is not null) return Reject("vessel_in_transit");
        var owner=state.FindEmpire(issuer);
        // The first target-specific check is knowledge. Never use hidden existence/occupancy first.
        var known=owner?.Surveys.SelectMany(o=>o.Planets).FirstOrDefault(p=>p.PlanetId==target);
        if(known is null) return Reject("target_not_known");
        var planet=state.FindPlanet(target);
        if(planet is null) return Reject("target_not_known");
        if(!content.Vessels.TryGetValue(vessel.RoleId,out var role) || !role.CanFoundColony || vessel.CarriedPopulation != role.EmbarkPopulation || vessel.CarriedPopulation < 1 || !SpeciesRules.CanBuildVessel(content,state,issuer,role.Id)) return Reject("cannot_found");
        population=vessel.CarriedPopulation;
        if(Rules.HasSpecies(state.RulesVersion) && role.FoundsSealedHabitat) mode=HabitatModes.Sealed;
        capacity=SpeciesRules.BaseCapacity(content,state,issuer,mode);
        if(vessel.StationaryStarId != planet.StarId) return Reject("wrong_system");
        if(Rules.HasScouting(state.RulesVersion)&&owner!.Scouting!.KnownHabitats.Any(h=>h.Star==planet.StarId))return Reject("ameba_habitat_guarded");
        if(Crises.CrisisQueries.IsHeld(state,vesselId)||Crises.CrisisQueries.PortClosed(state,issuer,planet.StarId))return Reject("population_port_closed");
        var type=content.PlanetTypes[known.PlanetTypeId];
        if(!type.Colonizable || type.SupportPerWorker <= 0 || capacity < population) return Reject("not_colonizable");
        if(Rules.HasSpecies(state.RulesVersion))
        {
            var species=SpeciesRules.Definition(content,state,issuer);
            if(species is null || (mode==HabitatModes.Sealed && !species.AllowsSealedHabitats)) return Reject("habitat_mode_not_available");
            if(known.EnvironmentId is not { } environment || !SpeciesRules.HabitatAllowed(content,state,issuer,environment,mode)) return Reject("habitat_not_suitable");
        }
        // This vessel is present at the target system, so its occupancy is a current permitted fact.
        if(state.Colonies.Any(c=>c.PlanetId==target)) return Reject("target_occupied");
        if(state.Vessels.Any(v=>v.Id!=vesselId && v.EmpireId==issuer && v.PendingFounding==target)) return Reject("target_reserved");
        // An unpublished one-colony calculation frame: no entity allocation or authoritative RNG draw.
        var colony=new ColonyState{Crisis=Rules.HasCrises(state.RulesVersion)?new():null,Id=new(vesselId.Value),PlanetId=target,EmpireId=issuer,Population=population,Workforce=new(population,0,0),HabitatMode=Rules.HasSpecies(state.RulesVersion)?mode:null};
        var frame=new GameState{Crisis=state.Crisis?.Clone(),RulesVersion=state.RulesVersion,Random=state.Random.Clone(),Empires=new(){owner!.Clone()},Planets=new(){new PlanetState{Id=target,StarId=planet.StarId,Orbit=known.Orbit,PlanetTypeId=known.PlanetTypeId,EnvironmentId=known.EnvironmentId}}};
        var economic=EconomyCalculator.CalculateColony(content,frame,colony);
        return new(target,mode,population,capacity,null){Economy=new(economic.SupportProduced,economic.SupportRequired,economic.GrowthEarned)};
    }
    internal static string Message(string code) => code switch
    {
        "target_not_known"=>"Planet is not known to this empire.",
        "cannot_found"=>"This vessel cannot found a colony.",
        "wrong_system"=>"Settler must be stationed at the selected planet's system.",
        "not_colonizable"=>"Planet cannot support this settlement.",
        "habitat_not_suitable"=>"A native colony needs an environment suitable for this species. Use a prepared expedition where available.",
        "habitat_mode_not_available"=>"This habitat mode is not available to the selected species.",
        "target_occupied"=>"Planet is already colonized.",
        "target_reserved"=>"Another settler already has a founding order here.",
        "ameba_habitat_guarded"=>"A space ameba guards this system. This expedition cannot found here.",
        _=>"This vessel cannot found at the selected planet."
    };
}
