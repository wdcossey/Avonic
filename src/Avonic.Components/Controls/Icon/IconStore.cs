using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avonic.Components.Controls;

/// <summary>
/// Where artwork comes from: <c>addIcons</c>'s map, the bundled ionicons, and sources. Parsed artwork is
/// cached for the life of the app, as <c>ioniconContent</c> is. UI thread only, like the controls.
/// </summary>
internal static partial class IconStore
{
    private const string BundleResource = "Avonic.Components.Icons.ionicons.br";

    private static readonly Dictionary<string, string> s_registered = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SvgArtwork> s_named = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SvgArtwork> s_sources = new(StringComparer.Ordinal);
    private static byte[]? s_bundleBytes;
    private static Dictionary<string, Range>? s_bundleIndex;

    internal static IReadOnlyCollection<string> BundledNames => BundleIndex.Keys;

    /// <summary>
    /// One SVG from the bundle. It's held as bytes and decoded on request, so an app using a few icons
    /// doesn't keep every SVG on the heap as a string.
    /// </summary>
    internal static string? BundledSvg(string name) =>
        BundleIndex.TryGetValue(name, out var range) ? Encoding.UTF8.GetString(s_bundleBytes.AsSpan(range)) : null;

    /// <summary>The bundle is only decompressed when the first icon asks for it.</summary>
    private static Dictionary<string, Range> BundleIndex => s_bundleIndex ??= LoadBundle();

    private static Dictionary<string, Range> LoadBundle()
    {
        using var compressed = typeof(IconStore).Assembly.GetManifestResourceStream(BundleResource)
            ?? throw new InvalidOperationException($"Missing resource {BundleResource}");
        using var brotli = new BrotliStream(compressed, CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        brotli.CopyTo(decompressed);
        s_bundleBytes = decompressed.ToArray();

        var index = new Dictionary<string, Range>(StringComparer.Ordinal);
        var remaining = s_bundleBytes.AsSpan();
        var offset = 0;
        while (!remaining.IsEmpty)
        {
            var end = remaining.IndexOf((byte)'\n');
            var length = end < 0 ? remaining.Length : end;
            var tab = remaining[..length].IndexOf((byte)'\t');
            index[Encoding.ASCII.GetString(remaining[..tab])] = (offset + tab + 1)..(offset + length);

            var consumed = end < 0 ? length : length + 1;
            remaining = remaining[consumed..];
            offset += consumed;
        }

        return index;
    }

    /// <summary><c>addIcons</c>: registers each source, and a camelCase key under its kebab-case name too.</summary>
    public static void AddIcons(IReadOnlyDictionary<string, string> icons)
    {
        foreach (var (name, source) in icons)
        {
            Register(name, source);

            var kebab = ToKebabCase(name);
            if (kebab != name)
            {
                Register(kebab, source);
            }
        }
    }

    private static void Register(string name, string source)
    {
        if (!s_registered.TryGetValue(name, out var existing))
        {
            s_registered[name] = source;
            s_named.Remove(name);
        }
        else if (existing != source)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                typeof(IconStore),
                "Multiple icons were mapped to name \"{Name}\". Ensure that multiple icons are not mapped to the same icon name.",
                name);
        }
    }

    internal static string ToKebabCase(string name) => KebabBoundary().Replace(name, "$1-$2").ToLowerInvariant();

    /// <summary>The expression in <c>addIcons</c>.</summary>
    [GeneratedRegex("([a-z0-9]|(?=[A-Z]))([A-Z0-9])")]
    private static partial Regex KebabBoundary();

    /// <summary><c>getNamedUrl</c>: the registry first, then the bundle. Blank if it's in neither.</summary>
    public static SvgArtwork GetNamed(string name)
    {
        if (s_named.TryGetValue(name, out var artwork))
        {
            return artwork;
        }

        if (s_registered.TryGetValue(name, out var source))
        {
            artwork = GetSource(source);
        }
        else if (BundledSvg(name) is { } svg)
        {
            artwork = SvgArtwork.Parse(svg);
        }
        else
        {
            // Not cached: it may be registered later.
            return SvgArtwork.Empty;
        }

        s_named[name] = artwork;
        return artwork;
    }

    public static SvgArtwork GetSource(string source)
    {
        if (!s_sources.TryGetValue(source, out var artwork))
        {
            artwork = LoadText(source) is { } svg ? SvgArtwork.Parse(svg) : SvgArtwork.Empty;
            s_sources[source] = artwork;
        }

        return artwork;
    }

    /// <summary>Null if it can't be loaded, which is blank, as <c>safeFallback</c> gives.</summary>
    private static string? LoadText(string source)
    {
        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return DecodeDataUrl(source);
        }

        if (!source.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            using var stream = AssetLoader.Open(new Uri(source));
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception e) when (e is FileNotFoundException or UriFormatException or IOException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string? DecodeDataUrl(string url)
    {
        var comma = url.IndexOf(',');
        if (comma < 0)
        {
            return null;
        }

        var meta = url[..comma];
        var data = url[(comma + 1)..];

        if (meta.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(data)));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        // isEncodedDataUrl: ionicons' own ";utf8," urls hold the SVG as it is.
        return meta.EndsWith(";utf8", StringComparison.OrdinalIgnoreCase) ? data : Uri.UnescapeDataString(data);
    }

    /// <summary>Forgets what tests registered, so they can't affect each other.</summary>
    internal static void ResetRegisteredForTests()
    {
        foreach (var name in s_registered.Keys)
        {
            s_named.Remove(name);
        }

        s_registered.Clear();
    }
}
