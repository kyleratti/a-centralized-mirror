using System.Data;

namespace ApplicationData;

public static class IDbConnectionExtensions
{
	public static IDbTransaction CreateTransaction(this IDbConnection connection, IsolationLevel isolationLevel)
	{
		if (connection.State == ConnectionState.Closed)
			connection.Open();

		return connection.BeginTransaction(isolationLevel);
	}
}
