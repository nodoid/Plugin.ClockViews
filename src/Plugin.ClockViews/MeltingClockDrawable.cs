using System.Linq;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// One cell of a <see cref="MeltingClockDrawable"/>: a digit (or a fixed separator).
/// <paramref name="Current"/> melts away while <paramref name="Next"/> is revealed as
/// <paramref name="Progress"/> runs 0→1.
/// </summary>
public readonly record struct MeltCell(string Current, string Next, double Progress, bool IsSeparator);

/// <summary>
/// Draws a "melting" digital clock. When a digit changes, the old digit is sliced into vertical
/// strips that drip downward, stretch, and fade until gone, revealing the new digit beneath.
/// Depends only on Microsoft.Maui.Graphics.
/// </summary>
public class MeltingClockDrawable : IDrawable
{
    const int Strips = 14;

    /// <summary>The cells to draw, left to right.</summary>
    public IReadOnlyList<MeltCell> Cells { get; set; } = Array.Empty<MeltCell>();

    /// <summary>The digit color.</summary>
    public Color DigitColor { get; set; } = Colors.Black;

    /// <summary>The background fill drawn behind the digits.</summary>
    public Color BackgroundColor { get; set; } = Colors.Transparent;

    /// <summary>Optional date (dd MMM) drawn under the clock.</summary>
    public string? DateText { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (BackgroundColor != Colors.Transparent)
        {
            canvas.FillColor = BackgroundColor;
            canvas.FillRectangle(dirtyRect);
        }

        bool hasDate = !string.IsNullOrEmpty(DateText);
        dirtyRect = ClockDateFooter.Split(dirtyRect, hasDate, out var dateFooter, 1f / 3f);
        if (hasDate)
            ClockDateFooter.DrawText(canvas, dateFooter, DateText!, DigitColor);

        int n = Cells.Count;
        if (n == 0)
            return;

        const float pad = 8f;
        float availW = dirtyRect.Width - 2 * pad;
        float availH = dirtyRect.Height - 2 * pad;
        if (availW <= 0 || availH <= 0)
            return;

        float[] weights = Cells.Select(c => c.IsSeparator ? 0.4f : 1f).ToArray();
        float cellH = availH;
        float unitW = cellH * 0.62f;
        float spacing = unitW * 0.12f;

        float totalW = weights.Sum() * unitW + spacing * (n - 1);
        if (totalW > availW)
        {
            float scale = availW / totalW;
            unitW *= scale;
            spacing *= scale;
            cellH *= scale;
            totalW = availW;
        }

        float x = dirtyRect.Left + pad + (availW - totalW) / 2f;
        float top = dirtyRect.Top + pad + (availH - cellH) / 2f;

        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;

        for (int i = 0; i < n; i++)
        {
            float cw = weights[i] * unitW;
            DrawCell(canvas, new RectF(x, top, cw, cellH), Cells[i], dirtyRect.Bottom);
            x += cw + spacing;
        }
    }

    void DrawCell(ICanvas canvas, RectF rect, MeltCell cell, float viewBottom)
    {
        if (cell.IsSeparator)
        {
            canvas.FontColor = DigitColor;
            canvas.FontSize = rect.Height * 0.6f;
            canvas.DrawString(cell.Current, rect.Left, rect.Top, rect.Width, rect.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        double p = cell.Current == cell.Next ? 0 : Math.Clamp(cell.Progress, 0, 1);

        // The incoming digit sits underneath, crisp.
        canvas.FontColor = DigitColor;
        canvas.FontSize = rect.Height * 0.6f;
        canvas.DrawString(cell.Next, rect.Left, rect.Top, rect.Width, rect.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        if (p <= 0)
            return;

        // The outgoing digit melts on top of it.
        float stripW = rect.Width / Strips;
        float maxDrop = rect.Height * 1.0f;
        float stretch = 1f + (float)p * 0.6f;

        for (int i = 0; i < Strips; i++)
        {
            // Deterministic per-strip variation so strips drip at different rates.
            float f = 0.4f + 0.6f * (float)Math.Abs(Math.Sin(i * 1.7 + 0.5));
            float offset = (float)p * maxDrop * f;
            float alpha = (float)Math.Clamp(1 - p * (0.7 + 0.3 * f), 0, 1);
            float stripX = rect.Left + i * stripW;

            canvas.SaveState();
            canvas.ClipRectangle(stripX, rect.Top, stripW + 0.5f, viewBottom - rect.Top);
            canvas.Alpha = alpha;
            // Elongate the strip downward as it melts.
            canvas.Translate(0, rect.Top);
            canvas.Scale(1, stretch);
            canvas.Translate(0, -rect.Top);
            canvas.FontColor = DigitColor;
            canvas.FontSize = rect.Height * 0.6f;
            canvas.DrawString(cell.Current, rect.Left, rect.Top + offset, rect.Width, rect.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.RestoreState();
        }
    }
}
