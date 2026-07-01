using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// Draws an analog clock face for a given <see cref="Time"/>.
/// This type depends only on Microsoft.Maui.Graphics, so it is fully unit-testable
/// and reusable across every target framework.
/// </summary>
public class AnalogClockDrawable : IDrawable
{
    /// <summary>The time to render. Defaults to <see cref="TimeSpan.Zero"/> (12:00:00).</summary>
    public TimeSpan Time { get; set; }

    /// <summary>The color of the dial outline and hour ticks.</summary>
    public Color FaceColor { get; set; } = Colors.Black;

    /// <summary>The color of the hour and minute hands.</summary>
    public Color HandColor { get; set; } = Colors.Black;

    /// <summary>The color of the second hand.</summary>
    public Color SecondHandColor { get; set; } = Colors.Red;

    /// <summary>The dial background fill.</summary>
    public Color BackgroundColor { get; set; } = Colors.White;

    /// <summary>Whether the second hand is drawn.</summary>
    public bool ShowSecondHand { get; set; } = true;

    /// <summary>Optional date (dd MMM) drawn in a small window at the 4 o'clock position.</summary>
    public string? DateText { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float centerX = dirtyRect.Center.X;
        float centerY = dirtyRect.Center.Y;
        float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 4f;
        if (radius <= 0)
            return;

        // Dial.
        canvas.FillColor = BackgroundColor;
        canvas.FillCircle(centerX, centerY, radius);
        canvas.StrokeColor = FaceColor;
        canvas.StrokeSize = Math.Max(2f, radius * 0.03f);
        canvas.DrawCircle(centerX, centerY, radius);

        // Hour ticks.
        canvas.StrokeColor = FaceColor;
        for (int hour = 0; hour < 12; hour++)
        {
            double angle = hour * 30.0;
            var outer = ClockMath.PointOnDial(centerX, centerY, radius * 0.92, angle);
            var inner = ClockMath.PointOnDial(centerX, centerY, radius * 0.80, angle);
            canvas.StrokeSize = Math.Max(2f, radius * 0.025f);
            canvas.DrawLine((float)outer.X, (float)outer.Y, (float)inner.X, (float)inner.Y);
        }

        // Date window at the 5 o'clock position (drawn before the hands, so hands pass over it).
        if (!string.IsNullOrEmpty(DateText))
        {
            var p = ClockMath.PointOnDial(centerX, centerY, radius * 0.52, 150);
            const float bw = 44f, bh = 16f;
            var box = new RectF((float)p.X - bw / 2f, (float)p.Y - bh / 2f, bw, bh);
            canvas.FillColor = BackgroundColor;
            canvas.FillRoundedRectangle(box, 3f);
            canvas.StrokeColor = FaceColor;
            canvas.StrokeSize = 1f;
            canvas.DrawRoundedRectangle(box, 3f);
            canvas.FontColor = FaceColor;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.FontSize = 8f;
            canvas.DrawString(DateText, box.Left, box.Top, box.Width, box.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        // Hands.
        DrawHand(canvas, centerX, centerY, ClockMath.HourAngle(Time), radius * 0.50, Math.Max(3f, radius * 0.04f), HandColor);
        DrawHand(canvas, centerX, centerY, ClockMath.MinuteAngle(Time), radius * 0.72, Math.Max(2f, radius * 0.028f), HandColor);

        if (ShowSecondHand)
            DrawHand(canvas, centerX, centerY, ClockMath.SecondAngle(Time), radius * 0.82, Math.Max(1f, radius * 0.012f), SecondHandColor);

        // Center cap.
        canvas.FillColor = HandColor;
        canvas.FillCircle(centerX, centerY, Math.Max(3f, radius * 0.04f));
    }

    static void DrawHand(ICanvas canvas, float cx, float cy, double angle, double length, float thickness, Color color)
    {
        var tip = ClockMath.PointOnDial(cx, cy, length, angle);
        canvas.StrokeColor = color;
        canvas.StrokeSize = thickness;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawLine(cx, cy, (float)tip.X, (float)tip.Y);
    }
}
