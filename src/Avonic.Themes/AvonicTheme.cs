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

    public AvonicTheme(IServiceProvider? _ = null)
    {
        Add(new AvonicComponentsStyles());

        Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Tokens/AvonicTokens.axaml"),
        });
    }
}
