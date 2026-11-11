using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The flex line of <c>.checkbox-wrapper</c>, <c>.radio-wrapper</c> and <c>.toggle-wrapper</c>, on its own: a 300-wide panel, a
/// label 100 wide and a control's part 20 by 20. Each control's own tests hold it to Chromium's boxes through its theme.
/// </summary>
public class LabelledControlPanelTests
{
    private static (LabelledControlPanel Panel, Border Label, Border Native, Scene Scene) Line(
        LabelledControlPlacement placement = LabelledControlPlacement.Start,
        LabelledControlJustify? justify = null,
        double labelWidth = 100,
        double width = 300,
        Action<LabelledControlPanel>? setUp = null)
    {
        var label = new Border { Width = labelWidth, Height = 10 };
        var native = new Border { Width = 20, Height = 20 };
        var panel = new LabelledControlPanel
        {
            LabelPlacement = placement,
            Justify = justify,
            Width = width,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Children = { label, native },
        };
        setUp?.Invoke(panel);
        var scene = new Scene(panel);
        return (panel, label, native, scene);
    }

    [AvaloniaFact]
    public void A_Row_Is_Space_Between_With_The_Label_First_And_Centred_Across()
    {
        var (_, label, native, scene) = Line();
        using var _ = scene;

        Assert.Equal(new Rect(0, 5, 100, 10), label.Bounds);
        Assert.Equal(new Rect(280, 0, 20, 20), native.Bounds);
    }

    [AvaloniaFact]
    public void End_Reverses_The_Row_And_Packs_It_At_The_Start_Unless_End_Justify_Says_Otherwise()
    {
        var (_, label, native, scene) = Line(LabelledControlPlacement.End);
        using (scene)
        {
            Assert.Equal(0, native.Bounds.X);
            Assert.Equal(20, label.Bounds.X);
        }

        // A radio's end rule only reverses the row, so it stays space-between.
        var (_, radioLabel, radioNative, radioScene) = Line(LabelledControlPlacement.End,
            setUp: p => p.EndJustify = LabelledControlJustify.SpaceBetween);
        using (radioScene)
        {
            Assert.Equal(0, radioNative.Bounds.X);
            Assert.Equal(200, radioLabel.Bounds.X);
        }
    }

    [AvaloniaFact]
    public void Justify_Start_And_End_Are_The_Lines_Own_In_Either_Direction()
    {
        var (_, label, native, scene) = Line(justify: LabelledControlJustify.End);
        using (scene)
        {
            Assert.Equal(180, label.Bounds.X);
            Assert.Equal(280, native.Bounds.X);
        }

        var (_, endLabel, endNative, endScene) = Line(LabelledControlPlacement.End, LabelledControlJustify.End);
        using (endScene)
        {
            Assert.Equal(180, endNative.Bounds.X);
            Assert.Equal(200, endLabel.Bounds.X);
        }
    }

    // A hidden label is display: none: space-between with one item is flex-start, which in row-reverse is the right.
    [AvaloniaFact]
    public void A_Hidden_Label_Takes_No_Part()
    {
        var (_, _, native, scene) = Line(setUp: p => p.Children[0].IsVisible = false);
        using (scene)
        {
            Assert.Equal(0, native.Bounds.X);
        }

        var (_, _, endNative, endScene) = Line(LabelledControlPlacement.End,
            setUp: p => { p.Children[0].IsVisible = false; p.EndJustify = LabelledControlJustify.SpaceBetween; });
        using (endScene)
        {
            Assert.Equal(280, endNative.Bounds.X);
        }
    }

    // overflow: hidden takes the label's minimum away, so it shrinks to what the part leaves; a fixed label is flex: 0 0 100px.
    [AvaloniaFact]
    public void A_Label_Shrinks_To_What_The_Part_Leaves_Unless_It_Is_Fixed()
    {
        var (panel, _, native, scene) = Line(labelWidth: double.NaN, width: 120, setUp: p => p.Children[0] = new TextBlock
        {
            Text = "A label much longer than the line it is on",
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
        });
        using (scene)
        {
            var text = panel.Children[0];
            Assert.Equal(100, text.Bounds.Width, 0.5);
            Assert.Equal(100, native.Bounds.X, 0.5);
        }

        var (_, fixedLabel, fixedNative, fixedScene) = Line(LabelledControlPlacement.Fixed, width: 100);
        using (fixedScene)
        {
            Assert.Equal(100, fixedLabel.Bounds.Width);
            Assert.Equal(100, fixedNative.Bounds.X);
        }
    }

