using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-tab-button</c>'s <c>.button-inner</c>, a flex box centred both ways, whose direction is the button's
/// <see cref="Layout"/>: a column for icon-top, icon-hide and label-hide, a reversed column for icon-bottom, a row for icon-start
/// and a reversed row for icon-end. Icons go first, as <c>::slotted(ion-icon) { order: -1 }</c> puts them, and the rest follow in
/// order. A badge is out of the flow, as <c>::slotted(ion-badge) { position: absolute }</c> takes it, at <see cref="BadgeTopProperty"/>
/// and <see cref="BadgeStartProperty"/> of this box, which is its containing block.
/// </summary>
/// <remarks>
/// The attached properties are md's <c>::slotted</c> values for each state, which the button's theme sets on the button. They
/// inherit, so this panel and the button's children read them: <c>Styles/TabButton.axaml</c> has one rule for each kind of child,
/// where a rule for each of md's would cost every element in an app over 20 alternatives. See
/// <c>docs/components/ion-tabs/PLAN.md</c>.
/// </remarks>
public sealed class TabButtonPanel : Panel
{
    public static readonly StyledProperty<TabButtonLayout> LayoutProperty =
        AvaloniaProperty.Register<TabButtonPanel, TabButtonLayout>(nameof(Layout));

    // ── md's ::slotted values, set on the button and inherited ───────────────────────────────

    /// <summary><c>::slotted(ion-icon)</c>'s margin.</summary>
    public static readonly AttachedProperty<Thickness> IconMarginProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, Thickness>("IconMargin", inherits: true);

    /// <summary><c>::slotted(ion-icon)</c>'s font size, which is its size.</summary>
    public static readonly AttachedProperty<double> IconSizeProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, double>("IconSize", double.NaN, inherits: true);

    /// <summary>False for <c>display: none</c> on <c>::slotted(ion-icon)</c>.</summary>
    public static readonly AttachedProperty<bool> IconVisibleProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, bool>("IconVisible", true, inherits: true);

    /// <summary><c>::slotted(ion-label)</c>'s margin.</summary>
    public static readonly AttachedProperty<Thickness> LabelMarginProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, Thickness>("LabelMargin", inherits: true);

    /// <summary>False for <c>display: none</c> on <c>::slotted(ion-label)</c>.</summary>
    public static readonly AttachedProperty<bool> LabelVisibleProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, bool>("LabelVisible", true, inherits: true);

    /// <summary><c>::slotted(ion-label)</c>'s <c>white-space</c>.</summary>
    public static readonly AttachedProperty<TextWrapping> LabelWrappingProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, TextWrapping>("LabelWrapping", TextWrapping.NoWrap, inherits: true);

    /// <summary><c>::slotted(ion-badge)</c>'s <c>top</c>.</summary>
    public static readonly AttachedProperty<double> BadgeTopProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, double>("BadgeTop", inherits: true);

    /// <summary>The percentage in <c>::slotted(ion-badge)</c>'s <c>inset-inline-start</c>, as a fraction of the box's width.</summary>
    public static readonly AttachedProperty<double> BadgeStartProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, double>("BadgeStart", inherits: true);

    /// <summary>What's added to <see cref="BadgeStartProperty"/>, the <c>6px</c> in <c>calc(50% + 6px)</c>.</summary>
    public static readonly AttachedProperty<double> BadgeStartOffsetProperty =
        AvaloniaProperty.RegisterAttached<TabButtonPanel, Control, double>("BadgeStartOffset", inherits: true);

    /// <summary>
    /// <c>::slotted(ion-badge:empty)</c>, for a badge's <c>Content</c>: the first number of the <c>ConverterParameter</c> when it's
    /// empty, as an empty badge is, and the second otherwise. <c>NaN</c> is <c>auto</c>.
    /// </summary>
    public static readonly IValueConverter IfEmpty = new FuncValueConverter<object?, string?, double>((content, parameter) =>
    {
        var values = (parameter ?? "NaN NaN").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return double.Parse(values[content is null or "" ? 0 : 1], CultureInfo.InvariantCulture);
    });

    private List<Control> _flow = [];
    private double[] _widths = [];

    static TabButtonPanel()
    {
        AffectsMeasure<TabButtonPanel>(LayoutProperty);
        AffectsArrange<TabButtonPanel>(BadgeTopProperty, BadgeStartProperty, BadgeStartOffsetProperty);
    }

