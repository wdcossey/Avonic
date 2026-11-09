using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avonic.Sample;

/// <summary>
/// Where the safe area goes. By default Avalonia pads the main view by it, through <see cref="TopLevel.AutoSafeAreaPadding"/>, and
/// <c>Avonic.SafeArea*</c> stay 0. <see cref="Set"/> turns on Ionic's way instead: the view edge to edge, unpadded, and the four keys
/// filled from the insets, as in docs/components/ion-content/README.md. The shell then pads its header by them, as a header's first
/// toolbar does. See docs/infra/android/PLAN.md.
/// </summary>
public static class SafeAreaMode
{
    public const string TopKey = "Avonic.SafeAreaTop";
    public const string BottomKey = "Avonic.SafeAreaBottom";
    public const string LeftKey = "Avonic.SafeAreaLeft";
    public const string RightKey = "Avonic.SafeAreaRight";

    private static IInsetsManager? s_insets;
    private static bool s_edgeToEdgeBefore;

    /// <summary>Whether Ionic's way is on.</summary>
    public static bool IsIonic => s_insets is not null;

    /// <summary>
    /// Turns Ionic's way on or off for <paramref name="topLevel"/>'s main view. False, with nothing changed, when the platform has
    /// no insets, as on desktop.
    /// </summary>
    public static bool Set(TopLevel topLevel, bool ionic)
    {
        if (topLevel.InsetsManager is not { } insets || topLevel.Content is not Control view)
        {
            return false;
        }

        var resources = Application.Current!.Resources;
        if (ionic && s_insets is null)
        {
            s_insets = insets;
            s_edgeToEdgeBefore = insets.DisplayEdgeToEdgePreference;
            TopLevel.SetAutoSafeAreaPadding(view, false);
            insets.DisplayEdgeToEdgePreference = true;
            insets.SafeAreaChanged += OnSafeAreaChanged;
            Fill(resources, insets.SafeAreaPadding);
        }
        else if (!ionic && s_insets is { } before)
        {
            before.SafeAreaChanged -= OnSafeAreaChanged;
            before.DisplayEdgeToEdgePreference = s_edgeToEdgeBefore;
            s_insets = null;
            TopLevel.SetAutoSafeAreaPadding(view, true);
            Clear(resources);
        }

        return true;
    }

    private static void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => Fill(Application.Current!.Resources, e.SafeAreaPadding);

    /// <summary>Sets the four keys, in the application's resources, so that they beat the theme's zeros.</summary>
    public static void Fill(IResourceDictionary resources, Thickness padding)
    {
        resources[TopKey] = padding.Top;
        resources[BottomKey] = padding.Bottom;
        resources[LeftKey] = padding.Left;
        resources[RightKey] = padding.Right;
    }

    /// <summary>Takes the four keys out again, so that the theme's zeros apply.</summary>
    public static void Clear(IResourceDictionary resources)
    {
        resources.Remove(TopKey);
        resources.Remove(BottomKey);
        resources.Remove(LeftKey);
        resources.Remove(RightKey);
    }

    /// <summary>
    /// Pads the shell's <paramref name="header"/> by the top, left and right keys, and <paramref name="bottom"/> by the bottom one, as
    /// long as the result isn't disposed. They're 0 unless Ionic's way is on. The page between isn't padded, as <c>ion-content</c>
    /// isn't: items pad their own sides.
    /// </summary>
    public static IDisposable PadShell(Control shell, Border header, Control bottom)
    {
        double top = 0, left = 0, right = 0, below = 0;
        var baseMargin = bottom.Margin;

        void Apply()
        {
            header.Padding = new Thickness(left, top, right, 0);
            bottom.Margin = new Thickness(baseMargin.Left + left, baseMargin.Top, baseMargin.Right + right, baseMargin.Bottom + below);
        }

        IDisposable Watch(string key, Action<double> set) =>
            shell.GetResourceObservable(key).Subscribe(new Observer(value =>
            {
                set(value is double d ? d : 0);
                Apply();
            }));

        var subscriptions = new[]
        {
            Watch(TopKey, v => top = v),
            Watch(LeftKey, v => left = v),
            Watch(RightKey, v => right = v),
            Watch(BottomKey, v => below = v),
        };

        return new Unsubscribe(() =>
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }

            header.Padding = default;
            bottom.Margin = baseMargin;
        });
    }

    private sealed class Observer(Action<object?> next) : IObserver<object?>
    {
        public void OnNext(object? value) => next(value);

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }

    private sealed class Unsubscribe(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
