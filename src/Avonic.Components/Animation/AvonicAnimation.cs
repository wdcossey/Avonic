using Avalonia;
using Avalonia.Animation.Easings;

namespace Avonic.Components.Animation;

/// <summary>
/// Ionic's <c>Animation</c>, made by <see cref="Create"/> as <c>createAnimation()</c> makes it: a builder over Web Animations,
/// ported from <c>utils/animation/animation.js</c>. Every member returns the animation, so the md builders read as Ionic's do.
/// See <c>docs/infra/overlays/PLAN.md</c>.
/// </summary>
/// <remarks>
/// The timing model is Web Animations' (phases, active time, iterations, direction, fill), on <see cref="AvonicConfig"/>'s clock
/// and frames. <see cref="Play"/> shows the current values at once and starts at the next frame; the animation finishes at the
/// first frame at or past its end, so one of zero duration finishes at the next frame, as in Chromium. Values are applied at
/// animation priority, and with <see cref="AnimationFill.Both"/>, the default, the last stays until <see cref="Stop"/> or
/// <see cref="Destroy"/>.
/// </remarks>
public sealed class AvonicAnimation
{
    private readonly List<Visual> _elements = [];
    private readonly List<AvonicAnimation> _childAnimations = [];
    private readonly List<(Action<int, AvonicAnimation> Callback, bool OneTime)> _onFinish = [];
    private readonly List<Action> _onStopOneTime = [];
    private readonly List<Action> _beforeAddRead = [];
    private readonly List<Action> _beforeAddWrite = [];
    private readonly List<Action> _afterAddRead = [];
    private readonly List<Action> _afterAddWrite = [];
    private readonly List<string> _beforeAddClasses = [];
    private readonly List<string> _beforeRemoveClasses = [];
    private readonly List<string> _afterAddClasses = [];
    private readonly List<string> _afterRemoveClasses = [];
    private Dictionary<string, object> _beforeStyles = new(StringComparer.Ordinal);
    private Dictionary<string, object> _afterStyles = new(StringComparer.Ordinal);
    private List<AnimationKeyframe> _keyframes = [];

    private double? _delay;
    private double? _duration;
    private string? _easing;
    private double? _iterations;
    private AnimationFill? _fill;
    private AnimationDirection? _direction;

    private bool _initialized;
    private int _numAnimationsRunning;
    private bool _shouldForceSyncPlayback;
    private bool _willComplete = true;
    private bool _finished;
    private bool _shouldCalculateNumAnimations = true;
    private bool _paused;
    private Player? _player;

    // progressStart and progressEnd's coercions: shouldForceLinearEasing, forceDurationValue, forceDirectionValue. Ionic's
    // forceDelayValue is for browsers without Web Animations only.
    private bool _shouldForceLinearEasing;
    private double? _forceDuration;
    private AnimationDirection? _forceDirection;

    private AvonicAnimation(string? id) => Id = id;

    /// <summary>Ionic <c>createAnimation(animationId?)</c>.</summary>
    public static AvonicAnimation Create(string? animationId = null) => new(animationId);

    /// <summary>Ionic <c>id</c>.</summary>
    public string? Id { get; }

    /// <summary>Ionic <c>elements</c>.</summary>
    public IReadOnlyList<Visual> Elements => _elements;

    /// <summary>Ionic <c>childAnimations</c>.</summary>
    public IReadOnlyList<AvonicAnimation> ChildAnimations => _childAnimations;

    /// <summary>Ionic <c>parentAnimation</c>.</summary>
    public AvonicAnimation? ParentAnimation { get; private set; }

    // ── Building ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>addElement</c>. A null element is ignored, as Ionic's <c>querySelector</c> results can be.</summary>
    public AvonicAnimation AddElement(Visual? element)
    {
        if (element is not null)
        {
            _elements.Add(element);
        }

        return this;
    }

    /// <summary>Ionic <c>addElement</c> with several.</summary>
    public AvonicAnimation AddElement(IEnumerable<Visual?> elements)
    {
        foreach (var element in elements)
        {
            AddElement(element);
        }

        return this;
    }

    /// <summary>Ionic <c>addAnimation</c>: each becomes a child, whose unset timing is this one's.</summary>
    public AvonicAnimation AddAnimation(params AvonicAnimation?[] animations)
    {
        foreach (var animation in animations)
        {
            if (animation is not null)
            {
                animation.Parent(this);
                _childAnimations.Add(animation);
            }
        }

        return this;
    }

