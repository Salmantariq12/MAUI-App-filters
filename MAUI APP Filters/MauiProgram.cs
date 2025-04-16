using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MauiAppFilter;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MauiAppFilter.appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();
        builder.Services.AddSingleton<IConfiguration>(config);
        builder.Services.AddSingleton<ProxyService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<TimeUpPage>();

        return builder.Build();
	}
}