    public TabButtonLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public static Thickness GetIconMargin(Control control) => control.GetValue(IconMarginProperty);
    public static void SetIconMargin(Control control, Thickness value) => control.SetValue(IconMarginProperty, value);
    public static double GetIconSize(Control control) => control.GetValue(IconSizeProperty);
    public static void SetIconSize(Control control, double value) => control.SetValue(IconSizeProperty, value);
    public static bool GetIconVisible(Control control) => control.GetValue(IconVisibleProperty);
    public static void SetIconVisible(Control control, bool value) => control.SetValue(IconVisibleProperty, value);
    public static Thickness GetLabelMargin(Control control) => control.GetValue(LabelMarginProperty);
    public static void SetLabelMargin(Control control, Thickness value) => control.SetValue(LabelMarginProperty, value);
    public static bool GetLabelVisible(Control control) => control.GetValue(LabelVisibleProperty);
    public static void SetLabelVisible(Control control, bool value) => control.SetValue(LabelVisibleProperty, value);
    public static TextWrapping GetLabelWrapping(Control control) => control.GetValue(LabelWrappingProperty);
    public static void SetLabelWrapping(Control control, TextWrapping value) => control.SetValue(LabelWrappingProperty, value);
    public static double GetBadgeTop(Control control) => control.GetValue(BadgeTopProperty);
    public static void SetBadgeTop(Control control, double value) => control.SetValue(BadgeTopProperty, value);
    public static double GetBadgeStart(Control control) => control.GetValue(BadgeStartProperty);
    public static void SetBadgeStart(Control control, double value) => control.SetValue(BadgeStartProperty, value);
    public static double GetBadgeStartOffset(Control control) => control.GetValue(BadgeStartOffsetProperty);
    public static void SetBadgeStartOffset(Control control, double value) => control.SetValue(BadgeStartOffsetProperty, value);

    private bool IsRow => Layout is TabButtonLayout.IconStart or TabButtonLayout.IconEnd;

    /// <summary><c>position: absolute</c> takes a badge out of the flow.</summary>
    private static bool IsAbsolute(Control child) => child is AvonicBadge;

    private List<Control> Flow()
    {
        var visible = Children.Where(child => child.IsVisible && !IsAbsolute(child)).ToList();
        var ordered = visible.Where(child => child is AvonicIcon).Concat(visible.Where(child => child is not AvonicIcon)).ToList();
        if (Layout is TabButtonLayout.IconEnd or TabButtonLayout.IconBottom)
        {
            ordered.Reverse();
        }

        return ordered;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children.Where(child => child.IsVisible && IsAbsolute(child)))
        {
            child.Measure(Size.Infinity);
        }

        _flow = Flow();
        _widths = new double[_flow.Count];
        if (!IsRow)
        {
            // A column: each as wide as it is, up to the box (max-width: 100%).
            double width = 0, height = 0;
            foreach (var child in _flow)
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }

            return new Size(width, height);
        }

        // A row: each at its width, then what doesn't fit is taken from each in proportion to its width (flex-shrink: 1).
        for (var i = 0; i < _flow.Count; i++)
        {
            _flow[i].Measure(new Size(double.PositiveInfinity, availableSize.Height));
            _widths[i] = _flow[i].DesiredSize.Width;
        }

        var total = _widths.Sum();
        if (total > availableSize.Width && total > 0)
        {
            var over = total - availableSize.Width;
            for (var i = 0; i < _flow.Count; i++)
            {
                _widths[i] = Math.Max(0, _widths[i] - over * _widths[i] / total);
                _flow[i].Measure(new Size(_widths[i], availableSize.Height));
            }
        }

        return new Size(_widths.Sum(), _flow.Count == 0 ? 0 : _flow.Max(child => child.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!IsRow)
        {
            // justify-content: center down the column, align-items: center across it.
            var y = (finalSize.Height - _flow.Sum(child => child.DesiredSize.Height)) / 2;
            foreach (var child in _flow)
            {
                var size = child.DesiredSize;
                var width = Math.Min(size.Width, finalSize.Width);
                child.Arrange(new Rect((finalSize.Width - width) / 2, y, width, size.Height));
                y += size.Height;
            }
        }
        else
        {
            var x = (finalSize.Width - _widths.Sum()) / 2;
            for (var i = 0; i < _flow.Count; i++)
            {
                var height = _flow[i].DesiredSize.Height;
                _flow[i].Arrange(new Rect(x, (finalSize.Height - height) / 2, _widths[i], height));
                x += _widths[i];
            }
        }

        // top and inset-inline-start. In RTL the whole box is mirrored, which puts the start on the right, as CSS's does.
        foreach (var child in Children.Where(child => child.IsVisible && IsAbsolute(child)))
        {
            var x = child.GetValue(BadgeStartProperty) * finalSize.Width + child.GetValue(BadgeStartOffsetProperty);
            child.Arrange(new Rect(new Point(x, child.GetValue(BadgeTopProperty)), child.DesiredSize));
        }

        return finalSize;
    }
}
