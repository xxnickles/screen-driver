# Bitwise Encoding Guide

A visual guide to the bitwise operators used in `ScreenCommand.Encode()` to pack four
10-bit screen coordinates into 5 bytes for the Revision A protocol.

## Prerequisites: Number systems (decimal, binary, hex)

We use three ways to write the same number. Each one is just a different base:

```
 Base 10 (decimal):  the everyday system    — digits 0-9
 Base 2  (binary):   what the hardware sees — digits 0-1
 Base 16 (hex):      a shorthand for binary — digits 0-9 then A-F
```

### Decimal (base 10) — what we already know

Each position is worth 10× more than the one to its right:

```
 197  =  1×100  +  9×10  +  7×1
```

### Binary (base 2) — what the hardware sees

Each position is worth 2× more than the one to its right. Only digits `0` and `1`:

```
 11000101  =  128 + 64 + 4 + 1  =  197
```

Binary is precise but long and hard to read at a glance.

### Hexadecimal (base 16) — the shorthand

Hex extends decimal with letters A–F to get 16 digits per position:

```
 Decimal:   0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15
 Hex:       0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
```

Each position is worth 16× more than the one to its right. The `0x` prefix means "this
is hex":

```
 0xC5  =  C×16  +  5×1  =  12×16  +  5  =  197
```

### Why hex is useful: each digit = exactly 4 bits

This is the key property. One hex digit maps perfectly to 4 binary digits:

```
 Hex digit:    0     1     2     3     4     5     6     7
 Binary:     0000  0001  0010  0011  0100  0101  0110  0111

 Hex digit:    8     9     A     B     C     D     E     F
 Binary:     1000  1001  1010  1011  1100  1101  1110  1111
```

So a full byte (8 bits) is always exactly 2 hex digits. No math needed — just substitute:

```
 Binary:    1100   0101
               ↓      ↓
 Hex:          C      5       →  0xC5

 The byte 11000101 = 0xC5 = 197.   All three are the same number.
```

### Why this matters in practice

**Binary** is too long to write by hand — `11000101` is 8 characters for one byte.

**Decimal** hides the bit structure. Look at these two command codes:

```
 Decimal:  69  and  197     — are these related? Hard to tell.
```

**Hex** preserves the bit structure because each digit maps to 4 bits:

```
 Hex:      0x45  and  0xC5

 0x45  →  0100  0101
 0xC5  →  1100  0101
                ^^^^
                same lower 4 bits (0101 = 5)
           ^^^^
           different upper 4 bits (0100 vs 1100)
```

In hex you can immediately see that `0x45` and `0xC5` share the same low 4 bits (`5`)
but differ in the high 4 bits (`4` vs `C`). In decimal (`69` vs `197`) that pattern is
completely invisible. This is why hardware protocols, memory addresses, and byte-level
code almost always use hex — it lets you see the bits without writing them all out.

### Quick conversion cheat sheet

| Decimal | Hex  | Binary     | As a byte                |
|---------|------|------------|--------------------------|
| 0       | 0x00 | 00000000   | all bits off             |
| 3       | 0x03 | 00000011   | mask for bottom 2 bits   |
| 15      | 0x0F | 00001111   | mask for bottom 4 bits   |
| 63      | 0x3F | 00111111   | mask for bottom 6 bits   |
| 255     | 0xFF | 11111111   | all bits on (full byte)  |
| 69      | 0x45 | 01000101   | HELLO command            |
| 197     | 0xC5 | 11000101   | DISPLAY_BITMAP command   |

## What is a byte?

A byte is 8 slots, each holding a `0` or `1`. Each slot has a "weight" (a power of 2):

```
Slot:     [  7  ][  6  ][  5  ][  4  ][  3  ][  2  ][  1  ][  0  ]
Weight:    128     64     32     16      8      4      2      1
```

The number **100** in a byte:

