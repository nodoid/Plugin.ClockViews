namespace Plugin.ClockViews;

/// <summary>
/// Platform-agnostic helpers for converting a time of day into clock-hand angles.
/// All angles are in degrees, measured clockwise from the 12 o'clock position
/// (0° points straight up, 90° points to 3 o'clock).
/// </summary>
public static class ClockMath
{
    /// <summary>The angle of the hour hand, including the partial advance from elapsed minutes.</summary>
    public static double HourAngle(TimeSpan time)
    {
        // 360° / 12h = 30° per hour, plus 0.5° per minute, plus a smooth advance for seconds.
        double hours = time.TotalHours % 12.0;
        return Normalize(hours * 30.0);
    }

    /// <summary>The angle of the minute hand, including the partial advance from elapsed seconds.</summary>
    public static double MinuteAngle(TimeSpan time)
    {
        // 360° / 60m = 6° per minute, advancing smoothly with seconds.
        double minutes = time.TotalMinutes % 60.0;
        return Normalize(minutes * 6.0);
    }

    /// <summary>The angle of the second hand.</summary>
    public static double SecondAngle(TimeSpan time)
    {
        // 360° / 60s = 6° per second.
        double seconds = time.TotalSeconds % 60.0;
        return Normalize(seconds * 6.0);
    }

    /// <summary>Wraps an arbitrary angle into the [0, 360) range.</summary>
    public static double Normalize(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0 ? degrees + 360.0 : degrees;
    }

    /// <summary>
    /// Returns the point on a circle for the given <paramref name="angleDegrees"/>
    /// (measured clockwise from 12 o'clock), at <paramref name="radius"/> from the center.
    /// </summary>
    public static (double X, double Y) PointOnDial(double centerX, double centerY, double radius, double angleDegrees)
    {
        // Convert clock angle (0° up, clockwise) to standard math radians (0 at +X, counter-clockwise).
        double radians = (angleDegrees - 90.0) * Math.PI / 180.0;
        double x = centerX + radius * Math.Cos(radians);
        double y = centerY + radius * Math.Sin(radians);
        return (x, y);
    }
}
