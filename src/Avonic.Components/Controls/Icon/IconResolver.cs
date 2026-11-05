namespace Avonic.Components.Controls;

/// <summary>
/// Ionicons' <c>icon/utils.js</c>: which icon a set of props asks for. Avonic's single skin is md, so
/// <c>mode</c> and <c>ios</c> don't appear.
/// </summary>
internal static class IconResolver
{
    /// <summary><c>getName</c>: the icon's name, or null if the props don't give a valid one.</summary>
    public static string? GetName(string? name, string? icon, string? md)
    {
        string? iconName;
        if (!string.IsNullOrEmpty(md))
        {
            iconName = md;
        }
        else
        {
            iconName = string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(icon) && !IsSrc(icon) ? icon : name;
        }

        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        iconName = iconName.ToLowerInvariant();

        // "only allow alpha characters and dash", which also lets digits through.
        return iconName.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ? iconName : null;
    }

    /// <summary><c>getSrc</c>: the trimmed source, or null if the value isn't one.</summary>
    public static string? GetSrc(string? src)
    {
        var trimmed = src?.Trim();
        return trimmed is not null && IsSrc(trimmed) ? trimmed : null;
    }

    /// <summary><c>isSrc</c>: anything with a slash or a dot is a source, not a name.</summary>
    public static bool IsSrc(string value) => value.Length > 0 && value.AsSpan().IndexOfAny('/', '.') >= 0;
}
