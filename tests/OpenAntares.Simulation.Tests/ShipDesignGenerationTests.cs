using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignGenerationTests
{
    [Theory,MemberData(nameof(ShipDesignStateTests.Setups),MemberType=typeof(ShipDesignStateTests))]
    public void CurrentSetupCreatesRules11WithoutFreeEquipment(string species,string size,bool sentries)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var g=GalaxyGenerator.CreateNewGame(c,new(size,42,SpeciesId:species,RivalCount:0,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries));
        Assert.True(g.Succeeded,g.Error);var s=g.State!;Assert.Equal(11,s.RulesVersion);Assert.Equal(13,SaveGame.SchemaVersion);
        var old=GalaxyGenerator.CreateNewGame(c,new(size,42,9,species,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries)).State!;
        Assert.Equal(old.NextEntityId,s.NextEntityId);Assert.Equal(old.Random.State,s.Random.State);Assert.Equal(old.Random.Increment,s.Random.Increment);
        Assert.Equal(old.Vessels.Count,s.Vessels.Count);Assert.All(s.Vessels,v=>Assert.Null(v.Design));Assert.Equal(old.Colonies[0].ProductionReserve,s.Colonies[0].ProductionReserve);
        Assert.Equal(old.Empires[0].KnownTechnologyIds,s.Empires[0].KnownTechnologyIds);Assert.Equal(old.Empires[0].ResearchReserve,s.Empires[0].ResearchReserve);
        var d=Assert.Single(s.Empires).ShipDesigns!;Assert.Single(d.Designs);Assert.Equal("Standard Escort",d.Designs[0].Name);Assert.Empty(d.Refits);Assert.Empty(GameStateValidator.Validate(c,s));
    }
    [Fact] public void MissingCurrentDefinitionsRejectEvenOffAndRetainPriorSession()
    {
        var session=ShipDesignFixtures.Session();string before=SaveGame.Serialize(session.Content,session.State);
        var g=GalaxyGenerator.CreateNewGame(session.Content with{ShipDesigns=null},new("small",42,SpeciesId:"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
        Assert.False(g.Succeeded);Assert.Contains("ship",g.Error!,StringComparison.OrdinalIgnoreCase);Assert.Equal(before,SaveGame.Serialize(session.Content,session.State));
    }
    [Fact] public void CurrentAlternateRoleUsesConfiguredBinding()
    {
        var c=ShipDesignFixtures.Session().Content;var role=c.Vessels["escort"] with{Id="guardian"};c=c with{Vessels=c.Vessels.Remove("escort").Add("guardian",role),ShipDesigns=c.ShipDesigns! with{WarshipRoleId="guardian"},Combat=c.Combat! with{RoleProfiles=c.Combat.RoleProfiles.Remove("escort").Add("guardian",c.Combat.RoleProfiles["escort"])}};
        var g=GalaxyGenerator.CreateNewGame(c,new("small",42,SpeciesId:"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Assert.True(g.Succeeded,g.Error);Assert.Equal(11,g.State!.RulesVersion);Assert.Single(g.State.Empires[0].ShipDesigns!.Designs);Assert.Empty(GameStateValidator.Validate(c,g.State));
    }
    [Fact] public void PeacefulPolicyDoesNotSelectShipEquipmentResearchOrWarships()
    {
        var c=ShipDesignFixtures.Session().Content;var g=GalaxyGenerator.CreateNewGame(c,new("small",42,10,"harl",1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));Assert.True(g.Succeeded,g.Error);var s=g.State!;var ai=s.Empires[1];ai.KnownTechnologyIds=c.Technologies.Values.Where(t=>!t.ShipDesignOnly).Select(t=>t.Id).Order(StringComparer.Ordinal).ToList();
        var planned=OpenAntares.Simulation.Rivals.RivalTurnPlanner.Prepare(c,s);Assert.True(planned.Succeeded);Assert.Null(planned.State.Empires[1].ResearchTargetId);Assert.Single(planned.State.Empires[1].ShipDesigns!.Designs);Assert.Empty(planned.State.Empires[1].ShipDesigns!.Refits);Assert.DoesNotContain(planned.State.Colonies.Where(col=>col.EmpireId==ai.Id),col=>col.ProjectKind=="ship_design");
    }

}

