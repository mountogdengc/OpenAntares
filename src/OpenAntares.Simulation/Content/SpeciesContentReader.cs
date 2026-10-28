using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Content;

public static partial class ContentLoader
{
    private sealed partial class Builder
    {
        private readonly SortedDictionary<string, (SpeciesDefinition Value, string File)> _species = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, (EnvironmentDefinition Value, string File)> _environments = new(StringComparer.Ordinal);
        private readonly HashSet<string> _extendedVessels = new(StringComparer.Ordinal);
        private EnvironmentDefinition ReadEnvironment(EntryReader reader, string id) => new(id, reader.RequiredString("name"), reader.RequiredString("description"));
        private SpeciesDefinition ReadSpecies(EntryReader reader, string id)
        {
            string name = reader.RequiredString("name"), description = reader.RequiredString("description"), culture = reader.RequiredString("culture_name"), unit = reader.RequiredString("population_unit_label");
            int population = reader.RequiredInt("starting_population", 1, int.MaxValue);
            Workforce workforce = default;
            if (reader.RequiredObject("starting_workforce") is { } w)
            {
                workforce = new(w.RequiredInt("support", 0, int.MaxValue), w.RequiredInt("production", 0, int.MaxValue), w.RequiredInt("research", 0, int.MaxValue));
                w.RejectUnknownFields();
            }
            return new(id, name, description, culture, unit, population, reader.RequiredInt("base_capacity", 1, int.MaxValue),
                reader.RequiredNullableInt("sealed_base_capacity", 1, int.MaxValue), workforce,
                reader.RequiredLong("support_per_population", 1, long.MaxValue), reader.RequiredLong("support_per_growth_point", 1, long.MaxValue),
                reader.RequiredInt("production_basis_points", 1, int.MaxValue), reader.RequiredInt("research_basis_points", 1, int.MaxValue),
                reader.RequiredInt("base_growth", 0, int.MaxValue), reader.RequiredInt("max_surplus_growth", 0, int.MaxValue),
                reader.RequiredStringArray("natural_environment_ids"), reader.RequiredStringArray("allowed_vessel_ids"), reader.RequiredBool("allows_sealed_habitats"))
            {
                CommandEmbodiment = ReadCommandEmbodiment(reader),
                CommandCultureId = reader.HasField("command_culture_id") ? reader.RequiredString("command_culture_id") : null
            };
        }

