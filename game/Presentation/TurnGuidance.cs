using System;
using System.Collections.Generic;
using System.Linq;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Presentation;

public enum GuidanceSeverity { Required, Consider }
public sealed record TurnGuidanceItem(string Key, GuidanceSeverity Severity, string Text, string Destination);

/// <summary>Requester-visible notices only. The engine checks forecast-dependent readiness.</summary>
public static class TurnGuidance
{
    public const string NoResponseNotices = "No urgent notices. End Turn checks your plans.";
    private static string Count(int n, string one, string many) => n + " " + (n == 1 ? one : many);
    public static IReadOnlyList<TurnGuidanceItem> Build(EmpireView view)
    {
        var items = new List<TurnGuidanceItem>();
        int conflict = view.Conflict?.Encounters.Count(e=>e.OwnNeedsResponse) ?? 0;
        if(conflict>0) items.Add(new("conflict",GuidanceSeverity.Required,Count(conflict,"fleet response","fleet responses") + " to review.","Conflict"));
        int combat = view.Combat?.Notices.Count(n=>n.Level==CombatNoticeLevel.Emergency) ?? 0;
        if(combat>0) items.Add(new("combat",GuidanceSeverity.Required,Count(combat,"combat response","combat responses") + " to review.","Combat"));
        int crisis = view.Crisis is {RequireAcknowledgement:true} health ? health.PendingReceipts.Length : 0;
        if(crisis>0) items.Add(new("crisis",GuidanceSeverity.Required,Count(crisis,"colony alert needs","colony alerts need") + " your acknowledgement.","Crisis"));
        int scouting = view.Scouting?.Vessels.Count(v=>v.Stage is not null && (!v.Acknowledged || view.Scouting.Attention.Any(t=>t.Vessel==v.Id&&t.Action=="response_required"))) ?? 0;
        if(scouting>0) items.Add(new("scouting",GuidanceSeverity.Required,Count(scouting,"scout encounter","scout encounters") + " to review.","Scouts"));
        if(view.Research.TargetId is null) items.Add(new("research",GuidanceSeverity.Consider,"No research target: research is saved. Choosing a target is optional.","Research"));
        int projects=view.Colonies.Count(c=>c.ProjectId is null);
        if(projects>0) items.Add(new("projects",GuidanceSeverity.Consider,Count(projects,"colony is","colonies are") + " saving production. Choose construction when you are ready.","Colonies"));
        int offers=view.Diplomacy?.Contacts.Count(c=>c.Pending is {OwnAnswered:false} offer&&offer.Recipient==view.Viewer)??0;
        if(offers>0) items.Add(new("offers",GuidanceSeverity.Consider,Count(offers,"diplomatic offer","diplomatic offers") + " awaiting your decision.","Diplomacy"));
        return items.OrderBy(i=>i.Severity).ThenBy(i=>i.Key,StringComparer.Ordinal).ToArray();
    }
}
