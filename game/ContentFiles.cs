using System.Linq;
using Godot;
using OpenAntares.Simulation.Content;

namespace OpenAntares.Game;

/// <summary>Reads content JSON through Godot's file API, so it works from <c>res://</c> in editor and exported builds.</summary>
public static class ContentFiles
{
    public const string CoreDirectory = "res://content/core";

    public static ContentLoadResult LoadCore()
    {
        string[] files = DirAccess.GetFilesAt(CoreDirectory)
            .Where(name => name.EndsWith(".json", System.StringComparison.Ordinal))
            .ToArray();

        return ContentLoader.Load(files.Select(name =>
            new ContentSource(name, FileAccess.GetFileAsString($"{CoreDirectory}/{name}"))));
    }
}
