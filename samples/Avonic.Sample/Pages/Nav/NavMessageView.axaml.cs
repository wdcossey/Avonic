namespace Avonic.Sample.Pages.Nav;

public partial class NavMessageView : NavSampleView
{
    private string _subject = "Message";

    public NavMessageView()
    {
        InitializeComponent();
        Show();
    }

    /// <summary>A plain CLR property, which the link's <c>ComponentProps</c> set before the page is attached.</summary>
    public string Subject
    {
        get => _subject;
        set
        {
            _subject = value;
            Show();
        }
    }

    public override string Title => Subject;

    private void Show()
    {
        TitleText.Content = Subject;
        Body.Text = $"Pushed as a page Type, with \"{Subject}\" as its subject prop.";
    }
}
