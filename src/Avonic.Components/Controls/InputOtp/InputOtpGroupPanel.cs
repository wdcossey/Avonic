using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-input-otp</c>'s <c>.input-otp-group</c>: <c>display: flex; align-items: center; justify-content: center; gap</c>, with the
/// group's padding. Each child's flex basis is <see cref="FlexBasisProperty"/> when it's set, as a box's <c>width: var(--width)</c>,
/// and its desired width otherwise. When the row is too narrow, children shrink in proportion to their basis down to their
/// <c>MinWidth</c>, as <c>flex-shrink: 1</c> and <c>min-width</c> do, except an <see cref="InputOtpSeparator"/>, which has
/// <c>flex-shrink: 0</c>. A row that still doesn't fit overflows both sides, as <c>justify-content: center</c> does. Hidden children
/// take no room and no gap. Avalonia mirrors it in RTL, as it does any panel.
/// </summary>
public sealed class InputOtpGroupPanel : Panel
{
    /// <summary>CSS <c>gap</c>.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<InputOtpGroupPanel, double>(nameof(Spacing));

    /// <summary>The group's padding.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<InputOtpGroupPanel>();

    /// <summary>
    /// A child's flex basis, as CSS <c>width</c>, which, unlike Avalonia's <c>Width</c>, still lets it shrink. Unset (NaN), the
    /// basis is the child's desired width.
    /// </summary>
    public static readonly AttachedProperty<double> FlexBasisProperty =
        AvaloniaProperty.RegisterAttached<InputOtpGroupPanel, Control, double>("FlexBasis", double.NaN);

    static InputOtpGroupPanel()
    {
        AffectsMeasure<InputOtpGroupPanel>(SpacingProperty, PaddingProperty);
        AffectsParentMeasure<InputOtpGroupPanel>(FlexBasisProperty);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public static double GetFlexBasis(Control control) => control.GetValue(FlexBasisProperty);

    public static void SetFlexBasis(Control control, double value) => control.SetValue(FlexBasisProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var inner = availableSize.Deflate(Padding);
        var items = Visible();
        var total = Gaps(items.Count);
        var height = 0.0;
        foreach (var child in items)
        {
            child.Measure(new Size(double.PositiveInfinity, inner.Height));
            total += Basis(child);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        // A block's width is what it's given; with none, the row's own.
        var width = double.IsFinite(inner.Width) ? Math.Min(total, inner.Width) : total;
        return new Size(width + Padding.Left + Padding.Right, height + Padding.Top + Padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var inner = new Rect(finalSize).Deflate(Padding);
        var items = Visible();
        var sizes = Resolve(items, inner.Width);
        var used = sizes.Sum() + Gaps(items.Count);

        // justify-content: center, which overflows both sides when the row doesn't fit.
        var x = inner.X + (inner.Width - used) / 2;
        for (var i = 0; i < items.Count; i++)
        {
            var child = items[i];
            var height = child.DesiredSize.Height;

            // align-items: center
            child.Arrange(new Rect(x, inner.Y + (inner.Height - height) / 2, sizes[i], height));
            x += sizes[i] + Spacing;
        }

        return finalSize;
    }

    private List<Control> Visible() => Children.Where(child => child.IsVisible).ToList();

    private double Gaps(int count) => count > 1 ? Spacing * (count - 1) : 0;

    /// <summary>The flex basis, clamped by the minimum and maximum widths, as the hypothetical main size.</summary>
    private static double Basis(Control child)
    {
        var basis = GetFlexBasis(child);
        if (!double.IsFinite(basis))
        {
            return child.DesiredSize.Width;
        }

        return Math.Max(child.MinWidth, Math.Min(child.MaxWidth, basis)) + child.Margin.Left + child.Margin.Right;
    }

    /// <summary>The flexbox spec's section 9.7, for shrinking only: nothing here grows.</summary>
    private double[] Resolve(List<Control> items, double width)
    {
        var bases = items.Select(Basis).ToArray();
        var sizes = bases.ToArray();
        var free = width - bases.Sum() - Gaps(items.Count);
        if (!double.IsFinite(width) || free >= 0)
        {
            return sizes;
        }

        // A separator has flex-shrink: 0, and nothing shrinks below its minimum.
        var frozen = items.Select((child, i) => child is InputOtpSeparator || bases[i] <= Minimum(child)).ToArray();
        while (frozen.Contains(false))
        {
            var remaining = width - Gaps(items.Count);
            var scaled = 0.0;
            for (var i = 0; i < items.Count; i++)
            {
                remaining -= frozen[i] ? sizes[i] : bases[i];
                scaled += frozen[i] ? 0 : bases[i];
            }

            var violation = 0.0;
            var clamped = new bool[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                if (frozen[i])
                {
                    continue;
                }

                var target = bases[i] + (scaled > 0 ? remaining * bases[i] / scaled : 0);
                var minimum = Minimum(items[i]);
                if (target < minimum)
                {
                    violation += minimum - target;
                    target = minimum;
                    clamped[i] = true;
                }

                sizes[i] = target;
            }

            for (var i = 0; i < items.Count; i++)
            {
                // With no violation every item is done; with a minimum violated, only those that hit it.
                if (!frozen[i] && (violation == 0 || clamped[i]))
                {
                    frozen[i] = true;
                }
            }
        }

        return sizes;
    }

    private static double Minimum(Control child) => child.MinWidth + child.Margin.Left + child.Margin.Right;
}
