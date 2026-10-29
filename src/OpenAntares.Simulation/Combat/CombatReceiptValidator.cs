using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Combat;
internal static class CombatReceiptValidator
{
    internal static bool Valid(ContentSet content,GameState state,CombatRoundReceipt receipt,EmpireId owner,PocketId pocket)
    {
        try
        {
            var f=receipt.Before;var d=content.Combat!;var p=state.Combat!.Pockets.Single(a=>a.Id==pocket);var command=state.FindEmpire(owner)?.Fleets;
            if(f.Owner!=owner||command is null||f.Fleet.Value<1||f.Fleet.Value>=command.NextFleetId||f.Ships.IsDefaultOrEmpty||f.Sentries.IsDefaultOrEmpty||f.Ships.Length>32||f.Sentries.Length>d.SentriesPerPocket||receipt.Participants.IsDefault)return false;
            if(f.Definition is null||JsonSerializer.Serialize(f.Definition)!=JsonSerializer.Serialize(d))return false;
            if(f.Orders.ReturnColony.Value<1||f.Orders.ReturnColony.Value>=state.NextEntityId||!Enum.IsDefined(f.Orders.Doctrine)||!Enum.IsDefined(f.Orders.Stragglers)||!Enum.IsDefined(f.Orders.OnArrival))return false;
            if(!Ordered(f.Ships.Select(s=>(long)s.VesselId.Value))||!Ordered(f.Sentries.Select(s=>(long)s.SentryId.Value))||f.Ships.Count(s=>s.Flagship)!=1)return false;
            foreach(var s in f.Ships)
            {
                if(s.VesselId.Value>=state.NextEntityId||!d.RoleProfiles.TryGetValue(s.RoleId,out var profile)||s.MaxHull!=profile.MaxHull||s.Attack!=profile.Attack||s.Armor!=profile.Armor||s.Hull<1||s.Hull>s.MaxHull||s.Cohesion is <0 or >100||s.Cargo<0||s.Cargo!=content.Vessels[s.RoleId].EmbarkPopulation)return false;
            }
            // The historical officer may now be lost/reassigned. Its frozen bonus must still
            // be one declared rank bonus shared by this pre-detachment fleet.
            if(!content.FleetCommand!.NavigatorBonuses.Prepend(0).Any(bonus=>f.Ships.All(s=>s.EffectiveSpeed==CombatMath.Stats(d.RoleProfiles[s.RoleId],s.Hull,content.Vessels[s.RoleId].Speed,bonus,d).Speed)))return false;
            foreach(var s in f.Sentries)
                if(s.SentryId.Value>=p.NextSentryId||s.MaxHull!=d.SentryProfile.MaxHull||s.Attack!=d.SentryProfile.Attack||s.Armor!=d.SentryProfile.Armor||s.Hull<1||s.Hull>s.MaxHull||s.PursuitSpeed!=CombatMath.Stats(d.SentryProfile,s.Hull,d.SentryPursuitSpeed,0,d).Speed)return false;
            if(f.Route is {} route)
            {
                var origin=state.FindStar(route.Origin);var target=state.FindStar(route.Destination);
                if(route.Origin!=p.Star||route.Origin==route.Destination||origin is null||target is null||origin.X!=route.OriginX||origin.Y!=route.OriginY||target.X!=route.DestinationX||target.Y!=route.DestinationY)return false;
            }
            var participants=receipt.Interception?CombatWithdrawal.Preview(content,f).Where(g=>g.Speed<=f.Sentries.Max(s=>s.PursuitSpeed)).SelectMany(g=>g.Members).OrderBy(id=>id).ToImmutableArray():f.Ships.Select(s=>s.VesselId).ToImmutableArray();
            if(!participants.SequenceEqual(receipt.Participants)||receipt.Interception&&(f.Route is null||participants.IsEmpty))return false;
            var expected=CombatExchange.Calculate(f,receipt.Interception,receipt.Interception?participants:ImmutableArray<VesselId>.Empty);var actual=receipt.Result;
            if(!expected.Ships.SequenceEqual(actual.Ships)||!expected.Sentries.SequenceEqual(actual.Sentries)||!expected.Shots.SequenceEqual(actual.Shots)||!expected.DestroyedVessels.SequenceEqual(actual.DestroyedVessels)||!expected.DestroyedSentries.SequenceEqual(actual.DestroyedSentries))return false;
            var forecasts=receipt.Interception?CombatQueries.Forecast(f,expected,participants):CombatQueries.Forecast(f,expected);
            return forecasts.SequenceEqual(receipt.Forecasts);
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or OverflowException or System.Collections.Generic.KeyNotFoundException){return false;}
    }
    private static bool Ordered(System.Collections.Generic.IEnumerable<long> ids)
    {long prior=0;foreach(long id in ids){if(id<=prior)return false;prior=id;}return true;}
}
