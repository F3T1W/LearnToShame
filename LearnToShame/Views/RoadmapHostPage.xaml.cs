using LearnToShame.Services;

namespace LearnToShame.Views;

public partial class RoadmapHostPage : ContentPage
{
    private readonly RoadmapPage _roadmapPage;
    private bool _roadmapDataLoaded;

    public RoadmapHostPage(RoadmapPage roadmapPage)
    {
        InitializeComponent();
        _roadmapPage = roadmapPage;
        PartContent.Content = _roadmapPage.Content;
        PartContent.BindingContext = _roadmapPage.BindingContext;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateThemeButtonIcon();
        _ = LoadRoadmapIfNeededAsync();
    }

    private void UpdateThemeButtonIcon()
    {
        ThemeButton.Text = Application.Current!.RequestedTheme == AppTheme.Dark ? "☀" : "🌙";
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

    private async void OnThemeClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        var options = new[] { loc.GetString("Theme_Dark"), loc.GetString("Theme_Light"), loc.GetString("Theme_System") };
        var action = await DisplayActionSheet(loc.GetString("Theme"), loc.GetString("CancelButton"), null, options);
        if (action == null || action == loc.GetString("CancelButton")) return;
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
    }
}
