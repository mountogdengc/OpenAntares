using System;
using System.Linq;
using System.Text.Json.Nodes;
using OpenAntares.Simulation.Content;
using Xunit;
namespace OpenAntares.Simulation.Tests;
public sealed class SpeciesContentTests
{
    [Fact]
    public void CompleteSpeciesContentLoadsWithoutChangingLegacyDefinitions()
    {
        var result = ContentLoader.Load(new[] {new ContentSource("species-test.json", SpeciesFixtures.ContentJson())});
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
    }
    [Theory]
    [InlineData("unknown_environment")][InlineData("unknown_role")][InlineData("duplicate_role")]
    [InlineData("unsorted_roles")][InlineData("sealed_missing")][InlineData("sealed_unavailable")]
    [InlineData("over_capacity")][InlineData("workforce")][InlineData("growth_overflow")]
    [InlineData("support_overflow")][InlineData("factor_overflow")][InlineData("capacity_overflow")]
    [InlineData("weight_zero")][InlineData("weight_overflow")][InlineData("weight_unknown")]
    [InlineData("cargo_capacity")][InlineData("minimum_unreachable")][InlineData("scout_full")]
    [InlineData("missing_role_field")][InlineData("fractional")][InlineData("missing_species")]
    public void InvalidSpeciesDefinitionsAreLocatedAndNeverPublished(string mutation)
    {
        var root = JsonNode.Parse(SpeciesFixtures.ContentJson())!;
        var p = root["species"]![0]!;
        var sulan = root["species"]![2]!;
        var role = root["vessels"]![1]!;
        var weights = root["planet_types"]![0]!["environment_generation_weights"]!;
        switch(mutation)
        {
            case "unknown_environment": p["natural_environment_ids"] = new JsonArray("missing"); break;
            case "unknown_role": p["allowed_vessel_ids"] = new JsonArray("missing"); break;
            case "duplicate_role": p["allowed_vessel_ids"] = new JsonArray("scout","scout"); break;
            case "unsorted_roles": p["allowed_vessel_ids"] = new JsonArray("scout","peldra_settler"); break;
            case "sealed_missing": sulan["sealed_base_capacity"] = null; break;
            case "sealed_unavailable": p["sealed_base_capacity"] = 8; break;
            case "over_capacity": p["starting_population"] = 11; break;
            case "workforce": p["starting_workforce"]!["support"] = 0; break;
            case "growth_overflow": p["base_growth"] = int.MaxValue; break;
            case "support_overflow": p["support_per_population"] = long.MaxValue; break;
            case "factor_overflow": root["planet_types"]![0]!["production_per_worker"] = long.MaxValue / 20; p["production_basis_points"] = int.MaxValue; break;
            case "capacity_overflow": p["base_capacity"] = int.MaxValue; break;
            case "weight_zero": foreach(var key in weights.AsObject().Select(kv => kv.Key).ToArray()) weights[key] = 0; break;
            case "weight_overflow": weights["dry"] = int.MaxValue; weights["open_ocean"] = 1; break;
            case "weight_unknown": weights["missing"] = 1; break;
            case "cargo_capacity": role["embark_population"] = 11; role["minimum_source_population"] = 12; break;
            case "minimum_unreachable": role["minimum_source_population"] = 20; break;
            case "scout_full": root["vessels"]![0]!["requires_full_colony"] = true; break;
            case "missing_role_field": role.AsObject().Remove("founds_sealed_habitat"); break;
            case "fractional": p["support_per_growth_point"] = 1.5; break;
            case "missing_species": root.AsObject().Remove("species"); break;
        }
        var result = ContentLoader.Load(new[]{new ContentSource("bad-species.json",root.ToJsonString())});
        Assert.Null(result.Content);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, e => {Assert.NotEmpty(e.File); Assert.NotNull(e.Field);});
    }
}
