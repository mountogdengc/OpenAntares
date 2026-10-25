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
    private OpenAntares.Game.Presentation.ColonyNotificationPreferences _preferences = OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults;

    /// <summary>Raised when the player wants to choose a colony's next project (the colony's star).</summary>
    public event Action<ColonyId>? ChooseProjectRequested;

    /// <summary>Raised when the player wants to choose the next research target.</summary>
    public event Action? ChooseResearchRequested;
    public event Action<StarId>? StarRequested;
    public event Action<ColonyId>? ColonyRequested;
    public event Action<VesselId>? VesselRequested;

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

    public void ShowReport(ContentSet content, EmpireTurnReportView report, EmpireView? empireView = null, OpenAntares.Game.Presentation.ColonyNotificationPreferences? preferences = null)
    {
        foreach (Node child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }

        _preferences = preferences ?? OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults;
        Title = "Turn report";
        _body.AddChild(new Label { Text = $"Turn {report.FromTurn} results", ThemeTypeVariation = "HeaderLabel" });

        if (empireView?.Development is not null) { var governors = new GovernorReportPanel { Preferences = preferences ?? OpenAntares.Game.Presentation.ColonyNotificationPreferences.Defaults }; _body.AddChild(governors); governors.ShowReports(content,empireView); }

        foreach (ColonyReportView colony in report.Colonies)
        {
            AddColony(content, colony);
        }

        EmpireEconomy empire = report.Research;
        _body.AddChild(new HSeparator());
        _body.AddChild(new Label { Text = "Research" });
        _body.AddChild(Subtle(ReserveSummary(empire.Research)));
        if (empire.Research.CompletedId is { } technology)
        {
            _body.AddChild(new Label { Text = $"Researched {content.Technologies[technology].Name}." });
            var choose = new Button { Text = "Choose next research" };
            choose.Pressed += () =>
            {
                Hide();
                ChooseResearchRequested?.Invoke();
            };
            _body.AddChild(choose);
        }

        foreach (var e in report.Events)
        {
            string location = e.Star is { } star && report.StarNames.TryGetValue(star, out var name) ? name : "system";
            string message = e.Kind switch { "arrived" => $"Vessel arrived at {location}.", "discovered" => $"Surveyed the planets of {location}.", "vessel_completed" => $"Expedition completed at {location}.", "colony_founded" => $"Colony founded at {location}.", "founding_failed" => $"Another expedition settled {location} first. Your settler and cargo were retained.", _ => "Expedition updated." };
            _body.AddChild(Wrapped(message, "SubtleLabel"));
            var view = new Button { Text = e.Colony.HasValue && e.Kind == "colony_founded" ? "View colony" : e.Vessel.HasValue && e.Kind == "vessel_completed" ? "View vessel" : "View system" };
            view.Pressed += () => { Hide(); if (e.Colony is { } colony && e.Kind == "colony_founded") ColonyRequested?.Invoke(colony); else if (e.Vessel is { } vessel && e.Kind == "vessel_completed") VesselRequested?.Invoke(vessel); else if (e.Star is { } destination) StarRequested?.Invoke(destination); }; _body.AddChild(view);
        }
        foreach (var encounter in report.EncounterEvents)
        {
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

    private void AddColony(ContentSet content, ColonyReportView view)
    {
        ColonyEconomy colony = view.Economy;
        _body.AddChild(new HSeparator());
        _body.AddChild(new Label { Text = $"Colony at {view.Name}" });
        _body.AddChild(Subtle("Production: " + ReserveSummary(colony.Production)));

        if (_preferences.LevelFor("completions") != OpenAntares.Game.Presentation.NotificationLevel.Off && colony.Production.CompletedId is { } building)
        {
            _body.AddChild(new Label { Text = $"Completed {(colony.Production.TargetKind == ProductionKinds.Vessel ? content.Vessels[building].Name : content.Buildings[building].Name)}." });
            var choose = new Button { Text = "Choose next project" };
            choose.Pressed += () =>
            {
                Hide();
                ChooseProjectRequested?.Invoke(view.Id);
            };
            _body.AddChild(choose);
        }

        if (colony.EmbarkOutcome is { } embark)
        {
            _body.AddChild(Wrapped($"Embarked workers: source {colony.PopulationUnitLabel} now {embark.PopulationAfter}; next support {Format.Points(embark.NextSupportProduced.Total)} / {Format.Points(embark.NextSupportRequired.Total)}.", embark.ReassignmentNeeded ? "WarningLabel" : "SubtleLabel"));
            if (embark.ReassignmentNeeded) _body.AddChild(Wrapped("Reassign workers before the next End Turn.", "WarningLabel"));
        }
        if (_preferences.LevelFor("embarkation_wait") != OpenAntares.Game.Presentation.NotificationLevel.Off && colony.Production.BlockedReasonCode is not null) _body.AddChild(Wrapped("Awaiting embarkation; production remains banked.", "WarningLabel"));
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
