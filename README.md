# Plugin.ClockViews

A collection of self-drawing analog and digital **clock views for .NET MAUI**, with live ticking,
countdown timers with alarms, and a set of themed/animated faces.

Targets **.NET 10** for Android, iOS, Mac Catalyst and **Windows**, plus a plain `net10.0` target
that carries the platform-agnostic drawing/logic so it can be unit-tested without a platform head.

- **Author:** Paul F. Johnson
- **Version:** 1.0.1
- **License:** DILLIGAF (see `LICENSE.txt`)

---

## Install & initialise

Add a reference to the package, then call `UseClockViews()` in `MauiProgram` (this registers the
audio backend used for countdown alarms):

```csharp
using Plugin.ClockViews;

builder
    .UseMauiApp<App>()
    .UseClockViews();
```

The controls themselves need no registration — use them directly in XAML or code.

```xml
xmlns:clock="clr-namespace:Plugin.ClockViews;assembly=Plugin.ClockViews"
...
<clock:ClockView IsRunning="True" IsSecondsShown="True" />
```

---

## Clock views

| View | Description |
|------|-------------|
| `ClockView` | Analog clock. Date (when enabled) appears in a small window at the **5 o'clock** position, behind the hands. |
| `ValveClockView` | Nixie-tube ("valve") clock — glowing numerals in glass tubes with a flashing colon. Always 24-hour. |
| `FlipClock` | Split-flap ("flip") clock — the old leaf folds down to reveal the new value. Optional AM/PM flap. |
| `MeltingClock` | Digits melt away and the new digit forms beneath (~0.8s). |
| `DematerialiseClock` | Sci-fi dematerialise/rematerialise (`SciFiTheme`: `DrWho` flicker-fade, `StarTrek` transporter sparkle). |
| `WatchClock` | Wristwatch faces (`WatchTheme`: `EightiesDigital` 7-segment, `AppleWatch`, `PixelWatch`). |
| `MultiTimeClock` | "World" clock — local time plus up to three other time zones, each a mini clock face (`Face`) with the zone name centred above it. |

All digital clocks flash the `:` separator every half-second and show the date in **their own style**
(valve = glowing filament, flip = a flip card, melt/dematerialise = styled text, 80s watch = 7-segment
numeric, Apple/Pixel = text). The date is half the height of the time (except analog and watch).

---

## Shared properties (`ClockViewBase`)

Every clock inherits these bindable properties:

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `Time` | `TimeSpan` | `0` | Time to show when not running. |
| `IsRunning` | `bool` | `false` | Ticks live when `true`. |
| `IsSecondsShown` | `bool` | `false` | Show seconds. |
| `Is24HourClock` | `bool` | `true` | 12/24-hour (ignored by the always-24h valve clock). |
| `ClockColor` | `Color` | `Black` | Primary/accent color (meaning is view-specific). |
| `ShowDate` | `bool` | `false` | Show the date. Never shown in Unix mode. |
| `ShowAsDayThenMonth` | `bool` | `true` | `dd MMM yyyy` when true, else `MMM dd yyyy` (analog window omits the year). |
| `IsUTC` | `bool` | `false` | Show UTC instead of local time. |
| `IsUnixTime` | `bool` | `false` | Show the Unix timestamp (digital clocks only; not analog/watch/world). |
| `IsCountdownTimer` | `bool` | `false` | Count down from `CountFrom` to zero. Disabled while UTC/Unix are on. |
| `CountFrom` | `TimeSpan` | `1 min` | Countdown start. |
| `AlarmSound` | `AlarmSource?` | `null` | Sound played for 5s at zero; falls back to a generated beep. |

**Events:** `TimeChanged` (each tick / value change), `CountdownElapsed` (countdown hits zero).

**Methods:** `ResetCountdown()` restarts the countdown; supports Start/Stop via `IsRunning`
(Stop freezes the remaining time, Start resumes).

