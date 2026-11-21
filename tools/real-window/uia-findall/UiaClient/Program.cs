using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using UIA = Interop.UIAutomationClient;

namespace UiaClient;

/// <summary>
/// #254's client. Starts an app, changes its pages by invoking buttons by name (UIA's Invoke pattern: no mouse, no keys), and
/// after each change searches the whole window again and again for <c>--poll-ms</c>, logging every search: when it started and
/// ended, what it found, or the HRESULT it failed with. On a failure it asks the app for a dump of its provider calls
/// (<c>dump-N.req</c> in the log folder), retries at once and 300 ms later, and logs both. With <c>--triggers</c> empty it only
/// searches, for <c>--poll-ms</c>, while the app changes itself (UiaRepro's <c>--auto</c>). It closes the app at the end.
/// </summary>
internal static class Program
{
    private static readonly StringBuilder Summary = new();
    private static readonly object WriteGate = new();
    private static StreamWriter _log = StreamWriter.Null;
    private static string _logDir = ".";
    private static int _dumpRequests;

    private static string Now => DateTime.UtcNow.ToString("HH:mm:ss.ffffff");

    public static int Main(string[] args)
    {
        var o = Options.Parse(args);
        _logDir = o.LogDir;
        Directory.CreateDirectory(_logDir);
        _log = new StreamWriter(Path.Combine(_logDir, $"client-{o.Name}.log"), append: false) { AutoFlush = true };
        Write($"start {o.Name}: api {o.Api}, rounds {o.Rounds}, poll {o.PollMs} ms, triggers [{string.Join(" | ", o.Triggers)}], " +
              $"listen {o.Listen}, exe {o.Exe} {o.AppArgs}");

        var psi = new ProcessStartInfo(o.Exe, o.AppArgs) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(o.Exe)! };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("the app didn't start");
        var stats = new Stats();
        try
        {
            var hwnd = IntPtr.Zero;
            for (var i = 0; i < 80 && hwnd == IntPtr.Zero; i++)
            {
                Thread.Sleep(250);
                p.Refresh();
                hwnd = p.MainWindowHandle;
            }

            if (hwnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("the app's window didn't come");
            }

            Thread.Sleep(1500);
            var probe = new Probe(o.Api, hwnd);
            Write($"window 0x{hwnd:X}");
            if (o.Listen)
            {
                Automation.AddStructureChangedEventHandler(probe.Managed, TreeScope.Subtree, (_, e) =>
                    Write($"event StructureChanged {e.StructureChangeType} [{string.Join(".", e.GetRuntimeId() ?? [])}]"));
                Write("listening to StructureChanged on the window's subtree");
            }

            if (o.Sample)
            {
                // The sample's shell: the index's ion-nav button, then the nav's root.
                Invoke(probe, "ion-nav", stats);
                WaitForText(probe, "Stack: Inbox (1)*", 10000, stats);
                Thread.Sleep(500);
            }

            if (o.Triggers.Count == 0)
            {
                Poll(probe, o, stats, "idle");
            }

            for (var round = 1; round <= o.Rounds && o.Triggers.Count > 0; round++)
            {
                var trigger = o.Triggers[(round - 1) % o.Triggers.Count];
                Write($"round {round}: {trigger}");
                Invoke(probe, trigger, stats);
                Poll(probe, o, stats, $"after {trigger}");
            }
        }
        catch (Exception ex)
        {
            Write($"STOPPED: {ex}");
            stats.Stopped = ex.Message;
        }
        finally
        {
            if (o.Listen)
            {
                Automation.RemoveAllEventHandlers();
            }

            Close(p);
        }

        var line = $"{o.Name}: {stats.Probes} searches, {stats.Failures} failed" +
                   (stats.ByHr.Count > 0 ? $" ({string.Join(", ", stats.ByHr.Select(kv => $"{kv.Key} x{kv.Value}"))})" : "") +
                   $"; retries at once {stats.RetryOk} ok / {stats.RetryFail} failed; trigger lookups failed {stats.TriggerFailures}" +
                   (stats.Stopped is { } s ? $"; STOPPED: {s}" : "");
        Write("summary " + line);
        Console.WriteLine(line);
        lock (WriteGate)
        {
            _log.Dispose();
            _log = StreamWriter.Null;
        }

