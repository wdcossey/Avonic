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
public class AvonicItemDivider : TemplatedControl
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
    private Visual? _inParent;
    private IDisposable? _transformFrame;
    private IDisposable? _zIndexFrame;

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

        // An ItemsControl wraps templated content in a presenter as tall as the divider. In the DOM there's no such
        // box, so what's beside the divider, and what it has to be drawn above, is the presenter's.
        _inParent = this;
        while (_inParent.GetVisualParent() is ContentPresenter presenter)
        {
            _inParent = presenter;
        }

        // Behaviour, so it doesn't depend on the theme, at style priority, as CSS is, so an app's own value wins.
        _transformFrame = SetValue(RenderTransformProperty, _stickyOffset, BindingPriority.Style);
        _zIndexFrame = _inParent.SetValue(ZIndexProperty, StickyZIndex, BindingPriority.Style);
        LayoutUpdated += OnLayoutUpdated;
        UpdateStickyOffset();
    }

    private void StopSticking()
    {
        LayoutUpdated -= OnLayoutUpdated;
        _transformFrame?.Dispose();
        _transformFrame = null;
        _zIndexFrame?.Dispose();
        _zIndexFrame = null;
        _inParent = null;
        _stickyOffset.Y = 0;
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
        if (_inParent?.GetVisualParent() is { } parent && FindScrollport() is { } scrollport &&
            parent.TranslatePoint(new Point(0, _inParent.Bounds.Top), scrollport) is { } natural)
        {
            var toParentEnd = parent.Bounds.Height - _inParent.Bounds.Top - Bounds.Height;
            offset = Math.Min(-natural.Y, toParentEnd);
            offset = Math.Max(offset, 0);
        }

        _stickyOffset.Y = offset;
    }

    /// <summary>
    /// The nearest thing that clips and scrolls. An inset list has <c>overflow: hidden</c>, so it's the divider's
    /// scrollport, and it never scrolls. Measured: the divider just moves with the page.
    /// </summary>
    private Visual? FindScrollport()
    {
        foreach (var ancestor in this.GetVisualAncestors())
        {
            switch (ancestor)
            {
                case AvonicList { Inset: true }:
                    return null;
                case ScrollViewer scrollViewer:
                    return (Visual?)scrollViewer.Presenter ?? scrollViewer;
            }
        }

        return null;
    }
}
