# System Stats Reference

How the driver collects live system metrics — what it reads, where it reads from, and how the numbers are calculated.

## Overview

| Stat | Source | Unit | Platform |
|------|--------|------|----------|
| CPU usage | `/proc/stat` | % (0–100) | Linux |
| CPU temperature | `/sys/class/hwmon` | °C | Linux, AMD/Intel |
| Memory usage | `/proc/meminfo` | %, MB | Linux |
| GPU usage | `/sys/class/drm` | % (0–100) | Linux, AMD only |
| GPU temperature | `/sys/class/drm` hwmon | °C | Linux, AMD only |
| Disk usage | `/proc/mounts` | GB | Linux |

All providers return `null` (or zero-valued tuples) when the data source is unavailable — callers should always handle the "not available" case.

---

## CPU

### Usage

CPU usage is not a simple counter you can read directly. The kernel tracks cumulative time-slices ("jiffies") spent in each state since boot. To get a useful percentage, you read two snapshots and compare the difference — this is called a **delta-based** calculation.

**Where it reads from:** The first line of `/proc/stat`.

**Example raw line:**

```
cpu  4310876 1245 1089432 48321945 23456 0 45678 0 0 0
```

The fields, in order, are: `user`, `nice`, `system`, `idle`, `iowait`, `irq`, `softirq`, `steal`, `guest`, `guest_nice`. All values are cumulative jiffies (time units) since boot.

**How the calculation works:**

```
totalIdle  = idle + iowait
total      = user + nice + system + totalIdle + irq + softirq + steal

deltaIdle  = totalIdle − previousIdle
deltaTotal = total − previousTotal

usagePercent = (1 − deltaIdle / deltaTotal) × 100
```

On the very first call, there is no previous snapshot, so the result is 0. Every subsequent call reflects the CPU load during the interval since the last call.

`iowait` is included in the idle bucket because the CPU is not actively computing during a disk wait — it is waiting on hardware. Treating it as idle gives a more accurate picture of actual compute load.

**Returns:** A value between 0.0 and 100.0.

### Temperature

The Linux kernel exposes hardware sensor readings through a virtual filesystem at `/sys/class/hwmon`. Each sensor device gets its own subdirectory (e.g., `hwmon0`, `hwmon1`) with a `name` file identifying the driver.

CPU temperature is probed **once on the first call** and the path is cached for all subsequent reads. This avoids repeated directory scans.

**Supported sensors:**

| Sensor name | Vendor |
|-------------|--------|
| `k10temp` | AMD |
| `coretemp` | Intel |

**Where it reads from:** The first matching sensor's `temp1_input` file, e.g.:

```
/sys/class/hwmon/hwmon2/temp1_input
```

**Example raw value:**

```
48000
```

The kernel reports temperature in **millidegrees Celsius**. Divide by 1000 to get degrees:

```
48000 / 1000 = 48°C
```

**Returns:** Integer degrees Celsius, or `null` if no supported sensor is found.

---

## Memory

Memory usage is read directly from the kernel's memory accounting file without any delta comparison — these are current live values, updated continuously.

**Where it reads from:** `/proc/meminfo`

**Example raw lines:**

```
MemTotal:       65748832 kB
MemFree:         1024512 kB
MemAvailable:   32456712 kB
...
```

Only two fields matter here: `MemTotal` and `MemAvailable`.

**Why `MemAvailable` and not `MemFree`?**

The kernel aggressively uses spare RAM for disk caching to speed up file access. `MemFree` is the amount of memory that is completely unused — it looks alarming on a healthy system because almost none of it is free. `MemAvailable` is the kernel's own estimate of how much memory could actually be freed if an application needed it (including reclaimable cache). It is the realistic "how much is left" number.

**How the calculation works:**

```
usedKb     = MemTotal − MemAvailable
usedPercent = usedKb / MemTotal × 100
totalMb    = MemTotal / 1024
usedMb     = usedKb / 1024
```

**Returns:** A tuple of `(UsedPercent, TotalMb, UsedMb)`. Returns `(0, 0, 0)` if the file cannot be read.

---

## GPU

GPU stats are AMD-only. The AMD GPU kernel driver (`amdgpu`) exposes both a usage counter and temperature through the same sysfs tree as the DRM display stack.

