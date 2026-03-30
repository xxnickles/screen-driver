using ScreenDriver.Device.Protocol;
using ScreenDriver.Rendering;

namespace ScreenDriver.Device;

public enum ScreenOrientation
{
    Portrait = 0,
    ReversePortrait = 1,
    Landscape = 2,
    ReverseLandscape = 3
}

/// <summary>
/// High-level API for the UsbMonitor 3.5" Revision A screen.
/// </summary>
public sealed class ScreenDevice : IDisposable
{
    public const int NativeWidth = 320;
    public const int NativeHeight = 480;

    // Landscape dimensions — the fixed operating mode for this driver
    public const int ScreenWidth = NativeHeight;  // 480
    public const int ScreenHeight = NativeWidth;  // 320

    // Chunk size: always based on native portrait width
    private const int ImageChunkSize = NativeWidth * 8;

    private readonly ScreenConnection _connection;
    private ScreenOrientation _orientation = ScreenOrientation.Portrait;

    /// <summary>Effective width after orientation is applied.</summary>
    public int Width => IsLandscape ? NativeHeight : NativeWidth;

    /// <summary>Effective height after orientation is applied.</summary>
    public int Height => IsLandscape ? NativeWidth : NativeHeight;

    private bool IsLandscape => _orientation is ScreenOrientation.Landscape or ScreenOrientation.ReverseLandscape;

    public ScreenDevice(string portName)
    {
        _connection = new ScreenConnection(portName);
    }

    /// <summary>
    /// Creates a ScreenDevice by auto-detecting the USB screen via sysfs.
    /// Throws if the screen is not found.
    /// </summary>
    public static ScreenDevice AutoDetect()
    {
        var port = DeviceScanner.FindScreen()
            ?? throw new InvalidOperationException(
                "Screen not found. Is it connected? Check VID 1a86, PID 5722 with 'lsusb'.");
        return new ScreenDevice(port);
    }

    /// <summary>
    /// Opens the connection and performs the HELLO handshake.
    /// Returns the screen size identifier byte.
    /// </summary>
    public byte Initialize()
    {
        _connection.Open();

        _connection.Write(DeviceCommand.BuildHello());
        var sizeId = _connection.ReadByte();

        return sizeId;
    }

    public void Reset() => _connection.Write(DeviceCommand.BuildReset());

    public void Clear() => _connection.Write(DeviceCommand.BuildClear());

    public void ScreenOn() => _connection.Write(DeviceCommand.BuildScreenOn());

    public void ScreenOff() => _connection.Write(DeviceCommand.BuildScreenOff());

    /// <summary>
    /// Sets brightness. 0 = maximum brightness, 255 = completely dark.
    /// </summary>
    public void SetBrightness(byte level) => _connection.Write(DeviceCommand.BuildSetBrightness(level));

    /// <summary>
    /// Sets orientation. Sends the full 16-byte command with target dimensions.
    /// The firmware handles coordinate remapping — no software rotation needed.
    /// </summary>
    public void SetOrientation(ScreenOrientation orientation)
    {
        _orientation = orientation;
        _connection.Write(DeviceCommand.BuildSetOrientation((int)orientation, Width, Height));
    }

    /// <summary>
    /// Sends RGB565 pixel data to a rectangular region of the screen.
    /// </summary>
    public void DisplayBitmap(int x, int y, int endX, int endY, byte[] rgb565Data)
    {
        _connection.Write(DeviceCommand.BuildDisplayBitmap(x, y, endX, endY));
        _connection.WriteChunked(rgb565Data, ImageChunkSize);
    }

    /// <summary>
    /// Fills the entire screen with a single color.
    /// </summary>
    public void FillScreen(byte r, byte g, byte b)
    {
        var frame = Rgb565Frame.SolidColor(Width, Height, r, g, b);
        DisplayBitmap(0, 0, Width - 1, Height - 1, frame.Data);
    }

    public void Dispose() => _connection.Dispose();
}
