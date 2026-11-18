namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-select</c> <c>interface</c>: which overlay shows the options.</summary>
public enum SelectInterface
{
    /// <summary>An <see cref="AvonicAlert"/> of radios, or checkboxes with <see cref="AvonicSelect.Multiple"/>, with Cancel and OK. The default.</summary>
    Alert,

    /// <summary>An <see cref="AvonicActionSheet"/> of radio buttons. Not with <see cref="AvonicSelect.Multiple"/>, which uses the alert.</summary>
    ActionSheet,

    /// <summary>An <see cref="AvonicPopover"/> at the select. Not from <see cref="AvonicSelect.Open"/> with no event, which uses the alert.</summary>
    Popover,

    /// <summary>An <see cref="AvonicModal"/> of an <see cref="AvonicSelectModal"/>.</summary>
    Modal,
}

/// <summary>Ionic <c>ion-select</c> <c>labelPlacement</c>.</summary>
public enum SelectLabelPlacement
{
    /// <summary>Before the select: left in LTR. The default.</summary>
    Start,

    /// <summary>After the select.</summary>
    End,

    /// <summary>As <see cref="Start"/>, 100 px wide, trimmed with an ellipsis.</summary>
    Fixed,

    /// <summary>On the select until it has a value, focus or is open; then small, above it.</summary>
    Floating,

    /// <summary>Small, above the select, always.</summary>
    Stacked,
}

/// <summary>Ionic <c>ion-select</c> <c>fill</c>. Null is none.</summary>
public enum SelectFill
{
    /// <summary>Transparent, with a border all round and a notch for the label.</summary>
    Outline,

    /// <summary>A background and a bottom border.</summary>
    Solid,
}

/// <summary>Ionic <c>ion-select</c> <c>shape</c>. Null is the default.</summary>
public enum SelectShape
{
    /// <summary>A larger radius.</summary>
    Round,
}

/// <summary>Ionic <c>ion-select</c> <c>justify</c>: how the label and the select share a line. Null is <c>space-between</c>'s look.</summary>
public enum SelectJustify
{
    /// <summary>Both at the start.</summary>
    Start,

    /// <summary>Both at the end.</summary>
    End,

    /// <summary>At opposite ends.</summary>
    SpaceBetween,
}

/// <summary>Ionic <c>ion-select-option</c> <c>labelPlacement</c>: the label beside the option's radio or checkbox in an interface.</summary>
public enum SelectOptionLabelPlacement
{
    /// <summary>Before the control.</summary>
    Start,

    /// <summary>After the control: md's default.</summary>
    End,
}

/// <summary>Ionic <c>ion-select-option</c> <c>justify</c>: how the label and the option's control share a line in an interface.</summary>
public enum SelectOptionJustify
{
    /// <summary>Both at the start: md's default.</summary>
    Start,

    /// <summary>Both at the end.</summary>
    End,

    /// <summary>At opposite ends.</summary>
    SpaceBetween,
}
