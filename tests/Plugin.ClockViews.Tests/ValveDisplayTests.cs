using Plugin.ClockViews;

namespace Plugin.ClockViews.Tests;

[TestFixture]
public class ValveDisplayTests
{
    [Test]
    public void Digits_WithoutSeconds_ReturnsFourHourMinuteDigits()
    {
        var digits = ValveDisplay.Digits(new TimeSpan(9, 5, 30), includeSeconds: false);
        Assert.That(digits, Is.EqualTo(new[] { 0, 9, 0, 5 }));
    }

    [Test]
    public void Digits_WithSeconds_ReturnsSixDigits()
    {
        var digits = ValveDisplay.Digits(new TimeSpan(13, 5, 9), includeSeconds: true);
        Assert.That(digits, Is.EqualTo(new[] { 1, 3, 0, 5, 0, 9 }));
    }

    [Test]
    public void Digits_IsAlways24Hour()
    {
        // 13:00 stays 13, never converts to 1 PM.
        var digits = ValveDisplay.Digits(new TimeSpan(13, 0, 0), includeSeconds: false);
        Assert.That(digits, Is.EqualTo(new[] { 1, 3, 0, 0 }));
    }

    [Test]
    public void Digits_Midnight_IsAllZeros()
    {
        var digits = ValveDisplay.Digits(TimeSpan.Zero, includeSeconds: true);
        Assert.That(digits, Is.EqualTo(new[] { 0, 0, 0, 0, 0, 0 }));
    }

    [Test]
    public void Digits_WrapsPastTwentyFourHours()
    {
        // 25:30 -> 01:30
        var digits = ValveDisplay.Digits(new TimeSpan(25, 30, 0), includeSeconds: false);
        Assert.That(digits, Is.EqualTo(new[] { 0, 1, 3, 0 }));
    }

    [Test]
    public void UnixDigits_SplitsEachDecimalDigit()
    {
        Assert.That(ValveDisplay.UnixDigits(1719849600), Is.EqualTo(new[] { 1, 7, 1, 9, 8, 4, 9, 6, 0, 0 }));
    }

    [Test]
    public void UnixDigits_Zero_IsSingleZero()
    {
        Assert.That(ValveDisplay.UnixDigits(0), Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void UnixDigits_Negative_UsesMagnitude()
    {
        Assert.That(ValveDisplay.UnixDigits(-42), Is.EqualTo(new[] { 4, 2 }));
    }
}
