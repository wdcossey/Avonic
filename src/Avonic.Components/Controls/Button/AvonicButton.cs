using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-button</c>. The look is in the theme. See <c>docs/components/ion-button/README.md</c>.
/// </summary>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_IconOnly", typeof(ContentPresenter))]
[TemplatePart("PART_Start", typeof(ContentPresenter))]
[TemplatePart("PART_Content", typeof(ContentPresenter))]
[TemplatePart("PART_End", typeof(ContentPresenter))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(
    ":fill-clear", ":fill-default", ":fill-outline", ":fill-solid", ":size-large", ":size-small", ":shape-round",
    ":expand-block", ":expand-full", ":strong", ":has-icon-only", ":in-item")]
public class AvonicButton : TemplatedControl, IItemControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicButton>();

    /// <summary>Ionic <c>expand</c>. Null keeps the button as wide as its content.</summary>
    public static readonly StyledProperty<ButtonExpand?> ExpandProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonExpand?>(nameof(Expand));

    /// <summary>Ionic <c>fill</c>. Null is <see cref="ButtonFill.Solid"/>.</summary>
    public static readonly StyledProperty<ButtonFill?> FillProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonFill?>(nameof(Fill));

    /// <summary>Ionic <c>shape</c>.</summary>
    public static readonly StyledProperty<ButtonShape?> ShapeProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonShape?>(nameof(Shape));

    /// <summary>Ionic <c>size</c>. Null is <see cref="ButtonSize.Small"/> inside an item, and the standard size otherwise.</summary>
    public static readonly StyledProperty<ButtonSize?> SizeProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonSize?>(nameof(Size));

    /// <summary>Ionic <c>strong</c>: a heavier font weight.</summary>
    public static readonly StyledProperty<bool> StrongProperty =
        AvaloniaProperty.Register<AvonicButton, bool>(nameof(Strong));

    /// <summary>Ionic's default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicButton, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicButton, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Ionic's <c>start</c> slot.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicButton, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicButton, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicButton, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicButton, IDataTemplate?>(nameof(EndContentTemplate));

    /// <summary>Ionic's <c>icon-only</c> slot. Anything in it gives the button its icon-only shape.</summary>
    public static readonly StyledProperty<object?> IconOnlyContentProperty =
        AvaloniaProperty.Register<AvonicButton, object?>(nameof(IconOnlyContent));

    public static readonly StyledProperty<IDataTemplate?> IconOnlyContentTemplateProperty =
        AvaloniaProperty.Register<AvonicButton, IDataTemplate?>(nameof(IconOnlyContentTemplate));

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ButtonExpand? Expand
    {
        get => GetValue(ExpandProperty);
        set => SetValue(ExpandProperty, value);
    }

    public ButtonFill? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public ButtonShape? Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public ButtonSize? Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool Strong
    {
        get => GetValue(StrongProperty);
        set => SetValue(StrongProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    public IDataTemplate? StartContentTemplate
    {
        get => GetValue(StartContentTemplateProperty);
        set => SetValue(StartContentTemplateProperty, value);
    }

    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    public IDataTemplate? EndContentTemplate
    {
        get => GetValue(EndContentTemplateProperty);
        set => SetValue(EndContentTemplateProperty, value);
    }

    public object? IconOnlyContent
    {
        get => GetValue(IconOnlyContentProperty);
        set => SetValue(IconOnlyContentProperty, value);
    }

    public IDataTemplate? IconOnlyContentTemplate
    {
        get => GetValue(IconOnlyContentTemplateProperty);
        set => SetValue(IconOnlyContentTemplateProperty, value);
    }

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionFocus</c>: the button has focus, however it got there.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicButton, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>: the button lost focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicButton, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

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

    // ── Extensions ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avonic extension: the button was tapped or activated by keyboard. Ionic gets this from its native
    /// <c>&lt;button&gt;</c>.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    /// <summary>
    /// Avonic extension: run after <see cref="Click"/>, unless a handler set <c>Handled</c>. While it can't
    /// execute, the button is disabled. As on Avalonia's own <c>Button</c>.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<AvonicButton, ICommand?>(nameof(Command));

    /// <summary>Avonic extension: the parameter for <see cref="Command"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<AvonicButton, object?>(nameof(CommandParameter));

    /// <summary>
    /// Avonic extension: CSS's <c>text-transform</c>, for text given as a string. The md theme sets
    /// <see cref="ButtonTextTransform.Uppercase"/>, and setting <see cref="ButtonTextTransform.None"/> is
    /// <c>text-transform: none</c>. A <c>TextBlock</c> or a template's own text isn't changed.
    /// </summary>
    public static readonly StyledProperty<ButtonTextTransform> TextTransformProperty =
        AvaloniaProperty.Register<AvonicButton, ButtonTextTransform>(nameof(TextTransform));

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public ButtonTextTransform TextTransform
    {
        get => GetValue(TextTransformProperty);
        set => SetValue(TextTransformProperty, value);
    }

    // ── For the theme ────────────────────────────────────────────────────────────────────────

    /// <summary>For the template: a slot's content, then <see cref="TextTransformProperty"/>. Only a string changes.</summary>
    public static readonly IMultiValueConverter TextTransformConverter = new SlotTextConverter();

    /// <summary>
    /// The four <c>--padding-*</c> variables, as a margin on the part inside the overlay. The values are the
    /// font size, then start, top, end and bottom, each a number or <see cref="CssUnset"/>. The parameter is
    /// the default, inline then block, such as <c>1.1em 8</c>, or <c>icon-only</c> for the chain in
    /// <c>:host(.button-has-icon-only)</c>.
    /// </summary>
    public static readonly IMultiValueConverter PaddingConverter = new ButtonPaddingConverter();

    /// <summary><c>clamp(30px, 2.86em, 60px)</c>: the value is the font size, and the parameter is <c>30,2.86,60</c>.</summary>
    public static readonly IValueConverter ClampConverter = new ButtonClampConverter();

    /// <summary>
    /// <c>margin-inline: -0.3em 0.3em</c> on a slotted icon, in the icon's own em: the value is its font size,
    /// and the parameter is <c>-0.3,0.3</c>.
    /// </summary>
    public static readonly IValueConverter IconMarginConverter = new ButtonIconMarginConverter();

    /// <summary><c>:host(.button-full:not(.button-round)) .button-native</c>: the first value that's set, with no side borders.</summary>
    public static readonly IMultiValueConverter NoSideBordersConverter = new ButtonNoSideBordersConverter();

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private readonly NativeButton? _native;
    private bool _inItem;
    private ContentPresenter?[] _slots = [];

    static AvonicButton()
    {
        // A native <button> takes focus.
        FocusableProperty.OverrideDefaultValue<AvonicButton>(true);

        // A TemplatedControl clips by default, which would cut off .button-native's box-shadow.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicButton>(false);

        // vertical-align: -webkit-baseline-middle. A default, not a theme's value, as FlexSlot would take that for align-self.
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicButton>(Avalonia.Layout.VerticalAlignment.Center);
    }

    public AvonicButton()
    {
        _native = new NativeButton(this, _ => RaiseClick(), () => true, UpdateIsEffectivelyEnabled);

        // ion-activatable and ion-focusable, always.
        Activatable.SetIsEnabled(this, true);
        FocusVisible.SetIsEnabled(this, true);
    }

    /// <summary>The base class's constructor reads this before there's a <see cref="NativeButton"/>.</summary>
    protected override bool IsEnabledCore => base.IsEnabledCore && _native?.CanExecute != false;

    ItemControlType IItemControl.ItemControlType => ItemControlType.Button;

    /// <summary>The item doesn't forward to a button today, because it isn't in Ionic's first-interactive list. It would be <c>click()</c>.</summary>
    void IItemControl.OnItemTap()
    {
        if (IsEffectivelyEnabled)
        {
            RaiseClick();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FillProperty || change.Property == SizeProperty || change.Property == ShapeProperty
            || change.Property == ExpandProperty || change.Property == StrongProperty || change.Property == IconOnlyContentProperty)
        {
            UpdateClasses();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _slots =
        [
            e.NameScope.Find<ContentPresenter>("PART_IconOnly"),
            e.NameScope.Find<ContentPresenter>("PART_Start"),
            e.NameScope.Find<ContentPresenter>("PART_Content"),
            e.NameScope.Find<ContentPresenter>("PART_End"),
        ];
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // closest('ion-item'). Ionic only looks on first load, but containers get recycled here.
        _inItem = this.FindAncestorOfType<AvonicItem>() is not null;
        UpdateClasses();
    }

    /// <summary><c>render</c>'s host classes. <c>fill</c> also resolves to clear in a toolbar or list header, which goes with those controls.</summary>
    private void UpdateClasses()
    {
        var fill = Fill ?? ButtonFill.Solid;
        PseudoClasses.Set(":fill-clear", fill == ButtonFill.Clear);
        PseudoClasses.Set(":fill-default", fill == ButtonFill.Default);
        PseudoClasses.Set(":fill-outline", fill == ButtonFill.Outline);
        PseudoClasses.Set(":fill-solid", fill == ButtonFill.Solid);

        // finalSize. button-default has no rules, so it needs no pseudoclass.
        var size = Size ?? (_inItem ? ButtonSize.Small : null);
        PseudoClasses.Set(":size-large", size == ButtonSize.Large);
        PseudoClasses.Set(":size-small", size == ButtonSize.Small);

        PseudoClasses.Set(":shape-round", Shape == ButtonShape.Round);
        PseudoClasses.Set(":expand-block", Expand == ButtonExpand.Block);
        PseudoClasses.Set(":expand-full", Expand == ButtonExpand.Full);
        PseudoClasses.Set(":strong", Strong);
        PseudoClasses.Set(":has-icon-only", IconOnlyContent is not null);
        PseudoClasses.Set(":in-item", _inItem);
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>As Avalonia's own <c>Button</c> does: a handler that sets <c>Handled</c> stops the command.</summary>
    private void RaiseClick()
    {
        var args = new RoutedEventArgs(ClickEvent, this);
        RaiseEvent(args);
        _native?.ExecuteUnless(args);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        // onFocus is on the native element, so focus inside a slot isn't this.
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

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new ButtonPeer(this);

    /// <summary>
    /// A slot's text, in slot order. A string is read from the property, because the presenter's copy may be
    /// uppercase and a native button's name keeps the original case, as measured.
    /// </summary>
    private IEnumerable<string> SlotText()
    {
        (object? Value, int Slot)[] slots = [(IconOnlyContent, 0), (StartContent, 1), (Content, 2), (EndContent, 3)];
        foreach (var (value, slot) in slots)
        {
            if (value is string text)
            {
                yield return text;
            }
            else if (slot < _slots.Length && _slots[slot]?.Child is { } child)
            {
                var nested = AccessibleText.Of(child);
                if (nested.Length > 0)
                {
                    yield return nested;
                }
            }
        }
    }

    private sealed class ButtonPeer(AvonicButton owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        /// <summary><c>aria-disabled</c>, and a command that can't execute.</summary>
        protected override bool IsEnabledCore() => owner.IsEffectivelyEnabled;

        /// <summary>A native button names itself from the text inside it. An icon adds nothing, as measured.</summary>
        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? string.Join(" ", owner.SlotText()) : name;
        }

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public void Invoke()
        {
            if (owner.IsEffectivelyEnabled)
            {
                owner.RaiseClick();
            }
        }
    }

    // ── Converters ───────────────────────────────────────────────────────────────────────────

    private static double[] Numbers(object? parameter) =>
        parameter is string text
            ? text.Split(',').Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToArray()
            : [];

    private sealed class SlotTextConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is [string text, ButtonTextTransform.Uppercase])
            {
                return text.ToUpperInvariant();
            }

            return values.Count > 0 && !ReferenceEquals(values[0], AvaloniaProperty.UnsetValue) ? values[0] : null;
        }
    }

    private sealed class ButtonPaddingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [double fontSize, var startValue, var topValue, var endValue, var bottomValue] || parameter is not string text)
            {
                return AvaloniaProperty.UnsetValue;
            }

            double? start = startValue as double?, top = topValue as double?, end = endValue as double?, bottom = bottomValue as double?;

            // --padding-top: 0; --padding-bottom: var(--padding-top); --padding-end: var(--padding-top); --padding-start: var(--padding-end)
            if (text == "icon-only")
            {
                var t = top ?? 0;
                var e = end ?? t;
                return new Thickness(start ?? e, t, e, bottom ?? t);
            }

            var parts = text.Split(' ');
            var inline = Length(parts[0], fontSize);
            var block = Length(parts[1], fontSize);
            return new Thickness(start ?? inline, top ?? block, end ?? inline, bottom ?? block);
        }

        private static double Length(string value, double fontSize) =>
            value.EndsWith("em", StringComparison.Ordinal)
                ? double.Parse(value[..^2], CultureInfo.InvariantCulture) * fontSize
                : double.Parse(value, CultureInfo.InvariantCulture);
    }

    private sealed class ButtonClampConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double fontSize && Numbers(parameter) is [var min, var factor, var max]
                ? Math.Clamp(fontSize * factor, min, max)
                : AvaloniaProperty.UnsetValue;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class ButtonIconMarginConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double fontSize && Numbers(parameter) is [var start, var end]
                ? new Thickness(fontSize * start, 0, fontSize * end, 0)
                : AvaloniaProperty.UnsetValue;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class ButtonNoSideBordersConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var width = values.OfType<Thickness>().FirstOrDefault();
            return new Thickness(0, width.Top, 0, width.Bottom);
        }
    }
}
