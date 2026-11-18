using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-select</c>: a field that shows the chosen option's text, and opens an interface to choose (<see cref="Interface"/>):
/// an <see cref="AvonicAlert"/>, an <see cref="AvonicActionSheet"/>, an <see cref="AvonicPopover"/> or an <see cref="AvonicModal"/>.
/// The options are <see cref="Items"/>, <see cref="AvonicSelectOption"/>s. The look is in the theme. See
/// <c>docs/components/ion-select/README.md</c>.
/// </summary>
/// <remarks>
/// Ionic's hidden <c>&lt;button&gt;</c> is the host itself: focusable, a click on release, Enter and Space, through
/// <see cref="NativeButton"/>. The interfaces are made in code when it opens, as Ionic's controllers make them, and presented in
/// the select's top level.
/// </remarks>
[TemplatePart("PART_Container", typeof(Control))]
[TemplatePart("PART_Start", typeof(ContentPresenter))]
[TemplatePart("PART_End", typeof(ContentPresenter))]
[TemplatePart("PART_LabelContent", typeof(ContentPresenter))]
[PseudoClasses(":in-item", ":in-item-color", ":select-disabled", ":select-expanded", ":has-expanded-icon", ":has-value")]
[PseudoClasses(":label-floating", ":has-placeholder", ":has-placeholder-shown", ":has-focus", ":fill-outline", ":fill-solid")]
[PseudoClasses(":justify-start", ":justify-end", ":justify-space-between", ":shape-round", ":label-text-wrapper-hidden")]
[PseudoClasses(":label-placement-start", ":label-placement-end", ":label-placement-fixed", ":label-placement-floating")]
[PseudoClasses(":label-placement-stacked", ":has-bottom", ":ion-hover")]
public partial class AvonicSelect : TemplatedControl, IItemControl, IInputHost
{
    /// <summary>The style class an app sets once the user has been in the field, read with <see cref="InvalidClass"/>.</summary>
    public const string TouchedClass = "ion-touched";

    /// <summary>The style class an app sets when the value is invalid.</summary>
    public const string InvalidClass = "ion-invalid";

    /// <summary>The style class an app sets when the value is valid.</summary>
    public const string ValidClass = "ion-valid";

    /// <summary>md's <c>toggleIcon</c> when there's none: <c>caretDownSharp</c>.</summary>
    internal const string MdToggleIcon = "caret-down-sharp";

    /// <summary><c>OPTION_CLASS</c>: every option of every interface has it.</summary>
    internal const string OptionClass = "select-interface-option";

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>value</c>. Null is <c>undefined</c>. Two-way by default, as the user changes it.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicSelect, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>cancelText</c>.</summary>
    public static readonly StyledProperty<string> CancelTextProperty =
        AvaloniaProperty.Register<AvonicSelect, string>(nameof(CancelText), "Cancel");

    /// <summary>Ionic <c>okText</c>.</summary>
    public static readonly StyledProperty<string> OkTextProperty =
        AvaloniaProperty.Register<AvonicSelect, string>(nameof(OkText), "OK");

    /// <summary>Ionic <c>color</c>: the highlight when focused or open.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicSelect>();

    /// <summary>
    /// Ionic <c>compareWith</c>: a <c>Func&lt;object?, object?, bool&gt;</c> given the current value then an option's, or the name
    /// of a property (or a dictionary's key) to compare on both. Anything else is strict equality.
    /// </summary>
    public static readonly StyledProperty<object?> CompareWithProperty =
        AvaloniaProperty.Register<AvonicSelect, object?>(nameof(CompareWith));

    /// <summary>Ionic <c>errorText</c>: under the select while it has the classes <c>ion-touched</c> and <c>ion-invalid</c>.</summary>
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<AvonicSelect, string?>(nameof(ErrorText));

    /// <summary>Ionic <c>helperText</c>: under the select, unless it's invalid.</summary>
    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<AvonicSelect, string?>(nameof(HelperText));

    /// <summary>Ionic <c>expandedIcon</c>: the icon while open, which then doesn't turn.</summary>
    public static readonly StyledProperty<string?> ExpandedIconProperty =
        AvaloniaProperty.Register<AvonicSelect, string?>(nameof(ExpandedIcon));

