using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-menu-button</c>: the hamburger button that toggles a menu through <see cref="MenuController"/>, and hides itself while
/// that menu isn't active. Its content, the default slot, is the menu icon unless it's given one. The look is in the theme. See
/// <c>docs/components/ion-menu/README.md</c>.
/// </summary>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_Icon", typeof(AvonicIcon))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(":in-toolbar", ":in-toolbar-color")]
public class AvonicMenuButton : ContentControl
{
    /// <summary>md's <c>menuIcon</c>, <c>menuSharp</c>, when <see cref="AvonicConfig.MenuIcon"/> isn't set.</summary>
    internal const string MdMenuIcon = "menu-sharp";

    /// <summary>The native button's <c>aria-label</c> when the app gives none.</summary>
    internal const string DefaultLabel = "menu";

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicMenuButton>();

    /// <summary>Ionic <c>autoHide</c>: hidden while its menu isn't active. True.</summary>
    public static readonly StyledProperty<bool> AutoHideProperty =
        AvaloniaProperty.Register<AvonicMenuButton, bool>(nameof(AutoHide), defaultValue: true);

    /// <summary>
    /// Ionic <c>menu</c>: the menu it toggles, by its <see cref="AvonicMenu.MenuId"/>, or <c>start</c> or <c>end</c> for the menu on
    /// that side. Null is the first enabled menu.
    /// </summary>
    public static readonly StyledProperty<string?> MenuProperty =
        AvaloniaProperty.Register<AvonicMenuButton, string?>(nameof(Menu));

    private readonly StyleValue<bool> _shown = new();
    private AvonicIcon? _icon;
    private bool _visible;
    private bool _attached;

    static AvonicMenuButton()
    {
        // The native <button> takes focus.
        FocusableProperty.OverrideDefaultValue<AvonicMenuButton>(true);
    }

    public AvonicMenuButton()
    {
        // ion-activatable and ion-focusable, always, at style priority, before any style, so that an app's style wins. Hidden
        // (display: none) the same way while autoHide hides it, which it does until it has looked for its menu.
        SetValue(Activatable.IsEnabledProperty, true, BindingPriority.Style);
        SetValue(FocusVisible.IsEnabledProperty, true, BindingPriority.Style);
        Bind(IsVisibleProperty, _shown, BindingPriority.Style);
        UpdateHidden();

        // The native button: a click, Enter and Space.
        _ = new NativeButton(this, _ => OnClick(), static () => true, static () => { });
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool AutoHide
    {
        get => GetValue(AutoHideProperty);
        set => SetValue(AutoHideProperty, value);
    }

    public string? Menu
    {
        get => GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Ionic's <c>onClick</c>: <c>menuController.toggle(menu)</c>.</summary>
    private void OnClick()
    {
        if (IsEffectivelyEnabled)
        {
            MenuController.ToggleFromClick(Menu, this);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _icon = e.NameScope.Find<AvonicIcon>("PART_Icon");
        Render();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        MenuController.MenuChanged += OnMenuChanged;

        // componentDidLoad: visibilityChanged.
        VisibilityChanged();
        Render();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        MenuController.MenuChanged -= OnMenuChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Ionic renders again when any of its props changes: autoHide hides it or not, and the config and hostContext are read.
        if (change.Property == AutoHideProperty || change.Property == ColorProperty || change.Property == MenuProperty
            || change.Property == IsEnabledProperty)
        {
            UpdateHidden();
            if (_attached)
            {
                Render();
            }
        }
    }

    private void OnMenuChanged(object? sender, EventArgs e) => VisibilityChanged();

    /// <summary>Ionic's <c>visibilityChanged</c>: <c>updateVisibility(menu)</c>, its menu found and active.</summary>
    private void VisibilityChanged()
    {
        _visible = MenuController.Get(Menu) is { } menu && menu.IsActive();
        UpdateHidden();
    }

    /// <summary><c>menu-button-hidden</c>: <c>display: none</c> and <c>aria-hidden</c>, while <see cref="AutoHide"/> and not visible.</summary>
    private void UpdateHidden() => _shown.SetWhen(AutoHide && !_visible, false);

    /// <summary>
    /// <c>render()</c>: the icon is <c>config.get('menuIcon', menuSharp)</c>, and <c>in-toolbar</c> and <c>in-toolbar-color</c> are
    /// <c>hostContext</c>: any ancestor toolbar, and any with a colour.
    /// </summary>
    private void Render()
    {
        if (_icon is not null)
        {
            _icon.IconName = AvonicConfig.MenuIcon ?? MdMenuIcon;
        }

        ToolbarHostContext.Set(PseudoClasses, this);
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new MenuButtonPeer(this);

    /// <summary>The native button: named by <c>aria-label</c>, the app's or <c>menu</c>; disabled with the host.</summary>
    private sealed class MenuButtonPeer(AvonicMenuButton owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : DefaultLabel;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public void Invoke() => owner.OnClick();
    }
}
