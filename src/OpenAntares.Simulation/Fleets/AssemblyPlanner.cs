using System;
using OpenAntares.Simulation.ShipDesigns;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Economy;
namespace OpenAntares.Simulation.Fleets;
public static class AssemblyPlanner
{
    public static AssemblyDraft DraftFor(AssemblyRequestView request)=>new(request.Name,request.Rally,request.RallyFleet,request.Slots.Any(s=>s.Design is not null)?ImmutableArray<string>.Empty:request.Slots.OrderBy(s=>s.Ordinal).Select(s=>s.RoleId).ToImmutableArray(),request.Colonies,request.Priority,ImmutableArray<VesselId>.Empty,request.AuthorizeCargo,request.Coordinator){Quotas=request.Slots.Any(s=>s.Design is not null)?request.Slots.OrderBy(s=>s.Ordinal).Select(s=>new AssemblyQuota(s.RoleId,s.Design)).ToImmutableArray():default};
    public static AssemblyPreview PreviewRetained(IFleetPlanningContext context,RequestId request,StarId? rally=null,FleetId? rallyFleet=null,RequestPriority? priority=null)
    {
        var current=context.View.Fleets?.Requests.SingleOrDefault(r=>r.Id==request);
        if(current is null)return Empty("Request is unavailable.");
        var draft=DraftFor(current) with{Rally=rally??current.Rally,RallyFleet=rally is null?rallyFleet??current.RallyFleet:rallyFleet,Priority=priority??current.Priority};
        try{return PreviewAmend(context,AssemblyDesigns.Normalize(context,draft,request),current,true,priority is null);}
        catch(ArgumentException error){return Empty(error.Message);}
    }
    public static AssemblyPreview Preview(IFleetPlanningContext context,AssemblyDraft draft,RequestId? replacing=null)
    {
        try{draft=AssemblyDesigns.Normalize(context,draft,replacing);}
        catch(ArgumentException error){return Empty(error.Message);}
        if(replacing is null)return PreviewCore(context,draft,null);
        var current=context.View.Fleets?.Requests.SingleOrDefault(r=>r.Id==replacing);
        return current is null?Empty("Request is unavailable."):PreviewAmend(context,draft,current,false,false);
    }
    private static AssemblyPreview Empty(string reason)=>new(false,"",ImmutableArray<AssemblySlotPlan>.Empty,ImmutableArray<AssemblyColonyPreview>.Empty,ImmutableArray<FleetShipView>.Empty,ImmutableArray<string>.Empty,ImmutableArray.Create(reason));
    private static string Fingerprint(IFleetPlanningContext context,AssemblyDraft draft,RequestId? replacing,bool retained=false)
    {
        var c=context.Content;var v=context.View;
        var material=new{Draft=draft,Replacing=replacing,Retained=retained,v.Turn,v.Viewer,v.RulesVersion,Galaxy=v.Galaxy with{Seed=null},v.Stars,v.Colonies,v.Fleets,v.Scouting,v.ForeignVessels,v.Research.KnownTechnologyIds,c.Vessels,c.Buildings,c.Species,c.FleetCommand};
        byte[] bytes=Rules.HasShipDesigns(v.RulesVersion)?JsonSerializer.SerializeToUtf8Bytes(new{Material=material,Catalogue=v.ShipDesigns,Definitions=c.ShipDesigns}):JsonSerializer.SerializeToUtf8Bytes(material);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }
    private static AssemblyPreview PreviewCore(IFleetPlanningContext context,AssemblyDraft draft,RequestId? replacing,int retainedSlots=0,int retainedFleetAddition=0,HashSet<(ColonyId,int)>? retainedTokens=null,bool preserveQueueOrder=false,AssemblyDraft? priceDraft=null)
    {
        var c=context.Content;var v=context.View;
        string fingerprint=Fingerprint(context,draft,replacing);
        var warnings=ImmutableArray.CreateBuilder<string>();var errors=ImmutableArray.CreateBuilder<string>();
        AssemblyPreview Failed(string reason)=>new(false,fingerprint,ImmutableArray<AssemblySlotPlan>.Empty,ImmutableArray<AssemblyColonyPreview>.Empty,ImmutableArray<FleetShipView>.Empty,warnings.ToImmutable(),ImmutableArray.Create(reason));
        try
        {
            if(!Rules.HasFleets(v.RulesVersion)||c.FleetCommand is not {} d||v.Fleets is not {} f)return Failed("Assembly requires fleet command rules.");
            if(RefitGuards.Conflicts(v,new AssemblyCommand(v.Viewer,v.Turn,new AssemblyAction.Accept(draft,string.Empty))))return Failed("Cancel or complete the affected refit first.");
            if(string.IsNullOrWhiteSpace(draft.Name)||draft.Name.Length>80||draft.Name.Any(char.IsControl)||!Enum.IsDefined(draft.Priority))return Failed("Choose a printable1..80 character name and valid priority.");
            if(draft.Roles.IsDefault||draft.Roles.Length+retainedSlots<1||draft.Roles.Length+retainedSlots>d.MemberLimit||draft.Colonies.IsDefaultOrEmpty||draft.Colonies.Length>d.ParticipatingColonyLimit||draft.Colonies.Distinct().Count()!=draft.Colonies.Length||draft.AcceptedIdleVessels.IsDefault||draft.AcceptedIdleVessels.Distinct().Count()!=draft.AcceptedIdleVessels.Length)return Failed("Choose a bounded finite quota, distinct own colonies and distinct idle ships.");
            if(!v.Galaxy.Stars.Any(s=>s.Id==draft.Rally)||v.Scouting?.Habitats.Any(h=>h.Star==draft.Rally)==true)return Failed("Rally must be charted and free of known ameba danger.");
            if(draft.Roles.Any(id=>id is null||!c.Vessels.ContainsKey(id)||!v.AvailableVesselRoleIds.Contains(id)))return Failed("A requested role is unavailable to this empire.");
            if(draft.Roles.Any(id=>c.Vessels[id].EmbarkPopulation>0)&&!draft.AuthorizeCargo)return Failed("Explicit population cargo authorization is required.");
            if(draft.Roles.Any(id=>c.Vessels[id].EmbarkPopulation>0)&&v.Colonies.Any(col=>col.StarId==draft.Rally&&col.Crisis?.Quarantined==true))return Failed("The own population rally port is quarantined.");
            var others=f.Requests.Where(r=>FleetQueries.IsActive(r.Status)&&r.Id!=replacing).ToArray();
            if(replacing is {} replacement&&!f.Requests.Any(r=>r.Id==replacement&&FleetQueries.IsActive(r.Status)))return Failed("The request to amend is unavailable or final.");
            if(others.Length>=d.RequestLimit||others.Any(r=>r.Colonies.Intersect(draft.Colonies).Any()))return Failed("A participating colony or active request slot is already reserved.");
            var colonies=draft.Colonies.OrderBy(id=>id).Select(id=>v.Colonies.SingleOrDefault(col=>col.Id==id&&col.EmpireId==v.Viewer)).ToArray();
            if(colonies.Any(col=>col?.Development is null))return Failed("Every participating colony must be owned and available.");
            if(f.Fleets.Length+others.Count(r=>r.ReservedFleetSlot)+(draft.RallyFleet is null?1:0)>d.FleetLimit)return Failed("No named rally-fleet slot is available.");
            var rally=draft.RallyFleet is {} fleet?f.Fleets.SingleOrDefault(x=>x.Id==fleet):null;
            if(draft.RallyFleet is not null&&(rally is null||others.Any(r=>r.RallyFleet==rally.Id)||rally.Members.Any(id=>f.Ships.Single(x=>x.Id==id) is not {Transit:null,PendingMove:null} ship||ship.Station!=draft.Rally)||rally.Members.Length+draft.Roles.Length+retainedFleetAddition-draft.AcceptedIdleVessels.Count(rally.Members.Contains)>d.MemberLimit))return Failed("The rally fleet is unavailable, departing, reserved or lacks capacity.");
            bool Forbidden(VesselId id)=>v.Scouting?.Vessels.Any(row=>row.Id==id&&row.Posture==Scouting.ScoutPosture.AvoidEncounters)==true&&Scouting.ScoutQueries.NeutralExcluded(v,draft.Rally);
            if(rally?.Members.Any(Forbidden)==true)return Failed("Rally is forbidden by a member's current posture.");
            if(draft.Coordinator is {} coordinator)
            {
                var officer=f.Officers.SingleOrDefault(o=>o.Id==coordinator);
                if(officer is null||!(officer.Status==OfficerStatus.Posted||officer.Status==OfficerStatus.Coordinating&&officer.Request==replacing)||officer.PostedColony is not {} posting||!draft.Colonies.Contains(posting))return Failed("Coordinator must be available at a participating colony.");
            }
            var idle=f.Ships.Where(ship=>ship.Transit is null&&ship.PendingMove is null&&(ship.Fleet is null||ship.Fleet==draft.RallyFleet&&ship.Request==replacing)&&(ship.Request is null||ship.Request==replacing)&&v.Vessels.Single(x=>x.Id==ship.Id).PendingFounding is null&&v.Scouting?.Vessels.Any(row=>row.Id==ship.Id&&(row.Held||row.Stage is not null))!=true&&!Forbidden(ship.Id)).OrderBy(ship=>ship.Id).ToImmutableArray();
            if(draft.AcceptedIdleVessels.Any(id=>!idle.Any(ship=>ship.Id==id)))return Failed("An explicitly accepted idle ship is stale, held, assigned or forbidden.");
            var remaining=Enumerable.Range(0,draft.Roles.Length).Select(i=>AssemblyDesigns.At(draft,i)).ToList();foreach(var id in draft.AcceptedIdleVessels){var ship=idle.Single(ship=>ship.Id==id);if(!remaining.Remove(new(ship.RoleId,ship.Design)))return Failed("Accepted idle ships exceed the exact design quota.");}
            var allocated=colonies.Select(col=>col!).ToDictionary(col=>col.Id,col=>new List<AssemblyQueueItem>());
            var slots=new List<AssemblySlotPlan>();var accepted=draft.AcceptedIdleVessels.OrderBy(id=>id).ToList();
            var forecasts=new Dictionary<(ColonyId,string,string),ColonyEconomy?>();
            long Price(ColonyId colony,string role,DesignId? design=null)=>FleetPlanningQueries.RequestShipCost(context,priceDraft??draft,colony,new AssemblyQuota(role,design),replacing);
            ColonyEconomy? Forecast(OwnedColonyView col,ProductionTarget? target)
            {
                var key=(col.Id,target?.Kind??"",target?.ContentId??"");
                if(!forecasts.TryGetValue(key,out var forecast)){forecast=context.Forecast(col.Id,col.Workforce,target);forecasts.Add(key,forecast);}return forecast;
            }
            long Travel(StarId origin,string role,DesignId? design=null,int? equippedSpeed=null)
            {if(origin==draft.Rally)return 0;var start=v.Galaxy.Stars.Single(s=>s.Id==origin);var end=v.Galaxy.Stars.Single(s=>s.Id==draft.Rally);return Movement.TravelCalculator.Duration(start.X,start.Y,end.X,end.Y,equippedSpeed??AssemblyDesigns.Speed(context,role,design));}
            List<AssemblyQueueItem> Queue(OwnedColonyView col,List<AssemblyQueueItem> additions)
            {
                var old=col.Development!.Queue.Select(q=>new AssemblyQueueItem(q.Token,q.Target,null)).ToList();
                if(preserveQueueOrder)return old.Concat(additions).ToList();
                if(retainedTokens is not null)
                {
                    var pinned=old.Where(q=>q.Token==col.Development.ActiveQueueToken).ToList();
                    var bound=old.Where(q=>q.Token!=col.Development.ActiveQueueToken&&q.Token is {} token&&retainedTokens.Contains((col.Id,token))).ToList();
                    var other=old.Where(q=>q.Token!=col.Development.ActiveQueueToken&&(q.Token is not {} token||!retainedTokens.Contains((col.Id,token)))).ToList();
                    return draft.Priority==RequestPriority.Urgent?pinned.Concat(bound).Concat(additions).Concat(other).ToList():pinned.Concat(other).Concat(bound).Concat(additions).ToList();
                }
                if(draft.Priority==RequestPriority.Normal)return old.Concat(additions).ToList();
                var active=old.Where(q=>q.Token==col.Development.ActiveQueueToken).ToList();return active.Concat(additions).Concat(old.Where(q=>q.Token!=col.Development.ActiveQueueToken)).ToList();
            }
            (List<AssemblyQueueItem> Queue,long? Active,long Output) Estimate(OwnedColonyView col,List<AssemblyQueueItem> additions)
            {
                var queue=Queue(col,additions);var baseline=Forecast(col,null);long output=baseline?.NetProduction.Total??0,growth=baseline?.GrowthEarned.Total??0;int capacity=baseline?.Growth.Capacity??col.Population;
                long reserve=col.SpendableProduction,time=0,progress=col.GrowthProgress;int population=col.Population,support=col.Workforce.Support;bool stalled=false;
                long? Work(ProductionTarget target,bool requestBound=false)
                {
                    if(stalled)return null;var forecast=Forecast(col,target);
                    long? price=target.Kind==ProductionKinds.ShipDesign?requestBound?FleetPlanningQueries.RequestShipCost(context,priceDraft??draft,col.Id,AssemblyDesigns.From(context,target),replacing):AssemblyDesigns.Cost(context,AssemblyDesigns.From(context,target)):target.Kind==ProductionKinds.Vessel&&c.Vessels.TryGetValue(target.ContentId,out var roleCost)?requestBound?Price(col.Id,target.ContentId):roleCost.Cost:target.Kind==ProductionKinds.Building&&c.Buildings.TryGetValue(target.ContentId,out var building)?building.Cost:null;
                    if(forecast is null||price is null){stalled=true;return null;}
                    long deficit=Math.Max(0,checked(price.Value-reserve));
                    if(deficit>0&&output<=0){stalled=true;return null;}
                    long interval=deficit==0?1:Math.Max(1,checked((deficit-1)/output+1));int cargo=0;
                    if(target.Kind==ProductionKinds.Vessel)
                    {
                        var role=c.Vessels[target.ContentId];cargo=role.EmbarkPopulation;
                        if(cargo>0&&(col.Crisis?.Quarantined==true||forecast.Production.BlockedRequirements.Any(r=>r.Code=="quarantine"))){stalled=true;return null;}
                        int required=role.RequiresFullColony?capacity:Math.Max(role.MinimumSourcePopulation,role.CanFoundColony?cargo+1:0);
                        int births=Math.Max(0,Math.Max(required-population,cargo-support));
                        if(required>capacity||population+births>capacity||births>0&&growth<=0){stalled=true;return null;}
                        if(births>0){long waits=Math.Max(0,checked(((long)births*100-progress+growth-1)/growth));interval=Math.Max(interval,checked(waits+1));}
                    }
                    time=checked(time+interval);reserve=checked(reserve+checked(output*interval)-price.Value);
                    long earned=checked(progress+checked(growth*interval));int added=checked((int)Math.Min(capacity-population,earned/100));population=checked(population+added-cargo);support=checked(support+added-cargo);progress=population+cargo>=capacity?0:earned%100;
                    return time;
                }
                long? active=null;
                if(col.ProjectId is {} project)active=Work(new(col.ProjectKind,project),col.Development!.ActiveQueueToken is {} token&&retainedTokens?.Contains((col.Id,token))==true);
                for(int i=0;i<queue.Count;i++)queue[i]=queue[i] with{CompletionTurns=queue[i].Token==col.Development!.ActiveQueueToken&&col.Development.ActiveQueueToken is not null?active:Work(queue[i].Target,queue[i].SlotOrdinal is not null||queue[i].Token is {} token&&retainedTokens?.Contains((col.Id,token))==true)};
                return(queue,active,output);
            }
            for(int index=0;index<draft.Roles.Length;index++)
            {
                string role=draft.Roles[index];var quota=AssemblyDesigns.At(draft,index);int ordinal=index+1;var existing=accepted.FirstOrDefault(id=>idle.Single(ship=>ship.Id==id) is {} row&&row.RoleId==role&&row.Design==quota.Design);
                if(existing.Value>0)
                {
                    accepted.Remove(existing);var ship=idle.Single(x=>x.Id==existing);slots.Add(new(ordinal,role,existing,null,null,0,c.Vessels[role].EmbarkPopulation,0,Travel(ship.Station!.Value,role,quota.Design,Rules.HasShipDesigns(v.RulesVersion)?v.Combat?.Ships.Single(x=>x.Id==ship.Id).EffectiveSpeed:null)){Design=quota.Design});continue;
                }
                var options=colonies.Select(col=>col!).Where(col=>col.Development!.Queue.Length+allocated[col.Id].Count<32).Select(col=>
                {
                    var additions=allocated[col.Id].Append(new AssemblyQueueItem(null,AssemblyDesigns.Target(role,quota.Design),ordinal)).ToList();var estimate=Estimate(col,additions);var item=estimate.Queue.Single(q=>q.SlotOrdinal==ordinal);
                    long? arrival=item.CompletionTurns is {} turns?checked(turns+Travel(col.StarId,role,quota.Design)):(long?)null;return new{Colony=col,Item=item,Arrival=arrival,Position=estimate.Queue.IndexOf(item)};
                }).OrderBy(option=>option.Arrival??long.MaxValue).ThenBy(option=>option.Colony.Id).ToArray();
                if(options.Length==0)return Failed("No participating colony has remaining finite queue capacity.");var chosen=options[0];allocated[chosen.Colony.Id].Add(new(null,AssemblyDesigns.Target(role,quota.Design),ordinal));slots.Add(new(ordinal,role,null,chosen.Colony.Id,chosen.Position,Price(chosen.Colony.Id,role,quota.Design),c.Vessels[role].EmbarkPopulation,chosen.Item.CompletionTurns,chosen.Arrival){Design=quota.Design});
            }
            var colonyPreviews=ImmutableArray.CreateBuilder<AssemblyColonyPreview>();
            foreach(var col in colonies.Select(col=>col!))
            {
                var estimate=Estimate(col,allocated[col.Id]);
                foreach(var item in estimate.Queue.Where(q=>q.SlotOrdinal is not null))
                {int i=slots.FindIndex(s=>s.Ordinal==item.SlotOrdinal);var slot=slots[i];slots[i]=slot with{CompletionTurns=item.CompletionTurns,ArrivalTurns=item.CompletionTurns is {} turns?checked(turns+Travel(col.StarId,slot.RoleId,slot.Design)):(long?)null,QueuePosition=estimate.Queue.IndexOf(item)};}
                colonyPreviews.Add(new(col.Id,col.ProjectId is {} project?new(col.ProjectKind,project):null,col.Development!.ActiveQueueToken,col.SpendableProduction,estimate.Output,allocated[col.Id].Sum(q=>q.Target.Kind==ProductionKinds.ShipDesign?0:c.Vessels[q.Target.ContentId].EmbarkPopulation),col.Development.Queue,estimate.Queue.ToImmutableArray()));
            }
            warnings.Add("Conditional estimates hold current output and growth fixed; future manual orders, quarantine and research may change them. Each project takes at least one resolution.");
            if(slots.Any(s=>s.ArrivalTurns is null))warnings.Add("Some work has no finite completion/arrival estimate and remains blocked behind its active project or current requirements.");
            return new(true,fingerprint,slots.OrderBy(s=>s.Ordinal).ToImmutableArray(),colonyPreviews.ToImmutable(),idle.Where(ship=>Enumerable.Range(0,draft.Roles.Length).Any(i=>AssemblyDesigns.At(draft,i) is {} quota&&quota.RoleId==ship.RoleId&&quota.Design==ship.Design)).ToImmutableArray(),warnings.ToImmutable(),errors.ToImmutable());
        }
        catch(Exception error)when(error is OverflowException or ArgumentException or InvalidOperationException){return Failed("Preview cannot estimate this finite request: "+error.Message);}
    }

