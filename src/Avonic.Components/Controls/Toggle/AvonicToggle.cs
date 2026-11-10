using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-toggle</c>: a switch, a track and a handle beside a label, flipped by a click, Space, or a drag on the track. The
/// look is in the theme. See <c>docs/components/ion-toggle/README.md</c>.
/// </summary>
/// <remarks>
/// A click is the DOM <c>click</c> on Ionic's host, a <c>&lt;div&gt;</c>, so <see cref="NativeButton"/> with no keys. The drag is
/// <see cref="Gesture"/> on <c>PART_Track</c>, with Ionic's numbers. RTL is the toggle's <see cref="Visual.FlowDirection"/>.
/// </remarks>
[TemplatePart("PART_Track", typeof(Control))]
[TemplatePart("PART_Handle", typeof(Control))]
[TemplatePart("PART_Label", typeof(Control))]
[TemplatePart("PART_LabelContent", typeof(ContentPresenter))]
[TemplatePart("PART_HelperText", typeof(TextBlock))]
[TemplatePart("PART_ErrorText", typeof(TextBlock))]
[PseudoClasses(":checked", ":activated", ":in-item", ":label-text-wrapper-hidden", ":toggle-disabled")]
[PseudoClasses(":justify-end", ":justify-space-between", ":justify-start", ":alignment-center", ":alignment-start")]
[PseudoClasses(":label-placement-end", ":label-placement-fixed", ":label-placement-stacked", ":label-placement-start")]
public class AvonicToggle : ContentControl, IItemControl
{
    /// <summary><c>createGesture</c>'s <c>threshold</c>: the drag decides 5 px from the press.</summary>
    public const double GestureThreshold = 5;

    /// <summary><c>createGesture</c>'s <c>gesturePriority</c>.</summary>
    public const int GesturePriority = 100;

    /// <summary><c>shouldToggle</c>'s margin: a drag flips the toggle once it's this far past the capture toward the other side.</summary>
    public const double DragMargin = 10;

    /// <summary><c>lastDrag + 300 &lt; Date.now()</c>: a click this soon after a drag's end is dropped.</summary>
    public static readonly TimeSpan ClickAfterDrag = TimeSpan.FromMilliseconds(300);

    /// <summary>The style classes Ionic's <c>checkInvalidState</c> reads: both mean invalid.</summary>
    public const string TouchedClass = "ion-touched";

    /// <inheritdoc cref="TouchedClass" />
    public const string InvalidClass = "ion-invalid";

