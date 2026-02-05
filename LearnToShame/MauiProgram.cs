using Microsoft.Extensions.Logging;

namespace LearnToShame;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				// fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				// fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<RedditService>();
        builder.Services.AddSingleton<GamificationService>();

		builder.Services.AddTransient<RoadmapViewModel>();
		builder.Services.AddTransient<RoadmapPage>();
		
		builder.Services.AddTransient<ShopViewModel>();
		builder.Services.AddTransient<ShopPage>();
		
		builder.Services.AddTransient<SessionViewModel>();
		builder.Services.AddTransient<SessionPage>();

		builder.Services.AddTransient<MainHostPage>();
		builder.Services.AddTransient<RoadmapHostPage>();
		builder.Services.AddTransient<ShopHostPage>();

		builder.ConfigureMauiHandlers(handlers =>
		{
			handlers.AddHandler<LearnToShame.Views.SfSymbolView, LearnToShame.Handlers.SfSymbolViewHandler>();
		});

		var app = builder.Build();
		App.SetServices(app.Services);
		return app;
	}
}
