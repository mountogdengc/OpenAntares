using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class CrisisIntegrationTests
{
    internal static ContentSet CoreWithApprovedPlague()
    {
        var core=Directory.GetFiles(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../content/core")),"*.json").OrderBy(x=>x,StringComparer.Ordinal).Select(path=>new ContentSource(Path.GetFileName(path),File.ReadAllText(path)));
        var result=ContentLoader.Load(core,requireSpecies:true,requireRivals:true,requireDevelopment:true,requireCrises:true);
        Assert.True(result.Succeeded,string.Join("; ",result.Errors));return result.Content!;
    }
    [Theory][InlineData("harl")][InlineData("peldra")][InlineData("sulan")][InlineData("wenth")]
    public void GeneratedSeed42HasSpontaneousHumanWarningAt20(string species)
    {
        var content=CoreWithApprovedPlague();var generated=GalaxyGenerator.CreateNewGame(content,new("small",42,6,species,1,"peldra",true));Assert.True(generated.Succeeded,generated.Error);
        var session=new GameSession(content,generated.State!);var owner=session.State.Empires.Single(e=>e.Controller==ControllerKind.Human).Id;
        Assert.Equal(0,session.State.Turn);Assert.Empty(session.View(owner).Crisis!.Cases);
        for(int i=0;i<20;i++){var outcome=session.Execute(new SetReadyCommand(owner,session.State.Turn,true));Assert.True(outcome.Accepted,outcome.Rejection?.Message);}
        Assert.Equal(20,session.State.Turn);var view=session.View(owner).Crisis!;var onset=Assert.Single(view.Cases);Assert.Equal(CrisisStage.Warning,onset.Stage);Assert.Equal(2,onset.GraceRemaining);Assert.Equal(600+100*onset.Population,view.Episode!.CureCost);
    }
}

