using Foundation;
using UIKit;

namespace LearnToShame;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		var result = base.FinishedLaunching(application, launchOptions);
		// Larger, centered-looking tab bar titles (e.g. Roadmap / Shop)
		UITabBarItem.Appearance.SetTitleTextAttributes(
			new UIStringAttributes { Font = UIFont.SystemFontOfSize(22, UIFontWeight.Medium) },
			UIControlState.Normal);
		return result;
	}
}
