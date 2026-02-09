using LearnToShame.Helpers;
using LearnToShame.Services;

namespace LearnToShame.Views;

public partial class MainHostPage : ContentPage
{
    private readonly RoadmapPage _roadmapPage;
    private readonly ShopPage _shopPage;
    private readonly StatisticsPage _statisticsPage;
    private int _activeTab; // 0 = Roadmap, 1 = Shop, 2 = Statistics
    private bool _roadmapDataLoaded;

    public MainHostPage(RoadmapPage roadmapPage, ShopPage shopPage, StatisticsPage statisticsPage)
    {
        InitializeComponent();
        _roadmapPage = roadmapPage;
        _shopPage = shopPage;
        _statisticsPage = statisticsPage;

        LocalizationService.Instance.CultureChanged += (_, _) => UpdateTopBarTitle();

        var roadmapTap = new TapGestureRecognizer();
        roadmapTap.Tapped += (_, _) => ShowRoadmap();
        RoadmapTabBorder.GestureRecognizers.Add(roadmapTap);

        var shopTap = new TapGestureRecognizer();
        shopTap.Tapped += (_, _) => ShowShop();
        ShopTabBorder.GestureRecognizers.Add(shopTap);

        var statisticsTap = new TapGestureRecognizer();
        statisticsTap.Tapped += (_, _) => ShowStatistics();
        StatisticsTabBorder.GestureRecognizers.Add(statisticsTap);
    }

    private void UpdateTopBarTitle()
    {
        TopBarTitleLabel.Text = _activeTab switch
        {
            1 => LocalizedStrings.Instance.Shop,
            2 => LocalizedStrings.Instance.Statistics,
            _ => LocalizedStrings.Instance.Roadmap
        };
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
        UpdateTabIcons();
        if (PartContent.Content == null)
        {
            PartContent.Content = _roadmapPage.Content;
            PartContent.BindingContext = _roadmapPage.BindingContext;
            _activeTab = 0;
            SetTabColors(0);
            UpdateTopBarTitle();
            _ = LoadRoadmapIfNeededAsync();
        }
    }

    private void UpdateThemeButtonIcon()
    {
        ThemeButton.Text = Application.Current!.RequestedTheme == AppTheme.Dark ? "☀" : "🌙";
    }

    private void UpdateTabIcons()
    {
        var isDark = Application.Current!.RequestedTheme == AppTheme.Dark;
        RoadmapTabIcon.Source = isDark ? "icon_map_white" : "icon_map";
        ShopTabIcon.Source = isDark ? "icon_cart_white" : "icon_cart";
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
        UpdateTabIcons();
        SetTabColors(_activeTab);
    }

    private async void ShowRoadmap()
    {
        if (_activeTab != 0)
        {
            _activeTab = 0;
            await PartContent.FadeTo(0, 150, Easing.CubicOut);
            PartContent.Content = _roadmapPage.Content;
            PartContent.BindingContext = _roadmapPage.BindingContext;
            SetTabColors(0);
            UpdateTopBarTitle();
            await PartContent.FadeTo(1, 200, Easing.CubicIn);
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

    private async void ShowShop()
    {
        if (_activeTab != 1)
        {
            _activeTab = 1;
            await PartContent.FadeTo(0, 150, Easing.CubicOut);
            PartContent.Content = _shopPage.Content;
            PartContent.BindingContext = _shopPage.BindingContext;
            SetTabColors(1);
            UpdateTopBarTitle();
            await PartContent.FadeTo(1, 200, Easing.CubicIn);
            _ = (_shopPage.BindingContext as ViewModels.ShopViewModel)?.InitializeAsync();
        }
    }

    private async void ShowStatistics()
    {
        if (_activeTab != 2)
        {
            _activeTab = 2;
            await PartContent.FadeTo(0, 150, Easing.CubicOut);
            PartContent.Content = _statisticsPage.Content;
            PartContent.BindingContext = _statisticsPage.BindingContext;
            SetTabColors(2);
            UpdateTopBarTitle();
            await PartContent.FadeTo(1, 200, Easing.CubicIn);
            await ((_statisticsPage.BindingContext as ViewModels.StatisticsViewModel)?.InitializeAsync() ?? Task.CompletedTask);
            _statisticsPage.RefreshChart();
        }
    }

    private void SetTabColors(int activeTab)
    {
        var primarySubtle = ((Color)Application.Current!.Resources["Primary"]).WithAlpha(0.2f);
        var transparent = new SolidColorBrush(Colors.Transparent);
        var active = new SolidColorBrush(primarySubtle);

        RoadmapTabBorder.Background = activeTab == 0 ? active : transparent;
        ShopTabBorder.Background = activeTab == 1 ? active : transparent;
        StatisticsTabBorder.Background = activeTab == 2 ? active : transparent;
    }
}