    /// <summary>Ionic <c>toggleIcon</c>. Null is md's <c>caret-down-sharp</c>.</summary>
    public static readonly StyledProperty<string?> ToggleIconProperty =
        AvaloniaProperty.Register<AvonicSelect, string?>(nameof(ToggleIcon));

    /// <summary>Ionic <c>fill</c>. Null is none.</summary>
    public static readonly StyledProperty<SelectFill?> FillProperty =
        AvaloniaProperty.Register<AvonicSelect, SelectFill?>(nameof(Fill));

    /// <summary>Ionic <c>shape</c>. Null is the default.</summary>
    public static readonly StyledProperty<SelectShape?> ShapeProperty =
        AvaloniaProperty.Register<AvonicSelect, SelectShape?>(nameof(Shape));

    /// <summary>Ionic <c>interface</c>.</summary>
    public static readonly StyledProperty<SelectInterface> InterfaceProperty =
        AvaloniaProperty.Register<AvonicSelect, SelectInterface>(nameof(Interface));

    /// <summary>
    /// Ionic <c>interfaceOptions</c>: the interface's properties by name, in Ionic's camelCase or Avonic's PascalCase, with
    /// <c>header</c>, <c>subHeader</c>, <c>message</c> and <c>cssClass</c> read as Ionic reads them. See the README.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, object?>?> InterfaceOptionsProperty =
        AvaloniaProperty.Register<AvonicSelect, IReadOnlyDictionary<string, object?>?>(nameof(InterfaceOptions));

    /// <summary>Ionic <c>justify</c>. Null is the wrapper's <c>space-between</c>. Not with a floating or stacked label.</summary>
    public static readonly StyledProperty<SelectJustify?> JustifyProperty =
        AvaloniaProperty.Register<AvonicSelect, SelectJustify?>(nameof(Justify));

    /// <summary>Ionic <c>label</c>. Beats <see cref="LabelContent"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AvonicSelect, string?>(nameof(Label));

    /// <summary>Ionic <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<SelectLabelPlacement> LabelPlacementProperty =
        SelectControlPanel.LabelPlacementProperty.AddOwner<AvonicSelect>();

    /// <summary>Ionic <c>multiple</c>: the value is a list.</summary>
    public static readonly StyledProperty<bool> MultipleProperty =
        AvaloniaProperty.Register<AvonicSelect, bool>(nameof(Multiple));

    /// <summary>Ionic <c>placeholder</c>: shown when there's no text.</summary>
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<AvonicSelect, string?>(nameof(Placeholder));

    /// <summary>Ionic <c>required</c>: a screen reader says it's required.</summary>
    public static readonly StyledProperty<bool> RequiredProperty =
        AvaloniaProperty.Register<AvonicSelect, bool>(nameof(Required));

    /// <summary>Ionic <c>selectedText</c>: shown in place of the options' text, unless null or empty.</summary>
    public static readonly StyledProperty<string?> SelectedTextProperty =
        AvaloniaProperty.Register<AvonicSelect, string?>(nameof(SelectedText));

    /// <summary>Ionic's <c>label</c> slot. Shown only when <see cref="Label"/> is null.</summary>
    public static readonly StyledProperty<object?> LabelContentProperty =
        AvaloniaProperty.Register<AvonicSelect, object?>(nameof(LabelContent));

    public static readonly StyledProperty<IDataTemplate?> LabelContentTemplateProperty =
        AvaloniaProperty.Register<AvonicSelect, IDataTemplate?>(nameof(LabelContentTemplate));

    /// <summary>Ionic's <c>start</c> slot.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicSelect, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicSelect, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicSelect, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicSelect, IDataTemplate?>(nameof(EndContentTemplate));

    /// <summary>
    /// Ionic's internal <c>--highlight-color</c>: the focused, valid or invalid colour, which the theme works out per state, so that
    /// the highlight, the label, the icon and the borders read one value.
    /// </summary>
    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<AvonicSelect, IBrush?>(nameof(HighlightBrush));

