using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-item</c>: a row with start, default and end content, and optional detail icon, that
/// can be tappable. It's also what the form controls inside it report to.
/// </summary>
[TemplatePart("PART_Start", typeof(ContentPresenter))]
[TemplatePart("PART_Container", typeof(ContentPresenter))]
[TemplatePart("PART_End", typeof(ContentPresenter))]
[TemplatePart("PART_DetailIcon", typeof(AvonicIcon))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(
    ":lines-default", ":lines-full", ":lines-inset", ":lines-none", ":button", ":ion-activatable",
    ":item-multiple-inputs", ":item-control-needs-pointer-cursor")]
public class AvonicItem : TemplatedControl
{
    /// <summary><c>margin-inline-end: -6px</c> on <c>.item-detail-icon</c>. There's no variable for it.</summary>
    private const double DetailIconMarginEnd = -6;

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>button</c>: the item is tappable.</summary>
    public static readonly StyledProperty<bool> ButtonProperty =
        AvaloniaProperty.Register<AvonicItem, bool>(nameof(Button));

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicItem>();

    /// <summary>Ionic <c>detail</c>. In md, only <c>true</c> shows the icon.</summary>
    public static readonly StyledProperty<bool?> DetailProperty =
        AvaloniaProperty.Register<AvonicItem, bool?>(nameof(Detail));

    /// <summary>Ionic <c>detailIcon</c>: a name or a source, as <see cref="AvonicIcon.Icon"/> takes.</summary>
    public static readonly StyledProperty<string> DetailIconProperty =
        AvaloniaProperty.Register<AvonicItem, string>(nameof(DetailIcon), "chevron-forward");

    /// <summary>Ionic <c>lines</c>. Null leaves it to the item's default, or an <c>ion-list</c>'s.</summary>
    public static readonly StyledProperty<ItemLines?> LinesProperty =
        AvaloniaProperty.Register<AvonicItem, ItemLines?>(nameof(Lines));

    /// <summary>Ionic's default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Ionic's <c>start</c> slot.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(EndContentTemplate));

