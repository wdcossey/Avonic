using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Avonic.Components.Tests.TestApp))]

namespace Avonic.Components.Tests;

public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
