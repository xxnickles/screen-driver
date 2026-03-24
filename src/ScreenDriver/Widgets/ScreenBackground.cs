using SkiaSharp;

namespace ScreenDriver.Widgets;

/// <summary>
/// Owns the full-screen background bitmap. Widgets crop their zone from it
/// at construction time to composite their content on top of the background.
/// </summary>
public class ScreenBackground
{
    private readonly SKBitmap _bitmap;

    private ScreenBackground(SKBitmap bitmap)
    {
        _bitmap = bitmap;
    }

    /// <summary>
    /// The full-screen background bitmap in BGRA8888 format.
    /// </summary>
    public SKBitmap Bitmap => _bitmap;

    /// <summary>
    /// Loads a PNG file as the background.
    /// </summary>
    public static ScreenBackground FromImage(string pngPath, int width, int height)
    {
        using var decoded = SKBitmap.Decode(pngPath)
            ?? throw new FileNotFoundException($"Background image not found: {pngPath}");

        if (decoded.Width != width || decoded.Height != height)
            throw new ArgumentException(
                $"Background image is {decoded.Width}x{decoded.Height}, expected {width}x{height}.");

        var bitmap = decoded.Copy(SKColorType.Bgra8888);
        return new ScreenBackground(bitmap);
    }

    /// <summary>
    /// Creates a solid-color background.
    /// </summary>
    public static ScreenBackground SolidColor(SKColor color, int width, int height)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        return new ScreenBackground(bitmap);
    }

    /// <summary>
    /// Returns a cropped copy of the background for the given widget zone.
    /// The caller owns the returned bitmap.
    /// </summary>
    public SKBitmap CropZone(WidgetZone zone)
    {
        var cropped = new SKBitmap(zone.Width, zone.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(cropped);
        canvas.DrawBitmap(_bitmap,
            SKRect.Create(zone.X, zone.Y, zone.Width, zone.Height),
            SKRect.Create(0, 0, zone.Width, zone.Height));
        return cropped;
    }
}
