using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace UiaRepro;

/// <summary>
/// The app's logs: <c>app-events.log</c> (what the app did, anomalies and exceptions, written at once), and a ring of the last
/// provider calls, dumped to <c>app-dump-N.log</c> on an anomaly or when the client asks (a <c>dump-N.req</c> file in the folder).
/// Times are UTC wall clock to the microsecond, as the client's, so the two logs line up.
/// </summary>
internal static class Log
{
    private const int RingSize = 30000;
    private const int MaxDumps = 40;

    private static readonly object Gate = new();
    private static readonly string?[] Ring = new string?[RingSize];
    private static readonly BlockingCollection<(string File, string Text)> Writes = new();
    private static long _next;
    private static long _dumpedUpTo;
    private static int _dumps;
    private static string _dir = ".";
    private static Timer? _requests;

    public static int UiThreadId { get; set; }

    public static string Now => DateTime.UtcNow.ToString("HH:mm:ss.ffffff");

    public static void Start(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
        var writer = new Thread(() =>
        {
            foreach (var (file, text) in Writes.GetConsumingEnumerable())
            {
                File.AppendAllText(Path.Combine(_dir, file), text);
            }
        }) { IsBackground = true, Name = "log writer" };
        writer.Start();

        // The client asks for a dump when one of its calls fails: dump-N.req in the folder.
        _requests = new Timer(_ =>
        {
            foreach (var req in Directory.GetFiles(_dir, "dump-*.req"))
            {
                var reason = File.ReadAllText(req);
                File.Delete(req);
                Dump($"client: {Path.GetFileNameWithoutExtension(req)} {reason}");
            }
        }, null, 100, 100);
    }

    /// <summary>A provider call, to the ring only.</summary>
    public static void Call(string text)
    {
        var line = $"{Now} t{Environment.CurrentManagedThreadId} {text}";
        lock (Gate)
        {
            Ring[_next++ % RingSize] = line;
        }
    }

    /// <summary>Something the app did, or an anomaly: to the ring and to app-events.log.</summary>
    public static void Event(string text)
    {
        var line = $"{Now} t{Environment.CurrentManagedThreadId} {text}";
        lock (Gate)
        {
            Ring[_next++ % RingSize] = line;
        }

        Writes.Add(("app-events.log", line + Environment.NewLine));
    }

    /// <summary>The calls since the last dump (at most the ring's size), to their own file.</summary>
    public static void Dump(string reason)
    {
        string[] lines;
        int n;
        lock (Gate)
        {
            if (_dumps >= MaxDumps)
            {
                return;
            }

            n = ++_dumps;
            var from = Math.Max(_dumpedUpTo, _next - RingSize);
            lines = new string[_next - from];
            for (var i = from; i < _next; i++)
            {
                lines[i - from] = Ring[i % RingSize] ?? "";
            }

            _dumpedUpTo = _next;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# dump {n} at {Now}: {reason}; {lines.Length} lines since the last dump");
        foreach (var l in lines)
        {
            sb.AppendLine(l);
        }

        Writes.Add(($"app-dump-{n:D2}.log", sb.ToString()));
        Event($"DUMP {n}: {reason}");
    }

    public static void Flush()
    {
        _requests?.Dispose();
        Writes.CompleteAdding();
        var sw = Stopwatch.StartNew();
        while (Writes.Count > 0 && sw.ElapsedMilliseconds < 3000)
        {
            Thread.Sleep(20);
        }

        Thread.Sleep(100);
    }
}
