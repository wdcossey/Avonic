using System.Collections.Specialized;
using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ion-alert</c>, md: a dialog of a header, a message, inputs (text fields, radios or checkboxes) and buttons, over a
/// backdrop. Present it in code (<c>new AvonicAlert { Header = …, Buttons = ["OK"] }.Present()</c>), or declare it in a page and
/// open it with <see cref="AvonicOverlay.IsOpen"/> or <see cref="AvonicOverlay.Trigger"/>. A button dismisses it with an
/// <see cref="AlertDismissData"/> of the inputs' values and its role. See <c>docs/components/ion-alert/README.md</c>.
/// </summary>
[TemplatePart("PART_Backdrop", typeof(AvonicBackdrop))]
[TemplatePart("PART_Wrapper", typeof(AlertWrapper))]
[TemplatePart("PART_Title", typeof(TextBlock))]
[TemplatePart("PART_SubTitle", typeof(TextBlock))]
[TemplatePart("PART_InputGroup", typeof(Control))]
[TemplatePart("PART_Inputs", typeof(Panel))]
[TemplatePart("PART_ButtonGroup", typeof(AlertButtonGroup))]
[PseudoClasses(BreakpointMd, InputGroup, RadioGroup, CheckboxGroup)]
public class AvonicAlert : AvonicOverlay
{
    /// <summary><c>@media (min-width: 768px)</c>, on the top level's client width.</summary>
    public const string BreakpointMd = ":breakpoint-md";

    /// <summary>Text fields: <c>.alert-input-group</c>.</summary>
    public const string InputGroup = ":input-group";

    /// <summary>Radios: <c>.alert-radio-group</c>.</summary>
    public const string RadioGroup = ":radio-group";

    /// <summary>Checkboxes: <c>.alert-checkbox-group</c>.</summary>
    public const string CheckboxGroup = ":checkbox-group";

    /// <summary>md's <c>min-width: 768px</c>: <see cref="GridViewport"/>'s <c>md</c>.</summary>
    internal const int BreakpointMdIndex = 2;

    /// <summary><c>--backdrop-opacity</c>'s md fallback, <c>var(--ion-backdrop-opacity, 0.32)</c>.</summary>
    internal const double DefaultBackdropOpacity = 0.32;

    /// <summary>md's enter and leave: 150 ms.</summary>
    internal const double AnimationDuration = 150;

    /// <summary>Ionic <c>backdropDismiss</c>: a tap on the backdrop, Escape and back dismiss it.</summary>
    public static readonly StyledProperty<bool> BackdropDismissProperty =
        AvaloniaProperty.Register<AvonicAlert, bool>(nameof(BackdropDismiss), defaultValue: true);

    /// <summary>Ionic <c>header</c>.</summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<AvonicAlert, string?>(nameof(Header));

    /// <summary>Ionic <c>subHeader</c>.</summary>
    public static readonly StyledProperty<string?> SubHeaderProperty =
        AvaloniaProperty.Register<AvonicAlert, string?>(nameof(SubHeader));

    /// <summary>
    /// Ionic <c>message</c>: a string is plain text, Ionic's default; anything else is content, the app's own controls in place
    /// of the HTML Ionic can be configured to take.
    /// </summary>
    public static readonly StyledProperty<object?> MessageProperty =
        AvaloniaProperty.Register<AvonicAlert, object?>(nameof(Message));

    /// <summary>Ionic <c>buttons</c>.</summary>
    public static readonly StyledProperty<IList<AlertButton>?> ButtonsProperty =
        AvaloniaProperty.Register<AvonicAlert, IList<AlertButton>?>(nameof(Buttons));

    /// <summary>Ionic <c>inputs</c>.</summary>
    public static readonly StyledProperty<IList<AlertInput>?> InputsProperty =
        AvaloniaProperty.Register<AvonicAlert, IList<AlertInput>?>(nameof(Inputs));

    /// <summary>Ionic <c>cssClass</c>: style classes on the alert, space-separated.</summary>
    public static readonly StyledProperty<string?> CssClassProperty =
        AvaloniaProperty.Register<AvonicAlert, string?>(nameof(CssClass));

