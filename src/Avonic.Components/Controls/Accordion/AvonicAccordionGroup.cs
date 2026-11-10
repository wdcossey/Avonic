using System.Collections;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-accordion-group</c>: holds which of its <see cref="AvonicAccordion"/>s are open, as <see cref="Value"/>, and
/// decides what a tap on one does. It's an <see cref="ItemsControl"/>, as <c>ion-list</c> is, and an accordion is its own
/// container.
/// </summary>
[PseudoClasses(":expand-compact", ":expand-inset")]
public class AvonicAccordionGroup : ItemsControl
{
    /// <summary>Ionic <c>animated</c>: whether its accordions animate as they open and close.</summary>
    public static readonly StyledProperty<bool> AnimatedProperty =
        AvaloniaProperty.Register<AvonicAccordionGroup, bool>(nameof(Animated), true);

    /// <summary>Ionic <c>multiple</c>: more than one accordion can be open.</summary>
    public static readonly StyledProperty<bool> MultipleProperty =
        AvaloniaProperty.Register<AvonicAccordionGroup, bool>(nameof(Multiple));

    /// <summary>Ionic <c>readonly</c>: its accordions can't be toggled, and don't look any different.</summary>
    public static readonly StyledProperty<bool> ReadonlyProperty =
        AvaloniaProperty.Register<AvonicAccordionGroup, bool>(nameof(Readonly));

    /// <summary>Ionic <c>expand</c>.</summary>
    public static readonly StyledProperty<AccordionGroupExpand> ExpandProperty =
        AvaloniaProperty.Register<AvonicAccordionGroup, AccordionGroupExpand>(nameof(Expand));

    /// <summary>
    /// Ionic <c>value</c>: the <see cref="AvonicAccordion.Value"/> of the open accordion, a list of them with
    /// <see cref="Multiple"/>, or null for none. A <see cref="string"/>, or an <see cref="IEnumerable{T}"/> of them.
    /// </summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicAccordionGroup, object?>(nameof(Value));

    /// <summary>Ionic <c>ionChange</c>: a tap or a key changed <see cref="Value"/>. Setting it doesn't raise this.</summary>
    public static readonly RoutedEvent<AccordionGroupChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicAccordionGroup, AccordionGroupChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    private bool _didLoad;

    /// <summary>A <see cref="Avalonia.Controls.Primitives.TemplatedControl"/> clips by default, and the group doesn't, so an inset accordion's shadow shows.</summary>
    static AvonicAccordionGroup()
    {
        ClipToBoundsProperty.OverrideDefaultValue<AvonicAccordionGroup>(false);
    }

    public AvonicAccordionGroup()
    {
        UpdateExpand();
    }

    public bool Animated
    {
        get => GetValue(AnimatedProperty);
        set => SetValue(AnimatedProperty, value);
    }

    public bool Multiple
    {
        get => GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
    }

    public bool Readonly
    {
        get => GetValue(ReadonlyProperty);
        set => SetValue(ReadonlyProperty, value);
    }

    public AccordionGroupExpand Expand
    {
        get => GetValue(ExpandProperty);
        set => SetValue(ExpandProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event EventHandler<AccordionGroupChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    /// <summary>Ionic's internal <c>ionValueChange</c>, which each accordion inside listens to.</summary>
    internal event Action? ValueChange;

    /// <summary>Whether an accordion with this value is open. <c>Array.isArray(value) ? value.includes(v) : value === v</c>.</summary>
    internal static bool Includes(object? value, string? accordionValue) => value switch
    {
        string single => single == accordionValue,
        IEnumerable many => many.Cast<object?>().Any(v => Equals(v, accordionValue)),
        _ => false,
    };

    /// <summary><c>getAccordions</c>: its own accordions, not those further in.</summary>
    internal IEnumerable<AvonicAccordion> Accordions() => Items.OfType<AvonicAccordion>();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<AvonicAccordion>(item, out recycleKey);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            OnValueChanged();
        }
        else if (change.Property == ExpandProperty)
        {
            UpdateExpand();
            NotifyAccordions();
        }
        else if (change.Property == ReadonlyProperty)
        {
            NotifyAccordions();
        }
    }

