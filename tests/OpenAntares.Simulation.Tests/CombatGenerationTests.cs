using System;
using System.Linq;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Species;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CombatGenerationTests
{
    [Fact] public void StandardUsesLocalIdsAndNoExistingRandomDraws()
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        foreach(var species in new[]{"harl","peldra","sulan","wenth"})foreach(var size in c.GalaxySizes.Keys)foreach(bool ameba in new[]{false,true})
        {
            var old=GalaxyGenerator.CreateNewGame(c,new(size,42,8,species,1,"peldra",AmebasEnabled:ameba)).State!;
            var generated=GalaxyGenerator.CreateNewGame(c,new(size,42,9,species,1,"peldra",AmebasEnabled:ameba,SentriesEnabled:true));
            Assert.True(generated.Succeeded,generated.Error);var s=generated.State!;Assert.NotNull(s.Combat);
            var p=Assert.Single(s.Combat.Pockets);Assert.Equal(2,p.Sentries.Count);Assert.All(p.Sentries,a=>Assert.Equal(80,a.Hull));Assert.Equal(2,s.Combat.NextPocketId);Assert.Equal(3,p.NextSentryId);
            var homes=s.Colonies.Select(a=>s.FindStar(s.FindPlanet(a.PlanetId)!.StarId)!).ToArray();
            Assert.DoesNotContain(homes,h=>h.Id==p.Star);Assert.NotEqual(s.Scouting!.Habitat?.Star,p.Star);
            Int128 Distance(StarState a,StarState b){Int128 x=(Int128)a.X-b.X,y=(Int128)a.Y-b.Y;return x*x+y*y;}
            bool Eligible(StarState a)=>!homes.Any(h=>h.Id==a.Id)&&s.Scouting.Habitat?.Star!=a.Id&&s.Planets.Any(pl=>pl.StarId==a.Id&&c.PlanetTypes[pl.PlanetTypeId].Colonizable&&c.PlanetTypes[pl.PlanetTypeId].SupportPerWorker>0&&c.Species.Values.Any(sp=>pl.EnvironmentId is {} env&&(sp.NaturalEnvironmentIds.Contains(env)||sp.AllowsSealedHabitats&&sp.AllowedVesselIds.Any(id=>c.Vessels[id].CanFoundColony&&c.Vessels[id].FoundsSealedHabitat))));
            var expected=s.Stars.Where(Eligible).OrderByDescending(a=>homes.Min(h=>Distance(a,h))).ThenBy(a=>a.Id).First();Assert.Equal(expected.Id,p.Star);
            Assert.Equal(old.NextEntityId,s.NextEntityId);Assert.Equal(old.Random.State,s.Random.State);Assert.Equal(old.Random.Increment,s.Random.Increment);
            Assert.Equal(old.Scouting!.PlacementRandom?.State,s.Scouting.PlacementRandom?.State);Assert.Equal(old.Empires[0].Scouting!.RiskRandom?.State,s.Empires[0].Scouting!.RiskRandom?.State);Assert.Equal(old.Empires[0].Fleets!.RecoveryRandom!.State,s.Empires[0].Fleets!.RecoveryRandom!.State);
            Assert.All(s.Vessels,v=>{Assert.Equal(c.Combat!.RoleProfiles[v.RoleId].MaxHull,v.Combat!.Hull);Assert.Equal(100,v.Combat.Cohesion);});
        }
    }
    [Fact] public void RenamedContentUsesDeclaredCapabilitiesAndIds()
    {
        var root=CombatContentTests.CoreNode();var role=root["vessels"]!.AsArray().Single(n=>n!["id"]!.GetValue<string>()=="escort")!;role["id"]="guardian";
        var profiles=root["combat"]!["role_profiles"]!.AsObject();var profile=profiles["escort"]!.DeepClone();profiles.Remove("escort");profiles["guardian"]=profile;root["combat"]!["sentry_role_id"]="gate_machine";
        var loaded=ContentLoader.Load(new[]{new ContentSource("alternative-combat.json",root.ToJsonString())},requireCombat:true);Assert.True(loaded.Succeeded,string.Join("; ",loaded.Errors));
        var result=GalaxyGenerator.CreateNewGame(loaded.Content!,new("small",42,9,"wenth",AmebasEnabled:false,SentriesEnabled:true));Assert.True(result.Succeeded,result.Error);Assert.Single(result.State!.Combat!.Pockets);
        Assert.Equal(100,loaded.Content!.Combat!.RoleProfiles["guardian"].MaxHull);Assert.Empty(GameStateValidator.Validate(loaded.Content,result.State));
    }
    [Fact] public void OffHasCompleteDormantStateAndChoiceIsExplicit()
    {
        var g=CombatFixtures.Game(enabled:false);Assert.NotNull(g.State.Combat);Assert.Empty(g.State.Combat.Pockets);Assert.All(g.State.Empires,e=>Assert.NotNull(e.Combat));Assert.All(g.State.Vessels,v=>Assert.NotNull(v.Combat));
        Assert.False(GalaxyGenerator.CreateNewGame(g.Content,new("small",42,9,"harl",AmebasEnabled:false)).Succeeded);
        Assert.False(GalaxyGenerator.CreateNewGame(g.Content,new("small",42,8,"harl",AmebasEnabled:false,SentriesEnabled:false)).Succeeded);
    }
    [Fact] public void NoEligibleStarFailsWithoutMutationOrReroll()
    {
        var g=CombatFixtures.Game(enabled:false);var s=g.State.Clone();s.Stars.RemoveAll(st=>s.Colonies.All(c=>s.FindPlanet(c.PlanetId)!.StarId!=st.Id));var before=CombatFixtures.Bytes(s);
        string? error=CombatInitializer.Initialize(g.Content,s,true);Assert.Contains("Sentries",error);Assert.Contains("Off",error);Assert.Equal(before,CombatFixtures.Bytes(s));
    }
    [Fact] public void DistanceTieUsesLowestIdAndLargeCoordinatesDoNotOverflow()
    {
        var g=CombatFixtures.Game(enabled:false);var s=g.State;var home=s.FindStar(s.FindPlanet(s.Colonies[0].PlanetId)!.StarId)!;home.X=0;home.Y=0;
        var candidates=s.Stars.Where(st=>st.Id!=home.Id).ToArray();foreach(var st in candidates){st.X=int.MaxValue;st.Y=int.MinValue;}
        foreach(var p in s.Planets.Where(p=>p.StarId!=home.Id)){p.PlanetTypeId=s.Planets.First(p=>p.StarId==home.Id).PlanetTypeId;p.EnvironmentId=s.Planets.First(p=>p.StarId==home.Id).EnvironmentId;}
        Assert.Null(CombatInitializer.Initialize(g.Content,s,true));Assert.Equal(candidates.Min(st=>st.Id),Assert.Single(s.Combat!.Pockets).Star);
    }
}
