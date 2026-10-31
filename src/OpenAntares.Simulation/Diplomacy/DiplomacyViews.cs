using System.Collections.Immutable;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public sealed record AttitudeView(int Score,string Label,int Streak,int Cooperation,int Penalty,int MemoryRemaining)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)] public int DeclarationGrievance {get;init;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)] public int GrievancePeaceRemaining {get;init;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] public long? GrievanceExpiresTurn {get;init;}
}
public sealed record DiplomacyOfferView(OfferId Id,EmpireId Proposer,EmpireId Recipient,AgreementKind Kind,int CreatedTurn,int ExpiryTurn,StarId? Star,bool OwnAnswered,bool? OwnAccept,ColonyId? OwnHub);
public sealed record DiplomacyAgreementView(AgreementKind Kind,StarId? Star,int ActivatedTurn,long FirstTerminationTurn,int? EndTurn,ColonyId? OwnHub,int WarmupRemaining,bool OwnNotice);
public sealed record OwnDiplomacyHub(ColonyId Id,HubEligibility NewCommitment,HubEligibility? ExistingCommitment);
public sealed record ContactDiplomacyView(ContactView Identity,long PublicRevision,long PrivateRevision,AttitudeView OwnAttitude,AttitudeView OtherAttitude,DiplomacyOfferView? Pending,ImmutableArray<DiplomacyAgreementView> Agreements,int NextAiOfferTurn,ImmutableArray<DiplomacyEvent> Results);
public sealed record DiplomacyView(ImmutableArray<ContactDiplomacyView> Contacts,ImmutableArray<OwnDiplomacyHub> Hubs);
