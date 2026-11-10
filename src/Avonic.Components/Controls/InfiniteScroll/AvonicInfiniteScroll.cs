using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-infinite-scroll</c>: raises <see cref="IonInfinite"/> when the scroll of the <see cref="AvonicContent"/> around it
/// comes within <see cref="Threshold"/> of the end, and waits for <see cref="Complete"/> before it does again. Its content is
/// usually an <see cref="AvonicInfiniteScrollContent"/>, which shows a spinner while it loads.
/// </summary>
/// <remarks>
/// Ionic's <c>disabled</c> is <see cref="InputElement.IsEnabled"/>: false hides it and stops it listening. It has no look of its
/// own. See <c>docs/components/ion-infinite-scroll/README.md</c>.
/// </remarks>
[PseudoClasses(LoadingClass, EnabledClass)]
public partial class AvonicInfiniteScroll : ContentControl
{
    internal const string LoadingClass = ":infinite-scroll-loading";
    internal const string EnabledClass = ":infinite-scroll-enabled";

    /// <summary>The class Ionic's selector also takes for a scroller: <c>.ion-content-scroll-host</c>.</summary>
    public const string ScrollHostClass = "ion-content-scroll-host";

    /// <summary>Ionic <c>position</c>.</summary>
    public static readonly StyledProperty<InfiniteScrollPosition> PositionProperty =
        AvaloniaProperty.Register<AvonicInfiniteScroll, InfiniteScrollPosition>(nameof(Position));

    /// <summary>
    /// Ionic <c>threshold</c>: how near the end it fires, as a percentage of the scroller's height (<c>"15%"</c>) or in pixels
    /// (<c>"100px"</c>, or a number). Read as <c>parseFloat</c> reads it, so anything that doesn't start with a number never fires.
    /// </summary>
    public static readonly StyledProperty<string?> ThresholdProperty =
        AvaloniaProperty.Register<AvonicInfiniteScroll, string?>(nameof(Threshold), "15%");

    /// <summary>Ionic <c>ionInfinite</c>: the scroll came within the threshold. Call <see cref="Complete"/> when the load is done.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonInfiniteEvent =
        RoutedEvent.Register<AvonicInfiniteScroll, RoutedEventArgs>(nameof(IonInfinite), RoutingStrategies.Bubble);

    private double _thrPx;
    private double _thrPc;

    // didFire keeps ionInfinite from firing again while the scroll stays within the threshold, until complete(). Ionic's canStart()
    // also asks for not isLoading and not isBusy, but each is only ever true while didFire is: fire sets both flags, complete()
    // clears loading first and didFire with busy at the end, and disabling clears loading and busy but not didFire. So didFire
    // is the whole of it here, and a disabled one isn't listening.
    private bool _didFire;
    private bool _isLoading;

    private ScrollViewer? _scrollEl;
    private bool _listening;
    private IDisposable? _offsetWatch;
    private ScrollViewer? _scrollEventTarget;
    private IDisposable? _scrollEvent;
    private IDisposable? _hidden;

    public AvonicInfiniteScroll()
    {
        ThresholdChanged();
        UpdateEnabled();
    }

