using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MonoMod.RuntimeDetour;

namespace UiaRepro;

/// <summary>
/// Hooks on Avalonia 12.1.4's Windows UIA provider (<c>Avalonia.Win32.Automation.AutomationNode</c> and
/// <c>RootAutomationNode</c>, internal): each call UIA makes is put in the log's ring with its thread, the element (the peer's
/// owner, and the node's runtime id), the result, how long it took, and any exception. Anomalies go to the events log and dump the
/// ring: an exception, and a non-root node whose fragment root is null (a peer whose parent chain doesn't reach the window).
/// </summary>
internal static class Hooks
{
    private static readonly List<Hook> Installed = [];
    private static Func<object, AutomationPeer?> _peerOf = _ => null;
    private static Type? _rootType;

    public static void Install()
    {
        var asm = Assembly.Load("Avalonia.Win32.Automation");
        var node = asm.GetType("Avalonia.Win32.Automation.AutomationNode", throwOnError: true)!;
        var root = asm.GetType("Avalonia.Win32.Automation.RootAutomationNode", throwOnError: true)!;
        _rootType = root;
        var peerProp = node.GetProperty("Peer", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        _peerOf = n => (AutomationPeer?)peerProp.GetValue(n);

        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        MethodInfo M(Type t, string name) => t.GetMethod(name, Any) ?? throw new MissingMethodException(t.Name, name);

        Add(M(node, "Navigate"), (Func<Func<object, int, object?>, object, int, object?>)((orig, self, dir) =>
            Logged(self, $"Navigate({(Direction)dir})", () => orig(self, dir), Describe)));
        Add(M(node, "GetFragmentRoot"), (Func<Func<object, object?>, object, object?>)((orig, self) =>
        {
            var r = Logged(self, "GetFragmentRoot", () => orig(self), Describe);
            if (r is null)
            {
                Anomaly(self, "GetFragmentRoot returned null");
            }

            return r;
        }));
        Add(M(root, "GetFragmentRoot"), (Func<Func<object, object?>, object, object?>)((orig, self) =>
            Logged(self, "Root.GetFragmentRoot", () => orig(self), Describe)));
        Add(M(node, "GetPropertyValue"), (Func<Func<object, int, object?>, object, int, object?>)((orig, self, id) =>
            Logged(self, $"GetPropertyValue({id})", () => orig(self, id), Value)));
        Add(M(node, "GetPatternProvider"), (Func<Func<object, int, object?>, object, int, object?>)((orig, self, id) =>
            Logged(self, $"GetPatternProvider({id})", () => orig(self, id), r => r is null ? "null" : "this")));
        Add(M(node, "GetBoundingRectangle"), (Func<Func<object, Avalonia.Rect>, object, Avalonia.Rect>)((orig, self) =>
            Logged(self, "GetBoundingRectangle", () => orig(self), r => r.ToString())));
        Add(M(node, "GetRuntimeId"), (Func<Func<object, int[]>, object, int[]>)((orig, self) =>
            Logged(self, "GetRuntimeId", () => orig(self), r => string.Join(".", r))));
        Add(M(node, "GetHostRawElementProvider"), (Func<Func<object, object?>, object, object?>)((orig, self) =>
            Logged(self, "GetHostRawElementProvider", () => orig(self), r => r is null ? "null" : "host")));
        Add(M(root, "GetHostRawElementProvider"), (Func<Func<object, object?>, object, object?>)((orig, self) =>
            Logged(self, "Root.GetHostRawElementProvider", () => orig(self), r => r is null ? "null" : "host")));
        Add(M(node, "GetProviderOptions"), (Func<Func<object, int>, object, int>)((orig, self) =>
            Logged(self, "GetProviderOptions", () => orig(self), r => r.ToString())));
        Add(M(root, "ElementProviderFromPoint"), (Func<Func<object, double, double, object?>, object, double, double, object?>)((orig, self, x, y) =>
            Logged(self, $"ElementProviderFromPoint({x},{y})", () => orig(self, x, y), Describe)));
        Add(M(root, "GetFocus"), (Func<Func<object, object?>, object, object?>)((orig, self) =>
            Logged(self, "GetFocus", () => orig(self), Describe)));
        Add(M(node, "RaiseChildrenChanged"), (Action<Action<object>, object>)((orig, self) =>
        {
            Log.Event($"StructureChanged(ChildrenInvalidated) raised by {Describe(self)}");
            orig(self);
        }));

        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            if (e.Exception is OperationCanceledException)
            {
                return;
            }

            var stack = new StackTrace(1, false).ToString().Split(Environment.NewLine).Take(25);
            Log.Event($"FIRST-CHANCE {e.Exception.GetType().FullName} 0x{e.Exception.HResult:X8}: {e.Exception.Message}" +
                Environment.NewLine + "    " + string.Join(Environment.NewLine + "    ", stack));
        };

        Log.Event($"hooks installed: {Installed.Count}");
    }

