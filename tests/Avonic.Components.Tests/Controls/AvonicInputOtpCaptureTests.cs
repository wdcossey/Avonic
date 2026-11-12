using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicInputOtpCaptureTests
{
    /// <summary>The same content as the snippet at the top of docs/components/ion-input-otp/COMPARISON.md.</summary>
    internal static StackPanel Page()
    {
        var invalid = new AvonicInputOtp { Value = "12", Content = "Invalid code" };
        invalid.Classes.Add(AvonicInputOtp.TouchedClass);
        invalid.Classes.Add(AvonicInputOtp.InvalidClass);

        return new StackPanel
        {
            Margin = new Thickness(16, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Children =
            {
                new AvonicInputOtp { Value = "12" },
                new AvonicInputOtp { Fill = InputOtpFill.Solid, Value = "12", Content = "Didn't get a code? Resend" },
                new AvonicInputOtp { Length = 6, Separators = "3", Shape = InputOtpShape.Soft, Size = InputOtpSize.Small, Value = "123" },
                new AvonicInputOtp { Shape = InputOtpShape.Rectangular, Size = InputOtpSize.Large, Type = InputOtpType.Text, Value = "AB" },
                new AvonicInputOtp { Color = "danger", Value = "1" },
                invalid,
                new AvonicInputOtp { IsEnabled = false, Value = "12" },
                new AvonicInputOtp { Readonly = true, Fill = InputOtpFill.Solid, Value = "12" },
                new AvonicInputOtp { Length = 6, Separators = "all", Value = "1234" },
            },
        };
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var page = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-input-otp-page.png");

        // Measured: 760 high, as Ionic's nine hosts are.
        Assert.Equal(760, page.Bounds.Height, 0.6);
    });

    [AvaloniaFact]
    public void Focused() => InstalledTheme.Run(_ =>
    {
        var otp = new AvonicInputOtp { Value = "12" };
        var page = new StackPanel { Margin = new Thickness(16, 0), Children = { otp } };
        using var scene = new Scene(page, 390, 90);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        otp.SetFocus(0);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-input-otp-focused.png");

        Assert.True(otp.IsKeyboardFocusWithin);
    });

    private static void Save(Scene scene, string name)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Frames.Save(frame!, name);
    }
}
