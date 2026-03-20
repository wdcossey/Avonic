using Avonic.Components.Primitives;

namespace Avonic.Components.Controls;

/// <summary>
/// The scrollable page body. Provides a scroll container for page content with
/// optional safe area padding and a non-scrolling fixed overlay slot.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-content</c>
/// </remarks>
[PseudoClasses(":fullscreen", ":scroll-x", ":scroll-y")]
public class AvonicContent : ContentControl
{
    // ── Part Names ───────────────────────────────────────────────────────────

    public const string PartScrollViewer = "PART_ScrollViewer";
    public const string PartFixedContent = "PART_FixedContent";

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<bool> ScrollYProperty =
        AvaloniaProperty.Register<AvonicContent, bool>(nameof(ScrollY), defaultValue: true);

    public static readonly StyledProperty<bool> ScrollXProperty =
        AvaloniaProperty.Register<AvonicContent, bool>(nameof(ScrollX), defaultValue: false);

    public static readonly StyledProperty<bool> FullscreenProperty =
        AvaloniaProperty.Register<AvonicContent, bool>(nameof(Fullscreen), defaultValue: false);

    public static readonly StyledProperty<FixedSlotPlacement> FixedSlotPlacementProperty =
        AvaloniaProperty.Register<AvonicContent, FixedSlotPlacement>(
            nameof(FixedSlotPlacement),
            defaultValue: FixedSlotPlacement.After);

    // ── Fixed Slot ───────────────────────────────────────────────────────────

    public static readonly StyledProperty<object?> FixedContentProperty =
        AvaloniaProperty.Register<AvonicContent, object?>(nameof(FixedContent));

    public static readonly StyledProperty<IDataTemplate?> FixedContentTemplateProperty =
        AvaloniaProperty.Register<AvonicContent, IDataTemplate?>(nameof(FixedContentTemplate));

    // ── Safe Area (computed, updated from IInsetsManager when Fullscreen) ────

    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<AvonicContent, Thickness>(nameof(ContentPadding));

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Enables vertical scrolling (default: <see langword="true"/>).</summary>
    public bool ScrollY
    {
        get => GetValue(ScrollYProperty);
        set => SetValue(ScrollYProperty, value);
    }

    /// <summary>Enables horizontal scrolling (default: <see langword="false"/>).</summary>
    public bool ScrollX
    {
        get => GetValue(ScrollXProperty);
        set => SetValue(ScrollXProperty, value);
    }

    /// <summary>
    /// When <see langword="true"/>, content extends behind system bars and
    /// <see cref="ContentPadding"/> is automatically set from platform safe area insets.
    /// </summary>
    public bool Fullscreen
    {
        get => GetValue(FullscreenProperty);
        set => SetValue(FullscreenProperty, value);
    }

    /// <summary>Controls whether the fixed slot renders before or after scrollable content.</summary>
    public FixedSlotPlacement FixedSlotPlacement
    {
        get => GetValue(FixedSlotPlacementProperty);
        set => SetValue(FixedSlotPlacementProperty, value);
    }

    /// <summary>Content that does not scroll — overlaid on the scrollable area.</summary>
    public object? FixedContent
    {
        get => GetValue(FixedContentProperty);
        set => SetValue(FixedContentProperty, value);
    }

    public IDataTemplate? FixedContentTemplate
    {
        get => GetValue(FixedContentTemplateProperty);
        set => SetValue(FixedContentTemplateProperty, value);
    }

    /// <summary>
    /// Padding applied to the scrollable content area. Automatically reflects
    /// platform safe area insets when <see cref="Fullscreen"/> is <see langword="true"/>.
    /// Can also be set manually.
    /// </summary>
    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachSafeAreaIfFullscreen();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FullscreenProperty)
        {
            PseudoClasses.Set(":fullscreen", change.GetNewValue<bool>());
            AttachSafeAreaIfFullscreen();
        }

        if (change.Property == ScrollYProperty)
            PseudoClasses.Set(":scroll-y", change.GetNewValue<bool>());

        if (change.Property == ScrollXProperty)
            PseudoClasses.Set(":scroll-x", change.GetNewValue<bool>());

        // Sync ContentPadding when SafeAreaInsets attached properties change on this control
        if (change.Property == SafeAreaInsets.TopProperty    ||
            change.Property == SafeAreaInsets.BottomProperty ||
            change.Property == SafeAreaInsets.LeftProperty   ||
            change.Property == SafeAreaInsets.RightProperty)
        {
            ContentPadding = SafeAreaInsets.GetInsets(this);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PseudoClasses.Set(":fullscreen", Fullscreen);
        PseudoClasses.Set(":scroll-y",   ScrollY);
        PseudoClasses.Set(":scroll-x",   ScrollX);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void AttachSafeAreaIfFullscreen()
    {
        if (!Fullscreen) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
            SafeAreaInsets.Attach(topLevel, this);
    }
}
