using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-back-button</c>: pops the closest <see cref="AvonicNav"/> when it can go back, and otherwise sets
/// <see cref="DefaultPage"/> as its root, or asks the app to go to <see cref="DefaultHref"/> (<see cref="DefaultHrefRequested"/>).
/// Shown only with either, or in the header of a page the nav has marked <c>:can-go-back</c>. The look is in the theme. See <c>docs/components/ion-back-button/README.md</c>.
/// </summary>
/// <remarks>
/// It has no hardware back of its own, as Ionic's has none: <see cref="AvonicNav.HandleBackButton"/> is the nav's.
/// </remarks>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_Icon", typeof(AvonicIcon))]
[TemplatePart("PART_Text", typeof(TextBlock))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(":in-toolbar", ":in-toolbar-color", ":icon-only")]
public partial class AvonicBackButton : TemplatedControl
{
    /// <summary>md's <c>backButtonIcon</c>, <c>arrowBackSharp</c>, when neither <see cref="Icon"/> nor the config is set.</summary>
    internal const string MdIcon = "arrow-back-sharp";

    /// <summary>The native button's <c>aria-label</c> when there's neither the app's nor a text.</summary>
    internal const string DefaultLabel = "back";

    /// <summary>The page class the nav gives every page it builds.</summary>
    private const string PageClass = "ion-page";

    /// <summary>Ionic's <c>SCHEME</c> in <c>openURL</c>: a URL with one isn't handed to the router.</summary>
    [GeneratedRegex("^[a-z][a-z0-9+\\-.]*:")]
    private static partial Regex Scheme();

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicBackButton>();

    /// <summary>
    /// Ionic <c>defaultHref</c>: where to go when there's nothing to pop. Not null (the empty string too) shows the button; a click
    /// with nothing to pop raises <see cref="DefaultHrefRequested"/> with it. Null when the button is first attached takes
    /// <see cref="AvonicConfig.BackButtonDefaultHref"/>.
    /// </summary>
    public static readonly StyledProperty<string?> DefaultHrefProperty =
        AvaloniaProperty.Register<AvonicBackButton, string?>(nameof(DefaultHref));

    /// <summary>
    /// Ionic <c>icon</c>: an <see cref="AvonicIcon.Icon"/>, a name or a source. Null takes <see cref="AvonicConfig.BackButtonIcon"/>,
    /// then md's <c>arrow-back-sharp</c>, both mirrored in right-to-left; empty shows none.
    /// </summary>
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<AvonicBackButton, string?>(nameof(Icon));

    /// <summary>Ionic <c>text</c>. Null takes <see cref="AvonicConfig.BackButtonText"/>, then md's none; empty shows none.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<AvonicBackButton, string?>(nameof(Text));

    /// <summary>Ionic <c>routerAnimation</c>: the pop's transition, over the nav's; and <see cref="DefaultHrefRequested"/>'s.</summary>
    public static readonly StyledProperty<AnimationBuilder?> RouterAnimationProperty =
        AvaloniaProperty.Register<AvonicBackButton, AnimationBuilder?>(nameof(RouterAnimation));

    /// <summary>
    /// Avonic extension: CSS's <c>text-transform</c> for <see cref="Text"/>, as on <see cref="AvonicButton"/>. The md theme sets
    /// <see cref="ButtonTextTransform.Uppercase"/>.
    /// </summary>
    public static readonly StyledProperty<ButtonTextTransform> TextTransformProperty =
        AvonicButton.TextTransformProperty.AddOwner<AvonicBackButton>();

    /// <summary>
    /// Avonic extension: the page to go back to when there's nothing to pop, in any of <see cref="AvonicNav"/>'s page forms (a
    /// <see cref="Control"/>, a <see cref="Type"/> of control, a <c>Func&lt;Control&gt;</c>, a view model). With nothing to pop
    /// and a nav around, a click sets it as the nav's root, as a back (<see cref="NavDirection.Back"/>, <see cref="RouterAnimation"/>,
    /// <c>skipIfBusy</c>): the frameworks' "navigate back to <c>defaultHref</c>", with a page for the URL. It shows the button,
    /// as <see cref="DefaultHref"/> does. With no nav around, the click is <see cref="DefaultHref"/>'s, as if it weren't set.
    /// </summary>
    public static readonly StyledProperty<object?> DefaultPageProperty =
        AvaloniaProperty.Register<AvonicBackButton, object?>(nameof(DefaultPage));

