using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace OpenAntares.Game.Presentation;
public enum NotificationLevel {Summary,Attention,Off}
public sealed record ColonyNotificationPreferences(ImmutableSortedDictionary<string,NotificationLevel> Levels,ImmutableArray<string> Diagnostics)
{
    public static ColonyNotificationPreferences Defaults=>new(ImmutableSortedDictionary<string,NotificationLevel>.Empty.WithComparers(StringComparer.Ordinal).Add("governor_actions",NotificationLevel.Summary).Add("completions",NotificationLevel.Summary).Add("support_shortage",NotificationLevel.Attention).Add("construction_locked",NotificationLevel.Attention).Add("embarkation_wait",NotificationLevel.Attention),ImmutableArray<string>.Empty);
    public NotificationLevel LevelFor(string category)=>Levels.TryGetValue(category,out var level)?level:NotificationLevel.Off;
    public ColonyNotificationPreferences WithLevel(string category,NotificationLevel level){if(!Defaults.Levels.ContainsKey(category)||!Enum.IsDefined(level))throw new ArgumentException("Unknown notification category or level.");return this with{Levels=Levels.SetItem(category,level),Diagnostics=ImmutableArray<string>.Empty};}
    public static ColonyNotificationPreferences Load(string path)
    {
        try {
            if(!File.Exists(path))return Defaults with{Diagnostics=ImmutableArray.Create("No preferences saved; using defaults.")};
            if(new FileInfo(path).Length>16384)throw new ArgumentException("Preferences exceed16KiB.");
            using var document=JsonDocument.Parse(File.ReadAllText(path));var root=document.RootElement;
            Fields(root,new[]{"format","version","levels"});if(root.GetProperty("format").GetString()!="OpenAntaresColonyNotifications"||(!root.GetProperty("version").TryGetInt32(out int version)||version!=1))throw new ArgumentException("Unsupported preference format.");
            var levels=root.GetProperty("levels");Fields(levels,Defaults.Levels.Keys.ToArray());var result=Defaults;
            foreach(var pair in levels.EnumerateObject()){if(!Enum.GetNames<NotificationLevel>().Contains(pair.Value.GetString(),StringComparer.Ordinal)||!Enum.TryParse<NotificationLevel>(pair.Value.GetString(),false,out var level))throw new ArgumentException("Unknown notification level.");result=result.WithLevel(pair.Name,level);}return result;
        }catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException){return Defaults with{Diagnostics=ImmutableArray.Create("Preferences could not be read; defaults restored. "+ex.Message)};}
    }
    public void Save(string path)
    {
        string text=JsonSerializer.Serialize(new{format="OpenAntaresColonyNotifications",version=1,levels=Levels.ToDictionary(p=>p.Key,p=>p.Value.ToString())},new JsonSerializerOptions{WriteIndented=true});
        string temporary=path+".openantares-tmp";bool created=false;
        try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){created=true;byte[] bytes=System.Text.Encoding.UTF8.GetBytes(text);stream.Write(bytes);stream.Flush(true);}File.Move(temporary,path,true);created=false;}finally{if(created)File.Delete(temporary);}
    }
    private static void Fields(JsonElement obj,string[] names){if(obj.ValueKind!=JsonValueKind.Object)throw new ArgumentException("Expected preference object.");var fields=obj.EnumerateObject().Select(p=>p.Name).ToArray();if(fields.Length!=names.Length||fields.Distinct(StringComparer.Ordinal).Count()!=fields.Length||fields.Any(f=>!names.Contains(f,StringComparer.Ordinal)))throw new ArgumentException("Missing, duplicate or unknown preference field.");}
}
