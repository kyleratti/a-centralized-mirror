using System.Data;
using FruityFoundation.Base.Structures;
using FruityFoundation.DataAccess.Abstractions;

namespace ApplicationData;

public sealed class LazyDbTransaction : IDisposable, IAsyncDisposable
{
	private readonly INonTransactionalDbConnection<ReadWrite> _connection;
	private readonly IsolationLevel _isolationLevel;

	private Maybe<IDatabaseTransactionConnection<ReadWrite>> _tx = Maybe.Empty<IDatabaseTransactionConnection<ReadWrite>>();

	private LazyDbTransaction(INonTransactionalDbConnection<ReadWrite> connection, IsolationLevel isolationLevel)
	{
		_connection = connection;
		_isolationLevel = isolationLevel;
	}

	public static LazyDbTransaction CreateFrom(INonTransactionalDbConnection<ReadWrite> connection, IsolationLevel isolationLevel)
	{
		var lazyTx = new LazyDbTransaction(connection, isolationLevel);

		return lazyTx;
	}

	public async Task<IDatabaseTransactionConnection<ReadWrite>> GetOrCreateTx(CancellationToken cancellationToken)
	{
		if (!_tx.Try(out var tx))
		{
			tx = await _connection.CreateTransaction(_isolationLevel, cancellationToken);
			_tx = Maybe.Create(tx);
		}

		return tx;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (!_tx.Try(out var tx))
			return;

		tx.Dispose();
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (!_tx.Try(out var tx))
			return;

		await tx.DisposeAsync();
	}
}
