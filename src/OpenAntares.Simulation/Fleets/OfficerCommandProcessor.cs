using System;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Fleets;
public static class OfficerCommandProcessor
{
    public static CommandOutcome Execute(ContentSet content,GameState state,OfficerCommand command)
    {
        CommandOutcome Reject(string reason)=>CommandOutcome.Reject(state,"officer_illegal",reason);
        if(!Rules.HasFleets(state.RulesVersion)||content.FleetCommand is not {} d||state.FindEmpire(command.Issuer)?.Fleets is null)return Reject("Officer command requires a fleet campaign.");
        if(command.ExpectedTurn!=state.Turn)return Reject("Officer command is stale.");
        try
        {
            var candidate=state.Clone();var e=candidate.FindEmpire(command.Issuer)!;var f=e.Fleets!;
            string before=JsonSerializer.Serialize(f);
            OfficerId? affected=null;
            void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
            OfficerRecord Officer(OfficerId id)=>f.Officers.SingleOrDefault(o=>o.Id==id)??throw new InvalidOperationException("Officer is unavailable to this empire.");
            ColonyState Colony(ColonyId id)=>candidate.Colonies.SingleOrDefault(c=>c.Id==id&&c.EmpireId==command.Issuer)??throw new InvalidOperationException("Posting colony is unavailable to this empire.");
            StarId ColonyStar(ColonyId id)=>candidate.FindPlanet(Colony(id).PlanetId)!.StarId;
            StarId? Location(OfficerRecord o)=>o.Status switch {OfficerStatus.Posted or OfficerStatus.Coordinating=>ColonyStar(o.PostedColony!.Value),OfficerStatus.Commanding=>candidate.FindVessel(o.Flagship!.Value)!.StationaryStarId,_=>null};
            void Release(OfficerRecord o)
            {
                if(o.Flagship is {} ship)foreach(var fleet in f.Fleets.Where(x=>x.Officer==o.Id&&x.Flagship==ship))fleet.Officer=null;
                if(o.Request is {} request)
                {
                    var r=f.Requests.Single(x=>x.Id==request);r.Coordinator=null;r.CoordinatorXpForfeited=true;r.Revision=checked(r.Revision+1);
                }
                o.Flagship=null;o.Request=null;o.PostedColony=null;o.AvailableAtTurn=null;o.RecoveryColony=null;
            }
            void Live(OfficerRecord o)=>Require(FleetQueries.IsActive(o.Status),"This service record is final.");
            void Doctrine(OfficerDoctrine value)=>Require(Enum.IsDefined(value),"Choose a valid doctrine.");
            switch(command.Action)
            {
                case OfficerAction.Commission a:
                    Require(f.LifetimeCommissions<d.LifetimeCommissionLimit&&f.Officers.Count(o=>FleetQueries.IsActive(o.Status))<d.ActiveOfficerLimit,"The active or lifetime commission limit is reached.");
                    Colony(a.Posting);var species=content.Species[e.SpeciesId!];var culture=d.Cultures.Single(c=>c.Id==species.CommandCultureId);OfficerProfileDefinition profile;
                    bool body=species.CommandEmbodiment==CommandEmbodiment.ColonyBody;
                    if(body)
                    {
                        Require(a.ProfileId is null&&a.SourceBody==a.Posting&&a.Specialty is {} specialty&&Enum.IsDefined(specialty),"Choose an owned source body, its own posting and a specialty.");
                        Require(!f.Officers.Any(o=>FleetQueries.IsActive(o.Status)&&o.SourceBody==a.SourceBody),"This body already has an active command appointment.");
                        profile=d.Profiles.Single(p=>p.CultureId==culture.Id&&p.Specialty==a.Specialty);
                    }
                    else
                    {
                        Require(a.SourceBody is null&&a.Specialty is null&&a.ProfileId is not null&&f.CandidateProfiles.Contains(a.ProfileId),"Choose an available fixed-specialty candidate.");
                        profile=d.Profiles.Single(p=>p.Id==a.ProfileId&&p.CultureId==culture.Id);
                    }
                    var id=new OfficerId(f.NextOfficerId);f.NextOfficerId=checked(f.NextOfficerId+1);f.LifetimeCommissions=checked(f.LifetimeCommissions+1);
                    string name=body?$"{candidate.FindStar(ColonyStar(a.Posting))!.Name} {candidate.FindPlanet(Colony(a.Posting).PlanetId)!.Orbit} — Command appointment{id.Value}":FleetQueries.CandidateName(f,profile);
                    f.ProfileCommissions.TryGetValue(profile.Id,out int count);f.ProfileCommissions[profile.Id]=checked(count+1);
                    f.Officers.Add(new(){Id=id,ProfileId=profile.Id,SpeciesId=species.Id,CultureId=culture.Id,Name=name,Pronouns=profile.Pronouns,Personality=profile.Personality,Embodiment=species.CommandEmbodiment!.Value,Specialty=profile.Specialty,Doctrine=f.FutureDoctrine??culture.DefaultDoctrine,Status=OfficerStatus.Commissioning,SourceBody=body?a.SourceBody:null,PostedColony=a.Posting,AvailableAtTurn=checked(state.Turn+d.CommissionTurns)});
                    if(!body){f.CandidateProfiles.Remove(profile.Id);if(f.CandidateProfiles.Count==0)f.CandidateProfiles=culture.ProfileIds.ToList();}
                    affected=id;break;
                case OfficerAction.Dismiss a:
                    var retired=Officer(a.Officer);affected=retired.Id;if(retired.Status==OfficerStatus.Retired)break;Live(retired);Release(retired);retired.Status=OfficerStatus.Retired;retired.FinalSummary=$"Retired on turn {state.Turn}; {retired.Xp} XP. Service record is final.";break;
                case OfficerAction.Post a:
                    var posted=Officer(a.Officer);Live(posted);Require(posted.Status==OfficerStatus.Posted&&Location(posted)==ColonyStar(a.Colony),"Posting requires an available officer physically at that system.");posted.PostedColony=a.Colony;affected=posted.Id;break;
                case OfficerAction.AssignFlagship a:
                    var assigned=Officer(a.Officer);Live(assigned);var group=f.Fleets.SingleOrDefault(x=>x.Id==a.Fleet)??throw new InvalidOperationException("Fleet is unavailable to this empire.");
                    if(assigned.Status==OfficerStatus.Commanding&&assigned.Flagship==group.Flagship&&group.Officer==assigned.Id)break;
                    Require(assigned.Status==OfficerStatus.Posted&&group.Officer is null&&Location(assigned)==candidate.FindVessel(group.Flagship)!.StationaryStarId,"Boarding requires an available posted officer and uncommanded stationary fleet at the same system.");
                    Release(assigned);assigned.Status=OfficerStatus.Commanding;assigned.Flagship=group.Flagship;group.Officer=assigned.Id;affected=assigned.Id;break;
                case OfficerAction.Transfer a:
                    var transfer=Officer(a.Officer);Live(transfer);Require(transfer.Status==OfficerStatus.Commanding&&transfer.Flagship==a.From,"Transfer must name the current flagship.");
                    var destination=f.Fleets.SingleOrDefault(x=>x.Flagship==a.To)??throw new InvalidOperationException("Destination must be an own fleet flagship.");
                    if(a.From==a.To)break;
                    Require(destination.Officer is null&&candidate.FindVessel(a.From)!.StationaryStarId is {} station&&candidate.FindVessel(a.To)!.StationaryStarId==station,"Transfer requires co-located stationary flagships and a free command assignment.");
                    Release(transfer);transfer.Status=OfficerStatus.Commanding;transfer.Flagship=a.To;destination.Officer=transfer.Id;affected=transfer.Id;break;
                case OfficerAction.Remove a:
                    var removed=Officer(a.Officer);Live(removed);Require(Location(removed) is {} location&&location==ColonyStar(a.Colony),"Removal requires an available officer physically at the owned posting system.");
                    if(removed.Status==OfficerStatus.Posted&&removed.PostedColony==a.Colony)break;
                    Release(removed);removed.Status=OfficerStatus.Posted;removed.PostedColony=a.Colony;affected=removed.Id;break;
                case OfficerAction.SetDoctrine a:
                    Doctrine(a.Doctrine);var policy=Officer(a.Officer);Live(policy);policy.Doctrine=a.Doctrine;affected=policy.Id;break;
                case OfficerAction.ApplyDoctrineToSelection a:
                    Doctrine(a.Doctrine);Require(!a.Officers.IsDefaultOrEmpty,"Choose own officers.");var selected=a.Officers.Distinct().OrderBy(id=>id).Select(Officer).ToArray();foreach(var o in selected)Live(o);foreach(var o in selected)o.Doctrine=a.Doctrine;break;
                case OfficerAction.SetFutureDoctrine a:
                    if(a.Doctrine is {} future)Doctrine(future);f.FutureDoctrine=a.Doctrine;break;
                default: throw new InvalidOperationException("Unknown officer action.");
            }
            if(before==JsonSerializer.Serialize(f))return CommandOutcome.NoChange(state);
            string kind=command.Action.GetType().Name;
            string reason=command.Action switch
            {
                OfficerAction.Commission=>$"{Officer(affected!.Value).Name} commissioned; available on turn {Officer(affected.Value).AvailableAtTurn}.",
                OfficerAction.Dismiss=>Officer(affected!.Value).FinalSummary!,
                OfficerAction.AssignFlagship a=>$"{Officer(a.Officer).Name} took command of {f.Fleets.Single(x=>x.Id==a.Fleet).Name}.",
                OfficerAction.Transfer a=>$"{Officer(a.Officer).Name} transferred to {f.Fleets.Single(x=>x.Flagship==a.To).Name}.",
                OfficerAction.Post a=>$"{Officer(a.Officer).Name} posted at {candidate.FindStar(ColonyStar(a.Colony))!.Name}.",
                OfficerAction.Remove a=>$"{Officer(a.Officer).Name} left the assignment and posted at {candidate.FindStar(ColonyStar(a.Colony))!.Name}.",
                OfficerAction.SetDoctrine a=>$"{Officer(a.Officer).Name} adopted {(a.Doctrine==OfficerDoctrine.LastWatch?"Last watch":"Recover command")}.",
                OfficerAction.ApplyDoctrineToSelection a=>$"Doctrine applied explicitly to {a.Officers.Distinct().Count()} selected officers.",
                OfficerAction.SetFutureDoctrine a=>a.Doctrine is null?"Future commissions use cultural doctrine defaults.":$"Future commissions explicitly use {(a.Doctrine==OfficerDoctrine.LastWatch?"Last watch":"Recover command")}.",
                _=>"Officer assignment changed."
            };
            f.PendingEvents.RemoveAll(x=>x.Turn==state.Turn&&x.Kind==kind&&x.Officer==affected);
            long eventId=f.NextEventId;f.NextEventId=checked(f.NextEventId+1);
            f.PendingEvents.Add(new(command.Issuer,eventId,state.Turn,kind,reason,Officer:affected));
            e.Ready=false;
            return new(candidate,null,true,null);
        }
        catch(Exception error)when(error is OverflowException or InvalidOperationException or ArgumentException){return Reject(error.Message);}
    }
}
