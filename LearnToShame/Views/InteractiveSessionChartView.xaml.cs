using LearnToShame.Helpers;
using LearnToShame.Models;
using LearnToShame.Services;

namespace LearnToShame.Views;

public partial class InteractiveSessionChartView : ContentView
{
    private readonly SessionChartDrawable _drawable;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    private DateTime _visibleMinDate;
    private DateTime _visibleMaxDate;
    private double _visibleMinDur;
    private double _visibleMaxDur;
    private bool _isZoomed;
    private float _panStartMinDateSec;
    private double _panStartMinDur;

    public InteractiveSessionChartView()
    {
        InitializeComponent();
        _drawable = new SessionChartDrawable { NoDataText = LocalizedStrings.Instance.Stats_NoData };
        ChartView.Drawable = _drawable;

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        ChartView.GestureRecognizers.Add(pinch);

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        ChartView.GestureRecognizers.Add(pan);

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        ChartView.GestureRecognizers.Add(tap);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += OnDoubleTapped;
        ChartView.GestureRecognizers.Add(doubleTap);

        ResetZoomButton.Text = LocalizedStrings.Instance.Stats_ResetZoom;
        ResetZoomButton.Clicked += (_, _) => ResetZoom();
    }

    public void SetSessions(IReadOnlyList<TrainingSession>? sessions)
    {
        _drawable.Sessions = sessions;
        _drawable.SelectedIndex = -1;
        TooltipBorder.IsVisible = false;
        if (sessions == null || sessions.Count == 0)
        {
            ChartView.Invalidate();
            ResetZoomButton.IsVisible = false;
            return;
        }
        var minDate = sessions.Min(s => s.Date);
        var maxDate = sessions.Max(s => s.Date);
        var maxDur = Math.Max(60, sessions.Max(s => s.DurationSeconds));
        var dateSpan = (maxDate - minDate).TotalSeconds;
        if (dateSpan < 1) dateSpan = 1;
        _visibleMinDate = minDate;
        _visibleMaxDate = maxDate;
        _visibleMinDur = 0;
        _visibleMaxDur = maxDur;
        _isZoomed = false;
        ResetZoomButton.IsVisible = false;
        ApplyVisibleToDrawable();
        ChartView.Invalidate();
    }

    public void RefreshChartColors()
    {
        if (Application.Current?.Resources.TryGetValue("Primary", out var primaryObj) != true || primaryObj is not Color primary)
            return;
        _drawable.LineColor = primary;
        _drawable.PointColor = primary;
        _drawable.FillGradientStart = primary.WithAlpha(0.25f);
        _drawable.FillGradientEnd = primary.WithAlpha(0f);
        var isDark = Application.Current.RequestedTheme == AppTheme.Dark;
        _drawable.GridColor = isDark ? Color.FromArgb("#333333") : Color.FromArgb("#E0E0E0");
        _drawable.TextColor = isDark ? Color.FromArgb("#AAAAAA") : Colors.Gray;
    }

    private void ApplyVisibleToDrawable()
    {
        _drawable.VisibleMinDate = _visibleMinDate;
        _drawable.VisibleMaxDate = _visibleMaxDate;
        _drawable.VisibleMinDur = _visibleMinDur;
        _drawable.VisibleMaxDur = _visibleMaxDur;
    }

