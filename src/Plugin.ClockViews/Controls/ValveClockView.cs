using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// A valve (Nixie tube) clock. Each digit is a "valve" that updates as the time changes;
/// when <see cref="ClockViewBase.IsSecondsShown"/> is true, two extra valves are shown for
/// the seconds. This clock is always 24-hour (it ignores <see cref="ClockViewBase.Is24HourClock"/>).
/// It honours <see cref="ClockViewBase.IsUnixTime"/> (shows the Unix timestamp) and
/// <see cref="ClockViewBase.IsUTC"/> (shows UTC).
/// <para>
/// <see cref="ClockViewBase.ClockColor"/> is the filament color (light red by default) and
/// <see cref="ValveShellColor"/> is the tube shell color (grey by default); both are bindable.
/// </para>
/// </summary>
public class ValveClockView : ClockViewBase
{
    static readonly int[] TimeColons = { 1 };            // HH:MM
    static readonly int[] TimeColonsWithSeconds = { 1, 3 }; // HH:MM:SS

    readonly ValveClockDrawable _drawable = new();

    public ValveClockView()
    {
        Drawable = _drawable;
        WidthRequest = 320;
        HeightRequest = 120;

        // Filament defaults to light red for this view.
        ClockColor = ValveClockDrawable.DefaultFilamentColor;

        TimeChanged += (_, _) => Refresh();
        Refresh();
    }

    public static readonly BindableProperty ValveShellColorProperty = BindableProperty.Create(
        nameof(ValveShellColor), typeof(Color), typeof(ValveClockView), Colors.Grey,
        propertyChanged: (b, _, _) => ((ValveClockView)b).OnAppearanceChanged());

    /// <summary>The valve (tube) shell color. Defaults to grey.</summary>
    public Color ValveShellColor
    {
        get => (Color)GetValue(ValveShellColorProperty);
        set => SetValue(ValveShellColorProperty, value);
    }

    /// <summary>Reads <see cref="ValveShellColor"/> on the UI thread.</summary>
    public Task<Color> GetValveShellColorAsync() => ReadAsync(() => ValveShellColor);

    protected override void OnAppearanceChanged() => Refresh();

    // Rebuilds the digits and colors from the current base state, then redraws.
    void Refresh()
    {
        if (IsUnixTime)
        {
            _drawable.Digits = ValveDisplay.UnixDigits(CurrentUnixTimeSeconds);
            _drawable.ColonAfter = Array.Empty<int>();
        }
        else
        {
            _drawable.Digits = ValveDisplay.Digits(EffectiveTime, IsSecondsShown);
            _drawable.ColonAfter = IsSecondsShown ? TimeColonsWithSeconds : TimeColons;
            // Blink the colon: lit for the first half of each second, unlit for the second half.
            // A fixed time (not running) shows the colon steady.
            _drawable.ColonLit = !IsRunning || CurrentDateTime.Millisecond < 500;
        }

        _drawable.FilamentColor = ClockColor;
        _drawable.ShellColor = ValveShellColor;
        _drawable.DateText = (ShowDate && !IsUnixTime) ? CurrentDateText : null;
        Invalidate();
    }
}
