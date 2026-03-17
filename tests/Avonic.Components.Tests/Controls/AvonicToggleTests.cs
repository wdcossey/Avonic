using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicToggleTests
{
    // ── Default state ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IsChecked_False_By_Default()
    {
        var toggle = new AvonicToggle();
        Assert.False(toggle.IsChecked);
    }

    // ── Pseudoclasses ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IsChecked_True_Sets_Checked_Pseudoclass()
    {
        var toggle = new AvonicToggle { IsChecked = true };
        var window = new Window { Content = toggle };
        window.Show();

        Assert.Contains(":checked", toggle.Classes);
    }

    [AvaloniaFact]
    public void IsChecked_False_Clears_Checked_Pseudoclass()
    {
        var toggle = new AvonicToggle { IsChecked = false };
        var window = new Window { Content = toggle };
        window.Show();

        Assert.DoesNotContain(":checked", toggle.Classes);
    }

    [AvaloniaFact]
    public void LabelPlacement_End_By_Default()
    {
        var toggle = new AvonicToggle();
        var window = new Window { Content = toggle };
        window.Show();

        Assert.Contains(":label-end", toggle.Classes);
    }

    // ── Toggle behaviour ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Tap_Toggles_IsChecked()
    {
        var toggle = new AvonicToggle { Content = "Wi-Fi" };
        var window = new Window { Content = toggle, Width = 200, Height = 100 };
        window.Show();

        Assert.False(toggle.IsChecked);

        window.MouseDown(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);

        Assert.True(toggle.IsChecked);
    }

    [AvaloniaFact]
    public void Tap_Again_Untoggles()
    {
        var toggle = new AvonicToggle { Content = "Wi-Fi", IsChecked = true };
        var window = new Window { Content = toggle, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);

        Assert.False(toggle.IsChecked);
    }

    [AvaloniaFact]
    public void Tap_Raises_CheckedChanged()
    {
        var toggle = new AvonicToggle { Content = "Wi-Fi" };
        var window = new Window { Content = toggle, Width = 200, Height = 100 };
        window.Show();

        CheckedChangedEventArgs? received = null;
        toggle.CheckedChanged += (_, e) => received = e;

        window.MouseDown(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);

        Assert.NotNull(received);
        Assert.True(received.IsChecked);
    }

    // ── Disabled ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Disabled_Does_Not_Toggle()
    {
        var toggle = new AvonicToggle { Content = "Wi-Fi", IsEnabled = false };
        var window = new Window { Content = toggle, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(26, 50), Avalonia.Input.MouseButton.Left);

        Assert.False(toggle.IsChecked);
    }

    // ── MinimumTouchTarget ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void MinimumTouchTarget_Enforced()
    {
        var toggle = new AvonicToggle
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Top,
        };
        var window = new Window { Content = toggle, Width = 200, Height = 200 };
        window.Show();

        Assert.True(toggle.Bounds.Width  >= 48);
        Assert.True(toggle.Bounds.Height >= 48);
    }
}
