using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Appearance;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class PlanetAppearanceTests
{
    [Theory][InlineData(0UL,1,7,415410781872786579UL)][InlineData(42UL,7,7,1201488599727174231UL)][InlineData(42UL,8,6,1201489286921941863UL)][InlineData(42UL,2147483647,2,1199460841799593817UL)][InlineData(18446744073709551615UL,1,2,1678581143798014196UL)]
    public void StableVectorsDoNotAdvanceRandomOrIds(ulong seed,int id,int variant,ulong surface) {
        var g=ColonyFixtures.Game();string before=JsonSerializer.Serialize(g.State);var previous=CultureInfo.CurrentCulture;
        try{CultureInfo.CurrentCulture=new CultureInfo("ar-SA");Assert.Equal(new PlanetAppearanceDescriptor(1,variant,surface),PlanetAppearance.Create(seed,new(id)));}finally{CultureInfo.CurrentCulture=previous;}
        Assert.Equal(before,JsonSerializer.Serialize(g.State));
    }
    public static IEnumerable<object[]> SpeciesPairs=>from size in new[]{"small","medium","large"} from human in new[]{"harl","peldra","sulan","wenth"} from rival in new[]{"harl","peldra","sulan","wenth"} select new object[]{size,human,rival};
    [Theory][MemberData(nameof(SpeciesPairs))] public void GeneratedRules5PreservesRules4BaseFactsAndRng(string size,string human,string rival) {
        var content=ColonyFixtures.Content();var old=GalaxyGenerator.CreateNewGame(CompatibilityFixtures.Rules4Content(),new(size,42,4,human,1,rival));var current=GalaxyGenerator.CreateNewGame(content,new(size,42,5,human,1,rival));Assert.True(old.Succeeded,old.Error);Assert.True(current.Succeeded,current.Error);var state=current.State!;
        Assert.All(state.Planets,p=>Assert.Equal(PlanetAppearance.Create(42,p.Id),p.Appearance));Assert.All(state.Colonies,c=>Assert.NotNull(c.Development));Assert.All(state.Empires,e=>Assert.NotNull(e.Development));
        var reduced=state.Clone();reduced.RulesVersion=4;foreach(var e in reduced.Empires){e.Development=null;foreach(var o in e.Surveys)o.Planets=o.Planets.Select(p=>p with{Appearance=null}).ToList();}foreach(var c in reduced.Colonies)c.Development=null;foreach(var p in reduced.Planets)p.Appearance=null;
        Assert.Equal(JsonSerializer.Serialize(old.State),JsonSerializer.Serialize(reduced));
    }
    [Fact] public void SoloAndRenamedContentKeepStableIdentities() {
        var c=ColonyFixtures.Content();var g=GalaxyGenerator.CreateNewGame(c,new("small",42,5,"harl"));Assert.True(g.Succeeded,g.Error);var first=g.State!.Planets.Select(p=>p.Appearance).ToArray();
        c=c with{Buildings=c.Buildings.ToImmutableSortedDictionary(p=>p.Key,p=>p.Value with{Name="Modded "+p.Value.Name},StringComparer.Ordinal)};
        var second=GalaxyGenerator.CreateNewGame(c,new("small",42,5,"harl"));Assert.True(second.Succeeded,second.Error);Assert.Equal(first,second.State!.Planets.Select(p=>p.Appearance));Assert.Equal(first[0],PlanetAppearance.Create(42,g.State.Planets[0].Id));Assert.Equal(first[0],g.State.Planets[0].Appearance);
    }
}
