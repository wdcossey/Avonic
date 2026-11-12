using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-list in Chromium, at 390 px wide. See docs/components/ion-list/PLAN.md.
/// What a list does to its items is in the theme, so nearly every test installs it.
/// </summary>
public class AvonicListTests
{
    private static readonly Thickness NoBorder = new(0);
    private static readonly Thickness Bottom = new(0, 0, 0, 1);

    private static AvonicItem Item(ItemLines? lines = null) => new() { Content = "x", Lines = lines };

    private static Border Part(Control control, string name) =>
        control.GetVisualDescendants().OfType<Border>().First(border => border.Name == name);

    /// <summary>The item's outer border, then its inner one.</summary>
    private static (Thickness Native, Thickness Inner) Borders(AvonicItem item) =>
        (Part(item, "PART_Native").BorderThickness, Part(item, "PART_Inner").BorderThickness);

    private static void Run() => Dispatcher.UIThread.RunJobs();

    private static Rect Box(Visual visual, Visual root)
    {
        var topLeft = visual.TranslatePoint(default, root)!.Value;
        return new Rect(topLeft, visual.Bounds.Size);
    }

    private static void PressKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Run();
    }

    /// <summary>TestApp has no base theme, and a ScrollViewer's template comes from one.</summary>
    private static void Scrolling(Action test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ => test());
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    // ── The block ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L1_Items_Stack_At_Full_Width_Inside_8_Of_Padding() => InstalledTheme.Run(_ =>
    {
        var first = Item();
        var second = Item();
        var list = new AvonicList { Items = { first, second } };
        using var scene = new Scene(new StackPanel { Children = { list } });

        Assert.Equal(new Rect(0, 0, 390, 112), Box(list, scene.Window));
        Assert.Equal(new Rect(0, 8, 390, 48), Box(first, scene.Window));
        Assert.Equal(new Rect(0, 56, 390, 48), Box(second, scene.Window));
    });

    [AvaloniaFact]
    public void L1_The_Background_Is_The_Item_Background() => InstalledTheme.Run(_ =>
    {
        var list = new AvonicList { Height = 50 };
        using var scene = new Scene(new StackPanel { Background = Brushes.Black, Children = { list } });
        scene.Window.Resources["Avonic.ItemBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(1, 2, 3));
        Run();

        var root = list.GetVisualChildren().OfType<Border>().Single();
        Assert.Equal(Color.FromRgb(1, 2, 3), Assert.IsAssignableFrom<ISolidColorBrush>(root.Background).Color);
    });

    [AvaloniaFact]
    public void L1_It_Works_On_The_Stub_Without_A_Theme()
    {
        var item = new TextBlock { Text = "x" };
        var list = new AvonicList { Items = { item } };
        using var scene = new Scene(new StackPanel { Children = { list } });

        Assert.Same(list, item.GetLogicalParent());
        Assert.True(item.Bounds.Height > 0);
    }

    [AvaloniaFact]
    public void L1_The_Stub_Passes_On_What_A_Theme_Sets()
    {
        var list = new AvonicList
        {
            Background = Brushes.Red,
            CornerRadius = new CornerRadius(3),
            ClipToBounds = true,
            Padding = new Thickness(4),
            ItemsPanel = new FuncTemplate<Panel?>(() => new WrapPanel()),
            Items = { new TextBlock { Text = "x" } },
        };
        using var scene = new Scene(new StackPanel { Children = { list } });

        var root = list.GetVisualChildren().OfType<Border>().Single();
        Assert.Same(Brushes.Red, root.Background);
        Assert.Equal(new CornerRadius(3), root.CornerRadius);
        Assert.True(root.ClipToBounds);
        Assert.Equal(new Thickness(4), root.Padding);
        Assert.Single(list.GetVisualDescendants().OfType<WrapPanel>());
    }

    [AvaloniaFact]
    public void L17_It_Does_Not_Scroll_By_Itself_So_What_It_Is_In_Scrolls() => Scrolling(() =>
    {
        var list = new AvonicList();
        for (var i = 0; i < 30; i++)
        {
            list.Items.Add(Item());
        }

        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 200);

        Assert.Empty(list.GetVisualDescendants().OfType<ScrollViewer>());
        Assert.Equal(30 * 48 + 16, scroller.Extent.Height);
        Assert.Equal(30 * 48 + 16, list.Bounds.Height);
    });

    // ── Lines ────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L2_Without_Lines_Items_Keep_Their_Default() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        using var scene = new Scene(new AvonicList { Items = { item } });

        Assert.Equal((NoBorder, Bottom), Borders(item));
        Assert.Equal(48, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void L2_None_Takes_Both_Borders_Away() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        using var scene = new Scene(new AvonicList { Lines = ListLines.None, Items = { item } });

        Assert.Equal((NoBorder, NoBorder), Borders(item));
        Assert.Equal(48, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void L3_Full_Draws_The_Outer_Border_And_Adds_To_The_Height() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        using var scene = new Scene(new AvonicList { Lines = ListLines.Full, Items = { item } });

        Assert.Equal((Bottom, NoBorder), Borders(item));
        Assert.Equal(49, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void L4_Inset_Draws_The_Inner_Border() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        using var scene = new Scene(new AvonicList { Lines = ListLines.Inset, Items = { item } });

        Assert.Equal((NoBorder, Bottom), Borders(item));
        Assert.Equal(48, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void L4_Inset_Overrides_A_Token_That_Would_Hide_The_Inner_Border() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        using var scene = new Scene(new AvonicList { Lines = ListLines.Inset, Items = { item } });
        scene.Window.Resources["Avonic.Item.InnerBorderWidth"] = new Thickness(0);
        Run();

        Assert.Equal((NoBorder, Bottom), Borders(item));
    });

    [AvaloniaFact]
    public void L5_An_Items_Own_Lines_Beat_The_Lists() => InstalledTheme.Run(_ =>
    {
        var inNone = Item(ItemLines.Full);
        var inNoneInset = Item(ItemLines.Inset);
        var inFull = Item(ItemLines.None);
        var inFullInset = Item(ItemLines.Inset);
        var inInset = Item(ItemLines.Full);
        var inInsetNone = Item(ItemLines.None);
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicList { Lines = ListLines.None, Items = { inNone, inNoneInset } },
                new AvonicList { Lines = ListLines.Full, Items = { inFull, inFullInset } },
                new AvonicList { Lines = ListLines.Inset, Items = { inInset, inInsetNone } },
            },
        });

        Assert.Equal((Bottom, NoBorder), Borders(inNone));
        Assert.Equal((NoBorder, Bottom), Borders(inNoneInset));
        Assert.Equal((NoBorder, NoBorder), Borders(inFull));
        Assert.Equal((NoBorder, Bottom), Borders(inFullInset));
        Assert.Equal((Bottom, NoBorder), Borders(inInset));
        Assert.Equal((NoBorder, NoBorder), Borders(inInsetNone));
    });

    [AvaloniaFact]
    public void L6_A_Lists_Lines_Beat_An_Interactive_Items_Borders() => InstalledTheme.Run(_ =>
    {
        // Measured with ion-input: item-interactive gives the item an outer border, and lines="none" removes it.
        var interactive = Item();
        var alone = Item();
        using var scene = new Scene(new StackPanel
        {
            Children = { new AvonicList { Lines = ListLines.None, Items = { interactive } }, alone },
        });

        ((IPseudoClasses)interactive.Classes).Set(":item-interactive", true);
        ((IPseudoClasses)alone.Classes).Set(":item-interactive", true);
        Run();

        Assert.Equal((Bottom, NoBorder), Borders(alone));
        Assert.Equal((NoBorder, NoBorder), Borders(interactive));
    });

    [AvaloniaFact]
    public void L7_Lines_Reach_Items_At_Any_Depth() => InstalledTheme.Run(_ =>
    {
        var inGroup = Item();
        var inPanel = Item();
        var deep = Item();
        using var scene = new Scene(new AvonicList
        {
            Lines = ListLines.Full,
            Items =
            {
                new AvonicItemGroup { Items = { inGroup } },
                new StackPanel { Children = { inPanel, new Border { Child = new StackPanel { Children = { deep } } } } },
            },
        });

        Assert.Equal((Bottom, NoBorder), Borders(inGroup));
        Assert.Equal((Bottom, NoBorder), Borders(inPanel));
        Assert.Equal((Bottom, NoBorder), Borders(deep));
    });

    [AvaloniaFact]
    public void L8_Between_Nested_Lists_The_Rule_Order_Decides_Not_The_Nesting() => InstalledTheme.Run(_ =>
    {
        // Measured: full wins over none whichever is inside, because it's later in list.md.css.
        var fullInNone = Item();
        var noneInFull = Item();
        var insetInFull = Item();
        var fullInInset = Item();
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicList { Lines = ListLines.None, Items = { new AvonicList { Lines = ListLines.Full, Items = { fullInNone } } } },
                new AvonicList { Lines = ListLines.Full, Items = { new AvonicList { Lines = ListLines.None, Items = { noneInFull } } } },
                new AvonicList { Lines = ListLines.Full, Items = { new AvonicList { Lines = ListLines.Inset, Items = { insetInFull } } } },
                new AvonicList { Lines = ListLines.Inset, Items = { new AvonicList { Lines = ListLines.Full, Items = { fullInInset } } } },
            },
        });

        Assert.Equal((Bottom, NoBorder), Borders(fullInNone));
        Assert.Equal((Bottom, NoBorder), Borders(noneInFull));
        Assert.Equal((NoBorder, Bottom), Borders(insetInFull));
        Assert.Equal((NoBorder, Bottom), Borders(fullInInset));
    });

    [AvaloniaFact]
    public void L9_Lines_Change_Live_And_Null_Restores_The_Default() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        var list = new AvonicList { Items = { item } };
        using var scene = new Scene(list);

        list.Lines = ListLines.None;
        Run();
        Assert.Equal((NoBorder, NoBorder), Borders(item));

        list.Lines = ListLines.Full;
        Run();
        Assert.Equal((Bottom, NoBorder), Borders(item));

        list.Lines = ListLines.Inset;
        Run();
        Assert.Equal((NoBorder, Bottom), Borders(item));

        list.Lines = null;
        Run();
        Assert.Equal((NoBorder, Bottom), Borders(item));
        Assert.Equal(48, item.Bounds.Height);
    });

    [AvaloniaFact]
    public void L10_Lines_Reach_Items_From_A_Template() => InstalledTheme.Run(_ =>
    {
        var list = new AvonicList
        {
            Lines = ListLines.None,
            ItemsSource = new[] { "a", "b" },
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new AvonicItem { Content = text }),
        };
        using var scene = new Scene(new StackPanel { Children = { list } });

        var items = list.GetVisualDescendants().OfType<AvonicItem>().ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal((NoBorder, NoBorder), Borders(item)));
    });

    // ── Inset ────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L11_Inset_Has_16_Of_Margin_And_Keeps_Its_Padding() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        var list = new AvonicList { Inset = true, Items = { item } };
        using var scene = new Scene(new StackPanel { Children = { list } });

        Assert.Equal(new Rect(16, 16, 358, 64), Box(list, scene.Window));
        Assert.Equal(new Rect(16, 24, 358, 48), Box(item, scene.Window));
    });

    [AvaloniaFact]
    public void L11_Inset_Has_Rounded_Corners_That_Clip_Its_Items() => InstalledTheme.Run(_ =>
    {
        var list = new AvonicList
        {
            Inset = true,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            Items = { new AvonicItem { Color = "danger", Content = "x", Height = 40 } },
        };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { list } });
        scene.Window.SetRenderScaling(4);
        Run();

        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 0, 0));
        Assert.NotEqual(Colors.White, Frames.PixelAt(frame, 8, 8));
    });

    [AvaloniaFact]
    public void L11_Without_Inset_There_Are_No_Corners_Or_Margin() => InstalledTheme.Run(_ =>
    {
        var list = new AvonicList { Padding = new Thickness(0), Items = { new AvonicItem { Color = "danger", Content = "x", Height = 40 } } };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { list } });
        scene.Window.SetRenderScaling(4);
        Run();

        Assert.Equal(new Thickness(0), list.Margin);
        Assert.NotEqual(Colors.White, Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, 0, 0));
    });

    [AvaloniaFact]
    public void L11_A_List_Clips_What_Overflows_It() => InstalledTheme.Run(_ =>
    {
        // contain: content
        var list = new AvonicList { Height = 20, Padding = new Thickness(0), Items = { new AvonicItem { Color = "danger", Content = "x" } } };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { list, new Border { Height = 100 } } });

        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.NotEqual(Colors.White, Frames.PixelAt(frame, 100, 10));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 100, 30));
    });

    [AvaloniaFact]
    public void L12_An_Inset_Lists_Last_Item_Has_No_Border() => InstalledTheme.Run(_ =>
    {
        var first = Item();
        var middle = Item();
        var last = Item();
        using var scene = new Scene(new AvonicList { Inset = true, Items = { first, middle, last } });

        Assert.Equal((NoBorder, Bottom), Borders(first));
        Assert.Equal((NoBorder, Bottom), Borders(middle));
        Assert.Equal((NoBorder, NoBorder), Borders(last));
    });

    [AvaloniaFact]
    public void L12_An_Only_Child_Has_No_Border() => InstalledTheme.Run(_ =>
    {
        var only = Item();
        using var scene = new Scene(new AvonicList { Inset = true, Items = { only } });

        Assert.Equal((NoBorder, NoBorder), Borders(only));
    });

    [AvaloniaFact]
    public void L12_The_Rule_Beats_Every_Kind_Of_Lines() => InstalledTheme.Run(_ =>
    {
        // Measured a7 and a8: lines on the list or on the item itself, and the last item stays 48 with no border.
        var fromList = Item();
        var beforeLast = Item();
        var own = Item(ItemLines.Full);
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicList { Inset = true, Lines = ListLines.Full, Items = { beforeLast, fromList } },
                new AvonicList { Inset = true, Items = { Item(), own } },
            },
        });

        Assert.Equal((Bottom, NoBorder), Borders(beforeLast));
        Assert.Equal((NoBorder, NoBorder), Borders(fromList));
        Assert.Equal((NoBorder, NoBorder), Borders(own));
        Assert.Equal(48, own.Bounds.Height);
    });

    [AvaloniaFact]
    public void L12_It_Applies_In_Each_Parent_Inside_The_List() => InstalledTheme.Run(_ =>
    {
        // Measured a6 and a10: last-of-type and only-child are per parent.
        var groupFirst = Item();
        var groupLast = Item();
        var panelOnly = Item();
        using var scene = new Scene(new AvonicList
        {
            Inset = true,
            Items =
            {
                new AvonicItemGroup { Items = { new AvonicItemDivider(), groupFirst, groupLast } },
                new StackPanel { Children = { panelOnly } },
                new TextBlock(),
            },
        });

        Assert.Equal((NoBorder, Bottom), Borders(groupFirst));
        Assert.Equal((NoBorder, NoBorder), Borders(groupLast));
        Assert.Equal((NoBorder, NoBorder), Borders(panelOnly));
    });

    [AvaloniaFact]
    public void L12_A_Change_Of_Items_Moves_It() => InstalledTheme.Run(_ =>
    {
        var first = Item();
        var second = Item();
        var list = new AvonicList { Inset = true, Items = { first, second } };
        using var scene = new Scene(list);

        var added = Item();
        list.Items.Add(added);
        Run();
        Assert.Equal((NoBorder, Bottom), Borders(second));
        Assert.Equal((NoBorder, NoBorder), Borders(added));

        list.Items.Remove(added);
        Run();
        Assert.Equal((NoBorder, NoBorder), Borders(second));
        Assert.Equal((NoBorder, Bottom), Borders(first));
    });

    [AvaloniaFact]
    public void L12_It_Reaches_The_Last_Item_From_A_Template() => InstalledTheme.Run(_ =>
    {
        var list = new AvonicList
        {
            Inset = true,
            ItemsSource = new[] { "a", "b", "c" },
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new AvonicItem { Content = text }),
        };
        using var scene = new Scene(new StackPanel { Children = { list } });

        var items = list.GetVisualDescendants().OfType<AvonicItem>().ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal((NoBorder, Bottom), Borders(items[0]));
        Assert.Equal((NoBorder, Bottom), Borders(items[1]));
        Assert.Equal((NoBorder, NoBorder), Borders(items[2]));
    });

    [AvaloniaFact]
    public void L13_A_Plain_Lists_Last_Item_Keeps_Its_Border() => InstalledTheme.Run(_ =>
    {
        var last = Item();
        var list = new AvonicList { Items = { Item(), last } };
        using var scene = new Scene(list);

        Assert.Equal((NoBorder, Bottom), Borders(last));

        list.Inset = true;
        Run();
        Assert.Equal((NoBorder, NoBorder), Borders(last));

        list.Inset = false;
        Run();
        Assert.Equal((NoBorder, Bottom), Borders(last));
    });

    [AvaloniaFact]
    public void L14_An_Inset_List_After_An_Inset_List_Has_No_Top_Margin() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicList { Inset = true, Items = { Item() } };
        var second = new AvonicList { Inset = true, Items = { Item() } };
        var plain = new AvonicList { Items = { Item() } };
        var after = new AvonicList { Inset = true, Items = { Item() } };
        using var scene = new Scene(new StackPanel { Children = { first, second, plain, after } });

        // Measured adj1 to adj4: 16, then 16 between the insets, then 16, then 16.
        Assert.Equal(new Rect(16, 16, 358, 64), Box(first, scene.Window));
        Assert.Equal(new Rect(16, 96, 358, 64), Box(second, scene.Window));
        Assert.Equal(new Rect(0, 176, 390, 64), Box(plain, scene.Window));
        Assert.Equal(new Rect(16, 256, 358, 64), Box(after, scene.Window));
    });

    [AvaloniaFact]
    public void L14_It_Follows_A_Change_On_The_List_Before_And_Of_The_Siblings() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicList { Inset = true };
        var second = new AvonicList { Inset = true };
        var panel = new StackPanel { Children = { first, second } };
        using var scene = new Scene(panel);
        Assert.Equal(0, second.Margin.Top);

        first.Inset = false;
        Run();
        Assert.Equal(16, second.Margin.Top);

        first.Inset = true;
        Run();
        Assert.Equal(0, second.Margin.Top);

        var between = new Border();
        panel.Children.Insert(1, between);
        Run();
        Assert.Equal(16, second.Margin.Top);

        panel.Children.Remove(between);
        Run();
        Assert.Equal(0, second.Margin.Top);

        panel.Children.Remove(first);
        Run();
        Assert.Equal(16, second.Margin.Top);
    });

    [AvaloniaFact]
    public void L14_It_Works_Through_An_Items_Controls_Presenters() => Scrolling(() =>
    {
        // An ItemTemplate that makes inset lists: data-driven sections.
        var sections = new ItemsControl
        {
            ItemsSource = new[] { "a", "b", "c" },
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new AvonicList { Inset = true, Items = { Item() } }),
        };
        using var scene = new Scene(new StackPanel { Children = { sections } });

        var lists = sections.GetVisualDescendants().OfType<AvonicList>().ToList();
        Assert.Equal(3, lists.Count);
        Assert.Equal(16, lists[0].Margin.Top);
        Assert.Equal(0, lists[1].Margin.Top);
        Assert.Equal(0, lists[2].Margin.Top);

        lists[1].Inset = false;
        Run();
        Assert.Equal(16, lists[2].Margin.Top);

        lists[1].Inset = true;
        Run();
        Assert.Equal(0, lists[2].Margin.Top);
    });

    [AvaloniaFact]
    public void L14_A_List_Added_Through_A_Template_Tells_The_Next_One() => Scrolling(() =>
    {
        var data = new System.Collections.ObjectModel.ObservableCollection<string> { "b" };
        var sections = new ItemsControl
        {
            ItemsSource = data,
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new AvonicList { Inset = true, Tag = text }),
        };
        using var scene = new Scene(new StackPanel { Children = { sections } });
        var b = sections.GetVisualDescendants().OfType<AvonicList>().Single();
        Assert.Equal(16, b.Margin.Top);

        data.Insert(0, "a");
        Run();

        Assert.Equal(0, b.Margin.Top);
    });

    /// <summary>Data-driven sections in a virtualising panel, as many as it takes to scroll a long way.</summary>
    private static (ItemsControl Sections, ScrollViewer Scroller) VirtualisedSections(Func<int, bool> inset, int count = 200)
    {
        var sections = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
            ItemsSource = Enumerable.Range(0, count).ToList(),
            ItemTemplate = new FuncDataTemplate<int>((index, _) =>
                new AvonicList { Inset = inset(index), Tag = index, Items = { Item() } }),
        };
        return (sections, new ScrollViewer { Content = sections });
    }

    /// <summary>What's realised and shown, in order, with the gap above each from the list before it.</summary>
    private static List<(AvonicList List, int Index)> Realised(ItemsControl sections) =>
        sections.GetVisualDescendants().OfType<AvonicList>()
            .Where(list => list.IsEffectivelyVisible && sections.IndexFromContainer(AvonicListContainer(list)) >= 0)
            .Select(list => (list, sections.IndexFromContainer(AvonicListContainer(list))))
            .OrderBy(row => row.Item2)
            .ToList();

    private static Control AvonicListContainer(AvonicList list) => list.GetVisualParent() as Avalonia.Controls.Presenters.ContentPresenter ?? (Control)list;

    private static void AssertIonicGaps(ItemsControl sections, Func<int, bool> inset)
    {
        var rows = Realised(sections);
        Assert.NotEmpty(rows);
        foreach (var (list, index) in rows)
        {
            // .list-inset + ion-list.list-inset: a margin of 0 on top, so the gap is the one before's 16. A row whose
            // item before isn't realised, at the top, reads that item too.
            var expected = inset(index) && !(index > 0 && inset(index - 1)) ? 16 : 0;
            Assert.True(expected == list.Margin.Top, $"row {index}: {list.Margin.Top}, not {expected}");
        }

        // Between two realised inset lists, the gap is 16, as in CSS, not 32.
        for (var i = 1; i < rows.Count; i++)
        {
            if (inset(rows[i].Index) && inset(rows[i - 1].Index) && rows[i].Index == rows[i - 1].Index + 1)
            {
                var gap = rows[i].List.TranslatePoint(default, sections)!.Value.Y
                    - (rows[i - 1].List.TranslatePoint(default, sections)!.Value.Y + rows[i - 1].List.Bounds.Height);
                Assert.Equal(16, gap, 3);
            }
        }
    }

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_The_Row_Before_Is_The_Item_Before() => Scrolling(() =>
    {
        // Measured: 16 between two inset lists. Its children are what's realised, in no set order, so the panel's
        // children can't say which list is before which.
        static bool Inset(int _) => true;
        var (sections, scroller) = VirtualisedSections(Inset);
        using var scene = new Scene(scroller, 390, 400);
        AssertIonicGaps(sections, Inset);

        foreach (var offset in new[] { 2000.0, 1000, 5000, 4990, 4900, 4000, 0 })
        {
            scroller.Offset = new Vector(0, offset);
            Run();
            AssertIonicGaps(sections, Inset);
        }

        // Scrolling up a little at a time realises each list above the top after the one below it.
        scroller.Offset = new Vector(0, 3000);
        Run();
        for (var offset = 3000.0; offset > 2000; offset -= 37)
        {
            scroller.Offset = new Vector(0, offset);
            Run();
            AssertIonicGaps(sections, Inset);
        }
    });

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_A_Plain_List_Between_Inset_Ones_Is_Read() => Scrolling(() =>
    {
        // Every third section isn't inset, so the one after it keeps its 16.
        static bool Inset(int index) => index % 3 != 1;
        var (sections, scroller) = VirtualisedSections(Inset);
        using var scene = new Scene(scroller, 390, 400);
        AssertIonicGaps(sections, Inset);

        for (var offset = 0.0; offset < 3000; offset += 41)
        {
            scroller.Offset = new Vector(0, offset);
            Run();
            AssertIonicGaps(sections, Inset);
        }

        for (var offset = 3000.0; offset > 0; offset -= 53)
        {
            scroller.Offset = new Vector(0, offset);
            Run();
            AssertIonicGaps(sections, Inset);
        }
    });

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_A_Change_Reaches_The_Next_Row() => Scrolling(() =>
    {
        static bool Inset(int _) => true;
        var (sections, scroller) = VirtualisedSections(Inset);
        using var scene = new Scene(scroller, 390, 400);
        var rows = Realised(sections);
        Assert.Equal(0, rows[2].List.Margin.Top);

        rows[1].List.Inset = false;
        Run();
        Assert.Equal(16, rows[2].List.Margin.Top);

        rows[1].List.Inset = true;
        Run();
        Assert.Equal(0, rows[2].List.Margin.Top);
    });

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_A_Removed_Item_Moves_The_Rows_After_It() => Scrolling(() =>
    {
        var data = new System.Collections.ObjectModel.ObservableCollection<bool> { true, false, true };
        var sections = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
            ItemsSource = data,
            ItemTemplate = new FuncDataTemplate<bool>((inset, _) => new AvonicList { Inset = inset, Items = { Item() } }),
        };
        using var scene = new Scene(new ScrollViewer { Content = sections }, 390, 400);
        var third = Realised(sections)[2].List;
        Assert.Equal(16, third.Margin.Top);

        data.RemoveAt(1);
        Run();

        Assert.Equal(0, third.Margin.Top);
    });

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_A_Template_That_Keeps_Its_List_Is_Read_Again() => Scrolling(() =>
    {
        // A recycling template gives a row's list the next item, so the list isn't attached again.
        static bool Inset(int index) => index % 3 != 1;
        var sections = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
            ItemsSource = Enumerable.Range(0, 200).Select(Inset).ToList(),
            ItemTemplate = new FuncDataTemplate<bool>(
                (_, _) => new AvonicList { [!AvonicList.InsetProperty] = new Avalonia.Data.Binding("."), Items = { Item() } },
                supportsRecycling: true),
        };
        var scroller = new ScrollViewer { Content = sections };
        using var scene = new Scene(scroller, 390, 400);
        for (var offset = 0.0; offset < 3000; offset += 41)
        {
            scroller.Offset = new Vector(0, offset);
            Run();
            AssertIonicGaps(sections, Inset);
        }
    });

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_An_Item_Before_The_Top_Is_Read_With_Its_Data() => Scrolling(() =>
    {
        // Inset bound to the item: the item before the top row, made to be read, has to have it as its data.
        static bool Inset(int index) => index % 3 != 1;
        var sections = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
            ItemsSource = Enumerable.Range(0, 200).Select(Inset).ToList(),
            ItemTemplate = new FuncDataTemplate<bool>(
                (_, _) => new AvonicList { [!AvonicList.InsetProperty] = new Avalonia.Data.Binding("."), Items = { Item() } }),
        };
        var scroller = new ScrollViewer { Content = sections };
        using var scene = new Scene(scroller, 390, 400);
        scroller.Offset = new Vector(0, 3000);
        Run();
        for (var offset = 3000.0; offset > 0; offset -= 53)
        {
            scroller.Offset = new Vector(0, offset);
            Run();
            AssertIonicGaps(sections, Inset);
        }
    });

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_A_Change_Reaches_The_Next_Row_When_Nothing_Else_Moves() => Scrolling(() =>
    {
        // An app's own margin on the list that changes, so the change itself moves nothing.
        static bool Inset(int _) => true;
        var (sections, scroller) = VirtualisedSections(Inset);
        using var scene = new Scene(scroller, 390, 400);
        var rows = Realised(sections);
        rows[1].List.Margin = new Thickness(16);
        Run();
        Assert.Equal(0, rows[2].List.Margin.Top);

        rows[1].List.Inset = false;
        Run();

        Assert.Equal(16, rows[2].List.Margin.Top);
    });

    [AvaloniaFact]
    public void L14_In_A_Virtualising_Panel_An_Inserted_Item_Moves_The_Rows_After_It() => Scrolling(() =>
    {
        var data = new System.Collections.ObjectModel.ObservableCollection<bool> { true, true, true };
        var sections = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
            ItemsSource = data,
            ItemTemplate = new FuncDataTemplate<bool>((inset, _) => new AvonicList { Inset = inset, Items = { Item() } }),
        };
        using var scene = new Scene(new ScrollViewer { Content = sections }, 390, 400);
        var second = Realised(sections)[1].List;
        Assert.Equal(0, second.Margin.Top);

        data.Insert(1, false);
        Run();

        Assert.Equal(16, second.Margin.Top);
    });

    [AvaloniaFact]
    public void L14_A_Change_Reaches_The_Next_List_Only_Through_Its_Neighbour() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicList { Inset = true };
        var second = new AvonicList { Inset = true };
        var third = new AvonicList { Inset = true };
        using var scene = new Scene(new StackPanel { Children = { first, second, third } });

        first.Inset = false;
        Run();

        Assert.Equal(16, second.Margin.Top);
        Assert.Equal(0, third.Margin.Top);
    });

    [AvaloniaFact]
    public void L14_Known_Difference_Margins_Do_Not_Collapse() => InstalledTheme.Run(_ =>
    {
        // Measured in CSS: the parent is 144 high with the margin outside it, and the gap beside a margin of 30 is 30.
        var parent = new StackPanel
        {
            Children =
            {
                new AvonicList { Inset = true, Items = { Item() } },
                new AvonicList { Inset = true, Items = { Item() } },
            },
        };
        var before = new Border { Height = 18, Margin = new Thickness(0, 30) };
        var after = new AvonicList { Inset = true, Items = { Item() } };
        using var scene = new Scene(new StackPanel { Children = { parent, before, after } });

        Assert.Equal(176, parent.Bounds.Height);
        Assert.Equal(46, after.Bounds.Top - before.Bounds.Bottom);
    });

    [AvaloniaFact]
    public void L14_A_Plain_List_After_An_Inset_List_Has_No_Margin_At_All() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicList();
        using var scene = new Scene(new StackPanel { Children = { new AvonicList { Inset = true }, plain } });

        Assert.Equal(new Thickness(0), plain.Margin);
    });

    [AvaloniaFact]
    public void L14_Moving_To_Another_Parent_Reads_The_New_Siblings() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicList { Inset = true };
        var second = new AvonicList { Inset = true };
        var oldParent = new StackPanel { Children = { first, second } };
        var newParent = new StackPanel();
        using var scene = new Scene(new StackPanel { Children = { oldParent, newParent } });
        Assert.Equal(0, second.Margin.Top);

        oldParent.Children.Remove(second);
        Run();
        Assert.Equal(16, second.Margin.Top);

        newParent.Children.Add(second);
        Run();
        Assert.Equal(16, second.Margin.Top);

        // Nothing is listening to the old parent's list now.
        oldParent.Children.Insert(0, new AvonicList { Inset = true });
        Run();
        Assert.Equal(16, second.Margin.Top);
    });

    // ── Input and automation ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L15_Every_Button_Item_Is_A_Tab_Stop() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicItem { Button = true };
        var inGroup = new AvonicItem { Button = true };
        var last = new AvonicItem { Button = true };
        using var scene = new Scene(new AvonicList
        {
            Items = { new AvonicListHeader(), first, new AvonicItemGroup { Items = { new AvonicItemDivider(), inGroup } }, last },
        });

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(first.IsFocused);
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(inGroup.IsFocused);
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(last.IsFocused);
    });

    [AvaloniaTheory]
    [InlineData(PhysicalKey.ArrowDown)]
    [InlineData(PhysicalKey.ArrowUp)]
    [InlineData(PhysicalKey.Home)]
    [InlineData(PhysicalKey.End)]
    public void L15_Arrow_Keys_Do_Not_Move_Focus(PhysicalKey key) => InstalledTheme.Run(_ =>
    {
        var first = new AvonicItem { Button = true };
        var second = new AvonicItem { Button = true };
        var third = new AvonicItem { Button = true };
        using var scene = new Scene(new AvonicList { Items = { first, second, third } });

        second.Focus();
        Run();
        PressKey(scene, key);

        Assert.True(second.IsFocused);
    });

    [AvaloniaFact]
    public void L16_It_Reads_As_A_List_With_List_Items() => InstalledTheme.Run(_ =>
    {
        var item = Item();
        var inGroup = Item();
        var alone = Item();
        var list = new AvonicList { Items = { item, new AvonicItemGroup { Items = { inGroup } } } };
        using var scene = new Scene(new StackPanel { Children = { list, alone } });

        var peer = ControlAutomationPeer.CreatePeerForElement(list);
        Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
        Assert.True(peer.IsControlElement());
        Assert.Equal(AutomationControlType.ListItem, ControlAutomationPeer.CreatePeerForElement(item).GetAutomationControlType());
        Assert.Equal(AutomationControlType.ListItem, ControlAutomationPeer.CreatePeerForElement(inGroup).GetAutomationControlType());
        Assert.Equal(AutomationControlType.Group, ControlAutomationPeer.CreatePeerForElement(alone).GetAutomationControlType());
    });

    [AvaloniaFact]
    public void L16_A_Button_Item_In_A_List_Stays_A_Button() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true, Content = "x" };
        using var scene = new Scene(new AvonicList { Items = { item } });

        Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(item).GetAutomationControlType());
    });

    // ── Virtualising ─────────────────────────────────────────────────────────────────────────

    private static AvonicList Virtualised(int count, bool inset = false, ListLines? lines = null) => new()
    {
        Inset = inset,
        Lines = lines,
        ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
        ItemsSource = Enumerable.Range(0, count).Select(i => i.ToString()).ToList(),
        ItemTemplate = new FuncDataTemplate<string>((text, _) => new AvonicItem { Content = text }),
    };

    [AvaloniaFact]
    public void L18_A_Virtualising_Panel_Only_Realises_What_Is_In_View() => Scrolling(() =>
    {
        var list = Virtualised(10000);
        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 400);

        Assert.InRange(list.GetVisualDescendants().OfType<AvonicItem>().Count(), 8, 40);
        Assert.Equal(10000 * 48 + 16, scroller.Extent.Height);
    });

    [AvaloniaFact]
    public void L18_Lines_And_The_Last_Item_Hold_In_A_Virtualising_Panel() => Scrolling(() =>
    {
        var list = Virtualised(100, inset: true, lines: ListLines.Full);
        var scroller = new ScrollViewer { Content = list };
        using var scene = new Scene(scroller, 390, 400);

        Assert.All(list.GetVisualDescendants().OfType<AvonicItem>(), item => Assert.Equal((Bottom, NoBorder), Borders(item)));

        scroller.Offset = new Vector(0, scroller.Extent.Height);
        Run();

        var items = list.GetVisualDescendants().OfType<AvonicItem>().OrderBy(item => Box(item, list).Top).ToList();
        Assert.Equal("99", items[^1].Content);
        Assert.Equal((NoBorder, NoBorder), Borders(items[^1]));
        Assert.Equal((Bottom, NoBorder), Borders(items[^2]));

        // A recycled container doesn't keep it.
        scroller.Offset = default;
        Run();
        Assert.All(list.GetVisualDescendants().OfType<AvonicItem>().Where(item => item.IsVisible), item => Assert.Equal((Bottom, NoBorder), Borders(item)));
    });
}
