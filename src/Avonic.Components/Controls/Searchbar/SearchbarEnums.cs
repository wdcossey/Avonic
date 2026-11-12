using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-searchbar</c> <c>showCancelButton</c>.</summary>
public enum SearchbarShowCancelButton
{
    /// <summary>Shown whatever the focus.</summary>
    Always,

    /// <summary>Shown while the field has focus.</summary>
    Focus,

    /// <summary>Not there at all. The default.</summary>
    Never,
}

/// <summary>Ionic <c>ion-searchbar</c> <c>showClearButton</c>. The button shows only while there's a value.</summary>
public enum SearchbarShowClearButton
{
    /// <summary>Shown whatever the focus. The default.</summary>
    Always,

    /// <summary>Shown while the field has focus.</summary>
    Focus,

    /// <summary>Never shown.</summary>
    Never,
}

/// <summary>Ionic <c>ion-searchbar</c> <c>type</c>: the soft keyboard, masking for <see cref="Password"/>, and Escape's clear for <see cref="Search"/>.</summary>
public enum SearchbarType
{
    Email,
    Number,
    Password,

    /// <summary>The default. Escape clears it, as Chromium's search field does.</summary>
    Search,
    Tel,
    Text,
    Url,
}

/// <summary>Ionic <c>ion-searchbar</c> <c>autocorrect</c>.</summary>
public enum SearchbarAutocorrect
{
    Off,
    On,
}

/// <summary>Ionic <c>ion-searchbar</c> <c>enterkeyhint</c>: the soft keyboard's enter key. Null is the platform's own.</summary>
public enum SearchbarEnterkeyhint
{
    Done,
    Enter,
    Go,
    Next,
    Previous,
    Search,
    Send,
}

/// <summary>
/// Ionic <c>ion-searchbar</c> <c>inputmode</c>: the soft keyboard, beating the type's. <c>none</c> isn't here: Avalonia can't
/// keep the keyboard away from a focused text box.
/// </summary>
public enum SearchbarInputmode
{
    Decimal,
    Email,
    Numeric,
    Search,
    Tel,
    Text,
    Url,
}

/// <summary>Ionic <c>SearchbarInputEventDetail</c>: what <see cref="AvonicSearchbar.IonInput"/> carries.</summary>
public sealed class SearchbarInputEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the value after the edit.</summary>
    public string? Value { get; } = value;
}

/// <summary>Ionic <c>SearchbarChangeEventDetail</c>: what <see cref="AvonicSearchbar.IonChange"/> carries.</summary>
public sealed class SearchbarChangeEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the value committed.</summary>
    public string? Value { get; } = value;
}