        return 0;
    }

    // StructureChanged handlers write from UIA's event thread too.
    private static void Write(string text)
    {
        lock (WriteGate)
        {
            _log.WriteLine($"{Now} {text}");
        }
    }

    private static void Poll(Probe probe, Options o, Stats stats, string what)
    {
        var sw = Stopwatch.StartNew();
        var n = 0;
        while (sw.ElapsedMilliseconds < o.PollMs)
        {
            n++;
            var kind = n % 2 == 0 ? ControlKind.Text : ControlKind.Button;
            var start = Now;
            var t = Stopwatch.StartNew();
            stats.Probes++;
            var result = probe.Run(kind);
            if (result.Error is null)
            {
                Write($"ok    {start} {t.Elapsed.TotalMilliseconds,7:0.0} ms {o.Api} {kind} n={result.Count} ({what}, +{sw.ElapsedMilliseconds} ms)");
            }
            else
            {
                stats.Failures++;
                stats.Count(result.Error);
                Write($"FAIL  {start} {t.Elapsed.TotalMilliseconds,7:0.0} ms {o.Api} {kind} ({what}, +{sw.ElapsedMilliseconds} ms): {Describe(result.Error)}" +
                      (result.Step is { } step ? $" at {step}" : ""));
                RequestDump($"{o.Api} {kind} {what}: {Hr(result.Error)}");
                var again = probe.Run(kind);
                if (again.Error is null) { stats.RetryOk++; } else { stats.RetryFail++; }
                Write($"retry at once: {(again.Error is null ? $"ok n={again.Count}" : $"FAIL {Describe(again.Error)}{(again.Step is { } s2 ? $" at {s2}" : "")}")}");
                Thread.Sleep(300);
                var later = probe.Run(kind);
                Write($"retry 300 ms later: {(later.Error is null ? $"ok n={later.Count}" : $"FAIL {Describe(later.Error)}")}");
            }

            if (o.GapMs > 0)
            {
                Thread.Sleep(o.GapMs);
            }
        }
    }

    private static void RequestDump(string reason)
    {
        var n = Interlocked.Increment(ref _dumpRequests);
        if (n <= 30)
        {
            File.WriteAllText(Path.Combine(_logDir, $"dump-{n:D2}.req"), reason);
        }
    }

    /// <summary>The first button whose name matches (* is a wildcard), invoked; the lookup retried for 3 s.</summary>
    private static void Invoke(Probe probe, string pattern, Stats stats)
    {
        var regex = Glob(pattern);
        var sw = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                var target = probe.FindManaged(ControlType.Button).FirstOrDefault(e => regex.IsMatch(e.Current.Name ?? ""));
                if (target is not null)
                {
                    // Read before the Invoke, as the element may be gone after it, and never invoked twice.
                    var name = target.Current.Name;
                    var invoke = (InvokePattern)target.GetCurrentPattern(InvokePattern.Pattern);
                    var at = Now;
                    invoke.Invoke();
                    Write($"invoked '{name}' at {at}");
                    return;
                }
            }
            catch (Exception ex)
            {
                stats.TriggerFailures++;
                Write($"trigger lookup failed: {Describe(ex)}");
                RequestDump($"trigger lookup {pattern}: {Hr(ex)}");
            }

            if (sw.ElapsedMilliseconds > 3000)
            {
                throw new InvalidOperationException($"no button '{pattern}'");
            }

            Thread.Sleep(100);
        }
    }

    private static void WaitForText(Probe probe, string pattern, int ms, Stats stats)
    {
        var regex = Glob(pattern);
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        {
            try
            {
                if (probe.FindManaged(ControlType.Text).Any(e => regex.IsMatch(e.Current.Name ?? "")))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                stats.TriggerFailures++;
                Write($"text lookup failed: {Describe(ex)}");
            }

            Thread.Sleep(100);
        }

        throw new InvalidOperationException($"no text '{pattern}'");
    }

    private static Regex Glob(string pattern) => new("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");

    private static string Hr(Exception ex) => $"0x{ex.HResult:X8}";

    private static string Describe(Exception ex) =>
        $"{ex.GetType().FullName} {Hr(ex)} \"{ex.Message}\"" + (ex.InnerException is { } inner ? $" inner {inner.GetType().FullName} {Hr(inner)}" : "");

    private static void Close(Process p)
    {
        try
        {
            if (!p.HasExited)
            {
                p.CloseMainWindow();
                if (!p.WaitForExit(5000))
                {
                    p.Kill();
                    Write("the app was killed");
                }
            }

            Write($"the app exited with {p.ExitCode}");
        }
        catch (Exception ex)
        {
            Write($"close: {ex.Message}");
        }
    }

    private sealed class Stats
    {
        public int Probes { get; set; }

        public int Failures { get; set; }

        public int RetryOk { get; set; }

        public int RetryFail { get; set; }

        public int TriggerFailures { get; set; }

        public string? Stopped { get; set; }

        public SortedDictionary<string, int> ByHr { get; } = [];

        public void Count(Exception ex)
        {
            var key = $"{ex.GetType().Name} {Hr(ex)}";
            ByHr[key] = ByHr.GetValueOrDefault(key) + 1;
        }
    }
}

