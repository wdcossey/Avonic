using Avalonia;
using Avalonia.Threading;
using Avonic.Themes;

namespace Avonic.Components.Tests;

/// <summary>
/// Runs a test with <see cref="AvonicTheme"/> in <c>Application.Styles</c>, which is how templates
/// really resolve tokens. <c>TestApp</c> loads only the stubs.
/// </summary>
internal static class InstalledTheme
{
    public static void Run(Action<AvonicTheme> test)
    {
        var app = Application.Current!;
        var theme = new AvonicTheme();
        app.Styles.Add(theme);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test(theme);
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }
}
