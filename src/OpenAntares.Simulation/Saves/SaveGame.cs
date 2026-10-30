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
    public const int SchemaVersion = 11;

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
            ContentWriter.Write(writer, content, state.RulesVersion);
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

            if (schema is not (1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11))
            {
                return Fail($"The save uses unsupported schema version {schema}.");
            }

            if ((schema,rulesVersion) is not ((1,1) or (2,1) or (3,1) or (3,2) or (4,1) or (4,2) or (4,3) or (5,1) or (5,2) or (5,3) or (5,4) or (6,1) or (6,2) or (6,3) or (6,4) or (6,5) or (7,1) or (7,2) or (7,3) or (7,4) or (7,5) or (7,6) or (8,1) or (8,2) or (8,3) or (8,4) or (8,5) or (8,6) or (8,7) or (9,1) or (9,2) or (9,3) or (9,4) or (9,5) or (9,6) or (9,7) or (9,8) or (10,1) or (10,2) or (10,3) or (10,4) or (10,5) or (10,6) or (10,7) or (10,8) or (10,9) or (11,1) or (11,2) or (11,3) or (11,4) or (11,5) or (11,6) or (11,7) or (11,8) or (11,9) or (11,10)))
            {
                return Fail($"The save uses rules version {rulesVersion}; this build implements versions 1 through 10 with explicit schema compatibility. Saves are not migrated automatically.");
            }

            // Embedded content goes through the same loader and validation as content files.
            ContentSet? content = null;
            if (root.RequiredObject("content") is not null)
            {
                string contentJson = document.RootElement.GetProperty("content").GetRawText();
                if (!Rules.HasRivals((int)rulesVersion) && document.RootElement.GetProperty("content").TryGetProperty("rival_ai", out _))
                    errors.Add(new(SaveLocation, null, "content.rival_ai", "Old campaigns cannot contain rival policy content."));
                if (!Rules.HasDevelopment((int)rulesVersion) && document.RootElement.GetProperty("content").TryGetProperty("governor_templates", out _))
                    errors.Add(new(SaveLocation, null, "content.governor_templates", "Old campaigns cannot contain governor templates."));
                if (!Rules.HasCrises((int)rulesVersion) && document.RootElement.GetProperty("content").TryGetProperty("plagues", out _))
                    errors.Add(new(SaveLocation,null,"content.plagues","Old campaigns cannot contain crisis content."));
                if (!Rules.HasScouting((int)rulesVersion) && document.RootElement.GetProperty("content").TryGetProperty("space_ameba", out _))
                    errors.Add(new(SaveLocation,null,"content.space_ameba","Old campaigns cannot contain scout hazard content."));
                if(!Rules.HasCombat((int)rulesVersion)&&document.RootElement.GetProperty("content").TryGetProperty("combat",out _))errors.Add(new(SaveLocation,null,"content.combat","Old campaigns cannot contain combat content."));
                if (!Rules.HasFleets((int)rulesVersion) && document.RootElement.GetProperty("content").TryGetProperty("fleet_command", out _))
                    errors.Add(new(SaveLocation, null, "content.fleet_command", "Old campaigns cannot contain fleet command content."));
                if(!Rules.HasFleets((int)rulesVersion)&&document.RootElement.GetProperty("content").TryGetProperty("species",out var speciesContent)&&speciesContent.ValueKind==JsonValueKind.Array)
                    foreach(var species in speciesContent.EnumerateArray())if(species.ValueKind==JsonValueKind.Object&&(species.TryGetProperty("command_culture_id",out _)||species.TryGetProperty("command_embodiment",out _)))errors.Add(new(SaveLocation,null,"content.species","Old campaigns cannot contain command capability metadata."));
                if(!Rules.HasShipDesigns((int)rulesVersion))
                {
                    var embedded=document.RootElement.GetProperty("content");
                    if(embedded.TryGetProperty("ship_designs",out _))errors.Add(new(SaveLocation,null,"content.ship_designs","Old campaigns cannot contain ship-design content."));
                    if(embedded.TryGetProperty("technologies",out var technologies)&&technologies.ValueKind==JsonValueKind.Array)
                        foreach(var technology in technologies.EnumerateArray())if(technology.ValueKind==JsonValueKind.Object&&technology.TryGetProperty("ship_design_only",out _))errors.Add(new(SaveLocation,null,"content.technologies","Old campaigns cannot contain ship-design technology flags."));
                }
                ContentLoadResult loaded = ContentLoader.Load(new[] { new ContentSource("save content", contentJson) }, requireSpecies: Rules.HasSpecies((int)rulesVersion), requireRivals: Rules.HasRivals((int)rulesVersion), requireDevelopment: Rules.HasDevelopment((int)rulesVersion), requireCrises: Rules.HasCrises((int)rulesVersion),requireScouting:Rules.HasScouting((int)rulesVersion),requireFleets:Rules.HasFleets((int)rulesVersion),requireCombat:Rules.HasCombat((int)rulesVersion),requireShipDesigns:Rules.HasShipDesigns((int)rulesVersion));
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

            CombatSaveData.AttachDefinition(content,state);
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
        CrisisSaveData.WriteGame(writer,state.Crisis,state.RulesVersion);
        ScoutSaveData.WriteGame(writer,state.Scouting,state.RulesVersion);
        CombatSaveData.WriteGame(writer,state);
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
            SpeciesSaveData.Write(writer, "species_id", empire.SpeciesId, state.RulesVersion);
            writer.WriteNumber("research_reserve", empire.ResearchReserve);
            WriteNullableString(writer, "research_target", empire.ResearchTargetId);
            WriteStrings(writer, "known_technologies", empire.KnownTechnologyIds);
            writer.WriteBoolean("ready", empire.Ready);
            ExpansionSaveData.WriteSurveys(writer, empire, state.RulesVersion);
            RivalSaveData.WriteEmpire(writer, empire, state.RulesVersion);
            CrisisSaveData.WriteEmpire(writer,empire.Crisis,state.RulesVersion);
            ScoutSaveData.WriteEmpire(writer,empire.Scouting,state.RulesVersion);
            DevelopmentSaveData.WriteEmpire(writer, empire.Development, state.RulesVersion);
            FleetSaveData.WriteEmpire(writer,empire.Fleets,state.RulesVersion);
            CombatSaveData.WriteEmpire(writer,empire.Combat,state.RulesVersion);
            ShipDesignSaveData.WriteEmpire(writer,empire.ShipDesigns,state.RulesVersion);
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
            SpeciesSaveData.Write(writer, "environment_id", planet.EnvironmentId, state.RulesVersion);
            DevelopmentSaveData.WriteAppearance(writer, planet.Appearance, state.RulesVersion);
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
            SpeciesSaveData.Write(writer, "habitat_mode", colony.HabitatMode, state.RulesVersion);
            writer.WriteNumber("population", colony.Population);
            writer.WriteStartObject("workforce");
            writer.WriteNumber("support", colony.Workforce.Support);
            writer.WriteNumber("production", colony.Workforce.Production);
            writer.WriteNumber("research", colony.Workforce.Research);
            writer.WriteEndObject();
            writer.WriteNumber("growth_progress", colony.GrowthProgress);
            writer.WriteNumber("production_reserve", colony.ProductionReserve);
            WriteNullableString(writer, "project", colony.ProjectId);
            writer.WriteString("project_kind", colony.ProjectKind);
            WriteStrings(writer, "completed_buildings", colony.CompletedBuildingIds);
            CrisisSaveData.WriteColony(writer,colony.Crisis,state.RulesVersion);
            DevelopmentSaveData.WriteColony(writer, colony.Development, state.RulesVersion);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        ExpansionSaveData.WriteVessels(writer, state);
        writer.WriteEndObject();
    }

    private static GameState? ReadState(EntryReader reader, int rulesVersion, int schemaVersion)
    {
        var state = new GameState
        {
            RulesVersion = rulesVersion,
            Crisis = CrisisSaveData.ReadGame(reader,rulesVersion),
            Scouting = ScoutSaveData.ReadGame(reader,rulesVersion),
            Combat = CombatSaveData.ReadGame(reader,rulesVersion),
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
                SpeciesId = SpeciesSaveData.Read(item, "species_id", rulesVersion),
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

            if (schemaVersion >= 3) ExpansionSaveData.ReadSurveys(item, empire, Rules.HasExpansion(rulesVersion), rulesVersion);
            RivalSaveData.ReadEmpire(item, empire, rulesVersion);
            empire.Crisis = CrisisSaveData.ReadEmpire(item,rulesVersion);
            empire.Scouting = ScoutSaveData.ReadEmpire(item,rulesVersion);
            empire.Development = DevelopmentSaveData.ReadEmpire(item, rulesVersion);
            empire.Fleets = FleetSaveData.ReadEmpire(item,rulesVersion);
            empire.Combat = CombatSaveData.ReadEmpire(item,rulesVersion);
            ShipDesignSaveData.ReadEmpire(item,empire,rulesVersion);
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
            EnvironmentId = SpeciesSaveData.Read(item, "environment_id", rulesVersion),
            Appearance = DevelopmentSaveData.ReadAppearance(item, rulesVersion),
        }));

        ReadItems(reader, "colonies", item =>
        {
            var colony = new ColonyState
            {
                Id = new ColonyId(item.RequiredInt("id", 1, int.MaxValue)),
                PlanetId = new PlanetId(item.RequiredInt("planet", 1, int.MaxValue)),
                EmpireId = new EmpireId(item.RequiredInt("empire", 1, int.MaxValue)),
                Population = item.RequiredInt("population", 0, int.MaxValue),
                HabitatMode = SpeciesSaveData.Read(item, "habitat_mode", rulesVersion),
                GrowthProgress = item.RequiredInt("growth_progress", 0, int.MaxValue),
                ProductionReserve = item.RequiredLong("production_reserve", 0, long.MaxValue),
                ProjectId = item.RequiredNullableString("project"),
                ProjectKind = schemaVersion < 3 ? ProductionKinds.Building : Rules.HasExpansion(rulesVersion) ? item.RequiredString("project_kind") : item.OptionalString("project_kind", ProductionKinds.Building),
                CompletedBuildingIds = item.RequiredStringArray("completed_buildings").ToList(),
                Crisis = CrisisSaveData.ReadColony(item,rulesVersion),
                Development = DevelopmentSaveData.ReadColony(item, rulesVersion),
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

        if (schemaVersion >= 3) ExpansionSaveData.ReadVessels(reader, state, Rules.HasExpansion(rulesVersion));
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
