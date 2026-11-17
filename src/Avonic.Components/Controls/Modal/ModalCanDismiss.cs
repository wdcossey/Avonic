using System.ComponentModel;
using System.Globalization;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>canDismiss</c>: <c>boolean | ((data?: any, role?: string) =&gt; Promise&lt;boolean&gt;)</c>, which has no name of its
/// own in Ionic. A <see cref="bool"/> or a handler converts to it, and XAML's <c>True</c> and <c>False</c> do. The default is
/// <see cref="True"/>.
/// </summary>
[TypeConverter(typeof(ModalCanDismissConverter))]
public readonly struct ModalCanDismiss : IEquatable<ModalCanDismiss>
{
    // Stored as "blocked", so that default(ModalCanDismiss) is Ionic's default, true.
    private readonly bool _blocked;

    public ModalCanDismiss(bool value)
    {
        _blocked = !value;
        Handler = null;
    }

    public ModalCanDismiss(Func<object?, string?, Task<bool>> handler)
    {
        _blocked = false;
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>Ionic's default: it can always dismiss.</summary>
    public static ModalCanDismiss True => new(true);

    /// <summary>It never dismisses, except through the <c>handler</c> role.</summary>
    public static ModalCanDismiss False => new(false);

    /// <summary>The function, called with the dismiss's data and role, or null for a boolean.</summary>
    public Func<object?, string?, Task<bool>>? Handler { get; }

    /// <summary>Ionic's <c>canDismiss === true</c>: neither false nor a function.</summary>
    public bool IsTrue => Handler is null && !_blocked;

    public static implicit operator ModalCanDismiss(bool value) => new(value);

    public static implicit operator ModalCanDismiss(Func<object?, string?, Task<bool>> handler) => new(handler);

    /// <summary>Ionic's <c>checkCanDismiss</c>: the boolean, or the handler's answer.</summary>
    public Task<bool> Check(object? data, string? role) => Handler is { } handler ? handler(data, role) : Task.FromResult(!_blocked);

    public bool Equals(ModalCanDismiss other) => _blocked == other._blocked && Equals(Handler, other.Handler);

    public override bool Equals(object? obj) => obj is ModalCanDismiss other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_blocked, Handler);

    public static bool operator ==(ModalCanDismiss left, ModalCanDismiss right) => left.Equals(right);

    public static bool operator !=(ModalCanDismiss left, ModalCanDismiss right) => !left.Equals(right);

    public override string ToString() => Handler is not null ? "handler" : IsTrue ? "true" : "false";
}

/// <summary>Reads <c>true</c> and <c>false</c>, in any case, as XAML gives them.</summary>
public sealed class ModalCanDismissConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || sourceType == typeof(bool) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch
    {
        bool b => new ModalCanDismiss(b),
        string s when bool.TryParse(s.Trim(), out var b) => new ModalCanDismiss(b),
        _ => base.ConvertFrom(context, culture, value),
    };
}
