using System.Globalization;

namespace Plugin.ClockViews;

/// <summary>
/// Platform-agnostic helpers for a valve (Nixie/seven-segment tube) clock display.
/// The display is always 24-hour.
/// </summary>
public static class ValveDisplay
{
    /// <summary>
    /// Returns the digits to show, left to right, as <c>HH MM</c> (4 digits) or
    /// <c>HH MM SS</c> (6 digits when <paramref name="includeSeconds"/> is true).
    /// Hours are always 24-hour (0–23).
    /// </summary>
    public static int[] Digits(TimeSpan time, bool includeSeconds)
    {
        int hours = ((int)time.TotalHours) % 24;
        if (hours < 0)
            hours += 24;

        int minutes = Math.Abs(time.Minutes);
        int seconds = Math.Abs(time.Seconds);

        return includeSeconds
            ? new[] { hours / 10, hours % 10, minutes / 10, minutes % 10, seconds / 10, seconds % 10 }
            : new[] { hours / 10, hours % 10, minutes / 10, minutes % 10 };
    }

    /// <summary>
    /// Returns the decimal digits of a Unix timestamp, left to right (one valve per digit).
    /// Negative values (pre-1970) are shown as their magnitude.
    /// </summary>
    public static int[] UnixDigits(long unixSeconds)
    {
        if (unixSeconds < 0)
            unixSeconds = -unixSeconds;

        string text = unixSeconds.ToString(CultureInfo.InvariantCulture);
        var digits = new int[text.Length];
        for (int i = 0; i < text.Length; i++)
            digits[i] = text[i] - '0';
        return digits;
    }
}
