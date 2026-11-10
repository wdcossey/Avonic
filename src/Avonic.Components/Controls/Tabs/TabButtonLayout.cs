namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-tab-button</c>'s <c>layout</c>: where the icon goes, or what's hidden.</summary>
public enum TabButtonLayout
{
    /// <summary><c>icon-top</c>, the default: a column, the icon over the label.</summary>
    IconTop,

    /// <summary><c>icon-start</c>: a row, the icon first.</summary>
    IconStart,

    /// <summary><c>icon-end</c>: a reversed row, the icon last.</summary>
    IconEnd,

    /// <summary><c>icon-bottom</c>: a reversed column, the icon under the label.</summary>
    IconBottom,

    /// <summary><c>icon-hide</c>: the icon isn't shown.</summary>
    IconHide,

    /// <summary><c>label-hide</c>: the label isn't shown.</summary>
    LabelHide,
}
