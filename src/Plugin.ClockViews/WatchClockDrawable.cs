using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>Border thickness setting for a <see cref="WatchClockDrawable"/>.</summary>
public enum WatchBorderThickness
{
    Thin,
    Medium,
    Thick,
}

/// <summary>The watch face style used by <see cref="WatchClockDrawable"/>.</summary>
public enum WatchTheme
{
    /// <summary>1980s LCD digital watch: seven-segment digits with an AM/PM dot.</summary>
    EightiesDigital,
    /// <summary>Apple Watch style: a black rounded screen with a bold digital time.</summary>
    AppleWatch,
    /// <summary>Pixel Watch style: a round black face with an accent ring.</summary>
    PixelWatch,
}

/// <summary>
/// Draws a wristwatch clock in one of three <see cref="WatchTheme"/> styles.
/// Depends only on Microsoft.Maui.Graphics.
/// </summary>
public class WatchClockDrawable : IDrawable
{
    static readonly Color LcdBackground = Color.FromArgb("#B4C7A6");
    static readonly Color LcdCase = Color.FromArgb("#2A2A2A");
    static readonly Color ScreenBlack = Color.FromArgb("#0B0B0D");

    public DateTime Time { get; set; }
    public bool ShowSeconds { get; set; }
    public bool Is24Hour { get; set; }
    public bool ColonLit { get; set; } = true;
    public WatchTheme Theme { get; set; } = WatchTheme.PixelWatch;

    /// <summary>Case/frame border thickness. Pixel value depends on the theme.</summary>
    public WatchBorderThickness Border { get; set; } = WatchBorderThickness.Medium;

    // Digital watch: 1/3/5px. Apple & Pixel frames: 2/4/6px.
    float BorderPx => Theme == WatchTheme.EightiesDigital
        ? Border switch { WatchBorderThickness.Thin => 1f, WatchBorderThickness.Thick => 5f, _ => 3f }
        : Border switch { WatchBorderThickness.Thin => 2f, WatchBorderThickness.Thick => 6f, _ => 4f };

    /// <summary>Accent color (from the view's ClockColor).</summary>
    public Color AccentColor { get; set; } = Colors.Black;

    public Color BackgroundColor { get; set; } = Colors.Transparent;

    /// <summary>Optional date (dd MMM yyyy) drawn under the Apple/Pixel watch.</summary>
    public string? DateText { get; set; }

    /// <summary>Optional date digits (ddMMyyyy) drawn as seven-segment under the 80s watch.</summary>
    public string? DateDigits { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (BackgroundColor != Colors.Transparent)
        {
            canvas.FillColor = BackgroundColor;
            canvas.FillRectangle(dirtyRect);
        }

        Color dateColor = AccentColor.Alpha <= 0 ? Colors.Black : AccentColor;
        bool eighties = Theme == WatchTheme.EightiesDigital;
        bool hasDate = eighties ? !string.IsNullOrEmpty(DateDigits) : !string.IsNullOrEmpty(DateText);
        dirtyRect = ClockDateFooter.Split(dirtyRect, hasDate, out var dateFooter, 0.2f, 26f);

        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;

        switch (Theme)
        {
            case WatchTheme.EightiesDigital:
                DrawEighties(canvas, dirtyRect);
                break;
            case WatchTheme.AppleWatch:
                DrawApple(canvas, dirtyRect);
                break;
            default:
                DrawPixel(canvas, dirtyRect);
                break;
        }

        if (hasDate)
        {
            if (eighties)
            {
                Color on = AccentColor.Alpha <= 0 ? Colors.Black : AccentColor;
                DrawDateSevenSeg(canvas, dateFooter, DateDigits!, on, on.WithAlpha(0.12f));
            }
            else
            {
                ClockDateFooter.DrawText(canvas, dateFooter, DateText!, dateColor);
            }
        }
    }

    // Renders the date as small seven-segment digits (numeric), grouped dd MM yyyy.
    void DrawDateSevenSeg(ICanvas canvas, RectF footer, string digits, Color on, Color off)
    {
        int n = digits.Length;
        if (n == 0)
            return;

        float h = footer.Height;
        float dw = h * 0.5f;
        float gap = dw * 0.28f;
        float groupGap = dw * 0.6f;

        // Wider gaps after the day (index 1) and month (index 3) groups.
        float total = n * dw + gap * (n - 1) + 2 * (groupGap - gap);
        if (total > footer.Width)
        {
            float s = footer.Width / total;
            dw *= s; gap *= s; groupGap *= s; total = footer.Width;
        }

        float x = footer.Left + (footer.Width - total) / 2f;
        for (int i = 0; i < n; i++)
        {
            int d = digits[i] - '0';
            DrawSevenSeg(canvas, x, footer.Top, dw, h, d, on, off);
            x += dw + (i == 1 || i == 3 ? groupGap : gap);
        }
    }

