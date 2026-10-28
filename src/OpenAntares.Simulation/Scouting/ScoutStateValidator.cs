using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Scouting;

public static class ScoutStateValidator
{
    public static void Validate(ContentSet content,GameState state,List<string> errors)=>ValidateCore(content,state,errors,false);
    internal static void ValidateFleetForecast(ContentSet content,GameState state,List<string> errors)=>ValidateCore(content,state,errors,true);
    private static void ValidateCore(ContentSet content,GameState state,List<string> errors,bool skipEffectiveConsent)
    {
        void Error(string message)=>errors.Add("scouting: "+message);
        if(!Rules.HasScouting(state.RulesVersion))
        {
            if(state.Scouting is not null||state.Empires.Any(e=>e.Scouting is not null)||state.Vessels.Any(v=>v.Scout is not null||v.AmebaEncounter is not null))Error("legacy campaigns cannot contain scouting fields");
            return;
        }
        if(content.SpaceAmeba is not {} definition||state.Scouting is not {} game||state.GalaxySeed is null){Error("explicit content, setup and seed required");return;}
        var habitat=game.Habitat;
        if(game.Enabled)
        {
            if(game.PlacementRandom is null||game.PlacementRandom.Increment!=unchecked((0x4841424954415401UL<<1)|1UL)||game.SelectionCount is <0 or >1||(habitat is null)!=(game.SelectionCount==0))Error("invalid placement stream/count");
            if(habitat is not null&&(habitat.Id!=1||habitat.DefinitionId!=definition.Id||state.FindStar(habitat.Star) is null||state.Colonies.Any(c=>state.FindPlanet(c.PlanetId)?.StarId==habitat.Star)))Error("invalid guarded habitat");
        }
        else if(game.PlacementRandom is not null||game.SelectionCount!=0||habitat is not null)Error("Off placement must remain dormant");
        var nonVesselIds=state.Empires.Select(e=>e.Id.Value).Concat(state.Stars.Select(s=>s.Id.Value)).Concat(state.Planets.Select(p=>p.Id.Value)).Concat(state.Colonies.Select(c=>c.Id.Value)).ToHashSet();
        foreach(var owner in state.Empires)
        {
            if(owner.Scouting is not {} e){Error($"{owner.Id} missing metadata");continue;}
            if(e.NextEncounterNumber is <1 or long.MaxValue||e.RiskRollCount is <0 or long.MaxValue)Error("counter outside bounds");
            if(game.Enabled?e.RiskRandom is null||e.RiskRandom.Increment!=unchecked((((ulong)owner.Id.Value^0x4553434150450001UL)<<1)|1UL):e.RiskRandom is not null||e.RiskRollCount!=0||e.NextEncounterNumber!=1||e.KnownHabitats.Count!=0||e.SuccessfulStudies.Count!=0||e.Receipts.Count!=0)Error("invalid owner risk stream or Off metadata");
            if(e.SuccessfulStudies.Count>e.RiskRollCount)Error("study claims exceed resolved risk operations");
            int previous=0;
            foreach(var known in e.KnownHabitats)
            {
                if(known.HabitatId<=previous||habitat is null||known.HabitatId!=habitat.Id||known.Star!=habitat.Star||known.ObservedTurn<0||known.ObservedTurn>state.Turn)Error("invalid/unsorted known habitat");
                previous=known.HabitatId;
            }
            previous=0;
            foreach(var claim in e.SuccessfulStudies)
            {
                var vessel=state.FindVessel(claim.Vessel);
                if(claim.HabitatId<=previous||!e.KnownHabitats.Any(k=>k.HabitatId==claim.HabitatId)||claim.Turn<0||claim.Turn>state.Turn||claim.Vessel.Value<=0||claim.Vessel.Value>=state.NextEntityId||nonVesselIds.Contains(claim.Vessel.Value)||vessel is not null&&vessel.EmpireId!=owner.Id)Error("invalid/unsorted study claim");
                if(vessel is not null&&(!content.Vessels.TryGetValue(vessel.RoleId,out var claimRole)||claimRole.CanFoundColony||claimRole.EmbarkPopulation!=0||vessel.CarriedPopulation!=0))Error("study claim requires scout capability");
                previous=claim.HabitatId;
            }
            var vessels=state.Vessels.Where(v=>v.EmpireId==owner.Id).ToArray();
            if(vessels.Where(v=>v.AmebaEncounter is not null).GroupBy(v=>v.AmebaEncounter!.Number).Any(g=>g.Count()>1))Error("duplicate encounter number");
            if(vessels.Count(v=>v.AmebaEncounter?.RiskOrder?.Action==AmebaRiskAction.Study)>1)Error("duplicate pending study");
            previous=0;
            foreach(var receipt in e.Receipts)
            {
                var t=receipt.Token;var v=state.FindVessel(t.Vessel);var encounter=v?.AmebaEncounter;
                if(t.Vessel.Value<=previous||v?.EmpireId!=owner.Id||encounter is null||t.EncounterNumber!=encounter.Number||t.Revision!=encounter.Revision||t.Stage!=encounter.Stage||receipt.AcknowledgedTurn!=state.Turn||t.Speed<=0||t.Cargo<0||t.Percent is <0 or >100||t.Reward<0||string.IsNullOrWhiteSpace(t.Action)||t.Action.Length>80||t.Destination is {} destination&&state.FindStar(destination) is null)Error("invalid/unsorted consent receipt");
                previous=t.Vessel.Value;
            }
            if(owner.Controller==ControllerKind.Human&&owner.Ready&&vessels.Any(v=>v.AmebaEncounter is {} a&&!e.Receipts.Any(r=>r.Token.Vessel==v.Id&&r.Token.EncounterNumber==a.Number&&r.Token.Revision==a.Revision&&r.Token.Stage==a.Stage)))Error("ready human has unread encounter");
            if(e.CompletedDigests.Count>10)Error("history limit");
            if(e.PendingDigest is {} pending&&pending.Turn!=state.Turn)Error("pending digest turn");
            int last=-1;
            foreach(var digest in e.CompletedDigests){if(digest.Turn<=last||digest.Turn>=state.Turn||digest.Turn<Math.Max(0,state.Turn-10))Error("completed history order/turn");last=digest.Turn;}
            foreach(var digest in e.CompletedDigests.Concat(e.PendingDigest is {} d?new[]{d}:Array.Empty<ScoutTurnDigest>()))
            {
                if(digest.Entries.IsDefault){Error("missing history entries");continue;}
                if((Int128)digest.Entries.Length>8*((Int128)state.NextEntityId-1))Error("history entry bound");
                (int Vessel,int Slot) previousEntry=(0,-1);
                foreach(var entry in digest.Entries)
                {
                    int slot=ScoutReports.Slot(entry.Kind);var key=(entry.Vessel.Value,slot);if(slot<0||key.CompareTo(previousEntry)<=0)Error("invalid/unsorted/duplicate history slot");previousEntry=key;
                    var v=state.FindVessel(entry.Vessel);var retired=e.RetiredVessels.Find(r=>r.Id==entry.Vessel);
                    if(entry.Owner!=owner.Id||entry.Turn!=digest.Turn||!content.Vessels.ContainsKey(entry.RoleId)||entry.Cargo<0||state.FindStar(entry.Star) is null||entry.Vessel.Value<=0||entry.Vessel.Value>=state.NextEntityId||nonVesselIds.Contains(entry.Vessel.Value)||v is not null&&v.EmpireId!=owner.Id||v is null&&retired is null||string.IsNullOrWhiteSpace(entry.Kind)||entry.Kind.Length>80||entry.Reason.Length>4096||entry.ResearchAward<0||entry.Percent is <0 or >100)Error("invalid history fact");
                    if(v is not null&&(v.RoleId!=entry.RoleId||v.CarriedPopulation!=entry.Cargo)||retired is not null&&(retired.RoleId!=entry.RoleId||retired.Cargo!=entry.Cargo)||entry.Destination is {} destination&&state.FindStar(destination) is null||entry.ResearchAward>0&&(entry.Kind!="study"||entry.ResearchAward!=definition.ResearchReward)||entry.EncounterNumber is <=0||entry.Revision is <=0||entry.EncounterNumber.HasValue!=entry.Revision.HasValue)Error("inconsistent immutable history facts");
                    if(!game.Enabled&&(entry.EncounterNumber is not null||entry.Kind is "warning" or "study" or "departure" or "escape" or "loss"))Error("Off history cannot contain ameba events");
                }
            }
            previous=0;
            foreach(var retired in e.RetiredVessels)
            {
                if(retired.Id.Value<=previous||retired.Id.Value>=state.NextEntityId||nonVesselIds.Contains(retired.Id.Value)||state.FindVessel(retired.Id) is not null||retired.Owner!=owner.Id||!content.Vessels.ContainsKey(retired.RoleId)||retired.Cargo<0||state.FindStar(retired.Star) is null||retired.RetiredTurn<0||retired.RetiredTurn>state.Turn)Error("invalid/unsorted retired vessel");
                if(!e.CompletedDigests.Concat(e.PendingDigest is {} pendingHistory?new[]{pendingHistory}:Array.Empty<ScoutTurnDigest>()).Any(d=>!d.Entries.IsDefault&&d.Entries.Any(entry=>entry.Vessel==retired.Id)))Error("unreferenced retired vessel");
                previous=retired.Id.Value;
            }
        }
        foreach(var v in state.Vessels)
        {
            if(!content.Vessels.TryGetValue(v.RoleId,out var role))continue;
            bool eligible=!role.CanFoundColony&&role.EmbarkPopulation==0&&v.CarriedPopulation==0;
            if(eligible!=(v.Scout is not null))Error("missing or ineligible scout metadata");
            var e=state.FindEmpire(v.EmpireId)?.Scouting;
            if(v.Scout is {} scout)
            {
                if(!Enum.IsDefined(scout.Posture)||!scout.WithdrawFromAmeba||scout.LastWaitSignature is {} signature&&(signature.Length!=64||signature.Any(c=>!Uri.IsHexDigit(c))))Error("invalid scout posture/survival/wait signature");
                if(scout.ApproachAcceptance is {} approach&&(habitat is null||approach.HabitatId!=habitat.Id||approach.Destination!=habitat.Star||v.PendingMove!=approach.Destination||v.Transit is not null||scout.Posture!=ScoutPosture.Observe||e?.KnownHabitats.Any(k=>k.HabitatId==approach.HabitatId)!=true))Error("invalid exact approach acceptance");
            }
            if(v.AmebaEncounter is not {} a){if(habitat is not null&&v.StationaryStarId==habitat.Star)Error("guarded-system presence requires an explicit encounter");continue;}
            if(!game.Enabled||habitat is null||a.HabitatId!=habitat.Id||v.StationaryStarId!=habitat.Star||v.Transit is not null||a.ArrivalTurn<0||a.ArrivalTurn>state.Turn||a.Number<1||e is null||a.Number>=e.NextEncounterNumber||a.Revision is <1 or long.MaxValue||!Enum.IsDefined(a.Stage)||e?.KnownHabitats.Any(k=>k.HabitatId==a.HabitatId)!=true)Error("invalid encounter reference/stage/counter");
            if(a.Stage==AmebaStage.Warning&&a.ArrivalTurn!=state.Turn)Error("Warning must retain its single current planning opportunity");
            if(a.Stage==AmebaStage.Exposed&&(!eligible||a.ArrivalTurn!=(long)state.Turn-1||e?.SuccessfulStudies.Any(c=>c.HabitatId==a.HabitatId&&c.Vessel==v.Id&&c.Turn==state.Turn)!=true))Error("Exposed requires the current surviving study scout");
            if(a.RiskOrder is {} order&&(!Enum.IsDefined(order.Action)||order.Turn!=state.Turn||order.Revision!=a.Revision||v.PendingMove is not null||order.Action==AmebaRiskAction.Study&&(!eligible||e?.SuccessfulStudies.Any(c=>c.HabitatId==a.HabitatId)==true||a.Stage!=AmebaStage.Warning)))Error("invalid risk order");
        }
        // Effective consent is evaluated only after the ordinary state and references are sound.
        // ProjectBase never invokes validation, so this check cannot recurse through this validator.
        if(!skipEffectiveConsent&&errors.Count==0)foreach(var owner in state.Empires)
        {
            var e=owner.Scouting!;
            if(e.Receipts.Count==0&&!owner.Ready)continue;
            var attention=ScoutQueries.Project(content,state,owner.Id).Attention;
            if(e.Receipts.Any(r=>!attention.Contains(r.Token))||owner.Controller==ControllerKind.Human&&owner.Ready&&attention.Any(t=>!e.Receipts.Any(r=>r.Token==t)))Error("consent does not match effective encounter consequences");
        }
    }
}
