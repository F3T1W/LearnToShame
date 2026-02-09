using System.Diagnostics;
using LearnToShame.Services;

namespace LearnToShame.ViewModels;

public partial class SessionViewModel : ObservableObject
{
    private const int ImagesPerSession = 20;

    private readonly DatabaseService _db;
    private readonly UserContentService _userContent;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private System.Timers.Timer _timer;
    private DateTime _startTime;
    private DateTime _triggerSwitchTime; // когда нажали Switch to Trigger
    private List<ImageSource> _triggerImageSources = new();

    [ObservableProperty]
    private ObservableCollection<ImageSource> _images = new();

    [ObservableProperty]
    private string _timerText = "00:00";

    [ObservableProperty]
    private bool _isSessionActive;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ImageSource? _currentImage;

    [ObservableProperty]
    private int _currentImageIndex;

    [ObservableProperty]
    private bool _hasMultipleImages;

    /// <summary>True = показываем Pre-Trigger, false = показываем Trigger (перед оргазмом).</summary>
    [ObservableProperty]
    private bool _isPreTriggerPhase = true;

    public SessionViewModel(DatabaseService db, UserContentService userContent)
    {
        _db = db;
        _userContent = userContent;
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var elapsed = DateTime.Now - _startTime;
        var text = elapsed.ToString(@"mm\:ss");
        MainThread.BeginInvokeOnMainThread(() => TimerText = text);
    }

    public async Task StartSessionAsync()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsLoading = true;
            IsSessionActive = true;
            IsPreTriggerPhase = true;
            Images.Clear();
            _triggerImageSources.Clear();
        });

        var prePaths = _userContent.GetPreTriggerPaths();
        var triggerPaths = _userContent.GetTriggerPaths();
        if (prePaths.Count == 0 || triggerPaths.Count == 0)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                IsLoading = false;
                await Shell.Current.DisplayAlertAsync(_loc.GetString("Alert_ShopTitle"), _loc.GetString("NeedBothPreAndTrigger"), _loc.GetString("OK"));
            });
            return;
        }

        var rnd = new Random();
        var selectedPre = PickRandomInRandomOrder(prePaths, ImagesPerSession, rnd);
        _triggerImageSources = await LoadImageSourcesAsync(triggerPaths);
        var preSources = await LoadImageSourcesAsync(selectedPre);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Images.Clear();
            foreach (var src in preSources)
                Images.Add(src);
            CurrentImageIndex = 0;
            CurrentImage = Images.Count > 0 ? Images[0] : null;
            HasMultipleImages = Images.Count > 1;
            IsLoading = false;
            _startTime = DateTime.Now;
            _timer.Start();
        });
    }

    private static async Task<List<ImageSource>> LoadImageSourcesAsync(IList<string> paths)
    {
        var list = new List<ImageSource>();
        foreach (var path in paths)
        {
            try
            {
                var bytes = await Task.Run(() => File.ReadAllBytes(path));
                var captured = bytes;
                list.Add(ImageSource.FromStream(() => new MemoryStream(captured)));
            }
            catch { }
        }
        return list;
    }

    [RelayCommand]
    private void SwitchToTrigger()
    {
        if (_triggerImageSources.Count == 0) return;
        _triggerSwitchTime = DateTime.Now;
        IsPreTriggerPhase = false;
        var rnd = new Random();
        var triggerImage = _triggerImageSources[rnd.Next(_triggerImageSources.Count)];
        Images.Clear();
        Images.Add(triggerImage);
        CurrentImageIndex = 0;
        CurrentImage = triggerImage;
        HasMultipleImages = false;
    }

    [RelayCommand]
    private void NextImage()
    {
        if (!IsPreTriggerPhase || Images.Count <= 1) return;
        CurrentImageIndex = (CurrentImageIndex + 1) % Images.Count;
        CurrentImage = Images[CurrentImageIndex];
    }

    [RelayCommand]
    private void PrevImage()
    {
        if (!IsPreTriggerPhase || Images.Count <= 1) return;
        CurrentImageIndex = CurrentImageIndex == 0 ? Images.Count - 1 : CurrentImageIndex - 1;
        CurrentImage = Images[CurrentImageIndex];
    }

    private static List<string> PickRandomInRandomOrder(IList<string> source, int count, Random rnd)
    {
        if (source.Count == 0) return new List<string>();
        var list = source.Count >= count
            ? source.OrderBy(_ => rnd.Next()).Take(count).ToList()
            : Enumerable.Range(0, count).Select(_ => source[rnd.Next(source.Count)]).ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    [RelayCommand]
    private async Task FinishSession()
    {
        _timer.Stop();
        IsSessionActive = false;
        var endTime = DateTime.Now;
        var duration = endTime - _startTime;
        var durationSeconds = duration.TotalSeconds;
        var triggerPhaseUsed = !IsPreTriggerPhase;
        var preTriggerSeconds = triggerPhaseUsed ? (_triggerSwitchTime - _startTime).TotalSeconds : durationSeconds;
        var triggerSeconds = triggerPhaseUsed ? (endTime - _triggerSwitchTime).TotalSeconds : 0.0;

        var progress = await _db.GetUserProgressAsync();
        var contentLevel = Math.Clamp(progress.ContentLevel, 1, 8);

        var session = new TrainingSession
        {
            Date = endTime,
            DurationSeconds = durationSeconds,
            Level = progress.CurrentLevel,
            ContentLevel = contentLevel,
            TriggerPhaseUsed = triggerPhaseUsed,
            PreTriggerSeconds = preTriggerSeconds,
            TriggerSeconds = triggerSeconds
        };
        await _db.AddSessionAsync(session);

        const double fastThresholdSeconds = 60;
        const int fastSessionsToLevelUp = 5;
        bool fastAndCorrect = triggerPhaseUsed && durationSeconds < fastThresholdSeconds;
        if (fastAndCorrect)
        {
            progress.FastSessionsInRow++;
            if (progress.FastSessionsInRow >= fastSessionsToLevelUp && progress.ContentLevel < 8)
            {
                progress.ContentLevel++;
                progress.FastSessionsInRow = 0;
            }
        }
        else
        {
            progress.FastSessionsInRow = 0;
        }

        await _db.UpdateUserProgressAsync(progress);

        string message;
        if (fastAndCorrect)
            message = _loc.GetString("SessionSuccessMessage", progress.FastSessionsInRow);
        else if (!triggerPhaseUsed)
            message = _loc.GetString("SessionRemindTrigger");
        else
            message = _loc.GetString("SessionOverTime");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.DisplayAlertAsync(
                _loc.GetString("Alert_SessionCompleteTitle"),
                $"{_loc.GetString("Alert_SessionCompleteMessage", TimerText)}\n\n{message}",
                _loc.GetString("OK"));
            await Shell.Current.GoToAsync("..");
        });
    }
}
