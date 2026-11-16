using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-app</c>: the root of an app, which starts what a native app needs from Ionic: the hardware back button
/// (<see cref="HardwareBackButton"/>) and the keyboard's events (<see cref="KeyboardAssist"/>), for the <see cref="TopLevel"/>
/// it's in. See <c>docs/components/ion-app/README.md</c>.
/// </summary>
/// <remarks>
/// No control needs it: each works without it, as an overlay still dismisses on back. Tap-click and focus-visible, which
/// <c>ion-app</c> also starts, are each control's own here, through <see cref="Behaviors.Activatable"/> and
/// <see cref="Behaviors.FocusVisible"/>. It fills <c>Avonic.SafeArea*</c> while Avalonia doesn't pad the root by the insets
/// (<see cref="TopLevel.AutoSafeAreaPadding"/> off), as a page's <c>viewport-fit=cover</c> gives <c>env(safe-area-inset-*)</c>,
/// and its theme gives what's in it a page's text: 16, Avonic's font and text colour, on Avonic's background. It has the class <c>ion-page</c>, as <c>ion-app</c>'s host does, so an
/// <see cref="AvonicContent"/> takes it as its page.
/// </remarks>
public class AvonicApp : ContentControl
{
    /// <summary>The class <c>ion-app</c>'s <c>render()</c> gives its host.</summary>
    public const string PageClass = "ion-page";

    private readonly List<IDisposable> _started = [];

    public AvonicApp()
    {
        Classes.Add(PageClass);
    }

    /// <summary>
    /// Ionic <c>setFocus(elements)</c>: shows the focus ring, <c>:ion-focused</c>, on exactly <paramref name="elements"/>, as when
    /// focus reaches them by keyboard, and takes it off whatever had it. It doesn't move focus. For focus moved by code as the
    /// result of a user's action other than a key, such as a popover focusing its first element when it's presented: focus moved
    /// by a key shows the ring by itself. It lasts until a press, a key that doesn't move focus, focus moving by keyboard, or
    /// focus leaving everything. Nothing while the app isn't in a <see cref="TopLevel"/>, as Ionic's does nothing before it's
    /// started focus-visible.
    /// </summary>
    /// <param name="elements">The elements to show it on; none takes it off everything.</param>
    public void SetFocus(IEnumerable<Control> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            Behaviors.FocusVisible.SetFocus(topLevel, elements.ToList());
        }
    }

    /// <inheritdoc cref="SetFocus(IEnumerable{Control})"/>
    public void SetFocus(params Control[] elements) => SetFocus((IEnumerable<Control>)elements);

    /// <summary>
    /// <c>componentDidLoad</c>: the utilities start for the top level, each as its config says then. Ionic starts them once and
    /// never stops them, as <c>ion-app</c> stays for the page's life; here an app can be taken out, or moved to another top level,
    /// so they stop when it's detached and start again when it's attached.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        if (AvonicConfig.HardwareBackButton)
        {
            _started.Add(Controls.HardwareBackButton.Start(topLevel));
        }

        _started.Add(KeyboardAssist.Start(topLevel));
        _started.Add(AppSafeArea.Start(topLevel));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        foreach (var started in _started)
        {
            started.Dispose();
        }

        _started.Clear();
    }
}
