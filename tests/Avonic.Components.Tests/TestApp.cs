using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avonic.Components.Tests.TestApp))]

namespace Avonic.Components.Tests;

/// <summary>Headless application used by every <c>[AvaloniaFact]</c> test.</summary>
public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
