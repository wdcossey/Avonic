using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avonic.Components.Animation;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ion-loading</c>, md: a spinner and a message in a box at the centre of the window, over a backdrop, which keeps the
/// page from being used while something loads. Present it in code (<c>new AvonicLoading { Message = "Loading…", Duration = 3000
/// }.Present()</c>) and dismiss it when the work is done, or declare it in a page and open it with <see cref="AvonicOverlay.IsOpen"/>
/// or <see cref="AvonicOverlay.Trigger"/>. See <c>docs/components/ion-loading/README.md</c>.
/// </summary>
[TemplatePart("PART_Backdrop", typeof(AvonicBackdrop))]
[TemplatePart("PART_Wrapper", typeof(Control))]
[TemplatePart("PART_Spinner", typeof(AvonicSpinner))]
public class AvonicLoading : AvonicOverlay
{
    /// <summary>md's enter and leave: 200 ms.</summary>
    internal const double AnimationDuration = 200;

    /// <summary>What <c>present()</c> adds to <see cref="Duration"/> before it dismisses: <c>setTimeout(…, this.duration + 10)</c>.</summary>
    internal const double DurationSlack = 10;

    /// <summary>The loading's z-index base: 40000, over an alert or a modal, under a toast.</summary>
    private const int LoadingZIndex = 40000;

    /// <summary>Ionic <c>backdropDismiss</c>: a tap on the backdrop, Escape and back dismiss it. False, unlike the alert's.</summary>
    public static readonly StyledProperty<bool> BackdropDismissProperty =
        AvaloniaProperty.Register<AvonicLoading, bool>(nameof(BackdropDismiss));

    /// <summary>Ionic <c>cssClass</c>: style classes on the loading, space-separated.</summary>
    public static readonly StyledProperty<string?> CssClassProperty =
        AvaloniaProperty.Register<AvonicLoading, string?>(nameof(CssClass));

    /// <summary>
    /// Ionic <c>duration</c>, in milliseconds: how long it stays once presented, plus Ionic's 10, before it dismisses itself, with
    /// no data and no role. 0 keeps it until it's dismissed.
    /// </summary>
    public static readonly StyledProperty<double> DurationProperty =
        AvaloniaProperty.Register<AvonicLoading, double>(nameof(Duration));

    /// <summary>
    /// Ionic <c>htmlAttributes</c>, spread on the host: <c>id</c> is its <c>AutomationId</c>; <c>aria-label</c> names it when there's
    /// no message, as <c>aria-labelledby</c> beats it. Others have nothing to do in Avalonia, and are ignored.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, object?>?> HtmlAttributesProperty =
        AvaloniaProperty.Register<AvonicLoading, IReadOnlyDictionary<string, object?>?>(nameof(HtmlAttributes));

    /// <summary>
    /// Ionic <c>message</c>: a string is plain text, Ionic's default; anything else is content, the app's own controls in place of
    /// the HTML Ionic can be configured to take. Null shows no message; <c>""</c> an empty one, which still takes its place after the
    /// spinner.
    /// </summary>
    public static readonly StyledProperty<object?> MessageProperty =
        AvaloniaProperty.Register<AvonicLoading, object?>(nameof(Message));

    /// <summary>Ionic <c>showBackdrop</c>: the backdrop is drawn. Without it, it's transparent, and still covers the page.</summary>
    public static readonly StyledProperty<bool> ShowBackdropProperty =
        AvaloniaProperty.Register<AvonicLoading, bool>(nameof(ShowBackdrop), defaultValue: true);

    /// <summary>
    /// Ionic <c>spinner</c>: the spinner to draw, or null for none. Left unset, it becomes <see cref="AvonicConfig.LoadingSpinner"/>,
    /// else <see cref="AvonicConfig.Spinner"/>, else <see cref="SpinnerName.Crescent"/>, when the loading is first attached, as
    /// Ionic's <c>componentWillLoad</c> gives it; where a config is <see cref="ConfigSpinner.None"/>, that's null.
    /// </summary>
    public static readonly StyledProperty<SpinnerName?> SpinnerProperty =
        AvaloniaProperty.Register<AvonicLoading, SpinnerName?>(nameof(Spinner));

