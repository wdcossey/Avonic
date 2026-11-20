using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// One of <c>ion-datetime-button</c>'s two <c>&lt;button part="native" class="ion-activatable"&gt;</c>s, <c>#date-button</c> or
/// <c>#time-button</c>: its slot's content, or <see cref="Text"/> when there's none, as a <c>&lt;slot&gt;</c>'s fallback; a native
/// button's click (<see cref="NativeButton"/>: on release, a finger within 15 px, Enter and Space), tap-click and a ripple; and
/// <c>aria-expanded</c> through its peer. Styled as <c>c|DatetimeButtonNative</c>, with the style class <c>native</c> for Ionic's
/// part. <see cref="AvonicDatetimeButton"/> sets its text, whether it shows, and its expanded state.
/// </summary>
public sealed class DatetimeButtonNative : ContentControl
{
    /// <summary>The slot's fallback: the datetime's value as text, shown while the button has no content.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DatetimeButtonNative, string?>(nameof(Text));

    private bool _expanded;
    private Point? _releasedAt;

    static DatetimeButtonNative()
    {
        // A native <button> takes focus. CSS's button doesn't clip its outline: the focus ring is 1 px outside it, and the ripple
        // is clipped by a part of its own.
        FocusableProperty.OverrideDefaultValue<DatetimeButtonNative>(true);
        ClipToBoundsProperty.OverrideDefaultValue<DatetimeButtonNative>(false);
    }

    public DatetimeButtonNative()
    {
        // ion-activatable, at style priority before any style, so an app's style wins.
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);

        // Where the release was, for the popover's event (clientX, clientY): known only while the release is on its way, around
        // the native button's own handler, so a click from a key, or from inside, has none.
        AddHandler(PointerReleasedEvent, (_, e) => _releasedAt = TopLevel.GetTopLevel(this) is { } top ? e.GetPosition(top) : null,
            Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        _ = new NativeButton(this, _ => OnClick(_releasedAt), static () => true, static () => { });
        AddHandler(PointerReleasedEvent, (_, _) => _releasedAt = null, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The DOM <c>click</c>: a press released on it, Enter, Space, or automation. The point, in the top level's coordinates, or none.</summary>
    internal event EventHandler<Point?>? Clicked;

    /// <summary><c>aria-expanded</c>: the datetime is shown.</summary>
    internal bool IsExpanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
            {
                return;
            }

            var old = _expanded;
            _expanded = value;
            (ControlAutomationPeer.FromElement(this) as Peer)?.RaiseExpanded(old, value);
        }
    }

    private void OnClick(Point? at)
    {
        if (IsEffectivelyEnabled)
        {
            Clicked?.Invoke(this, at);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == ContentProperty)
        {
            (ControlAutomationPeer.FromElement(this) as Peer)?.NameChanged();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);

    /// <summary>
    /// A button, named as a browser names one from what's inside (the text, or the slotted content's), with <c>aria-expanded</c>.
    /// Expanding is a click, which presents the datetime; nothing collapses it but the overlay's own dismiss.
    /// </summary>
    private sealed class Peer(DatetimeButtonNative owner) : ControlAutomationPeer(owner), IInvokeProvider, IExpandCollapseProvider
    {
        private string? _name;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override string? GetNameCore() => _name = CurrentName();

        private string? CurrentName() =>
            AutomationProperties.GetName(owner) is { Length: > 0 } name ? name : owner.Content is null ? owner.Text : AccessibleText.Of(owner);

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public void Invoke() => owner.OnClick(null);

        public ExpandCollapseState ExpandCollapseState => owner._expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public bool ShowsMenu => false;

        public void Expand() => Invoke();

        public void Collapse()
        {
        }

        public void NameChanged()
        {
            var name = CurrentName();
            if (name != _name)
            {
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, _name, name);
                _name = name;
            }
        }

        public void RaiseExpanded(bool old, bool now) =>
            RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                old ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                now ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }
}
