namespace LearnToShame.ViewModels;

public partial class RoadmapViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly GamificationService _game;

    [ObservableProperty]
    private UserProgress? _userProgress;

    [ObservableProperty]
    private ObservableCollection<RoadmapTask> _tasks = new();
    
    [ObservableProperty]
    private string _currentLevelName = "Intern";

    [ObservableProperty]
    private double _progressValue;

    public RoadmapViewModel(DatabaseService db, GamificationService game)
    {
        _db = db;
        _game = game;
    }

    public async Task InitializeAsync()
    {
        UserProgress = await _db.GetUserProgressAsync();
        CurrentLevelName = UserProgress.CurrentLevel.ToString();
        
        var tasks = await _db.GetTasksAsync();
        Tasks.Clear();
        foreach (var task in tasks)
        {
            Tasks.Add(task);
        }
        
        CalculateTotalProgress();
    }

    private void CalculateTotalProgress()
    {
        if (Tasks.Count == 0) return;
        ProgressValue = (double)Tasks.Count(t => t.IsCompleted) / Tasks.Count;
    }

    [RelayCommand]
    private async Task GoToShop()
    {
        await Shell.Current.GoToAsync("//ShopPage");
    }

    [RelayCommand]
    private async Task ToggleTask(RoadmapTask task)
    {
        if (task.IsCompleted) return; // Already done

        bool confirm = await Shell.Current.DisplayAlertAsync("Complete Task", $"Mark '{task.Title}' as complete for {task.PointsReward} points?", "Yes", "No");
        if (confirm)
        {
            await _game.CompleteTaskAsync(task);
            await InitializeAsync(); // Refresh
        }
    }
}
