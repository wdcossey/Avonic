using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-segment-view</c>'s <c>display: flex</c> row of contents, each <c>width: 100%</c> of the view and as high as the
/// view, with a snap point at each, as <c>scroll-snap-align: center</c> gives.
/// </summary>
public sealed class SegmentViewPanel : Panel, IScrollSnapPointsInfo
{
    private double _pageWidth;

    public SegmentViewPanel()
    {
        // overflow-x: hidden with swiping off: a wheel that a content didn't take doesn't page the view either. Here, below the
        // view's scroller, so that a content's own vertical scroll still has it first.
        AddHandler(PointerWheelChangedEvent, (_, e) => e.Handled |= this.FindAncestorOfType<AvonicSegmentView>() is { CanSwipe: false });
    }

    /// <summary>The view's width, which a scroller around this panel doesn't pass on.</summary>
    private double Page(Size available) =>
        this.FindAncestorOfType<AvonicSegmentView>()?.PageWidth is { } page && double.IsFinite(page) ? page
        : double.IsFinite(available.Width) ? available.Width : 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        var page = Page(availableSize);
        var height = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(page, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(page * Children.Count, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var page = Children.Count == 0 ? 0 : finalSize.Width / Children.Count;
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(new Rect(i * page, 0, page, finalSize.Height));
        }

        if (page != _pageWidth)
        {
            _pageWidth = page;
            HorizontalSnapPointsChanged?.Invoke(this, new RoutedEventArgs());
        }

        return finalSize;
    }

    public bool AreHorizontalSnapPointsRegular { get; set; } = true;

    public bool AreVerticalSnapPointsRegular { get; set; }

    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment) =>
        orientation == Orientation.Horizontal
            ? Enumerable.Range(0, Children.Count).Select(i => i * _pageWidth + Within(snapPointsAlignment)).ToList()
            : [];

    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
    {
        offset = orientation == Orientation.Horizontal ? Within(snapPointsAlignment) : 0;
        return orientation == Orientation.Horizontal ? _pageWidth : 0;
    }

    /// <summary>Where in a page its snap point is.</summary>
    private double Within(SnapPointsAlignment alignment) => alignment switch
    {
        SnapPointsAlignment.Center => _pageWidth / 2,
        SnapPointsAlignment.Far => _pageWidth,
        _ => 0,
    };

    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;

    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
    {
        add { }
        remove { }
    }
}