```
           128    64     32     16      8      4      2      1
            0      1      1      0      0      1      0      0
                   64  +  32              +    4              = 100
```

## Why 10 bits? And how do 4 values fit in 5 bytes?

The screen is 320x480 pixels. The largest coordinate is 479. How many bits do we need?

```
 8 bits  = 1 byte  → max value 255      ← not enough (479 doesn't fit)
 9 bits            → max value 511      ← enough, but awkward
10 bits            → max value 1023     ← enough, with room to spare
16 bits = 2 bytes  → max value 65535    ← way more than needed, wasteful
```

So each coordinate needs **10 bits**. That's more than 1 byte (8 bits) but less than 2
bytes (16 bits).

### The naive approach: 2 bytes each (wasteful)

If we used 2 full bytes per coordinate, we'd waste 6 bits per value:

```
 x  in 2 bytes:  [ 0 0 0 0 0 0 x x | x x x x x x x x ]   6 bits wasted
 y  in 2 bytes:  [ 0 0 0 0 0 0 y y | y y y y y y y y ]   6 bits wasted
 ex in 2 bytes:  [ 0 0 0 0 0 0 e e | e e e e e e e e ]   6 bits wasted
 ey in 2 bytes:  [ 0 0 0 0 0 0 E E | E E E E E E E E ]   6 bits wasted
                                                          ─────────────
 Total: 8 bytes, but 24 bits (3 bytes!) are just zeros = wasted
```

### The tight approach: pack with no gaps (what we do)

Instead, we line up all 40 bits in a row with no gaps, then slice every 8 bits into a byte:

```
 All 40 bits in a row (no gaps):

 x x x x x x x x x x y y y y y y y y y y e e e e e e e e e e E E E E E E E E E E
 ├─── 10 bits of x ──┤├─── 10 bits of y ──┤├── 10 bits of ex ─┤├── 10 bits of ey ─┤


 Now slice every 8 bits into a byte:

 ┌─── Byte 0 ───┐┌─── Byte 1 ───┐┌─── Byte 2 ───┐┌─── Byte 3 ───┐┌─── Byte 4 ───┐
 x x x x x x x x x x y y y y y y y y y y e e e e e e e e e e E E E E E E E E E E
 ├── 8 bits ────┤├── 8 bits ────┤├── 8 bits ────┤├── 8 bits ────┤├── 8 bits ────┤

 Total: 5 bytes, zero waste
```

Notice how the byte boundaries **don't line up** with the value boundaries — that's the
whole reason we need bitwise operators. Values get **split across two bytes**:

```
 Byte 0:   [xxxxxxxx]         ← 8 bits of x (easy, fits in one byte)
 Byte 1:   [xx yyyyyy]        ← 2 bits of x + 6 bits of y  (x is SPLIT here)
 Byte 2:   [yyyy eeee]        ← 4 bits of y + 4 bits of ex (y is SPLIT here)
 Byte 3:   [eeeeee EE]        ← 6 bits of ex + 2 bits of ey (ex is SPLIT here)
 Byte 4:   [EEEEEEEE]         ← 8 bits of ey (easy, fits in one byte)
```

The bitwise operators are the tools that let us split a value across two bytes and
reassemble the pieces.

## The four bitwise operators

> **Note on diagram width:** In C#, these values are `int` (32 bits), so the operators
> work on all 32 bits. The diagrams below only show 8 slots because the upper bits are
> all zeros — drawing them would just add visual noise without changing the result. When
> a 10-bit value is involved (like in the full trace), we show 10 slots instead. The
> operators always work on the full width; we just trim the leading zeros for readability.

### 1. Right Shift `>>` — slide bits right, drop what falls off

Think of a conveyor belt moving right. Bits that fall off the edge are gone forever.
Zeros fill in from the left.

