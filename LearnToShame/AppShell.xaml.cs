using LearnToShame.Views;

namespace LearnToShame;

public partial class AppShell : Shell
{
	/// <summary>Android: контент передаётся из CreateWindow, без обращения к DI из конструктора Shell.</summary>
	public AppShell(MainHostPage mainHostPage)
	{
		InitializeComponent();
		MainShellContent.Content = mainHostPage;
		Routing.RegisterRoute(nameof(SessionPage), typeof(SessionPage));
	}
}
