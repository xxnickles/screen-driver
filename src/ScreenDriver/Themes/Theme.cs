using ScreenDriver.Widgets;
using SkiaSharp;

namespace ScreenDriver.Themes;

/// <summary>
/// Pure data container holding a theme's background and font.
/// Loaded from a named folder under the templates directory, or created programmatically.
/// </summary>
public sealed class Theme
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".bmp"];
    private static readonly string[] FontExtensions = [".ttf", ".otf"];

    public ScreenBackground Background { get; }
    public SKTypeface Typeface { get; }

    private Theme(ScreenBackground background, SKTypeface typeface)
    {
        Background = background;
        Typeface = typeface;
    }

    /// <summary>
    /// Loads a theme from a named folder under the themes root directory.
    /// Discovers exactly one image file and at most one font file by extension.
    /// </summary>
    public static Theme Load(string themesRoot, string name, int width, int height)
    {
        var dir = Path.Combine(themesRoot, name);
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Theme directory not found: {dir}");

        var imagePath = DiscoverSingle(dir, ImageExtensions,
            $"No background image found in theme folder: {dir}",
            $"Multiple background images found in theme folder: {dir}. Expected exactly one.");

        var fontPaths = DiscoverAll(dir, FontExtensions);
        if (fontPaths.Length > 1)
            throw new InvalidOperationException(
                $"Multiple font files found in theme folder: {dir}. Expected at most one.");

        var background = ScreenBackground.FromImage(imagePath, width, height);
        var typeface = fontPaths.Length == 1 ? SKTypeface.FromFile(fontPaths[0]) : SKTypeface.Default;

        return new Theme(background, typeface);
    }

    /// <summary>
    /// Creates a theme with a solid-color background and the default font.
    /// </summary>
    public static Theme SolidColor(SKColor color, int width, int height)
    {
        var background = ScreenBackground.SolidColor(color, width, height);
        return new Theme(background, SKTypeface.Default);
    }

    private static string DiscoverSingle(string dir, string[] extensions, string noneMessage, string multipleMessage)
    {
        var files = DiscoverAll(dir, extensions);
        return files.Length switch
        {
            0 => throw new InvalidOperationException(noneMessage),
            1 => files[0],
            _ => throw new InvalidOperationException(multipleMessage)
        };
    }

    private static string[] DiscoverAll(string dir, string[] extensions)
    {
        return Directory.GetFiles(dir)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();
    }
}
