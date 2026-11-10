using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.LogicalTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-tab</c>: one page of an <see cref="AvonicTabs"/>, hidden until it's selected. A hidden tab keeps its content. See
/// <c>docs/components/ion-tabs/README.md</c>.
/// </summary>
public class AvonicTab : ContentControl
{
    /// <summary>
    /// Ionic <c>tab</c>: the id that <see cref="AvonicTabs.Select(string)"/>, an <see cref="AvonicTabButton"/> and a bar's
    /// <see cref="AvonicTabBar.SelectedTab"/> name it by.
    /// </summary>
    public static readonly StyledProperty<string?> TabProperty =
        AvaloniaProperty.Register<AvonicTab, string?>(nameof(Tab));

    /// <summary>
    /// Ionic <c>component</c>: what the tab shows, built the first time it's shown, which is Ionic's lazy loading. A
    /// <see cref="Control"/>, or a <see cref="System.Type"/> of one with a parameterless constructor.
    /// </summary>
    public static readonly StyledProperty<object?> ComponentProperty =
        AvaloniaProperty.Register<AvonicTab, object?>(nameof(Component));

    /// <summary>Ionic's internal <c>active</c>: shown.</summary>
    public static readonly DirectProperty<AvonicTab, bool> IsActiveProperty =
        AvaloniaProperty.RegisterDirect<AvonicTab, bool>(nameof(IsActive), tab => tab.IsActive);

    private bool _isActive;
    private bool _loaded;
    private bool _checkedComponent;
    private IDisposable? _hidden;

    static AvonicTab()
    {
        ComponentProperty.Changed.AddClassHandler<AvonicTab>((tab, _) => tab.UpdatePageClass());
    }

    public AvonicTab()
    {
        UpdateHidden();
        UpdatePageClass();
    }

    public string? Tab
    {
        get => GetValue(TabProperty);
        set => SetValue(TabProperty, value);
    }

    public object? Component
    {
        get => GetValue(ComponentProperty);
        set => SetValue(ComponentProperty, value);
    }

    public bool IsActive
    {
        get => _isActive;
        internal set
        {
            if (SetAndRaise(IsActiveProperty, ref _isActive, value))
            {
                // changeActive: the component is built when the tab becomes active.
                if (value)
                {
                    PrepareLazyLoaded();
                }

                UpdateHidden();
            }
        }
    }

    /// <summary>Ionic <c>setActive()</c>: builds the component and shows the tab. It hides no other tab, as Ionic's doesn't.</summary>
    public void SetActive()
    {
        PrepareLazyLoaded();
        IsActive = true;
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        CheckComponent();
    }

    /// <summary>
    /// <c>componentWillLoad</c>, in a dev build: a component and inline content can't be used together. Before the component is
    /// first built too, which can come before the tab joins the tree.
    /// </summary>
    private void CheckComponent()
    {
        if (_checkedComponent)
        {
            return;
        }

        _checkedComponent = true;
        if (Component is not null && Content is not null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(
                this,
                "[ion-tab] - You can not use a lazy-loaded component in a tab and inlined content at the same time. " +
                "- Remove the component attribute in: <ion-tab component=\"{Component}\"> or " +
                "- Remove the embedded content inside the ion-tab: <ion-tab></ion-tab>",
                Component);
        }
    }

    /// <summary>
    /// <c>prepareLazyLoaded</c>: the component is built once, the first time, and given class <c>ion-page</c>, as
    /// <c>attachComponent</c> gives it. A tab with content keeps it, as a <see cref="ContentControl"/> holds one, and the component
    /// isn't built, as it would never show.
    /// </summary>
    private void PrepareLazyLoaded()
    {
        CheckComponent();
        if (_loaded || Component is null || Content is not null)
        {
            return;
        }

        _loaded = true;
        try
        {
            var control = Component switch
            {
                Control instance => instance,
                Type type => (Control)Activator.CreateInstance(type)!,
                _ => throw new InvalidOperationException($"A component is a Control or a Type of one, not {Component.GetType()}."),
            };

            control.Classes.Add("ion-page");
            SetCurrentValue(ContentProperty, control);
        }
        catch (Exception exception)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "[ion-tab] - Exception in prepareLazyLoaded: {Exception}", exception);
        }
    }

    /// <summary>
    /// <c>:host(.tab-hidden) { display: none !important }</c>, at style priority, so that it survives a replaced theme. Hidden, it's
    /// also out of the automation tree, as <c>aria-hidden</c> takes it out.
    /// </summary>
    private void UpdateHidden()
    {
        if (!_isActive && _hidden is null)
        {
            _hidden = SetValue(IsVisibleProperty, false, BindingPriority.Style);
        }
        else if (_isActive && _hidden is not null)
        {
            _hidden.Dispose();
            _hidden = null;
        }
    }

    /// <summary><c>'ion-page': component === undefined</c>, so that an <see cref="AvonicContent"/> in it takes it as its page.</summary>
    private void UpdatePageClass() => Classes.Set("ion-page", Component is null);

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new TabPeer(this);

    /// <summary>
    /// <c>role="tabpanel"</c>, <c>aria-labelledby="tab-button-{tab}"</c>: named by the tab button with the same id, in the same
    /// tabs.
    /// </summary>
    private sealed class TabPeer(AvonicTab owner) : ControlAutomationPeer(owner)
    {
        private AvonicTabButton? Button => owner.Tab is { } id
            ? owner.FindLogicalAncestorOfType<AvonicTabs>()?.TabButtons().FirstOrDefault(button => button.Tab == id)
            : null;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        /// <summary>An app's own name, then <c>aria-labelledby</c>, which beats the content's text, as it does in a browser.</summary>
        protected override string? GetNameCore() =>
            AutomationProperties.GetName(owner) is { Length: > 0 } own ? own
            : Button is { } button ? CreatePeerForElement(button).GetName()
            : base.GetNameCore();

        protected override AutomationPeer? GetLabeledByCore() =>
            base.GetLabeledByCore() ?? (Button is { } button ? CreatePeerForElement(button) : null);

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
