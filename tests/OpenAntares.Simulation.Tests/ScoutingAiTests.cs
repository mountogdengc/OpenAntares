using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Rivals;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ScoutingAiTests
{
    private static LoadedGame Warning(string species)
    {
        var c=ScoutingFixtures.Content();var result=GalaxyGenerator.CreateNewGame(c,new("small",42,7,"harl",1,species,AmebasEnabled:true));Assert.True(result.Succeeded,result.Error);var s=result.State!;var owner=s.Empires.Single(e=>e.Controller==ControllerKind.Ai);var v=s.Vessels.Single(v=>v.EmpireId==owner.Id);var habitat=s.Scouting!.Habitat!;
        v.StationaryStarId=habitat.Star;v.AmebaEncounter=new(){Number=1,HabitatId=habitat.Id};owner.Scouting!.NextEncounterNumber=2;owner.Scouting.KnownHabitats.Add(new(habitat.Id,habitat.Star,0));ObservationUpdater.Refresh(s,0);ContactUpdater.Refresh(s.Clone(),s);return ScoutingFixtures.Valid(new(c,s));
    }
    [Theory][InlineData("harl")][InlineData("peldra")][InlineData("sulan")][InlineData("wenth")]
    public void AiOptsIntoAvoidAndSafelyWithdrawsThroughBoundedSharedCommands(string species)
    {
        var g=Warning(species);var ai=g.State.Empires.Single(e=>e.Controller==ControllerKind.Ai);var v=g.State.Vessels.Single(v=>v.EmpireId==ai.Id);var commands=new List<Command>();
        var result=RivalTurnPlanner.Prepare(g.Content,g.State,(current,apply)=>ExpansionRivalPlanner.Plan(current,c=>{commands.Add(c);return apply(c);}));Assert.True(result.Succeeded);Assert.Equal(ScoutPosture.AvoidEncounters,result.State.FindVessel(v.Id)!.Scout!.Posture);Assert.NotNull(result.State.FindVessel(v.Id)!.PendingMove);Assert.DoesNotContain(commands,c=>c is SetAmebaRiskOrderCommand or AcknowledgeAmebaCommand);Assert.Contains(commands,c=>c is SetScoutPostureCommand);Assert.InRange(commands.Count,0,2*g.State.Colonies.Count(c=>c.EmpireId==ai.Id)+2*g.State.Vessels.Count(v=>v.EmpireId==ai.Id)+1);Assert.Equal(0,result.State.FindEmpire(ai.Id)!.Scouting!.RiskRollCount);
        var r=ScoutingResolutionTests.Ready(g);Assert.True(r.Accepted,r.Rejection?.Message);Assert.Null(r.State.FindVessel(v.Id)!.AmebaEncounter);Assert.Equal(0,r.State.FindEmpire(ai.Id)!.Scouting!.RiskRollCount);Assert.Equal(ai.Scouting!.RiskRandom!.State,r.State.FindEmpire(ai.Id)!.Scouting!.RiskRandom!.State);
    }
    [Fact] public void UnseenHazardAndForeignPlansCannotChangeAiCommands()
    {
        var c=ScoutingFixtures.Content();var game=GalaxyGenerator.CreateNewGame(c,new("small",42,7,"harl",1,"peldra",AmebasEnabled:true));Assert.True(game.Succeeded,game.Error);var before=game.State!;var after=before.Clone();var old=after.Scouting!.Habitat!;after.Scouting.Habitat=old with{Star=after.Stars.First(star=>star.Id!=old.Star&&!after.Colonies.Any(col=>after.FindPlanet(col.PlanetId)!.StarId==star.Id)).Id};after.Empires[0].Scouting!.KnownHabitats.Add(new(1,after.Scouting.Habitat.Star,0));after.Empires[0].Scouting!.SuccessfulStudies.Add(new(1,0,after.Vessels[0].Id));after.Empires[0].Scouting!.RiskRandom!.NextInt(100);after.Empires[0].Scouting!.RiskRollCount=1;after.Vessels[0].PendingMove=old.Star;Assert.Empty(GameStateValidator.Validate(c,after));
        static string Commands(OpenAntares.Simulation.Content.ContentSet c,GameState s){var commands=new List<string>();var r=RivalTurnPlanner.Prepare(c,s,(current,apply)=>ExpansionRivalPlanner.Plan(current,cmd=>{commands.Add(JsonSerializer.Serialize(cmd,cmd.GetType()));return apply(cmd);}));Assert.True(r.Succeeded);return string.Join("\n",commands);}
        // Mutation of hidden global placement and foreign knowledge is deliberately not a generated campaign.
        // Foreign study state is not consulted by a rival's policy.
        Assert.Equal(Commands(c,before),Commands(c,after));
    }
    [Fact] public void ZeroColonyAiUsesUnknownChartedEmergencyRouteAndNeverStudies()
    {
        var g=Warning("peldra");var s=g.State;var ai=s.Empires.Single(e=>e.Controller==ControllerKind.Ai);var oldColony=s.Colonies.Single(c=>c.EmpireId==ai.Id);var location=CrisisFixtures.Location(s,oldColony);s.Colonies.Remove(oldColony);ai.Crisis!.LostColonyLocations.Add(location);ai.Surveys.RemoveAll(o=>o.StarId!=s.Scouting!.Habitat!.Star);ObservationUpdater.Refresh(s,0);g=ScoutingFixtures.Valid(g);
        var result=RivalTurnPlanner.Prepare(g.Content,s);Assert.True(result.Succeeded);var v=result.State.Vessels.Single(v=>v.EmpireId==ai.Id);Assert.NotNull(v.PendingMove);Assert.NotEqual(s.Scouting!.Habitat!.Star,v.PendingMove);Assert.Null(v.AmebaEncounter!.RiskOrder);Assert.Equal(0,result.State.FindEmpire(ai.Id)!.Scouting!.RiskRollCount);
    }
    [Fact] public void RiskAndAcknowledgementInjectionCannotPassRivalCommandWhitelist()
    {
        var g=Warning("peldra");var ai=g.State.Empires.Single(e=>e.Controller==ControllerKind.Ai);var v=g.State.Vessels.Single(v=>v.EmpireId==ai.Id);
        var r=RivalTurnPlanner.Prepare(g.Content,g.State,(current,apply)=>new(apply(new SetAmebaRiskOrderCommand(ai.Id,0,v.Id,1,1,AmebaRiskAction.Study)),1));Assert.False(r.Succeeded);Assert.Same(g.State,r.State);
    }
    [Fact] public void AlternateScoutCapabilitiesAndMultipleCargoResponsesUseTheSameBoundedPath()
    {
        var g=Warning("peldra");var s=g.State;var ai=s.Empires.Single(e=>e.Controller==ControllerKind.Ai);var original=s.Vessels.Single(v=>v.EmpireId==ai.Id);var role=g.Content.Vessels[original.RoleId] with{Id="probe"};var species=g.Content.Species[ai.SpeciesId!];g=g with{Content=g.Content with{Vessels=g.Content.Vessels.Add(role.Id,role),Species=g.Content.Species.SetItem(species.Id,species with{AllowedVesselIds=species.AllowedVesselIds.Add(role.Id)})}};original.RoleId=role.Id;
        string cargoRole=species.AllowedVesselIds.First(id=>g.Content.Vessels[id].CanFoundColony);var cargoDef=g.Content.Vessels[cargoRole];s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=ai.Id,RoleId=cargoRole,CarriedPopulation=cargoDef.EmbarkPopulation,StationaryStarId=original.StationaryStarId,Crisis=new(),AmebaEncounter=new(){Number=ai.Scouting!.NextEncounterNumber++,HabitatId=1}});s.Vessels.Add(new(){Id=new(s.AllocateId()),EmpireId=ai.Id,RoleId=role.Id,StationaryStarId=original.StationaryStarId,Crisis=new(),Scout=new(),AmebaEncounter=new(){Number=ai.Scouting!.NextEncounterNumber++,HabitatId=1}});g=ScoutingFixtures.Valid(g);
        var commands=new List<Command>();var result=RivalTurnPlanner.Prepare(g.Content,s,(current,apply)=>ExpansionRivalPlanner.Plan(current,c=>{commands.Add(c);return apply(c);}));Assert.True(result.Succeeded);Assert.All(result.State.Vessels.Where(v=>v.EmpireId==ai.Id),v=>Assert.NotNull(v.PendingMove));Assert.InRange(commands.Count,0,2*s.Colonies.Count(c=>c.EmpireId==ai.Id)+2*3+1);Assert.DoesNotContain(commands,c=>c is SetAmebaRiskOrderCommand);
    }
    [Fact] public void LabelledLossLeadsToPaidProductionSelectionWithoutAllocatingAReplacement()
    {
        var g=Warning("peldra");var s=g.State;var ai=s.Empires.Single(e=>e.Controller==ControllerKind.Ai);var v=s.Vessels.Single(v=>v.EmpireId==ai.Id);s.Turn=10;v.AmebaEncounter!.Stage=AmebaStage.Exposed;v.AmebaEncounter.ArrivalTurn=9;ai.Scouting!.KnownHabitats[0]=ai.Scouting.KnownHabitats[0] with{ObservedTurn=10};ai.Scouting.SuccessfulStudies.Add(new(1,10,v.Id));ai.Scouting.RiskRandom!.NextInt(100);ai.Scouting.RiskRollCount=1;
        for(int i=0;i<100000;i++){var probe=ai.Scouting.RiskRandom.Clone();if(probe.NextInt(100)==95)break;ai.Scouting.RiskRandom.NextInt(100);ai.Scouting.RiskRollCount++;}
        Assert.Equal(95,ai.Scouting.RiskRandom.Clone().NextInt(100));ObservationUpdater.Refresh(s,10);g=ScoutingFixtures.Valid(g);var loss=ScoutingResolutionTests.Ready(g);Assert.True(loss.Accepted,loss.Rejection?.Message);Assert.DoesNotContain(loss.State.Vessels,unit=>unit.EmpireId==ai.Id);int nextId=loss.State.NextEntityId;long reserve=loss.State.Colonies.Single(c=>c.EmpireId==ai.Id).ProductionReserve;
        var result=RivalTurnPlanner.Prepare(g.Content,loss.State);Assert.True(result.Succeeded);Assert.Equal(nextId,result.State.NextEntityId);Assert.DoesNotContain(result.State.Vessels,unit=>unit.EmpireId==ai.Id);var colony=result.State.Colonies.Single(c=>c.EmpireId==ai.Id);Assert.Equal(ProductionKinds.Vessel,colony.ProjectKind);Assert.Equal(g.Content.Expansion!.StartingVesselId,colony.ProjectId);Assert.Equal(reserve,colony.ProductionReserve);
    }
}
