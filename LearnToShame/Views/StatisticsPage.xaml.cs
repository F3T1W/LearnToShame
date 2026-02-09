using LearnToShame.ViewModels;

namespace LearnToShame.Views;

public partial class StatisticsPage : ContentPage
{
    public StatisticsPage(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadChartAsync();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged += OnThemeChanged;
    }

    protected override void OnParentChanging(ParentChangingEventArgs e)
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        base.OnParentChanging(e);
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        InteractiveChart.RefreshChartColors();
        RefreshChartFromViewModel();
    }

    private async Task LoadChartAsync()
    {
        var vm = BindingContext as StatisticsViewModel;
        if (vm == null) return;
        await vm.InitializeAsync();
        RefreshChartFromViewModel();
    }

    private void RefreshChartFromViewModel()
    {
        var vm = BindingContext as StatisticsViewModel;
        if (vm == null) return;
        InteractiveChart.SetSessions(vm.Sessions);
        InteractiveChart.RefreshChartColors();
    }

    /// <summary>Вызвать при переключении на вкладку «Статистика» — контент показывается без OnAppearing.</summary>
    public void RefreshChart()
    {
        RefreshChartFromViewModel();
    }
}
