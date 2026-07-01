using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// A wristwatch-style clock with a bindable <see cref="WatchTheme"/> (1980s LCD digital,
/// Apple Watch, or Pixel Watch — the default). The 80s digital face uses seven-segment digits
/// with an AM/PM dot, shows half-size seconds when <see cref="ClockViewBase.IsSecondsShown"/>
/// is set, and its separator flashes every half-second regardless. Honours
/// <see cref="ClockViewBase.Is24HourClock"/> and <see cref="ClockViewBase.IsUTC"/>;
/// <see cref="ClockViewBase.ClockColor"/> is the accent color. Unix time does not apply.
/// </summary>
public class WatchClock : ClockViewBase
{
    readonly WatchClockDrawable _drawable = new();

    public WatchClock()
    {
        Drawable = _drawable;
        WidthRequest = 280;
        HeightRequest = 160;

        TimeChanged += (_, _) => Refresh();
        Refresh();
    }

    public static readonly BindableProperty WatchThemeProperty = BindableProperty.Create(
        nameof(WatchTheme), typeof(WatchTheme), typeof(WatchClock), Plugin.ClockViews.WatchTheme.PixelWatch,
        propertyChanged: (b, _, _) => ((WatchClock)b).OnAppearanceChanged());

    /// <summary>The watch face style. Defaults to <see cref="WatchTheme.PixelWatch"/>.</summary>
    public WatchTheme WatchTheme
    {
        get => (WatchTheme)GetValue(WatchThemeProperty);
        set => SetValue(WatchThemeProperty, value);
    }

    public static readonly BindableProperty WatchBorderThicknessProperty = BindableProperty.Create(
        nameof(WatchBorderThickness), typeof(WatchBorderThickness), typeof(WatchClock), Plugin.ClockViews.WatchBorderThickness.Medium,
        propertyChanged: (b, _, _) => ((WatchClock)b).OnAppearanceChanged());

    /// <summary>
    /// The case/frame border thickness. Pixel size depends on the theme — the 80s digital
    /// watch uses 1/3/5px and the Apple/Pixel faces use 2/4/6px. Defaults to
    /// <see cref="WatchBorderThickness.Medium"/>.
    /// </summary>
    public WatchBorderThickness WatchBorderThickness
    {
        get => (WatchBorderThickness)GetValue(WatchBorderThicknessProperty);
        set => SetValue(WatchBorderThicknessProperty, value);
    }

    /// <summary>Reads <see cref="WatchTheme"/> on the UI thread.</summary>
    public Task<WatchTheme> GetWatchThemeAsync() => ReadAsync(() => WatchTheme);

    /// <summary>Reads <see cref="WatchBorderThickness"/> on the UI thread.</summary>
    public Task<WatchBorderThickness> GetWatchBorderThicknessAsync() => ReadAsync(() => WatchBorderThickness);

    protected override void OnAppearanceChanged() => Refresh();

    void Refresh()
    {
        var now = CurrentDateTime;
        _drawable.Time = now;
        _drawable.ShowSeconds = IsSecondsShown;
        _drawable.Is24Hour = Is24HourClock;
        // The separator flashes every half-second while running (independent of seconds display).
        _drawable.ColonLit = !IsRunning || now.Millisecond < 500;
        _drawable.Theme = WatchTheme;
        _drawable.Border = WatchBorderThickness;
        _drawable.AccentColor = ClockColor;
        _drawable.DateText = ShowDate ? CurrentDateText : null;
        _drawable.DateDigits = ShowDate ? CurrentDateDigits : null;
        Invalidate();
    }
}
