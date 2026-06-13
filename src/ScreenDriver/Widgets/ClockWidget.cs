using ScreenDriver.Rendering;
using SkiaSharp;

namespace ScreenDriver.Widgets;

public record ClockWidget : ScheduledWidget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    public ClockWidget(
        WidgetZone zone,
        ScreenBackground background,
        SKTypeface typeface,
        float fontSize,
        SKColor color) : base(zone, TimeSpan.FromMinutes(1))
    {
        _typeface = typeface;
        _fontSize = fontSize;
        _color = color;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var text = DateTime.Now.ToString("HH:mm");
        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}
