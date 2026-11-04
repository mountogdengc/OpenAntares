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
    private readonly OptionButton _crisisPicker = new() { Name = "CrisisModePicker", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly OptionButton _amebaPicker = new() { Name="AmebaModePicker",SizeFlagsHorizontal=SizeFlags.ExpandFill };
    private readonly OptionButton _sentriesPicker=new(){Name="SentriesModePicker",SizeFlagsHorizontal=SizeFlags.ExpandFill};
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
        var panel = new PanelContainer(); AddChild(panel);
        var viewport = GetViewport();
        void FitSetupPanel() { var size = viewport.GetVisibleRect().Size; panel.CustomMinimumSize = new Vector2(Math.Min(600, size.X), Math.Min(720, size.Y)); }
        viewport.SizeChanged += FitSetupPanel;
        TreeExiting += () => viewport.SizeChanged -= FitSetupPanel;
        FitSetupPanel();
        var margin = new MarginContainer { ThemeTypeVariation = "PanelPadding" }; panel.AddChild(margin);
        var column = new VBoxContainer(); margin.AddChild(column);
        column.AddChild(new Label { Text = "OpenAntares", ThemeTypeVariation = "TitleLabel", HorizontalAlignment = HorizontalAlignment.Center });
        column.AddChild(new Label { Text = "New Game", ThemeTypeVariation = "HeaderLabel", HorizontalAlignment = HorizontalAlignment.Center });
        var setupScroll = new ScrollContainer { Name = "SetupFormScroll", SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        column.AddChild(setupScroll);
        var formColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        setupScroll.AddChild(formColumn);
        var form = new GridContainer { Columns = 2 }; formColumn.AddChild(form);
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
        int rival = harl >= 0 ? Array.FindIndex(_species,s=>s.Id=="peldra") : -1;
        if(rival<0) rival = Array.FindIndex(_species,s=>s.Id!=(_speciesPicker.Selected>=0?_species[_speciesPicker.Selected].Id:null));
        if(_species.Length>0) _rivalSpeciesPicker.Select(rival>=0?rival:0); else _rivalSpeciesPicker.Disabled=true;
        form.AddChild(_rivalSpeciesPicker);
        form.AddChild(new Label { Text = "Colony crises" });
        _crisisPicker.AddItem("Standard");_crisisPicker.SetItemMetadata(0,true);
        _crisisPicker.AddItem("Off");_crisisPicker.SetItemMetadata(1,false);_crisisPicker.Select(0);form.AddChild(_crisisPicker);
        if(Rules.HasScouting(Rules.CurrentVersion)){form.AddChild(new Label{Text="Space ameba"});_amebaPicker.AddItem("Standard");_amebaPicker.SetItemMetadata(0,true);_amebaPicker.AddItem("Off");_amebaPicker.SetItemMetadata(1,false);_amebaPicker.Select(0);form.AddChild(_amebaPicker);_amebaPicker.ItemSelected+=_=>UpdateSummary();}
        else{_amebaPicker.Hide();column.AddChild(_amebaPicker);}
        if(Rules.HasCombat(Rules.CurrentVersion)){form.AddChild(new Label{Text="Sentries"});_sentriesPicker.AddItem("Standard");_sentriesPicker.SetItemMetadata(0,true);_sentriesPicker.AddItem("Off");_sentriesPicker.SetItemMetadata(1,false);_sentriesPicker.Select(0);form.AddChild(_sentriesPicker);_sentriesPicker.ItemSelected+=_=>UpdateSummary();}
        else{_sentriesPicker.Hide();column.AddChild(_sentriesPicker);}
        form.AddChild(new Label { Text = "Galaxy size" });
        foreach (var size in _sizes) _sizePicker.AddItem($"{size.Name} ({size.StarCount} stars)");
        _sizePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill; form.AddChild(_sizePicker);
        form.AddChild(new Label { Text = "Seed" });
        var seedRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _seedField.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _seedField.TooltipText = "The same seed, size and rival count reproduce galaxy facts across species.";
        seedRow.AddChild(_seedField); var random = new Button { Text = "Random" }; random.Pressed += RandomizeSeed; seedRow.AddChild(random); form.AddChild(seedRow);
        var details = new ScrollContainer { CustomMinimumSize = new Vector2(0, 140), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true, FocusMode = FocusModeEnum.All };
        formColumn.AddChild(details); details.AddChild(_summary);
        formColumn.AddChild(new Label { Text = "Explore, settle and negotiate with rivals who can declare limited war. Optional sentry battles can clear one guarded system for ordinary founding.", ThemeTypeVariation = "SubtleLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        column.AddChild(_error); _start.Pressed += Start; column.AddChild(_start);
        var load = new Button { Text = "Load Game" }; load.Pressed += () => LoadRequested?.Invoke(); column.AddChild(load);
        _start.Disabled = _species.Length == 0 || _sizes.Length == 0 || _content.RivalAi is null || _content.GovernorTemplates.IsEmpty || _content.Plagues.Count!=1 || Rules.HasShipDesigns(Rules.CurrentVersion)&&_content.ShipDesigns is null || Rules.HasScouting(Rules.CurrentVersion)&&_content.SpaceAmeba is null||Rules.HasCombat(Rules.CurrentVersion)&&_content.Combat is null||Rules.HasConflict(Rules.CurrentVersion)&&_content.Conflict is null;
        _rivalCountPicker.ItemSelected += _ => { _rivalSpeciesPicker.Disabled = _rivalCountPicker.Selected == 0 || _species.Length == 0; UpdateSummary(); };
        _rivalSpeciesPicker.ItemSelected += _ => UpdateSummary();
        _speciesPicker.ItemSelected += _ => UpdateSummary(); _sizePicker.ItemSelected += _ => UpdateSummary(); _crisisPicker.ItemSelected += _ => UpdateSummary(); _seedField.TextChanged += _ => UpdateSummary();
        RandomizeSeed();
        if (_start.Disabled) { ShowError("Species, galaxy, rival policy, colony template, plague, space ameba or combat definitions are missing. Load an existing campaign instead."); load.GrabFocus(); } else _start.GrabFocus();
    }
    public void Configure(string sizeId, ulong seed, string? speciesId = null, int? rivalCount = null, string? rivalSpeciesId = null, bool? crisesEnabled = null,bool? amebasEnabled=null,bool? sentriesEnabled=null)
    {
        int size = Array.FindIndex(_sizes, s => s.Id == sizeId); if (size >= 0) _sizePicker.Select(size);
        if (speciesId is not null) _speciesPicker.Select(Array.FindIndex(_species, s => s.Id == speciesId));
        if (rivalCount is not null) _rivalCountPicker.Select(rivalCount is 0 or 1 ? rivalCount.Value : -1);
        if (rivalSpeciesId is not null) _rivalSpeciesPicker.Select(Array.FindIndex(_species,s=>s.Id==rivalSpeciesId));
        if (crisesEnabled is not null) _crisisPicker.Select(crisesEnabled.Value?0:1);
        if(amebasEnabled is not null&&_amebaPicker.ItemCount>0)_amebaPicker.Select(amebasEnabled.Value?0:1);
        if(sentriesEnabled is not null&&_sentriesPicker.ItemCount>0)_sentriesPicker.Select(sentriesEnabled.Value?0:1);
        _rivalSpeciesPicker.Disabled = _rivalCountPicker.Selected == 0 || _species.Length == 0;
        _seedField.Text = seed.ToString(CultureInfo.InvariantCulture); UpdateSummary();
    }
    public void Start()
    {
        if(Rules.HasConflict(Rules.CurrentVersion)&&_content.Conflict is null){ShowError("Conflict definitions are required for current campaigns, including Solo. Load an existing campaign instead.");return;}
        if(Rules.HasShipDesigns(Rules.CurrentVersion)&&_content.ShipDesigns is null){ShowError("Ship-design definitions are required for current campaigns, including Sentries Off. Load an existing campaign instead.");return;}
        if(Rules.HasCombat(Rules.CurrentVersion)&&_content.Combat is null){ShowError("Combat definitions are required for current campaigns, including Sentries Off. Load an existing campaign instead.");return;}
        if(Rules.HasScouting(Rules.CurrentVersion)&&_content.SpaceAmeba is null){ShowError("Space ameba definitions are required for current campaigns, including Off. Load an existing campaign instead.");return;}
        if (_content.GovernorTemplates.IsEmpty) { ShowError("Colony template definitions are missing. Load an existing campaign instead."); return; }
        if (_content.RivalAi is null) { ShowError("Rival policy definitions are missing. Load an existing campaign instead."); return; }
        if (_content.Plagues.Count!=1) { ShowError("One valid plague family is required for new campaigns, including Off. Load an existing campaign instead."); return; }
        if (_rivalCountPicker.Selected < 0 || (_rivalCountPicker.Selected == 1 && _rivalSpeciesPicker.Selected < 0)) { ShowError("Select a defined rival count and species."); return; }
        if (_sizes.Length == 0 || _sizePicker.Selected < 0) { ShowError("No galaxy sizes are defined in the content files."); return; }
        if (_species.Length == 0 || _speciesPicker.Selected < 0) { ShowError("Select a defined species before starting a new campaign."); return; }
        if (!ulong.TryParse(_seedField.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed)) { ShowError($"The seed must be a whole number from 0 to {ulong.MaxValue}."); return; }
        _error.Visible = false;
        StartRequested?.Invoke(new(_sizes[_sizePicker.Selected].Id, seed, Rules.CurrentVersion, _species[_speciesPicker.Selected].Id, _rivalCountPicker.Selected, _rivalCountPicker.Selected == 1 ? _species[_rivalSpeciesPicker.Selected].Id : null,_crisisPicker.Selected==0,Rules.HasScouting(Rules.CurrentVersion)?_amebaPicker.Selected==0:(bool?)null,Rules.HasCombat(Rules.CurrentVersion)?_sentriesPicker.Selected==0:(bool?)null));
    }
    private void UpdateSummary()
    {
        if (_speciesPicker.Selected < 0 || _species.Length == 0 || _sizes.Length == 0 || _rivalCountPicker.Selected < 0 || (_rivalCountPicker.Selected == 1 && _rivalSpeciesPicker.Selected < 0)) { _summary.Text = "Load a saved campaign to continue with its original embedded rules."; return; }
        var sp = _species[_speciesPicker.Selected];
        if (!ulong.TryParse(_seedField.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed)) { _summary.Text = sp.Description; return; }
        var preview = GalaxyGenerator.CreateNewGame(_content, new(_sizes[_sizePicker.Selected].Id, seed, Rules.CurrentVersion, sp.Id, _rivalCountPicker.Selected, _rivalCountPicker.Selected == 1 ? _species[_rivalSpeciesPicker.Selected].Id : null,_crisisPicker.Selected==0,Rules.HasScouting(Rules.CurrentVersion)?_amebaPicker.Selected==0:(bool?)null,Rules.HasCombat(Rules.CurrentVersion)?_sentriesPicker.Selected==0:(bool?)null));
        if (preview.State is null) { _summary.Text = sp.Description + "\n" + preview.Error; return; }
        var economy = EmpireViewProjector.Project(_content, preview.State, preview.State.Empires[0].Id).Colonies[0].Economy;
        var roles = sp.AllowedVesselIds.Concat(Rules.HasCombat(Rules.CurrentVersion)?_content.Vessels.Values.Where(v=>v.CombatOnly).Select(v=>v.Id):Array.Empty<string>()).Distinct(StringComparer.Ordinal).Select(id => _content.Vessels[id]).Select(role => role.CanFoundColony
            ? $"{role.Name}: {Format.Points(role.Cost)} production; cargo {role.EmbarkPopulation} {sp.PopulationUnitLabel}; minimum colony {role.MinimumSourcePopulation}." + (role.RequiresFullColony ? " Must be full at its current capacity." : string.Empty) + (role.FoundsSealedHabitat ? " Creates a sealed habitat." : " Creates a native colony.")
            : Rules.HasCombat(Rules.CurrentVersion)&&_content.Combat!.RoleProfiles[role.Id] is{Attack:>0} profile?$"{role.Name}: {Format.Points(role.Cost)} production; hull {profile.MaxHull}, attack {profile.Attack}, armor {profile.Armor}, speed {role.Speed}; no cargo.":$"{role.Name}: {Format.Points(role.Cost)} production; surveys systems.");
        _summary.Text = $"{sp.Name} — {sp.CultureName}\n{sp.Description}\n\nColony crises: {(_crisisPicker.Selected==0?"Standard":"Off")}. Standard includes plague warnings and response choices; Off keeps crises dormant.\n\nStarting population: {sp.StartingPopulation} ({sp.PopulationUnitLabel}); capacity {economy.Growth.Capacity}." +
            (sp.SealedBaseCapacity is { } sealedCapacity ? $" Sealed capacity {sealedCapacity}." : string.Empty) +
            $"\nWorkers: {sp.StartingWorkforce.Support} support / {sp.StartingWorkforce.Production} production / {sp.StartingWorkforce.Research} research.\nSupport {Format.Points(economy.SupportProduced.Total)} / {Format.Points(economy.SupportRequired.Total)}; production {Format.Points(economy.NetProduction.Total)}, research {Format.Points(economy.NetResearch.Total)} per turn.\nGrowth +{Format.Points(economy.GrowthEarned.Total)} per turn; base +{Format.Points(sp.BaseGrowth)}, surplus bonus up to +{Format.Points(sp.MaxSurplusGrowth)}.\n\n" + string.Join("\n", roles);
        _summary.TooltipText = BreakdownText.Describe(economy.NetProduction, _content, "Production") + "\n\n" + BreakdownText.Describe(economy.GrowthEarned, _content, "Growth");
        if(Rules.HasScouting(Rules.CurrentVersion))_summary.Text+=$"\n\nSpace ameba: {(_amebaPicker.Selected==0?"Standard":"Off")}. Standard may place a stationary danger; arrival gives one safe departure opportunity. Off keeps ameba encounters dormant. Scouts start with neutral Observe and enabled ameba withdrawal; Avoid encounters is a separate opt-in. Study and remaining at risk require an explicit response.";
        if(Rules.HasConflict(Rules.CurrentVersion))_summary.Text+="\n\nEmpire conflict: contacted rivals can declare limited war. Armed fleets can fight and blockade production; civilians remain protected. Peace ends fighting and starts a ten-turn truce. Solo has no opponent.";
        if(Rules.HasDiplomacy(Rules.CurrentVersion))_summary.Text+="\n\nDiplomacy: explore to meet civilizations. Settlement accords freeze further founding in one named system. Trade costs5 production per side and pays1 per turn after3 warmup turns. Agreements last at least10 effective turns, then2 turns notice. No diplomatic partner in Solo.";
        if(Rules.HasShipDesigns(Rules.CurrentVersion))_summary.Text+="\n\nShip designs: Standard Escort is the permanent starting recipe. Save your own designs in Fleet Command, earn component research, and pay for construction or same-frame refits at an owned port. Existing ships keep their equipment until a paid refit completes.";
        if(Rules.HasCombat(Rules.CurrentVersion))_summary.Text+=$"\n\nSentries: {(_sentriesPicker.Selected==0?"Standard":"Off")}. Standard places one pocket of two automated sentries, blocking founding until cleared. Ordinary passage and survey stay safe; battles require a separate fleet attack order. Off keeps warship construction and repair available.";
        if(preview.State.Scouting is {Enabled:true,Habitat:null})_summary.Text+="\nThis setup will not place a space ameba: there is not enough eligible non-home habitat.";
    }
    public void ShowError(string message) { _error.Text = message; _error.Visible = true; }
    private void RandomizeSeed() { _seedField.Text = (GD.Randi() % 1_000_000).ToString(CultureInfo.InvariantCulture); UpdateSummary(); }
}
