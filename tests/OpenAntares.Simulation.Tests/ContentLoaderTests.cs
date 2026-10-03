using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public class ContentLoaderTests
{
    private const string BaseRules = """
        {
          "rules": { "support_per_population": 100, "base_growth": 10, "max_surplus_growth": 40, "base_colony_capacity": 10 },
          "starting_colony": { "planet_type": "home", "population": 2, "workforce": { "support": 1, "production": 1, "research": 0 } }
        }
        """;

    private const string BaseWorld = """
        {
          "planet_types": [
            { "id": "home", "name": "Home", "support_per_worker": 300, "production_per_worker": 200, "research_per_worker": 200 }
          ],
          "technologies": [
            { "id": "t1", "name": "T1", "cost": 100, "prerequisites": [] }
          ],
          "buildings": [
            { "id": "b1", "name": "B1", "cost": 100, "prerequisites": ["t1"], "effects": [{ "kind": "production", "amount": 100 }] }
          ]
        }
        """;

    [Fact]
    public void CoreContentLoadsAndMatchesTheSpecificationFixture()
    {
        ContentLoadResult result = ContentLoader.LoadDirectory(CoreContentDirectory());

        Assert.Empty(result.Errors);
        Assert.Equal(Serialize(Fixtures.PrototypeContent()), Serialize(result.Content!));
    }

    [Fact]
    public void MinimalContentLoads()
    {
        ContentLoadResult result = Load(BaseRules, BaseWorld);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(new[] { "home" }, result.Content!.PlanetTypes.Keys);
        Assert.Equal(new BuildingEffect("production", 100), Assert.Single(result.Content.Buildings["b1"].Effects));
    }

    [Fact]
    public void CommentsAndTrailingCommasAreAllowed()
    {
        string world = BaseWorld.Replace("\"cost\": 100, \"prerequisites\": [] }", "\"cost\": 100, \"prerequisites\": [], } // first tech");

        Assert.True(Load(BaseRules, world).Succeeded);
    }

    [Fact]
    public void SectionsCanBeSplitAcrossFilesInAnyCombination()
    {
        ContentLoadResult result = ContentLoader.Load(new[]
        {
            new ContentSource("z_buildings.json", """{ "buildings": [] }"""),
            new ContentSource("a_everything.json", BaseRules.TrimEnd().TrimEnd('}') + ", " + BaseWorld.Trim().TrimStart('{')),
        });

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Fact]
    public void MissingDirectoryIsReported()
    {
        ContentLoadResult result = ContentLoader.LoadDirectory(Path.Combine(Path.GetTempPath(), "openantares-no-such-dir"));

        Assert.Contains("does not exist", Assert.Single(result.Errors).Message);
    }

    public static TheoryData<string, string, string, string, string?, string?, string> InvalidContent => new()
    {
        // file to edit, find, replace, expected file, expected entry, expected field, expected message fragment
        { "world.json", "{", "{{", "world.json", null, null, "Invalid JSON" },
        { "world.json", "\"planet_types\"", "\"planets\"", "world.json", null, "planets", "Unknown section" },
        { "world.json", "\"planet_types\": [", "\"planet_types\": {}, \"x\": [", "world.json", null, "planet_types", "Expected an array" },
        { "world.json", "\"name\": \"Home\", ", "", "world.json", "home", "name", "Required field is missing" },
        { "world.json", "\"cost\": 100, \"prerequisites\": []", "\"costt\": 100, \"cost\": 100, \"prerequisites\": []", "world.json", "t1", "costt", "Unknown field" },
        { "world.json", "\"cost\": 100, \"prerequisites\": []", "\"cost\": 100, \"cost\": 100, \"prerequisites\": []", "world.json", "t1", "cost", "more than once" },
        { "world.json", "\"cost\": 100, \"prerequisites\": []", "\"cost\": 12.5, \"prerequisites\": []", "world.json", "t1", "cost", "Decimal values are not allowed" },
        { "world.json", "\"cost\": 100, \"prerequisites\": []", "\"cost\": \"100\", \"prerequisites\": []", "world.json", "t1", "cost", "Expected a whole number" },
        { "world.json", "\"cost\": 100, \"prerequisites\": []", "\"cost\": 0, \"prerequisites\": []", "world.json", "t1", "cost", "outside the allowed range" },
        { "world.json", "\"research_per_worker\": 200", "\"research_per_worker\": -1", "world.json", "home", "research_per_worker", "outside the allowed range" },
        { "world.json", "\"id\": \"t1\"", "\"id\": \"Tech-1\"", "world.json", "Tech-1", "id", "lowercase" },
        { "world.json", "{ \"id\": \"t1\", ", "{ ", "world.json", null, "technologies[0].id", "Required field is missing" },
        { "world.json", "\"prerequisites\": [\"t1\"]", "\"prerequisites\": [\"t9\"]", "world.json", "b1", "prerequisites[0]", "'t9' is not defined" },
        { "world.json", "\"prerequisites\": [\"t1\"]", "\"prerequisites\": [\"t1\", \"t1\"]", "world.json", "b1", "prerequisites[1]", "more than once" },
        { "world.json", "\"kind\": \"production\"", "\"kind\": \"morale\"", "world.json", "b1", "effects[0].kind", "Unsupported effect kind 'morale'" },
        { "world.json", "\"kind\": \"production\", \"amount\": 100", "\"kind\": \"capacity\", \"amount\": 3000000000", "world.json", "b1", "effects[0].amount", "outside the allowed range" },
        { "world.json", "\"amount\": 100 }", "\"amount\": 100, \"scope\": \"empire\" }", "world.json", "b1", "effects[0].scope", "Unknown field" },
        { "rules.json", "\"planet_type\": \"home\"", "\"planet_type\": \"moon\"", "rules.json", null, "starting_colony.planet_type", "'moon' is not defined" },
        { "rules.json", "\"research\": 0", "\"research\": 5", "rules.json", null, "starting_colony.workforce", "must sum" },
        { "rules.json", "\"workforce\": {", "\"workforce\": { \"idle\": 0,", "rules.json", null, "starting_colony.workforce.idle", "Unknown field" },
        { "rules.json", "\"base_colony_capacity\": 10", "\"base_colony_capacity\": 1", "rules.json", null, "starting_colony.population", "exceeds base_colony_capacity" },
        { "rules.json", "\"base_growth\": 10,", "", "rules.json", null, "rules.base_growth", "Required field is missing" },
        { "world.json", "\"support_per_worker\": 300", "\"support_per_worker\": 0", "rules.json", null, "starting_colony.planet_type", "positive support_per_worker" },
        { "world.json", "\"production_per_worker\": 200", "\"production_per_worker\": 9223372036854775807", "(content)", null, null, "largest reachable production" },
    };

    [Theory]
    [MemberData(nameof(InvalidContent))]
    public void InvalidContentIsReportedWithItsLocation(
        string editFile, string find, string replace,
        string expectedFile, string? expectedEntry, string? expectedField, string expectedMessage)
    {
        string rules = BaseRules;
        string world = BaseWorld;
        if (editFile == "rules.json")
        {
            rules = ReplaceOnce(rules, find, replace);
        }
        else
        {
            world = ReplaceOnce(world, find, replace);
        }

        ContentLoadResult result = Load(rules, world);

        Assert.Null(result.Content);
        Assert.Contains(result.Errors, error =>
            error.File == expectedFile
            && error.EntryId == expectedEntry
            && error.Field == expectedField
            && error.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateIdAcrossFilesNamesTheFirstFile()
    {
        ContentLoadResult result = ContentLoader.Load(new[]
        {
            new ContentSource("rules.json", BaseRules),
            new ContentSource("world.json", BaseWorld),
            new ContentSource("extra.json", """{ "technologies": [{ "id": "t1", "name": "Again", "cost": 5, "prerequisites": [] }] }"""),
        });

        ContentError error = Assert.Single(result.Errors);
        Assert.Equal(("world.json", "t1", "id"), (error.File, error.EntryId, error.Field));
        Assert.Contains("already defined in extra.json", error.Message);
    }

    [Fact]
    public void SingletonSectionsMustBeDefinedExactlyOnce()
    {
        ContentLoadResult missing = ContentLoader.Load(new[] { new ContentSource("world.json", BaseWorld) });
        Assert.Contains(missing.Errors, e => e.Field == "rules" && e.Message.Contains("No file defines"));
        Assert.Contains(missing.Errors, e => e.Field == "starting_colony" && e.Message.Contains("No file defines"));

        ContentLoadResult twice = ContentLoader.Load(new[]
        {
            new ContentSource("a.json", BaseRules),
            new ContentSource("b.json", BaseRules),
            new ContentSource("world.json", BaseWorld),
        });
        Assert.Contains(twice.Errors, e => e.File == "b.json" && e.Field == "rules" && e.Message.Contains("already defined in a.json"));
    }

    [Fact]
    public void PrerequisiteCyclesAreRejected()
    {
        string world = BaseWorld.Replace(
            """{ "id": "t1", "name": "T1", "cost": 100, "prerequisites": [] }""",
            """
            { "id": "t1", "name": "T1", "cost": 100, "prerequisites": ["t3"] },
            { "id": "t2", "name": "T2", "cost": 100, "prerequisites": ["t1"] },
            { "id": "t3", "name": "T3", "cost": 100, "prerequisites": ["t2"] }
            """);

        ContentLoadResult result = Load(BaseRules, world);

        ContentError error = Assert.Single(result.Errors);
        Assert.Equal("prerequisites", error.Field);
        Assert.Contains("cycle: t1 -> t3 -> t2 -> t1", error.Message);
    }

    [Fact]
    public void EveryProblemIsReportedAtOnce()
    {
        string world = BaseWorld
            .Replace("\"name\": \"Home\", ", "")
            .Replace("\"cost\": 100, \"prerequisites\": []", "\"cost\": -5, \"prerequisites\": []");

        ContentLoadResult result = Load(BaseRules, world);

        Assert.Contains(result.Errors, e => e.EntryId == "home" && e.Field == "name");
        Assert.Contains(result.Errors, e => e.EntryId == "t1" && e.Field == "cost");
    }

    [Fact]
    public void ErrorTextNamesFileEntryAndField()
    {
        var error = new ContentError("buildings.json", "fabrication_hall", "cost", "Value 0 is outside the allowed range 1 to 9.");

        Assert.Equal("buildings.json, entry 'fabrication_hall', field 'cost': Value 0 is outside the allowed range 1 to 9.", error.ToString());
    }

    private static ContentLoadResult Load(string rules, string world) => ContentLoader.Load(new[]
    {
        new ContentSource("rules.json", rules),
        new ContentSource("world.json", world),
    });

    private static string ReplaceOnce(string text, string find, string replace)
    {
        int index = text.IndexOf(find, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Test setup: '{find}' not found.");
        return text.Remove(index, find.Length).Insert(index, replace);
    }

    private static string Serialize(ContentSet content) => JsonSerializer.Serialize(content);

    private static string CoreContentDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenAntares.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "content", "core");
    }
}
