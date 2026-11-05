using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-note</c>: a small piece of text, usually at the end of an <see cref="AvonicItem"/>. Its look is in
/// the theme's <c>Styles/Note.axaml</c>.
/// </summary>
public class AvonicNote : ContentControl
{
    /// <summary>On an item's slot while a note is in it. The theme aligns a note to the top of the row with it.</summary>
    internal const string HasNotePseudoClass = ":has-note";

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicNote>();

    private ContentPresenter? _slot;

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // A slot holds one child, so a panel directly in it stands for the slot. A slot is a row, though, so a vertical
        // stack is a group of its own, which the item centres as Ionic does a column in a <div slot="end">.
        var parent = Parent is Panel panel and not StackPanel { Orientation: Orientation.Vertical } ? panel.Parent : Parent;
        if (parent is ContentPresenter { TemplatedParent: AvonicItem } slot)
        {
            _slot = slot;
            SetHasNote(slot, true);
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);

        if (_slot is { } slot)
        {
            _slot = null;
            SetHasNote(slot, slot.Child is Panel panel && panel.Children.OfType<AvonicNote>().Any(note => ((ILogical)note).IsAttachedToLogicalTree));
        }
    }

    private static void SetHasNote(ContentPresenter slot, bool value) =>
        ((IPseudoClasses)slot.Classes).Set(HasNotePseudoClass, value);
}
