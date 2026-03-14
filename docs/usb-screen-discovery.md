# USB Screen Discovery Guide

How to identify and understand a USB screen device on Linux — explained from scratch.

## Step 1: Find the device with `lsusb`

```bash
lsusb
```

`lsusb` lists every USB device currently connected to your computer. Think of it as "show me everything plugged into my USB ports."

**Our output:**

```
Bus 005 Device 004: ID 1a86:5722 QinHeng Electronics UsbMonitor
```

Let's break this down piece by piece:

| Part | Meaning |
|------|---------|
| `Bus 005` | Which physical USB controller on your motherboard this device is connected through. Your PC has multiple USB buses (think of them as separate highways). |
| `Device 004` | A number assigned to this device on that bus. It's like a seat number — it can change if you unplug and replug. |
| `ID 1a86:5722` | **This is the important part.** It's the device's identity card, split into two halves: |
| `1a86` | **Vendor ID (VID)** — identifies the manufacturer. `1a86` = QinHeng Electronics (a Chinese chip maker). Every USB chip maker gets a unique VID from the USB-IF organization. |
| `5722` | **Product ID (PID)** — identifies the specific product from that vendor. `5722` = their "UsbMonitor" chip. |
| `QinHeng Electronics UsbMonitor` | A human-readable name looked up from a database of known VID/PID pairs. |

**Why VID/PID matters:** This is how software knows what it's talking to. The Python driver uses `1a86:5722` to detect that a compatible screen is connected.

## Step 2: Find the serial device node

```bash
ls /dev/ttyACM* /dev/ttyUSB*
```

When a USB device speaks a serial protocol (like our screen does), Linux creates a virtual serial port for it. These show up as files under `/dev/`:

- `/dev/ttyUSB0`, `/dev/ttyUSB1`, ... — for devices using a USB-to-serial converter chip
- `/dev/ttyACM0`, `/dev/ttyACM1`, ... — for devices that present themselves as a "CDC ACM" device (a USB standard for serial communication)

**Our output:**

```
/dev/ttyACM0
```

This means our screen registered as a CDC ACM serial device. This is the "file" our .NET code will open to talk to the screen — you write bytes to it, and the screen receives them.

**ACM stands for** "Abstract Control Model" — it's part of the USB CDC (Communications Device Class) specification. You don't need to care about the details; just know it means "this USB device acts like a serial port."

## Step 3: Get detailed device info with `udevadm`

```bash
udevadm info -a -n /dev/ttyACM0
```

`udevadm` is Linux's tool for inspecting USB device metadata. Let's break down the command:

- `udevadm info` — "give me information about a device"
- `-a` — "walk up the device tree and show all parent devices too" (the screen chip → the USB port → the USB controller → the PCI bus)
- `-n /dev/ttyACM0` — "for this specific device node"

**The key piece we're after:**

```
ATTRS{serial}=="USB35INCHIPSV2"
```

This is the **USB serial number string** — a text label baked into the device's firmware. It's not a "serial number" like a product serial; it's a self-identification string the device reports when asked "who are you?"

| Serial String | What it identifies |
|---|---|
| `USB35INCHIPSV2` | Turing Smart Screen / UsbMonitor 3.5" — **Revision A** protocol |
| `2017-2-25` | XuanFang 3.5" — **Revision B** protocol |
| `USB7INCH` | Turing 7" — **Revision C** protocol |

The Python driver checks this string to decide which communication protocol to use. Different screens from the same manufacturer use completely different command formats.

**For our screen:** `USB35INCHIPSV2` means Revision A — the simplest protocol, which is great for learning.

## Step 4: Verify you have permission to access the device

```bash
ls -la /dev/ttyACM0
```

Serial devices are protected by Linux permissions. You'll see something like:

```
crw-rw---- 1 root dialout ... /dev/ttyACM0
```

- `crw-rw----` — it's a **c**haracter device (c), readable/writable by owner and group
- `root` — owned by root
- `dialout` — the group that has access

**To use the screen without `sudo`**, your user needs to be in the `dialout` group:

