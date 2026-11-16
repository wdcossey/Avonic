using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// What a phone's browser draws on a scroller while it's scrolled: a thumb at its end edge, as Chromium on Android does, measured in
/// mobile emulation (<c>docs/components/ion-content/PLAN.md</c>). Not Ionic's, so an extension. It's part of the template of the
/// <see cref="ScrollViewer"/> it's in, one for each <see cref="Orientation"/>, and it takes no input.
/// </summary>
/// <remarks>
/// It shows on a scroll by the user, the wheel or a finger and the fling after it, and not on one by a script, Ionic's scroll methods
/// or a key, as measured. It stays for <see cref="FadeDelay"/> after the last, then fades out over <see cref="FadeDuration"/>. The
/// thumb is <see cref="TemplatedControl.Background"/>, as thick as the control, and at least as long as it is thick. Its length is the
/// track's share that the viewport is of the extent, and its place the share of the way there is to scroll, both rounded down to a whole
/// pixel. The track is the control's length, less its thickness when the other axis can scroll too, as Chromium leaves the corner.
/// </remarks>
[PseudoClasses(":vertical", ":horizontal")]
public class ContentScrollIndicator : TemplatedControl
{
    /// <summary>How long it stays after the last scroll: Chromium's <c>scrollbar_fade_delay</c> on Android.</summary>
    internal static readonly TimeSpan FadeDelay = TimeSpan.FromMilliseconds(300);

    /// <summary>How long it takes to fade out, at a steady rate: Chromium's <c>scrollbar_fade_duration</c> on Android.</summary>
    internal static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>The axis it shows: vertical, at the end edge, or horizontal, along the bottom.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ContentScrollIndicator, Orientation>(nameof(Orientation), Orientation.Vertical);

    /// <summary>The scrollers that have an indicator, with how many: only these are marked below.</summary>
    private static readonly Dictionary<ScrollViewer, int> s_watched = [];

    /// <summary>
    /// The scrollers a wheel or a scroll gesture is being dispatched to: an offset that changes in the meantime is the user's. A wheel
    /// adds its scroller on its way down, and a gesture, which only bubbles, at the presenter, before the presenter's own handler
    /// takes it. Both take it out at the scroller, on the way up. A set, as a wheel goes down through every scroller around.
    /// </summary>
    private static readonly HashSet<ScrollViewer> s_userScrolling = [];

    private ScrollViewer? _scroller;
    private double _opacity;
    private TimeSpan _lastScroll;
    private TimeSpan _fadeStart;
    private IDisposable? _fadeWait;
    private IDisposable? _fadeFrame;

