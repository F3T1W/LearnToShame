using Microsoft.Maui.Graphics;
using LearnToShame.Models;

namespace LearnToShame.Helpers;

/// <summary>Рисует линейный график с поддержкой видимой области (зум/пан) и выделенной точки. По X — дата/время, по Y — длительность (сек).</summary>
public class SessionChartDrawable : IDrawable
{
    public const float PaddingLeft = 52;
    public const float PaddingRight = 16;
    public const float PaddingTop = 16;
    public const float PaddingBottom = 36;
    private const float GridLineStrokeWidth = 1f;
    private const float LineStrokeWidth = 3f;
    private const float PointRadius = 5f;
    private const float SelectedPointRadius = 8f;

    /// <summary>Полные сессии для графика и тултипа. Если задано — используются видимая область и выделение.</summary>
    public IReadOnlyList<TrainingSession>? Sessions { get; set; }

    /// <summary>Видимая область по X (дата). Задаётся извне при зуме/панораме.</summary>
    public DateTime VisibleMinDate { get; set; }
    public DateTime VisibleMaxDate { get; set; }
    /// <summary>Видимая область по Y (длительность, сек).</summary>
    public double VisibleMinDur { get; set; }
    public double VisibleMaxDur { get; set; }
    /// <summary>Индекс выделенной точки (-1 — нет).</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>После Draw() — координаты точек в области графика (chart area), для позиционирования тултипа.</summary>
    public IReadOnlyList<PointF> PointPositions { get; private set; } = Array.Empty<PointF>();

    /// <summary>Границы области графика в последнем Draw (для конвертации координат в представлении).</summary>
    public RectF ChartArea { get; private set; }

    public Color LineColor { get; set; } = Color.FromArgb("#512BD4");
    public Color PointColor { get; set; } = Color.FromArgb("#512BD4");
    public Color SelectedPointColor { get; set; } = Color.FromArgb("#FFD700");
    public Color GridColor { get; set; } = Color.FromArgb("#E0E0E0");
    public Color TextColor { get; set; } = Colors.Gray;
    public Color FillGradientStart { get; set; } = Color.FromArgb("#20512BD4");
    public Color FillGradientEnd { get; set; } = Color.FromArgb("#00512BD4");
    public string NoDataText { get; set; } = "No sessions yet";

    /// <summary>Преобразует экранные координаты (относительно view) в данные: дата и длительность. Возвращает (date, durationSeconds) или null если вне области.</summary>
    public (DateTime date, double durationSeconds)? ScreenToData(float viewX, float viewY)
    {
        var r = ChartArea;
        if (r.Width <= 0 || r.Height <= 0) return null;
        var spanDate = (VisibleMaxDate - VisibleMinDate).TotalSeconds;
        var spanDur = VisibleMaxDur - VisibleMinDur;
        if (spanDate < 1) spanDate = 1;
        if (spanDur < 1) spanDur = 1;
        var t = (viewX - r.Left) / r.Width;
        var u = 1 - (viewY - r.Top) / r.Height;
        var date = VisibleMinDate.AddSeconds(t * spanDate);
        var dur = VisibleMinDur + u * spanDur;
        return (date, dur);
    }

