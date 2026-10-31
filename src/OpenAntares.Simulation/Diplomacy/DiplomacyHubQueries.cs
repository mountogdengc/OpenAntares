using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Diplomacy;
public sealed record HubEligibility(bool Allowed,string? Reason,long Bank,long Reserved,long Spendable);
public static class DiplomacyHubQueries
{
    public static HubEligibility Eligibility(ContentSet content,GameState state,EmpireId owner,ColonyId hub,OfferId? existing)
    {
        var colony=state.FindColony(hub);if(colony is null||colony.EmpireId!=owner||content.Diplomacy is not {} d)return new(false,"own_hub_required",0,0,0);
        long reserved=DiplomacyReservations.Reserved(state,hub);long matching=0;
        foreach(var pair in state.Diplomacy!.Pairs)
        {
            if(pair.Pending is not {} o||o.Id!=existing||o.Withdraw)continue;
            if(o.Proposer==owner&&o.ProposerHub?.Hub==hub)matching=o.ProposerHub.Reserved;
            if(o.Recipient==owner&&o.Answer?.Accept==true&&o.Answer.Hub?.Hub==hub)matching=o.Answer.Hub.Reserved;
        }
        long available=checked(colony.ProductionReserve-reserved+matching);string? reason=null;
        if(colony.ProjectKind==ProductionKinds.Refit)reason="hub_refitting";
        else if(state.FindEmpire(owner)!.Fleets!.Requests.Any(r=>r.Status==RequestStatus.Active&&r.Slots.Any(s=>s.Colony==hub&&s.Status==SlotStatus.Queued)))reason="hub_assembly";
        else if(reserved!=matching)reason="hub_already_committed";
        else if(available<d.SetupCost)reason="hub_setup_unfunded";
        else if(EconomyCalculator.CalculateColony(content,state,colony).NetProduction.Total<=0)reason="hub_not_productive";
        return new(reason is null,reason,colony.ProductionReserve,reserved,checked(colony.ProductionReserve-reserved));
    }
}