    /// <summary>
    /// <c>componentDidLoad</c> calls the watcher by hand, once, so the accordions hear of the value it started with. An accordion
    /// only counts a tap, or a later change, as an interaction.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (!_didLoad)
        {
            _didLoad = true;
            OnValueChanged();
        }
    }

    private void UpdateExpand()
    {
        PseudoClasses.Set(":expand-compact", Expand == AccordionGroupExpand.Compact);
        PseudoClasses.Set(":expand-inset", Expand == AccordionGroupExpand.Inset);
    }

    private void NotifyAccordions()
    {
        foreach (var accordion in Accordions())
        {
            accordion.OnGroupChanged();
        }
    }

    /// <summary><c>valueChanged</c>.</summary>
    private void OnValueChanged()
    {
        if (!Multiple && Value is IEnumerable and not string)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                this,
                "[ion-accordion-group] - An array of values was passed, but multiple is \"false\". This is incorrect usage and may result in unexpected behaviors. To dismiss this warning, pass a string to the \"value\" property when multiple=\"false\".");
        }

        ValueChange?.Invoke();
    }

    /// <summary><c>setValue</c>: only for a user's action. <see cref="AvaloniaObject.SetCurrentValue"/>, so a binding stays.</summary>
    private void SetValueFromUser(object? value)
    {
        SetCurrentValue(ValueProperty, value);
        RaiseEvent(new AccordionGroupChangeEventArgs(IonChangeEvent, value));
    }

    private static List<string> AsList(object? value) => value switch
    {
        null => [],
        string single => [single],
        IEnumerable many => many.Cast<object?>().Select(v => v as string ?? v?.ToString() ?? string.Empty).ToList(),
        _ => [value.ToString() ?? string.Empty],
    };

    /// <summary><c>requestAccordionToggle</c>.</summary>
    internal void RequestAccordionToggle(string? accordionValue, bool expand)
    {
        // Ionic checks disabled here too. An accordion in a disabled group is disabled itself, through IsEnabled, so it never asks.
        if (Readonly)
        {
            return;
        }

        if (expand)
        {
            if (Multiple)
            {
                var values = AsList(Value);
                if (!values.Contains(accordionValue!) && accordionValue is not null)
                {
                    SetValueFromUser(values.Append(accordionValue).ToArray());
                }
            }
            else
            {
                SetValueFromUser(accordionValue);
            }
        }
        else if (Multiple)
        {
            SetValueFromUser(AsList(Value).Where(v => v != accordionValue).ToArray());
        }
        else
        {
            SetValueFromUser(null);
        }
    }

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>onKeydown</c>, in place of an <see cref="ItemsControl"/>'s own arrow keys. Only with focus in an accordion's header, so
    /// that a control in the content keeps its keys. Ionic doesn't prevent the key's default, so it isn't handled: a scroller
    /// around still scrolls, as a page does.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is not (Key.Down or Key.Up or Key.Home or Key.End) || e.Source is not Visual source)
        {
            return;
        }

        var inHeader = false;
        AvonicAccordion? accordionEl = null;
        foreach (var accordion in source.GetSelfAndVisualAncestors().OfType<AvonicAccordion>())
        {
            accordionEl ??= accordion;
            if (accordion.IsInHeader(source))
            {
                inHeader = true;
                break;
            }
        }

        if (!inHeader || accordionEl is null)
        {
            return;
        }

        // closest('ion-accordion-group') === this, and in its own accordions: the second means the first, here.
        var accordions = Accordions().ToList();
        var start = accordions.IndexOf(accordionEl);
        if (start == -1)
        {
            return;
        }

        var next = e.Key switch
        {
            Key.Down => accordions[start + 1 < accordions.Count ? start + 1 : 0],
            Key.Up => accordions[start > 0 ? start - 1 : accordions.Count - 1],
            Key.Home => accordions[0],
            _ => accordions[^1],
        };

        next.FocusFirst();
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary><c>role="presentation"</c>: it says nothing of itself, and its accordions are read as they are.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}
