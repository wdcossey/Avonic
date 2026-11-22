using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages.Showcase;

/// <summary>
/// One message of the showcase's inbox, pushed on its <see cref="AvonicNav"/> by a factory with what the message's row shows. Its
/// properties are plain storage; the page shows them when it's attached, as the push sets them all before that.
/// </summary>
public partial class MessageScreen : UserControl
{
    public MessageScreen() => InitializeComponent();

    public string Sender { get; set; } = "";

    public string Subject { get; set; } = "";

    public string Body { get; set; } = "";

    public string Time { get; set; } = "";

    /// <summary>The sender's picture, or null for <see cref="Initials"/> on <see cref="AvatarColor"/>.</summary>
    public string? Picture { get; set; }

    public string Initials { get; set; } = "";

    /// <summary>
    /// The palette's colour behind the initials, by name (<c>Tertiary</c>), read through the theme's resources, so that it follows
    /// a dark palette as the inbox's row does.
    /// </summary>
    public string? AvatarColor { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Show();
    }

    private void Show()
    {
        SubjectTitle.Content = Subject;
        SenderText.Text = Sender;
        TimeNote.Content = Time;
        BodyText.Text = Body;
        if (Picture is { } picture)
        {
            Avatar.ClearValue(TemplatedControl.BackgroundProperty);
            Avatar.Content = new AvonicImg { Src = picture, Alt = Sender };
        }
        else
        {
            var initials = new TextBlock
            {
                Text = Initials,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (AvatarColor is { } color)
            {
                Avatar[!TemplatedControl.BackgroundProperty] = Avatar.GetResourceObservable($"Avonic.Color.{color}Brush").ToBinding();
                initials[!TextBlock.ForegroundProperty] = initials.GetResourceObservable($"Avonic.Color.{color}ContrastBrush").ToBinding();
            }

            Avatar.Content = initials;
        }
    }
}
