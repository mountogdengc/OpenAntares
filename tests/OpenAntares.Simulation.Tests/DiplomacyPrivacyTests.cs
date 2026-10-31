using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Crises;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyPrivacyTests
{
    [Fact] public void HiddenPartnerMutationDoesNotChangeOwnToken()
    {
        var s=DiplomacyCommandTests.Funded();var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;
        var before=DiplomacyTokens.PrivateRevision(s.Content,s.State,a,b);s.State.Colonies.Single(c=>c.EmpireId==b).ProductionReserve+=10000;
        Assert.Equal(before,DiplomacyTokens.PrivateRevision(s.Content,s.State,a,b));
        s.State.Colonies.Single(c=>c.EmpireId==a).ProductionReserve++;
        Assert.NotEqual(before,DiplomacyTokens.PrivateRevision(s.Content,s.State,a,b));
    }
    [Fact] public void PreviewBindsOwnMaterialAndWarnsAboutOnlyItsDisclosure()
    {
        var s=DiplomacyCommandTests.Funded();var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;var hub=s.State.Colonies.Single(c=>c.EmpireId==a);
        var view=s.View(a);var preview=DiplomacyQueries.Preview(s.Content,view,b,new DiplomacyAction.OfferTrade(hub.Id));Assert.True(preview.Allowed);Assert.Equal(8,preview.BreakEvenBoundaries);
        var old=new DiplomacyCommand(a,0,b,preview.PublicRevision,preview.PrivateRevision,new DiplomacyAction.OfferTrade(hub.Id));hub.ProductionReserve++;
        Assert.False(s.Execute(old).Accepted);var accord=DiplomacyQueries.Preview(s.Content,s.View(a),b,new DiplomacyAction.OfferSettlement(s.State.FindPlanet(hub.PlanetId)!.StarId));Assert.True(accord.Allowed);Assert.Contains(accord.Consequences,t=>t.Contains("reveals",StringComparison.Ordinal));
    }
    [Fact] public void PartnerQueuedAnswerAndHubStayPrivate()
    {
        var s=DiplomacyOfferResolutionTests.Offered(false);var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;
        var before=s.View(a).Diplomacy!.Contacts.Single();var own=s.State.Colonies.Single(c=>c.EmpireId==a).Id;var foreign=s.State.Colonies.Single(c=>c.EmpireId==b).Id;
        Assert.True(DiplomacyCommandTests.Act(s,b,new DiplomacyAction.Answer(before.Pending!.Id,true,foreign)).Accepted);
        var after=s.View(a).Diplomacy!.Contacts.Single();Assert.Equal(before.PrivateRevision,after.PrivateRevision);Assert.Equal(before.PublicRevision,after.PublicRevision);Assert.False(after.Pending!.OwnAnswered);Assert.Null(after.Pending.OwnAccept);Assert.Equal(own,after.Pending.OwnHub);
        Assert.All(s.View(a).Diplomacy!.Hubs,h=>Assert.NotEqual(foreign,h.Id));
    }
    [Fact] public void ThirdEmpireOfferCannotEnterUnrelatedContactView()
    {
        var s=DiplomacyCommandTests.Funded();var state=s.State;var a=state.Empires[0].Id;var b=state.Empires[1].Id;
        // Valid labelled three-empire supplement with a retired crew appointment and no third assets.
        var third=state.Empires[0].Clone();third.Id=new(state.AllocateId());third.Ready=false;third.Contacts.Clear();third.Surveys.Clear();
        third.Crisis=new(){Random=CrisisInitializer.CreateRandom(state.GalaxySeed!.Value,third.Id)};
        third.Fleets!.RecoveryRandom=FleetInitializer.RecoveryRandom(state.GalaxySeed.Value,third.Id);third.Fleets.SurveyCredits.Clear();
        foreach(var o in third.Fleets.Officers){o.Status=OfficerStatus.Retired;o.PostedColony=null;o.Flagship=null;o.Request=null;o.RecoveryColony=null;o.AvailableAtTurn=null;o.FinalSummary="Retired before this labelled fixture.";}
        state.Empires.Add(third);state.Empires[1].Contacts.Add(new(third.Id,third.SpeciesId!,0));third.Contacts.Add(new(b,state.Empires[1].SpeciesId!,0));
        state.Diplomacy!.Pairs.Add(new(){Pair=DiplomacyPair.For(b,third.Id),Parties=new(){new(){Owner=b},new(){Owner=third.Id}}});
        Assert.Empty(GameStateValidator.Validate(s.Content,state));var before=s.View(a).Diplomacy!.Contacts.Single();
        var pair=state.Diplomacy.Pairs.Single(p=>p.Pair.Contains(third.Id));var command=new DiplomacyCommand(b,0,third.Id,pair.PublicRevision,DiplomacyTokens.PrivateRevision(s.Content,state,b,third.Id),new DiplomacyAction.OfferTrade(state.Colonies.Single(c=>c.EmpireId==b).Id));Assert.True(s.Execute(command).Accepted);
        var after=s.View(a).Diplomacy!;Assert.Single(after.Contacts);Assert.DoesNotContain(after.Contacts,c=>c.Identity.OtherEmpireId==third.Id);Assert.Equal(before.PrivateRevision,after.Contacts[0].PrivateRevision);Assert.Null(after.Contacts[0].Pending);
    }

    [Fact] public void AccordPreviewNamesTheDisclosedSystem()
    {
        var s=DiplomacyCommandTests.Funded();var a=s.State.Empires[0].Id;var b=s.State.Empires[1].Id;var view=s.View(a);var star=view.Stars.Single(st=>st.Chart.Id==view.Colonies[0].StarId);
        var preview=DiplomacyQueries.Preview(s.Content,view,b,new DiplomacyAction.OfferSettlement(star.Chart.Id));Assert.Contains(star.Chart.Name,preview.Terms);
        Assert.True(DiplomacyCommandTests.Act(s,a,new DiplomacyAction.OfferSettlement(star.Chart.Id)).Accepted);var offer=s.View(b).Diplomacy!.Contacts.Single().Pending!;
        var recipient=DiplomacyQueries.Preview(s.Content,s.View(b),a,new DiplomacyAction.Answer(offer.Id,true,null));Assert.Contains(star.Chart.Name,recipient.Terms);
    }
}
