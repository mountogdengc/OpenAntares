using System.Linq;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class DiplomacyStateTests
{
    [Fact] public void LiveMissingHubEndsWithoutValidationDeadlock()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var pair=s.State.Diplomacy!.Pairs.Single();
        s.State.Turn=1;var low=pair.Pair.Low;var high=pair.Pair.High;
        var historical=s.State.Colonies.First(c=>c.EmpireId==low).Id;
        pair.Agreements.Add(new(){Kind=AgreementKind.Trade,ActivatedTurn=1,LowHub=new(historical,low,1),HighHub=new(s.State.Colonies.First(c=>c.EmpireId==high).Id,high,1)});
        s.State.Colonies.RemoveAll(c=>c.Id==historical);
        Assert.Empty(DiplomacyStateValidator.Validate(s.Content,s.State));pair.Agreements.Clear();
        pair.NextOfferId=2;pair.Pending=new(){Id=new(1),Proposer=low,Recipient=high,Kind=AgreementKind.Trade,CreatedTurn=1,ExpiryTurn=4,ProposerHub=new(historical,500)};
        Assert.NotEmpty(DiplomacyStateValidator.Validate(s.Content,s.State));
    }
    [Fact] public void PairClonesDeeplyAndMalformedCountersReject()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());var clone=s.State.Clone();
        Assert.NotSame(s.State.Diplomacy,clone.Diplomacy);clone.Diplomacy!.Pairs[0].Parties[0].Attitude.Streak=7;
        Assert.Equal(0,s.State.Diplomacy!.Pairs[0].Parties[0].Attitude.Streak);
        clone.Diplomacy.Pairs[0].NextOfferId=0;Assert.NotEmpty(DiplomacyStateValidator.Validate(s.Content,clone));
    }
    [Fact] public void AsymmetricContactsAndDuplicatePairsReject()
    {
        var s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());s.State.Empires[0].Contacts.Clear();Assert.NotEmpty(DiplomacyStateValidator.Validate(s.Content,s.State));
        s=DiplomacyFixtures.WithContact(DiplomacyFixtures.Create());s.State.Diplomacy!.Pairs.Add(s.State.Diplomacy.Pairs[0]);Assert.NotEmpty(DiplomacyStateValidator.Validate(s.Content,s.State));
    }
}
