namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-input-otp</c> <c>fill</c>.</summary>
public enum InputOtpFill
{
    /// <summary>Transparent boxes with a border. The default.</summary>
    Outline,

    /// <summary>Boxes with a background.</summary>
    Solid,
}

/// <summary>Ionic <c>ion-input-otp</c> <c>shape</c>.</summary>
public enum InputOtpShape
{
    /// <summary>No border radius.</summary>
    Rectangular,

    /// <summary>A larger border radius. The default.</summary>
    Round,

    /// <summary>A soft border radius.</summary>
    Soft,
}

/// <summary>Ionic <c>ion-input-otp</c> <c>size</c>.</summary>
public enum InputOtpSize
{
    /// <summary>56 square in md.</summary>
    Large,

    /// <summary>48 square in md. The default.</summary>
    Medium,

    /// <summary>40 square in md, with a smaller gap.</summary>
    Small,
}

/// <summary>Ionic <c>ion-input-otp</c> <c>type</c>: the characters allowed, unless <c>Pattern</c> says, and the keyboard.</summary>
public enum InputOtpType
{
    /// <summary>Digits, <c>[\p{N}]</c>, with a numeric keyboard. The default.</summary>
    Number,

    /// <summary>Letters and digits, <c>[\p{L}\p{N}]</c>, with a text keyboard.</summary>
    Text,
}

/// <summary>
/// Ionic <c>ion-input-otp</c> <c>inputmode</c>: the soft keyboard. <c>none</c> isn't declared, as Avalonia can't keep the soft
/// keyboard away from a focused <c>TextBox</c>.
/// </summary>
public enum InputOtpInputmode
{
    /// <summary>Numbers with a decimal separator.</summary>
    Decimal,

    /// <summary>An email address.</summary>
    Email,

    /// <summary>Digits.</summary>
    Numeric,

    /// <summary>A search.</summary>
    Search,

    /// <summary>A telephone number: digits, here.</summary>
    Tel,

    /// <summary>Text.</summary>
    Text,

    /// <summary>A URL.</summary>
    Url,
}
