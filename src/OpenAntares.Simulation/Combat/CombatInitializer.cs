using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatInitializer
{
    public static string? Initialize(ContentSet content,GameState state,bool enabled)
    {
        if(!Rules.HasCombat(state.RulesVersion))return null;
        if(content.Combat is not {} definition)return "Sentries setup: combat content is required, including Off.";
        var game=new CombatGameState{SentriesEnabled=enabled};
        if(enabled)
        {
            var homes=state.Colonies.Select(c=>state.FindStar(state.FindPlanet(c.PlanetId)!.StarId)!).ToArray();
            var chosen=state.Stars.Where(st=>!homes.Any(h=>h.Id==st.Id)&&state.Scouting?.Habitat?.Star!=st.Id&&state.Planets.Any(p=>p.StarId==st.Id&&Settleable(content,p)))
                .OrderByDescending(st=>homes.Length==0?Int128.Zero:homes.Min(h=>Distance(st,h))).ThenBy(st=>st.Id).FirstOrDefault();
            if(chosen is null)return "Sentries Standard setup: no eligible nonhome, nonameba settleable system. Choose Sentries Off.";
            var pocket=new PocketState{Id=new(game.NextPocketId++),Star=chosen.Id};
            for(int i=0;i<definition.SentriesPerPocket;i++)pocket.Sentries.Add(new(){Id=new(pocket.NextSentryId++),Profile=definition.SentryProfile,Hull=definition.SentryProfile.MaxHull});
            game.Pockets.Add(pocket);
        }
        state.Combat=game;
        foreach(var owner in state.Empires)owner.Combat=new();
        foreach(var vessel in state.Vessels)InitializeVessel(content,state,vessel);
        return null;
    }
    private static Int128 Distance(StarState a,StarState b)
    {
        Int128 x=(Int128)a.X-b.X,y=(Int128)a.Y-b.Y;return checked(x*x+y*y);
    }
    private static bool Settleable(ContentSet content,PlanetState planet)
    {
        if(!content.PlanetTypes.TryGetValue(planet.PlanetTypeId,out var type)||!type.Colonizable||type.SupportPerWorker<=0||planet.EnvironmentId is not {} env)return false;
        return content.Species.Values.Any(sp=>sp.AllowedVesselIds.Any(id=>content.Vessels.TryGetValue(id,out var role)&&role.CanFoundColony&&
            (role.FoundsSealedHabitat?sp.AllowsSealedHabitats&&sp.SealedBaseCapacity>=role.EmbarkPopulation:sp.NaturalEnvironmentIds.Contains(env)&&sp.BaseCapacity>=role.EmbarkPopulation)));
    }
    public static void InitializeVessel(ContentSet content,int rules,VesselState vessel)
    {
        if(Rules.HasShipDesigns(rules))throw new ArgumentException("Rules10 initialization requires the owning state and installed design.");
        if(!Rules.HasCombat(rules))return;
        if(content.Combat is not {} definition||!definition.RoleProfiles.TryGetValue(vessel.RoleId,out var profile))throw new ArgumentException("Combat hull profile missing: "+vessel.RoleId);
        vessel.Combat=new(){Hull=profile.MaxHull,Cohesion=100};
    }
    public static void InitializeVessel(ContentSet content,GameState state,VesselState vessel)
    {
        if(!Rules.HasCombat(state.RulesVersion))return;
        var profile=ShipDesigns.VesselLoadouts.CombatProfile(content,state,vessel);
        vessel.Combat=new(){Hull=profile.MaxHull,Cohesion=100};
    }
}
