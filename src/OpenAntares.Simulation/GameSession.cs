using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Economy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation;

/// <summary>
/// Holds the published content and state of one running game. Presentation reads from it and sends
/// commands through it; it never changes state directly. Every change goes through
/// <see cref="CommandProcessor"/>, and a failed load leaves the session untouched.
/// </summary>
public sealed class GameSession
{
    public GameSession(ContentSet content, GameState state)
    {
        Content = content;
        State = state;
    }

    public ContentSet Content { get; private set; }
    public GameState State { get; private set; }

    public CommandOutcome Execute(Command command)
    {
        CommandOutcome outcome = CommandProcessor.Execute(Content, State, command);
        State = outcome.State;
        return outcome;
    }

    /// <summary>What resolving the turn now would do, with breakdowns. Changes nothing.</summary>
    public TurnEconomy Forecast() => EconomyCalculator.Calculate(Content, State);

    public void Save(string path) => SaveGame.WriteFile(path, Content, State);

    /// <summary>Replaces the session's game with a saved one, only if the save loads successfully.</summary>
    public SaveLoadResult Load(string path)
    {
        SaveLoadResult result = SaveGame.ReadFile(path);
        if (result.Game is { } game)
        {
            Content = game.Content;
            State = game.State;
        }

        return result;
    }
}
