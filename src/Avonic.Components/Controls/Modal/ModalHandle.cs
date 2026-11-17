using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The sheet modal's <c>.modal-handle</c>, its <c>handle</c> part: a native <c>&lt;button&gt;</c> (a click on release, Enter and
/// Space, through <see cref="NativeButton"/>), named as Ionic's <c>aria-label</c> for automation. Its <c>::before</c> adds a
/// 4 px tap area all round, which is its hit area here. <see cref="AvonicModal"/> listens to its click.
/// </summary>
public class ModalHandle : Border, ICustomHitTest
{
    /// <summary>Ionic's <c>aria-label</c> on the handle.</summary>
    public const string DefaultName = "Activate to adjust the size of the dialog overlaying the screen";

    /// <summary>The <c>::before</c>'s padding: the tap area around the handle.</summary>
    internal const double TapArea = 4;

    static ModalHandle()
    {
        FocusableProperty.OverrideDefaultValue<ModalHandle>(true);
        AutomationProperties.NameProperty.OverrideDefaultValue<ModalHandle>(DefaultName);
    }

    public ModalHandle()
    {
        _ = new NativeButton(this, _ => Click?.Invoke(this, EventArgs.Empty), static () => true, static () => { });
    }

    /// <summary>The native button's <c>onClick</c>.</summary>
    internal event EventHandler? Click;

    /// <summary>The handle's box and the 4 px around it.</summary>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Inflate(TapArea).Contains(point);

    protected override AutomationPeer OnCreateAutomationPeer() => new ModalHandleAutomationPeer(this);

    private sealed class ModalHandleAutomationPeer(ModalHandle owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;

        public void Invoke()
        {
            if (Owner.IsEffectivelyEnabled)
            {
                ((ModalHandle)Owner).Click?.Invoke(Owner, EventArgs.Empty);
            }
        }
    }
}

/// <summary>
/// Ionic's <c>.ion-page</c> around a modal's content: <c>position: absolute</c> at the top of the wrapper, as tall as the wrapper up to
/// its child's <c>MaxHeight</c>, which the sheet's <c>contentAnimation</c> sets when <c>expandToScroll</c> is off. A
/// <see cref="Avalonia.Layout.Layoutable"/> held below its slot by a maximum would be centred in it; this keeps it at the top.
/// </summary>
public class ModalPagePanel : Decorator
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            child.Arrange(new Rect(0, 0, finalSize.Width, Math.Min(finalSize.Height, child.MaxHeight)));
        }

        return finalSize;
    }
}
