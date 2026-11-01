using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Turns;

namespace OpenAntares.Game.Components;

/// <summary>
/// What happened in the turn that just resolved, for one empire: each colony's production, growth,
/// completions, and shortages, and the empire's research. A completion is reported once, with a
/// button to choose the next target.
/// </summary>
public partial class TurnReportDialog : AcceptDialog
{
    private readonly VBoxContainer _body = new();
    public string CombatPreferencesPath{get;set;}="user://combat-notifications.json";
    public string FleetPreferencesPath{get;set;}="user://fleet-notifications.json";
    private OpenAntares.Game.Presentation.CrisisNotificationPreferences _crisisPreferences=OpenAntares.Game.Presentation.CrisisNotificationPreferences.Defaults;
    private OpenAntares.Game.Presentation.ColonyNotificationPreferences _preferences = OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults;

    /// <summary>Raised when the player wants to choose a colony's next project (the colony's star).</summary>
    public event Action<ColonyId>? ChooseProjectRequested;

    /// <summary>Raised when the player wants to choose the next research target.</summary>
    public event Action? ChooseResearchRequested;
    public event Action<StarId>? StarRequested;
    public event Action<ColonyId>? ColonyRequested;
    public event Action<VesselId>? VesselRequested;
    public event Action? FleetCommandRequested;
    public event Action<EmpireId>? DiplomacyRequested;
    public event Action<VesselId?>? ConflictOrdersRequested;

    public override void _Ready()
    {
        OkButtonText = "Continue";
        var padding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(520, 360), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        scroll.AddChild(_body);
        _body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        padding.AddChild(scroll);
        AddChild(padding);
    }

