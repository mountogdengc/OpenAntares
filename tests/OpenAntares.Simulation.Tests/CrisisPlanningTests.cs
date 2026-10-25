using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public sealed class CrisisPlanningTests
{
    [Fact]
    public void ManualDefaultNeverActsAndOffOrLegacyNeverPlans()
    {
        var g=CrisisFixtures.Active();var manual=CrisisTurnCoordinator.Prepare(g.Content,g.State,g.State.Empires[0].Id);Assert.True(manual.Succeeded);Assert.Same(g.State,manual.State);
        foreach(var old in new[]{CrisisFixtures.Game(enabled:false),ColonyFixtures.Game(),CompatibilityFixtures.LoadRules4()})
        {
            bool called=false;var result=CrisisTurnCoordinator.Prepare(old.Content,old.State,old.State.Empires[0].Id,(current,submit,ai)=>{called=true;throw new InvalidOperationException();});Assert.True(result.Succeeded);Assert.Same(old.State,result.State);Assert.False(called);
        }
    }
    [Fact]
    public void ManualOverridesOnlyItsCrisisChannel()
    {
        var g=CrisisFixtures.Active();var source=g.State.Colonies[0];source.Development!.Template=ColonyFixtures.Snapshot(g.Content,"industrial");source.Development.WorkforceEnabled=true;source.Development.ConstructionEnabled=true;
        var other=source.Clone();other.Id=new(g.State.AllocateId());other.PlanetId=new(7);other.Development=new();g.State.Colonies.Add(other);ObservationUpdater.Refresh(g.State,g.State.Turn);var owner=source.EmpireId;g.State.Empires[0].Crisis!.Authority=CrisisAuthority.ContainAndResearch;
        var q=CommandProcessor.Execute(g.Content,g.State,new SetQuarantineCommand(owner,g.State.Turn,CrisisQueries.PreviewQuarantine(g.Content,g.State,owner,ImmutableArray.Create(source.Id),true,true)));Assert.True(q.Accepted);
        var share=CommandProcessor.Execute(g.Content,q.State,new SetCureShareCommand(owner,g.State.Turn,1,25));Assert.True(share.Accepted);
        var prepared=CrisisTurnCoordinator.Prepare(g.Content,share.State,owner);Assert.True(prepared.Succeeded);Assert.True(prepared.State.FindColony(other.Id)!.Crisis!.Quarantined);Assert.False(prepared.State.FindColony(other.Id)!.Crisis!.PolicyPaused);Assert.True(prepared.State.FindColony(source.Id)!.Crisis!.PolicyPaused);Assert.Equal(25,prepared.State.Empires[0].Crisis!.Episode!.ResearchShare);Assert.True(prepared.State.FindColony(source.Id)!.Development!.WorkforceEnabled);
        var resume=CommandProcessor.Execute(g.Content,prepared.State,new ResumeCrisisPolicyCommand(owner,g.State.Turn,ImmutableArray.Create(source.Id),true));Assert.True(resume.Accepted);
        var resumed=CrisisTurnCoordinator.Prepare(g.Content,resume.State,owner);Assert.True(resumed.Succeeded);Assert.Equal(50,resumed.State.Empires[0].Crisis!.Episode!.ResearchShare);
    }
    [Fact]
    public void ExistingPreventiveKeepUntilOrderedIsNotSilentlyAutoReleased()
    {
        var g=CrisisFixtures.Active(quarantine:true);g.State.Empires[0].Crisis!.Authority=CrisisAuthority.Contain;
        var result=CrisisTurnCoordinator.Prepare(g.Content,g.State,g.State.Empires[0].Id);Assert.True(result.Succeeded);Assert.Same(g.State,result.State);Assert.Null(result.State.Colonies[0].Crisis!.ReleaseEpisode);
    }
    [Fact]
    public void ContextOwnFactsCannotAliasLaterSourceChanges()
    {
        var g=MultiWithCase();var context=CrisisPlanningQueries.Create(g.Content,g.State,g.State.Empires[0].Id);string before=JsonSerializer.Serialize(context.View);
        g.State.Colonies[0].Crisis!.Case!.Severity=99;g.State.Empires[0].Crisis!.Episode!.CureProgress=999;g.State.Empires[0].Crisis!.Random.NextUInt32();g.State.Empires[0].Crisis!.PendingReceipts.Clear();
        Assert.Equal(before,JsonSerializer.Serialize(context.View));
    }
    [Fact]
    public void OwnPlansIgnoreHiddenRivalHealthResearchWaiverAndCargo()
    {
        var g=MultiWithCase();var a=PlanCommands(g);var foreign=g.State.Empires[1];var colony=g.State.Colonies[1];var location=CrisisQueries.Location(g.State,colony);foreign.Crisis!.NextEpisodeNumber=2;foreign.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=location,SourcePopulation=6,CureCost=1200,ResearchShare=100};foreign.Crisis.RequireAcknowledgement=false;foreign.ResearchReserve=long.MaxValue;foreign.Crisis.Random.NextUInt32();colony.Crisis!.Case=new(){EpisodeId=1,Stage=CrisisStage.Active,Severity=80,AttentionRevision=2,WaivedRevision=2,Origin=location};
        g.State.Vessels.Add(new(){Id=new(g.State.AllocateId()),EmpireId=foreign.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=new(6),Crisis=new(){CarrierEpisode=1}});ObservationUpdater.Refresh(g.State,g.State.Turn);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));
        var b=PlanCommands(g);Assert.Equal(a,b);Assert.Contains("SetQuarantineCommand",a);
    }
    [Theory][InlineData("foreign")][InlineData("stale")][InlineData("purge")][InlineData("waiver")][InlineData("rejected_after_change")][InlineData("wrong_count")][InlineData("exception")][InlineData("budget")]
    public void PreparationFaultsAndScopeViolationsRollBack(string kind)
    {
        var g=MultiWithCase();var owner=g.State.Empires[0].Id;string before=SaveGame.Serialize(g.Content,g.State);
        var result=CrisisTurnCoordinator.Prepare(g.Content,g.State,owner,(current,submit,ai)=>
        {
            var view=current().View;
            if(kind=="foreign"){submit(new SetCureShareCommand(new(999),view.Turn,1,50));return new(true,1);}
            if(kind=="stale"){submit(new SetCureShareCommand(owner,view.Turn-1,1,50));return new(true,1);}
            if(kind=="purge"){submit(new PurgeColonyCommand(owner,view.Turn,CrisisQueries.PreviewPurge(g.Content,g.State,owner,g.State.Colonies[0].Id)));return new(true,1);}
            if(kind=="waiver"){submit(new SetCrisisAcknowledgementCommand(owner,view.Turn,false));return new(true,1);}
            if(kind=="exception")throw new InvalidOperationException("private detail");
            if(kind=="budget"){for(int i=0;i<20;i++)submit(new SetCureShareCommand(owner,view.Turn,1,50));return new(true,20);}
            bool first=submit(new SetCureShareCommand(owner,view.Turn,1,50));Assert.True(first);
            if(kind=="wrong_count")return new(true,0);
            submit(new SetCureShareCommand(owner,view.Turn,99,50));return new(true,2);
        });
        Assert.False(result.Succeeded);Assert.Same(g.State,result.State);Assert.Equal(before,SaveGame.Serialize(g.Content,result.State));
    }
    [Fact]
    public void GovernorAndRivalPrivateForecastsUseActualCrisisFactorsAndPorts()
    {
        var g=CrisisFixtures.Active(quarantine:true);var c=g.State.Colonies[0];c.Workforce=new(2,2,2);c.Development!.Template=ColonyFixtures.Snapshot(g.Content,"industrial");c.Development.WorkforceEnabled=true;
        var view=EmpireViewProjector.Project(g.Content,g.State,c.EmpireId);var governor=GovernorPlanningQueries.Create(g.Content,view).Forecast(c.Id,c.Workforce,null)!;
        var rival=OpenAntares.Simulation.Rivals.RivalPlanningQueries.Create(g.Content,g.State,c.EmpireId).Forecast(c.Id,c.Workforce,null)!;
        Assert.Equal(240,governor.NetProduction.Total);Assert.Equal(240,rival.NetProduction.Total);
        var cargo=GovernorPlanningQueries.Create(g.Content,view).Forecast(c.Id,c.Workforce,new("vessel","harl_settler"))!;Assert.Equal("quarantine",cargo.Production.BlockedReasonCode);
    }
    [Fact]
    public void SingleColonyPreviewSeesPeerPortClosureAndReportsQuarantineWait()
    {
        var g=CrisisFixtures.Active();var c=g.State.Colonies[0];
        var peer=c.Clone();peer.Id=new(g.State.AllocateId());peer.PlanetId=new(g.State.AllocateId());peer.Crisis!.Case=null;peer.Crisis.Quarantined=true;
        var planet=g.State.Planets.Single(p=>p.Id==c.PlanetId).Clone();planet.Id=peer.PlanetId;planet.Orbit=4;g.State.Planets.Add(planet);g.State.Colonies.Add(peer);
        c.ProjectKind="vessel";c.ProjectId="harl_settler";ObservationUpdater.Refresh(g.State,g.State.Turn);
        var view=EmpireViewProjector.Project(g.Content,g.State,c.EmpireId);
        var preview=GovernorPlanningQueries.Preview(g.Content,view,c.Id,ColonyFixtures.Snapshot(g.Content,"industrial"),true,true)!;
        Assert.Contains(preview.Issues,i=>i.Code=="quarantine_wait");
    }
    [Fact]
    public void PolicyGroupsContainmentAndInspectsCargoUsingFreshFacts()
    {
        var g=CrisisFixtures.Active();var owner=g.State.Empires[0];owner.Crisis!.Authority=CrisisAuthority.ContainAndResearch;
        var second=g.State.Colonies[0].Clone();second.Id=new(g.State.AllocateId());second.PlanetId=new(7);second.Development=new();g.State.Colonies.Add(second);
        g.State.Vessels.Add(new(){Id=new(g.State.AllocateId()),EmpireId=owner.Id,RoleId="harl_settler",CarriedPopulation=1,StationaryStarId=Fixtures.HomeStar,Crisis=new(){CarrierEpisode=1}});
        ObservationUpdater.Refresh(g.State,g.State.Turn);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));string before=SaveGame.Serialize(g.Content,g.State);
        var result=CrisisTurnCoordinator.Prepare(g.Content,g.State,owner.Id);Assert.True(result.Succeeded);
        Assert.All(result.State.Colonies,c=>Assert.True(c.Crisis!.Quarantined));
        Assert.Equal(50,result.State.Empires[0].Crisis!.Episode!.ResearchShare);
        Assert.Equal(g.State.Turn+1,result.State.Vessels.Last().Crisis!.InspectionDueTurn);
        Assert.Equal(before,SaveGame.Serialize(g.Content,g.State));
    }
    internal static LoadedGame MultiWithCase(bool warning=false)
    {
        var g=CrisisFixtures.TwoHumansAndAi();g.State.Turn=warning?20:22;var owner=g.State.Empires[0];var c=g.State.Colonies[0];var location=CrisisQueries.Location(g.State,c);owner.Crisis!.Authority=CrisisAuthority.ContainAndResearch;owner.Crisis.NextEpisodeNumber=2;owner.Crisis.Episode=new(){Id=1,FamilyId="ash_bloom",Source=location,SourcePopulation=6,CureCost=1200};c.Crisis!.Case=new(){EpisodeId=1,Stage=warning?CrisisStage.Warning:CrisisStage.Active,GraceRemaining=warning?2:0,Severity=warning?0:20,AttentionRevision=warning?1:2,HandledRevision=warning?0:2,Origin=location};ObservationUpdater.Refresh(g.State,g.State.Turn);Assert.Empty(GameStateValidator.Validate(g.Content,g.State));return g;
    }
    private static string PlanCommands(LoadedGame g)
    {
        var working=g.State.Clone();var commands=new List<string>();var owner=working.Empires[0].Id;
        var result=CrisisPlanner.Plan(()=>CrisisPlanningQueries.Create(g.Content,working,owner),command=>{commands.Add(command.GetType().Name+":"+JsonSerializer.Serialize(command,command.GetType()));var outcome=CommandProcessor.ExecuteCrisisAutomatic(g.Content,working,(CrisisCommand)command);working=outcome.State;return outcome.Accepted;},false);
        Assert.True(result.Succeeded);return string.Join("\n",commands);
    }
}
