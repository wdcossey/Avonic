using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Themes;

namespace Avonic.Components.Tests.Behaviors;

/// <summary>A control that adds <c>Color</c> the documented way, for the template tests.</summary>
public class ColoredControl : TemplatedControl
{
    public static readonly StyledProperty<string?> ColorProperty = ColorPalette.ColorProperty.AddOwner<ColoredControl>();

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}

/// <summary>
/// Each case was measured in real Ionic 8.8.1 (md); see "Measured behaviour" in
/// docs/infra/color-system/PLAN.md.
/// </summary>
public class ColorPaletteTests
{
    private static readonly Color Danger = Color.Parse("#c5000f");
    private static readonly Color DangerContrast = Color.Parse("#ffffff");
    private static readonly Color DangerShade = Color.Parse("#ad000d");
    private static readonly Color DangerTint = Color.Parse("#cb1a27");
    private static readonly Color Favorite = Color.Parse("#69bb7b");

    private static void WithTheme(Action test)
    {
        var app = Application.Current!;
        var theme = new AvonicTheme();
        app.Styles.Add(theme);
        try
        {
            test();
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }

    private static Color? BrushColor(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    private static bool HasIonColor(StyledElement element) => element.Classes.Contains(ColorPalette.IonColorPseudoClass);

    private static void AssertUnset(StyledElement element)
    {
        Assert.Null(ColorPalette.GetBase(element));
        Assert.Null(ColorPalette.GetContrast(element));
        Assert.Null(ColorPalette.GetShade(element));
        Assert.Null(ColorPalette.GetTint(element));
        Assert.Null(ColorPalette.GetBaseColor(element));
        Assert.Null(ColorPalette.GetContrastColor(element));
    }

    private static void AssertDanger(StyledElement element)
    {
        Assert.Equal(Danger, BrushColor(ColorPalette.GetBase(element)));
        Assert.Equal(DangerContrast, BrushColor(ColorPalette.GetContrast(element)));
        Assert.Equal(DangerShade, BrushColor(ColorPalette.GetShade(element)));
        Assert.Equal(DangerTint, BrushColor(ColorPalette.GetTint(element)));
        Assert.Equal(Danger, ColorPalette.GetBaseColor(element));
        Assert.Equal(DangerContrast, ColorPalette.GetContrastColor(element));
    }

    private static Border Colored(string? color)
    {
        var element = new Border();
        ColorPalette.SetColor(element, color);
        return element;
    }

    // ── Measured cases ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void No_Color_Sets_Nothing() => WithTheme(() =>
    {
        var element = new Border();
        using var scene = new Scene(element);

        Assert.False(HasIonColor(element));
        AssertUnset(element);
    });

    [AvaloniaFact]
    public void Known_Color_Resolves_Every_Variable() => WithTheme(() =>
    {
        var element = Colored("danger");
        using var scene = new Scene(element);

        Assert.True(HasIonColor(element));
        AssertDanger(element);
    });

    // Ionic: the ion-color class still applies, with nothing behind --ion-color-base.
    [AvaloniaFact]
    public void Unknown_Color_Sets_The_PseudoClass_And_Resolves_To_Nothing() => WithTheme(() =>
    {
        var element = Colored("nonexistent");
        using var scene = new Scene(element);

        Assert.True(HasIonColor(element));
        AssertUnset(element);
    });

    // A CSS class is case-sensitive: .ion-color-Danger doesn't match .ion-color-danger.
    [AvaloniaFact]
    public void Wrong_Case_Is_An_Unknown_Color() => WithTheme(() =>
    {
        var element = Colored("Danger");
        using var scene = new Scene(element);

        Assert.True(HasIonColor(element));
        AssertUnset(element);
    });

    // createColorClasses: typeof color === 'string' && color.length > 0
    [AvaloniaFact]
    public void Empty_Color_Is_No_Color() => WithTheme(() =>
    {
        var element = Colored("");
        using var scene = new Scene(element);

        Assert.False(HasIonColor(element));
        AssertUnset(element);
    });

    // Ionic: defining .ion-color-favorite is how an app adds a colour.
    [AvaloniaFact]
    public void Custom_Color_Comes_From_Resource_Keys() => WithTheme(() =>
    {
        var element = Colored("favorite");
        var scope = new Border { Child = element };
        scope.Resources["Avonic.Color.Favorite"] = Favorite;
        scope.Resources["Avonic.Color.FavoriteContrast"] = Colors.Black;
        scope.Resources["Avonic.Color.FavoriteShade"] = Colors.Gray;
        scope.Resources["Avonic.Color.FavoriteTint"] = Colors.White;
        using var scene = new Scene(scope);

        Assert.Equal(Favorite, BrushColor(ColorPalette.GetBase(element)));
        Assert.Equal(Colors.Black, BrushColor(ColorPalette.GetContrast(element)));
        Assert.Equal(Colors.Gray, BrushColor(ColorPalette.GetShade(element)));
        Assert.Equal(Colors.White, BrushColor(ColorPalette.GetTint(element)));
    });

    [AvaloniaFact]
    public void Multi_Word_Custom_Color_Maps_From_Kebab_Case() => WithTheme(() =>
    {
        var element = Colored("my-brand");
        var scope = new Border { Child = element };
        scope.Resources["Avonic.Color.MyBrand"] = Favorite;
        using var scene = new Scene(scope);

        Assert.Equal(Favorite, BrushColor(ColorPalette.GetBase(element)));
    });

    // Ionic has no .ion-color-danger-shade, though Avonic.Color.DangerShade is a real key.
    [AvaloniaTheory]
    [InlineData("danger-shade")]
    [InlineData("danger-tint")]
    [InlineData("danger-contrast")]
    public void A_Palette_Variant_Is_Not_A_Color_Name(string name) => WithTheme(() =>
    {
        var element = Colored(name);
        using var scene = new Scene(element);

        Assert.True(HasIonColor(element));
        AssertUnset(element);
    });

    [AvaloniaFact]
    public void A_Brush_Key_Is_Not_A_Color_Name() => WithTheme(() =>
    {
        var element = Colored("danger-brush");
        using var scene = new Scene(element);

        AssertUnset(element);
    });

    // Ionic: --ion-color-base is inherited, and an undefined class doesn't reset it.
    [AvaloniaFact]
    public void Unknown_Color_Inside_A_Colored_Ancestor_Shows_The_Ancestors_Values() => WithTheme(() =>
    {
        var element = Colored("nonexistent");
        var ancestor = Colored("danger");
        ancestor.Child = element;
        using var scene = new Scene(ancestor);

        Assert.True(HasIonColor(element));
        AssertDanger(element);
    });

    // Ionic: the variable inherits, but rules that read it need .ion-color on the element.
    [AvaloniaFact]
    public void No_Color_Inside_A_Colored_Ancestor_Inherits_Without_The_PseudoClass() => WithTheme(() =>
    {
        var element = new Border();
        var ancestor = Colored("danger");
        ancestor.Child = element;
        using var scene = new Scene(ancestor);

        Assert.False(HasIonColor(element));
        AssertDanger(element);
    });

    [AvaloniaFact]
    public void Known_Color_Inside_A_Colored_Ancestor_Wins() => WithTheme(() =>
    {
        var element = Colored("danger");
        var ancestor = Colored("primary");
        ancestor.Child = element;
        using var scene = new Scene(ancestor);

        AssertDanger(element);
    });

    // Ionic: setting --ion-color-danger on a wrapper changes only that subtree.
    [AvaloniaFact]
    public void Overriding_A_Color_On_An_Ancestor_Changes_Only_That_Subtree() => WithTheme(() =>
    {
        var inside = Colored("danger");
        var outside = Colored("danger");
        var scope = new Border { Child = inside };
        scope.Resources["Avonic.Color.Danger"] = Colors.Lime;
        using var scene = new Scene(new StackPanel { Children = { scope, outside } });

        Assert.Equal(Colors.Lime, BrushColor(ColorPalette.GetBase(inside)));
        Assert.Equal(Colors.Lime, ColorPalette.GetBaseColor(inside));
        Assert.Equal(DangerContrast, BrushColor(ColorPalette.GetContrast(inside)));
        AssertDanger(outside);
    });

    // ── Changes over time ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Changing_The_Color_Replaces_The_Values() => WithTheme(() =>
    {
        var element = Colored("primary");
        using var scene = new Scene(element);

        ColorPalette.SetColor(element, "danger");

        AssertDanger(element);
    });

