using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avonic.Components.Controls;

/// <summary>
/// What a browser does for an <c>&lt;img src&gt;</c>, for <see cref="AvonicImg"/>: reads the bytes and decodes them, off the UI
/// thread, as <c>decoding="async"</c> does. Anything that fails throws, which is <c>ionError</c>.
/// </summary>
/// <remarks>
/// <list type="table">
/// <item><term><c>http:</c>, <c>https:</c></term><description>One shared <see cref="HttpClient"/>; a status that isn't success fails.</description></item>
/// <item><term><c>data:…;base64,…</c></term><description>Decoded from the URI. Not base64 fails.</description></item>
/// <item><term><c>avares:</c></term><description>An app's own asset, through <see cref="AssetLoader"/>.</description></item>
/// <item><term><c>file:</c>, or a path</term><description>A file. A relative path is relative to the working directory.</description></item>
/// <item><term><c>""</c>, or another scheme</term><description>Fails, as Chromium fails <c>src=""</c>.</description></item>
/// </list>
/// Whatever Avalonia's <see cref="Bitmap"/> decodes loads: PNG, JPEG, WebP, BMP, a GIF's first frame. SVG doesn't.
/// </remarks>
internal static class ImgLoader
{
    /// <summary>
    /// One client for every image, which says what it is and what it takes, as a browser does: some servers turn away a request
    /// with no <c>User-Agent</c>.
    /// </summary>
    private static readonly Lazy<HttpClient> s_http = new(() =>
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Avonic (+https://github.com/wdcossey/Avonic)");
        client.DefaultRequestHeaders.Accept.ParseAdd("image/webp,image/png,image/jpeg,image/*;q=0.8,*/*;q=0.5");
        return client;
    });

    /// <summary>Reads and decodes <paramref name="src"/> on the thread pool, so that nothing of it runs on the caller's thread.</summary>
    public static Task<Bitmap> LoadAsync(string src, CancellationToken cancellationToken) =>
        Task.Run(async () => Decode(await ReadAsync(src, cancellationToken).ConfigureAwait(false)), cancellationToken);

    /// <summary>Decodes the bytes. Tests wrap it to see which thread it runs on.</summary>
    internal static Func<byte[], Bitmap> Decode { get; set; } = static bytes =>
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return new Bitmap(stream);
    };

    private static async Task<byte[]> ReadAsync(string src, CancellationToken cancellationToken)
    {
        if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = src.IndexOf(',');
            if (comma < 0 || !src.AsSpan(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Only a data: URI in base64 is read.");
            }

            return Convert.FromBase64String(Uri.UnescapeDataString(src[(comma + 1)..]));
        }

        // A path is read as it's written: a URI would read %41 in a file's name as A. Only a file: URI is unescaped.
        if (Uri.TryCreate(src, UriKind.Absolute, out var uri)
            && (!uri.IsFile || src.StartsWith("file:", StringComparison.OrdinalIgnoreCase)))
        {
            switch (uri.Scheme)
            {
                case "http":
                case "https":
                {
                    using var response = await s_http.Value
                        .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                }
                case "avares":
                {
                    await using var asset = AssetLoader.Open(uri);
                    using var copy = new MemoryStream();
                    await asset.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
                    return copy.ToArray();
                }
                case "file":
                    return await File.ReadAllBytesAsync(uri.LocalPath, cancellationToken).ConfigureAwait(false);
                default:
                    throw new NotSupportedException($"An image can't be loaded from a {uri.Scheme}: URI.");
            }
        }

        // A path, relative to the working directory. An empty one throws, as Chromium fails src=""
        return await File.ReadAllBytesAsync(src, cancellationToken).ConfigureAwait(false);
    }
}
