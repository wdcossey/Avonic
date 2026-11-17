using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avonic.Components.Animation;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ion-action-sheet</c>, md: a sheet of buttons from the bottom of the window, under an optional header, over a
/// backdrop. Present it in code (<c>new AvonicActionSheet { Header = …, Buttons = [...] }.Present()</c>), or declare it in a page
/// and open it with <see cref="AvonicOverlay.IsOpen"/> or <see cref="AvonicOverlay.Trigger"/>. A button dismisses it with its
/// <see cref="ActionSheetButton.Data"/> and <see cref="ActionSheetButton.Role"/>. See
/// <c>docs/components/ion-action-sheet/README.md</c>.
/// </summary>
[TemplatePart("PART_Backdrop", typeof(AvonicBackdrop))]
[TemplatePart("PART_Wrapper", typeof(Control))]
[TemplatePart("PART_Buttons", typeof(Panel))]
[TemplatePart("PART_CancelGroup", typeof(Decorator))]
[PseudoClasses(HasCancel)]
public class AvonicActionSheet : AvonicOverlay
{
    /// <summary>There's a cancel button, so <c>.action-sheet-group-cancel</c> is rendered, and is the last group.</summary>
    public const string HasCancel = ":has-cancel";

    /// <summary>md's enter: 400 ms.</summary>
    internal const double EnterDuration = 400;

    /// <summary>md's leave: 450 ms.</summary>
    internal const double LeaveDuration = 450;

    /// <summary>md's easing, both ways.</summary>
    internal const string Easing = "cubic-bezier(.36,.66,.04,1)";

    /// <summary>Ionic <c>backdropDismiss</c>: a tap on the backdrop, Escape and back dismiss it.</summary>
    public static readonly StyledProperty<bool> BackdropDismissProperty =
        AvaloniaProperty.Register<AvonicActionSheet, bool>(nameof(BackdropDismiss), defaultValue: true);

    /// <summary>Ionic <c>header</c>: the title, shown when it isn't null.</summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<AvonicActionSheet, string?>(nameof(Header));

    /// <summary>Ionic <c>subHeader</c>: under the header, when there's one and this isn't empty.</summary>
    public static readonly StyledProperty<string?> SubHeaderProperty =
        AvaloniaProperty.Register<AvonicActionSheet, string?>(nameof(SubHeader));

    /// <summary>Ionic <c>buttons</c>.</summary>
    public static readonly StyledProperty<IList<ActionSheetButton>?> ButtonsProperty =
        AvaloniaProperty.Register<AvonicActionSheet, IList<ActionSheetButton>?>(nameof(Buttons));

    /// <summary>Ionic <c>cssClass</c>: style classes on the action sheet, space-separated.</summary>
    public static readonly StyledProperty<string?> CssClassProperty =
        AvaloniaProperty.Register<AvonicActionSheet, string?>(nameof(CssClass));

    /// <summary>
    /// Ionic <c>htmlAttributes</c>, spread on the host: <c>aria-label</c> names it when there's no header, as
    /// <c>aria-labelledby</c> beats it; <c>id</c> is its <c>AutomationId</c>. Others have nothing to do in Avalonia, and are ignored.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, object?>?> HtmlAttributesProperty =
        AvaloniaProperty.Register<AvonicActionSheet, IReadOnlyDictionary<string, object?>?>(nameof(HtmlAttributes));

