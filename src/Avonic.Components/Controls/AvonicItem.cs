using Avonic.Components.Primitives;

namespace Avonic.Components.Controls;

/// <summary>
/// The core layout primitive for list-based UI. Provides Start, Content, and End
/// slots with a minimum 48dp touch target and optional bottom border.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-item</c>
/// </remarks>
[PseudoClasses(":button", ":detail", ":lines-full", ":lines-inset", ":lines-none")]
public class AvonicItem : AvonicPressable
{
    // ── Part Names ───────────────────────────────────────────────────────────

    public const string PartStartContent = "PART_StartContent";
    public const string PartEndContent   = "PART_EndContent";

    // ── Start Slot ───────────────────────────────────────────────────────────

    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(StartContentTemplate));

    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    public IDataTemplate? StartContentTemplate
    {
        get => GetValue(StartContentTemplateProperty);
        set => SetValue(StartContentTemplateProperty, value);
    }

    // ── End Slot ─────────────────────────────────────────────────────────────

    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(EndContentTemplate));

    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    public IDataTemplate? EndContentTemplate
    {
        get => GetValue(EndContentTemplateProperty);
        set => SetValue(EndContentTemplateProperty, value);
    }

    // ── Appearance ───────────────────────────────────────────────────────────

    public static readonly StyledProperty<ItemLines> LinesProperty =
        AvaloniaProperty.Register<AvonicItem, ItemLines>(
            nameof(Lines),
            defaultValue: ItemLines.Full);

    public static readonly StyledProperty<bool> ButtonProperty =
        AvaloniaProperty.Register<AvonicItem, bool>(nameof(Button));

    public static readonly StyledProperty<bool> DetailProperty =
        AvaloniaProperty.Register<AvonicItem, bool>(nameof(Detail));

    /// <summary>How the bottom separator border is rendered.</summary>
    public ItemLines Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    /// <summary>
    /// When <see langword="true"/>, the item renders with interactive pressed/hover
    /// states and participates in keyboard navigation.
    /// </summary>
    public bool Button
    {
        get => GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    /// <summary>
    /// When <see langword="true"/>, a forward chevron is shown in the end slot.
    /// Automatically set to <see langword="true"/> when <see cref="Button"/> is set.
    /// </summary>
    public bool Detail
    {
        get => GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LinesProperty)
            UpdateLinesPseudoClasses(change.GetNewValue<ItemLines>());

        if (change.Property == ButtonProperty)
            PseudoClasses.Set(":button", change.GetNewValue<bool>());

        if (change.Property == DetailProperty)
            PseudoClasses.Set(":detail", change.GetNewValue<bool>());
    }

    private void UpdateLinesPseudoClasses(ItemLines lines)
    {
        PseudoClasses.Set(":lines-full",  lines == ItemLines.Full);
        PseudoClasses.Set(":lines-inset", lines == ItemLines.Inset);
        PseudoClasses.Set(":lines-none",  lines == ItemLines.None);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Sync initial pseudoclasses after template is applied
        UpdateLinesPseudoClasses(Lines);
        PseudoClasses.Set(":button", Button);
        PseudoClasses.Set(":detail", Detail);
    }
}
