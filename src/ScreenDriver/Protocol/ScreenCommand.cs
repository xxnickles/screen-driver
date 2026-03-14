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
    /// Builds a SET_ORIENTATION packet.
    /// 0=portrait, 1=landscape, 2=reverse portrait, 3=reverse landscape.
    /// The protocol adds 100 to the value.
    /// </summary>
    public static byte[] BuildSetOrientation(int orientation) => Encode(orientation + 100, 0, 0, 0, SetOrientation);

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
