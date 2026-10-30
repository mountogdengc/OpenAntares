using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class ShipDesignMathTests
{
    [Theory]
    [InlineData("standard",2400L,100,24,4,300,5,6)]
    [InlineData("fast",2800L,100,24,4,400,5,6)]
    [InlineData("gun",2800L,100,48,0,300,6,6)]
    [InlineData("frigate",4900L,180,60,8,225,10,10)]
    public void ApprovedLoadoutsHaveExactStats(string kind,long cost,int hull,int attack,int armor,int speed,int used,int capacity)
    {
        var session=Equipped(kind);var state=session.State;var ship=state.Vessels[0];var expected=new DesignStats(cost,hull,attack,armor,speed,used,capacity);
        var loadout=VesselLoadouts.Resolve(session.Content,state,ship);
        Assert.Equal(cost,loadout.BuildCost);Assert.Equal(speed,loadout.BaseSpeed);Assert.Equal(new CombatProfile(hull,attack,armor),loadout.Profile);Assert.Equal(ship.Design,loadout.Design);
        Assert.Equal(expected,ShipDesignMath.Calculate(session.Content.ShipDesigns!,state.Empires[0].ShipDesigns!.Designs.Single(d=>d.Id==ship.Design).Recipe));
        Assert.Empty(GameStateValidator.Validate(session.Content,state));
        var view=session.View(ship.EmpireId);Assert.Equal(loadout,VesselLoadouts.Resolve(session.Content,view,ship.Id));
        var before=JsonSerializer.Serialize(view);state.Empires[0].ShipDesigns!.Designs.Clear();
        Assert.Equal(loadout,VesselLoadouts.Resolve(session.Content,view,ship.Id));Assert.Equal(before,JsonSerializer.Serialize(view));
    }
    [Theory]
    [InlineData(2800L,700L)][InlineData(4900L,1225L)][InlineData(0L,1L)][InlineData(1L,1L)][InlineData(4L,1L)][InlineData(5L,2L)][InlineData(long.MaxValue,2305843009213693952L)]
    public void RefitPriceUsesCheckedCeiling(long cost,long expected)=>Assert.Equal(expected,ShipDesignMath.RefitPrice(cost));
    [Fact] public void NegativeRefitPriceRejects()=>Assert.Throws<ArgumentOutOfRangeException>(()=>ShipDesignMath.RefitPrice(-1));
    [Fact]
    public void CapacityOverflowAndComponentMultiplicityReject()
    {
        var d=ShipDesignFixtures.Session().Content.ShipDesigns!;
        Assert.Throws<ArgumentException>(()=>ShipDesignMath.Calculate(d,new("escort_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("lance_bank"))));
        Assert.Throws<ArgumentException>(()=>ShipDesignMath.Calculate(d,new("frigate_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("lance_bank","lance_bank"))));
        var frame=d.Frames["escort_frame"] with{Cost=long.MaxValue};Assert.Throws<OverflowException>(()=>ShipDesignMath.Calculate(d with{Frames=d.Frames.SetItem(frame.Id,frame)},d.StartingRecipe));
        var weapon=d.Components["pulse_bank"] with{Value=int.MaxValue};Assert.Throws<OverflowException>(()=>ShipDesignMath.Calculate(d with{Components=d.Components.SetItem(weapon.Id,weapon)},new("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank"))));
        var a=new ShipRecipe("frigate_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("pulse_bank","lance_bank"));
        Assert.Equal(ShipDesignMath.Calculate(d,a),ShipDesignMath.Calculate(d,a with{WeaponBankIds=ImmutableArray.Create("lance_bank","pulse_bank")}));
    }
    [Fact]
    public void InstalledMaxHullControlsDamageBandsBeforeNavigator()
    {
        var session=Equipped("standard");var v=session.State.Vessels[0];var loadout=VesselLoadouts.Resolve(session.Content,session.State,v);
        Assert.Equal(225,CombatMath.Stats(loadout.Profile!,50,loadout.BaseSpeed,0).Speed);Assert.Equal(150,CombatMath.Stats(loadout.Profile!,25,loadout.BaseSpeed,0).Speed);
        var fast=Equipped("fast");var f=VesselLoadouts.Resolve(fast.Content,fast.State,fast.State.Vessels[0]);Assert.Equal(330,CombatMath.Stats(f.Profile!,50,f.BaseSpeed,10).Speed);
        var frigate=Equipped("frigate");Assert.Empty(GameStateValidator.Validate(frigate.Content,frigate.State));frigate.State.Vessels[0].Combat!.Hull=181;Assert.NotEmpty(GameStateValidator.Validate(frigate.Content,frigate.State));
    }
    [Fact]
    public void GunBanksFireOneShotWithArmorSubtractedOnce()
    {
        var session=Equipped("gun");var v=session.State.Vessels[0];var l=VesselLoadouts.Resolve(session.Content,session.State,v);var p=l.Profile!;
        var frame=new CombatFrame(null,0,1,v.EmpireId,new(1),ImmutableArray.Create(new CombatShipFrame(v.Id,v.RoleId,100,p.MaxHull,100,p.Attack,p.Armor,l.BaseSpeed,true,0)),ImmutableArray.Create(new SentryFrame(new(1),80,80,0,2,250)),new(CombatDoctrine.Cautious,session.State.Colonies[0].Id,StragglerPolicy.KeepTogether,CombatArrivalOrder.Hold));
        var result=CombatExchange.Calculate(frame,false,ImmutableArray<VesselId>.Empty);Assert.Equal(46,Assert.Single(result.Shots).Damage);
    }
    [Fact]
    public void LegacyLoadoutIsByteStable()
    {
        var root=CompatibilityFixtures.PathFor("rules9-ship-design-baselines");var manifest=JsonNode.Parse(File.ReadAllText(Path.Combine(root,"manifest.json")))!;
        var source=manifest["cases"]![0];
        var loaded=SaveGame.ReadFile(Path.Combine(root,source!["save"]!.GetValue<string>()));Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));var g=loaded.Game!;
        var before=CompatibilityFixtures.CanonicalWithoutSchema(g.Content,g.State);
        foreach(var v in g.State.Vessels)
        {
            var l=VesselLoadouts.Resolve(g.Content,g.State,v);Assert.Null(l.Design);Assert.Null(l.FrameId);Assert.Equal(g.Content.Vessels[v.RoleId].Cost,l.BuildCost);Assert.Equal(g.Content.Vessels[v.RoleId].Speed,l.BaseSpeed);Assert.Equal(g.Content.Combat!.RoleProfiles[v.RoleId],l.Profile);
        }
        Assert.Equal(before,CompatibilityFixtures.CanonicalWithoutSchema(g.Content,g.State));
    }
    [Fact]
    public void RenamedBindingAndComponentsResolveWithoutCoreNames()
    {
        var session=Equipped("fast");var c=session.Content;var d=c.ShipDesigns!;var engine=d.Components["vector_drive"] with{Id="quick_engine"};var role=c.Vessels["escort"] with{Id="guardian"};
        var row=session.State.Empires[0].ShipDesigns!.Designs[1];session.State.Empires[0].ShipDesigns!.Designs[1]=row with{Recipe=row.Recipe with{EngineId="quick_engine"}};
        var ship=session.State.Vessels[0];ship.RoleId="guardian";
        c=c with{ShipDesigns=d with{WarshipRoleId="guardian",Components=d.Components.Remove("vector_drive").Add(engine.Id,engine)},Vessels=c.Vessels.Remove("escort").Add(role.Id,role)};
        var l=VesselLoadouts.Resolve(c,session.State,ship);Assert.Equal(400,l.BaseSpeed);Assert.Equal(2800,l.BuildCost);Assert.Equal(new CombatProfile(100,24,4),l.Profile);
    }
    internal static GameSession Equipped(string kind)
    {
        var session=ShipDesignFixtures.Session(ImmutableArray.Create("measurement_methods","industrial_coordination","focused_emitters","reinforced_laminates","vector_drives","frigate_construction"));var s=session.State;var d=session.Content.ShipDesigns!;
        var recipe=kind switch{"fast"=>d.StartingRecipe with{EngineId="vector_drive"},"gun"=>new ShipRecipe("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank")),"frigate"=>new ShipRecipe("frigate_frame","conventional_drive","reinforced_armor",ImmutableArray.Create("lance_bank","pulse_bank")),_=>d.StartingRecipe};
        var owner=s.Empires[0];DesignId id=new(1);if(kind!="standard"){id=new(2);owner.ShipDesigns!.Designs.Add(new(id,"Fixture "+kind,recipe,false));owner.ShipDesigns.NextDesignId=3;}
        var v=s.Vessels[0];v.RoleId=d.WarshipRoleId;v.Design=id;v.Combat=new(){Hull=kind=="frigate"?180:100,Cohesion=100};return session;
    }
}