    /// <inheritdoc cref="PositionProperty"/>
    public InfiniteScrollPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <inheritdoc cref="ThresholdProperty"/>
    public string? Threshold
    {
        get => GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    /// <inheritdoc cref="IonInfiniteEvent"/>
    public event EventHandler<RoutedEventArgs>? IonInfinite
    {
        add => AddHandler(IonInfiniteEvent, value);
        remove => RemoveHandler(IonInfiniteEvent, value);
    }

    /// <summary>Ionic's <c>isLoading</c>: from <see cref="IonInfinite"/> to <see cref="Complete"/>, or to being disabled.</summary>
    internal bool IsLoading => _isLoading;

    /// <summary>For an <see cref="AvonicInfiniteScrollContent"/> inside, which shows its spinner while loading.</summary>
    internal event EventHandler? LoadingChanged;

    /// <summary>
    /// Ionic <c>complete()</c>: the load is done, so it can fire again. Nothing while it isn't loading. At the <see cref="InfiniteScrollPosition.Top"/>,
    /// it waits for the rows added above to be laid out and scrolls by their height, so that what's shown stays put; it can't fire
    /// until then.
    /// </summary>
    public void Complete()
    {
        var scrollEl = _scrollEl;
        if (!_isLoading || scrollEl is null)
        {
            return;
        }

        SetLoading(false);
        if (Position != InfiniteScrollPosition.Top)
        {
            _didFire = false;
            return;
        }

        // Ionic's isBusy, until the position is restored, is didFire, which stays true until then.
        // Not a forced layout, unlike the reads after: Ionic's own comment says the UI hasn't updated yet, which is what an app
        // that adds its rows and then calls this sees in a browser, as a framework renders them later. Here they're in the tree
        // already, and not yet laid out, so the last layout's numbers are the ones from before.
        var prev = ScrollHeight(scrollEl) - scrollEl.Offset.Y;
        // raf, readTask, raf, writeTask: four frames, as measured.
        AvonicConfig.RequestFrame(scrollEl, () => AvonicConfig.RequestFrame(scrollEl, () =>
        {
            scrollEl.UpdateLayout();
            var newScrollTop = ScrollHeight(scrollEl) - prev;
            AvonicConfig.RequestFrame(scrollEl, () => AvonicConfig.RequestFrame(scrollEl, () =>
            {
                scrollEl.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(scrollEl.Offset.X, newScrollTop));
                _didFire = false;
            }));
        }));
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ThresholdProperty)
        {
            ThresholdChanged();
        }
        else if (change.Property == IsEnabledProperty)
        {
            DisabledChanged();
            UpdateEnabled();
        }
    }

    /// <summary>Ionic's <c>connectedCallback</c>.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (FindClosestScrollHost(out var inside) is not { } host)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "AvonicInfiniteScroll must be used inside AvonicContent.");
            return;
        }

        // Ionic parses the threshold here too; it's parsed whenever it changes, so it's ready. A host with the class scrolls
        // itself in a browser; here only a ScrollViewer can, so it's the host, or the one in the host's template around this, as a
        // ListBox has. A content with a replaced template may have none.
        var scrollEl = host is AvonicContent content ? content.GetScrollElement() : host as ScrollViewer ?? inside;
        if (scrollEl is null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "AvonicInfiniteScroll found no ScrollViewer in {Host}.", host);
            return;
        }

        _scrollEl = scrollEl;
        if (!ReferenceEquals(_scrollEventTarget, scrollEl))
        {
            _scrollEvent?.Dispose();
            _scrollEvent = null;
        }

        _offsetWatch = scrollEl.GetPropertyChangedObservable(ScrollViewer.OffsetProperty).Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(_ => OnOffsetChanged(scrollEl)));
        DisabledChanged();
        if (Position == InfiniteScrollPosition.Top)
        {
            // writeTask: scrollTop = scrollHeight - clientHeight, if it's still connected.
            AvonicConfig.RequestFrame(scrollEl, () =>
            {
                if (_scrollEl is { } current)
                {
                    current.UpdateLayout();
                    current.SetCurrentValue(
                        ScrollViewer.OffsetProperty,
                        new Vector(current.Offset.X, Math.Max(0, ScrollHeight(current) - current.Viewport.Height)));
                }
            });
        }
    }

    /// <summary>Ionic's <c>disconnectedCallback</c>.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        EnableScrollEvents(false);
        _offsetWatch?.Dispose();
        _offsetWatch = null;
        _scrollEl = null;
    }

    /// <summary>
    /// <c>el.closest('ion-content, .ion-content-scroll-host')</c>, which starts at the element itself and stops at the first, with
    /// the nearest <see cref="ScrollViewer"/> on the way to it.
    /// </summary>
    private Control? FindClosestScrollHost(out ScrollViewer? inside)
    {
        inside = null;
        foreach (var visual in this.GetSelfAndVisualAncestors())
        {
            if (visual is AvonicContent || (visual is Control control && control.Classes.Contains(ScrollHostClass)))
            {
                return (Control)visual;
            }

            inside ??= visual as ScrollViewer;
        }

        return null;
    }

    private void ThresholdChanged()
    {
        var val = Threshold;
        if (val is not null && val.Contains('%', StringComparison.Ordinal))
        {
            _thrPx = 0;
            _thrPc = ParseFloat(val) / 100;
        }
        else
        {
            _thrPx = ParseFloat(val);
            _thrPc = 0;
        }
    }

    private void DisabledChanged()
    {
        if (!IsEnabled)
        {
            SetLoading(false);
        }

        EnableScrollEvents(IsEnabled);
    }

    /// <summary><c>display: none</c> unless <c>.infinite-scroll-enabled</c>, at style priority, so that an app's value wins.</summary>
    private void UpdateEnabled()
    {
        PseudoClasses.Set(EnabledClass, IsEnabled);
        if (!IsEnabled && _hidden is null)
        {
            _hidden = SetValue(IsVisibleProperty, false, BindingPriority.Style);
        }
        else if (IsEnabled && _hidden is not null)
        {
            _hidden.Dispose();
            _hidden = null;
        }
    }

    private void SetLoading(bool loading)
    {
        if (_isLoading == loading)
        {
            return;
        }

        _isLoading = loading;
        PseudoClasses.Set(LoadingClass, loading);
        LoadingChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ionic's add and remove of its <c>scroll</c> listener: who hears the scroll event when it's dispatched.</summary>
    private void EnableScrollEvents(bool shouldListen) => _listening = shouldListen && _scrollEl is not null;

    /// <summary>
    /// A browser dispatches <c>scroll</c> once, at the next frame, however many times the position changed, to whoever listens
    /// then: one removed and added again in the meantime, by being disabled and enabled or moved, still hears it.
    /// </summary>
    private void OnOffsetChanged(ScrollViewer scrollEl)
    {
        if (_scrollEvent is not null)
        {
            return;
        }

        _scrollEventTarget = scrollEl;
        _scrollEvent = AvonicConfig.RequestFrame(scrollEl, () =>
        {
            _scrollEvent = null;
            _scrollEventTarget = null;
            if (_listening && ReferenceEquals(_scrollEl, scrollEl))
            {
                OnScroll();
            }
        });
    }

    /// <summary>Ionic's <c>onScroll</c>.</summary>
    private void OnScroll()
    {
        var scrollEl = _scrollEl;
        if (scrollEl is null || _didFire)
        {
            return;
        }

        // Each of Ionic's reads lays out what's due first.
        scrollEl.UpdateLayout();
        // A hidden element's offsetHeight is 0.
        var infiniteHeight = IsEffectivelyVisible ? Bounds.Height : 0;
        if (infiniteHeight == 0)
        {
            return;
        }

        var scrollTop = scrollEl.Offset.Y;
        var scrollHeight = ScrollHeight(scrollEl);
        var height = scrollEl.Bounds.Height;
        var threshold = _thrPc != 0 ? height * _thrPc : _thrPx;
        var distanceFromInfinite = Position == InfiniteScrollPosition.Bottom
            ? scrollHeight - infiniteHeight - scrollTop - threshold - height
            : scrollTop - infiniteHeight - threshold;

        if (distanceFromInfinite < 0)
        {
            SetLoading(true);
            _didFire = true;
            RaiseEvent(new RoutedEventArgs(IonInfiniteEvent, this));
        }
    }

    /// <summary>A scroll container's <c>scrollHeight</c>: its content, or its own height when that's taller.</summary>
    private static double ScrollHeight(ScrollViewer scrollEl) => Math.Max(scrollEl.Extent.Height, scrollEl.Viewport.Height);

    /// <summary>JavaScript's <c>parseFloat</c>: the longest number at the start, after JavaScript's white space, or NaN.</summary>
    internal static double ParseFloat(string? value)
    {
        if (value is null)
        {
            return double.NaN;
        }

        var match = FloatPrefix().Match(value);
        if (!match.Success)
        {
            return double.NaN;
        }

        var number = match.Groups["number"].Value;
        var negative = match.Groups["sign"].Value == "-";
        if (number == "Infinity")
        {
            return negative ? double.NegativeInfinity : double.PositiveInfinity;
        }

        var parsed = double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);
        return negative ? -parsed : parsed;
    }

    [GeneratedRegex(@"^[\t\n\v\f\r \u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000\uFEFF]*(?<sign>[+-]?)(?<number>Infinity|(?:[0-9]+\.?[0-9]*|\.[0-9]+)(?:[eE][+-]?[0-9]+)?)", RegexOptions.CultureInvariant)]
    private static partial Regex FloatPrefix();
}
