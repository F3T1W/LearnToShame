using LearnToShame.Services;

namespace LearnToShame.Views;

public partial class TriggerMethodInfoPage : ContentPage
{
    public TriggerMethodInfoPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BodyLabel.Text = LocalizationService.Instance.GetString("HowItWorksBody");
    }
}
