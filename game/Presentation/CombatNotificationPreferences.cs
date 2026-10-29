using System;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace OpenAntares.Game.Presentation;
public sealed record CombatNotificationPreferences(bool ShowRoutine)
{
    public static CombatNotificationPreferences Load(string path)
    {
        try{if(!File.Exists(path)||new FileInfo(path).Length>4096)return new(true);using var doc=JsonDocument.Parse(File.ReadAllText(path));var r=doc.RootElement;if(r.ValueKind!=JsonValueKind.Object||!r.EnumerateObject().Select(p=>p.Name).OrderBy(s=>s,StringComparer.Ordinal).SequenceEqual(new[]{"format","show_routine","version"})||r.GetProperty("format").GetString()!="OpenAntaresCombatNotifications"||r.GetProperty("version").GetInt32()!=1)return new(true);return new(r.GetProperty("show_routine").GetBoolean());}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArgumentException){return new(true);}
    }
    public void Save(string path)
    {
        string temporary=path+".openantares-tmp";bool created=false;
        try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){created=true;byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{format="OpenAntaresCombatNotifications",version=1,show_routine=ShowRoutine}));stream.Write(bytes);stream.Flush(true);}File.Move(temporary,path,true);created=false;}finally{if(created)File.Delete(temporary);}
    }
}
