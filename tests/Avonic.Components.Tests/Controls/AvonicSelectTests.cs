using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicSelectCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-select</c> itself, md: B1 to B3 and B8 to B16 in docs/components/ion-select/PLAN.md, from <c>select.js</c>,
/// <c>select.md.css</c> and <c>ref/page.json</c>. The interfaces are <see cref="AvonicSelectInterfaceTests"/>.
/// </summary>
public class AvonicSelectTests
{
    private sealed class Record(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public override string ToString() => Name;
    }

    /// <summary>The select alone in a window with the theme, laid out at 390 wide.</summary>
    private static (Scene Scene, StackPanel Page) Show(AvonicSelect select, bool inItem = false)
    {
        var page = new StackPanel { Width = 390, [Avalonia.Controls.Documents.TextElement.FontSizeProperty] = 16.0 };
        page.Bind(Avalonia.Controls.Documents.TextElement.FontFamilyProperty, page.GetResourceObservable("Avonic.FontFamily"));
        page.Children.Add(inItem
            ? new AvonicList { Items = { new AvonicItem { Content = select } } }
            : new Border { Padding = new Thickness(16, 0), Child = select });
        var scene = new Scene(page, 390, 844);
        // At 2x, as the measurements, so that layout rounds to the half pixel.
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        return (scene, page);
    }

    // ── B1: the text ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_The_Text_Is_The_Matching_Options_Then_The_Placeholder()
    {
        var select = Select("s", s => s.Placeholder = "Pick one");
        Assert.Equal("Pick one", select.DisplayText);

        select.Value = "banana";
        Assert.Equal("Banana", select.DisplayText);

        // A value no option has: no text.
        select.Value = "kiwi";
        Assert.Equal("Pick one", select.DisplayText);
    }

    [AvaloniaFact]
    public void B1_A_List_Joins_The_Matches_And_Skips_The_Rest()
    {
        var select = Select("s", s => { s.Multiple = true; s.Value = new object?[] { "grape", "kiwi", "apple" }; });
        Assert.Equal("Grape, Apple", select.DisplayText);
    }

    [AvaloniaFact]
    public void B1_Selected_Text_Wins_Unless_Empty()
    {
        var select = Select("s", s => { s.Value = "apple"; s.SelectedText = "Chosen"; });
        Assert.Equal("Chosen", select.DisplayText);
        select.SelectedText = "";
        Assert.Equal("Apple", select.DisplayText);
    }

