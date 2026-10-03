using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Content;

/// <summary>
/// Loads and validates content from JSON. Every file is an object whose optional sections are
/// <c>rules</c>, <c>starting_colony</c>, <c>galaxy</c>, <c>galaxy_sizes</c>, <c>planet_types</c>,
/// <c>buildings</c>, and <c>technologies</c>, so content can be split across files however is convenient. Files are read
/// in ordinal name order. All problems are reported together with their file, entry ID, and field.
/// </summary>
public static class ContentLoader
{
    private const string AllFiles = "(content)";

    // Generous limits that keep generation arithmetic far from overflow.
    private const int MaxStars = 1000;
    private const int MaxPlanetsPerStar = 50;
    private const int MaxCoordinate = 1_000_000;

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>Loads every <c>*.json</c> file directly inside <paramref name="directory"/>.</summary>
    public static ContentLoadResult LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new ContentLoadResult(null, new[] { new ContentError(directory, null, null, "Content directory does not exist.") });
        }

        ContentSource[] sources = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new ContentSource(Path.GetFileName(path), File.ReadAllText(path)))
            .ToArray();
        return Load(sources);
    }

    public static ContentLoadResult Load(IEnumerable<ContentSource> sources)
    {
        var builder = new Builder();
        foreach (ContentSource source in sources.OrderBy(s => s.Name, StringComparer.Ordinal))
        {
            builder.AddFile(source);
        }

        return builder.Build();
    }

    private sealed class Builder
    {
        private readonly List<ContentError> _errors = new();
        private (RulesParameters Value, string File)? _rules;
        private (StartingColonyDefinition Value, string File)? _startingColony;
        private (GalaxyRules Value, string File)? _galaxy;
        private readonly SortedDictionary<string, (GalaxySizeDefinition Value, string File)> _galaxySizes = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, (PlanetTypeDefinition Value, string File)> _planetTypes = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, (BuildingDefinition Value, string File)> _buildings = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, (TechnologyDefinition Value, string File)> _technologies = new(StringComparer.Ordinal);

        public void AddFile(ContentSource source)
        {
            string file = source.Name;
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(source.Json, JsonOptions);
            }
            catch (JsonException exception)
            {
                string position = exception.LineNumber is { } line ? $" at line {line + 1}" : string.Empty;
                _errors.Add(new ContentError(file, null, null, $"Invalid JSON{position}: {exception.Message}"));
                return;
            }

            using (document)
            {
                if (EntryReader.Create(document.RootElement, file, null, null, _errors) is not { } root)
                {
                    return;
                }

                foreach (JsonProperty section in document.RootElement.EnumerateObject())
                {
                    switch (section.Name)
                    {
                        case "rules":
                            SetOnce(ref _rules, ReadRules(section.Value, file), file, "rules");
                            break;
                        case "starting_colony":
                            SetOnce(ref _startingColony, ReadStartingColony(section.Value, file), file, "starting_colony");
                            break;
                        case "galaxy":
                            SetOnce(ref _galaxy, ReadGalaxy(section.Value, file), file, "galaxy");
                            break;
                        case "galaxy_sizes":
                            ReadEntries(section.Value, file, section.Name, ReadGalaxySize, _galaxySizes);
                            break;
                        case "planet_types":
                            ReadEntries(section.Value, file, section.Name, ReadPlanetType, _planetTypes);
                            break;
                        case "buildings":
                            ReadEntries(section.Value, file, section.Name, ReadBuilding, _buildings);
                            break;
                        case "technologies":
                            ReadEntries(section.Value, file, section.Name, ReadTechnology, _technologies);
                            break;
                        default:
                            _errors.Add(new ContentError(file, null, section.Name,
                                "Unknown section. Expected rules, starting_colony, galaxy, galaxy_sizes, planet_types, buildings, or technologies."));
                            break;
                    }
                }
            }
        }

        public ContentLoadResult Build()
        {
            if (_rules is null)
            {
                _errors.Add(new ContentError(AllFiles, null, "rules", "No file defines the rules section."));
            }

            if (_startingColony is null)
            {
                _errors.Add(new ContentError(AllFiles, null, "starting_colony", "No file defines the starting_colony section."));
            }

            if (_galaxy is null)
            {
                _errors.Add(new ContentError(AllFiles, null, "galaxy", "No file defines the galaxy section."));
            }

            CheckPrerequisiteReferences();
            CheckGeneration();
            CheckTechnologyCycles();
            if (_rules is { } rules && _startingColony is { } start)
            {
                CheckStartingColony(rules.Value, start.Value, start.File);
                CheckOutputBounds(rules.Value);
            }

            if (_errors.Count > 0)
            {
                return new ContentLoadResult(null, _errors);
            }

            var content = new ContentSet(
                _rules!.Value.Value,
                _startingColony!.Value.Value,
                _galaxy!.Value.Value,
                _galaxySizes.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal),
                _planetTypes.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal),
                _buildings.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal),
                _technologies.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal));
            return new ContentLoadResult(content, Array.Empty<ContentError>());
        }

        // --- Sections ---

        private RulesParameters? ReadRules(JsonElement element, string file)
        {
            if (EntryReader.Create(element, file, null, "rules", _errors) is not { } reader)
            {
                return null;
            }

            var rules = new RulesParameters(
                SupportPerPopulation: reader.RequiredLong("support_per_population", 1, long.MaxValue),
                BaseGrowth: reader.RequiredInt("base_growth", 0, int.MaxValue),
                MaxSurplusGrowth: reader.RequiredInt("max_surplus_growth", 0, int.MaxValue),
                BaseColonyCapacity: reader.RequiredInt("base_colony_capacity", 1, int.MaxValue));
            reader.RejectUnknownFields();

            // Growth earned is base plus the capped surplus bonus, and must fit an int.
            if ((long)rules.BaseGrowth + rules.MaxSurplusGrowth > int.MaxValue)
            {
                reader.Error("max_surplus_growth", "base_growth plus max_surplus_growth does not fit a 32-bit integer.");
            }

            return rules;
        }

        private StartingColonyDefinition? ReadStartingColony(JsonElement element, string file)
        {
            if (EntryReader.Create(element, file, null, "starting_colony", _errors) is not { } reader)
            {
                return null;
            }

            string planetType = reader.RequiredString("planet_type");
            int population = reader.RequiredInt("population", 1, int.MaxValue);
            Workforce workforce = default;
            if (reader.RequiredObject("workforce") is { } workforceReader)
            {
                workforce = new Workforce(
                    workforceReader.RequiredInt("support", 0, int.MaxValue),
                    workforceReader.RequiredInt("production", 0, int.MaxValue),
                    workforceReader.RequiredInt("research", 0, int.MaxValue));
                workforceReader.RejectUnknownFields();
            }

            reader.RejectUnknownFields();
            return new StartingColonyDefinition(planetType, population, workforce);
        }

        private PlanetTypeDefinition ReadPlanetType(EntryReader reader, string id) => new(
            id,
            reader.RequiredString("name"),
            reader.RequiredLong("support_per_worker", 0, long.MaxValue),
            reader.RequiredLong("production_per_worker", 0, long.MaxValue),
            reader.RequiredLong("research_per_worker", 0, long.MaxValue),
            reader.RequiredInt("generation_weight", 0, int.MaxValue));

        private GalaxyRules? ReadGalaxy(JsonElement element, string file)
        {
            if (EntryReader.Create(element, file, null, "galaxy", _errors) is not { } reader)
            {
                return null;
            }

            var galaxy = new GalaxyRules(
                MinStarDistance: reader.RequiredInt("min_star_distance", 0, MaxCoordinate),
                MinPlanetsPerStar: reader.RequiredInt("min_planets_per_star", 1, MaxPlanetsPerStar),
                MaxPlanetsPerStar: reader.RequiredInt("max_planets_per_star", 1, MaxPlanetsPerStar),
                StarNames: reader.RequiredStringArray("star_names"));
            reader.RejectUnknownFields();

            if (galaxy.MaxPlanetsPerStar < galaxy.MinPlanetsPerStar)
            {
                reader.Error("max_planets_per_star", "Must not be less than min_planets_per_star.");
            }

            return galaxy;
        }

        private GalaxySizeDefinition ReadGalaxySize(EntryReader reader, string id) => new(
            id,
            reader.RequiredString("name"),
            reader.RequiredInt("star_count", 1, MaxStars),
            reader.RequiredInt("width", 1, MaxCoordinate),
            reader.RequiredInt("height", 1, MaxCoordinate));

        private BuildingDefinition ReadBuilding(EntryReader reader, string id)
        {
            string name = reader.RequiredString("name");
            long cost = reader.RequiredLong("cost", 1, long.MaxValue);
            ImmutableArray<string> prerequisites = reader.RequiredStringArray("prerequisites");

            var effects = ImmutableArray.CreateBuilder<BuildingEffect>();
            if (reader.RequiredArray("effects") is { } array)
            {
                int index = 0;
                foreach (JsonElement item in array.EnumerateArray())
                {
                    if (ReadEffect(item, reader, $"effects[{index}]") is { } effect)
                    {
                        effects.Add(effect);
                    }

                    index++;
                }
            }

            return new BuildingDefinition(id, name, cost, prerequisites, effects.ToImmutable());
        }

        private static BuildingEffect? ReadEffect(JsonElement element, EntryReader parent, string field)
        {
            if (parent.Nested(element, field) is not { } reader)
            {
                return null;
            }

            string kind = reader.RequiredString("kind");
            bool supported = kind is EffectKinds.Capacity or EffectKinds.Support or EffectKinds.Production or EffectKinds.Research;
            if (kind.Length > 0 && !supported)
            {
                reader.Error("kind", $"Unsupported effect kind '{kind}'. Expected support, production, research, or capacity.");
            }

            long amount = reader.RequiredLong("amount", 0, kind == EffectKinds.Capacity ? int.MaxValue : long.MaxValue);
            reader.RejectUnknownFields();
            return supported ? new BuildingEffect(kind, amount) : null;
        }

        private TechnologyDefinition ReadTechnology(EntryReader reader, string id) => new(
            id,
            reader.RequiredString("name"),
            reader.RequiredLong("cost", 1, long.MaxValue),
            reader.RequiredStringArray("prerequisites"));

        private void ReadEntries<T>(
            JsonElement section,
            string file,
            string sectionName,
            Func<EntryReader, string, T> parse,
            SortedDictionary<string, (T Value, string File)> target)
        {
            if (section.ValueKind != JsonValueKind.Array)
            {
                _errors.Add(new ContentError(file, null, sectionName, $"Expected an array, found {EntryReader.Describe(section)}."));
                return;
            }

            int index = 0;
            foreach (JsonElement item in section.EnumerateArray())
            {
                // Locate errors by entry ID when there is one, otherwise by position in the section.
                string? entryId = item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("id", out JsonElement idElement)
                    && idElement.ValueKind == JsonValueKind.String
                    && idElement.GetString() is { Length: > 0 } text
                        ? text
                        : null;
                string? prefix = entryId is null ? $"{sectionName}[{index}]" : null;
                index++;

                if (EntryReader.Create(item, file, entryId, prefix, _errors) is not { } reader)
                {
                    continue;
                }

                string id = reader.RequiredString("id");
                if (id.Length > 0 && !IsValidId(id))
                {
                    reader.Error("id", "IDs must start with a lowercase letter and contain only lowercase letters, digits, and underscores.");
                }

                T value = parse(reader, id);
                reader.RejectUnknownFields();

                if (id.Length == 0)
                {
                    continue;
                }

                if (target.TryGetValue(id, out var existing))
                {
                    reader.Error("id", $"Duplicate ID; already defined in {existing.File}.");
                }
                else
                {
                    target.Add(id, (value, file));
                }
            }
        }

        private void SetOnce<T>(ref (T Value, string File)? slot, T? value, string file, string section)
            where T : class
        {
            if (slot is { } existing)
            {
                _errors.Add(new ContentError(file, null, section, $"Section is already defined in {existing.File}."));
                return;
            }

            if (value is not null)
            {
                slot = (value, file);
            }
        }

        // --- Cross-reference checks ---

        private void CheckPrerequisiteReferences()
        {
            foreach (var (id, (building, file)) in _buildings)
            {
                CheckTechnologyReferences(building.PrerequisiteTechnologyIds, file, id);
            }

            foreach (var (id, (technology, file)) in _technologies)
            {
                CheckTechnologyReferences(technology.PrerequisiteTechnologyIds, file, id);
            }
        }

        private void CheckTechnologyReferences(ImmutableArray<string> prerequisites, string file, string entryId)
        {
            for (int i = 0; i < prerequisites.Length; i++)
            {
                if (!_technologies.ContainsKey(prerequisites[i]))
                {
                    _errors.Add(new ContentError(file, entryId, $"prerequisites[{i}]", $"Technology '{prerequisites[i]}' is not defined."));
                }
            }
        }

        private void CheckTechnologyCycles()
        {
            var state = new Dictionary<string, bool>(StringComparer.Ordinal); // false = in progress, true = done
            var path = new List<string>();

            foreach (string id in _technologies.Keys)
            {
                Visit(id);
            }

            void Visit(string id)
            {
                if (state.TryGetValue(id, out bool done))
                {
                    if (!done)
                    {
                        int start = path.IndexOf(id);
                        string cycle = string.Join(" -> ", path.Skip(start).Append(id));
                        _errors.Add(new ContentError(_technologies[id].File, id, "prerequisites", $"Prerequisites form a cycle: {cycle}."));
                    }

                    return;
                }

                state[id] = false;
                path.Add(id);
                foreach (string prerequisite in _technologies[id].Value.PrerequisiteTechnologyIds)
                {
                    if (_technologies.ContainsKey(prerequisite))
                    {
                        Visit(prerequisite);
                    }
                }

                path.RemoveAt(path.Count - 1);
                state[id] = true;
            }
        }

        private void CheckGeneration()
        {
            long totalWeight = _planetTypes.Values.Sum(p => (long)p.Value.GenerationWeight);
            if (_planetTypes.Count > 0 && totalWeight == 0)
            {
                _errors.Add(new ContentError(AllFiles, null, "generation_weight", "At least one planet type needs a positive generation_weight."));
            }
            else if (totalWeight > int.MaxValue)
            {
                _errors.Add(new ContentError(AllFiles, null, "generation_weight", "Planet type generation weights must sum to at most 2147483647."));
            }

            if (_galaxy is not { } galaxy)
            {
                return;
            }

            if (_galaxySizes.Count == 0)
            {
                _errors.Add(new ContentError(galaxy.File, null, "galaxy_sizes", "At least one galaxy size must be defined."));
            }

            foreach (var (id, (size, file)) in _galaxySizes)
            {
                if (size.StarCount > galaxy.Value.StarNames.Length)
                {
                    _errors.Add(new ContentError(file, id, "star_count",
                        $"Needs {size.StarCount} star names, but galaxy.star_names lists {galaxy.Value.StarNames.Length}."));
                }
            }
        }

        private void CheckStartingColony(RulesParameters rules, StartingColonyDefinition start, string file)
        {
            const string Section = "starting_colony";
            if (start.PlanetTypeId.Length > 0)
            {
                if (!_planetTypes.TryGetValue(start.PlanetTypeId, out var planetType))
                {
                    _errors.Add(new ContentError(file, null, Section + ".planet_type", $"Planet type '{start.PlanetTypeId}' is not defined."));
                }
                else if (planetType.Value.SupportPerWorker <= 0)
                {
                    _errors.Add(new ContentError(file, null, Section + ".planet_type",
                        $"The starting planet type '{start.PlanetTypeId}' must have a positive support_per_worker."));
                }
            }

            if ((long)start.Workforce.Support + start.Workforce.Production + start.Workforce.Research != start.Population)
            {
                _errors.Add(new ContentError(file, null, Section + ".workforce", $"Workforce must sum to the starting population {start.Population}."));
            }

            if (start.Population > rules.BaseColonyCapacity)
            {
                _errors.Add(new ContentError(file, null, Section + ".population",
                    $"Starting population {start.Population} exceeds base_colony_capacity {rules.BaseColonyCapacity}."));
            }
        }

        /// <summary>
        /// Rejects content whose largest reachable output could overflow the stored types: maximum
        /// capacity times the highest per-worker rate, plus every building's bonus of that kind.
        /// </summary>
        private void CheckOutputBounds(RulesParameters rules)
        {
            Int128 maxCapacity = rules.BaseColonyCapacity + Sum(EffectKinds.Capacity);
            if (maxCapacity > int.MaxValue)
            {
                _errors.Add(new ContentError(AllFiles, null, null,
                    $"Base capacity plus every capacity building ({maxCapacity}) does not fit a 32-bit integer."));
                return;
            }

            Check("support required", maxCapacity * rules.SupportPerPopulation);
            Check("support", maxCapacity * MaxRate(p => p.SupportPerWorker) + Sum(EffectKinds.Support));
            Check("production", maxCapacity * MaxRate(p => p.ProductionPerWorker) + Sum(EffectKinds.Production));
            Check("research", maxCapacity * MaxRate(p => p.ResearchPerWorker) + Sum(EffectKinds.Research));

            void Check(string quantity, Int128 bound)
            {
                if (bound > long.MaxValue)
                {
                    _errors.Add(new ContentError(AllFiles, null, null,
                        $"The largest reachable {quantity} per colony ({bound}) does not fit a 64-bit integer."));
                }
            }

            Int128 MaxRate(Func<PlanetTypeDefinition, long> rate) =>
                _planetTypes.Count == 0 ? 0 : _planetTypes.Values.Max(p => rate(p.Value));

            Int128 Sum(string kind)
            {
                Int128 total = 0;
                foreach (var (building, _) in _buildings.Values)
                {
                    foreach (BuildingEffect effect in building.Effects)
                    {
                        if (effect.Kind == kind)
                        {
                            total += effect.Amount;
                        }
                    }
                }

                return total;
            }
        }

        private static bool IsValidId(string id)
        {
            if (id[0] is < 'a' or > 'z')
            {
                return false;
            }

            foreach (char c in id)
            {
                if (c is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '_'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
