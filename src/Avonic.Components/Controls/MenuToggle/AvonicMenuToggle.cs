using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-menu-toggle</c>: a click on anything in it toggles a menu through <see cref="MenuController"/>, as a DOM
/// <c>click</c> from its content reaches its host; and it hides itself while that menu isn't active. It has no look of its own. See
/// <c>docs/components/ion-menu/README.md</c>.
/// </summary>
public class AvonicMenuToggle : ContentControl
{
    /// <summary>Ionic <c>autoHide</c>: hidden while its menu isn't active. True.</summary>
    public static readonly StyledProperty<bool> AutoHideProperty =
        AvaloniaProperty.Register<AvonicMenuToggle, bool>(nameof(AutoHide), defaultValue: true);

    /// <summary>
    /// Ionic <c>menu</c>: the menu it toggles, by its <see cref="AvonicMenu.MenuId"/>, or <c>start</c> or <c>end</c> for the menu on
    /// that side. Null is the first active menu.
    /// </summary>
    public static readonly StyledProperty<string?> MenuProperty =
        AvaloniaProperty.Register<AvonicMenuToggle, string?>(nameof(Menu));

    private readonly StyleValue<bool> _shown = new();
    private bool _visible;

    public AvonicMenuToggle()
    {
        // menu-toggle-hidden: display: none, at style priority, before any style, so that an app's style wins. Hidden until it has
        // looked for its menu, as visible starts false.
        Bind(IsVisibleProperty, _shown, BindingPriority.Style);
        UpdateHidden();

        // The DOM click on the host, from anything inside: a press's, and a button's inside from a key, as the chip has it.
        _ = new NativeButton(this, _ => OnClick(), static () => false, static () => { }) { StockButtonsClick = true };
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        MenuController.MenuChanged += OnMenuChanged;

        // connectedCallback: visibilityChanged.
        VisibilityChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        MenuController.MenuChanged -= OnMenuChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AutoHideProperty)
        {
            UpdateHidden();
        }
    }

    private void OnMenuChanged(object? sender, EventArgs e) => VisibilityChanged();

    /// <summary>Ionic's <c>visibilityChanged</c>: <c>updateVisibility(menu)</c>, its menu found and active.</summary>
    private void VisibilityChanged()
    {
        _visible = MenuController.Get(Menu) is { } menu && menu.IsActive();
        UpdateHidden();
    }

    private void UpdateHidden() => _shown.SetWhen(AutoHide && !_visible, false);
}