    /// <summary>Ionic <c>ionActionSheetWillPresent</c>, raised before <see cref="AvonicOverlay.WillPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonActionSheetWillPresentEvent =
        RoutedEvent.Register<AvonicActionSheet, RoutedEventArgs>(nameof(IonActionSheetWillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionActionSheetDidPresent</c>, raised before <see cref="AvonicOverlay.DidPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonActionSheetDidPresentEvent =
        RoutedEvent.Register<AvonicActionSheet, RoutedEventArgs>(nameof(IonActionSheetDidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionActionSheetWillDismiss</c>, raised before <see cref="AvonicOverlay.WillDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonActionSheetWillDismissEvent =
        RoutedEvent.Register<AvonicActionSheet, OverlayEventArgs>(nameof(IonActionSheetWillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionActionSheetDidDismiss</c>, raised before <see cref="AvonicOverlay.DidDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonActionSheetDidDismissEvent =
        RoutedEvent.Register<AvonicActionSheet, OverlayEventArgs>(nameof(IonActionSheetDidDismiss), RoutingStrategies.Bubble);

    private readonly List<ActionSheetNativeButton> _buttonControls = [];
    private ActionSheetNativeButton? _cancelControl;
    private string[] _cssClasses = [];
    private INotifyCollectionChanged? _watchedButtons;
    private bool _buttonsPending;

    /// <summary>Ionic's <c>activeRadioId</c>: the checked radio's <see cref="ButtonKey"/>.</summary>
    private string? _activeRadio;

    private AvonicBackdrop? _backdrop;
    private Control? _wrapper;
    private Panel? _buttonsPanel;
    private Decorator? _cancelGroup;

    public AvonicActionSheet()
    {
        SetCurrentValue(ButtonsProperty, new AvaloniaList<ActionSheetButton>());

        // The host's onIonActionSheetWillDismiss, Ionic's dispatchCancelHandler.
        AddHandler(IonActionSheetWillDismissEvent, OnWillDismissCancel);

        // Ionic's keydown listener on the host; Tunnel, so that the group's scroller doesn't take the arrows first.
        AddHandler(KeyDownEvent, OnSheetKeyDown, RoutingStrategies.Tunnel);
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

    public IList<ActionSheetButton>? Buttons
    {
        get => GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
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

    public event EventHandler<RoutedEventArgs>? IonActionSheetWillPresent
    {
        add => AddHandler(IonActionSheetWillPresentEvent, value);
        remove => RemoveHandler(IonActionSheetWillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonActionSheetDidPresent
    {
        add => AddHandler(IonActionSheetDidPresentEvent, value);
        remove => RemoveHandler(IonActionSheetDidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonActionSheetWillDismiss
    {
        add => AddHandler(IonActionSheetWillDismissEvent, value);
        remove => RemoveHandler(IonActionSheetWillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonActionSheetDidDismiss
    {
        add => AddHandler(IonActionSheetDidDismissEvent, value);
        remove => RemoveHandler(IonActionSheetDidDismissEvent, value);
    }

    /// <summary>The backdrop part.</summary>
    internal AvonicBackdrop? Backdrop => _backdrop;

    /// <summary>The wrapper, <c>.action-sheet-wrapper</c>.</summary>
    internal Control? Wrapper => _wrapper;

    /// <summary>The buttons' controls in the first group, in order.</summary>
    internal IReadOnlyList<ActionSheetNativeButton> ButtonControls => _buttonControls;

    /// <summary>The cancel button's control, when there's one.</summary>
    internal ActionSheetNativeButton? CancelControl => _cancelControl;

    /// <summary>Every button's control, the cancel button last, as they're shown.</summary>
    internal IEnumerable<ActionSheetNativeButton> AllButtonControls =>
        _cancelControl is null ? _buttonControls : _buttonControls.Append(_cancelControl);

    /// <summary>The header and sub header, as <c>aria-labelledby</c> names the host; else <c>aria-label</c>, else its own name.</summary>
    internal string? AccessibleName
    {
        get
        {
            if (Header is not null)
            {
                var parts = new[] { Header, SubHeader }.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (parts.Length > 0)
                {
                    return string.Join(" ", parts);
                }
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

    protected override RoutedEvent<RoutedEventArgs> ControlWillPresentEvent => IonActionSheetWillPresentEvent;

    protected override RoutedEvent<RoutedEventArgs> ControlDidPresentEvent => IonActionSheetDidPresentEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlWillDismissEvent => IonActionSheetWillDismissEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlDidDismissEvent => IonActionSheetDidDismissEvent;

    /// <summary>
    /// <c>md.enter.js</c>: 400 ms <c>cubic-bezier(.36,.66,.04,1)</c>, the backdrop to <c>--backdrop-opacity</c>, the wrapper up from
    /// <c>translateY(100%)</c>.
    /// </summary>
    protected override AnimationBuilder MdEnterAnimation => (baseElement, _) =>
    {
        var sheet = (AvonicActionSheet)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(sheet._backdrop)
            .FromTo("opacity", 0.01, sheet.BackdropOpacity())
            .BeforeStyles(new() { ["pointer-events"] = "none" })
            .AfterClearStyles(["pointer-events"]);
        var wrapper = AvonicAnimation.Create()
            .AddElement(sheet._wrapper)
            .FromTo("transform", "translateY(100%)", "translateY(0%)");
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing(Easing)
            .Duration(EnterDuration)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary><c>md.leave.js</c>: 450 ms, the same curve, the backdrop to 0, the wrapper down to <c>translateY(100%)</c>.</summary>
    protected override AnimationBuilder MdLeaveAnimation => (baseElement, _) =>
    {
        var sheet = (AvonicActionSheet)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(sheet._backdrop)
            .FromTo("opacity", sheet.BackdropOpacity(), 0);
        var wrapper = AvonicAnimation.Create()
            .AddElement(sheet._wrapper)
            .FromTo("transform", "translateY(0%)", "translateY(100%)");
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing(Easing)
            .Duration(LeaveDuration)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary>
    /// <c>var(--backdrop-opacity)</c>: <c>Avonic.ActionSheet.BackdropOpacity</c>, falling back to <c>Avonic.BackdropOpacity</c>
    /// (<c>--ion-backdrop-opacity</c>), then 0.32.
    /// </summary>
    internal double BackdropOpacity() => OverlayHandler.BackdropOpacity(this, "Avonic.ActionSheet.BackdropOpacity");

    /// <summary>The render, once there's an overlay index for the buttons' ids.</summary>
    protected override void OnPresenting() => RenderButtons();

    protected override AutomationPeer OnCreateAutomationPeer() => new ActionSheetAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _backdrop = e.NameScope.Find<AvonicBackdrop>("PART_Backdrop");
        _wrapper = e.NameScope.Find<Control>("PART_Wrapper");
        _buttonsPanel?.Children.Clear();
        _buttonsPanel = e.NameScope.Find<Panel>("PART_Buttons");
        _cancelGroup?.Child = null;
        _cancelGroup = e.NameScope.Find<Decorator>("PART_CancelGroup");
        RenderButtons();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ButtonsProperty)
        {
            if (_watchedButtons is not null)
            {
                _watchedButtons.CollectionChanged -= OnButtonsCollectionChanged;
            }

            _watchedButtons = change.NewValue as INotifyCollectionChanged;
            if (_watchedButtons is not null)
            {
                _watchedButtons.CollectionChanged += OnButtonsCollectionChanged;
            }

            ButtonsChanged();
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

    // Changes to the list in place come in a batch, as one render after them, as Stencil renders once for many changes.
    private void OnButtonsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_buttonsPending)
        {
            return;
        }

        _buttonsPending = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(ButtonsChanged);
    }

    // ── Ionic's buttons ─────────────────────────────────────────────────────────────────────

    /// <summary>The first <c>cancel</c> button, which is rendered last, on its own.</summary>
    private ActionSheetButton? CancelButton => Buttons?.FirstOrDefault(b => b?.Role == "cancel");

    /// <summary>The others, in order: what the first group renders.</summary>
    private List<ActionSheetButton> GroupButtons => Buttons?.Where(b => b is not null && b.Role != "cancel").ToList() ?? [];

    private static bool IsRadio(ActionSheetButton button) =>
        button.HtmlAttributes is { } attributes && attributes.TryGetValue("role", out var role) && role as string == "radio";

    /// <summary>
    /// What identifies a button of the first group for <c>activeRadioId</c>: its <c>id</c>, else its place. Ionic's id also holds
    /// the overlay index, which a present can change, and counts the cancel button when it's set but not when it's rendered;
    /// neither is copied (PLAN.md).
    /// </summary>
    private static string ButtonKey(ActionSheetButton button, int index) => button.Id ?? "#" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// <c>buttonsChanged</c>: with radios, the first whose <c>aria-checked</c> is <c>"true"</c> becomes the checked one; with none,
    /// the checked one stays. Then the render.
    /// </summary>
    private void ButtonsChanged()
    {
        _buttonsPending = false;
        var buttons = GroupButtons;
        for (var i = 0; i < buttons.Count; i++)
        {
            if (IsRadio(buttons[i]) && buttons[i].HtmlAttributes!.TryGetValue("aria-checked", out var isChecked) && isChecked as string == "true")
            {
                _activeRadio = ButtonKey(buttons[i], i);
                break;
            }
        }

        RenderButtons();
    }

    /// <summary><c>render</c>'s buttons: the first group's, then the cancel group's.</summary>
    private void RenderButtons()
    {
        if (_buttonsPanel is null)
        {
            return;
        }

        // Ionic's render patches the buttons that are there, so one with focus keeps it: the one in its place takes it here.
        var all = AllButtonControls.ToList();
        var focused = all.FindIndex(c => c.IsKeyboardFocusWithin);
        foreach (var control in all)
        {
            control.Clicked -= OnButtonClicked;
        }

        _buttonControls.Clear();
        _buttonsPanel.Children.Clear();
        _cancelControl = null;
        if (_cancelGroup is not null)
        {
            _cancelGroup.Child = null;
        }

        var buttons = GroupButtons;
        var radios = buttons.Where(IsRadio).ToList();
        var activeExists = _activeRadio is not null;
        for (var i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            var isRadio = IsRadio(button);
            var control = new ActionSheetNativeButton { Button = button, Index = i, IsRadio = isRadio };
            Fill(control, button);
            AutomationProperties.SetAutomationId(control, button.Id ?? $"action-sheet-button-{OverlayIndex}-{i}");
            control.IsEnabled = !button.Disabled;
            if (isRadio)
            {
                var active = ButtonKey(button, i) == _activeRadio;
                control.IsChecked = active;

                // A radio's action-sheet-selected is whether it's checked, which comes after its role's class.
                control.IsSelected = active;

                // The one tab stop: the checked radio, or the first when none is.
                KeyboardNavigation.SetIsTabStop(control, active || !activeExists && ReferenceEquals(button, radios[0]));
            }

            control.Clicked += OnButtonClicked;
            _buttonControls.Add(control);
            _buttonsPanel.Children.Add(control);
        }

        if (CancelButton is { } cancel)
        {
            var control = new ActionSheetNativeButton { Button = cancel };
            Fill(control, cancel);
            if (cancel.HtmlAttributes is { } attributes)
            {
                // Spread, with nothing of Ionic's after it: disabled and id reach the cancel button.
                if (attributes.TryGetValue("disabled", out var disabled) && AlertClasses.IsTrue(disabled))
                {
                    control.IsEnabled = false;
                }

                if (attributes.TryGetValue("id", out var id) && id?.ToString() is { Length: > 0 } automationId)
                {
                    AutomationProperties.SetAutomationId(control, automationId);
                }
            }

            control.Clicked += OnButtonClicked;
            _cancelControl = control;
            _cancelGroup?.Child = control;
        }

        PseudoClasses.Set(HasCancel, _cancelControl is not null);
        RefocusAt(focused);
    }

    private static void Fill(ActionSheetNativeButton control, ActionSheetButton button)
    {
        control.Text = button.Text;
        control.Icon = button.Icon;
        control.Role = button.Role;
        control.IsSelected = button.Role == "selected";
        AlertClasses.Apply(control, button.CssClass);
        if (button.HtmlAttributes is { } attributes && attributes.TryGetValue("aria-label", out var label) && label is not null)
        {
            AutomationProperties.SetName(control, label.ToString());
        }
    }

    /// <summary>
    /// Focus that was on a button a render replaced goes to the one in its place, or the sheet if there's none, rather than out of
    /// the sheet, as Ionic's patched element keeps it.
    /// </summary>
    private void RefocusAt(int index)
    {
        if (index < 0)
        {
            return;
        }

        var controls = AllButtonControls.ToList();
        var target = index < controls.Count ? controls[index] : null;
        if (target is null || !target.Focus())
        {
            Focus();
        }
    }

    /// <summary><c>selectRadioButton</c>: the radio becomes the checked one, and the one tab stop.</summary>
    private void SelectRadio(ActionSheetNativeButton selected)
    {
        if (selected.Button is not { } button)
        {
            return;
        }

        _activeRadio = ButtonKey(button, selected.Index);
        foreach (var control in _buttonControls.Where(c => c.IsRadio))
        {
            var active = ReferenceEquals(control, selected);
            control.IsChecked = active;
            control.IsSelected = active;
            KeyboardNavigation.SetIsTabStop(control, active);
        }
    }

    // ── Input ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The button's <c>onClick</c>: a radio is checked first; then <c>buttonClick</c>.</summary>
    private void OnButtonClicked(object? sender, EventArgs e)
    {
        if (sender is not ActionSheetNativeButton { Button: { } button } control)
        {
            return;
        }

        if (control.IsRadio)
        {
            SelectRadio(control);
        }

        LogFault(ButtonClick(button), "button's handler");
    }

    /// <summary>
    /// <c>buttonClick</c>: a <c>cancel</c> or <c>backdrop</c> role dismisses with the button's data and role at once (its handler
    /// then runs from the will-dismiss listener); otherwise the handler, awaited: false keeps the sheet open.
    /// </summary>
    internal async Task<bool> ButtonClick(ActionSheetButton button)
    {
        if (OverlayRoles.IsCancel(button.Role))
        {
            return await Dismiss(button.Data, button.Role);
        }

        if (!await CallButtonHandler(button))
        {
            return false;
        }

        return await Dismiss(button.Data, button.Role);
    }

    /// <summary><c>callButtonHandler</c>: false when the handler said false.</summary>
    private static async Task<bool> CallButtonHandler(ActionSheetButton? button) =>
        button?.Handler is not { } handler || await OverlayHandler.Run(handler) is not false;

    /// <summary>
    /// <c>dispatchCancelHandler</c>: a dismiss with a cancel role (a cancel button, the backdrop, Escape, back) calls the first cancel
    /// button's handler, and doesn't wait for it or read what it returns.
    /// </summary>
    private void OnWillDismissCancel(object? sender, OverlayEventArgs e)
    {
        if (ReferenceEquals(e.Source, this) && OverlayRoles.IsCancel(e.Role) && CancelButton is { } cancel)
        {
            LogFault(CallButtonHandler(cancel), "cancel button's handler");
        }
    }

    /// <summary>
    /// Ionic's <c>onKeydown</c>, with radios, while presented, on a radio: ArrowDown and ArrowRight focus and check the next enabled
    /// radio, ArrowUp and ArrowLeft the previous, wrapping. Space and Enter, which check it and click it, are its native button's
    /// click here, which does the same.
    /// </summary>
    private void OnSheetKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsShown || e.Source is not ActionSheetNativeButton { IsRadio: true } current || !_buttonControls.Contains(current))
        {
            return;
        }

        var radios = _buttonControls.Where(c => c.IsRadio && c.IsEnabled).ToList();
        var index = radios.IndexOf(current);
        if (index < 0)
        {
            return;
        }

        var next = e.Key switch
        {
            Key.Down or Key.Right => index == radios.Count - 1 ? radios[0] : radios[index + 1],
            Key.Up or Key.Left => index == 0 ? radios[^1] : radios[index - 1],
            _ => null,
        };
        if (next is not null)
        {
            e.Handled = true;
            SelectRadio(next);
            next.Focus(NavigationMethod.Directional);
            next.BringIntoView();
        }
    }

    /// <summary>
    /// The host: <c>role="dialog"</c>, <c>aria-modal</c>, to automation a <see cref="AutomationControlType.Window"/>, Avalonia's
    /// nearest type to a dialog, named by its header and sub header (<c>aria-labelledby</c>), else <c>aria-label</c>; its
    /// <c>AutomationId</c> <c>htmlAttributes.id</c>.
    /// </summary>
    private sealed class ActionSheetAutomationPeer(AvonicActionSheet owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

        protected override string? GetNameCore() => owner.AccessibleName;

        protected override string? GetAutomationIdCore() =>
            owner.HtmlAttributes is { } attributes && attributes.TryGetValue("id", out var value) && value?.ToString() is { Length: > 0 } id
                ? id
                : base.GetAutomationIdCore();

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;
    }
}
