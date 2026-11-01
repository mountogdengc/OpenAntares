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
        if(c.NextEncounterId<1||c.NextEventId<1||c.NextEncounterId==long.MaxValue||c.NextEventId==long.MaxValue)Error("Counters must be positive and not exhausted.");
        if(c.Pairs is null||c.Owners is null||c.Encounters is null){Error("State collections required.");return errors;}
        if(c.Pairs.Count>(long)state.Empires.Count*(state.Empires.Count-1)/2)Error("Too many pair records.");
        var seenPairs=new HashSet<DiplomacyPair>();var belligerents=new HashSet<EmpireId>();DiplomacyPair? previous=null;
        foreach(var p in c.Pairs)
        {
            if(p is null){Error("Null pair.");continue;}
            var key=p.Pair;
            if(key.Low.Value<1||key.Low.CompareTo(key.High)>=0||state.FindEmpire(key.Low) is null||state.FindEmpire(key.High) is null||!seenPairs.Add(key)||previous is {} prev&&prev.CompareTo(key)>=0)Error("Pairs must be sorted unique existing empires.");previous=key;
            if(p.Revision<1||p.NextPeaceOfferId<1||p.Revision==long.MaxValue||p.NextPeaceOfferId==long.MaxValue||!Enum.IsDefined(p.Relation))Error("Invalid pair revision or relation.");
            if(p.Parties is null||p.Parties.Count!=2||p.Parties.Any(x=>x is null)||p.Parties[0].Owner!=key.Low||p.Parties[1].Owner!=key.High){Error("Exactly two ordered parties required.");continue;}
            foreach(var party in p.Parties)
            {
                if(party.PrivateRevision<1||party.PrivateRevision==long.MaxValue||party.GrievancePeaceBoundaries<0||party.GrievancePeaceBoundaries>d.GrievancePeaceLifetime||party.NextPeaceOfferTurn<0||party.NextPeaceOfferTurn>(long)state.Turn+d.PeaceCooldown||!Past(party.LastCounterpartDeclarationTurn)||!Past(party.ClaimObservedSince)||!Past(party.LastVisibleActivityTurn))Error("Invalid private party history/counter.");
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
            if(p.PeaceIntents.Count>0&&p.PendingPeace is not null)Error("Published and private peace offers cannot coexist.");
            if(p.Relation==RelationKind.War&&state.Diplomacy?.Pairs?.Any(x=>x is not null&&x.Pair==key&&(x.Pending is not null||x.Agreements?.Count>0||x.Parties?.Any(a=>a?.Attitude?.Streak!=0)==true))==true)Error("War cannot retain ordinary agreements or cooperation.");
            if((p.PeaceIntents.Count>0||p.PendingPeace is not null)&&p.Relation!=RelationKind.War)Error("Peace offers require war.");
            if(p.PendingPeace is {} offer&&(offer.Id.Value<1||offer.Id.Value>=p.NextPeaceOfferId||!key.Contains(offer.Proposer)||offer.CreatedTurn<0||offer.CreatedTurn>state.Turn||offer.ExpiryTurn!=(long)offer.CreatedTurn+d.OfferLifetime||offer.ExpiryTurn<=state.Turn))Error("Invalid peace offer identity/dates.");
        }
        if(state.Diplomacy is {} diplomacy&&(diplomacy.Pairs is null||diplomacy.Pairs.Any(p=>p is null||!seenPairs.Contains(p.Pair))))Error("Every diplomatic pair requires conflict relation state.");
        var used=new HashSet<VesselId>();var encounterIds=new HashSet<long>();var livePerOwner=new Dictionary<EmpireId,int>();long lastEncounter=0;
        foreach(var e in c.Encounters)
        {
            if(e is null){Error("Null encounter.");continue;}
            if(e.Id.Value<=lastEncounter||e.Id.Value<1||e.Id.Value>=c.NextEncounterId||!encounterIds.Add(e.Id.Value)||e.Revision<1||e.Round<0||!Enum.IsDefined(e.Phase)||!Past(e.CreatedTurn)||!Past(e.CompletedTurn)||!Past(e.LastResolvedTurn)||state.FindStar(e.Star) is null)Error("Invalid encounter identity/phase/date.");lastEncounter=e.Id.Value;
            if(e.Rounds is null||e.Rounds.Count>d.RoundHistoryLimit)Error("Bounded encounter receipts required.");
            else
            {
                long priorRound=0;int priorTurn=-1;
                foreach(var receipt in e.Rounds)
                {
                    if(receipt is null){Error("Null round receipt.");continue;}
                    if(receipt.Round<=priorRound||receipt.Round>e.Round||receipt.Turn<=priorTurn||!Past(receipt.Turn)||receipt.Shots.IsDefault||receipt.Own.IsDefault||receipt.Opposing.IsDefault||receipt.Shots.Length>64||receipt.Own.Length>32||receipt.Opposing.Length>32)Error("Invalid bounded round receipt.");
                    priorRound=receipt.Round;priorTurn=receipt.Turn;
                    if(!receipt.Shots.IsDefault&&receipt.Shots.Any(s=>s is null||s.Source.Sentry||s.Target.Sentry||s.Source.Id<1||s.Source.Id>=state.NextEntityId||s.Target.Id<1||s.Target.Id>=state.NextEntityId||s.Source.Id==s.Target.Id||s.Damage<1))Error("Invalid real-vessel shot.");
                    if(!receipt.Own.IsDefault&&!receipt.Opposing.IsDefault&&receipt.Own.Concat(receipt.Opposing).Any(x=>x is null||x.Id.Value<1||x.Id.Value>=state.NextEntityId||x.Before<1||x.After<0||x.After>x.Before||x.ActualLoss!=x.Before-x.After))Error("Invalid round damage.");
                }
                if(e.Round>0&&(e.Rounds.Count==0||e.Rounds[^1] is not {} latest||latest.Round!=e.Round||latest.Turn!=e.LastResolvedTurn)||e.Round==0&&e.Rounds.Count>0)Error("Latest applied round must survive report trimming.");
            }
            var pair=c.Pairs.FirstOrDefault(p=>p?.Pair==e.Pair);
            bool live=e.Phase!=ConflictPhase.Completed;
            if(pair is null||live&&pair.Relation!=RelationKind.War)Error("A live encounter requires the matching war.");
            if(e.LowSide is null||e.HighSide is null||e.LowSide.Owner!=e.Pair.Low||e.HighSide.Owner!=e.Pair.High){Error("Two correctly ordered sides required.");continue;}
            if(e.Revision==long.MaxValue||e.LowSide.ConsentRevision==long.MaxValue||e.HighSide.ConsentRevision==long.MaxValue)Error("Encounter consent counter exhausted.");
            if(e.Rounds is not null&&e.LowSide.Members is not null&&e.HighSide.Members is not null&&e.LowSide.DestroyedMembers is not null&&e.HighSide.DestroyedMembers is not null)
                foreach(var receipt in e.Rounds)if(!ReceiptValid(receipt,e.LowSide.Members.Concat(e.LowSide.DestroyedMembers),e.HighSide.Members.Concat(e.HighSide.DestroyedMembers),content.Combat))Error("Round shots and damage must match the recorded armed sides.");
            if(live&&e.CompletedTurn is not null||!live&&e.CompletedTurn is null)Error("Completed date must match encounter phase.");
            foreach(var side in new[]{e.LowSide,e.HighSide})
            {
                if(side.Members is null||side.ProtectedCivilians is null||side.DestroyedMembers is null){Error("Side membership collections required.");continue;}
                if(side.ConsentRevision<1||!Orders(side.Orders,side.Owner))Error("Invalid side consent/orders.");
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
                    if(v?.Combat?.Repairing==true||state.FindEmpire(side.Owner)?.ShipDesigns?.Refits.Any(r=>r.Vessel==id)==true)Error("A live empire battle cannot also repair or refit its armed participant.");
                }
                foreach(var id in side.ProtectedCivilians)
                    if(state.FindVessel(id) is not {} v||v.EmpireId!=side.Owner||v.StationaryStarId!=e.Star||Armed(v)||side.Members.Contains(id))Error("Protected membership must contain only own co-located civilians.");
                if(side.Fleet is {} fleet)
                {
                    var f=state.FindEmpire(side.Owner)?.Fleets?.Fleets.SingleOrDefault(x=>x.Id==fleet);
                    if(f is null||side.Members.Any(x=>!f.Members.Contains(x))||side.ProtectedCivilians.Any(x=>!f.Members.Contains(x)))Error("Encounter fleet must retain its members.");
                    if(f is not null&&side.Flagship!=f.Flagship)Error("Live encounter flagship must equal the current fleet flagship.");
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
            if(o.Revision<1||o.NextEventId<1||o.Revision==long.MaxValue||o.NextEventId==long.MaxValue)Error("Invalid owner counters.");
            if(o.PublishedPressure is null||o.Preferences is null||o.Attacks is null||o.Blockades is null||o.Observations is null||o.Events is null||o.Notices is null||o.History is null){Error("Owner collections required.");continue;}
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
                if(f is null||target is null||target.EmpireId==o.Owner||a.Turn!=state.Turn||a.Revision<1||a.Revision==long.MaxValue||!Orders(a.Orders,o.Owner)||!tasked.Add(a.Fleet.Value))Error("Invalid own pending attack.");
                else if(!Armed(target)||target.StationaryStarId is not {} station||target.Transit is not null||a.Orders.ReturnColony is null||c.Pairs.All(p=>p.Pair!=DiplomacyPair.For(o.Owner,target.EmpireId)||p.Relation!=RelationKind.War)||f.Order is not {Kind:Fleets.FleetOrderKind.Hold}||f.Cohort is not null||f.Members is null||f.Members.Count is <1 or >32||f.Members.Any(id=>state.FindVessel(id) is not {} v||!Armed(v)||v.StationaryStarId!=station||v.Transit is not null||v.PendingMove is not null||v.PendingFounding is not null||v.NewlyCompleted||v.Combat is null||v.Combat.Repairing||v.AmebaEncounter is not null||used.Contains(id)||state.FindEmpire(o.Owner)!.ShipDesigns?.Refits?.Any(r=>r is not null&&r.Vessel==id)==true||state.Combat?.Encounters?.Any(e=>e is not null&&e.Members?.Contains(id)==true)==true||state.FindEmpire(o.Owner)!.Fleets?.Requests?.Any(r=>r is not null&&Fleets.FleetQueries.IsActive(r.Status)&&(r.RallyFleet==f.Id||r.Slots?.Any(s=>s?.Vessel==id)==true))==true))Error("Pending attack requires its own available armed fleet and local hostile armed anchor.");
            }
            foreach(var b in o.Blockades)
            {
                if(b is null){Error("Null blockade.");continue;}
                if(state.FindEmpire(o.Owner)!.Fleets?.Fleets.All(f=>f.Id!=b.Fleet)!=false||b.Other==o.Owner||state.FindEmpire(b.Other) is null||state.FindStar(b.Star) is null||!Past(b.AcceptedTurn)||b.Revision<1||!tasked.Add(b.Fleet.Value)||c.Pairs.All(p=>p?.Pair!=DiplomacyPair.For(o.Owner,b.Other)||p.Relation!=RelationKind.War))Error("Invalid live blockade authority.");
                // A historically selected target colony may have disappeared; resolution lifts it.
            }
            if(o.PublishedPressure.Any(p=>p is null)||o.PublishedPressure.Count>state.Stars.Count||o.PublishedPressure.Select(p=>p.Star).Distinct().Count()!=o.PublishedPressure.Count||o.PublishedPressure.Any(p=>p is null||p.Other==o.Owner||state.FindEmpire(p.Other) is null||state.FindStar(p.Star) is null||p.RetainedPercent!=d.BlockadeRetainedPercent||!c.Pairs.Any(pair=>pair.Pair==DiplomacyPair.For(o.Owner,p.Other)&&pair.Relation==RelationKind.War)))Error("Invalid published own blockade pressure.");
            if(o.Observations.Any(x=>x is null)||o.Events.Any(x=>x is null)||o.Notices.Any(x=>x is null)||o.History.Any(x=>x is null)){Error("Null owner history row.");continue;}
            if(o.Observations.Select(x=>x.Vessel).Distinct().Count()!=o.Observations.Count)Error("Duplicate military observation.");
            foreach(var obs in o.Observations)
            {
                if(obs.Vessel.Value<1||obs.Vessel.Value>=state.NextEntityId||obs.Owner==o.Owner||state.FindEmpire(obs.Owner) is null||state.FindStar(obs.Star) is null||!Past(obs.Turn)||obs.Current&&(obs.Turn!=state.Turn||!ConflictObservations.OwnPresence(state,o.Owner,obs.Star))||obs.Hull<1||obs.MaxHull<obs.Hull||obs.Attack<1||obs.Armor<0||obs.Speed<1||string.IsNullOrWhiteSpace(obs.RoleId)||!content.Vessels.ContainsKey(obs.RoleId)||obs.WeaponBanks.IsDefault)Error("Invalid current/stale military fact.");
                if(obs.Current&&!ObservationMatches(content,state,obs))Error("Current published equipment and hull fact is forged.");
            }
            if(o.Observations.Count(x=>!x.Current)>256)Error("Stale military history exceeds bound.");
            if(!Ordered(o.Events.Select(x=>x.Id)))Error("Events must be unique and ordered.");
            foreach(var ev in o.Events)
                if(ev.Id<1||ev.Id>=o.NextEventId||ev.Viewer!=o.Owner||ev.Other==o.Owner||state.FindEmpire(ev.Other) is null||!Past(ev.Turn)||!Text(ev.Kind,80)||!Text(ev.Reason,1000))Error("Invalid filtered event.");
            if(o.Notices.Select(x=>x.Key).Distinct(StringComparer.Ordinal).Count()!=o.Notices.Count||o.Notices.Any(x=>!Text(x.Key,128)||!Text(x.Reason,1000)||!Enum.IsDefined(x.Level)))Error("Invalid notices.");
            foreach(var h in o.History)
            {
                if(h.Encounter.Value<1||h.Encounter.Value>=c.NextEncounterId||h.Other==o.Owner||state.FindEmpire(h.Other) is null||state.FindStar(h.Star) is null||!Past(h.StartedTurn)||!Past(h.EndedTurn)||h.EndedTurn<h.StartedTurn||!Text(h.Outcome,200)||h.LostMembers is null||h.Rounds is null||h.Rounds.Count>d.RoundHistoryLimit||h.LostMembers.Any(id=>id.Value<1||id.Value>=state.NextEntityId||state.FindVessel(id) is not null))Error("Invalid historical encounter receipt.");
                if(h.Rounds is not null&&h.Rounds.Any(r=>!ReceiptValid(r,definition:content.Combat)))Error("Historical round shots and damage are inconsistent.");
            }
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
    private static bool ReceiptValid(ConflictRoundReceipt? r,IEnumerable<VesselId>? low=null,IEnumerable<VesselId>? high=null,Combat.CombatDefinition? definition=null)
    {
        if(r is null||r.Shots.IsDefaultOrEmpty||r.Own.IsDefaultOrEmpty||r.Opposing.IsDefaultOrEmpty||r.Own.Any(x=>x is null)||r.Opposing.Any(x=>x is null))return false;
        var rows=new Dictionary<long,(int Side,Combat.CombatShipDamage Damage)>();
        foreach(var row in r.Own)if(!rows.TryAdd(row.Id.Value,(0,row)))return false;
        foreach(var row in r.Opposing)if(!rows.TryAdd(row.Id.Value,(1,row)))return false;
        if(low is not null&&r.Own.Any(x=>!low.Contains(x.Id))||high is not null&&r.Opposing.Any(x=>!high.Contains(x.Id)))return false;
        var sources=new HashSet<long>();var loss=new Dictionary<long,Int128>();var sides=new HashSet<int>();
        foreach(var shot in r.Shots)
        {
            if(shot is null||shot.Source.Sentry||shot.Target.Sentry||shot.Damage<1||!rows.TryGetValue(shot.Source.Id,out var source)||!rows.TryGetValue(shot.Target.Id,out var target)||source.Side==target.Side||!sources.Add(shot.Source.Id))return false;
            sides.Add(source.Side);loss[shot.Target.Id]=loss.GetValueOrDefault(shot.Target.Id)+shot.Damage;
        }
        if(r.Interception?sides.Count!=1:sources.Count!=rows.Count)return false;
        if(r.Calculations.IsDefault||r.Calculations.Length!=r.Shots.Length||r.Calculations.Any(c=>c is null)||r.Calculations.Select(c=>c.Source).Distinct().Count()!=r.Calculations.Length)return false;
        foreach(var calculation in r.Calculations)
        {
            var shot=r.Shots.SingleOrDefault(s=>s.Source.Id==calculation.Source.Value&&s.Target.Id==calculation.Target.Value);
            if(shot is null||calculation.BaseAttack<1||calculation.SourceHull<1||calculation.SourceMaxHull<calculation.SourceHull||calculation.TargetArmor<0||calculation.SourceHull!=rows[calculation.Source.Value].Damage.Before||calculation.Damage!=shot.Damage)return false;
            int band=(int)Combat.CombatMath.Band(new(calculation.SourceMaxHull,calculation.BaseAttack,0),calculation.SourceHull,definition);
            int percent=definition?.AttackPercent[band]??new[]{100,50,25}[band];
            if(calculation.FirePercent!=percent||Int128.Max(1,(Int128)calculation.BaseAttack*percent/100-calculation.TargetArmor)!=calculation.Damage)return false;
        }
        foreach(var row in rows.Values)if(row.Damage.Before<1||row.Damage.After<0||row.Damage.ActualLoss!=Int128.Min(row.Damage.Before,loss.GetValueOrDefault(row.Damage.Id.Value))||row.Damage.After!=row.Damage.Before-row.Damage.ActualLoss)return false;
        return true;
    }
    private static bool ObservationMatches(ContentSet content,GameState state,MilitaryObservation row)
    {
        try
        {
            var ship=state.FindVessel(row.Vessel);if(ship is null||ship.EmpireId!=row.Owner||ship.RoleId!=row.RoleId||ship.StationaryStarId!=row.Star||ship.Transit is not null||ship.Combat?.Hull!=row.Hull)return false;
            var load=VesselLoadouts.Resolve(content,state,ship);var profile=load.Profile!;var speed=Combat.CombatMath.Stats(profile,ship.Combat.Hull,load.BaseSpeed,0,content.Combat).Speed;
            if(row.MaxHull!=profile.MaxHull||row.Attack!=profile.Attack||row.Armor!=profile.Armor||row.Speed!=speed)return false;
            var recipe=ship.Design is {} id?state.FindEmpire(ship.EmpireId)!.ShipDesigns!.Designs.Single(d=>d.Id==id).Recipe:null;
            // Grouping is deliberately the last published fact; private regrouping may differ.
            return row.FrameId==recipe?.FrameId&&row.EngineId==recipe?.EngineId&&row.ArmorId==recipe?.ArmorId&&row.WeaponBanks.SequenceEqual(recipe?.WeaponBankIds??System.Collections.Immutable.ImmutableArray<string>.Empty);
        }
        catch(Exception e)when(e is ArgumentException or InvalidOperationException or OverflowException or KeyNotFoundException){return false;}
    }
}