**Async getters:** every publicly-exposed value has a thread-safe async accessor that marshals to
the UI thread — e.g. `GetTimeAsync()`, `GetIsRunningAsync()`, `GetClockColorAsync()`,
`GetCurrentTimeAsync()`, `GetCountdownRemainingAsync()`. (Bindable getters stay synchronous, as MAUI
data binding requires.)

### View-specific properties

| View | Property | Type | Default |
|------|----------|------|---------|
| `ClockView` | `FaceColor`, `HandColor`, `SecondHandColor`, `DialColor` | `Color` | black / black / red / white |
| `ValveClockView` | `ValveShellColor` | `Color` | grey (filament = `ClockColor`, light red) |
| `FlipClock` | `CardColor` | `Color` | white |
| `DematerialiseClock` | `SciFiTheme` | `SciFiTheme` | `DrWho` |
| `WatchClock` | `WatchTheme` | `WatchTheme` | `PixelWatch` |
| `WatchClock` | `WatchBorderThickness` | `WatchBorderThickness` | `Medium` (digital 1/3/5px, Apple/Pixel 2/4/6px) |
| `MultiTimeClock` | `ClockTimeZone` | `string` | `""` — `;`/`,`-delimited system timezone ids (up to 3) |
| `MultiTimeClock` | `Face` | `MultiClockFace` | `Analog` — face style used for each zone |

---

## World clock (`MultiTimeClock`)

Shows the local time first, then up to three time zones from `ClockTimeZone`. Each zone is rendered
as a mini clock face of `Face` with the timezone name (e.g. "London", "Brisbane") centred above it.
Faces lay out in a 2×2 grid, except `Valve` which stacks the clocks vertically. All zones update from
the same tick. **UTC and Unix time do not apply** to this clock.

```xml
<clock:MultiTimeClock IsRunning="True"
                      Face="Analog"
                      ClockTimeZone="Europe/London;America/New_York;Asia/Tokyo" />
```

---

## Countdown & alarm

```xml
<clock:FlipClock x:Name="Timer"
                 IsCountdownTimer="True"
                 CountFrom="00:05:00"
                 AlarmSound="{x:Null}" />
```

```csharp
// Supply the alarm sound (resources / file / URL):
Timer.AlarmSound = AlarmSource.FromResources("alarm.wav");
Timer.AlarmSound = AlarmSource.FromFile("/path/to/alarm.mp3");
Timer.AlarmSound = AlarmSource.FromUrl("https://example.com/alarm.mp3");

Timer.IsRunning = true;   // start
Timer.IsRunning = false;  // stop (freezes remaining)
Timer.ResetCountdown();   // back to CountFrom
```

At zero the clock raises `CountdownElapsed` and plays `AlarmSound` for five seconds.

---

## Enums

- `SciFiTheme` — `DrWho`, `StarTrek`
- `WatchTheme` — `EightiesDigital`, `AppleWatch`, `PixelWatch`
- `WatchBorderThickness` — `Thin`, `Medium`, `Thick`
- `MultiClockFace` — `Analog`, `Valve`, `Flip`, `Melt`, `Beam`, `Watch`
- `AlarmSourceKind` — `Resources`, `File`, `Url`

---

## Solution layout

```
src/Plugin.ClockViews          The library (multi-target: net10.0 + platform TFMs)
tests/Plugin.ClockViews.Tests  NUnit tests for the platform-agnostic logic
samples/Plugin.ClockViews.Sample  MAUI sample app demonstrating every clock and option
```

## Building

```bash
dotnet build src/Plugin.ClockViews/Plugin.ClockViews.csproj   # all targets incl. Windows
dotnet test  tests/Plugin.ClockViews.Tests                    # unit tests
```

The Windows target is enabled unconditionally (`EnableWindowsTargeting`), so it restores/compiles on
any host; producing a Windows *app* still requires a Windows machine.

## Changelog

- **1.0.1** — Added world time clocks (`MultiTimeClock`).
- **1.0** — Initial release.