```
x = 100:   [ 0 ][ 1 ][ 1 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ]

x >> 2:    [ 0 ][ 0 ][ 0 ][ 1 ][ 1 ][ 0 ][ 0 ][ 1 ]
            ^^^   ^^^                               ▲
          zeros fill in                     fell off: 0 0
                                            (gone forever)

Result: 25
```

**Purpose:** extract the upper/left portion of a number by sliding it down to the bottom.

### 2. AND `&` — a cookie cutter that keeps only certain slots

Each `1` in the mask means "keep this slot." Each `0` means "erase it to zero."

```
x = 100:   [ 0 ][ 1 ][ 1 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ]
mask = 3:  [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ]
           ─────────────────────────────────────────────
x & 3:    [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]  = 0
```

The bottom 2 bits of 100 happen to both be 0. Here's one where the result is non-zero:

```
y = 200:   [ 1 ][ 1 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ][ 0 ]
mask = 15: [ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ]
           ─────────────────────────────────────────────
y & 15:   [ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ][ 0 ]  = 8
```

**Purpose:** extract the lower/right portion of a number by erasing everything above.

Common masks and what they keep:

| Mask | Binary       | Keeps bottom N bits |
|------|-------------|---------------------|
| 3    | `00000011`  | 2 bits              |
| 15   | `00001111`  | 4 bits              |
| 63   | `00111111`  | 6 bits              |
| 255  | `11111111`  | 8 bits (full byte)  |

### 3. Left Shift `<<` — slide bits left, fill zeros on the right

The opposite of right shift. Bits move left, zeros fill in from the right.

```
value = 3: [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ]

3 << 6:    [ 1 ][ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]
                                      ^^^^^^^^^^^^^^^^^^^
                                      zeros fill in

Result: 192
```

**Purpose:** position bits exactly where you want them within a byte.

### 4. OR `|` — merge two non-overlapping values

Like stacking two transparencies. Anywhere *either* has a `1`, the result has a `1`.
This works cleanly when the two values don't have `1`s in the same slot.

```
A = 192:   [ 1 ][ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]
B = 12:    [ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ][ 0 ][ 0 ]
           ─────────────────────────────────────────────
A | B:     [ 1 ][ 1 ][ 0 ][ 0 ][ 1 ][ 1 ][ 0 ][ 0 ]  = 204
```

**Purpose:** glue two pieces together into one byte.

## The encoding problem

The screen protocol packs 4 coordinate values (`x`, `y`, `ex`, `ey`) into 5 bytes + 1
command byte. Each coordinate can be 0–1023, which needs 10 bits. Four coordinates = 40
bits = exactly 5 bytes.

The bit layout across the 5 bytes:

```
         Byte 0          Byte 1          Byte 2          Byte 3          Byte 4
     ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
     │xxxxxxxx  │   │xxyyyyyy  │   │yyyyeeeeee│   │eeeeeeEEEE│   │EEEEEEEE  │
     │          │   │          │   │    ^^^^^^ │   │^^^^^^    │   │          │
     │ top 8    │   │bot 2 top6│   │bot4 top4 │   │bot6 top2 │   │ bottom 8 │
     │ of x     │   │of x  of y│   │of y of ex│   │of ex of ey│  │ of ey    │
     └──────────┘   └──────────┘   └──────────┘   └──────────┘   └──────────┘

     x = uppercase x bits    e = ex bits (lowercase)
     y = y bits              E = ey bits (uppercase)
```

## Full trace: `Encode(259, 200, 319, 479, 0xC5)`

These are realistic values: drawing a rectangle from pixel (259, 200) to (319, 479).

### Converting decimal to 10-bit binary

To convert a number to binary, find the powers of 2 that add up to it:

```
 Powers of 2 for 10 bits:

 Bit:    9     8     7     6     5     4     3     2     1     0
 Value: 512   256   128    64    32    16     8     4     2     1
```

For example, **259**:

```
 259 - 256 = 3    → bit 8 = 1
   3 -   2 = 1    → bit 1 = 1
   1 -   1 = 0    → bit 0 = 1

 Bit:    9     8     7     6     5     4     3     2     1     0
         0     1     0     0     0     0     0     0     1     1
              256                                       2     1  = 259 ✓
```

