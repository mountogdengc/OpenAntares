using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace OpenAntares.Simulation.Tests;

public class ArchitectureTests
{
    [Fact]
    public void SimulationLibraryDoesNotReferenceGodot()
    {
        Assembly simulation = Assembly.Load("OpenAntares.Simulation");

        string[] godotReferences = simulation.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Godot", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(godotReferences);
    }
}
