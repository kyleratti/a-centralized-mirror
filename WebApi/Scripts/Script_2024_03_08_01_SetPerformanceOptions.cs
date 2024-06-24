using System.Data;
using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Set performance options
/// </summary>
public class Script_2024_03_08_01_SetPerformanceOptions : IDbMaintenanceScript
{
	/// <inheritdoc />
	public async Task Run(IDatabaseConnection<ReadWrite> dbConnection)
	{
		var journalMode = await dbConnection.ExecuteScalar<string?>("PRAGMA journal_mode");

		if (journalMode is null || !journalMode.Equals("WAL", StringComparison.OrdinalIgnoreCase))
			await dbConnection.Execute("PRAGMA journal_mode = WAL");

		var synchronous = await dbConnection.ExecuteScalar<string?>("PRAGMA synchronous");

		if (synchronous is null || !synchronous.Equals("NORMAL", StringComparison.OrdinalIgnoreCase))
			await dbConnection.Execute("PRAGMA synchronous = NORMAL");
	}
}
