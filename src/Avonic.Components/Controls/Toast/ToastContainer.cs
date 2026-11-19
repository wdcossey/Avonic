using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.toast-container</c>: <c>display: flex; align-items: center</c>, and <c>flex-wrap: wrap</c> when <see cref="Wrap"/> (the stacked
/// layout). Its children in order: the start group, the icon, the content and the end group. A child with <see cref="FlexProperty"/>
/// (the content, <c>flex: 1; min-width: 0</c>) has a basis of 0 and takes what its line leaves; one with
/// <see cref="FillsLineProperty"/> (a stacked group, <c>width: 100%</c>) takes a line of its own; the others are as wide as they
/// want. Each child is centred across its line; lines share any height left over, as <c>align-content: normal</c> stretches them.
/// </summary>
/// <remarks>
/// A line too narrow for its children doesn't shrink them, where CSS's <c>flex-shrink: 1</c> would take a button group down to its
/// <c>min-content</c>: a toast's groups are as wide as their buttons in every case measured, and the overflow is clipped, as
/// <c>contain: content</c> clips the container.
/// </remarks>
public class ToastContainer : Panel
{
    /// <summary><c>flex-wrap: wrap</c>.</summary>
    public static readonly StyledProperty<bool> WrapProperty =
        AvaloniaProperty.Register<ToastContainer, bool>(nameof(Wrap));

    /// <summary><c>flex: 1</c> with <c>min-width: 0</c>: no basis, and what the line leaves.</summary>
    public static readonly AttachedProperty<bool> FlexProperty =
        AvaloniaProperty.RegisterAttached<ToastContainer, Control, bool>("Flex");

    /// <summary><c>width: 100%</c>: a line of its own, when the container wraps.</summary>
    public static readonly AttachedProperty<bool> FillsLineProperty =
        AvaloniaProperty.RegisterAttached<ToastContainer, Control, bool>("FillsLine");

    static ToastContainer()
    {
        AffectsMeasure<ToastContainer>(WrapProperty);
        AffectsParentMeasure<ToastContainer>(FlexProperty, FillsLineProperty);
    }

    public bool Wrap { get => GetValue(WrapProperty); set => SetValue(WrapProperty, value); }

    public static bool GetFlex(Control control) => control.GetValue(FlexProperty);

    public static void SetFlex(Control control, bool value) => control.SetValue(FlexProperty, value);

    public static bool GetFillsLine(Control control) => control.GetValue(FillsLineProperty);

    public static void SetFillsLine(Control control, bool value) => control.SetValue(FillsLineProperty, value);

    private readonly List<(List<Control> Items, double Height, double FlexWidth)> _lines = [];

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        _lines.Clear();
        var line = new List<Control>();
        double used = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            double basis;
            if (GetFlex(child))
            {
                basis = child.Margin.Left + child.Margin.Right;
            }
            else if (Wrap && GetFillsLine(child))
            {
                // width: 100%, and its margins on top, so it never shares a line.
                child.Measure(new Size(width, double.PositiveInfinity));
                basis = width + child.Margin.Left + child.Margin.Right;
            }
            else
            {
                child.Measure(Size.Infinity);
                basis = child.DesiredSize.Width;
            }

            if (Wrap && line.Count > 0 && used + basis > width)
            {
                _lines.Add((line, 0, 0));
                line = [];
                used = 0;
            }

            line.Add(child);
            used += basis;
        }

        if (line.Count > 0)
        {
            _lines.Add((line, 0, 0));
        }

        double total = 0;
        for (var i = 0; i < _lines.Count; i++)
        {
            var items = _lines[i].Items;
            var flexCount = items.Count(GetFlex);
            var fixedWidth = items.Where(c => !GetFlex(c)).Sum(c => Wrap && GetFillsLine(c) ? width : c.DesiredSize.Width);
            var flexWidth = flexCount == 0 ? 0 : Math.Max(0, width - fixedWidth) / flexCount;
            foreach (var flex in items.Where(GetFlex))
            {
                flex.Measure(new Size(Math.Max(flexWidth, flex.Margin.Left + flex.Margin.Right), double.PositiveInfinity));
            }

            var height = items.Count == 0 ? 0 : items.Max(c => c.DesiredSize.Height);
            _lines[i] = (items, height, flexWidth);
            total += height;
        }

        return new Size(width, total);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var total = _lines.Sum(l => l.Height);
        var extra = _lines.Count == 0 ? 0 : Math.Max(0, finalSize.Height - total) / _lines.Count;
        double y = 0;
        foreach (var (items, lineHeight, flexWidth) in _lines)
        {
            var height = lineHeight + extra;
            double x = 0;
            foreach (var child in items)
            {
                var childWidth = GetFlex(child) ? Math.Max(flexWidth, child.Margin.Left + child.Margin.Right)
                    : Wrap && GetFillsLine(child) ? finalSize.Width
                    : child.DesiredSize.Width;
                var childHeight = child.DesiredSize.Height;
                child.Arrange(new Rect(x, y + (height - childHeight) / 2, childWidth, childHeight));
                x += childWidth;
            }

            y += height;
        }

        return finalSize;
    }
}
