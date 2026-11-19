namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ToastButton</c>: a button of an <see cref="AvonicToast"/>. Plain data, as Ionic's is. A string converts to one, as
/// Ionic's string form does: its text, and no role (so <c>"Cancel"</c> is a plain button).
/// </summary>
public sealed class ToastButton
{
    public ToastButton()
    {
    }

    /// <summary>Ionic's string form, <c>{ text }</c>.</summary>
    public ToastButton(string text) => Text = text;

    /// <summary>Ionic <c>text</c>, shown uppercase by md.</summary>
    public string? Text { get; set; }

    /// <summary>
    /// Ionic <c>icon</c>: an ionicons name, or what <see cref="AvonicIcon.Icon"/> takes, before the text. With no text, the button
    /// is an icon-only one.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>Ionic <c>side</c>: <see cref="ToastButtonSide.Start"/> puts it before the icon and the content; null is the end.</summary>
    public ToastButtonSide? Side { get; set; }

    /// <summary>
    /// Ionic <c>role</c>: the dismiss's role, and the pseudoclass <c>:role-&lt;role&gt;</c> on the button (<c>toast-button-&lt;role&gt;</c>).
    /// <c>cancel</c> and <c>backdrop</c> dismiss at once, with no say from <see cref="Handler"/>; the first <c>cancel</c> button's
    /// handler runs at any dismiss with one of those roles.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Ionic <c>htmlAttributes</c>, spread on the <c>&lt;button&gt;</c>: <c>aria-label</c> names it. Other attributes have nothing to
    /// do in Avalonia, and are ignored.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? HtmlAttributes { get; set; }

    /// <summary>
    /// Ionic <c>handler</c>: <c>false</c> keeps the toast open; a <see cref="Task"/> or <see cref="ValueTask"/> is awaited, and its
    /// result read the same way; anything else, an exception too, dismisses.
    /// </summary>
    public Func<object?>? Handler { get; set; }

    public static implicit operator ToastButton(string text) => new(text);
}
