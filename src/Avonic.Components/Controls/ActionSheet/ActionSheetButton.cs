namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ActionSheetButton</c>: a button of an <see cref="AvonicActionSheet"/>. Plain data, as Ionic's is, so that code (an
/// <c>ion-select</c>) can build the sheet's buttons. A string converts to one, as Ionic's string form does: its text, and no role.
/// </summary>
public sealed class ActionSheetButton
{
    public ActionSheetButton()
    {
    }

    /// <summary>Ionic's string form, <c>{ text }</c>. Unlike the alert's, it never has the <c>cancel</c> role.</summary>
    public ActionSheetButton(string text) => Text = text;

    /// <summary>Ionic <c>text</c>.</summary>
    public string? Text { get; set; }

    /// <summary>
    /// Ionic <c>role</c>: the first <c>cancel</c> button is shown last, in a group of its own, and dismisses with no say from
    /// <see cref="Handler"/>; any role is the dismiss's, and the pseudoclass <c>:role-&lt;role&gt;</c> on the button
    /// (<c>action-sheet-&lt;role&gt;</c>); <c>selected</c> shows it selected.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>Ionic <c>icon</c>: an ionicons name, or what <see cref="AvonicIcon.Icon"/> takes, shown before the text.</summary>
    public string? Icon { get; set; }

    /// <summary>Ionic <c>cssClass</c>: style classes on the button, space-separated.</summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Ionic <c>id</c>: the button's <c>AutomationId</c>, and what identifies a radio. Ionic doesn't render it on the cancel
    /// button.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Ionic <c>htmlAttributes</c>, spread on the <c>&lt;button&gt;</c>: <c>aria-label</c> names it; <c>role</c> <c>"radio"</c>
    /// makes it a radio, with <c>aria-checked</c> <c>"true"</c> the checked one, as <c>ion-select</c> builds them; <c>disabled</c>
    /// disables the cancel button, and <c>id</c> is its <c>AutomationId</c>, as on the others Ionic's own values come after the
    /// spread. Other attributes have nothing to do in Avalonia, and are ignored.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? HtmlAttributes { get; set; }

    /// <summary>
    /// Ionic <c>handler</c>: <c>false</c> keeps the sheet open; a <see cref="Task"/> or <see cref="ValueTask"/> is awaited, and
    /// its result read the same way; anything else dismisses. A cancel button's runs when the sheet is dismissed with a cancel role.
    /// </summary>
    public Func<object?>? Handler { get; set; }

    /// <summary>Ionic <c>data</c>: the dismiss's data.</summary>
    public object? Data { get; set; }

    /// <summary>Ionic <c>disabled</c>: not interactive. A cancel button can't be disabled, as Ionic's docs say.</summary>
    public bool Disabled { get; set; }

    public static implicit operator ActionSheetButton(string text) => new(text);
}
