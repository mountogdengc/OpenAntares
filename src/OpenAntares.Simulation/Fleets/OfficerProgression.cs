using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
internal static class OfficerProgression
{
    internal static void Apply(ContentSet content,GameState snapshot,GameState candidate,FleetResolutionContext context,Scouting.AmebaResolutionContext? scouting)
    {
        var d=content.FleetCommand!;
        foreach(var empire in candidate.Empires.OrderBy(e=>e.Id))
        {
            var f=empire.Fleets!;var appointments=context.Officers.Where(o=>o.Owner==empire.Id).ToArray();
            foreach(var star in empire.Surveys.Select(s=>s.StarId).Distinct().OrderBy(id=>id))
            {
                if(!f.SurveyCredits.Add(star))continue;
                // First observation consumes the credit even when no officer was present.
                var recipient=appointments.Where(a=>a.Officer.Status==OfficerStatus.Commanding&&a.Officer.Flagship is {} id&&candidate.FindVessel(id)?.StationaryStarId==star&&snapshot.FindVessel(id) is not null)
                    .OrderBy(a=>a.Officer.Flagship).FirstOrDefault();
                if(recipient is null||snapshot.FindEmpire(empire.Id)!.Surveys.Any(s=>s.StarId==star))continue;
                var officer=f.Officers.Single(o=>o.Id==recipient.Officer.Id);long research=OfficerBonuses.SurveyResearch(content,recipient.Officer.Specialty,recipient.Officer.Rank);
                empire.ResearchReserve=checked(empire.ResearchReserve+research);officer.Career=officer.Career with{FirstSurveys=checked(officer.Career.FirstSurveys+1)};
                Award(content,candidate,context,empire.Id,officer,d.FirstSurveyXp,"first_survey",$"First survey of {star}; {research} internal research from the snapshot specialty/rank.");
            }
            foreach(var claim in empire.Scouting!.SuccessfulStudies.OrderBy(s=>s.HabitatId))
            {
                if(!f.StudyCredits.Add(claim.HabitatId))continue;
                if(scouting?.Events.Any(e=>e.Owner==empire.Id&&e.Vessel==claim.Vessel&&e.Kind=="study")!=true)continue;
                var recipient=appointments.SingleOrDefault(a=>a.Officer.Status==OfficerStatus.Commanding&&a.Officer.Flagship==claim.Vessel);if(recipient is null)continue;
                var officer=f.Officers.Single(o=>o.Id==recipient.Officer.Id);officer.Career=officer.Career with{Studies=checked(officer.Career.Studies+1)};
                Award(content,candidate,context,empire.Id,officer,d.StudyXp,"ameba_study","First rewarded study; only the ordinary ameba research reward applies.");
            }
            foreach(var fulfilled in context.Fulfilled.Where(r=>r.Owner==empire.Id))
            {
                var request=f.Requests.Single(r=>r.Id==fulfilled.Request);int hulls=0;
                foreach(var slot in request.Slots.Where(s=>s.NewHull&&s.Vessel is not null))
                {
                    var ship=candidate.FindVessel(slot.Vessel!.Value)!;
                    if(ship.BuildOrigin is {} origin&&origin.Request==request.Id&&origin.Slot==slot.Ordinal&&f.HullCredits.Add(ship.Id))hulls=checked(hulls+1);
                }
                var recipient=appointments.SingleOrDefault(a=>a.Officer.Id==request.InitialXpRecipient&&a.Officer.Status==OfficerStatus.Coordinating&&a.Officer.Request==request.Id);
                if(request.CoordinatorXpForfeited||recipient is null)continue;
                var officer=f.Officers.Single(o=>o.Id==recipient.Officer.Id);officer.Career=officer.Career with{NewHulls=checked(officer.Career.NewHulls+hulls),CompletedRequests=checked(officer.Career.CompletedRequests+1)};
                Award(content,candidate,context,empire.Id,officer,checked(d.AssemblyHullXp*hulls),"request_fulfilled",$"Finite request fulfilled with {hulls} original newly built hulls.");
            }
            foreach(var appointment in appointments.Where(a=>a.Officer.Status==OfficerStatus.Commanding))
            {
                var group=snapshot.FindEmpire(empire.Id)!.Fleets!.Fleets.Single(fleet=>fleet.Officer==appointment.Officer.Id);
                int lost=group.Members.Count(id=>candidate.FindVessel(id) is null&&context.Events.Any(e=>e.Kind=="fleet_ship_lost"&&e.Owner==empire.Id&&e.Vessel==id));if(lost==0)continue;
                var officer=f.Officers.Single(o=>o.Id==appointment.Officer.Id);bool flagship=context.Losses.Any(l=>l.Appointment.Owner==empire.Id&&l.Appointment.Officer.Id==officer.Id);
                officer.Career=officer.Career with{ShipsLost=checked(officer.Career.ShipsLost+lost),FlagshipsLost=checked(officer.Career.FlagshipsLost+(flagship?1:0))};
                context.Event(candidate,empire.Id,"command_losses",$"{lost} ship(s) lost while commanding; no experience awarded.",officer:officer.Id,importance:FleetImportance.Attention);
            }
        }
    }
    private static void Award(ContentSet content,GameState state,FleetResolutionContext context,EmpireId owner,OfficerRecord officer,int xp,string kind,string reason)
    {
        int oldXp=officer.Xp,oldRank=officer.Rank;officer.Xp=Math.Min(content.FleetCommand!.XpCap,checked(officer.Xp+xp));officer.Rank=officer.Xp>=content.FleetCommand.RankThresholds[1]?2:officer.Xp>=content.FleetCommand.RankThresholds[0]?1:0;
        officer.Career=officer.Career with{XpEarned=checked(officer.Career.XpEarned+officer.Xp-oldXp)};context.Event(state,owner,kind,$"{reason} XP +{officer.Xp-oldXp}.",officer:officer.Id);
        if(officer.Rank!=oldRank)context.Event(state,owner,"officer_promoted",$"Rank {officer.Rank} unlocked at {officer.Xp} XP; new bonuses apply from the next snapshot.",officer:officer.Id);
    }
}
