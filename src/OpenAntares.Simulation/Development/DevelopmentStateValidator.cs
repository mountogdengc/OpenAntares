using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Development;
internal static class DevelopmentStateValidator
{
    internal static IEnumerable<string> DraftErrors(ContentSet content,GovernorTemplateDraft d)
    {
        if(string.IsNullOrWhiteSpace(d.Name)||d.Name.Length>80)yield return "name must have1..80 characters";
        if(d.FoundationIds.IsDefault||d.DevelopmentIds.IsDefault){yield return "missing preference lists";yield break;}
        if(d.FoundationIds.Length>32||d.DevelopmentIds.Length>32)yield return "preference list exceeds32";
        if(d.FoundationIds.Concat(d.DevelopmentIds).Any(id=>id is null||!content.Buildings.ContainsKey(id)))yield return "unknown building reference";
        if(d.FoundationIds.Concat(d.DevelopmentIds).Distinct(StringComparer.Ordinal).Count()!=d.FoundationIds.Length+d.DevelopmentIds.Length)yield return "duplicate building reference";
        if(!Enum.IsDefined(d.SupportTarget)||d.ProductionWeight is <0 or >100||d.ResearchWeight is <0 or >100||d.ProductionWeight+d.ResearchWeight==0)yield return "invalid support/weights";
    }
    internal static bool DraftEqual(GovernorTemplateDraft a,GovernorTemplateDraft b)=>a.Name==b.Name&&a.SupportTarget==b.SupportTarget&&a.ProductionWeight==b.ProductionWeight&&a.ResearchWeight==b.ResearchWeight&&a.FoundationIds.SequenceEqual(b.FoundationIds)&&a.DevelopmentIds.SequenceEqual(b.DevelopmentIds);
    internal static void Validate(ContentSet content,GameState state,List<string> errors)
    {
        void Error(string text)=>errors.Add("development: "+text);
        if(!Rules.HasDevelopment(state.RulesVersion)) {
            if(state.Empires.Any(e=>e.Development is not null)||state.Colonies.Any(c=>c.Development is not null))Error("legacy campaigns cannot contain development state");
            if(state.Planets.Any(p=>p.Appearance is not null)||state.Empires.Any(e=>e.Surveys.Any(o=>o.Planets.Any(p=>p.Appearance is not null))))Error("legacy campaigns cannot contain appearance");
            return;
        }
        if(content.GovernorTemplates.IsEmpty)Error("explicit default templates required");
        foreach(var p in state.Planets)if(p.Appearance is not {Version:1,Variant:>=0 and <8})Error($"{p.Id} invalid appearance");
        foreach(var e in state.Empires)
        {
            var d=e.Development;if(d is null){Error($"{e.Id} missing development");continue;}
            if(d.NextTemplateNumber<1||d.CustomTemplates.Count>64)Error($"{e.Id} template limits/counter");
            var ids=new HashSet<string>(StringComparer.Ordinal);string? previous=null;
            foreach(var t in d.CustomTemplates) {
                if(!ids.Add(t.Id)||content.GovernorTemplates.ContainsKey(t.Id)||previous is not null&&StringComparer.Ordinal.Compare(previous,t.Id)>=0)Error($"{e.Id} duplicate/unsorted custom template");
                if(!t.Id.StartsWith("custom_",StringComparison.Ordinal)||!int.TryParse(t.Id.AsSpan(7),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out int n)||n<1||n>=d.NextTemplateNumber)Error($"{e.Id} invalid local template counter");
                Snapshot(t,e);previous=t.Id;
            }
            if(d.NewColonyPlan is {} inherit){Snapshot(inherit.Template,e);}
            foreach(var o in e.Surveys)foreach(var p in o.Planets)if(p.Appearance is not {Version:1,Variant:>=0 and <8})Error($"{e.Id} invalid observed appearance");
            if(d.PendingReport is {} pending) {if(pending.Turn!=state.Turn)Error("pending report turn");Report(pending,e);}
            if(d.CompletedReports.Count>10)Error("report history exceeds10");int last=-1;
            foreach(var report in d.CompletedReports){if(report.Turn<=last||report.Turn>=state.Turn)Error("completed report turns/order");last=report.Turn;Report(report,e);}
            if(e.Controller==ControllerKind.Ai&&(d.CustomTemplates.Count!=0||d.NewColonyPlan is not null||d.PendingReport is not null||d.CompletedReports.Count!=0||d.NextTemplateNumber!=1))Error("AI cannot have player governor library/reports");
        }
        foreach(var c in state.Colonies)
        {
            var d=c.Development;var e=state.FindEmpire(c.EmpireId);if(d is null||e is null){Error($"{c.Id} missing development/owner");continue;}
            if(d.Template is {} template)Snapshot(template,e);
            if(d.WorkforceEnabled&&d.Template is null)Error("workforce automation requires template");
            if(d.NextQueueToken<1||d.Queue.Count>32||!Enum.IsDefined(d.Origin))Error("queue limits/counter/origin");
            var tokens=new HashSet<int>();var buildings=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in d.Queue) {
                if(entry.Token<1||entry.Token>=d.NextQueueToken||!tokens.Add(entry.Token))Error("duplicate/invalid queue token");
                if(!TargetValid(entry.Target,c))Error("invalid queue target");
                if(entry.Target.Kind==ProductionKinds.Building&&(!buildings.Add(entry.Target.ContentId)||c.CompletedBuildingIds.Contains(entry.Target.ContentId)))Error("duplicate/completed queue building");
            }
            if(d.ActiveQueueToken is {} active) {
                var entry=d.Queue.Find(q=>q.Token==active);
                if(d.Origin!=ProductionOrigin.Queue||entry is null||c.ProjectId!=entry.Target.ContentId||c.ProjectKind!=entry.Target.Kind)Error("active queue binding");
            } else if(d.Origin==ProductionOrigin.Queue)Error("queue origin needs binding");
            if(c.ProjectId is null&&d.Origin!=ProductionOrigin.None)Error("origin without active project");
            if(e.Controller==ControllerKind.Ai&&(d.Template is not null||d.WorkforceEnabled||d.ConstructionEnabled||d.Queue.Count!=0||d.ActiveQueueToken is not null||d.NextQueueToken!=1||d.Origin!=ProductionOrigin.None))Error("AI cannot have player colony controls");
        }
        bool TargetValid(ProductionTarget t,ColonyState c)=>t.Kind==ProductionKinds.Building?content.Buildings.ContainsKey(t.ContentId):t.Kind==ProductionKinds.ShipDesign?ShipDesigns.ShipDesignTargets.Bound(content,state,c.EmpireId,t) is not null:t.Kind==ProductionKinds.Vessel&&Species.SpeciesRules.CanBuildVessel(content,state,c.EmpireId,t.ContentId)&&(!Rules.HasShipDesigns(state.RulesVersion)||t.ContentId!=content.ShipDesigns?.WarshipRoleId);
        void Snapshot(GovernorTemplateSnapshot t,EmpireState owner) {
            if(string.IsNullOrWhiteSpace(t.Id)||t.Revision<1||DraftErrors(content,t.Definition).Any())Error("invalid template snapshot");
            if(content.GovernorTemplates.TryGetValue(t.Id,out var builtin)) {
                var draft=new GovernorTemplateDraft(builtin.Name,builtin.FoundationIds,builtin.DevelopmentIds,builtin.SupportTarget,builtin.ProductionWeight,builtin.ResearchWeight);
                if(t.Revision!=1||!DraftEqual(t.Definition,draft))Error("built-in snapshot modified");
            } else {
                var latest=owner.Development?.CustomTemplates.Find(x=>x.Id==t.Id);
                if(latest is null||t.Revision>latest.Revision)Error("undefined template snapshot/revision");
            }
        }
        void Report(GovernorTurnReport r,EmpireState owner) {
            if(r.UnchangedCount<0||r.Entries.IsDefault){Error("report entries/count");return;}int last=0;
            foreach(var entry in r.Entries) {
                var live=state.FindColony(entry.Colony);
                bool retired=live is null&&Rules.HasCrises(state.RulesVersion)&&owner.Crisis?.LostColonyLocations.Any(l=>l.Colony==entry.Colony)==true;
                if(entry.Colony.Value<=last||live?.EmpireId!=owner.Id&&!retired)Error("report entry ownership/order");last=entry.Colony.Value;
                if(entry.BeforeWorkforce.HasValue!=entry.AfterWorkforce.HasValue||entry.Issues.IsDefault)Error("report action/issue shape");
                foreach(var w in new[]{entry.BeforeWorkforce,entry.AfterWorkforce})if(w is {} value&&(value.Support<0||value.Production<0||value.Research<0))Error("report workforce");
                bool Historical(ProductionTarget? target,ShipDesigns.EquipmentFact? fact)
                {
                    if(target?.Kind!=ProductionKinds.ShipDesign)return fact is null;
                    return Rules.HasShipDesigns(state.RulesVersion)&&ShipDesigns.ShipDesignTargets.TryId(target.ContentId,out int id)&&fact?.Id.Value==id&&ShipDesigns.ShipDesignStateValidator.ValidFact(content,owner,fact);
                }
                if(!Historical(entry.BeforeTarget,entry.BeforeEquipment)||!Historical(entry.AfterTarget,entry.AfterEquipment))Error("report copied equipment facts");
                var issues=new HashSet<GovernorIssue>();foreach(var issue in entry.Issues)if(!(issue.Code is ("support_shortage" or "construction_locked" or "embarkation_wait" or "plan_complete")||Rules.HasCrises(state.RulesVersion)&&issue.Code=="quarantine_wait")||!issues.Add(issue)||issue.ContentId is {} id&&!content.Buildings.ContainsKey(id)&&!content.Vessels.ContainsKey(id)&&!(Rules.HasShipDesigns(state.RulesVersion)&&new[]{entry.BeforeEquipment,entry.AfterEquipment}.Any(f=>f is not null&&ShipDesigns.ShipDesignTargets.TryId(id,out int n)&&f.Id.Value==n)))Error("unknown/duplicate report issue");
                foreach(var target in new[]{entry.BeforeTarget,entry.AfterTarget})if(target is {} t&&!(t.Kind==ProductionKinds.Building?content.Buildings.ContainsKey(t.ContentId):t.Kind==ProductionKinds.Vessel&&content.Vessels.ContainsKey(t.ContentId)||Rules.HasShipDesigns(state.RulesVersion)&&t.Kind==ProductionKinds.ShipDesign))Error("report target");
            }
        }
    }
}
