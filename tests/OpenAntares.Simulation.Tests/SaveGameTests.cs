using System;
using System.IO;
using System.Text.Json;
using OpenAntares.Simulation.Commands;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;
using Xunit;
using static OpenAntares.Simulation.Tests.Fixtures;

namespace OpenAntares.Simulation.Tests;

public class SaveGameTests : IDisposable
{
    private readonly ContentSet _content = PrototypeContent();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "openantares-tests-" + Path.GetRandomFileName());

    public SaveGameTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void RoundTripPreservesStateAndContent()
    {
        GameState state = PlayedState();

        LoadedGame loaded = Load(SaveGame.Serialize(_content, state));

        Assert.Equal(Snapshot(state), Snapshot(loaded.State));
        Assert.Equal(JsonSerializer.Serialize(_content), JsonSerializer.Serialize(loaded.Content));
    }

    [Fact]
    public void RoundTripPreservesReadinessAndPlanningChoices()
    {
        GameState state = StartingState();
        EmpireId second = AddEmpire(state, ControllerKind.Human);
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetReadyCommand(Empire, 0, true));

        LoadedGame loaded = Load(SaveGame.Serialize(_content, state));

        Assert.Equal(0, loaded.State.Turn);
        Assert.True(loaded.State.FindEmpire(Empire)!.Ready);
        Assert.False(loaded.State.FindEmpire(second)!.Ready);
        Assert.Equal("fabrication_hall", loaded.State.FindColony(HomeColony)!.ProjectId);
    }

    [Fact]
    public void SerializationIsStable()
    {
        string first = SaveGame.Serialize(_content, PlayedState());
        LoadedGame loaded = Load(first);

        Assert.Equal(first, SaveGame.Serialize(loaded.Content, loaded.State));
    }

    [Fact]
    public void ContinuingAfterReloadMatchesUninterruptedPlay()
    {
        GameState uninterrupted = PlayTurns(StartingState(), _content, 10);

        GameState firstHalf = PlayTurns(StartingState(), _content, 5);
        LoadedGame reloaded = Load(SaveGame.Serialize(_content, firstHalf));
        GameState continued = PlayTurns(reloaded.State, reloaded.Content, 5);

        Assert.Equal(Snapshot(uninterrupted), Snapshot(continued));
    }

    [Fact]
    public void RandomGeneratorStateIsSaved()
    {
        GameState state = StartingState();
        state.Random.NextUInt32();
        state.Random.NextUInt32();

        LoadedGame loaded = Load(SaveGame.Serialize(_content, state));

        Assert.Equal(state.Random.NextUInt32(), loaded.State.Random.NextUInt32());
    }

    [Fact]
    public void SaveUsesItsEmbeddedContentNotTheCurrentFiles()
    {
        var cheaperHall = _content.Buildings["fabrication_hall"] with { Cost = 300 };
        ContentSet modded = _content with { Buildings = _content.Buildings.SetItem(cheaperHall.Id, cheaperHall) };

        LoadedGame loaded = Load(SaveGame.Serialize(modded, StartingState()));

        Assert.Equal(300, loaded.Content.Buildings["fabrication_hall"].Cost);
    }

    [Fact]
    public void RefusesToSaveAnInvalidState()
    {
        GameState state = StartingState();
        state.Colonies[0].Population = 0;

        Assert.Throws<ArgumentException>(() => SaveGame.Serialize(_content, state));
    }

    public static TheoryData<string, string, string> CorruptSaves => new()
    {
        // find (last occurrence, so state fields win over embedded content), replace, expected error fragment
        { "\"format\": \"openantares-save\"", "\"format\": \"something-else\"", "not an OpenAntares save" },
        { "\"schema_version\": 1", "\"schema_version\": 2", "schema version 2; this build reads version 1" },
        { "\"rules_version\": 1", "\"rules_version\": 7", "rules version 7; this build implements version 1" },
        { "\"turn\": 2,", "", "field 'state.turn': Required field is missing" },
        { "\"controller\": \"human\"", "\"controller\": \"robot\"", "field 'state.empires[0].controller': Unknown controller 'robot'" },
        { "\"ready\": false", "\"ready\": \"no\"", "field 'state.empires[0].ready': Expected true or false" },
        { "\"growth_progress\"", "\"growth\": 0, \"growth_progress\"", "field 'state.colonies[0].growth': Unknown field" },
        { "\"cost\": 1200", "\"cost\": 0", "save content, entry 'measurement_methods', field 'cost'" },
        { "\"population\": 6,\r\n", "\"population\": 60,\r\n", "save state: colony:3: population 60 exceeds capacity 10" },
        { "\"population\": 6,\n", "\"population\": 60,\n", "save state: colony:3: population 60 exceeds capacity 10" },
        { "\"planet_type\": \"verdant_world\"\n", "\"planet_type\": \"gas_giant\"\n", "save state: planet:2: planet type 'gas_giant' is not defined" },
        { "\"planet_type\": \"verdant_world\"\r\n", "\"planet_type\": \"gas_giant\"\r\n", "save state: planet:2: planet type 'gas_giant' is not defined" },
        { "\"ready\": false", "\"ready\": true", "Every human-controlled empire is ready" },
    };

    [Theory]
    [MemberData(nameof(CorruptSaves))]
    public void CorruptOrIncompatibleSaveIsRejectedWithAClearError(string find, string replace, string expected)
    {
        GameState state = PlayTurns(StartingState(), _content, 2);
        string text = SaveGame.Serialize(_content, state);
        if (!text.Contains(find, StringComparison.Ordinal))
        {
            return; // Line-ending variant that doesn't apply on this platform.
        }

        SaveLoadResult result = SaveGame.Deserialize(ReplaceLast(text, find, replace));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ not json")]
    [InlineData("[]")]
    public void NonSaveTextIsRejected(string text)
    {
        SaveLoadResult result = SaveGame.Deserialize(text);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    // --- Files ---

    [Fact]
    public void WritesAndReadsFilesWithoutLeavingTemporaryFiles()
    {
        string path = Path.Combine(_directory, "game.json");
        GameState state = PlayedState();

        SaveGame.WriteFile(path, _content, state);
        SaveGame.WriteFile(path, _content, state); // overwrite an existing save

        SaveLoadResult result = SaveGame.ReadFile(path);
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(Snapshot(state), Snapshot(result.Game!.State));
        Assert.Equal(new[] { path }, Directory.GetFiles(_directory));
    }

    [Fact]
    public void FailedWriteLeavesTheExistingSaveIntact()
    {
        string path = Path.Combine(_directory, "game.json");
        SaveGame.WriteFile(path, _content, StartingState());
        string before = File.ReadAllText(path);
        GameState invalid = StartingState();
        invalid.Colonies[0].Population = 0;

        Assert.Throws<ArgumentException>(() => SaveGame.WriteFile(path, _content, invalid));

        Assert.Equal(before, File.ReadAllText(path));
        Assert.Equal(new[] { path }, Directory.GetFiles(_directory));
    }

    [Fact]
    public void MissingFileIsReported()
    {
        SaveLoadResult result = SaveGame.ReadFile(Path.Combine(_directory, "missing.json"));

        Assert.False(result.Succeeded);
        Assert.Contains("Cannot read save file", Assert.Single(result.Errors));
    }

    // --- Session ---

    [Fact]
    public void SessionLoadReplacesTheGameWithoutAdvancingTheTurn()
    {
        string path = Path.Combine(_directory, "game.json");
        GameState saved = PlayTurns(StartingState(), _content, 3);
        SaveGame.WriteFile(path, _content, saved);
        var session = new GameSession(_content, StartingState());

        SaveLoadResult result = session.Load(path);

        Assert.True(result.Succeeded);
        Assert.Equal(3, session.State.Turn);
        Assert.Equal(Snapshot(saved), Snapshot(session.State));
    }

    [Fact]
    public void FailedSessionLoadPreservesTheCurrentGame()
    {
        string path = Path.Combine(_directory, "broken.json");
        File.WriteAllText(path, "{ \"format\": \"openantares-save\" ");
        var session = new GameSession(_content, PlayTurns(StartingState(), _content, 2));
        GameState before = session.State;

        SaveLoadResult result = session.Load(path);

        Assert.False(result.Succeeded);
        Assert.Same(before, session.State);
    }

    [Fact]
    public void SessionRoutesCommandsAndForecasts()
    {
        var session = new GameSession(_content, StartingState());

        Assert.Equal(400, session.Forecast().Colony(HomeColony).NetProduction.Total);
        Assert.True(session.Execute(new SetReadyCommand(Empire, 0, true)).Accepted);
        Assert.Equal(1, session.State.Turn);
        Assert.False(session.Execute(new SetReadyCommand(Empire, 0, true)).Accepted);
        Assert.Equal(1, session.State.Turn);
    }

    // --- Helpers ---

    private GameState PlayedState()
    {
        GameState state = StartingState();
        state = Execute(state, new SetProjectCommand(Empire, 0, HomeColony, "fabrication_hall"));
        state = Execute(state, new SetResearchTargetCommand(Empire, 0, "cultivation_methods"));
        state = PlayTurns(state, _content, 4);
        state = Execute(state, new SetResearchTargetCommand(Empire, 4, "habitat_methods"));
        return state;
    }

    /// <summary>Plays turns with a fixed plan that changes workforce, so the continuation test exercises real decisions.</summary>
    private static GameState PlayTurns(GameState state, ContentSet content, int turns)
    {
        for (int i = 0; i < turns; i++)
        {
            ColonyState colony = state.FindColony(HomeColony)!;
            int population = colony.Population;
            var workforce = new Workforce(population - 4, 2, 2);
            state = CommandProcessor.Execute(content, state, new SetWorkforceCommand(Empire, state.Turn, HomeColony, workforce)).State;
            if (state.FindEmpire(Empire)!.ResearchTargetId is null && !state.FindEmpire(Empire)!.KnownTechnologyIds.Contains("measurement_methods"))
            {
                state = CommandProcessor.Execute(content, state, new SetResearchTargetCommand(Empire, state.Turn, "measurement_methods")).State;
            }

            CommandOutcome outcome = CommandProcessor.Execute(content, state, new SetReadyCommand(Empire, state.Turn, true));
            Assert.True(outcome.Accepted, outcome.Rejection?.Message);
            state = outcome.State;
        }

        return state;
    }

    private GameState Execute(GameState state, Command command)
    {
        CommandOutcome outcome = CommandProcessor.Execute(_content, state, command);
        Assert.True(outcome.Accepted, outcome.Rejection?.Message);
        return outcome.State;
    }

    private static LoadedGame Load(string text)
    {
        SaveLoadResult result = SaveGame.Deserialize(text);
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Game!;
    }

    private static string ReplaceLast(string text, string find, string replace)
    {
        int index = text.LastIndexOf(find, StringComparison.Ordinal);
        return text.Remove(index, find.Length).Insert(index, replace);
    }
}
