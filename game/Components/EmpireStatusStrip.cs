using System.Linq;
using Godot;
using OpenAntares.Simulation.Knowledge;

namespace OpenAntares.Game.Components;

/// <summary>Planning readouts from the published empire view, never an independent forecast.</summary>
public partial class EmpireStatusStrip : PanelContainer
{
    private readonly HBoxContainer _row = new();
    private readonly Label _date = new() { ThemeTypeVariation = "StencilLabel" };
    private readonly Label[] _values = new Label[3];
    private readonly Label[] _deltas = new Label[3];
    private readonly Label[] _headings = new Label[3];
    private readonly ProgressBar _research = new() { ShowPercentage = false, SizeFlagsHorizontal = SizeFlags.ExpandFill };
    public override void _Ready()
    {
        Name = "EmpireStatusStrip"; ThemeTypeVariation = "StatusStrip";
        AddChild(_row); _row.AddChild(_date);
        string[] labels = { "PRODUCTION", "RESEARCH", "POPULATION" };
        string[] variations = { "ResourceProduction", "ResourceResearch", "ResourcePopulation" };
        for (int i = 0; i < labels.Length; i++)
        {
            var cell = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _row.AddChild(cell);
            _headings[i] = new Label { Text = labels[i], ThemeTypeVariation = "StencilLabel" };
            var heading = new HBoxContainer(); cell.AddChild(heading);
            var icon = new TextureRect { Texture = GetThemeIcon(labels[i].ToLowerInvariant(), "Icons"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
            int iconSize = GetThemeConstant("icon_size", "ConsoleMetrics"); icon.CustomMinimumSize = new Vector2(iconSize,iconSize);
            heading.AddChild(icon); heading.AddChild(_headings[i]);
            _values[i] = new Label { Name = labels[i] + "Readout", ThemeTypeVariation = variations[i], MouseFilter = MouseFilterEnum.Stop };
            var numbers = new HBoxContainer(); cell.AddChild(numbers);
            numbers.AddChild(_values[i]);
            _deltas[i] = new Label { Name = labels[i] + "Delta", ThemeTypeVariation = "DeltaPositive" };
            numbers.AddChild(_deltas[i]);
            if (i == 1) cell.AddChild(_research);
        }
    }
    public void ShowEmpire(EmpireView view, OpenAntares.Simulation.Content.ContentSet content)
    {
        _date.Text = (view.SpeciesId is { } species && content.Species.TryGetValue(species,out var identity) ? identity.Name.ToUpperInvariant() : "YOUR EMPIRE") + "\nTURN " + view.Turn;
        static string Signed(long value, bool points) => (value > 0 ? "+" : "") + (points ? Format.Points(value) : value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        long production = view.Colonies.Sum(c => c.ProductionReserve);
        long productionChange = view.Colonies.Sum(c => c.Economy.Production.Change.Total);
        long population = view.Colonies.Sum(c => (long)c.Population);
        long growth = view.Colonies.Sum(c => (long)(c.Economy.EmbarkOutcome?.PopulationAfter ?? c.Economy.Growth.PopulationAfter) - c.Population);
        _values[0].Text = Format.Points(production);
        _values[0].TooltipText = "Sum of local colony production reserves. Parentheses show next-turn reserve change after planned spending. Production remains local to each colony.\n" + string.Join("\n",view.Colonies.Select(c=>BreakdownText.Describe(c.Economy.Production.Change,content,view.Research.ColonyNames.TryGetValue(c.Id.ToString(),out string? name)?name:"Colony "+c.Id.Value)));
        _values[1].Text = Format.Points(view.Research.Reserve);
        _values[1].TooltipText = "Saved research. Parentheses show next-turn reserve change after planned research spending and cure allocations.";
        _values[2].Text = population.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _values[2].TooltipText = $"Total {view.PopulationUnitLabel}. Parentheses show the projected change in your current colonies, including colonists embarking on completed ships. New colonies are counted when founded. Growth points are not population.";
        long[] changes = { productionChange, view.Research.Economy.Research.Change.Total, growth };
        for (int i = 0; i < changes.Length; i++)
        {
            _deltas[i].Text = "(" + Signed(changes[i], i < 2) + ")";
            _deltas[i].ThemeTypeVariation = changes[i] < 0 ? "DeltaNegative" : "DeltaPositive";
        }
        _values[1].TooltipText += "\n" + BreakdownText.Describe(view.Research.Economy.Research.Change, content, "Next-turn research change", id => view.Research.ColonyNames.TryGetValue(id, out string? name) ? name : id);
        var forecast = view.Research.Economy.Research;
        _research.Visible = forecast.TargetCost is > 0;
        _research.Value = forecast.TargetCost is > 0 ? System.Math.Clamp(100.0 * view.Research.Reserve / forecast.TargetCost.Value, 0, 100) : 0;
        _research.TooltipText = forecast.TargetId is null ? "Research is being saved" : (content.Technologies.TryGetValue(forecast.TargetId,out var technology)?technology.Name:forecast.TargetId)+" · "+BreakdownText.Estimate(forecast.Estimate);
    }
}
