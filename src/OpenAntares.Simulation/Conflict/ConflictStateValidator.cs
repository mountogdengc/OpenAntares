using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public static class ConflictStateValidator
{
    public static IReadOnlyList<string> Validate(ContentSet content,GameState state)
    {
        var errors=new List<string>();void Error(string reason)=>errors.Add("Conflict: "+reason);
        if(!Rules.HasConflict(state.RulesVersion)){if(state.Conflict is not null)Error("Old rules cannot contain conflict state.");return errors;}
        if(content.Conflict is not {} d||state.Conflict is not {} c){Error("Explicit conflict content and state required.");return errors;}
        if(c.NextEncounterId<1||c.NextEventId<1)Error("Counters must be positive.");
        if(c.Pairs is null||c.Owners is null||c.Encounters is null){Error("State collections required.");return errors;}
        if(c.Pairs.Count>(long)state.Empires.Count*(state.Empires.Count-1)/2)Error("Too many pair records.");
        var seenPairs=new HashSet<DiplomacyPair>();var belligerents=new HashSet<EmpireId>();DiplomacyPair? previous=null;
        foreach(var p in c.Pairs)
        {
            if(p is null){Error("Null pair.");continue;}
            var key=p.Pair;
            if(key.Low.Value<1||key.Low.CompareTo(key.High)>=0||state.FindEmpire(key.Low) is null||state.FindEmpire(key.High) is null||!seenPairs.Add(key)||previous is {} prev&&prev.CompareTo(key)>=0)Error("Pairs must be sorted unique existing empires.");previous=key;
            if(p.Revision<1||p.NextPeaceOfferId<1||!Enum.IsDefined(p.Relation))Error("Invalid pair revision or relation.");
            if(p.Parties is null||p.Parties.Count!=2||p.Parties.Any(x=>x is null)||p.Parties[0].Owner!=key.Low||p.Parties[1].Owner!=key.High){Error("Exactly two ordered parties required.");continue;}
            foreach(var party in p.Parties)
            {
                if(party.PrivateRevision<1||party.GrievancePeaceBoundaries<0||party.GrievancePeaceBoundaries>d.GrievancePeaceLifetime||party.NextPeaceOfferTurn<0||!Past(party.LastCounterpartDeclarationTurn)||!Past(party.ClaimObservedSince)||!Past(party.LastVisibleActivityTurn))Error("Invalid private party history/counter.");
                if(party.DeclarationGrievance&&party.LastCounterpartDeclarationTurn is null)Error("Grievance requires declaration history.");
                if(state.FindEmpire(party.Owner) is not {} owner||!owner.Contacts.Any(x=>x.OtherEmpireId==key.Other(party.Owner)))Error("Reciprocal known contacts required.");
            }
            if(p.Declarers is null||p.Declarations is null||p.PeaceIntents is null){Error("Pair intent collections required.");continue;}
            if(!Ordered(p.Declarers.Select(x=>(long)x.Value))||p.Declarers.Any(x=>!key.Contains(x)))Error("Invalid declarers.");
            if(p.Relation==RelationKind.War)
            {
                if(!Past(p.WarTurn)||p.WarTurn is null||p.Declarers.Count==0)Error("War requires published declaration date/declarers.");
                if(!belligerents.Add(key.Low)||!belligerents.Add(key.High))Error("Only one active opponent per empire.");
                if(p.TruceEndTurn is {} truce&&truce>state.Turn)Error("War cannot bypass an active truce.");
            }
            if(!Past(p.WarTurn)||!Past(p.LastPeaceTurn)||p.TruceEndTurn is {} end&&(end<0||p.LastPeaceTurn is not {} peace||end!=(long)peace+d.Truce))Error("Invalid war/peace/truce dates.");
            if(p.LastPeaceTurn.HasValue!=p.TruceEndTurn.HasValue)Error("Peace and truce date must be paired.");
            if(p.Declarations.Count>2||p.Declarations.Any(x=>x is null||!key.Contains(x.Owner)||x.Turn!=state.Turn)||!Ordered(p.Declarations.Select(x=>(long)x.Owner.Value)))Error("Invalid queued declaration.");
            if(p.Declarations.Count>0&&p.Relation!=RelationKind.Peace)Error("Cannot declare during war.");
            if(p.PeaceIntents.Count>2||p.PeaceIntents.Any(x=>x is null||!key.Contains(x.Owner)||x.Turn!=state.Turn)||!Ordered(p.PeaceIntents.Select(x=>(long)x.Owner.Value)))Error("Invalid private peace intent.");
            if((p.PeaceIntents.Count>0||p.PendingPeace is not null)&&p.Relation!=RelationKind.War)Error("Peace offers require war.");
            if(p.PendingPeace is {} offer&&(offer.Id.Value<1||offer.Id.Value>=p.NextPeaceOfferId||!key.Contains(offer.Proposer)||offer.CreatedTurn<0||offer.CreatedTurn>state.Turn||offer.ExpiryTurn!=(long)offer.CreatedTurn+d.OfferLifetime||offer.ExpiryTurn<=state.Turn))Error("Invalid peace offer identity/dates.");
        }
        if(state.Diplomacy is {} diplomacy&&(diplomacy.Pairs is null||diplomacy.Pairs.Any(p=>p is null||!seenPairs.Contains(p.Pair))))Error("Every diplomatic pair requires conflict relation state.");
        var used=new HashSet<VesselId>();var encounterIds=new HashSet<long>();var livePerOwner=new Dictionary<EmpireId,int>();long lastEncounter=0;
        foreach(var e in c.Encounters)
        {
            if(e is null){Error("Null encounter.");continue;}
            if(e.Id.Value<=lastEncounter||e.Id.Value<1||e.Id.Value>=c.NextEncounterId||!encounterIds.Add(e.Id.Value)||e.Revision<1||e.Round<0||!Enum.IsDefined(e.Phase)||!Past(e.CreatedTurn)||!Past(e.CompletedTurn)||!Past(e.LastResolvedTurn)||state.FindStar(e.Star) is null)Error("Invalid encounter identity/phase/date.");lastEncounter=e.Id.Value;
            var pair=c.Pairs.FirstOrDefault(p=>p?.Pair==e.Pair);
            bool live=e.Phase!=ConflictPhase.Completed;
            if(pair is null||live&&pair.Relation!=RelationKind.War)Error("A live encounter requires the matching war.");
            if(e.LowSide is null||e.HighSide is null||e.LowSide.Owner!=e.Pair.Low||e.HighSide.Owner!=e.Pair.High){Error("Two correctly ordered sides required.");continue;}
            if(live&&e.CompletedTurn is not null||!live&&e.CompletedTurn is null)Error("Completed date must match encounter phase.");
            foreach(var side in new[]{e.LowSide,e.HighSide})
            {
                if(side.Members is null||side.ProtectedCivilians is null||side.DestroyedMembers is null){Error("Side membership collections required.");continue;}
                if(side.ConsentRevision<1||side.ConsentRevision>e.Revision||!Orders(side.Orders,side.Owner))Error("Invalid side consent/orders.");
                if(!Ordered(side.Members.Select(x=>(long)x.Value))||!Ordered(side.ProtectedCivilians.Select(x=>(long)x.Value))||!Ordered(side.DestroyedMembers.Select(x=>(long)x.Value))||side.Members.Count+side.DestroyedMembers.Count>32)Error("Invalid bounded side membership.");
                if(side.DestroyedMembers.Any(x=>x.Value<1||x.Value>=state.NextEntityId||state.FindVessel(x) is not null))Error("Destroyed membership must be historical.");
                if(!live)continue;
                livePerOwner[side.Owner]=livePerOwner.GetValueOrDefault(side.Owner)+1;
                if(livePerOwner[side.Owner]>d.ActiveEncounterLimit||side.Members.Count<1||side.Members.Count>32)Error("Live encounter bound exceeded.");
                foreach(var id in side.Members)
                {
                    var v=state.FindVessel(id);
                    if(v is null||v.EmpireId!=side.Owner||v.StationaryStarId!=e.Star||v.Transit is not null||!Armed(v)||!used.Add(id))Error("Live participants must be unique own stationary armed ships.");
                    if(state.Combat?.Encounters.Any(b=>b.Members.Contains(id))==true)Error("A ship cannot also fight sentries.");
                }
                foreach(var id in side.ProtectedCivilians)
                    if(state.FindVessel(id) is not {} v||v.EmpireId!=side.Owner||v.StationaryStarId!=e.Star||Armed(v)||side.Members.Contains(id))Error("Protected membership must contain only own co-located civilians.");
                if(side.Fleet is {} fleet)
                {
                    var f=state.FindEmpire(side.Owner)?.Fleets?.Fleets.SingleOrDefault(x=>x.Id==fleet);
                    if(f is null||side.Members.Any(x=>!f.Members.Contains(x))||side.ProtectedCivilians.Any(x=>!f.Members.Contains(x)))Error("Encounter fleet must retain its members.");
                }
                else if(side.Members.Count!=1||side.ProtectedCivilians.Count!=0)Error("Ungrouped defending side must be a singleton.");
                if(!side.Members.Contains(side.Anchor)&&!side.DestroyedMembers.Contains(side.Anchor))Error("Invalid side anchor.");
                if(!side.Members.Contains(side.Flagship)&&!side.ProtectedCivilians.Contains(side.Flagship)&&!side.DestroyedMembers.Contains(side.Flagship))Error("Invalid side flagship.");
            }
        }
        if(c.Owners.Count!=state.Empires.Count||!Ordered(c.Owners.Where(x=>x is not null).Select(x=>(long)x.Owner.Value)))Error("Exactly one ordered owner record per empire required.");
        foreach(var o in c.Owners)
        {
            if(o is null||state.FindEmpire(o.Owner) is null){Error("Invalid owner.");continue;}
            if(o.Revision<1||o.NextEventId<1)Error("Invalid owner counters.");
            if(o.Preferences is null||o.Attacks is null||o.Blockades is null||o.Observations is null||o.Events is null||o.Notices is null||o.History is null){Error("Owner collections required.");continue;}
            if(o.Preferences.Count>state.Vessels.Count||o.Attacks.Count>64||o.Blockades.Count>64||o.History.Count>d.HistoryLimit||o.Events.Count>256||o.Notices.Count>256||o.Observations.Count>(long)state.Vessels.Count+256)Error("Owner collection bound exceeded.");
            var preferenceSlots=new HashSet<string>();
            foreach(var p in o.Preferences)
            {
                if(p is null){Error("Null preference.");continue;}
                var ship=state.FindVessel(p.Anchor);
                string slot=p.Fleet is {} f?"fleet:"+f.Value:"ship:"+p.Anchor.Value;
                if(ship is null||ship.EmpireId!=o.Owner||p.Revision<1||!Orders(p.Orders,o.Owner)||!preferenceSlots.Add(slot)||p.Fleet is {} fleet&&ship.Fleet!=fleet)Error("Invalid own defense preference.");
            }
            var tasked=new HashSet<int>();
            foreach(var a in o.Attacks)
            {
                if(a is null){Error("Null attack intent.");continue;}
                var f=state.FindEmpire(o.Owner)!.Fleets?.Fleets.SingleOrDefault(x=>x.Id==a.Fleet);var target=state.FindVessel(a.Target);
                if(f is null||target is null||target.EmpireId==o.Owner||a.Turn!=state.Turn||a.Revision<1||!Orders(a.Orders,o.Owner)||!tasked.Add(a.Fleet.Value))Error("Invalid own pending attack.");
            }
            foreach(var b in o.Blockades)
            {
                if(b is null){Error("Null blockade.");continue;}
                if(state.FindEmpire(o.Owner)!.Fleets?.Fleets.All(f=>f.Id!=b.Fleet)!=false||b.Other==o.Owner||state.FindEmpire(b.Other) is null||state.FindStar(b.Star) is null||!Past(b.AcceptedTurn)||b.Revision<1||!tasked.Add(b.Fleet.Value)||c.Pairs.All(p=>p?.Pair!=DiplomacyPair.For(o.Owner,b.Other)||p.Relation!=RelationKind.War))Error("Invalid live blockade authority.");
                // A historically selected target colony may have disappeared; resolution lifts it.
            }
            if(o.Observations.Any(x=>x is null)||o.Events.Any(x=>x is null)||o.Notices.Any(x=>x is null)||o.History.Any(x=>x is null)){Error("Null owner history row.");continue;}
            if(o.Observations.Select(x=>x.Vessel).Distinct().Count()!=o.Observations.Count)Error("Duplicate military observation.");
            foreach(var obs in o.Observations)
                if(obs.Vessel.Value<1||obs.Vessel.Value>=state.NextEntityId||obs.Owner==o.Owner||state.FindEmpire(obs.Owner) is null||state.FindStar(obs.Star) is null||!Past(obs.Turn)||obs.Current&&obs.Turn!=state.Turn||obs.Hull<1||obs.MaxHull<obs.Hull||obs.Attack<1||obs.Armor<0||obs.Speed<1||string.IsNullOrWhiteSpace(obs.RoleId)||!content.Vessels.ContainsKey(obs.RoleId)||obs.WeaponBanks.IsDefault)Error("Invalid current/stale military fact.");
            if(o.Observations.Count(x=>!x.Current)>256)Error("Stale military history exceeds bound.");
            if(!Ordered(o.Events.Select(x=>x.Id)))Error("Events must be unique and ordered.");
            foreach(var ev in o.Events)
                if(ev.Id<1||ev.Id>=o.NextEventId||ev.Viewer!=o.Owner||ev.Other==o.Owner||state.FindEmpire(ev.Other) is null||!Past(ev.Turn)||!Text(ev.Kind,80)||!Text(ev.Reason,1000))Error("Invalid filtered event.");
            if(o.Notices.Select(x=>x.Key).Distinct(StringComparer.Ordinal).Count()!=o.Notices.Count||o.Notices.Any(x=>!Text(x.Key,128)||!Text(x.Reason,1000)||!Enum.IsDefined(x.Level)))Error("Invalid notices.");
            foreach(var h in o.History)
                if(h.Encounter.Value<1||h.Encounter.Value>=c.NextEncounterId||h.Other==o.Owner||state.FindEmpire(h.Other) is null||state.FindStar(h.Star) is null||!Past(h.StartedTurn)||!Past(h.EndedTurn)||h.EndedTurn<h.StartedTurn||!Text(h.Outcome,200)||h.LostMembers is null||h.Rounds is null||h.Rounds.Count>d.RoundHistoryLimit||h.LostMembers.Any(id=>id.Value<1||id.Value>=state.NextEntityId||state.FindVessel(id) is not null))Error("Invalid historical encounter receipt.");
        }
        return errors;
        bool Past(int? turn)=>turn is null||turn>=0&&turn<=state.Turn;
        bool Ordered(IEnumerable<long> values){long prev=0;foreach(long n in values){if(n<=prev)return false;prev=n;}return true;}
        bool Text(string? value,int max)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=max;
        bool Armed(VesselState ship)
        {
            if(!content.Vessels.TryGetValue(ship.RoleId,out var role)||!role.CombatOnly||role.CanFoundColony||role.EmbarkPopulation!=0||ship.CarriedPopulation!=0)return false;
            try{return VesselLoadouts.Resolve(content,state,ship).Profile?.Attack>0;}
            catch(Exception e)when(e is ArgumentException or InvalidOperationException){return false;}
        }
        bool Orders(ConflictOrders? orders,EmpireId owner)=>orders is not null&&Enum.IsDefined(orders.Doctrine)&&Enum.IsDefined(orders.Stragglers)&&Enum.IsDefined(orders.OnArrival)&&(orders.ReturnColony is not {} port||port.Value>0&&port.Value<state.NextEntityId&&(state.FindColony(port) is not {} colony||colony.EmpireId==owner));
    }
}
