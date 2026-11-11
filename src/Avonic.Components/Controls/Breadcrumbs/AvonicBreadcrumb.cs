using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-breadcrumb</c>: one step of an <see cref="AvonicBreadcrumbs"/> trail, with a separator after it, or the collapsed
/// indicator standing for the hidden ones. The breadcrumbs works out which is last, active, separated and collapsed. The look is in
/// the theme. See <c>docs/components/ion-breadcrumbs/README.md</c>.
/// </summary>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_CollapsedIndicator", typeof(Button))]
[TemplatePart("PART_Separator", typeof(Control))]
[PseudoClasses(":active", ":collapsed", ":in-breadcrumbs-color", ":in-toolbar-color")]
public class AvonicBreadcrumb : TemplatedControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic <c>active</c>: the current page. <see cref="AvonicBreadcrumbs"/> makes the last one active if none is, as Ionic
    /// assigns the prop, so it reads true here too.
    /// </summary>
    public static readonly StyledProperty<bool> ActiveProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, bool>(nameof(Active));

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicBreadcrumb>();

    /// <summary>
    /// Ionic <c>href</c>: makes the breadcrumb a link, which takes focus, clicks on Enter, and shows hover and focus. Avonic doesn't
    /// navigate, as there's no router: follow it from <see cref="Click"/> or <see cref="Command"/>.
    /// </summary>
    public static readonly StyledProperty<string?> HrefProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, string?>(nameof(Href));

    /// <summary>
    /// Ionic <c>separator</c>. Null leaves it to <see cref="AvonicBreadcrumbs"/>, which sets it true on every breadcrumb but the last,
    /// as Ionic assigns the prop. The last never shows one.
    /// </summary>
    public static readonly StyledProperty<bool?> SeparatorProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, bool?>(nameof(Separator));

    /// <summary>Ionic's default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Ionic's <c>start</c> slot, which <c>core.json</c> doesn't list and <c>breadcrumb.js</c> renders.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, IDataTemplate?>(nameof(EndContentTemplate));

    /// <summary>Ionic's <c>separator</c> slot. Anything in it replaces md's <c>/</c>, as a slot's children are its fallback.</summary>
    public static readonly StyledProperty<object?> SeparatorContentProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, object?>(nameof(SeparatorContent));

    public static readonly StyledProperty<IDataTemplate?> SeparatorContentTemplateProperty =
        AvaloniaProperty.Register<AvonicBreadcrumb, IDataTemplate?>(nameof(SeparatorContentTemplate));

    public bool Active
    {
        get => GetValue(ActiveProperty);
        set => SetValue(ActiveProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string? Href
    {
        get => GetValue(HrefProperty);
        set => SetValue(HrefProperty, value);
    }

    public bool? Separator
    {
        get => GetValue(SeparatorProperty);
        set => SetValue(SeparatorProperty, value);
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

    public object? SeparatorContent
    {
        get => GetValue(SeparatorContentProperty);
        set => SetValue(SeparatorContentProperty, value);
    }

    public IDataTemplate? SeparatorContentTemplate
    {
        get => GetValue(SeparatorContentTemplateProperty);
        set => SetValue(SeparatorContentTemplateProperty, value);
    }

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionFocus</c>: the breadcrumb itself took focus, not its collapsed indicator.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicBreadcrumb, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>: the breadcrumb itself lost focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicBreadcrumb, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

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

    /// <summary>
    /// Ionic's internal <c>collapsedClick</c>: the indicator was clicked. <see cref="AvonicBreadcrumbs"/> turns it into
    /// <see cref="AvonicBreadcrumbs.IonCollapsedClick"/>.
    /// </summary>
    internal static readonly RoutedEvent<RoutedEventArgs> CollapsedClickEvent =
        RoutedEvent.Register<AvonicBreadcrumb, RoutedEventArgs>("CollapsedClick", RoutingStrategies.Bubble);

    // ── Extensions ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avonic extension: the DOM <c>click</c> an Ionic app listens to on the breadcrumb, and the way to follow <see cref="Href"/>
    /// until there's a router. On the release of every press on it, link or not, and on Enter on a link, not Space, as an
    /// <c>&lt;a&gt;</c>. A click on the collapsed indicator clicks the breadcrumb too, after
    /// <see cref="AvonicBreadcrumbs.IonCollapsedClick"/>, as the DOM click bubbles to the host.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicBreadcrumb, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add
        {
            AddHandler(ClickEvent, value);
            _clickHandlers++;
        }

        remove
        {
            RemoveHandler(ClickEvent, value);
            _clickHandlers = Math.Max(0, _clickHandlers - 1);
        }
    }

    /// <summary>
    /// Avonic extension: runs when the breadcrumb clicks, unless a <see cref="Click"/> handler set <c>Handled</c>. One that can't
    /// execute disables the breadcrumb, as it does a <see cref="Button"/>.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvonicButton.CommandProperty.AddOwner<AvonicBreadcrumb>();

    /// <summary>Avonic extension: the parameter for <see cref="Command"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvonicButton.CommandParameterProperty.AddOwner<AvonicBreadcrumb>();

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

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private readonly NativeButton _native;
    private readonly StyleValue<bool> _link = new();
    private int _clickHandlers;
    private bool _collapsed;
    private bool _last;
    private bool _showCollapsedIndicator;
    private bool _releasing;
    private bool _indicatorReleased;
    private Button? _indicator;
    private Control? _separatorPart;
    private Control? _nativePart;
    private AvonicBreadcrumbs? _breadcrumbs;

    static AvonicBreadcrumb()
    {
        // align-items: center in the breadcrumbs' row. A default, not the theme's, so that an app's value wins and a replaced theme keeps it.
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicBreadcrumb>(VerticalAlignment.Center);

        // overflow: visible, so that a focused breadcrumb's shadow isn't cut off. A TemplatedControl clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicBreadcrumb>(false);
    }

    public AvonicBreadcrumb()
    {
        // ion-activatable and ion-focusable while it's a link that shows, and a tab stop, as the <a href> is. Style priority, bound
        // once, so that an app's value wins. A collapsed one's <a> is display: none, so it takes no focus.
        Bind(Activatable.IsEnabledProperty, _link, BindingPriority.Style);
        Bind(FocusVisible.IsEnabledProperty, _link, BindingPriority.Style);
        Bind(FocusableProperty, _link, BindingPriority.Style);

        // The DOM click on release, with no keys of its own: a link takes Enter only, below.
        _native = new NativeButton(this, _ => OnNativeClick(), () => false, UpdateIsEffectivelyEnabled);

        // A release on its way through, for the indicator, which clicks during it, before NativeButton sees it: see OnIndicatorClick.
        // Registered after NativeButton, so the bubble clears both after it. Nothing is left set when a release goes elsewhere, as
        // one does when a gesture takes the pointer.
        AddHandler(PointerReleasedEvent, (_, _) => _releasing = true, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, (_, _) => _releasing = _indicatorReleased = false, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>Ionic's internal <c>collapsed</c>: hidden in the collapsed middle of the trail.</summary>
    internal bool Collapsed
    {
        get => _collapsed;
        set
        {
            if (_collapsed != value)
            {
                _collapsed = value;
                Update();

                // display: none on the focused <a> blurs it, and focus goes back to the page.
                if (value && IsFocused)
                {
                    TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
                }
            }
        }
    }

    /// <summary>Ionic's internal <c>last</c>: the end of the trail, with no separator.</summary>
    internal bool Last
    {
        get => _last;
        set
        {
            if (_last != value)
            {
                _last = value;
                Update();
            }
        }
    }

    /// <summary>Ionic's internal <c>showCollapsedIndicator</c>: the first collapsed breadcrumb shows the indicator.</summary>
    internal bool ShowCollapsedIndicator
    {
        get => _showCollapsedIndicator;
        set
        {
            if (_showCollapsedIndicator != value)
            {
                _showCollapsedIndicator = value;
                Update();
            }
        }
    }

    /// <summary>
    /// <c>showSeparator</c>: never on the last; collapsed, only beside the indicator; otherwise <see cref="Separator"/>.
    /// </summary>
    internal bool ShowSeparator => !_last && (_collapsed ? _showCollapsedIndicator : Separator == true);

    /// <summary>The indicator, for <see cref="BreadcrumbCollapsedClickEventArgs.IonShadowTarget"/>.</summary>
    internal Button? CollapsedIndicator => _indicator;

    /// <summary>A command that can't execute disables the breadcrumb. The base class's constructor reads this before there's a <see cref="NativeButton"/>.</summary>
    protected override bool IsEnabledCore => base.IsEnabledCore && _native?.CanExecute != false;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _indicator?.RemoveHandler(Button.ClickEvent, OnIndicatorClick);
        _indicator = e.NameScope.Find<Button>("PART_CollapsedIndicator");
        _indicator?.AddHandler(Button.ClickEvent, OnIndicatorClick);
        _separatorPart = e.NameScope.Find<Control>("PART_Separator");
        _nativePart = e.NameScope.Find<Control>("PART_Native");
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Ionic renders again when a prop changes, and reads hostContext then.
        if (change.Property == ActiveProperty || change.Property == SeparatorProperty || change.Property == HrefProperty
            || change.Property == ColorProperty)
        {
            Update();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Update();
        _breadcrumbs = this.FindAncestorOfType<AvonicBreadcrumbs>();
        _breadcrumbs?.SlotChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var breadcrumbs = _breadcrumbs;
        _breadcrumbs = null;
        breadcrumbs?.SlotChanged();
    }

    /// <summary><c>render</c>: the host classes, and the indicator and separator, which Ionic renders or not.</summary>
    private void Update()
    {
        PseudoClasses.Set(":active", Active);
        PseudoClasses.Set(":collapsed", _collapsed);

        // hostContext('ion-breadcrumbs[color]') and hostContext('ion-toolbar[color]'): any ancestor, at each render.
        bool inBreadcrumbsColor = false, inToolbarColor = false;
        foreach (var ancestor in this.GetVisualAncestors())
        {
            if (ancestor is AvonicBreadcrumbs { Color: not null })
            {
                inBreadcrumbsColor = true;
            }
            else if (ancestor is AvonicToolbar { Color: not null })
            {
                inToolbarColor = true;
            }
        }

        PseudoClasses.Set(":in-breadcrumbs-color", inBreadcrumbsColor);
        PseudoClasses.Set(":in-toolbar-color", inToolbarColor);

        _link.SetWhen(Href is not null && !_collapsed, true);

        if (_indicator is not null)
        {
            _indicator.IsVisible = _showCollapsedIndicator;
        }

        if (_separatorPart is not null)
        {
            _separatorPart.IsVisible = ShowSeparator;
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>collapsedIndicatorClick</c>, then the DOM click reaching the host. A key or a screen reader clicks the indicator with no
    /// release, so both are raised here. A release that clicks it only marks it, and <see cref="NativeButton"/> decides, later in the
    /// same release, whether the press was a click at all, as Chromium cancels the whole click: then both are raised, or neither.
    /// </summary>
    private void OnIndicatorClick(object? sender, RoutedEventArgs e)
    {
        if (_releasing)
        {
            _indicatorReleased = true;
        }
        else
        {
            RaiseEvent(new RoutedEventArgs(CollapsedClickEvent, this));
            OnNativeClick();
        }
    }

    /// <summary>
    /// A click from a press, Enter, or automation. <see cref="NativeButton"/> already ends a press that the breadcrumb is disabled
    /// during; this is for automation's <c>Invoke</c>, which a disabled breadcrumb doesn't answer.
    /// </summary>
    private void OnNativeClick()
    {
        if (_indicatorReleased)
        {
            _indicatorReleased = false;
            RaiseEvent(new RoutedEventArgs(CollapsedClickEvent, this));
        }

        if (IsEffectivelyEnabled)
        {
            _native.Click(ClickEvent);
        }
    }

    /// <summary>Enter follows a link, on the key's press. Space doesn't: an <c>&lt;a&gt;</c> isn't a button. Measured.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.Enter && ReferenceEquals(e.Source, this) && Href is not null && !_collapsed)
        {
            OnNativeClick();
            e.Handled = true;
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        // onFocus is on the native element, so the indicator's focus isn't this.
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

    protected override AutomationPeer OnCreateAutomationPeer() => new BreadcrumbAutomationPeer(this);

    /// <summary>
    /// A link is a hyperlink named by its content, and a screen reader's activation is its click. Without <see cref="Href"/> it's a
    /// <c>&lt;span&gt;</c>, with no role, and its children speak for themselves. <c>aria-disabled</c> is the peer's
    /// <c>IsEnabled</c>.
    /// </summary>
    private sealed class BreadcrumbAutomationPeer(AvonicBreadcrumb owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        /// <summary>
        /// An <c>&lt;a href&gt;</c>. A disabled one has its <c>href</c> taken away, so it isn't a link: disabled itself, or by a
        /// <see cref="Command"/> that can't execute, but not by an ancestor, which leaves the <c>href</c>.
        /// </summary>
        private bool IsLink => owner.Href is not null && !owner._collapsed && owner.IsEnabledCore;

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            IsLink ? AutomationControlType.Hyperlink : AutomationControlType.Group;

        protected override bool IsContentElementCore() => IsLink;

        protected override bool IsControlElementCore() => true;

        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) && IsLink && owner._nativePart is { } native
                ? AccessibleText.Of(native)
                : name;
        }

        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IInvokeProvider) && !IsLink && owner._clickHandlers == 0 && owner.Command is null
                ? null
                : base.GetProviderCore(providerType);

        public void Invoke() => owner.OnNativeClick();
    }
}
