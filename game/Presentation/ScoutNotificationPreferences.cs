using System;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace OpenAntares.Game.Presentation;
public enum ScoutDisplayLevel { Interrupt,Attention,Digest }
public sealed record ScoutNotificationPreferences(ScoutDisplayLevel Level)
{
    public static ScoutNotificationPreferences Defaults=>new(ScoutDisplayLevel.Interrupt);
    public static ScoutNotificationPreferences Load(string path)
    {
        try
        {
            if(!File.Exists(path))return Defaults;
            if(new FileInfo(path).Length>4096)return Defaults;
            using var doc=JsonDocument.Parse(File.ReadAllText(path));var root=doc.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||!root.EnumerateObject().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(new[]{"format","level","version"})||root.GetProperty("format").GetString()!="OpenAntaresScoutNotifications"||root.GetProperty("version").GetInt32()!=1||!Enum.TryParse<ScoutDisplayLevel>(root.GetProperty("level").GetString(),false,out var level)||!Enum.IsDefined(level))return Defaults;
            return new(level);
        }catch(Exception error)when(error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArgumentException){return Defaults;}
    }
    public void Save(string path)
    {
        if(!Enum.IsDefined(Level))throw new ArgumentException("Unknown scout display level.");
        string text=JsonSerializer.Serialize(new{format="OpenAntaresScoutNotifications",version=1,level=Level.ToString()});string temporary=path+".openantares-tmp";bool created=false;
        try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){created=true;byte[] bytes=System.Text.Encoding.UTF8.GetBytes(text);stream.Write(bytes);stream.Flush(true);}File.Move(temporary,path,true);created=false;}finally{if(created)File.Delete(temporary);}
    }
}
