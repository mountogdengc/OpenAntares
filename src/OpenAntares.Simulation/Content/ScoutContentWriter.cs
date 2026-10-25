using System.Text.Json;
namespace OpenAntares.Simulation.Content;

public static partial class ContentWriter
{
    private static void WriteScouting(Utf8JsonWriter w,AmebaDefinition d)
    {
        w.WriteStartObject("space_ameba");w.WriteString("id",d.Id);w.WriteString("name",d.Name);
        w.WriteNumber("pursuit_speed",d.PursuitSpeed);WriteProbability(w,"late_escape",d.LateEscape);WriteProbability(w,"study",d.Study);w.WriteNumber("research_reward",d.ResearchReward);w.WriteEndObject();
    }
    private static void WriteProbability(Utf8JsonWriter w,string name,ProbabilityRule p)
    {
        w.WriteStartObject(name);w.WriteNumber("base_percent",p.BasePercent);w.WriteNumber("relative_speed_coefficient",p.RelativeSpeedCoefficient);w.WriteNumber("minimum_percent",p.MinimumPercent);w.WriteNumber("maximum_percent",p.MaximumPercent);w.WriteEndObject();
    }
}
