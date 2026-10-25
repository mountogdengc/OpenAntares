using System;
using System.Text.Json;
using OpenAntares.Simulation.Content;
using OpenAntares.Simulation.State;
namespace OpenAntares.Simulation.Saves;

internal static class ExpansionSaveData
{
    private static void Id(Utf8JsonWriter w, string key, int? id) { if (id is { } n) w.WriteNumber(key, n); else w.WriteNull(key); }
    internal static void WriteVessels(Utf8JsonWriter w, GameState s)
    {
        w.WriteStartArray("vessels");
        foreach (var v in s.Vessels)
        {
            w.WriteStartObject(); w.WriteNumber("id", v.Id.Value); w.WriteNumber("empire", v.EmpireId.Value); w.WriteString("role", v.RoleId);
            Id(w, "station", v.StationaryStarId?.Value); Id(w, "pending_move", v.PendingMove?.Value); Id(w, "pending_founding", v.PendingFounding?.Value); w.WriteNumber("carried_population", v.CarriedPopulation);
            if (v.Transit is { } t) { w.WriteStartObject("transit"); w.WriteNumber("origin", t.Origin.Value); w.WriteNumber("destination", t.Destination.Value); w.WriteNumber("total_turns", t.TotalTurns); w.WriteNumber("remaining_turns", t.RemainingTurns); w.WriteEndObject(); } else w.WriteNull("transit");
            CrisisSaveData.WriteVessel(w,v.Crisis,s.RulesVersion);
            ScoutSaveData.WriteVessel(w,v,s.RulesVersion);
            w.WriteEndObject();
        }
        w.WriteEndArray();
    }
    internal static void WriteSurveys(Utf8JsonWriter w, EmpireState e, int rulesVersion)
    {
        w.WriteStartArray("surveys");
        foreach (var o in e.Surveys)
        {
            w.WriteStartObject(); w.WriteNumber("star", o.StarId.Value); w.WriteNumber("last_observed_turn", o.LastObservedTurn); w.WriteStartArray("planets");
            foreach (var p in o.Planets) { w.WriteStartObject(); w.WriteNumber("planet", p.PlanetId.Value); w.WriteNumber("orbit", p.Orbit); w.WriteString("planet_type", p.PlanetTypeId); Id(w, "colony", p.ColonyId?.Value); Id(w, "owner", p.OwnerId?.Value); SpeciesSaveData.Write(w, "environment_id", p.EnvironmentId, rulesVersion); DevelopmentSaveData.WriteAppearance(w, p.Appearance, rulesVersion); w.WriteEndObject(); }
            w.WriteEndArray(); w.WriteEndObject();
        }
        w.WriteEndArray();
    }
    internal static void ReadVessels(EntryReader r, GameState s, bool required)
    {
        Items(r, "vessels", required, v =>
        {
            int? station = v.RequiredNullableInt("station", 1, int.MaxValue), move = v.RequiredNullableInt("pending_move", 1, int.MaxValue), found = v.RequiredNullableInt("pending_founding", 1, int.MaxValue);
            var vessel = new VesselState { Id = new(v.RequiredInt("id", 1, int.MaxValue)), EmpireId = new(v.RequiredInt("empire", 1, int.MaxValue)), RoleId = v.RequiredString("role"), CarriedPopulation = v.RequiredInt("carried_population", 0, int.MaxValue), StationaryStarId = station is { } a ? new(a) : null, PendingMove = move is { } b ? new(b) : null, PendingFounding = found is { } c ? new(c) : null };
            if (v.RequiredNullableObject("transit") is { } t) { vessel.Transit = new(new(t.RequiredInt("origin", 1, int.MaxValue)), new(t.RequiredInt("destination", 1, int.MaxValue)), t.RequiredInt("total_turns", 1, int.MaxValue), t.RequiredInt("remaining_turns", 1, int.MaxValue)); t.RejectUnknownFields(); }
            vessel.Crisis=CrisisSaveData.ReadVessel(v,s.RulesVersion);
            ScoutSaveData.ReadVessel(v,vessel,s.RulesVersion);
            s.Vessels.Add(vessel);
        });
    }
    internal static void ReadSurveys(EntryReader r, EmpireState e, bool required, int rulesVersion)
    {
        Items(r, "surveys", required, o =>
        {
            var survey = new SystemObservation { StarId = new(o.RequiredInt("star", 1, int.MaxValue)), LastObservedTurn = o.RequiredInt("last_observed_turn", 0, int.MaxValue) };
            Items(o, "planets", true, p =>
            {
                int? colony = p.RequiredNullableInt("colony", 1, int.MaxValue), owner = p.RequiredNullableInt("owner", 1, int.MaxValue);
                survey.Planets.Add(new(new(p.RequiredInt("planet", 1, int.MaxValue)), p.RequiredInt("orbit", 1, int.MaxValue), p.RequiredString("planet_type"), colony is { } c ? new(c) : null, owner is { } x ? new(x) : null) { EnvironmentId = SpeciesSaveData.Read(p, "environment_id", rulesVersion), Appearance = DevelopmentSaveData.ReadAppearance(p, rulesVersion) });
            });
            e.Surveys.Add(survey);
        });
    }
    private static void Items(EntryReader r, string field, bool required, Action<EntryReader> action)
    {
        var array = required ? r.RequiredArray(field) : r.OptionalArray(field);
        if (array is null) return;
        int index = 0;
        foreach (var element in array.Value.EnumerateArray())
        {
            if (r.Nested(element, $"{field}[{index}]") is { } item) { action(item); item.RejectUnknownFields(); }
            index++;
        }
    }
}
