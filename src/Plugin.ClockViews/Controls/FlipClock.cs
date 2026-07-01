using System.Linq;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// A split-flap ("flip") clock. Shows hours and minutes (and seconds when
/// <see cref="ClockViewBase.IsSecondsShown"/> is set) on flip cards; when a value changes the
/// old top leaf folds down to reveal the new value, then the new bottom leaf folds into place.
/// When <see cref="ClockViewBase.Is24HourClock"/> is <c>false</c>, an extra narrow AM/PM flap
/// is shown (blank top, AM/PM in the bottom half) that flips at noon and midnight.
/// Honours <see cref="ClockViewBase.IsUnixTime"/> (one card per Unix digit) and
/// <see cref="ClockViewBase.IsUTC"/>.
/// <para><see cref="ClockViewBase.ClockColor"/> is the digit color; <see cref="CardColor"/> is
/// the card color; both are bindable.</para>
/// </summary>
public class FlipClock : ClockViewBase
{
    const uint FlipDurationMs = 350;

    readonly FlipClockDrawable _drawable = new();

    List<string> _shown = new();
    List<string> _target = new();
    List<bool> _bottomOnly = new();
    string _signature = "";   // identifies the panel layout so we can snap when it changes
    bool _initialised, _animating;

    public FlipClock()
    {
        Drawable = _drawable;
        WidthRequest = 300;
        HeightRequest = 140;

        TimeChanged += (_, _) => Refresh();
        Reset();
    }

    public static readonly BindableProperty CardColorProperty = BindableProperty.Create(
        nameof(CardColor), typeof(Color), typeof(FlipClock), Colors.White,
        propertyChanged: (b, _, _) => ((FlipClock)b).OnAppearanceChanged());

    /// <summary>The flip-card color. Defaults to white.</summary>
    public Color CardColor
    {
        get => (Color)GetValue(CardColorProperty);
        set => SetValue(CardColorProperty, value);
    }

    /// <summary>Reads <see cref="CardColor"/> on the UI thread.</summary>
    public Task<Color> GetCardColorAsync() => ReadAsync(() => CardColor);

    protected override void OnAppearanceChanged() => Reset();

    // Builds the panel values (and which are AM/PM bottom-only flaps) plus a signature
    // describing the layout, so a layout change (seconds/24-hour/Unix) forces a snap.
    (List<string> values, List<bool> bottomOnly, string signature) Compute()
    {
        var values = new List<string>();
        var bottomOnly = new List<bool>();

        if (IsUnixTime)
        {
            foreach (char c in Math.Abs(CurrentUnixTimeSeconds).ToString())
            {
                values.Add(c.ToString());
                bottomOnly.Add(false);
            }
            return (values, bottomOnly, $"unix-{values.Count}");
        }

        var t = EffectiveTime;
        int h24 = t.Hours;
        bool ampm = !Is24HourClock;

        if (ampm)
        {
            int h12 = h24 % 12;
            if (h12 == 0)
                h12 = 12;
            values.Add(h12.ToString("D2"));
        }
        else
        {
            values.Add(h24.ToString("D2"));
        }
        bottomOnly.Add(false);

        values.Add(t.Minutes.ToString("D2"));
        bottomOnly.Add(false);

        if (IsSecondsShown)
        {
            values.Add(t.Seconds.ToString("D2"));
            bottomOnly.Add(false);
        }

        if (ampm)
        {
            values.Add(h24 < 12 ? "AM" : "PM");
            bottomOnly.Add(true);
        }

        return (values, bottomOnly, $"time-{IsSecondsShown}-{ampm}");
    }

    // Snaps to the current values with no animation (initial load, or layout change).
    void Reset()
    {
        var (values, bottomOnly, signature) = Compute();
        _shown = values;
        _target = new List<string>(values);
        _bottomOnly = bottomOnly;
        _signature = signature;
        _initialised = true;
        _animating = false;
        BuildPanels(0);
    }

    void Refresh()
    {
        if (!_initialised)
        {
            Reset();
            return;
        }

        var (values, bottomOnly, signature) = Compute();

        // The panel layout changed (seconds/24-hour/Unix toggled) — snap rather than flip.
        if (signature != _signature)
        {
            Reset();
            return;
        }

        if (_animating)
            return;

        // No digit change — still rebuild so the colon keeps flashing.
        if (values.SequenceEqual(_shown))
        {
            BuildPanels(0);
            return;
        }

        _target = values;
        _bottomOnly = bottomOnly;
        _animating = true;

        var animation = new Animation(v => BuildPanels(v), 0, 1, Easing.CubicInOut);
        animation.Commit(this, "flip", 16, FlipDurationMs, finished: (_, _) =>
        {
            _shown = _target;
            _animating = false;
            BuildPanels(0);
        });
    }

    void BuildPanels(double progress)
    {
        bool isUnix = _signature.StartsWith("unix", StringComparison.Ordinal);
        bool secondsPresent = _bottomOnly.Count(b => !b) >= 3; // H, M, S all present
        bool colonLit = !IsRunning || CurrentDateTime.Millisecond < 500;

        var panels = new List<FlipPanel>();
        for (int i = 0; i < _shown.Count; i++)
        {
            string current = _shown[i];
            string next = _animating ? _target[i] : current;
            panels.Add(new FlipPanel(current, next, _animating ? progress : 0, _bottomOnly[i]));

            // Flashing colon after hours, and after minutes when seconds are shown.
            if (!isUnix && (i == 0 || (i == 1 && secondsPresent)))
                panels.Add(new FlipPanel("", "", 0, false, IsSeparator: true, ColonLit: colonLit));
        }

        _drawable.Panels = panels;
        _drawable.DigitColor = ClockColor;
        _drawable.CardColor = CardColor;
        _drawable.DateText = (ShowDate && !isUnix) ? CurrentDateText : null;
        Invalidate();
    }
}
