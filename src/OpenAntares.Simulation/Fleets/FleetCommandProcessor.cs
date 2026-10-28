using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
public static class FleetCommandProcessor
{
    internal static void ValidateAction(ContentSet content, EmpireView view, FleetAction action)
    {
        if (!Rules.HasFleets(view.RulesVersion) || view.Fleets is not { } f) throw new InvalidOperationException("Fleet command requires a fleet campaign.");
        void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
        FleetView Fleet(FleetId id) => f.Fleets.SingleOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("Fleet is unavailable to this empire.");
        VesselId[] Ships(ImmutableArray<VesselId> ids)
        {
            Require(!ids.IsDefaultOrEmpty, "Choose own ships.");
            var result = ids.Distinct().OrderBy(x => x).ToArray();
            Require(result.Length<=f.Ships.Length&&result.All(id => f.Ships.Any(v => v.Id == id)), "Selection contains an unavailable ship."); return result;
        }
        void Stationary(IEnumerable<VesselId> ids, bool group)
        {
            var ships=ids.Select(id=>view.Vessels.Single(v=>v.Id==id)).ToArray();
            Require(ships.All(v=>v.Transit is null), "Travel is committed until arrival.");
            if(group) Require(ships.Select(v=>v.StationaryStarId).Distinct().Count()==1 && ships.All(v=>v.Crisis?.Held!=true && view.Scouting?.Vessels.Any(row=>row.Id==v.Id && row.Stage is not null)!=true), "Grouping requires safe stationary co-location.");
        }
        void Name(string name) => Require(!string.IsNullOrWhiteSpace(name)&&name.Length<=80&&!name.Any(char.IsControl),"Fleet names require1..80 printable characters.");
        void Capacity() => Require(f.Fleets.Length + f.Requests.Count(r=>FleetQueries.IsActive(r.Status)&&r.ReservedFleetSlot) < content.FleetCommand!.FleetLimit,"All named fleet slots are active or reserved.");
        void Independent(IEnumerable<VesselId> ids) => Require(ids.All(id=>f.Ships.Single(v=>v.Id==id) is {Fleet:null,Request:null}),"Explicitly detach ships from their fleet/request first.");
        void Ungarrisoned(FleetView fleet) => Require(fleet.Officer is null,"Remove or transfer the admiral before removing their flagship/fleet.");
        switch(action)
        {
            case FleetAction.Create a:
                Name(a.Name);Capacity();var members=Ships(a.Members);Require(members.Length<=content.FleetCommand!.MemberLimit,"Fleet exceeds its member limit.");Independent(members);Stationary(members,true);
                Require(a.Flagship is null||members.Contains(a.Flagship.Value),"Flagship must be a selected member.");Require(members.Select(id=>f.Ships.Single(v=>v.Id==id).PendingMove).Distinct().Count()==1,"Resolve incompatible pending routes before grouping.");break;
            case FleetAction.Rename a: Fleet(a.Fleet);Name(a.Name);break;
            case FleetAction.Disband a:
                var disband=Fleet(a.Fleet);Stationary(disband.Members,false);Ungarrisoned(disband);break;
            case FleetAction.Merge a:
                Require(!a.Sources.IsDefaultOrEmpty,"Choose source fleets.");var target=Fleet(a.Target);var sources=a.Sources.Distinct().Where(id=>id!=a.Target).Select(Fleet).ToArray();Require(sources.Length>0,"Choose another source fleet.");
                var merged=target.Members.Concat(sources.SelectMany(x=>x.Members)).ToArray();Require(merged.Length<=content.FleetCommand!.MemberLimit,"Fleet exceeds its member limit.");Stationary(merged,true);Require(merged.Select(id=>f.Ships.Single(x=>x.Id==id).PendingMove).Distinct().Count()==1,"Resolve incompatible pending routes before merging.");
                foreach(var source in sources) {Ungarrisoned(source);Require(!f.Requests.Any(r=>FleetQueries.IsActive(r.Status)&&r.RallyFleet==source.Id),"Release the source rally request first.");}break;
            case FleetAction.Split a:
                var sourceFleet=Fleet(a.Source);Name(a.Name);Capacity();var split=Ships(a.Members);Require(split.All(sourceFleet.Members.Contains)&&split.Length<sourceFleet.Members.Length,"Split must leave members in both fleets.");Stationary(sourceFleet.Members,true);
                Require(a.Flagship is null||split.Contains(a.Flagship.Value),"Flagship must be a selected member.");if(split.Contains(sourceFleet.Flagship))Ungarrisoned(sourceFleet);break;
            case FleetAction.SetFlagship a:
                var flagship=Fleet(a.Fleet);Require(flagship.Members.Contains(a.Flagship),"Flagship must belong to the fleet.");Stationary(flagship.Members,true);break;
            case FleetAction.SetOrder a:
                Require(!a.Fleets.IsDefaultOrEmpty&&a.Fleets.Length<=f.Fleets.Length,"Choose fleets.");Require(a.Order is not null&&Enum.IsDefined(a.Order.Kind),"Choose a valid order.");
                if(a.Order!.Kind is FleetOrderKind.Assemble or FleetOrderKind.Return) Require(a.Order.Destination is {} star&&view.Galaxy.Stars.Any(x=>x.Id==star),"Choose a charted destination.");else Require(a.Order.Destination is null,"This order has no destination.");
                if(a.Order.Kind==FleetOrderKind.Return)Require(view.Colonies.Any(col=>col.StarId==a.Order.Destination),"Return requires a selected owned colony system.");
                foreach(var id in a.Fleets.Distinct())
                {
                    var ordered=Fleet(id);Stationary(ordered.Members,false);
                    if(ordered.Order!=a.Order&&a.Order.Kind!=FleetOrderKind.Hold)Require(!f.Requests.Any(r=>r.Status==RequestStatus.Active&&r.RallyFleet==id),"Pause the rally request before changing its movement goal.");
                    if(a.Order.Kind==FleetOrderKind.Explore)Require(ordered.Members.All(id=>{var role=content.Vessels[f.Ships.Single(v=>v.Id==id).RoleId];return !role.CanFoundColony&&role.EmbarkPopulation==0;}),"Explore requires scout-only members.");
                }break;
            case FleetAction.MoveSelection a:
                Require(!a.Vessels.IsDefault&&!a.Fleets.IsDefault,"Selection is missing.");var selected=a.Vessels.Concat(a.Fleets.Distinct().SelectMany(id=>Fleet(id).Members)).Distinct().ToImmutableArray();var moving=Ships(selected);Stationary(moving,false);
                Require(a.Destination is null||view.Galaxy.Stars.Any(x=>x.Id==a.Destination),"Choose a charted destination.");
                if(a.Destination is not null&&moving.Any(id=>f.Ships.Single(ship=>ship.Id==id).PendingMove!=a.Destination))Require(!moving.Any(id=>f.Ships.Single(ship=>ship.Id==id).Fleet is {} group&&f.Requests.Any(r=>r.Status==RequestStatus.Active&&r.RallyFleet==group)),"Pause the rally request before departing.");
                foreach(var id in moving)
                {
                    var ship=f.Ships.Single(v=>v.Id==id);Require(ship.Request is null,"Explicitly detach request ships first.");if(ship.Fleet is {} group)Require(Fleet(group).Members.All(moving.Contains),"Select the complete fleet or explicitly detach ships first.");
                    if(a.Destination is {} destination)
                    {
                        Require(Scouting.ScoutQueries.LegalRoute(content,view,view.Vessels.Single(v=>v.Id==id),destination),"Selected route is unavailable or held.");
                        var row=view.Scouting?.Vessels.SingleOrDefault(v=>v.Id==id);var habitat=view.Scouting?.Habitats.FirstOrDefault(h=>h.Star==destination);
                        Require(row?.Posture!=Scouting.ScoutPosture.AvoidEncounters||!Scouting.ScoutQueries.NeutralExcluded(view,destination),"Avoid encounters excludes this system.");
                        if(habitat is not null)Require(row?.Posture is Scouting.ScoutPosture.Observe&&ship.ApproachAcceptance==new Scouting.AmebaApproach(destination,habitat.HabitatId),"Confirm an eligible known-habitat approach explicitly first.");
                    }
                }break;
            case FleetAction.DetachSelection a:
                var detach=Ships(a.Vessels);Stationary(detach,false);foreach(var id in detach)if(f.Ships.Single(x=>x.Id==id).Fleet is {} group&&Fleet(group).Flagship==id)Ungarrisoned(Fleet(group));break;
            case FleetAction.DismissNewMarker a: Ships(a.Vessels);break;
            default: throw new InvalidOperationException("Unknown fleet action.");
        }
    }