    // With no room, space-between falls back to flex-start: the main start, which is the right for row-reverse.
    [AvaloniaFact]
    public void Space_Between_With_No_Room_Packs_At_The_Main_Start()
    {
        var (_, label, native, scene) = Line(LabelledControlPlacement.Fixed, width: 100);
        using (scene)
        {
            Assert.Equal(0, label.Bounds.X);
            Assert.Equal(100, native.Bounds.X);
        }
    }

    // max-width: calc(100% / 0.75) on a stacked label, the scale being its Css.Scale, which md's theme sets.
    [AvaloniaFact]
    public void A_Stacked_Label_Can_Be_Wider_Than_The_Line_By_Its_Scale()
    {
        var (panel, _, native, scene) = Line(LabelledControlPlacement.Stacked, labelWidth: double.NaN, width: 150,
            setUp: p => p.Children[0] = LongLine(0.75));
        using (scene)
        {
            var wide = panel.Children[0];
            Assert.Equal(200, wide.Bounds.Width, 0.5);
            Assert.Equal(-25, wide.Bounds.X, 0.5);
            Assert.Equal(new Point(65, wide.Bounds.Height), native.Bounds.Position);
        }

        var (halfPanel, _, _, halfScene) = Line(LabelledControlPlacement.Stacked, labelWidth: double.NaN, width: 150,
            setUp: p => p.Children[0] = LongLine(0.5));
        using (halfScene)
        {
            Assert.Equal(300, halfPanel.Children[0].Bounds.Width, 0.5);
        }
    }

    // A template that doesn't scale the label, such as the functional stub, keeps it to the line.
    [AvaloniaFact]
    public void A_Stacked_Label_That_Isnt_Scaled_Is_No_Wider_Than_The_Line()
    {
        var (panel, _, _, scene) = Line(LabelledControlPlacement.Stacked, labelWidth: double.NaN, width: 150,
            setUp: p => p.Children[0] = LongLine());
        using (scene)
        {
            Assert.Equal(new Rect(0, 0, 150, panel.Children[0].Bounds.Height), panel.Children[0].Bounds);
        }
    }

    // A style that scales the label later, as a change of placement does, lays the line out again.
    [AvaloniaFact]
    public void A_Label_Scaled_Later_Is_Laid_Out_Again()
    {
        var (panel, _, _, scene) = Line(LabelledControlPlacement.Stacked, labelWidth: double.NaN, width: 150,
            setUp: p => p.Children[0] = LongLine());
        using (scene)
        {
            panel.Children[0].SetValue(Css.ScaleProperty, 0.75);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            scene.Window.UpdateLayout();

            Assert.Equal(200, panel.Children[0].Bounds.Width, 0.5);
        }
    }

    private static TextBlock LongLine(double? scale = null)
    {
        var text = new TextBlock
        {
            Text = "A stacked label much longer than the line it is on, and longer than a third more",
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
        };
        if (scale is { } value)
        {
            text.SetValue(Css.ScaleProperty, value);
        }

        return text;
    }

    [AvaloniaFact]
    public void A_Controls_Own_Enum_Maps_By_Name()
    {
        var convert = LabelledControlPanel.FromControl;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        Assert.Equal(LabelledControlPlacement.Stacked, convert.Convert(CheckboxLabelPlacement.Stacked, typeof(LabelledControlPlacement), null, culture));
        Assert.Equal(LabelledControlJustify.SpaceBetween, convert.Convert(ToggleJustify.SpaceBetween, typeof(LabelledControlJustify?), null, culture));
        Assert.Null(convert.Convert(null, typeof(LabelledControlAlignment?), null, culture));
    }

    // The converter maps by name, so a member a control gains without the panel would throw in a binding: this says so first.
    [Theory]
    [InlineData(typeof(CheckboxLabelPlacement), typeof(LabelledControlPlacement))]
    [InlineData(typeof(RadioLabelPlacement), typeof(LabelledControlPlacement))]
    [InlineData(typeof(ToggleLabelPlacement), typeof(LabelledControlPlacement))]
    [InlineData(typeof(CheckboxJustify), typeof(LabelledControlJustify))]
    [InlineData(typeof(RadioJustify), typeof(LabelledControlJustify))]
    [InlineData(typeof(ToggleJustify), typeof(LabelledControlJustify))]
    [InlineData(typeof(CheckboxAlignment), typeof(LabelledControlAlignment))]
    [InlineData(typeof(RadioAlignment), typeof(LabelledControlAlignment))]
    [InlineData(typeof(ToggleAlignment), typeof(LabelledControlAlignment))]
    public void Every_Member_Of_A_Controls_Enum_Has_The_Panels_Of_The_Same_Name(Type control, Type panel) =>
        Assert.Empty(Enum.GetNames(control).Except(Enum.GetNames(panel)));
}
