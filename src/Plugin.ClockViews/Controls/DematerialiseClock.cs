using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// A digital clock whose digits dematerialise and rematerialise when they change, in a chosen
/// sci-fi style (<see cref="SciFiTheme"/>): a Doctor Who TARDIS flicker-fade, or a Star Trek
/// transporter sparkle-dissolve. Each change takes about 0.6s (0.3s out, 0.3s in). Shows hours
/// and minutes (and seconds when <see cref="ClockViewBase.IsSecondsShown"/> is set), honours
/// <see cref="ClockViewBase.Is24HourClock"/> and <see cref="ClockViewBase.IsUTC"/>.
/// <see cref="ClockViewBase.ClockColor"/> is the digit color (bindable).
/// </summary>
public class DematerialiseClock : ClockViewBase
{
    const uint EffectDurationMs = 600; // 0.3s dematerialise + 0.3s materialise

    readonly DematerialiseClockDrawable _drawable = new();

    string? _shown;
    string _target = "";
    bool _showSeconds, _isUnix, _animating;

    public DematerialiseClock()
    {
        Drawable = _drawable;
        WidthRequest = 300;
        HeightRequest = 130;

        TimeChanged += (_, _) => Refresh();
        Reset();
    }

    public static readonly BindableProperty SciFiThemeProperty = BindableProperty.Create(
        nameof(SciFiTheme), typeof(SciFiTheme), typeof(DematerialiseClock), Plugin.ClockViews.SciFiTheme.DrWho,
        propertyChanged: (b, _, _) => ((DematerialiseClock)b).OnAppearanceChanged());

    /// <summary>The dematerialisation style. Defaults to <see cref="SciFiTheme.DrWho"/>.</summary>
    public SciFiTheme SciFiTheme
    {
        get => (SciFiTheme)GetValue(SciFiThemeProperty);
        set => SetValue(SciFiThemeProperty, value);
    }

    /// <summary>Reads <see cref="SciFiTheme"/> on the UI thread.</summary>
    public Task<SciFiTheme> GetSciFiThemeAsync() => ReadAsync(() => SciFiTheme);

    protected override void OnAppearanceChanged() => Reset();

    string BuildDigits(out bool showSeconds, out bool isUnix)
    {
        isUnix = IsUnixTime;
        if (isUnix)
        {
            showSeconds = false;
            return Math.Abs(CurrentUnixTimeSeconds).ToString();
        }

        var t = EffectiveTime;
        int h = t.Hours;
        if (!Is24HourClock)
        {
            h %= 12;
            if (h == 0)
                h = 12;
        }

        showSeconds = IsSecondsShown;
        string s = h.ToString("D2") + t.Minutes.ToString("D2");
        if (showSeconds)
            s += t.Seconds.ToString("D2");
        return s;
    }

    void Reset()
    {
        _shown = _target = BuildDigits(out _showSeconds, out _isUnix);
        _animating = false;
        BuildCells(0);
    }

    void Refresh()
    {
        if (_shown is null)
        {
            Reset();
            return;
        }

        string target = BuildDigits(out bool showSeconds, out bool isUnix);
        if (showSeconds != _showSeconds || isUnix != _isUnix || target.Length != _shown.Length)
        {
            Reset();
            return;
        }

        if (_animating)
            return;

        // No digit change — still rebuild so the colon keeps flashing.
        if (target == _shown)
        {
            BuildCells(0);
            return;
        }

        _target = target;
        _animating = true;

        var animation = new Animation(v => BuildCells(v), 0, 1, Easing.Linear);
        animation.Commit(this, "dematerialise", 16, EffectDurationMs, finished: (_, _) =>
        {
            _shown = _target;
            _animating = false;
            BuildCells(0);
        });
    }

    void BuildCells(double progress)
    {
        var cells = new List<DematerialiseCell>();
        string shown = _shown!;
        string colon = (!IsRunning || CurrentDateTime.Millisecond < 500) ? ":" : " ";
        for (int i = 0; i < shown.Length; i++)
        {
            char current = shown[i];
            char next = _animating ? _target[i] : current;
            cells.Add(new DematerialiseCell(current.ToString(), next.ToString(), _animating ? progress : 0, false));

            if (!_isUnix && (i == 1 || (_showSeconds && i == 3)))
                cells.Add(new DematerialiseCell(colon, colon, 0, true));
        }

        _drawable.Cells = cells;
        _drawable.DigitColor = ClockColor;
        _drawable.Theme = SciFiTheme;
        _drawable.DateText = (ShowDate && !_isUnix) ? CurrentDateText : null;
        Invalidate();
    }
}
