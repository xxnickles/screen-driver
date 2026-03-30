using System.IO.Ports;

namespace ScreenDriver.Device;

/// <summary>
/// Manages the serial port connection to the USB screen.
/// Handles opening, closing, and raw byte-level read/write.
/// </summary>
public sealed class ScreenConnection : IDisposable
{
    private readonly SerialPort _port;

    public ScreenConnection(string portName)
    {
        _port = new SerialPort(portName)
        {
            BaudRate = 115200,
            Handshake = Handshake.RequestToSend, // RTS/CTS flow control
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
    }

    public void Open() => _port.Open();

    public void Close()
    {
        if (_port.IsOpen)
            _port.Close();
    }

    public void Write(byte[] data) => _port.Write(data, 0, data.Length);

    /// <summary>
    /// Writes data in chunks to avoid overwhelming the screen's receive buffer.
    /// </summary>
    public void WriteChunked(byte[] data, int chunkSize)
    {
        for (var offset = 0; offset < data.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, data.Length - offset);
            _port.Write(data, offset, length);
        }
    }

    public byte ReadByte()
    {
        var b = _port.ReadByte();
        return b >= 0 ? (byte)b : throw new TimeoutException("No response from screen");
    }

    public void Dispose() => _port.Dispose();
}
