using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
namespace OpenAntares.Simulation.Combat;
public static class CombatExchange
{
    public static CombatExchangeResult Calculate(CombatFrame frame,bool interception,ImmutableArray<VesselId> exposed)
    {
        if(frame.Ships.IsDefault||frame.Sentries.IsDefault||exposed.IsDefault||frame.Ships.Length>32||frame.Sentries.Length>2||frame.Ships.Select(s=>s.VesselId).Distinct().Count()!=frame.Ships.Length||frame.Sentries.Select(s=>s.SentryId).Distinct().Count()!=frame.Sentries.Length)throw new ArgumentException("Invalid bounded combat frame.");
        var ships=frame.Ships.OrderBy(s=>s.VesselId).ToArray();var sentries=frame.Sentries.OrderBy(s=>s.SentryId).ToArray();
        foreach(var ship in ships){if(ship.VesselId.Value<=0||ship.Cohesion is <0 or >100||ship.EffectiveSpeed<=0||ship.Cargo<0)throw new ArgumentException("Invalid ship frame.");CombatMath.Band(new(ship.MaxHull,ship.Attack,ship.Armor),ship.Hull,frame.Definition);}
        foreach(var sentry in sentries){if(sentry.SentryId.Value<=0||sentry.PursuitSpeed<=0)throw new ArgumentException("Invalid sentry frame.");CombatMath.Band(new(sentry.MaxHull,sentry.Attack,sentry.Armor),sentry.Hull,frame.Definition);}
        if(exposed.Distinct().Count()!=exposed.Length||exposed.Any(id=>!ships.Any(s=>s.VesselId==id)))throw new ArgumentException("Invalid exposed cohort.");
        var shots=ImmutableArray.CreateBuilder<CombatShot>();
        // Every target is selected from the same pre-volley frame. No shot can spill over.
        var enemy=sentries.OrderByDescending(s=>s.Attack>0).ThenBy(s=>s.SentryId).FirstOrDefault();
        if(!interception&&enemy is not null)foreach(var ship in ships.Where(s=>s.Attack>0))
        {
            int fire=CombatMath.Stats(new(ship.MaxHull,ship.Attack,ship.Armor),ship.Hull,1,0,frame.Definition).Firepower;
            shots.Add(new(new(false,ship.VesselId.Value),new(true,enemy.SentryId.Value),Math.Max(1,fire-enemy.Armor)));
        }
        var target=ships.Where(s=>!interception||exposed.Contains(s.VesselId)).OrderByDescending(s=>s.Attack>0).ThenBy(s=>s.VesselId).FirstOrDefault();
        if(target is not null)foreach(var sentry in sentries.Where(s=>s.Attack>0))
        {
            int fire=CombatMath.Stats(new(sentry.MaxHull,sentry.Attack,sentry.Armor),sentry.Hull,1,0,frame.Definition).Firepower;
            shots.Add(new(new(true,sentry.SentryId.Value),new(false,target.VesselId.Value),Math.Max(1,fire-target.Armor)));
        }
        int Loss(CombatActor target,int hull)
        {
            Int128 total=0;foreach(var shot in shots)if(shot.Target==target)total=checked(total+shot.Damage);
            return checked((int)Int128.Min(hull,total));
        }
        var own=ships.Select(s=>{int loss=Loss(new(false,s.VesselId.Value),s.Hull);return new CombatShipDamage(s.VesselId,s.Hull,s.Hull-loss,loss);}).ToImmutableArray();
        var other=sentries.Select(s=>{int loss=Loss(new(true,s.SentryId.Value),s.Hull);return new SentryDamage(s.SentryId,s.Hull,s.Hull-loss,loss);}).ToImmutableArray();
        return new(own,other,shots.ToImmutable(),own.Where(s=>s.After==0).Select(s=>s.Id).ToImmutableArray(),other.Where(s=>s.After==0).Select(s=>s.Id).ToImmutableArray());
    }
}
