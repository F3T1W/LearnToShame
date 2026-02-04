namespace LearnToShame.Views;

public partial class RoadmapPage : ContentPage
{
	public RoadmapPage(RoadmapViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is RoadmapViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
