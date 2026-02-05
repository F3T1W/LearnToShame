using LearnToShame.Helpers;

namespace LearnToShame;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		Resources["L"] = LocalizedStrings.Instance;
		ApplySavedTheme();
	}

	private static void ApplySavedTheme()
	{
		var saved = Preferences.Default.Get("AppTheme", "System");
		Application.Current!.UserAppTheme = saved switch
		{
			"Dark" => AppTheme.Dark,
			"Light" => AppTheme.Light,
			_ => AppTheme.Unspecified
		};
	}

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
