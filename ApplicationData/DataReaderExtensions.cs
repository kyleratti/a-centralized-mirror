using System.Data;
using System.Data.Common;

namespace ApplicationData;

public static class DataReaderExtensions
{
	public static async IAsyncEnumerable<T> SelectAsync<T>(this DbDataReader reader, Func<IDataReader, T> mapper)
	{
		await using var scopedReader = reader; // auto-dispose

		while (await scopedReader.ReadAsync())
		{
			yield return await Task.FromResult(mapper(scopedReader));
		}
	}
}