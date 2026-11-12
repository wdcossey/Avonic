namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-textarea</c> <c>labelPlacement</c>.</summary>
public enum TextareaLabelPlacement
{
    /// <summary>Before the field, at the top.</summary>
    Start,

    /// <summary>After the field.</summary>
    End,

    /// <summary>Before the field, 100 px wide.</summary>
    Fixed,

    /// <summary>On the field until it has a value, focus or slotted content, then above it, smaller.</summary>
    Floating,

    /// <summary>Above the field, smaller.</summary>
    Stacked,
}

/// <summary>Ionic <c>ion-textarea</c> <c>fill</c>.</summary>
public enum TextareaFill
{
    /// <summary>A border around the field and the label.</summary>
    Outline,

    /// <summary>A background and a bottom border.</summary>
    Solid,
}

/// <summary>Ionic <c>ion-textarea</c> <c>shape</c>.</summary>
public enum TextareaShape
{
    /// <summary>A larger radius.</summary>
    Round,
}

/// <summary>Ionic <c>ion-textarea</c> <c>inputmode</c>: which soft keyboard. <c>none</c> isn't declared: Avalonia can't keep it away.</summary>
public enum TextareaInputmode
{
    /// <summary>Numbers and a decimal point.</summary>
    Decimal,

    /// <summary>An email address.</summary>
    Email,

    /// <summary>Digits.</summary>
    Numeric,

    /// <summary>A search.</summary>
    Search,

    /// <summary>A telephone number.</summary>
    Tel,

    /// <summary>Text.</summary>
    Text,

    /// <summary>A URL.</summary>
    Url,
}

/// <summary>Ionic <c>ion-textarea</c> <c>enterkeyhint</c>: the soft keyboard's Enter key.</summary>
public enum TextareaEnterkeyhint
{
    /// <summary>Done.</summary>
    Done,

    /// <summary>A new line.</summary>
    Enter,

    /// <summary>Go.</summary>
    Go,

    /// <summary>Next.</summary>
    Next,

    /// <summary>Previous.</summary>
    Previous,

    /// <summary>Search.</summary>
    Search,

    /// <summary>Send.</summary>
    Send,
}

/// <summary>Ionic <c>ion-textarea</c> <c>wrap</c>: how the text wraps.</summary>
public enum TextareaWrap
{
    /// <summary>Wraps; a submitted form would have the breaks too, which doesn't apply here.</summary>
    Hard,

    /// <summary>Doesn't wrap: a long line scrolls sideways.</summary>
    Off,

    /// <summary>Wraps, as with no value.</summary>
    Soft,
}
