using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages.Showcase;

/// <summary>
/// One message of the showcase's inbox, pushed on its <see cref="AvonicNav"/> by a factory with what the message's row shows.
/// </summary>
public partial class MessageScreen : UserControl
{
    private string _sender = "";
    private string _subject = "";
    private string _body = "";
    private string _time = "";
    private string? _picture;
    private string _initials = "";
    private IBrush? _avatarBackground;
    private IBrush? _avatarForeground;

    public MessageScreen()
    {
        InitializeComponent();
        Show();
    }

    public string Sender { get => _sender; set { _sender = value; Show(); } }

    public string Subject { get => _subject; set { _subject = value; Show(); } }

    public string Body { get => _body; set { _body = value; Show(); } }

    public string Time { get => _time; set { _time = value; Show(); } }

    /// <summary>The sender's picture, or null for <see cref="Initials"/> on <see cref="AvatarBackground"/>.</summary>
    public string? Picture { get => _picture; set { _picture = value; Show(); } }

    public string Initials { get => _initials; set { _initials = value; Show(); } }

    public IBrush? AvatarBackground { get => _avatarBackground; set { _avatarBackground = value; Show(); } }

    public IBrush? AvatarForeground { get => _avatarForeground; set { _avatarForeground = value; Show(); } }

    private void Show()
    {
        SubjectTitle.Content = Subject;
        SenderText.Text = Sender;
        TimeNote.Content = Time;
        var firstName = Sender.Split(' ')[0];
        BodyText.Text = Body.Length == 0 ? "" : $"{Body}\n\n{firstName}";
        if (Picture is { } picture)
        {
            Avatar.ClearValue(Avalonia.Controls.Primitives.TemplatedControl.BackgroundProperty);
            Avatar.Content = new AvonicImg { Src = picture, Alt = Sender };
        }
        else
        {
            Avatar.Background = AvatarBackground;
            Avatar.Content = new TextBlock
            {
                Text = Initials,
                Foreground = AvatarForeground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
    }
}
