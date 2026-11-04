using System;
using System.Linq;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Movement;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatQueries
{
    public static CombatEmpireView? Project(ContentSet content,GameState state,EmpireId viewer)
    {
        if(!Rules.HasCombat(state.RulesVersion)||state.FindEmpire(viewer)?.Combat is not {} c)return null;
        var d=content.Combat!;var notices=ImmutableArray.CreateBuilder<CombatNotice>();
        var ships=state.Vessels.Where(v=>v.EmpireId==viewer).OrderBy(v=>v.Id).Select(v=>
        {
            var own=v.Combat!;var loadout=ShipDesigns.VesselLoadouts.Resolve(content,state,v);var profile=loadout.Profile!;var stats=CombatMath.Stats(profile,own.Hull,loadout.BaseSpeed,OfficerBonuses.NavigatorBonus(content,state,v),d);
            var encounter=state.Combat!.Encounters.SingleOrDefault(e=>e.Owner==viewer&&e.Members.Contains(v.Id));
            bool atPort=own.ReturnColony is {} port&&state.FindColony(port) is {} colony&&state.FindPlanet(colony.PlanetId)?.StarId==v.StationaryStarId;
            var status=encounter is not null?encounter.Phase switch{CombatPhase.Pending=>CombatShipStatus.AttackCommitted,CombatPhase.Active=>CombatShipStatus.InCombat,CombatPhase.WithdrawalQueued=>CombatShipStatus.WithdrawalQueued,_=>CombatShipStatus.ResponseRequired}:v.Transit is not null&&own.ReturnColony is not null?CombatShipStatus.Returning:own.PendingRepairBlocked(state,v,atPort)?CombatShipStatus.RepairBlocked:own.Repairing?CombatShipStatus.Repairing:own.ReturnColony is not null?CombatShipStatus.ReturnOrdered:own.Hull<profile.MaxHull||own.Cohesion<100?CombatShipStatus.Damaged:CombatShipStatus.Hold;
            if(own.Hull<profile.MaxHull||own.Cohesion<100)notices.Add(new("damage/"+v.Id.Value,CombatNoticeLevel.Attention,"Persistent hull damage or command cohesion needs attention; Repair requires a legal owned port.",v.Id));
            if(status is CombatShipStatus.Returning or CombatShipStatus.ReturnOrdered or CombatShipStatus.WithdrawalQueued)notices.Add(new("return/"+v.Id.Value,CombatNoticeLevel.Attention,"Authorized return/withdrawal retains this ship, cargo and its selected arrival order.",v.Id));
            if(status==CombatShipStatus.RepairBlocked)notices.Add(new("repair/"+v.Id.Value,CombatNoticeLevel.Attention,"Repair is blocked by the current port or cargo conditions; no healing is applied.",v.Id));
            return new ShipCombatView(v.Id,own.Hull,own.Cohesion,own.ReturnColony,own.OnArrival,own.Repairing){MaxHull=profile.MaxHull,DamageBand=CombatMath.Band(profile,own.Hull,d),Firepower=stats.Firepower,Armor=stats.Armor,EffectiveSpeed=stats.Speed,Status=status,RepairIntervals=RepairIntervals(d,profile.MaxHull,own.Hull,own.Cohesion),HullLost=own.HullLost,HullRepaired=own.HullRepaired};
        }).ToImmutableArray();
        foreach(var e in state.Combat!.Encounters.Where(e=>e.Owner==viewer).OrderBy(e=>e.Id))
            if(c.Responses.Any(r=>r.Encounter==e.Id)||e.ConsentRevision!=e.Revision||e.ContinueRiskTurn!=state.Turn&&CombatWithdrawal.ReturnBlocked(state,e))notices.Add(new("response/"+e.Id.Value,CombatNoticeLevel.Emergency,"Review this encounter and revision before Ready: changed force, civilian risk, or blocked return requires its specific response.",Encounter:e.Id,Revision:e.Revision));
        foreach(var o in state.FindEmpire(viewer)!.Fleets!.Officers.Where(o=>o.Career.FlagshipsLost>0&&o.Status is OfficerStatus.Recovering or OfficerStatus.Dead or OfficerStatus.Missing))notices.Add(new("officer/"+o.Id.Value,CombatNoticeLevel.Attention,o.Name+": "+o.Status+" after flagship loss. Existing recovery doctrine and equipment apply."));
        return new(ships,c.Observations.OrderBy(o=>o.Pocket).Select(o=>new CombatPocketView(o.Pocket,o.Star,o.Turn,o.Cleared,ObservationUpdater.IsCurrent(state,viewer,o.Star),ObservationUpdater.IsCurrent(state,viewer,o.Star)&&state.Combat.Encounters.Any(e=>e.Pocket==o.Pocket&&e.Owner!=viewer),o.Sentries)).ToImmutableArray(),
            state.Combat.Encounters.Where(e=>e.Owner==viewer).OrderBy(e=>e.Id).Select(e=>new CombatEncounterView(e.Id,e.Fleet,e.Pocket,e.Revision,e.Members.ToImmutableArray(),e.Orders,e.Phase,e.Round,e.ConsentRevision,e.CivilianRiskAuthorized,e.ContinueRiskTurn){Details=e.Rounds.ToImmutableArray()}).ToImmutableArray(),c.Responses.ToImmutableArray())
            {Notices=notices.ToImmutable(),History=c.History.ToImmutableArray(),Events=c.RecentEvents.Concat(c.PendingEvents).OrderBy(e=>e.Id).TakeLast(d.HistoryLimit).ToImmutableArray(),Totals=new(c.LifetimeEncounters,c.LifetimeShipsLost,c.LifetimeHullLost,c.LifetimeCargoLost,c.LifetimeHullRepaired)};
    }
    private static bool PendingRepairBlocked(this ShipCombatState own,GameState state,VesselState vessel,bool atPort)=>atPort&&own.OnArrival==CombatArrivalOrder.Repair&&!CombatRepair.Eligible(state,vessel);
    internal static int RepairIntervals(CombatDefinition d,int maximum,int hull,int cohesion)
    {
        Int128 gain=((Int128)maximum+d.RepairHullDivisor-1)/d.RepairHullDivisor;
        Int128 hullTurns=((Int128)maximum-hull+gain-1)/gain;Int128 cohesionTurns=((Int128)100-cohesion+d.RepairCohesion-1)/d.RepairCohesion;
        return checked((int)Int128.Max(hullTurns,cohesionTurns));
    }
    public static CombatPreview Preview(ContentSet content,EmpireView view,CombatAction action)
    {
        CombatPreview Reject(string code=CombatRejectionCodes.Unavailable)=>new(false,code,string.Empty);
        if(!Rules.HasCombat(view.RulesVersion)||content.Combat is not {} d||view.Combat is not {} combat||view.Fleets is not {} fleets)return Reject();
        try
        {
            if(Conflict.ConflictCommandGuards.Conflicts(view,new CombatActionCommand(view.Viewer,view.Turn,action,string.Empty)))return Reject();
            if(ShipDesigns.RefitGuards.Conflicts(view,new CombatActionCommand(view.Viewer,view.Turn,action,string.Empty)))return Reject();
            if(action is CombatAction.SetRepair repair)return RepairPreview(content,view,repair);
            CombatEncounterView? existing=null;FleetId fleetId;PocketId pocketId;CombatOrders orders;bool cargoConsent;bool withdrawal=false,remain=false;
            if(action is CombatAction.AttackPocket attack)
            {
                fleetId=attack.Fleet;pocketId=attack.Pocket;orders=attack.Orders;cargoConsent=attack.AuthorizeCivilianRisk;
                if(combat.Encounters.Any(e=>e.Fleet==fleetId)||fleets.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.RallyFleet==fleetId))return Reject();
            }
            else
            {
                var id=action switch {CombatAction.ChangeCombatOrders a=>a.Encounter,CombatAction.WithdrawCombat a=>a.Encounter,CombatAction.RespondCombat a=>a.Encounter,_=>default};
                long revision=action switch {CombatAction.ChangeCombatOrders a=>a.Revision,CombatAction.WithdrawCombat a=>a.Revision,CombatAction.RespondCombat a=>a.Revision,_=>0};
                existing=combat.Encounters.SingleOrDefault(e=>e.Id==id);if(existing is null)return Reject();if(revision!=existing.Revision)return Reject(CombatRejectionCodes.StalePreview);
                fleetId=existing.Fleet;pocketId=existing.Pocket;orders=existing.Orders;cargoConsent=existing.CivilianRiskAuthorized;
                if(action is CombatAction.ChangeCombatOrders change){orders=change.Orders;cargoConsent=change.AuthorizeCivilianRisk;}
                else if(action is CombatAction.WithdrawCombat)withdrawal=true;
                else if(action is CombatAction.RespondCombat response)
                {
                    var pending=PendingResponse(view,existing);
                    if(!Enum.IsDefined(response.Response)||pending is null)return Reject();
                    remain=response.Response==CombatResponse.RemainWithBlockedReturn;
                    if(!pending.Need.HasFlag(remain?CombatResponseNeed.BlockedReturn:CombatResponseNeed.RevisedForce))return Reject();
                }
                else return Reject();
            }
            var fleet=fleets.Fleets.SingleOrDefault(f=>f.Id==fleetId);var pocket=combat.Pockets.SingleOrDefault(p=>p.Id==pocketId);
            if(fleet is null||pocket is null||pocket.Cleared||!pocket.Current||pocket.ObservedTurn!=view.Turn)return Reject();
            if(pocket.Unavailable)return Reject(CombatRejectionCodes.Reserved);
            if(fleet.Members.IsDefaultOrEmpty||fleet.Members.Length>32||!fleet.Members.Contains(fleet.Flagship)||fleet.Cohort is not null||fleet.Order.Kind!=FleetOrderKind.Hold)return Reject();
            if(orders is null||!Enum.IsDefined(orders.Doctrine)||!Enum.IsDefined(orders.Stragglers)||!Enum.IsDefined(orders.OnArrival))return Reject();
            var port=view.Colonies.SingleOrDefault(c=>c.Id==orders.ReturnColony);if(port is null&&!(existing is not null&&action is CombatAction.RespondCombat&&(remain||existing.ContinueRiskTurn==view.Turn))||port?.StarId==pocket.Star)return Reject();
            var officer=fleets.Officers.SingleOrDefault(o=>o.Id==fleet.Officer&&o.Flagship==fleet.Flagship&&o.Status==OfficerStatus.Commanding);
            int bonus=officer is {Specialty:OfficerSpecialty.Navigator,Rank:>0}?content.FleetCommand!.NavigatorBonuses[officer.Rank-1]:0;
            var ships=ImmutableArray.CreateBuilder<CombatShipFrame>();
            foreach(var id in fleet.Members.OrderBy(id=>id))
            {
                var vessel=view.Vessels.SingleOrDefault(v=>v.Id==id);var own=combat.Ships.SingleOrDefault(s=>s.Id==id);
                if(fleets.Ships.Single(s=>s.Id==id).Request is not null||vessel is null||own is null||vessel.StationaryStarId!=pocket.Star||vessel.Transit is not null||vessel.PendingMove is not null||vessel.PendingFounding is not null||own.Repairing||vessel.Crisis?.Held==true)return Reject();
                var loadout=ShipDesigns.VesselLoadouts.Resolve(content,view,id);var profile=loadout.Profile!;ships.Add(new(id,vessel.RoleId,own.Hull,profile.MaxHull,own.Cohesion,profile.Attack,profile.Armor,CombatMath.Stats(profile,own.Hull,loadout.BaseSpeed,bonus,d).Speed,id==fleet.Flagship,vessel.CarriedPopulation){Equipment=ShipDesigns.VesselLoadouts.Fact(content,view,id)});
            }
            var members=ships.ToImmutable();if(!members.Any(s=>s.Attack>0)&&existing is null)return Reject();
            CombatPhase? changedPhase=action is CombatAction.ChangeCombatOrders?CombatOrderRules.AfterChange(existing!,orders,cargoConsent,members,combat.Responses.Any(r=>r.Encounter==existing!.Id),d):null;
            if(changedPhase is not null)withdrawal=changedPhase==CombatPhase.WithdrawalQueued;
            if(combat.Encounters.Any(e=>e.Id!=existing?.Id&&e.Members.Any(m=>members.Any(s=>s.VesselId==m))))return Reject();
            var sentries=pocket.Sentries.Where(s=>s.Hull>0).OrderBy(s=>s.Id).Select(s=>new SentryFrame(s.Id,s.Hull,s.Profile.MaxHull,s.Profile.Attack,s.Profile.Armor,CombatMath.Stats(s.Profile,s.Hull,s.PursuitSpeed,0,d).Speed)).ToImmutableArray();
            var origin=view.Galaxy.Stars.Single(s=>s.Id==pocket.Star);var target=port is null?null:view.Galaxy.Stars.Single(s=>s.Id==port.StarId);
            var frame=new CombatFrame(existing?.Id,view.Turn,existing?.Revision??1,view.Viewer,fleet.Id,members,sentries,orders){Definition=d,Route=target is null?null:new(pocket.Star,origin.X,origin.Y,target.Id,target.X,target.Y)};
            bool civilian=members.Any(s=>s.Attack==0||s.Cargo>0);bool closed=port is null||members.Any(s=>s.Cargo>0)&&(PortClosed(view,port!.StarId)||PortClosed(view,pocket.Star));
            if(existing is not null&&!members.Any(s=>s.Attack>0)&&!closed)withdrawal=true;
            bool allowed=(!civilian||cargoConsent)&&(!closed||action is CombatAction.RespondCombat&&(remain||existing?.ContinueRiskTurn==view.Turn));
            string? code=allowed?null:closed?CombatRejectionCodes.PortClosed:CombatRejectionCodes.Unavailable;
            var departures=CombatWithdrawal.Preview(content,frame);bool fired=existing is {Round:>0};
            var exposed=(!withdrawal||fired)&&sentries.Length>0?departures.Where(g=>g.Speed<=sentries.Max(s=>s.PursuitSpeed)).SelectMany(g=>g.Members).OrderBy(id=>id).ToImmutableArray():ImmutableArray<VesselId>.Empty;
            var exchange=CombatExchange.Calculate(frame,withdrawal,withdrawal?exposed:ImmutableArray<VesselId>.Empty);
            OfficerLossConsequence? risk=null;
            if(officer is not null)
            {
                var command=content.FleetCommand!;bool equipment=view.Research.KnownTechnologyIds.Contains(command.RecoveryTechnologyId);
                int percent=officer.Embodiment==CommandEmbodiment.ColonyBody?100:officer.Doctrine==OfficerDoctrine.LastWatch?0:equipment?command.ResearchedRecoveryPercent:command.MortalRecoveryPercent;
                int delay=officer.Embodiment==CommandEmbodiment.ColonyBody?Math.Max(1,(officer.Doctrine==OfficerDoctrine.LastWatch?command.LastWatchLinkTurns:command.LinkRecoveryTurns)-(equipment?command.ResearchedLinkReduction:0)):command.MortalRecoveryTurns;
                risk=new(officer.Name,officer.Embodiment,officer.Doctrine,percent,delay);
            }
            var repairs=members.Where(s=>exchange.Ships.Single(a=>a.Id==s.VesselId).After>0).Select(s=>{int hull=exchange.Ships.Single(a=>a.Id==s.VesselId).After;int cohesion=(withdrawal?Forecast(frame,exchange,exposed):Forecast(frame,exchange)).Single(a=>a.Id==s.VesselId).CohesionAfter;return new CombatRepairForecast(s.VesselId,hull,cohesion,RepairIntervals(d,s.MaxHull,hull,cohesion));}).ToImmutableArray();
            var explanations=ImmutableArray.Create("No combat XP, ownership or loot is awarded. Officer loss uses the displayed existing recovery doctrine; cargo remains at risk.","Repair estimates count full eligible stationary intervals after arrival; arrival itself earns no healing.","A return route that closes during resolution prevents departure, permits one defensive exchange, and requires a new response.","Departure estimates use current hull bands and the displayed observation; later damage can change travel speed.");
            explanations=explanations.Add(CombatOrderRules.Explain(orders.Doctrine,d));
            if(existing?.Phase==CombatPhase.WithdrawalQueued&&changedPhase==CombatPhase.Active)explanations=explanations.Add("This reviewed change overrides the queued withdrawal and resumes a normal exchange under the selected doctrine.");
            else if(changedPhase==CombatPhase.WithdrawalQueued)explanations=explanations.Add("The withdrawal remains queued; these orders retain enemy-only interception and the authorized return.");
            var preview=new CombatPreview(allowed,code,string.Empty){OfficerRisk=risk,RepairForecasts=repairs,Frame=frame,Exchange=exchange,AffectedShips=members,Cargo=checked((int)members.Aggregate((Int128)0,(sum,s)=>checked(sum+s.Cargo))),Departures=departures,Exposed=exposed,Explanations=explanations,Forecasts=withdrawal?Forecast(frame,exchange,exposed):Forecast(frame,exchange)};
            var material=new{view.Turn,view.Viewer,ActionKind=action.GetType().Name,Action=JsonSerializer.SerializeToElement(action,action.GetType()),Frame=frame,Pocket=pocket with {Sentries=pocket.Sentries.OrderBy(s=>s.Id).ToImmutableArray()},Officer=officer,Equipment=view.Research.KnownTechnologyIds.Contains(content.FleetCommand!.RecoveryTechnologyId),Port=orders.ReturnColony,PortStar=port?.StarId,Closed=closed,CargoConsent=cargoConsent,Continuation=existing?.ContinueRiskTurn,Response=combat.Responses.FirstOrDefault(r=>r.Encounter==existing?.Id),Preview=preview};
            return preview with {Token=Token(material)};
        }
        catch(Exception ex)when(ex is OverflowException or ArgumentException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException){return Reject();}
    }
    internal static bool PortClosed(EmpireView view,StarId star)=>view.Colonies.Any(c=>c.StarId==star&&c.Crisis?.Quarantined==true)||view.Crisis is {Enabled:true,Authority:not Crises.CrisisAuthority.Manual,ContainmentPreview:{Allowed:true} policy}&&policy.ClosedPortsAfter.Contains(star);
    private static CombatPreview RepairPreview(ContentSet content,EmpireView view,CombatAction.SetRepair repair)
    {
        if(repair.Vessels.IsDefaultOrEmpty||repair.Vessels.Length>32||repair.Vessels.Distinct().Count()!=repair.Vessels.Length)return new(false,CombatRejectionCodes.Unavailable,string.Empty);
        var ships=ImmutableArray.CreateBuilder<CombatShipFrame>();var ports=ImmutableDictionary.CreateBuilder<VesselId,ColonyId>();
        foreach(var id in repair.Vessels.OrderBy(id=>id))
        {
            var vessel=view.Vessels.SingleOrDefault(v=>v.Id==id);var own=view.Combat!.Ships.SingleOrDefault(v=>v.Id==id);if(vessel is null||own is null)return new(false,CombatRejectionCodes.Unavailable,string.Empty);
            if(repair.Enabled)
            {
                var fleetShip=view.Fleets!.Ships.Single(v=>v.Id==id);var fleet=view.Fleets.Fleets.SingleOrDefault(f=>f.Id==fleetShip.Fleet);
                if(fleetShip.Request is not null||fleet is not null&&(fleet.Order.Kind!=FleetOrderKind.Hold||view.Fleets.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.RallyFleet==fleet.Id)))return new(false,CombatRejectionCodes.Unavailable,string.Empty);
                var port=view.Colonies.Where(c=>c.StarId==vessel.StationaryStarId).OrderBy(c=>c.Id).FirstOrDefault();
                if(port is null||vessel.Transit is not null||vessel.PendingMove is not null||vessel.PendingFounding is not null||view.Combat.Encounters.Any(e=>e.Members.Contains(id))||vessel.Crisis?.Held==true||vessel.CarriedPopulation>0&&PortClosed(view,port.StarId))return new(false,CombatRejectionCodes.PortClosed,string.Empty);
                ports.Add(id,port.Id);
            }
            var loadout=ShipDesigns.VesselLoadouts.Resolve(content,view,id);var profile=loadout.Profile!;ships.Add(new(id,vessel.RoleId,own.Hull,profile.MaxHull,own.Cohesion,profile.Attack,profile.Armor,CombatMath.Stats(profile,own.Hull,loadout.BaseSpeed,0,content.Combat).Speed,false,vessel.CarriedPopulation){Equipment=ShipDesigns.VesselLoadouts.Fact(content,view,id)});
        }
        var preview=new CombatPreview(true,null,string.Empty){AffectedShips=ships.ToImmutable(),RepairPorts=ports.ToImmutable(),RepairForecasts=ships.Select(s=>new CombatRepairForecast(s.VesselId,s.Hull,s.Cohesion,repair.Enabled?RepairIntervals(content.Combat!,s.MaxHull,s.Hull,s.Cohesion):0)).ToImmutableArray()};
        return preview with {Token=Token(new{view.Turn,view.Viewer,repair.Enabled,Ships=preview.AffectedShips,Ports=preview.RepairPorts.OrderBy(x=>x.Key).Select(x=>new{x.Key,x.Value}),Orders=repair.Vessels.OrderBy(id=>id).Select(id=>view.Vessels.Single(v=>v.Id==id)),Current=repair.Vessels.OrderBy(id=>id).Select(id=>view.Combat!.Ships.Single(v=>v.Id==id))})};
    }
    private static string Token<T>(T facts)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(facts)));
    internal static CombatPendingResponse? PendingResponse(EmpireView view,CombatEncounterView encounter)
    {
        var saved=view.Combat!.Responses.SingleOrDefault(r=>r.Encounter==encounter.Id&&r.Revision==encounter.Revision);
        if(!ReturnBlocked(view,encounter))return saved;
        if(saved is null)return new(encounter.Id,encounter.Revision,"Known blocked return",CombatResponseNeed.BlockedReturn);
        return encounter.ContinueRiskTurn==view.Turn?saved:saved with{Need=saved.Need|CombatResponseNeed.BlockedReturn};
    }
    internal static bool ReturnBlocked(EmpireView view,CombatEncounterView encounter)
    {
        var port=view.Colonies.SingleOrDefault(c=>c.Id==encounter.Orders.ReturnColony);if(port is null)return true;
        var members=view.Vessels.Where(v=>encounter.Members.Contains(v.Id));return members.Any(v=>v.CarriedPopulation>0&&(v.Crisis?.Held==true||PortClosed(view,port.StarId)||v.StationaryStarId is {} origin&&PortClosed(view,origin)));
    }
    internal static ImmutableArray<CombatShipForecast> Forecast(CombatFrame frame,CombatExchangeResult result,ImmutableArray<VesselId> participants)
    {
        var selected=frame with {Ships=frame.Ships.Where(s=>participants.Contains(s.VesselId)).ToImmutableArray()};var subset=result with {Ships=result.Ships.Where(s=>participants.Contains(s.Id)).ToImmutableArray(),DestroyedVessels=result.DestroyedVessels.Where(participants.Contains).ToImmutableArray()};
        var forecasts=Forecast(selected,subset);
        return frame.Ships.OrderBy(s=>s.VesselId).Select(s=>forecasts.FirstOrDefault(f=>f.Id==s.VesselId)??new CombatShipForecast(s.VesselId,CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),s.Hull,frame.Definition),CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),s.Hull,frame.Definition),s.Cohesion,s.Cohesion)).ToImmutableArray();
    }
    internal static ImmutableArray<CombatShipForecast> Forecast(CombatFrame frame,CombatExchangeResult exchange)
    {
        int crippled=frame.Ships.Count(s=>exchange.Ships.Single(d=>d.Id==s.VesselId).After>0&&CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),s.Hull,frame.Definition)!=CombatDamageBand.Crippled&&CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),exchange.Ships.Single(d=>d.Id==s.VesselId).After,frame.Definition)==CombatDamageBand.Crippled);
        bool flagship=frame.Ships.Any(s=>s.Flagship&&exchange.DestroyedVessels.Contains(s.VesselId));
        int loss=checked((int)Int128.Min(100,(Int128)exchange.DestroyedVessels.Length*(frame.Definition?.DestroyedCohesionLoss??20)+(Int128)crippled*(frame.Definition?.CrippledCohesionLoss??10)+(flagship?frame.Definition?.FlagshipCohesionLoss??10:0)));
        return frame.Ships.OrderBy(s=>s.VesselId).Select(s=>{int hull=exchange.Ships.Single(d=>d.Id==s.VesselId).After;return new CombatShipForecast(s.VesselId,CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),s.Hull,frame.Definition),hull>0?CombatMath.Band(new(s.MaxHull,s.Attack,s.Armor),hull,frame.Definition):null,s.Cohesion,hull>0?Math.Max(0,s.Cohesion-loss):s.Cohesion);}).ToImmutableArray();
    }
    internal static CombatFrame Frame(ContentSet content,GameState state,EncounterId encounter)
    {
        var e=state.Combat!.Encounters.Single(e=>e.Id==encounter);var p=state.Combat.Pockets.Single(p=>p.Id==e.Pocket);var f=state.FindEmpire(e.Owner)!.Fleets!.Fleets.Single(f=>f.Id==e.Fleet);var d=content.Combat!;
        var ships=e.Members.OrderBy(id=>id).Select(id=>
        {
            var vessel=state.FindVessel(id)??throw new ArgumentException("Committed combat member missing.");if(vessel.EmpireId!=e.Owner||vessel.StationaryStarId!=p.Star||vessel.Transit is not null||vessel.Fleet!=e.Fleet)throw new ArgumentException("Committed combat member invalid.");
            var profile=ShipDesigns.VesselLoadouts.CombatProfile(content,state,vessel);return new CombatShipFrame(id,vessel.RoleId,vessel.Combat!.Hull,profile.MaxHull,vessel.Combat.Cohesion,profile.Attack,profile.Armor,OfficerBonuses.EffectiveSpeed(content,state,id),id==f.Flagship,vessel.CarriedPopulation){Equipment=ShipDesigns.VesselLoadouts.Fact(content,state,vessel)};
        }).ToImmutableArray();
        var sentries=p.Sentries.Where(s=>s.Hull>0).OrderBy(s=>s.Id).Select(s=>new SentryFrame(s.Id,s.Hull,s.Profile.MaxHull,s.Profile.Attack,s.Profile.Armor,CombatMath.Stats(s.Profile,s.Hull,d.SentryPursuitSpeed,0,d).Speed)).ToImmutableArray();
        var origin=state.FindStar(p.Star)!;var colony=state.FindColony(e.Orders.ReturnColony);var planet=colony is null?null:state.FindPlanet(colony.PlanetId);var target=planet is null?null:state.FindStar(planet.StarId);
        return new(e.Id,state.Turn,e.Revision,e.Owner,e.Fleet,ships,sentries,e.Orders){Definition=d,Route=target is null?null:new(origin.Id,origin.X,origin.Y,target.Id,target.X,target.Y)};
    }
}
