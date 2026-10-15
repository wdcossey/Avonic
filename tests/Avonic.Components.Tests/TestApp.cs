using Avalonia;
using Avalonia.Headless;
using Avonic.Components;

[assembly: AvaloniaTestApplication(typeof(Avonic.Components.Tests.TestApp))]

namespace Avonic.Components.Tests;

/// <summary>
/// Headless application used by every <c>[AvaloniaFact]</c> test. Renders with Skia so tests
/// can capture real frames for the compare step; loads the Components stub templates only.
/// </summary>
public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

    public override void Initialize()
    {
        Styles.Add(new AvonicComponentsStyles());
    }
}
