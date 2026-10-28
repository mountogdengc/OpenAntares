using System;
using System.Linq;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
using OpenAntares.Simulation.Random;

namespace OpenAntares.Simulation.Fleets;

public static class FleetInitializer
{
    public static Pcg32 RecoveryRandom(ulong seed, EmpireId owner) => Pcg32.FromSeed(seed ^ 0x4F46464943455201UL, (ulong)owner.Value ^ 0x5245434F56455201UL);

    public static void Initialize(ContentSet content, GameState state)
    {
        if (!Rules.HasFleets(state.RulesVersion)) return;
        var d = content.FleetCommand ?? throw new ArgumentException("Fleet campaigns require command content.");
        ulong seed = state.GalaxySeed ?? throw new ArgumentException("Fleet campaigns require a galaxy seed.");
        foreach (var e in state.Empires.OrderBy(e => e.Id))
        {
            if (e.Fleets is not null) throw new ArgumentException("Fleet state is already initialized.");
            var species = content.Species[e.SpeciesId!];
            var culture = d.Cultures.Single(c => c.Id == species.CommandCultureId);
            var home = state.Colonies.Where(c => c.EmpireId == e.Id).OrderBy(c => c.Id).First();
            var profile = d.Profiles.Single(p => p.Id == culture.ProfileIds.First(id => d.Profiles.Single(p => p.Id == id).Specialty == OfficerSpecialty.Navigator));
            var body = species.CommandEmbodiment == CommandEmbodiment.ColonyBody;
            var planet = state.FindPlanet(home.PlanetId)!;
            string name = body ? $"{state.FindStar(planet.StarId)!.Name} {planet.Orbit} — Command appointment1" : profile.Name;
            e.Fleets = new()
            {
                RecoveryRandom = RecoveryRandom(seed, e.Id), NextOfficerId = 2, LifetimeCommissions = 1,
                Officers = new() { new() { Id = new(1), ProfileId = profile.Id, SpeciesId = species.Id, CultureId = culture.Id, Name = name, Pronouns = profile.Pronouns, Personality = profile.Personality, Embodiment = species.CommandEmbodiment!.Value, Specialty = profile.Specialty, Doctrine = culture.DefaultDoctrine, Status = OfficerStatus.Posted, SourceBody = body ? home.Id : null, PostedColony = home.Id } },
                CandidateProfiles = body ? new() : new[] { OfficerSpecialty.Surveyor, OfficerSpecialty.Organizer }.Select(specialty => culture.ProfileIds.First(id => d.Profiles.Single(p => p.Id == id).Specialty == specialty)).ToList(),
                ProfileCommissions = new(StringComparer.Ordinal) { [profile.Id] = 1 },
                SurveyCredits = new(e.Surveys.Select(o => o.StarId))
            };
        }
    }
}
