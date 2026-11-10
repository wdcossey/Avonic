using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>A tick: where it is along the slider, and whether it's between the ends of the active bar.</summary>
public readonly record struct RangeTick(double Ratio, bool IsActive);

/// <summary>
/// <c>ion-range</c>'s <c>.range-tick</c> elements, which it makes one per step: each a square of <see cref="TickSize"/>, its start edge
/// at its ratio of the width, as <c>left: n%</c> puts it, and centred down. Drawn here, as the range makes them and a theme can't
/// reach elements made in code: <see cref="Background"/> is the <c>tick</c> part's, <see cref="ActiveBackground"/> the
/// <c>tick-active</c> part's. Avalonia mirrors it in RTL.
/// </summary>
public sealed class RangeTicks : Control
{
    /// <summary><c>.range-tick { background }</c>.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<RangeTicks, IBrush?>(nameof(Background));

    /// <summary><c>.range-tick-active { background }</c>.</summary>
    public static readonly StyledProperty<IBrush?> ActiveBackgroundProperty =
        AvaloniaProperty.Register<RangeTicks, IBrush?>(nameof(ActiveBackground));

    /// <summary>A tick's width and height: <c>var(--bar-height)</c>.</summary>
    public static readonly StyledProperty<double> TickSizeProperty =
        AvaloniaProperty.Register<RangeTicks, double>(nameof(TickSize), 2);

    /// <summary>The ticks, from the range.</summary>
    public static readonly DirectProperty<RangeTicks, IReadOnlyList<RangeTick>> ItemsProperty =
        AvaloniaProperty.RegisterDirect<RangeTicks, IReadOnlyList<RangeTick>>(nameof(Items), ticks => ticks.Items);

    private IReadOnlyList<RangeTick> _items = [];

    static RangeTicks()
    {
        AffectsRender<RangeTicks>(BackgroundProperty, ActiveBackgroundProperty, TickSizeProperty, ItemsProperty);

        // pointer-events: none
        IsHitTestVisibleProperty.OverrideDefaultValue<RangeTicks>(false);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? ActiveBackground
    {
        get => GetValue(ActiveBackgroundProperty);
        set => SetValue(ActiveBackgroundProperty, value);
    }

    public double TickSize
    {
        get => GetValue(TickSizeProperty);
        set => SetValue(TickSizeProperty, value);
    }

    public IReadOnlyList<RangeTick> Items
    {
        get => _items;
        internal set
        {
            if (!_items.SequenceEqual(value))
            {
                SetAndRaise(ItemsProperty, ref _items, value);
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        var size = TickSize;
        var y = (Bounds.Height - size) / 2;
        foreach (var tick in _items)
        {
            if ((tick.IsActive ? ActiveBackground : Background) is { } brush)
            {
                context.FillRectangle(brush, new Rect(tick.Ratio * Bounds.Width, y, size, size));
            }
        }
    }
}