    /// <summary>
    /// Ionic <c>htmlAttributes</c>, spread on the wrapper: <c>aria-label</c> names it when there's no header or sub header, as
    /// <c>aria-labelledby</c> beats it; <c>id</c> is its <c>AutomationId</c>. Others have nothing to do in Avalonia, and are ignored.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, object?>?> HtmlAttributesProperty =
        AvaloniaProperty.Register<AvonicAlert, IReadOnlyDictionary<string, object?>?>(nameof(HtmlAttributes));

    /// <summary>Ionic <c>ionAlertWillPresent</c>, raised before <see cref="AvonicOverlay.WillPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonAlertWillPresentEvent =
        RoutedEvent.Register<AvonicAlert, RoutedEventArgs>(nameof(IonAlertWillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionAlertDidPresent</c>, raised before <see cref="AvonicOverlay.DidPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonAlertDidPresentEvent =
        RoutedEvent.Register<AvonicAlert, RoutedEventArgs>(nameof(IonAlertDidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionAlertWillDismiss</c>, raised before <see cref="AvonicOverlay.WillDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonAlertWillDismissEvent =
        RoutedEvent.Register<AvonicAlert, OverlayEventArgs>(nameof(IonAlertWillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionAlertDidDismiss</c>, raised before <see cref="AvonicOverlay.DidDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonAlertDidDismissEvent =
        RoutedEvent.Register<AvonicAlert, OverlayEventArgs>(nameof(IonAlertDidDismiss), RoutingStrategies.Bubble);

    /// <summary>Whether a message is shown: <c>.alert-message:empty</c> has no padding, so an empty one takes no room.</summary>
    public static readonly IValueConverter HasMessage =
        new FuncValueConverter<object?, bool>(message => message is not (null or ""));

    private readonly List<AlertInput> _processedInputs = [];
    private readonly List<AlertButton> _processedButtons = [];
    private readonly List<AlertOptionButton> _options = [];
    private readonly List<AlertActionButton> _buttonControls = [];
    private AlertInputType? _inputType;
    private bool _inputsProcessed;
    private bool _wrapped;
    private IDisposable? _wrapCheck;
    private string[] _cssClasses = [];
    private INotifyCollectionChanged? _watchedButtons;
    private INotifyCollectionChanged? _watchedInputs;
    private GridViewport? _viewport;

    private AvonicBackdrop? _backdrop;
    private AlertWrapper? _wrapper;
    private TextBlock? _title;
    private TextBlock? _subTitle;
    private Control? _inputGroup;
    private Panel? _inputsPanel;
    private AlertButtonGroup? _buttonGroup;

    public AvonicAlert()
    {
        SetCurrentValue(ButtonsProperty, new AvaloniaList<AlertButton>());
        SetCurrentValue(InputsProperty, new AvaloniaList<AlertInput>());

        // The host's onIonAlertWillDismiss, Ionic's dispatchCancelHandler.
        AddHandler(IonAlertWillDismissEvent, OnWillDismissCancel);

        // Ionic's keydown listener on the document; Tunnel, so that a scroller around the radios doesn't take the arrows first.
        AddHandler(KeyDownEvent, OnAlertKeyDown, RoutingStrategies.Tunnel);
    }

    public bool BackdropDismiss
    {
        get => GetValue(BackdropDismissProperty);
        set => SetValue(BackdropDismissProperty, value);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? SubHeader
    {
        get => GetValue(SubHeaderProperty);
        set => SetValue(SubHeaderProperty, value);
    }

    public object? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public IList<AlertButton>? Buttons
    {
        get => GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
    }

    public IList<AlertInput>? Inputs
    {
        get => GetValue(InputsProperty);
        set => SetValue(InputsProperty, value);
    }

    public string? CssClass
    {
        get => GetValue(CssClassProperty);
        set => SetValue(CssClassProperty, value);
    }

    public IReadOnlyDictionary<string, object?>? HtmlAttributes
    {
        get => GetValue(HtmlAttributesProperty);
        set => SetValue(HtmlAttributesProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonAlertWillPresent
    {
        add => AddHandler(IonAlertWillPresentEvent, value);
        remove => RemoveHandler(IonAlertWillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonAlertDidPresent
    {
        add => AddHandler(IonAlertDidPresentEvent, value);
        remove => RemoveHandler(IonAlertDidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonAlertWillDismiss
    {
        add => AddHandler(IonAlertWillDismissEvent, value);
        remove => RemoveHandler(IonAlertWillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonAlertDidDismiss
    {
        add => AddHandler(IonAlertDidDismissEvent, value);
        remove => RemoveHandler(IonAlertDidDismissEvent, value);
    }

    /// <summary>The backdrop part.</summary>
    internal AvonicBackdrop? Backdrop => _backdrop;

    /// <summary>The wrapper, <c>.alert-wrapper</c>.</summary>
    internal AlertWrapper? Wrapper => _wrapper;

    /// <summary>The button group's panel.</summary>
    internal AlertButtonGroup? ButtonGroup => _buttonGroup;

    /// <summary>The buttons' controls, in order.</summary>
    internal IReadOnlyList<AlertActionButton> ButtonControls => _buttonControls;

    /// <summary>The inputs' controls, in order: <see cref="AlertOptionButton"/>s or <see cref="AlertTextField"/>s.</summary>
    internal IReadOnlyList<Control> InputControls => _inputsPanel?.Children ?? (IReadOnlyList<Control>)[];

    /// <summary>Ionic's <c>processedInputs</c>: the alert's copies.</summary>
    internal IReadOnlyList<AlertInput> ProcessedInputs => _processedInputs;

    /// <summary>The header and sub header, as <c>aria-labelledby</c> names the wrapper; else <c>aria-label</c>, else the alert's name.</summary>
    internal string? AccessibleName
    {
        get
        {
            var parts = new[] { Header, SubHeader }.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            if (parts.Length > 0)
            {
                return string.Join(" ", parts);
            }

            if (HtmlAttributes is { } attributes && attributes.TryGetValue("aria-label", out var label) && label?.ToString() is { Length: > 0 } text)
            {
                return text;
            }

            return AutomationProperties.GetName(this);
        }
    }

    protected internal override bool BackdropDismissEnabled => BackdropDismiss;

    protected internal override Control? FocusTrapWrapper => _wrapper;

    protected override RoutedEvent<RoutedEventArgs> ControlWillPresentEvent => IonAlertWillPresentEvent;

    protected override RoutedEvent<RoutedEventArgs> ControlDidPresentEvent => IonAlertDidPresentEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlWillDismissEvent => IonAlertWillDismissEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlDidDismissEvent => IonAlertDidDismissEvent;

    /// <summary><c>md.enter.js</c>: 150 ms <c>ease-in-out</c>, the backdrop to <c>--backdrop-opacity</c>, the wrapper up from 0.9 as it appears.</summary>
    protected override AnimationBuilder MdEnterAnimation => (baseElement, _) =>
    {
        var alert = (AvonicAlert)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(alert._backdrop)
            .FromTo("opacity", 0.01, alert.BackdropOpacity())
            .BeforeStyles(new() { ["pointer-events"] = "none" })
            .AfterClearStyles(["pointer-events"]);
        var wrapper = AvonicAnimation.Create()
            .AddElement(alert._wrapper)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 0.01, ["transform"] = "scale(0.9)" },
                new AnimationKeyframe(1) { ["opacity"] = 1, ["transform"] = "scale(1)" },
            ]);
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(AnimationDuration)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary><c>md.leave.js</c>: 150 ms <c>ease-in-out</c>, the backdrop to 0, the wrapper from 0.99 to 0.</summary>
    protected override AnimationBuilder MdLeaveAnimation => (baseElement, _) =>
    {
        var alert = (AvonicAlert)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(alert._backdrop)
            .FromTo("opacity", alert.BackdropOpacity(), 0);
        var wrapper = AvonicAnimation.Create()
            .AddElement(alert._wrapper)
            .FromTo("opacity", 0.99, 0);
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("ease-in-out")
            .Duration(AnimationDuration)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary>
    /// <c>var(--backdrop-opacity)</c>: <c>Avonic.Alert.BackdropOpacity</c>, falling back to <c>Avonic.BackdropOpacity</c>
    /// (<c>--ion-backdrop-opacity</c>), then 0.32.
    /// </summary>
    internal double BackdropOpacity()
    {
        foreach (var key in (string[])["Avonic.Alert.BackdropOpacity", "Avonic.BackdropOpacity"])
        {
            if (this.TryFindResource(key, ActualThemeVariant, out var value) && Css.IsSet(value) && value is double or float or int)
            {
                return Convert.ToDouble(value);
            }
        }

        return DefaultBackdropOpacity;
    }

    /// <summary><c>componentWillLoad</c>'s <c>inputsChanged</c>, once there's an overlay index for the ids, and the render.</summary>
    protected override void OnPresenting()
    {
        if (!_inputsProcessed)
        {
            ProcessInputs();
        }

        RenderParts();
    }

    /// <summary>
    /// The alert's own <c>present()</c> after the shared one: the one button, when it's the only button and there are no inputs,
    /// else the wrapper, is focused, whatever <see cref="AvonicOverlay.KeyboardClose"/> says.
    /// </summary>
    protected override void OnPresented()
    {
        if ((Buttons?.Count ?? 0) == 1 && (Inputs?.Count ?? 0) == 0 && _buttonControls.Count > 0)
        {
            _buttonControls[0].Focus();
        }
        else
        {
            _wrapper?.Focus();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _backdrop = e.NameScope.Find<AvonicBackdrop>("PART_Backdrop");
        _wrapper = e.NameScope.Find<AlertWrapper>("PART_Wrapper");
        _title = e.NameScope.Find<TextBlock>("PART_Title");
        _subTitle = e.NameScope.Find<TextBlock>("PART_SubTitle");
        _inputGroup = e.NameScope.Find<Control>("PART_InputGroup");
        _inputsPanel?.Children.Clear();
        _inputsPanel = e.NameScope.Find<Panel>("PART_Inputs");
        if (_buttonGroup is not null)
        {
            _buttonGroup.Wrapped -= OnButtonGroupWrapped;
            _buttonGroup.Children.Clear();
        }

        _buttonGroup = e.NameScope.Find<AlertButtonGroup>("PART_ButtonGroup");
        if (_buttonGroup is not null)
        {
            _buttonGroup.Wrapped += OnButtonGroupWrapped;
        }

        UpdateHeadings();
        RenderParts();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ButtonsProperty)
        {
            Watch(ref _watchedButtons, change.NewValue as INotifyCollectionChanged, OnButtonsCollectionChanged);
            ButtonsChanged();
        }
        else if (change.Property == InputsProperty)
        {
            Watch(ref _watchedInputs, change.NewValue as INotifyCollectionChanged, OnInputsCollectionChanged);
            InputsChanged();
        }
        else if (change.Property == HeaderProperty || change.Property == SubHeaderProperty)
        {
            UpdateHeadings();
        }
        else if (change.Property == CssClassProperty)
        {
            foreach (var name in _cssClasses)
            {
                Classes.Remove(name);
            }

            _cssClasses = AlertClasses.Apply(this, change.GetNewValue<string?>());
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The media queries matter only while it's shown, which is while it's in the overlay host: the grid's breakpoints, which
        // say when one is crossed.
        if (Parent is OverlayHost)
        {
            _viewport = GridViewport.Of(this);
            _viewport?.Changed += UpdateBreakpoint;
            UpdateBreakpoint();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _viewport?.Changed -= UpdateBreakpoint;
        _viewport = null;
        _wrapCheck?.Dispose();
        _wrapCheck = null;
    }

    /// <summary><c>@media (min-width: 768px)</c>: the grid's <c>md</c> breakpoint and up.</summary>
    private void UpdateBreakpoint() =>
        PseudoClasses.Set(BreakpointMd, (_viewport?.Breakpoint ?? 0) >= BreakpointMdIndex);

    private static void Watch(ref INotifyCollectionChanged? watched, INotifyCollectionChanged? next, NotifyCollectionChangedEventHandler handler)
    {
        if (watched is not null)
        {
            watched.CollectionChanged -= handler;
        }

        watched = next;
        if (watched is not null)
        {
            watched.CollectionChanged += handler;
        }
    }

    private void OnButtonsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => ButtonsChanged();

    private void OnInputsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InputsChanged();

    // ── Ionic's watchers ────────────────────────────────────────────────────────────────────

    /// <summary><c>buttonsChanged</c>: the buttons again, and the wrap state reset, to be checked again.</summary>
    private void ButtonsChanged()
    {
        _processedButtons.Clear();
        _processedButtons.AddRange(Buttons ?? []);
        _wrapped = false;
        _wrapCheck?.Dispose();
        _wrapCheck = null;
        RenderButtons();
    }

    /// <summary><c>inputsChanged</c>, once the alert has loaded; before that, its present does it.</summary>
    private void InputsChanged()
    {
        if (_inputsProcessed)
        {
            ProcessInputs();
            RenderInputs();
        }
    }

    /// <summary>
    /// <c>inputsChanged</c>: copies, with Ionic's defaults; the first enabled checked one, else the first enabled one, is a radio
    /// alert's one tab stop. The kind is the first input's type; radios and checkboxes can't mix (a warning).
    /// </summary>
    private void ProcessInputs()
    {
        _inputsProcessed = true;
        var inputs = Inputs ?? [];
        var first = inputs.FirstOrDefault(i => !i.Disabled);
        var focusable = inputs.FirstOrDefault(i => i.Checked && !i.Disabled) ?? first;
        var types = inputs.Select(i => i.Type).ToHashSet();
        if (types.Contains(AlertInputType.Checkbox) && types.Contains(AlertInputType.Radio))
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this,
                "[ion-alert] - Alert cannot mix input types: {Types}. Please see alert docs for more info.",
                string.Join("/", types.Select(t => t?.ToString().ToLowerInvariant() ?? "undefined")));
        }

        _inputType = inputs.Count > 0 ? inputs[0].Type : null;
        _processedInputs.Clear();
        _tabStops.Clear();
        for (var index = 0; index < inputs.Count; index++)
        {
            var source = inputs[index];
            var copy = source.Copy();
            copy.Type = source.Type ?? AlertInputType.Text;
            copy.Name = source.Name is { Length: > 0 } name ? name : index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            copy.Placeholder = source.Placeholder ?? "";
            copy.Id = source.Id is { Length: > 0 } id ? id : $"alert-input-{OverlayIndex}-{index}";
            copy.CssClass = source.CssClass ?? "";
            _processedInputs.Add(copy);
            _tabStops[copy] = source.Type != AlertInputType.Radio || ReferenceEquals(source, focusable);
        }
    }

    /// <summary>Ionic's <c>tabindex</c> on each processed input: 0 is a stop, -1 isn't.</summary>
    private readonly Dictionary<AlertInput, bool> _tabStops = new(ReferenceEqualityComparer.Instance);

    // ── Rendering ───────────────────────────────────────────────────────────────────────────

    private void RenderParts()
    {
        RenderInputs();
        RenderButtons();
    }

    private void UpdateHeadings()
    {
        // The sub header is an h3 under a header, else an h2.
        if (_title is not null)
        {
            AutomationProperties.SetHeadingLevel(_title, 2);
        }

        if (_subTitle is not null)
        {
            AutomationProperties.SetHeadingLevel(_subTitle, string.IsNullOrEmpty(Header) ? 2 : 3);
        }
    }

    /// <summary><c>renderAlertButtons</c>.</summary>
    private void RenderButtons()
    {
        if (_buttonGroup is null)
        {
            return;
        }

        // Ionic's render patches the buttons that are there, so one with focus keeps it: the one in its place takes it here.
        var focused = _buttonControls.FindIndex(c => c.IsKeyboardFocusWithin);
        foreach (var control in _buttonControls)
        {
            control.Clicked -= OnButtonClicked;
        }

        _buttonControls.Clear();
        _buttonGroup.Children.Clear();
        foreach (var button in _processedButtons)
        {
            var control = new AlertActionButton(button);
            control.Clicked += OnButtonClicked;
            _buttonControls.Add(control);
            _buttonGroup.Children.Add(control);
        }

        UpdateVertical();
        RefocusAt(focused, _buttonControls);
    }

    /// <summary><c>alert-button-group-vertical</c>: more than two buttons, or two that wrapped.</summary>
    private void UpdateVertical()
    {
        if (_buttonGroup is not null)
        {
            _buttonGroup.Vertical = _processedButtons.Count > 2 || _wrapped;
        }
    }

    /// <summary>
    /// <c>checkButtonGroupWrap</c>, from the <c>ResizeObserver</c>, after a frame: a row of two or more that wrapped makes the group a
    /// column, which then stays, as its buttons no longer share a top.
    /// </summary>
    private void OnButtonGroupWrapped(object? sender, EventArgs e)
    {
        if (_wrapped || _wrapCheck is not null || _processedButtons.Count < 2)
        {
            return;
        }

        _wrapCheck = AvonicConfig.RequestFrame(this, () =>
        {
            _wrapCheck = null;

            // Read again after the frame, as Ionic reads the buttons' tops in its raf: a narrow pass on the way doesn't count.
            _wrapped = _buttonGroup?.RowWraps == true;
            UpdateVertical();
        });
    }

    /// <summary><c>renderAlertInputs</c>: the kind of the first input, for all of them.</summary>
    private void RenderInputs()
    {
        if (_inputsPanel is null)
        {
            return;
        }

        var focused = _inputsPanel.Children.ToList().FindIndex(c => c.IsKeyboardFocusWithin);
        foreach (var option in _options)
        {
            option.Clicked -= OnOptionClicked;
        }

        _options.Clear();
        _inputsPanel.Children.Clear();
        try
        {
            RenderInputControls(_inputsPanel);
        }
        finally
        {
            RefocusAt(focused, _inputsPanel.Children);
        }
    }

    /// <summary>
    /// Focus that was on a control a render replaced goes to the one in its place, or the wrapper if there's none, rather than out
    /// of the alert, as Ionic's patched element keeps it.
    /// </summary>
    private void RefocusAt(int index, IReadOnlyList<Control> controls)
    {
        if (index < 0)
        {
            return;
        }

        var target = index < controls.Count ? controls[index] : null;
        (target as TemplatedControl)?.ApplyTemplate();
        var focusable = target is AlertTextField { Native: { } native } ? native : target;
        if (focusable is null || !focusable.Focus())
        {
            _wrapper?.Focus();
        }
    }

    private void RenderInputControls(Panel panel)
    {
        PseudoClasses.Set(InputGroup, false);
        PseudoClasses.Set(RadioGroup, false);
        PseudoClasses.Set(CheckboxGroup, false);
        if (_inputGroup is not null)
        {
            _inputGroup.IsVisible = _processedInputs.Count > 0;
        }

        if (_processedInputs.Count == 0)
        {
            return;
        }

        switch (_inputType)
        {
            case AlertInputType.Checkbox:
                PseudoClasses.Set(CheckboxGroup, true);
                foreach (var input in _processedInputs)
                {
                    AddOption(new AlertCheckboxButton(), input);
                }

                break;
            case AlertInputType.Radio:
                PseudoClasses.Set(RadioGroup, true);
                foreach (var input in _processedInputs)
                {
                    AddOption(new AlertRadioButton(), input);
                }

                break;
            default:
                PseudoClasses.Set(InputGroup, true);
                foreach (var input in _processedInputs)
                {
                    AlertTextField field = input.Type == AlertInputType.Textarea ? new AlertTextareaInput() : new AlertTextInput();
                    field.Input = input;
                    AlertClasses.Apply(field, input.CssClass);
                    var attributes = input.Attributes;
                    if (attributes is not null && attributes.TryGetValue("class", out var cls))
                    {
                        AlertClasses.Apply(field, cls?.ToString());
                    }

                    // attributes.disabled ?? disabled
                    field.IsEnabled = !(attributes is not null && attributes.TryGetValue("disabled", out var disabled) && disabled is not null
                        ? AlertClasses.IsTrue(disabled)
                        : input.Disabled);
                    panel.Children.Add(field);
                }

                break;
        }
    }

    private void AddOption(AlertOptionButton option, AlertInput input)
    {
        option.Input = input;
        option.Label = input.Label;
        option.IsEnabled = !input.Disabled;
        AlertClasses.Apply(option, input.CssClass);
        AutomationProperties.SetAutomationId(option, input.Id);
        option.Clicked += OnOptionClicked;
        _options.Add(option);
        _inputsPanel?.Children.Add(option);
        UpdateOption(option);
    }

    private void UpdateOption(AlertOptionButton option)
    {
        if (option.Input is not { } input)
        {
            return;
        }

        option.IsChecked = input.Checked;
        KeyboardNavigation.SetIsTabStop(option, _tabStops.GetValueOrDefault(input, true));
    }

    // ── Input ───────────────────────────────────────────────────────────────────────────────

    private void OnOptionClicked(object? sender, EventArgs e)
    {
        if (sender is not AlertOptionButton { Input: { } input })
        {
            return;
        }

        if (_inputType == AlertInputType.Checkbox)
        {
            CbClick(input);
        }
        else
        {
            RbClick(input);
        }
    }

    /// <summary><c>rbClick</c>: only this one checked, and the one tab stop; its handler.</summary>
    private void RbClick(AlertInput selected)
    {
        foreach (var input in _processedInputs)
        {
            input.Checked = ReferenceEquals(input, selected);
            _tabStops[input] = ReferenceEquals(input, selected);
        }

        selected.Handler?.Invoke(selected);
        foreach (var option in _options)
        {
            UpdateOption(option);
        }
    }

    /// <summary><c>cbClick</c>: toggled; its handler.</summary>
    private void CbClick(AlertInput selected)
    {
        selected.Checked = !selected.Checked;
        selected.Handler?.Invoke(selected);
        foreach (var option in _options)
        {
            UpdateOption(option);
        }
    }

    private void OnButtonClicked(object? sender, EventArgs e)
    {
        if (sender is AlertActionButton { Button: { } button })
        {
            LogFault(ButtonClick(button), "button's handler");
        }
    }

    /// <summary>
    /// <c>buttonClick</c>: a cancel role dismisses with the values at once; otherwise the handler, awaited, with the values: false
    /// keeps the alert open, and what else it returns is merged into the data beside them.
    /// </summary>
    internal async Task<bool> ButtonClick(AlertButton button)
    {
        var values = GetValues();
        if (OverlayRoles.IsCancel(button.Role))
        {
            return await Dismiss(new AlertDismissData(values), button.Role);
        }

        if (await CallButtonHandler(button, values) is not { } returned)
        {
            return false;
        }

        var data = new AlertDismissData(values);
        foreach (var (key, value) in returned)
        {
            data[key] = value;
        }

        return await Dismiss(data, button.Role);
    }

    /// <summary><c>callButtonHandler</c>: null when the handler said false; what it returned to spread; else nothing to add.</summary>
    private static async Task<IEnumerable<KeyValuePair<string, object?>>?> CallButtonHandler(AlertButton? button, object? data)
    {
        if (button?.Handler is not { } handler)
        {
            return [];
        }

        var returned = handler(data);
        // A ValueTask is a Task once asked for one.
        if (returned is not null && returned.GetType() is { IsValueType: true } type
            && (type == typeof(ValueTask) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            returned = type.GetMethod(nameof(ValueTask.AsTask))!.Invoke(returned, null);
        }

        if (returned is Task task)
        {
            await task;
            returned = TaskResult(task);
        }

        return returned switch
        {
            false => null,
            IEnumerable<KeyValuePair<string, object?>> pairs => pairs,
            System.Collections.IDictionary dictionary => dictionary.Keys.Cast<object>()
                .Select(key => new KeyValuePair<string, object?>(key.ToString() ?? "", dictionary[key])).ToList(),
            _ => [],
        };
    }

    /// <summary>A <c>Task&lt;T&gt;</c>'s result, or null for a plain <see cref="Task"/>.</summary>
    private static object? TaskResult(Task task)
    {
        var type = task.GetType();
        while (type is not null && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)))
        {
            type = type.BaseType;
        }

        // An async method's plain Task is a Task<VoidTaskResult>: nothing to read.
        return type is not null && type.GetGenericArguments()[0].FullName != "System.Threading.Tasks.VoidTaskResult"
            ? type.GetProperty(nameof(Task<object>.Result), BindingFlags.Public | BindingFlags.Instance)?.GetValue(task)
            : null;
    }

    /// <summary>
    /// <c>dispatchCancelHandler</c>: a dismiss with a cancel role (a cancel button, the backdrop, Escape, back) calls the cancel
    /// button's handler, with no values, and doesn't wait for it or read what it returns.
    /// </summary>
    private void OnWillDismissCancel(object? sender, OverlayEventArgs e)
    {
        if (ReferenceEquals(e.Source, this) && OverlayRoles.IsCancel(e.Role)
            && _processedButtons.FirstOrDefault(b => b.Role == "cancel") is { } cancel)
        {
            LogFault(CallButtonHandler(cancel, null), "cancel button's handler");
        }
    }

    /// <summary><c>getValues</c>.</summary>
    internal object? GetValues()
    {
        if (_processedInputs.Count == 0)
        {
            return null;
        }

        return _inputType switch
        {
            AlertInputType.Radio => _processedInputs.FirstOrDefault(i => i.Checked)?.Value,
            AlertInputType.Checkbox => _processedInputs.Where(i => i.Checked).Select(i => i.Value).ToArray(),
            _ => TextValues(),
        };
    }

    /// <summary>A text alert's values: each input's value, <c>''</c> when it has none, under its name; a later name wins.</summary>
    private Dictionary<string, object?> TextValues()
    {
        var values = new Dictionary<string, object?>();
        foreach (var input in _processedInputs)
        {
            values[input.Name!] = input.Value is null or "" ? "" : input.Value;
        }

        return values;
    }

    /// <summary>
    /// Ionic's <c>onKeydown</c>: Shift+Tab on the wrapper focuses the last button; in a radio alert, the arrows on a radio check and
    /// focus the next enabled one (Down, Right) or the previous (Up, Left), wrapping.
    /// </summary>
    private void OnAlertKeyDown(object? sender, KeyEventArgs e)
    {
        if (ReferenceEquals(e.Source, _wrapper) && e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            _buttonControls.LastOrDefault()?.Focus(NavigationMethod.Tab, KeyModifiers.Shift);
            return;
        }

        if (_inputType != AlertInputType.Radio || e.Source is not AlertRadioButton current)
        {
            return;
        }

        var radios = _options.Where(o => o.IsEnabled).ToList();
        if (radios.Count == 0)
        {
            return;
        }

        var index = radios.IndexOf(current);
        AlertOptionButton? next = e.Key switch
        {
            Key.Down or Key.Right => index == radios.Count - 1 ? radios[0] : radios[index + 1],
            Key.Up or Key.Left => index <= 0 ? radios[^1] : radios[index - 1],
            _ => null,
        };
        if (next is { Input: { } input })
        {
            e.Handled = true;
            RbClick(input);
            next.Focus(NavigationMethod.Directional);
            next.BringIntoView();
        }
    }

    private void LogFault(Task task, string what) =>
        task.ContinueWith(
            t => Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "The alert's {What} failed: {Exception}", what, t.Exception!.InnerException),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
}
