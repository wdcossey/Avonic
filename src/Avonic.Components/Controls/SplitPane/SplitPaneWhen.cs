using System.ComponentModel;
using System.Globalization;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>when</c> on <c>ion-split-pane</c>: <c>boolean | string</c>, which has no name of its own in Ionic. A boolean shows the
/// split pane or not; a string is a shortcut, <c>xs</c>, <c>sm</c>, <c>md</c>, <c>lg</c>, <c>xl</c> or <c>never</c>, or a CSS media
/// query, which shows it while it matches the window. A <see cref="bool"/> or a <see cref="string"/> converts to it, and so do XAML's
/// <c>True</c> and <c>False</c>, in any case, as booleans, as a framework's binding passes them (Ionic's own HTML attribute
/// <c>when="true"</c> is a string, a media query that matches nothing). The default is <see cref="Default"/>, Ionic's
/// <c>(min-width: 992px)</c>.
/// </summary>
[TypeConverter(typeof(SplitPaneWhenConverter))]
public readonly struct SplitPaneWhen : IEquatable<SplitPaneWhen>
{
    /// <summary>Ionic's default, <c>QUERY['lg']</c>.</summary>
    public const string DefaultQuery = "(min-width: 992px)";

    private readonly string? _query;
    private readonly bool _isBoolean;
    private readonly bool _value;

    public SplitPaneWhen(bool value)
    {
        _isBoolean = true;
        _value = value;
        _query = null;
    }

    public SplitPaneWhen(string? query)
    {
        _isBoolean = false;
        _value = false;
        _query = query;
    }

    /// <summary>Ionic's default: <c>(min-width: 992px)</c>.</summary>
    public static SplitPaneWhen Default => new(DefaultQuery);

    /// <summary>Whether it's a boolean, rather than a string.</summary>
    public bool IsBoolean => _isBoolean;

    /// <summary>The boolean, or false for a string.</summary>
    public bool Value => _value;

    /// <summary>The string: a shortcut or a media query; null for a boolean, or a null string.</summary>
    public string? Query => _query;

    public static implicit operator SplitPaneWhen(bool value) => new(value);

    public static implicit operator SplitPaneWhen(string? query) => new(query);

    public bool Equals(SplitPaneWhen other) =>
        _isBoolean == other._isBoolean && _value == other._value && string.Equals(_query, other._query, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is SplitPaneWhen other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_isBoolean, _value, _query);

    public static bool operator ==(SplitPaneWhen left, SplitPaneWhen right) => left.Equals(right);

    public static bool operator !=(SplitPaneWhen left, SplitPaneWhen right) => !left.Equals(right);

    public override string ToString() => _isBoolean ? (_value ? "true" : "false") : _query ?? string.Empty;
}

/// <summary>Reads <c>true</c> and <c>false</c>, in any case, as booleans, and any other text as a shortcut or a media query.</summary>
public sealed class SplitPaneWhenConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || sourceType == typeof(bool) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch
    {
        bool b => new SplitPaneWhen(b),
        string s when bool.TryParse(s.Trim(), out var b) => new SplitPaneWhen(b),
        string s => new SplitPaneWhen(s),
        _ => base.ConvertFrom(context, culture, value),
    };
}
