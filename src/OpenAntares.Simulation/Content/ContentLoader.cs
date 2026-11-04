using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Content;

/// <summary>
/// Loads and validates content from JSON. Every file is an object whose optional sections are
/// <c>rules</c>, <c>starting_colony</c>, <c>galaxy</c>, <c>galaxy_sizes</c>, <c>planet_types</c>,
/// <c>buildings</c>, and <c>technologies</c>, so content can be split across files however is convenient. Files are read
/// in ordinal name order. All problems are reported together with their file, entry ID, and field.
/// </summary>
public static partial class ContentLoader
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

    public static ContentLoadResult Load(IEnumerable<ContentSource> sources, bool requireSpecies = false, bool requireRivals = false, bool requireDevelopment = false, bool requireCrises = false, bool requireScouting = false, bool requireFleets = false, bool requireCombat = false, bool requireShipDesigns = false, bool requireDiplomacy = false, bool requireConflict = false)
    {
        var builder = new Builder(requireSpecies || requireRivals || requireDevelopment || requireCrises || requireScouting || requireFleets || requireCombat || requireShipDesigns || requireDiplomacy || requireConflict, requireRivals || requireDevelopment || requireCrises || requireScouting || requireFleets || requireCombat || requireShipDesigns || requireDiplomacy || requireConflict, requireDevelopment || requireCrises || requireScouting || requireFleets || requireCombat || requireShipDesigns || requireDiplomacy || requireConflict, requireCrises || requireScouting || requireFleets || requireCombat || requireShipDesigns || requireDiplomacy || requireConflict, requireScouting || requireFleets || requireCombat || requireShipDesigns || requireDiplomacy || requireConflict, requireFleets || requireCombat || requireShipDesigns || requireDiplomacy || requireConflict, requireCombat || requireShipDesigns || requireDiplomacy || requireConflict, requireShipDesigns || requireDiplomacy || requireConflict, requireDiplomacy || requireConflict, requireConflict);
        foreach (ContentSource source in sources.OrderBy(s => s.Name, StringComparer.Ordinal))
        {
            builder.AddFile(source);
        }

        return builder.Build();
    }

    private sealed partial class Builder(bool requireSpecies, bool requireRivals, bool requireDevelopment, bool requireCrises, bool requireScouting, bool requireFleets, bool requireCombat, bool requireShipDesigns, bool requireDiplomacy, bool requireConflict)
    {
        private readonly List<ContentError> _errors = new();
        private (Conflict.ConflictDefinition Value, string File)? _conflict;
        private (Diplomacy.DiplomacyDefinition Value, string File)? _diplomacy;
        private (ShipDesigns.ShipDesignDefinition Value, string File)? _shipDesigns;
        private (Combat.CombatDefinition Value, string File)? _combat;
        private (ExpansionDefinition Value, string File)? _expansion;
        private readonly SortedDictionary<string, (VesselDefinition Value, string File)> _vessels = new(StringComparer.Ordinal);
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
                        case "conflict":
                            SetOnce(ref _conflict, Conflict.ConflictContentReader.Read(section.Value, file, _errors), file, "conflict");
                            break;
                        case "diplomacy":
                            SetOnce(ref _diplomacy, Diplomacy.DiplomacyContentReader.Read(section.Value, file, _errors), file, "diplomacy");
                            break;
                        case "ship_designs":
                            SetOnce(ref _shipDesigns, ShipDesigns.ShipDesignContentReader.Read(section.Value, file, _errors), file, "ship_designs");
                            break;
                        case "combat":
                            SetOnce(ref _combat, Combat.CombatContentReader.Read(section.Value, file, _errors), file, "combat");
                            break;
                        case "fleet_command":
                            SetOnce(ref _fleetCommand, ReadFleetCommand(section.Value, file), file, "fleet_command");
                            break;
                        case "space_ameba":
                            SetOnce(ref _spaceAmeba, ReadAmeba(section.Value,file),file,"space_ameba");
                            break;
                        case "plagues":
                            ReadEntries(section.Value,file,section.Name,ReadPlague,_plagues);
                            break;
                        case "governor_templates":
                            ReadEntries(section.Value, file, section.Name, ReadGovernorTemplate, _governorTemplates);
                            break;
                        case "rival_ai":
                            SetOnce(ref _rivalAi, ReadRivalAi(section.Value, file), file, "rival_ai");
                            break;
                        case "species":
                            ReadEntries(section.Value, file, section.Name, ReadSpecies, _species);
                            break;
                        case "environments":
                            ReadEntries(section.Value, file, section.Name, ReadEnvironment, _environments);
                            break;
                        case "vessels":
                            ReadEntries(section.Value, file, section.Name, ReadVessel, _vessels);
                            break;
                        case "expansion":
                            SetOnce(ref _expansion, ReadExpansion(section.Value, file), file, "expansion");
                            break;
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
            if (_conflict is {} conflict) Conflict.ConflictContentReader.Validate(conflict.Value,_vessels.Values.Select(v=>v.Value),_combat?.Value,conflict.File,_errors);
            else if(requireConflict) _errors.Add(new ContentError(AllFiles,"conflict","conflict","Conflict campaigns require explicit conflict content, including Solo."));
            if (_diplomacy is {} diplomacy) Diplomacy.DiplomacyContentReader.Validate(diplomacy.Value,diplomacy.File,_errors);
            else if(requireDiplomacy) _errors.Add(new ContentError(AllFiles,"diplomacy","diplomacy","Diplomacy campaigns require explicit diplomacy content, including Solo."));
            if (_shipDesigns is {} ships) ShipDesigns.ShipDesignContentReader.Validate(ships.Value, _vessels.Values.Select(v => v.Value), _combat?.Value, _technologies.Values.Select(t => t.Value), ships.File, _errors);
            else if (requireShipDesigns) _errors.Add(new ContentError(AllFiles, "ship_designs", "ship_designs", "Ship-design campaigns require explicit ship_designs content, including Off."));
            if (_combat is {} combat) Combat.CombatContentReader.Validate(combat.Value, _vessels.Values.Select(v => v.Value), combat.File, _errors);
            else if (requireCombat || _vessels.Values.Any(v => v.Value.CombatOnly)) _errors.Add(new ContentError(AllFiles, "combat", "combat", "Combat campaigns and hulls require explicit combat content, including Off."));
            CheckFleets();
            CheckScouting();
            CheckCrises();
            CheckDevelopment();
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

            CheckExpansion();
            CheckSpecies();
            CheckRivals();
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
                _technologies.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal))
            {
                Conflict = _conflict?.Value,
                Diplomacy = _diplomacy?.Value,
                ShipDesigns = _shipDesigns?.Value,
                Combat = _combat?.Value,
                FleetCommand = _fleetCommand?.Value,
                SpaceAmeba = _spaceAmeba?.Value,
                Plagues = _plagues.ToImmutableSortedDictionary(p => p.Key,p => p.Value.Value,StringComparer.Ordinal),
                Vessels = _vessels.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal),
                Expansion = _expansion?.Value,
                RivalAi = _rivalAi?.Value,
                GovernorTemplates = _governorTemplates.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal),
                Species = _species.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal),
                Environments = _environments.ToImmutableSortedDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal)
            };
            return new ContentLoadResult(content, Array.Empty<ContentError>());
        }

        private ExpansionDefinition? ReadExpansion(JsonElement element, string file)
        {
            if (EntryReader.Create(element, file, null, "expansion", _errors) is not { } reader) return null;
            var value = new ExpansionDefinition(reader.RequiredString("starting_vessel"));
            reader.RejectUnknownFields(); return value;
        }
        private VesselDefinition ReadVessel(EntryReader reader, string id)
        {
            if (reader.HasField("requires_full_colony") && reader.HasField("founds_sealed_habitat")) _extendedVessels.Add(id);
            return new(id, reader.RequiredString("name"), reader.RequiredLong("cost", 1, long.MaxValue),
                reader.RequiredInt("speed", 1, int.MaxValue), reader.RequiredInt("embark_population", 0, int.MaxValue),
                reader.RequiredInt("minimum_source_population", 0, int.MaxValue), reader.RequiredBool("can_found_colony"))
            { CombatOnly = reader.OptionalBool("combat_only"), RequiresFullColony = reader.OptionalBool("requires_full_colony"), FoundsSealedHabitat = reader.OptionalBool("founds_sealed_habitat") };
        }
        private void CheckExpansion()
        {
            if ((_expansion is null) != (_vessels.Count == 0))
                _errors.Add(new ContentError(AllFiles, null, "expansion", "Vessels and expansion must be defined together."));
            if (_expansion is { } x && (!_vessels.TryGetValue(x.Value.StartingVesselId, out var start) || start.Value.CanFoundColony || start.Value.EmbarkPopulation != 0))
                _errors.Add(new ContentError(x.File, null, "expansion.starting_vessel", "Starting vessel must be a defined scout with no population cargo."));
            foreach (var (v, file) in _vessels.Values)
                if (v.CanFoundColony ? v.EmbarkPopulation < 1 || v.MinimumSourcePopulation <= v.EmbarkPopulation : v.EmbarkPopulation != 0 || v.MinimumSourcePopulation != 0)
                    _errors.Add(new ContentError(file, v.Id, "embark_population", "Founding cargo must be positive and leave source population; scouts carry none."));
            foreach (var (p, file) in _planetTypes.Values)
                if (p.Colonizable && p.SupportPerWorker <= 0)
                    _errors.Add(new ContentError(file, p.Id, "support_per_worker", "Colonizable worlds require positive support."));
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

        private PlanetTypeDefinition ReadPlanetType(EntryReader reader, string id)
        {
            bool colonizable = reader.OptionalBool("colonizable");
            string name = reader.RequiredString("name");
            long support = reader.RequiredLong("support_per_worker", 0, long.MaxValue);
            long production = reader.RequiredLong("production_per_worker", 0, long.MaxValue);
            long research = reader.RequiredLong("research_per_worker", 0, long.MaxValue);
            int weight = reader.RequiredInt("generation_weight", 0, int.MaxValue);
            var regionWeights = ImmutableSortedDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
            if (reader.OptionalObjectElement("region_generation_weights") is { } overrides)
            {
                foreach (JsonProperty entry in overrides.EnumerateObject())
                {
                    if (!GalaxyRegions.IsKnown(entry.Name))
                    {
                        reader.Error($"region_generation_weights.{entry.Name}", "Unknown galaxy region.");
                        continue;
                    }

                    if (regionWeights.ContainsKey(entry.Name))
                    {
                        reader.Error($"region_generation_weights.{entry.Name}", "Region is given more than once.");
                        continue;
                    }

                    int parsed = (int)EntryReader.ReadWholeNumber(entry.Value, entry.Name, 0, int.MaxValue,
                        (_, message) => reader.Error($"region_generation_weights.{entry.Name}", message));
                    regionWeights.Add(entry.Name, parsed);
                }
            }

            return new PlanetTypeDefinition(id, name, support, production, research, weight)
            {
                RegionGenerationWeights = regionWeights.ToImmutable(),
                EnvironmentGenerationWeights = ReadEnvironmentWeights(reader),
                Colonizable = colonizable,
            };
        }

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
            var regions = GalaxyRegions.DefaultDefinitions.ToBuilder();
            if (reader.OptionalObjectElement("regions") is { } definitions)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (JsonProperty entry in definitions.EnumerateObject())
                {
                    if (!GalaxyRegions.IsKnown(entry.Name))
                    {
                        reader.Error($"regions.{entry.Name}", "Unknown galaxy region.");
                        continue;
                    }

                    if (!seen.Add(entry.Name))
                    {
                        reader.Error($"regions.{entry.Name}", "Region is given more than once.");
                        continue;
                    }

                    if (reader.Nested(entry.Value, $"regions.{entry.Name}") is { } region)
                    {
                        string name = region.RequiredString("name");
                        string description = region.RequiredString("description");
                        region.RejectUnknownFields();
                        regions[entry.Name] = new GalaxyRegionDefinition(entry.Name, name, description);
                    }
                }
            }

            galaxy = galaxy with { RegionDefinitions = regions.ToImmutable() };
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
            reader.RequiredStringArray("prerequisites")) { ShipDesignOnly = reader.OptionalBool("ship_design_only") };

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

            foreach (string regionId in GalaxyRegions.Ids)
            {
                long regionTotal = _planetTypes.Values.Sum(p => (long)p.Value.WeightForRegion(regionId));
                if (_planetTypes.Count > 0 && regionTotal == 0)
                {
                    _errors.Add(new ContentError(AllFiles, null, $"region_generation_weights.{regionId}",
                        $"At least one planet type needs a positive weight in {regionId}."));
                }
                else if (regionTotal > int.MaxValue)
                {
                    _errors.Add(new ContentError(AllFiles, null, $"region_generation_weights.{regionId}",
                        $"Planet type weights in {regionId} must sum to at most {int.MaxValue}."));
                }
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
            Int128 researchBound = maxCapacity * MaxRate(p => p.ResearchPerWorker) + Sum(EffectKinds.Research);
            Check("research", researchBound);
            if (_expansion is not null && _galaxy is { } galaxy)
            {
                Int128 colonies = (Int128)(_galaxySizes.Count == 0 ? 0 : _galaxySizes.Values.Max(s => s.Value.StarCount)) * galaxy.Value.MaxPlanetsPerStar;
                Check("empire research across all possible colonies", researchBound * colonies);
            }

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