    /// <summary>Находит индекс сессии, ближайшей к точке (viewX, viewY). Порог — в пикселях.</summary>
    public int HitTest(float viewX, float viewY, float maxDistancePx = 30f)
    {
        if (Sessions == null || Sessions.Count == 0) return -1;
        var positions = PointPositions;
        if (positions.Count == 0) return -1;
        var chartLeft = ChartArea.Left;
        var chartTop = ChartArea.Top;
        int best = -1;
        float bestD = maxDistancePx * maxDistancePx;
        for (var i = 0; i < positions.Count; i++)
        {
            var px = chartLeft + positions[i].X;
            var py = chartTop + positions[i].Y;
            var dx = viewX - px;
            var dy = viewY - py;
            var d = dx * dx + dy * dy;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var sessions = Sessions;
        if (sessions == null || sessions.Count == 0)
        {
            DrawNoData(canvas, dirtyRect);
            PointPositions = Array.Empty<PointF>();
            return;
        }

        var w = dirtyRect.Width;
        var h = dirtyRect.Height;
        var chartLeft = PaddingLeft;
        var chartRight = w - PaddingRight;
        var chartTop = PaddingTop;
        var chartBottom = h - PaddingBottom;
        var chartW = chartRight - chartLeft;
        var chartH = chartBottom - chartTop;
        ChartArea = new RectF(chartLeft, chartTop, chartW, chartH);

        var minDate = VisibleMinDate;
        var maxDate = VisibleMaxDate;
        var minDur = VisibleMinDur;
        var maxDur = VisibleMaxDur;
        var dateSpan = (maxDate - minDate).TotalSeconds;
        if (dateSpan < 1) dateSpan = 1;
        var durSpan = maxDur - minDur;
        if (durSpan < 1) durSpan = 1;

        // Сетка по Y
        var yStep = maxDur <= 60 ? 15.0 : (maxDur <= 120 ? 30.0 : 60.0);
        for (var d = Math.Ceiling(minDur / yStep) * yStep; d <= maxDur; d += yStep)
        {
            var y = chartBottom - (float)((d - minDur) / durSpan * chartH);
            canvas.StrokeColor = GridColor;
            canvas.StrokeSize = GridLineStrokeWidth;
            canvas.DrawLine(chartLeft, y, chartRight, y);
        }

        // Сетка по X
        var n = 5;
        for (var i = 0; i <= n; i++)
        {
            var t = (double)i / n;
            var x = chartLeft + (float)(t * chartW);
            canvas.StrokeColor = GridColor;
            canvas.StrokeSize = GridLineStrokeWidth;
            canvas.DrawLine(x, chartTop, x, chartBottom);
        }

        // Подписи оси Y
        canvas.FontColor = TextColor;
        canvas.FontSize = 10f;
        for (var d = Math.Ceiling(minDur / yStep) * yStep; d <= maxDur; d += yStep)
        {
            var y = chartBottom - (float)((d - minDur) / durSpan * chartH);
            var label = d < 60 ? $"{d:F0}s" : $"{d / 60:F0}m";
            canvas.DrawString(label, chartLeft - 44, y - 4, 40, 12, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Подписи оси X
        for (var i = 0; i <= n; i++)
        {
            var t = (double)i / n;
            var x = chartLeft + (float)(t * chartW);
            var dt = minDate.AddSeconds(t * dateSpan);
            var label = dt.ToString("g", System.Globalization.CultureInfo.CurrentCulture);
            canvas.DrawString(label, x - 40, chartBottom + 4, 80, 14, HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        // Координаты всех точек (в области графика: 0..chartW, 0..chartH) для хит-теста и тултипа
        var pointList = new List<PointF>();
        var path = new PathF();
        var first = true;
        foreach (var s in sessions)
        {
            var tx = (s.Date - minDate).TotalSeconds / dateSpan;
            var ty = (s.DurationSeconds - minDur) / durSpan;
            var x = (float)(tx * chartW);
            var y = (float)((1 - ty) * chartH);
            pointList.Add(new PointF(x, y));
            var absX = chartLeft + x;
            var absY = chartTop + y;
            if (first) { path.MoveTo(absX, absY); first = false; }
            else path.LineTo(absX, absY);
        }
        PointPositions = pointList;

        if (path.Count > 0)
        {
            var fillPath = new PathF(path);
            fillPath.LineTo(chartRight, chartBottom);
            fillPath.LineTo(chartLeft, chartBottom);
            fillPath.Close();
            var linearGradient = new LinearGradientPaint
            {
                StartColor = FillGradientStart,
                EndColor = FillGradientEnd,
                StartPoint = new Point(0.5f, 0),
                EndPoint = new Point(0.5f, 1)
            };
            canvas.SetFillPaint(linearGradient, new RectF(chartLeft, chartTop, chartW, chartH));
            canvas.FillPath(fillPath);

            canvas.StrokeColor = LineColor;
            canvas.StrokeSize = LineStrokeWidth;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.DrawPath(path);

            var idx = 0;
            foreach (var s in sessions)
            {
                var tx = (s.Date - minDate).TotalSeconds / dateSpan;
                var ty = (s.DurationSeconds - minDur) / durSpan;
                var x = chartLeft + (float)(tx * chartW);
                var y = chartTop + (float)((1 - ty) * chartH);
                var isSelected = idx == SelectedIndex;
                var r = isSelected ? SelectedPointRadius : PointRadius;
                canvas.FillColor = isSelected ? SelectedPointColor : PointColor;
                canvas.FillCircle(x, y, r);
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = isSelected ? 2f : 1.5f;
                canvas.DrawCircle(x, y, r);
                idx++;
            }
        }
    }

    private void DrawNoData(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = TextColor;
        canvas.FontSize = 16f;
        canvas.DrawString(NoDataText, dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
