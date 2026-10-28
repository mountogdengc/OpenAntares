using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetOfficerTests
{
    internal static CommandOutcome Run(Saves.LoadedGame g,OfficerAction action)=>CommandProcessor.Execute(g.Content,g.State,new OfficerCommand(g.State.Empires[0].Id,g.State.Turn,action));
    internal static ColonyState AddColony(Saves.LoadedGame g)
    {
        var s=g.State;var home=s.Colonies[0];var p=s.FindPlanet(home.PlanetId)!;
        var planet=s.Planets.First(x=>x.StarId!=p.StarId&&x.PlanetTypeId==p.PlanetTypeId&&x.EnvironmentId==p.EnvironmentId&&!s.Colonies.Any(c=>c.PlanetId==x.Id));
        var extra=home.Clone();extra.Id=new(s.NextEntityId++);extra.PlanetId=planet.Id;s.Colonies.Add(extra);Knowledge.ObservationUpdater.Refresh(s,s.Turn);return extra;
    }
    [Fact]
    public void CommissionStoresTurnTwelveDeadlineAndCountsTowardFour()
    {
        var g=FleetFixtures.Game();g.State.Turn=10;Knowledge.ObservationUpdater.Refresh(g.State,10);var col=g.State.Colonies[0].Id;
        for(int i=0;i<3;i++)
        {
            var f=g.State.Empires[0].Fleets!;var result=Run(g,new OfficerAction.Commission(f.CandidateProfiles[0],col));Assert.True(result.Accepted,result.Rejection?.Message);g=new(g.Content,result.State);
            var added=g.State.Empires[0].Fleets!.Officers.Last();Assert.Equal(OfficerStatus.Commissioning,added.Status);Assert.Equal(12,added.AvailableAtTurn);Assert.Equal(0,added.Xp);
        }
        Assert.Equal(4,g.State.Empires[0].Fleets!.LifetimeCommissions);string before=FleetFixtures.Bytes(g.State);
        var rejected=Run(g,new OfficerAction.Commission(g.State.Empires[0].Fleets!.CandidateProfiles[0],col));Assert.False(rejected.Accepted);Assert.Same(g.State,rejected.State);Assert.Equal(before,FleetFixtures.Bytes(g.State));
        Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact]
    public void CannotTeleportOrServeTwoAssignments()
    {
        var g=FleetFixtures.Nested();var distant=AddColony(g);var f=g.State.Empires[0].Fleets!;var officer=f.Officers[0];var fleet=f.Fleets[0];
        Assert.False(Run(g,new OfficerAction.Post(officer.Id,distant.Id)).Accepted);
        var assigned=Run(g,new OfficerAction.AssignFlagship(officer.Id,fleet.Id));Assert.True(assigned.Accepted,assigned.Rejection?.Message);g=new(g.Content,assigned.State);
        Assert.Equal(OfficerStatus.Commanding,g.State.Empires[0].Fleets!.Officers[0].Status);Assert.False(Run(g,new OfficerAction.Remove(officer.Id,distant.Id)).Accepted);
        var duplicate=Run(g,new OfficerAction.AssignFlagship(officer.Id,fleet.Id));Assert.True(duplicate.Accepted);Assert.Same(g.State,duplicate.State);
        var removed=Run(g,new OfficerAction.Remove(officer.Id,g.State.Colonies[0].Id));Assert.True(removed.Accepted,removed.Rejection?.Message);Assert.Null(removed.State.Empires[0].Fleets!.Fleets[0].Officer);
        Assert.Empty(GameStateValidator.Validate(g.Content,removed.State));
    }
    [Fact]
    public void WenthOneAppointmentPerSourceBodyWithoutPopulationCost()
    {
        var g=FleetFixtures.Game("wenth");var home=g.State.Colonies[0];Assert.False(Run(g,new OfficerAction.Commission(null,home.Id,home.Id,OfficerSpecialty.Organizer)).Accepted);
        var other=AddColony(g);var populations=g.State.Colonies.Select(c=>c.Population).ToArray();
        Assert.False(Run(g,new OfficerAction.Commission(null,home.Id,other.Id,OfficerSpecialty.Organizer)).Accepted);
        var result=Run(g,new OfficerAction.Commission(null,other.Id,other.Id,OfficerSpecialty.Organizer));Assert.True(result.Accepted,result.Rejection?.Message);
        var officer=result.State.Empires[0].Fleets!.Officers.Last();Assert.Equal(other.Id,officer.SourceBody);Assert.Contains("Command appointment",officer.Name);Assert.Equal(populations,result.State.Colonies.Select(c=>c.Population));Assert.Empty(result.State.Empires[0].Fleets!.CandidateProfiles);
        Assert.Empty(GameStateValidator.Validate(g.Content,result.State));
    }
    [Fact]
    public void DoctrineFromTurnZeroAndBulkIsAtomic()
    {
        foreach(string species in new[]{"peldra","harl","sulan","wenth"})
        {
            var g=FleetFixtures.Game(species);var f=g.State.Empires[0].Fleets!;var officer=f.Officers[0];Assert.Equal(OfficerDoctrine.RecoverCommand,officer.Doctrine);
            var rejected=Run(g,new OfficerAction.ApplyDoctrineToSelection(ImmutableArray.Create(officer.Id,new OfficerId(999)),OfficerDoctrine.LastWatch));Assert.False(rejected.Accepted);Assert.Equal(OfficerDoctrine.RecoverCommand,officer.Doctrine);
            var changed=Run(g,new OfficerAction.SetDoctrine(officer.Id,OfficerDoctrine.LastWatch));Assert.True(changed.Accepted,changed.Rejection?.Message);g=new(g.Content,changed.State);
            var same=Run(g,new OfficerAction.SetDoctrine(officer.Id,OfficerDoctrine.LastWatch));Assert.True(same.Accepted);Assert.Same(g.State,same.State);Assert.Equal(0,same.State.Empires[0].Fleets!.RecoveryRollCount);
        }
        var mortal=FleetFixtures.Game();var future=Run(mortal,new OfficerAction.SetFutureDoctrine(OfficerDoctrine.LastWatch));Assert.True(future.Accepted);mortal=new(mortal.Content,future.State);Assert.Equal(OfficerDoctrine.RecoverCommand,mortal.State.Empires[0].Fleets!.Officers[0].Doctrine);
        var commission=Run(mortal,new OfficerAction.Commission(mortal.State.Empires[0].Fleets!.CandidateProfiles[0],mortal.State.Colonies[0].Id));Assert.True(commission.Accepted);Assert.Equal(OfficerDoctrine.LastWatch,commission.State.Empires[0].Fleets!.Officers.Last().Doctrine);
    }
    [Fact]
    public void DismissalIsPermanentAndLifetimeCatalogIsBoundedAndUnique()
    {
        var g=FleetFixtures.Game();var col=g.State.Colonies[0].Id;
        for(int i=1;i<64;i++)
        {
            var previous=g.State.Empires[0].Fleets!.Officers.Last();var dismissed=Run(g,new OfficerAction.Dismiss(previous.Id));Assert.True(dismissed.Accepted);g=new(g.Content,dismissed.State);
            Assert.False(Run(g,new OfficerAction.AssignFlagship(previous.Id,new(1))).Accepted);
            var f=g.State.Empires[0].Fleets!;var added=Run(g,new OfficerAction.Commission(f.CandidateProfiles[0],col));Assert.True(added.Accepted,added.Rejection?.Message);g=new(g.Content,added.State);
        }
        var all=g.State.Empires[0].Fleets!.Officers;Assert.Equal(64,all.Select(o=>o.Name).Distinct().Count());Assert.All(all.Take(63),o=>Assert.NotNull(o.FinalSummary));
        var retire=Run(g,new OfficerAction.Dismiss(all.Last().Id));Assert.True(retire.Accepted);g=new(g.Content,retire.State);
        Assert.False(Run(g,new OfficerAction.Commission(g.State.Empires[0].Fleets!.CandidateProfiles[0],col)).Accepted);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
    }
    [Fact]
    public void TransferAndCoordinatorRemovalKeepOnePhysicalAssignment()
    {
        var g=FleetFixtures.Nested();var s=g.State;var f=s.Empires[0].Fleets!;var officer=f.Officers[0];var request=f.Requests[0];
        officer.Status=OfficerStatus.Coordinating;officer.Request=request.Id;request.Coordinator=officer.Id;request.InitialXpRecipient=officer.Id;
        FleetFixtures.Valid(g);Assert.False(Run(g,new OfficerAction.AssignFlagship(officer.Id,f.Fleets[0].Id)).Accepted);
        var remove=Run(g,new OfficerAction.Remove(officer.Id,s.Colonies[0].Id));Assert.True(remove.Accepted,remove.Rejection?.Message);g=new(g.Content,remove.State);f=g.State.Empires[0].Fleets!;Assert.True(f.Requests[0].CoordinatorXpForfeited);
        var ship=g.State.Vessels.First(v=>v.EmpireId==g.State.Empires[0].Id).Clone();ship.Id=new(g.State.NextEntityId++);ship.Fleet=new(2);g.State.Vessels.Add(ship);f.Fleets.Add(new(){Id=new(2),Name="Second",Members=new(){ship.Id},Flagship=ship.Id});f.NextFleetId=3;
        var board=Run(g,new OfficerAction.AssignFlagship(officer.Id,new(1)));Assert.True(board.Accepted);g=new(g.Content,board.State);
        var transfer=Run(g,new OfficerAction.Transfer(officer.Id,g.State.Empires[0].Fleets!.Fleets[0].Flagship,ship.Id));Assert.True(transfer.Accepted,transfer.Rejection?.Message);
        Assert.Null(transfer.State.Empires[0].Fleets!.Fleets[0].Officer);Assert.Equal(officer.Id,transfer.State.Empires[0].Fleets!.Fleets[1].Officer);Assert.Empty(GameStateValidator.Validate(g.Content,transfer.State));
    }
    [Fact]
    public void ActualDoctrineChangeInvalidatesReceiptWithoutAcknowledging()
    {
        var g=FleetFixtures.Game(amebas:true);var ship=g.State.Vessels[0];var create=FleetCommandTests.Run(g,new FleetAction.Create("Command",ImmutableArray.Create(ship.Id)));Assert.True(create.Accepted);g=new(g.Content,create.State);
        var officer=g.State.Empires[0].Fleets!.Officers[0];var board=Run(g,new OfficerAction.AssignFlagship(officer.Id,new(1)));Assert.True(board.Accepted);g=new(g.Content,board.State);
        var s=g.State;var e=s.Empires[0];ship=s.FindVessel(ship.Id)!;var habitat=s.Scouting!.Habitat!;ship.StationaryStarId=habitat.Star;ship.AmebaEncounter=new(){Number=1,HabitatId=habitat.Id,ArrivalTurn=0,Stage=Scouting.AmebaStage.Warning};e.Scouting!.NextEncounterNumber=2;e.Scouting.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));Knowledge.ObservationUpdater.Refresh(s,0);FleetFixtures.Valid(g);
        var token=Assert.Single(Scouting.ScoutQueries.Project(g.Content,s,e.Id).Attention);
        var ack=CommandProcessor.Execute(g.Content,s,new AcknowledgeAmebaCommand(e.Id,s.Turn,ImmutableArray.Create(token)));Assert.True(ack.Accepted,ack.Rejection?.Message);g=new(g.Content,ack.State);
        var unchanged=Run(g,new OfficerAction.SetDoctrine(officer.Id,OfficerDoctrine.RecoverCommand));Assert.True(unchanged.Accepted);Assert.Same(g.State,unchanged.State);Assert.Single(unchanged.State.Empires[0].Scouting!.Receipts);
        var changed=Run(g,new OfficerAction.SetDoctrine(officer.Id,OfficerDoctrine.LastWatch));Assert.True(changed.Accepted,changed.Rejection?.Message);Assert.Empty(changed.State.Empires[0].Scouting!.Receipts);
        var newToken=Assert.Single(Scouting.ScoutQueries.Project(g.Content,changed.State,e.Id).Attention);Assert.True(newToken.Revision>token.Revision);Assert.NotEqual(token.FleetConsequenceFingerprint,newToken.FleetConsequenceFingerprint);Assert.Equal(token.Percent,newToken.Percent);Assert.Equal(0,changed.State.Empires[0].Fleets!.RecoveryRollCount);
    }
}