    public void ShowReport(ContentSet content, EmpireTurnReportView report, EmpireView? empireView = null, OpenAntares.Game.Presentation.ColonyNotificationPreferences? preferences = null,OpenAntares.Game.Presentation.CrisisNotificationPreferences? crisisPreferences = null)
    {
        foreach (Node child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }

        _preferences = preferences ?? OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults;
        _crisisPreferences=crisisPreferences??OpenAntares.Game.Presentation.CrisisNotificationPreferences.Defaults;
        Title = "Turn report";
        if(report.Conflict is {} conflict)
        {
            foreach(var contact in conflict.Events.GroupBy(e=>e.Other))
            {
                foreach(var entry in contact){_body.AddChild(DevelopmentUi.Text(entry.Reason));if(entry.Star is {} star)DevelopmentUi.Button(_body,"Locate conflict system",()=>StarRequested?.Invoke(star),"ReportConflictEventStar_"+entry.Id);}
                var other=contact.Key;DevelopmentUi.Button(_body,"Open conflict diplomacy",()=>DiplomacyRequested?.Invoke(other),"ReportOpenConflictDiplomacy_"+other.Value);
            }
            foreach(var battle in conflict.Encounters)
            {
                _body.AddChild(DevelopmentUi.Text($"Battle {battle.Id.Value}: {battle.OwnMembers.Length} own armed ships and {battle.ProtectedCivilians.Length} protected civilians. "+(battle.OwnNeedsResponse?"Your revised force needs a response before Ready.":"Review your committed orders and return route.")));
                DevelopmentUi.Button(_body,"Review this force / withdrawal",()=>ConflictOrdersRequested?.Invoke(battle.OwnMembers.FirstOrDefault()),"ReportConflictOrders_"+battle.Id.Value);
                DevelopmentUi.Button(_body,"Locate battle",()=>StarRequested?.Invoke(battle.Star),"ReportConflictStar_"+battle.Id.Value);
            }
            foreach(var round in conflict.Rounds)_body.AddChild(DevelopmentUi.Text($"Battle {round.Encounter.Value}, round {round.Receipt.Round}{(round.Receipt.Interception?" — interception":"")}: your hull lost {round.Receipt.Own.Sum(s=>s.ActualLoss)}; observed opposing hull lost {round.Receipt.Opposing.Sum(s=>s.ActualLoss)}. Enemy future orders are unknown."));
            foreach(var blockade in conflict.Blockades)_body.AddChild(DevelopmentUi.Text($"Your fleet {blockade.Fleet.Value} blockade: {blockade.Status}; eligible from turn {blockade.EffectiveTurn}. Opposing production and reserves are unknown."));
            foreach(var pressure in conflict.OwnPressure)
            {
                _body.AddChild(DevelopmentUi.Text($"Your colony system retains {pressure.RetainedPercent}% of ordinary net production before trade under current blockade pressure. Review your own income breakdown below."));
                DevelopmentUi.Button(_body,"Open blockade diplomacy",()=>DiplomacyRequested?.Invoke(pressure.Other),"ReportOpenBlockadeDiplomacy_"+pressure.Other.Value);
                DevelopmentUi.Button(_body,"Locate affected colony system",()=>StarRequested?.Invoke(pressure.Star),"ReportBlockadeStar_"+pressure.Star.Value);
            }
            if(!conflict.Rounds.IsEmpty)DevelopmentUi.Button(_body,"Review survivors, return and finite repair",()=>FleetCommandRequested?.Invoke(),"ReportConflictRepair");
        }
        _body.AddChild(new Label { Text = $"Turn {report.FromTurn} results", ThemeTypeVariation = "HeaderLabel" });
        if(report.ShipDesignEvents is {} equipmentEvents&&!equipmentEvents.IsEmpty)
        {
            _body.AddChild(new Label{Text="Ship equipment",ThemeTypeVariation="HeaderLabel"});
            foreach(var entry in equipmentEvents.DistinctBy(e=>e.Id).OrderBy(e=>e.Id))
            {
                string names=(entry.Before?.Name??"Previous equipment")+" → "+(entry.After?.Name??"Retained equipment");string paid=entry.Paid>0?$" Paid {Format.Points(entry.Paid)} ({entry.Paid} internal) once.":" No payment.";
                _body.AddChild(Wrapped(names+". "+entry.Reason+paid,"SubtleLabel"));
            }
        }
        if(report.Combat is not null){var combat=new CombatReportPanel{Name="TurnCombatReport",ShowRoutine=OpenAntares.Game.Presentation.CombatNotificationPreferences.Load(ProjectSettings.GlobalizePath(CombatPreferencesPath)).ShowRoutine};_body.AddChild(combat);combat.SetReport(report);}
        if(empireView?.Fleets is not null)
        {
            var command=new Button{Name="ReportOpenFleetCommand",Text="Fleet command and service records"};_body.AddChild(command);command.Pressed+=()=>FleetCommandRequested?.Invoke();
            bool routine=OpenAntares.Game.Presentation.FleetNotificationPreferences.Load(ProjectSettings.GlobalizePath(FleetPreferencesPath)).ShowRoutine;
            foreach(var group in report.FleetEvents.Where(e=>routine||e.Importance==OpenAntares.Simulation.Fleets.FleetImportance.Attention).GroupBy(e=>(e.Kind,e.Importance)))
                _body.AddChild(new Label{Text=$"{group.Key.Kind.Replace('_',' ')}: {group.Count()} outcome(s). "+string.Join(" ",group.Take(4).Select(e=>e.Reason))+(group.Count()>4?" Open fleet command for all own details.":""),ThemeTypeVariation=group.Key.Importance==OpenAntares.Simulation.Fleets.FleetImportance.Attention?"WarningLabel":"SubtleLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill});
        }
        foreach(var entry in report.ScoutEvents)
        {
            _body.AddChild(new Label{Text=$"{content.Vessels[entry.RoleId].Name} {entry.Vessel.Value}: {entry.Kind.Replace('_',' ')}. {entry.Reason}"+(entry.ResearchAward>0?$" Earned {Format.Points(entry.ResearchAward)} research.":""),ThemeTypeVariation=entry.Kind=="loss"?"WarningLabel":"SubtleLabel",AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=Control.SizeFlags.ExpandFill});
            var star=entry.Star;var link=new Button{Text="Show recorded scout system"};_body.AddChild(link);link.Pressed+=()=>{Hide();StarRequested?.Invoke(star);};
        }

        if (empireView?.Development is not null) { var governors = new GovernorReportPanel { Preferences = preferences ?? OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults }; _body.AddChild(governors); governors.ShowReports(content,empireView); }

        foreach (ColonyReportView colony in report.Colonies)
        {
            AddColony(content, colony, report, empireView?.Colonies.Any(c=>c.Id==colony.Id)??true);
        }

        EmpireEconomy empire = report.Research;
        _body.AddChild(new HSeparator());
        _body.AddChild(new Label { Text = "Research" });
        _body.AddChild(Subtle(ReserveSummary(empire.Research)));
        if (_preferences.LevelFor("completions") != OpenAntares.Game.Presentation.NotificationLevel.Off && empire.Research.CompletedId is { } technology)
        {
            _body.AddChild(CompletionNotice($"Researched {content.Technologies[technology].Name}."));
            var choose = new Button { Text = "Choose next research" };
            choose.Pressed += () =>
            {
                Hide();
                ChooseResearchRequested?.Invoke();
            };
            _body.AddChild(choose);
        }

        foreach(var crisis in report.CrisisEvents.Where(e=>e.Kind is not ("cure_progress" or "inspection_completed")||_crisisPreferences.Routine!=OpenAntares.Game.Presentation.NotificationLevel.Off))_body.AddChild(Wrapped($"{crisis.Location.Name} ({crisis.Location.Colony}): {crisis.Kind} — {crisis.Reason}; population lost {crisis.PopulationLost}.","WarningLabel"));
        foreach (var e in report.Events)
        {
            if(e.Kind=="vessel_completed"&&_preferences.LevelFor("completions")==OpenAntares.Game.Presentation.NotificationLevel.Off)continue;
            // The colony completion owns the notice and both navigation actions.
            if (e.Kind == "vessel_completed" && report.Colonies.Any(c =>
                c.Id == e.Colony && c.Economy.Production.TargetKind is ProductionKinds.Vessel or ProductionKinds.ShipDesign &&
                c.Economy.Production.CompletedId is not null)) continue;
            string location = e.Star is { } star && report.StarNames.TryGetValue(star, out var name) ? name : "system";
            string message = e.Kind switch { "arrived" => $"Vessel arrived at {location}.", "discovered" => $"Surveyed the planets of {location}.", "vessel_completed" => $"Expedition completed at {location}.", "colony_founded" => $"Colony founded at {location}.", "founding_failed" => $"Another expedition settled {location} first. Your settler and cargo were retained.", _ => "Expedition updated." };
            _body.AddChild(e.Kind=="vessel_completed"?CompletionNotice(message):Wrapped(message, "SubtleLabel"));
            var view = new Button { Text = e.Colony.HasValue && e.Kind == "colony_founded" ? "View colony" : e.Vessel.HasValue && e.Kind == "vessel_completed" ? "View vessel" : "View system" };
            view.Pressed += () => { Hide(); if (e.Colony is { } colony && e.Kind == "colony_founded") ColonyRequested?.Invoke(colony); else if (e.Vessel is { } vessel && e.Kind == "vessel_completed") VesselRequested?.Invoke(vessel); else if (e.Star is { } destination) StarRequested?.Invoke(destination); }; _body.AddChild(view);
        }
        if(report.DiplomacyEvents is {} diplomacyEvents&&!diplomacyEvents.IsEmpty)
        {
            _body.AddChild(new Label{Text="Diplomacy",ThemeTypeVariation="HeaderLabel"});
            foreach(var group in diplomacyEvents.GroupBy(e=>e.Other))
            {
                foreach(var e in group)_body.AddChild(Wrapped(e.Kind.Replace('_',' ')+": "+e.Reason+(e.Paid>0?" Setup paid: "+Format.Points(e.Paid)+" production.":""),"SubtleLabel"));
                var other=group.Key;string name=empireView?.Contacts.FirstOrDefault(c=>c.OtherEmpireId==other)?.CultureName??"this contact";
                DevelopmentUi.Button(_body,"Open diplomacy with "+name,()=>{Hide();DiplomacyRequested?.Invoke(other);},"ReportOpenDiplomacy_"+other.Value);
            }
        }
        foreach (var encounter in report.EncounterEvents)
        {
            if(encounter.Kind=="first_contact"&&report.DiplomacyEvents is {} de&&de.Any(e=>e.Kind=="first_contact"&&e.Other==encounter.OtherEmpireId))continue;
            string location = report.StarNames.TryGetValue(encounter.StarId,out var name) ? name : "system";
            string species = encounter.SpeciesName ?? "unidentified";
            string culture = encounter.CultureName is { } knownCulture ? $" ({knownCulture})" : string.Empty;
            string world = encounter.PlanetOrbit is { } orbit ? $"{location}, orbit {orbit}" : location;
            _body.AddChild(Wrapped(encounter.Kind == "first_contact"
                ? $"First contact observed with {species}{culture} at {location} on turn {encounter.ObservedTurn}."
                : $"Observed a {species} colony{culture} at {world} on turn {encounter.ObservedTurn}.","SubtleLabel"));
        }
        PopupCentered(new Vector2I(580, 440));
    }

    private Label CompletionNotice(string text)
    {
        bool attention=_preferences.LevelFor("completions")==OpenAntares.Game.Presentation.NotificationLevel.Attention;
        return new Label{Text=(attention?"Attention: ":"")+text,ThemeTypeVariation=attention?"WarningLabel":"",AutowrapMode=TextServer.AutowrapMode.WordSmart};
    }

    private void AddColony(ContentSet content, ColonyReportView view, EmpireTurnReportView report, bool colonySurvives)
    {
        ColonyEconomy colony = view.Economy;
        _body.AddChild(new HSeparator());
        _body.AddChild(new Label { Text = $"Colony at {view.Name}" });
        _body.AddChild(Subtle("Production: " + ReserveSummary(colony.Production)));

        if (_preferences.LevelFor("completions") != OpenAntares.Game.Presentation.NotificationLevel.Off && colony.Production.CompletedId is { } building)
        {
            _body.AddChild(CompletionNotice($"Completed {(colony.Production.TargetKind == ProductionKinds.Vessel ? content.Vessels[building].Name : colony.Production.TargetKind is ProductionKinds.ShipDesign or ProductionKinds.Refit ? (colony.Production.TargetKind==ProductionKinds.Refit?"refit to ":"")+(view.ProductionEquipment?.Name??"design #"+building) : content.Buildings[building].Name)}."));
            if(colonySurvives)
            {
                var choose = new Button { Text = "Choose next project" };
                choose.Pressed += () =>
                {
                    Hide();
                    ChooseProjectRequested?.Invoke(view.Id);
                };
                _body.AddChild(choose);
            }
            else _body.AddChild(Wrapped("This colony was lost after completion; no next project can be selected here.","WarningLabel"));
            if (colony.Production.TargetKind is ProductionKinds.Vessel or ProductionKinds.ShipDesign)
            {
                foreach (var completion in report.Events.Where(e =>
                    e.Kind == "vessel_completed" && e.Colony == view.Id && e.Vessel.HasValue))
                {
                    var inspect = new Button { Text = "View vessel" };
                    inspect.Pressed += () =>
                    {
                        Hide();
                        VesselRequested?.Invoke(completion.Vessel!.Value);
                    };
                    _body.AddChild(inspect);
                }
            }
        }

        if (colony.EmbarkOutcome is { } embark)
        {
            _body.AddChild(Wrapped($"Embarked workers: source {colony.PopulationUnitLabel} now {embark.PopulationAfter}; next support {Format.Points(embark.NextSupportProduced.Total)} / {Format.Points(embark.NextSupportRequired.Total)}.", embark.ReassignmentNeeded ? "WarningLabel" : "SubtleLabel"));
            if (embark.ReassignmentNeeded) _body.AddChild(Wrapped("Reassign workers before the next End Turn.", "WarningLabel"));
        }
        if (_preferences.LevelFor("embarkation_wait") != OpenAntares.Game.Presentation.NotificationLevel.Off && colony.Production.BlockedReasonCode is not null) _body.AddChild(Wrapped(colony.Production.BlockedReasonCode=="quarantine"?"Quarantine: cargo waits for the owned port to reopen; production remains banked.":"Awaiting embarkation; production remains banked.", "WarningLabel"));
        GrowthOutcome growth = colony.Growth;
        if (growth.Births > 0)
        {
            _body.AddChild(new Label { Text = $"{char.ToUpperInvariant(colony.PopulationUnitLabel[0])}{colony.PopulationUnitLabel[1..]} grew to {growth.PopulationAfter}. New workers start in support." });
        }

        if (growth.AtCapacity && growth.PopulationBefore < growth.Capacity)
        {
            _body.AddChild(Wrapped($"The colony reached its capacity of {growth.Capacity}.", "SubtleLabel"));
        }

        if (_preferences.LevelFor("support_shortage") != OpenAntares.Game.Presentation.NotificationLevel.Off && colony.SupportDeficit > 0)
        {
            long productionLost = -ShortageAmount(colony.NetProduction);
            long researchLost = -ShortageAmount(colony.NetResearch);
            _body.AddChild(Wrapped(
                $"Support shortage: lost {Format.Points(productionLost)} production and {Format.Points(researchLost)} research, and growth was paused.",
                "WarningLabel"));
        }
    }

    /// <summary>Earned, paid, and the resulting reserve, all read from the simulation's reserve outcome.</summary>
    private static string ReserveSummary(ReserveOutcome reserve)
    {
        long earned = reserve.Change.Lines.Where(l => l.Kind == BreakdownKinds.Earned).Sum(l => l.Amount);
        long paid = -reserve.Change.Lines.Where(l => l.Kind == BreakdownKinds.CostPaid).Sum(l => l.Amount);
        string paidText = paid > 0 ? $", paid {Format.Points(paid)}" : string.Empty;
        return $"{BreakdownText.Signed(earned)}{paidText}, reserve now {Format.Points(reserve.Ending)}";
    }

    private static long ShortageAmount(Quantity quantity) =>
        quantity.Lines.Where(l => l.Kind == BreakdownKinds.Shortage).Sum(l => l.Amount);

    private static Label Subtle(string text) => Wrapped(text, "SubtleLabel");

    private static Label Wrapped(string text, string variation) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        CustomMinimumSize = new Vector2(400, 0),
    };
}
