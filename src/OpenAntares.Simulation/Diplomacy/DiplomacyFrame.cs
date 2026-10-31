using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public sealed record EffectiveAgreement(DiplomacyPair Pair,AgreementKind Kind,StarId? Star,HubOrigin? LowHub,HubOrigin? HighHub,int ActivatedTurn,int? EndTurn);
public sealed record DiplomacyFrame(int Turn,ImmutableArray<EffectiveAgreement> Agreements)
{
    public static DiplomacyFrame Capture(ContentSet content,GameState state)=>new(state.Turn,(state.Diplomacy?.Pairs.SelectMany(p=>p.Agreements.Where(a=>a.ActivatedTurn<=state.Turn).Select(a=>new EffectiveAgreement(p.Pair,a.Kind,a.Star,a.LowHub,a.HighHub,a.ActivatedTurn,a.EndTurn)))??Enumerable.Empty<EffectiveAgreement>()).ToImmutableArray());
}
