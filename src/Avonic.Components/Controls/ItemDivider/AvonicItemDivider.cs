using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-item-divider</c>: a heading row between items, with start, default and end content, that can
/// stay at the top of what's scrolling.
/// </summary>
[TemplatePart("PART_Start", typeof(ContentPresenter))]
[TemplatePart("PART_Inner", typeof(Border))]
[TemplatePart("PART_Container", typeof(ContentPresenter))]
[TemplatePart("PART_End", typeof(ContentPresenter))]
public class AvonicItemDivider : TemplatedControl, IItemHost
{
    /// <summary><c>z-index: 100</c>. There's no variable for it.</summary>
    private const int StickyZIndex = 100;

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicItemDivider>();

    /// <summary>
    /// Ionic <c>sticky</c>: the divider stays at the top of what's scrolling, until the end of its parent or until
    /// the next sticky divider covers it.
    /// </summary>
    public static readonly StyledProperty<bool> StickyProperty =
        AvaloniaProperty.Register<AvonicItemDivider, bool>(nameof(Sticky));

    /// <summary>Ionic's default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicItemDivider, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemDivider, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Ionic's <c>start</c> slot.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicItemDivider, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemDivider, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicItemDivider, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItemDivider, IDataTemplate?>(nameof(EndContentTemplate));

    private readonly TranslateTransform _stickyOffset = new();
    private readonly List<AvonicList> _listsInTheWay = [];
    private Visual? _inParent;
    private ScrollViewer? _scrollViewer;
    private readonly StyleValue<ITransform?> _transform = new();
    private readonly StyleValue<int> _zIndex = new();
    private IDisposable? _containerZIndexFrame;

    public AvonicItemDivider()
    {
        // Behaviour, so it doesn't depend on the theme, at style priority, as CSS is. Bound once, before any style, so that an
        // app's style still wins when sticky turns on after it.
        Bind(RenderTransformProperty, _transform, BindingPriority.Style);
        Bind(ZIndexProperty, _zIndex, BindingPriority.Style);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool Sticky
    {
        get => GetValue(StickyProperty);
        set => SetValue(StickyProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    public IDataTemplate? StartContentTemplate
    {
        get => GetValue(StartContentTemplateProperty);
        set => SetValue(StartContentTemplateProperty, value);
    }

    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    public IDataTemplate? EndContentTemplate
    {
        get => GetValue(EndContentTemplateProperty);
        set => SetValue(EndContentTemplateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StickyProperty && VisualRoot is not null)
        {
            UpdateSticking();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateSticking();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopSticking();
    }

    // ── position: sticky ─────────────────────────────────────────────────────────────────────

    private void UpdateSticking()
    {
        StopSticking();
        if (!Sticky)
        {
            return;
        }

        // What's beside the divider, and what it has to be drawn above, is the presenter's if it has one.
        _inParent = AvonicList.ContainerOf(this);
        if (AvonicList.IsStuckCopy(_inParent))
        {
            // The list's copy of a recycled row, which the list sticks.
            _inParent = null;
            return;
        }

        // A virtualising panel recycles the row once its place is above the top, so the list sticks a copy then.
        AvonicList.WatchStickyRow(_inParent);
        FindScrollport();

        // Through the values bound in the constructor, which an app's value or style beats.
        _transform.Set(_stickyOffset);
        _zIndex.Set(StickyZIndex);
        if (_inParent != this)
        {
            // An items control's wrapper, which is what the panel sorts, and isn't in the DOM: it takes the divider's own
            // z-index, so that an app's value or style for the divider reaches it, as it does a divider that isn't wrapped.
            _containerZIndexFrame = _inParent.Bind(ZIndexProperty, this.GetObservable(ZIndexProperty), BindingPriority.Style);
        }

        if (_scrollViewer is not null)
        {
            LayoutUpdated += OnLayoutUpdated;
        }

        UpdateStickyOffset();
    }

    private void StopSticking()
    {
        LayoutUpdated -= OnLayoutUpdated;
        _transform.Clear();
        _zIndex.Clear();
        _containerZIndexFrame?.Dispose();
        _containerZIndexFrame = null;
        _inParent = null;
        _scrollViewer = null;
        _listsInTheWay.Clear();
    }

    /// <summary>A scroll is a change of arrangement, so a layout pass is the one thing that follows every cause.</summary>
    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateStickyOffset();

    /// <summary>
    /// Moves the drawing, not the layout, as <c>position: sticky</c> does: at the top of the scrollport, but
    /// never above where it would be, and never out of its parent.
    /// </summary>
    private void UpdateStickyOffset()
    {
        var offset = 0.0;
        if (_inParent?.GetVisualParent() is { } parent && _scrollViewer is { } scrollViewer && !IsInAnInsetList())
        {
            // Bounds don't include this control's own transform, or its margin.
            var top = Bounds.Top;
            for (Visual? visual = this; visual != _inParent && visual is not null;)
            {
                visual = visual.GetVisualParent();
                top += visual?.Bounds.Top ?? 0;
            }

            if (parent.TranslatePoint(new Point(0, top), scrollViewer) is { } natural)
            {
                // Measured: the border box reaches the top of the scroller's padding, whatever its own margin, but the
                // bottom margin stays in the parent.
                var toParentEnd = parent.Bounds.Height - top - Bounds.Height - Margin.Bottom;
                offset = Math.Min(scrollViewer.Padding.Top - natural.Y, toParentEnd);
                offset = Math.Max(offset, 0);
            }
        }

        _stickyOffset.Y = offset;
    }

    /// <summary>What scrolls, and the lists before it, once per attach, because the offset runs on every layout pass.</summary>
    private void FindScrollport()
    {
        foreach (var ancestor in this.GetVisualAncestors())
        {
            if (ancestor is ScrollViewer scrollViewer)
            {
                _scrollViewer = scrollViewer;
                return;
            }

            if (ancestor is AvonicList list)
            {
                _listsInTheWay.Add(list);
            }
        }
    }

    /// <summary>
    /// An inset list has <c>overflow: hidden</c>, so it's the divider's scrollport, and it never scrolls. Measured:
    /// the divider just moves with the page.
    /// </summary>
    private bool IsInAnInsetList()
    {
        foreach (var list in _listsInTheWay)
        {
            if (list.Inset)
            {
                return true;
            }
        }

        return false;
    }
}
