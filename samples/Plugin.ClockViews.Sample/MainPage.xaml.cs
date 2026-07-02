namespace Plugin.ClockViews.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		// Set the initial selection and visibility explicitly, after the tree is built,
		// so it doesn't depend on load-time CheckedChanged ordering.
		AnalogOption.IsChecked = true;
		WatchPixelOption.IsChecked = true;  // default watch theme
		UpdateClockVisibility();
		UpdateOptionAvailability();

		// The alarm sound is user-supplied; here we use a bundled Raw asset.
		var alarm = AlarmSource.FromResources("alarm.wav");
		foreach (var clock in AllClocks)
			clock.AlarmSound = alarm;
	}

	IEnumerable<ClockViewBase> AllClocks => new ClockViewBase[] { AnalogClock, ValveClock, FlipClock, MeltClock, DematClock, WatchClock, WorldClock };

	// Clocks that don't support Unix time.
	bool UnixUnavailable => AnalogOption.IsChecked || WatchOption.IsChecked || WorldOption.IsChecked;

	// Clocks that don't support UTC.
	bool UtcUnavailable => WorldOption.IsChecked;

	void OnClockTypeChanged(object? sender, CheckedChangedEventArgs e)
	{
		UpdateClockVisibility();
		UpdateOptionAvailability();
	}

	void UpdateClockVisibility()
	{
		// May fire during XAML load before the named clocks exist — guard against that.
		if (AnalogClock is null || ValveClock is null || FlipClock is null || MeltClock is null || DematClock is null || WatchClock is null || WorldClock is null)
			return;

		AnalogClock.IsVisible = AnalogOption.IsChecked;
		ValveClock.IsVisible = ValveOption.IsChecked;
		FlipClock.IsVisible = FlipOption.IsChecked;
		MeltClock.IsVisible = MeltOption.IsChecked;
		DematClock.IsVisible = BeamOption.IsChecked;
		WatchClock.IsVisible = WatchOption.IsChecked;
		WorldClock.IsVisible = WorldOption.IsChecked;

		// World shows the general options but not UTC / Unix / Countdown.
		bool world = WorldOption.IsChecked;
		UtcLabel.IsVisible = !world;
		UtcSwitch.IsVisible = !world;
		UnixLabel.IsVisible = !world;
		UnixSwitch.IsVisible = !world;
		CountdownSection.IsVisible = !world;

		// Theme selectors only apply to their clock.
		ThemePanel.IsVisible = BeamOption.IsChecked;
		WatchThemePanel.IsVisible = WatchOption.IsChecked;
	}

	void OnThemeChanged(object? sender, CheckedChangedEventArgs e)
	{
		if (!e.Value)
			return;
		DematClock.SciFiTheme = StarTrekOption.IsChecked ? SciFiTheme.StarTrek : SciFiTheme.DrWho;
	}

	void OnWatchThemeChanged(object? sender, CheckedChangedEventArgs e)
	{
		if (!e.Value)
			return;
		if (Watch80sOption.IsChecked)
			WatchClock.WatchTheme = WatchTheme.EightiesDigital;
		else if (WatchAppleOption.IsChecked)
			WatchClock.WatchTheme = WatchTheme.AppleWatch;
		else
			WatchClock.WatchTheme = WatchTheme.PixelWatch;
	}

	void OnRunningToggled(object? sender, ToggledEventArgs e)
	{
		// The Live switch drives the clocks only when not in countdown mode
		// (countdown uses the Start/Stop buttons instead).
		if (CountdownSwitch.IsToggled)
			return;

		foreach (var clock in AllClocks)
			clock.IsRunning = e.Value;
	}

	void OnSecondsToggled(object? sender, ToggledEventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.IsSecondsShown = e.Value;
	}

	void On24HourToggled(object? sender, ToggledEventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.Is24HourClock = e.Value;
	}

	void OnShowDateToggled(object? sender, ToggledEventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.ShowDate = e.Value;
	}

	void OnDayThenMonthToggled(object? sender, ToggledEventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.ShowAsDayThenMonth = e.Value;
	}

	void OnUtcToggled(object? sender, ToggledEventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.IsUTC = e.Value;

		// Countdown is disabled while UTC is selected.
		if (e.Value && CountdownSwitch.IsToggled)
			CountdownSwitch.IsToggled = false;

		UpdateOptionAvailability();
	}

	void OnUnixToggled(object? sender, ToggledEventArgs e)
	{
		// Unix time applies to every clock except analog and watch.
		foreach (var clock in AllClocks)
			if (clock != AnalogClock && clock != WatchClock)
				clock.IsUnixTime = e.Value;

		// Countdown is disabled while Unix time is selected.
		if (e.Value && CountdownSwitch.IsToggled)
			CountdownSwitch.IsToggled = false;

		UpdateOptionAvailability();
	}

	void OnCountdownToggled(object? sender, ToggledEventArgs e)
	{
		if (e.Value)
		{
			// Countdown is mutually exclusive with UTC/Unix time.
			UtcSwitch.IsToggled = false;
			UnixSwitch.IsToggled = false;

			foreach (var clock in AllClocks)
			{
				clock.IsRunning = false;          // start stopped, showing the start time
				clock.IsCountdownTimer = true;
				clock.ResetCountdown();           // show CountFrom
			}
		}
		else
		{
			foreach (var clock in AllClocks)
			{
				clock.IsCountdownTimer = false;
				clock.IsRunning = LiveSwitch.IsToggled; // restore normal running state
			}
		}

		UpdateOptionAvailability();
	}

	void OnStartClicked(object? sender, EventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.IsRunning = true;
		UpdateOptionAvailability();
	}

	void OnStopClicked(object? sender, EventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.IsRunning = false;
		UpdateOptionAvailability();
	}

	void OnResetClicked(object? sender, EventArgs e)
	{
		foreach (var clock in AllClocks)
			clock.ResetCountdown();
		UpdateOptionAvailability();
	}

	// Keeps option enabled-states consistent with the current mode.
	void UpdateOptionAvailability()
	{
		if (CountdownSwitch is null)
			return;

		bool countdown = CountdownSwitch.IsToggled;
		bool running = AnalogClock.IsRunning;

		// Countdown is disabled when UTC or Unix time is selected, and vice versa.
		CountdownSwitch.IsEnabled = !UtcSwitch.IsToggled && !UnixSwitch.IsToggled;
		UtcSwitch.IsEnabled = !countdown && !UtcUnavailable;
		UnixLabel.IsEnabled = !UnixUnavailable && !countdown;
		UnixSwitch.IsEnabled = !UnixUnavailable && !countdown;

		// The Live switch is only used outside countdown mode.
		LiveLabel.IsEnabled = !countdown;
		LiveSwitch.IsEnabled = !countdown;

		// Start/Stop/Reset only apply in countdown mode; Reset requires a stopped timer.
		StartButton.IsEnabled = countdown && !running;
		StopButton.IsEnabled = countdown && running;
		ResetButton.IsEnabled = countdown && !running;
	}
}
