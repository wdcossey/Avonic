namespace Avonic.Components.Controls;

/// <summary>Ionic's popover <c>side</c>: which side of the reference it's on. <see cref="Start"/> and <see cref="End"/> follow the direction.</summary>
public enum PopoverSide
{
    Top,
    Right,
    Bottom,
    Left,
    Start,
    End,
}

/// <summary>Ionic's popover <c>alignment</c>: how it lines up along the reference.</summary>
public enum PopoverAlignment
{
    Start,
    Center,
    End,
}

/// <summary>Ionic's popover <c>size</c>: <see cref="Auto"/> is <c>--width</c>, <see cref="Cover"/> the reference's width.</summary>
public enum PopoverSize
{
    Auto,
    Cover,
}

/// <summary>Ionic's popover <c>reference</c>: the trigger's box, or the event's point.</summary>
public enum PopoverReference
{
    Event,
    Trigger,
}

/// <summary>Ionic's popover <c>triggerAction</c>: what on the trigger presents it.</summary>
public enum PopoverTriggerAction
{
    Click,
    Hover,
    ContextMenu,
}
