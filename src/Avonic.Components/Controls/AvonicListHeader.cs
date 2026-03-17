using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Avonic.Components.Controls;

/// <summary>
/// A section header rendered above an <see cref="AvonicList"/>.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-list-header</c>
/// </remarks>
[PseudoClasses(":lines-full", ":lines-inset", ":lines-none")]
public class AvonicListHeader : ContentControl
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicListHeader, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicListHeader, IDataTemplate?>(nameof(EndContentTemplate));

    public static readonly StyledProperty<ItemLines?> LinesProperty =
        AvaloniaProperty.Register<AvonicListHeader, ItemLines?>(nameof(Lines));

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Optional content rendered at the end (right) of the header.</summary>
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

    /// <summary>
    /// Optional bottom border. When <see langword="null"/> (default), no border is shown.
    /// </summary>
    public ItemLines? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LinesProperty)
            UpdateLinesPseudoClasses(change.GetNewValue<ItemLines?>());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateLinesPseudoClasses(Lines);
    }

    private void UpdateLinesPseudoClasses(ItemLines? lines)
    {
        PseudoClasses.Set(":lines-full",  lines == ItemLines.Full);
        PseudoClasses.Set(":lines-inset", lines == ItemLines.Inset);
        PseudoClasses.Set(":lines-none",  lines == ItemLines.None);
    }
}
