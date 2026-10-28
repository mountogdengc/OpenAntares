using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetTravelTests
{
    [Fact]
    public void Mixed300And200AtDistance301TakesTwoTurns()
    {
        var g=FleetFixtures.Nested(); var s=g.State; var f=s.Empires[0].Fleets!.Fleets[0]; var a=s.FindVessel(f.Flagship)!;
        var b=a.Clone();b.Id=new(s.NextEntityId++);b.RoleId="harl_settler";b.Scout=null;b.CarriedPopulation=1;s.Vessels.Add(b);f.Members.Add(b.Id);
        var start=s.FindStar(a.StationaryStarId!.Value)!; var destination=s.Stars.First(x=>x.Id!=start.Id);
        start.X=0;start.Y=0;destination.X=301;destination.Y=0;
        Assert.Equal(2,FleetTravel.Duration(g.Content,s,s.Empires[0].Id,f.Id,destination.Id));
        var moved=FleetCommandTests.Run(g,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(f.Id),destination.Id)); Assert.True(moved.Accepted,moved.Rejection?.Message);
        var candidate=moved.State.Clone(); MovementResolver.Apply(g.Content,moved.State,candidate,new());
        Assert.All(candidate.Vessels.Where(v=>v.EmpireId==s.Empires[0].Id),v=>{Assert.Equal(2,v.Transit!.TotalTurns);Assert.Equal(1,v.Transit.RemainingTurns);});
        Assert.Equal(2,candidate.Empires[0].Fleets!.Fleets[0].Cohort!.TotalTurns);
        Assert.False(FleetCommandTests.Run(new(g.Content,candidate),new FleetAction.Disband(f.Id)).Accepted);
    }
    [Fact]
    public void NavigatorSpeed315And330DoesNotChangeFrozenTransitOrAmebaOdds()
    {
        var g=FleetFixtures.Nested();var s=g.State;var f=s.Empires[0].Fleets!;var group=f.Fleets[0];var officer=f.Officers[0];
        officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=group.Flagship;group.Officer=officer.Id;
        officer.Xp=10;officer.Rank=1;Assert.Equal(315,OfficerBonuses.EffectiveSpeed(g.Content,s,group.Flagship));
        var ship=s.FindVessel(group.Flagship)!;var start=s.FindStar(ship.StationaryStarId!.Value)!;var destination=s.Stars.First(x=>x.Id!=start.Id);start.X=0;start.Y=0;destination.X=640;destination.Y=0;
        ship.PendingMove=destination.Id;var launched=s.Clone();MovementResolver.Apply(g.Content,s,launched,new());
        Assert.Equal(3,launched.FindVessel(ship.Id)!.Transit!.TotalTurns);
        launched.Empires[0].Fleets!.Officers[0].Xp=30;launched.Empires[0].Fleets!.Officers[0].Rank=2;
        var next=launched.Clone();MovementResolver.Apply(g.Content,launched,next,new());Assert.Equal(3,next.FindVessel(ship.Id)!.Transit!.TotalTurns);Assert.Equal(1,next.FindVessel(ship.Id)!.Transit!.RemainingTurns);
        officer.Xp=30;officer.Rank=2;Assert.Equal(330,OfficerBonuses.EffectiveSpeed(g.Content,s,group.Flagship));
        var d=g.Content.SpaceAmeba!; Assert.Equal(Scouting.AmebaOdds.Study(d,300),Scouting.AmebaOdds.Study(d,g.Content.Vessels["scout"].Speed));
    }
    [Fact]
    public void EncounterFingerprintIsStableAndTracksOwnGroupOfficerConsequences()
    {
        var g=FleetFixtures.Game(amebas:true);var s=g.State;var e=s.Empires[0];var ship=s.Vessels[0];
        var created=FleetCommandTests.Run(g,new FleetAction.Create("Survey",ImmutableArray.Create(ship.Id)));Assert.True(created.Accepted);s=created.State;e=s.Empires[0];ship=s.FindVessel(ship.Id)!;
        var fleet=e.Fleets!.Fleets[0];var officer=e.Fleets.Officers[0];fleet.Officer=officer.Id;officer.Status=OfficerStatus.Commanding;officer.PostedColony=null;officer.Flagship=ship.Id;
        var habitat=s.Scouting!.Habitat!;ship.StationaryStarId=habitat.Star;ship.AmebaEncounter=new(){Number=1,HabitatId=habitat.Id,ArrivalTurn=0,Stage=Scouting.AmebaStage.Warning};e.Scouting!.NextEncounterNumber=2;e.Scouting.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));
        Knowledge.ObservationUpdater.Refresh(s,0);FleetFixtures.Valid(new(g.Content,s));
        var token=Assert.Single(Scouting.ScoutQueries.Project(g.Content,s,e.Id).Attention);Assert.NotNull(token.FleetConsequenceFingerprint);
        string before=FleetFixtures.Bytes(s);Assert.Equal(token,Assert.Single(Scouting.ScoutQueries.Project(g.Content,s,e.Id).Attention));Assert.Equal(before,FleetFixtures.Bytes(s));
        officer.Doctrine=OfficerDoctrine.LastWatch;var changed=Assert.Single(Scouting.ScoutQueries.Project(g.Content,s,e.Id).Attention);Assert.NotEqual(token,changed);Assert.Equal(token.Percent,changed.Percent);Assert.Equal(token.Speed,changed.Speed);
        var action=new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(fleet.Id),s.FindPlanet(s.Colonies[0].PlanetId)!.StarId);
        var preview=FleetQueries.PreviewAction(g.Content,Knowledge.EmpireViewProjector.Project(g.Content,s,e.Id),action);Assert.True(preview.Allowed,preview.Rejection);Assert.Single(preview.Routes);
    }
}
