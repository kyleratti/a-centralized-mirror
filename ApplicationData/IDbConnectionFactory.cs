using System.Data;

namespace ApplicationData;

public interface IDbConnectionFactory
{
	public ValueTask<IDbConnection> CreateReadOnlyConnection();
	public ValueTask<IDbConnection> CreateConnection();
}
