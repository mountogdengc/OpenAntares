using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
public static class CombatStateValidator
{
    public static IReadOnlyList<string> Validate(ContentSet content,GameState state)
    {
        var errors=new List<string>();void Error(string message)=>errors.Add("combat: "+message);
        if(!Rules.HasCombat(state.RulesVersion))
        {
            if(state.Combat is not null||state.Empires.Any(e=>e.Combat is not null)||state.Vessels.Any(v=>v.Combat is not null))Error("old rules cannot contain combat fields");
            return errors;
        }
        if(content.Combat is not {} d||state.Combat is not {} game){Error("explicit content and setup required, including Off");return errors;}
        if(game.NextPocketId is <1 or int.MaxValue||game.NextEncounterId is <1 or long.MaxValue)Error("local counter outside bounds");
        Sorted(game.Pockets.Select(p=>(long)p.Id.Value),"pocket IDs");Sorted(game.Encounters.Select(e=>e.Id.Value),"encounter IDs");
        if(game.Pockets.Count>d.PocketLimit||game.SentriesEnabled&&game.Pockets.Count!=1||!game.SentriesEnabled&&(game.Pockets.Count!=0||game.Encounters.Count!=0||game.NextPocketId!=1||game.NextEncounterId!=1))Error("Standard/Off pocket count or counter");
        if(game.Pockets.GroupBy(p=>p.Star).Any(g=>g.Count()>1))Error("duplicate pocket star");
        foreach(var pocket in game.Pockets)
        {
            if(pocket.Id.Value<=0||pocket.Id.Value>=game.NextPocketId||state.FindStar(pocket.Star) is null||state.Scouting?.Habitat?.Star==pocket.Star)Error("invalid pocket reference/local ID");
            if(!pocket.Cleared&&state.Colonies.Any(c=>state.FindPlanet(c.PlanetId)?.StarId==pocket.Star))Error("guarded pocket cannot have a colony");
            if(pocket.Sentries.Count!=d.SentriesPerPocket||pocket.NextSentryId is <1 or int.MaxValue)Error("sentry count/local counter");
            Sorted(pocket.Sentries.Select(a=>(long)a.Id.Value),"sentry IDs");
            foreach(var sentry in pocket.Sentries)
                if(sentry.Id.Value<=0||sentry.Id.Value>=pocket.NextSentryId||sentry.Profile!=d.SentryProfile||sentry.Hull<0||sentry.Hull>sentry.Profile.MaxHull)Error("invalid sentry ID/profile/hull");
            if(pocket.Cleared!=pocket.Sentries.All(a=>a.Hull==0))Error("cleared flag must agree with destroyed sentries");
        }
        if(game.Encounters.GroupBy(e=>e.Pocket).Any(g=>g.Count()>1)||game.Encounters.GroupBy(e=>(e.Owner,e.Fleet)).Any(g=>g.Count()>1)||game.Encounters.SelectMany(e=>e.Members).GroupBy(v=>v).Any(g=>g.Count()>1))Error("duplicate combat reservation");
        foreach(var encounter in game.Encounters)
        {
            var owner=state.FindEmpire(encounter.Owner);var fleet=owner?.Fleets?.Fleets.FirstOrDefault(f=>f.Id==encounter.Fleet);var pocket=game.Pockets.FirstOrDefault(p=>p.Id==encounter.Pocket);
            if(encounter.Id.Value<=0||encounter.Id.Value>=game.NextEncounterId||owner is null||fleet is null||pocket is null||pocket.Cleared)Error("invalid encounter references/local ID");
            if(encounter.HullLost<0||encounter.CargoLost<0||encounter.DestroyedMembers.Count>32||encounter.Members.Count+encounter.DestroyedMembers.Count>32||encounter.DestroyedMembers.Any(v=>v.Value<=0||v.Value>=state.NextEntityId||state.FindVessel(v) is not null))Error("invalid encounter loss counters/actors");
            Sorted(encounter.DestroyedMembers.Select(v=>(long)v.Value),"destroyed member IDs");
            Details(encounter.Rounds,encounter.Id,encounter.Round);
            if(encounter.Round>0&&encounter.Rounds.Count==0)Error("resolved encounter missing its bounded latest receipt");
            if(encounter.ContinueRiskTurn is {} continued&&(continued<0||continued>state.Turn))Error("invalid continued-risk authorization turn");
            if(!Enum.IsDefined(encounter.Phase)||encounter.Revision is <1 or long.MaxValue||encounter.Round<0||encounter.Round>(long)state.Turn+1||encounter.ConsentRevision is {} consent&&(consent<1||consent>encounter.Revision))Error("encounter phase/revision/round/consent bounds");
            if((encounter.LastResolvedTurn is null)!=(encounter.LastResolvedPhase is null)||encounter.LastResolvedTurn is {} turn&&(turn<0||turn>state.Turn)||encounter.LastResolvedPhase is {} phase&&!Enum.IsDefined(phase))Error("last resolution phase/turn");
            if(encounter.Round==0&&encounter.LastResolvedTurn is not null||encounter.Round>0&&encounter.LastResolvedTurn is null||encounter.Phase==CombatPhase.Pending&&encounter.Round!=0)Error("round and resolution state conflict");
            if(owner?.Fleets is {} command&&command.Requests.Any(r=>Fleets.FleetQueries.IsActive(r.Status)&&(r.RallyFleet==encounter.Fleet||r.Slots.Any(slot=>slot.Vessel is {} member&&encounter.Members.Contains(member)&&slot.Status!=Fleets.SlotStatus.Missing))))Error("combat and finite request authorities conflict");
            if(fleet is not null&&(fleet.Order is not {Kind:Fleets.FleetOrderKind.Hold}||fleet.Cohort is not null))Error("combat conflicts with persistent fleet orders/transit");
            var orders=encounter.Orders;var port=state.FindColony(orders.ReturnColony);
            if(!Enum.IsDefined(orders.Doctrine)||!Enum.IsDefined(orders.Stragglers)||!Enum.IsDefined(orders.OnArrival)||port?.EmpireId!=encounter.Owner&&encounter.Phase!=CombatPhase.BlockedResponse&&encounter.ContinueRiskTurn!=state.Turn||port is not null&&state.FindPlanet(port.PlanetId)?.StarId==pocket?.Star)Error("invalid return orders");
            Sorted(encounter.Members.Select(v=>(long)v.Value),"encounter member IDs");
            if(encounter.Members.Count==0||fleet is not null&&!fleet.Members.SequenceEqual(encounter.Members))Error("encounter membership differs from committed fleet");
            foreach(var id in encounter.Members)
            {
                var v=state.FindVessel(id);
                if(v is null||v.EmpireId!=encounter.Owner||v.Fleet!=encounter.Fleet||v.Transit is not null||v.StationaryStarId!=pocket?.Star||v.PendingMove is not null||v.PendingFounding is not null||v.Combat?.Repairing==true)Error("invalid participating vessel/order conflict");
            }
            bool response=owner?.Combat?.Responses.Any(r=>r.Encounter==encounter.Id&&r.Revision==encounter.Revision)==true;
            if((encounter.Phase==CombatPhase.BlockedResponse)!=response)Error("blocked phase requires exactly its current response");
        }
        foreach(var owner in state.Empires)
        {
            if(owner.Combat is not {} c){Error($"{owner.Id} missing combat metadata");continue;}
            if(c.LifetimeHullLost is <0 or long.MaxValue||c.LifetimeCargoLost is <0 or long.MaxValue)Error("lifetime loss counter outside bounds");
            if(c.NextEventId is <1 or long.MaxValue||c.LifetimeEncounters is <0 or long.MaxValue||c.LifetimeShipsLost is <0 or long.MaxValue)Error("owner counter outside bounds");
            if(!game.SentriesEnabled&&(c.Observations.Count!=0||c.Responses.Count!=0||c.History.Count!=0||c.LifetimeEncounters!=0||c.LifetimeShipsLost!=0))Error("Off has encounter metadata");
            Sorted(c.Observations.Select(o=>(long)o.Pocket.Value),"observation IDs");
            foreach(var o in c.Observations)
            {
                var p=game.Pockets.FirstOrDefault(p=>p.Id==o.Pocket);
                if(p is null||p.Star!=o.Star||o.Turn<0||o.Turn>state.Turn||o.Sentries.IsDefault||o.Sentries.Length!=d.SentriesPerPocket)Error("invalid pocket observation reference/turn");
                else
                {
                    Sorted(o.Sentries.Select(a=>(long)a.Id.Value),"observed sentry IDs");
                    if(o.Sentries.Any(a=>!p.Sentries.Any(s=>s.Id==a.Id)||a.Profile!=d.SentryProfile||a.Hull<0||a.Hull>a.Profile.MaxHull||a.PursuitSpeed!=d.SentryPursuitSpeed)||o.Cleared!=o.Sentries.All(a=>a.Hull==0))Error("invalid observed sentry facts");
                }
            }
            Sorted(c.Responses.Select(r=>r.Encounter.Value),"response IDs");
            if(c.Responses.Any(r=>!game.Encounters.Any(e=>e.Id==r.Encounter&&e.Owner==owner.Id&&e.Revision==r.Revision&&e.Phase==CombatPhase.BlockedResponse)||string.IsNullOrWhiteSpace(r.Reason)||(int)r.Need is <1 or >3))Error("invalid pending response");
            if(c.History.Count>d.HistoryLimit||c.History.Select(h=>h.Encounter).Distinct().Count()!=c.History.Count)Error("history bound/duplicates");
            foreach(var h in c.History)
            {
                Details(h.Details,h.Encounter,h.Rounds);
                if(h.HullLost<0||h.CargoLost<0)Error("invalid retained loss totals");
                if(h.Encounter.Value<=0||h.Encounter.Value>=game.NextEncounterId||!game.Pockets.Any(p=>p.Id==h.Pocket)||game.Encounters.Any(e=>e.Id==h.Encounter)||h.Turn<0||h.Turn>state.Turn||h.Rounds<0||h.Rounds>(long)h.Turn+1||string.IsNullOrWhiteSpace(h.Outcome)||h.Destroyed.IsDefault||h.Destroyed.Any(v=>v.Value<=0||v.Value>=state.NextEntityId)||h.Destroyed.Distinct().Count()!=h.Destroyed.Length)Error("invalid encounter history");
            }
            var events=c.RecentEvents.Concat(c.PendingEvents).ToArray();
            if(c.RecentEvents.Count>d.HistoryLimit||c.PendingEvents.Count>d.HistoryLimit||events.Select(e=>e.Id).Distinct().Count()!=events.Length)Error("event bounds/duplicates");
            Sorted(c.RecentEvents.Select(e=>e.Id),"recent event IDs");Sorted(c.PendingEvents.Select(e=>e.Id),"pending event IDs");
            if(events.Any(e=>e.Id<=0||e.Id>=c.NextEventId||e.Turn<0||e.Turn>state.Turn||string.IsNullOrWhiteSpace(e.Kind)||string.IsNullOrWhiteSpace(e.Reason)||e.Encounter is {} id&&(id.Value<=0||id.Value>=game.NextEncounterId)||e.Vessel is {} v&&(v.Value<=0||v.Value>=state.NextEntityId)))Error("invalid event facts");
        }
        foreach(var vessel in state.Vessels)
        {
            if(vessel.Combat is not {} c){Error($"{vessel.Id} missing hull/cohesion");continue;}
            if(!d.RoleProfiles.TryGetValue(vessel.RoleId,out var profile)||c.Hull<=0||c.Hull>profile.MaxHull||c.Cohesion is <0 or >100)Error("invalid live hull/cohesion");
            if((c.ReturnColony is null)!=(c.OnArrival is null)||c.OnArrival is {} arrival&&!Enum.IsDefined(arrival)||c.ReturnColony is {} port&&state.FindColony(port)?.EmpireId!=vessel.EmpireId)Error("invalid ship return/arrival authority");
            var command=state.FindEmpire(vessel.EmpireId)?.Fleets;var group=command?.Fleets.FirstOrDefault(f=>f.Id==vessel.Fleet);
            bool authorityConflict=group is not null&&group.Order is not {Kind:Fleets.FleetOrderKind.Hold}||command?.Requests.Any(r=>Fleets.FleetQueries.IsActive(r.Status)&&(group is not null&&r.RallyFleet==group.Id||r.Slots.Any(s=>s.Vessel==vessel.Id&&s.Status!=Fleets.SlotStatus.Missing)))==true;
            if(c.Repairing&&(authorityConflict||c.ReturnColony is not {} destination||vessel.Transit is not null||vessel.PendingMove is not null||vessel.PendingFounding is not null||state.FindColony(destination) is not {} repairColony||state.FindPlanet(repairColony.PlanetId)?.StarId!=vessel.StationaryStarId||c.OnArrival!=CombatArrivalOrder.Repair||game.Encounters.Any(e=>e.Members.Contains(vessel.Id))))Error("repair state conflicts with location/orders");
        }
        return errors;
        void Details(IEnumerable<CombatRoundReceipt> records,EncounterId id,long total)
        {
            var receipts=records.ToArray();if(receipts.Length>d.RoundDetailLimit)Error("round detail limit");long previous=0;int previousTurn=-1;
            foreach(var r in receipts)
            {
                if(r.Encounter!=id||r.Round<=previous||r.Round>total||r.Round<1||r.Turn<=previousTurn||r.Turn<0||r.Turn>state.Turn||r.Round>(long)r.Turn+1)Error("invalid round receipt identity/order/turn");
                previous=r.Round;previousTurn=r.Turn;
                if(r.Before.EncounterId!=id||r.Before.Turn!=r.Turn||r.Before.Revision is <1 or long.MaxValue||r.Before.Ships.IsDefault||r.Before.Sentries.IsDefault||r.Before.Ships.Length>32||r.Before.Sentries.Length>2||r.Result.Ships.IsDefault||r.Result.Sentries.IsDefault||r.Result.Shots.IsDefault||r.Result.DestroyedVessels.IsDefault||r.Result.DestroyedSentries.IsDefault||r.Forecasts.IsDefault)Error("invalid round receipt frame/result");
            }
        }
        void Sorted(IEnumerable<long> ids,string name){long last=0;foreach(long id in ids){if(id<=last){Error("unsorted/duplicate "+name);break;}last=id;}}
    }
}
