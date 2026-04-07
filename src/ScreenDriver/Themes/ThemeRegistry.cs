using ScreenDriver.Device;

namespace ScreenDriver.Themes;

public class ThemeRegistry
{
    private Dictionary<string, (ScreenLayoutMode Layout, Func<Theme> Factory)> AvailableThemes { get; }
    private Dictionary<ScreenLayoutMode, string> PreferredThemes { get; }

    public ThemeRegistry(
        Dictionary<string, (ScreenLayoutMode Layout, Func<Theme> Factory)> availableThemes,
        Dictionary<ScreenLayoutMode, string> preferredThemes)
    {
        foreach (var (layout, name) in preferredThemes)
        {
            if (!availableThemes.TryGetValue(name, out var entry))
                throw new ArgumentException($"Preferred theme '{name}' is not present in the available themes.");
            if (entry.Layout != layout)
                throw new ArgumentException($"Preferred theme '{name}' is registered for {entry.Layout}, not {layout}.");
        }

        AvailableThemes = availableThemes;
        PreferredThemes = preferredThemes;
    }

    public string GetPreferredTheme(ScreenLayoutMode layout) =>
        PreferredThemes.TryGetValue(layout, out var name)
            ? name
            : GetThemesForLayout(layout).First();

    public Theme Build(string name) =>
        AvailableThemes.TryGetValue(name, out var entry)
            ? entry.Factory()
            : throw new ArgumentException($"Theme '{name}' is not present in the available themes.");

    public string[] GetAvailableThemes() => AvailableThemes.Keys.ToArray();

    public IEnumerable<string> GetThemesForLayout(ScreenLayoutMode layout) =>
        AvailableThemes.Where(kv => kv.Value.Layout == layout).Select(kv => kv.Key);
}
