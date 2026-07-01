using System.Linq;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// One flap panel of a <see cref="FlipClockDrawable"/>.
/// <paramref name="Current"/> is the value shown before the flip, <paramref name="Next"/> the
/// value being flipped to, and <paramref name="Progress"/> the flip animation (0 = showing
/// current, 1 = showing next). <paramref name="BottomOnly"/> panels (AM/PM) draw their text in
/// the lower half only, with a blank top.
/// </summary>
public readonly record struct FlipPanel(string Current, string Next, double Progress, bool BottomOnly, bool IsSeparator = false, bool ColonLit = true);

/// <summary>
/// Draws a split-flap ("flip") clock. Each panel is a card split across the middle; when its
/// value changes the top leaf of the old value folds down to reveal the new top behind it, then
/// the new bottom leaf folds down into place. Depends only on Microsoft.Maui.Graphics.
/// </summary>
public class FlipClockDrawable : IDrawable
{
    static readonly Color HingeColor = Color.FromArgb("#33000000");
    static readonly Color BorderColor = Color.FromArgb("#22000000");
    static readonly Color ShadowColor = Color.FromArgb("#55000000");

    /// <summary>The panels to draw, left to right.</summary>
    public IReadOnlyList<FlipPanel> Panels { get; set; } = Array.Empty<FlipPanel>();

    /// <summary>The digit/text color.</summary>
    public Color DigitColor { get; set; } = Colors.Black;

    /// <summary>The card (flap) color.</summary>
    public Color CardColor { get; set; } = Colors.White;

    /// <summary>The background fill drawn behind the panels.</summary>
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
            DrawDateCard(canvas, dateFooter, DateText!);

        int n = Panels.Count;
        if (n == 0)
            return;

        const float pad = 8f;
        float availW = dirtyRect.Width - 2 * pad;
        float availH = dirtyRect.Height - 2 * pad;
        if (availW <= 0 || availH <= 0)
            return;

        float[] weights = Panels.Select(p => p.IsSeparator ? 0.32f : p.BottomOnly ? 0.62f : 1f).ToArray();
        float panelH = availH;
        float unitW = panelH * 0.72f;
        float spacing = unitW * 0.14f;

        float totalW = weights.Sum() * unitW + spacing * (n - 1);
        if (totalW > availW)
        {
            float scale = availW / totalW;
            unitW *= scale;
            spacing *= scale;
            panelH *= scale;
            totalW = availW;
        }

        float x = dirtyRect.Left + pad + (availW - totalW) / 2f;
        float top = dirtyRect.Top + pad + (availH - panelH) / 2f;

