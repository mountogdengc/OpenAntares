using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
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

    /// <summary>Raised when the player wants to choose a colony's next project (the colony's star).</summary>
    public event Action<StarId>? ChooseProjectRequested;

    /// <summary>Raised when the player wants to choose the next research target.</summary>
    public event Action? ChooseResearchRequested;

    public override void _Ready()
    {
        OkButtonText = "Continue";
        var padding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        padding.AddChild(_body);
        AddChild(padding);
    }

    public void ShowReport(ContentSet content, GameState state, TurnReport report, EmpireId viewer)
    {
        foreach (Node child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }

        Title = "Turn report";
        _body.AddChild(new Label { Text = $"Turn {report.FromTurn} results", ThemeTypeVariation = "HeaderLabel" });

        foreach (ColonyEconomy colony in report.Economy.Colonies.Where(c => c.Empire == viewer))
        {
            AddColony(content, state, colony);
        }

        EmpireEconomy empire = report.Economy.Empire(viewer);
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

        PopupCentered(new Vector2I(460, 0));
    }

    private void AddColony(ContentSet content, GameState state, ColonyEconomy colony)
    {
        PlanetState? planet = state.FindColony(colony.Colony) is { } current ? state.FindPlanet(current.PlanetId) : null;
        StarState? star = planet is null ? null : state.FindStar(planet.StarId);

        _body.AddChild(new HSeparator());
        _body.AddChild(new Label { Text = star is null ? "Colony" : $"Colony at {star.Name} {planet!.Orbit}" });
        _body.AddChild(Subtle("Production: " + ReserveSummary(colony.Production)));

        if (colony.Production.CompletedId is { } building && star is not null)
        {
            _body.AddChild(new Label { Text = $"Completed {content.Buildings[building].Name}." });
            var choose = new Button { Text = "Choose next project" };
            choose.Pressed += () =>
            {
                Hide();
                ChooseProjectRequested?.Invoke(star.Id);
            };
            _body.AddChild(choose);
        }

        GrowthOutcome growth = colony.Growth;
        if (growth.Births > 0)
        {
            _body.AddChild(new Label { Text = $"Population grew to {growth.PopulationAfter}. New workers start in support." });
        }

        if (growth.AtCapacity && growth.PopulationBefore < growth.Capacity)
        {
            _body.AddChild(Wrapped($"The colony reached its capacity of {growth.Capacity}.", "SubtleLabel"));
        }

        if (colony.SupportDeficit > 0)
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
