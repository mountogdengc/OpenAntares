using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class FleetProgressionTests
{
    [Theory]
    [InlineData(0,2)]
    [InlineData(10,1)]
    [InlineData(30,1)]
    public void NavigatorChangesActualTravelAndNeverHullStudyOrEscapeOdds(int xp,int turns)
    {
        var g=FleetFixtures.Game(amebas:true);var s=g.State;var owner=s.Empires[0].Id;var v=s.Vessels[0];var home=v.StationaryStarId!.Value;var h=s.Scouting!.Habitat!;
        var made=FleetCommandTests.Run(g,new FleetAction.Create("Navigator",ImmutableArray.Create(v.Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);var aboard=FleetOfficerTests.Run(g,new OfficerAction.AssignFlagship(new(1),new(1)));Assert.True(aboard.Accepted);g=new(g.Content,aboard.State);s=g.State;var officer=s.Empires[0].Fleets!.Officers[0];officer.Xp=xp;officer.Rank=xp>=30?2:xp>=10?1:0;
        s.FindStar(h.Star)!.X=0;s.FindStar(h.Star)!.Y=0;s.FindStar(home)!.X=301;s.FindStar(home)!.Y=0;v=s.FindVessel(v.Id)!;v.StationaryStarId=h.Star;v.AmebaEncounter=new(){Number=1,HabitatId=h.Id,ArrivalTurn=0,Stage=AmebaStage.Warning};s.Empires[0].Scouting!.NextEncounterNumber=2;s.Empires[0].Scouting!.KnownHabitats.Add(new(h.Id,h.Star,0));ObservationUpdater.Refresh(s,0);
        var moved=FleetCommandTests.Run(g,new FleetAction.MoveSelection(ImmutableArray<VesselId>.Empty,ImmutableArray.Create(new FleetId(1)),home));Assert.True(moved.Accepted,moved.Rejection?.Message);g=new(g.Content,moved.State);var view=ScoutQueries.Project(g.Content,g.State,owner);var row=Assert.Single(view.Vessels);Assert.Equal(turns,row.DepartureRoute!.Duration);Assert.Equal(75,row.StudyPercent);Assert.Equal(95,row.LateEscapePercent);Assert.Equal(300,row.Speed);
        var ack=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(owner,0,view.Attention));Assert.True(ack.Accepted);var result=TurnResolver.Resolve(g.Content,ack.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));if(turns==1)Assert.Equal(home,result.State.FindVessel(v.Id)!.StationaryStarId);else Assert.Equal(turns-1,result.State.FindVessel(v.Id)!.Transit!.RemainingTurns);
    }
    internal static Saves.LoadedGame Finish(Saves.LoadedGame g)
    {
        for(int i=0;i<12&&g.State.Empires[0].Fleets!.Requests.Last().Status!=RequestStatus.Complete;i++)
        {
            g.State.Colonies[0].ProductionReserve=1600;var resolved=TurnResolver.Resolve(g.Content,g.State);Assert.True(resolved.Succeeded,string.Join(";",resolved.Errors));g=new(g.Content,resolved.State);
        }
        Assert.Equal(RequestStatus.Complete,g.State.Empires[0].Fleets!.Requests.Last().Status);return g;
    }
    [Fact]
    public void FourNewHullsAndFourSingleRequestsBothAwardFour()
    {
        foreach(int count in new[]{1,4})
        {
            var g=FleetEconomyTests.Organizer(0,request:false);var o=g.State.Empires[0].Fleets!.Officers.Last();
            for(int i=0;i<count;i++){g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,count==1?4:1) with{Coordinator=o.Id});g=Finish(g);}
            var f=g.State.Empires[0].Fleets!;var final=f.Officers.Last();Assert.Equal(4,final.Xp);Assert.Equal(4,final.Career.NewHulls);Assert.Equal(count,final.Career.CompletedRequests);Assert.Equal(4,f.HullCredits.Count);
            var existing=f.Fleets[0];var ship=existing.Members[0];var disbanded=FleetCommandTests.Run(g,new FleetAction.Disband(existing.Id));Assert.True(disbanded.Accepted);g=new(g.Content,disbanded.State);
            g=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Coordinator=o.Id,AcceptedIdleVessels=ImmutableArray.Create(ship)});g=Finish(g);Assert.Equal(4,g.State.Empires[0].Fleets!.Officers.Last().Xp);Assert.Equal(4,g.State.Empires[0].Fleets!.HullCredits.Count);
        }
    }
    [Fact]
    public void RemovalCancellationAndUnassignedSurveyCannotBeClaimedLater()
    {
        var g=FleetEconomyTests.Organizer(0);var o=g.State.Empires[0].Fleets!.Officers.Last();var removed=FleetOfficerTests.Run(g,new OfficerAction.Remove(o.Id,g.State.Colonies[0].Id));Assert.True(removed.Accepted);g=Finish(new(g.Content,removed.State));Assert.Equal(0,g.State.Empires[0].Fleets!.Officers.Last().Xp);
        var second=FleetRequestTests.Accept(g,FleetPlanningTests.Draft(g,1) with{Coordinator=o.Id});var r=second.State.Empires[0].Fleets!.Requests.Last();var cancel=FleetRequestTests.Run(second,new AssemblyAction.Cancel(r.Id,r.Revision,RequestCancellation.KeepQueue));Assert.True(cancel.Accepted);second=new(second.Content,cancel.State);second.State.Colonies[0].ProductionReserve=1600;var resolved=TurnResolver.Resolve(second.Content,second.State);Assert.True(resolved.Succeeded);Assert.Equal(0,resolved.State.Empires[0].Fleets!.Officers.Last().Xp);
        var scout=Surveyor();var target=Arriving(scout);var f=scout.State.Empires[0].Fleets!;f.Fleets[0].Officer=null;f.Officers[0].Status=OfficerStatus.Posted;f.Officers[0].Flagship=null;f.Officers[0].PostedColony=scout.State.Colonies[0].Id;
        var arrived=TurnResolver.Resolve(scout.Content,scout.State);Assert.True(arrived.Succeeded);Assert.Contains(target,arrived.State.Empires[0].Fleets!.SurveyCredits);Assert.Equal(0,arrived.State.Empires[0].Fleets!.Officers[0].Xp);
        arrived.State.Empires[0].Fleets!.Officers[0].Status=OfficerStatus.Commanding;arrived.State.Empires[0].Fleets!.Officers[0].PostedColony=null;arrived.State.Empires[0].Fleets!.Officers[0].Flagship=arrived.State.Vessels[0].Id;arrived.State.Empires[0].Fleets!.Fleets[0].Officer=new(1);
        var later=TurnResolver.Resolve(scout.Content,arrived.State);Assert.True(later.Succeeded);Assert.Equal(0,later.State.Empires[0].Fleets!.Officers[0].Xp);
    }
    [Fact]
    public void CareerHistoryIsBoundedAndPermanentSummaryRetainsTotals()
    {
        var g=FleetFixtures.Game();for(int i=0;i<40;i++)
        {
            var changed=FleetOfficerTests.Run(g,new OfficerAction.SetDoctrine(new(1),i%2==0?OfficerDoctrine.LastWatch:OfficerDoctrine.RecoverCommand));Assert.True(changed.Accepted);var result=TurnResolver.Resolve(g.Content,changed.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));g=new(g.Content,result.State);
        }
        var officer=g.State.Empires[0].Fleets!.Officers[0];Assert.Equal(32,officer.Highlights.Count);Assert.Equal(10,officer.Details.Count);Assert.Equal(0,officer.Xp);Assert.Equal(0,officer.Career.XpEarned);
        var dismissed=FleetOfficerTests.Run(g,new OfficerAction.Dismiss(officer.Id));Assert.True(dismissed.Accepted,dismissed.Rejection?.Message);var retired=dismissed.State.Empires[0].Fleets!.Officers[0];Assert.Equal(OfficerStatus.Retired,retired.Status);Assert.NotNull(retired.FinalSummary);Assert.Equal(officer.Career,retired.Career);
    }
    // Labelled pre-session specialty/rank supplements; outcomes below are actually resolved.
    internal static Saves.LoadedGame Surveyor(int xp=0,bool amebas=false)
    {
        var g=FleetFixtures.Game(amebas:amebas);var f=g.State.Empires[0].Fleets!;var o=f.Officers[0];var p=g.Content.FleetCommand!.Profiles.Single(p=>p.CultureId==o.CultureId&&p.Specialty==OfficerSpecialty.Surveyor);
        o.ProfileId=p.Id;o.Specialty=p.Specialty;o.Name=p.Name;o.Pronouns=p.Pronouns;o.Personality=p.Personality;o.Xp=xp;o.Rank=xp>=30?2:xp>=10?1:0;f.ProfileCommissions.Clear();f.ProfileCommissions.Add(p.Id,1);f.CandidateProfiles=g.Content.FleetCommand.Profiles.Where(p=>p.CultureId==o.CultureId&&p.Specialty!=o.Specialty).Select(p=>p.Id).ToList();
        var made=FleetCommandTests.Run(g,new FleetAction.Create("Survey",ImmutableArray.Create(g.State.Vessels[0].Id)));Assert.True(made.Accepted);g=new(g.Content,made.State);var assigned=FleetOfficerTests.Run(g,new OfficerAction.AssignFlagship(new(1),new(1)));Assert.True(assigned.Accepted);return FleetFixtures.Valid(new(g.Content,assigned.State));
    }
    internal static StarId Arriving(Saves.LoadedGame g)
    {
        var s=g.State;var destination=s.Stars.First(star=>!s.Empires[0].Surveys.Any(ob=>ob.StarId==star.Id)&&star.Id!=s.Scouting?.Habitat?.Star).Id;var fleet=s.Empires[0].Fleets!.Fleets[0];var ship=s.FindVessel(fleet.Flagship)!;var origin=ship.StationaryStarId!.Value;
        int turns=FleetTravel.Duration(g.Content,s,s.Empires[0].Id,fleet.Id,destination);int launch=Math.Max(0,s.Turn-turns+1);s.Turn=checked(launch+turns-1);ship.Transit=new(origin,destination,turns,1);ship.StationaryStarId=null;fleet.Cohort=new(origin,destination,turns,launch,300);return destination;
    }
    [Theory]
    [InlineData(8,10,0)]
    [InlineData(28,30,100)]
    [InlineData(30,30,200)]
    public void SurveyPromotionUsesPreEventBonus(int xp,int after,long bonus)
    {
        var g=Surveyor(xp);var target=Arriving(g);long ordinary=EconomyCalculator.Calculate(g.Content,g.State).Empires[0].Research.Ending;
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));var f=result.State.Empires[0].Fleets!;Assert.Equal(after,f.Officers[0].Xp);Assert.Equal(ordinary+bonus,result.State.Empires[0].ResearchReserve);Assert.Contains(target,f.SurveyCredits);Assert.Equal(1,f.Officers[0].Career.FirstSurveys);
        var repeat=TurnResolver.Resolve(g.Content,result.State);Assert.True(repeat.Succeeded,string.Join(";",repeat.Errors));Assert.Equal(after,repeat.State.Empires[0].Fleets!.Officers[0].Xp);Assert.Equal(1,repeat.State.Empires[0].Fleets!.Officers[0].Career.FirstSurveys);
    }
    [Fact]
    public void SurveyDepositCannotCompleteResearchAtTheSameBoundary()
    {
        var g=Surveyor(28);Arriving(g);var empire=g.State.Empires[0];empire.ResearchTargetId="cultivation_methods";empire.ResearchReserve=g.Content.Technologies[empire.ResearchTargetId].Cost-100;foreach(var col in g.State.Colonies)col.Workforce=new(col.Population,0,0);
        var result=TurnResolver.Resolve(g.Content,g.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.DoesNotContain("cultivation_methods",result.State.Empires[0].KnownTechnologyIds);Assert.Equal(g.Content.Technologies["cultivation_methods"].Cost,result.State.Empires[0].ResearchReserve);
        var next=TurnResolver.Resolve(g.Content,result.State);Assert.True(next.Succeeded);Assert.Contains("cultivation_methods",next.State.Empires[0].KnownTechnologyIds);Assert.Equal(0,next.State.Empires[0].ResearchReserve);
    }
    [Fact]
    public void ConcurrentFirstSurveyCreditsLowestEligibleFlagshipOnce()
    {
        var g=Surveyor();var s=g.State;var f=s.Empires[0].Fleets!;var ship=s.Vessels[0];var extra=ship.Clone();extra.Id=new(s.AllocateId());extra.Fleet=new(2);s.Vessels.Add(extra);var o=f.Officers[0].Clone();o.Id=new(2);o.Flagship=extra.Id;f.Officers.Add(o);f.NextOfficerId=3;f.LifetimeCommissions=2;f.ProfileCommissions[o.ProfileId]=2;f.Fleets.Add(new(){Id=new(2),Name="Second",Members=new(){extra.Id},Flagship=extra.Id,Officer=o.Id});f.NextFleetId=3;
        var target=Arriving(g);extra.StationaryStarId=null;extra.Transit=ship.Transit;f.Fleets[1].Cohort=f.Fleets[0].Cohort;FleetFixtures.Valid(g);
        var result=TurnResolver.Resolve(g.Content,s);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Equal(2,result.State.Empires[0].Fleets!.Officers[0].Xp);Assert.Equal(0,result.State.Empires[0].Fleets!.Officers[1].Xp);Assert.Single(result.State.Empires[0].Fleets!.SurveyCredits,id=>id==target);
    }
    [Fact]
    public void StudyAddsEightXpAndOnlyOrdinarySixResearch()
    {
        var g=Surveyor(amebas:true);var s=g.State;var v=s.Vessels[0];var h=s.Scouting!.Habitat!;v.StationaryStarId=h.Star;v.AmebaEncounter=new(){Number=1,HabitatId=h.Id,ArrivalTurn=0,Stage=AmebaStage.Warning};var scout=s.Empires[0].Scouting!;scout.NextEncounterNumber=2;scout.KnownHabitats.Add(new(h.Id,h.Star,0));ObservationUpdater.Refresh(s,0);
        while(scout.RiskRandom!.Clone().NextInt(100)>=75){scout.RiskRandom.NextInt(100);scout.RiskRollCount++;}
        var study=CommandProcessor.Execute(g.Content,s,new SetAmebaRiskOrderCommand(s.Empires[0].Id,0,v.Id,1,1,AmebaRiskAction.Study));Assert.True(study.Accepted,study.Rejection?.Message);g=new(g.Content,study.State);var view=ScoutQueries.Project(g.Content,g.State,s.Empires[0].Id);Assert.Equal(75,view.Vessels[0].StudyPercent);Assert.Equal(95,view.Vessels[0].LateEscapePercent);Assert.Equal(600,view.Vessels[0].ResearchReward);
        var ack=CommandProcessor.Execute(g.Content,g.State,new AcknowledgeAmebaCommand(s.Empires[0].Id,0,view.Attention));Assert.True(ack.Accepted);long ordinary=EconomyCalculator.Calculate(g.Content,ack.State).Empires[0].Research.Ending;
        var reward=Assert.Single(ScoutQueries.Project(g.Content,ack.State,s.Empires[0].Id).Vessels).FleetConsequence!.Reward!;Assert.Equal(8,reward.XpAward);Assert.Equal(0,reward.SurveyResearch);string before=FleetFixtures.Bytes(ack.State);
        for(int i=0;i<25;i++)Assert.True(view.Attention.SequenceEqual(ScoutQueries.Project(g.Content,ack.State,s.Empires[0].Id).Attention));
        foreach(var phase in new[]{FleetResolutionPhase.Rewards,FleetResolutionPhase.Reports}){var failed=TurnResolver.ResolveFleet(g.Content,ack.State,p=>{if(p==phase)throw new InvalidOperationException("Late reward rollback");});Assert.False(failed.Succeeded);Assert.Same(ack.State,failed.State);Assert.Null(failed.Report);Assert.Equal(before,FleetFixtures.Bytes(ack.State));}
        var result=TurnResolver.Resolve(g.Content,ack.State);Assert.True(result.Succeeded,string.Join(";",result.Errors));Assert.Equal(8,result.State.Empires[0].Fleets!.Officers[0].Xp);Assert.Equal(ordinary+600,result.State.Empires[0].ResearchReserve);Assert.Equal(1,result.State.Empires[0].Fleets!.Officers[0].Career.Studies);Assert.Single(result.Report!.FleetEvents,e=>e.Kind=="ameba_study");Assert.Empty(result.State.Empires[0].Fleets!.PendingEvents);
    }
}
