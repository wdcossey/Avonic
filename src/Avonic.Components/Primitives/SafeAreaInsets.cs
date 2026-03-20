using Avalonia.Controls.Platform;

namespace Avonic.Components.Primitives;

/// <summary>
/// Attached properties that expose platform safe area insets (notch, home indicator,
/// status bar) as individual double values on any <see cref="AvaloniaObject"/>.
/// </summary>
/// <remarks>
/// <para>
/// Backed by Avalonia's <see cref="IInsetsManager"/> on mobile platforms.
/// On desktop, all values default to 0.
/// </para>
/// <para>
/// To wire platform values automatically, call <see cref="Attach"/> with the
/// application's <see cref="TopLevel"/> (typically in <c>UseAvonic()</c>).
/// Values can also be set manually for testing or layout overrides.
/// </para>
/// </remarks>
public sealed class SafeAreaInsets
{
    private SafeAreaInsets() { }

    // ── Attached Properties ──────────────────────────────────────────────────

    public static readonly AttachedProperty<double> TopProperty =
        AvaloniaProperty.RegisterAttached<SafeAreaInsets, AvaloniaObject, double>("Top");

    public static readonly AttachedProperty<double> BottomProperty =
        AvaloniaProperty.RegisterAttached<SafeAreaInsets, AvaloniaObject, double>("Bottom");

    public static readonly AttachedProperty<double> LeftProperty =
        AvaloniaProperty.RegisterAttached<SafeAreaInsets, AvaloniaObject, double>("Left");

    public static readonly AttachedProperty<double> RightProperty =
        AvaloniaProperty.RegisterAttached<SafeAreaInsets, AvaloniaObject, double>("Right");

    // ── Getters / Setters ────────────────────────────────────────────────────

    public static double GetTop(AvaloniaObject element)    => element.GetValue(TopProperty);
    public static void   SetTop(AvaloniaObject element, double value) => element.SetValue(TopProperty, value);

    public static double GetBottom(AvaloniaObject element)    => element.GetValue(BottomProperty);
    public static void   SetBottom(AvaloniaObject element, double value) => element.SetValue(BottomProperty, value);

    public static double GetLeft(AvaloniaObject element)    => element.GetValue(LeftProperty);
    public static void   SetLeft(AvaloniaObject element, double value) => element.SetValue(LeftProperty, value);

    public static double GetRight(AvaloniaObject element)    => element.GetValue(RightProperty);
    public static void   SetRight(AvaloniaObject element, double value) => element.SetValue(RightProperty, value);

    /// <summary>
    /// Returns all four inset values as a <see cref="Thickness"/>.
    /// </summary>
    public static Thickness GetInsets(AvaloniaObject element) =>
        new(GetLeft(element), GetTop(element), GetRight(element), GetBottom(element));

    /// <summary>
    /// Sets all four inset values from a <see cref="Thickness"/>.
    /// </summary>
    public static void SetInsets(AvaloniaObject element, Thickness insets)
    {
        SetLeft(element, insets.Left);
        SetTop(element, insets.Top);
        SetRight(element, insets.Right);
        SetBottom(element, insets.Bottom);
    }

    // ── Platform Bridge ──────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to <see cref="IInsetsManager.SafeAreaChanged"/> on the given
    /// <paramref name="topLevel"/> and keeps the attached properties on
    /// <paramref name="target"/> in sync with the platform safe area.
    /// Also applies the current safe area immediately.
    /// </summary>
    /// <param name="topLevel">The window or top-level to observe.</param>
    /// <param name="target">
    /// The object on which to set the inset properties.
    /// Defaults to <paramref name="topLevel"/> itself when <see langword="null"/>.
    /// </param>
    public static void Attach(TopLevel topLevel, AvaloniaObject? target = null)
    {
        target ??= topLevel;

        var insetsManager = topLevel.InsetsManager;
        if (insetsManager is null)
            return;

        Apply(insetsManager.SafeAreaPadding, target);

        insetsManager.SafeAreaChanged += (_, args) => Apply(args.SafeAreaPadding, target);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private static void Apply(Thickness padding, AvaloniaObject target) =>
        SetInsets(target, padding);
}
