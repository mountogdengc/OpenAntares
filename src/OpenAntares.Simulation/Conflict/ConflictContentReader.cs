using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Combat;
namespace OpenAntares.Simulation.Conflict;
internal static class ConflictContentReader
{
    internal static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    internal static ConflictDefinition? Read(JsonElement element,string file,List<ContentError> errors)
    {
        try
        {
            if(element.ValueKind!=JsonValueKind.Object)throw new JsonException("A conflict object is required.");
            var names=new HashSet<string>(StringComparer.Ordinal);foreach(var p in element.EnumerateObject())if(!names.Add(p.Name))throw new JsonException("Duplicate conflict field: "+p.Name);
            return element.Deserialize<ConflictDefinition>(Options)??throw new JsonException("A conflict object is required.");
        }
        catch(Exception e)when(e is JsonException or ArgumentException or NotSupportedException)
        {errors.Add(new(file,"conflict","conflict",e.Message));return null;}
    }
    internal static void Validate(ConflictDefinition d,IEnumerable<VesselDefinition> roles,CombatDefinition? combat,string file,List<ContentError> errors)
    {
        void Error(string field,string reason)=>errors.Add(new(file,"conflict","conflict."+field,reason));
        void Bound(int v,string f,int min,int max){if(v<min||v>max)Error(f,$"Expected {min}..{max}.");}
        if(d.AlgorithmId!="limited_conflict_v1")Error("algorithm_id","Supported algorithm: limited_conflict_v1.");
        Bound(d.Truce,"truce",1,1000);
        Bound(d.OfferLifetime,"offer_lifetime",1,1000);
        Bound(d.GrievancePeaceLifetime,"grievance_peace_lifetime",1,1000);
        Bound(d.ContactGate,"contact_gate",1,1000);
        Bound(d.ClaimGate,"claim_gate",1,1000);
        Bound(d.PeaceEarliest,"peace_earliest",1,1000);
        Bound(d.PeaceDeadline,"peace_deadline",1,1000);
        Bound(d.PeaceInactivity,"peace_inactivity",1,1000);
        Bound(d.PeaceCooldown,"peace_cooldown",1,1000);
        Bound(d.DeclarationGrievance,"declaration_grievance",0,1000);
        Bound(d.BlockadeRetainedPercent,"blockade_retained_percent",1,100);
        Bound(d.PeaceHullTarget,"peace_hull_target",1,32);
        Bound(d.WarHullTarget,"war_hull_target",1,32);
        Bound(d.MilitaryHullCap,"military_hull_cap",1,32);
        Bound(d.FormationLimit,"formation_limit",1,32);
        Bound(d.ActiveEncounterLimit,"active_encounter_limit",1,64);
        Bound(d.HistoryLimit,"history_limit",1,20);
        Bound(d.RoundHistoryLimit,"round_history_limit",1,20);
        Bound(d.RepairHullPercent,"repair_hull_percent",1,100);
        Bound(d.HealthyCohesion,"healthy_cohesion",1,100);
        Bound(d.GroupingCommandLimit,"grouping_command_limit",1,2);
        Bound(d.FleetCommandLimit,"fleet_command_limit",1,1);

        if(d.PeaceHullTarget>d.WarHullTarget||d.PeaceHullTarget>d.MilitaryHullCap)Error("peace_hull_target","Peace target must not exceed war target or cap.");
        if(d.WarHullTarget<d.PeaceHullTarget||d.WarHullTarget>d.MilitaryHullCap)Error("war_hull_target","War target must be between peace target and cap.");
        if(d.PeaceEarliest>d.PeaceDeadline)Error("peace_earliest","Earliest peace must not follow the peace deadline.");
        if(combat is not null)foreach(var role in roles)
            if(combat.RoleProfiles.TryGetValue(role.Id,out var profile)&&profile.Attack>0&&(!role.CombatOnly||role.CanFoundColony||role.EmbarkPopulation>0))
                Error("military_role","Armed conflict roles must be combat-only and cannot be protected civilian hulls: "+role.Id);
    }
}
