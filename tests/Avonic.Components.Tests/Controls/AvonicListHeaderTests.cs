using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicListHeaderTests
{
    // ── EndContent ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void EndContent_Can_Be_Set()
    {
        var seeAll = new TextBlock { Text = "See All" };
        var header = new AvonicListHeader { Content = "FAVOURITES", EndContent = seeAll };
        var window = new Window { Content = header, Width = 390, Height = 100 };
        window.Show();

        Assert.Equal(seeAll, header.EndContent);
    }

    [AvaloniaFact]
    public void EndContent_Defaults_To_Null()
    {
        var header = new AvonicListHeader { Content = "SECTION" };
        var window = new Window { Content = header, Width = 390, Height = 100 };
        window.Show();

        Assert.Null(header.EndContent);
    }

    // ── Lines pseudoclasses ───────────────────────────────────────────────────

    [AvaloniaFact]
    public void Lines_Null_By_Default_Sets_No_Lines_Pseudoclass()
    {
        var header = new AvonicListHeader { Content = "SECTION" };
        var window = new Window { Content = header, Width = 390, Height = 100 };
        window.Show();

        Assert.DoesNotContain(":lines-full",  header.Classes);
        Assert.DoesNotContain(":lines-inset", header.Classes);
        Assert.DoesNotContain(":lines-none",  header.Classes);
    }

    [AvaloniaFact]
    public void Lines_Full_Sets_LinesFull_Pseudoclass()
    {
        var header = new AvonicListHeader { Content = "SECTION", Lines = ItemLines.Full };
        var window = new Window { Content = header, Width = 390, Height = 100 };
        window.Show();

        Assert.Contains(":lines-full", header.Classes);
        Assert.DoesNotContain(":lines-inset", header.Classes);
        Assert.DoesNotContain(":lines-none",  header.Classes);
    }

    [AvaloniaFact]
    public void Lines_Inset_Sets_LinesInset_Pseudoclass()
    {
        var header = new AvonicListHeader { Content = "SECTION", Lines = ItemLines.Inset };
        var window = new Window { Content = header, Width = 390, Height = 100 };
        window.Show();

        Assert.Contains(":lines-inset", header.Classes);
        Assert.DoesNotContain(":lines-full", header.Classes);
        Assert.DoesNotContain(":lines-none", header.Classes);
    }

    [AvaloniaFact]
    public void Lines_None_Sets_LinesNone_Pseudoclass()
    {
        var header = new AvonicListHeader { Content = "SECTION", Lines = ItemLines.None };
        var window = new Window { Content = header, Width = 390, Height = 100 };
        window.Show();

        Assert.Contains(":lines-none", header.Classes);
        Assert.DoesNotContain(":lines-full",  header.Classes);
        Assert.DoesNotContain(":lines-inset", header.Classes);
    }

    [AvaloniaFact]
    public void Lines_Changing_Value_Updates_Pseudoclasses()
    {
        var header = new AvonicListHeader { Content = "SECTION", Lines = ItemLines.Full };
        var window = new Window { Content = header, Width = 390, Height = 100 };
        window.Show();

        header.Lines = ItemLines.Inset;

        Assert.Contains(":lines-inset", header.Classes);
        Assert.DoesNotContain(":lines-full", header.Classes);
    }
}
