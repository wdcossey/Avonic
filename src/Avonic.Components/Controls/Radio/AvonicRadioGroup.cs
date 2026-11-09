using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>RadioGroupChangeEventDetail</c>: the group's new value.</summary>
public class RadioGroupChangeEventArgs(RoutedEvent routedEvent, object? value) : RoutedEventArgs(routedEvent)
{
    public object? Value { get; } = value;
}

/// <summary>
/// Ionic <c>ion-radio-group</c>: makes the <see cref="AvonicRadio"/>s inside it one choice. A click on one, or an arrow key or
/// Space on a focused one, sets <see cref="Value"/> and raises <see cref="IonChange"/>, and each radio is checked when its value
/// is the group's. Helper or error text sits above the radios. See <c>docs/components/ion-radio/README.md</c>.
/// </summary>
/// <remarks>
/// Its children are <see cref="ItemsControl.Items"/>, as <c>core.json</c> lists no slot, as <c>ion-list</c>'s are. A radio finds
/// its group among its logical ancestors, and the group finds its radios among its visual descendants, in tree order, as
/// <c>querySelectorAll('ion-radio')</c> does.
/// </remarks>
[TemplatePart("PART_Top", typeof(Control))]
[TemplatePart("PART_HelperText", typeof(Control))]
[TemplatePart("PART_ErrorText", typeof(Control))]
[PseudoClasses(":in-list")]
public class AvonicRadioGroup : ItemsControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>allowEmptySelection</c>: the checked radio can be unchecked, by a click or Space.</summary>
    public static readonly StyledProperty<bool> AllowEmptySelectionProperty =
        AvaloniaProperty.Register<AvonicRadioGroup, bool>(nameof(AllowEmptySelection));

    /// <summary>
    /// Ionic <c>compareWith</c>: how a radio's value is matched to the group's. A <c>Func&lt;object?, object?, bool&gt;</c> given
    /// the group's value then the radio's, or the name of a property to compare on both, as Ionic's function or string. Anything
    /// else is strict equality.
    /// </summary>
    public static readonly StyledProperty<object?> CompareWithProperty =
        AvaloniaProperty.Register<AvonicRadioGroup, object?>(nameof(CompareWith));

    /// <summary>Ionic <c>errorText</c>: at the top, when the group has the classes <c>ion-touched</c> and <c>ion-invalid</c>.</summary>
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<AvonicRadioGroup, string?>(nameof(ErrorText));

    /// <summary>Ionic <c>helperText</c>: at the top, unless the error text is showing.</summary>
    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<AvonicRadioGroup, string?>(nameof(HelperText));

    /// <summary>Ionic <c>value</c>. Null is <c>undefined</c>: nothing is checked. Two-way by default, as the user changes it.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicRadioGroup, object?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionChange</c>: the user changed the value. Not raised when <see cref="Value"/> is set in code.</summary>
    public static readonly RoutedEvent<RadioGroupChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicRadioGroup, RadioGroupChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    public event EventHandler<RadioGroupChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    /// <summary>Ionic's internal <c>ionValueChange</c>, which each radio inside listens to.</summary>
    internal event Action? ValueChanged;

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private Control? _top;
    private Control? _helperText;
    private Control? _errorText;

    static AvonicRadioGroup()
    {
        // overflow: visible, for a focused radio's circle at the edge. A TemplatedControl clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicRadioGroup>(false);

        // Tab moves through what's inside, as in a page: an ItemsControl is one tab stop that remembers the last one focused, which
        // would skip a button in an item and come back to a radio that's no longer the choice.
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<AvonicRadioGroup>(KeyboardNavigationMode.Continue);

        ValueProperty.Changed.AddClassHandler<AvonicRadioGroup>((group, _) => group.OnValueChanged());
        HelperTextProperty.Changed.AddClassHandler<AvonicRadioGroup>((group, _) => group.UpdateHint());
        ErrorTextProperty.Changed.AddClassHandler<AvonicRadioGroup>((group, _) => group.UpdateHint());
    }

    public AvonicRadioGroup()
    {
        // componentDidLoad, once the radios inside are there.
        Loaded += (_, _) => SetRadioTabStops();

        // The MutationObserver on class, for checkInvalidState.
        Classes.CollectionChanged += OnClassesChanged;
    }

    public bool AllowEmptySelection
    {
        get => GetValue(AllowEmptySelectionProperty);
        set => SetValue(AllowEmptySelectionProperty, value);
    }

    public object? CompareWith
    {
        get => GetValue(CompareWithProperty);
        set => SetValue(CompareWithProperty, value);
    }

    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public string? HelperText
    {
        get => GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary><c>checkInvalidState</c>: the classes an app's validation sets.</summary>
    private bool IsInvalid => Classes.Contains("ion-touched") && Classes.Contains("ion-invalid");

    /// <summary><c>getRadios</c>: every radio inside, in tree order.</summary>
    private IEnumerable<AvonicRadio> Radios() => this.GetVisualDescendants().OfType<AvonicRadio>().Where(radio => radio.Group == this);

    /// <summary><c>valueChanged</c>: the tab stop follows the value, and the radios hear of it.</summary>
    private void OnValueChanged()
    {
        SetRadioTabStops();
        ValueChanged?.Invoke();
    }

    /// <summary>
    /// <c>setRadioTabindex</c>: the first enabled radio whose value is the group's, by strict equality, is the one tab stop, or the
    /// first enabled radio if none is.
    /// </summary>
    private void SetRadioTabStops()
    {
        var radios = Radios().ToList();
        var focusable = radios.FirstOrDefault(radio => Equals(radio.Value, Value) && radio.IsEffectivelyEnabled)
                        ?? radios.FirstOrDefault(radio => radio.IsEffectivelyEnabled);
        if (focusable is null)
        {
            return;
        }

        foreach (var radio in radios)
        {
            radio.SetTabStop(radio == focusable);
        }
    }

    // ── Selecting ────────────────────────────────────────────────────────────────────────────

    /// <summary><c>isOptionSelected</c>: whether a radio with this value is checked.</summary>
    internal bool Selects(object? radioValue)
    {
        var value = Value;
        if (value is null)
        {
            return false;
        }

        // Array.isArray: a list, not any IEnumerable, as a string or a dictionary isn't an array.
        return value is IList values
            ? values.Cast<object?>().Any(item => Compare(item, radioValue))
            : Compare(value, radioValue);
    }

    /// <summary><c>compareOptions</c>.</summary>
    private bool Compare(object? current, object? compare) => CompareWith switch
    {
        Func<object?, object?, bool> compareWith => compareWith(current, compare),
        string property => Equals(Read(current, property), Read(compare, property)),
        // Array.isArray(compareValue) ? compareValue.includes(currentValue)
        _ when compare is IList values => values.Cast<object?>().Any(value => Equals(value, current)),
        _ => Equals(current, compare),
    };

    /// <summary><c>value[compareWith]</c>: an entry of a dictionary, or a public property.</summary>
    private static object? Read(object? value, string property) => value switch
    {
        null => null,
        IDictionary dictionary => dictionary.Contains(property) ? dictionary[property] : null,
        _ => value.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value),
    };

    /// <summary><c>onClick</c>, for a click on one of the group's radios, which has checked that it's enabled.</summary>
    internal void OnRadioClick(AvonicRadio radio)
    {
        if (!Equals(radio.Value, Value))
        {
            SetCurrentValue(ValueProperty, radio.Value);
            EmitValueChange();
        }
        else if (AllowEmptySelection)
        {
            SetCurrentValue(ValueProperty, null);
            EmitValueChange();
        }
    }

    private void EmitValueChange() => RaiseEvent(new RadioGroupChangeEventArgs(IonChangeEvent, Value));

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>onKeydown</c>, Ionic's listener on the document, for a key on one of this group's radios: the arrows move to the next or
    /// previous enabled radio, wrapping, and check it; Space checks the focused one. The base class isn't called: an
    /// <see cref="ItemsControl"/> moves focus between its containers with the arrows, which Ionic's group doesn't.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source is not AvonicRadio current || current.Group != this || !current.IsEffectivelyEnabled)
        {
            return;
        }

        var radios = Radios().Where(radio => radio.IsEffectivelyEnabled).ToList();
        var index = radios.IndexOf(current);
        if (index < 0)
        {
            return;
        }

        AvonicRadio? next = e.Key switch
        {
            Key.Down or Key.Right => radios[(index + 1) % radios.Count],
            Key.Up or Key.Left => radios[(index - 1 + radios.Count) % radios.Count],
            _ => null,
        };

        if (next is not null)
        {
            // setFocus(ev) stops the key and its default.
            e.Handled = true;
            next.Focus(NavigationMethod.Directional);
            SetCurrentValue(ValueProperty, next.Value);
            EmitValueChange();
        }

        if (e.Key == Key.Space)
        {
            var previous = Value;
            SetCurrentValue(ValueProperty, AllowEmptySelection && Value is not null ? null : current.Value);
            if (!Equals(previous, Value) || AllowEmptySelection)
            {
                EmitValueChange();
            }

            // preventDefault, so the page doesn't scroll.
            e.Handled = true;
        }
    }

    // ── Hint ─────────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _top = e.NameScope.Find<Control>("PART_Top");
        _helperText = e.NameScope.Find<Control>("PART_HelperText");
        _errorText = e.NameScope.Find<Control>("PART_ErrorText");
        UpdateHint();
    }

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateHint();

    /// <summary>
    /// <c>renderHintText</c>: the top only with helper or error text, the helper text's node empty when invalid, and the error
    /// text's empty when not, which <c>display: none</c> hides as well.
    /// </summary>
    private void UpdateHint()
    {
        var invalid = IsInvalid;
        if (_top is not null)
        {
            _top.IsVisible = !string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText);
        }

        if (_helperText is not null)
        {
            _helperText.IsVisible = !invalid && !string.IsNullOrEmpty(HelperText);
        }

        if (_errorText is not null)
        {
            _errorText.IsVisible = invalid && !string.IsNullOrEmpty(ErrorText);
        }
    }

    /// <summary><c>getHintTextId</c>: what <c>aria-describedby</c> points to.</summary>
    private string? HintText => IsInvalid && !string.IsNullOrEmpty(ErrorText) ? ErrorText
        : !string.IsNullOrEmpty(HelperText) ? HelperText
        : null;

    // ── Host context ─────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // ion-list .radio-group-top
        PseudoClasses.Set(":in-list", this.GetLogicalAncestors().OfType<AvonicList>().Any());
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new RadioGroupAutomationPeer(this);

    /// <summary>
    /// <c>role="radiogroup"</c>, a group here; <c>aria-describedby</c>, the hint it shows; and <c>aria-labelledby</c>, the label
    /// in the first list header in it, or else the first divider, as <c>connectedCallback</c> finds it.
    /// </summary>
    private sealed class RadioGroupAutomationPeer(AvonicRadioGroup owner) : ItemsControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : owner.HintText;

        protected override AutomationPeer? GetLabeledByCore()
        {
            if (base.GetLabeledByCore() is { } labeledBy)
            {
                return labeledBy;
            }

            // Visual, as what's in a divider's slots isn't the divider's logical descendant.
            var descendants = owner.GetVisualDescendants().ToList();
            var header = descendants.OfType<AvonicListHeader>().FirstOrDefault() as Visual
                         ?? descendants.OfType<AvonicItemDivider>().FirstOrDefault();
            var label = header?.GetVisualDescendants().OfType<AvonicLabel>().FirstOrDefault();
            return label is null ? null : CreatePeerForElement(label);
        }
    }
}
