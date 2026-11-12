using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Tests.Behaviors;

public class TextContentTests
{
    [AvaloniaFact]
    public void HasText_Is_False_For_Nothing_And_For_Empty_Text()
    {
        Assert.False(TextContent.HasText(null, null));
        Assert.False(TextContent.HasText(string.Empty, null));
        Assert.False(TextContent.HasText(new TextBlock(), null));
        Assert.False(TextContent.HasText(new StackPanel { Children = { new Border(), new TextBlock { Text = string.Empty } } }, null));
    }

    [AvaloniaFact]
    public void HasText_Finds_Text_Shown_Or_Not_At_Any_Depth()
    {
        Assert.True(TextContent.HasText("a", null));
        Assert.True(TextContent.HasText(new TextBlock { Inlines = [new Run("a")] }, null));

        // textContent counts hidden text, where AccessibleText doesn't.
        var hidden = new TextBlock { Text = "a", IsVisible = false };
        Assert.True(TextContent.HasText(new StackPanel { Children = { new Border(), new Border { Child = hidden } } }, null));
        Assert.True(TextContent.HasText(new ContentControl { Content = new ContentControl { Content = "a" } }, null));
    }

    [AvaloniaFact]
    public void HasText_Reads_Data_As_What_Its_Template_Made()
    {
        // Data made into an element with no text has none, though its ToString() has some.
        var presenter = new ContentPresenter
        {
            Content = 42,
            ContentTemplate = new FuncDataTemplate<int>((_, _) => new Border()),
        };
        presenter.UpdateChild();
        Assert.False(TextContent.HasText(42, presenter));

        // With nothing made yet, its text.
        Assert.True(TextContent.HasText(42, null));
    }
}
