using Avalonia.Controls;
using Avalonia.Threading;

namespace Avonic.Components.Tests;

/// <summary>
/// A shown headless window that is closed when the test ends, so controls detach, their timers
/// stop and nothing leaks into the next test on the shared UI thread.
/// </summary>
internal sealed class Scene : IDisposable
{
    public Window Window { get; }

    public Scene(Control content, double width = 390, double height = 844)
    {
        Window = new Window { Width = width, Height = height, Content = content };
        Window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        Window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}
