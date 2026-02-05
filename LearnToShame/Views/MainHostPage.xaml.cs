using LearnToShame.Services;

namespace LearnToShame.Views;

public partial class MainHostPage : ContentPage
{
    private readonly RoadmapPage _roadmapPage;
    private readonly ShopPage _shopPage;
    private bool _showRoadmap = true;
    private bool _roadmapDataLoaded;

    public MainHostPage(RoadmapPage roadmapPage, ShopPage shopPage)
    {
        InitializeComponent();
        _roadmapPage = roadmapPage;
        _shopPage = shopPage;

        RoadmapTab.Clicked += (_, _) => ShowRoadmap();
        ShopTab.Clicked += (_, _) => ShowShop();
    }

    private async void OnLanguageClicked(object? sender, EventArgs e)
    {
        var langs = LocalizationService.Languages;
        var names = langs.Select(x => x.DisplayName).ToArray();
        var cancelText = LocalizationService.Instance.GetString("CancelButton");
        var action = await DisplayActionSheet(
            LocalizationService.Instance.GetString("Language"),
            cancelText,
            null,
            names);
        if (action == null || action == cancelText) return;
        var idx = Array.IndexOf(names, action);
        if (idx >= 0 && idx < langs.Length)
            LocalizationService.Instance.SetLanguage(langs[idx].Code);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateThemeButtonIcon();
        if (PartContent.Content == null)
        {
            PartContent.Content = _roadmapPage.Content;
            PartContent.BindingContext = _roadmapPage.BindingContext;
            SetTabColors(activeRoadmap: true);
            _showRoadmap = true;
            _ = LoadRoadmapIfNeededAsync();
        }
    }

    private void UpdateThemeButtonIcon()
    {
        ThemeButton.Text = Application.Current!.RequestedTheme == AppTheme.Dark ? "☀" : "🌙";
    }

    private async void OnThemeClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        var options = new[] { loc.GetString("Theme_Dark"), loc.GetString("Theme_Light"), loc.GetString("Theme_System") };
        var action = await DisplayActionSheet(loc.GetString("Theme"), loc.GetString("CancelButton"), null, options);
        if (action == null) return;
        if (action == loc.GetString("CancelButton")) return;

        if (action == loc.GetString("Theme_Dark"))
        {
            Preferences.Default.Set("AppTheme", "Dark");
            Application.Current!.UserAppTheme = AppTheme.Dark;
        }
        else if (action == loc.GetString("Theme_Light"))
        {
            Preferences.Default.Set("AppTheme", "Light");
            Application.Current!.UserAppTheme = AppTheme.Light;
        }
        else
        {
            Preferences.Default.Set("AppTheme", "System");
            Application.Current!.UserAppTheme = AppTheme.Unspecified;
        }
        UpdateThemeButtonIcon();
        SetTabColors(_showRoadmap);
    }

    private void ShowRoadmap()
    {
        if (!_showRoadmap)
        {
            _showRoadmap = true;
            PartContent.Content = _roadmapPage.Content;
            PartContent.BindingContext = _roadmapPage.BindingContext;
            SetTabColors(activeRoadmap: true);
            _ = LoadRoadmapIfNeededAsync();
        }
    }

    private async Task LoadRoadmapIfNeededAsync()
    {
        if (_roadmapDataLoaded) return;
        var vm = _roadmapPage.BindingContext as ViewModels.RoadmapViewModel;
        if (vm != null)
        {
            await vm.InitializeAsync();
            _roadmapDataLoaded = true;
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