    /// <summary>Ionic <c>ionLoadingWillPresent</c>, raised before <see cref="AvonicOverlay.WillPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonLoadingWillPresentEvent =
        RoutedEvent.Register<AvonicLoading, RoutedEventArgs>(nameof(IonLoadingWillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionLoadingDidPresent</c>, raised before <see cref="AvonicOverlay.DidPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonLoadingDidPresentEvent =
        RoutedEvent.Register<AvonicLoading, RoutedEventArgs>(nameof(IonLoadingDidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionLoadingWillDismiss</c>, raised before <see cref="AvonicOverlay.WillDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonLoadingWillDismissEvent =
        RoutedEvent.Register<AvonicLoading, OverlayEventArgs>(nameof(IonLoadingWillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionLoadingDidDismiss</c>, raised before <see cref="AvonicOverlay.DidDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonLoadingDidDismissEvent =
        RoutedEvent.Register<AvonicLoading, OverlayEventArgs>(nameof(IonLoadingDidDismiss), RoutingStrategies.Bubble);

    /// <summary>
    /// Whether the message has text to show: an empty string renders an empty <c>.loading-content</c>, 0 by 0, where an empty
    /// <see cref="TextBlock"/> is a line high.
    /// </summary>
    public static readonly IValueConverter HasText =
        new FuncValueConverter<object?, bool>(message => message is not (null or ""));

    private readonly ConfigOnLoad<SpinnerName?> _spinner;
    private string[] _cssClasses = [];
    private LoadingAutomationPeer? _peer;

    /// <summary>Ionic's <c>durationTimeout</c>.</summary>
    private IDisposable? _durationTimeout;

    private AvonicBackdrop? _backdrop;
    private Control? _wrapper;
    private AvonicSpinner? _spinnerPart;

    public AvonicLoading()
    {
        _spinner = ConfigOnLoad<SpinnerName?>.IfUnset(this, SpinnerProperty);
    }

    public bool BackdropDismiss
    {
        get => GetValue(BackdropDismissProperty);
        set => SetValue(BackdropDismissProperty, value);
    }

    public string? CssClass
    {
        get => GetValue(CssClassProperty);
        set => SetValue(CssClassProperty, value);
    }

    public double Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public IReadOnlyDictionary<string, object?>? HtmlAttributes
    {
        get => GetValue(HtmlAttributesProperty);
        set => SetValue(HtmlAttributesProperty, value);
    }

    public object? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool ShowBackdrop
    {
        get => GetValue(ShowBackdropProperty);
        set => SetValue(ShowBackdropProperty, value);
    }

    public SpinnerName? Spinner
    {
        get => GetValue(SpinnerProperty);
        set => SetValue(SpinnerProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonLoadingWillPresent
    {
        add => AddHandler(IonLoadingWillPresentEvent, value);
        remove => RemoveHandler(IonLoadingWillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonLoadingDidPresent
    {
        add => AddHandler(IonLoadingDidPresentEvent, value);
        remove => RemoveHandler(IonLoadingDidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonLoadingWillDismiss
    {
        add => AddHandler(IonLoadingWillDismissEvent, value);
        remove => RemoveHandler(IonLoadingWillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonLoadingDidDismiss
    {
        add => AddHandler(IonLoadingDidDismissEvent, value);
        remove => RemoveHandler(IonLoadingDidDismissEvent, value);
    }

    /// <summary>The backdrop part.</summary>
    internal AvonicBackdrop? Backdrop => _backdrop;

    /// <summary>The wrapper, <c>.loading-wrapper</c>.</summary>
    internal Control? Wrapper => _wrapper;

    /// <summary>The spinner part.</summary>
    internal AvonicSpinner? SpinnerPart => _spinnerPart;

    /// <summary>
    /// The name the host's <c>aria-labelledby</c> gives it: the message's text; else <c>htmlAttributes['aria-label']</c>, else its
    /// own name.
    /// </summary>
    internal string? AccessibleName
    {
        get
        {
            if (Message is string { Length: > 0 } text)
            {
                return text;
            }

            if (HtmlAttributes is { } attributes && attributes.TryGetValue("aria-label", out var label) && label?.ToString() is { Length: > 0 } name)
            {
                return name;
            }

            return AutomationProperties.GetName(this);
        }
    }

    protected internal override bool BackdropDismissEnabled => BackdropDismiss;

    protected internal override Control? FocusTrapWrapper => _wrapper;

    protected override int ZIndexBase => LoadingZIndex;

    protected override RoutedEvent<RoutedEventArgs> ControlWillPresentEvent => IonLoadingWillPresentEvent;

    protected override RoutedEvent<RoutedEventArgs> ControlDidPresentEvent => IonLoadingDidPresentEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlWillDismissEvent => IonLoadingWillDismissEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlDidDismissEvent => IonLoadingDidDismissEvent;

    /// <summary>
    /// <c>md.enter.js</c>: 200 ms <c>ease-in-out</c>, the backdrop from 0.01 to <c>--backdrop-opacity</c>, not hit-testable until it
    /// ends; the wrapper from opacity 0.01 and <c>scale(1.1)</c> to 1 and <c>scale(1)</c>.
    /// </summary>
    protected override AnimationBuilder MdEnterAnimation => (baseElement, _) =>
    {
        var loading = (AvonicLoading)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(loading._backdrop)
            .FromTo("opacity", 0.01, loading.BackdropOpacity())
            .BeforeStyles(new() { ["pointer-events"] = "none" })
            .AfterClearStyles(["pointer-events"]);
        var wrapper = AvonicAnimation.Create()
            .AddElement(loading._wrapper)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 0.01, ["transform"] = "scale(1.1)" },
                new AnimationKeyframe(1) { ["opacity"] = 1, ["transform"] = "scale(1)" },
            ]);
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(AnimationDuration)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary>
    /// <c>md.leave.js</c>: 200 ms <c>ease-in-out</c>, the backdrop to 0; the wrapper from 0.99 and <c>scale(1)</c> to 0 and
    /// <c>scale(0.9)</c>.
    /// </summary>
    protected override AnimationBuilder MdLeaveAnimation => (baseElement, _) =>
    {
        var loading = (AvonicLoading)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(loading._backdrop)
            .FromTo("opacity", loading.BackdropOpacity(), 0);
        var wrapper = AvonicAnimation.Create()
            .AddElement(loading._wrapper)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 0.99, ["transform"] = "scale(1)" },
                new AnimationKeyframe(1) { ["opacity"] = 0, ["transform"] = "scale(0.9)" },
            ]);
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(AnimationDuration)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary>
    /// <c>var(--backdrop-opacity)</c>: <c>Avonic.Loading.BackdropOpacity</c>, falling back to <c>Avonic.BackdropOpacity</c>
    /// (<c>--ion-backdrop-opacity</c>), then 0.32.
    /// </summary>
    internal double BackdropOpacity() => OverlayHandler.BackdropOpacity(this, "Avonic.Loading.BackdropOpacity");

    /// <summary>
    /// The end of <c>present()</c>: with a <see cref="Duration"/>, the timer that dismisses it, <c>duration + 10</c> ms on. Only when
    /// this present presented it: Ionic starts one at every <c>present()</c>, and loses the earlier one, which can then dismiss a
    /// later presentation (<c>docs/infra/ionic-issues.md</c>).
    /// </summary>
    protected override void OnPresented()
    {
        // A browser's setTimeout overflows past 2^31 - 1 ms; a timer here throws, so a duration that long (infinity, say) never comes.
        if (Duration > 0 && Duration + DurationSlack <= int.MaxValue)
        {
            _durationTimeout = AvonicConfig.Schedule(() => DismissFromInput(null), TimeSpan.FromMilliseconds(Duration + DurationSlack));
        }
    }

    /// <summary>The start of <c>dismiss()</c>, under the lock: the timer goes.</summary>
    protected override Task<bool> OnDismissing(object? data, string? role)
    {
        _durationTimeout?.Dispose();
        _durationTimeout = null;
        return Task.FromResult(true);
    }

    /// <summary>Ionic's <c>componentWillLoad</c>: an unset <see cref="Spinner"/> takes the config's, once.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // config.get('loadingSpinner', config.get('spinner', 'crescent')), whose null is none.
        _spinner.Load(static () =>
            ConfigSpinner.Get(AvonicConfig.LoadingSpinner, ConfigSpinner.Get(AvonicConfig.Spinner, SpinnerName.Crescent)));
        base.OnAttachedToVisualTree(e);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new LoadingAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _backdrop = e.NameScope.Find<AvonicBackdrop>("PART_Backdrop");
        _wrapper = e.NameScope.Find<Control>("PART_Wrapper");
        _spinnerPart = e.NameScope.Find<AvonicSpinner>("PART_Spinner");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CssClassProperty)
        {
            foreach (var name in _cssClasses)
            {
                Classes.Remove(name);
            }

            _cssClasses = AlertClasses.Apply(this, change.GetNewValue<string?>());
        }
        else if (change.Property == MessageProperty || change.Property == HtmlAttributesProperty || change.Property == AutomationProperties.NameProperty)
        {
            _peer?.NameChanged();
        }
    }

    /// <summary>
    /// The host: <c>role="dialog"</c>, <c>aria-modal</c>, to automation a <see cref="AutomationControlType.Window"/>, Avalonia's
    /// nearest type to a dialog, named by its message (<c>aria-labelledby</c>), else <c>aria-label</c>; its <c>AutomationId</c>
    /// <c>htmlAttributes.id</c>.
    /// </summary>
    private sealed class LoadingAutomationPeer(AvonicLoading owner) : ControlAutomationPeer(owner)
    {
        private string? _name;

        /// <summary>The name, read again, and raised if it changed.</summary>
        public void NameChanged()
        {
            var name = owner.AccessibleName;
            if (name != _name)
            {
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, _name, name);
                _name = name;
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

        protected override string? GetNameCore() => _name = owner.AccessibleName;

        protected override string? GetAutomationIdCore() =>
            owner.HtmlAttributes is { } attributes && attributes.TryGetValue("id", out var value) && value?.ToString() is { Length: > 0 } id
                ? id
                : base.GetAutomationIdCore();

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;
    }
}
