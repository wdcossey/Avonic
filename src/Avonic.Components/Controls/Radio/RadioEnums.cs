namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-radio</c> <c>labelPlacement</c>: where the label sits beside the radio.</summary>
public enum RadioLabelPlacement
{
    /// <summary>Before the radio: on the left in LTR, on the right in RTL.</summary>
    Start,

    /// <summary>After the radio: on the right in LTR, on the left in RTL.</summary>
    End,

    /// <summary>As <see cref="Start"/>, 100 wide, with an ellipsis.</summary>
    Fixed,

    /// <summary>Above the radio, scaled to 0.75. <see cref="AvonicRadio.Alignment"/> places it across.</summary>
    Stacked,
}

/// <summary>Ionic <c>ion-radio</c> <c>justify</c>: how the label and the radio share their line.</summary>
public enum RadioJustify
{
    Start,
    End,
    SpaceBetween,
}

/// <summary>Ionic <c>ion-radio</c> <c>alignment</c>: where the label and the radio sit across their line.</summary>
public enum RadioAlignment
{
    Start,
    Center,
}
