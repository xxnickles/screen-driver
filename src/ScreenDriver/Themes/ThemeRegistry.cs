using ScreenDriver.Device;

namespace ScreenDriver.Themes;

public class ThemeRegistry
{
    public string DefaultThemeName { get; }
    private Dictionary<string, (ScreenLayoutMode Layout, Func<Theme> Factory)> AvailableThemes { get; }

    public ThemeRegistry(string defaultThemeName, Dictionary<string, (ScreenLayoutMode Layout, Func<Theme> Factory)> availableThemes)
    {
        if (!availableThemes.ContainsKey(defaultThemeName))
            throw new ArgumentException(
                $"Theme '{defaultThemeName}' is not present in the available themes.");

        AvailableThemes = availableThemes;
        DefaultThemeName = defaultThemeName;
    }

    public Theme BuildDefault() => AvailableThemes[DefaultThemeName].Factory();

    public Theme Build(string name) =>
        AvailableThemes.TryGetValue(name, out var entry)
            ? entry.Factory()
            : throw new ArgumentException($"Theme '{name}' is not present in the available themes.");

    public string[] GetAvailableThemes() => AvailableThemes.Keys.ToArray();

    public IEnumerable<string> GetThemesForLayout(ScreenLayoutMode layout) =>
        AvailableThemes.Where(kv => kv.Value.Layout == layout).Select(kv => kv.Key);
}
