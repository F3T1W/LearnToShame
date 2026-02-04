namespace LearnToShame.Views;

public partial class MainHostPage : ContentPage
{
    private readonly RoadmapPage _roadmapPage;
    private readonly ShopPage _shopPage;
    private bool _showRoadmap = true;

    public MainHostPage(RoadmapPage roadmapPage, ShopPage shopPage)
    {
        InitializeComponent();
        _roadmapPage = roadmapPage;
        _shopPage = shopPage;

        RoadmapTab.Clicked += (_, _) => ShowRoadmap();
        ShopTab.Clicked += (_, _) => ShowShop();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (PartContent.Content == null)
        {
            PartContent.Content = _roadmapPage.Content;
            PartContent.BindingContext = _roadmapPage.BindingContext;
            SetTabColors(activeRoadmap: true);
            _showRoadmap = true;
            _ = (_roadmapPage.BindingContext as ViewModels.RoadmapViewModel)?.InitializeAsync();
        }
    }

    private void ShowRoadmap()
    {
        if (!_showRoadmap)
        {
            _showRoadmap = true;
            PartContent.Content = _roadmapPage.Content;
            PartContent.BindingContext = _roadmapPage.BindingContext;
            SetTabColors(activeRoadmap: true);
            _ = (_roadmapPage.BindingContext as ViewModels.RoadmapViewModel)?.InitializeAsync();
        }
    }

    private void ShowShop()
    {
        if (_showRoadmap)
        {
            _showRoadmap = false;
            PartContent.Content = _shopPage.Content;
            PartContent.BindingContext = _shopPage.BindingContext;
            SetTabColors(activeRoadmap: false);
            _ = (_shopPage.BindingContext as ViewModels.ShopViewModel)?.InitializeAsync();
        }
    }

    private void SetTabColors(bool activeRoadmap)
    {
        var primary = (Color)Application.Current!.Resources["Primary"];
        var inactive = Application.Current.RequestedTheme == AppTheme.Dark
            ? (Color)Application.Current.Resources["White"]
            : (Color)Application.Current.Resources["Black"];
        RoadmapTab.TextColor = activeRoadmap ? primary : inactive;
        ShopTab.TextColor = activeRoadmap ? inactive : primary;
    }
}
