using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace OpenAntares.Game.Presentation;
public enum CrisisDisplayLevel {Interrupt,Attention,Digest}
public sealed record CrisisNotificationPreferences(CrisisDisplayLevel Critical,NotificationLevel Routine,ImmutableArray<string> Diagnostics)
{
    public static CrisisNotificationPreferences Defaults=>new(CrisisDisplayLevel.Interrupt,NotificationLevel.Summary,ImmutableArray<string>.Empty);
    public static CrisisNotificationPreferences Load(string path)
    {
        try
        {
            if(!File.Exists(path))return Defaults;
            if(new FileInfo(path).Length>16384)throw new ArgumentException("Preference file exceeds16KiB.");
            using var document=JsonDocument.Parse(File.ReadAllText(path));var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||root.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(new[]{"critical","format","routine","version"})==false||root.GetProperty("format").GetString()!="OpenAntaresCrisisNotifications"||root.GetProperty("version").GetInt32()!=1)throw new ArgumentException("Unsupported crisis notification preferences.");
            if(!Enum.TryParse<CrisisDisplayLevel>(root.GetProperty("critical").GetString(),false,out var critical)||!Enum.IsDefined(critical)||!Enum.TryParse<NotificationLevel>(root.GetProperty("routine").GetString(),false,out var routine)||!Enum.IsDefined(routine))throw new ArgumentException("Unknown notification level.");
            return new(critical,routine,ImmutableArray<string>.Empty);
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException){return Defaults with{Diagnostics=ImmutableArray.Create("Crisis preferences could not be read; defaults restored. "+error.Message)};}
    }
    public void Save(string path)
    {
        if(!Enum.IsDefined(Critical)||!Enum.IsDefined(Routine))throw new ArgumentException("Unknown notification level.");
        string text=JsonSerializer.Serialize(new{format="OpenAntaresCrisisNotifications",version=1,critical=Critical.ToString(),routine=Routine.ToString()},new JsonSerializerOptions{WriteIndented=true});
        string temporary=path+".openantares-tmp";bool created=false;
        try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){created=true;byte[] bytes=System.Text.Encoding.UTF8.GetBytes(text);stream.Write(bytes);stream.Flush(true);}File.Move(temporary,path,true);created=false;}finally{if(created)File.Delete(temporary);}
    }
}
