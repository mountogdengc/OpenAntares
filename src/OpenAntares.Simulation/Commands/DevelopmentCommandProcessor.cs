using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Commands;
public static partial class CommandProcessor
{
    private static CommandOutcome ExecuteDevelopment(ContentSet content,GameState state,EmpireState issuer,DevelopmentCommand command)
    {
        if(!Rules.HasDevelopment(state.RulesVersion))return CommandOutcome.Reject(state,"development_unavailable","Start a new colony-development campaign to use governors and queues.");
        if(issuer.Controller!=ControllerKind.Human||issuer.Development is null)return CommandOutcome.Reject(state,"not_human","Player colony controls require a human empire.");
        var candidate=state.Clone();var owner=candidate.FindEmpire(issuer.Id)!;var library=owner.Development!;
        try {
            switch(command) {
                case UpsertGovernorTemplateCommand c:
                    CheckDraft(c.Draft);
                    if(c.TemplateId is null)Add(c.Draft);
                    else {
                        int index=library.CustomTemplates.FindIndex(t=>t.Id==c.TemplateId);Require(index>=0,"Built-in or missing templates cannot be edited; copy to a new custom template.");
                        var old=library.CustomTemplates[index];if(!DevelopmentStateValidator.DraftEqual(old.Definition,c.Draft))library.CustomTemplates[index]=new(old.Id,checked(old.Revision+1),c.Draft);
                    }
                    break;
                case DeleteGovernorTemplateCommand c:
                    Require(library.CustomTemplates.Any(t=>t.Id==c.TemplateId),"Only existing custom templates may be deleted.");
                    Require(!candidate.Colonies.Any(col=>col.EmpireId==issuer.Id&&col.Development?.Template?.Id==c.TemplateId)&&library.NewColonyPlan?.Template.Id!=c.TemplateId,"Template is assigned to colonies or new-colony inheritance.");
                    library.CustomTemplates.RemoveAll(t=>t.Id==c.TemplateId);break;
                case AssignGovernorTemplateCommand c:
                    Require(!c.Colonies.IsDefaultOrEmpty&&c.Colonies.Distinct().Count()==c.Colonies.Length,"Select distinct owned colonies.");
                    var snapshot=Resolve(c.TemplateId);Require(!c.WorkforceEnabled||snapshot is not null,"Workforce automation requires a template.");
                    foreach(var id in c.Colonies.OrderBy(x=>x)) {
                        var colony=Owned(id);var d=colony.Development!;d.Template=snapshot;d.WorkforceEnabled=c.WorkforceEnabled;d.ConstructionEnabled=c.ConstructionEnabled;
                        if(c.ReplaceActive&&colony.ProjectId is not null){Clear(colony);MarkOverride(candidate,colony,false);}
                    }break;
                case SetColonyAutomationCommand c:
                    var automation=Owned(c.Colony).Development!;Require(!c.WorkforceEnabled||automation.Template is not null,"Workforce automation requires a template.");
                    automation.WorkforceEnabled=c.WorkforceEnabled;automation.ConstructionEnabled=c.ConstructionEnabled;break;
                case SetColonyQueueCommand c:
                    Require(!c.Entries.IsDefault&&c.Entries.Length<=32,"Queue requires0..32 finite entries.");
                    var queued=Owned(c.Colony);var controls=queued.Development!;
                    var replacements=new System.Collections.Generic.List<ProductionQueueEntry>();
                    foreach(var draft in c.Entries) {
                        Require(draft is not null&&draft.Target is not null,"Missing queue entry/target.");
                        int token;if(draft!.Token is {} retained){Require(controls.Queue.Any(q=>q.Token==retained),"Queue token belongs to another queue or no longer exists.");token=retained;}
                        else {token=controls.NextQueueToken;controls.NextQueueToken=checked(token+1);}
                        replacements.Add(new(token,draft.Target));
                    }
                    if(controls.ActiveQueueToken is {} active) {
                        var old=controls.Queue.Single(q=>q.Token==active);var replacement=replacements.Find(q=>q.Token==active);
                        if(replacement!=old){Require(c.ReplaceActive,"Confirm replacement of active work.");Clear(queued);MarkOverride(candidate,queued,false);}
                    }
                    controls.Queue=replacements;break;
                case SetNewColonyPlanCommand c:
                    var inherited=Resolve(c.TemplateId);Require(inherited is not null||!c.WorkforceEnabled&&!c.ConstructionEnabled,"Select an inheritance template or disable inheritance.");
                    library.NewColonyPlan=inherited is null?null:new(inherited,c.WorkforceEnabled,c.ConstructionEnabled);break;
                case ImportGovernorTemplatesCommand c:
                    Require(!c.Templates.IsDefault&&c.Templates.Length<=64,"Invalid preset batch.");foreach(var draft in c.Templates){CheckDraft(draft);Add(draft);}break;
                default: throw new InvalidOperationException("Unknown development command.");
            }
            library.CustomTemplates.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Id,b.Id));
            bool changed=!EmpireEqual(issuer.Development,library)||state.Colonies.Where(c=>c.EmpireId==issuer.Id).Any(c=>!ColonyEqual(c,candidate.FindColony(c.Id)!));
            if(!changed)return CommandOutcome.NoChange(state);
            owner.Ready=false;
            var errors=GameStateValidator.Validate(content,candidate);Require(errors.Count==0,string.Join("; ",errors));
            return new(candidate,null,true,null);
        }catch(Exception ex) when(ex is InvalidOperationException or OverflowException or ArgumentException) {return CommandOutcome.Reject(state,"invalid_development",ex.Message);}
        void CheckDraft(GovernorTemplateDraft draft){Require(draft is not null,"Missing template.");Require(!DevelopmentStateValidator.DraftErrors(content,draft).Any(),"Invalid template name, references, support or weights.");}
        void Add(GovernorTemplateDraft draft) {
            Require(library.CustomTemplates.Count<64,"Template library is full (64).");
            string id;
            do {int n=library.NextTemplateNumber;library.NextTemplateNumber=checked(n+1);id="custom_"+n.ToString(CultureInfo.InvariantCulture);}while(content.GovernorTemplates.ContainsKey(id)||library.CustomTemplates.Any(t=>t.Id==id));
            library.CustomTemplates.Add(new(id,1,draft));
        }
        GovernorTemplateSnapshot? Resolve(string? id) {
            if(id is null)return null;
            if(content.GovernorTemplates.TryGetValue(id,out var d))return new(id,1,new(d.Name,d.FoundationIds,d.DevelopmentIds,d.SupportTarget,d.ProductionWeight,d.ResearchWeight));
            var own=library.CustomTemplates.Find(t=>t.Id==id);Require(own is not null,"Template is missing or not owned.");return own;
        }
        ColonyState Owned(ColonyId id){var c=candidate.FindColony(id);Require(c is not null&&c.EmpireId==issuer.Id&&c.Development is not null,"Select owned colonies with development controls.");return c!;}
    }
    private static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool test,string message){if(!test)throw new InvalidOperationException(message);}
    private static bool SnapshotEqual(GovernorTemplateSnapshot? a,GovernorTemplateSnapshot? b)=>a is null?b is null:b is not null&&a.Id==b.Id&&a.Revision==b.Revision&&DevelopmentStateValidator.DraftEqual(a.Definition,b.Definition);
    private static bool EmpireEqual(EmpireDevelopmentState a,EmpireDevelopmentState b)=>a.NextTemplateNumber==b.NextTemplateNumber&&a.CustomTemplates.Count==b.CustomTemplates.Count&&a.CustomTemplates.Zip(b.CustomTemplates).All(p=>SnapshotEqual(p.First,p.Second))&&(a.NewColonyPlan is null?b.NewColonyPlan is null:b.NewColonyPlan is not null&&a.NewColonyPlan.WorkforceEnabled==b.NewColonyPlan.WorkforceEnabled&&a.NewColonyPlan.ConstructionEnabled==b.NewColonyPlan.ConstructionEnabled&&SnapshotEqual(a.NewColonyPlan.Template,b.NewColonyPlan.Template));
    private static bool ColonyEqual(ColonyState a,ColonyState b) {
        var x=a.Development!;var y=b.Development!;return a.ProjectId==b.ProjectId&&a.ProjectKind==b.ProjectKind&&SnapshotEqual(x.Template,y.Template)&&x.WorkforceEnabled==y.WorkforceEnabled&&x.ConstructionEnabled==y.ConstructionEnabled&&x.NextQueueToken==y.NextQueueToken&&x.ActiveQueueToken==y.ActiveQueueToken&&x.Origin==y.Origin&&x.Queue.SequenceEqual(y.Queue);
    }
    private static void Clear(ColonyState c){c.ProjectId=null;c.ProjectKind=ProductionKinds.Building;c.Development!.ActiveQueueToken=null;c.Development.Origin=ProductionOrigin.None;}
    private static void ApplyManualIntervention(GameState candidate,Command command)
    {
        ColonyId? id=command switch {SetWorkforceCommand c=>c.Colony,SetProjectCommand c=>c.Colony,SetProductionTargetCommand c=>c.Colony,_=>null};
        if(id is null||candidate.FindEmpire(command.Issuer)?.Controller!=ControllerKind.Human)return;
        var colony=candidate.FindColony(id.Value)!;var d=colony.Development;if(d is null)return;
        bool workforce=command is SetWorkforceCommand;
        if(workforce)d.WorkforceEnabled=false;
        else {d.ConstructionEnabled=false;d.ActiveQueueToken=null;d.Origin=colony.ProjectId is null?ProductionOrigin.None:ProductionOrigin.Manual;}
        MarkOverride(candidate,colony,workforce);
    }
    private static void MarkOverride(GameState candidate,ColonyState colony,bool workforce)
    {
        var d=candidate.FindEmpire(colony.EmpireId)!.Development!;
        if(d.PendingReport is not {} report)return;
        var entries=report.Entries.Select(e=>e.Colony!=colony.Id?e:workforce?e with{WorkforceOverridden=e.BeforeWorkforce.HasValue||e.WorkforceOverridden}:e with{ConstructionOverridden=e.BeforeTarget!=e.AfterTarget||e.ConstructionOverridden}).ToImmutableArray();
        d.PendingReport=report with{Entries=entries};
    }
    internal static CommandOutcome ExecuteGovernor(ContentSet content,GameState state,Command command,int? queueToken,ProductionOrigin origin)
    {
        if(!Rules.HasDevelopment(state.RulesVersion)||state.FindEmpire(command.Issuer)?.Controller!=ControllerKind.Human)
            return CommandOutcome.Reject(state,"invalid_governor","Only human development planning may prepare governors.");
        ColonyId? id=command switch{SetWorkforceCommand c=>c.Colony,SetProductionTargetCommand c=>c.Colony,_=>null};
        if(id is null||state.FindColony(id.Value) is not {} colony||colony.EmpireId!=command.Issuer||colony.Development is not {} d)
            return CommandOutcome.Reject(state,"invalid_governor","Invalid governor command scope.");
        if(command is SetWorkforceCommand&&!d.WorkforceEnabled||command is SetProductionTargetCommand&&!d.ConstructionEnabled)
            return CommandOutcome.Reject(state,"invalid_governor","Automation channel is paused.");
        if(command is SetProductionTargetCommand target) {
            bool bound=target.Target is null?origin==ProductionOrigin.None&&queueToken is null:
                origin==ProductionOrigin.Queue?queueToken is {} token&&d.Queue.Any(q=>q.Token==token&&q.Target==target.Target):
                origin==ProductionOrigin.Template&&queueToken is null&&target.Target.Kind==ProductionKinds.Building&&d.Template is {} t&&t.Definition.FoundationIds.Concat(t.Definition.DevelopmentIds).Contains(target.Target.ContentId);
            if(!bound)return CommandOutcome.Reject(state,"invalid_governor","Target is outside the explicit queue or assigned template.");
        }
        var outcome=ExecuteCore(content,state,command);
        if(!outcome.Accepted||!outcome.Changed)return outcome;
        if(command is SetProductionTargetCommand) {var after=outcome.State.FindColony(id.Value)!.Development!;after.ActiveQueueToken=queueToken;after.Origin=origin;}
        if(GameStateValidator.Validate(content,outcome.State,allowAllHumansReady:true).Count!=0)return CommandOutcome.Reject(state,"invalid_governor","Prepared orders failed validation.");
        return outcome;
    }
}
