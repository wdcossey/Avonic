using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-header</c>: the toolbars at the top of a page, with md's shadow below them, which <c>Classes="ion-no-border"</c>
/// removes. The first toolbar's top padding is <c>Avonic.SafeAreaTop</c>. It holds many children and Ionic lists no slot, so
/// it's an <see cref="ItemsControl"/>. See <c>docs/components/ion-toolbar/README.md</c>.
/// </summary>
[PseudoClasses(":collapse-condense", ":collapse-fade")]
public class AvonicHeader : ItemsControl
{
    /// <summary>
    /// Ionic <c>collapse</c>. <see cref="HeaderCollapse.Condense"/> is the header an ios page puts in its content for the
    /// collapsible large title, which md hides. <see cref="HeaderCollapse.Fade"/> only does something in ios.
    /// </summary>
    public static readonly StyledProperty<HeaderCollapse?> CollapseProperty =
        AvaloniaProperty.Register<AvonicHeader, HeaderCollapse?>(nameof(Collapse));

    private IDisposable? _noRole;

    static AvonicHeader()
    {
        // role="banner"
        AutomationProperties.LandmarkTypeProperty.OverrideDefaultValue<AvonicHeader>(AutomationLandmarkType.Banner);

        // A TemplatedControl clips by default, which would cut off the shadow.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicHeader>(false);
    }

    public HeaderCollapse? Collapse
    {
        get => GetValue(CollapseProperty);
        set => SetValue(CollapseProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CollapseProperty)
        {
            var condense = Collapse == HeaderCollapse.Condense;
            PseudoClasses.Set(":collapse-condense", condense);

            // header-collapse-fade. Only ios has rules for it, but it's the host class an app can style, as in Ionic.
            PseudoClasses.Set(":collapse-fade", Collapse == HeaderCollapse.Fade);

            // getRoleType: role="none" for a condensed header in md, as its large title is never shown. At style priority,
            // so an app's value wins.
            _noRole?.Dispose();
            _noRole = condense ? SetValue(AutomationProperties.LandmarkTypeProperty, null, BindingPriority.Style) : null;
        }
    }
}
