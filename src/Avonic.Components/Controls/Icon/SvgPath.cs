using System.Text;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// SVG path data to a <see cref="StreamGeometry"/>. It corrects what <see cref="StreamGeometry.Parse"/>
/// gets wrong for icons in Avalonia 12.1.4: its default fill rule, how it starts a smooth curve, and
/// two kinds of malformed data, which a custom SVG can contain.
/// </summary>
internal static class SvgPath
{
    /// <summary>Null if the data can't be parsed. A bad icon draws nothing, rather than bringing down the app.</summary>
    public static Geometry? Parse(string data, bool evenOdd)
    {
        var prepared = Prepare(data);
        if (prepared is null)
        {
            return null;
        }

        try
        {
            // F1 is nonzero, SVG's default. StreamGeometry's own default is even-odd.
            return StreamGeometry.Parse((evenOdd ? "F0 " : "F1 ") + prepared);
        }
        catch (Exception e) when (e is InvalidDataException or IndexOutOfRangeException or FormatException)
        {
            // What it throws on bad data, found by fuzzing. Anything else, such as no render platform
            // yet, isn't about the data, so it mustn't become an icon cached as blank.
            return null;
        }
    }

    /// <summary>The data, changed where Avalonia would misread it. Null if it can't start a path.</summary>
    internal static string? Prepare(string data)
    {
        // Path data is ASCII. Avalonia also reads Unicode spaces and digits, which would get past the
        // closepath check below. An error stops a browser, and here it means drawing nothing.
        if (!Ascii.IsValid(data))
        {
            return null;
        }

        StringBuilder? result = null;
        var previous = '\0';
        var seenCurve = false;
        var copied = 0;

        for (var i = 0; i < data.Length; i++)
        {
            var command = data[i];
            if (!char.IsAsciiLetter(command) || command is 'e' or 'E')
            {
                continue;
            }

            // Only a moveto can start a path. Avalonia draws from the origin instead.
            if (previous == '\0' && command is not ('M' or 'm'))
            {
                return null;
            }

            // After any command but a curve, SVG starts a smooth curve at the current point. Avalonia
            // uses the last control point, however long ago it was, which warps wifi-outline, infinite
            // and others. A zero-length curve first makes that control point the current point.
            // The two kinds of curve share that control point, so either can go stale for the other.
            var reset = command switch
            {
                's' or 'S' when seenCurve && previous is not ('c' or 'C' or 's' or 'S') => "c0 0 0 0 0 0",
                't' or 'T' when seenCurve && previous is not ('q' or 'Q' or 't' or 'T') => "q0 0 0 0",
                _ => null,
            };

            seenCurve |= command is 'c' or 'C' or 's' or 'S' or 'q' or 'Q' or 't' or 'T';

            if (reset is not null)
            {
                result ??= new StringBuilder(data.Length + 32);
                result.Append(data, copied, i - copied).Append(reset);
                copied = i;
            }

            previous = command;

            // Anything but a command straight after a closepath makes Avalonia loop forever. It's an
            // error, and a browser stops at an error, so the path ends here.
            if (command is 'z' or 'Z' && !CommandFollows(data, i + 1))
            {
                return (result ?? new StringBuilder()).Append(data, copied, i + 1 - copied).ToString();
            }
        }

        return result is null ? data : result.Append(data, copied, data.Length - copied).ToString();
    }

    private static bool CommandFollows(string data, int start)
    {
        var i = start;
        while (i < data.Length && (char.IsWhiteSpace(data[i]) || data[i] == ','))
        {
            i++;
        }

        return i == data.Length || (char.IsAsciiLetter(data[i]) && data[i] is not ('e' or 'E'));
    }
}
