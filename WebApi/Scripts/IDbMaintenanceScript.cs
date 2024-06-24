using System.Data;
using FruityFoundation.DataAccess.Abstractions;

namespace WebApi.Scripts;

/// <summary>
/// Database maintenance script
/// </summary>
public interface IDbMaintenanceScript
{
	/// <summary>
	/// The script to run
	/// </summary>
	public Task Run(IDatabaseConnection<ReadWrite> dbConnection);
}
