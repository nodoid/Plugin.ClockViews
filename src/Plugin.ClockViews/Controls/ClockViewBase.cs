using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// Base class for all clock views. Owns the shared time/ticking machinery, the common
/// display options (<see cref="IsSecondsShown"/>, <see cref="Is24HourClock"/>,
/// <see cref="ClockColor"/>), and the <see cref="TimeChanged"/> event that each derived
/// view subscribes to in order to re-render whenever the time advances.
/// </summary>
public abstract class ClockViewBase : GraphicsView
{
    IDispatcherTimer? _timer;

    /// <summary>
    /// Raised whenever the time to display changes: on each tick while <see cref="IsRunning"/>,
    /// and whenever <see cref="Time"/> or <see cref="IsRunning"/> changes. Derived views
    /// subscribe to this to update their rendering.
    /// </summary>
    public event EventHandler<ClockTimeChangedEventArgs>? TimeChanged;

    public static readonly BindableProperty TimeProperty = BindableProperty.Create(
        nameof(Time), typeof(TimeSpan), typeof(ClockViewBase), TimeSpan.Zero,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).RaiseTimeChanged());

    /// <summary>The time to display. Ignored while <see cref="IsRunning"/> is true.</summary>
    public TimeSpan Time
    {
        get => (TimeSpan)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
        nameof(IsRunning), typeof(bool), typeof(ClockViewBase), false,
        propertyChanged: (b, _, n) => ((ClockViewBase)b).OnIsRunningChanged((bool)n));

    /// <summary>When true, the clock ticks live in real time, ignoring <see cref="Time"/>.</summary>
    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public static readonly BindableProperty IsSecondsShownProperty = BindableProperty.Create(
        nameof(IsSecondsShown), typeof(bool), typeof(ClockViewBase), false,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).OnAppearanceChanged());

    /// <summary>Whether seconds are displayed (second hand / seconds valves). Defaults to <c>false</c>.</summary>
    public bool IsSecondsShown
    {
        get => (bool)GetValue(IsSecondsShownProperty);
        set => SetValue(IsSecondsShownProperty, value);
    }

    public static readonly BindableProperty Is24HourClockProperty = BindableProperty.Create(
        nameof(Is24HourClock), typeof(bool), typeof(ClockViewBase), true,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).OnAppearanceChanged());

    /// <summary>
    /// Whether the clock uses a 24-hour representation. Defaults to <c>true</c>.
    /// Note: some views (e.g. <see cref="ValveClockView"/>) are always 24-hour and ignore this.
    /// </summary>
    public bool Is24HourClock
    {
        get => (bool)GetValue(Is24HourClockProperty);
        set => SetValue(Is24HourClockProperty, value);
    }

    public static readonly BindableProperty ShowDateProperty = BindableProperty.Create(
        nameof(ShowDate), typeof(bool), typeof(ClockViewBase), false,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).OnAppearanceChanged());

    /// <summary>
    /// When true, the date (<c>dd MMM</c>) is shown — under the time for digital clocks, or in a
    /// small window at the 4 o'clock position for the analog clock. Never shown in Unix time mode.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ShowDate
    {
        get => (bool)GetValue(ShowDateProperty);
        set => SetValue(ShowDateProperty, value);
    }

    public static readonly BindableProperty ShowAsDayThenMonthProperty = BindableProperty.Create(
        nameof(ShowAsDayThenMonth), typeof(bool), typeof(ClockViewBase), true,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).OnAppearanceChanged());

    /// <summary>
    /// Date ordering: <c>true</c> (default) formats as <c>dd MMM yyyy</c>; <c>false</c> as
    /// <c>MMM dd yyyy</c>. The analog date window omits the year.
    /// </summary>
    public bool ShowAsDayThenMonth
    {
        get => (bool)GetValue(ShowAsDayThenMonthProperty);
        set => SetValue(ShowAsDayThenMonthProperty, value);
    }

    /// <summary>The current date for digital clocks (<c>dd MMM yyyy</c> / <c>MMM dd yyyy</c>).</summary>
    protected string CurrentDateText => CurrentDateTime.ToString(
        ShowAsDayThenMonth ? "dd MMM yyyy" : "MMM dd yyyy", CultureInfo.InvariantCulture);

    /// <summary>The current date for the analog window (no year: <c>dd MMM</c> / <c>MMM dd</c>).</summary>
    protected string CurrentDateWindowText => CurrentDateTime.ToString(
        ShowAsDayThenMonth ? "dd MMM" : "MMM dd", CultureInfo.InvariantCulture);

    /// <summary>The current date as digits (<c>ddMMyyyy</c> / <c>MMddyyyy</c>) for seven-segment styles.</summary>
    protected string CurrentDateDigits => CurrentDateTime.ToString(
        ShowAsDayThenMonth ? "ddMMyyyy" : "MMddyyyy", CultureInfo.InvariantCulture);

    public static readonly BindableProperty ClockColorProperty = BindableProperty.Create(
        nameof(ClockColor), typeof(Color), typeof(ClockViewBase), Colors.Black,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).OnAppearanceChanged());

    /// <summary>
    /// The primary color of the clock. Its exact meaning is view-specific — for
    /// <see cref="ValveClockView"/> it is the filament (digit) color.
    /// </summary>
    public Color ClockColor
    {
        get => (Color)GetValue(ClockColorProperty);
        set => SetValue(ClockColorProperty, value);
    }

    public static readonly BindableProperty IsUnixTimeProperty = BindableProperty.Create(
        nameof(IsUnixTime), typeof(bool), typeof(ClockViewBase), false,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).RaiseTimeChanged());

    /// <summary>
    /// When true, the clock shows Unix time (seconds since 1970-01-01 UTC) instead of a
    /// clock time. Defaults to <c>false</c>. Views that cannot represent it (e.g. the analog
    /// <see cref="ClockView"/>) ignore this.
    /// </summary>
    public bool IsUnixTime
    {
        get => (bool)GetValue(IsUnixTimeProperty);
        set => SetValue(IsUnixTimeProperty, value);
    }

    public static readonly BindableProperty IsUTCProperty = BindableProperty.Create(
        nameof(IsUTC), typeof(bool), typeof(ClockViewBase), false,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).RaiseTimeChanged());

    /// <summary>When true, the live time is displayed in UTC rather than local time. Defaults to <c>false</c>.</summary>
    public bool IsUTC
    {
        get => (bool)GetValue(IsUTCProperty);
        set => SetValue(IsUTCProperty, value);
    }

    public static readonly BindableProperty IsCountdownTimerProperty = BindableProperty.Create(
        nameof(IsCountdownTimer), typeof(bool), typeof(ClockViewBase), false,
        propertyChanged: (b, _, _) => ((ClockViewBase)b).ResetCountdown());

    /// <summary>
    /// When true, the clock counts down from <see cref="CountFrom"/> to zero instead of showing
    /// the time of day. On reaching zero it raises <see cref="CountdownElapsed"/> and plays
    /// <see cref="AlarmSound"/> for five seconds. Defaults to <c>false</c>.
    /// </summary>
    public bool IsCountdownTimer
    {
        get => (bool)GetValue(IsCountdownTimerProperty);
        set => SetValue(IsCountdownTimerProperty, value);
    }

    public static readonly BindableProperty CountFromProperty = BindableProperty.Create(
        nameof(CountFrom), typeof(TimeSpan), typeof(ClockViewBase), TimeSpan.FromMinutes(1),
        propertyChanged: (b, _, _) => ((ClockViewBase)b).ResetCountdown());

    /// <summary>The starting duration for <see cref="IsCountdownTimer"/>. Defaults to one minute.</summary>
    public TimeSpan CountFrom
    {
        get => (TimeSpan)GetValue(CountFromProperty);
        set => SetValue(CountFromProperty, value);
    }

    public static readonly BindableProperty AlarmSoundProperty = BindableProperty.Create(
        nameof(AlarmSound), typeof(AlarmSource), typeof(ClockViewBase), null);

    /// <summary>
    /// The sound played for five seconds when a countdown reaches zero. Supply one via
    /// <see cref="AlarmSource.FromResources"/>, <see cref="AlarmSource.FromFile"/>, or
    /// <see cref="AlarmSource.FromUrl"/>. When <c>null</c>, a generated beep is used.
    /// </summary>
    public AlarmSource? AlarmSound
    {
        get => (AlarmSource?)GetValue(AlarmSoundProperty);
        set => SetValue(AlarmSoundProperty, value);
    }

    /// <summary>Raised when an <see cref="IsCountdownTimer"/> countdown reaches zero.</summary>
    public event EventHandler? CountdownElapsed;

    DateTime _countdownDeadline;              // when running, remaining = deadline - now
    TimeSpan _countdownFrozen = TimeSpan.Zero; // remaining while stopped (supports pause/resume)
    bool _alarmFired;

    /// <summary>The time the clock should currently render: the live time while running, otherwise <see cref="Time"/>.</summary>
    protected TimeSpan EffectiveTime => CurrentDateTime.TimeOfDay;

    /// <summary>
    /// Whether the countdown is actually in effect. Countdown is disabled while
    /// <see cref="IsUTC"/> or <see cref="IsUnixTime"/> is selected (those take precedence).
    /// </summary>
    protected bool CountdownActive => IsCountdownTimer && !IsUTC && !IsUnixTime;

    /// <summary>
    /// The current instant the clock should render: the countdown remaining when
    /// <see cref="CountdownActive"/>; otherwise live now (UTC if <see cref="IsUTC"/>) while
    /// running, or today's date with the fixed <see cref="Time"/>.
    /// </summary>
    protected DateTime CurrentDateTime => CountdownActive
        ? DateTime.Today + CountdownRemaining
        : IsRunning
            ? (IsUTC ? DateTime.UtcNow : DateTime.Now)
            : DateTime.Today + Time;

    /// <summary>The remaining countdown time (clamped at zero). Frozen while stopped so it can resume.</summary>
    protected TimeSpan CountdownRemaining
    {
        get
        {
            if (!IsRunning)
                return _countdownFrozen;
            var remaining = _countdownDeadline - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    /// <summary>Restarts the countdown from <see cref="CountFrom"/> (keeps the running/stopped state).</summary>
    public void ResetCountdown()
    {
        _countdownFrozen = CountFrom;
        _alarmFired = false;
        if (IsRunning)
            _countdownDeadline = DateTime.UtcNow + CountFrom;
        RaiseTimeChanged();
    }

    /// <summary>The <see cref="CurrentDateTime"/> as an absolute Unix timestamp in seconds.</summary>
    protected long CurrentUnixTimeSeconds => new DateTimeOffset(CurrentDateTime).ToUnixTimeSeconds();

    // --- Async, thread-safe getters ---------------------------------------------
    // Bindable property getters are synchronous (data binding requires it). These async
    // accessors let callers read the current values safely from a background thread by
    // marshalling onto the control's UI dispatcher when required.

    /// <summary>Reads <see cref="Time"/> on the UI thread.</summary>
    public Task<TimeSpan> GetTimeAsync() => ReadAsync(() => Time);

    /// <summary>Reads <see cref="IsRunning"/> on the UI thread.</summary>
    public Task<bool> GetIsRunningAsync() => ReadAsync(() => IsRunning);

    /// <summary>Reads <see cref="IsSecondsShown"/> on the UI thread.</summary>
    public Task<bool> GetIsSecondsShownAsync() => ReadAsync(() => IsSecondsShown);

    /// <summary>Reads <see cref="Is24HourClock"/> on the UI thread.</summary>
    public Task<bool> GetIs24HourClockAsync() => ReadAsync(() => Is24HourClock);

    /// <summary>Reads <see cref="ClockColor"/> on the UI thread.</summary>
    public Task<Color> GetClockColorAsync() => ReadAsync(() => ClockColor);

    /// <summary>Reads <see cref="ShowDate"/> on the UI thread.</summary>
    public Task<bool> GetShowDateAsync() => ReadAsync(() => ShowDate);

    /// <summary>Reads <see cref="IsUnixTime"/> on the UI thread.</summary>
    public Task<bool> GetIsUnixTimeAsync() => ReadAsync(() => IsUnixTime);

    /// <summary>Reads <see cref="IsUTC"/> on the UI thread.</summary>
    public Task<bool> GetIsUTCAsync() => ReadAsync(() => IsUTC);

    /// <summary>Reads <see cref="IsCountdownTimer"/> on the UI thread.</summary>
    public Task<bool> GetIsCountdownTimerAsync() => ReadAsync(() => IsCountdownTimer);

    /// <summary>Reads <see cref="CountFrom"/> on the UI thread.</summary>
    public Task<TimeSpan> GetCountFromAsync() => ReadAsync(() => CountFrom);

    /// <summary>Reads <see cref="AlarmSound"/> on the UI thread.</summary>
    public Task<AlarmSource?> GetAlarmSoundAsync() => ReadAsync(() => AlarmSound);

    /// <summary>Reads the current instant the clock is displaying, on the UI thread.</summary>
    public Task<DateTime> GetCurrentTimeAsync() => ReadAsync(() => CurrentDateTime);

    /// <summary>Reads the current countdown remaining, on the UI thread.</summary>
    public Task<TimeSpan> GetCountdownRemainingAsync() => ReadAsync(() => CountdownRemaining);

    /// <summary>Reads a value on the control's UI dispatcher, or inline if already on it.</summary>
    protected Task<T> ReadAsync<T>(Func<T> read)
    {
        var dispatcher = Dispatcher;
        if (dispatcher is null || !dispatcher.IsDispatchRequired)
            return Task.FromResult(read());
        return dispatcher.DispatchAsync(read);
    }

    /// <summary>
    /// Called when a non-time property affecting the rendered output changes
    /// (seconds visibility, colors, 24-hour mode). The default simply invalidates;
    /// derived views override to also push appearance state into their drawable.
    /// </summary>
    protected virtual void OnAppearanceChanged() => Invalidate();

    void RaiseTimeChanged()
        => TimeChanged?.Invoke(this, new ClockTimeChangedEventArgs(CurrentDateTime));

    void OnIsRunningChanged(bool running)
    {
        if (running)
        {
            // Resume the countdown from wherever it was frozen.
            _countdownDeadline = DateTime.UtcNow + _countdownFrozen;
            if (_countdownFrozen > TimeSpan.Zero)
                _alarmFired = false;
            StartTimer();
        }
        else
        {
            // Freeze the remaining time so a later Start resumes from here.
            var remaining = _countdownDeadline - DateTime.UtcNow;
            _countdownFrozen = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            StopTimer();
        }
        RaiseTimeChanged();
    }

    void OnTick()
    {
        if (IsCountdownTimer && IsRunning && !_alarmFired && CountdownRemaining <= TimeSpan.Zero)
        {
            _alarmFired = true;
            CountdownElapsed?.Invoke(this, EventArgs.Empty);
            AlarmPlayer.Play(AlarmSound, TimeSpan.FromSeconds(5));
        }

        RaiseTimeChanged();
    }

    void StartTimer()
    {
        StopTimer();
        var dispatcher = Dispatcher;
        if (dispatcher is null)
            return;

        _timer = dispatcher.CreateTimer();
        // 100ms keeps the second hand smooth and the valve colon blink crisp at 0.5s.
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
    }

    void StopTimer()
    {
        if (_timer is null)
            return;
        _timer.Stop();
        _timer = null;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        // Manage the timer with the control's presence in the visual tree.
        if (Handler is null)
            StopTimer();
        else if (IsRunning)
            StartTimer();
    }
}
