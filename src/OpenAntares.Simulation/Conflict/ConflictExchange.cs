using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
public sealed record ConflictExchangeResult(ImmutableArray<CombatShipDamage> Low,ImmutableArray<CombatShipDamage> High,ImmutableArray<CombatShot> Shots,ImmutableArray<VesselId> Destroyed)
{
    public ImmutableArray<CombatShipForecast> LowForecasts {get;init;}=ImmutableArray<CombatShipForecast>.Empty;
    public ImmutableArray<CombatShipForecast> HighForecasts {get;init;}=ImmutableArray<CombatShipForecast>.Empty;
}
public static partial class ConflictExchange
{
    public static void Apply(ContentSet content,GameState snapshot,GameState candidate,ConflictResolutionContext context)
    {
        if(!context.MatchingResolved||context.ExchangesResolved||context.FromTurn!=snapshot.Turn||candidate.Turn!=snapshot.Turn)throw new InvalidOperationException("Conflict exchanges already applied or snapshot changed.");
        context.ExchangesResolved=true;if(!Rules.HasConflict(snapshot.RulesVersion))return;
        foreach(var frame in context.Battles)
        {
            var encounter=candidate.Conflict!.Encounters.Single(e=>e.Id==frame.Encounter);
            if(encounter.Phase==ConflictPhase.Completed||encounter.LastResolvedTurn==context.FromTurn||context.AcceptedPeace.Contains(frame.Pair)||context.PausedSet.Contains(frame.Encounter)||frame.LowWithdraw||frame.HighWithdraw||frame.LowDepartureStar is not null||frame.HighDepartureStar is not null)continue;
            ApplyResult(content,candidate,context,frame,Calculate(frame),false);
        }
    }
    internal static void ApplyResult(ContentSet content,GameState candidate,ConflictResolutionContext context,ConflictBattleFrame frame,ConflictExchangeResult result,bool interception)
    {
        var encounter=candidate.Conflict!.Encounters.Single(e=>e.Id==frame.Encounter);
        if(encounter.LastResolvedTurn==context.FromTurn||encounter.Phase==ConflictPhase.Completed||context.AcceptedPeace.Contains(frame.Pair)||context.PausedSet.Contains(frame.Encounter))throw new InvalidOperationException("An encounter cannot receive a second or suppressed volley.");
        var participants=frame.Low.Concat(frame.High).Select(s=>s.VesselId).ToArray();
        if(participants.Any(context.VolleyParticipants.Contains))throw new InvalidOperationException("A ship cannot participate in two volleys at one boundary.");
        context.VolleyParticipants.UnionWith(participants);
        foreach(var damage in result.Low.Concat(result.High))
        {
            var ship=candidate.FindVessel(damage.Id)??throw new InvalidOperationException("Frozen participant is unavailable.");if(ship.Combat!.Hull!=damage.Before)throw new InvalidOperationException("Frozen hull changed after matching.");
            var side=ship.EmpireId==frame.Pair.Low?encounter.LowSide:encounter.HighSide;var actor=frame.Low.Concat(frame.High).Single(s=>s.VesselId==ship.Id);
            ship.Combat.HullLost=checked(ship.Combat.HullLost+damage.ActualLoss);
            if(damage.After==0)
            {
                if(context.DestructionFacts.Any(d=>d.Vessel==ship.Id))throw new InvalidOperationException("Military loss applied twice.");
                context.DestructionFacts.Add(new(encounter.Id,ship.Id,ship.EmpireId,ship.RoleId,frame.Star,actor.Flagship));side.Members.Remove(ship.Id);side.DestroyedMembers.Add(ship.Id);side.DestroyedMembers.Sort();candidate.Vessels.Remove(ship);
            }
            else {ship.Combat.Hull=damage.After;ship.Combat.Cohesion=result.LowForecasts.Concat(result.HighForecasts).Single(f=>f.Id==ship.Id).CohesionAfter;}
        }
        encounter.Round=checked(encounter.Round+1);encounter.LastResolvedTurn=context.FromTurn;encounter.Revision=checked(encounter.Revision+1);
        encounter.Rounds.Add(new(context.FromTurn,encounter.Round,interception,result.Shots,result.Low,result.High));while(encounter.Rounds.Count>content.Conflict!.RoundHistoryLimit)encounter.Rounds.RemoveAt(0);
        context.RoundFacts.Add(new(frame,result,interception));
        foreach(var side in new[]{encounter.LowSide,encounter.HighSide})
        {
            side.ConsentRevision=checked(side.ConsentRevision+1);
            var actors=side.Owner==frame.Pair.Low?frame.Low:frame.High;var damage=side.Owner==frame.Pair.Low?result.Low:result.High;
            int cohesion=side.Members.Select(id=>candidate.FindVessel(id)!.Combat!.Cohesion).DefaultIfEmpty(0).Min();bool crippled=side.Members.Any(id=>candidate.FindVessel(id) is {} ship&&CombatMath.Band(new(actors.Single(s=>s.VesselId==id).MaxHull,actors.Single(s=>s.VesselId==id).Attack,actors.Single(s=>s.VesselId==id).Armor),ship.Combat!.Hull,content.Combat)==CombatDamageBand.Crippled);
            if(!interception)side.WithdrawQueued=side.Orders.Doctrine switch{CombatDoctrine.Cautious=>crippled||damage.Any(d=>d.After==0)||cohesion<=content.Combat!.RetreatCohesionThreshold,CombatDoctrine.Press=>cohesion<=content.Combat!.RetreatCohesionThreshold,CombatDoctrine.HitAndRun=>true,_=>false};
            candidate.Conflict.Pairs.Single(p=>p.Pair==frame.Pair).Parties.Single(p=>p.Owner==side.Owner).LastVisibleActivityTurn=checked(context.FromTurn+1);
            ConflictRelations.RecordEvent(candidate,side.Owner,frame.Pair.Other(side.Owner),interception?"interception":"exchange",interception?"One enemy-only interception volley resolved before departure.":"One frozen simultaneous exchange resolved. Damage and losses are retained; no ownership or loot is awarded.",frame.Star);
        }
        if(encounter.LowSide.Members.Count==0||encounter.HighSide.Members.Count==0)
        {
            encounter.Phase=ConflictPhase.Completed;encounter.CompletedTurn=checked(context.FromTurn+1);
            foreach(var side in new[]{encounter.LowSide,encounter.HighSide}){side.WithdrawQueued=false;side.NeedsResponse=false;if(side.Fleet is {} id&&candidate.FindEmpire(side.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==id) is {} fleet)fleet.Order=new(FleetOrderKind.Hold);}
        }
        else encounter.Phase=ConflictPhase.Active;
    }
    public static ConflictExchangeResult Calculate(ConflictBattleFrame frame)=>Calculate(frame,true,true,null,null);
    internal static ConflictExchangeResult Calculate(ConflictBattleFrame frame,bool lowFires,bool highFires,IReadOnlySet<VesselId>? exposedLow,IReadOnlySet<VesselId>? exposedHigh)
    {
        if(frame.Definition is null||frame.Low.IsDefaultOrEmpty||frame.High.IsDefaultOrEmpty||frame.Low.Length>32||frame.High.Length>32||frame.Low.Concat(frame.High).Select(s=>s.VesselId).Distinct().Count()!=frame.Low.Length+frame.High.Length)throw new ArgumentException("A bounded two-sided armed frame is required.");
        var low=frame.Low.OrderBy(s=>s.VesselId).ToArray();var high=frame.High.OrderBy(s=>s.VesselId).ToArray();
        foreach(var ship in low.Concat(high))
        {if(ship.VesselId.Value<1||ship.Attack<1||ship.Cohesion is <0 or >100||ship.EffectiveSpeed<1||ship.Cargo!=0)throw new ArgumentException("Invalid armed participant.");CombatMath.Band(new(ship.MaxHull,ship.Attack,ship.Armor),ship.Hull,frame.Definition);}
        var shots=new List<CombatShot>();
        void Fire(CombatShipFrame[] actors,CombatShipFrame[] targets,IReadOnlySet<VesselId>? exposed)
        {
            var target=targets.FirstOrDefault(t=>exposed is null||exposed.Contains(t.VesselId));if(target is null)return;
            foreach(var actor in actors)
            {int fire=CombatMath.Stats(new(actor.MaxHull,actor.Attack,actor.Armor),actor.Hull,1,0,frame.Definition).Firepower;shots.Add(new(new(false,actor.VesselId.Value),new(false,target.VesselId.Value),Math.Max(1,fire-target.Armor)));}
        }
        if(lowFires)Fire(low,high,exposedHigh);if(highFires)Fire(high,low,exposedLow);
        var ordered=shots.OrderBy(s=>s.Source.Id).ThenBy(s=>s.Target.Id).ToImmutableArray();
        ImmutableArray<CombatShipDamage> Damage(CombatShipFrame[] ships)=>ships.Select(s=>
        {Int128 total=0;foreach(var shot in ordered)if(shot.Target.Id==s.VesselId.Value)total=checked(total+shot.Damage);int loss=checked((int)Int128.Min(s.Hull,total));return new CombatShipDamage(s.VesselId,s.Hull,s.Hull-loss,loss);}).ToImmutableArray();
        var own=Damage(low);var other=Damage(high);var destroyed=own.Concat(other).Where(s=>s.After==0).Select(s=>s.Id).OrderBy(id=>id).ToImmutableArray();
        return new(own,other,ordered,destroyed){LowForecasts=Forecast(frame,low,own),HighForecasts=Forecast(frame,high,other)};
    }
    private static ImmutableArray<CombatShipForecast> Forecast(ConflictBattleFrame frame,CombatShipFrame[] ships,ImmutableArray<CombatShipDamage> damage)
    {
        var d=frame.Definition;
        int destroyed=damage.Count(s=>s.After==0);int crippled=ships.Count(s=>damage.Single(v=>v.Id==s.VesselId).After>0&&Band(s,s.Hull)!=CombatDamageBand.Crippled&&Band(s,damage.Single(v=>v.Id==s.VesselId).After)==CombatDamageBand.Crippled);
        bool flagship=ships.Any(s=>s.Flagship&&damage.Single(v=>v.Id==s.VesselId).After==0);
        int loss=checked((int)Int128.Min(100,(Int128)destroyed*d.DestroyedCohesionLoss+(Int128)crippled*d.CrippledCohesionLoss+(flagship?d.FlagshipCohesionLoss:0)));
        return ships.Select(s=>{int hull=damage.Single(v=>v.Id==s.VesselId).After;return new CombatShipForecast(s.VesselId,Band(s,s.Hull),hull>0?Band(s,hull):null,s.Cohesion,hull>0?Math.Max(0,s.Cohesion-loss):s.Cohesion);}).ToImmutableArray();
        CombatDamageBand Band(CombatShipFrame ship,int hull)=>CombatMath.Band(new(ship.MaxHull,ship.Attack,ship.Armor),hull,d);
    }
}
