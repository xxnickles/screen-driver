namespace ScreenDriver.Tui;

internal static class Constants
{
    internal const string ScreenOrientationCommand = "Orientation";
    internal const string ScreenBrightnessCommand = "Brightness";
    internal const string ScreenThemeCommand = "Theme";
    internal const string BackCommand = "Back to log";

    //Brightness
    internal static readonly Dictionary<string, byte> BrightnessOptions = new()
    {
        {"Off", 255},
        {"Low", 240},
        {"Dim", 128},
        {"High", 0},
    };
    
}