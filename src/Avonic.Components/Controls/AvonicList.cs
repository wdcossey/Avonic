namespace Avonic.Components.Controls;

/// <summary>
/// A container for <see cref="AvonicItem"/> elements. Supports full-bleed and
/// inset (rounded, margined) variants.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-list</c>
/// </remarks>
[PseudoClasses(":inset")]
public class AvonicList : ItemsControl
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<bool> InsetProperty =
        AvaloniaProperty.Register<AvonicList, bool>(nameof(Inset));

    public static readonly StyledProperty<ItemLines> LinesProperty =
        AvaloniaProperty.Register<AvonicList, ItemLines>(
            nameof(Lines),
            defaultValue: ItemLines.Full);

    /// <summary>
    /// When <see langword="true"/>, the list renders with horizontal margin and
    /// rounded corners (the iOS "inset grouped" style).
    /// </summary>
    public bool Inset
    {
        get => GetValue(InsetProperty);
        set => SetValue(InsetProperty, value);
    }

    /// <summary>
    /// Sets the bottom border style applied to all child <see cref="AvonicItem"/>
    /// elements that have not explicitly set their own <see cref="AvonicItem.Lines"/>.
    /// </summary>
    public ItemLines Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == InsetProperty)
            PseudoClasses.Set(":inset", change.GetNewValue<bool>());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PseudoClasses.Set(":inset", Inset);

        Items.CollectionChanged += (_, _) => UpdateItemBorders();
        UpdateItemBorders();
    }

    // ── Item Border Management ────────────────────────────────────────────────

    /// <summary>
    /// Ensures the last <see cref="AvonicItem"/> in an inset list has no bottom
    /// border, and propagates the list's <see cref="Lines"/> to all items that
    /// haven't explicitly overridden it.
    /// </summary>
    private void UpdateItemBorders()
    {
        var items = Items.OfType<AvonicItem>().ToList();
        if (items.Count == 0) return;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var isLast = i == items.Count - 1;

            item.Lines = (Inset && isLast) ? ItemLines.None : Lines;
        }
    }
}