        for (int i = 0; i < n; i++)
        {
            float pw = weights[i] * unitW;
            var slot = new RectF(x, top, pw, panelH);
            if (Panels[i].IsSeparator)
                DrawSeparator(canvas, slot, Panels[i].ColonLit);
            else
                DrawFlap(canvas, slot, Panels[i]);
            x += pw + spacing;
        }
    }

    // Draws the date on a single flip-style card (with hinge line) to match the clock.
    void DrawDateCard(ICanvas canvas, RectF footer, string text)
    {
        float h = footer.Height * 0.92f;
        float w = Math.Min(footer.Width * 0.95f, h * 7f);
        var card = new RectF(footer.Center.X - w / 2f, footer.Center.Y - h / 2f, w, h);
        float corner = h * 0.16f;

        canvas.SaveState();
        canvas.SetShadow(new SizeF(0, h * 0.05f), h * 0.1f, ShadowColor);
        canvas.FillColor = CardColor;
        canvas.FillRoundedRectangle(card, corner);
        canvas.RestoreState();

        canvas.FontColor = DigitColor;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = h * 0.52f;
        canvas.DrawString(text, card.Left, card.Top, card.Width, card.Height,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.StrokeColor = HingeColor;
        canvas.StrokeSize = Math.Max(1f, h * 0.03f);
        canvas.DrawLine(card.Left, card.Center.Y, card.Right, card.Center.Y);

        canvas.StrokeColor = BorderColor;
        canvas.StrokeSize = 1f;
        canvas.DrawRoundedRectangle(card, corner);
    }

    void DrawSeparator(ICanvas canvas, RectF rect, bool lit)
    {
        float r = rect.Width * 0.22f;
        float cx = rect.Center.X;
        canvas.FillColor = lit ? DigitColor : DigitColor.WithAlpha(0.12f);
        canvas.FillCircle(cx, rect.Top + rect.Height * 0.38f, r);
        canvas.FillCircle(cx, rect.Top + rect.Height * 0.62f, r);
    }

    void DrawFlap(ICanvas canvas, RectF card, FlipPanel panel)
    {
        float gap = card.Height * 0.02f;
        float half = (card.Height - gap) / 2f;
        float hingeY = card.Top + half + gap / 2f;
        float corner = card.Width * 0.10f;

        var topRect = new RectF(card.Left, card.Top, card.Width, half);
        var botRect = new RectF(card.Left, hingeY + gap / 2f, card.Width, half);

        bool changing = panel.Current != panel.Next;
        double p = changing ? Math.Clamp(panel.Progress, 0, 1) : 0;

        // Shadowed card base so white cards stand out on any background.
        canvas.SaveState();
        canvas.SetShadow(new SizeF(0, card.Height * 0.03f), card.Height * 0.06f, ShadowColor);
        canvas.FillColor = CardColor;
        canvas.FillRoundedRectangle(card, corner);
        canvas.RestoreState();

        // Static halves: once the flip starts, the top already shows the new value behind
        // the folding leaf; the bottom keeps the old value until the new bottom leaf lands.
        DrawCardHalf(canvas, topRect, card, hingeY, p > 0 ? panel.Next : panel.Current, isTop: true, panel.BottomOnly, corner, 1f);
        DrawCardHalf(canvas, botRect, card, hingeY, panel.Current, isTop: false, panel.BottomOnly, corner, 1f);

        if (p > 0)
        {
            if (p < 0.5)
            {
                // Old top folding down toward the hinge.
                float s = (float)(1 - p * 2);
                DrawCardHalf(canvas, topRect, card, hingeY, panel.Current, isTop: true, panel.BottomOnly, corner, s);
            }
            else
            {
                // New bottom unfolding down from the hinge.
                float s = (float)(p * 2 - 1);
                DrawCardHalf(canvas, botRect, card, hingeY, panel.Next, isTop: false, panel.BottomOnly, corner, s);
            }
        }

        // Hinge line across the middle.
        canvas.StrokeColor = HingeColor;
        canvas.StrokeSize = Math.Max(1f, card.Height * 0.012f);
        canvas.DrawLine(card.Left, hingeY, card.Right, hingeY);

        // Card border for definition.
        canvas.StrokeColor = BorderColor;
        canvas.StrokeSize = 1f;
        canvas.DrawRoundedRectangle(card, corner);
    }

    // Draws one half of a card, optionally foreshortened by scaleY about the hinge.
    void DrawCardHalf(ICanvas canvas, RectF halfRect, RectF card, float hingeY, string text, bool isTop, bool bottomOnly, float corner, float scaleY)
    {
        canvas.SaveState();
        canvas.ClipRectangle(halfRect.Left, halfRect.Top, halfRect.Width, halfRect.Height);

        if (scaleY != 1f)
        {
            canvas.Translate(0, hingeY);
            canvas.Scale(1, scaleY);
            canvas.Translate(0, -hingeY);
        }

        canvas.FillColor = CardColor;
        canvas.FillRoundedRectangle(card, corner);

        canvas.FontColor = DigitColor;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;

        if (bottomOnly)
        {
            // AM/PM: text only in the lower half, top blank.
            if (!isTop)
            {
                canvas.FontSize = card.Height * 0.26f;
                canvas.DrawString(text, halfRect.Left, halfRect.Top, halfRect.Width, halfRect.Height,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }
        else
        {
            // Full digit centered across the whole card so the two halves join up.
            canvas.FontSize = card.Height * 0.58f;
            canvas.DrawString(text, card.Left, card.Top, card.Width, card.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        canvas.RestoreState();
    }
}
