using LearnToShame.Services;

namespace LearnToShame.ViewModels;

public partial class RoadmapViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly GamificationService _game;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private List<RoadmapTask> _allTasks = new();

    public static readonly int[] PageSizeOptions = { 10, 25, 50 };

    [ObservableProperty]
    private UserProgress? _userProgress;

    [ObservableProperty]
    private ObservableCollection<RoadmapTask> _tasksOnCurrentPage = new();
    
    [ObservableProperty]
    private string _currentLevelName = "Intern";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _selectedPageSize = 10;

    partial void OnSelectedPageSizeChanged(int value)
    {
        ApplyPageSize();
    }

    public string PageInfo => _loc.GetString("PageOf", CurrentPage, TotalPages);
    public bool CanGoPrev => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public RoadmapViewModel(DatabaseService db, GamificationService game)
    {
        _db = db;
        _game = game;
        _loc.CultureChanged += (_, _) =>
        {
            RefreshLocalizedProps();
        };
    }

    private void RefreshLocalizedProps()
    {
        if (UserProgress != null)
            CurrentLevelName = _loc.GetString("Level_" + UserProgress.CurrentLevel);
        OnPropertyChanged(nameof(PageInfo));
        // Force list refresh so Reward format converter re-runs
        var list = TasksOnCurrentPage.ToList();
        TasksOnCurrentPage = new ObservableCollection<RoadmapTask>(list);
    }

    public async Task InitializeAsync()
    {
        try
        {
            UserProgress = await _db.GetUserProgressAsync();
            CurrentLevelName = _loc.GetString("Level_" + UserProgress.CurrentLevel);
            _allTasks = await _db.GetTasksAsync();
            ApplyPageSize();
            CalculateTotalProgress();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RoadmapViewModel.InitializeAsync: {ex.Message}");
        }
    }

    private void ApplyPageSize()
    {
        var size = Math.Max(1, SelectedPageSize);
        TotalPages = Math.Max(1, (_allTasks.Count + size - 1) / size);
        CurrentPage = 1;
        FillCurrentPage();
        NotifyPaginationChanged();
    }

    private void FillCurrentPage()
    {
        var size = Math.Max(1, SelectedPageSize);
        var start = (CurrentPage - 1) * size;
        var page = _allTasks.Skip(start).Take(size).ToList();
        var newPage = new ObservableCollection<RoadmapTask>();
        foreach (var task in page)
            newPage.Add(task);
        TasksOnCurrentPage = newPage;
    }

    private void CalculateTotalProgress()
    {
        if (_allTasks.Count == 0) return;
        ProgressValue = (double)_allTasks.Count(t => t.IsCompleted) / _allTasks.Count;
    }

    [RelayCommand]
    private async Task GoToShop()
    {
        await Shell.Current.GoToAsync("//ShopPage");
    }

    private void NotifyPaginationChanged()
    {
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageInfo));
    }

    [RelayCommand]
    private void SetPageSize(object parameter)
    {
        var size = parameter switch
        {
            int i when i is 10 or 25 or 50 => i,
            string s when int.TryParse(s, out var n) && n is 10 or 25 or 50 => n,
            _ => 0
        };
        if (size > 0)
            SelectedPageSize = size;
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage <= 1) return;
        CurrentPage--;
        FillCurrentPage();
        NotifyPaginationChanged();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage >= TotalPages) return;
        CurrentPage++;
        FillCurrentPage();
        NotifyPaginationChanged();
    }

    [RelayCommand]
    private async Task ToggleTask(RoadmapTask task)
    {
        if (task.IsCompleted) return; // Already done

        bool confirm = await Shell.Current.DisplayAlertAsync(
            _loc.GetString("Alert_CompleteTaskTitle"),
            _loc.GetString("Alert_CompleteTaskMessage", task.Title, task.PointsReward),
            _loc.GetString("Yes"),
            _loc.GetString("No"));
        if (confirm)
        {
            await _game.CompleteTaskAsync(task);
            await InitializeAsync(); // Refresh
        }
    }
}
