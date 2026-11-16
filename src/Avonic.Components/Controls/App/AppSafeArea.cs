using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>--ion-safe-area-*</c>, which a page gets from <c>env(safe-area-inset-*)</c> once its viewport is <c>viewport-fit=cover</c>,
/// and is 0 otherwise. Avalonia's version of <c>cover</c> is the top level's content with <see cref="TopLevel.AutoSafeAreaPadding"/>
/// off: then Avalonia doesn't pad the root by the insets, and the four <c>Avonic.SafeArea*</c> keys are filled from
/// <see cref="IInsetsManager.SafeAreaPadding"/>, in the top level's resources, so that they reach its overlays too, which are
/// outside the app. While Avalonia pads the root, they stay the theme's 0, so nothing is padded twice. Started by
/// <see cref="AvonicApp"/>.
/// </summary>
internal static class AppSafeArea
{
    internal const string TopKey = "Avonic.SafeAreaTop";
    internal const string BottomKey = "Avonic.SafeAreaBottom";
    internal const string LeftKey = "Avonic.SafeAreaLeft";
    internal const string RightKey = "Avonic.SafeAreaRight";

    /// <summary>Where the insets come from. Tests replace it, as a headless window has none.</summary>
    internal static Func<TopLevel, IInsetsManager?> InsetsOf { get; set; } = top => top.InsetsManager;

    private static readonly ConditionalWeakTable<TopLevel, Fill> s_started = new();

    internal static IDisposable Start(TopLevel topLevel)
    {
        var fill = s_started.GetValue(topLevel, top => new Fill(top));
        if (fill.Count++ == 0)
        {
            fill.Attach();
        }

        return new Stop(fill);
    }

    private sealed class Stop(Fill fill) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (--fill.Count == 0)
                {
                    fill.Detach();
                }
            }
        }
    }

    private sealed class Fill(TopLevel topLevel)
    {
        private IInsetsManager? _insets;
        private Control? _root;
        private bool _filled;

        public int Count { get; set; }

        public void Attach()
        {
            _insets = InsetsOf(topLevel);
            if (_insets is not null)
            {
                _insets.SafeAreaChanged += OnSafeAreaChanged;
            }

            topLevel.PropertyChanged += OnTopLevelChanged;
            Watch(topLevel.Content as Control);
            Update();
        }

        public void Detach()
        {
            if (_insets is not null)
            {
                _insets.SafeAreaChanged -= OnSafeAreaChanged;
                _insets = null;
            }

            topLevel.PropertyChanged -= OnTopLevelChanged;
            Watch(null);
            Clear();
        }

        private void Watch(Control? root)
        {
            if (_root is not null)
            {
                _root.PropertyChanged -= OnRootChanged;
            }

            _root = root;
            if (root is not null)
            {
                root.PropertyChanged += OnRootChanged;
            }
        }

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => Update();

        private void OnTopLevelChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ContentControl.ContentProperty)
            {
                Watch(topLevel.Content as Control);
                Update();
            }
        }

        private void OnRootChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TopLevel.AutoSafeAreaPaddingProperty)
            {
                Update();
            }
        }

        private void Update()
        {
            if (_insets is { } insets && _root is { } root && !TopLevel.GetAutoSafeAreaPadding(root))
            {
                var padding = insets.SafeAreaPadding;
                var resources = topLevel.Resources;
                resources[TopKey] = padding.Top;
                resources[BottomKey] = padding.Bottom;
                resources[LeftKey] = padding.Left;
                resources[RightKey] = padding.Right;
                _filled = true;
            }
            else
            {
                Clear();
            }
        }

        private void Clear()
        {
            if (!_filled)
            {
                return;
            }

            _filled = false;
            var resources = topLevel.Resources;
            resources.Remove(TopKey);
            resources.Remove(BottomKey);
            resources.Remove(LeftKey);
            resources.Remove(RightKey);
        }
    }
}