    private void ResetZoom()
    {
        var sessions = _drawable.Sessions;
        if (sessions == null || sessions.Count == 0) return;
        var minDate = sessions.Min(s => s.Date);
        var maxDate = sessions.Max(s => s.Date);
        var maxDur = Math.Max(60, sessions.Max(s => s.DurationSeconds));
        var dateSpan = (maxDate - minDate).TotalSeconds;
        if (dateSpan < 1) dateSpan = 1;
        _visibleMinDate = minDate;
        _visibleMaxDate = maxDate;
        _visibleMinDur = 0;
        _visibleMaxDur = maxDur;
        _isZoomed = false;
        _drawable.SelectedIndex = -1;
        TooltipBorder.IsVisible = false;
        ResetZoomButton.IsVisible = false;
        ApplyVisibleToDrawable();
        ChartView.Invalidate();
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (_drawable.Sessions == null || _drawable.Sessions.Count == 0) return;
        if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
            return;
        var scale = (float)e.Scale;
        if (scale <= 0.01f) return;
        var spanDateSec = (_visibleMaxDate - _visibleMinDate).TotalSeconds;
        var spanDur = _visibleMaxDur - _visibleMinDur;
        var centerDateSec = (_visibleMinDate - DateTime.MinValue).TotalSeconds + spanDateSec / 2;
        var centerDur = (_visibleMinDur + _visibleMaxDur) / 2;
        spanDateSec /= scale;
        spanDur /= scale;
        if (spanDateSec < 10) spanDateSec = 10;
        if (spanDur < 5) spanDur = 5;
        _visibleMinDate = DateTime.MinValue.AddSeconds(centerDateSec - spanDateSec / 2);
        _visibleMaxDate = DateTime.MinValue.AddSeconds(centerDateSec + spanDateSec / 2);
        _visibleMinDur = Math.Max(0, centerDur - spanDur / 2);
        _visibleMaxDur = centerDur + spanDur / 2;
        _isZoomed = true;
        ResetZoomButton.IsVisible = true;
        ApplyVisibleToDrawable();
        ChartView.Invalidate();
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_drawable.Sessions == null || _drawable.Sessions.Count == 0) return;
        if (e.StatusType == GestureStatus.Started)
        {
            _panStartMinDateSec = (float)(_visibleMinDate - DateTime.MinValue).TotalSeconds;
            _panStartMinDur = _visibleMinDur;
        }
        if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            return;
        var r = _drawable.ChartArea;
        if (r.Width <= 0 || r.Height <= 0) return;
        var spanDate = (_visibleMaxDate - _visibleMinDate).TotalSeconds;
        var spanDur = _visibleMaxDur - _visibleMinDur;
        var datePerPx = spanDate / r.Width;
        var durPerPx = spanDur / r.Height;
        _visibleMinDate = DateTime.MinValue.AddSeconds(_panStartMinDateSec - e.TotalX * datePerPx);
        _visibleMaxDate = _visibleMinDate.AddSeconds(spanDate);
        _visibleMinDur = Math.Max(0, _panStartMinDur + e.TotalY * durPerPx);
        _visibleMaxDur = _visibleMinDur + spanDur;
        _isZoomed = true;
        ResetZoomButton.IsVisible = true;
        ApplyVisibleToDrawable();
        ChartView.Invalidate();
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (e?.Parameter != null) return;
        var pos = e?.GetPosition(ChartView);
        if (!pos.HasValue) return;
        var x = (float)pos.Value.X;
        var y = (float)pos.Value.Y;
        var idx = _drawable.HitTest(x, y, 40);
        if (idx < 0)
        {
            _drawable.SelectedIndex = -1;
            TooltipBorder.IsVisible = false;
        }
        else
        {
            _drawable.SelectedIndex = idx;
            var sessions = _drawable.Sessions!;
            var s = sessions[idx];
            var text = BuildTooltipText(s);
            TooltipLabel.Text = text;
            var chartLeft = SessionChartDrawable.PaddingLeft;
            var chartTop = SessionChartDrawable.PaddingTop;
            var positions = _drawable.PointPositions;
            if (idx < positions.Count)
            {
                var px = chartLeft + positions[idx].X;
                var py = chartTop + positions[idx].Y;
                TooltipBorder.TranslationX = px - 140;
                TooltipBorder.TranslationY = py - 120;
            }
            TooltipBorder.IsVisible = true;
        }
        ChartView.Invalidate();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e) => ResetZoom();

    private string BuildTooltipText(TrainingSession s)
    {
        var dateStr = s.Date.ToString("g", System.Globalization.CultureInfo.CurrentCulture);
        var durStr = s.DurationSeconds < 60 ? $"{s.DurationSeconds:F0} sec" : $"{s.DurationSeconds / 60:F1} min";
        var preStr = s.PreTriggerSeconds >= 0 ? (s.PreTriggerSeconds < 60 ? $"{s.PreTriggerSeconds:F0} s" : $"{s.PreTriggerSeconds / 60:F1} m") : "—";
        var trigStr = s.TriggerSeconds >= 0 ? (s.TriggerSeconds < 60 ? $"{s.TriggerSeconds:F0} s" : $"{s.TriggerSeconds / 60:F1} m") : "—";
        var onTrigger = s.TriggerPhaseUsed ? _loc.GetString("Yes") : _loc.GetString("No");
        var levelStr = _loc.GetString("Level_" + s.Level);
        return $"{_loc.GetString("Stats_Date")}: {dateStr}\n" +
               $"{_loc.GetString("Stats_Duration")}: {durStr}\n" +
               $"{_loc.GetString("Stats_PreTriggerTime")}: {preStr}\n" +
               $"{_loc.GetString("Stats_TriggerTime")}: {trigStr}\n" +
               $"{_loc.GetString("Stats_FinishedOnTrigger")}: {onTrigger}\n" +
               $"{_loc.GetString("Stats_Level")}: {levelStr}\n" +
               $"{_loc.GetString("Stats_ContentLevel")}: {s.ContentLevel}";
    }
}