### Our inputs as 10-bit binary

```
 x  = 259  →  0 1 0 0 0 0 0 0 1 1      (256 + 2 + 1)
 y  = 200  →  0 0 1 1 0 0 1 0 0 0      (128 + 64 + 8)
 ex = 319  →  0 1 0 0 1 1 1 1 1 1      (256 + 32 + 16 + 8 + 4 + 2 + 1)
 ey = 479  →  0 1 1 1 0 1 1 1 1 1      (256 + 128 + 64 + 16 + 8 + 4 + 2 + 1)
```

### What "top" and "bottom" bits mean

"Top" = the leftmost (highest-value) bits. "Bottom" = the rightmost (lowest-value) bits.
Think of it like reading left to right — the top comes first.

For each value, the encoding **cuts** it at a specific point. The underscore below shows
where the cut happens for each byte boundary:

```
                  cut
                   ↓
 x  = 259  →  01000000 | 11            top 8 = 01000000 (64)    bottom 2 = 11 (3)
 y  = 200  →  001100   | 1000          top 6 = 001100   (12)    bottom 4 = 1000 (8)
 ex = 319  →  0100     | 111111        top 4 = 0100     (4)     bottom 6 = 111111 (63)
 ey = 479  →  01       | 11011111      top 2 = 01       (1)     bottom 8 = 11011111 (223)
```

The "top" piece goes into the end of one byte, and the "bottom" piece goes into the start
of the next byte. That's the split across byte boundaries.

### Byte 0: `x >> 2` — top 8 bits of x

```
 x (10 bits):  [ 0 ][ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ]

 Slide right by 2, drop the last two:

 x >> 2:             [ 0 ][ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]  →  fell off: 1 1

 Byte 0 = 64
```

### Byte 1: `((x & 3) << 6) | (y >> 4)` — bottom 2 of x + top 6 of y

```
 Step A: Extract bottom 2 bits of x
 ─────────────────────────────────────────────────────────
 x = 259:      [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ]   (just the low byte)
 mask = 3:     [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ]
               ─────────────────────────────────────────────
 x & 3:       [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ]  = 3

 Step B: Slide those 2 bits to the top of the byte
 ─────────────────────────────────────────────────────────
 3:            [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ]
 3 << 6:       [ 1 ][ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]  = 192
                ─x──  ─x─

 Step C: Extract top 6 bits of y
 ─────────────────────────────────────────────────────────
 y (10 bits):  [ 0 ][ 0 ][ 1 ][ 1 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ][ 0 ]

 Slide right by 4, drop the last four:

 y >> 4:             [ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ][ 0 ][ 0 ]  →  fell off: 1 0 0 0
                                                                  = 12

 Step D: Merge with OR
 ─────────────────────────────────────────────────────────
 (x&3)<<6:    [ 1 ][ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]   ← x's 2 bits at top
 y >> 4:       [ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ][ 0 ][ 0 ]   ← y's 6 bits at bottom
               ─────────────────────────────────────────────
 result:       [ 1 ][ 1 ][ 0 ][ 0 ][ 1 ][ 1 ][ 0 ][ 0 ]  = 204
                ─x──  ─x─  ─────────── y ──────────────

 Byte 1 = 204
```

### Byte 2: `((y & 15) << 4) | (ex >> 6)` — bottom 4 of y + top 4 of ex