Both the usage path and temperature path are discovered **once on the first call** (a single shared probe) and cached permanently. The probe walks `/sys/class/drm` looking for a card that exposes the expected files.

### Usage

**Where it reads from:** `/sys/class/drm/card0/device/gpu_busy_percent`

(The `card0` part may vary — the probe finds whichever card exposes this file.)

**Example raw value:**

```
23
```

This is a direct percentage already — no calculation needed. The kernel driver computes it internally based on how often the GPU engine is active.

**Returns:** Integer between 0 and 100, or `null` if no AMD GPU is found.

### Temperature

Temperature is found through the GPU card's own hwmon subtree rather than the top-level hwmon directory used for CPU temperature. This keeps the GPU temperature tied to the specific GPU card rather than relying on sensor ordering.

**Probe path:**

```
/sys/class/drm/card0/device/hwmon/hwmon*/name   →  must read "amdgpu"
/sys/class/drm/card0/device/hwmon/hwmon*/temp1_input
```

**Example raw value:**

```
52000
```

Same millidegrees format as CPU temperature — divide by 1000:

```
52000 / 1000 = 52°C
```

**Returns:** Integer degrees Celsius, or `null` if no `amdgpu` hwmon sensor is found under the GPU card.

---

## Disk

Disk stats are built from a combination of three sources: `/proc/mounts` (what is mounted), `/dev/disk/by-label/` (human-readable names), and the OS `DriveInfo` API (actual space figures). The result is one entry per unique physical device currently mounted.

### Reading mounts

**Where it reads from:** `/proc/mounts`

This file lists every active mount, one per line. Each line has the format:

```
<device> <mountpoint> <fstype> <options> <dump> <pass>
```

**Example lines:**

```
/dev/nvme0n1p2 / ext4 rw,relatime 0 0
/dev/nvme0n1p1 /boot/efi vfat rw,relatime 0 0
/dev/sda1 /data ext4 rw,relatime 0 0
tmpfs /tmp tmpfs rw,nosuid 0 0
```

Only lines where the device starts with `/dev/` are considered — virtual filesystems like `tmpfs`, `sysfs`, and `cgroup` are ignored automatically.

### Deduplication

A physical disk may have multiple partitions, all of which appear as separate lines in `/proc/mounts`. To avoid reporting the same disk multiple times, entries are deduplicated by device path. Only the first mounted partition from each device is used to query total/available space.

### Excluding removable media

Mount points under `/run/media/` are skipped entirely. This is where desktop environments (like GNOME) automatically mount USB drives and SD cards.

### Labels

Raw device names like `/dev/nvme0n1p2` are not useful on a display. Labels are resolved in this order:

1. **Filesystem label** — if a filesystem was formatted with a label (e.g., `mkfs.ext4 -L "Games" /dev/sda1`), the kernel creates a symlink under `/dev/disk/by-label/` pointing back to the device. These symlinks are read and inverted into a lookup table at the start of each call.

2. **Device type fallback** — if no label exists, the device filename is used to determine the type:
   - Starts with `nvme` → labeled `NVMe`
   - Anything else → labeled `SATA`, `SATA 2`, etc. (incrementing counter)

**Example `/dev/disk/by-label/` entry:**

```
/dev/disk/by-label/Games  →  ../../nvme0n1p3   (symlink target)
```

After resolving the symlink to a full path, this becomes: `{ "/dev/nvme0n1p3" → "Games" }`.

Note: labels containing spaces are encoded in the filesystem as `\x20` (e.g., `My\x20Drive`). These are decoded back to spaces when building the label map.

### Space calculation

Space figures come from the OS `DriveInfo` API, called on the mount point of each device. Used space is calculated as:

```
usedGb = (TotalSize − AvailableFreeSpace) / 1024³
totalGb = TotalSize / 1024³
```

`AvailableFreeSpace` (rather than `TotalFreeSpace`) is used because Linux reserves a percentage of disk space for the root user by default. `AvailableFreeSpace` reflects what is actually available to normal processes.

**Returns:** A list of `DiskInfo` records, each containing a label, device path, used GB, and total GB. Returns an empty list if `/proc/mounts` cannot be read or no real block devices are mounted.
