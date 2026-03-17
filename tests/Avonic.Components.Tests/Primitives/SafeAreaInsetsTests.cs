using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Primitives;

namespace Avonic.Components.Tests.Primitives;

public class SafeAreaInsetsTests
{
    [AvaloniaFact]
    public void Default_Values_Are_Zero()
    {
        var control = new Border();

        Assert.Equal(0, SafeAreaInsets.GetTop(control));
        Assert.Equal(0, SafeAreaInsets.GetBottom(control));
        Assert.Equal(0, SafeAreaInsets.GetLeft(control));
        Assert.Equal(0, SafeAreaInsets.GetRight(control));
    }

    [AvaloniaFact]
    public void GetInsets_Returns_Zero_Thickness_By_Default()
    {
        var control = new Border();

        Assert.Equal(new Thickness(0), SafeAreaInsets.GetInsets(control));
    }

    [AvaloniaFact]
    public void SetTop_Roundtrips()
    {
        var control = new Border();
        SafeAreaInsets.SetTop(control, 44.0);

        Assert.Equal(44.0, SafeAreaInsets.GetTop(control));
    }

    [AvaloniaFact]
    public void SetBottom_Roundtrips()
    {
        var control = new Border();
        SafeAreaInsets.SetBottom(control, 34.0);

        Assert.Equal(34.0, SafeAreaInsets.GetBottom(control));
    }

    [AvaloniaFact]
    public void SetLeft_And_SetRight_Roundtrip()
    {
        var control = new Border();
        SafeAreaInsets.SetLeft(control, 8.0);
        SafeAreaInsets.SetRight(control, 16.0);

        Assert.Equal(8.0,  SafeAreaInsets.GetLeft(control));
        Assert.Equal(16.0, SafeAreaInsets.GetRight(control));
    }

    [AvaloniaFact]
    public void SetInsets_Sets_All_Four_Values()
    {
        var control = new Border();
        SafeAreaInsets.SetInsets(control, new Thickness(8, 44, 16, 34));

        Assert.Equal(8,  SafeAreaInsets.GetLeft(control));
        Assert.Equal(44, SafeAreaInsets.GetTop(control));
        Assert.Equal(16, SafeAreaInsets.GetRight(control));
        Assert.Equal(34, SafeAreaInsets.GetBottom(control));
    }

    [AvaloniaFact]
    public void GetInsets_Returns_Correct_Thickness()
    {
        var control = new Border();
        SafeAreaInsets.SetInsets(control, new Thickness(8, 44, 16, 34));

        var insets = SafeAreaInsets.GetInsets(control);

        Assert.Equal(new Thickness(8, 44, 16, 34), insets);
    }

    [AvaloniaFact]
    public void Properties_Are_Isolated_Between_Controls()
    {
        var a = new Border();
        var b = new Border();

        SafeAreaInsets.SetTop(a, 44.0);

        Assert.Equal(44.0, SafeAreaInsets.GetTop(a));
        Assert.Equal(0.0,  SafeAreaInsets.GetTop(b));
    }

    [AvaloniaFact]
    public void Attach_With_No_InsetsManager_Does_Not_Throw()
    {
        // Headless TopLevel has no real IInsetsManager — Attach should be a no-op
        var window = new Window();
        window.Show();

        var ex = Record.Exception(() => SafeAreaInsets.Attach(window));

        Assert.Null(ex);
    }
}
