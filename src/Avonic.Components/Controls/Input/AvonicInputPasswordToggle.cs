using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-input-password-toggle</c>: a round clear button, for an <see cref="AvonicInput"/>'s end slot, that shows or
/// hides the password by flipping the input's <see cref="AvonicInput.Type"/> between <see cref="InputType.Password"/> and
/// <see cref="InputType.Text"/>. See <c>docs/components/ion-input-password-toggle/README.md</c>.
/// </summary>
[TemplatePart("PART_Button", typeof(AvonicButton))]
[PseudoClasses(":password-visible", ":input-hidden")]
public class AvonicInputPasswordToggle : TemplatedControl
{
    /// <summary>The icon when the password is hidden, which shows it: Ionic's <c>eye</c>.</summary>
    public const string DefaultShowIcon = "eye";

    /// <summary>The icon when the password is shown, which hides it: Ionic's <c>eye-off</c>.</summary>
    public const string DefaultHideIcon = "eye-off";

    /// <summary>Ionic <c>color</c>: the button's.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicInputPasswordToggle>();

    /// <summary>Ionic <c>showIcon</c>: an <see cref="AvonicIcon.Icon"/>. Null is <c>eye</c>.</summary>
    public static readonly StyledProperty<string?> ShowIconProperty =
        AvaloniaProperty.Register<AvonicInputPasswordToggle, string?>(nameof(ShowIcon));

    /// <summary>Ionic <c>hideIcon</c>: an <see cref="AvonicIcon.Icon"/>. Null is <c>eye-off</c>.</summary>
    public static readonly StyledProperty<string?> HideIconProperty =
        AvaloniaProperty.Register<AvonicInputPasswordToggle, string?>(nameof(HideIcon));

    /// <summary>The icon the button shows now: <see cref="HideIcon"/> while the password is visible, else <see cref="ShowIcon"/>.</summary>
    public static readonly DirectProperty<AvonicInputPasswordToggle, string> IconProperty =
        AvaloniaProperty.RegisterDirect<AvonicInputPasswordToggle, string>(nameof(Icon), toggle => toggle.Icon);

    /// <summary>The button's <c>aria-label</c>: "Show password" or "Hide password".</summary>
    public static readonly DirectProperty<AvonicInputPasswordToggle, string> ButtonLabelProperty =
        AvaloniaProperty.RegisterDirect<AvonicInputPasswordToggle, string>(nameof(ButtonLabel), toggle => toggle.ButtonLabel);

    private AvonicInput? _input;

    public AvonicInputPasswordToggle()
    {
        // onPointerDown's preventDefault: a press on the button doesn't take focus from the field. On the toggle, which the
        // press tunnels through before it reaches the button.
        AddHandler(PointerPressedEvent, static (_, e) => KeepFocus.During(e), RoutingStrategies.Tunnel, handledEventsToo: true);
    }
    private AvonicButton? _button;
    private InputType _type = InputType.Password;
    private string _icon = DefaultShowIcon;
    private string _buttonLabel = "Show password";

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string? ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    public string? HideIcon
    {
        get => GetValue(HideIconProperty);
        set => SetValue(HideIconProperty, value);
    }

    public string Icon
    {
        get => _icon;
        private set => SetAndRaise(IconProperty, ref _icon, value);
    }

    public string ButtonLabel
    {
        get => _buttonLabel;
        private set => SetAndRaise(ButtonLabelProperty, ref _buttonLabel, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_button is not null)
        {
            _button.Click -= OnButtonClick;
        }

        _button = e.NameScope.Find<AvonicButton>("PART_Button");
        if (_button is not null)
        {
            _button.Click += OnButtonClick;
        }
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // connectedCallback: the closest ion-input, and its type. The input's onTypeChange keeps it, which watching does.
        Unwatch();
        _input = this.FindLogicalAncestorOfType<AvonicInput>();
        if (_input is not null)
        {
            _input.PropertyChanged += OnInputPropertyChanged;
        }

        Update();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        // disconnectedCallback
        Unwatch();
        base.OnDetachedFromLogicalTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowIconProperty || change.Property == HideIconProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        if (_input is { } input)
        {
            _type = input.Type;
        }

        var visible = _type == InputType.Text;
        Icon = visible ? HideIcon ?? DefaultHideIcon : ShowIcon ?? DefaultShowIcon;
        ButtonLabel = visible ? "Hide password" : "Show password";
        PseudoClasses.Set(":password-visible", visible);

        // :host([disabled]) ::slotted(ion-input-password-toggle), :host([readonly]) ...: visibility: hidden.
        PseudoClasses.Set(":input-hidden", _input is { } host && (!host.IsEnabled || host.Readonly));
    }

    /// <summary><c>togglePasswordVisibility</c>.</summary>
    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_input is not { } input)
        {
            return;
        }

        input.SetCurrentValue(AvonicInput.TypeProperty, input.Type == InputType.Text ? InputType.Password : InputType.Text);
    }



    private void Unwatch()
    {
        if (_input is not null)
        {
            _input.PropertyChanged -= OnInputPropertyChanged;
            _input = null;
        }
    }

    private void OnInputPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == AvonicInput.TypeProperty || e.Property == IsEnabledProperty || e.Property == AvonicInput.ReadonlyProperty)
        {
            Update();
        }
    }
}
