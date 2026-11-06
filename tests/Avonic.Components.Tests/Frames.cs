using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avonic.Components.Tests;

/// <summary>Reading a captured frame.</summary>
internal static class Frames
{
    /// <summary>A pixel, in whichever order the frame holds them: Skia's are RGBA, which a grey wouldn't show.</summary>
    public static unsafe Color PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var pixel = (byte*)buffer.Address + y * buffer.RowBytes + x * 4;
        return buffer.Format == PixelFormat.Rgba8888
            ? Color.FromArgb(pixel[3], pixel[0], pixel[1], pixel[2])
            : Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }
}
