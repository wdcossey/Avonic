using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Avonic.Themes;

/// <summary>
/// The default Avonic theme. Add to your application's <c>Styles</c> collection
/// to apply all design tokens and control templates.
/// </summary>
/// <example>
/// <code>
/// // In App.axaml:
/// &lt;Application.Styles&gt;
///     &lt;avonic:AvonicTheme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// </example>
public class AvonicTheme : Styles
{
    private static readonly Uri BaseUri = new("avares://Avonic.Themes/");

    public AvonicTheme(IServiceProvider? _ = null)
    {
        Resources.MergedDictionaries.Add(
            new ResourceInclude(BaseUri)
            {
                Source = new Uri("avares://Avonic.Themes/Tokens/AvonicTokens.axaml")
            });

        // Internal base templates — must load before control styles so that
        // embedded native controls (ScrollViewer, TextBox, Button) have templates
        // when Avonic control templates are instantiated.
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Internals/AvonicScrollViewerStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Internals/AvonicTextBoxStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Internals/AvonicButtonBaseStyles.axaml")
        });

        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicRippleStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicButtonStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicLabelStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicItemStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicListStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicListHeaderStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicContentStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicCheckboxStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicToggleStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicRadioStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicRangeStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicInputStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicTextareaStyles.axaml")
        });

        // ── Tier 4 — Composed Patterns ───────────────────────────────────────
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicCardStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicChipStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicBadgeStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicAvatarStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicProgressBarStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicSpinnerStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicSearchbarStyles.axaml")
        });
        Add(new StyleInclude(BaseUri)
        {
            Source = new Uri("avares://Avonic.Themes/Controls/AvonicItemSlidingStyles.axaml")
        });
    }
}
