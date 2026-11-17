using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The action sheet's host as <c>action-sheet.css</c> places <c>.action-sheet-wrapper</c>: <c>position: absolute</c> with
/// <c>left</c>, <c>right</c> and <c>bottom</c> 0, so at the bottom; <c>--width</c> within <c>--min-width</c> and <c>--max-width</c>,
/// centred by <c>margin-inline: auto</c>; <c>--height</c> (<c>auto</c>: its content) within <c>--min-height</c> and
/// <c>--max-height</c>. Each size is a number (px) or a CSS string, read as <see cref="AlertPanel"/> reads one: px, a percentage of
/// the host, <c>vw</c> and <c>vh</c>, sums and differences, <c>calc()</c>, <c>min()</c>, or <c>auto</c> and <c>none</c>. The
/// maximum applies and then the minimum, which wins, as in CSS.
/// </summary>
public class ActionSheetPanel : Panel
{
    public static readonly StyledProperty<object?> ContentWidthProperty =
        AvaloniaProperty.Register<ActionSheetPanel, object?>(nameof(ContentWidth));

    public static readonly StyledProperty<object?> ContentMinWidthProperty =
        AvaloniaProperty.Register<ActionSheetPanel, object?>(nameof(ContentMinWidth));

    public static readonly StyledProperty<object?> ContentMaxWidthProperty =
        AvaloniaProperty.Register<ActionSheetPanel, object?>(nameof(ContentMaxWidth));

    public static readonly StyledProperty<object?> ContentHeightProperty =
        AvaloniaProperty.Register<ActionSheetPanel, object?>(nameof(ContentHeight));

    public static readonly StyledProperty<object?> ContentMinHeightProperty =
        AvaloniaProperty.Register<ActionSheetPanel, object?>(nameof(ContentMinHeight));

    public static readonly StyledProperty<object?> ContentMaxHeightProperty =
        AvaloniaProperty.Register<ActionSheetPanel, object?>(nameof(ContentMaxHeight));

    /// <summary>
    /// <c>--max-height</c>'s md default, <c>calc(100% - (var(--ion-safe-area-top) + var(--ion-safe-area-bottom)))</c>: the values
    /// are the sheet's own key, which wins when it's set, then the top and bottom safe areas.
    /// </summary>
    public static readonly IMultiValueConverter MaxHeightConverter = new FuncMultiValueConverter<object?, object?>(values =>
    {
        var list = values.ToList();
        if (list is [var own, ..] && Css.IsSet(own) && own is not null)
        {
            return own;
        }

        var top = list.ElementAtOrDefault(1) is double t ? t : 0;
        var bottom = list.ElementAtOrDefault(2) is double b ? b : 0;
        return string.Create(CultureInfo.InvariantCulture, $"calc(100% - {top + bottom}px)");
    });

    private Size _content;

    static ActionSheetPanel()
    {
        AffectsMeasure<ActionSheetPanel>(ContentWidthProperty, ContentMinWidthProperty, ContentMaxWidthProperty, ContentHeightProperty,
            ContentMinHeightProperty, ContentMaxHeightProperty);
    }

    /// <summary><c>--width</c>.</summary>
    public object? ContentWidth { get => GetValue(ContentWidthProperty); set => SetValue(ContentWidthProperty, value); }

    /// <summary><c>--min-width</c>.</summary>
    public object? ContentMinWidth { get => GetValue(ContentMinWidthProperty); set => SetValue(ContentMinWidthProperty, value); }

    /// <summary><c>--max-width</c>.</summary>
    public object? ContentMaxWidth { get => GetValue(ContentMaxWidthProperty); set => SetValue(ContentMaxWidthProperty, value); }

    /// <summary><c>--height</c>.</summary>
    public object? ContentHeight { get => GetValue(ContentHeightProperty); set => SetValue(ContentHeightProperty, value); }

    /// <summary><c>--min-height</c>.</summary>
    public object? ContentMinHeight { get => GetValue(ContentMinHeightProperty); set => SetValue(ContentMinHeightProperty, value); }

