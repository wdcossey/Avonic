using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Avonic.Components.Controls;

/// <summary>
/// The data an <see cref="AvonicAlert"/>'s button dismisses it with: Ionic's <c>{ values, ...returnData }</c>, a dictionary with
/// the <c>values</c> key and whatever the button's handler returned beside it. <see cref="Values"/> reads the first; the
/// dictionary's own collection of values is its <see cref="IReadOnlyDictionary{TKey,TValue}"/> interface's.
/// </summary>
public sealed class AlertDismissData : IReadOnlyDictionary<string, object?>
{
    /// <summary>Ionic's key for the values.</summary>
    public const string ValuesKey = "values";

    private readonly Dictionary<string, object?> _entries = [];

    internal AlertDismissData(object? values) => _entries[ValuesKey] = values;

    /// <summary>
    /// The alert's values: null with no inputs; a radio alert's checked value; a checkbox alert's checked values (an
    /// <c>object?[]</c>); a text alert's <c>Dictionary&lt;string, object?&gt;</c> of name to value.
    /// </summary>
    public object? Values => _entries.GetValueOrDefault(ValuesKey);

    public int Count => _entries.Count;

    public IEnumerable<string> Keys => _entries.Keys;

    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => _entries.Values;

    public object? this[string key]
    {
        get => _entries[key];
        internal set => _entries[key] = value;
    }

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value) => _entries.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => "{" + string.Join(", ", _entries.Select(e => $"{e.Key}: {Show(e.Value)}")) + "}";

    private static string Show(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        IDictionary<string, object?> d => "{" + string.Join(", ", d.Select(e => $"{e.Key}: {Show(e.Value)}")) + "}",
        IEnumerable e => "[" + string.Join(", ", e.Cast<object?>().Select(Show)) + "]",
        _ => value.ToString() ?? "",
    };
}
