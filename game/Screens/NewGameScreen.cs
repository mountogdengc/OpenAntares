using System;
using System.Globalization;
using System.Linq;
using Godot;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Knowledge;
using OpenAntares.Simulation.Galaxy;
namespace OpenAntares.Game.Screens;

/// <summary>Species, galaxy and seed setup; all opening economic previews come from the simulation.</summary>
public partial class NewGameScreen : CenterContainer
{
    private readonly ContentSet _content;
    private readonly GalaxySizeDefinition[] _sizes;
    private readonly SpeciesDefinition[] _species;
    private readonly OptionButton _sizePicker = new();
    private readonly OptionButton _speciesPicker = new() { Name = "SpeciesPicker", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly OptionButton _rivalCountPicker = new() { Name = "RivalCountPicker", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly OptionButton _rivalSpeciesPicker = new() { Name = "RivalSpeciesPicker", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly LineEdit _seedField = new();
    private readonly Label _summary = new() { Name = "SpeciesSummary", ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Label _error = new() { ThemeTypeVariation = "WarningLabel", Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Button _start = new() { Text = "Start" };
    public event Action<NewGameSettings>? StartRequested;
    public event Action? LoadRequested;

    public NewGameScreen(ContentSet content)
    {
        _content = content;
        _sizes = content.GalaxySizes.Values.OrderBy(s => s.StarCount).ThenBy(s => s.Id, StringComparer.Ordinal).ToArray();
        _species = content.Species.Values.OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
    }
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(600, 0) }; AddChild(panel);
        var margin = new MarginContainer { ThemeTypeVariation = "PanelPadding" }; panel.AddChild(margin);
        var column = new VBoxContainer(); margin.AddChild(column);
        column.AddChild(new Label { Text = "OpenAntares", ThemeTypeVariation = "TitleLabel", HorizontalAlignment = HorizontalAlignment.Center });
        column.AddChild(new Label { Text = "New Game", ThemeTypeVariation = "HeaderLabel", HorizontalAlignment = HorizontalAlignment.Center });
        var form = new GridContainer { Columns = 2 }; column.AddChild(form);
        form.AddChild(new Label { Text = "Species" });
        foreach (var sp in _species) { _speciesPicker.AddItem(sp.Name); _speciesPicker.SetItemMetadata(_speciesPicker.ItemCount - 1, sp.Id); }
        int harl = Array.FindIndex(_species, s => s.Id == "harl");
        if (_species.Length > 0) _speciesPicker.Select(harl >= 0 ? harl : 0);
        else _speciesPicker.Disabled = true;
        form.AddChild(_speciesPicker);
        form.AddChild(new Label { Text = "Rivals" });
        _rivalCountPicker.AddItem("None (Solo)"); _rivalCountPicker.SetItemMetadata(0,"0");
        _rivalCountPicker.AddItem("One"); _rivalCountPicker.SetItemMetadata(1,"1"); _rivalCountPicker.Select(1); form.AddChild(_rivalCountPicker);
        form.AddChild(new Label { Text = "Rival species" });
        foreach (var sp in _species) { _rivalSpeciesPicker.AddItem(sp.Name); _rivalSpeciesPicker.SetItemMetadata(_rivalSpeciesPicker.ItemCount-1,sp.Id); }
        int rival = Array.FindIndex(_species,s=>s.Id=="peldra");
        if(rival<0) rival = Array.FindIndex(_species,s=>s.Id!=(_speciesPicker.Selected>=0?_species[_speciesPicker.Selected].Id:null));
        if(_species.Length>0) _rivalSpeciesPicker.Select(rival>=0?rival:0); else _rivalSpeciesPicker.Disabled=true;
        form.AddChild(_rivalSpeciesPicker);
        form.AddChild(new Label { Text = "Galaxy size" });
        foreach (var size in _sizes) _sizePicker.AddItem($"{size.Name} ({size.StarCount} stars)");
        _sizePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill; form.AddChild(_sizePicker);
        form.AddChild(new Label { Text = "Seed" });
        var seedRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _seedField.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _seedField.TooltipText = "The same seed, size and rival count reproduce galaxy facts across species.";
        seedRow.AddChild(_seedField); var random = new Button { Text = "Random" }; random.Pressed += RandomizeSeed; seedRow.AddChild(random); form.AddChild(seedRow);
        var details = new ScrollContainer { CustomMinimumSize = new Vector2(0, 140), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true, FocusMode = FocusModeEnum.All };
        column.AddChild(details); details.AddChild(_summary);
        column.AddChild(new Label { Text = "Race to explore and settle. Warfare and negotiation are not available yet.", ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        column.AddChild(_error); _start.Pressed += Start; column.AddChild(_start);
        var load = new Button { Text = "Load Game" }; load.Pressed += () => LoadRequested?.Invoke(); column.AddChild(load);
        _start.Disabled = _species.Length == 0 || _sizes.Length == 0 || _content.RivalAi is null;
        _rivalCountPicker.ItemSelected += _ => { _rivalSpeciesPicker.Disabled = _rivalCountPicker.Selected == 0 || _species.Length == 0; UpdateSummary(); };
        _rivalSpeciesPicker.ItemSelected += _ => UpdateSummary();
        _speciesPicker.ItemSelected += _ => UpdateSummary(); _sizePicker.ItemSelected += _ => UpdateSummary(); _seedField.TextChanged += _ => UpdateSummary();
        RandomizeSeed();
        if (_start.Disabled) { ShowError("Species, galaxy or rival policy definitions are missing. Load an existing campaign instead."); load.GrabFocus(); } else _start.GrabFocus();
    }
    public void Configure(string sizeId, ulong seed, string? speciesId = null, int? rivalCount = null, string? rivalSpeciesId = null)
    {
        int size = Array.FindIndex(_sizes, s => s.Id == sizeId); if (size >= 0) _sizePicker.Select(size);
        if (speciesId is not null) _speciesPicker.Select(Array.FindIndex(_species, s => s.Id == speciesId));
        if (rivalCount is not null) _rivalCountPicker.Select(rivalCount is 0 or 1 ? rivalCount.Value : -1);
        if (rivalSpeciesId is not null) _rivalSpeciesPicker.Select(Array.FindIndex(_species,s=>s.Id==rivalSpeciesId));
        _rivalSpeciesPicker.Disabled = _rivalCountPicker.Selected == 0 || _species.Length == 0;
        _seedField.Text = seed.ToString(CultureInfo.InvariantCulture); UpdateSummary();
    }
    public void Start()
    {
        if (_content.RivalAi is null) { ShowError("Rival policy definitions are missing. Load an existing campaign instead."); return; }
        if (_rivalCountPicker.Selected < 0 || (_rivalCountPicker.Selected == 1 && _rivalSpeciesPicker.Selected < 0)) { ShowError("Select a defined rival count and species."); return; }
        if (_sizes.Length == 0 || _sizePicker.Selected < 0) { ShowError("No galaxy sizes are defined in the content files."); return; }
        if (_species.Length == 0 || _speciesPicker.Selected < 0) { ShowError("Select a defined species before starting a new campaign."); return; }
        if (!ulong.TryParse(_seedField.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed)) { ShowError($"The seed must be a whole number from 0 to {ulong.MaxValue}."); return; }
        _error.Visible = false;
        StartRequested?.Invoke(new(_sizes[_sizePicker.Selected].Id, seed, Rules.CurrentVersion, _species[_speciesPicker.Selected].Id, _rivalCountPicker.Selected, _rivalCountPicker.Selected == 1 ? _species[_rivalSpeciesPicker.Selected].Id : null));
    }
    private void UpdateSummary()
    {
        if (_speciesPicker.Selected < 0 || _species.Length == 0 || _sizes.Length == 0 || _rivalCountPicker.Selected < 0 || (_rivalCountPicker.Selected == 1 && _rivalSpeciesPicker.Selected < 0)) { _summary.Text = "Load a saved campaign to continue with its original embedded rules."; return; }
        var sp = _species[_speciesPicker.Selected];
        if (!ulong.TryParse(_seedField.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed)) { _summary.Text = sp.Description; return; }
        var preview = GalaxyGenerator.CreateNewGame(_content, new(_sizes[_sizePicker.Selected].Id, seed, Rules.CurrentVersion, sp.Id, _rivalCountPicker.Selected, _rivalCountPicker.Selected == 1 ? _species[_rivalSpeciesPicker.Selected].Id : null));
        if (preview.State is null) { _summary.Text = sp.Description + "\n" + preview.Error; return; }
        var economy = EmpireViewProjector.Project(_content, preview.State, preview.State.Empires[0].Id).Colonies[0].Economy;
        var roles = sp.AllowedVesselIds.Select(id => _content.Vessels[id]).Select(role => role.CanFoundColony
            ? $"{role.Name}: {Format.Points(role.Cost)} production; cargo {role.EmbarkPopulation} {sp.PopulationUnitLabel}; minimum colony {role.MinimumSourcePopulation}." + (role.RequiresFullColony ? " Must be full at its current capacity." : string.Empty) + (role.FoundsSealedHabitat ? " Creates a sealed habitat." : " Creates a native colony.")
            : $"{role.Name}: {Format.Points(role.Cost)} production; surveys systems.");
        _summary.Text = $"{sp.Name} — {sp.CultureName}\n{sp.Description}\n\nStarting population: {sp.StartingPopulation} ({sp.PopulationUnitLabel}); capacity {economy.Growth.Capacity}." +
            (sp.SealedBaseCapacity is { } sealedCapacity ? $" Sealed capacity {sealedCapacity}." : string.Empty) +
            $"\nWorkers: {sp.StartingWorkforce.Support} support / {sp.StartingWorkforce.Production} production / {sp.StartingWorkforce.Research} research.\nSupport {Format.Points(economy.SupportProduced.Total)} / {Format.Points(economy.SupportRequired.Total)}; production {Format.Points(economy.NetProduction.Total)}, research {Format.Points(economy.NetResearch.Total)} per turn.\nGrowth +{Format.Points(economy.GrowthEarned.Total)} per turn; base +{Format.Points(sp.BaseGrowth)}, surplus bonus up to +{Format.Points(sp.MaxSurplusGrowth)}.\n\n" + string.Join("\n", roles);
        _summary.TooltipText = BreakdownText.Describe(economy.NetProduction, _content, "Production") + "\n\n" + BreakdownText.Describe(economy.GrowthEarned, _content, "Growth");
    }
    public void ShowError(string message) { _error.Text = message; _error.Visible = true; }
    private void RandomizeSeed() { _seedField.Text = (GD.Randi() % 1_000_000).ToString(CultureInfo.InvariantCulture); UpdateSummary(); }
}