    public static CommandOutcome Execute(ContentSet content, GameState state, FleetCommand command)
    {
        if(state.FindEmpire(command.Issuer) is null||state.Turn!=command.ExpectedTurn)return CommandOutcome.Reject(state,"fleet_stale","Fleet command is stale.");
        try
        {
            var preview=FleetQueries.PreviewAction(content,EmpireViewProjector.Project(content,state,command.Issuer),command.Action);
            if(command.Fingerprint is not null&&command.Fingerprint!=preview.Fingerprint)return CommandOutcome.Reject(state,"fleet_stale","Fleet preview changed; review the current selection and consequences again.");
            if(!preview.Allowed)return CommandOutcome.Reject(state,"fleet_illegal",preview.Rejection!);
            var candidate=state.Clone();var f=candidate.FindEmpire(command.Issuer)!.Fleets!;
            string Facts(GameState s)=>JsonSerializer.Serialize(new{Fleet=s.FindEmpire(command.Issuer)!.Fleets,Ships=s.Vessels.Where(v=>v.EmpireId==command.Issuer)});
            string before=Facts(state);
            FleetRecord Find(FleetId id)=>f.Fleets.Single(x=>x.Id==id);
            void Add(string name,IEnumerable<VesselId> members,VesselId? flagship)
            {
                var ids=members.Distinct().OrderBy(x=>x).ToList();var id=new FleetId(f.NextFleetId);f.NextFleetId=checked(f.NextFleetId+1);
                f.Fleets.Add(new(){Id=id,Name=name,Members=ids,Flagship=flagship??ids[0]});foreach(var ship in ids){candidate.FindVessel(ship)!.Fleet=id;candidate.FindVessel(ship)!.NewlyCompleted=false;}
            }
            void Detach(VesselId id)
            {
                var ship=candidate.FindVessel(id)!;
                if(ship.Fleet is {} group){var fleet=Find(group);fleet.Members.Remove(id);if(fleet.Members.Count==0){f.Fleets.Remove(fleet);RequestQueueAuthority.RallyDisbanded(candidate,command.Issuer,group);}else if(fleet.Flagship==id)fleet.Flagship=fleet.Members.Min();ship.Fleet=null;}
                foreach(var request in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)))foreach(var slot in request.Slots.Where(x=>x.Vessel==id))
                {slot.FormerVessel=id;slot.Vessel=null;slot.Status=SlotStatus.Missing;slot.ResumeStatus=null;slot.BlockedReason="Manually detached; finite amendment required.";request.Status=RequestStatus.Paused;request.Revision=checked(request.Revision+1);}
            }
            switch(command.Action)
            {
                case FleetAction.Create a:Add(a.Name,a.Members,a.Flagship);break;
                case FleetAction.Rename a:Find(a.Fleet).Name=a.Name;break;
                case FleetAction.Disband a:
                    var disbanded=Find(a.Fleet);
                    foreach(var id in disbanded.Members)
                    {
                        candidate.FindVessel(id)!.Fleet=null;
                        foreach(var r in f.Requests.Where(r=>FleetQueries.IsActive(r.Status)))foreach(var slot in r.Slots.Where(s=>s.Vessel==id))
                        {if(slot.Status==SlotStatus.BlockedSafety&&slot.ResumeStatus==SlotStatus.Arrived)slot.ResumeStatus=SlotStatus.Built;else if(slot.Status==SlotStatus.Arrived)slot.Status=SlotStatus.Built;}
                    }
                    f.Fleets.Remove(disbanded);RequestQueueAuthority.RallyDisbanded(candidate,command.Issuer,a.Fleet);break;
                case FleetAction.Merge a:
                    var target=Find(a.Target);foreach(var id in a.Sources.Distinct().Where(id=>id!=a.Target).OrderBy(id=>id)){var source=Find(id);foreach(var ship in source.Members){target.Members.Add(ship);candidate.FindVessel(ship)!.Fleet=target.Id;}f.Fleets.Remove(source);}target.Members.Sort();break;
                case FleetAction.Split a:foreach(var id in a.Members.Distinct())Detach(id);Add(a.Name,a.Members,a.Flagship);break;
                case FleetAction.SetFlagship a:
                    var fleet=Find(a.Fleet);fleet.Flagship=a.Flagship;if(fleet.Officer is {} officer)f.Officers.Single(x=>x.Id==officer).Flagship=a.Flagship;break;
                case FleetAction.SetOrder a:
                    foreach(var id in a.Fleets.Distinct()){var ordered=Find(id);ordered.Order=a.Order;if(a.Order.Kind==FleetOrderKind.Hold)foreach(var member in ordered.Members)candidate.FindVessel(member)!.PendingMove=null;}break;
                case FleetAction.MoveSelection a:
                    var selected=a.Vessels.Concat(a.Fleets.Distinct().SelectMany(id=>Find(id).Members)).Distinct().OrderBy(id=>id).ToArray();
                    foreach(var id in selected){var outcome=CommandProcessor.SetFleetMoveOrder(content,candidate,new(command.Issuer,command.ExpectedTurn,id,a.Destination));if(!outcome.Accepted)return CommandOutcome.Reject(state,outcome.Rejection!.Code,outcome.Rejection.Message);candidate=outcome.State;}
                    f=candidate.FindEmpire(command.Issuer)!.Fleets!;foreach(var group in selected.Select(id=>candidate.FindVessel(id)!.Fleet).OfType<FleetId>().Distinct())Find(group).Order=new(FleetOrderKind.Hold);break;
                case FleetAction.DetachSelection a:foreach(var id in a.Vessels.Distinct().OrderBy(id=>id))Detach(id);break;
                case FleetAction.DismissNewMarker a:foreach(var id in a.Vessels.Distinct())candidate.FindVessel(id)!.NewlyCompleted=false;break;
            }
            if(before==Facts(candidate))return CommandOutcome.NoChange(state);
            candidate.FindEmpire(command.Issuer)!.Ready=false;
            return new(candidate,null,true,null);
        }
        catch(Exception e)when(e is OverflowException or ArgumentException or InvalidOperationException){return CommandOutcome.Reject(state,"fleet_illegal","Fleet action is unavailable: "+e.Message);}
    }
}
