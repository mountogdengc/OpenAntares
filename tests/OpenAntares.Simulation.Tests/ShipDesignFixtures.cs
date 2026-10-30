using System;
using System.Collections.Immutable;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.ShipDesigns;
namespace OpenAntares.Simulation.Tests;
// Labelled pre-session supplements; these are not generated earned campaigns.
internal static class ShipDesignFixtures
{
    internal static DesignId SaveDesign(GameSession session,string name,ShipRecipe recipe)
    {
        var e=session.State.Empires[0];var result=session.Execute(new ShipDesignCommand(e.Id,session.State.Turn,e.ShipDesigns!.CatalogueRevision,new ShipDesignAction.Save(name,recipe)));
        if(!result.Accepted)throw new InvalidOperationException(result.Rejection?.Message);
        return session.State.Empires[0].ShipDesigns!.Designs.Single(d=>string.Equals(d.Name,name.Trim(),StringComparison.OrdinalIgnoreCase)).Id;
    }
    internal static GameSession Session(ImmutableArray<string> known=default,string species="harl",bool sentries=false)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var g=GalaxyGenerator.CreateNewGame(c,new("small",42,10,species,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:sentries));
        if(!g.Succeeded)throw new InvalidOperationException(g.Error);
        if(!known.IsDefault)g.State!.Empires[0].KnownTechnologyIds=known.Order(StringComparer.Ordinal).ToList();
        return new(c,g.State!);
    }
}
