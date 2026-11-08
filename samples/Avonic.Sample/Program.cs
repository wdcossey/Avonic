using System.Diagnostics;
using Avalonia;

namespace Avonic.Sample;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        EnableDiagnostics();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    [Conditional("DEBUG")]
    private static void EnableDiagnostics() => Diagnostics.PerformanceMonitor.EnableDiagnostics();

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithRoboto()
            .LogToTrace();
}
