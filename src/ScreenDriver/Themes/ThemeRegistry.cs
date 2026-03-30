namespace ScreenDriver.Themes;

public class ThemeRegistry
{
    public string DefaultThemeName { get; }
    private Dictionary<string, Func<Theme>> AvailableThemes { get; }
    
    public ThemeRegistry(string defaultThemeName, Dictionary<string, Func<Theme>> availableThemes)
    {
        if (!availableThemes.ContainsKey(defaultThemeName))
            throw new ArgumentException(
                $"Theme '{defaultThemeName}' is not present in the available themes.");
        
        AvailableThemes = availableThemes;
        DefaultThemeName = defaultThemeName;
    }

    public Theme BuildDefault() => AvailableThemes[DefaultThemeName]();
    
    public Theme Build(string name) =>
        AvailableThemes.TryGetValue(name, out var factory)
            ? factory()
            : throw new ArgumentException($"Theme '{name}' is not present in the available themes.");
    
    public string[] GetAvailableThemes() => AvailableThemes.Keys.ToArray();
}
