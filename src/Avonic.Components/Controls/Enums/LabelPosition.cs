namespace Avonic.Components.Controls;

public enum LabelPosition
{
    /// <summary>Label flows inline with content.</summary>
    Default,
    /// <summary>Label is fixed to a set width, does not grow or shrink.</summary>
    Fixed,
    /// <summary>Label stacks above the content (used for inputs).</summary>
    Stacked,
    /// <summary>Label floats above the content when the input has focus or a value.</summary>
    Floating,
}
