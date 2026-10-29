using System;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using OpenAntares.Simulation.Combat;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.Scouting;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Tests;
// Explicitly labelled pre-session supplements; no assertion of earned combat history.
internal static class CombatFixtures
{
    internal static LoadedGame Game(string species="harl",bool enabled=true)
    {
        var c=ContentLoader.LoadDirectory(ContentLoaderTests.CoreContentDirectory()).Content!;
        var g=GalaxyGenerator.CreateNewGame(c,new("small",42,9,species,CrisesEnabled:false,AmebasEnabled:false,SentriesEnabled:enabled));
        if(!g.Succeeded)throw new InvalidOperationException(g.Error);
        return new(c,g.State!);
    }
    internal static string Bytes(GameState s)=>JsonSerializer.Serialize(s);
    internal static LoadedGame Duel(int ownHull=100,int sentryHull=80)
    {
        var g=Game();var s=g.State;var e=s.Empires[0];var v=s.Vessels[0];var pocket=s.Combat!.Pockets[0];
        v.RoleId="escort";CombatInitializer.InitializeVessel(g.Content,9,v);ScoutInitializer.InitializeVessel(g.Content,9,v);v.Combat!.Hull=ownHull;
        v.StationaryStarId=pocket.Star;v.Fleet=new(1);
        e.Fleets!.Fleets.Add(new(){Id=new(1),Name="Fixture combat fleet",Members=new(){v.Id},Flagship=v.Id});e.Fleets.NextFleetId=2;
        foreach(var sentry in pocket.Sentries)sentry.Hull=sentryHull;
        e.Combat!.Observations.Add(new(pocket.Id,pocket.Star,0,false,pocket.Sentries.Select(a=>new ObservedSentry(a.Id,a.Profile,a.Hull,g.Content.Combat!.SentryPursuitSpeed)).ToImmutableArray()));
        OpenAntares.Simulation.Knowledge.ObservationUpdater.Refresh(s,s.Turn); return Valid(g);
    }
    internal static LoadedGame Convoy()
    {
        var g=Duel();var s=g.State;var f=s.Empires[0].Fleets!.Fleets[0];var role=g.Content.Species[s.Empires[0].SpeciesId!].AllowedVesselIds.Select(id=>g.Content.Vessels[id]).First(v=>v.CanFoundColony);
        var ship=new VesselState{Id=new(s.AllocateId()),EmpireId=s.Empires[0].Id,RoleId=role.Id,CarriedPopulation=role.EmbarkPopulation,StationaryStarId=s.Vessels[0].StationaryStarId,Fleet=f.Id};
        CombatInitializer.InitializeVessel(g.Content,9,ship);ship.Crisis=new();s.Vessels.Add(ship);f.Members.Add(ship.Id);return Valid(g);
    }
    internal static LoadedGame Valid(LoadedGame g){var errors=GameStateValidator.Validate(g.Content,g.State);if(errors.Count!=0)throw new InvalidOperationException(string.Join("; ",errors));return g;}
}
