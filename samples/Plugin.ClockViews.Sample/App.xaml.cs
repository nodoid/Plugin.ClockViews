using Microsoft.Extensions.DependencyInjection;

namespace Plugin.ClockViews.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());

		// Open large enough (on desktop) to show every option including the countdown buttons.
		const double width = 540;
		const double height = 1150;
		window.Width = width;
		window.Height = height;
		window.MinimumWidth = 420;
		window.MinimumHeight = 700;

		return window;
	}
}