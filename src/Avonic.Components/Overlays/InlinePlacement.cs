using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.VisualTree;

namespace Avonic.Components.Overlays;

/// <summary>
/// Ionic's <c>CoreDelegate</c> for an inline overlay, one declared in a page: at present it's moved to the overlay host, and a
/// placeholder takes its place, as Ionic leaves a comment where it was and moves it to <c>ion-app</c>; after its dismiss it goes
/// back. The placeholder is invisible, as the overlay was (<c>overlay-hidden</c>), so the page's layout doesn't change.
/// </summary>
/// <remarks>
/// An Avalonia control has one parent, and a template's parts or an items host's children aren't the app's to move, so the
/// overlay can be in a <see cref="Panel"/>'s <c>Children</c>, a <see cref="Decorator"/>'s <c>Child</c>, or the <c>Content</c> of a
/// <see cref="ContentControl"/> or a <see cref="ContentPresenter"/>. Its <see cref="StyledElement.DataContext"/> stays the
/// page's, through a style-priority binding to the placeholder's, as a framework's bindings work across Ionic's teleport; a
/// local value still wins.
/// </remarks>
internal sealed class InlinePlacement
{
    private readonly Control _overlay;
    private readonly Action _restore;
    private readonly Action _onParentRemoved;
    private readonly IDisposable _dataContext;

    private InlinePlacement(Control overlay, Control placeholder, Action restore, Action onParentRemoved)
    {
        _overlay = overlay;
        Placeholder = placeholder;
        _restore = restore;
        _onParentRemoved = onParentRemoved;
        _dataContext = overlay.Bind(StyledElement.DataContextProperty, placeholder.GetObservable(StyledElement.DataContextProperty), BindingPriority.Style);
        placeholder.DetachedFromVisualTree += OnPlaceholderDetached;
    }

    /// <summary>What's in the overlay's place while it's presented.</summary>
    public Control Placeholder { get; }

    /// <summary>Moves <paramref name="overlay"/> out of its parent, leaving a placeholder.</summary>
    /// <param name="onParentRemoved">
    /// Called when the placeholder leaves the visual tree while the overlay is out: its page has gone, as the modal's
    /// <c>MutationObserver</c> sees.
    /// </param>
    /// <exception cref="InvalidOperationException">Its parent isn't one it can be moved out of and back into.</exception>
    public static InlinePlacement Teleport(Control overlay, Action onParentRemoved)
    {
        var placeholder = new Control { IsVisible = false };
        var parent = overlay.Parent ?? overlay.GetVisualParent() as StyledElement;
        Action restore;
        switch (parent)
        {
            case Panel { IsItemsHost: false } panel when panel.Children.IndexOf(overlay) is var index and >= 0:
                panel.Children[index] = placeholder;
                restore = () =>
                {
                    if (panel.Children.IndexOf(placeholder) is var at and >= 0)
                    {
                        panel.Children[at] = overlay;
                    }
                };
                break;
            case Decorator decorator when decorator.Child == overlay:
                decorator.Child = placeholder;
                restore = () =>
                {
                    if (decorator.Child == placeholder)
                    {
                        decorator.Child = overlay;
                    }
                };
                break;
            case ContentControl contentControl when contentControl.Content == overlay:
                contentControl.SetCurrentValue(ContentControl.ContentProperty, placeholder);
                restore = () =>
                {
                    if (contentControl.Content == placeholder)
                    {
                        contentControl.SetCurrentValue(ContentControl.ContentProperty, overlay);
                    }
                };
                break;
            case ContentPresenter presenter when presenter.Content == overlay:
                presenter.SetCurrentValue(ContentPresenter.ContentProperty, placeholder);
                restore = () =>
                {
                    if (presenter.Content == placeholder)
                    {
                        presenter.SetCurrentValue(ContentPresenter.ContentProperty, overlay);
                    }
                };
                break;
            default:
                throw new InvalidOperationException(
                    $"An inline {overlay.GetType().Name} is moved to the overlay layer while it's presented, and back after, so it has to be in a " +
                    $"Panel's Children, a Decorator's Child, or a ContentControl's or ContentPresenter's Content. This one is in a {parent?.GetType().Name}.");
        }

        return new InlinePlacement(overlay, placeholder, restore, onParentRemoved);
    }

    /// <summary>Puts the overlay back in its place, if the placeholder is still there. The overlay has to have no parent by then.</summary>
    public void Restore()
    {
        Placeholder.DetachedFromVisualTree -= OnPlaceholderDetached;
        _dataContext.Dispose();
        if (_overlay.Parent is null && _overlay.GetVisualParent() is null)
        {
            _restore();
        }
    }

    private void OnPlaceholderDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Placeholder.DetachedFromVisualTree -= OnPlaceholderDetached;
        _onParentRemoved();
    }
}