    /// <summary>
    /// Avonic extension: a click with nothing to pop and no <see cref="DefaultPage"/> to go to, where Ionic's <c>openURL</c> hands <see cref="DefaultHref"/> to the router
    /// (<c>router.push(url, 'back', routerAnimation)</c>): not null, not starting with <c>#</c>, with no scheme. Bubbles, so one
    /// handler on the window serves every back button, as one <c>ion-router</c> does. With no handler nothing happens, as in
    /// Ionic with no router.
    /// </summary>
    public static readonly RoutedEvent<BackButtonDefaultHrefEventArgs> DefaultHrefRequestedEvent =
        RoutedEvent.Register<AvonicBackButton, BackButtonDefaultHrefEventArgs>(nameof(DefaultHrefRequested), RoutingStrategies.Bubble);

    private readonly StyleValue<bool> _shown = new();
    private readonly ConfigOnLoad<string?> _defaultHref;
    private readonly List<Control> _pages = [];
    private AvonicIcon? _icon;
    private TextBlock? _text;
    private AvonicRippleEffect? _ripple;
    private bool _attached;
    private bool _inCanGoBackPage;
    private double _arrangedWidth;

    static AvonicBackButton()
    {
        // The native <button> takes focus.
        FocusableProperty.OverrideDefaultValue<AvonicBackButton>(true);
    }

