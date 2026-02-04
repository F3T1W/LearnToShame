namespace LearnToShame.Views;

public partial class SessionPage : ContentPage
{
	public SessionPage(SessionViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SessionViewModel vm)
        {
            await vm.StartSessionAsync();
        }
    }
}
