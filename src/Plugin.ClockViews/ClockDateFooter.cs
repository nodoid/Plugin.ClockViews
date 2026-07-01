using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// Helper for the date line beneath a digital clock: splits off a bottom strip for the date and
/// (for text styles) draws it. Digit-styled clocks render their own date in the footer strip.
/// </summary>
static class ClockDateFooter
{
    /// <summary>
    /// If <paramref name="hasDate"/>, outputs a bottom strip in <paramref name="footer"/> and
    /// returns the remaining area above it; otherwise returns <paramref name="full"/>.
    /// </summary>
    public static RectF Split(RectF full, bool hasDate, out RectF footer, float fraction = 0.2f, float maxStrip = float.MaxValue)
    {
        if (!hasDate)
        {
            footer = RectF.Zero;
            return full;
        }

        float strip = Math.Min(full.Height * fraction, maxStrip);
        footer = new RectF(full.Left, full.Bottom - strip, full.Width, strip);
        return new RectF(full.Left, full.Top, full.Width, full.Height - strip);
    }

    /// <summary>Draws the date as plain centred text (used by the text-based clock styles).</summary>
    public static void DrawText(ICanvas canvas, RectF footer, string text, Color color)
    {
        canvas.SaveState();
        canvas.FontColor = color;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = footer.Height * 0.62f;
        canvas.DrawString(text, footer.Left, footer.Top, footer.Width, footer.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);
        canvas.RestoreState();
    }
}