```
 Step A: Extract bottom 4 bits of y
 ─────────────────────────────────────────────────────────
 y = 200:      [ 1 ][ 1 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ][ 0 ]
 mask = 15:    [ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ]
               ─────────────────────────────────────────────
 y & 15:      [ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ][ 0 ]  = 8

 Step B: Slide to top half of byte
 ─────────────────────────────────────────────────────────
 8 << 4:       [ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]  = 128
                ─y──  ─y──  ─y──  ─y──

 Step C: Extract top 4 bits of ex
 ─────────────────────────────────────────────────────────
 ex = 319 (10 bits):  [ 0 ][ 1 ][ 0 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]

 ex >> 6:      [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ]  = 4
                                           ─ex── ─ex── ─ex── ─ex──

 Step D: Merge with OR
 ─────────────────────────────────────────────────────────
 (y&15)<<4:   [ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ]   ← y's 4 bits
 ex >> 6:      [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ]   ← ex's 4 bits
               ─────────────────────────────────────────────
 result:       [ 1 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ][ 0 ][ 0 ]  = 132
                ──── y ─────────────  ──── ex ────────────

 Byte 2 = 132
```

### Byte 3: `((ex & 63) << 2) | (ey >> 8)` — bottom 6 of ex + top 2 of ey

```
 Step A: Extract bottom 6 bits of ex
 ─────────────────────────────────────────────────────────
 ex = 319:     [ 0 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]   (low byte)
 mask = 63:    [ 0 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]
               ─────────────────────────────────────────────
 ex & 63:     [ 0 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]  = 63

 Step B: Slide left by 2
 ─────────────────────────────────────────────────────────
 63 << 2:      [ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 0 ][ 0 ]  = 252
                ─ex── ─ex── ─ex── ─ex── ─ex── ─ex──

 Step C: Extract top 2 bits of ey
 ─────────────────────────────────────────────────────────
 ey = 479 (10 bits):  [ 0 ][ 1 ][ 1 ][ 1 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]

 ey >> 8:      [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ]  = 1
                                                      ─ey── ─ey──

 Step D: Merge with OR
 ─────────────────────────────────────────────────────────
 (ex&63)<<2:  [ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 0 ][ 0 ]   ← ex's 6 bits
 ey >> 8:      [ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 0 ][ 1 ]   ← ey's 2 bits
               ─────────────────────────────────────────────
 result:       [ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 0 ][ 1 ]  = 253
                ──────────── ex ────────────────  ─── ey ──

 Byte 3 = 253
```

### Byte 4: `ey & 255` — bottom 8 bits of ey

```
 ey = 479:     [...] [ 1 ][ 1 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]
 mask = 255:         [ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]
                     ───────────────────────────────────────────
 ey & 255:          [ 1 ][ 1 ][ 0 ][ 1 ][ 1 ][ 1 ][ 1 ][ 1 ]  = 223

 Byte 4 = 223
```

This is just keeping the low 8 bits — effectively casting the 10-bit value to a byte.

### Byte 5: the command code

```
 command = 0xC5 = 197 (DisplayBitmap)

 Byte 5 = 197
```

## Final result

```
 Encode(259, 200, 319, 479, 0xC5)  →  [ 64, 204, 132, 253, 223, 197 ]
                                        ──── ──── ──── ──── ──── ─────
                                         x    x+y  y+ex ex+ey ey   cmd
```

Six bytes sent over serial. The screen firmware reverses the process — using the same
shifts, masks, and ORs in the opposite direction — to recover `x=259, y=200, ex=319,
ey=479` and knows to start a bitmap transfer.

## The recipe (pattern for every byte)

Every shared byte in the encoding follows the same 4-step pattern:

1. **AND** — extract the leftover low bits from value A
2. **Left Shift** — push them to the top of the byte
3. **Right Shift** — extract the high bits from value B
4. **OR** — merge both halves into one byte

The only thing that changes between bytes is **how many bits** come from each value:

| Byte | From left value | From right value | Split |
|------|----------------|-----------------|-------|
| 0    | 8 bits of x    | —               | 8/0   |
| 1    | 2 bits of x    | 6 bits of y     | 2/6   |
| 2    | 4 bits of y    | 4 bits of ex    | 4/4   |
| 3    | 6 bits of ex   | 2 bits of ey    | 6/2   |
| 4    | —              | 8 bits of ey    | 0/8   |