    [AvaloniaFact]
    public void Clearing_The_Color_Removes_The_Values_And_PseudoClass() => WithTheme(() =>
    {
        var element = Colored("danger");
        using var scene = new Scene(element);

        ColorPalette.SetColor(element, null);

        Assert.False(HasIonColor(element));
        AssertUnset(element);
    });

    [AvaloniaFact]
    public void Clearing_The_Color_Shows_An_Ancestors_Values_Again() => WithTheme(() =>
    {
        var element = Colored("primary");
        var ancestor = Colored("danger");
        ancestor.Child = element;
        using var scene = new Scene(ancestor);

        ColorPalette.SetColor(element, null);

        AssertDanger(element);
    });

    [AvaloniaFact]
    public void Changing_To_An_Unknown_Color_Removes_The_Values() => WithTheme(() =>
    {
        var element = Colored("danger");
        using var scene = new Scene(element);

        ColorPalette.SetColor(element, "nonexistent");

        Assert.True(HasIonColor(element));
        AssertUnset(element);
    });

    [AvaloniaFact]
    public void A_Cleared_Color_No_Longer_Tracks_Its_Old_Key() => WithTheme(() =>
    {
        var element = Colored("danger");
        var scope = new Border { Child = element };
        using var scene = new Scene(scope);
        ColorPalette.SetColor(element, null);

        scope.Resources["Avonic.Color.Danger"] = Colors.Lime;
        Dispatcher.UIThread.RunJobs();

        AssertUnset(element);
    });

