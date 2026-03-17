using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicContentTests
{
    // ── Pseudoclasses ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ScrollY_True_By_Default_Sets_ScrollY_Pseudoclass()
    {
        var content = new AvonicContent();
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.Contains(":scroll-y", content.Classes);
    }

    [AvaloniaFact]
    public void ScrollY_False_Removes_ScrollY_Pseudoclass()
    {
        var content = new AvonicContent { ScrollY = false };
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.DoesNotContain(":scroll-y", content.Classes);
    }

    [AvaloniaFact]
    public void ScrollX_False_By_Default_Does_Not_Set_ScrollX_Pseudoclass()
    {
        var content = new AvonicContent();
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.DoesNotContain(":scroll-x", content.Classes);
    }

    [AvaloniaFact]
    public void ScrollX_True_Sets_ScrollX_Pseudoclass()
    {
        var content = new AvonicContent { ScrollX = true };
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.Contains(":scroll-x", content.Classes);
    }

    [AvaloniaFact]
    public void Fullscreen_False_By_Default_Does_Not_Set_Fullscreen_Pseudoclass()
    {
        var content = new AvonicContent();
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.DoesNotContain(":fullscreen", content.Classes);
    }

    [AvaloniaFact]
    public void Fullscreen_True_Sets_Fullscreen_Pseudoclass()
    {
        var content = new AvonicContent { Fullscreen = true };
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.Contains(":fullscreen", content.Classes);
    }

    // ── Property changes after attach ─────────────────────────────────────────

    [AvaloniaFact]
    public void Setting_ScrollY_After_Attach_Updates_Pseudoclass()
    {
        var content = new AvonicContent();
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        content.ScrollY = false;

        Assert.DoesNotContain(":scroll-y", content.Classes);
    }

    [AvaloniaFact]
    public void Setting_ScrollX_After_Attach_Updates_Pseudoclass()
    {
        var content = new AvonicContent();
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        content.ScrollX = true;

        Assert.Contains(":scroll-x", content.Classes);
    }

    // ── ContentPadding ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ContentPadding_Can_Be_Set_Manually()
    {
        var content = new AvonicContent { ContentPadding = new Avalonia.Thickness(0, 44, 0, 34) };
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.Equal(new Avalonia.Thickness(0, 44, 0, 34), content.ContentPadding);
    }

    // ── FixedContent ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void FixedContent_Can_Be_Set()
    {
        var fab = new Button { Content = "+" };
        var content = new AvonicContent { FixedContent = fab };
        var window = new Window { Content = content, Width = 390, Height = 844 };
        window.Show();

        Assert.Equal(fab, content.FixedContent);
    }
}