internal enum ControlKind
{
    Button,
    Text,
}

internal readonly record struct ProbeResult(int Count, Exception? Error, string? Step);

/// <summary>One way to search the window: a FindAll or a walk, through the managed client or the native one.</summary>
internal sealed class Probe
{
    private const int UiaControlTypePropertyId = 30003;
    private const int UiaNamePropertyId = 30005;
    private const int UiaButtonControlTypeId = 50000;
    private const int UiaTextControlTypeId = 50020;

    private readonly string _api;
    private readonly UIA.CUIAutomation8 _com = new();
    private readonly UIA.IUIAutomationElement _comWindow;
    private readonly UIA.IUIAutomationCacheRequest _cache;

    public Probe(string api, IntPtr hwnd)
    {
        _api = api;
        Managed = AutomationElement.FromHandle(hwnd);
        _comWindow = _com.ElementFromHandle(hwnd);
        _cache = _com.CreateCacheRequest();
        _cache.AddProperty(UiaNamePropertyId);
        _cache.AddProperty(UiaControlTypePropertyId);
    }

    public AutomationElement Managed { get; }

    public IEnumerable<AutomationElement> FindManaged(ControlType type) =>
        Managed.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, type)).Cast<AutomationElement>().ToList();

    public ProbeResult Run(ControlKind kind)
    {
        string? step = null;
        try
        {
            var count = _api switch
            {
                "managed" => FindManaged(kind == ControlKind.Text ? ControlType.Text : ControlType.Button).Count(),
                "com" => _comWindow.FindAll(UIA.TreeScope.TreeScope_Descendants, Condition(kind)).Length,
                "com-cache" => _comWindow.FindAllBuildCache(UIA.TreeScope.TreeScope_Descendants, Condition(kind), _cache).Length,
                "managed-walk" => WalkManaged(Managed, kind == ControlKind.Text ? ControlType.Text : ControlType.Button, ref step),
                "com-walk" => WalkCom(kind == ControlKind.Text ? UiaTextControlTypeId : UiaButtonControlTypeId, ref step),
                _ => throw new ArgumentException($"unknown api {_api}"),
            };
            return new ProbeResult(count, null, null);
        }
        catch (Exception ex)
        {
            // Any failure is a failed search, logged, never the end of the scenario.
            return new ProbeResult(0, ex, step);
        }
    }

    private UIA.IUIAutomationCondition Condition(ControlKind kind) =>
        _com.CreatePropertyCondition(UiaControlTypePropertyId, kind == ControlKind.Text ? UiaTextControlTypeId : UiaButtonControlTypeId);

    /// <summary>The raw view, depth first, one element at a time, as a find does it; the step that fails is logged.</summary>
    private static int WalkManaged(AutomationElement root, ControlType type, ref string? step)
    {
        var walker = TreeWalker.RawViewWalker;
        var count = 0;
        var stack = new Stack<(AutomationElement El, string Path)>();
        step = "first child of the window";
        var child = walker.GetFirstChild(root);
        if (child is not null) { stack.Push((child, "0")); }
        while (stack.Count > 0)
        {
            var (el, path) = stack.Pop();
            step = $"control type of {path}";
            if (el.Current.ControlType == type) { count++; }
            step = $"next sibling of {path} ({Name(el)})";
            var next = walker.GetNextSibling(el);
            if (next is not null) { stack.Push((next, NextPath(path))); }
            step = $"first child of {path} ({Name(el)})";
            var first = walker.GetFirstChild(el);
            if (first is not null) { stack.Push((first, path + ".0")); }
        }

        return count;
    }

    private int WalkCom(int typeId, ref string? step)
    {
        var walker = _com.RawViewWalker;
        var count = 0;
        var stack = new Stack<(UIA.IUIAutomationElement El, string Path)>();
        step = "first child of the window";
        var child = walker.GetFirstChildElement(_comWindow);
        if (child is not null) { stack.Push((child, "0")); }
        while (stack.Count > 0)
        {
            var (el, path) = stack.Pop();
            step = $"control type of {path}";
            if (el.CurrentControlType == typeId) { count++; }
            step = $"next sibling of {path} ({Name(el)})";
            var next = walker.GetNextSiblingElement(el);
            if (next is not null) { stack.Push((next, NextPath(path))); }
            step = $"first child of {path} ({Name(el)})";
            var first = walker.GetFirstChildElement(el);
            if (first is not null) { stack.Push((first, path + ".0")); }
        }

        return count;
    }

    private static string NextPath(string path)
    {
        var dot = path.LastIndexOf('.');
        var last = int.Parse(path[(dot + 1)..]);
        return (dot < 0 ? "" : path[..(dot + 1)]) + (last + 1);
    }

    private static string Name(AutomationElement el)
    {
        try { return $"{el.Current.ClassName} '{el.Current.Name}' [{string.Join(".", el.GetRuntimeId())}]"; }
        catch (Exception ex) { return $"<{ex.GetType().Name} 0x{ex.HResult:X8}>"; }
    }

    private static string Name(UIA.IUIAutomationElement el)
    {
        try { return $"{el.CurrentClassName} '{el.CurrentName}' [{string.Join(".", (int[])el.GetRuntimeId())}]"; }
        catch (Exception ex) { return $"<{ex.GetType().Name} 0x{ex.HResult:X8}>"; }
    }
}