    /// <summary>Ionic <c>parent</c>. <see cref="AddAnimation"/> sets it.</summary>
    public AvonicAnimation Parent(AvonicAnimation animation)
    {
        ParentAnimation = animation;
        return this;
    }

    /// <summary>
    /// Ionic <c>keyframes</c>. The properties are <c>opacity</c>, a number or a numeric string, <c>transform</c>, a CSS
    /// transform list (see <c>docs/infra/overlays/PLAN.md</c>), and <c>maxHeight</c>, a length in px or a percentage of the
    /// parent's height. On an animation that has started, they replace its keyframes where it is, as Ionic's
    /// <c>setKeyframes</c> does: the sheet modal's gesture changes them between drags.
    /// </summary>
    /// <exception cref="ArgumentException">A property or value this doesn't take.</exception>
    public AvonicAnimation Keyframes(IEnumerable<AnimationKeyframe> keyframes)
    {
        var list = keyframes.ToList();
        foreach (var keyframe in list)
        {
            Validate(keyframe);
        }

        _keyframes = list;
        _player?.SetKeyframes();
        return this;
    }

    /// <summary>Ionic <c>getKeyframes</c>.</summary>
    public IReadOnlyList<AnimationKeyframe> GetKeyframes() => _keyframes;

    /// <summary>Ionic <c>from</c>: the value at offset 0, in the first keyframe if it's there, else a new one.</summary>
    public AvonicAnimation From(string property, object value)
    {
        Validate(property, value);
        var first = _keyframes.Count > 0 ? _keyframes[0] : null;
        if (first is not null && first.Offset is null or 0)
        {
            first[property] = value;
        }
        else
        {
            _keyframes = [new AnimationKeyframe(0) { [property] = value }, .. _keyframes];
        }

        return this;
    }

    /// <summary>Ionic <c>to</c>: the value at offset 1, in the last keyframe if it's there, else a new one.</summary>
    public AvonicAnimation To(string property, object value)
    {
        Validate(property, value);
        var last = _keyframes.Count > 0 ? _keyframes[^1] : null;
        if (last is not null && last.Offset is null or 1)
        {
            last[property] = value;
        }
        else
        {
            _keyframes = [.. _keyframes, new AnimationKeyframe(1) { [property] = value }];
        }

        return this;
    }

    /// <summary>Ionic <c>fromTo</c>.</summary>
    public AvonicAnimation FromTo(string property, object fromValue, object toValue) => From(property, fromValue).To(property, toValue);

    // ── Timing ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>duration</c>, in milliseconds.</summary>
    public AvonicAnimation Duration(double milliseconds)
    {
        _duration = milliseconds >= 0 ? milliseconds : throw new ArgumentOutOfRangeException(nameof(milliseconds));
        return this;
    }

    /// <summary>Ionic <c>delay</c>, in milliseconds.</summary>
    public AvonicAnimation Delay(double milliseconds)
    {
        _delay = milliseconds;
        return this;
    }

    /// <summary>Ionic <c>easing</c>: <c>linear</c>, <c>ease</c>, <c>ease-in</c>, <c>ease-out</c>, <c>ease-in-out</c> or <c>cubic-bezier()</c>.</summary>
    /// <exception cref="ArgumentException">Any other CSS easing.</exception>
    public AvonicAnimation Easing(string easing)
    {
        _ = CubicBezierEasing.ParseCss(easing);
        _easing = easing;
        return this;
    }

    /// <summary>Ionic <c>iterations</c>. <see cref="double.PositiveInfinity"/> repeats for ever.</summary>
    public AvonicAnimation Iterations(double iterations)
    {
        _iterations = iterations >= 0 ? iterations : throw new ArgumentOutOfRangeException(nameof(iterations));
        return this;
    }

    /// <summary>Ionic <c>fill</c>.</summary>
    public AvonicAnimation Fill(AnimationFill fill)
    {
        _fill = fill;
        return this;
    }

    /// <summary>Ionic <c>direction</c>.</summary>
    public AvonicAnimation Direction(AnimationDirection direction)
    {
        _direction = direction;
        return this;
    }

    /// <summary>Ionic <c>getFill</c>: this one's, else the parent's, else <see cref="AnimationFill.Both"/>.</summary>
    public AnimationFill GetFill() => _fill ?? ParentAnimation?.GetFill() ?? AnimationFill.Both;

