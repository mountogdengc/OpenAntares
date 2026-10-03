using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;

namespace OpenAntares.Simulation.Content;

/// <summary>
/// Reads fields from one JSON object and records a located <see cref="ContentError"/> for anything
/// missing, mistyped, out of range, or unexpected. Failed reads return a placeholder so loading can
/// continue and report every problem at once.
/// </summary>
internal sealed class EntryReader
{
    private readonly JsonElement _object;
    private readonly string _file;
    private readonly string? _entryId;
    private readonly string _fieldPrefix;
    private readonly List<ContentError> _errors;
    private readonly HashSet<string> _readFields = new(StringComparer.Ordinal);

    private EntryReader(JsonElement obj, string file, string? entryId, string fieldPrefix, List<ContentError> errors)
    {
        _object = obj;
        _file = file;
        _entryId = entryId;
        _fieldPrefix = fieldPrefix;
        _errors = errors;
    }

    /// <summary>Creates a reader if <paramref name="element"/> is an object; otherwise records an error.</summary>
    public static EntryReader? Create(JsonElement element, string file, string? entryId, string? field, List<ContentError> errors)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ContentError(file, entryId, field, $"Expected an object, found {Describe(element)}."));
            return null;
        }

        return new EntryReader(element, file, entryId, field is null ? string.Empty : field + ".", errors);
    }

    public string Path(string field) => _fieldPrefix + field;

    public void Error(string? field, string message) =>
        _errors.Add(new ContentError(_file, _entryId, field is null ? null : Path(field), message));

    public string RequiredString(string field)
    {
        if (!TryGet(field, out JsonElement value))
        {
            return string.Empty;
        }

        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { Length: > 0 } text)
        {
            Error(field, $"Expected a non-empty string, found {Describe(value)}.");
            return string.Empty;
        }

        return text;
    }

    public long RequiredLong(string field, long min, long max)
    {
        if (!TryGet(field, out JsonElement value))
        {
            return min;
        }

        return ReadWholeNumber(value, Path(field), min, max);
    }

    public int RequiredInt(string field, int min, int max) => (int)RequiredLong(field, min, max);

    public JsonElement? RequiredArray(string field)
    {
        if (!TryGet(field, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            Error(field, $"Expected an array, found {Describe(value)}.");
            return null;
        }

        return value;
    }

    public EntryReader? RequiredObject(string field)
    {
        if (!TryGet(field, out JsonElement value))
        {
            return null;
        }

        return Nested(value, field);
    }

    /// <summary>Creates a reader for a nested object that reports errors under this entry.</summary>
    public EntryReader? Nested(JsonElement element, string field) =>
        Create(element, _file, _entryId, Path(field), _errors);

    /// <summary>Reads an array of non-empty strings. Duplicates are reported.</summary>
    public ImmutableArray<string> RequiredStringArray(string field)
    {
        if (RequiredArray(field) is not { } array)
        {
            return ImmutableArray<string>.Empty;
        }

        var items = ImmutableArray.CreateBuilder<string>();
        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string itemField = $"{field}[{index}]";
            if (item.ValueKind != JsonValueKind.String || item.GetString() is not { Length: > 0 } text)
            {
                Error(itemField, $"Expected a non-empty string, found {Describe(item)}.");
            }
            else if (items.Contains(text))
            {
                Error(itemField, $"'{text}' is listed more than once.");
            }
            else
            {
                items.Add(text);
            }

            index++;
        }

        return items.ToImmutable();
    }

    /// <summary>Reports fields that were never read (usually typos) and fields given more than once.</summary>
    public void RejectUnknownFields()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in _object.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                Error(property.Name, "Field is given more than once.");
            }
            else if (!_readFields.Contains(property.Name))
            {
                Error(property.Name, "Unknown field.");
            }
        }
    }

    public static long ReadWholeNumber(JsonElement value, string path, long min, long max, Action<string, string> error)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            error(path, $"Expected a whole number, found {Describe(value)}.");
            return min;
        }

        if (!value.TryGetInt64(out long number))
        {
            error(path, $"Expected a whole number, found {value.GetRawText()}. Decimal values are not allowed.");
            return min;
        }

        if (number < min || number > max)
        {
            error(path, $"Value {number} is outside the allowed range {min} to {max}.");
            return min;
        }

        return number;
    }

    private long ReadWholeNumber(JsonElement value, string path, long min, long max) =>
        ReadWholeNumber(value, path, min, max, (p, m) => _errors.Add(new ContentError(_file, _entryId, p, m)));

    private bool TryGet(string field, out JsonElement value)
    {
        _readFields.Add(field);
        if (!_object.TryGetProperty(field, out value))
        {
            Error(field, "Required field is missing.");
            return false;
        }

        return true;
    }

    public static string Describe(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        JsonValueKind.String => $"the string \"{element.GetString()}\"",
        JsonValueKind.Number => $"the number {element.GetRawText()}",
        JsonValueKind.True or JsonValueKind.False => $"the value {element.GetRawText()}",
        JsonValueKind.Null => "null",
        _ => "nothing",
    };
}
