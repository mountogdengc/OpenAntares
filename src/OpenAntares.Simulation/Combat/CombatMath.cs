using System;
namespace OpenAntares.Simulation.Combat;
public static class CombatMath
{
    public static CombatStats Stats(CombatProfile profile,int hull,int baseSpeed,int navigatorBonus)=>Stats(profile,hull,baseSpeed,navigatorBonus,null);
    public static CombatStats Stats(CombatProfile profile,int hull,int baseSpeed,int navigatorBonus,CombatDefinition? definition)
    {
        if(baseSpeed<=0||navigatorBonus<0)throw new ArgumentOutOfRangeException(nameof(baseSpeed));
        int band=(int)Band(profile,hull,definition);
        int fire=definition is null?new[]{100,50,25}[band]:definition.AttackPercent[band];
        int travel=definition is null?new[]{100,75,50}[band]:definition.SpeedPercent[band];
        int attack=checked((int)((Int128)profile.Attack*fire/100));
        Int128 damaged=(Int128)baseSpeed*travel/100;
        int speed=checked((int)Int128.Max(1,damaged*((Int128)100+navigatorBonus)/100));
        return new(attack,profile.Armor,speed);
    }
    public static CombatDamageBand Band(CombatProfile profile,int hull,CombatDefinition? definition=null)
    {
        if(profile.MaxHull<=0||profile.Attack<0||profile.Armor<0||hull<=0||hull>profile.MaxHull)throw new ArgumentOutOfRangeException(nameof(hull));
        int damaged=definition?.HullThresholds[0]??50,crippled=definition?.HullThresholds[1]??25;
        return (Int128)hull*100>(Int128)profile.MaxHull*damaged?CombatDamageBand.Operational:(Int128)hull*100>(Int128)profile.MaxHull*crippled?CombatDamageBand.Damaged:CombatDamageBand.Crippled;
    }
}