    [AvaloniaFact]
    public void A_Color_Added_After_Show_Resolves() => WithTheme(() =>
    {
        var element = Colored("favorite");
        var scope = new Border { Child = element };
        using var scene = new Scene(scope);
        AssertUnset(element);

        scope.Resources["Avonic.Color.Favorite"] = Favorite;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Favorite, BrushColor(ColorPalette.GetBase(element)));
    });

    [AvaloniaFact]
    public void Moving_To_Another_Scope_Follows_That_Scope() => WithTheme(() =>
    {
        var element = Colored("danger");
        var first = new Border { Child = element };
        var second = new Border();
        first.Resources["Avonic.Color.Danger"] = Colors.Lime;
        second.Resources["Avonic.Color.Danger"] = Colors.Blue;
        using var scene = new Scene(new StackPanel { Children = { first, second } });
        Assert.Equal(Colors.Lime, BrushColor(ColorPalette.GetBase(element)));

        first.Child = null;
        second.Child = element;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Colors.Blue, BrushColor(ColorPalette.GetBase(element)));
    });

    [AvaloniaFact]
    public void Merging_The_Dark_Palette_Updates_A_Shown_Element() => WithTheme(() =>
    {
        var element = Colored("danger");
        using var scene = new Scene(element);
        AssertDanger(element);

        var app = Application.Current!;
        var dark = new ResourceInclude(new Uri("avares://Avonic.Themes/"))
        {
            Source = new Uri("avares://Avonic.Themes/Tokens/Palettes/Dark.axaml"),
        };
        app.Resources.MergedDictionaries.Add(dark);
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Color.Parse("#f24c58"), BrushColor(ColorPalette.GetBase(element)));
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(dark);
        }

        Dispatcher.UIThread.RunJobs();
        AssertDanger(element);
    });

    // ── The documented template pattern ──────────────────────────────────────────────────────

    private static void WithUsageTheme(Action test) => WithTheme(() =>
    {
        var app = Application.Current!;
        var usage = new ResourceInclude(new Uri("avares://Avonic.Components.Tests/"))
        {
            Source = new Uri("avares://Avonic.Components.Tests/Behaviors/ColorPaletteUsage.axaml"),
        };
        app.Resources.MergedDictionaries.Add(usage);
        try
        {
            test();
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(usage);
        }
    });

    private static T Part<T>(TemplatedControl control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(part => part.Name == name);

    [AvaloniaFact]
    public void AddOwner_Sets_The_PseudoClass_And_Values() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "danger" };
        using var scene = new Scene(control);

        Assert.True(HasIonColor(control));
        AssertDanger(control);
        Assert.Equal("danger", ColorPalette.GetColor(control));
    });

    [AvaloniaFact]
    public void Template_Uses_The_Defaults_Without_A_Color() => WithUsageTheme(() =>
    {
        var control = new ColoredControl();
        using var scene = new Scene(control);

        Assert.Equal(Color.Parse("#010203"), BrushColor(Part<Border>(control, "PART_Native").Background));
        Assert.Equal(Color.Parse("#040506"), BrushColor(Part<TextBlock>(control, "PART_Text").Foreground));
    });

    [AvaloniaFact]
    public void Template_Paints_Base_And_Contrast_Through_The_Pseudoclass_Style() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "danger" };
        using var scene = new Scene(control);

        Assert.Equal(Danger, BrushColor(Part<Border>(control, "PART_Native").Background));
        Assert.Equal(DangerContrast, BrushColor(Part<TextBlock>(control, "PART_Text").Foreground));
    });

    [AvaloniaFact]
    public void Template_Can_Use_TemplateBinding_On_A_Palette_Property() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "danger" };
        using var scene = new Scene(control);

        Assert.Equal(DangerShade, BrushColor(Part<Border>(control, "PART_TemplateBound").Background));
    });

    [AvaloniaFact]
    public void Template_Part_Can_Read_The_Inherited_Value() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "danger" };
        using var scene = new Scene(control);

        Assert.Equal(DangerTint, BrushColor(Part<Border>(control, "PART_Inherited").Background));
    });

    // rgba(var(--ion-color-base-rgb), 0.08)
    [AvaloniaFact]
    public void Template_Can_Add_Alpha_From_BaseColor() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "danger" };
        using var scene = new Scene(control);

        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(Part<Border>(control, "PART_Alpha").Background);
        Assert.Equal(Danger, brush.Color);
        Assert.Equal(0.08, brush.Opacity);
    });

    // Ionic: a solid button with an unknown colour has a transparent background, not its default.
    [AvaloniaFact]
    public void Template_With_An_Unknown_Color_Has_No_Background() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "nonexistent" };
        using var scene = new Scene(control);

        Assert.Null(Part<Border>(control, "PART_Native").Background);
    });

    // Ionic: a badge with an unknown colour keeps visible text, measured as black. A null Foreground
    // would hide it, so the documented pattern uses NullToUnset.
    [AvaloniaFact]
    public void Template_With_An_Unknown_Color_Keeps_Text_Visible() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "nonexistent" };
        using var scene = new Scene(control);

        Assert.Equal(Colors.Black, BrushColor(Part<TextBlock>(control, "PART_Text").Foreground));
    });

    [AvaloniaFact]
    public void Template_Follows_A_Changed_Color() => WithUsageTheme(() =>
    {
        var control = new ColoredControl { Color = "primary" };
        using var scene = new Scene(control);

        control.Color = "danger";
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Danger, BrushColor(Part<Border>(control, "PART_Native").Background));

        control.Color = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Color.Parse("#010203"), BrushColor(Part<Border>(control, "PART_Native").Background));
    });
}
