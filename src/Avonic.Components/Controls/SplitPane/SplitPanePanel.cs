using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// <see cref="AvonicSplitPane"/>'s panel: <c>split-pane.md.css</c>'s flex row and the menu's rules for a side pane
/// (<c>menu.md.css</c>'s <c>.menu-pane-visible.split-pane-side</c>). While the split pane is hidden every child fills it, as a menu
/// (<c>position: absolute; inset: 0</c>) and an <c>.ion-page</c>, <c>ion-router-outlet</c> or <c>ion-content</c> main do. While it's
/// visible it's a row in the split pane's direction: each side (a child that is, or holds, a menu shown in this pane) at
/// <see cref="SideWidth"/> within <see cref="SideMinWidth"/> and <see cref="SideMaxWidth"/>, the maximum and then the minimum, which
/// wins, as in CSS; start sides first, end sides last (<c>order: -1</c> and <c>1</c>); the main content (<c>flex: 1</c>) takes what's
/// left; any other child sits in the row at its own width, in order. A side's border, <c>border-inline-end</c> at the start and
/// <c>border-inline-start</c> at the end, is drawn by the panel inside its width. Each size is a number (px) or a CSS string, as the
/// alert reads them (<see cref="AlertPanel.Length(object?, double, Size)"/>): a percentage is of the split pane's width.
/// </summary>
public class SplitPanePanel : Panel
{
    /// <summary><c>--side-width</c>. Unset, the side menu's own <c>--width</c>, <c>Avonic.Menu.Width</c>.</summary>
    public static readonly StyledProperty<object?> SideWidthProperty =
        AvaloniaProperty.Register<SplitPanePanel, object?>(nameof(SideWidth));

    /// <summary><c>--side-min-width</c>.</summary>
    public static readonly StyledProperty<object?> SideMinWidthProperty =
        AvaloniaProperty.Register<SplitPanePanel, object?>(nameof(SideMinWidth));

    /// <summary><c>--side-max-width</c>.</summary>
    public static readonly StyledProperty<object?> SideMaxWidthProperty =
        AvaloniaProperty.Register<SplitPanePanel, object?>(nameof(SideMaxWidth));

    /// <summary>The colour of <c>--border</c>.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<SplitPanePanel, IBrush?>(nameof(BorderBrush));

    /// <summary>The width of <c>--border</c>.</summary>
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<SplitPanePanel, double>(nameof(BorderThickness));

    /// <summary>The split pane is visible: <c>split-pane-visible</c>. Set by <see cref="AvonicSplitPane"/>.</summary>
    public static readonly StyledProperty<bool> IsSplitProperty =
        AvaloniaProperty.Register<SplitPanePanel, bool>(nameof(IsSplit));

    /// <summary>The key of the menu's <c>--width</c>, which <c>--side-width</c> falls back to.</summary>
    internal const string MenuWidthKey = "Avonic.Menu.Width";

    private readonly List<(Control Child, Kind Kind, double Width)> _row = [];
    private AvonicSplitPane? _owner;

    private enum Kind
    {
        Start,
        Main,
        Other,
        End,
    }

    static SplitPanePanel()
    {
        AffectsMeasure<SplitPanePanel>(SideWidthProperty, SideMinWidthProperty, SideMaxWidthProperty, BorderThicknessProperty, IsSplitProperty);
    }

    /// <inheritdoc cref="SideWidthProperty"/>
    public object? SideWidth { get => GetValue(SideWidthProperty); set => SetValue(SideWidthProperty, value); }

    /// <inheritdoc cref="SideMinWidthProperty"/>
    public object? SideMinWidth { get => GetValue(SideMinWidthProperty); set => SetValue(SideMinWidthProperty, value); }

    /// <inheritdoc cref="SideMaxWidthProperty"/>
    public object? SideMaxWidth { get => GetValue(SideMaxWidthProperty); set => SetValue(SideMaxWidthProperty, value); }

