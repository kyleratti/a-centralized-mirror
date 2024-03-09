using System.Data;
using ApplicationData;
using Microsoft.Data.Sqlite;

namespace WebApi.Services;

/// <summary>
/// Database connection factory
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
	private readonly IConfiguration _configuration;

	/// <summary>
	/// C'tor
	/// </summary>
	public DbConnectionFactory(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	/// <inheritdoc />
	public ValueTask<IDbConnection> CreateConnection()
	{
		var connectionString = _configuration.GetConnectionString("DbConnection");

		if (string.IsNullOrEmpty(connectionString))
			throw new InvalidOperationException("DB Connection string is empty");

		return ValueTask.FromResult((IDbConnection)new SqliteConnection(connectionString));
	}

	/// <inheritdoc />
	public ValueTask<IDbConnection> CreateReadOnlyConnection()
	{
		var connectionString = _configuration.GetConnectionString("DbConnectionReadOnly");

		if (string.IsNullOrEmpty(connectionString))
			throw new InvalidOperationException("Read-only DB Connection string is empty");

		return ValueTask.FromResult((IDbConnection)new SqliteConnection(connectionString));
	}
}
