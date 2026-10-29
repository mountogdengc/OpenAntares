using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetSaveTests
{
    [Fact]
    public void LostLastShipyardCanStillCancelAndRetainItsHistoricalRequest()
    {
        var g=FleetEconomyTests.Organizer(0,crises:true);FleetRecoveryTests.FatalPlague(g,g.State.Colonies[0].Id);g=Advance(g);var request=g.State.Empires[0].Fleets!.Requests[0];Assert.Empty(request.Colonies);var cancel=FleetRequestTests.Run(g,new AssemblyAction.Cancel(request.Id,request.Revision,RequestCancellation.KeepQueue));Assert.True(cancel.Accepted,cancel.Rejection?.Message);var loaded=SaveGame.Deserialize(SaveGame.Serialize(g.Content,cancel.State));Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.Equal(RequestStatus.Cancelled,loaded.Game!.State.Empires[0].Fleets!.Requests[0].Status);
    }
    [Fact]
    public void StrictFieldsEnumsDuplicatesReferencesTimersAndCreditsRejectCleanly()
    {
        var fixture=FleetFixtures.Nested();fixture.State.Empires[0].Fleets!.Requests[0].RallyFleet=new(1);fixture.State.Empires[0].Fleets!.Requests[0].ReservedFleetSlot=false;string text=SaveGame.Serialize(fixture.Content,fixture.State);
        foreach(var change in new Action<JsonNode>[]
        {
            n=>n["requests"]![0]!.AsObject().Remove("priority"),
            n=>n["requests"]![0]!["priority"]=1,
            n=>n["requests"]![0]!["priority"]="Urgent",
            n=>n["requests"]![0]!["slots"]![0]!["queue_token"]=999,
            n=>n["requests"]![0]!["next_slot_ordinal"]=1,
            n=>n["recovery_algorithm_version"]=2,
            n=>n["recovery_random"]!["increment"]=2,
            n=>n["officers"]![0]!["xp"]=31,
            n=>n["officers"]![0]!["rank"]=2,
            n=>n["officers"]![0]!["available_at_turn"]=1,
            n=>n["next_fleet_id"]=1,
            n=>n["fleets"]![0]!["members"]!.AsArray().Add(n["fleets"]![0]!["members"]![0]!.DeepClone()),
            n=>n["fleets"]!.AsArray().Add(n["fleets"]![0]!.DeepClone()),
            n=>n["officers"]![0]!["source_body"]=1,
            n=>n["fleets"]![0]!["flagship"]=9999,
            n=>n["survey_credits"]!.AsArray().Add(n["survey_credits"]![0]!.DeepClone()),
            n=>n["surprise"]=true,
            n=>n["officers"]![0]!.AsObject().Remove("career"),
        })
        {
            var root=JsonNode.Parse(text)!;change(root["state"]!["empires"]![0]!["fleet_command"]!);var loaded=SaveGame.Deserialize(root.ToJsonString());Assert.False(loaded.Succeeded);Assert.Null(loaded.Game);
        }
        var duplicate=text.Replace("\"next_fleet_id\": 2","\"next_fleet_id\": 2, \"next_fleet_id\": 2",StringComparison.Ordinal);Assert.NotEqual(text,duplicate);Assert.False(SaveGame.Deserialize(duplicate).Succeeded);
    }
    [Fact]
    public void InvalidLiveConsentFleetReferenceRejectsRatherThanThrowing()
    {
        var g=FleetRecoveryTests.Loss();var root=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;root["state"]!["empires"]![0]!["fleet_command"]!["fleets"]![0]!["flagship"]=99999;Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);
    }
    [Fact]
    public void DuplicateEmbeddedCommandFieldIsRejected()
    {
        var g=FleetFixtures.Game();string text=SaveGame.Serialize(g.Content,g.State);string changed=text.Replace("\"member_limit\": 32","\"member_limit\": 32, \"member_limit\": 32",StringComparison.Ordinal);Assert.NotEqual(text,changed);Assert.False(SaveGame.Deserialize(changed).Succeeded);
    }
    [Fact]
    public void InvalidRecoveryOriginAndCommittedSpeedAreRejected()
    {
        var recovered=Advance(FleetRecoveryTests.Loss());string text=SaveGame.Serialize(recovered.Content,recovered.State);
        foreach(var change in new Action<JsonNode>[] {n=>n["recovery_started_turn"]=-1,n=>n["recovery_started_turn"]=null,n=>n["recovery_percent_at_loss"]=51,n=>n["available_at_turn"]=10000})
        {var root=JsonNode.Parse(text)!;change(root["state"]!["empires"]![0]!["fleet_command"]!["officers"]![0]!);Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded);}
        var g=FleetRequestTests.Accept(FleetFixtures.Game(),FleetPlanningTests.Draft(FleetFixtures.Game(),1));g.State.Colonies[0].ProductionReserve=800;g=Advance(g);var built=g.State.Vessels.Single(v=>v.BuildOrigin is not null);var corrupt=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;corrupt["state"]!["vessels"]!.AsArray().Single(v=>v!["id"]!.GetValue<int>()==built.Id.Value)!["build_origin"]!["colony"]=built.Id.Value;Assert.False(SaveGame.Deserialize(corrupt.ToJsonString()).Succeeded);
        g=FleetProgressionTests.Surveyor();FleetProgressionTests.Arriving(g);corrupt=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;corrupt["state"]!["empires"]![0]!["fleet_command"]!["fleets"]![0]!["cohort"]!["effective_speed"]=1;Assert.False(SaveGame.Deserialize(corrupt.ToJsonString()).Succeeded);
    }
    internal static LoadedGame Advance(LoadedGame g)
    {var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));return new(g.Content,result.State);}
    internal static IEnumerable<LoadedGame> Cases()
    {
        yield return FleetFixtures.Game();yield return FleetFixtures.Game("wenth");yield return FleetFixtures.Valid(FleetFixtures.Nested());
        var wenth=WenthFounding();yield return wenth;yield return Advance(wenth);
        var multi=FleetReadyTests.Explore(FleetReadyTests.Multi());var ready=CommandProcessor.Execute(multi.Content,multi.State,new SetReadyCommand(multi.State.Empires[0].Id,0,true));Assert.True(ready.Accepted);yield return new(multi.Content,ready.State);
        var g=FleetProgressionTests.Surveyor(8);FleetProgressionTests.Arriving(g);yield return g;yield return Advance(g);
        g=FleetReadyTests.Explore(FleetFixtures.Game());var target=g.State.Stars.First(s=>s.Id!=g.State.Vessels[0].StationaryStarId).Id;
        var moved=FleetCommandTests.Run(g,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(new FleetId(1)),target));Assert.True(moved.Accepted);g=new(g.Content,moved.State);yield return g;yield return Advance(g);
        g=FleetRequestTests.Accept(FleetFixtures.Game(),FleetPlanningTests.Draft(FleetFixtures.Game(),2) with{Priority=RequestPriority.Urgent});g.State.Colonies[0].Workforce=new(g.State.Colonies[0].Population,0,0);g.State.Colonies[0].ProductionReserve=800;g=Advance(g);Assert.Contains(g.State.Empires[0].Fleets!.Requests[0].Slots,s=>s.Status==SlotStatus.Built);yield return g;g=Advance(g);Assert.Contains(g.State.Empires[0].Fleets!.Requests[0].Slots,s=>s.Status==SlotStatus.Arrived);Assert.Contains(g.State.Empires[0].Fleets!.Requests[0].Slots,s=>s.Status==SlotStatus.Queued);yield return g;
        var request=g.State.Empires[0].Fleets!.Requests[0];var detached=FleetRequestTests.Run(g,new AssemblyAction.DetachSlot(request.Id,request.Revision,request.Slots.First().Ordinal));Assert.True(detached.Accepted,detached.Rejection?.Message);yield return new(g.Content,detached.State);
        g=FleetFixtures.Game(amebas:true);var h=g.State.Scouting!.Habitat!;g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Rally=h.Star});g.State.Empires[0].Scouting!.KnownHabitats.Add(new(h.Id,h.Star,0));var safety=FleetTurnCoordinator.PrepareRequests(g.Content,g.State,g.State.Empires[0].Id);Assert.True(safety.Succeeded);yield return new(g.Content,safety.State);
        g=FleetFixtures.Game();var commission=FleetOfficerTests.Run(g,new OfficerAction.Commission(g.State.Empires[0].Fleets!.CandidateProfiles[0],g.State.Colonies[0].Id));Assert.True(commission.Accepted);yield return new(g.Content,commission.State);
        yield return FleetRecoveryTests.Loss();yield return Advance(FleetRecoveryTests.Loss());yield return Advance(FleetRecoveryTests.Loss(50));yield return Advance(FleetRecoveryTests.Loss(species:"wenth",doctrine:OfficerDoctrine.LastWatch));
        g=FleetRecoveryTests.Loss(crises:true);FleetRecoveryTests.FatalPlague(g,g.State.Colonies[0].Id);yield return Advance(g);
    }
    internal static LoadedGame WenthFounding()
    {
        var g=FleetFixtures.Game("wenth");var s=g.State;var ship=s.Vessels[0];ship.RoleId=g.Content.Vessels.Values.First(v=>v.CanFoundColony&&g.Content.Species["wenth"].AllowedVesselIds.Contains(v.Id)).Id;ship.CarriedPopulation=g.Content.Vessels[ship.RoleId].EmbarkPopulation;ship.Scout=null;
        var target=s.Planets.First(p=>!s.Colonies.Any(c=>c.PlanetId==p.Id)&&g.Content.PlanetTypes[p.PlanetTypeId].Colonizable&&g.Content.PlanetTypes[p.PlanetTypeId].SupportPerWorker>0&&Species.SpeciesRules.HabitatAllowed(g.Content,s,s.Empires[0].Id,p.EnvironmentId!,g.Content.Vessels[ship.RoleId].FoundsSealedHabitat?Species.HabitatModes.Sealed:Species.HabitatModes.Native));
        var made=FleetCommandTests.Run(g,new FleetAction.Create("Founding appointment",ImmutableArray.Create(ship.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);var board=FleetOfficerTests.Run(g,new OfficerAction.AssignFlagship(new(1),new(1)));Assert.True(board.Accepted);g=new(g.Content,board.State);
        if(target.StarId!=ship.StationaryStarId)
        {var move=FleetCommandTests.Run(g,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(new FleetId(1)),target.StarId));Assert.True(move.Accepted,move.Rejection?.Message);g=new(g.Content,move.State);for(int i=0;i<100&&g.State.FindVessel(ship.Id)!.StationaryStarId!=target.StarId;i++)g=Advance(g);}
        var founding=CommandProcessor.Execute(g.Content,g.State,new SetFoundingOrderCommand(g.State.Empires[0].Id,g.State.Turn,ship.Id,target.Id));Assert.True(founding.Accepted,founding.Rejection?.Message);return new(g.Content,founding.State);
    }
    [Fact]
    public void ExactThirtySevenPairsAndNoEightEight()
    {
        string[] old={"legacy-v2-save.json","legacy-rules2-v3-save.json","legacy-rules3-v4-save.json","legacy-rules4-v5-save.json","legacy-rules5-v6-save.json"};int accepted=0;
        for(int schema=1;schema<=9;schema++)for(int rules=1;rules<=9;rules++)
        {
            string text=rules==8?SaveGame.Serialize(FleetContentTests.Core(),FleetFixtures.Game().State):rules==7?SaveGame.Serialize(ScoutingFixtures.Content(),ScoutingFixtures.Game().State):rules==6?SaveGame.Serialize(CrisisFixtures.Content(),CrisisFixtures.Game().State):File.ReadAllText(CompatibilityFixtures.PathFor(old[Math.Min(rules,5)-1]));
            var root=JsonNode.Parse(text)!;root["schema_version"]=schema;root["rules_version"]=rules;if(schema==1&&rules==1)root=JsonNode.Parse(File.ReadAllText(CompatibilityFixtures.PathFor("legacy-v1-save.json")))!;
            bool expected=(schema,rules) is (1,1) or (2,1)||schema is >=3 and <=8&&rules>=1&&rules<=schema-1||schema==9&&rules is >=1 and <=8;
            var loaded=SaveGame.Deserialize(root.ToJsonString());Assert.True(expected==loaded.Succeeded,$"({schema},{rules}) expected {expected}: {string.Join(";",loaded.Errors)}");if(loaded.Succeeded)accepted++;
        }
        Assert.Equal(37,accepted);Assert.Equal(10,SaveGame.SchemaVersion);Assert.Equal(8,Rules.CurrentVersion);
    }
    [Fact]
    public void EveryPendingPhaseRoundTripsToIdenticalFuture()
    {
        foreach(var source in Cases())
        {
            var g=FleetFixtures.Valid(source);string text=SaveGame.Serialize(g.Content,g.State);var loaded=SaveGame.Deserialize(text);Assert.True(loaded.Succeeded,string.Join(";",loaded.Errors));Assert.Equal(text,SaveGame.Serialize(loaded.Game!.Content,loaded.Game.State));Assert.Equal(FleetFixtures.Bytes(g.State),FleetFixtures.Bytes(loaded.Game.State));
            var direct=g;var restored=loaded.Game;for(int i=0;i<4;i++)
            {
                // A saved live encounter retains exact explicit consent; later fresh warnings get
                // the same ordinary explicit departure acknowledgement in both continuations.
                direct=Consent(direct);restored=Consent(restored);direct=Advance(direct);restored=Advance(restored);Assert.Equal(SaveGame.Serialize(direct.Content,direct.State),SaveGame.Serialize(restored.Content,restored.State));
            }
        }
    }
    internal static LoadedGame Consent(LoadedGame g)
    {
        foreach(var owner in g.State.Empires.Where(e=>e.Controller==ControllerKind.Human))
        {var tokens=Scouting.ScoutQueries.Project(g.Content,g.State,owner.Id).Attention;if(tokens.IsEmpty)continue;var ack=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(owner.Id,g.State.Turn,tokens));Assert.True(ack.Accepted,ack.Rejection?.Message);g=new(g.Content,ack.State);}
        return g;
    }
    [Fact]
    public void LegacyFleetInjectionRejected()
    {
        var old=ScoutingFixtures.Game();var template=JsonNode.Parse(SaveGame.Serialize(FleetContentTests.Core(),FleetFixtures.Game().State))!;
        foreach(string field in new[]{"fleet_command","fleet","newly_completed","build_origin"})
        {
            var root=JsonNode.Parse(SaveGame.Serialize(old.Content,old.State))!;
            if(field=="fleet_command")root["state"]!["empires"]![0]![field]=template["state"]!["empires"]![0]![field]?.DeepClone();else root["state"]!["vessels"]![0]![field]=template["state"]!["vessels"]![0]![field]?.DeepClone();
            Assert.False(SaveGame.Deserialize(root.ToJsonString()).Succeeded,field);
        }
    }
    [Fact]
    public void FailedLoadAndWritePreserveSessionAndDestination()
    {
        var g=FleetFixtures.Nested();var root=JsonNode.Parse(SaveGame.Serialize(g.Content,g.State))!;root["state"]!["empires"]![0]!["fleet_command"]!["requests"]![0]!["priority"]="impossible";
        string dir=Path.Combine(Path.GetTempPath(),"OpenAntares-fleet-codec-"+Guid.NewGuid());Directory.CreateDirectory(dir);string file=Path.Combine(dir,"save.json");try
        {
            File.WriteAllText(file,root.ToJsonString());var session=new GameSession(g.Content,g.State);Assert.False(session.Load(file).Succeeded);Assert.Same(g.Content,session.Content);Assert.Same(g.State,session.State);
            File.WriteAllText(file,"prior");g.State.Empires[0].Fleets!.Requests[0].Priority=(RequestPriority)99;Assert.Throws<ArgumentException>(()=>SaveGame.WriteFile(file,g.Content,g.State));Assert.Equal("prior",File.ReadAllText(file));Assert.False(File.Exists(file+".tmp"));
            g.State.Empires[0].Fleets!.Requests[0].Priority=RequestPriority.Normal;
            if(OperatingSystem.IsWindows())
            {
                using(var locked=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read))
                {var failure=Record.Exception(()=>SaveGame.WriteFile(file,g.Content,g.State));Assert.True(failure is IOException or UnauthorizedAccessException,failure?.ToString());}
                Assert.Equal("prior",File.ReadAllText(file));Assert.False(File.Exists(file+".tmp"));
            }
        }finally{File.Delete(file);Directory.Delete(dir);}
    }
}
