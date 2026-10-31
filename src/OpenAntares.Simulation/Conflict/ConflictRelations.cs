using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictWarDecision(DiplomacyPair Pair,ImmutableArray<EmpireId> Declarers);
public sealed record ConflictDeclarationFailure(EmpireId Owner,EmpireId Other);
public static partial class ConflictRelations
{
    public static void Prepare(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        if(context.RelationsPrepared||context.FromTurn!=snapshot.Turn||candidate.Turn!=snapshot.Turn)throw new InvalidOperationException("Conflict relation phase already applied or changed.");
        context.RelationsPrepared=true;if(!Rules.HasConflict(snapshot.RulesVersion))return;
        ConflictPeace.Prepare(content,snapshot,candidate,context);
        context.FrozenBlockades=context.FrozenBlockades.Suppress(context.AcceptedPeace);
        var busy=candidate.Conflict!.Pairs.Where(p=>p.Relation==RelationKind.War&&!context.AcceptedPeace.Contains(p.Pair)).SelectMany(p=>new[]{p.Pair.Low,p.Pair.High}).ToHashSet();
        var wars=ImmutableArray.CreateBuilder<ConflictWarDecision>();var failed=ImmutableArray.CreateBuilder<ConflictDeclarationFailure>();
        foreach(var pair in candidate.Conflict.Pairs.OrderBy(p=>p.Pair))
        {
            if(pair.Declarations.Count==0)continue;
            var declarers=pair.Declarations.Select(i=>i.Owner).OrderBy(x=>x).ToImmutableArray();
            if(pair.Relation!=RelationKind.Peace||pair.TruceEndTurn is {} end&&end>snapshot.Turn||busy.Contains(pair.Pair.Low)||busy.Contains(pair.Pair.High))
            {foreach(var owner in declarers)failed.Add(new(owner,pair.Pair.Other(owner)));continue;}
            wars.Add(new(pair.Pair,declarers));busy.Add(pair.Pair.Low);busy.Add(pair.Pair.High);
        }
        context.NewWars=wars.ToImmutable();context.RejectedDeclarations=failed.ToImmutable();
        ConflictTreatyEffects.Prepare(content,snapshot,candidate,context);
    }
    public static void Finalize(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        if(!context.RelationsPrepared||context.RelationsFinalized||candidate.Turn!=(long)context.FromTurn+1)throw new InvalidOperationException("Conflict publication phase already applied or changed.");
        context.RelationsFinalized=true;if(!Rules.HasConflict(snapshot.RulesVersion))return;
        ConflictAttitudes.Age(content,snapshot,candidate);
        ConflictPeace.Finalize(content,candidate,context);
        foreach(var war in context.NewWars)
        {
            var pair=candidate.Conflict!.Pairs.Single(p=>p.Pair==war.Pair);pair.Relation=RelationKind.War;pair.WarTurn=candidate.Turn;pair.Declarers=war.Declarers.ToList();pair.Revision=checked(pair.Revision+1);
            ConflictAttitudes.Declared(candidate,pair,war.Declarers);
            foreach(var viewer in new[]{pair.Pair.Low,pair.Pair.High})RecordEvent(candidate,viewer,pair.Pair.Other(viewer),"war_declared","War is now effective. Armed vessels may be attacked; civilian vessels and colonies remain protected.",attention:true);
        }
        ConflictTreatyEffects.Finalize(content,candidate,context);
        foreach(var failed in context.RejectedDeclarations)RecordEvent(candidate,failed.Owner,failed.Other,"declaration_unavailable","The declaration could not take effect under the current conflict limit.",attention:true);
        foreach(var pair in candidate.Conflict!.Pairs)
        {
            foreach(var declaration in pair.Declarations)
            {
                var party=pair.Parties.Single(p=>p.Owner==declaration.Owner);party.PrivateRevision=checked(party.PrivateRevision+1);
                var owner=candidate.Conflict.Owners.Single(o=>o.Owner==declaration.Owner);owner.Revision=checked(owner.Revision+1);
            }
            pair.Declarations.Clear();
        }
    }
    internal static void RecordEvent(GameState state,EmpireId viewer,EmpireId other,string kind,string reason,StarId? star=null,bool attention=false)
    {
        var data=state.Conflict!.Owners.Single(o=>o.Owner==viewer);long id=data.NextEventId;data.NextEventId=checked(id+1);state.Conflict.NextEventId=checked(state.Conflict.NextEventId+1);
        data.Events.Add(new(id,state.Turn,viewer,other,kind,reason,star));if(data.Events.Count>256)data.Events.RemoveRange(0,data.Events.Count-256);
        if(attention)
        {
            string key=kind+"/"+other.Value;data.Notices.RemoveAll(n=>n.Key==key);data.Notices.Add(new(key,ConflictNoticeLevel.Attention,reason,other,star));
            if(data.Notices.Count>256)data.Notices.RemoveRange(0,data.Notices.Count-256);
        }
    }
}
