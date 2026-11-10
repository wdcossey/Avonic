using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-tabs</c>, in its standalone mode, without a router: it shows one of its <see cref="AvonicTab"/>s at a time, selects
/// the first at load, and selects a tab button's tab when it's clicked. Its tabs are the <see cref="AvonicTab"/>s anywhere in its
/// slots, as <c>querySelectorAll('ion-tab')</c> finds them; a slot holds one child, so several go in a <see cref="Panel"/>, which
/// overlaps them as <c>.ion-page</c>'s <c>position: absolute</c> does. See <c>docs/components/ion-tabs/README.md</c>.
/// </summary>
[TemplatePart("PART_Top", typeof(ContentPresenter))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_Bottom", typeof(ContentPresenter))]
public class AvonicTabs : TemplatedControl
{
    // ── Slots ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's default slot, <c>.tabs-inner</c>, between the named ones and taking the rest of the height.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicTabs, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicTabs, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Ionic's <c>top</c> slot. An <see cref="AvonicTabBar"/> here is Ionic's <c>[slot=top]</c>.</summary>
    public static readonly StyledProperty<object?> TopContentProperty =
        AvaloniaProperty.Register<AvonicTabs, object?>(nameof(TopContent));

    public static readonly StyledProperty<IDataTemplate?> TopContentTemplateProperty =
        AvaloniaProperty.Register<AvonicTabs, IDataTemplate?>(nameof(TopContentTemplate));

    /// <summary>Ionic's <c>bottom</c> slot, where a tab bar usually goes.</summary>
    public static readonly StyledProperty<object?> BottomContentProperty =
        AvaloniaProperty.Register<AvonicTabs, object?>(nameof(BottomContent));

    public static readonly StyledProperty<IDataTemplate?> BottomContentTemplateProperty =
        AvaloniaProperty.Register<AvonicTabs, IDataTemplate?>(nameof(BottomContentTemplate));

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionTabsWillChange</c>: a tab is about to be selected, and nothing has changed yet. It doesn't bubble.</summary>
    public static readonly RoutedEvent<TabsChangeEventArgs> IonTabsWillChangeEvent =
        RoutedEvent.Register<AvonicTabs, TabsChangeEventArgs>(nameof(IonTabsWillChange), RoutingStrategies.Direct);

    /// <summary>
    /// Ionic <c>ionTabsDidChange</c>: a tab was selected; it shows, the last one is hidden, and the tab bar is marked. It doesn't
    /// bubble.
    /// </summary>
    public static readonly RoutedEvent<TabsChangeEventArgs> IonTabsDidChangeEvent =
        RoutedEvent.Register<AvonicTabs, TabsChangeEventArgs>(nameof(IonTabsDidChange), RoutingStrategies.Direct);

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private AvonicTab? _selectedTab;
    private AvonicTab? _leavingTab;
    private bool _transitioning;
    private bool _willLoad;

    public AvonicTabs()
    {
        // onIonTabButtonClick on the host: a tab button's click, from anywhere in it.
        AddHandler(AvonicTabButton.IonTabButtonClickEvent, OnTabClicked);
    }

    [Avalonia.Metadata.Content]
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

    public object? TopContent
    {
        get => GetValue(TopContentProperty);
        set => SetValue(TopContentProperty, value);
    }

    public IDataTemplate? TopContentTemplate
    {
        get => GetValue(TopContentTemplateProperty);
        set => SetValue(TopContentTemplateProperty, value);
    }

    public object? BottomContent
    {
        get => GetValue(BottomContentProperty);
        set => SetValue(BottomContentProperty, value);
    }

    public IDataTemplate? BottomContentTemplate
    {
        get => GetValue(BottomContentTemplateProperty);
        set => SetValue(BottomContentTemplateProperty, value);
    }

    public event EventHandler<TabsChangeEventArgs>? IonTabsWillChange
    {
        add => AddHandler(IonTabsWillChangeEvent, value);
        remove => RemoveHandler(IonTabsWillChangeEvent, value);
    }

    public event EventHandler<TabsChangeEventArgs>? IonTabsDidChange
    {
        add => AddHandler(IonTabsDidChangeEvent, value);
        remove => RemoveHandler(IonTabsDidChangeEvent, value);
    }

    // ── Methods ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic <c>select(tab)</c>, by its <see cref="AvonicTab.Tab"/>. False, changing nothing, if there's no such tab, it's already
    /// selected, or a selection is under way, which is only so from one of its event handlers here.
    /// </summary>
    public bool Select(string tab) => SelectTab(FindTab(tab));

    /// <summary>Ionic <c>select(tab)</c>, by reference.</summary>
    public bool Select(AvonicTab tab) => SelectTab(tab);

    /// <summary>Ionic <c>getTab(tab)</c>: the tab with that id, or null.</summary>
    public AvonicTab? GetTab(string tab) => FindTab(tab);

    /// <summary>Ionic <c>getTab(tab)</c> with an element, which it gives back as it is.</summary>
    public AvonicTab? GetTab(AvonicTab tab) => tab;

    /// <summary>Ionic <c>getSelected()</c>: the selected tab's id, or null.</summary>
    public string? GetSelected() => _selectedTab?.Tab;

    // ── Load ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>componentWillLoad</c>: without a router, the first tab is selected, before the first layout, so that no other shows.
    /// Once, as Stencil calls it once. On the visual tree, which a window's own content joins only when it's shown, after its
    /// XAML has filled the slots.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_willLoad)
        {
            _willLoad = true;
            if (Tabs().FirstOrDefault() is { } first)
            {
                SelectTab(first);
            }
        }
    }

    // ── The tabs and the bar ─────────────────────────────────────────────────────────────────

    /// <summary>Its slots' content, in the order the slots are in.</summary>
    private IEnumerable<ILogical> Slotted() =>
        new[] { TopContent, Content, BottomContent }.OfType<ILogical>();

    /// <summary><c>querySelectorAll('ion-tab')</c>: every <see cref="AvonicTab"/> in it, at any depth.</summary>
    internal IEnumerable<AvonicTab> Tabs() => Slotted().SelectMany(root => root.GetSelfAndLogicalDescendants()).OfType<AvonicTab>();

    /// <summary>Every <see cref="AvonicTabButton"/> in it, for a tab's automation name.</summary>
    internal IEnumerable<AvonicTabButton> TabButtons() =>
        Slotted().SelectMany(root => root.GetSelfAndLogicalDescendants()).OfType<AvonicTabButton>();

    /// <summary><c>getTab</c> by id, which logs Ionic's error when there's none.</summary>
    private AvonicTab? FindTab(string tab)
    {
        var found = Tabs().FirstOrDefault(t => t.Tab == tab);
        if (found is null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "[ion-tabs] - Tab with id: \"{Tab}\" does not exist", tab);
        }

        return found;
    }

    /// <summary><c>updateTabBar</c>: the first tab bar in it gets the selected tab's id, if it has one and it differs.</summary>
    private void UpdateTabBar()
    {
        var bar = Slotted().SelectMany(root => root.GetSelfAndLogicalDescendants()).OfType<AvonicTabBar>().FirstOrDefault();
        if (bar is null || _selectedTab?.Tab is not { } tab || bar.SelectedTab == tab)
        {
            return;
        }

        bar.SetCurrentValue(AvonicTabBar.SelectedTabProperty, tab);
    }

    // ── Selecting ────────────────────────────────────────────────────────────────────────────

    /// <summary><c>onTabClicked</c>: without a router, the button's tab is selected.</summary>
    private void OnTabClicked(object? sender, TabButtonClickEventArgs e) => Select(e.Tab);

    /// <summary><c>select</c>: <c>shouldSwitch</c>, <c>setActive</c>, then <c>tabSwitch</c>. With no router to notify, it's synchronous.</summary>
    private bool SelectTab(AvonicTab? tab)
    {
        if (tab is null || tab == _selectedTab || _transitioning)
        {
            return false;
        }

        SetActive(tab);
        TabSwitch();
        return true;
    }

    /// <summary><c>setActive</c>: the event, with nothing changed yet; then the new tab shows, and the bar is told.</summary>
    private void SetActive(AvonicTab tab)
    {
        _transitioning = true;
        try
        {
            _leavingTab = _selectedTab;
            _selectedTab = tab;
            RaiseEvent(new TabsChangeEventArgs(IonTabsWillChangeEvent, tab.Tab));
            tab.IsActive = true;
            UpdateTabBar();
        }
        catch
        {
            _transitioning = false;
            throw;
        }
    }

    /// <summary><c>tabSwitch</c>: the last tab hides, and the event.</summary>
    private void TabSwitch()
    {
        var selected = _selectedTab;
        var leaving = _leavingTab;
        _leavingTab = null;
        _transitioning = false;
        if (selected is null || leaving == selected)
        {
            return;
        }

        if (leaving is not null)
        {
            leaving.IsActive = false;
        }

        RaiseEvent(new TabsChangeEventArgs(IonTabsDidChangeEvent, selected.Tab));
    }
}
