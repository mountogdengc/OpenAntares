using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAntares.Simulation.Content;
namespace OpenAntares.Simulation.Diplomacy;
internal static class DiplomacyContentReader
{
    internal static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    internal static DiplomacyDefinition? Read(JsonElement element,string file,List<ContentError> errors)
    {
        try
        {
            if(element.ValueKind!=JsonValueKind.Object)throw new JsonException("A diplomacy object is required.");
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in element.EnumerateObject())if(!names.Add(p.Name))throw new JsonException("Duplicate diplomacy field: "+p.Name);
            return element.Deserialize<DiplomacyDefinition>(Options)??throw new JsonException("A diplomacy object is required.");
        }
        catch(Exception e)when(e is JsonException or NotSupportedException or ArgumentException)
        {errors.Add(new(file,"diplomacy","diplomacy",e.Message));return null;}
    }
    internal static void Validate(DiplomacyDefinition d,string file,List<ContentError> errors)
    {
        void Error(string field,string message)=>errors.Add(new(file,"diplomacy","diplomacy."+field,message));
        if(d.AlgorithmId!="cooperation_v1")Error("algorithm_id","Supported algorithm: cooperation_v1.");
        if(d.SetupCost<1||d.SetupCost>1000000)Error("setup_cost","Expected 1..1000000.");
        if(d.TradeOutput<1||d.TradeOutput>1000000)Error("trade_output","Expected 1..1000000.");
        if(d.Warmup<1||d.Warmup>1000)Error("warmup","Expected 1..1000.");
        if(d.Minimum<1||d.Minimum>1000)Error("minimum","Expected 1..1000.");
        if(d.Notice<1||d.Notice>1000)Error("notice","Expected 1..1000.");
        if(d.OfferLifetime<1||d.OfferLifetime>1000)Error("offer_lifetime","Expected 1..1000.");
        if(d.Cooldown<1||d.Cooldown>1000)Error("cooldown","Expected 1..1000.");
        if(d.StreakCap<1||d.StreakCap>1000)Error("streak_cap","Expected 1..1000.");
        if(d.StreakStep<0||d.StreakStep>1000)Error("streak_step","Expected 0..1000.");
        if(d.CancellationPenalty<0||d.CancellationPenalty>1000)Error("cancellation_penalty","Expected 0..1000.");
        if(d.MemoryLifetime<1||d.MemoryLifetime>1000)Error("memory_lifetime","Expected 1..1000.");
        if(d.AccordThreshold<0||d.AccordThreshold>1000)Error("accord_threshold","Expected 0..1000.");
        if(d.TradeThreshold<0||d.TradeThreshold>1000)Error("trade_threshold","Expected 0..1000.");
        long maxScore=checked((long)d.StreakCap*d.StreakStep);
        if(d.AccordThreshold>maxScore)Error("accord_threshold","Threshold cannot exceed maximum cooperation score.");
        if(d.TradeThreshold>maxScore)Error("trade_threshold","Threshold cannot exceed maximum cooperation score.");
        if(d.TradeThreshold<d.AccordThreshold)Error("trade_threshold","Trade threshold must be at least the accord threshold.");
    }
}
