using System.Data;

namespace WebApi.Scripts;

/// <summary>
/// Database maintenance script
/// </summary>
public interface IDbMaintenanceScript
{
	/// <summary>
	/// The script to run
	/// </summary>
	public Task Run(IDbConnection dbConnection);
}