    // ---- 1980s LCD digital watch ------------------------------------------------

    void DrawEighties(ICanvas canvas, RectF rect)
    {
        // Watch case + LCD panel. The case border thickness is bindable.
        float caseCorner = rect.Height * 0.18f;
        float border = BorderPx;
        canvas.FillColor = LcdCase;
        canvas.FillRoundedRectangle(rect, caseCorner);

        var lcd = new RectF(rect.Left + border, rect.Top + border,
            rect.Width - 2 * border, rect.Height - 2 * border);
        canvas.FillColor = LcdBackground;
        canvas.FillRoundedRectangle(lcd, Math.Max(0, caseCorner - border));

        Color on = AccentColor.Alpha <= 0 ? Colors.Black : AccentColor;
        Color off = on.WithAlpha(0.12f);

        int h = Time.Hour;
        bool pm = h >= 12;
        int displayH = Is24Hour ? h : (h % 12 == 0 ? 12 : h % 12);
        int hTens = displayH / 10;
        bool blankLeading = !Is24Hour && hTens == 0;

        float margin = lcd.Height * 0.14f;
        float availW = lcd.Width - 2 * margin;

        float digitH = lcd.Height * 0.62f;
        float digitW = digitH * 0.55f;
        float colonW = digitW * 0.35f;
        float gap = digitW * 0.18f;
        float secDigitH = digitH * 0.5f;
        float secDigitW = secDigitH * 0.55f;
        float dotW = digitW * 0.35f;

        // Widths of every element, in order, so we can size the gaps between them and centre exactly.
        var widths = new List<float> { digitW, digitW, colonW, digitW, digitW };
        if (ShowSeconds) { widths.Add(secDigitW); widths.Add(secDigitW); }
        if (!Is24Hour) widths.Add(dotW);

        float sum = 0;
        foreach (var w in widths) sum += w;
        float total = sum + gap * (widths.Count - 1);
        if (total > availW)
        {
            float s = availW / total;
            digitW *= s; digitH *= s; colonW *= s; gap *= s;
            secDigitW *= s; secDigitH *= s; dotW *= s; total *= s;
        }

        // Centre both axes.
        float x = lcd.Left + (lcd.Width - total) / 2f;
        float top = lcd.Top + (lcd.Height - digitH) / 2f;
        float secTop = top + (digitH - secDigitH); // bottom-align seconds with the main digits

        DrawSevenSeg(canvas, x, top, digitW, digitH, blankLeading ? -1 : hTens, on, off); x += digitW + gap;
        DrawSevenSeg(canvas, x, top, digitW, digitH, displayH % 10, on, off); x += digitW + gap;
        DrawColon(canvas, x, top, colonW, digitH, ColonLit ? on : off); x += colonW + gap;
        DrawSevenSeg(canvas, x, top, digitW, digitH, Time.Minute / 10, on, off); x += digitW + gap;
        DrawSevenSeg(canvas, x, top, digitW, digitH, Time.Minute % 10, on, off); x += digitW + gap;

        if (ShowSeconds)
        {
            DrawSevenSeg(canvas, x, secTop, secDigitW, secDigitH, Time.Second / 10, on, off); x += secDigitW + gap;
            DrawSevenSeg(canvas, x, secTop, secDigitW, secDigitH, Time.Second % 10, on, off); x += secDigitW + gap;
        }

        if (!Is24Hour)
        {
            // AM/PM dot (lit for PM).
            float r = dotW * 0.4f;
            canvas.FillColor = pm ? on : off;
            canvas.FillCircle(x + dotW / 2f, top + digitH * 0.25f, r);
        }
    }

    void DrawColon(ICanvas canvas, float x, float top, float w, float h, Color color)
    {
        float r = w * 0.28f;
        float cx = x + w / 2f;
        canvas.FillColor = color;
        canvas.FillCircle(cx, top + h * 0.36f, r);
        canvas.FillCircle(cx, top + h * 0.64f, r);
    }

    void DrawSevenSeg(ICanvas canvas, float x, float y, float w, float h, int digit, Color on, Color off)
    {
        bool[] seg = digit < 0 || digit > 9 ? new bool[7] : Segments(digit);
        bool showGhost = digit >= 0; // blanked leading digit draws nothing

        float t = Math.Min(w, h) * 0.16f;
        float inset = t * 0.6f;
        float left = x + t / 2f, right = x + w - t / 2f;
        float topY = y + t / 2f, midY = y + h / 2f, botY = y + h - t / 2f;

        void S(bool lit, float x1, float y1, float x2, float y2)
        {
            if (!lit && !showGhost)
                return;
            canvas.StrokeColor = lit ? on : off;
            canvas.StrokeSize = t;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawLine(x1, y1, x2, y2);
        }

        S(seg[0], left + inset, topY, right - inset, topY);   // a
        S(seg[1], right, topY + inset, right, midY - inset);  // b
        S(seg[2], right, midY + inset, right, botY - inset);  // c
        S(seg[3], left + inset, botY, right - inset, botY);   // d
        S(seg[4], left, midY + inset, left, botY - inset);    // e
        S(seg[5], left, topY + inset, left, midY - inset);    // f
        S(seg[6], left + inset, midY, right - inset, midY);   // g
    }

