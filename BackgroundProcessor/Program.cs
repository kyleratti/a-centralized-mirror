using ApplicationData;
using ApplicationData.Services;
using BackgroundProcessor;
using SnooBrowser.Util;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddHostedService<BackgroundServiceWorker>();

		services.AddScoped<LinkProvider>();
		services.AddScoped<RedditCommentProvider>();

		services.AddSnooBrowserClient<RedditAuthParameterProvider, RedditAccessTokenProvider>();
	})
	.Build();

await host.RunAsync();