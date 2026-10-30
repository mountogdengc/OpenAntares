using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignStateTests
{
    public static IEnumerable<object[]> Setups()=>from s in new[]{"harl","peldra","sulan","wenth"} from z in new[]{"small","medium","large"} from b in new[]{false,true} select new object[]{s,z,b};
    [Theory,MemberData(nameof(Setups))]
    public void Rules10InitializesOnlyPermanentStandardEscort(string species,string size,bool sentries)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var old=GalaxyGenerator.CreateNewGame(c,new(size,42,9,species,1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries));
        var result=GalaxyGenerator.CreateNewGame(c,new(size,42,10,species,1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries));
        Assert.True(result.Succeeded,result.Error);var state=result.State!;var before=old.State!;
        Assert.Equal(before.NextEntityId,state.NextEntityId);Assert.Equal(before.Random.State,state.Random.State);Assert.Equal(before.Random.Increment,state.Random.Increment);
        Assert.Equal(before.Vessels.Count,state.Vessels.Count);
        foreach(var owner in state.Empires)
        {
            Assert.NotNull(owner.ShipDesigns);var d=owner.ShipDesigns!;var starter=Assert.Single(d.Designs);
            Assert.Equal(new DesignId(1),starter.Id);Assert.Equal("Standard Escort",starter.Name);Assert.False(starter.Archived);
            Assert.Equal(2,d.NextDesignId);Assert.Equal(1,d.NextRefitId);Assert.Equal(1,d.CatalogueRevision);Assert.Empty(d.Refits);Assert.Empty(d.PendingEvents);Assert.Empty(d.RecentEvents);
            Assert.Equal(new DesignStats(2400,100,24,4,300,5,6),ShipDesignMath.Calculate(c.ShipDesigns!,starter.Recipe));
            Assert.Equal(before.FindEmpire(owner.Id)!.KnownTechnologyIds,owner.KnownTechnologyIds);Assert.Equal(before.FindEmpire(owner.Id)!.ResearchReserve,owner.ResearchReserve);
        }
        Assert.All(before.Empires,e=>Assert.Null(e.ShipDesigns));Assert.All(state.Vessels,v=>Assert.Null(v.Design));Assert.Empty(GameStateValidator.Validate(c,state));
        var repeat=GalaxyGenerator.CreateNewGame(c,new(size,42,10,species,1,"peldra",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries)).State!;
        Assert.Equal(JsonSerializer.Serialize(state),JsonSerializer.Serialize(repeat));
    }
    [Fact]
    public void CloneDoesNotShareJobsOrCatalogueLists()
    {
        var session=ShipDesignFixtures.Session();var original=session.State;var owner=original.Empires[0];
        owner.ShipDesigns??=new();owner.ShipDesigns.Refits.Add(new(){Id=new(1),Reason="before"});
        var bytes=JsonSerializer.Serialize(original);var clone=original.Clone();var d=clone.Empires[0].ShipDesigns!;
        d.Designs.Clear();d.Refits[0].Reason="after";d.Refits.Add(new(){Id=new(2)});d.RecentEvents.Add(new(owner.Id,1,0,"test","test",null,null,null,null,0));d.PendingEvents.Add(new(owner.Id,2,0,"test","test",null,null,null,null,0));
        Assert.Equal(bytes,JsonSerializer.Serialize(original));
    }
    [Theory]
    [InlineData("missing")][InlineData("old")][InlineData("duplicate")][InlineData("counter")][InlineData("revision")][InlineData("job_port")][InlineData("job_ship")][InlineData("warship")][InlineData("civilian")][InlineData("name")][InlineData("active_limit")][InlineData("retained_limit")][InlineData("locked_recipe")]
    public void InvalidOwnerStateIsRejected(string mutation)
    {
        var session=ShipDesignFixtures.Session();var c=session.Content;var s=session.State;var e=s.Empires[0];
        e.ShipDesigns??=new(){NextDesignId=2,Designs=new(){new(new(1),"Standard Escort",c.ShipDesigns!.StartingRecipe,false)}};var d=e.ShipDesigns;
        switch(mutation)
        {
            case "missing":e.ShipDesigns=null;break;
            case "old":s.RulesVersion=9;break;
            case "duplicate":d.Designs.Add(d.Designs[0]);break;
            case "counter":d.NextDesignId=1;break;
            case "revision":d.CatalogueRevision=0;break;
            case "job_port":d.Refits.Add(new(){Id=new(1),Vessel=new(300),Port=s.Colonies[0].Id});d.Refits.Add(new(){Id=new(2),Vessel=new(301),Port=s.Colonies[0].Id});break;
            case "job_ship":d.Refits.Add(new(){Id=new(1),Vessel=s.Vessels[0].Id,Port=new(300)});d.Refits.Add(new(){Id=new(2),Vessel=s.Vessels[0].Id,Port=new(301)});break;
            case "warship":s.Vessels[0].RoleId=c.ShipDesigns!.WarshipRoleId;break;
            case "civilian":s.Vessels[0].Design=new(1);break;
            case "name":d.Designs.Add(new(new(2)," standard escort ",c.ShipDesigns!.StartingRecipe,false));d.NextDesignId=3;break;
            case "active_limit":for(int i=2;i<=33;i++)d.Designs.Add(new(new(i),"Design "+i,c.ShipDesigns!.StartingRecipe,false));d.NextDesignId=34;break;
            case "retained_limit":for(int i=2;i<=257;i++)d.Designs.Add(new(new(i),"Design "+i,c.ShipDesigns!.StartingRecipe,true));d.NextDesignId=258;break;
            case "locked_recipe":d.Designs.Add(new(new(2),"Locked",c.ShipDesigns!.StartingRecipe with{EngineId="vector_drive"},false));d.NextDesignId=3;break;
        }
        Assert.NotEmpty(ShipDesignStateValidator.Validate(c,s));Assert.NotEmpty(GameStateValidator.Validate(c,s));
    }
    [Fact]
    public void ExplicitRules10RequiresContentEvenWithSentriesOff()
    {
        var session=ShipDesignFixtures.Session();var result=GalaxyGenerator.CreateNewGame(session.Content with{ShipDesigns=null},new("small",42,10,"harl",AmebasEnabled:false,SentriesEnabled:false));
        Assert.False(result.Succeeded);Assert.Contains("ship",result.Error!,StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void AlternativeRoleUsesDeclaredBindingAndLocalIdentity()
    {
        var session=ShipDesignFixtures.Session();var c=session.Content;var role=c.Vessels["escort"] with{Id="guardian"};
        c=c with{Vessels=c.Vessels.Remove("escort").Add("guardian",role),ShipDesigns=c.ShipDesigns! with{WarshipRoleId="guardian"},Combat=c.Combat! with{RoleProfiles=c.Combat.RoleProfiles.Remove("escort").Add("guardian",c.Combat.RoleProfiles["escort"])}};
        var s=session.State;s.Vessels[0].RoleId="guardian";s.Empires[0].ShipDesigns=null;ShipDesignInitializer.Initialize(c,s);
        Assert.NotNull(s.Empires[0].ShipDesigns);Assert.Equal(new DesignId(1),s.Vessels[0].Design);Assert.Equal(2,s.Empires[0].ShipDesigns!.NextDesignId);
    }
}