    static bool[] Segments(int d) => d switch
    {
        0 => new[] { true, true, true, true, true, true, false },
        1 => new[] { false, true, true, false, false, false, false },
        2 => new[] { true, true, false, true, true, false, true },
        3 => new[] { true, true, true, true, false, false, true },
        4 => new[] { false, true, true, false, false, true, true },
        5 => new[] { true, false, true, true, false, true, true },
        6 => new[] { true, false, true, true, true, true, true },
        7 => new[] { true, true, true, false, false, false, false },
        8 => new[] { true, true, true, true, true, true, true },
        9 => new[] { true, true, true, true, false, true, true },
        _ => new bool[7],
    };

    // ---- Apple Watch (square, rounded, solid frame) -----------------------------

    void DrawApple(ICanvas canvas, RectF rect)
    {
        float side = Math.Min(rect.Width, rect.Height);
        var outer = new RectF(rect.Center.X - side / 2f, rect.Center.Y - side / 2f, side, side);
        float corner = side * 0.26f;
        float frame = BorderPx;

        // Solid aluminium frame with the black screen inside.
        canvas.FillColor = Color.FromArgb("#3A3A3C");
        canvas.FillRoundedRectangle(outer, corner);

        var screen = new RectF(outer.Left + frame, outer.Top + frame, outer.Width - 2 * frame, outer.Height - 2 * frame);
        canvas.FillColor = ScreenBlack;
        canvas.FillRoundedRectangle(screen, corner - frame);

        Color accent = AccentColor.Alpha <= 0 || AccentColor == Colors.Black ? Color.FromArgb("#34C759") : AccentColor;
        DrawFaceTime(canvas, screen, Colors.White, accent, side * 0.26f);
    }

    // ---- Pixel Watch (round) ----------------------------------------------------

    void DrawPixel(ICanvas canvas, RectF rect)
    {
        float d = Math.Min(rect.Width, rect.Height);
        var face = new RectF(rect.Center.X - d / 2f, rect.Center.Y - d / 2f, d, d);

        canvas.FillColor = ScreenBlack;
        canvas.FillEllipse(face);

        Color accent = AccentColor.Alpha <= 0 || AccentColor == Colors.Black ? Color.FromArgb("#8AB4F8") : AccentColor;
        canvas.StrokeColor = accent;
        canvas.StrokeSize = BorderPx;
        float inset = d * 0.04f;
        canvas.DrawEllipse(face.Left + inset, face.Top + inset, face.Width - 2 * inset, face.Height - 2 * inset);

        // Keep the text within the round face.
        float m = d * 0.18f;
        var screen = new RectF(face.Left + m, face.Top + m, face.Width - 2 * m, face.Height - 2 * m);
        DrawFaceTime(canvas, screen, Colors.White, accent, d * 0.2f);
    }

    // Draws "HH:MM" (flashing colon) with the seconds (or AM/PM) right next to it.
    void DrawFaceTime(ICanvas canvas, RectF screen, Color digitColor, Color accent, float baseFont)
    {
        int h = Time.Hour;
        int displayH = Is24Hour ? h : (h % 12 == 0 ? 12 : h % 12);
        string sep = ColonLit ? ":" : " ";
        string time = $"{displayH:D2}{sep}{Time.Minute:D2}";
        string sub = ShowSeconds ? $"{Time.Second:D2}" : (Is24Hour ? "" : (h >= 12 ? "PM" : "AM"));

        if (string.IsNullOrEmpty(sub))
        {
            canvas.FontColor = digitColor;
            canvas.FontSize = baseFont;
            canvas.DrawString(time, screen.Left, screen.Top, screen.Width, screen.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        // Reserve the right portion for the seconds so they sit beside the time.
        float subW = screen.Width * 0.28f;
        var timeRect = new RectF(screen.Left, screen.Top, screen.Width - subW, screen.Height);
        var subRect = new RectF(screen.Right - subW, screen.Top, subW, screen.Height);

        canvas.FontColor = digitColor;
        canvas.FontSize = baseFont;
        canvas.DrawString(time, timeRect.Left, timeRect.Top, timeRect.Width, timeRect.Height,
            HorizontalAlignment.Right, VerticalAlignment.Center);

        canvas.FontColor = accent;
        canvas.FontSize = baseFont * 0.45f;
        canvas.DrawString(sub, subRect.Left + subW * 0.12f, subRect.Top, subRect.Width, subRect.Height,
            HorizontalAlignment.Left, VerticalAlignment.Center);
    }
}
