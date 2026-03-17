using ScreenDriver.Widgets;

namespace ScreenDriver.Commands;

/// <summary>
/// Base type for all commands that can be sent to the screen.
/// </summary>
public abstract record ScreenCommand;

/// <summary>
/// Sends rendered pixel data to a rectangular screen region.
/// </summary>
public record DisplayBitmapCommand(WidgetZone Zone, Rgb565Frame Frame) : ScreenCommand;

/// <summary>
/// Changes the screen orientation and coordinate system.
/// </summary>
public record SetOrientationCommand(ScreenOrientation Orientation) : ScreenCommand;

/// <summary>
/// Sets the backlight brightness. 0 = max, 255 = dark.
/// </summary>
public record SetBrightnessCommand(byte Level) : ScreenCommand;

/// <summary>
/// Fills the entire screen with a solid color.
/// </summary>
public record FillScreenCommand(byte R, byte G, byte B) : ScreenCommand;
