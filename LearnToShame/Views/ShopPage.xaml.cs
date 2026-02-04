namespace LearnToShame.Views;

public partial class ShopPage : ContentPage
{
	public ShopPage(ShopViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ShopViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
