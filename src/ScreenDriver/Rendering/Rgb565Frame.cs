namespace ScreenDriver.Rendering;

/// <summary>
/// RGB565 Little-Endian pixel data for a known-size region.
/// RGB565 packs each pixel into 2 bytes: 5 bits red, 6 bits green, 5 bits blue.
/// </summary>
public record Rgb565Frame
{
    public byte[] Data { get; }

    private Rgb565Frame(byte[] data, int width, int height)
    {
        var expected = width * height * 2;
        if (data.Length != expected)
            throw new ArgumentException(
                $"RGB565 data length {data.Length} does not match expected {expected} ({width}x{height}x2).");

        Data = data;
    }

    /// <summary>
    /// Creates a solid-color frame in RGB565 LE format.
    /// </summary>
    public static Rgb565Frame SolidColor(int width, int height, byte r, byte g, byte b)
    {
        var (low, high) = FromRgb(r, g, b);
        var buffer = new byte[width * height * 2];

        for (var i = 0; i < buffer.Length; i += 2)
        {
            buffer[i] = low;
            buffer[i + 1] = high;
        }

        return new Rgb565Frame(buffer, width, height);
    }

    /// <summary>
    /// Converts BGRA8888 pixel data (SkiaSharp default) to an Rgb565Frame.
    /// </summary>
    public static Rgb565Frame FromBgra8888(ReadOnlySpan<byte> bgraPixels, int width, int height)
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

        return new Rgb565Frame(buffer, width, height);
    }

    private static (byte low, byte high) FromRgb(byte r, byte g, byte b)
    {
        ushort pixel = (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
        return ((byte)(pixel & 0xFF), (byte)(pixel >> 8));
    }
}
