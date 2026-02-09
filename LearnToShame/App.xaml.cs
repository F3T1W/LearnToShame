using LearnToShame.Helpers;
using LearnToShame.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LearnToShame;

public partial class App : Application
{
	public static IServiceProvider? Services { get; private set; }
	internal static void SetServices(IServiceProvider serviceProvider) => Services = serviceProvider;

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
        var roadmapPage = Services!.GetRequiredService<RoadmapPage>();
        var shopPage = Services!.GetRequiredService<ShopPage>();
        var statisticsPage = Services!.GetRequiredService<StatisticsPage>();

        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            var roadmapHost = new RoadmapHostPage(roadmapPage);
            var shopHost = new ShopHostPage(shopPage);
            return new Window(new AppShellIOS(roadmapHost, shopHost));
        }
        var mainHost = new MainHostPage(roadmapPage, shopPage, statisticsPage);
        return new Window(new AppShell(mainHost));
    }
}
