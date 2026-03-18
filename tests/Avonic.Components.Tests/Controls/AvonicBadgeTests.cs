using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicBadgeTests
{
    [AvaloniaFact]
    public void Content_Can_Be_Set_To_Number()
    {
        var badge = new AvonicBadge { Content = "3" };
        var window = new Window { Content = badge, Width = 390, Height = 200 };
        window.Show();

        Assert.Equal("3", badge.Content);
    }

    [AvaloniaFact]
    public void Content_Can_Be_Set_To_Text()
    {
        var badge = new AvonicBadge { Content = "New" };
        var window = new Window { Content = badge, Width = 390, Height = 200 };
        window.Show();

        Assert.Equal("New", badge.Content);
    }

    [AvaloniaFact]
    public void Attaches_To_Visual_Tree_Without_Error()
    {
        var badge = new AvonicBadge { Content = "1" };
        var window = new Window { Content = badge, Width = 390, Height = 200 };
        var ex = Record.Exception(() => window.Show());

        Assert.Null(ex);
    }

    [AvaloniaFact]
    public void Background_Can_Be_Overridden()
    {
        var badge = new AvonicBadge
        {
            Content    = "99+",
            Background = Avalonia.Media.Brushes.Red
        };
        var window = new Window { Content = badge, Width = 390, Height = 200 };
        window.Show();

        Assert.Equal(Avalonia.Media.Brushes.Red, badge.Background);
    }
}
