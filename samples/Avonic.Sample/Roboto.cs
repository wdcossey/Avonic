using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Avonic.Sample;

/// <summary>
/// Makes the plain family name <c>Roboto</c> resolve to the files in <c>tools/fonts/roboto/</c>, which this
/// assembly embeds, so that the theme's <c>Avonic.FontFamily</c> (<c>Roboto, Helvetica Neue, sans-serif</c>)
/// finds them unchanged. The sample and the tests use it, and the Chromium tools load the same files with
/// <c>@font-face</c>. The libraries don't ship Roboto, as Ionic doesn't.
/// </summary>
public static class Roboto
{
    /// <summary>The collection's key, for <c>fonts:Avonic.Roboto#Roboto</c>.</summary>
    public static readonly Uri CollectionKey = new("fonts:Avonic.Roboto", UriKind.Absolute);

    /// <summary>Where the files are embedded in this assembly.</summary>
    public static readonly Uri Source = new("avares://Avonic.Sample/Fonts/Roboto", UriKind.Absolute);

    /// <summary>
    /// Registers the files as a font collection and maps <c>Roboto</c> to it. In Avalonia 12.1 a mapping is
    /// applied before the system's fonts are asked, so an installed Roboto doesn't win over these files.
    /// It's also the default family, as Ionic sets its font on <c>html</c>: text outside Avonic's controls, such
    /// as a plain <c>TextBlock</c>, is Roboto too, and measures the same on every machine.
    /// This replaces any <see cref="FontManagerOptions"/> the app set before.
    /// </summary>
    public static AppBuilder WithRoboto(this AppBuilder builder) =>
        builder
            .With(new FontManagerOptions
            {
                DefaultFamilyName = CollectionKey + "#Roboto",
                FontFamilyMappings = new Dictionary<string, FontFamily>
                {
                    ["Roboto"] = new FontFamily(CollectionKey + "#Roboto"),
                },
            })
            .ConfigureFonts(fonts => fonts.AddFontCollection(new EmbeddedFontCollection(CollectionKey, Source)));
}
