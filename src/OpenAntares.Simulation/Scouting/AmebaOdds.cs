using System;
using OpenAntares.Simulation.Content;
namespace OpenAntares.Simulation.Scouting;
public static class AmebaOdds
{
    public static int LateEscape(AmebaDefinition d,int speed)=>Calculate(d,d.LateEscape,speed);
    public static int Study(AmebaDefinition d,int speed)=>Calculate(d,d.Study,speed);
    public static bool Succeeds(int percent,int draw)
    {
        if(percent is <0 or >100||draw is <0 or >99)throw new ArgumentOutOfRangeException();
        return draw<percent;
    }
    private static int Calculate(AmebaDefinition d,ProbabilityRule rule,int speed)
    {
        if(speed<=0||d.PursuitSpeed<=0)throw new ArgumentOutOfRangeException(nameof(speed));
        Int128 numerator=(Int128)rule.RelativeSpeedCoefficient*((Int128)speed-d.PursuitSpeed);
        Int128 quotient=numerator/d.PursuitSpeed;
        if(numerator<0&&numerator%d.PursuitSpeed!=0)quotient--;
        Int128 result=rule.BasePercent+quotient;
        return (int)Int128.Clamp(result,rule.MinimumPercent,rule.MaximumPercent);
    }
}
