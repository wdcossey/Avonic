using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-segment-button</c>: one tab of an <see cref="AvonicSegment"/>, checked when its <see cref="Value"/> is the
/// segment's, with an indicator under it. Its children, an <see cref="AvonicLabel"/> and an <see cref="AvonicIcon"/>, are
/// <see cref="ItemsControl.Items"/>, as <c>core.json</c> lists no slot, laid out by <see cref="Layout"/>. See
/// <c>docs/components/ion-segment/README.md</c>.
/// </summary>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_Indicator", typeof(Control))]
[TemplatePart("PART_IndicatorBackground", typeof(Border))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(
    ":checked", ":after-checked", ":segment-button-activated", ":in-segment", ":in-segment-color", ":in-segment-scrollable",
    ":in-toolbar", ":in-toolbar-color", ":has-label", ":has-icon", ":has-label-only", ":has-icon-only", ":layout-icon-top",
    ":layout-icon-bottom", ":layout-icon-start", ":layout-icon-end", ":layout-icon-hide", ":layout-label-hide")]
public class AvonicSegmentButton : ItemsControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic <c>contentId</c>: the <c>Name</c> of the <see cref="AvonicSegmentContent"/> it shows, as Ionic's is that content's
    /// <c>id</c>. A button with one can't be disabled.
    /// </summary>
    public static readonly StyledProperty<string?> ContentIdProperty =
        AvaloniaProperty.Register<AvonicSegmentButton, string?>(nameof(ContentId));

    /// <summary>Ionic <c>layout</c>: where the icon goes, or what's hidden. <see cref="SegmentButtonLayout.IconTop"/> by default.</summary>
    public static readonly StyledProperty<SegmentButtonLayout> LayoutProperty =
        AvaloniaProperty.Register<AvonicSegmentButton, SegmentButtonLayout>(nameof(Layout));

    /// <summary>Ionic <c>value</c>: a unique <c>ion-sb-N</c> until it's set, as Ionic's field initialiser gives.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicSegmentButton, object?>(nameof(Value));

    // ── For the theme ────────────────────────────────────────────────────────────────────────

    /// <summary>A template for strings, in upper case, as <c>text-transform: uppercase</c> shows them. It matches nothing else.</summary>
    public static readonly IDataTemplate UppercaseText =
        new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text.ToUpperInvariant() });

    /// <summary>
    /// <see cref="UppercaseText"/> for a label's <c>Content</c> when it's a string, and nothing otherwise, because a
    /// <c>ContentPresenter</c> with a template builds from it even for a control.
    /// </summary>
    public static readonly IValueConverter UppercaseTextForContent =
        new FuncValueConverter<object?, IDataTemplate?>(content => content is string ? UppercaseText : null);

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private static int s_ids;
    private readonly NativeButton _native;
    private AvonicSegment? _segment;

    static AvonicSegmentButton()
    {
        // A native <button> takes focus, and each is a tab stop.
        FocusableProperty.OverrideDefaultValue<AvonicSegmentButton>(true);

        LayoutProperty.Changed.AddClassHandler<AvonicSegmentButton>((button, _) => button.UpdateClasses());
        ValueProperty.Changed.AddClassHandler<AvonicSegmentButton>((button, _) => button.UpdateState());
    }

    public AvonicSegmentButton()
    {
        Value = "ion-sb-" + s_ids++;

        // ion-activatable, ion-activatable-instant and ion-focusable, always.
        Activatable.SetIsEnabled(this, true);
        Activatable.SetIsInstant(this, true);
        FocusVisible.SetIsEnabled(this, true);

        // The native button's click, which the segment hears as its own. No keys: the segment's onKeyDown takes Enter and Space,
        // and prevents the button's.
        _native = new NativeButton(this, _ => OnNativeClick(), () => false, UpdateIsEffectivelyEnabled);

        Items.CollectionChanged += (_, _) => UpdateClasses();
    }

    public string? ContentId
    {
        get => GetValue(ContentIdProperty);
        set => SetValue(ContentIdProperty, value);
    }

    public SegmentButtonLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Ionic's <c>checked</c> state: its value is its segment's.</summary>
    internal bool IsChecked => PseudoClasses.Contains(":checked");

    /// <summary>The <c>.segment-button-indicator</c>, which the segment slides.</summary>
    internal Control? Indicator { get; private set; }

    /// <summary>The indicator's transition, which the segment starts.</summary>
    internal IndicatorSlide? Slide { get; private set; }

    protected override bool IsEnabledCore => base.IsEnabledCore && _native?.CanExecute != false;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Slide?.Stop();
        Indicator = e.NameScope.Find<Control>("PART_Indicator");
        Slide = Indicator is null ? null : new IndicatorSlide(Indicator);
    }

    // ── Children ─────────────────────────────────────────────────────────────────────────────

    /// <summary>A control is its own container, so that its logical parent is the button, as a slotted child's host is.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    // ── The segment ──────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // connectedCallback: closest('ion-segment'), and its ionSelect and ionStyle.
        _segment = this.FindLogicalAncestorOfType<AvonicSegment>();
        if (_segment is not null)
        {
            _segment.Select += UpdateState;
            _segment.StyleChanged += UpdateClasses;
        }

        // Prevent buttons from being disabled when associated with segment content.
        if (ContentId is not null && !IsEnabled)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                this, "[ion-segment-button] - Segment buttons cannot be disabled when associated with an <ion-segment-content>.");
            SetCurrentValue(IsEnabledProperty, true);
        }

        UpdateState();
        UpdateClasses();
    }

    /// <summary>hostContext reads the visual ancestors, which a toolbar's slot holds the segment among.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateClasses();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (_segment is not null)
        {
            _segment.Select -= UpdateState;
            _segment.StyleChanged -= UpdateClasses;
            _segment = null;
        }

        base.OnDetachedFromLogicalTree(e);
    }

    /// <summary>
    /// <c>updateState</c>: checked when its value is its segment's. Ionic also sets its own <c>disabled</c> when the segment is
    /// disabled; here a disabled segment disables it as any ancestor does.
    /// </summary>
    private void UpdateState()
    {
        var value = _segment?.Value;
        PseudoClasses.Set(":checked", value is not null && Equals(value, Value));
    }

    /// <summary>The classes Ionic's <c>render</c> works out.</summary>
    internal void UpdateClasses()
    {
        PseudoClasses.Set(":in-segment", _segment is not null);
        PseudoClasses.Set(":in-segment-color", _segment?.Color is not null);
        // .segment-scrollable ::slotted(ion-segment-button) { min-width: auto }, as a class the theme reads.
        PseudoClasses.Set(":in-segment-scrollable", _segment?.Scrollable == true);

        // hostContext('ion-toolbar'), hostContext('ion-toolbar[color]').
        bool inToolbar = false, inToolbarColor = false;
        foreach (var toolbar in this.GetVisualAncestors().OfType<AvonicToolbar>())
        {
            inToolbar = true;
            inToolbarColor |= toolbar.Color is not null;
        }

        PseudoClasses.Set(":in-toolbar", inToolbar);
        PseudoClasses.Set(":in-toolbar-color", inToolbarColor);

        // hasLabel, hasIcon: querySelector, anywhere inside.
        var descendants = this.GetLogicalDescendants().ToList();
        var hasLabel = descendants.OfType<AvonicLabel>().Any();
        var hasIcon = descendants.OfType<AvonicIcon>().Any();
        PseudoClasses.Set(":has-label", hasLabel);
        PseudoClasses.Set(":has-icon", hasIcon);
        PseudoClasses.Set(":has-label-only", hasLabel && !hasIcon);
        PseudoClasses.Set(":has-icon-only", hasIcon && !hasLabel);

        PseudoClasses.Set(":layout-icon-top", Layout == SegmentButtonLayout.IconTop);
        PseudoClasses.Set(":layout-icon-bottom", Layout == SegmentButtonLayout.IconBottom);
        PseudoClasses.Set(":layout-icon-start", Layout == SegmentButtonLayout.IconStart);
        PseudoClasses.Set(":layout-icon-end", Layout == SegmentButtonLayout.IconEnd);
        PseudoClasses.Set(":layout-icon-hide", Layout == SegmentButtonLayout.IconHide);
        PseudoClasses.Set(":layout-label-hide", Layout == SegmentButtonLayout.LabelHide);
    }

    private void OnNativeClick()
    {
        if (IsEffectivelyEnabled)
        {
            _segment?.OnButtonClick(this);
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new SegmentButtonPeer(this);

    /// <summary><c>role="tab"</c> and <c>aria-selected</c>, named by its text.</summary>
    private sealed class SegmentButtonPeer(AvonicSegmentButton owner) : ControlAutomationPeer(owner), ISelectionItemProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : AccessibleText.Of(owner);

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public bool IsSelected => owner.IsChecked;

        public ISelectionProvider? SelectionContainer =>
            owner._segment is { } segment ? CreatePeerForElement(segment) as ISelectionProvider : null;

        public void Select() => owner.OnNativeClick();

        public void AddToSelection() => Select();

        public void RemoveFromSelection()
        {
        }
    }
}
