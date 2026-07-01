using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// A self-drawing analog clock. Set <see cref="ClockViewBase.Time"/> for a fixed time,
/// or set <see cref="ClockViewBase.IsRunning"/> to <c>true</c> to tick live.
/// </summary>
public class ClockView : ClockViewBase
{
    readonly AnalogClockDrawable _clock = new();

    public ClockView()
    {
        Drawable = _clock;
        HeightRequest = 200;
        WidthRequest = 200;
        TimeChanged += OnTimeChanged;
        _clock.Time = EffectiveTime;
        _clock.ShowSecondHand = IsSecondsShown;
    }

    public static readonly BindableProperty FaceColorProperty = BindableProperty.Create(
        nameof(FaceColor), typeof(Color), typeof(ClockView), Colors.Black,
        propertyChanged: (b, _, n) => ((ClockView)b).Apply(c => c.FaceColor = (Color)n));

    /// <summary>The color of the dial outline and hour ticks.</summary>
    public Color FaceColor
    {
        get => (Color)GetValue(FaceColorProperty);
        set => SetValue(FaceColorProperty, value);
    }

    public static readonly BindableProperty HandColorProperty = BindableProperty.Create(
        nameof(HandColor), typeof(Color), typeof(ClockView), Colors.Black,
        propertyChanged: (b, _, n) => ((ClockView)b).Apply(c => c.HandColor = (Color)n));

    /// <summary>The color of the hour and minute hands.</summary>
    public Color HandColor
    {
        get => (Color)GetValue(HandColorProperty);
        set => SetValue(HandColorProperty, value);
    }

    public static readonly BindableProperty SecondHandColorProperty = BindableProperty.Create(
        nameof(SecondHandColor), typeof(Color), typeof(ClockView), Colors.Red,
        propertyChanged: (b, _, n) => ((ClockView)b).Apply(c => c.SecondHandColor = (Color)n));

    /// <summary>The color of the second hand.</summary>
    public Color SecondHandColor
    {
        get => (Color)GetValue(SecondHandColorProperty);
        set => SetValue(SecondHandColorProperty, value);
    }

    public static readonly BindableProperty DialColorProperty = BindableProperty.Create(
        nameof(DialColor), typeof(Color), typeof(ClockView), Colors.White,
        propertyChanged: (b, _, n) => ((ClockView)b).Apply(c => c.BackgroundColor = (Color)n));

    /// <summary>The dial background fill.</summary>
    public Color DialColor
    {
        get => (Color)GetValue(DialColorProperty);
        set => SetValue(DialColorProperty, value);
    }

    /// <summary>Reads <see cref="FaceColor"/> on the UI thread.</summary>
    public Task<Color> GetFaceColorAsync() => ReadAsync(() => FaceColor);

    /// <summary>Reads <see cref="HandColor"/> on the UI thread.</summary>
    public Task<Color> GetHandColorAsync() => ReadAsync(() => HandColor);

    /// <summary>Reads <see cref="SecondHandColor"/> on the UI thread.</summary>
    public Task<Color> GetSecondHandColorAsync() => ReadAsync(() => SecondHandColor);

    /// <summary>Reads <see cref="DialColor"/> on the UI thread.</summary>
    public Task<Color> GetDialColorAsync() => ReadAsync(() => DialColor);

    void Apply(Action<AnalogClockDrawable> mutate)
    {
        mutate(_clock);
        Invalidate();
    }

    void OnTimeChanged(object? sender, ClockTimeChangedEventArgs e)
    {
        _clock.Time = e.DateTime.TimeOfDay;
        _clock.DateText = ShowDate ? CurrentDateWindowText : null;
        Invalidate();
    }

    protected override void OnAppearanceChanged()
    {
        _clock.ShowSecondHand = IsSecondsShown;
        _clock.DateText = ShowDate ? CurrentDateWindowText : null;
        Invalidate();
    }
}
