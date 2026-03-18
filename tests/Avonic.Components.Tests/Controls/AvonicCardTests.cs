using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicCardTests
{
    // ── Button pseudoclass ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Button_False_By_Default_Does_Not_Set_Button_Pseudoclass()
    {
        var card = new AvonicCard { Content = "Content" };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":button", card.Classes);
    }

    [AvaloniaFact]
    public void Button_True_Sets_Button_Pseudoclass()
    {
        var card = new AvonicCard { Content = "Content", Button = true };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":button", card.Classes);
    }

    [AvaloniaFact]
    public void Button_Changing_To_True_Updates_Pseudoclass()
    {
        var card = new AvonicCard { Content = "Content" };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        card.Button = true;

        Assert.Contains(":button", card.Classes);
    }

    [AvaloniaFact]
    public void Button_Changing_To_False_Clears_Pseudoclass()
    {
        var card = new AvonicCard { Content = "Content", Button = true };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        card.Button = false;

        Assert.DoesNotContain(":button", card.Classes);
    }

    // ── Click event ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Button_True_Tap_Raises_Click_Event()
    {
        var card = new AvonicCard
        {
            Content = "Click me",
            Button  = true,
            Width   = 200,
            Height  = 80
        };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        var clicked = false;
        card.Click += (_, _) => clicked = true;

        window.MouseDown(new Avalonia.Point(100, 40), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(100, 40), Avalonia.Input.MouseButton.Left);

        Assert.True(clicked);
    }

    [AvaloniaFact]
    public void Button_False_Tap_Does_Not_Raise_Click_Event()
    {
        var card = new AvonicCard
        {
            Content = "Static",
            Button  = false,
            Width   = 200,
            Height  = 80
        };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        var clicked = false;
        card.Click += (_, _) => clicked = true;

        window.MouseDown(new Avalonia.Point(100, 40), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(100, 40), Avalonia.Input.MouseButton.Left);

        Assert.False(clicked);
    }

    [AvaloniaFact]
    public void Disabled_Card_Does_Not_Raise_Click()
    {
        var card = new AvonicCard
        {
            Content   = "Disabled",
            Button    = true,
            IsEnabled = false,
            Width     = 200,
            Height    = 80
        };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        var clicked = false;
        card.Click += (_, _) => clicked = true;

        window.MouseDown(new Avalonia.Point(100, 40), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(100, 40), Avalonia.Input.MouseButton.Left);

        Assert.False(clicked);
    }

    // ── Content ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Content_Can_Be_Set()
    {
        var card = new AvonicCard { Content = "Hello" };
        var window = new Window { Content = card, Width = 390, Height = 200 };
        window.Show();

        Assert.Equal("Hello", card.Content);
    }
}
