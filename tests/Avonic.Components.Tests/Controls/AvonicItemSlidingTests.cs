using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Tests.Controls;

public class AvonicItemSlidingTests
{
    // ── Initial state ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void No_Open_Pseudoclass_By_Default()
    {
        var sliding = new AvonicItemSliding { Content = new AvonicItem { Content = "Item" } };
        var window  = new Window { Content = sliding, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":open-start", sliding.Classes);
        Assert.DoesNotContain(":open-end",   sliding.Classes);
        Assert.DoesNotContain(":dragging",   sliding.Classes);
    }

    // ── Close / Open API ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Open_End_Sets_OpenEnd_Pseudoclass()
    {
        var options = new AvonicItemOptions { Side = SlideSide.End };
        options.Items.Add(new AvonicItemOption { Content = "Delete" });

        var sliding = new AvonicItemSliding
        {
            Content    = new AvonicItem { Content = "Item" },
            EndOptions = options
        };
        var window = new Window { Content = sliding, Width = 390, Height = 200 };
        window.Show();

        sliding.Open(SlideSide.End);

        Assert.Contains(":open-end",   sliding.Classes);
        Assert.DoesNotContain(":open-start", sliding.Classes);
    }

    [AvaloniaFact]
    public void Open_Start_Sets_OpenStart_Pseudoclass()
    {
        var options = new AvonicItemOptions { Side = SlideSide.Start };
        options.Items.Add(new AvonicItemOption { Content = "Fav" });

        var sliding = new AvonicItemSliding
        {
            Content      = new AvonicItem { Content = "Item" },
            StartOptions = options
        };
        var window = new Window { Content = sliding, Width = 390, Height = 200 };
        window.Show();

        sliding.Open(SlideSide.Start);

        Assert.Contains(":open-start", sliding.Classes);
        Assert.DoesNotContain(":open-end", sliding.Classes);
    }

    [AvaloniaFact]
    public void Close_Clears_Open_Pseudoclasses()
    {
        var options = new AvonicItemOptions { Side = SlideSide.End };
        options.Items.Add(new AvonicItemOption { Content = "Delete" });

        var sliding = new AvonicItemSliding
        {
            Content    = new AvonicItem { Content = "Item" },
            EndOptions = options
        };
        var window = new Window { Content = sliding, Width = 390, Height = 200 };
        window.Show();

        sliding.Open(SlideSide.End);
        sliding.Close();

        Assert.DoesNotContain(":open-start", sliding.Classes);
        Assert.DoesNotContain(":open-end",   sliding.Classes);
    }

    // ── Drag event ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Drag_Raises_Drag_Event_During_Mouse_Move()
    {
        var sliding = new AvonicItemSliding
        {
            Content = new AvonicItem { Content = "Item" },
            Width   = 320,
            Height  = 56
        };
        var window = new Window { Content = sliding, Width = 390, Height = 200 };
        window.Show();

        var dragRaised = false;
        sliding.Drag += (_, _) => dragRaised = true;

        window.MouseDown(new Avalonia.Point(160, 28), Avalonia.Input.MouseButton.Left);
        window.MouseMove(new Avalonia.Point(110, 28));

        Assert.True(dragRaised);
    }

    // ── AvonicItemOptions ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ItemOptions_Side_End_By_Default_Sets_SideEnd_Pseudoclass()
    {
        var options = new AvonicItemOptions();
        var window  = new Window { Content = options, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":side-end",   options.Classes);
        Assert.DoesNotContain(":side-start", options.Classes);
    }

    [AvaloniaFact]
    public void ItemOptions_Side_Start_Sets_SideStart_Pseudoclass()
    {
        var options = new AvonicItemOptions { Side = SlideSide.Start };
        var window  = new Window { Content = options, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":side-start", options.Classes);
        Assert.DoesNotContain(":side-end", options.Classes);
    }

    // ── AvonicItemOption ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ItemOption_Color_Default_Sets_ColorDefault_Pseudoclass()
    {
        var option = new AvonicItemOption { Content = "Delete" };
        var window = new Window { Content = option, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":color-default", option.Classes);
    }

    [AvaloniaFact]
    public void ItemOption_Color_Danger_Sets_ColorDanger_Pseudoclass()
    {
        var option = new AvonicItemOption { Content = "Delete", Color = ItemOptionColor.Danger };
        var window = new Window { Content = option, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":color-danger", option.Classes);
        Assert.DoesNotContain(":color-default", option.Classes);
    }

    [AvaloniaFact]
    public void ItemOption_Color_Success_Sets_ColorSuccess_Pseudoclass()
    {
        var option = new AvonicItemOption { Content = "Fav", Color = ItemOptionColor.Success };
        var window = new Window { Content = option, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":color-success", option.Classes);
    }

    [AvaloniaFact]
    public void ItemOption_IconOnly_Sets_IconOnly_Pseudoclass()
    {
        var option = new AvonicItemOption { IconOnly = true };
        var window = new Window { Content = option, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":icon-only", option.Classes);
    }

    [AvaloniaFact]
    public void ItemOption_Raises_Pressed_Event_On_Tap()
    {
        var option = new AvonicItemOption
        {
            Content = "Action",
            Width   = 80,
            Height  = 56
        };
        var window = new Window { Content = option, Width = 390, Height = 200 };
        window.Show();

        var raised = false;
        option.Pressed += (_, _) => raised = true;

        window.MouseDown(new Avalonia.Point(40, 28), Avalonia.Input.MouseButton.Left);

        Assert.True(raised);
    }
}
