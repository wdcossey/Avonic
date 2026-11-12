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
    private ContentPresenter? _topPresenter;
    private ContentPresenter? _contentPresenter;
    private ContentPresenter? _bottomPresenter;

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
    /// <c>componentWillLoad</c>: without a router, the first tab is selected, before the first layout, so that no other shows. On
    /// the visual tree, which a window's own content joins only when it's shown, after its XAML has filled the slots. A tab a
    /// template or an <c>ItemsSource</c> makes is only there after a layout, so with none found, it's tried again at the load. A
    /// tab selected in code before then is kept, as Ionic runs a method called before its load after it.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        WillLoad();
    }

    /// <summary><c>componentDidLoad</c>: the bar is told again, which finds one a template made.</summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        WillLoad();
        _willLoad = true;
        UpdateTabBar();
    }

    private void WillLoad()
    {
        if (!_willLoad && _selectedTab is null && Tabs().FirstOrDefault() is { } first)
        {
            _willLoad = true;
            SelectTab(first);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _topPresenter = e.NameScope.Find<ContentPresenter>("PART_Top");
        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _bottomPresenter = e.NameScope.Find<ContentPresenter>("PART_Bottom");
    }

    // ── The tabs and the bar ─────────────────────────────────────────────────────────────────

    /// <summary>What's in a slot: its content, or what its template made of it.</summary>
    private static ILogical? InSlot(object? content, ContentPresenter? presenter) => content as ILogical ?? presenter?.Child;

    /// <summary>Its slots' content, in the order the slots are in.</summary>
    private IEnumerable<ILogical> Slotted() =>
        new[] { InSlot(TopContent, _topPresenter), InSlot(Content, _contentPresenter), InSlot(BottomContent, _bottomPresenter) }.OfType<ILogical>();

    /// <summary>
    /// The bar slots first, then the default slot. Ionic's <c>querySelector</c> takes the first in the markup, which the author
    /// orders; slots have no order here, and a bar inside a page, such as a page's own, mustn't take the main bar's place.
    /// </summary>
    private IEnumerable<ILogical> BarsFirst() =>
        new[] { InSlot(TopContent, _topPresenter), InSlot(BottomContent, _bottomPresenter), InSlot(Content, _contentPresenter) }.OfType<ILogical>();

    /// <summary>
    /// <c>tabs.querySelector(':scope > ion-tab-bar').slot === 'bottom'</c>, which <see cref="AvonicFooter"/> reads: the first
    /// <see cref="AvonicTabBar"/> directly in a bar slot, the top's first as in <see cref="BarsFirst"/>, is in the bottom one.
    /// </summary>
    internal bool HasBottomTabBar() =>
        InSlot(TopContent, _topPresenter) is not AvonicTabBar && InSlot(BottomContent, _bottomPresenter) is AvonicTabBar;

    /// <summary><c>querySelectorAll('ion-tab')</c>: every <see cref="AvonicTab"/> in it, at any depth.</summary>
    internal IEnumerable<AvonicTab> Tabs() => Slotted().SelectMany(root => root.GetSelfAndLogicalDescendants()).OfType<AvonicTab>();

    /// <summary>Every <see cref="AvonicTabButton"/> in it, for a tab's automation name, the bar slots' first.</summary>
    internal IEnumerable<AvonicTabButton> TabButtons() =>
        BarsFirst().SelectMany(root => root.GetSelfAndLogicalDescendants()).OfType<AvonicTabButton>();

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
        var bar = BarsFirst().SelectMany(root => root.GetSelfAndLogicalDescendants()).OfType<AvonicTabBar>().FirstOrDefault();
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
        var wasActive = tab.IsActive;
        _leavingTab = _selectedTab;
        _selectedTab = tab;
        try
        {
            RaiseEvent(new TabsChangeEventArgs(IonTabsWillChangeEvent, tab.Tab));
            tab.IsActive = true;
            UpdateTabBar();
        }
        catch
        {
            // An exception from a handler escapes here, where a DOM listener's goes to window.onerror and the switch goes on.
            // Undone, so that the selected tab is still the one that shows, rather than left half done.
            tab.IsActive = wasActive;
            _selectedTab = _leavingTab;
            _leavingTab = null;
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