    /// <inheritdoc cref="BorderBrushProperty"/>
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }

    /// <inheritdoc cref="BorderThicknessProperty"/>
    public double BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }

    /// <inheritdoc cref="IsSplitProperty"/>
    public bool IsSplit { get => GetValue(IsSplitProperty); set => SetValue(IsSplitProperty, value); }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.GetVisualParent() is ItemsPresenter { TemplatedParent: AvonicSplitPane owner } ? owner : this.FindAncestorOfType<AvonicSplitPane>();
        _owner?.PanelAttached(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BorderBrushProperty)
        {
            _owner?.BordersChanged();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _owner = null;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _row.Clear();
        if (!IsSplit)
        {
            var desired = default(Size);
            foreach (var child in Children)
            {
                child.Measure(availableSize);
                desired = new Size(Math.Max(desired.Width, child.DesiredSize.Width), Math.Max(desired.Height, child.DesiredSize.Height));
            }

            return desired;
        }

        var width = availableSize.Width;
        var height = availableSize.Height;
        var border = Math.Max(0, BorderThickness);
        var used = 0.0;
        var tallest = 0.0;
        var mains = new List<Control>();
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            if (SideMenu(child) is { } menu)
            {
                var side = SideWidthFor(menu, child, width);
                child.Measure(new Size(Math.Max(0, side - border), height));
                _row.Add((child, IsEndSide(menu) ? Kind.End : Kind.Start, side));
                used += side;
            }
            else if (child.Classes.Contains(AvonicSplitPane.SplitPaneMain))
            {
                mains.Add(child);
                _row.Add((child, Kind.Main, 0));
            }
            else
            {
                child.Measure(new Size(double.PositiveInfinity, height));
                _row.Add((child, Kind.Other, child.DesiredSize.Width));
                used += child.DesiredSize.Width;
            }

            tallest = Math.Max(tallest, child.DesiredSize.Height);
        }

        // flex: 1, from a basis of 0: what's left, shared, never below 0.
        var rest = double.IsInfinity(width) ? double.PositiveInfinity : Math.Max(0, width - used);
        var share = mains.Count == 0 ? 0 : rest / mains.Count;
        for (var i = 0; i < _row.Count; i++)
        {
            if (_row[i].Kind == Kind.Main)
            {
                var child = _row[i].Child;
                child.Measure(new Size(share, height));
                var w = double.IsInfinity(share) ? child.DesiredSize.Width : share;
                _row[i] = (child, Kind.Main, w);
                used += w;
                tallest = Math.Max(tallest, child.DesiredSize.Height);
            }
        }

        return new Size(double.IsInfinity(width) ? used : width, double.IsInfinity(height) ? tallest : height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!IsSplit)
        {
            foreach (var child in Children)
            {
                child.Arrange(new Rect(finalSize));
            }

            _owner?.BordersChanged();
            return finalSize;
        }

        var border = Math.Max(0, BorderThickness);
        var x = 0.0;
        foreach (var kind in new[] { Kind.Start, Kind.Main, Kind.End })
        {
            foreach (var (child, k, w) in _row)
            {
                // order: -1, then the main and any other child in order, then 1.
                if (k != kind && !(kind == Kind.Main && k == Kind.Other))
                {
                    continue;
                }

                var rect = k switch
                {
                    Kind.Start => new Rect(x, 0, Math.Max(0, w - border), finalSize.Height),
                    Kind.End => new Rect(x + Math.Min(border, w), 0, Math.Max(0, w - border), finalSize.Height),
                    _ => new Rect(x, 0, w, finalSize.Height),
                };
                child.Arrange(rect);
                x += w;
            }
        }

        _owner?.BordersChanged();
        return finalSize;
    }

    /// <summary>
    /// Each side's border, in the panel's coordinates: inside its width, at its inline end for the start side, at its inline start for
    /// the end side. <see cref="SplitPaneBorders"/> draws them, as a panel can't draw over its own background.
    /// </summary>
    internal IEnumerable<Rect> BorderRects()
    {
        var border = BorderThickness;
        if (!IsSplit || border <= 0)
        {
            yield break;
        }

        foreach (var (child, kind, width) in _row)
        {
            if (kind is not (Kind.Start or Kind.End) || !child.IsVisible)
            {
                continue;
            }

            var bounds = child.Bounds;
            var w = Math.Min(border, width);
            yield return kind == Kind.Start ? new Rect(bounds.Right, 0, w, Bounds.Height) : new Rect(bounds.Left - w, 0, w, Bounds.Height);
        }
    }

    /// <summary>The menu shown in this pane that <paramref name="child"/> is, or holds, as a menu wrapped in another control is.</summary>
    private AvonicMenu? SideMenu(Control child)
    {
        if (_owner is null)
        {
            return null;
        }

        foreach (var menu in _owner.Sides)
        {
            if (menu.IsPaneShown && (ReferenceEquals(menu, child) || child.IsVisualAncestorOf(menu)))
            {
                return menu;
            }
        }

        return null;
    }

    private static bool IsEndSide(AvonicMenu menu) => menu.Side == MenuSide.End;

    /// <summary>
    /// <c>width: var(--side-width, var(--width))</c> within <c>min-width: var(--side-min-width)</c> and
    /// <c>max-width: var(--side-max-width)</c>, percentages of the split pane's width. With no width at all, the side's own.
    /// </summary>
    private double SideWidthFor(AvonicMenu menu, Control child, double basis)
    {
        var viewport = TopLevel.GetTopLevel(this)?.ClientSize ?? new Size(basis, basis);
        var min = AlertPanel.Length(SideMinWidth, basis, viewport) ?? 0;
        var max = AlertPanel.Length(SideMaxWidth, basis, viewport) ?? double.PositiveInfinity;
        var width = AlertPanel.Length(SideWidth, basis, viewport)
            ?? (menu.TryFindResource(MenuWidthKey, menu.ActualThemeVariant, out var own) ? AlertPanel.Length(own, basis, viewport) : null);
        if (width is null)
        {
            // width: auto, the side's own width.
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = child.DesiredSize.Width;
        }

        // Rounded to the device's pixels here, so that the side, its border and the main content meet on them, as Avalonia's
        // layout rounding would round each of them on its own.
        var side = Math.Max(min, Math.Min(width.Value, max));
        var scale = LayoutHelper.GetLayoutScale(this);
        return UseLayoutRounding && scale > 0 ? Math.Round(side * scale) / scale : side;
    }
}
