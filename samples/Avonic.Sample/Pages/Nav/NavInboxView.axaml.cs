namespace Avonic.Sample.Pages.Nav;

public partial class NavInboxView : NavSampleView
{
    public NavInboxView()
    {
        InitializeComponent();
        LunchLink.Component = typeof(NavMessageView);
        LunchLink.ComponentProps = new Dictionary<string, object?> { ["subject"] = "Lunch on Friday" };
        ReportLink.Component = typeof(NavMessageView);
        ReportLink.ComponentProps = new Dictionary<string, object?> { ["subject"] = "Quarterly report" };
    }

    public override string Title => "Inbox";
}
