using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.ShipDesigns;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class ShipDesignContentTests
{
    [Theory]
    [InlineData("cost")][InlineData("speed")][InlineData("attack")]
    public void OptionalComponentOverflowFailsAtLoadWithComponentField(string field)
    {
        var node=JsonNode.Parse(BasicJson)!;var d=node["ship_designs"]!;
        var part=d["components"]![field=="attack"?"pulse_bank":"conventional_drive"]!.DeepClone();
        part["id"]="optional_part";part["name"]="Optional part";
        part[field=="cost"?"cost":"value"]=field=="cost"?JsonValue.Create(long.MaxValue):JsonValue.Create(int.MaxValue);
        d["components"]!["optional_part"]=part;
        if(field=="speed")
        {
            var frame=d["frames"]!["escort_frame"]!.DeepClone();frame["id"]="optional_frame";frame["travel_percent"]=200;d["frames"]!["optional_frame"]=frame;
        }
        var loaded=Load(node.ToJsonString());Assert.False(loaded.Succeeded);
        Assert.Contains(loaded.Errors,e=>e.File=="ship-basic.json"&&e.Field=="ship_designs.components.optional_part."+(field=="cost"?"cost":"value")&&e.Message.Contains("overflow",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CoreShipContentMatchesApprovedTables()
    {
        var c=Core();Assert.NotNull(c.ShipDesigns);var d=c.ShipDesigns!;
        var e=d.Frames["escort_frame"];var f=d.Frames["frigate_frame"];
        Assert.Equal((100,6,1200L,100),(e.Hull,e.Space,e.Cost,e.TravelPercent));
        Assert.Equal((180,10,2400L,75),(f.Hull,f.Space,f.Cost,f.TravelPercent));
        Assert.Equal("escort",d.WarshipRoleId);
        Assert.Equal(new DesignStats(2400,100,24,4,300,5,6),ShipDesignMath.Calculate(d,d.StartingRecipe));
        foreach(var item in new[]{("focused_emitters",2400L,"measurement_methods"),("reinforced_laminates",2400L,"industrial_coordination"),("vector_drives",3600L,"industrial_coordination"),("frigate_construction",3600L,"industrial_coordination")})
        {
            Assert.True(c.Technologies.TryGetValue(item.Item1,out var technology));
            Assert.Equal(item.Item2,technology!.Cost);Assert.Equal(new[]{item.Item3},technology.PrerequisiteTechnologyIds);Assert.True(technology.ShipDesignOnly);
        }
    }

    [Theory]
    [InlineData("conventional_drive",0,400L,300)]
    [InlineData("vector_drive",0,800L,400)]
    [InlineData("pulse_bank",3,600L,24)]
    [InlineData("lance_bank",4,1000L,36)]
    [InlineData("plated_armor",2,200L,4)]
    [InlineData("reinforced_armor",3,500L,8)]
    public void CoreComponentContributionIsExact(string id,int space,long cost,int value)
    {
        var c=Core();Assert.NotNull(c.ShipDesigns);var component=c.ShipDesigns!.Components[id];
        Assert.Equal((space,cost,value),(component.Space,component.Cost,component.Value));
    }

    [Fact]
    public void BasicPackWithoutOptionalAdvancedContentLoads()
    {
        var result=Load(BasicJson);Assert.True(result.Succeeded,string.Join("; ",result.Errors));
        Assert.Single(result.Content!.ShipDesigns!.Frames);Assert.Equal(3,result.Content.ShipDesigns.Components.Count);
        Assert.Equal(new DesignStats(2400,100,24,4,300,5,6),ShipDesignMath.Calculate(result.Content.ShipDesigns,result.Content.ShipDesigns.StartingRecipe));
    }

    [Fact]
    public void RequiredSectionCannotBeOmitted()
    {
        var result=ContentLoader.Load(Sources(),requireShipDesigns:true);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors,e=>e.Field?.Contains("ship_designs",StringComparison.Ordinal)==true);
    }

    [Fact]
    public void RenamedArmedRoleIsBoundByData()
    {
        var sources=Sources().Append(new ContentSource("ship-basic.json",BasicJson))
            .Select(s=>s with{Json=s.Json.Replace("\"escort\"","\"mod_guard\"",StringComparison.Ordinal)});
        var result=ContentLoader.Load(sources,requireShipDesigns:true);
        Assert.True(result.Succeeded,string.Join("; ",result.Errors));
        Assert.Equal("mod_guard",result.Content!.ShipDesigns!.WarshipRoleId);
    }

    [Theory]
    [InlineData("missing_recipe","starting_recipe")]
    [InlineData("unknown_role","warship_role_id")]
    [InlineData("missing_engine","engine")]
    [InlineData("unknown_tech","prerequisites")]
    [InlineData("negative_cost","cost")]
    [InlineData("starting_mismatch","starting_recipe")]
    [InlineData("unknown_field","mystery")]
    [InlineData("overflow","overflow")]
    public void InvalidDefinitionsHaveActionableErrors(string fault,string expected)
    {
        var node=JsonNode.Parse(BasicJson)!;var d=node["ship_designs"]!;
        switch(fault)
        {
            case "missing_recipe":d.AsObject().Remove("starting_recipe");break;
            case "unknown_role":d["warship_role_id"]="absent_role";break;
            case "missing_engine":d["starting_recipe"]!["engine_id"]="absent_engine";break;
            case "unknown_tech":d["components"]!["pulse_bank"]!["prerequisites"]=new JsonArray("absent_tech");break;
            case "negative_cost":d["frames"]!["escort_frame"]!["cost"]=-1;break;
            case "starting_mismatch":d["components"]!["pulse_bank"]!["value"]=25;break;
            case "unknown_field":d["components"]!["pulse_bank"]!["mystery"]=1;break;
            case "overflow":d["frames"]!["escort_frame"]!["cost"]=long.MaxValue;break;
        }
        var result=Load(node.ToJsonString());Assert.False(result.Succeeded);
        Assert.Contains(result.Errors,e=>e.File=="ship-basic.json"&&(e.Message.Contains(expected,StringComparison.OrdinalIgnoreCase)||e.Field?.Contains(expected,StringComparison.OrdinalIgnoreCase)==true));
    }

    [Fact]
    public void DuplicateFieldsAreRejectedInsteadOfLastWins()
    {
        string bad=BasicJson.Replace("\"space\":6","\"space\":6,\"space\":6",StringComparison.Ordinal);
        Assert.NotEqual(BasicJson,bad);var result=Load(bad);Assert.False(result.Succeeded);
        Assert.Contains(result.Errors,e=>e.Message.Contains("duplicate",StringComparison.OrdinalIgnoreCase)&&e.Message.Contains("space",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TwoBanksSumOnceAndCapacityIsEnforced()
    {
        var d=BasicDefinition();
        Assert.Equal(new DesignStats(2800,100,48,0,300,6,6),ShipDesignMath.Calculate(d,new("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank"))));
        Assert.Throws<ArgumentException>(()=>ShipDesignMath.Calculate(d,new("escort_frame","conventional_drive","plated_armor",ImmutableArray.Create("pulse_bank","pulse_bank"))));
        Assert.Throws<ArgumentException>(()=>ShipDesignMath.Calculate(d,new("escort_frame","plated_armor",null,ImmutableArray.Create("pulse_bank"))));
        Assert.Throws<ArgumentException>(()=>ShipDesignMath.Calculate(d,new("escort_frame","conventional_drive",null,ImmutableArray<string>.Empty)));
        Assert.Throws<ArgumentException>(()=>ShipDesignMath.Calculate(d,new("escort_frame","conventional_drive",null,ImmutableArray.Create("pulse_bank","pulse_bank","pulse_bank"))));
    }

    [Fact]
    public void NewResearchAndContentAreGatedForOldCampaigns()
    {
        var c=Core();var technology=new TechnologyDefinition("design_test","Design test",100,ImmutableArray<string>.Empty){ShipDesignOnly=true};
        c=c with{Technologies=c.Technologies.Add(technology.Id,technology)};
        Assert.False(Rules.TechnologyAvailable(c,9,technology.Id));Assert.True(Rules.TechnologyAvailable(c,10,technology.Id));
        var old=GalaxyGenerator.CreateNewGame(c,new("small",42,9,"harl",CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:false));
        Assert.True(old.Succeeded,old.Error);var s=new GameSession(c,old.State!);var outcome=s.Execute(new SetResearchTargetCommand(old.State!.Empires[0].Id,0,technology.Id));Assert.False(outcome.Accepted);
        var saved=JsonNode.Parse(SaveGame.Serialize(c,old.State))!;
        Assert.Null(saved["content"]!["ship_designs"]);
        Assert.DoesNotContain(saved["content"]!["technologies"]!.AsArray(),t=>t!["id"]!.GetValue<string>()==technology.Id);
        Assert.Equal(11,Rules.CurrentVersion);
    }

    [Fact]
    public void ExplicitTenRetainsItsFeaturesAndCurrentElevenIncludesThem()
    {
        Assert.True(Rules.IsSupported(10));Assert.True(Rules.HasShipDesigns(10));Assert.False(Rules.HasShipDesigns(9));
        Assert.True(Rules.HasExpansion(10));Assert.True(Rules.HasSpecies(10));Assert.True(Rules.HasRivals(10));Assert.True(Rules.HasDevelopment(10));Assert.True(Rules.HasCrises(10));Assert.True(Rules.HasScouting(10));Assert.True(Rules.HasFleets(10));Assert.True(Rules.HasCombat(10));
        Assert.Equal(11,Rules.CurrentVersion);
    }

    private static ContentSet Core(){var r=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory());Assert.True(r.Succeeded,string.Join("; ",r.Errors));return r.Content!;}
    private static ContentSource[] Sources()=>Directory.GetFiles(ContentLoaderTests.CoreContentDirectory(),"*.json").Where(p=>Path.GetFileName(p)!="ship_design.json").Select(p=>
    {
        var root=JsonNode.Parse(File.ReadAllText(p))!.AsObject();
        if(root["technologies"] is JsonArray technologies)
            root["technologies"]=new JsonArray(technologies.Where(t=>t?["ship_design_only"]?.GetValue<bool>()!=true).Select(t=>t!.DeepClone()).ToArray());
        return new ContentSource(Path.GetFileName(p),root.ToJsonString());
    }).ToArray();
    private static ContentLoadResult Load(string json)=>ContentLoader.Load(Sources().Append(new ContentSource("ship-basic.json",json)),requireShipDesigns:true);
    private static ShipDesignDefinition BasicDefinition()=>new("escort",new("escort_frame","conventional_drive","plated_armor",ImmutableArray.Create("pulse_bank")),
        ImmutableSortedDictionary<string,ShipFrameDefinition>.Empty.Add("escort_frame",new("escort_frame","Escort",100,6,1200,100,ImmutableArray<string>.Empty)),
        ImmutableSortedDictionary<string,ShipComponentDefinition>.Empty.Add("conventional_drive",new("conventional_drive","Conventional",ShipComponentKind.Engine,0,400,300,ImmutableArray<string>.Empty))
            .Add("pulse_bank",new("pulse_bank","Pulse",ShipComponentKind.Weapon,3,600,24,ImmutableArray<string>.Empty))
            .Add("plated_armor",new("plated_armor","Plated",ShipComponentKind.Armor,2,200,4,ImmutableArray<string>.Empty)));
    private const string BasicJson="""
    {"ship_designs":{
      "warship_role_id":"escort",
      "starting_recipe":{"frame_id":"escort_frame","engine_id":"conventional_drive","armor_id":"plated_armor","weapon_bank_ids":["pulse_bank"]},
      "frames":{"escort_frame":{"id":"escort_frame","name":"Escort","hull":100,"space":6,"cost":1200,"travel_percent":100,"prerequisites":[]}},
      "components":{
        "conventional_drive":{"id":"conventional_drive","name":"Conventional","kind":"engine","space":0,"cost":400,"value":300,"prerequisites":[]},
        "pulse_bank":{"id":"pulse_bank","name":"Pulse","kind":"weapon","space":3,"cost":600,"value":24,"prerequisites":[]},
        "plated_armor":{"id":"plated_armor","name":"Plated","kind":"armor","space":2,"cost":200,"value":4,"prerequisites":[]}
      }
    }}
    """;
}

