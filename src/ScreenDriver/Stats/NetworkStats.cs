namespace ScreenDriver.Stats;

/// <summary>
/// Reads network transfer speeds from /proc/net/dev using delta-based byte comparison.
/// Detects the active interface via /proc/net/route (default gateway).
/// </summary>
public static class NetworkStats
{
    private static long _prevRxBytes;
    private static long _prevTxBytes;
    private static DateTime _prevTime;
    private static (double Down, double Up) _lastResult;
    private static DateTime _lastResultTime;
    private static string? _activeInterface;
    private static bool _probed;

    /// <summary>
    /// Returns current network speeds as (downloadBytesPerSec, uploadBytesPerSec).
    /// First call returns (0, 0) to establish a baseline.
    /// Results are cached for 100ms so multiple meters can read the same sample.
    /// </summary>
    public static (double DownBytesPerSec, double UpBytesPerSec) GetSpeed()
    {
        var now = DateTime.UtcNow;

        // Return cached result if called again within 100ms (multiple meters sharing one sample)
        if (_lastResultTime != default && (now - _lastResultTime).TotalMilliseconds < 100)
            return _lastResult;

        var iface = GetActiveInterface();
        if (iface is null) return (0, 0);

        var (rxBytes, txBytes) = ReadInterfaceBytes(iface);

        if (_prevTime == default)
        {
            _prevRxBytes = rxBytes;
            _prevTxBytes = txBytes;
            _prevTime = now;
            _lastResultTime = now;
            return (0, 0);
        }

        var elapsed = (now - _prevTime).TotalSeconds;
        if (elapsed <= 0) return _lastResult;

        var downSpeed = Math.Max((rxBytes - _prevRxBytes) / elapsed, 0);
        var upSpeed = Math.Max((txBytes - _prevTxBytes) / elapsed, 0);

        _prevRxBytes = rxBytes;
        _prevTxBytes = txBytes;
        _prevTime = now;
        _lastResult = (downSpeed, upSpeed);
        _lastResultTime = now;

        return _lastResult;
    }

    private static (long rxBytes, long txBytes) ReadInterfaceBytes(string iface)
    {
        foreach (var line in File.ReadLines("/proc/net/dev"))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(iface + ":")) continue;

            var parts = trimmed[(iface.Length + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // Column 0 = RX bytes, Column 8 = TX bytes
            var rxBytes = long.Parse(parts[0]);
            var txBytes = long.Parse(parts[8]);
            return (rxBytes, txBytes);
        }

        return (0, 0);
    }

    private static string? GetActiveInterface()
    {
        if (_probed) return _activeInterface;
        _probed = true;

        try
        {
            foreach (var line in File.ReadLines("/proc/net/route"))
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                // Default route has destination 00000000
                if (parts[1] == "00000000")
                {
                    _activeInterface = parts[0];
                    return _activeInterface;
                }
            }
        }
        catch
        {
            // Best effort
        }

        return null;
    }

    /// <summary>
    /// Formats bytes per second into a human-readable string.
    /// </summary>
    public static string FormatSpeed(double bytesPerSec)
    {
        return bytesPerSec switch
        {
            >= 1_000_000_000 => $"{bytesPerSec / 1_000_000_000:F1} GB/s",
            >= 1_000_000 => $"{bytesPerSec / 1_000_000:F1} MB/s",
            >= 1_000 => $"{bytesPerSec / 1_000:F0} KB/s",
            _ => $"{bytesPerSec:F0} B/s"
        };
    }
}
