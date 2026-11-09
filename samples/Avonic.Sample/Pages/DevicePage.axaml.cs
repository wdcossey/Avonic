using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.VisualTree;
using Avonic.Sample.Diagnostics;

namespace Avonic.Sample.Pages;

/// <summary>
/// What to try on a phone (#66), not a control: the screen, the safe area and the switch to Ionic's way of filling it, and the style
/// benchmark. It works on desktop too, where there are no insets. See docs/infra/android/README.md.
/// </summary>
[SamplePage("device", Order = 66)]
public partial class DevicePage : UserControl
{
    /// <summary>What a benchmark row starts with in the log, to find it there.</summary>
    public const string LogPrefix = "AvonicBenchmark";

    /// <summary>Makes the benchmark for a host: the table's counts, unless a test makes it smaller.</summary>
    public static Func<Panel, StyleCostRunner> MakeBenchmark { get; set; } = host => new StyleCostRunner(host);

    private TopLevel? _topLevel;
    private IInsetsManager? _insets;
    private CancellationTokenSource? _benchmark;

    public DevicePage()
    {
        InitializeComponent();
        IonicSafeArea.IsChecked = SafeAreaMode.IsIonic;
        IonicSafeArea.IsCheckedChanged += (_, _) =>
        {
            if (_topLevel is { } top && !SafeAreaMode.Set(top, IonicSafeArea.IsChecked == true))
            {
                IonicSafeArea.IsChecked = false;
            }

            ShowInsets();
        };
        RunBenchmark.Click += async (_, _) => await Benchmark();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _insets = _topLevel?.InsetsManager;
        if (_insets is not null)
        {
            _insets.SafeAreaChanged += OnSafeAreaChanged;
        }

        if (_topLevel is not null)
        {
            _topLevel.PropertyChanged += OnTopLevelChanged;
        }

        ShowScreen();
        ShowInsets();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_insets is not null)
        {
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
        }

        if (_topLevel is not null)
        {
            _topLevel.PropertyChanged -= OnTopLevelChanged;
        }

        _insets = null;
        _topLevel = null;
        // Leaving the page stops a run, with the theme back.
        _benchmark?.Cancel();
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => ShowInsets();

    private void OnTopLevelChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
        {
            ShowScreen();
        }
    }

    private void ShowScreen()
    {
        var size = _topLevel?.ClientSize ?? default;
        var scale = _topLevel?.RenderScaling ?? 1;
        var build =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        Screen.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"{RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}. {size.Width:0} × {size.Height:0} at {scale:0.##}×. A {build} build.");
    }

    private void ShowInsets()
    {
        Insets.Text = _insets is { } insets
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Insets {Format(insets.SafeAreaPadding)}. {(SafeAreaMode.IsIonic ? "Ionic's way: the keys are filled, and the root isn't padded." : "Avalonia's way: the root is padded, and the keys are 0.")}")
            : "No insets on this platform, so the switch does nothing.";

        static string Format(Thickness t) => string.Create(CultureInfo.InvariantCulture, $"top {t.Top:0.#}, bottom {t.Bottom:0.#}, left {t.Left:0.#}, right {t.Right:0.#}");
    }

    private async Task Benchmark()
    {
        // The shell's host, outside Avonic's controls, so that a tree's ancestors are the same with and without the theme.
        if (this.FindAncestorOfType<SampleShell>() is not { } shell)
        {
            BenchmarkStatus.Text = "Stopped: the benchmark runs from the sample's own list, in its shell.";
            return;
        }

        RunBenchmark.IsEnabled = false;
        BenchmarkRow.IsVisible = false;
        using var cancel = _benchmark = new CancellationTokenSource();
        try
        {
            var row = await MakeBenchmark(shell.BenchmarkArea).Run(new Progress<string>(text => BenchmarkStatus.Text = text), cancel.Token);
            var caveat = StyleCostRunner.WhyNotForTheTable();
            BenchmarkStatus.Text = caveat is null
                ? $"Done. The row is from this runner on {RuntimeInformation.OSDescription}: compare it with this runner on desktop."
                : $"Done, but not for the table: {caveat}.";
            BenchmarkRow.Text = row;
            BenchmarkRow.IsVisible = true;
            Console.WriteLine($"{LogPrefix} {RuntimeInformation.OSDescription} {(caveat is null ? "" : $"(not for the table: {caveat}) ")}{row}");
        }
        catch (OperationCanceledException)
        {
            BenchmarkStatus.Text = "Stopped: the page was left.";
        }
        catch (Exception error)
        {
            // Shown and logged rather than thrown: an exception out of an async click would end the app on a phone.
            BenchmarkStatus.Text = $"Stopped: {error.GetType().Name}: {error.Message}";
            Console.WriteLine($"{LogPrefix} stopped: {error}");
        }
        finally
        {
            _benchmark = null;
            RunBenchmark.IsEnabled = true;
        }
    }
}
