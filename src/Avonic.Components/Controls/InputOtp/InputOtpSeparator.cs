using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-input-otp</c>'s <c>.input-otp-separator</c>: the dot between two boxes, which <see cref="AvonicInputOtp"/> makes and
/// shows where <c>separators</c> says. Its look is its <c>ControlTheme</c>'s, as a <c>ControlTheme</c>'s <c>/template/</c> rules
/// can't reach what a control makes in code.
/// </summary>
public sealed class InputOtpSeparator : Border
{
    internal InputOtpSeparator()
    {
    }

    protected override Type StyleKeyOverride => typeof(InputOtpSeparator);
}
