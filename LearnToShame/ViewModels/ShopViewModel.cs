using LearnToShame.Services;

namespace LearnToShame.ViewModels;

public partial class ShopViewModel : ObservableObject
{
    private readonly GamificationService _game;
    private readonly DatabaseService _db;
    private readonly UserContentService _userContent;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    [ObservableProperty]
    private int _points;

    [ObservableProperty]
    private string _contentStatusText = "";

    public ShopViewModel(GamificationService game, DatabaseService db, UserContentService userContent)
    {
        _game = game;
        _db = db;
        _userContent = userContent;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var progress = await _db.GetUserProgressAsync();
            Points = progress.CurrentPoints;
            UpdateContentStatus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ShopViewModel.InitializeAsync: {ex.Message}");
            Points = 0;
            UpdateContentStatus();
        }
    }

    private void UpdateContentStatus()
    {
        var pre = _userContent.CountPreTrigger;
        var trig = _userContent.CountTrigger;
        ContentStatusText = _loc.GetString("ContentStatusTwoLists", pre, trig);
    }

    [RelayCommand]
    private async Task PickPreTriggerAsync()
    {
        var added = await _userContent.PickAndSaveImagesAsync(ContentRole.PreTrigger);
        UpdateContentStatus();
        if (added > 0)
            await Shell.Current.DisplayAlertAsync(_loc.GetString("Alert_ShopTitle"), _loc.GetString("ImagesAddedPreTrigger", added), _loc.GetString("OK"));
    }

    [RelayCommand]
    private async Task PickTriggerAsync()
    {
        var added = await _userContent.PickAndSaveImagesAsync(ContentRole.Trigger);
        UpdateContentStatus();
        if (added > 0)
            await Shell.Current.DisplayAlertAsync(_loc.GetString("Alert_ShopTitle"), _loc.GetString("ImagesAddedTrigger", added), _loc.GetString("OK"));
    }

    [RelayCommand]
    private async Task GoToRoadmap()
    {
        await Shell.Current.GoToAsync("//RoadmapPage");
    }

    [RelayCommand]
    private async Task ShowHowItWorks()
    {
        await Shell.Current.GoToAsync(nameof(TriggerMethodInfoPage));
    }

    [RelayCommand]
    private async Task BuySession()
    {
        if (_userContent.CountPreTrigger == 0 || _userContent.CountTrigger == 0)
        {
            await Shell.Current.DisplayAlertAsync(_loc.GetString("Alert_ShopTitle"), _loc.GetString("NeedBothPreAndTrigger"), _loc.GetString("OK"));
            return;
        }
        int cost = 100;
        if (await _game.CanBuySessionAsync(cost))
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                _loc.GetString("Alert_ShopTitle"),
                _loc.GetString("Alert_BuyMessage", cost),
                _loc.GetString("Buy"),
                _loc.GetString("Cancel"));
            if (confirm)
            {
                await _game.PurchaseSessionAsync(cost);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await InitializeAsync();
                    await Shell.Current.GoToAsync(nameof(SessionPage));
                });
            }
        }
        else
        {
            await Shell.Current.DisplayAlertAsync(_loc.GetString("Alert_ShopTitle"), _loc.GetString("Alert_NotEnoughPoints"), _loc.GetString("OK"));
        }
    }
}