    /// <summary>Ionic <c>checked</c>. Two-way by default, as the toggle sets it itself.</summary>
    public static readonly StyledProperty<bool> CheckedProperty =
        AvaloniaProperty.Register<AvonicToggle, bool>(nameof(Checked), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>color</c>: the checked track and handle.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicToggle>();

    /// <summary>Ionic <c>value</c>: what <see cref="IonChange"/> carries. <c>"on"</c> by default.</summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<AvonicToggle, string?>(nameof(Value), "on");

    /// <summary>Ionic <c>enableOnOffLabels</c>: an icon in the handle that says on or off.</summary>
    public static readonly StyledProperty<bool> EnableOnOffLabelsProperty =
        AvaloniaProperty.Register<AvonicToggle, bool>(nameof(EnableOnOffLabels));

    /// <summary>Ionic <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<ToggleLabelPlacement> LabelPlacementProperty =
        TogglePanel.LabelPlacementProperty.AddOwner<AvonicToggle>();

    /// <summary>Ionic <c>justify</c>. Set, the toggle fills the width, as <c>display: block</c> does.</summary>
    public static readonly StyledProperty<ToggleJustify?> JustifyProperty =
        TogglePanel.JustifyProperty.AddOwner<AvonicToggle>();

    /// <summary>Ionic <c>alignment</c>. Set, the toggle fills the width, as <c>display: block</c> does.</summary>
    public static readonly StyledProperty<ToggleAlignment?> AlignmentProperty =
        TogglePanel.AlignmentProperty.AddOwner<AvonicToggle>();

    /// <summary>Ionic <c>helperText</c>: under the label, unless the toggle is invalid.</summary>
    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<AvonicToggle, string?>(nameof(HelperText));

    /// <summary>Ionic <c>errorText</c>: under the label while it has the style classes <c>ion-touched</c> and <c>ion-invalid</c>.</summary>
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<AvonicToggle, string?>(nameof(ErrorText));

    /// <summary>Ionic <c>ionChange</c>: the user flipped it. Not raised when <see cref="Checked"/> is set in code.</summary>
    public static readonly RoutedEvent<ToggleChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicToggle, ToggleChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionFocus</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicToggle, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicToggle, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    // ── For the theme ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>translate3d(calc(100% - var(--handle-width)), 0, 0)</c> on the icon wrapper when checked: the values are the wrapper's
    /// width, which is the track's, then the handle's width. A width that's set, not <c>Bounds</c>, which a layout changes later,
    /// and the transition would then show.
    /// </summary>
    public static readonly IMultiValueConverter CheckedOffsetConverter =
        new FuncMultiValueConverter<object?, ITransform>(values =>
        {
            var numbers = values.Select(Number).ToArray();
            return Translate(numbers.ElementAtOrDefault(0) - numbers.ElementAtOrDefault(1));
        });

    /// <summary><c>translate3d(calc(var(--handle-spacing) * -2), 0, 0)</c> on the handle when checked. The value is the spacing.</summary>
    public static readonly IMultiValueConverter CheckedSpacingConverter =
        new FuncMultiValueConverter<object?, ITransform>(values => Translate(-2 * Number(values.FirstOrDefault())));

    /// <summary><c>left: var(--handle-spacing)</c>: the handle's inset from the track's start, as a margin. The value is the spacing.</summary>
    public static readonly IMultiValueConverter SpacingMarginConverter =
        new FuncMultiValueConverter<object?, Thickness>(values => new Thickness(Number(values.FirstOrDefault()), 0, 0, 0));

    private static double Number(object? value) => value is double number && double.IsFinite(number) ? number : 0;

    private static ITransform Translate(double x)
    {
        var builder = TransformOperations.CreateBuilder(1);
        builder.AppendTranslate(x, 0);
        return builder.Build();
    }

    private Control? _track;
    private ContentPresenter? _labelContent;
    private Gesture? _gesture;
    private TimeSpan? _lastDrag;
    private bool _inItem;

    static AvonicToggle()
    {
        // tabindex="0", unless disabled, which Avalonia already leaves out.
        FocusableProperty.OverrideDefaultValue<AvonicToggle>(true);

        // overflow: visible. The handle hangs 3 px over the track, and over the toggle when there's no label.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicToggle>(false);
    }

    public AvonicToggle()
    {
        // The host's onClick: the plain DOM click on release of every press, with no keys, as it's a <div>.
        _ = new NativeButton(this, _ => OnClick(), () => false, UpdateIsEffectivelyEnabled);

        UpdateClasses();
    }

    public bool Checked
    {
        get => GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool EnableOnOffLabels
    {
        get => GetValue(EnableOnOffLabelsProperty);
        set => SetValue(EnableOnOffLabelsProperty, value);
    }

    public ToggleLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public ToggleJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    public ToggleAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    public string? HelperText
    {
        get => GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public event EventHandler<ToggleChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonFocus
    {
        add => AddHandler(IonFocusEvent, value);
        remove => RemoveHandler(IonFocusEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonBlur
    {
        add => AddHandler(IonBlurEvent, value);
        remove => RemoveHandler(IonBlurEvent, value);
    }

    /// <summary><c>checkInvalidState</c>: the app has marked it both touched and invalid.</summary>
    private bool IsInvalid => Classes.Contains(TouchedClass) && Classes.Contains(InvalidClass);

    /// <summary><c>hasLabel</c>: <c>textContent !== ''</c>. Any control counts, as its text isn't read here.</summary>
    private bool HasLabel => Content is not (null or "");

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _track = e.NameScope.Find<Control>("PART_Track");
        _labelContent = e.NameScope.Find<ContentPresenter>("PART_LabelContent");
        SetupGesture();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // hostContext('ion-item'), at render. Read again here, as a moved toggle is attached again.
        _inItem = this.FindAncestorOfType<AvonicItem>() is not null;
        UpdateClasses();
        SetupGesture();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // disconnectedCallback
        _gesture?.Destroy();
        _gesture = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary><c>setupGesture</c>: on the track, once it exists and the toggle is in the tree. Then <c>disabledChanged</c>.</summary>
    private void SetupGesture()
    {
        _gesture?.Destroy();
        _gesture = null;
        if (_track is not InputElement track || !this.IsAttachedToVisualTree())
        {
            return;
        }

        _gesture = Gesture.Create(new GestureConfig
        {
            Element = track,
            GestureName = "toggle",
            GesturePriority = GesturePriority,
            Threshold = GestureThreshold,
            OnStart = _ => OnDragStart(),
            OnMove = OnDragMove,
            OnEnd = OnDragEnd,
        });
        _gesture.Enable(IsEffectivelyEnabled);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEffectivelyEnabledProperty)
        {
            // disabledChanged
            _gesture?.Enable(IsEffectivelyEnabled);
        }
        else if (change.Property == CheckedProperty || change.Property == JustifyProperty || change.Property == AlignmentProperty
                 || change.Property == LabelPlacementProperty || change.Property == ContentProperty || change.Property == IsEnabledProperty)
        {
            UpdateClasses();
        }
    }

    /// <summary><c>render</c>'s host classes.</summary>
    private void UpdateClasses()
    {
        PseudoClasses.Set(":checked", Checked);
        PseudoClasses.Set(":toggle-disabled", !IsEnabled);
        PseudoClasses.Set(":in-item", _inItem);
        PseudoClasses.Set(":label-text-wrapper-hidden", !HasLabel);

        var justify = Justify;
        PseudoClasses.Set(":justify-end", justify == ToggleJustify.End);
        PseudoClasses.Set(":justify-space-between", justify == ToggleJustify.SpaceBetween);
        PseudoClasses.Set(":justify-start", justify == ToggleJustify.Start);

        var alignment = Alignment;
        PseudoClasses.Set(":alignment-center", alignment == ToggleAlignment.Center);
        PseudoClasses.Set(":alignment-start", alignment == ToggleAlignment.Start);

        var placement = LabelPlacement;
        PseudoClasses.Set(":label-placement-end", placement == ToggleLabelPlacement.End);
        PseudoClasses.Set(":label-placement-fixed", placement == ToggleLabelPlacement.Fixed);
        PseudoClasses.Set(":label-placement-stacked", placement == ToggleLabelPlacement.Stacked);
        PseudoClasses.Set(":label-placement-start", placement == ToggleLabelPlacement.Start);
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>onClick</c>: a click, or <c>click()</c> from an item, unless disabled or just after a drag.</summary>
    private void OnClick()
    {
        if (!IsEffectivelyEnabled)
        {
            return;
        }

        if (_lastDrag is not { } lastDrag || lastDrag + ClickAfterDrag < AvonicConfig.Now())
        {
            ToggleChecked();
        }
    }

    /// <summary>
    /// <c>onKeyDown</c>: Space, on every key down, repeats too. Enter does nothing. Ionic checks <c>disabled</c> here too, for a
    /// toggle an app has made focusable while disabled; Avalonia gives a disabled control neither focus nor keys.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space && !e.Handled)
        {
            e.Handled = true;
            ToggleChecked();
        }
    }

    /// <summary><c>toggleChecked</c>.</summary>
    private void ToggleChecked()
    {
        var isNowChecked = !Checked;
        SetCurrentValue(CheckedProperty, isNowChecked);
        RaiseEvent(new ToggleChangeEventArgs(IonChangeEvent, this, isNowChecked, Value));
    }

    /// <summary><c>onStart</c>: <c>activated</c>, and <c>setFocus</c>.</summary>
    private void OnDragStart()
    {
        PseudoClasses.Set(":activated", true);
        Focus(NavigationMethod.Pointer);
    }

    /// <summary><c>onMove</c>: <c>shouldToggle(isRTL, checked, deltaX, -10)</c>.</summary>
    private void OnDragMove(GestureDetail detail)
    {
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var margin = -DragMargin;
        var deltaX = detail.DeltaX;
        var shouldToggle = Checked
            ? (!rtl && margin > deltaX) || (rtl && -margin < deltaX)
            : (!rtl && -margin < deltaX) || (rtl && margin > deltaX);

        if (shouldToggle)
        {
            ToggleChecked();
        }
    }

    /// <summary><c>onEnd</c>: no longer <c>activated</c>, and the time, for <see cref="OnClick"/>.</summary>
    private void OnDragEnd(GestureDetail detail)
    {
        PseudoClasses.Set(":activated", false);
        _lastDrag = AvonicConfig.Now();

        // ev.event.preventDefault(); ev.event.stopImmediatePropagation()
        if (detail.Event is { } pointerEvent)
        {
            pointerEvent.Handled = true;
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        // onFocus is on the host, and focus doesn't bubble, so focus on something in the label isn't this.
        if (ReferenceEquals(e.Source, this))
        {
            RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);

        if (ReferenceEquals(e.Source, this))
        {
            RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
        }
    }

    // ── Item ─────────────────────────────────────────────────────────────────────────────────

    ItemControlType IItemControl.ItemControlType => ItemControlType.Toggle;

    /// <summary>The item's <c>firstInteractive.click()</c>, which reaches <c>onClick</c>.</summary>
    void IItemControl.OnItemTap() => OnClick();

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>aria-labelledby</c> the label part: the slot's text, and the helper text while it's shown, as Chromium names it,
    /// measured. The error text isn't in the name: it's <c>role="alert"</c>.
    /// </summary>
    private string AccessibleName()
    {
        if (!HasLabel)
        {
            return "";
        }

        var text = _labelContent?.Child is { } child ? AccessibleText.Of(child) : Content as string ?? "";
        return !IsInvalid && !string.IsNullOrEmpty(HelperText) ? $"{text} {HelperText}".Trim() : text;
    }

    /// <summary>
    /// <c>aria-describedby</c>: <c>getHintTextId</c>, the error text when invalid and set, or else the helper text's element, which
    /// is empty while invalid, as <c>renderHintText</c> leaves it.
    /// </summary>
    private string? HintText() =>
        IsInvalid ? (string.IsNullOrEmpty(ErrorText) ? null : ErrorText)
        : string.IsNullOrEmpty(HelperText) ? null
        : HelperText;

    protected override AutomationPeer OnCreateAutomationPeer() => new TogglePeer(this);

    /// <summary><c>role="switch"</c>: a button with the toggle pattern, as Avalonia's own <c>ToggleSwitch</c> is.</summary>
    private sealed class TogglePeer : ControlAutomationPeer, IToggleProvider
    {
        private readonly AvonicToggle _owner;

        public TogglePeer(AvonicToggle owner)
            : base(owner)
        {
            _owner = owner;

            // aria-checked follows checked, as Avalonia's own ToggleButton peer follows IsChecked.
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == CheckedProperty)
                {
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, State(e.GetOldValue<bool>()), State(e.GetNewValue<bool>()));
                }
                else if (e.Property == HelperTextProperty || e.Property == ErrorTextProperty || e.Property == ContentProperty)
                {
                    RaiseTextChanged();
                }
            };

            // The MutationObserver on class: a new aria-describedby, so that a screen reader announces the error.
            owner.Classes.CollectionChanged += (_, _) => RaiseTextChanged();
        }

        private string? _name;
        private string? _help;

        private void RaiseTextChanged()
        {
            var name = GetName();
            var help = GetHelpText();
            if (name != _name)
            {
                RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, _name, name);
                _name = name;
            }

            if (help != _help)
            {
                RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, _help, help);
                _help = help;
            }
        }

        public ToggleState ToggleState => State(_owner.Checked);

        private static ToggleState State(bool isChecked) => isChecked ? ToggleState.On : ToggleState.Off;

        /// <summary>A screen reader's activation is a click, which a disabled toggle doesn't take.</summary>
        public void Toggle() => _owner.OnClick();

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        /// <summary><c>aria-labelledby</c> the label, whenever there is one, which beats <c>aria-label</c>: measured.</summary>
        protected override string? GetNameCore() => _owner.HasLabel ? _owner.AccessibleName() : base.GetNameCore();

        protected override string? GetHelpTextCore()
        {
            var help = base.GetHelpTextCore();
            return string.IsNullOrEmpty(help) ? _owner.HintText() : help;
        }
    }
}
