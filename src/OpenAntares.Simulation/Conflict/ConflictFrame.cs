using System.Linq;
using System.Collections.Immutable;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Diplomacy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.ShipDesigns;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Conflict;
// Private calculator input. Public reports must project each viewer's own side separately.
public sealed record ConflictBattleFrame(ConflictEncounterId Encounter,DiplomacyPair Pair,StarId Star,int Turn,long Round,ConflictPhase Phase,ImmutableArray<CombatShipFrame> Low,ImmutableArray<CombatShipFrame> High,ConflictOrders LowOrders,ConflictOrders HighOrders)
{
    public CombatDefinition Definition {get;init;}=null!;
    public bool LowWithdraw {get;init;}
    public bool HighWithdraw {get;init;}
    public StarId? LowDepartureStar {get;init;}
    public StarId? HighDepartureStar {get;init;}
    internal static ConflictBattleFrame Capture(ContentSet content,GameState state,ConflictEncounterState encounter)
    {
        ImmutableArray<CombatShipFrame> Ships(ConflictSideState side)=>side.Members.OrderBy(id=>id).Select(id=>
        {
            var ship=state.FindVessel(id)!;var load=VesselLoadouts.Resolve(content,state,ship);var profile=load.Profile!;
            bool participatingFlagship=side.Members.Contains(side.Flagship);int bonus=participatingFlagship?OfficerBonuses.NavigatorBonus(content,state,ship):0;
            return new CombatShipFrame(id,ship.RoleId,ship.Combat!.Hull,profile.MaxHull,ship.Combat.Cohesion,profile.Attack,profile.Armor,CombatMath.Stats(profile,ship.Combat.Hull,load.BaseSpeed,bonus,content.Combat).Speed,id==side.Flagship,ship.CarriedPopulation){Equipment=VesselLoadouts.Fact(content,state,ship)};
        }).ToImmutableArray();
        StarId? Departure(ConflictSideState side)
        {
            var fleet=state.FindEmpire(side.Owner)!.Fleets!.Fleets.SingleOrDefault(f=>f.Id==side.Fleet);
            var destination=side.Members.Select(state.FindVessel).Where(v=>v?.PendingMove is not null).Select(v=>v!.PendingMove).FirstOrDefault()??(fleet?.Order.Kind is FleetOrderKind.Return or FleetOrderKind.Assemble?fleet.Order.Destination:null);
            return destination==encounter.Star?null:destination;
        }
        return new(encounter.Id,encounter.Pair,encounter.Star,state.Turn,encounter.Round,encounter.Phase,Ships(encounter.LowSide),Ships(encounter.HighSide),encounter.LowSide.Orders,encounter.HighSide.Orders){Definition=content.Combat!,LowWithdraw=encounter.LowSide.WithdrawQueued,HighWithdraw=encounter.HighSide.WithdrawQueued,LowDepartureStar=Departure(encounter.LowSide),HighDepartureStar=Departure(encounter.HighSide)};
    }
}
