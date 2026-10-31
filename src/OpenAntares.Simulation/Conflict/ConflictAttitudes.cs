using System;
using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictAttitudes
{
    internal static void Age(ContentSet content,GameState snapshot,GameState candidate)
    {
        foreach(var pair in candidate.Conflict!.Pairs)
        {
            var before=snapshot.Conflict!.Pairs.SingleOrDefault(p=>p.Pair==pair.Pair);
            if(before?.Relation!=RelationKind.Peace||pair.Relation!=RelationKind.Peace)continue;
            foreach(var party in pair.Parties.Where(p=>p.DeclarationGrievance))
            {party.GrievancePeaceBoundaries=Math.Min(content.Conflict!.GrievancePeaceLifetime,checked(party.GrievancePeaceBoundaries+1));if(party.GrievancePeaceBoundaries==content.Conflict.GrievancePeaceLifetime)party.DeclarationGrievance=false;}
        }
    }
    internal static void Declared(GameState state,ConflictPairState pair,ImmutableArray<EmpireId> declarers)
    {
        foreach(var party in pair.Parties.Where(p=>declarers.Contains(pair.Pair.Other(p.Owner))))
        {party.DeclarationGrievance=true;party.GrievancePeaceBoundaries=0;party.LastCounterpartDeclarationTurn=state.Turn;}
    }
    public static AttitudeView Apply(ContentSet content,GameState state,DiplomacyPair key,EmpireId viewer,AttitudeView basis)
    {
        var pair=state.Conflict?.Pairs.SingleOrDefault(p=>p.Pair==key);if(pair is null||!Rules.HasConflict(state.RulesVersion))return basis;
        var party=pair.Parties.Single(p=>p.Owner==viewer);int remaining=party.DeclarationGrievance?Math.Max(0,content.Conflict!.GrievancePeaceLifetime-party.GrievancePeaceBoundaries):0;
        int grievance=remaining>0?content.Conflict!.DeclarationGrievance:0;int score=checked(basis.Score-grievance);
        return basis with{Score=score,Label=pair.Relation==RelationKind.War?"War":DiplomacyAttitudes.Label(score),DeclarationGrievance=grievance,GrievancePeaceRemaining=remaining,GrievanceExpiresTurn=remaining>0&&pair.Relation==RelationKind.Peace?(long)state.Turn+remaining:null};
    }
}
