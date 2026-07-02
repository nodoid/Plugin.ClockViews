using Microsoft.Maui.Graphics;

namespace Plugin.ClockViews;

/// <summary>The clock face style used for each zone in a <see cref="MultiTimeClockDrawable"/>.</summary>
public enum MultiClockFace
{
    Analog,
    Valve,
    Flip,
    Melt,
    Beam,
    Watch,
}

/// <summary>One clock of a <see cref="MultiTimeClockDrawable"/>: a zone name and its local time.</summary>
public readonly record struct MultiTimeRow(string Label, DateTime Time);

/// <summary>
/// Draws a multi-timezone ("world") clock: up to four zone clocks, each a mini clock face of the
/// chosen <see cref="Face"/> with the zone name centred above it. Faces are laid out in a 2×2 grid,
/// except <see cref="MultiClockFace.Valve"/> which stacks the clocks vertically (its tube row is too
/// wide for a grid cell). Composes the individual clock drawables. Uses Microsoft.Maui.Graphics only.
/// </summary>
public class MultiTimeClockDrawable : IDrawable
{
    readonly AnalogClockDrawable _analog = new();
    readonly ValveClockDrawable _valve = new();
    readonly FlipClockDrawable _flip = new();
    readonly MeltingClockDrawable _melt = new();
    readonly DematerialiseClockDrawable _demat = new();
    readonly WatchClockDrawable _watch = new();

    public IReadOnlyList<MultiTimeRow> Rows { get; set; } = Array.Empty<MultiTimeRow>();
    public MultiClockFace Face { get; set; } = MultiClockFace.Analog;
    public bool IsSecondsShown { get; set; }
    public bool Is24Hour { get; set; } = true;
    public bool ColonLit { get; set; } = true;
    public SciFiTheme SciFiTheme { get; set; } = SciFiTheme.DrWho;
    public WatchTheme WatchTheme { get; set; } = WatchTheme.PixelWatch;
    public Color DigitColor { get; set; } = Colors.Black;
    public Color BackgroundColor { get; set; } = Colors.Transparent;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (BackgroundColor != Colors.Transparent)
        {
            canvas.FillColor = BackgroundColor;
            canvas.FillRectangle(dirtyRect);
        }

        int n = Rows.Count;
        if (n == 0)
            return;

        const float pad = 8f;
        float availW = dirtyRect.Width - 2 * pad;
        float availH = dirtyRect.Height - 2 * pad;
        if (availW <= 0 || availH <= 0)
            return;

        // Valve stacks vertically; every other face uses a 2×2 grid.
        bool stack = Face == MultiClockFace.Valve;
        int cols = stack ? 1 : (n == 1 ? 1 : 2);
        int rowCount = (n + cols - 1) / cols;
        float cellW = availW / cols;
        float cellH = availH / rowCount;

