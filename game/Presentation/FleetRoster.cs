using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Presentation;

/// <summary>Requester-visible ship equivalence and copy shared by every fleet roster.</summary>
public static class FleetRoster
{
    public static string Key(EmpireView view, OwnedVesselView own)
    {
        var ship=view.Fleets?.Ships.FirstOrDefault(s=>s.Id==own.Id);
        var combat=view.Combat?.Ships.FirstOrDefault(s=>s.Id==own.Id);
        var health=own.Crisis;
        var scout=view.Scouting?.Vessels.FirstOrDefault(s=>s.Id==own.Id);
        var combatFacts=combat is null?null:new{combat.Hull,combat.Cohesion,combat.ReturnColony,combat.OnArrival,combat.Repairing,combat.MaxHull,combat.DamageBand,combat.Firepower,combat.Armor,combat.EffectiveSpeed,combat.Status,combat.RepairIntervals,combat.HullLost,combat.HullRepaired};
        var healthFacts=health is null?null:new{health.RoleId,health.Population,health.CarrierEpisode,health.InspectionDueTurn,health.Station,health.Transit,health.PendingMove,health.PendingFounding,health.PortClosed,health.Held,health.CanDecontaminate,health.ContactRisks};
        var scoutFacts=scout is null?null:new{scout.Posture,scout.RiskAction,scout.Stage,scout.Revision,scout.Held,scout.Blocker,scout.Acknowledged,scout.DepartureDeadline,scout.EffectiveDestination};
        // Active safety responses remain individually identifiable, even if their short status happens to match.
        bool attention=view.Combat?.Notices.Any(n=>n.Vessel==own.Id&&n.Level!=Simulation.Combat.CombatNoticeLevel.Summary)==true
            ||scout?.Stage is not null||scout?.Held==true||scout?.Blocker is not null;
        return JsonSerializer.Serialize(new{own.RoleId,Design=ship?.Design??own.Loadout?.Design,own.StationaryStarId,own.Transit,own.PendingMove,own.PendingFounding,own.CarriedPopulation,ship?.Fleet,ship?.Request,ship?.NewlyCompleted,ship?.ApproachAcceptance,combatFacts,healthFacts,scoutFacts,AttentionShip=attention?(int?)own.Id.Value:null});
    }

    public static string Title(ContentSet content,EmpireView view,IReadOnlyList<OwnedVesselView> ships) =>
        ships.Count==1?FleetNaming.Ship(content,view,ships[0]):FleetNaming.ShipClass(content,view,ships[0])+" ×"+ships.Count;

    public static string Status(EmpireView view,IReadOnlyList<OwnedVesselView> ships,bool location=false)
    {
        var first=ships[0];var ship=view.Fleets?.Ships.FirstOrDefault(s=>s.Id==first.Id);
        var fleet=ship?.Fleet is {} id?view.Fleets!.Fleets.FirstOrDefault(f=>f.Id==id):null;
        string text=FleetNaming.Status(view,ships,fleet);
        if(location&&first.StationaryStarId is {} at)text+=" · "+FleetNaming.Star(view,at);
        if(fleet is not null)text+=" · "+fleet.Name;
        if(ship?.Request is {} request)text+=" · request "+request.Value;
        if(first.CarriedPopulation>0)text+=" · "+first.CarriedPopulation+" "+view.PopulationUnitLabel+" aboard";
        if(view.Combat?.Ships.FirstOrDefault(s=>s.Id==first.Id) is {} combat)
        {
            if(combat.Status!=OpenAntares.Simulation.Combat.CombatShipStatus.Hold||combat.Hull<combat.MaxHull)text+=" · "+CombatReportPanel.Status(combat);
            text+=" · hull "+combat.Hull+"/"+combat.MaxHull;
        }
        if(first.Crisis?.Held==true)text+=" · held";
        if(ship?.NewlyCompleted==true)text+=" · NEW";
        if(fleet?.Attention is not null||view.Scouting?.Vessels.Any(s=>s.Id==first.Id&&(s.Stage is not null||s.Held||s.Blocker is not null))==true)text+=" · ATTENTION";
        return text;
    }
}
