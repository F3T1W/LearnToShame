using LearnToShame.Helpers;

namespace LearnToShame;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		Resources["L"] = LocalizedStrings.Instance;
	}

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
