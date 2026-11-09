using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avonic.Components.Tests;

/// <summary>Reading, comparing and saving a captured frame.</summary>
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

    /// <summary>How many rows of two frames of the same size differ, compared row by row, as a frame's rows may be padded.</summary>
    public static unsafe int DifferingRows(WriteableBitmap expected, WriteableBitmap actual)
    {
        using var a = expected.Lock();
        using var b = actual.Lock();
        Assert.Equal(a.Size, b.Size);
        var differing = 0;
        for (var y = 0; y < a.Size.Height; y++)
        {
            var rowA = new ReadOnlySpan<byte>((byte*)a.Address + y * a.RowBytes, a.Size.Width * 4);
            var rowB = new ReadOnlySpan<byte>((byte*)b.Address + y * b.RowBytes, b.Size.Width * 4);
            if (!rowA.SequenceEqual(rowB))
            {
                differing++;
            }
        }

        return differing;
    }

    /// <summary>Every byte of two frames, row by row.</summary>
    public static void AssertSameFrame(WriteableBitmap expected, WriteableBitmap actual)
    {
        var differing = DifferingRows(expected, actual);
        Assert.True(differing == 0, $"{differing} rows differ");
    }

    /// <summary>Saves the frame as <paramref name="name"/> in <c>AVONIC_CAPTURE_DIR</c>, for the compare step, if it's set.</summary>
    public static void Save(WriteableBitmap frame, string name)
    {
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, name), PngBitmapEncoderOptions.Default);
        }
    }
}
