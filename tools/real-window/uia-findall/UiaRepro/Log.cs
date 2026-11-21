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
    private const int MaxAnomalyDumps = 20;
    private const int MaxClientDumps = 40;

    private static readonly object Gate = new();
    private static readonly string?[] Ring = new string?[RingSize];
    private static readonly BlockingCollection<(string File, string Text)> Writes = new();
    private static long _next;
    private static long _dumpedUpTo;
    private static int _dumps;
    private static int _anomalyDumps;
    private static int _clientDumps;
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
        // A one-shot timer, armed again after each look, so that two looks never overlap; a request still being written is
        // read at the next look.
        _requests = new Timer(_ =>
        {
            try
            {
                foreach (var req in Directory.GetFiles(_dir, "dump-*.req"))
                {
                    string reason;
                    try
                    {
                        reason = File.ReadAllText(req);
                        File.Delete(req);
                    }
                    catch (IOException)
                    {
                        continue;
                    }

                    Dump($"client: {Path.GetFileNameWithoutExtension(req)} {reason}", fromClient: true);
                }
            }
            catch (Exception ex)
            {
                Event($"dump requests: {ex.GetType().Name} {ex.Message}");
            }
            finally
            {
                _requests?.Change(100, Timeout.Infinite);
            }
        }, null, 100, Timeout.Infinite);
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

        Write("app-events.log", line + Environment.NewLine);
    }

    /// <summary>Queued for the writer; dropped once the log has been flushed at exit, as UIA calls can still come then.</summary>
    private static void Write(string file, string text)
    {
        if (!Writes.IsAddingCompleted)
        {
            try
            {
                Writes.TryAdd((file, text));
            }
            catch (InvalidOperationException)
            {
                // Completed between the check and the add.
            }
        }
    }

    /// <summary>The calls since the last dump (at most the ring's size), to their own file.</summary>
    public static void Dump(string reason, bool fromClient = false)
    {
        string[] lines;
        int n;
        lock (Gate)
        {
            // Separate budgets, so that anomalies can't use up the dumps of the client's failed searches.
            if (fromClient ? _clientDumps >= MaxClientDumps : _anomalyDumps >= MaxAnomalyDumps)
            {
                if (fromClient)
                {
                    Write("app-events.log", $"{Now} DUMP DROPPED (over budget): {reason}{Environment.NewLine}");
                }

                return;
            }

            if (fromClient)
            {
                _clientDumps++;
            }
            else
            {
                _anomalyDumps++;
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

        Write($"app-dump-{n:D2}.log", sb.ToString());
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
