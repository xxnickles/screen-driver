# Widget Scheduling Reference

_Created: 2026-06-12_

This document covers how widgets are timed, why there are two scheduling modes, the math behind boundary alignment, and how to extend the system. It is written for someone who knows C# and wants to re-derive the logic or adapt an existing widget.

---

## Contents

1. [Two scheduling modes](#1-two-scheduling-modes)
2. [Why the split exists — the bug that motivated it](#2-why-the-split-exists)
3. [Boundary-alignment math](#3-boundary-alignment-math)
4. [The modulo operator — a focused primer](#4-the-modulo-operator)
5. [Extending to cron-like schedules](#5-extending-to-cron-like-schedules)

---

## 1. Two scheduling modes

Every widget carries two fields from its base type:

| Field | Type | Meaning |
|---|---|---|
| `Zone` | `WidgetZone` | Screen region the widget owns |
| `Interval` | `TimeSpan` | How often the widget should tick |

The scheduler runs one async loop per widget. The entire timing decision is a single line:

```csharp
var delay = widget is ScheduledWidget scheduled ? scheduled.NextDelay() : widget.Interval;
await Task.Delay(delay, ct);
EmitRender(widget);
```

That `is` check is the only place the two modes diverge.

### Mode A — Steady cadence (plain `Widget`)

The loop sleeps exactly `widget.Interval` between ticks. The gap between two consecutive renders is always approximately `Interval`. Nothing is recomputed; no wall-clock math is involved.

**Which widgets use this:**

| Widget family | Why steady cadence is correct |
|---|---|
| `CpuUsageWidget` | Delta-based: computes `(currentTotal - prevTotal)`. A consistent gap is required — see section 2. |
| `NetworkWidget` | Delta-based: divides byte delta by elapsed seconds. Same requirement. |
| `MemoryBarWidget`, `MemoryTextWidget`, `MemoryPercentWidget`, `MemoryGaugeWidget` | Instantaneous: reads current `/proc/meminfo` each tick. Timing doesn't affect correctness. |
| `GpuMemoryBarWidget`, `GpuMemoryTextWidget`, `GpuMemoryPercentWidget`, `GpuMemoryGaugeWidget` | Instantaneous: reads current VRAM sysfs files each tick. |

### Mode B — Boundary-aligned (`ScheduledWidget`)

The loop recomputes the delay on every tick by asking the widget `NextDelay()`. That method returns the time until the next multiple of `Interval` counted from local midnight, so the widget fires at wall-clock boundaries rather than at an arbitrary phase from app start.

**Which widgets use this:**

| Widget | Interval | Effect |
|---|---|---|
| `ClockWidget` | 1 minute (hardcoded) | Updates on every `:00` second |
| `DateWidget` | Caller-supplied | Typically 1 hour or 24 h |

The rationale: a clock that starts at 12:03:47 should next tick at 12:04:00, not 12:04:47. Boundary alignment achieves this. Delta-based meters must NOT use it — see section 2.

### Immediate render on start

Before entering its loop, every widget calls `EmitRender` once unconditionally:

```csharp
// Render immediately on start
EmitRender(widget);

while (!ct.IsCancellationRequested)
{
    var delay = ...;
    await Task.Delay(delay, ct);
    EmitRender(widget);
}
```

This means every zone shows correct content at launch, regardless of how long the first tick is. For a boundary-aligned widget that starts near the top of a minute, the first scheduled tick might be only a few milliseconds away; the immediate render makes that irrelevant.

---

## 2. Why the split exists

### The bug

In an earlier version, all widgets were boundary-aligned: each tick recomputed a delay to the next interval boundary. This introduced a subtle failure mode for delta-based meters.

`Task.Delay` uses a monotonic clock and can wake a few milliseconds *before* the wall-clock boundary. When the loop immediately recomputed the next boundary delay, the result was a tiny "sliver" — sometimes single-digit milliseconds. The widget ticked twice in rapid succession:

```
tick 1 at T+0:59.997  → gap from previous = 59.997 s  (normal)
tick 2 at T+1:00.001  → gap from previous = 4 ms       (sliver)
```

For `CpuUsageWidget`, `deltaTotal` on that sliver tick was effectively 0 → displayed 0% usage.

For `NetworkWidget`, the 4 ms elapsed divided into the byte delta produced an enormous spike, then back to 0 — both values are wrong.

The jitter was visible on screen: meters occasionally flashed 0 or spiked before settling back.

### The fix — by construction

Confining `NextDelay()` to `ScheduledWidget` means delta-based meters never touch boundary math. Their loop gap is always the full `Interval` the widget was constructed with. The sliver condition cannot occur for them because their delay is a constant, not a wall-clock recomputation.

### The design principle at work

The project explicitly avoids over-abstraction. The fix is not a virtual method on all 9+ widget types with different override strategies. It is one `is` check in the scheduler against the two widget types that actually benefit from alignment. All other widgets ignore the distinction entirely.

---

## 3. Boundary-alignment math

`NextDelay()` in full:

```csharp
public TimeSpan NextDelay()
{
    long step = Interval.Ticks;
    long sinceMidnight = DateTime.Now.TimeOfDay.Ticks;
    return TimeSpan.FromTicks(step - (sinceMidnight % step));
}
```

**Step by step:**

1. `sinceMidnight` is the number of ticks elapsed since local midnight (always ≥ 0, always < 86,400 seconds worth of ticks).
2. `sinceMidnight % step` is the offset into the current interval — how many ticks have elapsed since the last boundary.
3. `step - offset` is the remaining ticks until the next boundary.

Example: interval = 1 minute (600,000,000 ticks). Current time is 12:03:47, so `sinceMidnight` = 723,470,000,000 ticks (roughly). `sinceMidnight % step` = 227,000,000 ticks (47 s). `step - 227,000,000` = 373,000,000 ticks (13 s). The next tick fires at 12:04:00 exactly.

### Boundary table

| Interval | Tick boundaries |
|---|---|
| 2 seconds | Every even second: :00, :02, :04, … |
| 1 minute | Every `:00` second: 12:00:00, 12:01:00, … |
| 1 hour | Top of each hour: 12:00, 13:00, … |
| 24 hours | Local midnight — `sinceMidnight % 24h` is always just `sinceMidnight` itself (left < right, so modulo returns the left side), making the delay `24h - sinceMidnight` = time until midnight |

### When alignment is clean vs. uneven

The grid tiles cleanly only when `Interval` divides evenly into 24 hours (or into an hour, or into a minute for sub-minute intervals). Examples of clean divisors: 1 s, 2 s, 3 s, 4 s, 5 s, 6 s, 10 s, 12 s, 15 s, 20 s, 30 s, 1 min, 2 min, 3 min, 4 min, 5 min, 6 min, 10 min, 12 min, 15 min, 20 min, 30 min, 1 h, 2 h, 3 h, 4 h, 6 h, 8 h, 12 h, 24 h.

A non-divisor like 7 hours gives boundaries at 00:00, 07:00, 14:00, 21:00 — and then the next boundary after 21:00 is midnight (3 h later, not 7 h). The final step in every day is shorter than `Interval`.

**This unevenness is acceptable.** These are display widgets; sub-interval precision at midnight is irrelevant. Adding logic to detect and compensate for non-divisor intervals would be complexity for no visible user benefit.

### Suspend/resume caveat

If the machine sleeps across a boundary (e.g., suspends at 12:03 and wakes at 12:07 on a 1-minute interval), `Task.Delay` completes late — the tick fires on wake with stale content briefly visible. Two things limit the impact:

- The immediate render on start means a fresh frame was already sent at the last start/resume.
- The next `NextDelay()` call recomputes from the current wall clock, so the following tick realigns correctly.

For a system-monitor display this is fine.

---

## 4. The modulo operator

The boundary math relies entirely on `%`. This section is a self-contained primer.

### Definition

`a % b` returns the remainder after dividing `a` by `b`.

```
17 % 5  →  2       (17 = 3×5 + 2)
10 % 5  →  0       (10 = 2×5 + 0, divides evenly)
 3 % 5  →  3       (3 = 0×5 + 3, left side returned when left < right)
```

Mental model: remove as many whole copies of `b` from `a` as possible; what's left is the result.

### Wrapping / cycling property

`% n` produces a value that cycles through `0, 1, 2, … n-1` and then wraps back to 0:

```
0%3=0  1%3=1  2%3=2  3%3=0  4%3=1  5%3=2  6%3=0
```

This is why it snaps a continuous quantity onto a repeating grid. `sinceMidnight % step` maps the ever-increasing time-of-day value onto the slot `[0, step)` within the current interval — exactly the "where am I within this interval" answer the boundary math needs.

### C# specifics

**Sign follows the dividend (left operand).** C# truncates integer division toward zero, so:

```csharp
-7 % 3   →  -1    // NOT 2
 7 % -3  →   1
```

If you ever need a guaranteed non-negative result (safe wrap-around with possibly-negative values):

```csharp
((x % n) + n) % n
```

The boundary code never needs this because `TimeOfDay.Ticks` is always ≥ 0.

**Works on floating-point too:**

```csharp
5.5 % 2.0  →  1.5
```

### Common uses

| Use | Expression |
|---|---|
| Even check | `n % 2 == 0` |
| Fire every Nth iteration | `i % n == 0` |
| Wrap an index into a fixed-size array | `index % array.Length` |
| Snap onto a repeating grid | `value - (value % step)` gives the floor boundary; `step - (value % step)` gives the distance to the ceiling boundary — our case |

---

## 5. Extending to cron-like schedules

The scheduler has no knowledge of how `NextDelay()` computes its result. It calls the method; the widget owns the strategy. This is the extension seam — richer schedules require zero scheduler changes.

### Current strategy: interval-aligned to midnight

`ScheduledWidget.NextDelay()` implements one strategy: align to multiples of `Interval` counted from midnight. All `ScheduledWidget` subclasses share it.

### Extension path

**`DailyAtWidget(TimeOnly time)`** — fires once per day at a specific wall-clock time:

```csharp
public TimeSpan NextDelay()
{
    var now = DateTime.Now;
    var target = now.Date.Add(time.ToTimeSpan());
    if (target <= now) target = target.AddDays(1);
    return target - now;
}
```

Covers "refresh data at 09:00" or "date widget flips at 00:05". Small, no dependencies.

**Full 5-field cron** — if you need weekday filters, ranges, or step values (e.g., `*/15 8-18 * * 1-5`), wrap an established parser such as [Cronos](https://github.com/HangfireIO/Cronos):

```csharp
public TimeSpan NextDelay()
{
    var next = _cronExpression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
    return next is null ? TimeSpan.FromHours(1) : next.Value - DateTimeOffset.Now;
}
```

Do not hand-roll cron parsing. DST transitions, day-of-week vs. day-of-month semantics, and range/step syntax are all foot-guns that Cronos already handles correctly.

### Honest recommendation

For a system-monitor display, interval-aligned-to-midnight plus a possible `DailyAt` covers every realistic need. Reach for Cronos only if weekday rules or complex interval expressions are genuinely required — they almost certainly are not.
