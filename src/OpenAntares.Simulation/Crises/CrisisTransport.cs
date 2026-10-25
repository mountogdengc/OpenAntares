using System;
using System.Globalization;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Crises;
public static class CrisisTransport
{
    public static void TagCompletion(ContentSet content,GameState snapshot,GameState candidate,CrisisResolutionContext context,ColonyId source,VesselState vessel)
    {
        if(!Rules.HasCrises(candidate.RulesVersion))return;vessel.Crisis=new();
        if(!Enabled(candidate)||vessel.CarriedPopulation<=0||snapshot.FindColony(source) is not {} colony||colony.EmpireId!=vessel.EmpireId||colony.Crisis?.Case is not{Stage:CrisisStage.Active} illness)return;
        var location=context.Locations[source];
        if(context.ClosedPorts.Contains((colony.EmpireId,location.Star)))return;
        Tag(candidate,context,vessel,new(colony.EmpireId,illness.EpisodeId,location),"Cargo embarked at an infectious port.");
    }
    public static void PrepareDepartures(ContentSet content,GameState snapshot,GameState candidate,CrisisResolutionContext context)
    {
        if(!Enabled(snapshot))return;
        foreach(var before in snapshot.Vessels.OrderBy(v=>v.Id))
        {
            var vessel=candidate.FindVessel(before.Id);if(vessel is null||before.CarriedPopulation<=0)continue;
            if(before.Crisis?.InspectionDueTurn is {} due&&due<=context.ToTurn)
            {
                vessel.Crisis!.CarrierEpisode=null;vessel.Crisis.InspectionDueTurn=null;context.SterileVessels.Add(vessel.Id);
                PortEvent(snapshot,candidate,context,vessel,"inspection_completed","Cargo inspection completed; this boundary remained sterile.");continue;
            }
            if(before.StationaryStarId is not {} star||CrisisQueries.IsHeld(snapshot,before.Id)||context.ClosedPorts.Contains((before.EmpireId,star))||context.SterileVessels.Contains(vessel.Id))continue;
            if(context.DepartureSources.TryGetValue((before.EmpireId,star),out var sources)&&sources.Length>0)Tag(candidate,context,vessel,sources[0],"Cargo contacted an infectious open port.");
        }
    }
    public static void HandleArrivals(ContentSet content,GameState snapshot,GameState candidate,CrisisResolutionContext context)
    {
        if(!Enabled(snapshot))return;
        foreach(var id in context.ArrivedVessels.Distinct().OrderBy(v=>v))
        {
            var vessel=candidate.FindVessel(id);if(vessel is null||vessel.CarriedPopulation<=0||vessel.StationaryStarId is not {} star)continue;
            if(context.ClosedPorts.Contains((vessel.EmpireId,star)))
            {
                vessel.Crisis!.InspectionDueTurn=checked(context.ToTurn+1);
                PortEvent(snapshot,candidate,context,vessel,"cargo_isolated","Cargo arrived into quarantine; one full further resolution is required for inspection.");continue;
            }
            if(context.SterileVessels.Contains(id)||vessel.Crisis?.InspectionDueTurn is not null)continue;
            var source=candidate.Colonies.Where(c=>c.EmpireId==vessel.EmpireId&&candidate.FindPlanet(c.PlanetId)?.StarId==star&&c.Crisis?.Case?.Stage==CrisisStage.Active&&!c.Crisis.Quarantined).OrderBy(c=>c.Id).FirstOrDefault();
            if(source is not null)Tag(candidate,context,vessel,new(source.EmpireId,source.Crisis!.Case!.EpisodeId,CrisisQueries.Location(candidate,source)),"Arriving cargo contacted an infectious open port.");
            if(vessel.Crisis?.CarrierEpisode is not {} episode||context.CuredEpisodes.Contains((vessel.EmpireId,episode))||candidate.FindEmpire(vessel.EmpireId)?.Crisis?.Episode is not {} active||active.Id!=episode)continue;
            foreach(var colony in candidate.Colonies.Where(c=>c.EmpireId==vessel.EmpireId&&candidate.FindPlanet(c.PlanetId)?.StarId==star).OrderBy(c=>c.Id))
                context.Exposures.Add(new(vessel.EmpireId,episode,colony.Id,vessel.Id,active.Source));
        }
    }
    public static void TransferFounding(ContentSet content,GameState snapshot,GameState candidate,CrisisResolutionContext context,VesselState consumed,ColonyState founded)
    {
        if(!Rules.HasCrises(candidate.RulesVersion))return;founded.Crisis??=new();
        if(!Enabled(snapshot)||consumed.CarriedPopulation<=0||context.SterileVessels.Contains(consumed.Id)||consumed.Crisis?.CarrierEpisode is not {} episode||context.CuredEpisodes.Contains((consumed.EmpireId,episode))||candidate.FindEmpire(consumed.EmpireId)?.Crisis?.Episode is not {} active||active.Id!=episode)return;
        context.Exposures.Add(new(consumed.EmpireId,episode,founded.Id,consumed.Id,active.Source));
    }
    public static void ApplyExposures(ContentSet content,GameState snapshot,GameState candidate,CrisisResolutionContext context)
    {
        if(!Enabled(snapshot))return;var definition=content.Plagues.Values.Single();
        foreach(var exposure in context.Exposures.OrderBy(x=>x.Destination).ThenBy(x=>x.SourceVessel).GroupBy(x=>(x.Owner,x.Episode,x.Destination)).Select(g=>g.First()))
        {
            if(context.CuredEpisodes.Contains((exposure.Owner,exposure.Episode))||candidate.FindEmpire(exposure.Owner)?.Crisis?.Episode?.Id!=exposure.Episode||candidate.FindColony(exposure.Destination) is not {} colony||colony.EmpireId!=exposure.Owner||colony.Crisis is not {} health||health.Case is not null||health.ImmunityFamilyId==definition.Id&&context.ToTurn<health.ImmuneUntilTurn)continue;
            health.ImmunityFamilyId=null;health.ImmuneUntilTurn=0;health.PolicyPaused=false;
            health.Case=new(){EpisodeId=exposure.Episode,Stage=CrisisStage.Warning,GraceRemaining=definition.GraceTurns,AttentionRevision=1,Origin=exposure.Source,SourceVessel=exposure.SourceVessel};
            context.Events.Add(new(new(exposure.Owner,exposure.Episode,colony.Id),context.ToTurn,CrisisQueries.Location(candidate,colony),"exposure",0,"Cargo "+exposure.SourceVessel+" carried the episode originating at "+exposure.Source.Name+"; colony entered Warning for "+definition.GraceTurns.ToString(CultureInfo.InvariantCulture)+" safe intervals."){Material=true});
        }
    }
    private static bool Enabled(GameState state)=>Rules.HasCrises(state.RulesVersion)&&state.Crisis is{Enabled:true};
    private static void Tag(GameState candidate,CrisisResolutionContext context,VesselState vessel,CrisisDepartureSource source,string reason)
    {
        if(vessel.Crisis!.CarrierEpisode is not null||context.SterileVessels.Contains(vessel.Id)||context.CuredEpisodes.Contains((source.Owner,source.Episode))||candidate.FindEmpire(source.Owner)?.Crisis?.Episode?.Id!=source.Episode)return;
        vessel.Crisis.CarrierEpisode=source.Episode;
        context.Events.Add(new(new(source.Owner,source.Episode,source.Location.Colony),context.ToTurn,source.Location,"cargo_infectious",0,vessel.Id+": "+reason));
    }
    private static void PortEvent(GameState snapshot,GameState candidate,CrisisResolutionContext context,VesselState vessel,string kind,string reason)
    {
        var e=snapshot.FindEmpire(vessel.EmpireId)!.Crisis!;long episode=vessel.Crisis?.CarrierEpisode??e.Episode?.Id??(e.NextEpisodeNumber-1);
        if(episode<1)return;
        CrisisLocation? location=candidate.Colonies.Where(c=>c.EmpireId==vessel.EmpireId&&candidate.FindPlanet(c.PlanetId)?.StarId==vessel.StationaryStarId).OrderBy(c=>c.Id).Select(c=>CrisisQueries.Location(candidate,c)).FirstOrDefault()
            ??candidate.FindEmpire(vessel.EmpireId)!.Crisis!.LostColonyLocations.FirstOrDefault(l=>l.Star==vessel.StationaryStarId)??e.Episode?.Source;
        if(location is null)return;
        context.Events.Add(new(new(vessel.EmpireId,episode,location.Colony),context.ToTurn,location,kind,0,vessel.Id+": "+reason));
    }
}
