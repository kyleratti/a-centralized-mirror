using System.Data;
using System.Reflection;
using Dapper;
using WebApi.Scripts;

namespace WebApi.HostedServices;

/// <summary>
/// Database maintenance service
/// </summary>
public class DbMaintenanceService : BackgroundService
{
	private readonly ILogger<DbMaintenanceService> _logger;
	private readonly IDbConnection _dbConnection;

	/// <summary>
	/// C'tor
	/// </summary>
	public DbMaintenanceService(
		ILogger<DbMaintenanceService> logger,
		IDbConnection dbConnection
	)
	{
		_logger = logger;
		_dbConnection = dbConnection;
	}

	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await CreateMaintenanceTablesIfNeeded();

		await foreach (var script in GetScriptsToRun().WithCancellation(stoppingToken))
		{
			var isSuccessful = await RunDbMaintenanceScript(script);

			if (!isSuccessful)
				return;
		}
	}

	private async Task<bool> RunDbMaintenanceScript(IDbMaintenanceScript script)
	{
		try
		{
			await script.Run(_dbConnection);

			var scriptName = script.GetType().Name;

			await _dbConnection.ExecuteAsync(
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

			await _dbConnection.ExecuteAsync(
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

	private async IAsyncEnumerable<IDbMaintenanceScript> GetScriptsToRun()
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

			var scriptHasRun = await _dbConnection.ExecuteScalarAsync<bool>(
				"SELECT EXISTS (SELECT 1 FROM DatabaseMaintenance WHERE ScriptName = @scriptName AND IsSuccessful = 1)",
				new { scriptName });

			if (scriptHasRun)
				continue;

			yield return script;
		}
	}

	private async Task CreateMaintenanceTablesIfNeeded()
	{
		try
		{
			var tableExists = await _dbConnection.ExecuteScalarAsync<bool>(
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

			await _dbConnection.ExecuteAsync(sql);
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "An error occurred while creating database maintenance tables.");
		}
	}
}
