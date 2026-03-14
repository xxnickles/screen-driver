namespace ScreenDriver.Protocol;

/// <summary>
/// Converts pixel data to RGB565 Little-Endian format for the Revision A screen.
/// RGB565 packs each pixel into 2 bytes: 5 bits red, 6 bits green, 5 bits blue.
/// </summary>
public static class Rgb565Encoder
{
    /// <summary>
    /// Converts a single RGB pixel to RGB565 Little-Endian (2 bytes).
    /// </summary>
    public static (byte low, byte high) FromRgb(byte r, byte g, byte b)
    {
        ushort pixel = (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
        return ((byte)(pixel & 0xFF), (byte)(pixel >> 8));
    }

    /// <summary>
    /// Creates a solid-color image buffer in RGB565 LE format.
    /// </summary>
    public static byte[] SolidColor(int width, int height, byte r, byte g, byte b)
    {
        var (low, high) = FromRgb(r, g, b);
        var buffer = new byte[width * height * 2];

        for (var i = 0; i < buffer.Length; i += 2)
        {
            buffer[i] = low;
            buffer[i + 1] = high;
        }

        return buffer;
    }

    /// <summary>
    /// Converts an SKBitmap's pixel data to RGB565 LE.
    /// Expects the bitmap in BGRA8888 format (SkiaSharp default).
    /// </summary>
    public static byte[] FromBgra8888(ReadOnlySpan<byte> bgraPixels, int width, int height)
    {
        var buffer = new byte[width * height * 2];
        var outIdx = 0;

        for (var i = 0; i < bgraPixels.Length; i += 4)
        {
            var b = bgraPixels[i];
            var g = bgraPixels[i + 1];
            var r = bgraPixels[i + 2];
            // bgraPixels[i + 3] is alpha — ignored

            var (low, high) = FromRgb(r, g, b);
            buffer[outIdx++] = low;
            buffer[outIdx++] = high;
        }

        return buffer;
    }
}
