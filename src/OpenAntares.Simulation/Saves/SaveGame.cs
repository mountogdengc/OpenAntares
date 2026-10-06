using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Random;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Saves;

/// <summary>A game loaded from a save: the embedded content and the state that uses it.</summary>
public sealed record LoadedGame(ContentSet Content, GameState State);

/// <summary>The outcome of reading a save. <see cref="Game"/> is set only when there are no errors.</summary>
public sealed record SaveLoadResult(LoadedGame? Game, IReadOnlyList<string> Errors)
{
    public bool Succeeded => Game is not null;
}

/// <summary>
/// Reads and writes saves. A save holds the complete planning state (including PRNG and ID
/// allocation state) and embeds the effective content, so it continues identically even if the
/// content files change. The rules implementation is versioned separately, because formulas in code
/// cannot be embedded. Schema 1 is read explicitly as a legacy layout; unsupported versions are rejected.
/// </summary>
public static class SaveGame
{
    public const string FormatName = "openantares-save";
    public const int SchemaVersion = 2;

    private const string SaveLocation = "save";

    private static readonly JsonWriterOptions WriterOptions = new() { Indented = true };

    /// <summary>Serializes a game. Throws <see cref="ArgumentException"/> if the state is not valid.</summary>
    public static string Serialize(ContentSet content, GameState state)
    {
        IReadOnlyList<string> errors = GameStateValidator.Validate(content, state);
        if (errors.Count > 0)
        {
            throw new ArgumentException("Refusing to save an invalid game state: " + string.Join("; ", errors), nameof(state));
        }

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("format", FormatName);
            writer.WriteNumber("schema_version", SchemaVersion);
            writer.WriteNumber("rules_version", state.RulesVersion);
            writer.WritePropertyName("content");
            ContentWriter.Write(writer, content);
            writer.WritePropertyName("state");
            WriteState(writer, state);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Writes a save without ever leaving a partial file at <paramref name="path"/>: the save is
    /// written and flushed to a temporary file, which then replaces the destination.
    /// </summary>
    public static void WriteFile(string path, ContentSet content, GameState state)
    {
        string text = Serialize(content, state);
        string temporary = path + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public static SaveLoadResult ReadFile(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Fail($"Cannot read save file: {exception.Message}");
        }

        return Deserialize(text);
    }

    public static SaveLoadResult Deserialize(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            string position = exception.LineNumber is { } line ? $" at line {line + 1}" : string.Empty;
            return Fail($"The save is not valid JSON{position}: {exception.Message}");
        }

        using (document)
        {
            var errors = new List<ContentError>();
            if (EntryReader.Create(document.RootElement, SaveLocation, null, null, errors) is not { } root)
            {
                return Fail(errors);
            }

            // Check the format and versions before anything else, so an incompatible save gets one clear message.
            string format = root.RequiredString("format");
            long schema = root.RequiredLong("schema_version", 0, int.MaxValue);
            long rulesVersion = root.RequiredLong("rules_version", 0, int.MaxValue);
            if (errors.Count > 0)
            {
                return Fail(errors);
            }

            if (format != FormatName)
            {
                return Fail($"This is not an OpenAntares save (format '{format}').");
            }

            if (schema is not (1 or SchemaVersion))
            {
                return Fail($"The save uses unsupported schema version {schema}.");
            }

            if (rulesVersion != Rules.CurrentVersion)
            {
                return Fail($"The save uses rules version {rulesVersion}; this build implements version {Rules.CurrentVersion}. Saves are not migrated automatically.");
            }

            // Embedded content goes through the same loader and validation as content files.
            ContentSet? content = null;
            if (root.RequiredObject("content") is not null)
            {
                string contentJson = document.RootElement.GetProperty("content").GetRawText();
                ContentLoadResult loaded = ContentLoader.Load(new[] { new ContentSource("save content", contentJson) });
                errors.AddRange(loaded.Errors);
                content = loaded.Content;
            }

            GameState? state = root.RequiredObject("state") is { } stateReader
                ? ReadState(stateReader, (int)rulesVersion, (int)schema) : null;
            root.RejectUnknownFields();

            if (errors.Count > 0 || content is null || state is null)
            {
                return Fail(errors);
            }

            IReadOnlyList<string> stateErrors = GameStateValidator.Validate(content, state);
            if (stateErrors.Count > 0)
            {
                return new SaveLoadResult(null, stateErrors.Select(e => "save state: " + e).ToList());
            }

            return new SaveLoadResult(new LoadedGame(content, state), Array.Empty<string>());
        }
    }

