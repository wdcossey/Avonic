using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-img", Order = 17)]
public partial class ImgPage : UserControl
{
    private static readonly Color[] s_palette =
    [
        Color.FromRgb(220, 40, 40), Color.FromRgb(40, 160, 40), Color.FromRgb(40, 40, 220), Color.FromRgb(230, 200, 30),
        Color.FromRgb(0, 84, 233), Color.FromRgb(197, 0, 15), Color.FromRgb(45, 211, 111), Color.FromRgb(255, 196, 9),
    ];

    /// <summary>The sample's own pictures, in Assets/Img: see its README.</summary>
    private static readonly string[] s_embedded =
    [
        "avares://Avonic.Sample/Assets/Img/hills.png",
        "avares://Avonic.Sample/Assets/Img/sea.png",
        "avares://Avonic.Sample/Assets/Img/mountains.png",
    ];

    private int _next;
    private int _loaded;

    public ImgPage()
    {
        InitializeComponent();

        for (var i = 0; i < 20; i++)
        {
            More.Children.Add(new AvonicImg { Src = s_embedded[i % s_embedded.Length], Alt = $"Picture {i + 1}", Tag = $"picture {i + 1}" });
        }

        // The data: URI and the file are drawn and encoded off the UI thread, so that the page opens at once
        _ = DrawPicturesAsync();

        Page.AddHandler(AvonicImg.IonImgWillLoadEvent, (_, e) => Say(e, "starts to load"));
        Page.AddHandler(AvonicImg.IonImgDidLoadEvent, (_, e) =>
        {
            _loaded++;
            Say(e, "has loaded");
        });
        Page.AddHandler(AvonicImg.IonErrorEvent, (_, e) => Say(e, "failed"));
    }

    private async Task DrawPicturesAsync()
    {
        var file = Path.Combine(Path.GetTempPath(), "avonic-sample-img.png");
        var drawn = await Task.Run(() =>
        {
            File.WriteAllBytes(file, Png(120, 120, 3));
            return DataUri(300, 200, 0);
        });

        Drawn.Src = drawn;
        FromFile.Src = file;
    }

    private void Say(RoutedEventArgs e, string what)
    {
        var img = (AvonicImg)e.Source!;
        Log.Text = $"{img.Tag as string ?? img.Alt} {what}. {_loaded} loaded so far.";
    }

    private void OnNext(object? sender, RoutedEventArgs e) => First.Src = s_embedded[++_next % s_embedded.Length];

    /// <summary>A picture of four quarters, as the tests draw it, in colours that start at <paramref name="shift"/>.</summary>
    private static byte[] Png(int width, int height, int shift)
    {
        using var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var buffer = bitmap.Lock())
        {
            for (var y = 0; y < height; y++)
            {
                var row = new int[width];
                for (var x = 0; x < width; x++)
                {
                    var quarter = (y < height / 2 ? 0 : 2) + (x < width / 2 ? 0 : 1);
                    var color = s_palette[(quarter + shift) % s_palette.Length];
                    row[x] = unchecked((int)(0xFF000000u | (uint)color.R << 16 | (uint)color.G << 8 | color.B));
                }

                Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, width);
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    private static string DataUri(int width, int height, int shift) =>
        "data:image/png;base64," + Convert.ToBase64String(Png(width, height, shift));
}