    static ContentScrollIndicator()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<ContentScrollIndicator>(false);
        AffectsRender<ContentScrollIndicator>(BackgroundProperty, OrientationProperty);
        PointerWheelChangedEvent.AddClassHandler<ScrollViewer>((scroller, _) => Mark(scroller), RoutingStrategies.Tunnel, handledEventsToo: true);
        PointerWheelChangedEvent.AddClassHandler<ScrollViewer>((scroller, _) => s_userScrolling.Remove(scroller), RoutingStrategies.Bubble, handledEventsToo: true);
        ScrollGestureEvent.AddClassHandler<ScrollContentPresenter>((presenter, _) => Mark(presenter.TemplatedParent as ScrollViewer), handledEventsToo: true);
        ScrollGestureEvent.AddClassHandler<ScrollViewer>((scroller, _) => s_userScrolling.Remove(scroller), handledEventsToo: true);
    }

    private static void Mark(ScrollViewer? scroller)
    {
        if (scroller is not null && s_watched.ContainsKey(scroller))
        {
            s_userScrolling.Add(scroller);
        }
    }

    public ContentScrollIndicator() => UpdatePseudoClasses();

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>How much of the thumb shows, from 1 to 0, for tests.</summary>
    internal double ThumbOpacity => _opacity;

    /// <summary>Where the thumb is in the control, or null if its axis can't scroll.</summary>
    internal Rect? Thumb
    {
        get
        {
            if (_scroller is not { } scroller)
            {
                return null;
            }

            var vertical = Orientation == Orientation.Vertical;
            var extent = vertical ? scroller.Extent.Height : scroller.Extent.Width;
            var viewport = vertical ? scroller.Viewport.Height : scroller.Viewport.Width;
            if (extent - viewport < 0.5 || viewport <= 0)
            {
                return null;
            }

            var other = vertical
                ? scroller.Extent.Width - scroller.Viewport.Width
                : scroller.Extent.Height - scroller.Viewport.Height;
            var thickness = vertical ? Bounds.Width : Bounds.Height;
            var track = (vertical ? Bounds.Height : Bounds.Width) - (other >= 0.5 ? thickness : 0);
            if (track <= 0 || thickness <= 0)
            {
                return null;
            }

            var length = Math.Min(track, Math.Max(thickness, Math.Floor(track * viewport / extent)));
            var offset = Math.Clamp(vertical ? scroller.Offset.Y : scroller.Offset.X, 0, extent - viewport);
            var position = Math.Floor((track - length) * offset / (extent - viewport));
            return vertical ? new Rect(0, position, thickness, length) : new Rect(position, 0, length, thickness);
        }
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_opacity > 0 && Background is { } brush && Thumb is { } thumb)
        {
            using (context.PushOpacity(_opacity))
            {
                context.FillRectangle(brush, thumb);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OrientationProperty)
        {
            UpdatePseudoClasses();
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TemplatedParent is ScrollViewer scroller)
        {
            _scroller = scroller;
            scroller.PropertyChanged += OnScrollerPropertyChanged;
            s_watched[scroller] = s_watched.GetValueOrDefault(scroller) + 1;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_scroller is { } scroller)
        {
            scroller.PropertyChanged -= OnScrollerPropertyChanged;
            if (s_watched[scroller] > 1)
            {
                s_watched[scroller]--;
            }
            else
            {
                s_watched.Remove(scroller);
                s_userScrolling.Remove(scroller);
            }

            _scroller = null;
        }

        Hide();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
    }

    private void OnScrollerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty)
        {
            if (_scroller is { } scroller && s_userScrolling.Contains(scroller))
            {
                Show();
            }
            else if (_opacity > 0)
            {
                InvalidateVisual();
            }
        }
        else if (_opacity > 0 && (e.Property == ScrollViewer.ExtentProperty || e.Property == ScrollViewer.ViewportProperty))
        {
            InvalidateVisual();
        }
    }

    /// <summary>
    /// A scroll by the user: all there, and the fade waits again, from now. One wait, which looks again when it's up, rather than one
    /// for each step of a fling. Nothing on an axis that can't scroll, which draws nothing.
    /// </summary>
    private void Show()
    {
        if (!CanScroll)
        {
            return;
        }

        _lastScroll = AvonicConfig.Now();
        _fadeFrame?.Dispose();
        _fadeFrame = null;
        _fadeWait ??= AvonicConfig.Schedule(OnFadeWait, FadeDelay);
        _opacity = 1;
        InvalidateVisual();
    }

    private bool CanScroll => _scroller is { } scroller && (Orientation == Orientation.Vertical
        ? scroller.Extent.Height - scroller.Viewport.Height
        : scroller.Extent.Width - scroller.Viewport.Width) >= 0.5;

    private void OnFadeWait()
    {
        var left = _lastScroll + FadeDelay - AvonicConfig.Now();
        if (left > TimeSpan.Zero)
        {
            _fadeWait = AvonicConfig.Schedule(OnFadeWait, left);
            return;
        }

        _fadeWait = null;
        _fadeStart = AvonicConfig.Now();
        _fadeFrame = AvonicConfig.RequestFrame(this, OnFadeFrame);
    }

    private void OnFadeFrame()
    {
        _fadeFrame = null;
        var progress = (AvonicConfig.Now() - _fadeStart).Ticks / (double)FadeDuration.Ticks;
        _opacity = Math.Max(0, 1 - progress);
        InvalidateVisual();
        if (_opacity > 0)
        {
            _fadeFrame = AvonicConfig.RequestFrame(this, OnFadeFrame);
        }
    }

    private void Hide()
    {
        _fadeWait?.Dispose();
        _fadeWait = null;
        _fadeFrame?.Dispose();
        _fadeFrame = null;
        _opacity = 0;
    }
}
