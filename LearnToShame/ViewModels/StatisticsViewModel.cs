using LearnToShame.Models;
using LearnToShame.Services;

namespace LearnToShame.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly DatabaseService _db;

    [ObservableProperty]
    private IReadOnlyList<(DateTime Date, double DurationSeconds)> _chartPoints = Array.Empty<(DateTime, double)>();

    [ObservableProperty]
    private IReadOnlyList<TrainingSession> _sessions = Array.Empty<TrainingSession>();

    [ObservableProperty]
    private bool _isLoaded;

    public StatisticsViewModel(DatabaseService db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var sessions = await _db.GetSessionsForStatsAsync(100);
            Sessions = sessions;
            ChartPoints = sessions.Select(s => (s.Date, s.DurationSeconds)).ToList();
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"StatisticsViewModel.InitializeAsync: {ex.Message}");
            IsLoaded = true;
        }
    }
}
