using ScreenDriver.Controller.Events;
using ScreenDriver.Device;
using ScreenDriver.Rendering;
using ScreenDriver.Widgets;

namespace ScreenDriver.Controller.Commands;

/// <summary>
/// Base type for all commands that can be sent to the screen.
/// </summary>
public abstract record ScreenCommand
{
    public abstract void Execute(ScreenDevice device);
};

public abstract record NotifiableCommand : ScreenCommand
{
    public abstract Event Event { get; }
}

/// <summary>
/// Sends rendered pixel data to a rectangular screen region.
/// </summary>
public record DisplayBitmapCommand(WidgetZone Zone, Rgb565Frame Frame) : ScreenCommand
{
    public override void Execute(ScreenDevice device)
    {
        device.DisplayBitmap(
            Zone.X, Zone.Y,
            Zone.EndX, Zone.EndY,
            Frame.Data);
    }
}

/// <summary>
/// Changes the screen orientation and coordinate system.
/// </summary>
public record SetOrientationCommand(ScreenOrientation Orientation) : NotifiableCommand
{
    public override void Execute(ScreenDevice device)
    {
        device.SetOrientation(Orientation);
    }

    public override Event Event { get; } = new Info(nameof(ScreenCommandQueue),
        $"Dispatched command orientation with value: {Orientation}");
}

/// <summary>
/// Sets the backlight brightness. 0 = max, 255 = dark.
/// </summary>
public record SetBrightnessCommand(byte Level) : NotifiableCommand
{
    public override void Execute(ScreenDevice device)
    {
        device.SetBrightness(Level);
    }

    public override Event Event { get; } = new Info(nameof(ScreenCommandQueue),
        $"Dispatched command brightness with value: {Level}");
}

/// <summary>
/// Fills the entire screen with a solid color.
/// </summary>
public record FillScreenCommand(byte R, byte G, byte B) : ScreenCommand
{
    public override void Execute(ScreenDevice device)
    {
        device.FillScreen(R, G, B);
    }
}

/// <summary>
/// Clears the entire screen.
/// </summary>
public record ClearScreenCommand() : NotifiableCommand
{
    public override void Execute(ScreenDevice device)
    {
        device.Clear();
    }
    
    public override Event Event { get; } = new Info(nameof(ScreenCommandQueue),
        $"Dispatched command clear screen");
}


/// <summary>
/// Turns the screen on.
/// </summary>
public record TurnOnScreenCommand : NotifiableCommand
{
    public override void Execute(ScreenDevice device)
    {
        device.ScreenOn();
    }
    
    public override Event Event { get; } = new Info(nameof(ScreenCommandQueue),
        $"Dispatched command turn on screen");
}


/// <summary>
/// Turns the screen off.
/// </summary>
public record TurnOffScreenCommand : NotifiableCommand
{
    public override void Execute(ScreenDevice device)
    {
        device.ScreenOff();
    }
    
    public override Event Event { get; } = new Info(nameof(ScreenCommandQueue),
        "Dispatched command turn off screen");
}
