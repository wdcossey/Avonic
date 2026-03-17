using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Tests.Controls;

public class AvonicCheckboxTests
{
    // ── Default state ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IsChecked_False_By_Default()
    {
        var cb = new AvonicCheckbox();
        Assert.False(cb.IsChecked);
    }

    [AvaloniaFact]
    public void IsIndeterminate_False_By_Default()
    {
        var cb = new AvonicCheckbox();
        Assert.False(cb.IsIndeterminate);
    }

    // ── Pseudoclasses ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IsChecked_True_Sets_Checked_Pseudoclass()
    {
        var cb     = new AvonicCheckbox { IsChecked = true };
        var window = new Window { Content = cb };
        window.Show();

        Assert.Contains(":checked", cb.Classes);
    }

    [AvaloniaFact]
    public void IsChecked_False_Clears_Checked_Pseudoclass()
    {
        var cb     = new AvonicCheckbox { IsChecked = false };
        var window = new Window { Content = cb };
        window.Show();

        Assert.DoesNotContain(":checked", cb.Classes);
    }

    [AvaloniaFact]
    public void IsIndeterminate_True_Sets_Indeterminate_Pseudoclass()
    {
        var cb     = new AvonicCheckbox { IsIndeterminate = true };
        var window = new Window { Content = cb };
        window.Show();

        Assert.Contains(":indeterminate", cb.Classes);
    }

    [AvaloniaFact]
    public void LabelPlacement_End_Sets_LabelEnd_Pseudoclass_By_Default()
    {
        var cb     = new AvonicCheckbox();
        var window = new Window { Content = cb };
        window.Show();

        Assert.Contains(":label-end", cb.Classes);
        Assert.DoesNotContain(":label-start",   cb.Classes);
        Assert.DoesNotContain(":label-stacked", cb.Classes);
    }

    [AvaloniaFact]
    public void LabelPlacement_Start_Sets_LabelStart_Pseudoclass()
    {
        var cb     = new AvonicCheckbox { LabelPlacement = LabelPlacement.Start };
        var window = new Window { Content = cb };
        window.Show();

        Assert.Contains(":label-start", cb.Classes);
        Assert.DoesNotContain(":label-end",     cb.Classes);
        Assert.DoesNotContain(":label-stacked", cb.Classes);
    }

    [AvaloniaFact]
    public void LabelPlacement_Stacked_Sets_LabelStacked_Pseudoclass()
    {
        var cb     = new AvonicCheckbox { LabelPlacement = LabelPlacement.Stacked };
        var window = new Window { Content = cb };
        window.Show();

        Assert.Contains(":label-stacked", cb.Classes);
        Assert.DoesNotContain(":label-end",   cb.Classes);
        Assert.DoesNotContain(":label-start", cb.Classes);
    }

    // ── Toggle behaviour ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Click_Toggles_IsChecked()
    {
        var cb     = new AvonicCheckbox { Content = "Check me" };
        var window = new Window { Content = cb, Width = 200, Height = 100 };
        window.Show();

        Assert.False(cb.IsChecked);

        window.MouseDown(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);

        Assert.True(cb.IsChecked);
    }

    [AvaloniaFact]
    public void Click_Again_Untoggles_IsChecked()
    {
        var cb     = new AvonicCheckbox { Content = "Check me", IsChecked = true };
        var window = new Window { Content = cb, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);

        Assert.False(cb.IsChecked);
    }

    [AvaloniaFact]
    public void Click_Raises_CheckedChanged_Event()
    {
        var cb     = new AvonicCheckbox { Content = "Check me" };
        var window = new Window { Content = cb, Width = 200, Height = 100 };
        window.Show();

        CheckedChangedEventArgs? received = null;
        cb.CheckedChanged += (_, e) => received = e;

        window.MouseDown(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);

        Assert.NotNull(received);
        Assert.True(received.IsChecked);
    }

    [AvaloniaFact]
    public void Indeterminate_Click_Clears_Indeterminate_And_Checks()
    {
        var cb = new AvonicCheckbox { Content = "Check me", IsIndeterminate = true };
        var window = new Window { Content = cb, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);

        Assert.True(cb.IsChecked);
        Assert.False(cb.IsIndeterminate);
    }

    // ── Disabled ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Disabled_Does_Not_Toggle()
    {
        var cb     = new AvonicCheckbox { Content = "Check me", IsEnabled = false };
        var window = new Window { Content = cb, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(24, 50), Avalonia.Input.MouseButton.Left);

        Assert.False(cb.IsChecked);
    }

    // ── MinimumTouchTarget ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void MinimumTouchTarget_Enforced()
    {
        var cb = new AvonicCheckbox
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Top,
        };
        var window = new Window { Content = cb, Width = 200, Height = 200 };
        window.Show();

        Assert.True(cb.Bounds.Width  >= 48);
        Assert.True(cb.Bounds.Height >= 48);
    }
}
