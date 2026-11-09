using System.Diagnostics;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Avonic.Sample.Android;

/// <summary>Builds the app, as the desktop head's <c>Program</c> does: Roboto, and Avalonia's diagnostics in a Debug build.</summary>
[Application]
public class SampleApplication(nint javaReference, JniHandleOwnership transfer) : AvaloniaAndroidApplication<App>(javaReference, transfer)
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        EnableDiagnostics();
        return base.CustomizeAppBuilder(builder).WithRoboto().LogToTrace();
    }

    [Conditional("DEBUG")]
    private static void EnableDiagnostics() => Diagnostics.PerformanceMonitor.EnableDiagnostics();
}
