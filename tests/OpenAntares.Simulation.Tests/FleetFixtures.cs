using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.Development;
using OpenAntares.Simulation.Fleets;
using OpenAntares.Simulation.Galaxy;
using OpenAntares.Simulation.Saves;
using OpenAntares.Simulation.State;

namespace OpenAntares.Simulation.Tests;

// Explicit pre-session supplements; never evidence of generated campaigns or earned ranks.
internal static class FleetFixtures
{
    internal static LoadedGame Game(string species = "harl", bool amebas = false, int rivals = 0)
    {
        var c = FleetContentTests.Core();
        var g = GalaxyGenerator.CreateNewGame(c, new("small", 42, 8, species, rivals, rivals == 1 ? "peldra" : null, CrisesEnabled: false, AmebasEnabled: amebas));
        if (!g.Succeeded) throw new InvalidOperationException(g.Error);
        return new(c, g.State!);
    }

    internal static void AttachForStateTest(LoadedGame g)
    {
        foreach (var e in g.State.Empires)
        {
            if (e.Fleets is not null) continue;
            var sp = g.Content.Species[e.SpeciesId!];
            var d = g.Content.FleetCommand!;
            var culture = d.Cultures.Single(c => c.Id == sp.CommandCultureId);
            var p = d.Profiles.Single(p => p.Id == culture.ProfileIds.First(id => d.Profiles.Single(p => p.Id == id).Specialty == OfficerSpecialty.Navigator));
            var col = g.State.Colonies.First(c => c.EmpireId == e.Id);
            e.Fleets = new()
            {
                RecoveryRandom = FleetInitializer.RecoveryRandom(42, e.Id),
                NextOfficerId = 2, LifetimeCommissions = 1,
                Officers = new() { new() { Id = new(1), ProfileId = p.Id, SpeciesId = sp.Id, CultureId = culture.Id, Name = p.Name, Pronouns = p.Pronouns, Personality = p.Personality, Specialty = p.Specialty, Doctrine = OfficerDoctrine.RecoverCommand, Status = OfficerStatus.Posted, Embodiment = sp.CommandEmbodiment!.Value, SourceBody = sp.CommandEmbodiment == CommandEmbodiment.ColonyBody ? col.Id : null, PostedColony = col.Id } },
                CandidateProfiles = sp.CommandEmbodiment == CommandEmbodiment.ColonyBody ? new() : culture.ProfileIds.Where(id => id != p.Id).ToList(),
                ProfileCommissions = new(StringComparer.Ordinal) { [p.Id] = 1 },
                SurveyCredits = new(e.Surveys.Select(s => s.StarId))
            };
        }
    }

    internal static LoadedGame Nested()
    {
        var g = Game(rivals: 1); AttachForStateTest(g);
        var e = g.State.Empires[0]; var f = e.Fleets!;
        var col = g.State.Colonies.Single(c => c.EmpireId == e.Id);
        var vessel = g.State.Vessels.Single(v => v.EmpireId == e.Id);
        vessel.Fleet = new(1);
        f.Fleets.Add(new() { Id = new(1), Name = "Home fleet", Members = new() { vessel.Id }, Flagship = vessel.Id }); f.NextFleetId = 2;
        col.Development!.Queue.Add(new(1, new(ProductionKinds.Vessel, "scout"))); col.Development.NextQueueToken = 2;
        f.Requests.Add(new() { Id = new(1), Name = "Finite request", Rally = vessel.StationaryStarId!.Value, ReservedFleetSlot = true, Colonies = new() { col.Id }, HasAllocated = true, Slots = new() { new() { Ordinal = 1, RoleId = "scout", Colony = col.Id, QueueToken = 1, Status = SlotStatus.Queued } } }); f.NextRequestId = 2;
        return g;
    }

    internal static string Bytes(GameState state) => JsonSerializer.Serialize(state);
    internal static LoadedGame Valid(LoadedGame g)
    {
        var errors = GameStateValidator.Validate(g.Content, g.State);
        if (errors.Count != 0) throw new InvalidOperationException(string.Join("; ", errors));
        return g;
    }
}
