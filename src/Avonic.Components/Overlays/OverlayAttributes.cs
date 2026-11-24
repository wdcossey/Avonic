namespace Avonic.Components.Overlays;

/// <summary>
/// What the alert, the action sheet, the toast and the loading read from an <c>htmlAttributes</c> dictionary (and the alert's
/// inputs from <c>attributes</c>). Ionic spreads it on an element, and Stencil writes each value: a key that's missing or null
/// isn't there, and anything else is written as text. Avonic reads the text with <see cref="object.ToString"/>, so a string is
/// itself, an empty string stays empty, and a number is its digits. A boolean is <c>True</c> or <c>False</c>, where Stencil writes
/// an attribute's <c>true</c> as <c>""</c> and leaves out one that's <c>false</c>, and writes an <c>id</c> on a button as
/// <c>"true"</c> or <c>"false"</c>; no caller is given one. <c>disabled</c> sets the button's property, so it's read as
/// JavaScript's truthiness, by <c>AlertClasses.IsTrue</c>.
/// </summary>
internal static class OverlayAttributes
{
    /// <summary>The value the spread gives the key: null when it's missing or null, else its text, which can be empty.</summary>
    public static string? Read(IReadOnlyDictionary<string, object?>? attributes, string key) =>
        attributes is not null && attributes.TryGetValue(key, out var value) ? value?.ToString() : null;

    /// <summary>
    /// <c>id</c>, the element's <c>AutomationId</c>: null when it isn't there or is empty, as an empty <c>id</c> names nothing.
    /// </summary>
    public static string? Id(IReadOnlyDictionary<string, object?>? attributes) => NonEmpty(Read(attributes, "id"));

    /// <summary>
    /// <c>aria-label</c>, the element's name: null when it isn't there or is empty, as an empty <c>aria-label</c> is skipped
    /// when the browser names the element.
    /// </summary>
    public static string? AriaLabel(IReadOnlyDictionary<string, object?>? attributes) => NonEmpty(Read(attributes, "aria-label"));

    /// <summary>
    /// The value itself is the string given, as Ionic's <c>htmlAttributes?.role === 'radio'</c> compares it before any spread:
    /// a value that isn't a string never matches.
    /// </summary>
    public static bool Is(IReadOnlyDictionary<string, object?>? attributes, string key, string value) =>
        attributes is not null && attributes.TryGetValue(key, out var given) && given is string text && text == value;

    private static string? NonEmpty(string? text) => text is { Length: > 0 } ? text : null;
}
