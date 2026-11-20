using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// One of <c>ion-datetime-button</c>'s two <c>&lt;button part="native" class="ion-activatable"&gt;</c>s, <c>#date-button</c> or
/// <c>#time-button</c>: its slot's content, or <see cref="Text"/> when there's none, as a <c>&lt;slot&gt;</c>'s fallback; tap-click
/// and a ripple; and <c>aria-expanded</c> through its peer. Styled as <c>c|DatetimeButtonNative</c>, with the style class
/// <c>native</c> for Ionic's part. <see cref="AvonicDatetimeButton"/> sets its text, whether it shows, and its expanded state.
/// </summary>
public sealed class DatetimeButtonNative : Button
{
    /// <summary>The slot's fallback: the datetime's value as text, shown while the button has no content.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DatetimeButtonNative, string?>(nameof(Text));

    private bool _expanded;

    static DatetimeButtonNative()
    {
        // CSS's button doesn't clip its outline: the focus ring is 1 px outside it. The ripple is clipped by a part of its own.
        ClipToBoundsProperty.OverrideDefaultValue<DatetimeButtonNative>(false);
    }

    public DatetimeButtonNative()
    {
        // ion-activatable, at style priority before any style, so an app's style wins.
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(DatetimeButtonNative);

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

    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);

    /// <summary>
    /// A button, named as a browser names one from what's inside (the text, or the slotted content's), with
    /// <c>aria-expanded</c>. Expanding is a click, which presents the datetime; nothing collapses it but the overlay's own dismiss.
    /// </summary>
    private sealed class Peer(DatetimeButtonNative owner) : ButtonAutomationPeer(owner), IExpandCollapseProvider
    {
        protected override string? GetNameCore() =>
            AutomationProperties.GetName(owner) is { Length: > 0 } name ? name : AccessibleText.Of(owner);

        public ExpandCollapseState ExpandCollapseState => owner._expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public bool ShowsMenu => false;

        public void Expand() => ((IInvokeProvider)this).Invoke();

        public void Collapse()
        {
        }

        public void RaiseExpanded(bool old, bool now) =>
            RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                old ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                now ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }
}
