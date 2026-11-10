namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-checkbox</c> <c>alignment</c>: where the label and the box sit on the cross axis.</summary>
public enum CheckboxAlignment
{
    Start,
    Center,
}

/// <summary>Ionic <c>ion-checkbox</c> <c>justify</c>: how the label and the box are packed on a line.</summary>
public enum CheckboxJustify
{
    Start,
    End,
    SpaceBetween,
}

/// <summary>Ionic <c>ion-checkbox</c> <c>labelPlacement</c>: where the label is, beside the box or above it.</summary>
public enum CheckboxLabelPlacement
{
    /// <summary>Before the box: on the left in LTR, on the right in RTL.</summary>
    Start,

    /// <summary>After the box.</summary>
    End,

    /// <summary>As <see cref="Start"/>, 100 wide, ending in an ellipsis.</summary>
    Fixed,

    /// <summary>Above the box, scaled down.</summary>
    Stacked,
}
