using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Reactive;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>--ion-safe-area-*</c>, which a page gets from <c>env(safe-area-inset-*)</c> once its viewport is <c>viewport-fit=cover</c>,
/// and is 0 otherwise. Avalonia's version of <c>cover</c> is the top level's content with <see cref="TopLevel.AutoSafeAreaPadding"/>
/// off: then Avalonia doesn't pad the root by the insets, and the four <c>Avonic.SafeArea*</c> keys are filled from
/// <see cref="IInsetsManager.SafeAreaPadding"/>, merged into the top level's resources, so that they reach its overlays too, which
/// are outside the app, and an app's own key there still wins. While Avalonia pads the root, they stay the theme's 0, so nothing is padded twice. Started by
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

    private static readonly TopLevelStarts<Fill> s_started = new(top => new Fill(top), fill => fill.Attach(), fill => fill.Detach());

    internal static IDisposable Start(TopLevel topLevel) => s_started.Start(topLevel);

    private sealed class Fill(TopLevel topLevel)
    {
        /// <summary>
        /// The keys, merged into the top level's resources while they're filled, so that a key an app sets there itself still
        /// wins, as a dictionary's own keys beat its merged ones.
        /// </summary>
        private readonly ResourceDictionary _keys = new();

        private IInsetsManager? _insets;
        private IDisposable? _content;
        private IDisposable? _padding;
        private bool _filled;

        public void Attach()
        {
            _insets = InsetsOf(topLevel);
            if (_insets is not null)
            {
                _insets.SafeAreaChanged += OnSafeAreaChanged;
            }

            _content = topLevel.GetPropertyChangedObservable(ContentControl.ContentProperty)
                .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(_ => WatchRoot()));
            WatchRoot();
        }

        public void Detach()
        {
            if (_insets is not null)
            {
                _insets.SafeAreaChanged -= OnSafeAreaChanged;
                _insets = null;
            }

            _content?.Dispose();
            _content = null;
            _padding?.Dispose();
            _padding = null;
            Clear();
        }

        private void WatchRoot()
        {
            _padding?.Dispose();
            _padding = (topLevel.Content as Control)?.GetPropertyChangedObservable(TopLevel.AutoSafeAreaPaddingProperty)
                .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(_ => Update()));
            Update();
        }

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => Update();

        private void Update()
        {
            if (_insets is { } insets && topLevel.Content is Control root && !TopLevel.GetAutoSafeAreaPadding(root))
            {
                var padding = insets.SafeAreaPadding;
                _keys[TopKey] = padding.Top;
                _keys[BottomKey] = padding.Bottom;
                _keys[LeftKey] = padding.Left;
                _keys[RightKey] = padding.Right;
                if (!_filled)
                {
                    _filled = true;
                    topLevel.Resources.MergedDictionaries.Add(_keys);
                }
            }
            else
            {
                Clear();
            }
        }

        private void Clear()
        {
            if (_filled)
            {
                _filled = false;
                topLevel.Resources.MergedDictionaries.Remove(_keys);
            }
        }
    }
}
