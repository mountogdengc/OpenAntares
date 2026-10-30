using System;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
namespace OpenAntares.Simulation.Tests;
// Labelled pre-session supplements; these are not generated earned campaigns.
internal static class ShipDesignFixtures
{
    internal static GameSession Session(ImmutableArray<string> known=default,string species="harl",bool sentries=false)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var g=GalaxyGenerator.CreateNewGame(c,new("small",42,10,species,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries));
        if(!g.Succeeded)throw new InvalidOperationException(g.Error);
        if(!known.IsDefault)g.State!.Empires[0].KnownTechnologyIds=known.Order(StringComparer.Ordinal).ToList();
        return new(c,g.State!);
    }
}