    /// <summary>A node made for a lone <see cref="Border"/>'s peer, and two of its calls, to see that the hooks fire.</summary>
    public static void SelfTest()
    {
        var asm = Assembly.Load("Avalonia.Win32.Automation");
        var node = asm.GetType("Avalonia.Win32.Automation.AutomationNode", throwOnError: true)!;
        var peer = ControlAutomationPeer.CreatePeerForElement(new Border { Name = "selftest" });
        var n = node.GetMethod("GetOrCreate", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [peer])!;
        node.GetMethod("GetRuntimeId")!.Invoke(n, null);
        var direction = node.GetMethod("Navigate")!.GetParameters()[0].ParameterType;
        node.GetMethod("Navigate")!.Invoke(n, [Enum.ToObject(direction, 3)]);
        Log.Dump("self-test");
    }

    private enum Direction
    {
        Parent,
        NextSibling,
        PreviousSibling,
        FirstChild,
        LastChild,
    }

    private static void Add(MethodInfo target, Delegate hook) => Installed.Add(new Hook(target, hook));

    private static T Logged<T>(object self, string call, Func<T> body, Func<T, string> describe)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var r = body();
            Log.Call($"{call} on {Describe(self)} -> {describe(r)} ({sw.Elapsed.TotalMilliseconds:0.000} ms)");
            return r;
        }
        catch (Exception ex)
        {
            Log.Call($"{call} on {Describe(self)} THREW {ex.GetType().Name} 0x{ex.HResult:X8} ({sw.Elapsed.TotalMilliseconds:0.000} ms)");
            Log.Event($"EXCEPTION in {call} on {Describe(self)}: {ex}");
            Log.Dump($"exception in {call}");
            throw;
        }
    }

    private static string Value(object? v) => v switch
    {
        null => "null",
        string s => $"\"{s}\"",
        int[] a => string.Join(".", a),
        double[] d => string.Join(",", d),
        _ => $"{v} ({v.GetType().Name})",
    };

    /// <summary>A node: its peer's owner, type and name, its visibility as last seen, and the node's runtime id (3.hash).</summary>
    private static string Describe(object? node)
    {
        if (node is null)
        {
            return "null";
        }

        try
        {
            var peer = _peerOf(node);
            var rid = RuntimeHelpers.GetHashCode(node);
            var kind = node.GetType() == _rootType ? "root " : "";
            return peer is ControlAutomationPeer { Owner: var owner }
                ? $"{kind}{owner.GetType().Name}#{owner.Name}{(owner.IsEffectivelyVisible ? "" : "(hidden)")} [3.{rid}]"
                : $"{kind}{peer?.GetType().Name ?? "?"} [3.{rid}]";
        }
        catch (Exception ex)
        {
            return $"<{ex.GetType().Name}>";
        }
    }

    /// <summary>An anomaly: the peer's chain read on the UI thread, then the ring dumped.</summary>
    private static void Anomaly(object node, string what)
    {
        var chain = "";
        try
        {
            if (_peerOf(node) is { } peer)
            {
                chain = Environment.CurrentManagedThreadId == Log.UiThreadId
                    ? Chain(peer)
                    : Dispatcher.UIThread.InvokeAsync(() => Chain(peer)).Result;
            }
        }
        catch (Exception ex)
        {
            chain = $"<{ex.GetType().Name}>";
        }

        Log.Event($"ANOMALY {what} on {Describe(node)}; chain: {chain}");
        Log.Dump($"{what} on {Describe(node)}");
    }

    /// <summary>The peer's parents up to the last, and its owner's visual parents: where the chain is cut.</summary>
    private static string Chain(AutomationPeer peer)
    {
        var peers = new List<string>();
        for (AutomationPeer? p = peer; p is not null; p = p.GetParent())
        {
            peers.Add(p is ControlAutomationPeer c ? $"{c.Owner.GetType().Name}#{c.Owner.Name}" : p.GetType().Name);
        }

        var visuals = new List<string>();
        if (peer is ControlAutomationPeer { Owner: var owner })
        {
            for (Avalonia.Visual? v = owner; v is not null; v = v.GetVisualParent())
            {
                visuals.Add($"{v.GetType().Name}#{(v as Control)?.Name}{(v.IsVisible ? "" : "(IsVisible=false)")}");
            }
        }

        return $"peers {string.Join(" < ", peers)}; visuals {string.Join(" < ", visuals)}";
    }
}
