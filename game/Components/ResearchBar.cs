using System;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Knowledge;

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

    public void ShowEmpire(ContentSet content, OwnResearchView empire)
    {
        _empire = empire.Id;
        _turn = empire.Turn;
        EmpireEconomy economy = empire.Economy;

        _picker.Clear();
        _picker.AddItem("No target");
        _picker.SetItemMetadata(0, string.Empty);
        var unknown = content.Technologies.Values.Where(t => !empire.KnownTechnologyIds.Contains(t.Id)).ToList();
        foreach (TechnologyDefinition technology in unknown.Where(t => t.PrerequisiteTechnologyIds.All(id => empire.KnownTechnologyIds.Contains(id))))
        {
            _picker.AddItem($"{technology.Name} ({Format.Points(technology.Cost)})");
            _picker.SetItemMetadata(_picker.ItemCount - 1, technology.Id);
            if (technology.Id == empire.TargetId)
            {
                _picker.Select(_picker.ItemCount - 1);
            }
        }

        foreach (TechnologyDefinition technology in unknown.Where(t => !t.PrerequisiteTechnologyIds.All(id => empire.KnownTechnologyIds.Contains(id))))
        {
            string needs = string.Join(", ", technology.PrerequisiteTechnologyIds
                .Where(id => !empire.KnownTechnologyIds.Contains(id))
                .Select(id => content.Technologies[id].Name));
            _picker.AddItem($"{technology.Name}: needs {needs}");
            _picker.SetItemMetadata(_picker.ItemCount - 1, technology.Id);
            _picker.SetItemDisabled(_picker.ItemCount - 1, true);
        }

        _output.Text = $"{Format.Points(economy.NetResearch.Total)} per turn";
        _output.TooltipText = BreakdownText.Describe(economy.NetResearch, content, "Research per turn", id => empire.ColonyNames.TryGetValue(id, out string? name) ? name : id);

        ReserveOutcome research = economy.Research;
        _progress.Text = research.TargetId is null
            ? $"No target: research is saved (reserve {Format.Points(research.Starting)})."
            : $"{BreakdownText.Progress(research)}, {BreakdownText.Estimate(research.Estimate)}";
    }

}