    [AvaloniaFact]
    public void B1_The_Placeholder_Shows_At_Its_Opacity()
    {
        InstalledTheme.Run(_ =>
        {
            var select = Select("s", s => { s.Label = "Fruit"; s.Placeholder = "Pick one"; });
            using var scene = Show(select).Scene;
            var text = Part<SelectTextBlock>(select, "PART_Text");
            text.Transitions = [];
            Assert.Equal(0.6, text.Opacity, 3);
            select.Value = "apple";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, text.Opacity, 3);
        });
    }

    // ── B2: the options ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_An_Option_With_No_Value_Is_Its_Text_And_Changes_Render_Again()
    {
        var select = new AvonicSelect { Value = "Pear" };
        Assert.Equal("", select.DisplayText);

        var option = new AvonicSelectOption { Content = "  Pear\n " };
        select.Items.Add(option);
        Assert.Equal("Pear", select.DisplayText);

        option.Content = new TextBlock { Text = "Pear" };
        Assert.Equal("Pear", select.DisplayText);
        option.Value = "pear";
        Assert.Equal("", select.DisplayText);
        select.Value = "pear";
        option.Content = new StackPanel { Children = { new TextBlock { Text = "Big " }, new TextBlock { Text = "pear" } } };
        Assert.Equal("Big pear", select.DisplayText);

        // A text set inside the option renders again too, as a new text node does.
        var block = new TextBlock { Text = "Pear" };
        option.Content = block;
        option.Value = null;
        select.Value = "Ripe pear";
        block.Text = "Ripe pear";
        Assert.Equal("Ripe pear", select.DisplayText);

        select.Items.Remove(option);
        Assert.Equal("", select.DisplayText);
    }

    // ── B3: compareWith ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_CompareWith_A_Function_Or_A_Property()
    {
        var one = new Record(1, "One");
        var two = new Record(2, "Two");
        var select = new AvonicSelect
        {
            Items = { new AvonicSelectOption { Value = one, Content = "One" }, new AvonicSelectOption { Value = two, Content = "Two" } },
        };

        select.Value = new Record(2, "copy");
        Assert.Equal("", select.DisplayText);

        select.CompareWith = "Id";
        Assert.Equal("Two", select.DisplayText);

        select.CompareWith = new Func<object?, object?, bool>((a, b) => ((Record)a!).Name == "copy" && ((Record)b!).Id == 1);
        Assert.Equal("One", select.DisplayText);
    }

    [AvaloniaFact]
    public void B3_An_Option_Value_That_Is_A_List_Matches_Any_Of_It()
    {
        var select = new AvonicSelect
        {
            Value = "b",
            Items = { new AvonicSelectOption { Value = new object?[] { "a", "b" }, Content = "A or B" } },
        };
        Assert.Equal("A or B", select.DisplayText);
    }

    [AvaloniaFact]
    public void B7_IsValueEqual_Compares_Lists_As_Multisets()
    {
        var select = new AvonicSelect { Multiple = true };
        Assert.True(select.IsValueEqual(new object?[] { "a", "b" }, new object?[] { "b", "a" }));
        Assert.False(select.IsValueEqual(new object?[] { "a", "b" }, new object?[] { "a", "a" }));
        Assert.True(select.IsValueEqual(null, new object?[] { }));
        select.Multiple = false;
        Assert.True(select.IsValueEqual(null, null));
        Assert.False(select.IsValueEqual(null, "a"));
    }

    // ── B8: focus ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_Focus_And_Blur_Raise_IonFocus_And_IonBlur_And_Set_Has_Focus()
    {
        var log = new List<string>();
        var select = Select("s");
        select.IonFocus += (_, _) => log.Add("focus");
        select.IonBlur += (_, _) => log.Add("blur");
        var other = new Button();
        var page = new StackPanel { Children = { select, other } };
        using var scene = new Scene(page);

        select.Focus();
        other.Focus();
        Assert.Equal(["focus", "blur"], log);
        Assert.True(FocusVisible.GetIsEnabled(select));
    }

    // ── B9: the label ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Floating_Label_Floats_With_A_Value_Or_Focus_A_Stacked_One_Always() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.LabelPlacement = SelectLabelPlacement.Floating; });
        var other = new Button();
        using var scene = new Scene(new StackPanel { Children = { select, other } });
        var label = Part<Border>(select, "PART_Label");
        label.Transitions = [];
        double Scale() { Dispatcher.UIThread.RunJobs(); return label.GetValue(Css.ScaleProperty); }

        Assert.Equal(1, Scale());
        select.Focus();
        Assert.Equal(0.75, Scale());
        other.Focus();
        Assert.Equal(1, Scale());
        select.Value = "apple";
        Assert.Equal(0.75, Scale());
        select.Value = null;
        select.LabelPlacement = SelectLabelPlacement.Stacked;
        Assert.Equal(0.75, Scale());
    });

    [AvaloniaFact]
    public void B9_The_Floating_Label_Rests_On_The_Field_And_Floats_Above_It() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Floating"; s.LabelPlacement = SelectLabelPlacement.Floating; });
        var (scene, page) = Show(select);
        using (scene)
        {
            var label = Part<Border>(select, "PART_Label");
            label.Transitions = [];
            Dispatcher.UIThread.RunJobs();
            // ref/page.json: 19 down, full size.
            AssertBox(16, 19, 57.52, 19, Box(label, page), 1);

            select.Value = "apple";
            Dispatcher.UIThread.RunJobs();
            // As the solid one floats: 9.5 down, at 0.75.
            Assert.Equal(9.5, Box(label, page).Y, 0.6);
            Assert.Equal(14.25, Box(label, page).Height, 0.6);
        }
    });

    // ── B10: disabled ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Disabled_Fades_As_One_And_Tells_The_Item() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.IsEnabled = false; });
        var (scene, page) = Show(select, inItem: true);
        using (scene)
        {
            Assert.Equal(0.38, Assert.IsAssignableFrom<ISolidColorBrush>(select.OpacityMask).Opacity, 3);
            var item = select.FindAncestorOfType<AvonicItem>()!;
            Assert.Contains(":item-interactive-disabled", item.Classes);

            select.IsEnabled = true;
            Assert.DoesNotContain(":item-interactive-disabled", item.Classes);
        }
    });

    // ── B11: validation ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Touched_And_Invalid_Show_The_Error_In_The_Invalid_Colour() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.HelperText = "Helper"; s.ErrorText = "Required"; });
        using var scene = Show(select).Scene;
        var helper = Part<TextBlock>(select, "PART_HelperText");
        var error = Part<TextBlock>(select, "PART_ErrorText");
        Assert.True(helper.IsVisible);
        Assert.False(error.IsVisible);
        Assert.Equal("Helper", ControlAutomationPeer.CreatePeerForElement(select).GetHelpText());

        select.Classes.Add(AvonicSelect.TouchedClass);
        select.Classes.Add(AvonicSelect.InvalidClass);
        Dispatcher.UIThread.RunJobs();
        Assert.False(helper.IsVisible);
        Assert.True(error.IsVisible);
        Assert.Equal("Required", ControlAutomationPeer.CreatePeerForElement(select).GetHelpText());
        // The icon and the bottom border take the invalid colour (ref/page.json: rgb(197, 0, 15)).
        var red = Color.FromRgb(197, 0, 15);
        Assert.Equal(red, ((ISolidColorBrush)Part<AvonicIcon>(select, "PART_Icon").Foreground!).Color);
        Assert.Equal(red, ((ISolidColorBrush)Part<Border>(select, "PART_Bottom").BorderBrush!).Color);
    });

    // ── B12: layout ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_Justify_And_Placement_Pack_The_Label_And_The_Text() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.Value = "apple"; });
        var (scene, page) = Show(select);
        using (scene)
        {
            double LabelX() { Dispatcher.UIThread.RunJobs(); return Box(Part<Border>(select, "PART_Label"), page).X; }
            double TextX() { Dispatcher.UIThread.RunJobs(); return Box(Part<Control>(select, "PART_Container"), page).X; }

            // space-between: the label at the start, the text at the end (ref/page.json: 316.22 in a 358-wide select at 16).
            Assert.Equal(16, LabelX(), 0.6);
            Assert.Equal(316.22, TextX(), 0.6);

            select.Justify = SelectJustify.Start;
            Assert.Equal(16 + 32.02 + 16, TextX(), 0.6);

            select.Justify = SelectJustify.End;
            Assert.Equal(316.22, TextX(), 0.6);
            Assert.InRange(LabelX(), 316.22 - 16 - 32.02 - 1, 316.22 - 16 - 32.02 + 1);

            // end: reversed, the text first.
            select.Justify = null;
            select.LabelPlacement = SelectLabelPlacement.End;
            Assert.Equal(16, TextX(), 0.6);
            Assert.Equal(374 - 32.02, LabelX(), 0.6);

            // fixed: 100 wide, then 16.
            select.LabelPlacement = SelectLabelPlacement.Fixed;
            select.Justify = SelectJustify.Start;
            Assert.Equal(16 + 116, TextX(), 0.6);
        }
    });

    [AvaloniaFact]
    public void B12_A_Long_Text_Gives_Way_Before_The_Label() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.SelectedText = new string('W', 80); });
        var (scene, page) = Show(select);
        using (scene)
        {
            Assert.Equal(32.02, Part<Border>(select, "PART_Label").Bounds.Width, 0.6);
            Assert.Equal(358 - 32.02 - 16, Part<Control>(select, "PART_Container").Bounds.Width, 0.6);
        }
    });

    // ── B14: the highlight ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_The_Highlight_Grows_When_Focused_And_Not_In_An_Item_Or_With_The_Outline() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => s.Label = "Fruit");
        using var scene = Show(select).Scene;
        var highlight = Part<Border>(select, "PART_Highlight");
        highlight.Transitions = [];
        Assert.True(highlight.IsVisible);
        Assert.Equal(0, highlight.GetValue(Css.ScaleProperty));
        select.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, highlight.GetValue(Css.ScaleProperty));

        select.Fill = SelectFill.Outline;
        Dispatcher.UIThread.RunJobs();
        Assert.False(highlight.IsVisible);
    });

    // ── B15: automation ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_The_Name_Is_The_Label_Then_The_Text_And_Required_Is_Said()
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.Placeholder = "Pick one"; });
        using var scene = new Scene(new StackPanel { Children = { select } });
        var peer = ControlAutomationPeer.CreatePeerForElement(select);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Fruit, Pick one", peer.GetName());
        select.Value = "apple";
        Assert.Equal("Fruit, Apple", peer.GetName());

        AutomationProperties.SetName(select, "Choose");
        Assert.Equal("Choose, Apple", peer.GetName());
        // An empty aria-label is still the app's: ariaLabel joins it all the same.
        AutomationProperties.SetName(select, "");
        Assert.Equal(", Apple", peer.GetName());

        Assert.False(AutomationProperties.GetIsRequiredForForm(select));
        select.Required = true;
        Assert.True(AutomationProperties.GetIsRequiredForForm(select));

        // The text and the icon are aria-hidden.
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(Part<SelectTextBlock>(select, "PART_Text")));
    }

    // ── B16: the item ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_In_An_Item_It_Fills_The_Row_With_No_Highlight() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.Value = "apple"; });
        var (scene, page) = Show(select, inItem: true);
        using (scene)
        {
            Assert.False(Part<Border>(select, "PART_Highlight").IsVisible);
            AssertBox(16, 9.5, 358, 44, Box(select, page));
        }
    });

    [AvaloniaFact]
    public void B13_An_Apps_Background_And_Border_Colour_Win_In_Every_State() => InstalledTheme.Run(_ =>
    {
        // --background and --border-color on the host beat the shadow :host(.select-fill-solid:hover) and .has-focus rules.
        var red = new SolidColorBrush(Colors.Red);
        var blue = new SolidColorBrush(Colors.Blue);
        var select = Select("s", s => { s.Label = "Solid"; s.Fill = SelectFill.Solid; });
        select.Resources["Avonic.Select.Background"] = red;
        select.Resources["Avonic.Select.BorderColor"] = blue;
        using var scene = Show(select).Scene;
        var wrapper = Part<Border>(select, "PART_Wrapper");
        wrapper.Transitions = [];
        ((Avalonia.Controls.IPseudoClasses)select.Classes).Set(":ion-hover", true);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(red, wrapper.Background);
        Assert.Same(blue, wrapper.BorderBrush);
        ((Avalonia.Controls.IPseudoClasses)select.Classes).Set(":ion-hover", false);
        select.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Same(red, wrapper.Background);
        Assert.Same(blue, wrapper.BorderBrush);
    });

    [AvaloniaFact]
    public void B13_Solid_Fill_Has_Its_Background_And_The_Highlight_Border_When_Focused() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Solid"; s.Fill = SelectFill.Solid; s.LabelPlacement = SelectLabelPlacement.Floating; });
        using var scene = Show(select).Scene;
        var wrapper = Part<Border>(select, "PART_Wrapper");
        wrapper.Transitions = [];
        Assert.Equal(Color.FromRgb(242, 242, 242), ((ISolidColorBrush)wrapper.Background!).Color);
        Assert.Equal(new Thickness(0, 0, 0, 1), wrapper.BorderThickness);
        Assert.Equal(new Thickness(16, 0, 16, 0), wrapper.Padding);
        var label = Part<Border>(select, "PART_Label");
        label.Transitions = [];
        select.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Color.FromRgb(217, 217, 217), ((ISolidColorBrush)wrapper.Background!).Color);
        Assert.Same(select.HighlightBrush, wrapper.BorderBrush);
        // A floating label takes the highlight colour while focused.
        Assert.Same(select.HighlightBrush, label.GetValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty));
    });

    [AvaloniaFact]
    public void B9_A_Floating_Labels_Placeholder_Is_Hidden_Until_It_Floats() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.LabelPlacement = SelectLabelPlacement.Floating; s.Placeholder = "Pick one"; });
        using var scene = Show(select).Scene;
        var text = Part<SelectTextBlock>(select, "PART_Text");
        text.Transitions = [];
        Assert.Equal(0, text.Opacity);
        select.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0.6, text.Opacity, 3);
    });

    [AvaloniaFact]
    public void B6_The_Icon_Turns_Half_Round_While_Open_Unless_There_Is_An_Expanded_Icon() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => s.Label = "Fruit");
        using var scene = Show(select).Scene;
        var icon = Part<AvonicIcon>(select, "PART_Icon");
        icon.Transitions = [];
        Assert.Equal(1, icon.RenderTransform!.Value.M11, 3);
        ((Avalonia.Controls.IPseudoClasses)select.Classes).Set(":select-expanded", true);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(-1, icon.RenderTransform!.Value.M11, 3);
        ((Avalonia.Controls.IPseudoClasses)select.Classes).Set(":has-expanded-icon", true);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, icon.RenderTransform!.Value.M11, 3);
    });

    [AvaloniaFact]
    public void B11_A_Colour_Is_The_Highlight_Unless_The_App_Sets_The_Focused_Highlight() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Fruit"; s.Color = "danger"; });
        using var scene = Show(select).Scene;
        Assert.Equal(Color.FromRgb(197, 0, 15), ((ISolidColorBrush)select.HighlightBrush!).Color);
        var orange = new SolidColorBrush(Colors.Orange);
        select.Resources["Avonic.Select.HighlightColorFocused"] = orange;
        Dispatcher.UIThread.RunJobs();
        Assert.Same(orange, select.HighlightBrush);
    });

    [AvaloniaFact]
    public void B16_In_A_Coloured_Item_The_Icon_Inherits_At_Rest_And_Is_Step_500_When_Focused() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => s.Label = "Fruit");
        using var scene = Show(select, inItem: true).Scene;
        select.FindAncestorOfType<AvonicItem>()!.Color = "primary";
        // in-item-color is read at render, which is the attach here.
        var item = select.FindAncestorOfType<AvonicItem>()!;
        item.Content = null;
        item.Content = select;
        Dispatcher.UIThread.RunJobs();
        var icon = Part<AvonicIcon>(select, "PART_Icon");
        icon.Transitions = [];
        Assert.Same(select.Foreground, icon.Foreground);
        select.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Color.FromRgb(128, 128, 128), ((ISolidColorBrush)icon.Foreground!).Color);
    });

    [AvaloniaFact]
    public void B13_The_Outline_Takes_The_Highlight_At_Its_Height_When_Focused() => InstalledTheme.Run(_ =>
    {
        var select = Select("s", s => { s.Label = "Outline"; s.Fill = SelectFill.Outline; s.LabelPlacement = SelectLabelPlacement.Floating; });
        using var scene = Show(select).Scene;
        var start = Part<Border>(select, "PART_OutlineStart");
        Assert.Equal(new Thickness(1, 1, 0, 1), start.BorderThickness);
        select.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Same(select.HighlightBrush, start.BorderBrush);
        Assert.Equal(new Thickness(2, 2, 0, 2), start.BorderThickness);
    });
}