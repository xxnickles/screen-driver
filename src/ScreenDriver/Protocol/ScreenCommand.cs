namespace ScreenDriver.Protocol;

/// <summary>
/// Revision A command codes and packet encoding for the UsbMonitor 3.5" screen.
/// Each command is a 6-byte packet: 5 bytes of packed coordinates + 1 byte command code.
/// </summary>
public static class ScreenCommand
{
    // Command codes (byte 5 of every packet)
    public const byte Hello = 0x45;          // 69 - Handshake / discovery
    public const byte Reset = 0x65;          // 101 - Reset display
    public const byte Clear = 0x66;          // 102 - Clear screen to white
    public const byte ScreenOff = 0x6C;      // 108 - Backlight off
    public const byte ScreenOn = 0x6D;       // 109 - Backlight on
    public const byte SetBrightness = 0x6E;  // 110 - Set brightness (0=max, 255=dark)
    public const byte SetOrientation = 0x79; // 121 - Set orientation (value + 100)
    public const byte DisplayBitmap = 0xC5;  // 197 - Begin bitmap transfer

    /// <summary>
    /// Encodes a 6-byte command packet. The four coordinate values (x, y, ex, ey)
    /// are bit-packed into 5 bytes, followed by the command code.
    /// </summary>
    public static byte[] Encode(int x, int y, int ex, int ey, byte command)
    {
        return
        [
            (byte)(x >> 2),
            (byte)(((x & 3) << 6) | (y >> 4)),
            (byte)(((y & 15) << 4) | (ex >> 6)),
            (byte)(((ex & 63) << 2) | (ey >> 8)),
            (byte)(ey & 255),
            command
        ];
    }

    /// <summary>
    /// Builds the 6-byte HELLO handshake packet (all HELLO bytes).
    /// </summary>
    public static byte[] BuildHello() => [Hello, Hello, Hello, Hello, Hello, Hello];

    /// <summary>
    /// Builds a SET_BRIGHTNESS packet. Level 0 = brightest, 255 = darkest.
    /// </summary>
    public static byte[] BuildSetBrightness(byte level) => Encode(level, 0, 0, 0, SetBrightness);

    /// <summary>
    /// Builds a 16-byte SET_ORIENTATION packet.
    /// Bytes 0-5: standard command (coords zeroed + command code).
    /// Byte 6: orientation + 100.
    /// Bytes 7-10: target width and height (big-endian).
    /// </summary>
    public static byte[] BuildSetOrientation(int orientation, int width, int height)
    {
        var buf = new byte[16];
        // Bytes 0-4: packed coordinates (all zero)
        // Byte 5: command code
        buf[5] = SetOrientation;
        // Byte 6: orientation value
        buf[6] = (byte)(orientation + 100);
        // Bytes 7-8: width (big-endian)
        buf[7] = (byte)(width >> 8);
        buf[8] = (byte)(width & 0xFF);
        // Bytes 9-10: height (big-endian)
        buf[9] = (byte)(height >> 8);
        buf[10] = (byte)(height & 0xFF);
        return buf;
    }

    /// <summary>
    /// Builds a DISPLAY_BITMAP command defining the target rectangle.
    /// After sending this, write the raw RGB565 pixel data.
    /// </summary>
    public static byte[] BuildDisplayBitmap(int x, int y, int endX, int endY) => Encode(x, y, endX, endY, DisplayBitmap);

    public static byte[] BuildReset() => Encode(0, 0, 0, 0, Reset);

    public static byte[] BuildClear() => Encode(0, 0, 0, 0, Clear);

    public static byte[] BuildScreenOn() => Encode(0, 0, 0, 0, ScreenOn);

    public static byte[] BuildScreenOff() => Encode(0, 0, 0, 0, ScreenOff);
}
