using BackgroundProcessor.Processors;

namespace BackgroundProcessor;

public class BackgroundServiceWorker : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<BackgroundServiceWorker> _logger;

	private readonly TimeSpan _startupDelay = TimeSpan.Zero; //TimeSpan.FromSeconds(10);
	private readonly TimeSpan _delayBetweenRuns = TimeSpan.FromSeconds(60);

	public BackgroundServiceWorker(IServiceProvider serviceProvider, ILogger<BackgroundServiceWorker> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Generate another state machine to prevent blocking the entire application
		// See https://github.com/dotnet/runtime/issues/36063
		await Task.Yield();

		await Task.Delay(_startupDelay, stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			using var scope = _serviceProvider.CreateScope();

			foreach (var processor in GetProcessors())
			{
				_logger.LogDebug("Creating processor {ProcessorName}", processor.FullName);

				var createdProcessor = (IBackgroundProcessor)ActivatorUtilities.CreateInstance(scope.ServiceProvider, processor);

				try
				{
					await createdProcessor.Process(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Background processor encountered an unhandled exception");
				}
			}

			_logger.LogDebug("All processors have run");

			await Task.Delay(_delayBetweenRuns, stoppingToken);
		}
	}

	private static IReadOnlyCollection<Type> GetProcessors() =>
		AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(x => !x.IsInterface && !x.IsAbstract && typeof(IBackgroundProcessor).IsAssignableFrom(x))
			.ToArray();
}