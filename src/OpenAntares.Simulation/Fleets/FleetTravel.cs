using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Movement;
namespace OpenAntares.Simulation.Fleets;
public static class FleetTravel
{
    internal static FleetDepartureConsequence? DepartureConsequence(ContentSet content,GameState state,EmpireId owner,VesselId id,IReadOnlyDictionary<VesselId,StarId?> departures)
    {
        var vessel=state.FindVessel(id)!;var f=state.FindEmpire(owner)!.Fleets!;
        var provenance=f.PendingEvents.Where(e=>e.Turn==state.Turn&&e.Kind=="fleet_safety_detached"&&e.Vessel is not null).ToArray();
        var source=vessel.Fleet??provenance.LastOrDefault(e=>e.Vessel==id)?.Fleet;
        var group=f.Fleets.SingleOrDefault(x=>x.Id==source);if(group is null)return null;
        var detached=provenance.Where(e=>e.Fleet==group.Id&&state.FindVessel(e.Vessel!.Value)?.Fleet!=group.Id).Select(e=>e.Vessel!.Value).ToHashSet();
        var original=group.Members.Concat(detached).Distinct().OrderBy(x=>x).ToArray();
        var route=departures.GetValueOrDefault(group.Flagship);
        var compatible=original.Where(member=>!detached.Contains(member)&&departures.GetValueOrDefault(member)==route).ToImmutableArray();
        bool named=compatible.Contains(id);var members=named?compatible:ImmutableArray.Create(id);
        int? duration=null;
        if(departures.GetValueOrDefault(id) is {} target&&vessel.StationaryStarId is {} station)
        {
            int speed=named?members.Min(member=>OfficerBonuses.EffectiveSpeed(content,state,member)):content.Vessels[vessel.RoleId].Speed;
            var a=state.FindStar(station)!;var b=state.FindStar(target)!;duration=TravelCalculator.Duration(a.X,a.Y,b.X,b.Y,speed);
        }
        var consequence=new FleetDepartureConsequence(group.Id,named?group.Id:null,group.Flagship,members,original.Except(compatible).OrderBy(x=>x).ToImmutableArray(),named?group.Officer:null,named&&id==group.Flagship&&group.Officer is not null,duration);
        var officer=f.Officers.SingleOrDefault(o=>o.Id==consequence.Officer&&o.Status==OfficerStatus.Commanding);
        if(consequence.OfficerAboard&&officer is not null)
        {
            var d=content.FleetCommand!;bool study=vessel.AmebaEncounter?.RiskOrder?.Action==Scouting.AmebaRiskAction.Study;
            bool researched=state.FindEmpire(owner)!.KnownTechnologyIds.Contains(d.RecoveryTechnologyId);int delay=officer.Embodiment==CommandEmbodiment.ColonyBody?System.Math.Max(1,(officer.Doctrine==OfficerDoctrine.LastWatch?d.LastWatchLinkTurns:d.LinkRecoveryTurns)-(researched?d.ResearchedLinkReduction:0)):d.MortalRecoveryTurns;
            int percent=officer.Embodiment==CommandEmbodiment.ColonyBody?100:officer.Doctrine==OfficerDoctrine.LastWatch?0:researched?d.ResearchedRecoveryPercent:d.MortalRecoveryPercent;
            consequence=consequence with{Recovery=new(officer.Name,officer.Embodiment,officer.Doctrine,percent,delay)};
            bool survey=!study&&departures.GetValueOrDefault(id) is {} surveyTarget&&!f.SurveyCredits.Contains(surveyTarget)&&!state.FindEmpire(owner)!.Surveys.Any(s=>s.StarId==surveyTarget);
            if(study||survey)
            {
                int xp=System.Math.Min(d.XpCap,checked(officer.Xp+(study?d.StudyXp:d.FirstSurveyXp)));int rank=xp>=d.RankThresholds[1]?2:xp>=d.RankThresholds[0]?1:0;
                consequence=consequence with{Reward=new(study?"If first study succeeds":"If this flagship earns the first survey",xp-officer.Xp,xp,rank,survey?OfficerBonuses.SurveyResearch(content,officer.Specialty,officer.Rank):0,officer.Specialty==OfficerSpecialty.Navigator&&rank>0?d.NavigatorBonuses[rank-1]:0,officer.Specialty==OfficerSpecialty.Organizer&&rank>0?d.OrganizerDiscounts[rank-1]:0,OfficerBonuses.SurveyResearch(content,officer.Specialty,rank))};
            }
        }
        return consequence;
    }
    internal static string? ConsequenceFingerprint(ContentSet content, GameState state, EmpireId owner, VesselId id, StarId? destination,IReadOnlyDictionary<VesselId,StarId?> departures)
    {
        var consequence=DepartureConsequence(content,state,owner,id,departures);if(consequence is null)return null;
        var f=state.FindEmpire(owner)!.Fleets!;
        var group=f.Fleets.Single(x=>x.Id==consequence.SourceFleet);
        var officer=consequence.RetainedFleet is null?null:f.Officers.SingleOrDefault(x=>x.Id==group.Officer&&x.Status==OfficerStatus.Commanding);
        var d=content.FleetCommand!;bool researched=state.FindEmpire(owner)!.KnownTechnologyIds.Contains(d.RecoveryTechnologyId);
        int delay=officer?.Embodiment==CommandEmbodiment.ColonyBody?System.Math.Max(1,(officer.Doctrine==OfficerDoctrine.LastWatch?d.LastWatchLinkTurns:d.LinkRecoveryTurns)-(researched?d.ResearchedLinkReduction:0)):d.MortalRecoveryTurns;
        int chance=officer?.Embodiment==CommandEmbodiment.ColonyBody?100:officer?.Doctrine==OfficerDoctrine.LastWatch?0:researched?d.ResearchedRecoveryPercent:d.MortalRecoveryPercent;
        var facts=new{Consequence=consequence,Officer=officer is null?null:new{officer.Id,officer.Xp,officer.Rank,officer.Specialty,officer.Doctrine,officer.Embodiment},Equipment=researched,RecoveryPercent=consequence.OfficerAboard?(int?)chance:null,RecoveryDelay=consequence.OfficerAboard?(int?)delay:null,Destination=destination};
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