        for (int i = 0; i < n; i++)
        {
            int c = i % cols;
            int r = i / cols;
            var cell = new RectF(dirtyRect.Left + pad + c * cellW, dirtyRect.Top + pad + r * cellH, cellW, cellH);
            var row = Rows[i];

            // Zone name, centred above the clock.
            float labelH = cellH * 0.2f;
            canvas.FontColor = DigitColor.WithAlpha(0.8f);
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.FontSize = Math.Min(labelH * 0.7f, 16f);
            canvas.DrawString(row.Label, cell.Left, cell.Top, cell.Width, labelH,
                HorizontalAlignment.Center, VerticalAlignment.Center);

            var faceRect = new RectF(cell.Left + 4, cell.Top + labelH, cell.Width - 8, cell.Height - labelH - 4);
            if (faceRect.Width <= 0 || faceRect.Height <= 0)
                continue;

            canvas.SaveState();
            canvas.ClipRectangle(faceRect.Left, faceRect.Top, faceRect.Width, faceRect.Height);
            DrawFace(canvas, faceRect, row.Time);
            canvas.RestoreState();
        }
    }

    void DrawFace(ICanvas canvas, RectF rect, DateTime t)
    {
        switch (Face)
        {
            case MultiClockFace.Valve:
                _valve.Digits = ValveDisplay.Digits(t.TimeOfDay, IsSecondsShown);
                _valve.ColonAfter = IsSecondsShown ? new[] { 1, 3 } : new[] { 1 };
                _valve.ColonLit = ColonLit;
                _valve.Draw(canvas, rect);
                break;

            case MultiClockFace.Flip:
                _flip.Panels = FlipPanels(t);
                _flip.DigitColor = DigitColor;
                _flip.Draw(canvas, rect);
                break;

            case MultiClockFace.Melt:
                _melt.Cells = MeltCells(t);
                _melt.DigitColor = DigitColor;
                _melt.Draw(canvas, rect);
                break;

            case MultiClockFace.Beam:
                _demat.Cells = DematCells(t);
                _demat.DigitColor = DigitColor;
                _demat.Theme = SciFiTheme;
                _demat.Draw(canvas, rect);
                break;

            case MultiClockFace.Watch:
                _watch.Time = t;
                _watch.ShowSeconds = IsSecondsShown;
                _watch.Is24Hour = Is24Hour;
                _watch.ColonLit = ColonLit;
                _watch.Theme = WatchTheme;
                _watch.AccentColor = DigitColor;
                _watch.Draw(canvas, rect);
                break;

            default: // Analog
                _analog.Time = t.TimeOfDay;
                _analog.ShowSecondHand = IsSecondsShown;
                _analog.FaceColor = DigitColor;
                _analog.HandColor = DigitColor;
                _analog.Draw(canvas, rect);
                break;
        }
    }

    // --- static-frame input builders (no animation for the world clock) ---

    List<FlipPanel> FlipPanels(DateTime t)
    {
        var list = new List<FlipPanel>();
        int h = t.Hour;
        bool ampm = !Is24Hour;
        string hh = ampm ? (h % 12 == 0 ? 12 : h % 12).ToString("D2") : h.ToString("D2");

        list.Add(new FlipPanel(hh, hh, 0, false));
        list.Add(new FlipPanel("", "", 0, false, IsSeparator: true, ColonLit: ColonLit));
        list.Add(new FlipPanel(t.Minute.ToString("D2"), t.Minute.ToString("D2"), 0, false));
        if (IsSecondsShown)
        {
            list.Add(new FlipPanel("", "", 0, false, IsSeparator: true, ColonLit: ColonLit));
            list.Add(new FlipPanel(t.Second.ToString("D2"), t.Second.ToString("D2"), 0, false));
        }
        if (ampm)
            list.Add(new FlipPanel(h >= 12 ? "PM" : "AM", h >= 12 ? "PM" : "AM", 0, true));
        return list;
    }

    string DigitsFor(DateTime t)
    {
        int h = Is24Hour ? t.Hour : (t.Hour % 12 == 0 ? 12 : t.Hour % 12);
        string s = h.ToString("D2") + t.Minute.ToString("D2");
        if (IsSecondsShown)
            s += t.Second.ToString("D2");
        return s;
    }

    List<MeltCell> MeltCells(DateTime t)
    {
        var list = new List<MeltCell>();
        string digits = DigitsFor(t);
        string colon = ColonLit ? ":" : " ";
        for (int i = 0; i < digits.Length; i++)
        {
            list.Add(new MeltCell(digits[i].ToString(), digits[i].ToString(), 0, false));
            if (i == 1 || (IsSecondsShown && i == 3))
                list.Add(new MeltCell(colon, colon, 0, true));
        }
        return list;
    }

    List<DematerialiseCell> DematCells(DateTime t)
    {
        var list = new List<DematerialiseCell>();
        string digits = DigitsFor(t);
        string colon = ColonLit ? ":" : " ";
        for (int i = 0; i < digits.Length; i++)
        {
            list.Add(new DematerialiseCell(digits[i].ToString(), digits[i].ToString(), 0, false));
            if (i == 1 || (IsSecondsShown && i == 3))
                list.Add(new DematerialiseCell(colon, colon, 0, true));
        }
        return list;
    }
}