```bash
# Check if you're in the group
groups

# If "dialout" is not listed, add yourself (requires logout/login to take effect)
sudo usermod -aG dialout $USER
```

## Summary: What we learned about our screen

| Property | Value | How we found it |
|----------|-------|-----------------|
| Manufacturer | QinHeng Electronics | `lsusb` (VID `1a86`) |
| Product | UsbMonitor | `lsusb` (PID `5722`) |
| Device node | `/dev/ttyACM0` | `ls /dev/ttyACM*` |
| Firmware ID | `USB35INCHIPSV2` | `udevadm` serial attribute |
| Protocol | Revision A | Matched serial string to known table |
| Resolution | 320x480 | Known from protocol docs |
| Communication | Serial @ 115200 baud, RTS/CTS | Known from protocol docs |

With this information, we know exactly how to talk to the screen from code: open `/dev/ttyACM0` as a serial port at 115200 baud and send Revision A commands.

## Step 5: Programmatic auto-discovery via sysfs

The steps above use CLI tools (`lsusb`, `udevadm`) — great for humans, but our .NET
driver needs to find the screen automatically. On Linux, all USB device metadata is
exposed through **sysfs**, a virtual filesystem at `/sys/` that the kernel populates.
No sudo required — it's world-readable.

### Where the data lives

Every USB device gets a directory under `/sys/bus/usb/devices/`. The directory name
encodes the physical topology (bus-port), but we don't need to understand that — we
just scan all of them.

```
/sys/bus/usb/devices/
  ├── 1-11/           ← some other device
  ├── 5-1/            ← our screen
  ├── 5-3/            ← some other device
  └── ...
```

### What's inside a device directory

Each device directory contains plain text files with USB metadata:

```bash
cat /sys/bus/usb/devices/5-1/idVendor     # → 1a86
cat /sys/bus/usb/devices/5-1/idProduct     # → 5722
cat /sys/bus/usb/devices/5-1/serial        # → USB35INCHIPSV2
cat /sys/bus/usb/devices/5-1/product       # → UsbMonitor
cat /sys/bus/usb/devices/5-1/manufacturer  # → 2017-2-25
```

These are the same values we found with `lsusb` and `udevadm`, just exposed as files
that any program can read.

### How to find the serial port name

The device directory also contains subdirectories for each USB **interface** the device
exposes. The serial interface contains a `tty/` folder with the port name:

```
/sys/bus/usb/devices/5-1/              ← the USB device
  ├── idVendor      → "1a86"           ← identify by this
  ├── idProduct     → "5722"           ← and this
  ├── serial        → "USB35INCHIPSV2"
  ├── 5-1:1.0/                         ← interface 0 (serial)
  │     └── tty/
  │           └── ttyACM0/             ← port name!
  └── 5-1:1.1/                         ← interface 1 (not serial)
```

The folder name inside `tty/` is the port name. Prepend `/dev/` to get the device path:
`ttyACM0` → `/dev/ttyACM0`.

### The auto-discovery algorithm

```
1. List all directories in /sys/bus/usb/devices/
2. For each directory:
   a. Read idVendor — skip if not "1a86"
   b. Read idProduct — skip if not "5722"
   c. Search for a "tty" subdirectory anywhere under this device
   d. Read the first entry inside tty/ — that's the port name
3. Return "/dev/{port name}" or null if no match found
```

### Where VID and PID come from

The VID (Vendor ID) and PID (Product ID) are not something we chose — they are
**burned into the USB chip by the manufacturer**. Every USB device in the world reports
a VID/PID pair when plugged in. The USB Implementers Forum (USB-IF) assigns unique
vendor IDs to companies, and each company assigns product IDs to their devices.

We learned our screen's VID/PID in Step 1 using `lsusb`. The Python reference project
[turing-smart-screen-python](https://github.com/mathoudebine/turing-smart-screen-python)
confirmed these values in its auto-detection code, which checks for `VID=0x1a86,
PID=0x5722` and serial string `USB35INCHIPSV2`.
