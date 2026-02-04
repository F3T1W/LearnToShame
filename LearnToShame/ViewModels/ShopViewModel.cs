using LearnToShame.Services;

namespace LearnToShame.ViewModels;

public partial class ShopViewModel : ObservableObject
{
    private readonly GamificationService _game;
    private readonly DatabaseService _db;
    private readonly LocalizationService _loc = LocalizationService.Instance;

    [ObservableProperty]
    private int _points;

    public ShopViewModel(GamificationService game, DatabaseService db)
    {
        _game = game;
        _db = db;
    }

    public async Task InitializeAsync()
    {
        var progress = await _db.GetUserProgressAsync();
        Points = progress.CurrentPoints;
    }

    [RelayCommand]
    private async Task GoToRoadmap()
    {
        await Shell.Current.GoToAsync("//RoadmapPage");
    }

    [RelayCommand]
    private async Task BuySession()
    {
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
                await InitializeAsync();
                await Shell.Current.GoToAsync(nameof(SessionPage));
            }
        }
        else
        {
            await Shell.Current.DisplayAlertAsync(_loc.GetString("Alert_ShopTitle"), _loc.GetString("Alert_NotEnoughPoints"), _loc.GetString("OK"));
        }
    }
}
