using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-tab-bar</c>: a row of <see cref="AvonicTabButton"/>s, usually in an <see cref="AvonicTabs"/>' bottom slot. Its
/// <see cref="SelectedTab"/> marks the button with that <see cref="AvonicTabButton.Tab"/>. Its buttons are
/// <see cref="ItemsControl.Items"/>, as <c>core.json</c> lists no slot. See <c>docs/components/ion-tabs/README.md</c>.
/// </summary>
[PseudoClasses(":translucent", ":slot-top")]
public class AvonicTabBar : ItemsControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicTabBar>();

    /// <summary>Ionic <c>selectedTab</c>: the <see cref="AvonicTabButton.Tab"/> of the selected button. An <see cref="AvonicTabs"/> writes it.</summary>
    public static readonly StyledProperty<string?> SelectedTabProperty =
        AvaloniaProperty.Register<AvonicTabBar, string?>(nameof(SelectedTab));

    /// <summary>Ionic <c>translucent</c>. In md, the buttons' background is transparent; the blur is iOS only.</summary>
    public static readonly StyledProperty<bool> TranslucentProperty =
        AvaloniaProperty.Register<AvonicTabBar, bool>(nameof(Translucent));

    private bool _didLoad;

    static AvonicTabBar()
    {
        SelectedTabProperty.Changed.AddClassHandler<AvonicTabBar>((bar, _) => bar.SelectedTabChanged());
        TranslucentProperty.Changed.AddClassHandler<AvonicTabBar>((bar, _) => bar.UpdateClasses());
        ColorProperty.Changed.AddClassHandler<AvonicTabBar>((bar, _) => bar.StyleChanged?.Invoke());
    }

    public AvonicTabBar() => UpdateClasses();

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string? SelectedTab
    {
        get => GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public bool Translucent
    {
        get => GetValue(TranslucentProperty);
        set => SetValue(TranslucentProperty, value);
    }

    /// <summary>Its <see cref="Color"/> or <see cref="Translucent"/> changed, which its buttons' <c>::slotted</c> rules read.</summary>
    internal event Action? StyleChanged;

    /// <summary>A control is its own container, so that its logical parent is the bar, as a slotted child's host is.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    // ── Selection ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>componentDidLoad</c>, after the first layout, when the buttons an <c>ItemsSource</c> makes are there: the selected tab
    /// is told to them. Once, as Stencil calls it once.
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (!_didLoad)
        {
            _didLoad = true;
            TabBarChanged();
        }
    }

    /// <summary><c>selectedTabChanged</c>: from the load on, each change is told to the buttons, and a change to null isn't.</summary>
    private void SelectedTabChanged()
    {
        if (_didLoad)
        {
            TabBarChanged();
        }
    }

    /// <summary>
    /// <c>ionTabBarChanged</c>, which each tab button in it hears (<c>onTabBarChanged</c>): it's selected when its tab is this
    /// bar's, and not otherwise. Ionic writes its <c>selected</c> prop.
    /// </summary>
    private void TabBarChanged()
    {
        if (SelectedTab is not { } tab)
        {
            return;
        }

        foreach (var button in this.GetLogicalDescendants().OfType<AvonicTabButton>())
        {
            button.SetCurrentValue(AvonicTabButton.SelectedProperty, button.Tab == tab);
        }
    }

    // ── Classes ──────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateClasses();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        UpdateClasses();
    }

    /// <summary><c>tab-bar-translucent</c>, and <c>[slot=top]</c>, which here is being the <see cref="AvonicTabs.TopContent"/> itself.</summary>
    private void UpdateClasses()
    {
        PseudoClasses.Set(":translucent", Translucent);
        PseudoClasses.Set(":slot-top", ((ILogical)this).IsAttachedToLogicalTree
            && this.FindLogicalAncestorOfType<AvonicTabs>() is { } tabs && ReferenceEquals(tabs.TopContent, this));
        StyleChanged?.Invoke();
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new TabBarPeer(this);

    /// <summary><c>role="tablist"</c>.</summary>
    private sealed class TabBarPeer(AvonicTabBar owner) : ItemsControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;
    }
}
