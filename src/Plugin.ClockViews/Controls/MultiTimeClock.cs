using Microsoft.Maui.Controls;

namespace Plugin.ClockViews;

/// <summary>
/// A multi-timezone ("world") clock: shows the local time plus up to three other time zones, each
/// rendered as a mini clock face of <see cref="Face"/> with the timezone name centred above it.
/// Faces are laid out 2×2, except the valve face which stacks vertically. The additional zones come
/// from <see cref="ClockTimeZone"/> — a delimited list of system timezone ids (e.g.
/// <c>"Europe/London;Australia/Brisbane;Asia/Tokyo"</c>). All clocks update from the same tick.
/// Unix time and UTC do not apply. Honours <see cref="ClockViewBase.IsSecondsShown"/> and
/// <see cref="ClockViewBase.Is24HourClock"/>; <see cref="ClockViewBase.ClockColor"/> is the color.
/// </summary>
public class MultiTimeClock : ClockViewBase
{
    const int MaxAdditionalZones = 3; // + local = up to 4 clocks

    readonly MultiTimeClockDrawable _drawable = new();

    public MultiTimeClock()
    {
        Drawable = _drawable;
        WidthRequest = 360;
        HeightRequest = 300;

        TimeChanged += (_, _) => Refresh();
        Refresh();
    }

    public static readonly BindableProperty ClockTimeZoneProperty = BindableProperty.Create(
        nameof(ClockTimeZone), typeof(string), typeof(MultiTimeClock), string.Empty,
        propertyChanged: (b, _, _) => ((MultiTimeClock)b).OnAppearanceChanged());

    /// <summary>
    /// The additional time zones to show, as a <c>;</c>- or <c>,</c>-delimited list of system
    /// timezone ids (up to three). The local zone is always shown first.
    /// </summary>
    public string ClockTimeZone
    {
        get => (string)GetValue(ClockTimeZoneProperty);
        set => SetValue(ClockTimeZoneProperty, value);
    }

    public static readonly BindableProperty FaceProperty = BindableProperty.Create(
        nameof(Face), typeof(MultiClockFace), typeof(MultiTimeClock), MultiClockFace.Analog,
        propertyChanged: (b, _, _) => ((MultiTimeClock)b).OnAppearanceChanged());

    /// <summary>The clock face style used for each zone. Defaults to <see cref="MultiClockFace.Analog"/>.</summary>
    public MultiClockFace Face
    {
        get => (MultiClockFace)GetValue(FaceProperty);
        set => SetValue(FaceProperty, value);
    }

    /// <summary>Reads <see cref="ClockTimeZone"/> on the UI thread.</summary>
    public Task<string> GetClockTimeZoneAsync() => ReadAsync(() => ClockTimeZone);

    /// <summary>Reads <see cref="Face"/> on the UI thread.</summary>
    public Task<MultiClockFace> GetFaceAsync() => ReadAsync(() => Face);

    protected override void OnAppearanceChanged() => Refresh();

    void Refresh()
    {
        var rows = new List<MultiTimeRow>
        {
            // The first clock is always local.
            new(FriendlyName(TimeZoneInfo.Local.Id), DateTime.Now),
        };

        var utcNow = DateTime.UtcNow;
        int count = 0;
        foreach (var id in SplitZones())
        {
            if (count++ >= MaxAdditionalZones)
                break;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                rows.Add(new MultiTimeRow(FriendlyName(id), TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz)));
            }
            catch (TimeZoneNotFoundException) { /* skip unknown zone */ }
            catch (InvalidTimeZoneException) { /* skip malformed zone */ }
        }

        _drawable.Rows = rows;
        _drawable.Face = Face;
        _drawable.IsSecondsShown = IsSecondsShown;
        _drawable.Is24Hour = Is24HourClock;
        _drawable.ColonLit = !IsRunning || DateTime.Now.Millisecond < 500;
        _drawable.DigitColor = ClockColor;
        Invalidate();
    }

    IEnumerable<string> SplitZones()
        => string.IsNullOrWhiteSpace(ClockTimeZone)
            ? Array.Empty<string>()
            : ClockTimeZone.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // "Australia/Brisbane" -> "Brisbane"; "Europe/London" -> "London".
    static string FriendlyName(string id)
    {
        int slash = id.LastIndexOf('/');
        string name = slash >= 0 ? id[(slash + 1)..] : id;
        return name.Replace('_', ' ');
    }
}