    private static AssemblyPreview PreviewAmend(IFleetPlanningContext context,AssemblyDraft draft,AssemblyRequestView request,bool retainAll,bool preserveQueueOrder)
    {
        string fingerprint=Fingerprint(context,draft,request.Id,retainAll);
        AssemblyPreview Fail(string reason)=>Empty(reason) with{Fingerprint=fingerprint};
        try
        {
            if(!FleetQueries.IsActive(request.Status)||draft.Roles.IsDefaultOrEmpty||draft.AcceptedIdleVessels.IsDefault)return Fail("Request/quota is unavailable.");
            var c=context.Content;var view=context.View;var own=view.Fleets!;
            if(draft.Roles.Any(role=>!c.Vessels.ContainsKey(role)))return Fail("Requested role is unavailable.");
            if(draft.Roles.Any(role=>c.Vessels[role].EmbarkPopulation>0)&&!draft.AuthorizeCargo)return Fail("Retained population cargo still requires explicit authorization.");
            var kept=new List<AssemblySlotView>();var freshRoles=new List<string>();var freshQuotas=new List<AssemblyQuota>();var freshAccepted=new List<VesselId>();var accepted=draft.AcceptedIdleVessels.ToList();
            for(int index=0;index<draft.Roles.Length;index++)
            {
                string role=draft.Roles[index];var quota=AssemblyDesigns.At(draft,index);
                var acceptedId=accepted.FirstOrDefault(id=>own.Ships.Any(ship=>ship.Id==id&&ship.RoleId==role&&ship.Design==quota.Design));
                var old=request.Slots.Where(slot=>!kept.Contains(slot)&&slot.RoleId==role&&slot.Design==quota.Design).OrderBy(slot=>slot.Ordinal).FirstOrDefault(slot=>
                    retainAll||(slot.Status!=SlotStatus.Missing&&slot.ResumeStatus!=SlotStatus.Missing&&(acceptedId.Value>0?slot.Vessel==acceptedId:slot.QueueToken is null||slot.Colony is {} colony&&draft.Colonies.Contains(colony))));
                if(old is not null){kept.Add(old);if(acceptedId.Value>0&&old.Vessel==acceptedId)accepted.Remove(acceptedId);}
                else {freshRoles.Add(role);freshQuotas.Add(quota);if(acceptedId.Value>0){accepted.Remove(acceptedId);freshAccepted.Add(acceptedId);}}
            }
            if(accepted.Count>0)return Fail("An accepted idle ship does not fit the amended quota.");
            if(retainAll&&kept.Count!=request.Slots.Length)return Fail("Retained review cannot change the authorized quota.");
            foreach(var slot in kept.Where(slot=>slot.Vessel is not null))
            {
                var row=view.Scouting?.Vessels.SingleOrDefault(ship=>ship.Id==slot.Vessel);
                if(row?.Posture==Scouting.ScoutPosture.AvoidEncounters&&Scouting.ScoutQueries.NeutralExcluded(view,draft.Rally))return Fail("Rally is forbidden by a retained member's current posture.");
            }
            // Retargeting must not silently strand a retained hull in its previous fleet,
            // displace a commander, or break a committed cohort. Ordinary disbanding keeps
            // request bindings, unlike explicitly detaching a request slot (which makes Missing).
            if((draft.Rally!=request.Rally||draft.RallyFleet!=request.RallyFleet)&&kept.Any(slot=>slot.Vessel is {} vessel&&own.Ships.Any(ship=>ship.Id==vessel&&ship.Fleet is {} fleet&&fleet!=draft.RallyFleet)))
                return Fail("First release retained ships from their existing fleet: wait for committed travel to arrive, then disband that fleet to retain request bindings. Transfer or remove its admiral first if commanded; review the new rally and resume separately.");
            var keptTokens=kept.Where(slot=>slot.Colony is not null&&slot.QueueToken is not null).Select(slot=>(slot.Colony!.Value,slot.QueueToken!.Value)).ToHashSet();
            var oldTokens=request.Slots.Where(slot=>slot.Colony is not null&&slot.QueueToken is not null).Select(slot=>(slot.Colony!.Value,slot.QueueToken!.Value)).ToHashSet();
            var modified=view.Colonies.Select(col=>col.Development is not {} dev?col:col with{Development=dev with{Queue=dev.Queue.Where(q=>!oldTokens.Contains((col.Id,q.Token))||keptTokens.Contains((col.Id,q.Token))||q.Token==dev.ActiveQueueToken).ToImmutableArray()}}).ToImmutableArray();
            var altered=view with{Colonies=modified};
            var rallyMembers=draft.RallyFleet is {} id?own.Fleets.SingleOrDefault(f=>f.Id==id)?.Members??ImmutableArray<VesselId>.Empty:ImmutableArray<VesselId>.Empty;
            int added=kept.Count(slot=>slot.Vessel is not {} ship||!rallyMembers.Contains(ship));
            bool unchangedWork=freshRoles.Count==0&&kept.Count==request.Slots.Length&&draft.Priority==request.Priority&&draft.Colonies.OrderBy(x=>x).SequenceEqual(request.Colonies.OrderBy(x=>x));
            var result=PreviewCore(FleetPlanningQueries.Create(c,altered),draft with{Roles=freshRoles.ToImmutableArray(),Quotas=Rules.HasShipDesigns(view.RulesVersion)?freshQuotas.ToImmutableArray():default,AcceptedIdleVessels=freshAccepted.ToImmutableArray()},request.Id,kept.Count,added,keptTokens,preserveQueueOrder||unchangedWork,draft);
            if(!result.Allowed)return result with{Fingerprint=fingerprint};
            int next=Math.Max(request.NextSlotOrdinal,checked(request.Slots.Max(slot=>slot.Ordinal)+1));
            var ordinalMap=result.Slots.ToDictionary(slot=>slot.Ordinal,_=>{int ordinal=next;next=checked(next+1);return ordinal;});
            var slots=result.Slots.Select(slot=>slot with{Ordinal=ordinalMap[slot.Ordinal]}).ToList();
            long Travel(StarId origin,string role,DesignId? design=null,int? equippedSpeed=null)
            {if(origin==draft.Rally)return 0;var a=view.Galaxy.Stars.Single(s=>s.Id==origin);var b=view.Galaxy.Stars.Single(s=>s.Id==draft.Rally);return Movement.TravelCalculator.Duration(a.X,a.Y,b.X,b.Y,equippedSpeed??AssemblyDesigns.Speed(context,role,design));}
            var colonyResults=result.Colonies.Select(col=>col with{PriorQueue=view.Colonies.Single(x=>x.Id==col.Colony).Development!.Queue,ProposedQueue=col.ProposedQueue.Select(q=>q.SlotOrdinal is {} fresh?q with{SlotOrdinal=ordinalMap[fresh]}:q.Token is {} token&&kept.FirstOrDefault(slot=>slot.Colony==col.Colony&&slot.QueueToken==token) is {} old?q with{SlotOrdinal=old.Ordinal}:q).ToImmutableArray()}).ToList();
            foreach(var original in view.Colonies.Where(col=>request.Colonies.Contains(col.Id)&&!draft.Colonies.Contains(col.Id)))
                colonyResults.Add(new(original.Id,original.ProjectId is {} project?new(original.ProjectKind,project):null,original.Development!.ActiveQueueToken,original.SpendableProduction,original.Economy.NetProduction.Total,0,original.Development.Queue,modified.Single(col=>col.Id==original.Id).Development!.Queue.Select(q=>new AssemblyQueueItem(q.Token,q.Target,null)).ToImmutableArray()));
            foreach(var old in kept)
            {
                var source=old.Colony is {} colony?colonyResults.FirstOrDefault(col=>col.Colony==colony):null;
                var queued=source?.ProposedQueue.FirstOrDefault(q=>q.Token==old.QueueToken&&q.Token is not null);
                long? completion=queued?.CompletionTurns,arrival=null;
                if(queued is not null&&old.Colony is {} yard&&completion is {} turns)arrival=checked(turns+Travel(view.Colonies.Single(col=>col.Id==yard).StarId,old.RoleId,old.Design));
                if(old.Vessel is {} vessel&&own.Ships.SingleOrDefault(ship=>ship.Id==vessel) is {} ship)
                {completion=0;arrival=ship.Transit is {} transit?checked(transit.RemainingTurns+Travel(transit.Destination,old.RoleId,old.Design,Rules.HasShipDesigns(view.RulesVersion)?view.Combat!.Ships.Single(s=>s.Id==vessel).EffectiveSpeed:null)):ship.Station is {} station?Travel(station,old.RoleId,old.Design,Rules.HasShipDesigns(view.RulesVersion)?view.Combat!.Ships.Single(s=>s.Id==vessel).EffectiveSpeed:null):(long?)null;}
                slots.Add(new(old.Ordinal,old.RoleId,old.Vessel,old.Colony,queued is null?null:source!.ProposedQueue.IndexOf(queued),old.QueueToken is null?0:FleetPlanningQueries.RequestShipCost(context,draft,old.Colony!.Value,new AssemblyQuota(old.RoleId,old.Design),request.Id),c.Vessels[old.RoleId].EmbarkPopulation,completion,arrival){RetainedSlotOrdinal=old.Ordinal,Design=old.Design});
            }
            var warnings=result.Warnings;if(kept.Any(slot=>slot.Status==SlotStatus.Missing))warnings=warnings.Add("Missing slots remain missing; only an explicit finite amendment may authorize replacements.");
            return result with{Fingerprint=fingerprint,Slots=slots.OrderBy(slot=>slot.Ordinal).ToImmutableArray(),Colonies=colonyResults.OrderBy(col=>col.Colony).ToImmutableArray(),Warnings=warnings};
        }
        catch(Exception error)when(error is OverflowException or InvalidOperationException or ArgumentException){return Fail("Retained request review failed: "+error.Message);}
    }
}
