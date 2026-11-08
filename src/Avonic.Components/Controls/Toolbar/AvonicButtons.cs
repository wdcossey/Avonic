using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-buttons</c>: a row of buttons in a toolbar's slot. A button in it with no <c>Fill</c> is clear, and its size
/// and colours follow the toolbar, through the theme's <c>Styles/Toolbar.axaml</c>. It holds many children and Ionic lists no
/// slot, so it's an <see cref="ItemsControl"/>.
/// </summary>
[PseudoClasses(":buttons-first-slot", ":buttons-last-slot")]
public class AvonicButtons : ItemsControl
{
    // A TemplatedControl clips by default, and ion-buttons doesn't.
    static AvonicButtons() => ClipToBoundsProperty.OverrideDefaultValue<AvonicButtons>(false);

    /// <summary>The classes <c>ion-toolbar</c> adds in <c>componentWillLoad</c>.</summary>
    internal void SetSlotClass(string pseudoClass, bool value) => PseudoClasses.Set(pseudoClass, value);
}