    /// <summary><c>renderSelectText</c>'s text: the value's text, or the placeholder. For the template.</summary>
    public static readonly DirectProperty<AvonicSelect, string> DisplayTextProperty =
        AvaloniaProperty.RegisterDirect<AvonicSelect, string>(nameof(DisplayText), select => select.DisplayText);

    /// <summary><c>renderSelectIcon</c>'s icon: <see cref="ExpandedIcon"/> while open, else <see cref="ToggleIcon"/> or md's. For the template.</summary>
    public static readonly DirectProperty<AvonicSelect, string> SelectIconProperty =
        AvaloniaProperty.RegisterDirect<AvonicSelect, string>(nameof(SelectIcon), select => select.SelectIcon);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionChange</c>: the user chose a new value in an interface. Not when <see cref="Value"/> is set in code.</summary>
    public static readonly RoutedEvent<SelectChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicSelect, SelectChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionCancel</c>: the alert's or the action sheet's cancel.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonCancelEvent =
        RoutedEvent.Register<AvonicSelect, RoutedEventArgs>(nameof(IonCancel), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionDismiss</c>: the interface has been dismissed.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonDismissEvent =
        RoutedEvent.Register<AvonicSelect, RoutedEventArgs>(nameof(IonDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionFocus</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicSelect, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicSelect, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    private readonly AvaloniaList<AvonicSelectOption> _items = [];
    private readonly StyleValue<bool> _required = new();
    private ContentPresenter? _start;
    private ContentPresenter? _end;
    private ContentPresenter? _labelContent;
    private Control? _container;
    private AvonicItem? _item;
    private AvonicOverlay? _overlay;
    private bool _isExpanded;
    private bool _hasFocus;
    private bool _invalid;
    private string _displayText = "";
    private string _selectIcon = MdToggleIcon;

    static AvonicSelect()
    {
        // The hidden <button> is a tab stop.
        FocusableProperty.OverrideDefaultValue<AvonicSelect>(true);
        // overflow: visible, for the floating label over the outline. A TemplatedControl clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicSelect>(false);
    }

    public AvonicSelect()
    {
        _items.CollectionChanged += OnItemsChanged;

        // The host's onClick, and the <button>'s Enter and Space, which click it.
        _ = new NativeButton(this, OnClick, () => true, UpdateIsEffectivelyEnabled);

        AddHandler(GotFocusEvent, OnGotFocus);
        AddHandler(LostFocusEvent, OnLostFocus);

        // aria-required, at style priority, so an app's own value wins.
        Bind(AutomationProperties.IsRequiredForFormProperty, _required, BindingPriority.Style);

        // ion-focusable, always (TODO FW-6451 in Ionic): an item around it reacts to its focus.
        SetValue(FocusVisible.IsEnabledProperty, true, BindingPriority.Style);

        Classes.CollectionChanged += OnClassesChanged;
        UpdateText();
        UpdateClasses();
    }

    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public string CancelText { get => GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }

    public string OkText { get => GetValue(OkTextProperty); set => SetValue(OkTextProperty, value); }

    public string? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    public object? CompareWith { get => GetValue(CompareWithProperty); set => SetValue(CompareWithProperty, value); }

    public string? ErrorText { get => GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

    public string? HelperText { get => GetValue(HelperTextProperty); set => SetValue(HelperTextProperty, value); }

    public string? ExpandedIcon { get => GetValue(ExpandedIconProperty); set => SetValue(ExpandedIconProperty, value); }

    public string? ToggleIcon { get => GetValue(ToggleIconProperty); set => SetValue(ToggleIconProperty, value); }

    public SelectFill? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    public SelectShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    public SelectInterface Interface { get => GetValue(InterfaceProperty); set => SetValue(InterfaceProperty, value); }

    public IReadOnlyDictionary<string, object?>? InterfaceOptions
    {
        get => GetValue(InterfaceOptionsProperty);
        set => SetValue(InterfaceOptionsProperty, value);
    }

    public SelectJustify? Justify { get => GetValue(JustifyProperty); set => SetValue(JustifyProperty, value); }

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public SelectLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public bool Multiple { get => GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    public string? Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    public bool Required { get => GetValue(RequiredProperty); set => SetValue(RequiredProperty, value); }

    public string? SelectedText { get => GetValue(SelectedTextProperty); set => SetValue(SelectedTextProperty, value); }

    public object? LabelContent { get => GetValue(LabelContentProperty); set => SetValue(LabelContentProperty, value); }

    public IDataTemplate? LabelContentTemplate
    {
        get => GetValue(LabelContentTemplateProperty);
        set => SetValue(LabelContentTemplateProperty, value);
    }

    public object? StartContent { get => GetValue(StartContentProperty); set => SetValue(StartContentProperty, value); }

    public IDataTemplate? StartContentTemplate
    {
        get => GetValue(StartContentTemplateProperty);
        set => SetValue(StartContentTemplateProperty, value);
    }

    public object? EndContent { get => GetValue(EndContentProperty); set => SetValue(EndContentProperty, value); }

    public IDataTemplate? EndContentTemplate
    {
        get => GetValue(EndContentTemplateProperty);
        set => SetValue(EndContentTemplateProperty, value);
    }

    public IBrush? HighlightBrush { get => GetValue(HighlightBrushProperty); set => SetValue(HighlightBrushProperty, value); }

    /// <summary>The options, Ionic's <c>ion-select-option</c> children, in order.</summary>
    [Content]
    public AvaloniaList<AvonicSelectOption> Items => _items;

    public string DisplayText
    {
        get => _displayText;
        private set => SetAndRaise(DisplayTextProperty, ref _displayText, value);
    }

    public string SelectIcon
    {
        get => _selectIcon;
        private set => SetAndRaise(SelectIconProperty, ref _selectIcon, value);
    }

    public event EventHandler<SelectChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonCancel
    {
        add => AddHandler(IonCancelEvent, value);
        remove => RemoveHandler(IonCancelEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonDismiss
    {
        add => AddHandler(IonDismissEvent, value);
        remove => RemoveHandler(IonDismissEvent, value);
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

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>isExpanded</c>: an interface is open, or opening.</summary>
    internal bool IsExpanded => _isExpanded;

    /// <summary>The open interface, or null.</summary>
    internal AvonicOverlay? Overlay => _overlay;

    /// <summary><c>hasLabel</c>: a <c>label</c>, even <c>""</c>, or anything in the <c>label</c> slot.</summary>
    private bool HasLabel => Label is not null || LabelContent is not null;

    /// <summary><c>labelText</c>: the label, or the <c>label</c> slot's text; null with neither.</summary>
    internal string? LabelText => Label ?? (LabelContent is null ? null : AvonicSelectOption.TextOf(_labelContent?.Child ?? LabelContent));

    /// <summary><c>checkInvalidState</c>: the app has marked it both touched and invalid.</summary>
    private bool IsInvalid => Classes.Contains(TouchedClass) && Classes.Contains(InvalidClass);

    /// <summary><c>getText()</c>: the selected text, or the matching options' text.</summary>
    internal string GetText() => SelectedText is { Length: > 0 } selected ? selected : GenerateText();

    /// <summary><c>hasValue</c>.</summary>
    private bool HasValue => GetText().Length > 0;

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _start = e.NameScope.Find<ContentPresenter>("PART_Start");
        _end = e.NameScope.Find<ContentPresenter>("PART_End");
        _container = e.NameScope.Find<Control>("PART_Container");
        if (_labelContent is not null)
        {
            _labelContent.PropertyChanged -= OnLabelContentChanged;
        }

        _labelContent = e.NameScope.Find<ContentPresenter>("PART_LabelContent");
        if (_labelContent is not null)
        {
            _labelContent.PropertyChanged += OnLabelContentChanged;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // hostContext('ion-item') and ('ion-item.ion-color'), at render.
        _item = this.FindAncestorOfType<AvonicItem>();
        UpdateClasses();

        // connectedCallback's emitStyle.
        EmitStyle();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        PseudoClasses.Set(Activatable.HoverPseudoClass, false);
        _item = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty || change.Property == SelectedTextProperty || change.Property == CompareWithProperty
            || change.Property == PlaceholderProperty || change.Property == MultipleProperty)
        {
            UpdateText();
            UpdateClasses();
            if (change.Property == ValueProperty || change.Property == PlaceholderProperty)
            {
                // The watchers' styleChanged.
                EmitStyle();
            }
        }
        else if (change.Property == ToggleIconProperty || change.Property == ExpandedIconProperty)
        {
            UpdateIcon();
            UpdateClasses();
        }
        else if (change.Property == LabelProperty || change.Property == LabelContentProperty || change.Property == HelperTextProperty
                 || change.Property == ErrorTextProperty || change.Property == LabelPlacementProperty || change.Property == FillProperty
                 || change.Property == ShapeProperty || change.Property == JustifyProperty)
        {
            UpdateClasses();
            UpdateAutomation();
        }
        else if (change.Property == IsEnabledProperty)
        {
            UpdateClasses();
            EmitStyle();
        }
        else if (change.Property == RequiredProperty)
        {
            _required.Set(Required);
        }
        else if (change.Property == AutomationProperties.NameProperty)
        {
            UpdateAutomation();
        }
    }

    private void OnLabelContentChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            UpdateAutomation();
        }
    }

    // ── The options ──────────────────────────────────────────────────────────────────────────

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var option in e.OldItems?.OfType<AvonicSelectOption>() ?? [])
        {
            option.Changed -= OnOptionChanged;
            LogicalChildren.Remove(option);
        }

        foreach (var option in e.NewItems?.OfType<AvonicSelectOption>() ?? [])
        {
            option.Changed += OnOptionChanged;
            LogicalChildren.Add(option);
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var option in LogicalChildren.OfType<AvonicSelectOption>().Where(option => !_items.Contains(option)).ToList())
            {
                option.Changed -= OnOptionChanged;
                LogicalChildren.Remove(option);
            }
        }

        OnOptionsChanged();
    }

    private void OnOptionChanged(AvonicSelectOption option) => OnOptionsChanged();

    /// <summary><c>watchForOptions</c>' callback: the open interface's options again, and the text.</summary>
    private void OnOptionsChanged()
    {
        UpdateOverlayOptions();
        UpdateText();
        UpdateClasses();
    }

    /// <summary><c>generateText</c>.</summary>
    private string GenerateText()
    {
        var value = Value;
        if (value is null)
        {
            return "";
        }

        if (AsList(value) is { } values)
        {
            return string.Join(", ", values.Cast<object?>().Select(TextForValue).Where(text => text is not null));
        }

        return TextForValue(value) ?? "";
    }

    /// <summary><c>textForValue</c>: the first matching option's text, or null when there's none or its text is empty.</summary>
    private string? TextForValue(object? value)
    {
        var option = _items.FirstOrDefault(option => CompareOptions(value, option.OptionValue));
        return option is null || option.Text.Length == 0 ? null : option.Text;
    }

    /// <summary><c>Array.isArray</c>: a list, not any enumerable, as a string or a dictionary isn't an array.</summary>
    internal static IList? AsList(object? value) => value as IList;

    /// <summary><c>compareOptions</c>.</summary>
    internal bool CompareOptions(object? current, object? compare) => CompareWith switch
    {
        Func<object?, object?, bool> compareWith => compareWith(current, compare),
        string property => Equals(Read(current, property), Read(compare, property)),
        _ when AsList(compare) is { } values => values.Cast<object?>().Any(value => Equals(value, current)),
        _ => Equals(current, compare),
    };

    /// <summary><c>isOptionSelected</c>.</summary>
    internal bool IsOptionSelected(object? current, object? compare)
    {
        if (current is null)
        {
            return false;
        }

        return AsList(current) is { } values
            ? values.Cast<object?>().Any(value => CompareOptions(value, compare))
            : CompareOptions(current, compare);
    }

    /// <summary><c>isValueEqual</c>: the same value, or for <see cref="Multiple"/> the same values in any order.</summary>
    internal bool IsValueEqual(object? current, object? now)
    {
        if (Multiple)
        {
            var currentValues = AsList(current)?.Cast<object?>().ToList() ?? [];
            var newValues = AsList(now)?.Cast<object?>().ToList() ?? [];
            if (currentValues.Count != newValues.Count)
            {
                return false;
            }

            // A multiset compare: each new value must match a distinct current one.
            foreach (var value in newValues)
            {
                var index = currentValues.FindIndex(item => CompareOptions(item, value));
                if (index < 0)
                {
                    return false;
                }

                currentValues.RemoveAt(index);
            }

            return true;
        }

        if (current is null && now is null)
        {
            return true;
        }

        if (current is null || now is null)
        {
            return false;
        }

        return CompareOptions(current, now);
    }

    /// <summary><c>value[compareWith]</c>: a dictionary's entry, or a public property.</summary>
    private static object? Read(object? value, string property) => value switch
    {
        null => null,
        IDictionary dictionary => dictionary.Contains(property) ? dictionary[property] : null,
        _ => value.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value),
    };

    /// <summary><c>setValue</c>: the new value and <c>ionChange</c>, unless it's equal to the current one.</summary>
    internal void SetValueFromUser(object? value)
    {
        if (IsValueEqual(Value, value))
        {
            return;
        }

        SetCurrentValue(ValueProperty, value);
        RaiseEvent(new SelectChangeEventArgs(IonChangeEvent, value));
    }

    // ── Render ───────────────────────────────────────────────────────────────────────────────

    /// <summary><c>renderSelectText</c>: the text, or the placeholder when it's <c>""</c>.</summary>
    private void UpdateText()
    {
        var text = GetText();
        var placeholder = text.Length == 0 && Placeholder is not null;
        DisplayText = placeholder ? Placeholder! : text;
        PseudoClasses.Set(":has-placeholder-shown", placeholder);
        UpdateAutomation();
    }

    /// <summary><c>renderSelectIcon</c>.</summary>
    private void UpdateIcon() => SelectIcon = _isExpanded && ExpandedIcon is { } expanded ? expanded : ToggleIcon ?? MdToggleIcon;

    /// <summary><c>render</c>'s host classes.</summary>
    private void UpdateClasses()
    {
        var placement = LabelPlacement;
        var floatingOrStacked = placement is SelectLabelPlacement.Floating or SelectLabelPlacement.Stacked;
        var hasValue = HasValue;
        PseudoClasses.Set(":in-item", _item is not null);
        PseudoClasses.Set(":in-item-color", _item?.Classes.Contains(":ion-color") == true);
        PseudoClasses.Set(":select-disabled", !IsEnabled);
        PseudoClasses.Set(":select-expanded", _isExpanded);
        PseudoClasses.Set(":has-expanded-icon", ExpandedIcon is not null);
        PseudoClasses.Set(":has-value", hasValue);
        PseudoClasses.Set(":label-floating",
            placement == SelectLabelPlacement.Stacked
            || (placement == SelectLabelPlacement.Floating && (hasValue || _hasFocus || _isExpanded)));
        PseudoClasses.Set(":has-placeholder", Placeholder is not null);
        PseudoClasses.Set(":has-focus", _hasFocus);
        PseudoClasses.Set(":fill-outline", Fill == SelectFill.Outline);
        PseudoClasses.Set(":fill-solid", Fill == SelectFill.Solid);
        var justify = floatingOrStacked ? null : Justify;
        PseudoClasses.Set(":justify-start", justify == SelectJustify.Start);
        PseudoClasses.Set(":justify-end", justify == SelectJustify.End);
        PseudoClasses.Set(":justify-space-between", justify == SelectJustify.SpaceBetween);
        PseudoClasses.Set(":shape-round", Shape == SelectShape.Round);
        PseudoClasses.Set(":label-placement-start", placement == SelectLabelPlacement.Start);
        PseudoClasses.Set(":label-placement-end", placement == SelectLabelPlacement.End);
        PseudoClasses.Set(":label-placement-fixed", placement == SelectLabelPlacement.Fixed);
        PseudoClasses.Set(":label-placement-floating", placement == SelectLabelPlacement.Floating);
        PseudoClasses.Set(":label-placement-stacked", placement == SelectLabelPlacement.Stacked);
        PseudoClasses.Set(":label-text-wrapper-hidden", !HasLabel);

        // renderBottomContent: only with helper or error text.
        PseudoClasses.Set(":has-bottom", !string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText));
    }

