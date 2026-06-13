namespace ScreenDriver.Widgets;

/// <summary>
/// A widget whose ticks align to interval boundaries measured from the start of the day
/// (cron-like: a 1-minute widget updates on the minute, not at an arbitrary phase from app start).
/// For time-of-day widgets (clock, date) where the displayed value is tied to the wall clock.
/// Delta-based meters should NOT derive from this — they need a steady sampling gap, which the
/// plain <see cref="Widget.Interval"/> cadence provides.
/// </summary>
public abstract record ScheduledWidget(WidgetZone Zone, TimeSpan Interval)
    : Widget(Zone, Interval)
{
    /// <summary>
    /// Delay from now until the next interval boundary, aligned to start-of-day.
    /// Recomputed each tick by the scheduler, so it self-corrects render and scheduling drift.
    /// </summary>
    public TimeSpan NextDelay()
    {
        long step = Interval.Ticks;
        long sinceMidnight = DateTime.Now.TimeOfDay.Ticks;
        return TimeSpan.FromTicks(step - (sinceMidnight % step));
    }
}
