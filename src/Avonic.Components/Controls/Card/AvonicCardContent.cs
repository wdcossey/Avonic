using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.LogicalTree;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-card-content</c>. Its look is in the theme's <c>Styles/Card.axaml</c>.</summary>
[PseudoClasses(AfterCardHeaderPseudoClass)]
public class AvonicCardContent : ContentControl
{
    /// <summary><c>ion-card-header + .card-content-md</c>. Avalonia has no adjacent sibling selector.</summary>
    public const string AfterCardHeaderPseudoClass = ":after-card-header";

    private Panel? _panel;

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        _panel = Parent as Panel;
        if (_panel is not null)
        {
            _panel.Children.CollectionChanged += OnSiblingsChanged;
        }

        UpdateAfterCardHeader();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);

        if (_panel is not null)
        {
            _panel.Children.CollectionChanged -= OnSiblingsChanged;
        }
    }

    private void OnSiblingsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateAfterCardHeader();

    /// <summary>A hidden header still counts, as <c>+</c> doesn't care about <c>display: none</c>.</summary>
    private void UpdateAfterCardHeader()
    {
        var index = _panel?.Children.IndexOf(this) ?? -1;
        PseudoClasses.Set(AfterCardHeaderPseudoClass, index > 0 && _panel!.Children[index - 1] is AvonicCardHeader);
    }
}
