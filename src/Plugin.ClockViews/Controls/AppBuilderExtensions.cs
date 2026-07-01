using Microsoft.Maui.Hosting;
using Plugin.Maui.Audio;

namespace Plugin.ClockViews;

/// <summary>
/// <see cref="MauiAppBuilder"/> extensions for initialising Plugin.ClockViews.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Registers everything Plugin.ClockViews needs. Call this from <c>MauiProgram</c>:
    /// <code>builder.UseMauiApp&lt;App&gt;().UseClockViews();</code>
    /// This wires up the audio backend used to play countdown alarms. The clock views
    /// themselves need no registration and can be used directly in XAML or code.
    /// </summary>
    public static MauiAppBuilder UseClockViews(this MauiAppBuilder builder)
    {
        // Register the audio manager so countdown alarms can play on every platform.
        builder.AddAudio();
        return builder;
    }
}
