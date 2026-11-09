using Avalonia;
using Avalonia.VisualTree;

namespace Avonic.Components.Behaviors;

/// <summary>
/// Watches <see cref="Visual.IsVisible"/> on whatever in a visual's chain is hidden, and calls back once all of it shows.
/// <c>display: none</c>, on an element or an ancestor, stops what CSS does for it, such as an animation or an intersection, and
/// Avalonia doesn't tell a control when an ancestor hides or shows it. A hide and show that happen before a control next looks
/// aren't seen, as in Chromium, where no style change sees them.
/// </summary>
/// <remarks>
/// It watches what was hidden when <see cref="Watch"/> found it. Something in the chain hidden after that isn't watched, so the
/// callback can come while the visual is still hidden: each control's callback looks again, and watches again if it is.
/// </remarks>
internal sealed class HiddenChainWatcher(Action onShown) : IDisposable
{
    private readonly List<Visual> _hiddenBy = [];

    /// <summary>Whether it's watching something hidden.</summary>
    public bool IsWaiting => _hiddenBy.Count > 0;

    /// <summary>
    /// Watches what's hidden in <paramref name="visual"/>'s chain, from it to the root. Nothing if it's already watching, or if
    /// nothing is hidden.
    /// </summary>
    public void Watch(Visual visual)
    {
        if (IsWaiting)
        {
            return;
        }

        for (var current = (Visual?)visual; current is not null; current = current.GetVisualParent())
        {
            if (!current.IsVisible)
            {
                current.PropertyChanged += OnHiddenVisualChanged;
                _hiddenBy.Add(current);
            }
        }
    }

    /// <summary>Stops watching, without calling back. It can watch again.</summary>
    public void Dispose()
    {
        foreach (var visual in _hiddenBy)
        {
            visual.PropertyChanged -= OnHiddenVisualChanged;
        }

        _hiddenBy.Clear();
    }

    private void OnHiddenVisualChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty || !_hiddenBy.TrueForAll(visual => visual.IsVisible))
        {
            return;
        }

        Dispose();
        onShown();
    }
}
