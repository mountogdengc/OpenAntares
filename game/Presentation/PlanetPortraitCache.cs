using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using OpenAntares.Simulation.Appearance;
namespace OpenAntares.Game.Presentation;
public sealed class PlanetPortraitCache
{
    private sealed record Key(PlanetAppearanceDescriptor Descriptor,string Style,int StyleVersion,Vector2I Size){internal string Order=>Descriptor.Version.ToString(CultureInfo.InvariantCulture)+"/"+Descriptor.Variant.ToString(CultureInfo.InvariantCulture)+"/"+Descriptor.SurfaceSeed.ToString(CultureInfo.InvariantCulture)+"/"+Style+"/"+StyleVersion.ToString(CultureInfo.InvariantCulture)+"/"+Size.X.ToString(CultureInfo.InvariantCulture)+"/"+Size.Y.ToString(CultureInfo.InvariantCulture);}
    private sealed record Entry(ImageTexture Texture,long Stamp);
    private readonly Dictionary<Key,Entry> _entries=new();private long _stamp;
    public int Count=>_entries.Count;
    public ImageTexture Get(PlanetAppearanceDescriptor descriptor,PlanetStyle style,Vector2I size)
    {
        var key=new Key(descriptor,style.Id,style.Version,size);if(_stamp==long.MaxValue){long next=0;foreach(var pair in _entries.OrderBy(e=>e.Value.Stamp).ThenBy(e=>e.Key.Order,StringComparer.Ordinal).ToArray())_entries[pair.Key]=pair.Value with{Stamp=++next};_stamp=next;}
        long stamp=++_stamp;if(_entries.TryGetValue(key,out var cached)){_entries[key]=cached with{Stamp=stamp};return cached.Texture;}
        using var image=PlanetPortraitRenderer.Render(descriptor,style,size);var texture=ImageTexture.CreateFromImage(image);
        if(_entries.Count==128){var oldest=_entries.OrderBy(e=>e.Value.Stamp).ThenBy(e=>e.Key.Order,StringComparer.Ordinal).First();_entries.Remove(oldest.Key);}
        _entries.Add(key,new(texture,stamp));return texture;
    }
    // Drop cache ownership; never dispose a texture that may still be bound to a visible control.
    public void Clear(){_entries.Clear();_stamp=0;}
}
