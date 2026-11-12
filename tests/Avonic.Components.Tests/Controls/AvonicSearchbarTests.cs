using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-searchbar in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-searchbar/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicSearchbarTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────

    private sealed class Log
    {
        private readonly List<string> _all = [];

        public static Log Of(AvonicSearchbar searchbar)
        {
            var log = new Log();
            searchbar.IonInput += (_, e) => log._all.Add($"input:{e.Value}");
            searchbar.IonChange += (_, e) => log._all.Add($"change:{e.Value}");
            searchbar.IonFocus += (_, _) => log._all.Add("focus");
            searchbar.IonBlur += (_, _) => log._all.Add("blur");
            searchbar.IonClear += (_, _) => log._all.Add("clear");
            searchbar.IonCancel += (_, _) => log._all.Add("cancel");
            return log;
        }

        public string[] Take()
        {
            var all = _all.ToArray();
            _all.Clear();
            return all;
        }
    }

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static TextBox Native(AvonicSearchbar searchbar) => Part<TextBox>(searchbar, "PART_NativeInput");

    private static Border ClearButton(AvonicSearchbar searchbar) => Part<Border>(searchbar, "PART_ClearButton");

    private static Border CancelButton(AvonicSearchbar searchbar) => Part<Border>(searchbar, "PART_CancelButton");

    private static AvonicIcon SearchIcon(AvonicSearchbar searchbar) => Part<AvonicIcon>(searchbar, "PART_SearchIcon");

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual, double tolerance = 0.6)
    {
        Assert.InRange(actual.X, x - tolerance, x + tolerance);
        Assert.InRange(actual.Y, y - tolerance, y + tolerance);
        Assert.InRange(actual.Width, width - tolerance, width + tolerance);
        Assert.InRange(actual.Height, height - tolerance, height + tolerance);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Render()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();
    }

    private static IInputElement? Focused(Visual visual) => TopLevel.GetTopLevel(visual)?.FocusManager?.GetFocusedElement();

    private static void Press(Scene scene, Visual target)
    {
        Render();
        scene.Window.MouseDown(Bounds(target, scene.Window).Center, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static void Release(Scene scene, Visual target)
    {
        scene.Window.MouseUp(Bounds(target, scene.Window).Center, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static void Click(Scene scene, Visual target)
    {
        Press(scene, target);
        Release(scene, target);
    }

    private static void Key(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        Settle();
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Settle();
    }

    private static void Type(Scene scene, PhysicalKey key, string text)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyTextInput(text);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Settle();
    }

    /// <summary>A searchbar on the stub, with a text box after it to take focus away.</summary>
    private static (AvonicSearchbar Searchbar, TextBox Other, Scene Scene) Stub(AvonicSearchbar? searchbar = null)
    {
        searchbar ??= new AvonicSearchbar();
        var other = new TextBox();
        var scene = new Scene(new StackPanel { Children = { searchbar, other } });
        Settle();
        return (searchbar, other, scene);
    }

    /// <summary>The page's 16 px Roboto.</summary>
    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { [TextElement.FontSizeProperty] = 16.0 };
        page.Bind(TextElement.FontFamilyProperty, page.GetResourceObservable("Avonic.FontFamily"));
        foreach (var child in children)
        {
            page.Children.Add(child);
        }

        return page;
    }

    private static Color BrushColor(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Style AppStyle(string pseudoClass, AvaloniaProperty property, object value) =>
        new(selector => selector.OfType<AvonicSearchbar>().Class(pseudoClass)) { Setters = { new Setter(property, value) } };

    // ── 1 to 5: the value and its events ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B01_Typing_Sets_The_Value_And_Raises_IonInput_With_Each_Edit_After_IonFocus()
    {
        var (searchbar, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(searchbar);

        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Type(scene, PhysicalKey.B, "b");

        Assert.Equal(["focus", "input:a", "input:ab"], log.Take());
        Assert.Equal("ab", searchbar.Value);
    }

    [AvaloniaFact]
    public void B02_Enter_Raises_IonChange_Once_After_An_Edit_With_A_Value_Set_In_Code_Committed()
    {
        var (searchbar, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(searchbar);
        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Type(scene, PhysicalKey.B, "b");
        log.Take();

        Key(scene, PhysicalKey.Enter);
        Assert.Equal(["change:ab"], log.Take());
        Key(scene, PhysicalKey.Enter);
        Assert.Empty(log.Take());

        // Measured D: an edit, then a value set in code, then Enter commits the value in code.
        Type(scene, PhysicalKey.Q, "q");
        searchbar.Value = "w";
        Key(scene, PhysicalKey.Enter);
        Assert.Equal(["input:abq", "change:w"], log.Take());

        // With no edit, Enter commits nothing, even after a value in code.
        searchbar.Value = "z";
        Key(scene, PhysicalKey.Enter);
        Assert.Empty(log.Take());
    }

    [AvaloniaFact]
    public void B03_Blur_Orders_The_Native_Change_Before_IonBlur_And_Ionics_Own_After_It()
    {
        var (searchbar, other, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(searchbar);

        // The user's edit: the native change, then ionBlur.
        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.C, "c");
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:c", "change:c", "blur"], log.Take());

        // No change: only focus and blur.
        searchbar.SetFocus();
        Settle();
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur"], log.Take());

        // Measured B: a value set in code while focused, with no edit: ionBlur, then Ionic's own ionChange.
        searchbar.SetFocus();
        Settle();
        searchbar.Value = "y";
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur", "change:y"], log.Take());

        // Measured A: an edit undone commits nothing.
        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Key(scene, PhysicalKey.Backspace);
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:ya", "input:y", "blur"], log.Take());

        // Measured C: an edit, then the value at focus set back in code, commits nothing.
        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.Z, "z");
        searchbar.Value = "y";
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:yz", "blur"], log.Take());

        // An edit that was never committed is forgotten at the next focus: a value in code is Ionic's change, after ionBlur.
        searchbar.SetFocus();
        Settle();
        searchbar.Value = "q";
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur", "change:q"], log.Take());
    }

    [AvaloniaFact]
    public void B04_A_Value_Set_In_Code_Raises_Nothing_Shows_And_Can_Replace_An_Edit()
    {
        var (searchbar, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(searchbar);

        searchbar.Value = "prog";
        Settle();
        Assert.Empty(log.Take());
        Assert.Equal("prog", Native(searchbar).Text);

        searchbar.Value = null;
        Settle();
        Assert.Equal("", Native(searchbar).Text);

        searchbar.IonInput += (_, e) => searchbar.Value = e.Value?.ToUpperInvariant();
        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Assert.Equal("A", searchbar.Value);
        Assert.Equal("A", Native(searchbar).Text);
    }

    [AvaloniaFact]
    public void B05_Debounce_Raises_IonInput_Once_With_The_Last_Value_After_The_Wait()
    {
        using var clock = new TestClock();
        var (searchbar, _, scene) = Stub(new AvonicSearchbar { Debounce = 200 });
        using var _ = scene;
        var log = Log.Of(searchbar);
        searchbar.SetFocus();
        Settle();
        log.Take();

        clock.At(0);
        Type(scene, PhysicalKey.A, "a");
        clock.At(50);
        Type(scene, PhysicalKey.B, "b");
        clock.At(249);
        Assert.Empty(log.Take());

        clock.At(250);
        Assert.Equal(["input:ab"], log.Take());

        searchbar.Debounce = null;
        Type(scene, PhysicalKey.C, "c");
        Assert.Equal(["input:abc"], log.Take());
    }

    // ── 6 to 8: the clear button ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B06_The_Clear_Button_Shows_With_A_Value_As_ShowClearButton_Says()
    {
        var (searchbar, other, scene) = Stub();
        using var _ = scene;
        var clear = ClearButton(searchbar);

        // Always, the default: only with a value.
        Assert.False(clear.IsVisible);
        searchbar.Value = "x";
        Assert.True(clear.IsVisible);

        // Focus: only while focused.
        searchbar.ShowClearButton = SearchbarShowClearButton.Focus;
        Assert.False(clear.IsVisible);
        searchbar.SetFocus();
        Settle();
        Assert.True(clear.IsVisible);
        other.Focus();
        Settle();
        Assert.False(clear.IsVisible);

        // Never.
        searchbar.ShowClearButton = SearchbarShowClearButton.Never;
        searchbar.SetFocus();
        Settle();
        Assert.False(clear.IsVisible);
    }

    [AvaloniaFact]
    public void B07_A_Click_On_Clear_Raises_IonClear_Then_Clears_After_64ms_And_Focuses()
    {
        using var clock = new TestClock();
        var (searchbar, other, scene) = Stub(new AvonicSearchbar { Value = "prog" });
        using var _ = scene;
        var log = Log.Of(searchbar);
        var clear = ClearButton(searchbar);

        // Unfocused: ionClear at once, and nothing else yet; the press takes no focus.
        clock.At(0);
        Press(scene, clear);
        Assert.Null(Focused(searchbar));
        Release(scene, clear);
        Assert.Equal(["clear"], log.Take());
        clock.At(63);
        Assert.Equal("prog", searchbar.Value);
        Assert.Empty(log.Take());

        // Measured: ionInput "" and then ionFocus, 64 ms on.
        clock.At(64);
        Assert.Equal("", searchbar.Value);
        Assert.Equal(["input:", "focus"], log.Take());
        Assert.Same(Native(searchbar), Focused(searchbar));

        // focusedValue is the old value, so the blur commits "": after ionBlur, as it isn't the native change.
        other.Focus();
        Settle();
        Assert.Equal(["blur", "change:"], log.Take());

        // Focused: the press keeps focus where it is, and nothing focuses again.
        searchbar.Value = "x";
        searchbar.SetFocus();
        Settle();
        log.Take();
        clock.At(100);
        Click(scene, clear);
        Assert.Same(Native(searchbar), Focused(searchbar));
        clock.At(164);
        Assert.Equal(["clear", "input:"], log.Take());

        // A second click within 64 ms restarts the wait; with no value, only ionClear.
        searchbar.Value = "y";
        Render();
        clock.At(200);
        Click(scene, clear);
        clock.At(240);
        Click(scene, clear);
        clock.At(264);
        Assert.Equal("y", searchbar.Value);
        clock.At(304);
        Assert.Equal(["clear", "clear", "input:"], log.Take());
    }

    [AvaloniaFact]
    public void B08_The_Clear_Button_Is_A_Tab_Stop_That_Enter_Clicks_And_Fades_While_Focused() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var searchbar = new AvonicSearchbar { Value = "T" };
        using var scene = new Scene(Page(searchbar));
        Settle();
        var log = Log.Of(searchbar);
        var clear = ClearButton(searchbar);
        Assert.Equal(1, clear.Opacity);

        searchbar.SetFocus();
        Settle();
        Key(scene, PhysicalKey.Tab);
        Assert.Same(clear, Focused(searchbar));
        Assert.Equal(0.5, clear.Opacity);

        // Enter clicks it: cleared, and the input focused again.
        log.Take();
        Key(scene, PhysicalKey.Enter);
        clock.At(64);
        Assert.Equal("", searchbar.Value);
        Assert.Equal(["clear", "input:", "focus"], log.Take());
        Assert.Same(Native(searchbar), Focused(searchbar));
    });

    // outline: none on both buttons. The adorner comes from the app's base theme, which TestApp doesn't load, so this adds it
    // underneath. FocusAdorner reads null whether or not it's set, so only the layer shows what's really drawn.
    [AvaloniaFact]
    public void B08_The_Buttons_Draw_No_Focus_Adorner_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var searchbar = new AvonicSearchbar { Value = "v", ShowCancelButton = SearchbarShowCancelButton.Always };
                var other = new Button();
                using var scene = new Scene(Page(searchbar, other));
                Settle();
                var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(searchbar)!;

                searchbar.SetFocus();
                Settle();
                Key(scene, PhysicalKey.Tab);
                Assert.Same(CancelButton(searchbar), Focused(searchbar));
                Assert.Empty(layer.Children);
                Key(scene, PhysicalKey.Tab);
                Assert.Same(ClearButton(searchbar), Focused(searchbar));
                Assert.Empty(layer.Children);

                // Proves the base theme really does give a focused control one.
                Key(scene, PhysicalKey.Tab);
                Assert.True(other.IsFocused);
                Assert.Single(layer.Children);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    // ── 9 to 11: the cancel button, and Escape ───────────────────────────────────────────────

    [AvaloniaFact]
    public void B09_The_Cancel_Button_Shows_With_Focus_Or_Always_Unless_Never_And_Hides_The_Search_Icon()
    {
        var (searchbar, other, scene) = Stub();
        using var _ = scene;
        var cancel = CancelButton(searchbar);
        var icon = SearchIcon(searchbar);

        // Never, the default: not there even with focus.
        searchbar.SetFocus();
        Settle();
        Assert.False(cancel.IsVisible);
        Assert.True(icon.IsVisible);

        // Focus: while focused, in place of the search icon.
        searchbar.ShowCancelButton = SearchbarShowCancelButton.Focus;
        Assert.True(cancel.IsVisible);
        Assert.False(icon.IsVisible);
        other.Focus();
        Settle();
        Assert.False(cancel.IsVisible);
        Assert.True(icon.IsVisible);

        // Always.
        searchbar.ShowCancelButton = SearchbarShowCancelButton.Always;
        Assert.True(cancel.IsVisible);
        Assert.False(icon.IsVisible);
    }

    [AvaloniaFact]
    public void B10_A_Press_On_Cancel_Raises_IonCancel_And_IonClear_Then_Clears_Commits_And_Blurs()
    {
        using var clock = new TestClock();
        var (searchbar, _, scene) = Stub(new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always, Value = "Old" });
        using var _ = scene;
        var log = Log.Of(searchbar);
        var cancel = CancelButton(searchbar);

        // Unfocused with a value: on the press, not the release; then ionInput and ionChange.
        clock.At(0);
        Press(scene, cancel);
        Assert.Equal(["cancel", "clear"], log.Take());
        Release(scene, cancel);
        Assert.Empty(log.Take());
        clock.At(64);
        Assert.Equal(["input:", "change:"], log.Take());
        Assert.Null(Focused(searchbar));

        // Focused after an edit: the press keeps focus until the clear, then the blur commits it natively.
        searchbar.Value = "Old";
        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.X, "X");
        log.Take();
        clock.At(100);
        Press(scene, cancel);
        Assert.Same(Native(searchbar), Focused(searchbar));
        Release(scene, cancel);
        clock.At(164);
        Assert.Equal(["cancel", "clear", "input:", "change:", "blur"], log.Take());
        Assert.Null(Focused(searchbar));

        // Focused with no value: only the blur after.
        searchbar.SetFocus();
        Settle();
        log.Take();
        clock.At(200);
        Click(scene, cancel);
        clock.At(264);
        Assert.Equal(["cancel", "clear", "blur"], log.Take());

        // Enter on it does nothing: it listens to presses only.
        searchbar.Value = "v";
        cancel.Focus(NavigationMethod.Tab);
        Settle();
        Assert.Same(cancel, Focused(searchbar));
        log.Take();
        Key(scene, PhysicalKey.Enter);
        Key(scene, PhysicalKey.Space);
        clock.At(400);
        Assert.Empty(log.Take());
        Assert.Equal("v", searchbar.Value);

        // An ancestor that stops the press on its way down, as stopPropagation in the capture phase: the button never hears it.
        scene.Window.AddHandler(InputElement.PointerPressedEvent, (_, e) => e.Handled = true, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Click(scene, cancel);
        clock.At(500);
        Assert.Empty(log.Take());
        Assert.Equal("v", searchbar.Value);
    }

    [AvaloniaFact]
    public void B11_Escape_Clears_A_Search_Field_As_An_Edit_But_Not_A_Text_Field()
    {
        var (searchbar, other, scene) = Stub(new AvonicSearchbar { Value = "x" });
        using var _ = scene;
        var log = Log.Of(searchbar);
        searchbar.SetFocus();
        Settle();

        Key(scene, PhysicalKey.Escape);
        Assert.Equal("", searchbar.Value);
        Key(scene, PhysicalKey.Escape);
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:", "change:", "blur"], log.Take());

        searchbar.Type = SearchbarType.Text;
        searchbar.Value = "tt";
        searchbar.SetFocus();
        Settle();
        Key(scene, PhysicalKey.Escape);
        Assert.Equal("tt", searchbar.Value);
    }

    [AvaloniaFact]
    public void B11_An_Apps_Handler_That_Handles_The_Key_Stops_Escapes_Clear_And_Enters_Change()
    {
        var (searchbar, _, scene) = Stub(new AvonicSearchbar { Value = "x" });
        using var _ = scene;
        var log = Log.Of(searchbar);

        // preventDefault() on keydown: the default action doesn't run.
        searchbar.AddHandler(InputElement.KeyDownEvent, (_, e) => e.Handled = e.Key is Avalonia.Input.Key.Escape or Avalonia.Input.Key.Enter, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        searchbar.SetFocus();
        Settle();
        Key(scene, PhysicalKey.Escape);
        Assert.Equal("x", searchbar.Value);
        Type(scene, PhysicalKey.Y, "y");
        Key(scene, PhysicalKey.Enter);
        Assert.Equal(["focus", "input:xy"], log.Take());
    }

    [AvaloniaFact]
    public void B04_A_Value_Set_During_A_Composition_Is_Shown_As_SearchbarJs_Doesnt_Wait()
    {
        var (searchbar, _, scene) = Stub(new AvonicSearchbar { Value = "a" });
        using var _ = scene;
        var presenter = Native(searchbar).GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().First();
        presenter.PreeditText = "ka";

        searchbar.Value = "b";

        Assert.Equal("b", Native(searchbar).Text);
    }

    // ── 12, 13: methods and the item ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_SetFocus_Focuses_The_Native_Input_That_GetInputElement_Returns()
    {
        var (searchbar, _, scene) = Stub();
        using var _ = scene;
        var native = searchbar.GetInputElement();
        Assert.Same(Native(searchbar), native);

        searchbar.SetFocus();
        Settle();
        Assert.Same(native, Focused(searchbar));
        Assert.False(searchbar.Focusable);
    }

    [AvaloniaFact]
    public void B12_GetInputElement_Applies_The_Template_First()
    {
        // An invisible control isn't measured, so its template isn't applied until asked for.
        var searchbar = new AvonicSearchbar { IsVisible = false };
        using var scene = new Scene(new StackPanel { Children = { searchbar } });
        Settle();
        Assert.Empty(searchbar.GetVisualChildren());
        Assert.NotNull(searchbar.GetInputElement());
    }

    [AvaloniaFact]
    public void B13_An_Item_Takes_Its_IonStyle_And_Counts_It_As_An_Input()
    {
        var searchbar = new AvonicSearchbar();
        var item = new AvonicItem { Content = searchbar };
        using var scene = new Scene(new AvonicList { Items = { item } });
        Settle();
        Assert.Contains(":item-searchbar", item.Classes);
        Assert.DoesNotContain(":item-multiple-inputs", item.Classes);

        var two = new AvonicItem { Content = new StackPanel { Children = { new AvonicSearchbar(), new AvonicInput() } } };
        using var scene2 = new Scene(new AvonicList { Items = { two } });
        Settle();
        Assert.Contains(":item-multiple-inputs", two.Classes);

        // A tap on the item's padding doesn't reach it: getFirstInteractive doesn't look for a searchbar.
        Assert.False(item.Focusable);
        Assert.Null(Focused(item));
    }

    // ── 14 to 16: the look ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_The_Md_Layout_Matches_Chromium() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicSearchbar { Value = "Value" };
        var cancel = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always, Value = "Cancel" };
        var inToolbar = new AvonicSearchbar { Value = "t" };
        var rtl = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always, Value = "RTL", FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(Page(plain, cancel, new AvonicToolbar { Content = inToolbar }, rtl));
        Settle();
        Render();

        // Measured: the host 58 high with 8 px padding, the input 374 × 42, the search icon 21 px at 16, 11.
        AssertBox(0, 0, 390, 58, Bounds(plain, plain));
        AssertBox(8, 8, 374, 42, Bounds(Native(plain), plain));
        AssertBox(24, 19, 21, 21, Bounds(SearchIcon(plain), plain));
        AssertBox(347, 8, 22, 42, Bounds(ClearButton(plain), plain));
        AssertBox(347, 18, 22, 22, Bounds(Part<AvonicIcon>(plain, "PART_ClearIcon"), plain));

        // The cancel button: 24 × 42 at 9, its icon 24 px.
        AssertBox(17, 8, 24, 42, Bounds(CancelButton(cancel), cancel));
        AssertBox(17, 17, 24, 24, Bounds(Part<AvonicIcon>(cancel, "PART_CancelIcon"), cancel));

        // :host-context(ion-toolbar): 3 px and 7 px.
        AssertBox(0, 0, 390, 48, Bounds(inToolbar, inToolbar));
        AssertBox(7, 3, 376, 42, Bounds(Native(inToolbar), inToolbar));
        AssertBox(348, 3, 22, 42, Bounds(ClearButton(inToolbar), inToolbar));

        // RTL mirrors: the cancel button at 349, the clear button at 21. The default arrow isn't flipped, as Ionic's SVG.
        // On screen, as a transform to the mirrored host itself is mirrored too.
        var top = Bounds(rtl, scene.Window).Y;
        AssertBox(349, top + 8, 24, 42, Bounds(CancelButton(rtl), scene.Window));
        AssertBox(21, top + 8, 22, 42, Bounds(ClearButton(rtl), scene.Window));
        Assert.False(Part<AvonicIcon>(rtl, "PART_CancelIcon").FlipRtl);

        // The text: 16 px, centred in 42.
        Assert.Equal(16, Native(plain).FontSize);
        var presenter = Native(plain).GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().First();
        Assert.InRange(Bounds(presenter, plain).Center.Y, 28.5, 29.5);
        Assert.Equal(55, Bounds(presenter, plain).X - 8, 0.6);
    });

    [AvaloniaFact]
    public void B15_The_Md_Colours_Match_Chromium_And_A_Color_Takes_Over() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always, Value = "v" };
        var empty = new AvonicSearchbar();
        var primary = new AvonicSearchbar { Color = "primary", ShowCancelButton = SearchbarShowCancelButton.Always, Value = "P" };
        using var scene = new Scene(Page(plain, empty, primary));
        Settle();

        Assert.Equal(Color.Parse("#262626"), BrushColor(plain.Foreground));
        Assert.Equal(Color.Parse("#262626"), BrushColor(Native(plain).Foreground));
        Assert.Equal(Color.Parse("#666666"), BrushColor(SearchIcon(empty).Foreground));
        Assert.Equal(Color.Parse("#1A1A1A"), BrushColor(Part<AvonicIcon>(plain, "PART_CancelIcon").Foreground));
        Assert.Equal(Color.Parse("#262626"), BrushColor(Part<AvonicIcon>(plain, "PART_ClearIcon").Foreground));
        Assert.Equal(Colors.White, BrushColor(Native(plain).Background));
        Assert.Equal(new CornerRadius(2), Native(plain).CornerRadius);
        var box = Native(plain).GetVisualChildren().OfType<Border>().First();
        Assert.Equal(3, box.BoxShadow.Count);
        Assert.False(Native(plain).ClipToBounds);

        // The placeholder: "Search", the input's colour at 0.6.
        Assert.False(ClearButton(empty).IsVisible);
        Assert.True(ClearButton(plain).IsVisible);
        var placeholder = Part<TextBlock>(empty, "PART_Placeholder");
        Assert.Equal("Search", placeholder.Text);
        Assert.Equal(Color.Parse("#262626"), BrushColor(placeholder.Foreground));
        Assert.Equal(0.6, placeholder.Opacity, 3);

        // primary: the input its base, the text and every icon its contrast.
        Assert.Equal(Primary, BrushColor(Native(primary).Background));
        Assert.Equal(Colors.White, BrushColor(Native(primary).Foreground));
        Assert.Equal(Colors.White, BrushColor(Part<AvonicIcon>(primary, "PART_CancelIcon").Foreground));
        Assert.Equal(Colors.White, BrushColor(Part<AvonicIcon>(primary, "PART_ClearIcon").Foreground));
        primary.ShowCancelButton = SearchbarShowCancelButton.Never;
        Settle();
        Assert.Equal(Colors.White, BrushColor(SearchIcon(primary).Foreground));
    });

    [AvaloniaFact]
    public void B15_The_Variables_Restyle_It() => InstalledTheme.Run(_ =>
    {
        var searchbar = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always, Value = "v" };
        searchbar.Resources["Avonic.Searchbar.ClearButtonColor"] = new SolidColorBrush(Colors.Red);
        searchbar.Resources["Avonic.Searchbar.Background"] = new SolidColorBrush(Colors.Blue);
        searchbar.Resources["Avonic.Searchbar.BorderRadius"] = new CornerRadius(9);
        searchbar.Resources["Avonic.Searchbar.Color"] = new SolidColorBrush(Colors.Green);
        searchbar.Resources["Avonic.Searchbar.CancelButtonColor"] = new SolidColorBrush(Colors.Orange);
        searchbar.Resources["Avonic.Searchbar.PlaceholderColor"] = new SolidColorBrush(Colors.Purple);
        searchbar.Resources["Avonic.Searchbar.PlaceholderOpacity"] = 0.3;
        searchbar.Resources["Avonic.Searchbar.BoxShadow"] = BoxShadows.Parse("0 1 1 0 #FF000000");
        var empty = new AvonicSearchbar();
        empty.Resources["Avonic.Searchbar.IconColor"] = new SolidColorBrush(Colors.Teal);
        using var scene = new Scene(Page(searchbar, empty));
        Settle();

        Assert.Equal(Colors.Red, BrushColor(Part<AvonicIcon>(searchbar, "PART_ClearIcon").Foreground));
        Assert.Equal(Colors.Blue, BrushColor(Native(searchbar).Background));
        Assert.Equal(new CornerRadius(9), Native(searchbar).CornerRadius);
        Assert.Equal(Colors.Green, BrushColor(Native(searchbar).Foreground));
        Assert.Equal(Colors.Orange, BrushColor(Part<AvonicIcon>(searchbar, "PART_CancelIcon").Foreground));
        Assert.Equal(Colors.Purple, BrushColor(Part<TextBlock>(searchbar, "PART_Placeholder").Foreground));
        Assert.Equal(0.3, Part<TextBlock>(searchbar, "PART_Placeholder").Opacity, 3);
        Assert.Equal(1, Native(searchbar).GetVisualChildren().OfType<Border>().First().BoxShadow.Count);
        Assert.Equal(Colors.Teal, BrushColor(SearchIcon(empty).Foreground));
    });

    [AvaloniaFact]
    public void B16_Disabled_Fades_The_Whole_To_04_And_Takes_No_Focus() => InstalledTheme.Run(_ =>
    {
        var searchbar = new AvonicSearchbar { IsEnabled = false, Value = "d" };
        using var scene = new Scene(Page(searchbar));
        Settle();

        Assert.Equal(0.4, Assert.IsAssignableFrom<ISolidColorBrush>(searchbar.OpacityMask).Opacity, 3);
        searchbar.SetFocus();
        Settle();
        Assert.Null(Focused(searchbar));

        searchbar.IsEnabled = true;
        Assert.Null(searchbar.OpacityMask);
    });

    // ── 17, 18: the platform and automation ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_The_Type_And_Hints_Reach_The_Native_Input()
    {
        var (searchbar, _, scene) = Stub();
        using var _ = scene;
        var native = Native(searchbar);

        Assert.Equal(TextInputContentType.Search, TextInputOptions.GetContentType(native));
        Assert.Equal("Search", native.PlaceholderText);
        searchbar.Type = SearchbarType.Password;
        Assert.Equal('•', native.PasswordChar);
        Assert.True(TextInputOptions.GetIsSensitive(native));
        Assert.Equal(TextInputContentType.Password, TextInputOptions.GetContentType(native));
        searchbar.Type = SearchbarType.Email;
        Assert.Equal(default, native.PasswordChar);
        Assert.Equal(TextInputContentType.Email, TextInputOptions.GetContentType(native));
        searchbar.Type = SearchbarType.Tel;
        Assert.Equal(TextInputContentType.Digits, TextInputOptions.GetContentType(native));
        searchbar.Type = SearchbarType.Url;
        Assert.Equal(TextInputContentType.Url, TextInputOptions.GetContentType(native));
        searchbar.Type = SearchbarType.Number;
        Assert.Equal(TextInputContentType.Number, TextInputOptions.GetContentType(native));
        searchbar.Type = SearchbarType.Text;
        Assert.Equal(TextInputContentType.Normal, TextInputOptions.GetContentType(native));

        searchbar.Inputmode = SearchbarInputmode.Decimal;
        Assert.Equal(TextInputContentType.Number, TextInputOptions.GetContentType(native));
        searchbar.Inputmode = SearchbarInputmode.Search;
        Assert.Equal(TextInputContentType.Search, TextInputOptions.GetContentType(native));

        searchbar.Enterkeyhint = SearchbarEnterkeyhint.Search;
        Assert.Equal(TextInputReturnKeyType.Search, TextInputOptions.GetReturnKeyType(native));
        searchbar.Enterkeyhint = SearchbarEnterkeyhint.Enter;
        Assert.Equal(TextInputReturnKeyType.Return, TextInputOptions.GetReturnKeyType(native));

        Assert.False(TextInputOptions.GetAutoCapitalization(native));
        searchbar.Autocapitalize = "words";
        Assert.True(TextInputOptions.GetAutoCapitalization(native));
        searchbar.Autocapitalize = "characters";
        Assert.True(TextInputOptions.GetUppercase(native));
        Assert.False(TextInputOptions.GetShowSuggestions(native));
        searchbar.Autocorrect = SearchbarAutocorrect.On;
        Assert.True(TextInputOptions.GetShowSuggestions(native));

        searchbar.Placeholder = "Find";
        Assert.Equal("Find", native.PlaceholderText);

        // maxlength: measured, abcdef types abc.
        searchbar.Maxlength = 3;
        searchbar.SetFocus();
        Settle();
        foreach (var (key, text) in new[] { (PhysicalKey.A, "a"), (PhysicalKey.B, "b"), (PhysicalKey.C, "c"), (PhysicalKey.D, "d") })
        {
            Type(scene, key, text);
        }

        Assert.Equal("abc", searchbar.Value);

        // 0 allows nothing.
        searchbar.Value = "";
        searchbar.Maxlength = 0;
        Type(scene, PhysicalKey.E, "e");
        Assert.Equal("", searchbar.Value);
    }

    [AvaloniaFact]
    public void B18_Automation_Names_The_Parts_And_Makes_The_Host_A_Search_Landmark()
    {
        var (searchbar, _, scene) = Stub(new AvonicSearchbar { Value = "v", ShowCancelButton = SearchbarShowCancelButton.Always });
        using var _ = scene;

        Assert.Equal(AutomationLandmarkType.Search, ControlAutomationPeer.CreatePeerForElement(searchbar).GetLandmarkType());
        Assert.Equal("search text", AutomationProperties.GetName(Native(searchbar)));
        Assert.Equal("reset", AutomationProperties.GetName(ClearButton(searchbar)));
        Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(ClearButton(searchbar)).GetAutomationControlType());
        Assert.Equal("Cancel", AutomationProperties.GetName(CancelButton(searchbar)));
        searchbar.CancelButtonText = "Close";
        Assert.Equal("Close", AutomationProperties.GetName(CancelButton(searchbar)));
        Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(CancelButton(searchbar)).GetAutomationControlType());
        foreach (var icon in searchbar.GetVisualDescendants().OfType<AvonicIcon>())
        {
            Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(icon));
        }
    }

    // ── 19 to 21 ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_Animated_Gives_An_Apps_Style_Its_Host_Classes()
    {
        using var clock = new TestClock();
        var searchbar = new AvonicSearchbar { Animated = true };
        var page = new StackPanel { Children = { searchbar, new TextBox() } };
        page.Styles.Add(AppStyle(":animated", Layoutable.MinWidthProperty, 11d));
        page.Styles.Add(AppStyle(":no-animate", Layoutable.MinHeightProperty, 12d));
        page.Styles.Add(AppStyle(":left-aligned", Visual.OpacityProperty, 0.5));
        clock.At(0);
        using var scene = new Scene(page);
        Settle();

        // Loaded: animated, not yet animating, and centred, as it has no value or focus.
        Assert.Equal(11, searchbar.MinWidth);
        Assert.Equal(12, searchbar.MinHeight);
        Assert.Equal(1, searchbar.Opacity);

        // 300 ms on, it animates.
        clock.At(299);
        Assert.Equal(12, searchbar.MinHeight);
        clock.At(300);
        Assert.Equal(0, searchbar.MinHeight);

        // Focus aligns it left; blur with no value centres it again.
        searchbar.SetFocus();
        Settle();
        Assert.Equal(0.5, searchbar.Opacity);
        ((TextBox)page.Children[1]).Focus();
        Settle();
        Assert.Equal(1, searchbar.Opacity);

        // The config turns animated off; not animated is always left-aligned, from the next position.
        AvonicConfig.Animated = false;
        try
        {
            searchbar.Animated = false;
            Assert.Equal(0, searchbar.MinWidth);
            searchbar.ShowCancelButton = SearchbarShowCancelButton.Always;
            clock.At(400);
            Assert.Equal(0.5, searchbar.Opacity);
            searchbar.Animated = true;
            Assert.Equal(0, searchbar.MinWidth);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    [AvaloniaFact]
    public void B20_Leaving_The_Tree_Drops_A_Pending_Clear()
    {
        using var clock = new TestClock();
        var searchbar = new AvonicSearchbar { Value = "x" };
        var host = new StackPanel { Children = { searchbar } };
        using var scene = new Scene(host);
        Settle();
        var log = Log.Of(searchbar);

        clock.At(0);
        Click(scene, ClearButton(searchbar));
        host.Children.Remove(searchbar);
        Settle();
        clock.At(100);
        Assert.Equal("x", searchbar.Value);
        Assert.Equal(["clear"], log.Take());
    }

    [AvaloniaFact]
    public void B20_Removing_A_Focused_Searchbar_Commits_And_Blurs_And_It_Comes_Back_Unfocused()
    {
        var searchbar = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Focus };
        var host = new StackPanel { Children = { searchbar } };
        using var scene = new Scene(host);
        Settle();
        var log = Log.Of(searchbar);
        searchbar.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        log.Take();

        // Measured: Chromium raises change and blur on the removal, and the re-render drops searchbar-has-focus.
        host.Children.Remove(searchbar);
        Settle();
        Assert.Equal(["change:a", "blur"], log.Take());

        host.Children.Add(searchbar);
        Settle();
        Assert.Empty(log.Take());
        Assert.False(CancelButton(searchbar).IsVisible);
        searchbar.SetFocus();
        Settle();
        Assert.Equal(["focus"], log.Take());
    }

    [AvaloniaFact]
    public void B21_The_Icons_Default_To_Mds_And_Follow_The_Properties()
    {
        var (searchbar, _, scene) = Stub(new AvonicSearchbar { Value = "v", ShowCancelButton = SearchbarShowCancelButton.Always });
        using var _ = scene;
        var cancelIcon = Part<AvonicIcon>(searchbar, "PART_CancelIcon");

        Assert.Equal("search-sharp", SearchIcon(searchbar).Icon);
        Assert.Equal("close-sharp", Part<AvonicIcon>(searchbar, "PART_ClearIcon").Icon);
        Assert.Equal("arrow-back-sharp", cancelIcon.Icon);
        Assert.False(cancelIcon.FlipRtl);

        searchbar.SearchIcon = "search-outline";
        searchbar.ClearIcon = "close-circle";
        searchbar.CancelButtonIcon = "arrow-back";
        Assert.Equal("search-outline", SearchIcon(searchbar).Icon);
        Assert.Equal("close-circle", Part<AvonicIcon>(searchbar, "PART_ClearIcon").Icon);
        Assert.Equal("arrow-back", cancelIcon.Icon);

        // A name given is the icon's own: an arrow flips in RTL.
        Assert.Null(cancelIcon.FlipRtl);
    }

    // cancelButtonIcon = config.get('backButtonIcon', arrowBackSharp), in the constructor: a searchbar made before the config
    // changes keeps md's; one made after takes it; the app's own value, set or styled, beats it.
    [AvaloniaFact]
    public void B21_The_Cancel_Icon_Defaults_To_The_Back_Button_Icon_Config_When_The_Searchbar_Is_Made()
    {
        var previous = AvonicConfig.BackButtonIcon;
        try
        {
            var before = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always };
            AvonicConfig.BackButtonIcon = "chevron-back";
            var after = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always };
            var own = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always, CancelButtonIcon = "close" };
            var styled = new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always };

            // A plain style, with no activator, which is at style priority as the config's value is.
            var styledPanel = new StackPanel { Children = { styled } };
            styledPanel.Styles.Add(new Style(x => x.OfType<AvonicSearchbar>())
            {
                Setters = { new Setter(AvonicSearchbar.CancelButtonIconProperty, "close-circle") },
            });
            var panel = new StackPanel { Children = { before, after, own, styledPanel } };
            using var scene = new Scene(panel);
            Settle();

            Assert.Equal("arrow-back-sharp", Part<AvonicIcon>(before, "PART_CancelIcon").Icon);
            Assert.False(Part<AvonicIcon>(before, "PART_CancelIcon").FlipRtl);
            Assert.Equal("chevron-back", after.CancelButtonIcon);
            Assert.Equal("chevron-back", Part<AvonicIcon>(after, "PART_CancelIcon").Icon);

            // A name from the config is the icon's own, as Ionic gives the name to ion-icon: a chevron flips in RTL.
            Assert.Null(Part<AvonicIcon>(after, "PART_CancelIcon").FlipRtl);
            Assert.Equal("close", Part<AvonicIcon>(own, "PART_CancelIcon").Icon);
            Assert.Equal("close-circle", Part<AvonicIcon>(styled, "PART_CancelIcon").Icon);
        }
        finally
        {
            AvonicConfig.BackButtonIcon = previous;
        }
    }
}
