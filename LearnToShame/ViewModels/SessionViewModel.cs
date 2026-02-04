namespace LearnToShame.ViewModels;

public partial class SessionViewModel : ObservableObject
{
    private readonly RedditService _reddit;
    private readonly DatabaseService _db;
    private System.Timers.Timer _timer;
    private DateTime _startTime;

    [ObservableProperty]
    private ObservableCollection<string> _images = new();

    [ObservableProperty]
    private string _timerText = "00:00";
    
    [ObservableProperty]
    private bool _isSessionActive;

    [ObservableProperty]
    private bool _isLoading;

    public SessionViewModel(RedditService reddit, DatabaseService db)
    {
        _reddit = reddit;
        _db = db;
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var elapsed = DateTime.Now - _startTime;
        TimerText = elapsed.ToString(@"mm\:ss");
    }

    public async Task StartSessionAsync()
    {
        IsLoading = true;
        IsSessionActive = true;
        Images.Clear();
        
        var progress = await _db.GetUserProgressAsync();
        
        int redditLevel = 1;
        switch(progress.CurrentLevel)
        {
            case DeveloperLevel.Intern: redditLevel = 1; break;
            case DeveloperLevel.Junior: redditLevel = 2; break;
            case DeveloperLevel.Middle: redditLevel = 4; break;
            case DeveloperLevel.Senior: redditLevel = 6; break;
            case DeveloperLevel.Lead: redditLevel = 8; break;
        }

        var posts = await _reddit.GetPostsForLevelAsync(redditLevel);
        
        // Pick random 3 images
        var random = new Random();
        var selectedPosts = posts.OrderBy(x => random.Next()).Take(3).ToList();
        
        foreach(var post in selectedPosts)
        {
            Images.Add(post.Url);
        }

        IsLoading = false;
        _startTime = DateTime.Now;
        _timer.Start();
    }

    [RelayCommand]
    private async Task FinishSession()
    {
        _timer.Stop();
        IsSessionActive = false;
        var duration = DateTime.Now - _startTime;
        
        var progress = await _db.GetUserProgressAsync();
        var session = new TrainingSession
        {
            Date = DateTime.Now,
            DurationSeconds = duration.TotalSeconds,
            Level = progress.CurrentLevel
        };
        await _db.AddSessionAsync(session);
        
        progress.SessionsCompletedAtCurrentLevel++;
        await _db.UpdateUserProgressAsync(progress);

        MainThread.BeginInvokeOnMainThread(async () => {
             await Shell.Current.DisplayAlertAsync("Session Complete", $"You lasted {TimerText}. Progress saved.", "OK");
             await Shell.Current.GoToAsync("..");
        });
    }
}
