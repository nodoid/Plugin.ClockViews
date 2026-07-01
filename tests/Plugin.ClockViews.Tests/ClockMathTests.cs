using Plugin.ClockViews;

namespace Plugin.ClockViews.Tests;

[TestFixture]
public class ClockMathTests
{
    const double Tolerance = 1e-6;

    [Test]
    public void HourAngle_AtTwelve_IsZero()
    {
        Assert.That(ClockMath.HourAngle(TimeSpan.Zero), Is.EqualTo(0).Within(Tolerance));
    }

    [Test]
    public void HourAngle_AtThree_IsNinety()
    {
        Assert.That(ClockMath.HourAngle(TimeSpan.FromHours(3)), Is.EqualTo(90).Within(Tolerance));
    }

    [Test]
    public void HourAngle_WrapsAtTwelveHours()
    {
        // 15:00 should display the same as 3:00.
        Assert.That(ClockMath.HourAngle(TimeSpan.FromHours(15)), Is.EqualTo(90).Within(Tolerance));
    }

    [Test]
    public void HourAngle_AdvancesWithMinutes()
    {
        // At 1:30 the hour hand is halfway between 1 and 2 => 45°.
        var time = new TimeSpan(1, 30, 0);
        Assert.That(ClockMath.HourAngle(time), Is.EqualTo(45).Within(Tolerance));
    }

    [Test]
    public void MinuteAngle_AtFifteenMinutes_IsNinety()
    {
        Assert.That(ClockMath.MinuteAngle(new TimeSpan(0, 15, 0)), Is.EqualTo(90).Within(Tolerance));
    }

    [Test]
    public void MinuteAngle_AdvancesWithSeconds()
    {
        // 30 seconds into the minute = half a minute tick = 3°.
        Assert.That(ClockMath.MinuteAngle(new TimeSpan(0, 0, 30)), Is.EqualTo(3).Within(Tolerance));
    }

    [Test]
    public void SecondAngle_AtThirtySeconds_IsOneEighty()
    {
        Assert.That(ClockMath.SecondAngle(new TimeSpan(0, 0, 30)), Is.EqualTo(180).Within(Tolerance));
    }

    [TestCase(-90, 270)]
    [TestCase(0, 0)]
    [TestCase(360, 0)]
    [TestCase(450, 90)]
    [TestCase(720, 0)]
    public void Normalize_WrapsIntoZeroTo360(double input, double expected)
    {
        Assert.That(ClockMath.Normalize(input), Is.EqualTo(expected).Within(Tolerance));
    }

    [Test]
    public void PointOnDial_ZeroDegrees_PointsStraightUp()
    {
        var (x, y) = ClockMath.PointOnDial(100, 100, 50, 0);
        Assert.Multiple(() =>
        {
            Assert.That(x, Is.EqualTo(100).Within(Tolerance));
            Assert.That(y, Is.EqualTo(50).Within(Tolerance)); // up = smaller Y
        });
    }

    [Test]
    public void PointOnDial_NinetyDegrees_PointsRight()
    {
        var (x, y) = ClockMath.PointOnDial(100, 100, 50, 90);
        Assert.Multiple(() =>
        {
            Assert.That(x, Is.EqualTo(150).Within(Tolerance));
            Assert.That(y, Is.EqualTo(100).Within(Tolerance));
        });
    }
}
