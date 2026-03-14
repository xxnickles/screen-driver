using ScreenDriver.Protocol;

namespace ScreenDriver;

/// <summary>
/// High-level API for the UsbMonitor 3.5" Revision A screen.
/// </summary>
public sealed class ScreenDevice : IDisposable
{
    public const int Width = 320;
    public const int Height = 480;

    // Chunk size for image data: width * 8 bytes (4 rows of pixels at 2 bytes/pixel)
    private const int ImageChunkSize = Width * 8;

    private readonly ScreenConnection _connection;

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

        _connection.Write(ScreenCommand.BuildHello());
        var sizeId = _connection.ReadByte();

        return sizeId;
    }

    public void Reset() => _connection.Write(ScreenCommand.BuildReset());

    public void Clear() => _connection.Write(ScreenCommand.BuildClear());

    public void ScreenOn() => _connection.Write(ScreenCommand.BuildScreenOn());

    public void ScreenOff() => _connection.Write(ScreenCommand.BuildScreenOff());

    /// <summary>
    /// Sets brightness. 0 = maximum brightness, 255 = completely dark.
    /// </summary>
    public void SetBrightness(byte level) => _connection.Write(ScreenCommand.BuildSetBrightness(level));

    /// <summary>
    /// Sets orientation: 0=portrait, 1=landscape, 2=reverse portrait, 3=reverse landscape.
    /// </summary>
    public void SetOrientation(int orientation) => _connection.Write(ScreenCommand.BuildSetOrientation(orientation));

    /// <summary>
    /// Sends RGB565 pixel data to a rectangular region of the screen.
    /// </summary>
    public void DisplayBitmap(int x, int y, int endX, int endY, byte[] rgb565Data)
    {
        _connection.Write(ScreenCommand.BuildDisplayBitmap(x, y, endX, endY));
        _connection.WriteChunked(rgb565Data, ImageChunkSize);
    }

    /// <summary>
    /// Fills the entire screen with a single color.
    /// </summary>
    public void FillScreen(byte r, byte g, byte b)
    {
        var data = Rgb565Encoder.SolidColor(Width, Height, r, g, b);
        DisplayBitmap(0, 0, Width - 1, Height - 1, data);
    }

    public void Dispose() => _connection.Dispose();
}
