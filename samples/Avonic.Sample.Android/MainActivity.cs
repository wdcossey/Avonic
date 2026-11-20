using Android.App;
using Android.Content.PM;
using Avalonia.Android;
#if AVONIC_PERF
using Android.OS;
#endif

namespace Avonic.Sample.Android;

/// <summary>
/// The sample's only activity. It declares the configuration changes that would otherwise recreate it, so that rotating, resizing,
/// a change of display size or a keyboard keeps its view, the shell the app's MainViewFactory made, on the page it's showing. Its Java name is fixed, for <c>adb shell am start -n</c>. See
/// docs/infra/android/PLAN.md.
/// </summary>
[Activity(
    Name = "io.github.wdcossey.avonic.sample.MainActivity",
    Label = "Avonic",
    Theme = "@style/Avonic.Theme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Density
                           | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public class MainActivity : AvaloniaMainActivity
{
#if AVONIC_PERF
    /// <summary>
    /// In a <c>-p:AvonicPerf=true</c> build, the pages the page-cost rounds open, from the intent's <c>AVONIC_PERF_ONLY</c> extra
    /// (<c>adb shell am start ... -e AVONIC_PERF_ONLY datetime</c>), before the base makes the shell, which starts them.
    /// docs/infra/performance/README.md, "Page costs on a device".
    /// </summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        if (Intent?.GetStringExtra(Diagnostics.PageCostSet.OnlyVariable) is { Length: > 0 } only)
        {
            SampleShell.PageCostsOnly = only;
        }

        base.OnCreate(savedInstanceState);
    }
#endif
}
