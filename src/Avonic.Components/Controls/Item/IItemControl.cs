namespace Avonic.Components.Controls;

/// <summary>
/// What a control is, for <see cref="AvonicItem"/>. Ionic's item finds these by tag name, and keeps its
/// lists of which tags do what. These are those tags.
/// </summary>
public enum ItemControlType
{
    Button,
    Checkbox,
    Datetime,
    Input,
    Radio,
    Range,
    Searchbar,
    Segment,
    Select,
    Textarea,
    Toggle,
}

/// <summary>
/// Implemented by a control that an <see cref="AvonicItem"/> around it needs to know about, so the
/// item doesn't need to reference it. Avalonia's own <c>Button</c> counts as
/// <see cref="ItemControlType.Button"/> without this.
/// </summary>
public interface IItemControl
{
    ItemControlType ItemControlType { get; }

    /// <summary>
    /// A tap landed on the item's padding, and this is the first interactive control in it. Ionic calls
    /// <c>click()</c> here, and <c>setFocus()</c> first for an input or textarea.
    /// </summary>
    void OnItemTap();
}
