using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Saves;

namespace OpenAntares.Game.Validation;

internal static class PlayabilityColonyChecks
{
    public static async Task Run(Node host, GameScreen screen, GameSession game, string output, Action<bool, string> check)
    {
        var panel = Descendants<ColonyPanel>(screen).Single();
        int issued = 0;
        Action<Command> observe = _ => issued++;
        panel.CommandIssued += observe;
        try
        {
            string before = SaveGame.Serialize(game.Content, game.State);
            var owner = game.State.Empires.First(e => e.Controller == OpenAntares.Simulation.State.ControllerKind.Human).Id;
            var view = game.View(owner); var colony = view.Colonies.First();
            var open = Descendants<Button>(screen).Single(b => b.Name == "OpenColonyWorkspace");
            open.EmitSignal(Button.SignalName.Pressed);
            for (int i = 0; i < 4; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            var summary = Descendants<Label>(panel).SingleOrDefault(n => n.Name == "ColonyDecisionSummary");
            check(summary is not null, "ColonySummaryMatchesProjectedEconomyForEverySpecies: concise decision summary exists");
            string text = summary!.Text;
            check(text.Contains(view.Research.ColonyNames[colony.Id.ToString()], StringComparison.Ordinal), "Summary identifies the exact owned colony");
            check(text.Contains(colony.PopulationUnitLabel, StringComparison.OrdinalIgnoreCase), "Summary retains the species population vocabulary");
            check(text.Contains("Support " + Format.Points(colony.Economy.SupportProduced.Total), StringComparison.Ordinal), "Support summary matches authoritative production");
            check(text.Contains(Format.Points(colony.Economy.SupportRequired.Total), StringComparison.Ordinal), "Support summary includes authoritative requirement");
            check(text.Contains("Production " + Format.Points(colony.Economy.NetProduction.Total), StringComparison.Ordinal), "Production summary matches authoritative forecast");
            check(text.Contains("Research " + Format.Points(colony.Economy.NetResearch.Total), StringComparison.Ordinal), "Research summary matches authoritative forecast");
            check(issued == 0 && before == SaveGame.Serialize(game.Content, game.State), "Opening colony overview issues no command and changes no campaign/PRNG state");
            check(screen.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(output, $"colony-{colony.SpeciesId}-{host.GetTree().Root.Size.X}x{host.GetTree().Root.Size.Y}.png")) == Error.Ok, "Actual colony frame retained");
        }
        finally { panel.CommandIssued -= observe; }
    }

    public static async Task Knowledge(Node host, GameSession game, string output, Action<bool, string> check)
    {
        var owner = game.State.Empires.First(e => e.Controller == OpenAntares.Simulation.State.ControllerKind.Human).Id;
        var view = game.View(owner); string before = SaveGame.Serialize(game.Content, game.State);
        var panel = new StarPanel { Position = new(12, 12), Size = new(560, 500) };
        host.AddChild(panel);
        var unknown = view.Stars.First(s => s.Planets is null);
        panel.ShowStar(game.Content, unknown, view);
        check(Descendants<Control>(panel).Any(n => n.Name == "SystemOverview"), "UnknownWorldShowsNoFabricatedDetails: grouped system inspection exists");
        string text = string.Join("\n", Descendants<Label>(panel).Select(n => n.Text));
        check(text.Contains("survey", StringComparison.OrdinalIgnoreCase) || text.Contains("unknown", StringComparison.OrdinalIgnoreCase), "Unknown system explicitly explains its missing survey");
        check(!Descendants<PlanetPortrait>(panel).Any() && !Descendants<Button>(panel).Any(), "Unknown world data creates neither fabricated planet portraits nor colony actions");
        var home = view.Stars.Single(s => s.Chart.Id == view.Colonies[0].StarId);
        var foreign = home with
        {
            Current = false, LastObservedTurn = 0,
            Planets = home.Planets!.Value.Select(p => p with { OwnerId = new EmpireId(int.MaxValue), ColonyId = null }).ToImmutableArray()
        };
        var supplement = view with { Turn = 5, Colonies = ImmutableArray<OwnedColonyView>.Empty };
        panel.ShowStar(game.Content, foreign, supplement);
        text = string.Join("\n", Descendants<Label>(panel).Select(n => n.Text));
        check(text.Contains("Last", StringComparison.OrdinalIgnoreCase) && text.Contains("0", StringComparison.Ordinal), "Stale observed worlds retain their observation date");
        check(!Descendants<Button>(panel).Any(b => b.Text.Contains("Manage", StringComparison.OrdinalIgnoreCase)), "Foreign observation offers inspection, never owned-colony management");
        check(before == SaveGame.Serialize(game.Content, game.State), "Unknown/stale/foreign projection supplements preserve actual Main campaign and PRNG");
        for (int i = 0; i < 3; i++) await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
        host.RemoveChild(panel); panel.QueueFree();
    }

    // Labelled forecast-only supplements; never mounted as earned Main campaigns.
    public static async Task Shortage(Node host, GameSession actual, Action<bool,string> check)
    {
        string original=SaveGame.Serialize(actual.Content,actual.State);
        var game=new GameSession(actual.Content,actual.State.Clone());
        var owner=game.State.Empires.First(e=>e.Controller==OpenAntares.Simulation.State.ControllerKind.Human).Id;
        var initial=game.View(owner).Colonies[0];
        check(game.Execute(new SetWorkforceCommand(owner,game.State.Turn,initial.Id,new(0,initial.Population,0))).Accepted,"Labelled shortage supplement uses an ordinary legal workforce command on its own clone");
        var view=game.View(owner);var colony=view.Colonies.Single(c=>c.Id==initial.Id);
        check(colony.Economy.SupportDeficit>0,"Supplement actually has the authoritative support shortage");
        var panel=new ColonyPanel();host.AddChild(panel);panel.ShowColony(game.Content,colony,view);
        var text=Descendants<Label>(panel).Single(n=>n.Name=="ColonyDecisionSummary").Text;
        check(text.Contains("Support "+Format.Points(colony.Economy.SupportProduced.Total),StringComparison.Ordinal)&&text.Contains("Production "+Format.Points(colony.Economy.NetProduction.Total),StringComparison.Ordinal),"Shortage summary retains actual reduced forecast values");
        check(Descendants<Label>(panel).Any(l=>l.Text.Contains("Support shortage",StringComparison.Ordinal)),"Shortage keeps the existing consequence explanation visible");
        check(original==SaveGame.Serialize(actual.Content,actual.State),"Forecast supplement changes no actual Main campaign or PRNG");
        await host.ToSignal(host.GetTree(),SceneTree.SignalName.ProcessFrame);host.RemoveChild(panel);panel.QueueFree();
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T typed) yield return typed;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
