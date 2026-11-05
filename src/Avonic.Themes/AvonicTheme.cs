using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avonic.Components;

namespace Avonic.Themes;

/// <summary>
/// The Avonic theme: a recreation of Ionic's <c>md</c> mode. Add to <c>Application.Styles</c>.
/// It layers the styled templates and resource keys over <see cref="AvonicComponentsStyles"/>,
/// so consumers never need to add the component stubs separately.
/// </summary>
/// <example>
/// <code>
/// &lt;Application.Styles&gt;
///     &lt;themes:AvonicTheme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// </example>
public class AvonicTheme : Styles
{
    private static readonly Uri BaseUri = new("avares://Avonic.Themes/");

    /// <summary>
    /// <c>Styles/*.axaml</c> are <c>&lt;Styles&gt;</c> files. A <c>ControlTheme</c>'s styles can't reach a
    /// control's slotted content, so rules like <c>::slotted(ion-icon)</c> go here.
    /// </summary>
    private const string StylesFolder = "Styles/";

    public AvonicTheme(IServiceProvider? _ = null)
    {
        Add(new AvonicComponentsStyles());

        foreach (var path in OrderForMerge(XamlManifest.Paths))
        {
            var source = new Uri(BaseUri, path);
            if (path.StartsWith(StylesFolder, StringComparison.Ordinal))
            {
                Add(new StyleInclude(BaseUri) { Source = source });
            }
            else
            {
                Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri) { Source = source });
            }
        }
    }

    /// <summary>Glob order differs between file systems, so merge order is fixed here.</summary>
    internal static IEnumerable<string> OrderForMerge(IEnumerable<string> paths) =>
        paths.Order(StringComparer.Ordinal);
}
