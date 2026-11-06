using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// What #72 shared between an item and its neighbours. The numbers are from Chromium, under "Shared press handling" in
/// docs/components/ion-item/PLAN.md.
/// </summary>
public class AvonicItemSharedTests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static T Part<T>(Control host, string name)
        where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    private static Rect Box(Visual visual, Visual root) => new(visual.TranslatePoint(default, root)!.Value, visual.Bounds.Size);

    private static void AssertMatrix(AvonicLabel label, double scale, double translateY)
    {
        Assert.NotNull(label.RenderTransform);
        var matrix = label.RenderTransform!.Value;
        Assert.Equal(scale, matrix.M11, 3);
        Assert.Equal(scale, matrix.M22, 3);
        Assert.Equal(translateY, matrix.M32, 1);
    }

    private static Control InHost(string host, AvonicLabel label) => host switch
    {
        "divider" => new AvonicItemDivider { Content = label },
        "group" => new AvonicItemGroup { Items = { label } },
        _ => new AvonicListHeader { Content = label },
    };

    // ── S7: a label's Position outside an item ───────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData("divider", LabelPosition.Stacked, 0.75, 0.5)]
    [InlineData("divider", LabelPosition.Floating, 1, 0.96)]
    [InlineData("group", LabelPosition.Stacked, 0.75, 0.5)]
    [InlineData("group", LabelPosition.Floating, 1, 0.96)]
    [InlineData("header", LabelPosition.Stacked, 0.75, 0.5)]
    [InlineData("header", LabelPosition.Floating, 1, 0.96)]
    public void S7_Stacked_And_Floating_Apply_Outside_An_Item(string host, LabelPosition position, double scale, double fraction)
    {
        InstalledTheme.Run(_ =>
        {
            var label = new AvonicLabel { Content = "x", Position = position };
            using var scene = new Scene(new StackPanel { Children = { InHost(host, label) } });

            AssertMatrix(label, scale, label.Bounds.Height * fraction);
        });
    }

    [AvaloniaTheory]
    [InlineData("divider", 30)]
    [InlineData("header", 45)]
    public void S7_Stacked_Fills_What_It_Is_In(string host, double height)
    {
        InstalledTheme.Run(_ =>
        {
            var label = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
            using var scene = new Scene(new StackPanel { Children = { InHost(host, label) } });

            Assert.Equal(height, label.Bounds.Height);
        });
    }

    [AvaloniaTheory]
    [InlineData("divider")]
    [InlineData("group")]
    [InlineData("header")]
    public void S7_Fixed_Is_100_Wide_Outside_An_Item(string host)
    {
        InstalledTheme.Run(_ =>
        {
            var label = new AvonicLabel { Content = "x", Position = LabelPosition.Fixed };
            using var scene = new Scene(new StackPanel { Children = { InHost(host, label) } });

            Assert.Equal(100, label.Bounds.Width);
        });
    }

    [AvaloniaFact]
    public void S7_Alone_A_Label_Has_None_Of_It() => InstalledTheme.Run(_ =>
    {
        var stacked = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked };
        var fixedLabel = new AvonicLabel { Content = "x", Position = LabelPosition.Fixed };
        using var scene = new Scene(new StackPanel { Children = { stacked, fixedLabel } });

        Assert.Null(stacked.RenderTransform);
        Assert.NotEqual(100, fixedLabel.Bounds.Width);
    });

    [AvaloniaTheory]
    [InlineData("divider", true)]
    [InlineData("group", true)]
    [InlineData("header", false)]
    public void S7_The_Label_Colour_Token_Needs_The_Class_Item(string host, bool reads)
    {
        InstalledTheme.Run(_ =>
        {
            var app = Application.Current!;
            app.Resources["Avonic.Label.Color"] = Brushes.Lime;
            try
            {
                var label = new AvonicLabel { Content = "x" };
                using var scene = new Scene(new StackPanel { Children = { InHost(host, label) } });

                Assert.Equal(reads, ReferenceEquals(label.Foreground, Brushes.Lime));
            }
            finally
            {
                app.Resources.Remove("Avonic.Label.Color");
            }
        });
    }

    // ── Icons in a panel ─────────────────────────────────────────────────────────────────────

    private static StackPanel PanelOf(Control child) => new() { Orientation = Orientation.Horizontal, Children = { child } };

    [AvaloniaFact]
    public void An_Icon_In_A_Panel_Has_The_Size_And_Colour_Of_One_In_A_Slot() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "star" };
        var direct = new AvonicIcon { IconName = "star" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItem { EndContent = PanelOf(icon) },
                new AvonicItem { EndContent = direct },
            },
        });

        Assert.Equal(24, icon.FontSize);
        Assert.Same(direct.Foreground, icon.Foreground);
    });

    [AvaloniaFact]
    public void An_Icon_In_A_Panel_In_A_Coloured_Item_Is_The_Items_Colour() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "star" };
        var item = new AvonicItem { Color = "primary", EndContent = PanelOf(icon) };
        using var scene = new Scene(item);

        Assert.Same(item.Foreground, icon.Foreground);
    });

    [AvaloniaFact]
    public void An_Icon_In_A_Panel_Has_The_Margin_Of_Its_Slot() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "star" };
        var end = new AvonicIcon { IconName = "star" };
        using var scene = new Scene(new AvonicItem { StartContent = PanelOf(start), EndContent = PanelOf(end) });

        Assert.Equal(new Thickness(0, 12, 32, 12), start.Margin);
        Assert.Equal(new Thickness(16, 12, 0, 12), end.Margin);
    });

    [AvaloniaFact]
    public void An_Icon_Deeper_Than_A_Panel_Has_None_Of_It() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "star" };
        using var scene = new Scene(new AvonicItem { EndContent = PanelOf(PanelOf(icon)) });

        Assert.Equal(default, icon.Margin);
    });

    // ── S8: a button's end margin ────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void S8_An_End_Button_Beside_A_Stacked_Label_Keeps_2_At_Its_End(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "x" };
        using var scene = new Scene(new AvonicItem { Content = new AvonicLabel { Content = "x", Position = position }, EndContent = button });

        Assert.Equal(new Thickness(16, 7, 2, 7), button.Margin);
    });

    [AvaloniaFact]
    public void S8_An_End_Button_Beside_A_Plain_Label() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "x" };
        using var scene = new Scene(new AvonicItem { Content = new AvonicLabel { Content = "x" }, EndContent = button });

        Assert.Equal(new Thickness(16, 4, 2, 4), button.Margin);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void S8_Anything_Else_At_The_End_Is_As_It_Was(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var note = new Border { Width = 10, Height = 10 };
        using var scene = new Scene(new AvonicItem { Content = new AvonicLabel { Content = "x", Position = position }, EndContent = note });

        Assert.Equal(new Thickness(16, 7, 0, 7), note.Margin);
    });

    // ── Safe area ────────────────────────────────────────────────────────────────────────────

    private static void WithSafeArea(double left, double right, Action test)
    {
        InstalledTheme.Run(_ =>
        {
            var app = Application.Current!;
            app.Resources["Avonic.SafeAreaLeft"] = left;
            app.Resources["Avonic.SafeAreaRight"] = right;
            try
            {
                test();
            }
            finally
            {
                app.Resources.Remove("Avonic.SafeAreaLeft");
                app.Resources.Remove("Avonic.SafeAreaRight");
            }
        });
    }

    [AvaloniaFact]
    public void Safe_Area_Is_0_By_Default() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "x" };
        using var scene = new Scene(item);

        Assert.Equal(new Thickness(16, 0, 0, 0), Part<FlexRow>(item, "PART_Layout").Margin);
        Assert.Equal(new Thickness(0, 0, 16, 0), Part<Border>(item, "PART_Inner").Padding);
    });

    [AvaloniaFact]
    public void Safe_Area_Adds_To_An_Items_Padding_At_Each_End() => WithSafeArea(10, 20, () =>
    {
        var item = new AvonicItem { Content = "x" };
        using var scene = new Scene(item);

        Assert.Equal(new Thickness(26, 0, 0, 0), Part<FlexRow>(item, "PART_Layout").Margin);
        Assert.Equal(new Thickness(0, 0, 36, 0), Part<Border>(item, "PART_Inner").Padding);
    });

    [AvaloniaFact]
    public void Safe_Area_Swaps_Sides_In_RTL() => WithSafeArea(10, 20, () =>
    {
        var item = new AvonicItem { Content = "x", FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(item);

        Assert.Equal(new Thickness(36, 0, 0, 0), Part<FlexRow>(item, "PART_Layout").Margin);
        Assert.Equal(new Thickness(0, 0, 26, 0), Part<Border>(item, "PART_Inner").Padding);
    });

    [AvaloniaFact]
    public void Safe_Area_Reaches_A_Divider() => WithSafeArea(10, 20, () =>
    {
        var divider = new AvonicItemDivider { Content = "x" };
        using var scene = new Scene(divider);

        Assert.Equal(new Thickness(26, 0, 0, 0), divider.Padding);
        Assert.Equal(new Thickness(0, 0, 36, 0), Part<Border>(divider, "PART_Inner").Padding);
    });

    [AvaloniaFact]
    public void Safe_Area_Reaches_A_Header_At_Both_Ends() => WithSafeArea(10, 20, () =>
    {
        var header = new AvonicListHeader { Content = "x" };
        var rtl = new AvonicListHeader { Content = "x", FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(new StackPanel { Children = { header, rtl } });

        Assert.Equal(new Thickness(26, 0, 20, 0), header.Padding);
        Assert.Equal(new Thickness(36, 0, 10, 0), rtl.Padding);
    });

    [AvaloniaFact]
    public void A_Change_Of_Safe_Area_Moves_It() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "x" };
        using var scene = new Scene(item);

        var app = Application.Current!;
        app.Resources["Avonic.SafeAreaLeft"] = 10d;
        try
        {
            Settle();
            Assert.Equal(26, Part<FlexRow>(item, "PART_Layout").Margin.Left);
        }
        finally
        {
            app.Resources.Remove("Avonic.SafeAreaLeft");
        }
    });

    // ── A card's background ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Card_Follows_The_Item_Background_Then_Its_Own() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Content = "x" };
        using var scene = new Scene(card);
        var app = Application.Current!;

        app.Resources["Avonic.ItemBackgroundBrush"] = Brushes.Lime;
        try
        {
            Settle();
            Assert.Same(Brushes.Lime, card.Background);

            app.Resources["Avonic.CardBackgroundBrush"] = Brushes.Red;
            Settle();
            Assert.Same(Brushes.Red, card.Background);
        }
        finally
        {
            app.Resources.Remove("Avonic.ItemBackgroundBrush");
            app.Resources.Remove("Avonic.CardBackgroundBrush");
        }
    });

    [AvaloniaFact]
    public void A_Card_Is_White_By_Default_As_The_Item_Background_Is() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Content = "x" };
        using var scene = new Scene(card);

        Assert.Equal(Colors.White, Assert.IsAssignableFrom<ISolidColorBrush>(card.Background).Color);
    });

    // ── RequestFrame ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Frame_Comes_When_The_Window_Draws_And_Not_Before()
    {
        var box = new Border();
        using var scene = new Scene(box);
        var ran = 0;

        AvonicConfig.RequestFrame(box, () => ran++);
        Assert.Equal(0, ran);

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();

        Assert.Equal(1, ran);
    }

    [AvaloniaFact]
    public void A_Frame_Can_Be_Cancelled()
    {
        var box = new Border();
        using var scene = new Scene(box);
        var ran = 0;

        AvonicConfig.RequestFrame(box, () => ran++).Dispose();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();

        Assert.Equal(0, ran);
    }

    [AvaloniaFact]
    public void Outside_A_Window_A_Frame_Is_A_Timer()
    {
        var previous = AvonicConfig.Schedule;
        TimeSpan? delay = null;
        Action? callback = null;
        AvonicConfig.Schedule = (run, wait) =>
        {
            callback = run;
            delay = wait;
            return Avalonia.Threading.DispatcherTimer.RunOnce(() => { }, TimeSpan.FromHours(1));
        };
        try
        {
            AvonicConfig.RequestFrame(new Border(), () => { });

            Assert.Equal(TimeSpan.FromMilliseconds(16), delay);
            Assert.NotNull(callback);
        }
        finally
        {
            AvonicConfig.Schedule = previous;
        }
    }
}
