using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;
public static class ScoutQueries
{
    public static ScoutView Project(ContentSet c,GameState s,EmpireId owner)=>Evaluate(c,s,owner).View;
    public static ScoutPreparationPreview PreviewPreparation(ContentSet c,GameState s,EmpireId owner)=>Evaluate(c,s,owner).Preview;
    internal static bool NeutralExcluded(EmpireView view,StarId target)
        =>view.Stars.Any(star=>star.Chart.Id==target&&star.Planets is {} planets&&planets.Any(p=>p.OwnerId is {} id&&id!=view.Viewer))||view.ForeignVessels.Any(v=>v.StarId==target);
    internal static bool KnownHabitat(ScoutView view,StarId target)=>view.Habitats.Any(h=>h.Star==target);
    internal static bool LegalRoute(ContentSet c,EmpireView view,OwnedVesselView vessel,StarId target)
    {
        if(vessel.StationaryStarId is not {} origin||origin==target||vessel.Transit is not null)return false;
        var a=view.Galaxy.Stars.FirstOrDefault(s=>s.Id==origin);var b=view.Galaxy.Stars.FirstOrDefault(s=>s.Id==target);
        if(a is null||b is null||!c.Vessels.TryGetValue(vessel.RoleId,out var role))return false;
        if(vessel.CarriedPopulation>0&&(vessel.Crisis is {Held:true} or {PortClosed:true}||view.Colonies.Any(col=>col.StarId==target&&col.Crisis?.Quarantined==true)))return false;
        try{_=TravelCalculator.Duration(a.X,a.Y,b.X,b.Y,role.Speed);return true;}catch(Exception ex)when(ex is OverflowException or ArgumentOutOfRangeException){return false;}
    }
    internal static int Duration(ContentSet c,EmpireView view,OwnedVesselView vessel,StarId target)
    {var a=view.Galaxy.Stars.Single(s=>s.Id==vessel.StationaryStarId);var b=view.Galaxy.Stars.Single(s=>s.Id==target);return TravelCalculator.Duration(a.X,a.Y,b.X,b.Y,c.Vessels[vessel.RoleId].Speed);}
    private static (ScoutView View,ScoutPreparationPreview Preview) Evaluate(ContentSet c,GameState s,EmpireId owner)
    {
        var e=s.FindEmpire(owner)?.Scouting;
        if(e is null||!Rules.HasScouting(s.RulesVersion)||c.SpaceAmeba is not {} d)return (new(owner,s.Turn,ImmutableArray<ScoutVesselView>.Empty,ImmutableArray<KnownAmebaView>.Empty,ImmutableArray<AmebaAttentionToken>.Empty,ImmutableArray<ScoutTurnDigest>.Empty),new(ImmutableArray<Command>.Empty,ImmutableArray<AmebaAttentionToken>.Empty,ImmutableArray<ScoutOrderEvent>.Empty,ImmutableArray<string>.Empty));
        var frame=EmpireViewProjector.ProjectBase(c,s,owner);
        var rows=ImmutableArray.CreateBuilder<ScoutVesselView>();var commands=ImmutableArray.CreateBuilder<Command>();var tokens=ImmutableArray.CreateBuilder<AmebaAttentionToken>();var blockers=ImmutableArray.CreateBuilder<string>();var outcomes=ImmutableArray.CreateBuilder<ScoutOrderEvent>();
        foreach(var v in frame.Vessels.OrderBy(v=>v.Id))
        {
            var own=s.FindVessel(v.Id)!;var scout=own.Scout;var encounter=own.AmebaEncounter;
            StarId? departure=v.PendingMove;string? blocker=null;
            var actions=ImmutableArray.CreateBuilder<Command>();
            bool Known(StarId star)=>e.KnownHabitats.Any(h=>h.Star==star);
            bool Forbidden(StarId star)=>Known(star)||scout?.Posture==ScoutPosture.AvoidEncounters&&NeutralExcluded(frame,star);
            StarId? Fallback()
            {
                if(encounter is null||scout?.WithdrawFromAmeba!=true||v.Transit is not null)return null;
                var known=frame.Galaxy.Stars.Select(x=>x.Id).Where(t=>!Known(t)&&LegalRoute(c,frame,v,t)&&frame.Stars.Any(star=>star.Chart.Id==t&&star.LastObservedTurn is not null)).ToArray();
                var ordinary=known.Where(t=>scout.Posture!=ScoutPosture.AvoidEncounters||!NeutralExcluded(frame,t)).ToArray();
                var allowed=ordinary.Length>0?ordinary:known;
                StarId? route=allowed.Where(t=>frame.Colonies.Any(col=>col.StarId==t)).OrderBy(t=>Duration(c,frame,v,t)).ThenBy(t=>t).Cast<StarId?>().FirstOrDefault()
                    ??allowed.OrderBy(t=>Duration(c,frame,v,t)).ThenBy(t=>t).Cast<StarId?>().FirstOrDefault();
                if(route is null&&s.FindEmpire(owner)!.Controller==ControllerKind.Ai)route=frame.Galaxy.Stars.Select(x=>x.Id).Where(t=>!Known(t)&&LegalRoute(c,frame,v,t)).OrderBy(t=>Duration(c,frame,v,t)).ThenBy(t=>t).Cast<StarId?>().FirstOrDefault();
                return route;
            }
            var fallback=Fallback();
            if(departure is {} pending&&(!LegalRoute(c,frame,v,pending)||Forbidden(pending)&&scout?.ApproachAcceptance?.Destination!=pending&&pending!=fallback))
            {
                departure=null;
                commands.Add(new SetMoveOrderCommand(owner,s.Turn,v.Id,null));
                outcomes.Add(new(owner,s.Turn,v.Id,v.RoleId,v.CarriedPopulation,v.StationaryStarId??v.Transit!.Origin,"move_cancelled","Standing order excludes this destination.",pending));
            }
            if(encounter is not null&&encounter.RiskOrder is null&&departure is null&&scout?.WithdrawFromAmeba==true&&v.Transit is null)
            {
                departure=fallback;
                if(departure is {} target)
                {
                    if(commands.Count>0&&commands[commands.Count-1] is SetMoveOrderCommand {Destination:null} cancelled&&cancelled.Vessel==v.Id)commands.RemoveAt(commands.Count-1);
                    commands.Add(new SetScoutRetreatOrderCommand(owner,s.Turn,v.Id,target));
                }
            }
            int late=AmebaOdds.LateEscape(d,c.Vessels[v.RoleId].Speed),study=AmebaOdds.Study(d,c.Vessels[v.RoleId].Speed);
            bool acknowledged=false;
            if(encounter is not null)
            {
                string action=encounter.RiskOrder?.Action switch{AmebaRiskAction.Study=>"study",AmebaRiskAction.Remain=>"remain",_=>departure is null?"response_required":"depart"};
                int percent=action=="study"?study:action=="depart"?(encounter.Stage==AmebaStage.Warning?100:late):0;
                var token=new AmebaAttentionToken(v.Id,encounter.Number,encounter.Revision,encounter.Stage,action,action=="depart"?departure:null,c.Vessels[v.RoleId].Speed,v.CarriedPopulation,percent,action=="study"?d.ResearchReward:0);
                tokens.Add(token);acknowledged=e.Receipts.Any(r=>r.Token==token);
                if(action=="response_required"){blocker=$"{v.Id}: choose a legal departure or explicit risk response.";blockers.Add(blocker);}
                actions.Add(new SetAmebaRiskOrderCommand(owner,s.Turn,v.Id,encounter.Number,encounter.Revision,AmebaRiskAction.Remain));
                if(scout is not null&&encounter.Stage==AmebaStage.Warning&&!e.SuccessfulStudies.Any(claim=>claim.HabitatId==encounter.HabitatId)&&!frame.Vessels.Any(other=>other.Id!=v.Id&&s.FindVessel(other.Id)?.AmebaEncounter?.RiskOrder?.Action==AmebaRiskAction.Study))actions.Add(new SetAmebaRiskOrderCommand(owner,s.Turn,v.Id,encounter.Number,encounter.Revision,AmebaRiskAction.Study));
            }
            rows.Add(new(v.Id,v.RoleId,v.CarriedPopulation,scout?.Posture,v.StationaryStarId,v.Transit,v.PendingMove,departure,encounter?.RiskOrder?.Action,encounter?.Stage,encounter?.Number,encounter?.Revision,c.Vessels[v.RoleId].Speed,d.PursuitSpeed,late,study,late*study,d.ResearchReward,encounter?.Stage==AmebaStage.Warning?s.Turn:null,v.Crisis?.Held==true,blocker,acknowledged,actions.ToImmutable()));
        }
        var attention=tokens.ToImmutable();
        return(new(owner,s.Turn,rows.ToImmutable(),e.KnownHabitats.Select(h=>new KnownAmebaView(h.HabitatId,h.Star,h.ObservedTurn)).ToImmutableArray(),attention,e.CompletedDigests.ToImmutableArray()),new(commands.ToImmutable(),attention,outcomes.ToImmutable(),blockers.ToImmutable()));
    }
}
