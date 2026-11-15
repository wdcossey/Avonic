using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Layout;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-input-otp</c>'s <c>.native-input</c>: one box, a <see cref="TextBox"/> that <see cref="AvonicInputOtp"/> makes and
/// handles, as Ionic's handlers do its <c>&lt;input&gt;</c>. Its look is its <c>ControlTheme</c>'s, as a <c>ControlTheme</c>'s
/// <c>/template/</c> rules can't reach what a control makes in code, so the host copies its state here as pseudoclasses: its
/// <c>fill</c>, <c>shape</c> and <c>size</c>, <c>:has-focus</c>, <c>:readonly</c>, <c>:ion-color</c>, and the app's
/// <c>ion-valid</c>, <c>ion-invalid</c> and <c>ion-touched</c>. Its own <c>:focus</c> and <c>:disabled</c> are Avalonia's.
/// </summary>
[PseudoClasses(":fill-outline", ":fill-solid", ":shape-rectangular", ":shape-round", ":shape-soft")]
[PseudoClasses(":size-large", ":size-medium", ":size-small", ":has-focus", ":readonly", ":ion-color")]
[PseudoClasses(":ion-valid", ":ion-invalid", ":ion-touched")]
public sealed class InputOtpBox : TextBox
{
    static InputOtpBox()
    {
        // The group's align-items: center, which a FlexRow leaves to each child. A default, not the theme's, so an app's value wins.
        VerticalAlignmentProperty.OverrideDefaultValue<InputOtpBox>(VerticalAlignment.Center);
    }

    internal InputOtpBox(int index)
    {
        Index = index;

        // Chromium's history goes when a script sets the value, which Ionic does after every edit.
        IsUndoEnabled = false;

        // aria-label and the box's place in the accessibility tree, from the host, at style priority, bound before any style
        // reaches the box, so that an app's style for them wins.
        Bind(AutomationProperties.NameProperty, AutomationName, BindingPriority.Style);
        Bind(AutomationProperties.AccessibilityViewProperty, AutomationView, BindingPriority.Style);
    }

    /// <summary>Its place in the row, from 0.</summary>
    internal int Index { get; }

    /// <summary><c>aria-label</c>: <c>Input 1 of 4</c>.</summary>
    internal StyleValue<string?> AutomationName { get; } = new();

    /// <summary>Raw for an empty box that isn't the tab stop, which a screen reader skips, as Ionic's <c>aria-hidden</c>.</summary>
    internal StyleValue<AccessibilityView> AutomationView { get; } = new();

    internal void SetState(string pseudoClass, bool on) => PseudoClasses.Set(pseudoClass, on);
}
