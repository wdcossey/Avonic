using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Controls;

/// <summary>
/// A list item container that reveals <see cref="AvonicItemOptions"/> swipe actions on either side.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-item-sliding</c><br/>
/// The main content translates horizontally as the user drags, exposing the option panels
/// positioned behind it. On release the content snaps closed (or open if dragged far enough).
/// </remarks>
[PseudoClasses(":open-start", ":open-end", ":dragging")]
public class AvonicItemSliding : ContentControl
{
    // ── Constants ────────────────────────────────────────────────────────────

    /// <summary>Fraction of the options width at which a release snaps the item open.</summary>
    private const double SnapThreshold = 0.4;

    // ── Template parts ───────────────────────────────────────────────────────

    private ContentPresenter? _contentPresenter;
    private ContentPresenter? _startOptionsPresenter;
    private ContentPresenter? _endOptionsPresenter;

    // ── Drag state ───────────────────────────────────────────────────────────

    private bool   _isDragging;
    private Point  _dragStart;
    private double _translateX;

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<AvonicItemOptions?> StartOptionsProperty =
        AvaloniaProperty.Register<AvonicItemSliding, AvonicItemOptions?>(nameof(StartOptions));

    public static readonly StyledProperty<AvonicItemOptions?> EndOptionsProperty =
        AvaloniaProperty.Register<AvonicItemSliding, AvonicItemOptions?>(nameof(EndOptions));

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Options revealed by swiping right (start side).</summary>
    public AvonicItemOptions? StartOptions
    {
        get => GetValue(StartOptionsProperty);
        set => SetValue(StartOptionsProperty, value);
    }

    /// <summary>Options revealed by swiping left (end side).</summary>
    public AvonicItemOptions? EndOptions
    {
        get => GetValue(EndOptionsProperty);
        set => SetValue(EndOptionsProperty, value);
    }

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent<SlidingDragEventArgs> DragEvent =
        RoutedEvent.Register<AvonicItemSliding, SlidingDragEventArgs>(
            nameof(Drag), RoutingStrategies.Bubble);

    /// <summary>Raised continuously while the user drags the item.</summary>
    public event EventHandler<SlidingDragEventArgs>? Drag
    {
        add    => AddHandler(DragEvent, value);
        remove => RemoveHandler(DragEvent, value);
    }

    // ── Template ─────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentPresenter      = e.NameScope.Find<ContentPresenter>("PART_Content");
        _startOptionsPresenter = e.NameScope.Find<ContentPresenter>("PART_StartOptions");
        _endOptionsPresenter   = e.NameScope.Find<ContentPresenter>("PART_EndOptions");

        ApplyTranslate(0);
    }

    // ── Public Methods ───────────────────────────────────────────────────────

    /// <summary>Snap the item closed, returning to its default position.</summary>
    public void Close()
    {
        AnimateToTranslate(0);
        PseudoClasses.Set(":open-start", false);
        PseudoClasses.Set(":open-end",   false);
    }

    /// <summary>Snap the item fully open on the specified side.</summary>
    public void Open(SlideSide side)
    {
        var optionsWidth = GetOptionsWidth(side);
        AnimateToTranslate(side == SlideSide.Start ? optionsWidth : -optionsWidth);
        PseudoClasses.Set(":open-start", side == SlideSide.Start);
        PseudoClasses.Set(":open-end",   side == SlideSide.End);
    }

    // ── Input Handling ───────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(this);
        _isDragging = true;
        _dragStart  = e.GetPosition(this);

        PseudoClasses.Set(":dragging", true);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging) return;

        var delta  = e.GetPosition(this).X - _dragStart.X;
        var target = _translateX + delta;
        _dragStart = e.GetPosition(this);

        // Clamp so we cannot drag past the full options width
        var maxStart = GetOptionsWidth(SlideSide.Start);
        var maxEnd   = GetOptionsWidth(SlideSide.End);
        target = Math.Clamp(target, -maxEnd, maxStart);

        ApplyTranslate(target);

        RaiseEvent(new SlidingDragEventArgs(DragEvent, this, target));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isDragging) return;

        _isDragging = false;
        e.Pointer.Capture(null);
        PseudoClasses.Set(":dragging", false);

        SnapOrClose();
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (!_isDragging) return;
        _isDragging = false;
        PseudoClasses.Set(":dragging", false);
        SnapOrClose();
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void SnapOrClose()
    {
        if (_translateX > 0)
        {
            // Swiped right — start options
            var threshold = GetOptionsWidth(SlideSide.Start) * SnapThreshold;
            if (_translateX >= threshold)
                Open(SlideSide.Start);
            else
                Close();
        }
        else if (_translateX < 0)
        {
            // Swiped left — end options
            var threshold = GetOptionsWidth(SlideSide.End) * SnapThreshold;
            if (Math.Abs(_translateX) >= threshold)
                Open(SlideSide.End);
            else
                Close();
        }
    }

    private void ApplyTranslate(double x)
    {
        _translateX = x;

        if (_contentPresenter != null)
            _contentPresenter.RenderTransform = new Avalonia.Media.TranslateTransform(x, 0);
    }

    private void AnimateToTranslate(double x)
    {
        // Direct set — animation can be layered via Avalonia Transitions on the template
        ApplyTranslate(x);
    }

    private double GetOptionsWidth(SlideSide side)
    {
        if (side == SlideSide.Start)
            return _startOptionsPresenter?.Bounds.Width ?? 0;

        return _endOptionsPresenter?.Bounds.Width ?? 0;
    }
}
