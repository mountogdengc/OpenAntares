using System;
namespace OpenAntares.Simulation.Movement;
public sealed record RoutePreview(StarId Origin,StarId Destination,Int128 SquaredDistance,long CeilingDistance,int Speed,int Turns);
public static class TravelCalculator
{
    public static int Duration(int x1,int y1,int x2,int y2,int speed)
    {
        if(speed<=0)throw new ArgumentOutOfRangeException(nameof(speed));
        Int128 dx=(Int128)x2-x1,dy=(Int128)y2-y1,d2=dx*dx+dy*dy;
        bool Covers(long n)=> (Int128)n*n*speed*speed>=d2;
        if(!Covers(int.MaxValue))throw new OverflowException("Travel duration exceeds supported turns.");
        long lo=1,hi=int.MaxValue;
        while(lo<hi){long mid=lo+(hi-lo)/2;if(Covers(mid))hi=mid;else lo=mid+1;}
        return (int)lo;
    }
    public static RoutePreview Preview(StarId origin,StarId destination,int x1,int y1,int x2,int y2,int speed)
    {
        Int128 dx=(Int128)x2-x1,dy=(Int128)y2-y1,d2=dx*dx+dy*dy;
        long lo=0,hi=1L<<33;
        while(lo<hi){long mid=lo+(hi-lo)/2;if((Int128)mid*mid>=d2)hi=mid;else lo=mid+1;}
        return new(origin,destination,d2,lo,speed,Duration(x1,y1,x2,y2,speed));
    }
}
