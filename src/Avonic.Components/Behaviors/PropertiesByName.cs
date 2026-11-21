using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Logging;

namespace Avonic.Components.Behaviors;

/// <summary>
/// <c>Object.assign(el, props)</c>: an element's properties set from a dictionary keyed by name, for <c>ion-nav</c>'s
/// <c>componentProps</c> and <c>ion-select</c>'s <c>interfaceOptions</c>. Each key is the property's name in Ionic's camelCase
/// or Avonic's PascalCase; a string is converted as XAML would convert it; a key with no property, or a value the property can't
/// take, is ignored, as an unknown property is on an element. What differs between the two is in <see cref="Rules"/>.
/// </summary>
internal static class PropertiesByName
{
    /// <summary>What one caller does that the other doesn't.</summary>
    /// <param name="LogPrefix">What a warning starts with, such as <c>[ion-nav] componentProps</c>.</param>
    public sealed record Rules(string LogPrefix)
    {
        /// <summary>Keys the caller reads itself, and doesn't set; the set's own comparer decides a match.</summary>
        public IReadOnlySet<string>? Skip { get; init; }

        /// <summary>
        /// With no writable Avalonia property of the name, a public settable CLR property of it, found by reflection: a trimmed app
        /// can strip one that no code of its own uses, where an Avalonia property is registered and always found.
        /// </summary>
        public bool ClrProperties { get; init; }

        /// <summary>A value the property can't take is logged, as well as dropped.</summary>
        public bool LogDropped { get; init; }

        /// <summary>
        /// An exception while finding a key's property (Avalonia's registry refuses a dotted name, <c>Grid.Row</c>, with
        /// "Attached properties not supported"; reflection, a name two CLR properties have) is logged and the key skipped. Off, it
        /// leaves <see cref="Apply"/>, and the keys after it aren't set; an exception while setting is logged either way.
        /// </summary>
        public bool LogLookupErrors { get; init; }
    }

    /// <summary>Sets <paramref name="target"/>'s properties from <paramref name="values"/>, in their order, at local priority.</summary>
    public static void Apply(AvaloniaObject target, IReadOnlyDictionary<string, object?>? values, Rules rules)
    {
        if (values is null)
        {
            return;
        }

        foreach (var (key, value) in values)
        {
            if (string.IsNullOrEmpty(key) || rules.Skip?.Contains(key) == true)
            {
                continue;
            }

            if (!rules.LogLookupErrors)
            {
                ApplyOne(target, key, value, rules);
                continue;
            }

            try
            {
                ApplyOne(target, key, value, rules);
            }
            catch (Exception exception)
            {
                Failed(target, key, exception, rules);
            }
        }
    }

    private static void ApplyOne(AvaloniaObject target, string key, object? value, Rules rules)
    {
        var name = char.ToUpperInvariant(key[0]) + key[1..];
        if (AvaloniaPropertyRegistry.Instance.FindRegistered(target, name) is { IsReadOnly: false } property)
        {
            if (TryConvert(value, property.PropertyType, out var converted))
            {
                Set(target, key, rules, () => target.SetValue(property, converted));
            }
            else
            {
                Dropped(target, key, value, rules);
            }

            return;
        }

        if (rules.ClrProperties && FindClrProperty(target.GetType(), name) is { } clr)
        {
            if (TryConvert(value, clr.PropertyType, out var converted))
            {
                Set(target, key, rules, () => clr.SetValue(target, converted));
            }
            else
            {
                Dropped(target, key, value, rules);
            }
        }
    }

    private static void Set(AvaloniaObject target, string key, Rules rules, Action set)
    {
        try
        {
            set();
        }
        catch (Exception exception)
        {
            // A value the property won't take is dropped, as an element ignores what it can't use.
            Failed(target, key, exception, rules);
        }
    }

    private static void Failed(AvaloniaObject target, string key, Exception exception, Rules rules) =>
        Logger.TryGet(LogEventLevel.Warning, "Avonic")?.Log(target, rules.LogPrefix + " {0}: {1}", key, exception.Message);

    private static void Dropped(AvaloniaObject target, string key, object? value, Rules rules)
    {
        if (rules.LogDropped)
        {
            Logger.TryGet(LogEventLevel.Warning, "Avonic")?.Log(target, rules.LogPrefix + " {0}: {1} isn't a value it takes", key, value);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Documented: a trimmed app may strip a CLR property.")]
    private static PropertyInfo? FindClrProperty(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is { CanWrite: true, SetMethod.IsPublic: true } property
            && property.GetIndexParameters().Length == 0 ? property : null;

    /// <summary>
    /// <paramref name="value"/> as a <paramref name="type"/>: as it is if it is one; a string as XAML reads it (an enum by name,
    /// case and hyphens ignored, then the type's <see cref="TypeConverter"/>); a number through <see cref="IConvertible"/>. Null
    /// fits a reference or nullable type. False if none of those gives one, or the conversion throws.
    /// </summary>
    public static bool TryConvert(object? value, Type type, out object? converted)
    {
        converted = value;
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (value is null)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
        }

        if (type.IsInstanceOfType(value) || target.IsInstanceOfType(value))
        {
            return true;
        }

        try
        {
            if (value is string text && target.IsEnum)
            {
                converted = Enum.Parse(target, text.Replace("-", ""), ignoreCase: true);
                return true;
            }

            if (value is string s && TypeDescriptor.GetConverter(target) is { } converter && converter.CanConvertFrom(typeof(string)))
            {
                converted = converter.ConvertFromInvariantString(s);
                return true;
            }

            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(target))
            {
                converted = Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch (Exception)
        {
            // A value that can't be converted is dropped.
        }

        return false;
    }
}
