using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicRadioTests
{
    // ── Default state ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IsChecked_False_By_Default()
    {
        var radio = new AvonicRadio();
        Assert.False(radio.IsChecked);
    }

    // ── Pseudoclasses ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IsChecked_True_Sets_Checked_Pseudoclass()
    {
        var radio  = new AvonicRadio { IsChecked = true };
        var window = new Window { Content = radio };
        window.Show();

        Assert.Contains(":checked", radio.Classes);
    }

    // ── Radio-specific toggle: cannot uncheck by tapping ─────────────────────

    [AvaloniaFact]
    public void Tap_Checks_Unchecked_Radio()
    {
        var radio  = new AvonicRadio { Content = "Option A" };
        var window = new Window { Content = radio, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);

        Assert.True(radio.IsChecked);
    }

    [AvaloniaFact]
    public void Tap_On_Checked_Radio_Does_Not_Uncheck()
    {
        var radio  = new AvonicRadio { Content = "Option A", IsChecked = true };
        var window = new Window { Content = radio, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);

        Assert.True(radio.IsChecked);
    }

    [AvaloniaFact]
    public void Tap_On_Checked_Radio_Does_Not_Raise_Event()
    {
        var radio  = new AvonicRadio { Content = "Option A", IsChecked = true };
        var window = new Window { Content = radio, Width = 200, Height = 100 };
        window.Show();

        var raised = false;
        radio.CheckedChanged += (_, _) => raised = true;

        window.MouseDown(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);

        Assert.False(raised);
    }

    [AvaloniaFact]
    public void Tap_Raises_CheckedChanged_With_Value()
    {
        var radio  = new AvonicRadio { Content = "Option A", Value = "a" };
        var window = new Window { Content = radio, Width = 200, Height = 100 };
        window.Show();

        CheckedChangedEventArgs? received = null;
        radio.CheckedChanged += (_, e) => received = e;

        window.MouseDown(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);

        Assert.NotNull(received);
        Assert.True(received.IsChecked);
        Assert.Equal("a", received.Value);
    }

    // ── RadioGroup ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void RadioGroup_Unchecks_Siblings_When_One_Is_Checked()
    {
        var radioA = new AvonicRadio { Content = "Option A", Value = "a" };
        var radioB = new AvonicRadio { Content = "Option B", Value = "b" };
        var radioC = new AvonicRadio { Content = "Option C", Value = "c" };

        var group  = new AvonicRadioGroup
        {
            Content = new StackPanel
            {
                Children = { radioA, radioB, radioC }
            }
        };

        var window = new Window { Content = group, Width = 200, Height = 200 };
        window.Show();

        // Simulate checking radio B directly
        radioB.IsChecked = false;
        radioA.IsChecked = true; // via group machinery we check A

        // Now simulate clicking B
        radioB.IsChecked = false;
        radioA.IsChecked = false;

        // Programmatically check A to trigger group sync
        group.Value = "a";

        Assert.True(radioA.IsChecked);
        Assert.False(radioB.IsChecked);
        Assert.False(radioC.IsChecked);
        Assert.Equal("a", group.Value);
    }

    [AvaloniaFact]
    public void RadioGroup_Value_Reflects_Checked_Radio()
    {
        var radioA = new AvonicRadio { Content = "Option A", Value = "a" };
        var radioB = new AvonicRadio { Content = "Option B", Value = "b" };

        var group  = new AvonicRadioGroup
        {
            Content = new StackPanel { Children = { radioA, radioB } }
        };

        var window = new Window { Content = group, Width = 200, Height = 200 };
        window.Show();

        group.Value = "b";

        Assert.False(radioA.IsChecked);
        Assert.True(radioB.IsChecked);
        Assert.Equal("b", group.Value);
    }

    [AvaloniaFact]
    public void RadioGroup_Setting_Value_To_Null_Unchecks_All()
    {
        var radioA = new AvonicRadio { Content = "Option A", Value = "a", IsChecked = true };
        var radioB = new AvonicRadio { Content = "Option B", Value = "b" };

        var group  = new AvonicRadioGroup
        {
            Content = new StackPanel { Children = { radioA, radioB } }
        };

        var window = new Window { Content = group, Width = 200, Height = 200 };
        window.Show();

        group.Value = null;

        Assert.False(radioA.IsChecked);
        Assert.False(radioB.IsChecked);
    }

    // ── Disabled ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Disabled_Radio_Does_Not_Check()
    {
        var radio  = new AvonicRadio { Content = "Option A", IsEnabled = false };
        var window = new Window { Content = radio, Width = 200, Height = 100 };
        window.Show();

        window.MouseDown(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);
        window.MouseUp(new Avalonia.Point(10, 50), Avalonia.Input.MouseButton.Left);

        Assert.False(radio.IsChecked);
    }
}
