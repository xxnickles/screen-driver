# Revision A Protocol Reference

How our UsbMonitor 3.5" screen communicates — the bytes we send and what they mean.

## How serial communication works (30-second primer)

Serial communication is the simplest way two devices can talk: one sends bytes, the other receives them. There's a single "wire" (channel) and they take turns.

**Key settings** for our screen:
- **Baud rate: 115200** — the speed, in bits per second. Both sides must agree on this or the data looks like garbage.
- **Flow control: RTS/CTS** — "Ready To Send / Clear To Send." Hardware signals that let the screen say "wait, I'm busy" before we overwhelm it with data. Without this, we could send image data faster than the screen can process it, losing pixels.

## The command packet

Every command to the screen is exactly **6 bytes**. The first 5 bytes encode coordinates (a rectangular region on screen), and the 6th byte is the command code.

> **Exception: SET_ORIENTATION is 16 bytes.** The first 6 bytes follow the standard layout, but the command is followed by 10 additional bytes carrying the orientation value and target display dimensions. See [SET_ORIENTATION packet](#set_orientation-packet) below.

### Coordinate encoding

The screen is 320×480 pixels. Commands often need to specify a rectangle: "from point (x, y) to point (ex, ey)." These four numbers are packed tightly into 5 bytes using bit manipulation:

```
Byte 0:  x >> 2                              (top 8 bits of x)
Byte 1: ((x & 3) << 6) | (y >> 4)           (bottom 2 bits of x + top 4 bits of y)
Byte 2: ((y & 15) << 4) | (ex >> 6)         (bottom 4 bits of y + top 4 bits of ex)
Byte 3: ((ex & 63) << 2) | (ey >> 8)        (bottom 6 bits of ex + top 2 bits of ey)
Byte 4:  ey & 255                            (bottom 8 bits of ey)
```

**Why pack them this way?** To fit four 10-bit coordinate values into 5 bytes. Each coordinate needs up to 10 bits (max value ~1024, enough for any screen dimension). 4 × 10 = 40 bits = 5 bytes exactly. It's a space-efficient binary encoding.

For commands that don't use coordinates (like brightness), the coordinate bytes are repurposed to carry the parameter value.

### Command codes

| Name | Code | Coordinate meaning | Notes |
|------|------|--------------------|-------|
| HELLO | 69 (0x45) | Unused (all zeros) | Send the byte `0x45` six times. Screen responds with its size ID. |
| RESET | 101 (0x65) | Unused | Resets the display to its power-on state. |
| CLEAR | 102 (0x66) | Unused | Fills the entire screen with white. |
| SCREEN_OFF | 108 (0x6C) | Unused | Turns the backlight off. |
| SCREEN_ON | 109 (0x6D) | Unused | Turns the backlight on. |
| SET_BRIGHTNESS | 110 (0x6E) | x = brightness level | **Inverted scale:** 0 = maximum brightness, 255 = completely dark. |
| SET_ORIENTATION | 121 (0x79) | Coordinates zeroed | **16-byte packet** (not 6). Orientation and display dimensions follow in bytes 6–15. See SET_ORIENTATION packet section. |
| DISPLAY_BITMAP | 197 (0xC5) | (x, y) to (ex, ey) | Defines the rectangle where the following image data will be drawn. |

### HELLO handshake

The HELLO command is special — it's the only command that expects a response:

```
Send:    [0x45, 0x45, 0x45, 0x45, 0x45, 0x45]   (six times the HELLO byte)
Receive: one byte identifying the screen size
```

| Response byte | Screen |
|---|---|
| 0x01 or 'C' | 3.5" (320×480) |
| 0x02 or 'E' | 5" (480×800) |
| 0x03 or 'G' | 7" (600×1024) |

### SET_ORIENTATION packet

SET_ORIENTATION is the one command that breaks the 6-byte rule. It sends **16 bytes total**: the standard 6-byte header followed by 10 bytes of orientation data.

```
Byte  0:  0x00   (x >> 2 — coordinates zeroed)
Byte  1:  0x00   ((x & 3) << 6 | y >> 4 — coordinates zeroed)
Byte  2:  0x00   ((y & 15) << 4 | ex >> 6 — coordinates zeroed)
Byte  3:  0x00   ((ex & 63) << 2 | ey >> 8 — coordinates zeroed)
Byte  4:  0x00   (ey & 255 — coordinates zeroed)
Byte  5:  0x79   SET_ORIENTATION command code
Byte  6:  orientation value + 100   (see table below)
Byte  7:  width high byte           (target display width, big-endian)
Byte  8:  width low byte
Byte  9:  height high byte          (target display height, big-endian)
Byte 10:  height low byte
Bytes 11–15: 0x00 (unused)
```

**Why width and height?** When you rotate the screen, the firmware remaps the entire coordinate system to the new dimensions. By including the target width and height in the packet, you tell the firmware what to map to. After sending SET_ORIENTATION, all subsequent DISPLAY_BITMAP coordinates should use the new dimensions — not the physical defaults.

For example, switching a 320×480 screen to landscape means width becomes 480 and height becomes 320. A full-screen DISPLAY_BITMAP after that uses `(x=0, y=0, ex=479, ey=319)`.

#### Orientation values

| Enum value | Firmware byte (value + 100) | Orientation |
|---|---|---|
| 0 | 100 | Portrait |
| 1 | 101 | Reverse Portrait |
| 2 | 102 | Landscape |
| 3 | 103 | Reverse Landscape |

## Sending an image

This is the core operation — displaying pixels on the screen.

### Step 1: Send the DISPLAY_BITMAP command

Pack the target rectangle coordinates into the 6-byte command:

```
DISPLAY_BITMAP with (x=0, y=0, ex=319, ey=479)  →  full screen update
```

### Step 2: Send the pixel data

Immediately after the command, send the raw pixel data as **RGB565 Little-Endian** bytes.

#### What is RGB565?

Normal computer images use 24-bit color: 8 bits each for Red, Green, Blue (16.7 million colors). Our screen uses **16-bit color** to save bandwidth:

```
Normal RGB888:  RRRRRRRR GGGGGGGG BBBBBBBB   (3 bytes per pixel)
RGB565:         RRRRRGGG GGGBBBBB             (2 bytes per pixel)
```

- **R: 5 bits** (32 levels of red)
- **G: 6 bits** (64 levels of green — humans are more sensitive to green)
- **B: 5 bits** (32 levels of blue)

**To convert a 24-bit pixel to RGB565:**

```
R5 = R8 >> 3        (keep top 5 bits)
G6 = G8 >> 2        (keep top 6 bits)
B5 = B8 >> 3        (keep top 5 bits)
rgb565 = (R5 << 11) | (G6 << 5) | B5
```

#### Little-Endian byte order

The 16-bit RGB565 value is sent **low byte first** (little-endian):

```
RGB565 value: 0xF800 (pure red)
Sent as:      [0x00, 0xF8]   (low byte, then high byte)
```

This is how x86 processors store numbers in memory, so on a PC you often get this for free.

### Step 3: Chunk the data

Don't send all the pixel data at once. Send it in chunks of `display_width × 8` bytes:

```
For 320px wide screen:
  320 pixels × 2 bytes/pixel × 4 rows = 2560 bytes per chunk
  (The "×8" in the Python code refers to bytes, which is 4 rows of pixels)
```

**Why chunking?** The screen has a small receive buffer. If we blast the entire image at once (~307 KB for a full frame), the buffer overflows and we get corrupted/missing pixels. By sending in chunks, we give the hardware time to process each batch.

### Full-screen update data size

```
320 × 480 pixels × 2 bytes/pixel = 307,200 bytes (~300 KB per frame)
```

At 115200 baud (roughly 11,520 bytes/second), a full frame takes about **26 seconds**. This is why partial updates (only redrawing changed regions) are important for a responsive display.

## Typical initialization sequence

```
1. Open serial port at 115200 baud, RTS/CTS flow control
2. Send HELLO (6× 0x45), read 1 byte response → confirms screen size
3. Send RESET command
4. Send SET_ORIENTATION (landscape or portrait)
5. Send SET_BRIGHTNESS (e.g., x=0 for max brightness)
6. Send DISPLAY_BITMAP + pixel data → image appears on screen
```

## Quick reference: example bytes

**Set brightness to maximum:**
```
x=0, y=0, ex=0, ey=0, cmd=110
→ [0x00, 0x00, 0x00, 0x00, 0x00, 0x6E]
```

**Set orientation to landscape (3.5" screen, width=480, height=320):**
```
16-byte packet:
  Bytes 0–5  (6-byte header, coordinates zeroed, command 0x79):
    [0x00, 0x00, 0x00, 0x00, 0x00, 0x79]
  Byte  6    (orientation value 2 + 100 = 102 = 0x66):
    [0x66]
  Bytes 7–8  (width 480 = 0x01E0, big-endian):
    [0x01, 0xE0]
  Bytes 9–10 (height 320 = 0x0140, big-endian):
    [0x01, 0x40]
  Bytes 11–15 (unused):
    [0x00, 0x00, 0x00, 0x00, 0x00]

Full packet: [0x00, 0x00, 0x00, 0x00, 0x00, 0x79, 0x66, 0x01, 0xE0, 0x01, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00]
```

**Display full-screen bitmap:**
```
x=0, y=0, ex=319, ey=479, cmd=197
→ [0x00, 0x00, 0x4F, 0xF7, 0xDF, 0xC5]
  then send 307,200 bytes of RGB565 pixel data in chunks
```
