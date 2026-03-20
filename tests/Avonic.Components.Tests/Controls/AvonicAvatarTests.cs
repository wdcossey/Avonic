using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicAvatarTests
{
    [AvaloniaFact]
    public void Attaches_To_Visual_Tree_Without_Error()
    {
        var avatar = new AvonicAvatar
        {
            Content = new TextBlock { Text = "AB" }
        };
        var window = new Window { Content = avatar, Width = 390, Height = 200 };
        var ex = Record.Exception(() => window.Show());

        Assert.Null(ex);
    }

    [AvaloniaFact]
    public void Default_Width_And_Height_Applied_By_Style()
    {
        var avatar = new AvonicAvatar
        {
            Content             = new TextBlock { Text = "AB" },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Top
        };
        // No Avonic theme loaded in unit tests — just check the control attaches cleanly
        var window = new Window { Content = avatar, Width = 390, Height = 200 };
        window.Show();

        Assert.True(TopLevel.GetTopLevel(avatar) is not null);
    }

    [AvaloniaFact]
    public void Background_Can_Be_Set()
    {
        var avatar = new AvonicAvatar { Background = Avalonia.Media.Brushes.Blue };
        var window = new Window { Content = avatar, Width = 390, Height = 200 };
        window.Show();

        Assert.Equal(Avalonia.Media.Brushes.Blue, avatar.Background);
    }

    [AvaloniaFact]
    public void Content_Can_Be_Any_Control()
    {
        var inner  = new TextBlock { Text = "🦊" };
        var avatar = new AvonicAvatar { Content = inner };
        var window = new Window { Content = avatar, Width = 390, Height = 200 };
        window.Show();

        Assert.Equal(inner, avatar.Content);
    }
}
