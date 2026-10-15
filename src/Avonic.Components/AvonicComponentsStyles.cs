using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Avonic.Components;

/// <summary>
/// The functional stub templates for every Avonic control — structure only, no visual opinion.
/// <c>Avonic.Themes.AvonicTheme</c> includes this automatically; add it yourself only when
/// using <c>Avonic.Components</c> with your own templates.
/// </summary>
public class AvonicComponentsStyles : Styles
{
    private static readonly Uri BaseUri = new("avares://Avonic.Components/");

    public AvonicComponentsStyles(IServiceProvider? _ = null)
    {
        Include("Controls/RippleEffect/AvonicRippleEffect.axaml");
    }

    private void Include(string path) =>
        Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri) { Source = new Uri(BaseUri, path) });
}
