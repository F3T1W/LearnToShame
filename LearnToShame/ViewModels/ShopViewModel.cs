namespace LearnToShame.ViewModels;

public partial class ShopViewModel : ObservableObject
{
    private readonly GamificationService _game;
    private readonly DatabaseService _db;

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
            bool confirm = await Shell.Current.DisplayAlertAsync("Shop", $"Buy training session for {cost} points?", "Buy", "Cancel");
            if (confirm)
            {
                await _game.PurchaseSessionAsync(cost);
                await InitializeAsync();
                await Shell.Current.GoToAsync(nameof(SessionPage));
            }
        }
        else
        {
            await Shell.Current.DisplayAlertAsync("Shop", "Not enough points! Complete more roadmap tasks.", "OK");
        }
    }
}