    /// <summary>
    /// Ionic <c>getDirection</c>: the one <see cref="ProgressEnd"/> forces, else this one's, else the parent's, else
    /// <see cref="AnimationDirection.Normal"/>.
    /// </summary>
    public AnimationDirection GetDirection() =>
        _forceDirection ?? _direction ?? ParentAnimation?.GetDirection() ?? AnimationDirection.Normal;

    /// <summary>
    /// Ionic <c>getEasing</c>: <c>linear</c> from a <see cref="ProgressStart"/> that forces it until <see cref="ProgressEnd"/>,
    /// else this one's, else the parent's, else <c>linear</c>.
    /// </summary>
    public string GetEasing() => _shouldForceLinearEasing ? "linear" : _easing ?? ParentAnimation?.GetEasing() ?? "linear";

    /// <summary>
    /// Ionic <c>getDuration</c>: 0 during a <c>sync</c> play, else the one <see cref="ProgressEnd"/> forces, else this one's, else
    /// the parent's, else 0.
    /// </summary>
    public double GetDuration() =>
        _shouldForceSyncPlayback ? 0 : _forceDuration ?? _duration ?? ParentAnimation?.GetDuration() ?? 0;

    /// <summary>Ionic <c>getIterations</c>: this one's, else the parent's, else 1.</summary>
    public double GetIterations() => _iterations ?? ParentAnimation?.GetIterations() ?? 1;

    /// <summary>Ionic <c>getDelay</c>: this one's, else the parent's, else 0.</summary>
    public double GetDelay() => _delay ?? ParentAnimation?.GetDelay() ?? 0;

    // ── Hooks ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>beforeAddRead</c>: run at the start of the first play, before the writes.</summary>
    public AvonicAnimation BeforeAddRead(Action read)
    {
        _beforeAddRead.Add(read);
        return this;
    }

    /// <summary>Ionic <c>beforeAddWrite</c>: run at the start of the first play, after the reads.</summary>
    public AvonicAnimation BeforeAddWrite(Action write)
    {
        _beforeAddWrite.Add(write);
        return this;
    }

    /// <summary>Ionic <c>afterAddRead</c>: run when it finishes, before the writes.</summary>
    public AvonicAnimation AfterAddRead(Action read)
    {
        _afterAddRead.Add(read);
        return this;
    }

    /// <summary>Ionic <c>afterAddWrite</c>: run when it finishes, after the reads.</summary>
    public AvonicAnimation AfterAddWrite(Action write)
    {
        _afterAddWrite.Add(write);
        return this;
    }

    /// <summary>Ionic <c>beforeAddClass</c>: style classes added to the elements at the start.</summary>
    public AvonicAnimation BeforeAddClass(params string[] classNames)
    {
        _beforeAddClasses.AddRange(classNames);
        return this;
    }

    /// <summary>Ionic <c>beforeRemoveClass</c>.</summary>
    public AvonicAnimation BeforeRemoveClass(params string[] classNames)
    {
        _beforeRemoveClasses.AddRange(classNames);
        return this;
    }

    /// <summary>Ionic <c>afterAddClass</c>: style classes added when it finishes.</summary>
    public AvonicAnimation AfterAddClass(params string[] classNames)
    {
        _afterAddClasses.AddRange(classNames);
        return this;
    }

    /// <summary>Ionic <c>afterRemoveClass</c>.</summary>
    public AvonicAnimation AfterRemoveClass(params string[] classNames)
    {
        _afterRemoveClasses.AddRange(classNames);
        return this;
    }

    /// <summary>
    /// Ionic <c>beforeStyles</c>: inline styles, local values, set at the start: <c>opacity</c>, and <c>pointer-events</c>
    /// (<c>none</c>, <c>auto</c>), which is <c>IsHitTestVisible</c>. <c>""</c> removes one. Replaces any given before.
    /// </summary>
    /// <exception cref="ArgumentException">Another property.</exception>
    public AvonicAnimation BeforeStyles(Dictionary<string, object>? styles = null)
    {
        _beforeStyles = ValidateStyles(styles);
        return this;
    }

    /// <summary>Ionic <c>beforeClearStyles</c>: removes these inline styles at the start.</summary>
    public AvonicAnimation BeforeClearStyles(IEnumerable<string>? propertyNames = null)
    {
        foreach (var name in propertyNames ?? [])
        {
            _ = AnimatedProperty.ForStyles(name);
            _beforeStyles[name] = "";
        }

        return this;
    }

