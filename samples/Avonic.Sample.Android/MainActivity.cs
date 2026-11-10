using Android.App;
using Android.Content.PM;
using Avalonia.Android;

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
public class MainActivity : AvaloniaMainActivity;
