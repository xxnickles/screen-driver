using SkiaSharp;

namespace ScreenDriver.Widgets;

/// <summary>
/// Full-screen background image. Loads a PNG and converts to RGB565 at construction.
/// Renders once (infinite interval) — sent on connect/reconnect by ScreenController.
/// </summary>
public record BackgroundWidget : Widget
{
    private readonly Rgb565Frame _frame;

    public BackgroundWidget(string pngPath, int screenWidth, int screenHeight)
        : base(new WidgetZone(0, 0, screenWidth, screenHeight), Timeout.InfiniteTimeSpan)
    {
        using var bitmap = SKBitmap.Decode(pngPath)
            ?? throw new FileNotFoundException($"Background image not found: {pngPath}");

        if (bitmap.Width != screenWidth || bitmap.Height != screenHeight)
            throw new ArgumentException(
                $"Background image is {bitmap.Width}x{bitmap.Height}, expected {screenWidth}x{screenHeight}.");

        using var converted = bitmap.Copy(SKColorType.Bgra8888);
        _frame = Rgb565Frame.FromBgra8888(converted.GetPixelSpan(), screenWidth, screenHeight);
    }

    public override Rgb565Frame Render() => _frame;
}
