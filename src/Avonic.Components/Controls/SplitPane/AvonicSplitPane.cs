using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Data;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-split-pane</c>: it fills its parent with its <see cref="ItemsControl.Items"/>, a side <see cref="AvonicMenu"/> and
/// the main content (the child named <see cref="ContentId"/>), and while <see cref="When"/> matches the window it shows the menu
/// beside the main content, where the menu can't open or close; otherwise the menu is an ordinary one. The menu does most of it, as
/// in Ionic: it reads its split pane, and <see cref="SplitPanePanel"/> lays out the row. See
/// <c>docs/components/ion-split-pane/README.md</c>.
/// </summary>
[TemplatePart("PART_Borders", typeof(SplitPaneBorders))]
[PseudoClasses(SplitPaneVisible)]
public class AvonicSplitPane : ItemsControl
{
    /// <summary><c>split-pane-visible</c>, on the host while the side pane shows.</summary>
    public const string SplitPaneVisible = ":split-pane-visible";

    /// <summary><c>split-pane-main</c>, on the main content: the child named <see cref="ContentId"/>.</summary>
    public const string SplitPaneMain = ":split-pane-main";

    /// <summary>Ionic's <c>QUERY</c>: the shortcuts, and the index of each one's breakpoint in <see cref="GridViewport"/>.</summary>
    private static readonly Dictionary<string, int> Shortcuts = new(StringComparer.Ordinal)
    {
        ["xs"] = 0,
        ["sm"] = 1,
        ["md"] = 2,
        ["lg"] = 3,
        ["xl"] = 4,
    };

    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new SplitPanePanel());

    /// <summary>
    /// Ionic <c>contentId</c>: the <c>Name</c> of the main content, one of its own children, which takes the room the side pane
    /// leaves. Looked for when the split pane is attached, as Ionic's <c>styleMainElement</c> in <c>connectedCallback</c>.
    /// </summary>
    public static readonly StyledProperty<string?> ContentIdProperty =
        AvaloniaProperty.Register<AvonicSplitPane, string?>(nameof(ContentId));

    /// <summary>
    /// Ionic <c>disabled</c>: the side pane never shows. Not <see cref="InputElement.IsEnabled"/>, which would disable every control
    /// in the split pane, where Ionic's leaves them enabled.
    /// </summary>
    public static readonly StyledProperty<bool> DisabledProperty =
        AvaloniaProperty.Register<AvonicSplitPane, bool>(nameof(Disabled));

    /// <summary>
    /// Ionic <c>when</c>: a boolean, a shortcut (<c>xs</c>, <c>sm</c>, <c>md</c>, <c>lg</c>, <c>xl</c>, <c>never</c>) or a CSS media
    /// query, matched against the window's size. <c>(min-width: 992px)</c> by default.
    /// </summary>
    public static readonly StyledProperty<SplitPaneWhen> WhenProperty =
        AvaloniaProperty.Register<AvonicSplitPane, SplitPaneWhen>(nameof(When), defaultValue: SplitPaneWhen.Default);

    /// <summary>Ionic <c>ionSplitPaneVisible</c>: the side pane showed or hid.</summary>
    public static readonly RoutedEvent<SplitPaneVisibleEventArgs> IonSplitPaneVisibleEvent =
        RoutedEvent.Register<AvonicSplitPane, SplitPaneVisibleEventArgs>(nameof(IonSplitPaneVisible), RoutingStrategies.Bubble);

    private readonly List<AvonicMenu> _sides = [];
    private bool _visible;
    private bool _attached;
    private TopLevel? _topLevel;
    private GridViewport? _viewport;
    private string? _query;
    private int _breakpoint = -1;
    private SplitPaneBorders? _borders;

    // overflow: hidden on the main content while visible, at style priority: bound to each child when it's added, before it's
    // styled, so that an app's own style for it still wins, and given a value on the main only.
    private readonly Dictionary<Control, (StyleValue<bool> Clip, IDisposable Binding)> _clips = [];
    private Control? _main;

    static AvonicSplitPane()
    {
        ItemsPanelProperty.OverrideDefaultValue<AvonicSplitPane>(DefaultPanel);
    }

    public AvonicSplitPane()
    {
        Items.CollectionChanged += OnItemsChanged;
    }

    public string? ContentId
    {
        get => GetValue(ContentIdProperty);
        set => SetValue(ContentIdProperty, value);
    }

    public bool Disabled
    {
        get => GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    public SplitPaneWhen When
    {
        get => GetValue(WhenProperty);
        set => SetValue(WhenProperty, value);
    }

    public event EventHandler<SplitPaneVisibleEventArgs>? IonSplitPaneVisible
    {
        add => AddHandler(IonSplitPaneVisibleEvent, value);
        remove => RemoveHandler(IonSplitPaneVisibleEvent, value);
    }

    /// <summary>Ionic's <c>visible</c> state, which its internal <c>isVisible()</c> gives the menu.</summary>
    internal bool Visible => _visible;

    /// <summary>The menus that are its side, as each finds it with <c>getSplitPane()</c>, while they're attached.</summary>
    internal IReadOnlyList<AvonicMenu> Sides => _sides;

    /// <summary>A control is its own container, so that its logical parent is the split pane, as a slotted child's host is.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // connectedCallback. A split pane's children attach after it, so its menus find its state already worked out.
        _attached = true;
        _topLevel = TopLevel.GetTopLevel(this);
        while (_topLevel is PopupRoot { ParentTopLevel: { } parent })
        {
            _topLevel = parent;
        }

        StyleMainElement();
        UpdateState();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // disconnectedCallback: the listener goes. The state stays as it was until the next attach reads the window again.
        _attached = false;
        StopListening();
        _topLevel = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // The watchers on disabled and when.
        if (change.Property == DisabledProperty || change.Property == WhenProperty)
        {
            UpdateState();
        }
    }

    /// <summary>
    /// Ionic's <c>styleMainElement</c>: the child named <see cref="ContentId"/> gets <see cref="SplitPaneMain"/>. A second one is
    /// warned about, and none. Here a child that was the main before and isn't now loses it, as Ionic means: Ionic leaves it on, so
    /// that after <c>contentId</c> changes and the split pane is attached again, two mains share the room.
    /// </summary>
    private void StyleMainElement()
    {
        var contentId = ContentId;
        Control? main = null;
        var twice = false;
        foreach (var item in Items)
        {
            if (item is Control child && contentId is not null && child.Name == contentId)
            {
                if (main is not null)
                {
                    twice = true;
                    break;
                }

                main = child;
            }
        }

        // An old main that has left the items loses its mark too, so that a menu in it, used elsewhere, isn't taken for a menu in
        // a main content.
        if (_main is not null && !ReferenceEquals(_main, main))
        {
            ((IPseudoClasses)_main.Classes).Set(SplitPaneMain, false);
        }

        foreach (var item in Items)
        {
            if (item is Control child)
            {
                ((IPseudoClasses)child.Classes).Set(SplitPaneMain, ReferenceEquals(child, main));
            }
        }

        _main = main;
        UpdateMainClip();

        if (twice)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "[AvonicSplitPane] - Cannot have more than one main node.");
        }
        else if (main is null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "[AvonicSplitPane] - Does not have a specified main node.");
        }

        Panel?.InvalidateMeasure();
    }

    /// <summary>
    /// Ionic's <c>updateState</c>: <see cref="Disabled"/> hides it; a boolean <see cref="When"/> is the state; a shortcut or a media
    /// query is matched against the window, and followed while it's attached; an empty one hides it.
    /// </summary>
    private void UpdateState()
    {
        StopListening();
        if (Disabled)
        {
            SetVisible(false);
            return;
        }

        var when = When;
        if (when.IsBoolean)
        {
            SetVisible(when.Value);
            return;
        }

        // QUERY[query] || query: 'never' is '' there, which falls back to 'never' itself, a media type that matches nothing.
        var query = when.Query ?? string.Empty;
        if (query.Length == 0)
        {
            SetVisible(false);
            return;
        }

        if (!_attached || _topLevel is null)
        {
            return;
        }

        if (Shortcuts.TryGetValue(query, out var breakpoint))
        {
            _breakpoint = breakpoint;
            _viewport = GridViewport.Of(this);
            if (_viewport is not null)
            {
                _viewport.Changed += OnViewportChanged;
            }
        }
        else
        {
            _query = query;
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
        }

        SetVisible(Matches());
    }

    private bool Matches() =>
        _breakpoint >= 0 ? _viewport is { } viewport && viewport.Breakpoint >= _breakpoint
        : _query is not null && _topLevel is not null && MediaQuery.Matches(_query, _topLevel.ClientSize);

    private void StopListening()
    {
        if (_viewport is not null)
        {
            _viewport.Changed -= OnViewportChanged;
            _viewport = null;
        }

        if (_query is not null && _topLevel is not null)
        {
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
        }

        _query = null;
        _breakpoint = -1;
    }

    private void OnViewportChanged() => SetVisible(Matches());

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
        {
            SetVisible(Matches());
        }
    }

    /// <summary>The state, and Ionic's <c>visibleChanged</c> watcher: <see cref="IonSplitPaneVisible"/> when it changes.</summary>
    private void SetVisible(bool visible)
    {
        if (visible == _visible)
        {
            return;
        }

        _visible = visible;
        PseudoClasses.Set(SplitPaneVisible, visible);
        UpdateMainClip();
        if (Panel is { } panel)
        {
            panel.IsSplit = visible;
        }

        RaiseEvent(new SplitPaneVisibleEventArgs(IonSplitPaneVisibleEvent, visible));
    }

    /// <summary>
    /// <c>:host(.split-pane-visible) ::slotted(.split-pane-main) { overflow: hidden }</c>, from here, which costs nothing in the style
    /// budget, where a rule in <c>Styles/</c> would cost every element an alternative. Its position, <c>flex: 1</c> and
    /// <c>z-index: 0</c> are the panel's; its <c>box-shadow: none</c> is the menu's, which draws the push and reveal types' content
    /// shadow.
    /// </summary>
    private void UpdateMainClip()
    {
        foreach (var (child, (clip, _)) in _clips)
        {
            clip.SetWhen(_visible && ReferenceEquals(child, _main), true);
        }
    }

    /// <summary>Each child added gets its clip's binding, before it's styled; one removed loses it, and the main's mark.</summary>
    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var child in _clips.Keys.ToList())
        {
            if (!Items.Contains(child))
            {
                _clips[child].Binding.Dispose();
                _clips.Remove(child);
                if (ReferenceEquals(child, _main))
                {
                    ((IPseudoClasses)child.Classes).Set(SplitPaneMain, false);
                    _main = null;
                }
            }
        }

        foreach (var item in Items)
        {
            if (item is Control child && !_clips.ContainsKey(child))
            {
                var clip = new StyleValue<bool>();
                _clips[child] = (clip, child.Bind(ClipToBoundsProperty, clip, BindingPriority.Style));
            }
        }

        // The main content put back, or a new one added, while attached: marked again, as Ionic's element keeps its class when
        // it's moved.
        if (_attached && _main is null && ContentId is { } id && Items.OfType<Control>().Any(c => c.Name == id))
        {
            StyleMainElement();
        }

        UpdateMainClip();
    }

    // ── What the menu calls ──────────────────────────────────────────────────────────────────

    /// <summary>A menu that found this split pane with <c>getSplitPane()</c>, when it's attached.</summary>
    internal void AddSide(AvonicMenu menu)
    {
        if (!_sides.Contains(menu))
        {
            _sides.Add(menu);
        }

        SidesChanged();
    }

    /// <summary>The menu is detached.</summary>
    internal void RemoveSide(AvonicMenu menu)
    {
        _sides.Remove(menu);
        SidesChanged();
    }

    /// <summary>A side menu showed or hid in the pane: the row is laid out again.</summary>
    internal void SidesChanged() => Panel?.InvalidateMeasure();

    private SplitPanePanel? Panel => ItemsPanelRoot as SplitPanePanel;

    /// <summary>The panel arranged its row, or its border's colour changed: the part that draws the borders draws again.</summary>
    internal void BordersChanged() => _borders?.InvalidateVisual();

    /// <summary>The panel whose borders <see cref="SplitPaneBorders"/> draws.</summary>
    internal SplitPanePanel? PanelRoot => Panel;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _borders = e.NameScope.Find<SplitPaneBorders>("PART_Borders");
    }

    /// <summary>The panel is made after the state may have been worked out: it takes it then.</summary>
    internal void PanelAttached(SplitPanePanel panel) => panel.IsSplit = _visible;

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>No <c>role</c>: Ionic's host is a plain element.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}

/// <summary>Ionic's <c>ionSplitPaneVisible</c> detail: <c>{ visible }</c>.</summary>
public sealed class SplitPaneVisibleEventArgs : RoutedEventArgs
{
    internal SplitPaneVisibleEventArgs(RoutedEvent routedEvent, bool visible)
        : base(routedEvent)
    {
        Visible = visible;
    }

    /// <summary>Whether the side pane shows now.</summary>
    public bool Visible { get; }
}