    /// <summary><c>--max-height</c>.</summary>
    public object? ContentMaxHeight { get => GetValue(ContentMaxHeightProperty); set => SetValue(ContentMaxHeightProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var height = availableSize.Height;
        var minWidth = AlertPanel.Length(ContentMinWidth, width, availableSize) ?? 0;
        var maxWidth = AlertPanel.Length(ContentMaxWidth, width, availableSize) ?? double.PositiveInfinity;
        var minHeight = AlertPanel.Length(ContentMinHeight, height, availableSize) ?? 0;
        var maxHeight = AlertPanel.Length(ContentMaxHeight, height, availableSize) ?? double.PositiveInfinity;

        // width: auto on an element with left and right 0 is what's between them.
        var w = Clamp(AlertPanel.Length(ContentWidth, width, availableSize) ?? width, minWidth, maxWidth);
        if (double.IsInfinity(w))
        {
            w = 0;
        }

        var desired = default(Size);
        foreach (var child in Children)
        {
            double h;
            if (AlertPanel.Length(ContentHeight, height, availableSize) is { } fixedHeight)
            {
                h = Clamp(fixedHeight, minHeight, maxHeight);
            }
            else
            {
                child.Measure(new Size(w, double.PositiveInfinity));
                h = Clamp(child.DesiredSize.Height, minHeight, maxHeight);
            }

            child.Measure(new Size(w, h));
            desired = new Size(Math.Max(desired.Width, w), Math.Max(desired.Height, h));
        }

        _content = desired;
        return new Size(double.IsInfinity(width) ? desired.Width : width, double.IsInfinity(height) ? desired.Height : height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // margin-inline: auto centres it; when it's wider than the host, the start margin is 0, as CSS resolves an
        // over-constrained box.
        var rect = new Rect(
            Math.Max(0, (finalSize.Width - _content.Width) / 2),
            finalSize.Height - _content.Height,
            _content.Width,
            _content.Height);
        foreach (var child in Children)
        {
            child.Arrange(rect);
        }

        return finalSize;
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}

/// <summary>
/// <c>.action-sheet-container</c>: <c>display: flex; flex-flow: column; justify-content: flex-end</c>, its children one under
/// another, as wide as it is, at its end. When they're taller than it may be, a child marked <see cref="ShrinksProperty"/> (the
/// group, <c>flex-shrink: 2</c> with <c>overflow-y: auto</c>, so a minimum of 0) gives up the difference, in proportion to its
/// height when there are several, and scrolls; the others (the cancel group, <c>flex-shrink: 0</c>) keep theirs.
/// </summary>
public class ActionSheetContainer : Panel
{
    /// <summary>A flex item that can shrink to nothing: <c>flex-shrink</c> above 0 and <c>overflow</c> other than <c>visible</c>.</summary>
    public static readonly AttachedProperty<bool> ShrinksProperty =
        AvaloniaProperty.RegisterAttached<ActionSheetContainer, Control, bool>("Shrinks");

    private double[] _heights = [];

    static ActionSheetContainer()
    {
        AffectsParentMeasure<ActionSheetContainer>(ShrinksProperty);
    }

    public static bool GetShrinks(Control control) => control.GetValue(ShrinksProperty);

    public static void SetShrinks(Control control, bool value) => control.SetValue(ShrinksProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Children;
        _heights = new double[children.Count];
        double width = 0, total = 0, shrinkable = 0;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            _heights[i] = child.DesiredSize.Height;
            width = Math.Max(width, child.DesiredSize.Width);
            total += _heights[i];
            if (GetShrinks(child))
            {
                shrinkable += _heights[i];
            }
        }

        var overflow = total - availableSize.Height;
        if (overflow > 0 && shrinkable > 0)
        {
            // Each gives up its part of the overflow, worked out so that one child alone gives up exactly the overflow.
            var shrink = Math.Min(overflow, shrinkable);
            total = 0;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (GetShrinks(child) && _heights[i] > 0)
                {
                    _heights[i] -= shrink * (_heights[i] / shrinkable);
                    child.Measure(new Size(availableSize.Width, _heights[i]));
                }

                total += _heights[i];
            }
        }

        return new Size(width, total);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // justify-content: flex-end
        var y = finalSize.Height - _heights.Sum();
        var children = Children;
        for (var i = 0; i < children.Count && i < _heights.Length; i++)
        {
            children[i].Arrange(new Rect(0, y, finalSize.Width, _heights[i]));
            y += _heights[i];
        }

        return finalSize;
    }
}