    /// <summary>Ionic <c>afterStyles</c>: as <see cref="BeforeStyles"/>, when it finishes.</summary>
    public AvonicAnimation AfterStyles(Dictionary<string, object>? styles = null)
    {
        _afterStyles = ValidateStyles(styles);
        return this;
    }

    /// <summary>Ionic <c>afterClearStyles</c>: removes these inline styles when it finishes.</summary>
    public AvonicAnimation AfterClearStyles(IEnumerable<string>? propertyNames = null)
    {
        foreach (var name in propertyNames ?? [])
        {
            _ = AnimatedProperty.ForStyles(name);
            _afterStyles[name] = "";
        }

        return this;
    }

    /// <summary>
    /// Ionic <c>onFinish</c>: called with the step it ended at, 1, and the animation, each time it finishes, or only the next time
    /// with <paramref name="oneTimeCallback"/>.
    /// </summary>
    public AvonicAnimation OnFinish(Action<int, AvonicAnimation> callback, bool oneTimeCallback = false)
    {
        _onFinish.Add((callback, oneTimeCallback));
        return this;
    }

    // ── Playing ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>isRunning</c>: playing and not paused.</summary>
    public bool IsRunning() => _numAnimationsRunning != 0 && !_paused;

    /// <summary>
    /// Ionic <c>play({ sync })</c>: plays this and its children, and completes when they've all finished, or at
    /// <see cref="Stop"/>. With <paramref name="sync"/>, its duration is 0 until it has.
    /// </summary>
    public Task Play(bool sync = false)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (sync)
        {
            _shouldForceSyncPlayback = true;
            OnFinish((_, _) => _shouldForceSyncPlayback = false, oneTimeCallback: true);
        }

        if (!_initialized)
        {
            InitializeAnimation();
        }

        if (_finished)
        {
            _player?.Seek(0);
            _finished = false;
        }

        if (_shouldCalculateNumAnimations)
        {
            _numAnimationsRunning = _childAnimations.Count + 1;
            _shouldCalculateNumAnimations = false;
        }

        // One fires, and takes the other away, so that a play, stop and play again doesn't resolve twice.
        Action<int, AvonicAnimation>? onFinish = null;
        Action onStop = () =>
        {
            _onFinish.RemoveAll(c => c.Callback == onFinish);
            done.TrySetResult();
        };
        onFinish = (_, _) =>
        {
            _onStopOneTime.Remove(onStop);
            done.TrySetResult();
        };
        OnFinish(onFinish, oneTimeCallback: true);
        _onStopOneTime.Add(onStop);

        foreach (var child in _childAnimations)
        {
            child.Play();
        }

        _player?.Play();
        if (_keyframes.Count == 0 || _elements.Count == 0)
        {
            AnimationFinish();
        }

