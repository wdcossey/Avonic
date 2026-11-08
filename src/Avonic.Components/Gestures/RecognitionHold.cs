using Avalonia.Input;

namespace Avonic.Components.Gestures;

/// <summary>
/// Keeps Avalonia's gesture recognisers, a <c>ScrollViewer</c>'s among them, off a pointer while a gesture on it is deciding, and
/// lets them back on if every gesture on it gives up without capturing. Avalonia's <c>ScrollGestureRecognizer</c> decides at 5 px
/// with no axis lock, before most gestures' thresholds, and would take the pointer from them.
/// </summary>
/// <remarks>
/// The flag is <see cref="Pointer.IsGestureRecognitionSkipped"/>, which every recogniser in the route checks. It's set in the
/// tunnel phase of each move, so a recogniser inside the gesture's element misses the move too. A recogniser that saw the press is
/// still tracking it, so once the flag is cleared the next move it sees can scroll. A gesture that captures keeps the flag, as
/// <see cref="PointerEventArgs.PreventGestureRecognition"/> sets it until the pointer's capture ends.
/// </remarks>
internal static class RecognitionHold
{
    private sealed class Entry(bool wasSkipped)
    {
        public bool WasSkipped { get; } = wasSkipped;

        public bool Captured { get; set; }

        public HashSet<Gesture> Holders { get; } = new();
    }

    private static readonly Dictionary<Pointer, Entry> s_entries = new();

    /// <summary>A gesture on <paramref name="pointer"/> hasn't decided yet.</summary>
    public static void Hold(Gesture gesture, IPointer pointer)
    {
        if (pointer is not Pointer p)
        {
            return;
        }

        if (!s_entries.TryGetValue(p, out var entry))
        {
            entry = new Entry(p.IsGestureRecognitionSkipped);
            s_entries[p] = entry;
        }

        entry.Holders.Add(gesture);
        p.IsGestureRecognitionSkipped = true;
    }

    /// <summary>A gesture captured <paramref name="e"/>'s pointer: no recogniser gets it until the press ends.</summary>
    public static void Captured(Gesture gesture, PointerEventArgs e)
    {
        e.PreventGestureRecognition();
        if (e.Pointer is Pointer p && s_entries.TryGetValue(p, out var entry))
        {
            entry.Captured = true;
            Remove(p, entry, gesture);
        }
    }

    /// <summary>A gesture stopped deciding, whether it captured or not.</summary>
    public static void Release(Gesture gesture, IPointer pointer)
    {
        if (pointer is Pointer p && s_entries.TryGetValue(p, out var entry))
        {
            Remove(p, entry, gesture);
        }
    }

    private static void Remove(Pointer pointer, Entry entry, Gesture gesture)
    {
        if (!entry.Holders.Remove(gesture) || entry.Holders.Count > 0)
        {
            return;
        }

        s_entries.Remove(pointer);
        if (!entry.Captured && !entry.WasSkipped)
        {
            pointer.IsGestureRecognitionSkipped = false;
        }
    }
}
