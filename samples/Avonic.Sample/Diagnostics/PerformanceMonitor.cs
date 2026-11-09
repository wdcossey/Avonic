using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// The line along the bottom of the sample in a Debug build: what a page cost to open, from Avalonia's own meter. It adds
/// nothing to the library, and it doesn't trace, because tracing would cost more than what it measures.
/// </summary>
public sealed class PerformanceMonitor : IDisposable
{
    private const string Meter = "Avalonia.Diagnostic.Meter";
    public const string LayoutMeasure = "avalonia.ui.measure.time";
    public const string LayoutArrange = "avalonia.ui.arrange.time";
    public const string Render = "avalonia.ui.render.time";

    private readonly MeterListener _listener = new();
    private readonly object _gate = new();
    private double _layout;
    private double _render;
    private long _started;
    private (int Rules, int Alternatives)? _census;

    /// <summary>Avalonia's switch for its diagnostics, off by default.</summary>
    public const string DiagnosticsSwitch = "Avalonia.Diagnostics.Diagnostic.IsEnabled";

    /// <summary>Avalonia reads the switch once, so call this before it starts.</summary>
    public static void EnableDiagnostics() => AppContext.SetSwitch(DiagnosticsSwitch, true);

    public PerformanceMonitor()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == Meter && instrument.Name is LayoutMeasure or LayoutArrange or Render)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => Record(instrument.Name, value));
        _listener.Start();
    }

    /// <summary>A time from Avalonia's meter, in milliseconds.</summary>
    public void Record(string instrument, double milliseconds)
    {
        lock (_gate)
        {
            if (instrument == Render)
            {
                _render += milliseconds;
            }
            else
            {
                _layout += milliseconds;
            }
        }
    }

    /// <summary>Call this before making a page, because styles apply as soon as it's attached.</summary>
    public void Begin()
    {
        lock (_gate)
        {
            _layout = 0;
            _render = 0;
        }

        _started = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Call this once the page is in. <paramref name="report"/> gets the line after it's been laid out, unless it's gone by
    /// then.
    /// </summary>
    public void Report(string title, Control page, Action<string> report)
    {
        var started = _started;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (TopLevel.GetTopLevel(page) is null)
                {
                    return;
                }

                var elapsed = Stopwatch.GetElapsedTime(started);
                var (rules, alternatives) = _census ??= StyleCensus.Count(Application.Current!.Styles);
                double layout;
                double render;
                lock (_gate)
                {
                    layout = _layout;
                    render = _render;
                }

                report(Format(title, elapsed.TotalMilliseconds, page.GetVisualDescendants().Count() + 1, rules, alternatives, layout, render));
            },
            DispatcherPriority.Background);
    }

    /// <summary>The line.</summary>
    public static string Format(string title, double openMs, int elements, int rules, int alternatives, double layoutMs, double renderMs) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{title}: {openMs:0} ms, {elements} elements, {rules} rules ({alternatives}), layout {layoutMs:0.0}, render {renderMs:0.0}");

    public void Dispose() => _listener.Dispose();
}
