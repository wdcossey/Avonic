using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-fab</c>: a floating action button, placed in its container, usually <see cref="AvonicContent.FixedContent"/>,
/// holding an <see cref="AvonicFabButton"/> and the <see cref="AvonicFabList"/>s it opens. The look is in the theme. See
/// <c>docs/components/ion-fab/README.md</c>.
/// </summary>
/// <remarks>
/// Its default slot holds several children and <c>core.json</c> lists no slot, so they're <see cref="ItemsControl.Items"/>. A
/// control is its own container, so the button and the lists are the fab's logical children, as they're its light DOM. They're laid
/// out by <see cref="FabPanel"/>.
/// </remarks>
[PseudoClasses(":horizontal-start", ":horizontal-center", ":horizontal-end", ":vertical-top", ":vertical-center", ":vertical-bottom", ":edge")]
public class AvonicFab : ItemsControl
{
    /// <summary>
    /// Ionic <c>activated</c>: the first fab button shows its close icon and every list opens. The fab changes it itself when one
    /// of its buttons is clicked, so it binds two ways, as a <c>ToggleButton</c>'s <c>IsChecked</c> does.
    /// </summary>
    public static readonly StyledProperty<bool> ActivatedProperty =
        AvaloniaProperty.Register<AvonicFab, bool>(nameof(Activated), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>edge</c>: with <see cref="Vertical"/> top or bottom, half the button over the header or the footer.</summary>
    public static readonly StyledProperty<bool> EdgeProperty =
        AvaloniaProperty.Register<AvonicFab, bool>(nameof(Edge));

    /// <summary>Ionic <c>horizontal</c>. Null leaves the fab where its container puts it.</summary>
    public static readonly StyledProperty<FabHorizontal?> HorizontalProperty =
        AvaloniaProperty.Register<AvonicFab, FabHorizontal?>(nameof(Horizontal));

    /// <summary>Ionic <c>vertical</c>. Null leaves the fab where its container puts it.</summary>
    public static readonly StyledProperty<FabVertical?> VerticalProperty =
        AvaloniaProperty.Register<AvonicFab, FabVertical?>(nameof(Vertical));

    /// <summary>
    /// For the theme: <c>fab.css</c>'s insets as a <c>Margin</c>. The values are <see cref="Horizontal"/>, <see cref="Vertical"/>,
    /// <see cref="Edge"/>, <c>Avonic.SafeAreaLeft</c>, <c>Avonic.SafeAreaRight</c> and the <c>FlowDirection</c>, and the parameter
    /// is the inset, <c>10</c>. Start and end add the safe area on their side, swapped in RTL, as the <c>[dir=rtl]</c> rules do.
    /// Top and bottom add none, and an edge has none.
    /// </summary>
    public static readonly IMultiValueConverter InsetConverter = new FabInsetConverter();

    static AvonicFab()
    {
        // position: absolute; width: fit-content; height: fit-content, with no inset: the container's start, as big as the button.
        // A default, so that AvonicContent's fixed slot, an app or the theme's rules for Horizontal and Vertical can change it.
        HorizontalAlignmentProperty.OverrideDefaultValue<AvonicFab>(HorizontalAlignment.Left);
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicFab>(VerticalAlignment.Top);

        // CSS doesn't clip: an edge button and the lists are outside the fab's box. A TemplatedControl clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicFab>(false);
    }

    public AvonicFab() => UpdateClasses();

    public bool Activated
    {
        get => GetValue(ActivatedProperty);
        set => SetValue(ActivatedProperty, value);
    }

    public bool Edge
    {
        get => GetValue(EdgeProperty);
        set => SetValue(EdgeProperty, value);
    }

    public FabHorizontal? Horizontal
    {
        get => GetValue(HorizontalProperty);
        set => SetValue(HorizontalProperty, value);
    }

    public FabVertical? Vertical
    {
        get => GetValue(VerticalProperty);
        set => SetValue(VerticalProperty, value);
    }

    /// <summary>Ionic <c>close()</c>: closes the lists, as <c>this.activated = false</c> does.</summary>
    public void Close() => SetCurrentValue(ActivatedProperty, false);

    /// <summary>Ionic's internal <c>toggle()</c>, which a fab button calls when it's clicked: only a fab with a list toggles.</summary>
    internal void Toggle()
    {
        if (this.GetLogicalDescendants().OfType<AvonicFabList>().Any())
        {
            SetCurrentValue(ActivatedProperty, !Activated);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActivatedProperty)
        {
            ActivatedChanged();
        }
        else if (change.Property == HorizontalProperty || change.Property == VerticalProperty || change.Property == EdgeProperty)
        {
            UpdateClasses();

            // The edge margins and the lists' offsets are worked out from these.
            ItemsPanelRoot?.InvalidateMeasure();
        }
    }

    /// <summary>
    /// Ionic's <c>componentDidLoad</c>: an <c>activated</c> given in the markup reaches the button and the lists, which a
    /// property set before they were added couldn't.
    /// </summary>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        if (Activated)
        {
            ActivatedChanged();
        }
    }

    /// <summary><c>activatedChanged</c>: <c>querySelector('ion-fab-button')</c>, the first in document order, and every list.</summary>
    private void ActivatedChanged()
    {
        var activated = Activated;
        if (this.GetLogicalDescendants().OfType<AvonicFabButton>().FirstOrDefault() is { } button)
        {
            button.SetCurrentValue(AvonicFabButton.ActivatedProperty, activated);
        }

        foreach (var list in this.GetLogicalDescendants().OfType<AvonicFabList>().ToList())
        {
            list.SetCurrentValue(AvonicFabList.ActivatedProperty, activated);
        }
    }

    private void UpdateClasses()
    {
        var horizontal = Horizontal;
        var vertical = Vertical;
        PseudoClasses.Set(":horizontal-start", horizontal == FabHorizontal.Start);
        PseudoClasses.Set(":horizontal-center", horizontal == FabHorizontal.Center);
        PseudoClasses.Set(":horizontal-end", horizontal == FabHorizontal.End);
        PseudoClasses.Set(":vertical-top", vertical == FabVertical.Top);
        PseudoClasses.Set(":vertical-center", vertical == FabVertical.Center);
        PseudoClasses.Set(":vertical-bottom", vertical == FabVertical.Bottom);
        PseudoClasses.Set(":edge", Edge);
    }

    /// <summary>A control is its own container, as a child in the light DOM is. Anything else is shown in a presenter.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. A fab doesn't: Tab does.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }

    /// <summary>A plain element, where an <see cref="ItemsControl"/>'s own peer would call it a list.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new FabGroupPeer(this);

    internal sealed class FabGroupPeer(Control owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsContentElementCore() => false;

        protected override bool IsControlElementCore() => false;
    }

    private sealed class FabInsetConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [var horizontalValue, var verticalValue, bool edge, double left, double right, FlowDirection flow]
                || parameter is not string text)
            {
                return AvaloniaProperty.UnsetValue;
            }

            var inset = double.Parse(text, CultureInfo.InvariantCulture);
            var horizontal = horizontalValue as FabHorizontal?;
            var vertical = verticalValue as FabVertical?;
            var rtl = flow == FlowDirection.RightToLeft;

            // A Thickness mirrors with the flow, so its first value is the start edge in both directions.
            return new Thickness(
                horizontal == FabHorizontal.Start ? inset + (rtl ? right : left) : 0,
                vertical == FabVertical.Top && !edge ? inset : 0,
                horizontal == FabHorizontal.End ? inset + (rtl ? left : right) : 0,
                vertical == FabVertical.Bottom && !edge ? inset : 0);
        }
    }
}
