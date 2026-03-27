using SkiaSharp;

namespace ScreenDriver.Widgets;

public record TextWidget : Widget
{
    private readonly string _text;
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    public TextWidget(
        WidgetZone zone,
        ScreenBackground background,
        SKTypeface typeface,
        float fontSize,
        SKColor color,
        string text) : base(zone, Timeout.InfiniteTimeSpan)
    {
        _text = text;
        _typeface = typeface;
        _fontSize = fontSize;
        _color = color;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, _text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}