    // --- State ---

    private static void WriteState(Utf8JsonWriter writer, GameState state)
    {
        writer.WriteStartObject();
        writer.WriteNumber("turn", state.Turn);
        writer.WriteNumber("next_entity_id", state.NextEntityId);
        writer.WriteStartObject("random");
        writer.WriteNumber("state", state.Random.State);
        writer.WriteNumber("increment", state.Random.Increment);
        writer.WriteEndObject();
        if (state.GalaxySeed is { } seed) writer.WriteNumber("galaxy_seed", seed);
        else writer.WriteNull("galaxy_seed");
        WriteNullableString(writer, "galaxy_shape", state.GalaxyShapeId);
        if (state.GalaxyWidth is { } width) writer.WriteNumber("galaxy_width", width);
        else writer.WriteNull("galaxy_width");
        if (state.GalaxyHeight is { } height) writer.WriteNumber("galaxy_height", height);
        else writer.WriteNull("galaxy_height");

        writer.WriteStartArray("empires");
        foreach (EmpireState empire in state.Empires)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", empire.Id.Value);
            writer.WriteString("controller", ControllerName(empire.Controller));
            writer.WriteNumber("research_reserve", empire.ResearchReserve);
            WriteNullableString(writer, "research_target", empire.ResearchTargetId);
            WriteStrings(writer, "known_technologies", empire.KnownTechnologyIds);
            writer.WriteBoolean("ready", empire.Ready);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("stars");
        foreach (StarState star in state.Stars)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", star.Id.Value);
            writer.WriteString("name", star.Name);
            writer.WriteNumber("x", star.X);
            writer.WriteNumber("y", star.Y);
            WriteNullableString(writer, "region", star.RegionId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("planets");
        foreach (PlanetState planet in state.Planets)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", planet.Id.Value);
            writer.WriteNumber("star", planet.StarId.Value);
            writer.WriteNumber("orbit", planet.Orbit);
            writer.WriteString("planet_type", planet.PlanetTypeId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("colonies");
        foreach (ColonyState colony in state.Colonies)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", colony.Id.Value);
            writer.WriteNumber("planet", colony.PlanetId.Value);
            writer.WriteNumber("empire", colony.EmpireId.Value);
            writer.WriteNumber("population", colony.Population);
            writer.WriteStartObject("workforce");
            writer.WriteNumber("support", colony.Workforce.Support);
            writer.WriteNumber("production", colony.Workforce.Production);
            writer.WriteNumber("research", colony.Workforce.Research);
            writer.WriteEndObject();
            writer.WriteNumber("growth_progress", colony.GrowthProgress);
            writer.WriteNumber("production_reserve", colony.ProductionReserve);
            WriteNullableString(writer, "project", colony.ProjectId);
            WriteStrings(writer, "completed_buildings", colony.CompletedBuildingIds);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static GameState? ReadState(EntryReader reader, int rulesVersion, int schemaVersion)
    {
        var state = new GameState
        {
            RulesVersion = rulesVersion,
            Turn = reader.RequiredInt("turn", 0, int.MaxValue),
            NextEntityId = reader.RequiredInt("next_entity_id", 1, int.MaxValue),
        };

        if (reader.RequiredObject("random") is { } random)
        {
            state.Random = new Pcg32 { State = random.RequiredUInt64("state"), Increment = random.RequiredUInt64("increment") };
            random.RejectUnknownFields();
        }

        if (schemaVersion >= 2)
        {
            state.GalaxySeed = reader.RequiredNullableUInt64("galaxy_seed");
            state.GalaxyShapeId = reader.RequiredNullableString("galaxy_shape");
            state.GalaxyWidth = reader.RequiredNullableInt("galaxy_width", 1, int.MaxValue);
            state.GalaxyHeight = reader.RequiredNullableInt("galaxy_height", 1, int.MaxValue);
        }

        ReadItems(reader, "empires", item =>
        {
            string controller = item.RequiredString("controller");
            var empire = new EmpireState
            {
                Id = new EmpireId(item.RequiredInt("id", 1, int.MaxValue)),
                ResearchReserve = item.RequiredLong("research_reserve", 0, long.MaxValue),
                ResearchTargetId = item.RequiredNullableString("research_target"),
                KnownTechnologyIds = item.RequiredStringArray("known_technologies").ToList(),
                Ready = item.RequiredBool("ready"),
            };
            if (TryParseController(controller, out ControllerKind kind))
            {
                empire.Controller = kind;
            }
            else if (controller.Length > 0)
            {
                item.Error("controller", $"Unknown controller '{controller}'. Expected human or ai.");
            }

            state.Empires.Add(empire);
        });

        ReadItems(reader, "stars", item => state.Stars.Add(new StarState
        {
            Id = new StarId(item.RequiredInt("id", 1, int.MaxValue)),
            Name = item.RequiredString("name"),
            X = item.RequiredInt("x", int.MinValue, int.MaxValue),
            Y = item.RequiredInt("y", int.MinValue, int.MaxValue),
            RegionId = schemaVersion >= 2 ? item.RequiredNullableString("region") : null,
        }));

        ReadItems(reader, "planets", item => state.Planets.Add(new PlanetState
        {
            Id = new PlanetId(item.RequiredInt("id", 1, int.MaxValue)),
            StarId = new StarId(item.RequiredInt("star", 1, int.MaxValue)),
            Orbit = item.RequiredInt("orbit", 1, int.MaxValue),
            PlanetTypeId = item.RequiredString("planet_type"),
        }));

        ReadItems(reader, "colonies", item =>
        {
            var colony = new ColonyState
            {
                Id = new ColonyId(item.RequiredInt("id", 1, int.MaxValue)),
                PlanetId = new PlanetId(item.RequiredInt("planet", 1, int.MaxValue)),
                EmpireId = new EmpireId(item.RequiredInt("empire", 1, int.MaxValue)),
                Population = item.RequiredInt("population", 0, int.MaxValue),
                GrowthProgress = item.RequiredInt("growth_progress", 0, int.MaxValue),
                ProductionReserve = item.RequiredLong("production_reserve", 0, long.MaxValue),
                ProjectId = item.RequiredNullableString("project"),
                CompletedBuildingIds = item.RequiredStringArray("completed_buildings").ToList(),
            };
            if (item.RequiredObject("workforce") is { } workforce)
            {
                colony.Workforce = new Workforce(
                    workforce.RequiredInt("support", 0, int.MaxValue),
                    workforce.RequiredInt("production", 0, int.MaxValue),
                    workforce.RequiredInt("research", 0, int.MaxValue));
                workforce.RejectUnknownFields();
            }

            state.Colonies.Add(colony);
        });

        reader.RejectUnknownFields();
        return state;
    }

    private static void ReadItems(EntryReader parent, string field, Action<EntryReader> read)
    {
        if (parent.RequiredArray(field) is not { } array)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (parent.Nested(item, $"{field}[{index}]") is { } reader)
            {
                read(reader);
                reader.RejectUnknownFields();
            }

            index++;
        }
    }

    private static string ControllerName(ControllerKind kind) => kind switch
    {
        ControllerKind.Human => "human",
        ControllerKind.Ai => "ai",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static bool TryParseController(string name, out ControllerKind kind)
    {
        switch (name)
        {
            case "human":
                kind = ControllerKind.Human;
                return true;
            case "ai":
                kind = ControllerKind.Ai;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (string value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static SaveLoadResult Fail(string error) => new(null, new[] { error });

    private static SaveLoadResult Fail(IEnumerable<ContentError> errors) =>
        new(null, errors.Select(e => e.ToString()).ToList());
}
