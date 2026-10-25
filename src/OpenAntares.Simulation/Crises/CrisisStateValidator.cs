using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Crises;

public static class CrisisStateValidator
{
    public static void Validate(ContentSet content,GameState state,List<string> errors)
    {
        void Error(string message)=>errors.Add("crisis: "+message);
        if(!Rules.HasCrises(state.RulesVersion))
        {
            if(state.Crisis is not null||state.Empires.Any(e=>e.Crisis is not null)||state.Colonies.Any(c=>c.Crisis is not null)||state.Vessels.Any(v=>v.Crisis is not null))Error("legacy campaigns cannot contain crisis fields");
            return;
        }
        if(content.Plagues.Count!=1){Error("exactly one supported plague family required");return;}
        var d=content.Plagues.Values.Single();
        if(state.Crisis is null||state.GalaxySeed is null){Error("explicit setup and galaxy seed required");return;}
        bool off=!state.Crisis.Enabled;
        var nonColonyIds=state.Empires.Select(e=>e.Id.Value).Concat(state.Stars.Select(x=>x.Id.Value)).Concat(state.Planets.Select(x=>x.Id.Value)).Concat(state.Vessels.Select(x=>x.Id.Value)).ToHashSet();
        var nonVesselIds=state.Empires.Select(e=>e.Id.Value).Concat(state.Stars.Select(x=>x.Id.Value)).Concat(state.Planets.Select(x=>x.Id.Value)).Concat(state.Colonies.Select(x=>x.Id.Value)).Concat(state.Empires.SelectMany(e=>e.Crisis?.LostColonyLocations??new()).Select(l=>l.Colony.Value)).ToHashSet();
        foreach(var owner in state.Empires)
        {
            var e=owner.Crisis;
            if(e is null){Error($"{owner.Id} missing metadata");continue;}
            if((e.Random.Increment&1)==0)Error("stream increment must be odd");
            if(e.NextEpisodeNumber<1||e.NextEpisodeNumber==long.MaxValue||e.NextEligibleTurn<0||e.NextEligibleTurn>int.MaxValue-10000)Error("episode counter/cooldown outside bounds");
            if(!Enum.IsDefined(e.Authority)||e.AuthorityResearchShare is not(25 or 50 or 100))Error("invalid authority/share");
            if(e.CivicLevel<0||e.CivicLevel>d.CivicMaxLevel||e.CivicExpiresAt<0||e.CivicExpiresAt>int.MaxValue-d.CivicTurns||(e.CivicLevel==0)!=(e.CivicExpiresAt==0)||e.CivicLevel>0&&e.CivicExpiresAt<=state.Turn)Error("invalid civic level/expiry");
            int previous=0;
            foreach(var location in e.LostColonyLocations)
            {
                if(location.Colony.Value<=previous||state.FindColony(location.Colony) is not null||!LocationShape(location))Error("invalid/unsorted retired owned location");
                previous=location.Colony.Value;
            }
            if(e.Episode is {} episode)
            {
                Int128 cost=(Int128)d.CureBase+(Int128)d.CurePerPopulation*episode.SourcePopulation;
                if(episode.Id<1||episode.Id>=e.NextEpisodeNumber||episode.FamilyId!=d.Id||!OwnedLocation(episode.Source,owner)||episode.SourcePopulation<1||cost!=episode.CureCost||episode.CureCost<=0||episode.CureProgress<0||episode.CureProgress>=episode.CureCost||episode.ResearchShare is not(0 or 25 or 50 or 100))Error("invalid active episode/source/frozen cost");
            }
            if(e.PendingDigests.Count>1||e.CompletedDigests.Count>10)Error("digest limits");
            foreach(var pending in e.PendingDigests)if(pending.Turn!=state.Turn)Error("pending digest turn");
            int last=-1;
            foreach(var digest in e.CompletedDigests){if(digest.Turn<=last||digest.Turn>=state.Turn)Error("completed digest turns/order");last=digest.Turn;}
            foreach(var digest in e.PendingDigests.Concat(e.CompletedDigests))
            {
                if(digest.Entries.IsDefault){Error("missing digest entries");continue;}
                foreach(var entry in digest.Entries)
                    if(!Key(entry.Key,entry.Location,entry.Revision,owner)||string.IsNullOrWhiteSpace(entry.Kind)||entry.Kind.Length>80||string.IsNullOrWhiteSpace(entry.Disposition)||entry.Disposition.Length>80||entry.Reason.Length>4096||entry.ResearchDiverted<0||entry.PopulationLost<0)Error("invalid digest facts");
            }
            (long Episode,int Colony) previousKey=(0,0);
            foreach(var receipt in e.PendingReceipts)
            {
                var key=(receipt.Key.Episode,receipt.Key.Colony.Value);
                if(key.CompareTo(previousKey)<=0||!Key(receipt.Key,receipt.Location,receipt.Revision,owner)||receipt.Turn<0||receipt.Turn>state.Turn||string.IsNullOrWhiteSpace(receipt.Reason)||receipt.Reason.Length>4096)Error("invalid/unsorted pending receipt");
                previousKey=key;
            }
            if(owner.Controller==ControllerKind.Human&&owner.Ready&&e.RequireAcknowledgement&&!CrisisQueries.PendingAttention(state,owner.Id).IsEmpty)Error("ready human has unacknowledged crisis facts");
            if(off)
            {
                var seeded=CrisisInitializer.CreateRandom(state.GalaxySeed.Value,owner.Id);
                if(e.Random.State!=seeded.State||e.Random.Increment!=seeded.Increment||e.NextEpisodeNumber!=1||e.NextEligibleTurn!=0||e.Episode is not null||e.Authority!=CrisisAuthority.Manual||e.AuthorityResearchShare!=50||e.ResearchPolicyPaused||!e.RequireAcknowledgement||e.CivicLevel!=0||e.CivicExpiresAt!=0||e.PendingDigests.Count!=0||e.CompletedDigests.Count!=0||e.PendingReceipts.Count!=0||e.LostColonyLocations.Count!=0)Error("Off campaign must remain dormant");
            }
        }
        foreach(var colony in state.Colonies)
        {
            var c=colony.Crisis;var owner=state.FindEmpire(colony.EmpireId);var e=owner?.Crisis;
            if(c is null||e is null){Error("missing colony/owner crisis metadata");continue;}
            if(c.ReleaseEpisode is {} release&&(!c.Quarantined||c.Case?.EpisodeId!=release))Error("release must bind this quarantined local case");
            if(c.ImmunityFamilyId is {} family ? family!=d.Id||c.ImmuneUntilTurn<=state.Turn||c.ImmuneUntilTurn>int.MaxValue-d.ImmunityTurns : c.ImmuneUntilTurn!=0)Error("invalid immunity family/expiry");
            if(c.Case is {} illness)
            {
                if(illness.EpisodeId!=e.Episode?.Id||!Enum.IsDefined(illness.Stage)||illness.AttentionRevision<1||illness.AttentionRevision==long.MaxValue||illness.HandledRevision<0||illness.HandledRevision>illness.AttentionRevision||illness.WaivedRevision<0||illness.WaivedRevision>illness.AttentionRevision||!OwnedLocation(illness.Origin,owner!)||c.ImmunityFamilyId is not null)Error("invalid owned case/revision/origin");
                if(illness.Stage==CrisisStage.Warning ? illness.GraceRemaining<1||illness.GraceRemaining>d.GraceTurns||illness.ActiveAge!=0||illness.Severity!=0 : illness.GraceRemaining!=0||illness.ActiveAge<0||illness.ActiveAge>=d.RecoveryIntervals||illness.Severity<d.InitialSeverity||illness.Severity>d.SeverityCap)Error("invalid health progression");
                if(illness.SourceVessel is {} source&&(source.Value<1||source.Value>=state.NextEntityId||nonVesselIds.Contains(source.Value)||state.FindVessel(source) is {} vessel&&vessel.EmpireId!=colony.EmpireId))Error("invalid owned source vessel");
            }
            if(off&&(c.Case is not null||c.Quarantined||c.ReleaseEpisode is not null||c.PolicyPaused||c.ImmunityFamilyId is not null||c.ImmuneUntilTurn!=0))Error("Off colony must remain dormant");
        }
        foreach(var vessel in state.Vessels)
        {
            var v=vessel.Crisis;var e=state.FindEmpire(vessel.EmpireId)?.Crisis;
            if(v is null||e is null){Error("missing vessel/owner metadata");continue;}
            if(v.CarrierEpisode is {} carrier&&(vessel.CarriedPopulation<=0||e.Episode?.Id!=carrier))Error("invalid owned cargo episode");
            if(v.InspectionDueTurn is {} due&&(vessel.CarriedPopulation<=0||vessel.StationaryStarId is null||due<=state.Turn||due>state.Turn+1L))Error("invalid full-interval inspection hold");
            if(off&&(v.CarrierEpisode is not null||v.InspectionDueTurn is not null))Error("Off vessel must remain dormant");
        }

        bool LocationShape(CrisisLocation l) => l.Colony.Value>0&&l.Colony.Value<state.NextEntityId&&!nonColonyIds.Contains(l.Colony.Value)&&l.Name.Length is >0 and <=160&&state.FindPlanet(l.Planet)?.StarId==l.Star&&state.FindStar(l.Star) is not null;
        bool OwnedLocation(CrisisLocation l,EmpireState owner)
        {
            if(!LocationShape(l))return false;
            if(state.FindColony(l.Colony) is {} live)return live.EmpireId==owner.Id&&live.PlanetId==l.Planet;
            return owner.Crisis!.LostColonyLocations.Contains(l);
        }
        bool Key(CrisisKey key,CrisisLocation location,long revision,EmpireState owner) => key.Owner==owner.Id&&key.Episode>0&&key.Episode<owner.Crisis!.NextEpisodeNumber&&key.Colony==location.Colony&&revision>0&&revision<long.MaxValue&&OwnedLocation(location,owner);
    }
}
