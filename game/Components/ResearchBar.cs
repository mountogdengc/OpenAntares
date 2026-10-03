using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Components;

/// <summary>The empire's research: target picker, output per turn with breakdown, and progress.</summary>
public partial class ResearchBar : HBoxContainer
{
    private readonly OptionButton _picker = new();
    private readonly Label _output = new() { MouseFilter = MouseFilterEnum.Stop };
    private readonly Label _progress = new() { ThemeTypeVariation = "SubtleLabel" };
    private EmpireId _empire;
    private int _turn;

    /// <summary>Raised when the player picks a target. The screen executes it and refreshes.</summary>
    public event Action<Command>? CommandIssued;

    public override void _Ready()
    {
        AddChild(new Label { Text = "Research" });
        AddChild(_picker);
        AddChild(_output);
        AddChild(_progress);
        _picker.ItemSelected += index =>
        {
            string id = _picker.GetItemMetadata((int)index).AsString();
            CommandIssued?.Invoke(new SetResearchTargetCommand(_empire, _turn, id.Length == 0 ? null : id));
        };
    }

    /// <summary>Opens the target list, e.g. after a technology completes.</summary>
    public void OpenPicker()
    {
        _picker.GrabFocus();
        _picker.ShowPopup();
    }

    public void ShowEmpire(ContentSet content, GameState state, EmpireId empireId, TurnEconomy forecast)
    {
        _empire = empireId;
        _turn = state.Turn;
        EmpireState empire = state.FindEmpire(empireId)!;
        EmpireEconomy economy = forecast.Empire(empireId);

        _picker.Clear();
        _picker.AddItem("No target");
        _picker.SetItemMetadata(0, string.Empty);
        var unknown = content.Technologies.Values.Where(t => !empire.KnownTechnologyIds.Contains(t.Id)).ToList();
        foreach (TechnologyDefinition technology in unknown.Where(t => Rules.PrerequisitesKnown(empire, t.PrerequisiteTechnologyIds)))
        {
            _picker.AddItem($"{technology.Name} ({Format.Points(technology.Cost)})");
            _picker.SetItemMetadata(_picker.ItemCount - 1, technology.Id);
            if (technology.Id == empire.ResearchTargetId)
            {
                _picker.Select(_picker.ItemCount - 1);
            }
        }

        foreach (TechnologyDefinition technology in unknown.Where(t => !Rules.PrerequisitesKnown(empire, t.PrerequisiteTechnologyIds)))
        {
            string needs = string.Join(", ", technology.PrerequisiteTechnologyIds
                .Where(id => !empire.KnownTechnologyIds.Contains(id))
                .Select(id => content.Technologies[id].Name));
            _picker.AddItem($"{technology.Name}: needs {needs}");
            _picker.SetItemMetadata(_picker.ItemCount - 1, technology.Id);
            _picker.SetItemDisabled(_picker.ItemCount - 1, true);
        }

        _output.Text = $"{Format.Points(economy.NetResearch.Total)} per turn";
        _output.TooltipText = BreakdownText.Describe(economy.NetResearch, content, "Research per turn", id => ColonyName(state, id));

        ReserveOutcome research = economy.Research;
        _progress.Text = research.TargetId is null
            ? $"No target: research is saved (reserve {Format.Points(research.Starting)})."
            : $"{BreakdownText.Progress(research)}, {BreakdownText.Estimate(research.Estimate)}";
    }

    /// <summary>Names a colony by its star and orbit, from the ID text used in breakdown lines.</summary>
    private static string ColonyName(GameState state, string colonyIdText)
    {
        ColonyState? colony = state.Colonies.FirstOrDefault(c => c.Id.ToString() == colonyIdText);
        PlanetState? planet = colony is null ? null : state.FindPlanet(colony.PlanetId);
        StarState? star = planet is null ? null : state.FindStar(planet.StarId);
        return star is null ? colonyIdText : $"{star.Name} {planet!.Orbit}";
    }
}
