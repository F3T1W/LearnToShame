namespace LearnToShame;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        Routing.RegisterRoute(nameof(SessionPage), typeof(SessionPage));
	}
}
