using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The COMPARE step's evidence generator. Renders the same scene as the Ionic reference
/// (`tools/ionic-ref/render.js ion-ripple-effect --tap`: a 240×96 activatable surface, #E0E0E0,
/// primary #3880FF, 2× scale), presses it with real pointer input, and captures frames at the
/// same offsets from press and from release as the reference. Frames are written only when
/// AVONIC_CAPTURE_DIR is set; the assertions run regardless.
/// </summary>
public class AvonicRippleEffectCaptureTests
{
    private const int Pad = 8;                     // the reference clip adds 8 px around the element
    private const double W = 240, H = 96;

    private static readonly Color Primary = Color.Parse("#3880FF");
    private static readonly Color Surface = Color.Parse("#E0E0E0");

    [AvaloniaFact]
    public void Press_And_Release_Frames_Match_The_Reference_Offsets()
    {
        using var clock = new TestClock();

        var ripple = new AvonicRippleEffect { Foreground = new SolidColorBrush(Primary) };
        var host = new Border
        {
            Width = W, Height = H, Margin = new Thickness(Pad),
            Background = new SolidColorBrush(Surface),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Child = new Panel
            {
                Background = Brushes.Transparent,
                Children =
                {
                    ripple,
                    new TextBlock
                    {
                        Text = "Tap me", Foreground = new SolidColorBrush(Primary), FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    },
                },
            },
        };
        Activatable.SetIsEnabled(host, true);

        using var scene = new Scene(host, W + 2 * Pad, H + 2 * Pad);
        var window = scene.Window;
        window.Background = Brushes.White;
        window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        var outDir = Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR");
        if (outDir is not null) Directory.CreateDirectory(outDir);

        WriteableBitmap Capture(string name)
        {
            ripple.Tick();
            Dispatcher.UIThread.RunJobs();
            var frame = window.CaptureRenderedFrame()!;
            if (outDir is not null) frame.Save(Path.Combine(outDir, name + ".png"), PngBitmapEncoderOptions.Default);
            return frame;
        }

        var baseline = Capture("avonic-baseline");
        var centre   = new Point(Pad + W / 2, Pad + H / 2);

        // press at the centre (mouse.down); the ripple starts 100 ms later, as in Ionic
        window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
        var press = new Dictionary<int, WriteableBitmap>();
        foreach (var ms in new[] { 0, 60, 160, 320 })
        {
            clock.At(ms);
            press[ms] = Capture($"avonic-press-{ms:000}ms");
        }

        // release ~330 ms after press (the reference released right after its 320 ms frame)
        const int ReleasedAt = 330;
        clock.At(ReleasedAt);
        window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
        var released = new Dictionary<int, WriteableBitmap>();
        foreach (var ms in new[] { 60, 220, 420 })
        {
            clock.At(ReleasedAt + ms);
            released[ms] = Capture($"avonic-release-{ms:000}ms");
        }

        // Nothing before the 100 ms defer; a growing circle; full coverage; gone after removal.
        Assert.False(Differs(baseline, press[0]),    "nothing should render at press");
        Assert.False(Differs(baseline, press[60]),   "nothing should render before the 100 ms defer");
        Assert.True(Differs(baseline, press[160]),   "ripple should be visible at 160 ms");
        Assert.True(Differs(press[160], press[320]), "ripple should keep expanding between 160 and 320 ms");
        Assert.False(Differs(baseline, released[420]), "ripple should be gone 420 ms after release");

        // At 160 ms (60 ms in) the circle has not reached the surface's left edge yet…
        AssertClose(Surface, PixelAt(press[160], (Pad + 4) * 2, (int)((Pad + H / 2) * 2)), tolerance: 1);
        // …and at 320 ms (220 ms in) it tints the whole surface: 16 % primary over #E0E0E0.
        AssertClose(Blend(Surface, Primary, RippleVisual.MaxOpacity), PixelAt(press[320], (Pad + 4) * 2, (int)((Pad + H / 2) * 2)), tolerance: 3);
    }

    // ── pixel helpers ────────────────────────────────────────────────────────────────────────

    private static unsafe Color PixelAt(WriteableBitmap bitmap, int x, int y)
    {
        using var fb = bitmap.Lock();
        var p = (byte*)fb.Address + y * fb.RowBytes + x * 4;
        return fb.Format == Avalonia.Platform.PixelFormats.Rgba8888
            ? Color.FromArgb(p[3], p[0], p[1], p[2])
            : Color.FromArgb(p[3], p[2], p[1], p[0]);      // Bgra8888
    }

    private static unsafe bool Differs(WriteableBitmap a, WriteableBitmap b)
    {
        using var fa = a.Lock();
        using var fb = b.Lock();
        var bytes = fa.RowBytes * fa.Size.Height;
        return !new ReadOnlySpan<byte>((void*)fa.Address, bytes).SequenceEqual(new ReadOnlySpan<byte>((void*)fb.Address, bytes));
    }

    private static Color Blend(Color under, Color over, double alpha) => Color.FromRgb(
        (byte)Math.Round(under.R + (over.R - under.R) * alpha),
        (byte)Math.Round(under.G + (over.G - under.G) * alpha),
        (byte)Math.Round(under.B + (over.B - under.B) * alpha));

    private static void AssertClose(Color expected, Color actual, int tolerance)
    {
        Assert.True(Math.Abs(expected.R - actual.R) <= tolerance
                 && Math.Abs(expected.G - actual.G) <= tolerance
                 && Math.Abs(expected.B - actual.B) <= tolerance,
            $"expected ≈ {expected} but was {actual}");
    }
}
