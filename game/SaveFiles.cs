using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;

namespace OpenAntares.Game;

/// <summary>One save file on disk.</summary>
public sealed record SaveFileInfo(string Name, string Path, DateTime Modified);

/// <summary>Locates save files in the user data folder (<c>user://saves</c>).</summary>
public static class SaveFiles
{
    public const string Extension = ".json";

    public static string Directory
    {
        get
        {
            string directory = ProjectSettings.GlobalizePath("user://saves");
            System.IO.Directory.CreateDirectory(directory);
            return directory;
        }
    }

    /// <summary>Existing saves, newest first.</summary>
    public static IReadOnlyList<SaveFileInfo> List() =>
        new DirectoryInfo(Directory)
            .GetFiles("*" + Extension)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => new SaveFileInfo(System.IO.Path.GetFileNameWithoutExtension(file.Name), file.FullName, file.LastWriteTime))
            .ToList();

    /// <summary>The file path for a save name, keeping only characters that are safe in file names everywhere.</summary>
    public static string PathFor(string name) => System.IO.Path.Combine(Directory, Sanitize(name) + Extension);

    public static string Sanitize(string name)
    {
        var builder = new StringBuilder();
        foreach (char c in name.Trim())
        {
            builder.Append(char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '_');
        }

        string result = builder.ToString().Trim();
        return result.Length == 0 ? "save" : result;
    }

    public static bool Exists(string name) => File.Exists(PathFor(name));
}
