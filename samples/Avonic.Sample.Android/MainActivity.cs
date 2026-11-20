using Android.App;
using Android.Content.PM;
using Avalonia.Android;
#if AVONIC_PERF
using Android.AccessibilityServices;
using Android.Content;
using Android.OS;
using Android.Views.Accessibility;
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

        SampleShell.PageCostsPlatform = Accessibility;
        SampleShell.PageCostsForeground = Foreground;
        base.OnCreate(savedInstanceState);
    }

    private static volatile bool s_resumed;

    protected override void OnResume()
    {
        base.OnResume();
        s_resumed = true;
    }

    protected override void OnPause()
    {
        s_resumed = false;
        base.OnPause();
    }

    /// <summary>
    /// Whether the app is in front, as a run needs it: the screen interactive, no keyguard, the activity resumed. On battery a
    /// phone's screen locks after its timeout whatever <c>svc power stayon</c> says, and Android then moves the app to the
    /// <c>/background</c> cpuset, the little cores: every page grew 7 to 10 times slower in #258's runs. Null when all is well,
    /// else what isn't.
    /// </summary>
    private static string? Foreground()
    {
        var problems = new List<string>();
        if (Application.Context.GetSystemService(Context.PowerService) is PowerManager power && !power.IsInteractive)
        {
            problems.Add("screen off");
        }

        if (Application.Context.GetSystemService(Context.KeyguardService) is KeyguardManager keyguard && keyguard.IsKeyguardLocked)
        {
            problems.Add("keyguard locked");
        }

        if (!s_resumed)
        {
            problems.Add("activity not resumed");
        }

        return problems.Count == 0 ? null : string.Join(", ", problems);
    }

    /// <summary>
    /// Whether accessibility is on, and which services: with it on, a service's queries of Avalonia's accessibility bridge run on
    /// the UI thread, between frames, and can leave the app no idle time. From the application's context, so that the static it's
    /// kept in holds no activity.
    /// </summary>
    private static string Accessibility()
    {
        if (Application.Context.GetSystemService(Context.AccessibilityService) is not AccessibilityManager manager)
        {
            return "accessibility unknown";
        }

        var services = manager.GetEnabledAccessibilityServiceList(FeedbackFlags.AllMask)?.Select(s => s.Id).ToArray() ?? [];
        return $"accessibility {(manager.IsEnabled ? "on" : "off")}, touch exploration {(manager.IsTouchExplorationEnabled ? "on" : "off")}, services: {(services.Length == 0 ? "none" : string.Join(" ", services))}";
    }
#endif
}