    public AvonicBackButton()
    {
        // ion-activatable and ion-focusable, always, at style priority, before any style, so that an app's style wins. Hidden
        // (display: none) the same way until there's somewhere to go.
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
        SetValue(FocusVisible.IsEnabledProperty, true, BindingPriority.Style);
        Bind(IsVisibleProperty, _shown, BindingPriority.Style);
        _defaultHref = ConfigOnLoad<string?>.IfUnset(this, DefaultHrefProperty);
        UpdateShown();

        // The native button: a click, Enter and Space.
        _ = new NativeButton(this, _ => OnClick(), static () => true, static () => { });
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string? DefaultHref
    {
        get => GetValue(DefaultHrefProperty);
        set => SetValue(DefaultHrefProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public object? DefaultPage
    {
        get => GetValue(DefaultPageProperty);
        set => SetValue(DefaultPageProperty, value);
    }

    public AnimationBuilder? RouterAnimation
    {
        get => GetValue(RouterAnimationProperty);
        set => SetValue(RouterAnimationProperty, value);
    }

    public ButtonTextTransform TextTransform
    {
        get => GetValue(TextTransformProperty);
        set => SetValue(TextTransformProperty, value);
    }

    /// <inheritdoc cref="DefaultHrefRequestedEvent"/>
    public event EventHandler<BackButtonDefaultHrefEventArgs>? DefaultHrefRequested
    {
        add => AddHandler(DefaultHrefRequestedEvent, value);
        remove => RemoveHandler(DefaultHrefRequestedEvent, value);
    }

    /// <summary>Ionic's <c>backButtonIcon</c>: the icon drawn, or null or empty for none.</summary>
    private string? ResolvedIcon => Icon ?? AvonicConfig.BackButtonIcon ?? MdIcon;

    /// <summary>Ionic's <c>backButtonText</c>: md's default is none.</summary>
    private string? ResolvedText => Text ?? AvonicConfig.BackButtonText;

    /// <summary>Ionic's <c>hasIconOnly</c>: an icon, and no text.</summary>
    private bool HasIconOnly => !string.IsNullOrEmpty(ResolvedIcon) && string.IsNullOrEmpty(ResolvedText);

    /// <summary>
    /// Ionic's <c>onClick</c>: on the closest nav, if it can go back, <c>pop({ animationBuilder: routerAnimation, skipIfBusy: true })</c>;
    /// otherwise <c>openURL(defaultHref, ev, 'back', routerAnimation)</c>, which is the app's to route.
    /// </summary>
    private void OnClick()
    {
        if (!IsEffectivelyEnabled)
        {
            return;
        }

        var nav = this.FindAncestorOfType<AvonicNav>();
        if (nav is not null && nav.CanGoBack())
        {
            _ = nav.Pop(new NavOptions { AnimationBuilder = RouterAnimation, SkipIfBusy = true });
            return;
        }

        if (nav is not null && DefaultPage is { } page)
        {
            _ = nav.SetRoot(page, null, new NavOptions { Direction = NavDirection.Back, AnimationBuilder = RouterAnimation, SkipIfBusy = true });
            return;
        }

        if (DefaultHref is { } url && (url.Length == 0 || url[0] != '#') && !Scheme().IsMatch(url))
        {
            RaiseEvent(new BackButtonDefaultHrefEventArgs(DefaultHrefRequestedEvent, this, url, RouterAnimation));
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _icon = e.NameScope.Find<AvonicIcon>("PART_Icon");
        _text = e.NameScope.Find<TextBlock>("PART_Text");
        _ripple = e.NameScope.Find<AvonicRippleEffect>("PART_Ripple");
        Render();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;

        // componentWillLoad: the config's defaultHref, once, if the prop isn't set.
        _defaultHref.Load(() => AvonicConfig.BackButtonDefaultHref is { } href ? href : Optional<string?>.Empty);
        WatchPages();
        Render();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        UnwatchPages();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DefaultHrefProperty || change.Property == DefaultPageProperty)
        {
            UpdateShown();
        }

        // Ionic renders again when any of its props changes: the config and hostContext are read again.
        if (change.Property == DefaultHrefProperty || change.Property == IconProperty || change.Property == TextProperty
            || change.Property == ColorProperty || change.Property == IsEnabledProperty || change.Property == TextTransformProperty)
        {
            if (_attached)
            {
                Render();
            }
        }
    }

    // ── Shown ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// core.css's <c>.can-go-back &gt; ion-header ion-back-button</c>: each <see cref="AvonicHeader"/> around the button whose page
    /// it's a direct child of (the page's logical child, or behind panels, which stand for the page's slot). The page's
    /// <c>Classes</c> are followed while attached, as the pseudoclass comes and goes with the nav's transitions.
    /// </summary>
    private void WatchPages()
    {
        UnwatchPages();
        foreach (var header in this.GetVisualAncestors().OfType<AvonicHeader>())
        {
            if (PageOf(header) is { } page)
            {
                _pages.Add(page);
                page.Classes.CollectionChanged += OnPageClassesChanged;
            }
        }

        UpdateCanGoBack();
    }

    private void UnwatchPages()
    {
        foreach (var page in _pages)
        {
            page.Classes.CollectionChanged -= OnPageClassesChanged;
        }

        _pages.Clear();
        UpdateCanGoBack();
    }

    /// <summary>The page the header is a direct child of: its logical parent, past any panels, with class <c>ion-page</c>.</summary>
    private static Control? PageOf(AvonicHeader header)
    {
        var parent = header.GetLogicalParent();
        while (parent is Panel)
        {
            parent = parent.GetLogicalParent();
        }

        return parent is Control page && page.Classes.Contains(PageClass) ? page : null;
    }

    private void OnPageClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCanGoBack();

    private void UpdateCanGoBack()
    {
        _inCanGoBackPage = _pages.Any(page => page.Classes.Contains(AvonicNav.CanGoBackPseudoClass));
        UpdateShown();
    }

    /// <summary><c>display: none</c>, but <c>display: block</c> with <c>.show-back-button</c> or in a can-go-back page's header.</summary>
    private void UpdateShown() => _shown.SetWhen(DefaultHref is null && DefaultPage is null && !_inCanGoBackPage, false);

    // ── Render ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>render()</c>: the icon (<c>flip-rtl</c> when <see cref="Icon"/> is null), the text, the ripple's type, the icon-only
    /// class, and <c>in-toolbar</c>, <c>in-toolbar-color</c> by <c>hostContext</c>: any ancestor toolbar, and any with a colour.
    /// </summary>
    private void Render()
    {
        var icon = ResolvedIcon;
        var text = ResolvedText;
        if (_icon is not null)
        {
            _icon.Icon = string.IsNullOrEmpty(icon) ? null : icon;
            _icon.FlipRtl = Icon is null ? true : null;
            _icon.IsVisible = !string.IsNullOrEmpty(icon);
        }

        if (_text is not null)
        {
            _text.Text = text is { Length: > 0 } && TextTransform == ButtonTextTransform.Uppercase ? text.ToUpperInvariant() : text;
            _text.IsVisible = !string.IsNullOrEmpty(text);
        }

        var iconOnly = HasIconOnly;
        if (_ripple is not null)
        {
            _ripple.Type = iconOnly ? RippleEffectType.Unbounded : RippleEffectType.Bounded;
        }

        bool inToolbar = false, inToolbarColor = false;
        foreach (var ancestor in this.GetVisualAncestors())
        {
            if (ancestor is AvonicToolbar toolbar)
            {
                inToolbar = true;
                inToolbarColor |= toolbar.Color is not null;
            }
        }

        if (PseudoClasses.Contains(":icon-only") != iconOnly)
        {
            _arrangedWidth = 0;
            InvalidateMeasure();
        }

        PseudoClasses.Set(":icon-only", iconOnly);
        PseudoClasses.Set(":in-toolbar", inToolbar);
        PseudoClasses.Set(":in-toolbar-color", inToolbarColor);
    }

    // ── Layout: aspect-ratio ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// md's <c>aspect-ratio: 1/1</c> with only an icon: as tall as it's wide. Its width is the layout's: its content's in a row
    /// (48), or the whole line when it's stretched across one, as a <c>display: block</c> host is (390 x 390 measured). The width
    /// it was last arranged at is its height in the next measure, so the width never follows the height.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        if (!PseudoClasses.Contains(":icon-only"))
        {
            return size;
        }

        var width = Math.Max(size.Width, Math.Min(_arrangedWidth, availableSize.Width));
        return new Size(size.Width, Math.Max(size.Height, width));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // The width the layout gave it: its content's unless it's stretched. A new one is its height in the next measure, which
        // the width doesn't depend on, so it settles in one more pass.
        if (PseudoClasses.Contains(":icon-only") && !finalSize.Width.Equals(_arrangedWidth))
        {
            _arrangedWidth = finalSize.Width;
            InvalidateMeasure();
        }

        return base.ArrangeOverride(finalSize);
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new BackButtonPeer(this);

    /// <summary>
    /// The native button: named by <c>aria-label</c>, the app's, else the text as written, else <c>back</c>; the icon and the text
    /// are <c>aria-hidden</c>, so it has no children.
    /// </summary>
    private sealed class BackButtonPeer(AvonicBackButton owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override string? GetNameCore() =>
            base.GetNameCore() is { Length: > 0 } name ? name
            : owner.ResolvedText is { Length: > 0 } text ? text
            : DefaultLabel;

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public void Invoke() => owner.OnClick();
    }
}

/// <summary>
/// <see cref="AvonicBackButton.DefaultHrefRequested"/>: Ionic's <c>router.push(defaultHref, 'back', routerAnimation)</c>, for the
/// app's own navigation.
/// </summary>
public sealed class BackButtonDefaultHrefEventArgs : RoutedEventArgs
{
    internal BackButtonDefaultHrefEventArgs(RoutedEvent routedEvent, object source, string defaultHref, AnimationBuilder? routerAnimation)
        : base(routedEvent, source)
    {
        DefaultHref = defaultHref;
        RouterAnimation = routerAnimation;
    }

    /// <summary>The back button's <see cref="AvonicBackButton.DefaultHref"/>.</summary>
    public string DefaultHref { get; }

    /// <summary>The back button's <see cref="AvonicBackButton.RouterAnimation"/>, for a transition the app runs.</summary>
    public AnimationBuilder? RouterAnimation { get; }
}
