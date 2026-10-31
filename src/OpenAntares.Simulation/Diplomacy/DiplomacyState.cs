using System;
using System.Collections.Generic;
using System.Text.Json;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public readonly record struct DiplomacyPair(EmpireId Low,EmpireId High):IComparable<DiplomacyPair>
{
    public static DiplomacyPair For(EmpireId a,EmpireId b)=>a.CompareTo(b)<0?new(a,b):new(b,a);
    public int CompareTo(DiplomacyPair b){int c=Low.CompareTo(b.Low);return c!=0?c:High.CompareTo(b.High);}
    public bool Contains(EmpireId e)=>e==Low||e==High;
    public EmpireId Other(EmpireId e)=>e==Low?High:Low;
}
public readonly record struct OfferId(long Value);
public enum AgreementKind {Settlement,Trade}
public sealed record HubCommitment(ColonyId Hub,long Reserved);
public sealed record HubOrigin(ColonyId Hub,EmpireId Owner,int ActivationTurn);
public sealed class AttitudeState {public int Streak {get;set;} public int? LastCounterpartNoticeTurn {get;set;}}
public sealed class DiplomaticParty
{
    public EmpireId Owner {get;set;} public long PrivateRevision {get;set;}=1;
    public AttitudeState Attitude {get;set;}=new(); public int NextAiOfferTurn {get;set;}
}
public sealed class DiplomacyAnswer {public bool Accept {get;set;} public HubCommitment? Hub {get;set;}}
public sealed class DiplomacyOffer
{
    public OfferId Id {get;set;} public EmpireId Proposer {get;set;} public EmpireId Recipient {get;set;}
    public AgreementKind Kind {get;set;} public int CreatedTurn {get;set;} public int ExpiryTurn {get;set;}
    public StarId? Star {get;set;} public HubCommitment? ProposerHub {get;set;} public DiplomacyAnswer? Answer {get;set;}
    public bool Withdraw {get;set;}
}
public sealed class DiplomacyAgreement
{
    public AgreementKind Kind {get;set;} public StarId? Star {get;set;} public int ActivatedTurn {get;set;}
    public HubOrigin? LowHub {get;set;} public HubOrigin? HighHub {get;set;}
    public int? LowNoticeTurn {get;set;} public int? HighNoticeTurn {get;set;} public int? EndTurn {get;set;}
}
public sealed record DiplomacyEvent(EmpireId Viewer,EmpireId Other,int Turn,string Kind,StarId? Star,ColonyId? OwnHub,string Reason) {public long Paid {get;init;}}
public sealed class DiplomacyPairState
{
    public DiplomacyPair Pair {get;set;} public long NextOfferId {get;set;}=1; public long PublicRevision {get;set;}=1;
    public List<DiplomaticParty> Parties {get;set;}=new(); public DiplomacyOffer? Pending {get;set;}
    public List<DiplomacyAgreement> Agreements {get;set;}=new(); public List<DiplomacyEvent> LastResults {get;set;}=new();
}
public sealed class DiplomacyState
{
    public List<DiplomacyPairState> Pairs {get;set;}=new();
    public DiplomacyState Clone()=>JsonSerializer.Deserialize<DiplomacyState>(JsonSerializer.Serialize(this))!;
}
