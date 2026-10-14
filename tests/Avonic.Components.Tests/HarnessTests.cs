using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace Avonic.Components.Tests;

/// <summary>
/// Proves the headless test harness itself works. Every control's behaviour tests
/// build on this; if this fails, nothing else is meaningful.
/// </summary>
public class HarnessTests
{
    [AvaloniaFact]
    public void Headless_Window_Opens_And_Lays_Out()
    {
        var window = new Window { Width = 390, Height = 844, Content = new Border() };
        window.Show();

        Assert.True(window.IsVisible);
        Assert.Equal(390, window.Bounds.Width);
    }
}
