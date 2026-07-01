namespace Plugin.ClockViews;

/// <summary>
/// Carries the current time whenever a clock's displayed time changes.
/// Raised by <see cref="ClockViewBase.TimeChanged"/>; every clock view subscribes to it.
/// </summary>
public class ClockTimeChangedEventArgs : EventArgs
{
    public ClockTimeChangedEventArgs(DateTime dateTime) => DateTime = dateTime;

    /// <summary>The current date and time the clock should display.</summary>
    public DateTime DateTime { get; }
}
