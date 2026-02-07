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

    [ObservableProperty]
    private ObservableCollection<ImageSource> _images = new();

    [ObservableProperty]
    private string _timerText = "00:00";
    
    [ObservableProperty]
    private bool _isSessionActive;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Текущее фото в сессии (один Image на Mac вместо сломанной CarouselView).</summary>
    [ObservableProperty]
    private ImageSource? _currentImage;

    [ObservableProperty]
    private int _currentImageIndex;

    [ObservableProperty]
    private bool _hasMultipleImages;

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
            Images.Clear();
        });

        var paths = _userContent.GetUserImagePaths();
        Debug.WriteLine($"[Session] GetUserImagePaths: count={paths.Count}");
        for (var i = 0; i < paths.Count; i++)
            Debug.WriteLine($"[Session]   [{i}] {paths[i]}");
        if (paths.Count == 0)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                IsLoading = false;
                await Shell.Current.DisplayAlertAsync(_loc.GetString("Alert_ShopTitle"), _loc.GetString("NoImagesHint"), _loc.GetString("OK"));
            });
            return;
        }

        var rnd = new Random();
        var selectedPaths = PickRandomInRandomOrder(paths, ImagesPerSession, rnd);
        Debug.WriteLine($"[Session] Selected for session: {selectedPaths.Count} paths");
        for (var i = 0; i < selectedPaths.Count; i++)
            Debug.WriteLine($"[Session]   selected[{i}] {selectedPaths[i]}");

        // На Mac Catalyst FromFile часто не отображает изображения — загружаем через поток.
        var imageSources = new List<ImageSource>();
        for (var idx = 0; idx < selectedPaths.Count; idx++)
        {
            var path = selectedPaths[idx];
            var exists = File.Exists(path);
            Debug.WriteLine($"[Session] Load [{idx}] path={path} exists={exists}");
            try
            {
                var bytes = await Task.Run(() => File.ReadAllBytes(path));
                Debug.WriteLine($"[Session] Load [{idx}] read OK, bytes={bytes?.Length ?? 0}");
                var captured = bytes;
                imageSources.Add(ImageSource.FromStream(() => new MemoryStream(captured)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Session] Load [{idx}] FAILED: {ex.Message}");
            }
        }
        Debug.WriteLine($"[Session] imageSources count={imageSources.Count}, adding to Images on main thread");

        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var src in imageSources)
                Images.Add(src);
            CurrentImageIndex = 0;
            CurrentImage = Images.Count > 0 ? Images[0] : null;
            HasMultipleImages = Images.Count > 1;
            Debug.WriteLine($"[Session] Images.Count={Images.Count}");
            IsLoading = false;
            _startTime = DateTime.Now;
            _timer.Start();
        });
    }

    [RelayCommand]
    private void NextImage()
    {
        if (Images.Count <= 1) return;
        CurrentImageIndex = (CurrentImageIndex + 1) % Images.Count;
        CurrentImage = Images[CurrentImageIndex];
    }

    [RelayCommand]
    private void PrevImage()
    {
        if (Images.Count <= 1) return;
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
        var duration = DateTime.Now - _startTime;
        var durationSeconds = duration.TotalSeconds;

        var progress = await _db.GetUserProgressAsync();
        var contentLevel = Math.Clamp(progress.ContentLevel, 1, 8);

        var session = new TrainingSession
        {
            Date = DateTime.Now,
            DurationSeconds = durationSeconds,
            Level = progress.CurrentLevel,
            ContentLevel = contentLevel
        };
        await _db.AddSessionAsync(session);

        // Прогрессия контента (1–8): 5 сессий подряд < 1 мин на одном уровне → переход на уровень выше (более прикрытый контент).
        const double fastThresholdSeconds = 60;
        const int fastSessionsToLevelUp = 5;
        if (durationSeconds < fastThresholdSeconds)
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

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.DisplayAlertAsync(
                _loc.GetString("Alert_SessionCompleteTitle"),
                _loc.GetString("Alert_SessionCompleteMessage", TimerText),
                _loc.GetString("OK"));
            await Shell.Current.GoToAsync("..");
        });
    }
}
