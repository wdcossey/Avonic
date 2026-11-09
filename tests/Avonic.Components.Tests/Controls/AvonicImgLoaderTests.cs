using System.Net;
using System.Net.Sockets;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Behaviour 16 from docs/components/ion-img/PLAN.md: the default loader, <see cref="ImgLoader"/>, with no network. A file, a
/// <c>data:</c> URI, an app's asset and a server on the loopback.
/// </summary>
public class AvonicImgLoaderTests
{
    private static Bitmap Load(string src) => ImgLoader.LoadAsync(src, CancellationToken.None).GetAwaiter().GetResult();

    private static Exception Fails(string src) =>
        Assert.ThrowsAny<Exception>(() => ImgLoader.LoadAsync(src, CancellationToken.None).GetAwaiter().GetResult());

    private static string TempPicture(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), $"avonic-img-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, TestPicture.Png(width, height));
        return path;
    }

    [AvaloniaFact]
    public void B16_A_File_Path_And_A_File_Uri_Load()
    {
        var path = TempPicture(30, 20);
        try
        {
            Assert.Equal(new PixelSize(30, 20), Load(path).PixelSize);
            Assert.Equal(new PixelSize(30, 20), Load(new Uri(path).AbsoluteUri).PixelSize);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void B16_A_Path_Is_Read_As_It_Is_Written_And_A_File_Uri_Is_Unescaped()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"avonic-img-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            // A file whose name has %41 in it, which a URI would read as A
            var path = Path.Combine(dir, "a%41b.png");
            File.WriteAllBytes(path, TestPicture.Png(14, 10));
            Assert.Equal(new PixelSize(14, 10), Load(path).PixelSize);
            var uri = "file://" + (path.StartsWith('/') ? "" : "/") + path.Replace(Path.DirectorySeparatorChar, '/').Replace("%", "%25");
            Assert.Equal(new PixelSize(14, 10), Load(uri).PixelSize);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public void B16_A_Relative_Path_Is_Relative_To_The_Working_Directory()
    {
        var path = TempPicture(12, 10);
        var previous = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(path)!;
            Assert.Equal(new PixelSize(12, 10), Load(Path.GetFileName(path)).PixelSize);
        }
        finally
        {
            Environment.CurrentDirectory = previous;
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void B16_A_Data_Uri_In_Base64_Loads_And_Any_Other_Does_Not()
    {
        var base64 = Convert.ToBase64String(TestPicture.Png(8, 6));
        Assert.Equal(new PixelSize(8, 6), Load("data:image/png;base64," + base64).PixelSize);
        Assert.Equal(new PixelSize(8, 6), Load("DATA:image/png;BASE64," + base64).PixelSize);
        Fails("data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg'/>");

        // Without ;base64 the text is the bytes, as a browser reads it, so base64 text there isn't a picture
        Fails("data:image/png," + base64);
        Fails("data:image/png;base64");
    }

    [AvaloniaFact]
    public void B16_An_Asset_Is_Read_And_A_Missing_One_Is_Not_Found()
    {
        // The test project's one asset is an SVG, which Bitmap doesn't decode: it's read, and then it fails to decode
        var svg = Fails("avares://Avonic.Components.Tests/Controls/TestIcon.svg");
        Assert.IsNotType<FileNotFoundException>(svg);
        Assert.IsType<FileNotFoundException>(Fails("avares://Avonic.Components.Tests/Controls/Missing.png"), exactMatch: false);
    }

    [AvaloniaFact]
    public void B16_An_Empty_Src_Another_Scheme_And_Bytes_That_Are_Not_A_Picture_Fail()
    {
        Fails("");
        Fails("ftp://127.0.0.1/a.png");
        var path = Path.Combine(Path.GetTempPath(), $"avonic-img-{Guid.NewGuid():N}.png");
        File.WriteAllText(path, "not a png");
        try
        {
            Fails(path);
            Fails(path + ".missing");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void B16_Http_Loads_From_A_Server_And_A_404_Fails()
    {
        using var server = new LoopbackServer(TestPicture.Png(20, 10));
        Assert.Equal(new PixelSize(20, 10), Load(server.Url("/picture.png")).PixelSize);
        Fails(server.Url("/missing.png"));
    }

    [AvaloniaFact]
    public void B16_The_Control_Loads_Through_It_And_Raises_DidLoad_Or_IonError()
    {
        var path = TempPicture(300, 200);
        try
        {
            var ok = new AvonicImg { Src = path };
            var bad = new AvonicImg { Src = path + ".missing" };
            var events = new List<string>();
            ok.IonImgDidLoad += (_, _) => events.Add("ok didLoad");
            bad.IonError += (_, _) => events.Add("bad error");
            using var scene = new Scene(new StackPanel { Children = { ok, bad } });

            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (events.Count < 2 && DateTime.UtcNow < deadline)
            {
                Dispatcher.UIThread.RunJobs();
                scene.Window.UpdateLayout();
                Thread.Sleep(5);
            }

            Assert.Equal(["bad error", "ok didLoad"], events.Order());
            Assert.Equal(new PixelSize(300, 200), ok.Picture!.PixelSize);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A server on the loopback that answers <c>/picture.png</c> with a PNG, anything else with a 404, and a request with no
    /// <c>User-Agent</c> with a 403.</summary>
    private sealed class LoopbackServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();

        public LoopbackServer(byte[] png)
        {
            _listener.Start();
            _ = Task.Run(async () =>
            {
                while (!_stop.IsCancellationRequested)
                {
                    using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                    await using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                    var requestLine = await reader.ReadLineAsync() ?? "";
                    var named = false;
                    while (await reader.ReadLineAsync() is { Length: > 0 } header)
                    {
                        named |= header.StartsWith("User-Agent:", StringComparison.OrdinalIgnoreCase);
                    }

                    // As some servers do, a request that doesn't say what it is gets a 403
                    var found = named && requestLine.Split(' ') is [_, "/picture.png", ..];
                    // A 404 with a picture in it, so that only its status can fail it
                    var body = png;
                    var head = $"HTTP/1.1 {(found ? "200 OK" : named ? "404 Not Found" : "403 Forbidden")}\r\nContent-Type: image/png\r\n" +
                               $"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(head));
                    await stream.WriteAsync(body);
                }
            });
        }

        public string Url(string path) => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}{path}";

        public void Dispose()
        {
            _stop.Cancel();
            _listener.Stop();
        }
    }
}
