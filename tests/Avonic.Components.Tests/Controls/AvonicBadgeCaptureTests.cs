using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicAvatarTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicBadgeCaptureTests
{
    // The same content as the --html at the top of docs/components/ion-badge/COMPARISON.md.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        static AvonicLabel Label(string text) => new() { Content = text };

        var thumbnailRound = new AvonicThumbnail { Content = Picture() };
        thumbnailRound.Resources["Avonic.Thumbnail.Size"] = 64.0;
        thumbnailRound.Resources["Avonic.Thumbnail.BorderRadius"] = "50%";
        var avatarSquare = new AvonicAvatar { Content = Picture() };
        avatarSquare.Resources["Avonic.Avatar.BorderRadius"] = new CornerRadius(8);

        var page = new StackPanel
        {
            Children =
            {
                // Inline badges, with the width of a space between them.
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(8),
                    Spacing = 4,
                    Children =
                    {
                        new AvonicBadge { Content = "22" },
                        new AvonicBadge { Content = "99+", Color = "danger" },
                        new AvonicBadge { Content = "New", Color = "success" },
                        new AvonicBadge { Content = "Bad", Color = "nonexistent" },
                        new AvonicBadge(),
                        new TextBlock { Text = "after an empty badge", VerticalAlignment = VerticalAlignment.Center },
                    },
                },
                new AvonicItem { Content = Label("Badge in the end slot"), EndContent = new AvonicBadge { Content = "22" } },
                new AvonicItem { StartContent = new AvonicBadge { Content = "3", Color = "danger" }, Content = Label("Badge in the start slot") },
                new AvonicItem { Color = "primary", Content = Label("A primary item"), EndContent = new AvonicBadge { Content = "Stays" } },
                new AvonicItem { Color = "primary", Content = Label("A primary item"), EndContent = new AvonicBadge { Content = "Light", Color = "light" } },
                new AvonicItem { StartContent = new AvonicAvatar { Content = Picture() }, Content = Label("Avatar in the start slot") },
                new AvonicItem { Content = Label("Avatar in the end slot"), EndContent = new AvonicAvatar { Content = Picture() } },
                new AvonicItem { StartContent = new AvonicThumbnail { Content = Picture() }, Content = Label("Thumbnail in the start slot") },
                new AvonicItem { Content = Label("Thumbnail in the end slot"), EndContent = new AvonicThumbnail { Content = Picture() } },
                new AvonicItem
                {
                    StartContent = new AvonicAvatar { Content = Picture() },
                    Content = new AvonicLabel
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { Text = "A tall row", Classes = { "h2" } },
                                new TextBlock { Text = "Line two", Classes = { "p" } },
                                new TextBlock { Text = "Line three", Classes = { "p" } },
                            },
                        },
                    },
                },
                new AvonicItem { Content = new AvonicLabel { Content = "Stacked", Position = LabelPosition.Stacked }, EndContent = new AvonicAvatar { Content = Picture() } },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(8),
                    Spacing = 16,
                    Children =
                    {
                        new AvonicAvatar { Content = Picture(), VerticalAlignment = VerticalAlignment.Top },
                        new AvonicThumbnail { Content = Picture(), VerticalAlignment = VerticalAlignment.Top },
                        avatarSquare,
                        thumbnailRound,
                    },
                },
                new AvonicItem
                {
                    StartContent = new AvonicAvatar { Content = Picture() },
                    Content = Label("RTL"),
                    EndContent = new AvonicBadge { Content = "7" },
                    FlowDirection = FlowDirection.RightToLeft,
                },
            },
        };
        avatarSquare.VerticalAlignment = VerticalAlignment.Top;
        thumbnailRound.VerticalAlignment = VerticalAlignment.Top;

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-badge-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        // Measured in Ionic: rows of 48, 56 and 72.
        Assert.Equal(48, page.Children[1].Bounds.Height);
        Assert.Equal(56, page.Children[5].Bounds.Height);
        Assert.Equal(72, page.Children[7].Bounds.Height);
    });
}
