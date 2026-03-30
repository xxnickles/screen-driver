# ScreenDriver

A .NET 10 driver for the QinHeng Electronics UsbMonitor 3.5" (Revision A protocol) on Linux. Sends graphics to a 320x480 USB screen over serial, targeting IoT-style system monitoring displays.

## Features

- System monitoring widgets: CPU, GPU, memory, disk, network, clock, date, and background
- Two built-in themes with hot-swap support — switch themes without reconnecting the device
- Terminal UI (TUI) with a live scrolling event log and interactive command menu
  - Press `Esc` to switch from the live log to the command menu
  - Commands: screen on/off, orientation (portrait, landscape, reverse), theme switching
  - Press "Back to log" to return to the live view

## Hardware

- **Screen:** QinHeng Electronics UsbMonitor 3.5" (320x480, RGB565 LE)
- **Protocol:** Revision A — 6-byte command packets over serial at 115200 baud, RTS/CTS flow control
- **Device node:** `/dev/ttyACM0`

## Prerequisites

### Linux serial port access

The screen appears as a serial device (`/dev/ttyACM0`). By default, only `root` can access it. To avoid running with `sudo`, add your user to the `dialout` group:

```bash
sudo usermod -aG dialout $USER
```

Then **log out and back in** (or reboot) for the change to take effect. Verify with:

```bash
groups
# Should include "dialout" in the output
```

This grants your user access to all serial devices on the system (`/dev/ttyACM*`, `/dev/ttyUSB*`, etc.).

## Build and run

```bash
# Build
dotnet build

# Run (screen must be connected)
dotnet run --project src/ScreenDriver

# Run with a specific port
dotnet run --project src/ScreenDriver -- /dev/ttyACM1

# Publish self-contained single-file binary
dotnet publish src/ScreenDriver -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o ./publish

# Run the published binary
./publish/ScreenDriver
```

## Project Structure

```
src/ScreenDriver/
├── Controller/   — Orchestration (screen controller, widget scheduler, commands, events)
├── Device/       — Hardware layer (serial connection, device scanner, protocol encoding)
├── Rendering/    — SkiaSharp helpers (RGB565 frame conversion, backgrounds)
├── Themes/       — Theme definitions and registry
├── Tui/          — Terminal UI built with Spectre.Console (log panel, command panel)
├── Widgets/      — Domain widgets (CPU, GPU, memory, disk, network, clock, date, text)
└── Program.cs
```

## Documentation

- [`docs/revision-a-protocol.md`](docs/revision-a-protocol.md) — Full protocol reference (commands, byte layout, image transfer)
- [`docs/usb-screen-discovery.md`](docs/usb-screen-discovery.md) — How to identify the screen on Linux
- [`docs/bitwise-encoding-guide.md`](docs/bitwise-encoding-guide.md) — Visual guide to the bitwise encoding used in the protocol

## Acknowledgments

This project's protocol implementation is based on the reverse-engineering work done by [turing-smart-screen-python](https://github.com/mathoudebine/turing-smart-screen-python). Their project decoded the USB screen communication protocol that made this .NET driver possible.

## AI-assisted development

This project is developed with AI assistance using [Claude Code](https://claude.com/claude-code). Code, documentation, and architecture decisions are human-reviewed and directed.

## License

[MIT](LICENSE)