internal sealed class Options
{
    public string Exe { get; private set; } = "";

    public string AppArgs { get; private set; } = "";

    public string LogDir { get; private set; } = ".";

    public string Name { get; private set; } = "run";

    public string Api { get; private set; } = "managed";

    public int Rounds { get; private set; } = 20;

    public int PollMs { get; private set; } = 1500;

    public int GapMs { get; private set; }

    public List<string> Triggers { get; private set; } = [];

    public bool Sample { get; private set; }

    public bool Listen { get; private set; }

    public static Options Parse(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--exe": o.Exe = Path.GetFullPath(Next()); break;
                case "--args": o.AppArgs = Next(); break;
                case "--log": o.LogDir = Next(); break;
                case "--name": o.Name = Next(); break;
                case "--api": o.Api = Next(); break;
                case "--rounds": o.Rounds = int.Parse(Next()); break;
                case "--poll-ms": o.PollMs = int.Parse(Next()); break;
                case "--gap-ms": o.GapMs = int.Parse(Next()); break;
                case "--triggers": o.Triggers = Next().Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(); break;
                case "--sample": o.Sample = true; break;
                case "--listen": o.Listen = true; break;
                default: throw new ArgumentException($"unknown argument {args[i]}");
            }
        }

        if (!File.Exists(o.Exe))
        {
            throw new ArgumentException($"no app at '{o.Exe}'");
        }

        return o;
    }
}
