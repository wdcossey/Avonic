using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-tab-bar</c>'s flex row, <c>justify-content: center</c>: each button is <c>flex: 1</c>, so they share the width
/// from a basis of 0, each at most its <c>MaxWidth</c> (md's 168) and at least its min-content (<c>min-width: auto</c>), with what's
/// left over split on both sides. Each is as high as the row (<c>height: 100%</c>).
/// </summary>
public sealed class TabBarPanel : Panel
{
    private List<Control> _items = [];
    private double[] _widths = [];

    protected override Size MeasureOverride(Size availableSize)
    {
        _items = Children.Where(child => child.IsVisible).ToList();
        _widths = new double[_items.Count];
        var height = availableSize.Height;

        // Each on one line first, which is what the min-content is worked out from.
        var mins = new double[_items.Count];
        var maxes = new double[_items.Count];
        for (var i = 0; i < _items.Count; i++)
        {
            var child = _items[i];
            child.Measure(new Size(double.PositiveInfinity, height));
            maxes[i] = child.MaxWidth + MinContent.Horizontal(child.Margin);
            // The automatic minimum is no more than the maximum.
            mins[i] = Math.Min(MinContent.Of(child), maxes[i]);
        }

        if (double.IsInfinity(availableSize.Width))
        {
            // No width to share: each at its own.
            for (var i = 0; i < _items.Count; i++)
            {
                _widths[i] = Math.Clamp(_items[i].DesiredSize.Width, mins[i], maxes[i]);
            }
        }
        else
        {
            Resolve(availableSize.Width, mins, maxes);
        }

        double tallest = 0;
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Measure(new Size(_widths[i], height));
            tallest = Math.Max(tallest, _items[i].DesiredSize.Height);
        }

        return new Size(Math.Min(_widths.Sum(), availableSize.Width), double.IsInfinity(height) ? tallest : height);
    }

    /// <summary>
    /// CSS's resolving of flexible lengths, for equal growth from a basis of 0: share what's free, clamp each to its minimum and
    /// maximum, and freeze the ones that broke the larger way, until none does.
    /// </summary>
    private void Resolve(double width, double[] mins, double[] maxes)
    {
        var frozen = new bool[_items.Count];
        while (true)
        {
            var open = Enumerable.Range(0, _items.Count).Where(i => !frozen[i]).ToList();
            if (open.Count == 0)
            {
                return;
            }

            var free = width - Enumerable.Range(0, _items.Count).Where(i => frozen[i]).Sum(i => _widths[i]);
            var share = Math.Max(0, free) / open.Count;
            double violation = 0;
            foreach (var i in open)
            {
                var clamped = Math.Clamp(share, mins[i], Math.Max(mins[i], maxes[i]));
                violation += clamped - share;
                _widths[i] = clamped;
            }

            if (violation == 0)
            {
                return;
            }

            foreach (var i in open)
            {
                if ((violation > 0 && _widths[i] > share) || (violation < 0 && _widths[i] < share))
                {
                    frozen[i] = true;
                }
            }
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // justify-content: center, which overflows on both sides when they don't fit.
        var x = (finalSize.Width - _widths.Sum()) / 2;
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Arrange(new Rect(x, 0, _widths[i], finalSize.Height));
            x += _widths[i];
        }

        return finalSize;
    }
}
