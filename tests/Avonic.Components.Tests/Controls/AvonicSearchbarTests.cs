using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicSearchbarTests
{
    // ── Initial state ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Text_Null_By_Default()
    {
        var bar = new AvonicSearchbar();

        Assert.Null(bar.Text);
    }

    [AvaloniaFact]
    public void ShowCancelButton_False_By_Default()
    {
        var bar = new AvonicSearchbar();

        Assert.False(bar.ShowCancelButton);
    }

    [AvaloniaFact]
    public void CancelButtonText_Defaults_To_Cancel()
    {
        var bar = new AvonicSearchbar();

        Assert.Equal("Cancel", bar.CancelButtonText);
    }

    [AvaloniaFact]
    public void Placeholder_Defaults_To_Search()
    {
        var bar = new AvonicSearchbar();

        Assert.Equal("Search", bar.Placeholder);
    }

    [AvaloniaFact]
    public void Debounce_Defaults_To_250()
    {
        var bar = new AvonicSearchbar();

        Assert.Equal(250, bar.Debounce);
    }

    // ── Pseudoclasses ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Empty_Text_Does_Not_Set_HasValue_Pseudoclass()
    {
        var bar    = new AvonicSearchbar { Text = null };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":has-value", bar.Classes);
    }

    [AvaloniaFact]
    public void Non_Empty_Text_Sets_HasValue_Pseudoclass()
    {
        var bar    = new AvonicSearchbar { Text = "hello" };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":has-value", bar.Classes);
    }

    [AvaloniaFact]
    public void ShowCancelButton_True_Sets_ShowCancel_Pseudoclass()
    {
        var bar    = new AvonicSearchbar { ShowCancelButton = true };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":show-cancel", bar.Classes);
    }

    [AvaloniaFact]
    public void ShowCancelButton_False_Does_Not_Set_ShowCancel_Pseudoclass()
    {
        var bar    = new AvonicSearchbar { ShowCancelButton = false };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":show-cancel", bar.Classes);
    }

    // ── Text property ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Setting_Text_Updates_Has_Value_Pseudoclass()
    {
        var bar    = new AvonicSearchbar();
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        bar.Text = "query";

        Assert.Contains(":has-value", bar.Classes);
    }

    [AvaloniaFact]
    public void Clearing_Text_Removes_HasValue_Pseudoclass()
    {
        var bar    = new AvonicSearchbar { Text = "hello" };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        bar.Text = string.Empty;

        Assert.DoesNotContain(":has-value", bar.Classes);
    }

    // ── ShowCancelButton toggle ───────────────────────────────────────────────

    [AvaloniaFact]
    public void ShowCancelButton_Changing_Updates_Pseudoclass()
    {
        var bar    = new AvonicSearchbar();
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        bar.ShowCancelButton = true;
        Assert.Contains(":show-cancel", bar.Classes);

        bar.ShowCancelButton = false;
        Assert.DoesNotContain(":show-cancel", bar.Classes);
    }

    // ── Attaches cleanly ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Attaches_To_Visual_Tree_Without_Error()
    {
        var bar    = new AvonicSearchbar { Placeholder = "Search…" };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        var ex = Record.Exception(() => window.Show());

        Assert.Null(ex);
    }
}
