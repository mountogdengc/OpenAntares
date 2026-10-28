using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Movement;
namespace OpenAntares.Simulation.Fleets;
public static class FleetTravel
{
    internal static string? ConsequenceFingerprint(ContentSet content, GameState state, EmpireId owner, VesselId id, StarId? destination)
    {
        var vessel=state.FindVessel(id)!;var f=state.FindEmpire(owner)!.Fleets!;
        var group=f.Fleets.SingleOrDefault(x=>x.Id==vessel.Fleet);
        var officer=f.Officers.SingleOrDefault(x=>x.Id==group?.Officer&&x.Status==OfficerStatus.Commanding);
        if(group is null&&officer is null)return null;
        var facts=new{Group=group?.Id,Members=group?.Members.OrderBy(x=>x).ToArray(),Flagship=group?.Flagship,Commander=group?.Officer,Officer=officer is null?null:new{officer.Id,officer.Xp,officer.Rank,officer.Specialty,officer.Doctrine},Equipment=state.FindEmpire(owner)!.KnownTechnologyIds.Contains(content.FleetCommand!.RecoveryTechnologyId),Destination=destination,Duration=destination is {} target?LaunchDuration(content,state,vessel,target):(int?)null};
        return System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(facts)));
    }
    public static int Duration(ContentSet content, GameState state, EmpireId owner, FleetId fleet, StarId destination)
    {
        var group = state.FindEmpire(owner)!.Fleets!.Fleets.Single(f => f.Id == fleet);
        var vessel = state.FindVessel(group.Flagship)!;
        var from = state.FindStar(vessel.StationaryStarId!.Value)!; var to = state.FindStar(destination)!;
        int speed = group.Members.Min(id => OfficerBonuses.EffectiveSpeed(content, state, id));
        return TravelCalculator.Duration(from.X, from.Y, to.X, to.Y, speed);
    }
    internal static int PreviewDuration(ContentSet content, Knowledge.EmpireView view, Knowledge.OwnedVesselView vessel, StarId target)
    {
        var a=view.Galaxy.Stars.Single(s=>s.Id==vessel.StationaryStarId);var b=view.Galaxy.Stars.Single(s=>s.Id==target);
        int speed=content.Vessels[vessel.RoleId].Speed;
        if(Rules.HasFleets(view.RulesVersion)&&view.Fleets is {} f&&f.Ships.Single(x=>x.Id==vessel.Id).Fleet is {} id)
        {
            var fleet=f.Fleets.Single(x=>x.Id==id);var officer=f.Officers.SingleOrDefault(o=>o.Id==fleet.Officer&&o.Status==OfficerStatus.Commanding);
            int bonus=officer is {Specialty:OfficerSpecialty.Navigator,Rank:>0}?content.FleetCommand!.NavigatorBonuses[officer.Rank-1]:0;
            speed=fleet.Members.Min(member=>checked((int)((long)content.Vessels[f.Ships.Single(x=>x.Id==member).RoleId].Speed*(100+bonus)/100)));
        }
        return TravelCalculator.Duration(a.X,a.Y,b.X,b.Y,speed);
    }
    public static int LaunchDuration(ContentSet content, GameState state, VesselState vessel, StarId destination)
    {
        if (Rules.HasFleets(state.RulesVersion) && vessel.Fleet is { } fleet) return Duration(content, state, vessel.EmpireId, fleet, destination);
        var from = state.FindStar(vessel.StationaryStarId!.Value)!; var to = state.FindStar(destination)!;
        return TravelCalculator.Duration(from.X, from.Y, to.X, to.Y, OfficerBonuses.EffectiveSpeed(content, state, vessel.Id));
    }
}
