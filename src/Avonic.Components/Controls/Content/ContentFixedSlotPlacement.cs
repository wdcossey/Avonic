namespace Avonic.Components.Controls;

/// <summary>Ionic <c>fixedSlotPlacement</c>: where the fixed slot sits relative to the scrolling area.</summary>
public enum ContentFixedSlotPlacement
{
    /// <summary>After it: painted over the content, and reached last by Tab.</summary>
    After,

    /// <summary>Before it: reached first by Tab, but painted under the content, which also takes its pointer input.</summary>
    Before,
}