        _paused = false;
        return done.Task;
    }

    /// <summary>Ionic <c>pause</c>: holds this and its children where they are. <see cref="Play"/> resumes.</summary>
    public AvonicAnimation Pause()
    {
        foreach (var child in _childAnimations)
        {
            child.Pause();
        }

        PauseAnimation();
        return this;
    }

    /// <summary>
    /// Ionic <c>progressStart(forceLinearEasing, step)</c>: pauses this and its children for a gesture to drive, linear if
    /// <paramref name="forceLinearEasing"/>, at <paramref name="step"/> (0 to 1 of the duration) if it's given.
    /// </summary>
    public AvonicAnimation ProgressStart(bool forceLinearEasing = false, double? step = null)
    {
        foreach (var child in _childAnimations)
        {
            child.ProgressStart(forceLinearEasing, step);
        }

        PauseAnimation();
        _shouldForceLinearEasing = forceLinearEasing;
        if (!_initialized)
        {
            InitializeAnimation();
        }

        Update(step);
        return this;
    }

    /// <summary>Ionic <c>progressStep(step)</c>: this and its children held at <paramref name="step"/>, clamped to 0..0.9999.</summary>
    public AvonicAnimation ProgressStep(double step)
    {
        foreach (var child in _childAnimations)
        {
            child.ProgressStep(step);
        }

        SetAnimationStep(step);
        return this;
    }

    /// <summary>
    /// Ionic <c>progressEnd(playTo, step, dur)</c>: the easing back, and played from <paramref name="step"/> to the end
    /// (<paramref name="playTo"/> 1) or back to the start (0, which finishes at step 0) over <paramref name="duration"/> ms, or its
    /// own duration. Null for <paramref name="playTo"/> only sets them up. A child plays with its parent.
    /// </summary>
    public AvonicAnimation ProgressEnd(int? playTo, double step, double? duration = null)
    {
        _shouldForceLinearEasing = false;
        foreach (var child in _childAnimations)
        {
            child.ProgressEnd(playTo, step, duration);
        }

        if (duration is { } forced)
        {
            _forceDuration = forced;
        }

        _finished = false;
        _willComplete = true;
        if (playTo == 0)
        {
            _forceDirection = GetDirection() == AnimationDirection.Reverse ? AnimationDirection.Normal : AnimationDirection.Reverse;
            if (_forceDirection == AnimationDirection.Reverse)
            {
                _willComplete = false;
            }

            Update();
            SetAnimationStep(1 - step);
        }
        else if (playTo == 1)
        {
            Update();
            SetAnimationStep(step);
        }

        if (playTo is not null && ParentAnimation is null)
        {
            _ = Play();
        }

        return this;
    }

    /// <summary>Ionic's <c>pauseAnimation</c>: this one's own playback, once it's been initialized.</summary>
    private void PauseAnimation()
    {
        if (_initialized)
        {
            _player?.Pause();
            _paused = true;
        }
    }

    /// <summary>Ionic's <c>update</c> without <c>deep</c>: the playback takes the timing as it is now, and the step if given.</summary>
    private void Update(double? step = null)
    {
        if (step is { } s)
        {
            SetAnimationStep(s);
        }
        else
        {
            _player?.Refresh();
        }
    }

    /// <summary>Ionic's <c>setAnimationStep</c>: paused at the delay plus <paramref name="step"/>, clamped to 0..0.9999, of the duration.</summary>
    private void SetAnimationStep(double step)
    {
        step = Math.Min(Math.Max(step, 0), 0.9999);
        _player?.SeekPaused(GetDelay() + GetDuration() * step);
    }

    /// <summary>
    /// Ionic <c>stop</c>: cancels this and its children, so their values go, and completes a pending <see cref="Play"/>.
    /// The finish callbacks stay, for the next play.
    /// </summary>
    public void Stop()
    {
        foreach (var child in _childAnimations)
        {
            child.Stop();
        }

        if (_initialized)
        {
            CleanUpElements();
            _initialized = false;
        }

        ResetFlags();
        var callbacks = _onStopOneTime.ToArray();
        _onStopOneTime.Clear();
        foreach (var callback in callbacks)
        {
            callback();
        }
    }

    /// <summary>Ionic <c>destroy</c>: cancels this and its children, and forgets their elements, keyframes and callbacks.</summary>
    public AvonicAnimation Destroy()
    {
        foreach (var child in _childAnimations)
        {
            child.Destroy();
        }

        CleanUpElements();
        _elements.Clear();
        _childAnimations.Clear();
        _keyframes.Clear();
        _onFinish.Clear();
        _initialized = false;
        _shouldCalculateNumAnimations = true;
        return this;
    }

    /// <summary>Ionic's internal <c>animationFinish</c>: this one's own playback, or a child's, has finished.</summary>
    internal void AnimationFinish()
    {
        if (_numAnimationsRunning == 0)
        {
            return;
        }

        _numAnimationsRunning--;
        if (_numAnimationsRunning == 0)
        {
            AfterAnimation();
            ParentAnimation?.AnimationFinish();
        }
    }

    private void ResetFlags()
    {
        _shouldForceLinearEasing = false;
        _forceDuration = null;
        _forceDirection = null;
        _shouldForceSyncPlayback = false;
        _shouldCalculateNumAnimations = true;
        _numAnimationsRunning = 0;
        _finished = false;
        _willComplete = true;
        _paused = false;
    }

    private void CleanUpElements()
    {
        _player?.Cancel();
        _player = null;
    }

    private void InitializeAnimation()
    {
        BeforeAnimation();
        if (_keyframes.Count > 0 && _elements.Count > 0)
        {
            _player = new Player(this);
        }

        _initialized = true;
    }

    private void BeforeAnimation()
    {
        foreach (var read in _beforeAddRead)
        {
            read();
        }

        foreach (var write in _beforeAddWrite)
        {
            write();
        }

        ApplyClassesAndStyles(_beforeAddClasses, _beforeRemoveClasses, _beforeStyles);
    }

    private void AfterAnimation()
    {
        foreach (var read in _afterAddRead)
        {
            read();
        }

        foreach (var write in _afterAddWrite)
        {
            write();
        }

        var currentStep = _willComplete ? 1 : 0;
        ApplyClassesAndStyles(_afterAddClasses, _afterRemoveClasses, _afterStyles);

        // The coercions go before the callbacks, so that one that starts a progress again doesn't get them.
        _forceDuration = null;
        _forceDirection = null;

        foreach (var (callback, _) in _onFinish.Where(c => !c.OneTime).ToArray())
        {
            callback(currentStep, this);
        }

        var oneTime = _onFinish.Where(c => c.OneTime).ToArray();
        _onFinish.RemoveAll(c => c.OneTime);
        foreach (var (callback, _) in oneTime)
        {
            callback(currentStep, this);
        }

        _shouldCalculateNumAnimations = true;
        if (_willComplete)
        {
            _finished = true;
        }

        _willComplete = true;
    }

    private void ApplyClassesAndStyles(List<string> add, List<string> remove, Dictionary<string, object> styles)
    {
        foreach (var element in _elements)
        {
            if (element is StyledElement styled)
            {
                foreach (var name in add)
                {
                    styled.Classes.Add(name);
                }

                foreach (var name in remove)
                {
                    styled.Classes.Remove(name);
                }
            }

            foreach (var (name, value) in styles)
            {
                AnimatedProperty.ForStyles(name).SetStyle(element, value);
            }
        }
    }

    private static void Validate(AnimationKeyframe keyframe)
    {
        foreach (var (property, value) in keyframe)
        {
            Validate(property, value);
        }
    }

    private static void Validate(string property, object value) => _ = AnimatedProperty.ForKeyframes(property).Parse(value);

    private static Dictionary<string, object> ValidateStyles(Dictionary<string, object>? styles)
    {
        var copy = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (name, value) in styles ?? [])
        {
            _ = AnimatedProperty.ForStyles(name);
            copy[name] = value;
        }

        return copy;
    }

    /// <summary>
    /// Web Animations' iteration progress, eased, for a local time, or null when the effect has no value (outside its active
    /// interval with no fill there). <c>css-animations</c> and <c>web-animations-1</c> §4.
    /// </summary>
    internal static double? Progress(
        double localTime, double delay, double duration, double iterations, AnimationDirection direction, AnimationFill fill, Easing? easing)
    {
        var activeDuration = duration == 0 || iterations == 0 ? 0 : duration * iterations;
        var endTime = Math.Max(delay + activeDuration, 0);
        var beforeActive = Math.Max(Math.Min(delay, endTime), 0);
        var activeAfter = Math.Max(Math.Min(delay + activeDuration, endTime), 0);
        var phase = localTime < beforeActive ? Phase.Before : localTime >= activeAfter ? Phase.After : Phase.Active;

        double activeTime;
        switch (phase)
        {
            case Phase.Before when fill is AnimationFill.Backwards or AnimationFill.Both:
                activeTime = Math.Max(localTime - delay, 0);
                break;
            case Phase.Active:
                activeTime = localTime - delay;
                break;
            case Phase.After when fill is AnimationFill.Forwards or AnimationFill.Both:
                activeTime = Math.Max(Math.Min(localTime - delay, activeDuration), 0);
                break;
            default:
                return null;
        }

        var overall = duration == 0 ? (phase == Phase.Before ? 0 : iterations) : activeTime / duration;
        var simple = double.IsInfinity(overall) ? 0 : overall % 1;
        if (simple == 0 && phase != Phase.Before && activeTime == activeDuration && iterations != 0)
        {
            simple = 1;
        }

        var iteration = phase == Phase.After && double.IsInfinity(iterations)
            ? double.PositiveInfinity
            : simple == 1 ? Math.Floor(overall) - 1 : Math.Floor(overall);
        var even = double.IsInfinity(iteration) || iteration % 2 == 0;
        var directed = direction switch
        {
            AnimationDirection.Reverse => 1 - simple,
            AnimationDirection.Alternate => even ? simple : 1 - simple,
            AnimationDirection.AlternateReverse => even ? 1 - simple : simple,
            _ => simple,
        };
        return easing?.Ease(directed) ?? directed;
    }

    private enum Phase
    {
        Before,
        Active,
        After,
    }

    /// <summary>
    /// The Web Animations of one <see cref="AvonicAnimation"/>: one per element in Ionic, which share their timing, so one clock
    /// here, and one binding per element and property.
    /// </summary>
    private sealed class Player
    {
        private readonly AvonicAnimation _owner;
        private readonly List<Track> _tracks = [];
        private double _holdTime;
        private double? _startTime;
        private bool _playing;
        private IDisposable? _frame;
        private string? _easingText;
        private Easing? _easing;

        internal Player(AvonicAnimation owner)
        {
            _owner = owner;
            BuildTracks();

            // Ionic creates each Web Animation paused at 0, which shows its value there at once.
            Apply(0);
        }

        private string[] Properties() => _owner._keyframes.SelectMany(k => k.Keys).Distinct(StringComparer.Ordinal).ToArray();

        private void BuildTracks()
        {
            var offsets = ComputedOffsets(_owner._keyframes);
            foreach (var element in _owner._elements)
            {
                foreach (var name in Properties())
                {
                    _tracks.Add(new Track(element, AnimatedProperty.ForKeyframes(name), _owner._keyframes, offsets));
                }
            }
        }

        private double Now => AvonicConfig.Now().TotalMilliseconds;

        private double EndTime
        {
            get
            {
                var duration = _owner.GetDuration();
                var iterations = _owner.GetIterations();
                var active = duration == 0 || iterations == 0 ? 0 : duration * iterations;
                return Math.Max(_owner.GetDelay() + active, 0);
            }
        }

        internal void Play()
        {
            // Web Animations' play() does nothing to an animation that's already playing.
            if (_playing)
            {
                return;
            }

            if (_holdTime >= EndTime && !double.IsInfinity(EndTime))
            {
                _holdTime = 0;
            }

            // Its values are already shown: by the first play's initialization, a seek, or a pause, as a paused Web Animation
            // shows its own.
            _playing = true;
            _startTime = null;
            RequestFrame();
        }

        internal void Pause()
        {
            if (_playing && _startTime is { } start)
            {
                _holdTime = Now - start;
            }

            _playing = false;
            _startTime = null;
            CancelFrame();
            Apply(_holdTime);
        }

        internal void Seek(double time)
        {
            _holdTime = time;
            _startTime = null;
            Apply(time);
        }

        /// <summary>Web Animations' <c>currentTime = time</c> and <c>pause()</c>: held there, showing its value.</summary>
        internal void SeekPaused(double time)
        {
            _playing = false;
            _startTime = null;
            CancelFrame();
            _holdTime = time;
            Apply(time);
        }

        /// <summary>Web Animations' <c>updateTiming</c>: the value at the current time, with the timing as it is now.</summary>
        internal void Refresh() => Apply(CurrentTime);

        /// <summary>Web Animations' <c>setKeyframes</c>: the tracks made again from the owner's keyframes, at the current time.</summary>
        internal void SetKeyframes()
        {
            // A track of a property that's still there keeps its binding, and takes the new frames; the rest go or come.
            var old = _tracks.ToList();
            _tracks.Clear();
            var offsets = ComputedOffsets(_owner._keyframes);
            foreach (var element in _owner._elements)
            {
                foreach (var name in Properties())
                {
                    var property = AnimatedProperty.ForKeyframes(name);
                    var kept = old.FirstOrDefault(t => t.Is(element, property));
                    if (kept is not null)
                    {
                        old.Remove(kept);
                        kept.SetFrames(_owner._keyframes, offsets);
                        _tracks.Add(kept);
                    }
                    else
                    {
                        _tracks.Add(new Track(element, property, _owner._keyframes, offsets));
                    }
                }
            }

            foreach (var track in old)
            {
                track.Dispose();
            }

            Apply(CurrentTime);
        }

        private double CurrentTime => _playing && _startTime is { } start ? Now - start : _holdTime;

        internal void Cancel()
        {
            CancelFrame();
            _playing = false;
            foreach (var track in _tracks)
            {
                track.Dispose();
            }

            _tracks.Clear();
        }

        private void RequestFrame()
        {
            CancelFrame();
            _frame = AvonicConfig.RequestFrame(_owner._elements[0], OnFrame);
        }

        private void CancelFrame()
        {
            _frame?.Dispose();
            _frame = null;
        }

        private void OnFrame()
        {
            _frame = null;
            if (!_playing)
            {
                return;
            }

            // The pending play becomes ready at a frame, whose time is its start, less where it was held.
            _startTime ??= Now - _holdTime;
            var time = Now - _startTime.Value;
            var end = EndTime;
            if (time >= end)
            {
                _playing = false;
                _startTime = null;
                _holdTime = end;
                Apply(end);
                _owner.AnimationFinish();
                return;
            }

            Apply(time);
            RequestFrame();
        }

        private void Apply(double time)
        {
            var easingText = _owner.GetEasing();
            if (easingText != _easingText)
            {
                _easingText = easingText;
                _easing = CubicBezierEasing.ParseCss(easingText);
            }

            var progress = Progress(
                time, _owner.GetDelay(), _owner.GetDuration(), _owner.GetIterations(), _owner.GetDirection(), _owner.GetFill(), _easing);
            foreach (var track in _tracks)
            {
                track.Apply(progress);
            }
        }

        /// <summary>Web Animations' computed offsets: 0 and 1 at the ends, and the rest spaced evenly between known ones.</summary>
        private static double[] ComputedOffsets(IReadOnlyList<AnimationKeyframe> keyframes)
        {
            var offsets = keyframes.Select(k => k.Offset).ToArray();
            if (offsets.Length == 1)
            {
                offsets[0] ??= 1;
            }
            else if (offsets.Length > 1)
            {
                offsets[0] ??= 0;
                offsets[^1] ??= 1;
            }

            var result = new double[offsets.Length];
            var previous = 0;
            for (var i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] is not { } known)
                {
                    continue;
                }

                for (var j = previous + 1; j < i; j++)
                {
                    result[j] = result[previous] + (known - result[previous]) * (j - previous) / (i - previous);
                }

                result[i] = known;
                previous = i;
            }

            return result;
        }
    }

    /// <summary>One element's property: its keyframes, with an underlying value where the list has no end, and its binding.</summary>
    private sealed class Track : IDisposable
    {
        private readonly Visual _element;
        private readonly AnimatedProperty _property;
        private readonly List<(double Offset, object Value)> _frames = [];
        private readonly ValueSubject _values = new();
        private IDisposable? _binding;

        internal Track(Visual element, AnimatedProperty property, IReadOnlyList<AnimationKeyframe> keyframes, double[] offsets)
        {
            _element = element;
            _property = property;
            SetFrames(keyframes, offsets);
        }

        internal bool Is(Visual element, AnimatedProperty property) => ReferenceEquals(element, _element) && ReferenceEquals(property, _property);

        /// <summary>The frames of its property in <paramref name="keyframes"/>, with the underlying value at an end they leave open.</summary>
        internal void SetFrames(IReadOnlyList<AnimationKeyframe> keyframes, double[] offsets)
        {
            _frames.Clear();
            var property = _property;
            var element = _element;
            for (var i = 0; i < keyframes.Count; i++)
            {
                if (keyframes[i].TryGetValue(property.Name, out var value))
                {
                    _frames.Add((offsets[i], property.Parse(value)));
                }
            }

            if (_frames[0].Offset != 0)
            {
                _frames.Insert(0, (0, property.Underlying(element)));
            }

            if (_frames[^1].Offset != 1)
            {
                _frames.Add((1, property.Underlying(element)));
            }
        }

        internal void Apply(double? progress)
        {
            // No value: the binding goes, as an animation-priority binding that yields UnsetValue gives the property's default,
            // not the value under it. It's bound again when there's a value.
            if (progress is not { } p)
            {
                Dispose();
                return;
            }

            // Web Animations' interval: the first two frames before 0, the last two from 1, else the last that starts at or before p.
            int start;
            if (p < 0)
            {
                start = 0;
            }
            else if (p >= 1)
            {
                start = _frames.Count - 2;
            }
            else
            {
                start = 0;
                for (var i = 0; i < _frames.Count - 1; i++)
                {
                    if (_frames[i].Offset <= p)
                    {
                        start = i;
                    }
                }
            }

            var (fromOffset, from) = _frames[start];
            var (toOffset, to) = _frames[start + 1];
            var local = toOffset == fromOffset ? 0 : (p - fromOffset) / (toOffset - fromOffset);
            _values.OnNext(_property.Interpolate(from, to, local));
            _binding ??= _property.Bind(_element, _values);
        }

        public void Dispose()
        {
            _binding?.Dispose();
            _binding = null;
        }
    }
}
