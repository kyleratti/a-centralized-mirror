using System.Data;
using Dapper;

namespace WebApi.Scripts;

/// <summary>
/// Set performance options
/// </summary>
public class Script_2024_03_08_01_SetPerformanceOptions : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDbConnection dbConnection)
	{
		var journalMode = await dbConnection.ExecuteScalarAsync<string?>("PRAGMA journal_mode");

		if (journalMode is null || !journalMode.Equals("WAL", StringComparison.OrdinalIgnoreCase))
			await dbConnection.ExecuteAsync("PRAGMA journal_mode = WAL");

		var synchronous = await dbConnection.ExecuteScalarAsync<string?>("PRAGMA synchronous");

		if (synchronous is null || !synchronous.Equals("NORMAL", StringComparison.OrdinalIgnoreCase))
			await dbConnection.ExecuteAsync("PRAGMA synchronous = NORMAL");
	}
}
