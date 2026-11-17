using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// The alert's <c>.alert-button-group</c>: <c>display: flex; flex-wrap: wrap-reverse; justify-content: flex-end</c>, so its buttons
/// sit at the end of a row, and a row that doesn't fit wraps upwards; <c>.alert-button-group-vertical</c>
/// (<see cref="Vertical"/>) is a column of buttons as wide as the group. Each button's margin is its own.
/// </summary>
public class AlertButtonGroup : Panel
{
    public static readonly StyledProperty<bool> VerticalProperty =
        AvaloniaProperty.Register<AlertButtonGroup, bool>(nameof(Vertical));

    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<AlertButtonGroup>();

    private readonly List<(int Start, int End, double Width, double Height)> _lines = [];

    static AlertButtonGroup()
    {
        AffectsMeasure<AlertButtonGroup>(VerticalProperty, PaddingProperty);
    }

    /// <summary><c>.alert-button-group-vertical</c>.</summary>
    public bool Vertical { get => GetValue(VerticalProperty); set => SetValue(VerticalProperty, value); }

    /// <summary>The group's padding.</summary>
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    /// <summary>
    /// Raised when a measure of the row puts its buttons on more than one line: Ionic's <c>checkButtonGroupWrap</c> reads it after a
    /// frame, from its <c>ResizeObserver</c>.
    /// </summary>
    internal event EventHandler? Wrapped;

    /// <summary>Whether the last measure of the row put its buttons on more than one line. A column doesn't change it.</summary>
    internal bool RowWraps { get; private set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // A column's max-content is its widest button, where a row's is all of them: the alert's fit-content width changes even
        // when the group's own size at the alert's width doesn't, as two wrapped buttons are as wide as the column.
        if (change.Property == VerticalProperty)
        {
            this.FindAncestorOfType<AlertPanel>()?.InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var inner = availableSize.Deflate(padding);
        _lines.Clear();
        var visible = Children.Where(c => c.IsVisible).ToList();

        if (Vertical)
        {
            double width = 0, height = 0;
            foreach (var child in visible)
            {
                child.Measure(new Size(inner.Width, double.PositiveInfinity));
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }

            return new Size(width, height).Inflate(padding);
        }

        // Lines of what fits, the first at the bottom (wrap-reverse).
        var start = 0;
        double lineWidth = 0, lineHeight = 0, total = 0, widest = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (!child.IsVisible)
            {
                continue;
            }

            child.Measure(new Size(inner.Width, double.PositiveInfinity));
            var size = child.DesiredSize;
            if (lineWidth > 0 && lineWidth + size.Width > inner.Width + 0.01)
            {
                _lines.Add((start, i, lineWidth, lineHeight));
                total += lineHeight;
                widest = Math.Max(widest, lineWidth);
                start = i;
                lineWidth = 0;
                lineHeight = 0;
            }

            lineWidth += size.Width;
            lineHeight = Math.Max(lineHeight, size.Height);
        }

        if (visible.Count > 0)
        {
            _lines.Add((start, Children.Count, lineWidth, lineHeight));
            total += lineHeight;
            widest = Math.Max(widest, lineWidth);
        }

        RowWraps = _lines.Count > 1;
        if (RowWraps)
        {
            Wrapped?.Invoke(this, EventArgs.Empty);
        }

        return new Size(widest, total).Inflate(padding);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var padding = Padding;
        var inner = new Rect(finalSize).Deflate(padding);
        if (Vertical)
        {
            var y = inner.Y;
            foreach (var child in Children.Where(c => c.IsVisible))
            {
                // align-items: stretch: as wide as the group, less the button's own margin, which the layout system takes off.
                child.Arrange(new Rect(inner.X, y, inner.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
            }

            return finalSize;
        }

        // wrap-reverse: the first line at the bottom; justify-content: flex-end, which is the left in RTL as the panel mirrors.
        var bottom = inner.Bottom;
        foreach (var (lineStart, lineEnd, width, height) in _lines)
        {
            var x = inner.Right - width;
            var top = bottom - height;
            for (var i = lineStart; i < lineEnd && i < Children.Count; i++)
            {
                var child = Children[i];
                if (!child.IsVisible)
                {
                    continue;
                }

                var y = child.VerticalAlignment switch
                {
                    VerticalAlignment.Top => top,
                    VerticalAlignment.Bottom => bottom - child.DesiredSize.Height,
                    VerticalAlignment.Center => top + (height - child.DesiredSize.Height) / 2,
                    _ => top,
                };
                var h = child.VerticalAlignment == VerticalAlignment.Stretch ? height : child.DesiredSize.Height;
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, h));
                x += child.DesiredSize.Width;
            }

            bottom = top;
        }

        return finalSize;
    }
}
