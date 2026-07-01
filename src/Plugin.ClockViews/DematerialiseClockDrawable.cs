using System.Linq;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>The dematerialise/materialise style used by <see cref="DematerialiseClockDrawable"/>.</summary>
public enum SciFiTheme
{
    /// <summary>Doctor Who TARDIS style: a flickering, ghostly cyan fade.</summary>
    DrWho,
    /// <summary>Star Trek transporter style: the digit dissolves into rising/falling sparkles.</summary>
    StarTrek,
}

/// <summary>
/// One cell of a <see cref="DematerialiseClockDrawable"/>: a digit (or a fixed separator).
/// <paramref name="Current"/> dematerialises while <paramref name="Next"/> materialises as
/// <paramref name="Progress"/> runs 0→1 (first half out, second half in).
/// </summary>
public readonly record struct DematerialiseCell(string Current, string Next, double Progress, bool IsSeparator);

/// <summary>
/// Draws a clock whose digits dematerialise and rematerialise when they change, in one of two
/// sci-fi styles (<see cref="SciFiTheme"/>). Depends only on Microsoft.Maui.Graphics.
/// </summary>
public class DematerialiseClockDrawable : IDrawable
{
    static readonly Color SparkleGlow = Color.FromArgb("#66CCFF");

    /// <summary>The cells to draw, left to right.</summary>
    public IReadOnlyList<DematerialiseCell> Cells { get; set; } = Array.Empty<DematerialiseCell>();

    /// <summary>The digit color.</summary>
    public Color DigitColor { get; set; } = Colors.Black;

    /// <summary>The dematerialisation style.</summary>
    public SciFiTheme Theme { get; set; } = SciFiTheme.DrWho;

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
            DrawCell(canvas, new RectF(x, top, cw, cellH), Cells[i], i);
            x += cw + spacing;
        }
    }

    void DrawCell(ICanvas canvas, RectF rect, DematerialiseCell cell, int seed)
    {
        if (cell.IsSeparator)
        {
            canvas.FontColor = DigitColor;
            canvas.FontSize = rect.Height * 0.6f;
            DrawText(canvas, cell.Current, rect);
            return;
        }

        double p = cell.Current == cell.Next ? 0 : Math.Clamp(cell.Progress, 0, 1);

        if (p <= 0)
        {
            canvas.SaveState();
            canvas.FontColor = DigitColor;
            canvas.FontSize = rect.Height * 0.6f;
            DrawText(canvas, cell.Current, rect);
            canvas.RestoreState();
            return;
        }

        if (p < 0.5)
            DrawThemed(canvas, rect, cell.Current, materialising: false, t: (float)(p * 2), seed);
        else
            DrawThemed(canvas, rect, cell.Next, materialising: true, t: (float)((p - 0.5) * 2), seed);
    }

    void DrawThemed(ICanvas canvas, RectF rect, string text, bool materialising, float t, int seed)
    {
        // presence: 1 = fully solid, 0 = gone.
        float presence = materialising ? t : 1 - t;

        if (Theme == SciFiTheme.DrWho)
            DrawDrWho(canvas, rect, text, presence, t);
        else
            DrawStarTrek(canvas, rect, text, presence, t, materialising, seed);
    }

    // TARDIS: a wheezing, flickering cyan fade that grows faint as it goes.
    void DrawDrWho(ICanvas canvas, RectF rect, string text, float presence, float t)
    {
        float pulse = 0.55f + 0.45f * (float)Math.Abs(Math.Sin(t * Math.PI * 4));
        float alpha = Math.Clamp(presence * pulse, 0, 1);
        float scale = 1f + (1 - presence) * 0.18f;

        canvas.SaveState();
        canvas.Alpha = alpha;
        canvas.SetShadow(new SizeF(0, 0), rect.Height * (0.10f + (1 - presence) * 0.15f), SparkleGlow);

        var c = rect.Center;
        canvas.Translate(c.X, c.Y);
        canvas.Scale(scale, scale);
        canvas.Translate(-c.X, -c.Y);

        canvas.FontColor = DigitColor;
        canvas.FontSize = rect.Height * 0.6f;
        DrawText(canvas, text, rect);
        canvas.RestoreState();
    }

    // Transporter: the digit itself breaks into a grid of glowing fragments that scatter and
    // twinkle as it dematerialises, and converge back together as it materialises.
    void DrawStarTrek(ICanvas canvas, RectF rect, string text, float presence, float t, bool materialising, int seed)
    {
        float dispersion = Math.Clamp(1 - presence, 0, 1); // 0 = assembled, 1 = fully scattered
        const int cols = 6;
        const int rows = 8;
        float tileW = rect.Width / cols;
        float tileH = rect.Height / rows;
        float maxDist = rect.Height * 0.7f;
        float fontSize = rect.Height * 0.6f;

        for (int ty = 0; ty < rows; ty++)
        {
            for (int tx = 0; tx < cols; tx++)
            {
                int k = seed * 997 + ty * cols + tx;
                float angle = Rand(k) * 6.2832f;
                float dist = dispersion * maxDist * (0.4f + Rand(k + 5));
                float ox = (float)Math.Cos(angle) * dist;
                float oy = (float)Math.Sin(angle) * dist - dispersion * rect.Height * 0.15f; // slight upward beam

                // Twinkle so the fragments glitter.
                float twinkle = 0.55f + 0.45f * (float)Math.Sin(k * 1.3 + presence * 25);
                float alpha = Math.Clamp((0.15f + 0.85f * presence) * twinkle, 0, 1);
                var color = Lerp(DigitColor, Colors.White, dispersion * 0.85f);

                canvas.SaveState();
                canvas.Alpha = alpha;
                canvas.SetShadow(new SizeF(0, 0), tileW * (0.5f + dispersion), SparkleGlow);
                canvas.ClipRectangle(rect.Left + tx * tileW + ox, rect.Top + ty * tileH + oy, tileW + 1f, tileH + 1f);
                canvas.Translate(ox, oy);
                canvas.FontColor = color;
                canvas.FontSize = fontSize;
                DrawText(canvas, text, rect);
                canvas.RestoreState();
            }
        }
    }

    static Color Lerp(Color a, Color b, float t) => new(
        a.Red + (b.Red - a.Red) * t,
        a.Green + (b.Green - a.Green) * t,
        a.Blue + (b.Blue - a.Blue) * t);

    void DrawText(ICanvas canvas, string text, RectF rect)
        => canvas.DrawString(text, rect.Left, rect.Top, rect.Width, rect.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);

    // Deterministic pseudo-random in [0,1) (shader-style hash), so sparkles are stable per frame.
    static float Rand(int n)
    {
        double s = Math.Sin(n * 12.9898) * 43758.5453;
        return (float)(s - Math.Floor(s));
    }
}
