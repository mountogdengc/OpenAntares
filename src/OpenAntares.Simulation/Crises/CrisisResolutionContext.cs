using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public sealed record CrisisEvent(CrisisKey Key,int Turn,CrisisLocation Location,string Kind,long Revision,string Reason,long ResearchDiverted=0,int PopulationLost=0)
{
    public bool Material { get; init; }
    public string Disposition { get; init; }="recorded";
    public long CureCost { get; init; }
    public long CureProgress { get; init; }
    public long DiscardedProgress { get; init; }
}
public sealed record CrisisExposure(EmpireId Owner,long Episode,ColonyId Destination,VesselId SourceVessel,CrisisLocation Source);
public sealed record CrisisDepartureSource(EmpireId Owner,long Episode,CrisisLocation Location);

/// <summary>One private resolution's frozen port/source facts and collected effects. Never saved.</summary>
public sealed class CrisisResolutionContext
{
    public int ToTurn { get; private init; }
    public ImmutableDictionary<ColonyId,CrisisLocation> Locations { get; private init; }=ImmutableDictionary<ColonyId,CrisisLocation>.Empty;
    public ImmutableHashSet<(EmpireId Owner,StarId Star)> ClosedPorts { get; private init; }=ImmutableHashSet<(EmpireId,StarId)>.Empty;
    public ImmutableDictionary<(EmpireId Owner,StarId Star),ImmutableArray<CrisisDepartureSource>> DepartureSources { get; private init; }=ImmutableDictionary<(EmpireId,StarId),ImmutableArray<CrisisDepartureSource>>.Empty;
    public HashSet<(EmpireId,long)> CuredEpisodes { get; }=new();
    public HashSet<VesselId> SterileVessels { get; }=new();
    public List<VesselId> ArrivedVessels { get; }=new();
    public List<CrisisExposure> Exposures { get; }=new();
    public List<CrisisEvent> Events { get; }=new();
    public static CrisisResolutionContext Capture(ContentSet content,GameState snapshot)
    {
        bool enabled=Rules.HasCrises(snapshot.RulesVersion)&&snapshot.Crisis is{Enabled:true};
        return new()
        {
            ToTurn=checked(snapshot.Turn+1),
            Locations=snapshot.Colonies.ToImmutableDictionary(c=>c.Id,c=>CrisisQueries.Location(snapshot,c)),
            ClosedPorts=enabled?snapshot.Colonies.Where(c=>c.Crisis!.Quarantined).Select(c=>(c.EmpireId,snapshot.FindPlanet(c.PlanetId)!.StarId)).ToImmutableHashSet():ImmutableHashSet<(EmpireId,StarId)>.Empty,
            DepartureSources=enabled?snapshot.Colonies.Where(c=>c.Crisis!.Case?.Stage==CrisisStage.Active&&!c.Crisis.Quarantined).OrderBy(c=>c.Id).GroupBy(c=>(c.EmpireId,snapshot.FindPlanet(c.PlanetId)!.StarId)).ToImmutableDictionary(g=>g.Key,g=>g.Select(c=>new CrisisDepartureSource(c.EmpireId,c.Crisis!.Case!.EpisodeId,CrisisQueries.Location(snapshot,c))).ToImmutableArray()):ImmutableDictionary<(EmpireId,StarId),ImmutableArray<CrisisDepartureSource>>.Empty
        };
    }
}