    /// <summary><c>emitStyle</c>: <c>interactive-disabled</c> to an item around it.</summary>
    private void EmitStyle() =>
        RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["interactive-disabled"] = !IsEnabled }));

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The validationObserver: a new aria-describedby when ion-touched or ion-invalid change.
        if (IsInvalid != _invalid)
        {
            _invalid = IsInvalid;
            UpdateAutomation();
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>onClick</c>: a click on the host, or Enter or Space on the <c>&lt;button&gt;</c>, focuses it and opens it, unless it
    /// started on interactive content in the start or end slot.
    /// </summary>
    private void OnClick(object? source)
    {
        if (source is Visual visual && SlottedInteractive(visual))
        {
            return;
        }

        SetFocus();
        var ev = new PopoverEvent(this);
        _ = Open(ev, openedByEnter: source == this && _enterDown);
    }

    /// <summary>A DOM <c>click()</c> on the select, from automation: its <c>onClick</c>.</summary>
    internal void Click() => OnClick(this);

    /// <summary>
    /// <c>getSlottedClickContent</c> and <c>INTERACTIVE_SLOTTED_CONTENT</c>: the click started inside what's in the start or end
    /// slot, on something that handles its own click: a focusable control other than the select, or a button.
    /// </summary>
    private bool SlottedInteractive(Visual source)
    {
        foreach (var slot in new[] { _start, _end })
        {
            if (slot?.Child is not { } slotted || (source != slotted && !slotted.IsVisualAncestorOf(source)))
            {
                continue;
            }

            for (Visual? at = source; at is not null && at != slot; at = at.GetVisualParent())
            {
                if (at is Button or IItemControl || (at is InputElement { Focusable: true } element && element != this)
                    || (at is Control control && FocusVisible.GetIsEnabled(control)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool _enterDown;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _enterDown = e.Key == Key.Enter;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _enterDown = false;
        }

        base.OnKeyUp(e);
    }

    /// <summary><c>setFocus</c>: the <c>&lt;button&gt;</c>.</summary>
    private void SetFocus() => Focus();

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source != this)
        {
            return;
        }

        // onFocus
        _hasFocus = true;
        UpdateClasses();
        RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source != this)
        {
            return;
        }

        // onBlur
        _hasFocus = false;
        UpdateClasses();
        RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        // @media (any-hover: hover): a mouse or a pen, not a finger.
        PseudoClasses.Set(Activatable.HoverPseudoClass, e.Pointer.Type != PointerType.Touch);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        PseudoClasses.Set(Activatable.HoverPseudoClass, false);
    }

    // ── Item ─────────────────────────────────────────────────────────────────────────────────

    ItemControlType IItemControl.ItemControlType => ItemControlType.Select;

    /// <summary>The item calls <c>click()</c>: the select's <c>onClick</c>, with itself as the target.</summary>
    void IItemControl.OnItemTap()
    {
        if (IsEffectivelyEnabled)
        {
            OnClick(this);
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new SelectAutomationPeer(this);

    /// <summary>
    /// <c>ariaLabel</c>: the app's name (Ionic's inherited <c>aria-label</c>) or the label's text, then <c>", "</c> and the text, or
    /// the placeholder when there's none.
    /// </summary>
    internal string AccessibleName(string? appName)
    {
        var rendered = GetText();
        if (rendered.Length == 0 && Placeholder is { } placeholder)
        {
            rendered = placeholder;
        }

        var defined = string.IsNullOrEmpty(appName) ? LabelText : appName;
        return defined is null ? rendered : rendered.Length == 0 ? defined : $"{defined}, {rendered}";
    }

    /// <summary><c>getHintTextId</c>: the error text when invalid, else the helper text.</summary>
    internal string? HintText => IsInvalid && !string.IsNullOrEmpty(ErrorText) ? ErrorText
        : !string.IsNullOrEmpty(HelperText) ? HelperText
        : null;

    private void UpdateAutomation()
    {
        if (ControlAutomationPeer.FromElement(this) is SelectAutomationPeer peer)
        {
            peer.Update();
        }
    }
}
