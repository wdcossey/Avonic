using Avalonia.Input;

namespace Avonic.Components.Gestures;

/// <summary>
/// Keeps Avalonia's gesture recognisers, a <c>ScrollViewer</c>'s among them, off a pointer while a gesture on it is deciding, and
/// lets them back on if every gesture on it gives up without capturing. Avalonia's <c>ScrollGestureRecognizer</c> decides at 5 px
/// with no axis lock, before most gestures' thresholds, and would take the pointer from them.
/// </summary>
/// <remarks>
/// The flag is <see cref="Pointer.IsGestureRecognitionSkipped"/>, which every recogniser in the route checks. It's set in the
/// tunnel phase of the first move, so a recogniser inside the gesture's element misses the move too, and it stays set until the last
/// gesture holding it gives up, or the press ends. A recogniser that saw the press is still tracking it, so once the flag is cleared
/// the next move it sees can scroll. A flag that was already set when the first gesture held the pointer, by the app or by a gesture
/// that captured at the press, is left set.
/// </remarks>
internal static class RecognitionHold
{
    private sealed class Entry(bool wasSkipped)
    {
        public bool WasSkipped { get; } = wasSkipped;

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

    /// <summary>
    /// A gesture captured <paramref name="e"/>'s pointer: no recogniser gets it until the press ends. This matters for a capture at
    /// the press, which goes on to the recognisers around the element; a capture on a move has already ended every recogniser on the
    /// route, by letting the pointer go.
    /// </summary>
    public static void Captured(Gesture gesture, PointerEventArgs e)
    {
        Release(gesture, e.Pointer);
        e.PreventGestureRecognition();
    }

    /// <summary>A gesture stopped deciding.</summary>
    public static void Release(Gesture gesture, IPointer pointer)
    {
        if (pointer is not Pointer p || !s_entries.TryGetValue(p, out var entry))
        {
            return;
        }

        if (!entry.Holders.Remove(gesture) || entry.Holders.Count > 0)
        {
            return;
        }

        s_entries.Remove(p);
        if (!entry.WasSkipped)
        {
            p.IsGestureRecognitionSkipped = false;
        }
    }
}
