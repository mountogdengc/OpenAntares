using System.Collections.Generic;

namespace OpenAntares.Simulation.Content;

/// <summary>
/// A problem found while loading content, located as precisely as possible: the file, the entry
/// ID (when the problem belongs to one entry), and the field path within it.
/// </summary>
public sealed record ContentError(string File, string? EntryId, string? Field, string Message)
{
    public override string ToString()
    {
        string location = File;
        if (EntryId is not null)
        {
            location += $", entry '{EntryId}'";
        }

        if (Field is not null)
        {
            location += $", field '{Field}'";
        }

        return $"{location}: {Message}";
    }
}

/// <summary>The outcome of loading content. <see cref="Content"/> is set only when there are no errors.</summary>
public sealed record ContentLoadResult(ContentSet? Content, IReadOnlyList<ContentError> Errors)
{
    public bool Succeeded => Content is not null;
}

/// <summary>One content file's name and JSON text. The name is used to order files and to locate errors.</summary>
public sealed record ContentSource(string Name, string Json);
