using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Colonization;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacySettlementTests
{
    [Fact] public void AccordActivatesDuringFounderTransit()
    {
        // Labelled pre-session founder/arrival supplement, not earned campaign evidence.
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var p=s.State.Diplomacy!.Pairs.Single();
        var target=s.State.Planets.First(x=>!s.State.Colonies.Any(c=>c.PlanetId==x.Id));var v=s.State.Vessels.First(x=>x.EmpireId==p.Pair.Low);
        var role=s.Content.Vessels.Values.First(r=>r.CanFoundColony&&r.EmbarkPopulation>0);v.RoleId=role.Id;v.CarriedPopulation=role.EmbarkPopulation;v.StationaryStarId=target.StarId;v.PendingFounding=target.Id;
        p.Agreements.Add(new(){Kind=AgreementKind.Settlement,Star=target.StarId,ActivatedTurn=0});
        var candidate=s.State.Clone();int cargo=v.CarriedPopulation;var events=new List<ExpansionEvent>();
        ColonizationResolver.Apply(s.Content,s.State,candidate,events);
        Assert.NotNull(candidate.FindVessel(v.Id));Assert.Equal(cargo,candidate.FindVessel(v.Id)!.CarriedPopulation);Assert.Equal(target.Id,candidate.FindVessel(v.Id)!.PendingFounding);
        Assert.Contains(v.Id,candidate.Diplomacy!.BlockedFounders);Assert.Contains(events,e=>e.Reason=="diplomacy_settlement_accord");
        var frame=DiplomacyFrame.Capture(s.Content,s.State);Assert.True(DiplomacyRules.BlocksFounding(frame,p.Pair.Low,target.StarId));Assert.True(DiplomacyRules.BlocksFounding(frame,p.Pair.High,target.StarId));Assert.False(DiplomacyRules.BlocksFounding(frame,new EmpireId(999),target.StarId));
        candidate.Diplomacy.Pairs.Single().Agreements.Clear();var after=candidate.Clone();ColonizationResolver.Apply(s.Content,candidate,after,new());Assert.Null(after.FindVessel(v.Id));
    }
    [Fact] public void NewlyAcceptedAccordDoesNotBlockOutgoingBoundary()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var p=s.State.Diplomacy!.Pairs.Single();var star=s.State.Stars[0].Id;
        p.Agreements.Add(new(){Kind=AgreementKind.Settlement,Star=star,ActivatedTurn=1});
        Assert.False(DiplomacyRules.BlocksFounding(DiplomacyFrame.Capture(s.Content,s.State),p.Pair.Low,star));
        s.State.Turn=1;Assert.True(DiplomacyRules.BlocksFounding(DiplomacyFrame.Capture(s.Content,s.State),p.Pair.Low,star));
    }
    [Fact] public void BothSidesConsentUsingTheirOwnCurrentObservation()
    {
        // Labelled shared-system ownership fixture; runtime campaigns must earn both colonies.
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create(humans:2));
        var group=s.State.Planets.GroupBy(p=>p.StarId).First(g=>g.Count()>=2);var worlds=group.Take(2).ToArray();
        var template=s.State.FindPlanet(s.State.Colonies[0].PlanetId)!;
        for(int i=0;i<2;i++)
        {
            worlds[i].PlanetTypeId=template.PlanetTypeId;worlds[i].EnvironmentId=template.EnvironmentId;
            s.State.Colonies[i].PlanetId=worlds[i].Id;
            foreach(var v in s.State.Vessels.Where(v=>v.EmpireId==s.State.Colonies[i].EmpireId))v.StationaryStarId=group.Key;
        }
        foreach(var e in s.State.Empires){e.Surveys.Clear();e.Fleets!.SurveyCredits.Clear();}ObservationUpdater.Refresh(s.State,0);Assert.Empty(GameStateValidator.Validate(s.Content,s.State));
        var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;
        Assert.True(DiplomacyCommandTests.Act(s,a,new DiplomacyAction.OfferSettlement(group.Key)).Accepted);
        var id=s.State.Diplomacy!.Pairs.Single().Pending!.Id;Assert.True(DiplomacyCommandTests.Act(s,b,new DiplomacyAction.Answer(id,true,null)).Accepted);
        var candidate=s.State.Clone();DiplomacyResolver.ResolveOffers(s.Content,s.State,candidate);candidate.Turn=1;ObservationUpdater.Refresh(candidate,1);
        Assert.Empty(GameStateValidator.Validate(s.Content,candidate));Assert.True(DiplomacyRules.BlocksFounding(DiplomacyFrame.Capture(s.Content,candidate),a,group.Key));
    }}