        private static Fleets.CommandEmbodiment? ReadCommandEmbodiment(EntryReader reader)
        {
            if (!reader.HasField("command_embodiment")) return null;
            string value = reader.RequiredString("command_embodiment");
            if (value == "mortal") return Fleets.CommandEmbodiment.Mortal;
            if (value == "colony_body") return Fleets.CommandEmbodiment.ColonyBody;
            reader.Error("command_embodiment", "Expected mortal or colony_body.");
            return null;
        }
        private static ImmutableSortedDictionary<string, int> ReadEnvironmentWeights(EntryReader reader)
        {
            var weights = ImmutableSortedDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
            if (reader.OptionalObjectElement("environment_generation_weights") is { } obj)
                foreach (JsonProperty property in obj.EnumerateObject())
                {
                    if (weights.ContainsKey(property.Name)) { reader.Error("environment_generation_weights", "Duplicate environment weight."); continue; }
                    weights.Add(property.Name, (int)EntryReader.ReadWholeNumber(property.Value, property.Name, 0, int.MaxValue,
                        (field, message) => reader.Error("environment_generation_weights." + field, message)));
                }
            return weights.ToImmutable();
        }
        private void CheckSpecies()
        {
            if (!requireSpecies && _species.Count == 0 && _environments.Count == 0) return;
            if (_species.Count == 0 || _environments.Count == 0) _errors.Add(new(AllFiles, null, "species", "Species rules require species and environments."));
            foreach (var (planet, file) in _planetTypes.Values)
            {
                long total = planet.EnvironmentGenerationWeights.Values.Sum(v => (long)v);
                if (total <= 0 || total > int.MaxValue) _errors.Add(new(file, planet.Id, "environment_generation_weights", "Weights must have a positive 32-bit sum."));
                foreach (string id in planet.EnvironmentGenerationWeights.Keys)
                    if (!_environments.ContainsKey(id)) _errors.Add(new(file, planet.Id, "environment_generation_weights." + id, "Environment is not defined."));
            }
            foreach (var (vessel, file) in _vessels.Values)
            {
                if (!_extendedVessels.Contains(vessel.Id)) _errors.Add(new(file, vessel.Id, "requires_full_colony", "Species vessel definitions require explicit full-colony and sealed-habitat fields."));
                if (!vessel.CanFoundColony && (vessel.RequiresFullColony || vessel.FoundsSealedHabitat)) _errors.Add(new(file, vessel.Id, "can_found_colony", "Scouts cannot require full colonies or found sealed habitats."));
            }
            foreach (var (sp, file) in _species.Values)
            {
                void Error(string field, string message) => _errors.Add(new(file, sp.Id, field, message));
                if (sp.StartingPopulation > sp.BaseCapacity) Error("starting_population", "Starting population exceeds native capacity.");
                if ((long)sp.StartingWorkforce.Support + sp.StartingWorkforce.Production + sp.StartingWorkforce.Research != sp.StartingPopulation) Error("starting_workforce", "Workforce must sum to population.");
                if (sp.AllowsSealedHabitats != sp.SealedBaseCapacity.HasValue) Error("sealed_base_capacity", "Only species allowing sealed habitats require a positive sealed capacity.");
                if ((long)sp.BaseGrowth + sp.MaxSurplusGrowth + 99 > int.MaxValue) Error("max_surplus_growth", "Growth plus prior progress must fit a 32-bit integer.");
                CheckList(sp.NaturalEnvironmentIds, "natural_environment_ids", _environments.ContainsKey);
                CheckList(sp.AllowedVesselIds, "allowed_vessel_ids", _vessels.ContainsKey);
                if (!_environments.ContainsKey("shelf_sea") || !sp.NaturalEnvironmentIds.Contains("shelf_sea")) Error("natural_environment_ids", "The starting Shelf Sea habitat must be defined and naturally allowed.");
                if (_expansion is not { } expansion || !sp.AllowedVesselIds.Contains(expansion.Value.StartingVesselId)) Error("allowed_vessel_ids", "Starting scout must be allowed.");
                Int128 maxCapacity = (Int128)Math.Max(sp.BaseCapacity, sp.SealedBaseCapacity ?? 0) + Sum(EffectKinds.Capacity);
                if (maxCapacity > int.MaxValue) Error("base_capacity", "Capacity plus completed effects must fit a 32-bit integer.");
                CheckBound("support_per_population", maxCapacity * sp.SupportPerPopulation);
                CheckBound("support_per_population", maxCapacity * MaxRate(p => p.SupportPerWorker) + Sum(EffectKinds.Support));
                CheckBound("production_basis_points", maxCapacity * MaxRate(p => p.ProductionPerWorker) * sp.ProductionBasisPoints / 10000 + Sum(EffectKinds.Production));
                Int128 research = maxCapacity * MaxRate(p => p.ResearchPerWorker) * sp.ResearchBasisPoints / 10000 + Sum(EffectKinds.Research);
                CheckBound("research_basis_points", research);
                Int128 colonies = (Int128)(_galaxySizes.Count == 0 ? 0 : _galaxySizes.Values.Max(x => x.Value.StarCount)) * (_galaxy?.Value.MaxPlanetsPerStar ?? 0);
                CheckBound("research_basis_points", research * colonies);
                foreach (string roleId in sp.AllowedVesselIds)
                    if (_vessels.TryGetValue(roleId, out var entry) && entry.Value.CanFoundColony)
                    {
                        var role = entry.Value;
                        if (role.FoundsSealedHabitat && !sp.AllowsSealedHabitats) Error("allowed_vessel_ids", "Sealed role requires species permission.");
                        int destinationCapacity = role.FoundsSealedHabitat ? sp.SealedBaseCapacity ?? 0 : sp.BaseCapacity;
                        if (role.EmbarkPopulation > destinationCapacity) Error("allowed_vessel_ids", "Cargo exceeds unbuilt destination habitat capacity.");
                        if (role.MinimumSourcePopulation > maxCapacity) Error("allowed_vessel_ids", "Minimum source population is unattainable.");
                    }
                void CheckBound(string field, Int128 value) { if (value > long.MaxValue) Error(field, "Largest reachable output does not fit a 64-bit integer."); }
                void CheckList(ImmutableArray<string> ids, string field, Func<string,bool> defined)
                {
                    if (ids.IsEmpty || !ids.SequenceEqual(ids.OrderBy(x => x, StringComparer.Ordinal))) Error(field, "IDs must be nonempty and ordinally ordered.");
                    foreach (string id in ids) if (!defined(id)) Error(field, "Undefined reference: " + id);
                }
            }
            Int128 MaxRate(Func<PlanetTypeDefinition,long> rate) => _planetTypes.Count == 0 ? 0 : _planetTypes.Values.Max(x => rate(x.Value));
            Int128 Sum(string kind)
            {
                Int128 total = 0;
                foreach (var entry in _buildings.Values)
                    foreach (var effect in entry.Value.Effects) if (effect.Kind == kind) total += effect.Amount;
                return total;
            }
        }
    }
}
