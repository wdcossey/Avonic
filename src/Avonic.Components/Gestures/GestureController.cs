namespace Avonic.Components.Gestures;

/// <summary>
/// Ionic's <c>GESTURE_CONTROLLER</c> (<c>utils/gesture/gesture-controller.js</c>): which of the started gestures may capture,
/// which gestures are disabled by name, and whether scrolling is disabled. One per process, as Ionic's is one per document.
/// </summary>
public sealed class GestureController
{
    private int _gestureId;
    private readonly Dictionary<int, double> _requestedStart = new();
    private readonly Dictionary<string, HashSet<int>> _disabledGestures = new();
    private readonly HashSet<int> _disabledScroll = new();
    private int? _capturedId;

    private GestureController()
    {
    }

    /// <summary>Ionic <c>GESTURE_CONTROLLER</c>.</summary>
    public static GestureController Instance { get; } = new();

    /// <summary>
    /// Ionic's <c>ionGestureCaptured</c> on <c>document</c>: a gesture captured. Ionic's <c>tap-click</c> cancels the pressed
    /// surface on it.
    /// </summary>
    public event EventHandler<GestureCapturedEventArgs>? IonGestureCaptured;

    /// <summary>
    /// <see cref="IsScrollDisabled"/> changed: Ionic adds <c>backdrop-no-scroll</c> to <c>body</c> when the first disabler arrives,
    /// and removes it when the last one goes.
    /// </summary>
    public event EventHandler? ScrollDisabledChanged;

    /// <summary>Ionic <c>isCaptured()</c>: a gesture holds the capture.</summary>
    public bool IsCaptured => _capturedId is not null;

    /// <summary>
    /// Ionic <c>isScrollDisabled()</c>: a captured gesture with <see cref="GestureConfig.DisableScroll"/>, or a blocked
    /// <see cref="GestureBlocker"/> made with <c>disableScroll</c>, is asking for scrolling to stop. Ionic's only effect is
    /// <c>overflow: hidden</c> on <c>body</c>, which nothing in Avonic is, so nothing reads it yet.
    /// </summary>
    public bool IsScrollDisabled => _disabledScroll.Count > 0;

    /// <summary>Ionic <c>createBlocker(opts)</c>: disables the named gestures, and scrolling, while it's blocked.</summary>
    public GestureBlocker CreateBlocker(IEnumerable<string>? disable = null, bool disableScroll = false) =>
        new(this, NewId(), disable?.ToArray(), disableScroll);

    internal GestureDelegate CreateGesture(string name, int priority, bool disableScroll) =>
        new(this, NewId(), name, priority, disableScroll);

    internal bool Start(string gestureName, int id, double priority)
    {
        if (!CanStart(gestureName))
        {
            _requestedStart.Remove(id);
            return false;
        }

        _requestedStart[id] = priority;
        return true;
    }

    internal bool Capture(string gestureName, int id, double priority)
    {
        if (!Start(gestureName, id, priority))
        {
            return false;
        }

        var maxPriority = -10000d;
        foreach (var value in _requestedStart.Values)
        {
            maxPriority = Math.Max(maxPriority, value);
        }

        if (maxPriority == priority)
        {
            _capturedId = id;
            _requestedStart.Clear();
            IonGestureCaptured?.Invoke(this, new GestureCapturedEventArgs(gestureName));
            return true;
        }

        _requestedStart.Remove(id);
        return false;
    }

    internal void Release(int id)
    {
        _requestedStart.Remove(id);
        if (_capturedId == id)
        {
            _capturedId = null;
        }
    }

    internal void DisableGesture(string gestureName, int id)
    {
        if (!_disabledGestures.TryGetValue(gestureName, out var set))
        {
            set = new HashSet<int>();
            _disabledGestures[gestureName] = set;
        }

        set.Add(id);
    }

    internal void EnableGesture(string gestureName, int id)
    {
        if (_disabledGestures.TryGetValue(gestureName, out var set))
        {
            set.Remove(id);
        }
    }

    internal void DisableScroll(int id)
    {
        if (_disabledScroll.Add(id) && _disabledScroll.Count == 1)
        {
            ScrollDisabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void EnableScroll(int id)
    {
        if (_disabledScroll.Remove(id) && _disabledScroll.Count == 0)
        {
            ScrollDisabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal bool CanStart(string gestureName)
    {
        if (_capturedId is not null)
        {
            // a gesture already captured
            return false;
        }

        return !IsDisabled(gestureName);
    }

    internal bool IsDisabled(string gestureName) =>
        _disabledGestures.TryGetValue(gestureName, out var disabled) && disabled.Count > 0;

    private int NewId() => ++_gestureId;
}

/// <summary>Ionic's <c>GestureDelegate</c>: one gesture's handle on the controller.</summary>
internal sealed class GestureDelegate
{
    private readonly int _id;
    private readonly string _name;
    private readonly bool _disableScroll;
    private readonly double _priority;
    private GestureController? _ctrl;

    public GestureDelegate(GestureController ctrl, int id, string name, int priority, bool disableScroll)
    {
        _ctrl = ctrl;
        _id = id;
        _name = name;
        _disableScroll = disableScroll;
        // A tie goes to the gesture created later.
        _priority = priority * 1000000d + id;
    }

    public bool Start() => _ctrl is not null && _ctrl.Start(_name, _id, _priority);

    public bool Capture()
    {
        if (_ctrl is null)
        {
            return false;
        }

        var captured = _ctrl.Capture(_name, _id, _priority);
        if (captured && _disableScroll)
        {
            _ctrl.DisableScroll(_id);
        }

        return captured;
    }

    public void Release()
    {
        if (_ctrl is null)
        {
            return;
        }

        _ctrl.Release(_id);
        if (_disableScroll)
        {
            _ctrl.EnableScroll(_id);
        }
    }

    public void Destroy()
    {
        Release();
        _ctrl = null;
    }
}