    public bool Button
    {
        get => GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool? Detail
    {
        get => GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public string DetailIcon
    {
        get => GetValue(DetailIconProperty);
        set => SetValue(DetailIconProperty, value);
    }

    public ItemLines? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
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

    // ── Extensions ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avonic extension: a <see cref="Button"/> item was tapped or activated by keyboard. Ionic gets
    /// this from its native <c>&lt;button&gt;</c>.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicItem, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>
    /// For the theme: <c>margin-inline-start: calc(var(--inner-padding-end) / 2)</c> and
    /// <c>margin-inline-end: -6px</c> on the detail icon.
    /// </summary>
    public static readonly IMultiValueConverter DetailIconMarginConverter =
        new FuncMultiValueConverter<object?, Thickness>(values =>
            new Thickness(
                values.FirstOrDefault() is double innerPaddingEnd ? innerPaddingEnd / 2 : 0,
                0,
                DetailIconMarginEnd,
                0));

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>getFirstInteractive</c>'s selector.</summary>
    private static readonly HashSet<ItemControlType> FirstInteractiveTypes =
    [
        ItemControlType.Toggle,
        ItemControlType.Checkbox,
        ItemControlType.Radio,
        ItemControlType.Select,
        ItemControlType.Input,
        ItemControlType.Textarea,
    ];

    private readonly Dictionary<Type, HashSet<string>> _itemStyles = [];
    private readonly HashSet<string> _appliedItemStyles = [];
    private ContentPresenter?[] _slots = [];
    private AvonicIcon? _detailIcon;
    private AvonicRippleEffect? _ripple;
    private bool _multipleInputs;
    private bool _spacePressed;
    private object? _secondTapSource;
    private IPointer? _secondTapPointer;
    private IPointer? _pressPointer;
    private IDisposable? _focusableFrame;
    private IDisposable? _noInputFrame;

    public AvonicItem()
    {
        AddHandler(TappedEvent, OnTapped);

        // A second quick press raises DoubleTapped, on the press, and no Tapped. A native <button> clicks each
        // time, on release, so a finger that goes down to scroll doesn't activate anything.
        AddHandler(PointerPressedEvent, OnPointerPressedBeforeSecondTap, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(DoubleTappedEvent, (_, e) =>
        {
            _secondTapSource = e.Source;
            _secondTapPointer = _pressPointer;
        });
        AddHandler(PointerReleasedEvent, OnPointerReleasedAfterSecondTap, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(IonStyle.IonStyleEvent, OnIonStyle);
        UpdateLines();
        UpdateInteractivity();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LinesProperty)
        {
            UpdateLines();
        }
        else if (change.Property == ButtonProperty)
        {
            _spacePressed = false;
            UpdateInteractivity();
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            // Later, because this runs before the controls inside find out.
            Dispatcher.UIThread.Post(UpdateInteractivity, DispatcherPriority.Loaded);
        }
        else if (change.Property == DetailProperty)
        {
            UpdateDetail();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        foreach (var slot in _slots)
        {
            if (slot is not null)
            {
                slot.PropertyChanged -= OnSlotPropertyChanged;
            }
        }

        _slots =
        [
            e.NameScope.Find<ContentPresenter>("PART_Start"),
            e.NameScope.Find<ContentPresenter>("PART_Container"),
            e.NameScope.Find<ContentPresenter>("PART_End"),
        ];

        foreach (var slot in _slots)
        {
            if (slot is not null)
            {
                slot.PropertyChanged += OnSlotPropertyChanged;
            }
        }

        _detailIcon = e.NameScope.Find<AvonicIcon>("PART_DetailIcon");
        _ripple = e.NameScope.Find<AvonicRippleEffect>("PART_Ripple");

        UpdateDetail();
        UpdateInteractivity();
    }

    /// <summary>
    /// <c>updateInteractivityOnSlotChange</c>: a presenter's child changing is <c>slotchange</c>. The child
    /// isn't attached yet, so whether it's disabled through an ancestor isn't known until later, which is
    /// why Ionic also does this in a <c>raf</c> on load.
    /// </summary>
    private void OnSlotPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            Dispatcher.UIThread.Post(UpdateInteractivity, DispatcherPriority.Loaded);
        }
    }

    private void UpdateLines()
    {
        var lines = Lines;
        PseudoClasses.Set(":lines-default", lines is null);
        PseudoClasses.Set(":lines-full", lines == ItemLines.Full);
        PseudoClasses.Set(":lines-inset", lines == ItemLines.Inset);
        PseudoClasses.Set(":lines-none", lines == ItemLines.None);
    }

    /// <summary><c>showDetail</c>, in md.</summary>
    private void UpdateDetail()
    {
        if (_detailIcon is not null)
        {
            _detailIcon.IsVisible = Detail == true;
        }
    }

    // ── The controls inside ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything slotted, in slot order. A presenter is its child's logical parent, so the host's own
    /// logical descendants don't include these.
    /// </summary>
    private IEnumerable<Control> SlottedControls() =>
        _slots.Select(slot => slot?.Child)
            .OfType<Control>()
            .SelectMany(child => child.GetSelfAndLogicalDescendants().OfType<Control>());

    private static ItemControlType? TypeOf(Control control) => control switch
    {
        IItemControl itemControl => itemControl.ItemControlType,
        Avalonia.Controls.Button => ItemControlType.Button,
        _ => null,
    };

    /// <summary><c>getFirstInteractive</c>.</summary>
    private IItemControl? FirstInteractive() =>
        SlottedControls()
            .Where(control => control.IsEffectivelyEnabled)
            .OfType<IItemControl>()
            .FirstOrDefault(control => FirstInteractiveTypes.Contains(control.ItemControlType));

    /// <summary><c>setMultipleInputs</c>, <c>canActivate</c> and <c>isFocusable</c>.</summary>
    private void UpdateInteractivity()
    {
        int covers = 0, inputs = 0, clickables = 0;
        var focusableChild = false;
        foreach (var control in SlottedControls())
        {
            focusableChild |= FocusVisible.GetIsEnabled(control);
            switch (TypeOf(control))
            {
                case ItemControlType.Checkbox or ItemControlType.Datetime or ItemControlType.Select or ItemControlType.Radio:
                    covers++;
                    break;
                case ItemControlType.Input or ItemControlType.Range or ItemControlType.Searchbar
                    or ItemControlType.Segment or ItemControlType.Textarea or ItemControlType.Toggle:
                    inputs++;
                    break;
                case ItemControlType.Button:
                    clickables++;
                    break;
            }
        }

        var clickable = Button;
        _multipleInputs = covers + inputs > 1 || covers + clickables > 1 || (covers > 0 && clickable);
        var hasCover = covers == 1 && !_multipleInputs;
        var canActivate = clickable || hasCover;

        PseudoClasses.Set(":button", clickable);
        PseudoClasses.Set(":ion-activatable", canActivate);
        PseudoClasses.Set(":item-multiple-inputs", _multipleInputs);
        Activatable.SetIsEnabled(this, canActivate);
        FocusVisible.SetIsEnabled(this, canActivate || focusableChild);

        // A <button> takes focus, and a cover's control takes it itself.
        SetStyleValue(ref _focusableFrame, FocusableProperty, clickable, true);

        // :host(.item-interactive-disabled:not(.item-multiple-inputs)) { pointer-events: none }
        SetStyleValue(ref _noInputFrame, IsHitTestVisibleProperty, PseudoClasses.Contains(":item-interactive-disabled") && !_multipleInputs, false);

        if (_ripple is not null)
        {
            _ripple.IsVisible = canActivate;
        }

        // firstInteractiveNeedsPointerCursor
        PseudoClasses.Set(
            ":item-control-needs-pointer-cursor",
            FirstInteractive() is { ItemControlType: not (ItemControlType.Input or ItemControlType.Textarea) });
    }

    /// <summary>
    /// Behaviour, so it doesn't depend on the theme, which an app can replace. At style priority, as CSS is,
    /// so an app's own value wins.
    /// </summary>
    private void SetStyleValue(ref IDisposable? frame, StyledProperty<bool> property, bool active, bool value)
    {
        if (active && frame is null)
        {
            frame = SetValue(property, value, BindingPriority.Style);
        }
        else if (!active)
        {
            frame?.Dispose();
            frame = null;
        }
    }

    /// <summary><c>clickedWithinShadowRoot</c>, inverted. Plain text in a slot has no element of its own, so it's padding.</summary>
    private bool IsOnSlottedControl(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != this; visual = visual.GetVisualParent())
        {
            foreach (var slot in _slots)
            {
                if (slot?.Child == visual)
                {
                    return !(visual is TextBlock && slot.Content is not Control && slot.ContentTemplate is null);
                }
            }
        }

        return false;
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    private void OnTapped(object? sender, TappedEventArgs e) => e.Handled = Tap(e.Source);

    private void OnPointerPressedBeforeSecondTap(object? sender, PointerPressedEventArgs e)
    {
        // Another finger going down doesn't end this one's press.
        if (e.Pointer == _secondTapPointer)
        {
            _secondTapSource = null;
            _secondTapPointer = null;
        }

        _pressPointer = e.Pointer;
    }

    private void OnPointerReleasedAfterSecondTap(object? sender, PointerReleasedEventArgs e)
    {
        if (_secondTapSource is { } source && e.Pointer == _secondTapPointer)
        {
            _secondTapSource = null;
            _secondTapPointer = null;
            if (new Rect(Bounds.Size).Contains(e.GetPosition(this)))
            {
                Tap(source);
            }
        }
    }

    /// <summary><c>onClick</c>. True if the tap was forwarded, which ends it: <c>stopImmediatePropagation</c>.</summary>
    private bool Tap(object? source)
    {
        if (Button)
        {
            RaiseClick();
        }

        if (!_multipleInputs && FirstInteractive() is { } first && !IsOnSlottedControl(source))
        {
            first.OnItemTap();
            return true;
        }

        return false;
    }

    private void RaiseClick() => RaiseEvent(new RoutedEventArgs(ClickEvent, this));

    /// <summary>A native <c>&lt;button&gt;</c>: Enter activates on press, and Space on release.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || !Button || !ReferenceEquals(e.Source, this))
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            Tap(this);
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            _spacePressed = true;
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == Key.Space && _spacePressed)
        {
            _spacePressed = false;
            Tap(this);
            e.Handled = true;
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _spacePressed = false;
    }

    // ── ionStyle ─────────────────────────────────────────────────────────────────────────────

    /// <summary><c>itemStyle</c>: each true key becomes <c>:item-key</c>, kept per type of child, as Ionic's by tag name.</summary>
    private void OnIonStyle(object? sender, IonStyleEventArgs e)
    {
        e.Handled = true;
        if (e.Source is null)
        {
            return;
        }

        _itemStyles[e.Source.GetType()] = e.Styles.Where(style => style.Value).Select(style => style.Key).ToHashSet();

        var styles = _itemStyles.Values.SelectMany(keys => keys).ToHashSet();
        foreach (var removed in _appliedItemStyles.Except(styles))
        {
            PseudoClasses.Remove(":item-" + removed);
        }

        foreach (var added in styles.Except(_appliedItemStyles))
        {
            PseudoClasses.Add(":item-" + added);
        }

        _appliedItemStyles.Clear();
        _appliedItemStyles.UnionWith(styles);

        // forceUpdate: Ionic re-renders, which also works out the first interactive control again.
        UpdateInteractivity();
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new ItemAutomationPeer(this);

    private sealed class ItemAutomationPeer(AvonicItem owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        /// <summary>Ionic only sets onClick when clickable, "to prevent screen readers from reading all items as clickable".</summary>
        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IInvokeProvider) && !owner.Button ? null : base.GetProviderCore(providerType);

        /// <summary><c>aria-disabled</c>: <c>disabled || childStyles['item-interactive-disabled']</c>.</summary>
        protected override bool IsEnabledCore() =>
            base.IsEnabledCore() && !owner.PseudoClasses.Contains(":item-interactive-disabled");

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            owner.Button ? AutomationControlType.Button : AutomationControlType.Group;

        /// <summary>A native button names itself from what's inside it.</summary>
        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var text = owner.SlottedControls().OfType<TextBlock>().Select(block => block.Text).Where(value => !string.IsNullOrEmpty(value));
            return string.Join(" ", text);
        }

        protected override bool IsContentElementCore() => owner.Button;

        protected override bool IsControlElementCore() => true;

        public void Invoke()
        {
            if (owner.Button && IsEnabled())
            {
                owner.RaiseClick();
            }
        }
    }
}
