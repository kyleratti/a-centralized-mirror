using System.Reflection;
using FruityFoundation.DataAccess.Abstractions;
using WebApi.Scripts;

namespace WebApi.HostedServices;

/// <summary>
/// Database maintenance service
/// </summary>
public class DbMaintenanceService : BackgroundService
{
	private readonly ILogger<DbMaintenanceService> _logger;
	private readonly IDbConnectionFactory _dbConnectionFactory;

	/// <summary>
	/// C'tor
	/// </summary>
	public DbMaintenanceService(
		ILogger<DbMaintenanceService> logger,
		IDbConnectionFactory dbConnectionFactory
	)
	{
		_logger = logger;
		_dbConnectionFactory = dbConnectionFactory;
	}

	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await using var connection = _dbConnectionFactory.CreateConnection();
		await CreateMaintenanceTablesIfNeeded(connection);

		await foreach (var script in GetScriptsToRun(connection).WithCancellation(stoppingToken))
		{
			var isSuccessful = await RunDbMaintenanceScript(connection, script);

			if (!isSuccessful)
				return;
		}
	}

	private async Task<bool> RunDbMaintenanceScript(IDatabaseConnection<ReadWrite> connection, IDbMaintenanceScript script)
	{
		try
		{
			await script.Run(connection);

			var scriptName = script.GetType().Name;

			await connection.Execute(
				"INSERT INTO DatabaseMaintenance (ScriptName, RanAt, IsSuccessful) VALUES (@scriptName, @ranAt, 1)",
				new
				{
					scriptName,
					ranAt = DateTimeOffset.UtcNow,
				});

			_logger.LogInformation("Database maintenance script {ScriptName} ran successfully.", scriptName);

			return true;
		}
		catch (Exception ex)
		{
			var scriptName = script.GetType().Name;

			_logger.LogError(ex, "An error occurred while running database maintenance script {ScriptName}.", scriptName);

			var errorDetails = new
			{
				version = 1,
				exception = GetExceptionDetails(ex),
			};

			await connection.Execute(
				"""
					INSERT INTO DatabaseMaintenance (ScriptName, RanAt, IsSuccessful, ErrorDetails)
					VALUES (@scriptName, @ranAt, 0, @errorDetails)
				""",
				new
				{
					scriptName,
					ranAt = DateTimeOffset.UtcNow,
					errorDetails = System.Text.Json.JsonSerializer.Serialize(errorDetails),
				});
		}

		return false;
	}

	private static object? GetExceptionDetails(Exception? ex)
	{
		if (ex is null)
			return null;

		return new
		{
			exceptionMessage = ex.Message,
			stackTrace = ex.StackTrace,
			innerException = GetExceptionDetails(ex?.InnerException),
		};
	}

	private async IAsyncEnumerable<IDbMaintenanceScript> GetScriptsToRun(IDatabaseConnection<ReadWrite> connection)
	{
		var allScripts = Assembly.GetExecutingAssembly().GetTypes()
			.Where(p => typeof(IDbMaintenanceScript).IsAssignableFrom(p) && !p.IsInterface)
			.Select(Activator.CreateInstance)
			.Cast<IDbMaintenanceScript>()
			.OrderBy(x => x.GetType().Name)
			.ToArray();

		foreach (var script in allScripts)
		{
			var scriptName = script.GetType().Name;

			var scriptHasRun = await connection.ExecuteScalar<bool>(
				"SELECT EXISTS (SELECT 1 FROM DatabaseMaintenance WHERE ScriptName = @scriptName AND IsSuccessful = 1)",
				new { scriptName });

			if (scriptHasRun)
				continue;

			yield return script;
		}
	}

	private async Task CreateMaintenanceTablesIfNeeded(IDatabaseConnection<ReadWrite> connection)
	{
		try
		{
			var tableExists = await connection.ExecuteScalar<bool>(
				"SELECT EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'DatabaseMaintenance')");

			if (tableExists)
				return;

			const string sql = """
				CREATE TABLE DatabaseMaintenance
				(
					RunId        INTEGER CONSTRAINT PK_DatabaseMaintenance PRIMARY KEY AUTOINCREMENT,
					ScriptName   TEXT    NOT NULL,
					RanAt        TEXT    NOT NULL,
					IsSuccessful INTEGER NOT NULL,
					ErrorDetails TEXT
				);
				""";

			await connection.Execute(sql);
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "An error occurred while creating database maintenance tables.");
		}
	}
}
