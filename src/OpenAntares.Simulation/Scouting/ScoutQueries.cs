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
        if(Rules.HasFleets(s.RulesVersion))
        {
            var safety=Crises.CrisisTurnCoordinator.PreviewForFleetConsent(c,s,owner);
            if(!safety.Succeeded)throw new InvalidOperationException("Own containment preparation could not be forecast.");s=safety.State;
        }
        var e=s.FindEmpire(owner)?.Scouting;
        if(e is null||!Rules.HasScouting(s.RulesVersion)||c.SpaceAmeba is not {} d)return (new(owner,s.Turn,ImmutableArray<ScoutVesselView>.Empty,ImmutableArray<KnownAmebaView>.Empty,ImmutableArray<AmebaAttentionToken>.Empty,ImmutableArray<ScoutTurnDigest>.Empty),new(ImmutableArray<Command>.Empty,ImmutableArray<AmebaAttentionToken>.Empty,ImmutableArray<ScoutOrderEvent>.Empty,ImmutableArray<string>.Empty));
        var frame=EmpireViewProjector.ProjectBase(c,s,owner);
        bool ai=s.FindEmpire(owner)!.Controller==ControllerKind.Ai;
        var rows=ImmutableArray.CreateBuilder<ScoutVesselView>();var commands=ImmutableArray.CreateBuilder<Command>();var tokens=ImmutableArray.CreateBuilder<AmebaAttentionToken>();var blockers=ImmutableArray.CreateBuilder<string>();var outcomes=ImmutableArray.CreateBuilder<ScoutOrderEvent>();
        foreach(var v in frame.Vessels.OrderBy(v=>v.Id))
        {
            var own=s.FindVessel(v.Id)!;var scout=own.Scout;var encounter=own.AmebaEncounter;
            bool neutralWithdrawal=encounter is null&&scout?.Posture==ScoutPosture.AvoidEncounters&&v.StationaryStarId is {} here&&NeutralExcluded(frame,here);
            StarId? departure=v.PendingMove;string? blocker=null;
            var actions=ImmutableArray.CreateBuilder<Command>();
            bool Known(StarId star)=>e.KnownHabitats.Any(h=>h.Star==star);
            bool Forbidden(StarId star)=>Known(star)||scout?.Posture==ScoutPosture.AvoidEncounters&&NeutralExcluded(frame,star);
            StarId? Fallback()
            {
                if((encounter is null&&!neutralWithdrawal)||(scout?.WithdrawFromAmeba!=true&&!(ai&&encounter is not null))||v.Transit is not null)return null;
                var known=frame.Galaxy.Stars.Select(x=>x.Id).Where(t=>!Known(t)&&LegalRoute(c,frame,v,t)&&frame.Stars.Any(star=>star.Chart.Id==t&&star.LastObservedTurn is not null)).ToArray();
                var ordinary=known.Where(t=>scout?.Posture!=ScoutPosture.AvoidEncounters||!NeutralExcluded(frame,t)).ToArray();
                var allowed=ordinary.Length>0?ordinary:encounter is not null?known:Array.Empty<StarId>();
                StarId? route=allowed.Where(t=>frame.Colonies.Any(col=>col.StarId==t)).OrderBy(t=>Duration(c,frame,v,t)).ThenBy(t=>t).Cast<StarId?>().FirstOrDefault()
                    ??allowed.OrderBy(t=>Duration(c,frame,v,t)).ThenBy(t=>t).Cast<StarId?>().FirstOrDefault();
                if(route is null&&encounter is not null&&ai)route=frame.Galaxy.Stars.Select(x=>x.Id).Where(t=>!Known(t)&&LegalRoute(c,frame,v,t)).OrderBy(t=>Duration(c,frame,v,t)).ThenBy(t=>t).Cast<StarId?>().FirstOrDefault();
                return route;
            }
            var fallback=Fallback();
            ScoutRouteView? Route(StarId? target)
            {
                if(target is not {} id||!LegalRoute(c,frame,v,id))return null;
                var observed=frame.Stars.Single(star=>star.Chart.Id==id);
                return new(id,observed.Chart.Name,Duration(c,frame,v,id),observed.LastObservedTurn,encounter is not null&&scout?.Posture==ScoutPosture.AvoidEncounters&&id==fallback&&NeutralExcluded(frame,id));
            }
            if(departure is {} pending&&(!LegalRoute(c,frame,v,pending)||Forbidden(pending)&&scout?.ApproachAcceptance?.Destination!=pending&&pending!=fallback))
            {
                departure=null;
                // Humans retain authority over ordinary cargo. Its legality still matters
                // for encounter blocking, but scout preparation cannot cancel its orders.
                if(scout is not null)
                {
                    commands.Add(new SetMoveOrderCommand(owner,s.Turn,v.Id,null));
                    outcomes.Add(new(owner,s.Turn,v.Id,v.RoleId,v.CarriedPopulation,v.StationaryStarId??v.Transit!.Origin,"move_cancelled","Standing order excludes this destination.",pending));
                }
            }
            if((encounter is not null&&encounter.RiskOrder is null||neutralWithdrawal)&&departure is null&&(scout?.WithdrawFromAmeba==true||ai&&encounter is not null)&&v.Transit is null)
            {
                departure=fallback;
                if(departure is {} target)
                {
                    if(commands.Count>0&&commands[commands.Count-1] is SetMoveOrderCommand {Destination:null} cancelled&&cancelled.Vessel==v.Id)commands.RemoveAt(commands.Count-1);
                    commands.Add(encounter is not null&&scout is not null?new SetScoutRetreatOrderCommand(owner,s.Turn,v.Id,target):new SetMoveOrderCommand(owner,s.Turn,v.Id,target));
                    var route=Route(target)!;string freshness=route.LastObservedTurn is {} observed?$"Observed turn {observed}.":"Unsurveyed chart; no observation date.";
                    string reason=$"Ordinary retreat to {route.Name} ({target}), {route.Duration} turn(s). {freshness}"+(route.SurvivalOverridesNeutralCaution?" Survival overrides neutral Avoid because no known unoccupied retreat is available.":"");
                    outcomes.Add(new(owner,s.Turn,v.Id,v.RoleId,v.CarriedPopulation,v.StationaryStarId!.Value,"retreat_planned",reason,target));
                }
            }
            if(neutralWithdrawal&&departure is null)
            {
                blocker="No known retreat destination; choose Observe or give a permitted destination.";
                string facts=string.Join(",",frame.ForeignVessels.Where(f=>f.StarId==v.StationaryStarId).Select(f=>f.Id.Value)) + ";"+string.Join(",",frame.Stars.Where(star=>star.Planets?.Any(p=>p.OwnerId is {} foreign&&foreign!=owner)==true).Select(star=>star.Chart.Id.Value));
                outcomes.Add(new(owner,s.Turn,v.Id,v.RoleId,v.CarriedPopulation,v.StationaryStarId!.Value,"wait",blocker+" Observed presence: "+facts));
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
            rows.Add(new(v.Id,v.RoleId,v.CarriedPopulation,scout?.Posture,v.StationaryStarId,v.Transit,v.PendingMove,departure,encounter?.RiskOrder?.Action,encounter?.Stage,encounter?.Number,encounter?.Revision,c.Vessels[v.RoleId].Speed,d.PursuitSpeed,late,study,late*study,d.ResearchReward,encounter?.Stage==AmebaStage.Warning?s.Turn:null,v.Crisis?.Held==true,blocker,acknowledged,actions.ToImmutable(),Route(departure)));
        }
        if(Rules.HasFleets(s.RulesVersion))
        {
            var departures=rows.ToDictionary(row=>row.Id,row=>row.EffectiveDestination);
            for(int i=0;i<tokens.Count;i++)tokens[i]=tokens[i] with{FleetConsequenceFingerprint=Fleets.FleetTravel.ConsequenceFingerprint(c,s,owner,tokens[i].Vessel,tokens[i].Destination,departures)};
            for(int i=0;i<rows.Count;i++)
            {
                var row=rows[i];var consequence=Fleets.FleetTravel.DepartureConsequence(c,s,owner,row.Id,departures);
                rows[i]=row with{Acknowledged=tokens.Any(t=>t.Vessel==row.Id&&e.Receipts.Any(receipt=>receipt.Token==t)),FleetConsequence=consequence,DepartureRoute=row.DepartureRoute is {} route&&consequence?.Duration is {} turns?route with{Duration=turns}:row.DepartureRoute};
            }
        }
        var attention=tokens.ToImmutable();
        return(new(owner,s.Turn,rows.ToImmutable(),e.KnownHabitats.Select(h=>new KnownAmebaView(h.HabitatId,h.Star,h.ObservedTurn)).ToImmutableArray(),attention,e.CompletedDigests.Concat(e.PendingDigest is {} pendingDigest?new[]{pendingDigest}:Array.Empty<ScoutTurnDigest>()).ToImmutableArray()),new(commands.ToImmutable(),attention,outcomes.ToImmutable(),blockers.ToImmutable()));
    }
}
