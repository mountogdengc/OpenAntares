using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenAntares.Game.Screens;
using OpenAntares.Simulation;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Game;

/// <summary>
/// Root of the entry scene. Loads content, then switches between screens. The interface is built in
/// code; styling comes from the project theme.
/// </summary>
/// <remarks>
/// Development launch options, passed after <c>--</c> on the command line:
/// <c>--new-game=SIZE:SEED</c> starts a game immediately, and <c>--screenshot=PATH</c> saves a PNG
/// of the window after a few frames and quits.
/// </remarks>
public partial class Main : Control
{
    private ContentSet? _content;
    private Control? _screen;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new Panel();
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        ContentLoadResult loaded = ContentFiles.LoadCore();
        if (loaded.Content is null)
        {
            ShowContentErrors(loaded);
            return;
        }

        _content = loaded.Content;
        NewGameScreen newGame = ShowNewGame();
        _ = RunDevelopmentOptions(newGame);
    }

    private NewGameScreen ShowNewGame()
    {
        var screen = new NewGameScreen(_content!);
        screen.StartRequested += settings => StartGame(screen, settings);
        SetScreen(screen);
        return screen;
    }

    private void StartGame(NewGameScreen from, NewGameSettings settings)
    {
        NewGameResult result = GalaxyGenerator.CreateNewGame(_content!, settings);
        if (result.State is null)
        {
            from.ShowError(result.Error ?? "The galaxy could not be generated.");
            return;
        }

        var session = new GameSession(_content!, result.State);
        EmpireId viewer = result.State.Empires.First(e => e.Controller == ControllerKind.Human).Id;
        SetScreen(new GameScreen(session, viewer));
    }

    private void SetScreen(Control screen)
    {
        if (_screen is not null)
        {
            RemoveChild(_screen);
            _screen.QueueFree();
        }

        _screen = screen;
        AddChild(screen);
    }

    private void ShowContentErrors(ContentLoadResult loaded)
    {
        var column = new VBoxContainer();
        column.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        column.AddChild(new Label { Text = "The game content could not be loaded.", ThemeTypeVariation = "HeaderLabel" });
        foreach (ContentError error in loaded.Errors)
        {
            column.AddChild(new Label
            {
                Text = error.ToString(),
                ThemeTypeVariation = "WarningLabel",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
        }

        SetScreen(column);
    }

    private async Task RunDevelopmentOptions(NewGameScreen newGame)
    {
        string[] args = OS.GetCmdlineUserArgs();
        string? Option(string name) => args
            .Where(arg => arg.StartsWith($"--{name}=", StringComparison.Ordinal))
            .Select(arg => arg[(name.Length + 3)..])
            .FirstOrDefault();

        if (Option("new-game") is { } newGameOption)
        {
            string[] parts = newGameOption.Split(':');
            ulong seed = parts.Length > 1 ? ulong.Parse(parts[1], CultureInfo.InvariantCulture) : 1;
            newGame.Configure(parts[0], seed);
            newGame.Start();
        }

        if (Option("screenshot") is { } path)
        {
            for (int frame = 0; frame < 10; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            GetViewport().GetTexture().GetImage().SavePng(path);
            GetTree().Quit();
        }
    }
}
