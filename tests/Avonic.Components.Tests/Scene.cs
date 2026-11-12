using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
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
        : this(content, width, height, [])
    {
    }

    private Scene(Control content, double width, double height, IStyle[] styles)
    {
        Window = new Window { Width = width, Height = height, Content = content };
        Window.Styles.AddRange(styles);
        Window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// A scene with an app's own styles, there before the window is shown, as an app's are. A value a control gives at style
    /// priority when a state turns on later must still lose to them, as an app's rule beats a shadow root's in CSS.
    /// </summary>
    public static Scene Styled(Control content, params IStyle[] styles) => new(content, 390, 844, styles);

    /// <summary>An app's plain style, with no class or pseudoclass: <c>T { property: value }</c>.</summary>
    public static Style AppStyle<T>(AvaloniaProperty property, object? value)
        where T : StyledElement =>
        new(x => x.OfType<T>()) { Setters = { new Setter(property, value) } };

    public void Dispose()
    {
        Window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}
