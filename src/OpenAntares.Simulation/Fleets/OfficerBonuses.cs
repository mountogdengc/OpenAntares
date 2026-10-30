using System;
using System.Linq;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
public static class OfficerBonuses
{
    public static long RequestShipCost(ContentSet content,GameState state,ColonyId colony,ProductionTarget target)
    {
        if(target.Kind==ProductionKinds.Vessel)return RequestShipCost(content,state,colony,target.ContentId);
        var col=state.FindColony(colony)??throw new ArgumentException("Owned shipyard required.");
        var design=ShipDesigns.ShipDesignTargets.Bound(content,state,col.EmpireId,target)??throw new ArgumentException("Pinned design required.");
        long basis=ShipDesigns.ShipDesignMath.Calculate(content.ShipDesigns!,design.Recipe).BuildCost;
        if(col.ProjectKind!=target.Kind||col.ProjectId!=target.ContentId||col.Development?.ActiveQueueToken is not {} token)return basis;
        var f=state.FindEmpire(col.EmpireId)!.Fleets!;
        var request=f.Requests.SingleOrDefault(r=>r.Status==RequestStatus.Active&&r.Colonies.Contains(colony)&&!r.PausedContributions.Contains(colony)&&r.Slots.Any(s=>s.Status==SlotStatus.Queued&&s.Colony==colony&&s.QueueToken==token&&s.Design==design.Id));
        if(request?.Coordinator is not {} coordinator||!col.Development.Queue.Any(q=>q.Token==token&&q.Target==target))return basis;
        var officer=f.Officers.SingleOrDefault(o=>o.Id==coordinator&&o.Status==OfficerStatus.Coordinating&&o.Request==request.Id&&o.PostedColony is {} posting&&request.Colonies.Contains(posting)&&state.FindColony(posting)?.EmpireId==col.EmpireId);
        return Price(content,basis,officer?.Specialty,officer?.Rank??0);
    }
    internal static long RequestShipCost(IFleetPlanningContext context,AssemblyDraft draft,ColonyId colony,AssemblyQuota quota,RequestId? replacing)
    {
        if(quota.Design is null)return RequestShipCost(context.Content,context.View,draft,colony,quota.RoleId,replacing);
        var view=context.View;long basis=AssemblyDesigns.Cost(context,quota);
        if(view.Fleets is not {} f||!draft.Colonies.Contains(colony)||draft.Coordinator is not {} id)return basis;
        if(replacing is {} request&&f.Requests.SingleOrDefault(r=>r.Id==request) is not{Status:RequestStatus.Active})return basis;
        if(replacing is {} paused&&f.Requests.Single(r=>r.Id==paused).PausedContributions.Contains(colony))return basis;
        var officer=f.Officers.SingleOrDefault(o=>o.Id==id&&o.PostedColony is {} posting&&draft.Colonies.Contains(posting)&&view.Colonies.Any(col=>col.Id==posting)&&(o.Status==OfficerStatus.Posted||o.Status==OfficerStatus.Coordinating&&o.Request==replacing));
        return Price(context.Content,basis,officer?.Specialty,officer?.Rank??0);
    }
    public static long SurveyResearch(ContentSet content,GameState state,EmpireId owner,FleetId fleet)
    {
        if(!Rules.HasFleets(state.RulesVersion))return 0;
        var f=state.FindEmpire(owner)!.Fleets!;var group=f.Fleets.Single(x=>x.Id==fleet);var officer=f.Officers.SingleOrDefault(o=>o.Id==group.Officer&&o.Status==OfficerStatus.Commanding&&o.Flagship==group.Flagship);
        return SurveyResearch(content,officer?.Specialty,officer?.Rank??0);
    }
    internal static long SurveyResearch(ContentSet content,OfficerSpecialty? specialty,int rank)
        =>specialty==OfficerSpecialty.Surveyor&&rank>0?content.FleetCommand!.SurveyResearchBonuses[rank-1]:0;
    public static long RequestShipCost(ContentSet content,GameState state,ColonyId colony,string roleId)
    {
        if(Rules.HasShipDesigns(state.RulesVersion)&&roleId==content.ShipDesigns?.WarshipRoleId)return RequestShipCost(content,state,colony,ShipDesigns.ShipDesignTargets.Build(new(1)));
        long basis=content.Vessels[roleId].Cost;
        if(!Rules.HasFleets(state.RulesVersion)||state.FindColony(colony) is not {} col||col.ProjectKind!=ProductionKinds.Vessel||col.ProjectId!=roleId||col.Development?.ActiveQueueToken is not {} token)return basis;
        var f=state.FindEmpire(col.EmpireId)!.Fleets;
        var request=f?.Requests.SingleOrDefault(r=>r.Status==RequestStatus.Active&&r.Colonies.Contains(colony)&&!r.PausedContributions.Contains(colony)&&r.Slots.Any(s=>s.Status==SlotStatus.Queued&&s.Colony==colony&&s.QueueToken==token&&s.RoleId==roleId));
        if(request?.Coordinator is not {} coordinator||!col.Development.Queue.Any(q=>q.Token==token&&q.Target.Kind==ProductionKinds.Vessel&&q.Target.ContentId==roleId))return basis;
        var officer=f!.Officers.SingleOrDefault(o=>o.Id==coordinator&&o.Status==OfficerStatus.Coordinating&&o.Request==request.Id&&o.PostedColony is {} posting&&request.Colonies.Contains(posting)&&state.FindColony(posting)?.EmpireId==col.EmpireId);
        return Price(content,basis,officer?.Specialty,officer?.Rank??0);
    }
    internal static long RequestShipCost(ContentSet content,EmpireView view,AssemblyDraft draft,ColonyId colony,string roleId,RequestId? replacing)
    {
        long basis=content.Vessels[roleId].Cost;
        if(!Rules.HasFleets(view.RulesVersion)||view.Fleets is not {} f||!draft.Colonies.Contains(colony)||!draft.Roles.Contains(roleId)||draft.Coordinator is not {} id)return basis;
        if(replacing is {} request&&f.Requests.SingleOrDefault(r=>r.Id==request) is not {Status:RequestStatus.Active})return basis;
        if(replacing is {} bound&&f.Requests.Single(r=>r.Id==bound).PausedContributions.Contains(colony))return basis;
        var officer=f.Officers.SingleOrDefault(o=>o.Id==id&&o.PostedColony is {} posting&&draft.Colonies.Contains(posting)&&view.Colonies.Any(col=>col.Id==posting)&&
            (o.Status==OfficerStatus.Posted||o.Status==OfficerStatus.Coordinating&&o.Request==replacing));
        return Price(content,basis,officer?.Specialty,officer?.Rank??0);
    }
    private static long Price(ContentSet content,long basis,OfficerSpecialty? specialty,int rank)
    {
        if(specialty!=OfficerSpecialty.Organizer||rank<1||content.FleetCommand is not {} d||rank>d.OrganizerDiscounts.Length)return basis;
        int discount=d.OrganizerDiscounts[rank-1];
        return Math.Max(1,checked((long)(((Int128)basis*(100-discount)+99)/100)));
    }
    internal static int NavigatorBonus(ContentSet content,GameState state,VesselState ship)
    {
        if(!Rules.HasFleets(state.RulesVersion)||ship.Fleet is not {} id)return 0;
        var f=state.FindEmpire(ship.EmpireId)!.Fleets!;var fleet=f.Fleets.Single(x=>x.Id==id);
        var officer=f.Officers.SingleOrDefault(o=>o.Id==fleet.Officer&&o.Flagship==fleet.Flagship&&o.Status==OfficerStatus.Commanding);
        return officer is {Specialty:OfficerSpecialty.Navigator,Rank:>0}?content.FleetCommand!.NavigatorBonuses[officer.Rank-1]:0;
    }
    public static int EffectiveSpeed(ContentSet content, GameState state, VesselId vessel)
    {
        var ship = state.FindVessel(vessel)!;
        var loadout=ShipDesigns.VesselLoadouts.Resolve(content,state,ship);int speed=loadout.BaseSpeed;
        if(Rules.HasCombat(state.RulesVersion))return Combat.CombatMath.Stats(loadout.Profile!,ship.Combat!.Hull,speed,NavigatorBonus(content,state,ship),content.Combat).Speed;
        if (!Rules.HasFleets(state.RulesVersion) || ship.Fleet is not { } id || state.FindEmpire(ship.EmpireId)!.Fleets is not { } f) return speed;
        var fleet = f.Fleets.Single(x => x.Id == id);
        var officer = f.Officers.SingleOrDefault(o => o.Id == fleet.Officer && o.Flagship == fleet.Flagship && o.Status == OfficerStatus.Commanding);
        if (officer is not { Specialty: OfficerSpecialty.Navigator, Rank: > 0 }) return speed;
        return checked((int)((long)speed * (100 + content.FleetCommand!.NavigatorBonuses[officer.Rank - 1]) / 100));
    }
    internal static int EffectiveSpeed(ContentSet content,EmpireView view,VesselId vessel)
    {
        var loadout=ShipDesigns.VesselLoadouts.Resolve(content,view,vessel);var fleet=view.Fleets?.Fleets.SingleOrDefault(f=>f.Members.Contains(vessel));
        var officer=fleet is null?null:view.Fleets?.Officers.SingleOrDefault(o=>o.Id==fleet.Officer&&o.Flagship==fleet.Flagship&&o.Status==OfficerStatus.Commanding);
        int bonus=officer is{Specialty:OfficerSpecialty.Navigator,Rank:>0}?content.FleetCommand!.NavigatorBonuses[officer.Rank-1]:0;
        return Rules.HasCombat(view.RulesVersion)?Combat.CombatMath.Stats(loadout.Profile!,view.Combat!.Ships.Single(s=>s.Id==vessel).Hull,loadout.BaseSpeed,bonus,content.Combat).Speed:checked((int)((long)loadout.BaseSpeed*(100+bonus)/100));
    }
}
