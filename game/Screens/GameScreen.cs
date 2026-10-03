using System.Linq;
using Godot;
using OpenAntares.Game.Components;
using OpenAntares.Simulation;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game.Screens;

/// <summary>The main in-game screen: a top bar, the galaxy map, and a panel for the selected star.</summary>
public partial class GameScreen : VBoxContainer
{
    private readonly GameSession _session;
    private readonly EmpireId _viewer;
    private readonly Label _turnLabel = new();
    private readonly GalaxyMap _map = new();
    private readonly StarPanel _starPanel = new();

    /// <param name="viewer">The empire this screen is shown to. Presentation-only; the simulation has no "current player".</param>
    public GameScreen(GameSession session, EmpireId viewer)
    {
        _session = session;
        _viewer = viewer;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var topBar = new PanelContainer();
        AddChild(topBar);
        var topPadding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        topBar.AddChild(topPadding);
        var topRow = new HBoxContainer();
        topPadding.AddChild(topRow);
        topRow.AddChild(_turnLabel);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(body);

        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.StarSelected += ShowStar;
        body.AddChild(_map);

        var side = new PanelContainer { CustomMinimumSize = new Vector2(340, 0) };
        body.AddChild(side);
        var sidePadding = new MarginContainer { ThemeTypeVariation = "PanelPadding" };
        side.AddChild(sidePadding);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        sidePadding.AddChild(scroll);
        _starPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_starPanel);

        Refresh();

        if (HomeStar() is { } home)
        {
            _map.Select(home);
        }
    }

    private void Refresh()
    {
        GameState state = _session.State;
        _turnLabel.Text = $"Turn {state.Turn}";
        _map.ShowGalaxy(state.Stars, HomeStar() is { } home ? new[] { home } : System.Array.Empty<StarId>());
        if (_map.Selected is { } selected)
        {
            ShowStar(selected);
        }
    }

    private void ShowStar(StarId star) => _starPanel.ShowStar(_session.Content, _session.State, star, _viewer);

    /// <summary>The star of the viewer's first colony.</summary>
    private StarId? HomeStar()
    {
        GameState state = _session.State;
        ColonyState? colony = state.Colonies.FirstOrDefault(c => c.EmpireId == _viewer);
        return colony is null ? null : state.FindPlanet(colony.PlanetId)?.StarId;
    }
}
