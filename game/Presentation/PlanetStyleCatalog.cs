using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Godot;
namespace OpenAntares.Game.Presentation;
public sealed record PlanetStyle(string Id,int Version,ImmutableArray<Color> SurfacePalette,Color OceanColor,Color CloudColor,string PatternId)
{
    public float OceanThreshold{get;init;}
    public float CloudCoverage{get;init;}
}
public sealed class PlanetStyleCatalog
{
    private sealed record EnvironmentStyle(Color Ocean,Color Cloud,float Threshold,float CloudCoverage);
    private readonly ImmutableDictionary<string,ImmutableArray<Color>> _palettes;
    private readonly ImmutableDictionary<string,EnvironmentStyle> _environments;
    private readonly ImmutableArray<string> _patterns;
    private PlanetStyleCatalog(ImmutableDictionary<string,ImmutableArray<Color>> palettes,ImmutableDictionary<string,EnvironmentStyle> environments,ImmutableArray<string> patterns){_palettes=palettes;_environments=environments;_patterns=patterns;}
    public static PlanetStyleCatalog Load(string resourcePath)
    {
        using var document=JsonDocument.Parse(Godot.FileAccess.GetFileAsString(resourcePath));var root=document.RootElement;
        if(root.GetProperty("format").GetString()!="OpenAntaresPlanetStyles"||root.GetProperty("version").GetInt32()!=1)throw new ArgumentException("Unsupported planet style catalogue.");
        var palettes=root.GetProperty("palettes").EnumerateObject().ToImmutableDictionary(p=>p.Name,p=>p.Value.EnumerateArray().Select(c=>Color.FromHtml(c.GetString()!)).ToImmutableArray(),StringComparer.Ordinal);
        if(!palettes.ContainsKey("neutral")||palettes.Values.Any(p=>p.Length<2))throw new ArgumentException("Planet styles require at least two colours per palette and a neutral fallback.");
        var environments=root.GetProperty("environments").EnumerateObject().ToImmutableDictionary(p=>p.Name,p=>new EnvironmentStyle(Color.FromHtml(p.Value.GetProperty("ocean_color").GetString()!),Color.FromHtml(p.Value.GetProperty("cloud_color").GetString()!),p.Value.GetProperty("ocean_threshold").GetSingle(),p.Value.GetProperty("cloud_coverage").GetSingle()),StringComparer.Ordinal);
        if(!environments.ContainsKey("neutral")||environments.Values.Any(e=>!float.IsFinite(e.Threshold)||!float.IsFinite(e.CloudCoverage)||e.Threshold<0||e.Threshold>1||e.CloudCoverage<0||e.CloudCoverage>1))throw new ArgumentException("Invalid planet environment style.");
        var patterns=root.GetProperty("patterns").EnumerateArray().Select(p=>p.GetString()!).ToImmutableArray();var supported=new[]{"continents","archipelago","riftlands","crescent","polar","shattered","great_basin","striated"};if(patterns.Length!=8||!patterns.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(supported.OrderBy(x=>x,StringComparer.Ordinal)))throw new ArgumentException("Planet catalogue requires the eight supported structure patterns.");
        return new(palettes,environments,patterns);
    }
    public PlanetStyle Resolve(string planetTypeId,string? environmentId,int variant)
    {
        if(variant is <0 or >7)throw new ArgumentOutOfRangeException(nameof(variant));string type=_palettes.ContainsKey(planetTypeId)?planetTypeId:"neutral",environment=environmentId is {} id&&_environments.ContainsKey(id)?id:"neutral";var env=_environments[environment];
        return new(type+"/"+environment+"/"+variant.ToString(System.Globalization.CultureInfo.InvariantCulture),1,_palettes[type],env.Ocean,env.Cloud,_patterns[variant]){OceanThreshold=env.Threshold,CloudCoverage=env.CloudCoverage};
    }
}
