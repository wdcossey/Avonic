using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Tests.Controls;

public class AvonicSpinnerTests
{
    // ── Default variant ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Variant_Circular_By_Default_Sets_Circular_Pseudoclass()
    {
        var spinner = new AvonicSpinner();
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":spinner-circular", spinner.Classes);
        Assert.DoesNotContain(":spinner-dots",    spinner.Classes);
        Assert.DoesNotContain(":spinner-lines",   spinner.Classes);
        Assert.DoesNotContain(":spinner-bubbles", spinner.Classes);
        Assert.DoesNotContain(":spinner-circles", spinner.Classes);
        Assert.DoesNotContain(":spinner-crescent",spinner.Classes);
    }

    // ── Variant pseudoclasses ─────────────────────────────────────────────────

    [AvaloniaFact]
    public void Variant_Crescent_Sets_Crescent_Pseudoclass()
    {
        var spinner = new AvonicSpinner { Variant = SpinnerName.Crescent };
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":spinner-crescent", spinner.Classes);
        Assert.DoesNotContain(":spinner-circular", spinner.Classes);
    }

    [AvaloniaFact]
    public void Variant_Dots_Sets_Dots_Pseudoclass()
    {
        var spinner = new AvonicSpinner { Variant = SpinnerName.Dots };
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":spinner-dots", spinner.Classes);
        Assert.DoesNotContain(":spinner-circular", spinner.Classes);
    }

    [AvaloniaFact]
    public void Variant_Lines_Sets_Lines_Pseudoclass()
    {
        var spinner = new AvonicSpinner { Variant = SpinnerName.Lines };
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":spinner-lines", spinner.Classes);
        Assert.DoesNotContain(":spinner-circular", spinner.Classes);
    }

    [AvaloniaFact]
    public void Variant_Bubbles_Sets_Bubbles_Pseudoclass()
    {
        var spinner = new AvonicSpinner { Variant = SpinnerName.Bubbles };
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":spinner-bubbles", spinner.Classes);
    }

    [AvaloniaFact]
    public void Variant_Circles_Sets_Circles_Pseudoclass()
    {
        var spinner = new AvonicSpinner { Variant = SpinnerName.Circles };
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":spinner-circles", spinner.Classes);
    }

    // ── Variant switching ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Variant_Changing_Updates_Pseudoclasses()
    {
        var spinner = new AvonicSpinner();
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        window.Show();

        spinner.Variant = SpinnerName.Dots;

        Assert.Contains(":spinner-dots", spinner.Classes);
        Assert.DoesNotContain(":spinner-circular", spinner.Classes);
    }

    // ── Attaches cleanly ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Attaches_To_Visual_Tree_Without_Error()
    {
        var spinner = new AvonicSpinner();
        var window  = new Window { Content = spinner, Width = 390, Height = 200 };
        var ex = Record.Exception(() => window.Show());

        Assert.Null(ex);
    }
}
