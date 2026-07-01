using System.Linq;
using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>
/// Draws a valve (Nixie tube) clock. Each digit is a "valve": a glass tube with a domed,
/// tipped top and a bronze base collar, containing a full glowing numeral behind a faint
/// anode-grid mesh — modelled on a real IN-14 style Nixie tube. The colon separator is its
/// own (narrower) valve whose dots can be lit or unlit, so the owning view can blink it.
/// The digits and colon positions are supplied by the owning view, so the same drawable
/// renders clock time, seconds, and Unix time. Depends only on Microsoft.Maui.Graphics.
/// </summary>
public class ValveClockDrawable : IDrawable
{
    /// <summary>A warm neon red-orange used as the default filament (glow) color.</summary>
    public static readonly Color DefaultFilamentColor = Color.FromArgb("#FF5A2C");

    static readonly Color TubeInteriorColor = Color.FromArgb("#241812");
    static readonly Color CollarColor = Color.FromArgb("#3A2A1E");

    /// <summary>The digits to display, left to right (one valve each).</summary>
    public IReadOnlyList<int> Digits { get; set; } = Array.Empty<int>();

    /// <summary>Indices of digits after which a colon valve is drawn.</summary>
    public IReadOnlyList<int> ColonAfter { get; set; } = Array.Empty<int>();

    /// <summary>Whether the colon valves' dots are currently lit (used to blink the colon).</summary>
    public bool ColonLit { get; set; } = true;

    /// <summary>The glowing numeral (filament) color.</summary>
    public Color FilamentColor { get; set; } = DefaultFilamentColor;

    /// <summary>The glass valve shell color.</summary>
    public Color ShellColor { get; set; } = Colors.Grey;

    /// <summary>The background fill drawn behind the valves.</summary>
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
            DrawGlowingText(canvas, DateText!, dateFooter, dateFooter.Height * 0.72f);

        int n = Digits.Count;
        if (n == 0)
            return;

        const float pad = 8f;
        float availW = dirtyRect.Width - 2 * pad;
        float availH = dirtyRect.Height - 2 * pad;
        if (availW <= 0 || availH <= 0)
            return;

        var colonSet = ColonAfter.ToHashSet();
        int colonCount = colonSet.Count;

        float valveH = availH;
        float valveW = valveH * 0.52f;
        float colonW = valveW * 0.5f;
        float spacing = valveW * 0.16f;

        int elementCount = n + colonCount;
        float totalW = n * valveW + colonCount * colonW + spacing * (elementCount - 1);
        if (totalW > availW)
        {
            float scale = availW / totalW;
            valveW *= scale;
            valveH *= scale;
            colonW *= scale;
            spacing *= scale;
            totalW = availW;
        }

        float x = dirtyRect.Left + pad + (availW - totalW) / 2f;
        float top = dirtyRect.Top + pad + (availH - valveH) / 2f;
        bool first = true;

        for (int i = 0; i < n; i++)
        {
            if (!first)
                x += spacing;
            first = false;

            DrawTube(canvas, x, top, valveW, valveH,
                body => DrawGlowingText(canvas, Digits[i].ToString(), body, body.Height * 0.60f));
            x += valveW;

            if (colonSet.Contains(i))
            {
                x += spacing;
                DrawTube(canvas, x, top, colonW, valveH, body => DrawColonDots(canvas, body, ColonLit));
                x += colonW;
            }
        }
    }

    // Draws the glass-tube chrome and invokes drawContent for the interior cavity.
    void DrawTube(ICanvas canvas, float x, float top, float w, float h, Action<RectF> drawContent)
    {
        float tipH = h * 0.05f;
        float domeH = h * 0.20f;
        float bodyTop = top + tipH + domeH * 0.5f;
        float bodyH = h - (tipH + domeH * 0.5f);
        float corner = w * 0.30f;
        var body = new RectF(x, bodyTop, w, bodyH);

        // Glass tip at the very top.
        canvas.FillColor = ShellColor.WithAlpha(0.55f);
        canvas.FillRoundedRectangle(x + w * 0.45f, top, w * 0.10f, tipH * 1.6f, w * 0.05f);

        // Tube interior (dark cavity so the glow reads).
        canvas.FillColor = TubeInteriorColor;
        canvas.FillRoundedRectangle(body, corner);

        // Faint anode-grid mesh, then the glowing content.
        DrawMesh(canvas, body);
        drawContent(body);

        // Glass body tint over the content, giving depth.
        canvas.FillColor = ShellColor.WithAlpha(0.16f);
        canvas.FillRoundedRectangle(body, corner);

        // Domed glass top.
        canvas.FillColor = ShellColor.WithAlpha(0.16f);
        canvas.FillEllipse(x, top + tipH, w, domeH * 2f);

        // Left-side glass highlight.
        canvas.FillColor = Colors.White.WithAlpha(0.14f);
        canvas.FillRoundedRectangle(x + w * 0.12f, bodyTop + bodyH * 0.08f, w * 0.13f, bodyH * 0.72f, w * 0.06f);

        // Glass outline.
        canvas.StrokeColor = ShellColor.WithAlpha(0.55f);
        canvas.StrokeSize = Math.Max(1f, w * 0.03f);
        canvas.DrawRoundedRectangle(body, corner);

        // Bronze base collar the tube sits in.
        float collarH = h * 0.10f;
        canvas.FillColor = CollarColor;
        canvas.FillRoundedRectangle(x - w * 0.06f, top + h - collarH, w * 1.12f, collarH, collarH * 0.35f);
        canvas.FillColor = CollarColor.WithAlpha(0.6f);
        canvas.FillRoundedRectangle(x - w * 0.06f, top + h - collarH, w * 1.12f, collarH * 0.4f, collarH * 0.2f);
    }

    void DrawMesh(ICanvas canvas, RectF body)
    {
        canvas.StrokeColor = FilamentColor.WithAlpha(0.10f);
        canvas.StrokeSize = Math.Max(0.5f, body.Width * 0.015f);
        const int lines = 6;
        for (int i = 1; i < lines; i++)
        {
            float lx = body.Left + body.Width * i / lines;
            canvas.DrawLine(lx, body.Top + body.Height * 0.10f, lx, body.Top + body.Height * 0.90f);
        }
    }

    void DrawGlowingText(ICanvas canvas, string text, RectF body, float fontSize)
    {
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = fontSize;

        // Soft outer glow.
        canvas.SaveState();
        canvas.SetShadow(new SizeF(0, 0), fontSize * 0.35f, FilamentColor);
        canvas.FontColor = FilamentColor;
        canvas.DrawString(text, body.Left, body.Top, body.Width, body.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
        canvas.RestoreState();

        // Bright core.
        canvas.FontColor = FilamentColor;
        canvas.DrawString(text, body.Left, body.Top, body.Width, body.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    void DrawColonDots(ICanvas canvas, RectF body, bool lit)
    {
        float radius = body.Width * 0.18f;
        float cx = body.Center.X;

        canvas.SaveState();
        if (lit)
        {
            canvas.SetShadow(new SizeF(0, 0), radius * 1.6f, FilamentColor);
            canvas.FillColor = FilamentColor;
        }
        else
        {
            canvas.FillColor = FilamentColor.WithAlpha(0.12f);
        }

        canvas.FillCircle(cx, body.Top + body.Height * 0.38f, radius);
        canvas.FillCircle(cx, body.Top + body.Height * 0.62f, radius);
        canvas.RestoreState();
    }
}
