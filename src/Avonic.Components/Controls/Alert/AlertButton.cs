namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>AlertButton</c>: a button of an <see cref="AvonicAlert"/>. Plain data, as Ionic's is, so that code (an
/// <c>ion-select</c>) can build the alert's buttons. A string converts to one, as Ionic's string form does.
/// </summary>
public sealed class AlertButton
{
    public AlertButton()
    {
    }

    /// <summary>Ionic's string form: <paramref name="text"/>, with the <c>cancel</c> role when it says <c>cancel</c> in any case.</summary>
    public AlertButton(string text)
    {
        Text = text;
        Role = string.Equals(text, "cancel", StringComparison.OrdinalIgnoreCase) ? "cancel" : null;
    }

    /// <summary>Ionic <c>text</c>.</summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Ionic <c>role</c>: <c>cancel</c> dismisses with no say from <see cref="Handler"/>; any role is the dismiss's, and the
    /// button's pseudoclass <c>:role-&lt;role&gt;</c> (Ionic's <c>alert-button-role-&lt;role&gt;</c>).
    /// </summary>
    public string? Role { get; set; }

    /// <summary>Ionic <c>cssClass</c>: style classes on the button, space-separated.</summary>
    public string? CssClass { get; set; }

    /// <summary>Ionic <c>id</c>: the button's <c>AutomationId</c>.</summary>
    public string? Id { get; set; }

    /// <summary>
    /// Ionic <c>htmlAttributes</c>, spread on the <c>&lt;button&gt;</c>: <c>aria-label</c> names it, and <c>disabled</c> (true, or
    /// any value but <c>false</c>) disables it. Other attributes have nothing to do in Avalonia, and are ignored.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? HtmlAttributes { get; set; }

    /// <summary>
    /// Ionic <c>handler</c>, given the alert's values. <c>false</c> keeps the alert open; a dictionary is merged into the dismiss's
    /// data beside <c>values</c>; a <see cref="Task"/> is awaited, and its result read the same way; anything else dismisses.
    /// </summary>
    public Func<object?, object?>? Handler { get; set; }

    public static implicit operator AlertButton(string text) => new(text);
}
