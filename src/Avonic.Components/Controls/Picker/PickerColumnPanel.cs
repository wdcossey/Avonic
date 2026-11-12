using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-picker-column</c>'s <c>.picker-opts</c>: the options one under another, each as wide as the widest
/// (<c>display: block</c>), between three empty rows above and three below, so that the first and the last option can reach the
/// centre; with a snap point at the centre of each option that <c>scroll-snap-align: center</c> gives one: every enabled option,
/// and every option when the column is disabled.
/// </summary>
public sealed class PickerColumnPanel : Panel, IScrollSnapPointsInfo
{
    /// <summary><c>.picker-opts</c>' padding. Its top and bottom are added to the empty rows.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<PickerColumnPanel>();

    /// <summary>The height of each of the six <c>.picker-item-empty</c> rows.</summary>
    public static readonly StyledProperty<double> EmptyRowHeightProperty =
        AvaloniaProperty.Register<PickerColumnPanel, double>(nameof(EmptyRowHeight));

    /// <summary>The number of empty rows on each side, as Ionic renders them.</summary>
    internal const int EmptyRows = 3;

    private IReadOnlyList<double> _snapPoints = [];
    private IReadOnlyList<(double Top, double Bottom)> _snapBoxes = [];

    static PickerColumnPanel()
    {
        AffectsMeasure<PickerColumnPanel>(PaddingProperty, EmptyRowHeightProperty);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public double EmptyRowHeight
    {
        get => GetValue(EmptyRowHeightProperty);
        set => SetValue(EmptyRowHeightProperty, value);
    }

    /// <summary>Where the options start: the padding and the three empty rows.</summary>
    internal double Top => Padding.Top + EmptyRows * EmptyRowHeight;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = 0.0;
        var inner = new Size(double.PositiveInfinity, double.PositiveInfinity);
        foreach (var child in Children)
        {
            child.Measure(inner);
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        return new Size(width + Padding.Left + Padding.Right, height + Top + Padding.Bottom + EmptyRows * EmptyRowHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = Top;
        var width = Math.Max(0, finalSize.Width - Padding.Left - Padding.Right);
        foreach (var child in Children)
        {
            var height = child.DesiredSize.Height;
            child.Arrange(new Rect(Padding.Left, y, width, height));
            y += height;
        }

        UpdateSnapPoints();
        return finalSize;
    }

    /// <summary>Called by the column when what snaps may have changed: an option's or the column's <c>disabled</c>.</summary>
    internal void UpdateSnapPoints()
    {
        var column = this.FindAncestorOfType<AvonicPickerColumn>();
        var columnDisabled = column is { IsEffectivelyEnabled: false };
        var points = new List<double>();
        var boxes = new List<(double, double)>();
        foreach (var child in Children)
        {
            if (child is AvonicPickerColumnOption option && child.IsVisible && (columnDisabled || option.IsEnabled))
            {
                points.Add(child.Bounds.Y + child.Bounds.Height / 2);
                boxes.Add((child.Bounds.Top, child.Bounds.Bottom));
            }
        }

        if (!points.SequenceEqual(_snapPoints))
        {
            _snapPoints = points;
            _snapBoxes = boxes;
            VerticalSnapPointsChanged?.Invoke(this, new RoutedEventArgs());
        }
    }

    /// <summary>The centres that snap, in the panel's own coordinates, which are the scroller's content's.</summary>
    internal IReadOnlyList<double> SnapCentres => _snapPoints;

    public bool AreHorizontalSnapPointsRegular { get; set; }

    public bool AreVerticalSnapPointsRegular { get; set; }

    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
    {
        if (orientation == Orientation.Horizontal)
        {
            return [];
        }

        return snapPointsAlignment switch
        {
            SnapPointsAlignment.Center => _snapPoints,
            SnapPointsAlignment.Near => _snapBoxes.Select(b => b.Top).ToList(),
            _ => _snapBoxes.Select(b => b.Bottom).ToList(),
        };
    }

    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
    {
        offset = 0;
        return 0;
    }

    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged
    {
        add { }
        remove { }
    }

    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;
}
