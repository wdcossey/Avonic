using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>SelectOverlayOption</c> (<c>SelectPopoverOption</c> and <c>SelectModalOption</c>, with the rich fields the select
/// passes): one option of an <see cref="AvonicSelectPopover"/> or an <see cref="AvonicSelectModal"/>. Plain data, as Ionic's is.
/// </summary>
public sealed class SelectOverlayOption
{
    /// <summary>Ionic <c>text</c>: the label.</summary>
    public string Text { get; set; } = "";

    /// <summary>Ionic <c>value</c>.</summary>
    public object? Value { get; set; }

    /// <summary>Ionic <c>disabled</c>.</summary>
    public bool Disabled { get; set; }

    /// <summary>Ionic <c>checked</c>. A checkbox's click sets it, as Ionic's <c>setChecked</c> does.</summary>
    public bool Checked { get; set; }

    /// <summary>Ionic <c>cssClass</c>: style classes on the option's item, space-separated.</summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Ionic <c>handler</c>: given the radio's value, or every checked value as an <c>object?[]</c> for checkboxes, when the user
    /// chooses. Its result isn't read, as <c>safeCall</c> drops it.
    /// </summary>
    public Func<object?, object?>? Handler { get; set; }

    /// <summary>Ionic <c>description</c>: a line under the text.</summary>
    public string? Description { get; set; }

    /// <summary>Ionic <c>labelPlacement</c>: null is md's <c>end</c>.</summary>
    public SelectOptionLabelPlacement? LabelPlacement { get; set; }

    /// <summary>Ionic <c>justify</c>: null is md's <c>start</c>.</summary>
    public SelectOptionJustify? Justify { get; set; }
}

/// <summary>Ionic <c>SelectChangeEventDetail</c>: the select's new value.</summary>
public sealed class SelectChangeEventArgs(RoutedEvent routedEvent, object? value) : RoutedEventArgs(routedEvent)
{
    /// <summary>Ionic <c>value</c>.</summary>
    public object? Value { get; } = value;
}
